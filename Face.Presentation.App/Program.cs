using System;
using System.IO;
using System.Runtime.InteropServices;
using Iot.Device.Media;
using Face.Presentation.App.Extensions;

namespace Face.Presentation.App
{
    public class Program
    {
        static void Main(string[] args)
        {
             if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                new Application().Start(500);
            }
            else
            {
                Console.WriteLine("Current OS is not supported!");
            }
        }
    }
}
