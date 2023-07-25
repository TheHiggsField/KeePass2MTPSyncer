using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MTPSync;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var swirp = new MTPSyncExt();
            
            Console.WriteLine(
                swirp.SyncDatabases()
            );

        }
    }
}
