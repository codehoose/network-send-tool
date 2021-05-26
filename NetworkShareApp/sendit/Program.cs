using NetworkShareLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sendit
{
    class Program
    {
        static void Main(string[] args)
        {
            var broadcaster = new Broadcaster(54000);
            //broadcaster.SayHello(54000);
        }
    }
}
