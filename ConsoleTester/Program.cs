using System;
using System.IO;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {

            var client = new MTPSync.HttpMtpClient(null);

            Console.WriteLine("Starting");

            Console.WriteLine($"Is connected = {client.IsConnected}");

            Console.WriteLine(String.Join(">=<",client.List(null)));

            //client.Download("test.txt", "test.txt");
            client.Download("GeneralKeepassCommon.kdbx", "GeneralKeepassCommon.kdbx");

            Console.WriteLine("program finished");
            Console.ReadLine();

        }
    }
}
