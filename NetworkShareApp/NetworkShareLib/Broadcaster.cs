using System;
using System.IO;
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

        public const string HEL = nameof(HEL); // SAY HELLO / READY
        public const string INI = nameof(INI); // INITIATE FILE TRANSFER
        public const string ACK = nameof(ACK); // HELLO ACK
        public const string SND = nameof(SND); // SEND REQUEST
        public const string SOK = nameof(SOK); // SEND ACKNOWLEDGEMENT

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

        public void SendFileRequest(IPEndPoint client, string hostAndUser, string filename)
        {
            string trimmedFilename = Path.GetFileName(filename);

            string msg = $"{SND}\r\n{hostAndUser}\r\n{trimmedFilename}";
            _client.Send(Encoding.ASCII.GetBytes(msg), msg.Length, client);
        }

        public void SendFileAcknowledgement(IPEndPoint client, string filename)
        {
            string msg = $"{SOK}\r\n{filename}";
            _client.Send(Encoding.ASCII.GetBytes(msg), msg.Length, client);
        }

        public void InitiatingTransfer(IPEndPoint client)
        {
            _client.Send(Encoding.ASCII.GetBytes(INI), INI.Length, client);
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
                    // The msg can be 1 - 3 lines in length, depending on the 
                    // message type

                    var msgSplit = msg.Split("\r\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                    switch (msgSplit[0])
                    {
                        case INI:
                            OnMessageReceived(BroadcastMessage.Initiate, sender);
                            break;
                        case ACK:
                            OnMessageReceived(BroadcastMessage.HelloAcknowledge, sender);
                            break;
                        case SND:
                            OnMessageReceived(BroadcastMessage.SendRequest, sender, msgSplit[1], msgSplit[2]);
                            break;
                        case SOK:
                            OnMessageReceived(BroadcastMessage.SendAcknowledge, sender, msgSplit[1]);
                            break;
                        default:
                            OnMessageReceived(BroadcastMessage.Hello, sender);
                            break;
                    }
                }

                client.BeginReceive(Client_MessageReceived, client);
            }
        }

        private void OnMessageReceived(BroadcastMessage message,
                                       IPEndPoint client,
                                       string hostname = "",
                                       string filename = "")
        {
            MessageReceived?.Invoke(this,
                                    new BroadcastPayload(message, client, hostname, filename));
        }
    }
}
