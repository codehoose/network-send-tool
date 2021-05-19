using System;
using System.Net;

namespace NetworkShareLib
{
    public class BroadcastPayload : EventArgs
    {
        public BroadcastMessage Message { get; }

        public IPEndPoint Client { get; }

        public BroadcastPayload(BroadcastMessage message, IPEndPoint client)
        {
            Message = message;
            Client = client;
        }
    }
}
