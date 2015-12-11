﻿using Lime.Protocol;
using System.Threading.Tasks;

namespace Takenet.MessagingHub.Client
{
    public interface IMessageReceiver
    {
        Task ReceiveAsync(Message message);
    }
}
