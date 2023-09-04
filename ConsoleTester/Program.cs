using System;
using System.IO;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {


            string path = @"This PC\Internal shared storage\Download\";
            var client = new MTPSync.MediaDeviceClient(path);


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
