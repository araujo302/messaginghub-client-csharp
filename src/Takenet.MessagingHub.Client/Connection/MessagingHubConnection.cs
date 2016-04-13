﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Lime.Messaging.Resources;
using Lime.Protocol;
using Takenet.MessagingHub.Client.LimeProtocol;
using Lime.Protocol.Client;
using Lime.Protocol.Network;


namespace Takenet.MessagingHub.Client.Connection
{
    public sealed class MessagingHubConnection : IMessagingHubConnection
    {
        public TimeSpan SendTimeout { get; }

        public int MaxConnectionRetries { get; set; }

        public IOnDemandClientChannel OnDemandClientChannel { get; private set; }

        private readonly SemaphoreSlim _semaphore;
        private readonly IEstablishedClientChannelBuilder _establishedClientChannelBuilder;
        private readonly IOnDemandClientChannelFactory _onDemandClientChannelFactory;

        private static readonly TimeSpan ChannelDiscardedDelay = TimeSpan.FromMilliseconds(300);

        internal MessagingHubConnection(
            TimeSpan sendTimeout, 
            int maxConnectionRetries,
            IOnDemandClientChannelFactory onDemandClientChannelFactory,
            IEstablishedClientChannelBuilder establishedClientChannelBuilder)
        {
            _semaphore = new SemaphoreSlim(1);
            MaxConnectionRetries = maxConnectionRetries;
            SendTimeout = sendTimeout;
            _establishedClientChannelBuilder = establishedClientChannelBuilder;
            _onDemandClientChannelFactory = onDemandClientChannelFactory;
        }

        public bool IsConnected { get; private set; }

        public async Task ConnectAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (IsConnected)
                    throw new InvalidOperationException("The client is already started");

                OnDemandClientChannel = _onDemandClientChannelFactory.Create(_establishedClientChannelBuilder);
                OnDemandClientChannel.ChannelCreationFailedHandlers.Add(StopOnLimeExceptionAsync);
                OnDemandClientChannel.ChannelDiscardedHandlers.Add(ChannelDiscarded);

                for (var i = 0; i < MaxConnectionRetries; i++)
                {
                    if (await EnsureConnectionIsOkayAsync()) 
                    {
                        IsConnected = true;
                        return;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)));
                }

                throw new TimeoutException("Could not connect to server!");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DisconnectAsync()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!IsConnected) throw new InvalidOperationException("The client is not started");

                using (var cancellationTokenSource = new CancellationTokenSource(SendTimeout))
                {
                    await OnDemandClientChannel.FinishAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                }

                OnDemandClientChannel.DisposeIfDisposable();
                IsConnected = false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// In this context, a LimeException usually means that some credential information is wrong,
        /// and should be checked.
        /// </summary>
        /// <param name="failedChannelInformation">Information about the failure</param>
        private static Task<bool> StopOnLimeExceptionAsync(FailedChannelInformation failedChannelInformation)
        {
            return (!(failedChannelInformation.Exception is LimeException)).AsCompletedTask();
        }

        private async Task<bool> EnsureConnectionIsOkayAsync()
        {
            try
            {
                using (var cancellationTokenSource = new CancellationTokenSource(SendTimeout))
                {
                    var command = new Command
                    {
                        Method = CommandMethod.Get,
                        Uri = new LimeUri(UriTemplates.PING)
                    };

                    var result = await OnDemandClientChannel.ProcessCommandAsync(command, cancellationTokenSource.Token).ConfigureAwait(false);
                    return result.Status == CommandStatus.Success;
                }
            }
            catch(LimeException)
            {
                // A LimeException usually means that some credential information is wrong, so throw it to allow client to check
                throw;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static Task ChannelDiscarded(ChannelInformation channelInformation)
        {
            return Task.Delay(ChannelDiscardedDelay);
        }
    }
}