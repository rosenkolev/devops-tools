using System;
using System.Drawing;
using System.IO;
using Iot.Device.Media;

namespace Face.Presentation.App.Extensions
{
    public static class VideoDeviceExtensions
    {
        public static Bitmap CaptureBitmap(this VideoDevice device)
        {
            var colors = GetVideoDeviceColors(device.Capture(), device.Settings.PixelFormat, device.Settings.CaptureSize);
            return VideoDevice.RgbToBitmap(device.Settings.CaptureSize, colors);
        }

        private static Color[] GetVideoDeviceColors(MemoryStream stream, PixelFormat format, (uint Width, uint Height) size)
        {
            switch (format)
            {
                case PixelFormat.YUYV: return VideoDevice.YuyvToRgb(stream);
                case PixelFormat.YUV444: return VideoDevice.YuvToRgb(stream);
                case PixelFormat.NV12:
                case PixelFormat.YVU420: return VideoDevice.Nv12ToRgb(stream, size);
                case PixelFormat.YUV420: return VideoDevice.Yv12ToRgb(stream, size);
                default: throw new ArgumentException($"{format} pixel format transformation is not implemented.", nameof(format));
            }
        }
    }
}