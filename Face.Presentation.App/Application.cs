using System;
using System.Threading;
using System.Threading.Tasks;
using System.Device.Gpio;
using Iot.Device.Media;
using Face.Presentation.App.Extensions;
using System.Drawing;

namespace Face.Presentation.App
{
    public sealed class Application
    {
        private const int MotionPin = 17;

        public void Start(int miliseconds)
        {
            var controller = new GpioController(PinNumberingScheme.Logical);
            controller.OpenPin(MotionPin, PinMode.Input);
            controller.RegisterCallbackForPinValueChangedEvent(MotionPin, PinEventTypes.Rising, MotionDetectHandler);
            Thread.CurrentThread.Join();
        }

        public void MotionDetectHandler(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            var azureClient = AzureFaceClient.Authenticate("", "");
            var client = new AzureFaceClient(azureClient);

            Console.WriteLine("Motion detected");
            var bitmap = TakePicture();
            client.Identify(bitmap).Wait();
            
            //Thread.Sleep(500);
            
            //var bitmap2 = TakePicture();
            //client.Identify(bitmap2);
        }

        public Bitmap TakePicture()
        {
            Console.WriteLine("Taking picture");
            var settings = new VideoConnectionSettings(busId: 0);
            using var device = VideoDevice.Create(settings);

            // Convert pixel format
            return device.CaptureBitmap();
        }
    }
}