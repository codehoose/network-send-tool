using NetworkShareLib;

namespace receivefile
{
    class Program
    {
        static void Main(string[] args)
        {
            var receiveFile = new ReceiveFile(54000);
            receiveFile.Listen();
        }
    }
}
