using System;
using System.IO;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {

            var client = new MTPSync.HttpMtpClient();

            Console.WriteLine("Starting");

            Console.WriteLine($"Is connected = {client.IsConnected}");

            Console.WriteLine(String.Join(">=<",client.List(null)));

            //client.Download("test.txt", "test.txt");
            client.Upload("test.txt", "test.txt");

            Console.WriteLine("program finished");
            Console.ReadLine();

        }
    }
}
