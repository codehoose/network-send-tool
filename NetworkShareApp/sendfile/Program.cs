using NetworkShareLib;
using System;

namespace sendfile
{
    class Program
    {
        static void Main(string[] args)
        {
            var hostname = args[0];
            var file = args[1];

            var transferFile = new TransferFile(file, hostname);
            transferFile.Start();
            Console.Write("Press any key");
            Console.ReadLine();
        }
    }
}
