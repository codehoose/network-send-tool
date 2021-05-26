using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkShareLib
{
    public class Broadcaster
    {
        // Message types:
        //              HEL - I'm new to the network
        //              CON - Confirm that you are still connected
        //              ACK - Acknowledge that everything is OK

        public const string HEL = nameof(HEL);
        public const string CON = nameof(CON);
        public const string ACK = nameof(ACK);

        private readonly UdpClient _client;
        private readonly int _port;

        public EventHandler<BroadcastPayload> MessageReceived;

        public Broadcaster(int port = 54000)
        {
            _port = port;
            _client = new UdpClient(_port);
        }

        public void SayHello()
        {
            var helloString = Encoding.ASCII.GetBytes(HEL);
            _client.Send(helloString,
                         helloString.Length, 
                         new IPEndPoint(IPAddress.Broadcast, _port));
        }

        public void Listen()
        {
            _client.BeginReceive(Client_MessageReceived, _client);
        }

        public void Acknowledge(IPEndPoint client)
        {
            _client.Send(Encoding.ASCII.GetBytes(ACK), ACK.Length, client);
        }

        private void Client_MessageReceived(IAsyncResult result)
        {
            if (result.IsCompleted)
            {
                var sender = new IPEndPoint(IPAddress.Any, 0);
                var client = result.AsyncState as UdpClient;
                var received = client.EndReceive(result, ref sender);

                if (received.Length > 0)
                {
                    var msg = Encoding.ASCII.GetString(received);
                    switch (msg)
                    {
                        case CON:
                            OnMessageReceived(BroadcastMessage.Confirm, sender);
                            break;
                        case ACK:
                            OnMessageReceived(BroadcastMessage.Acknowledge, sender);
                            break;
                        default:
                            OnMessageReceived(BroadcastMessage.Hello, sender);
                            break;
                    }
                }

                client.BeginReceive(Client_MessageReceived, client);
            }
        }

        private void OnMessageReceived(BroadcastMessage message, IPEndPoint client)
        {
            MessageReceived?.Invoke(this,
                                    new BroadcastPayload(message, client));
        }
    }
}
