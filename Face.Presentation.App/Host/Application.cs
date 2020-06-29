using System.Threading;
using System.Threading.Tasks;
using Face.Presentation.App.Controls;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Face.Presentation.App.Host
{
    public sealed class Application : IHost
    {
        private const int RecognitionAttempts = 3;
        private const string PathToPicture = "/home/pi/face.jpeg";
        
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly MotionDetectControl _motionDetectControl;
        private readonly SpeechControl _speechControl;
        private readonly CameraControl _cameraControl;
        private readonly FaceDetectControl _faceDetectControl;

        public Application(ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _motionDetectControl = new MotionDetectControl(logger);
            _speechControl = new SpeechControl(logger, _configuration["AzureSpeechKey"], _configuration["AzureRegion"]);
            _cameraControl = new CameraControl(logger);
            _faceDetectControl = new FaceDetectControl(logger, _configuration["AzureFaceKey"], _configuration["AzureRegion"]);

            _motionDetectControl.OnMotionDetected = () => Task.Run(OnMotionDetectedAsync);
        }

        public void Start()
        {
            Task.Run(() => _speechControl.SpeakAsync("The system is up and ready for work."));
            _motionDetectControl.AttachToPin(17);
            _motionDetectControl.AttachToPin(27);
            Thread.CurrentThread.Join();
        }

        public void Stop()
        {
            _motionDetectControl.DetachFromPin(17);
            _motionDetectControl.DetachFromPin(27);
            _faceDetectControl.Dispose();
        }

        public async Task OnMotionDetectedAsync()
        {
            var count = RecognitionAttempts;
            do
            {
                count--;
                if (_cameraControl.TakePicture(PathToPicture))
                {
                    var person = await _faceDetectControl.IdentifyAsync(PathToPicture);
                    if (person != null)
                    {
                        var text = "Hello, " + person.Name + ", how are you today?";
                        if (await _speechControl.SpeakAsync(text))
                        {
                            break;
                        }
                    }
                }

                _logger.LogInformation("No person recognized in attempt " + count);

                Thread.Sleep(1000);
            }
            while(count > 0);
        }
    }
}