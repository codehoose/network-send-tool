using NetworkShareLib;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace NetworkShareGUI
{
    public partial class MainForm : Form
    {
        private string _fileToTransfer = "";
        private bool _tempZip = false;
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
                    var transfer = new TransferFile(_fileToTransfer, e.Client.Address.ToString());
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
                ofd.Multiselect = true;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    var client = lstNodes.SelectedItem as IPEndPoint;
                    _fileToTransfer = ofd.FileNames[0];
                    _tempZip = ofd.FileNames.Length > 1;

                    if (ofd.FileNames.Length > 0)
                    {
                        _fileToTransfer = MakeZipFile(ofd.FileNames);
                    }

                    var hostName = $"{Environment.UserName}@{Environment.MachineName}";
                    _broadcaster.SendFileRequest(client, hostName, _fileToTransfer);
                }
            }
        }

        private string MakeZipFile(string[] files)
        {
            var tempFile = Path.GetTempFileName().Replace(".tmp", "") + ".zip";
            using (var archive = ZipFile.Open(tempFile, ZipArchiveMode.Create))
            {
                foreach (var f in files)
                {
                    var filename = Path.GetFileName(f);
                    archive.CreateEntryFromFile(f, filename);
                }
            }

            return tempFile;
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
            if (_tempZip && _fileToTransfer.EndsWith(".zip"))
            {
                File.Delete(_fileToTransfer);
            }
        }
    }
}
