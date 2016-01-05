﻿using Lime.Protocol;
using Lime.Protocol.Client;
using Lime.Protocol.Network;
using Lime.Protocol.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Takenet.MessagingHub.Client
{
    internal class LimeSessionProvider : ILimeSessionProvider
    {
       
        public async Task EstablishSessionAsync(IClientChannel clientChannel, Uri endPoint, Identity identity, Authentication authentication, CancellationToken cancellationToken)
        {
            await clientChannel.Transport.OpenAsync(endPoint, cancellationToken).ConfigureAwait(false);

            if (!clientChannel.Transport.IsConnected)
            {
                throw new Exception("Could not open connection");
            }

            await clientChannel.EstablishSessionAsync(
                            _ => SessionCompression.None,
                            _ => SessionEncryption.TLS,
                            identity,
                            (_, __) => authentication,
                            Environment.MachineName,
                            cancellationToken);
        }

        public async Task FinishSessionAsync(IClientChannel clientChannel, CancellationToken cancellationToken)
        {
            if (IsSessionEstablished(clientChannel))
            {
                await clientChannel.SendFinishingSessionAsync();
            }

            if (clientChannel.Transport.IsConnected)
            {
                await clientChannel.Transport.CloseAsync(cancellationToken);
            }
        }

        public bool IsSessionEstablished(IClientChannel clientChannel)
        {
            return clientChannel.Transport.IsConnected && clientChannel.State == SessionState.Established;
        }
    }
}