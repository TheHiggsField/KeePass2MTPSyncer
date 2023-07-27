using System;
using System.Collections.Generic;
using Gio;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {

            var gioDerper = new MTPSync.MediaDeviceClient();


            foreach(var item in gioDerper.List(""))
            {
                Console.WriteLine(
                    item
                );
            }

        }
    }
}
