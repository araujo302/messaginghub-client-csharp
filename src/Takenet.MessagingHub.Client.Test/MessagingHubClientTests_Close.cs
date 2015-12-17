﻿using Lime.Protocol;
using Lime.Protocol.Network;
using NSubstitute;
using NUnit.Framework;
using Shouldly;
using System;
using System.Threading;

namespace Takenet.MessagingHub.Client.Test
{
    internal class MessagingHubClientTests_Close : MessagingHubClientTestBase
    {
        [SetUp]
        protected override void Setup()
        {
            base.Setup();
        }

        [Test]
        public void Start_Then_Stop_Should_Finish_Session_With_Success()
        {
            //Arrange
            ClientChannel.WhenForAnyArgs(c => c.SendFinishingSessionAsync()).Do(c => ClientChannel.State.Returns(SessionState.Finished));

            ClientChannel.State.Returns(SessionState.Established);
            MessagingHubClient.UsingAccessKey("login", "key");
            MessagingHubClient.StartAsync().Wait(); 

            // Act
            MessagingHubClient.StopAsync().Wait();

            // Assert
            ClientChannel.State.ShouldBe(SessionState.Finished);
        }

        [Test]
        public void Stop_Without_Start_Should_Throw_Exception()
        {
            //Arrange
            MessagingHubClient.UsingAccessKey("login", "key");
            
            // Act // Assert
            Should.ThrowAsync<InvalidOperationException>(async () => await MessagingHubClient.StopAsync()).Wait();
        }

        [Test]
        public void Start_With_Session_Failed_Should_Stop_With_Success()
        {
            //Arrange
            ClientChannel.State.Returns(SessionState.Failed);

            var transport = Substitute.For<ITransport>();
            ClientChannel.Transport.Returns(transport);

            MessagingHubClient.UsingAccessKey("login", "key");
            MessagingHubClient.StartAsync().Wait();

            // Act
            MessagingHubClient.StopAsync().Wait();

            // Assert
            ClientChannel.State.ShouldBe(SessionState.Failed);
            transport.CloseAsync(CancellationToken.None).Wait();
        }
    }
}
