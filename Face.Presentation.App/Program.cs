using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;

namespace Face.Presentation.App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var config = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", true, false)
                    .Build();

                new Application(config).Start();
            }
            else
            {
                Console.WriteLine("Current OS is not supported!");
            }
        }
    }
}
