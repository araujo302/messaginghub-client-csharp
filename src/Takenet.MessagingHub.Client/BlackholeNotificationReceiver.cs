﻿using System.Threading.Tasks;
using Lime.Protocol;

namespace Takenet.MessagingHub.Client
{
    internal class BlackholeNotificationReceiver : INotificationReceiver
    {
        public Task ReceiveAsync(Notification notification)
        {
            return Task.FromResult(0);
        }
    }
}