﻿using System.Threading.Tasks;
using Lime.Protocol;
using Takenet.MessagingHub.Client.Receivers;

namespace Takenet.MessagingHub.Client.Senders
{
    /// <summary>
    /// Proxy used to send notifications to the Messaging Hub
    /// </summary>
    public interface INotificationSender
    {
        Task SendNotificationAsync(Notification notification);
    }
}