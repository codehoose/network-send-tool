using NetworkShareLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetworkShareGUI
{
    public partial class MainForm : Form
    {
        private Broadcaster _broadcaster;

        public MainForm()
        {
            InitializeComponent();
            InitBroadcaster();
        }

        private void InitBroadcaster()
        {
            _broadcaster = new Broadcaster();
            _broadcaster.SayHello();
            _broadcaster.Listen();
            _broadcaster.MessageReceived += Broadcaster_MessageReceived;
        }

        private void Broadcaster_MessageReceived(object sender, BroadcastPayload e)
        {
            var broadcaster = sender as Broadcaster;

            switch(e.Message)
            {
                case BroadcastMessage.Hello:
                    // Send Acknowldge message
                    broadcaster.Acknowledge(e.Client);
                    break;
                case BroadcastMessage.Acknowledge:
                    // Add client to list
                    // Filter this out....
                    IPEndPoint client = e.Client;

                    var infs = NetworkInterface.GetAllNetworkInterfaces();
                    bool found = false;
                    foreach (var i in infs)
                    {
                        var addrs = i.GetIPProperties();
                        foreach (var ip in addrs.UnicastAddresses)
                        {
                            if (ip.Address.Equals(client.Address))
                            {
                                found = true;
                                break;
                            }
                        }

                        if (found)
                            break;
                    }

                    if (!found)
                    {
                        Invoke((Action)(() => lstNodes.Items.Add(e.Client)));
                    }

                    break;
                case BroadcastMessage.Initiate:
                    var receiver = new ReceiveFile(54000);
                    receiver.TransferComplete += FileReceived_Complete;
                    receiver.Listen();
                    break;
            }
        }

        private void mnuSendFile_Click(object sender, EventArgs e)
        {
            if (lstNodes.SelectedItem == null)
            {
                MessageBox.Show("Please select an item");
            }
            else
            {
                var ofd = new OpenFileDialog();
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var client = lstNodes.SelectedItem as IPEndPoint;
                    var hostname = client.Address.ToString();

                    _broadcaster.InitiatingTransfer(client);

                    var transfer = new TransferFile(ofd.FileName, hostname);
                    transfer.TransferComplete += Tranfer_Complete;
                    transfer.Start();
                }
            }
        }

        private void FileReceived_Complete(object sender, EventArgs e)
        {
            var receiveFile = sender as ReceiveFile;
            receiveFile.Stop();
            MessageBox.Show("Transfer complete!");
        }

        private void Tranfer_Complete(object sender, EventArgs e)
        {
            MessageBox.Show("Transfer complete!");
        }
    }
}
