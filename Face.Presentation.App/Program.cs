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
            var count = 1;
            while (count-- > 0)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var settings = new VideoConnectionSettings(busId: 0)
                    {
                        ExposureType = ExposureType.Auto
                    };

                    using var device = VideoDevice.Create(settings);

                    //var supportedFormat = device.GetSupportedPixelFormats().First();
                    //var supporterResolution = device.GetPixelFormatResolutions(supportedFormat).First();

                    // Change capture setting
                    //device.Settings.PixelFormat = supportedFormat;
                    //device.Settings.CaptureSize = supporterResolution;

                    Console.Write("Format" + device.Settings.PixelFormat.ToString());

                    // Convert pixel format
                    var bitmap = device.CaptureBitmap();

string path = Directory.GetCurrentDirectory();
                    bitmap.Save($"{path}/test.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                }
                else
                {
                    Console.WriteLine("Current OS is not supported!");
                    break;
                }
            }
        }
    }
}
