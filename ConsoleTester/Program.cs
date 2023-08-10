using System;
using System.IO;
using System.Collections.Generic;
using Gio;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {

            var client = new MTPSync.MediaDeviceClient();

            string path = @"Internal shared storage\Download\";


            Console.WriteLine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

            foreach (var item in client.List(path))
            {
                Console.WriteLine(
                    "item ="
                );
                Console.WriteLine(
                    item
                );

                if ( Path.GetExtension(item) == ".kdbx")
                    client.Download(path + item, @"C:\Users\rune\Desktop\" + item);
            }

            while (true);

        }
    }
}
