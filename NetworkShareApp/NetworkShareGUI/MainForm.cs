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
        private string _fileToTransfer = "";
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

        private void AddToClientList(IPEndPoint client)
        {
            if (!lstNodes.Items.Contains(client))
            {
                lstNodes.Items.Add(client);
            }
        }

        private void Broadcaster_MessageReceived(object sender, BroadcastPayload e)
        {
            var broadcaster = sender as Broadcaster;

            switch(e.Message)
            {
                case BroadcastMessage.Hello:
                    // Send Acknowldge message
                    broadcaster.Acknowledge(e.Client);
                    CheckAndAdd(e.Client);
                    break;
                case BroadcastMessage.HelloAcknowledge:
                    // Add client to list
                    CheckAndAdd(e.Client);
                    break;
                case BroadcastMessage.Initiate:
                    var receiver = new ReceiveFile(54000);
                    receiver.TransferComplete += FileReceived_Complete;
                    receiver.Listen();
                    break;
                case BroadcastMessage.SendRequest:
                    if (MessageBox.Show($"Receive {e.Filename} from {e.Hostname}?", "Receive File?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        broadcaster.SendFileAcknowledgement(e.Client, e.Filename);
                    }
                    break;
                case BroadcastMessage.SendAcknowledge:
                    _broadcaster.InitiatingTransfer(e.Client);
                    var transfer = new TransferFile(_fileToTransfer, e.Client.ToString());
                    transfer.TransferComplete += Tranfer_Complete;
                    transfer.Start();
                    break;
            }
        }

        private void CheckAndAdd(IPEndPoint client)
        {
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
                Invoke((Action)(() => AddToClientList(client)));
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
                    //var hostname = client.Address.ToString();
                    //_broadcaster.InitiatingTransfer(client);
                    _fileToTransfer = ofd.FileName;
                    var hostName = $"{Environment.UserName}@{Environment.MachineName}";
                    _broadcaster.SendFileRequest(client, hostName, _fileToTransfer);

                    //var transfer = new TransferFile(ofd.FileName, hostname);
                    //transfer.TransferComplete += Tranfer_Complete;
                    //transfer.Start();
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
