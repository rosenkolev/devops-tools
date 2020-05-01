using System;
using System.IO;
using System.Threading;
using System.Device.Gpio;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Configuration;
using Microsoft.CognitiveServices.Speech.Audio;

namespace Face.Presentation.App
{
    public sealed class Application
    {
        private const int RecognitionAttempts = 3;
        private const int SecondsDelayBetweenMotionDetection = 7;
        private const string PathToPicture = "/home/pi/face.jpeg";
        private const string PathToAudio = "/home/pi/face.wav";
        private static DateTime _lastAccessDate = DateTime.MinValue;
        private static object _locker = new object();
        private static AzureFaceClient _client;
        
        private readonly IConfiguration _configuration;

        public Application(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private AzureFaceClient Client
        {
            get
            {
                if (_client == null)
                {
                    var azureUrl = $"https://{_configuration["AzureRegion"]}.api.cognitive.microsoft.com";
                    Console.WriteLine("Azure url: " + azureUrl);
                    var azureClient = AzureFaceClient.Authenticate(azureUrl, _configuration["AzureFaceKey"]);
                    _client = new AzureFaceClient(azureClient);
                }

                return _client;
            }
        }

        public void Start()
        {
            AttachToPins(17, 27);
            Task.Run(() => SpeakAsync("The system is up and ready for work."));
            Thread.CurrentThread.Join();
        }

        public void AttachToPins(params int[] pins)
        {
            var controller = new GpioController(PinNumberingScheme.Logical);
            foreach (var pin in pins)
            {
                controller.OpenPin(pin, PinMode.Input);
                controller.RegisterCallbackForPinValueChangedEvent(pin, PinEventTypes.Rising, MotionDetectHandler);
            }
        }

        public void MotionDetectHandler(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            lock (_locker)
            {
                Console.WriteLine("Motion detected");

                var now = DateTime.Now;
                var timeDifference = now -_lastAccessDate;
                if (timeDifference.Seconds < SecondsDelayBetweenMotionDetection)
                {
                    Console.WriteLine("Skip this motion!");
                    return;
                }

                _lastAccessDate = now;
                Task.Run(ExecuteAsync);               
            }
        }

        public async Task ExecuteAsync()
        {
            var count = RecognitionAttempts;
            do
            {
                count--;
                if (TakePicture(PathToPicture))
                {
                    var person = await Client.IdentifyAsync(PathToPicture);
                    if (person != null)
                    {
                        Console.WriteLine("Greet person");
                        var text = "Hello, " + person.Name + ", how are you today?";
                        if (await SpeakAsync(text))
                        {
                            break;
                        }
                    }
                }

                Console.WriteLine("No person recognized in attempt " + count);

                Thread.Sleep(1000);
            }
            while(count > 0);
        }

        private bool TakePicture(string pathToFile)
        {
            var command = new Command("ffmpeg", "-y -f video4linux2 -i /dev/video0 -s 1024x768 -v warning -input_format mjpeg -vframes 1 " + pathToFile, Directory.GetCurrentDirectory());

            command.WaitForResult();

            return command.ExitCode == 0;
        }

        private async Task<bool> SpeakAsync(string text)
        {
            if (await TextToWavFileAsync(text, PathToAudio))
            {
                var command = new Command("aplay", PathToAudio, Directory.GetCurrentDirectory());

                command.WaitForResult();

                return command.ExitCode == 0;
            }

            return false;
        }

        private async Task<bool> TextToWavFileAsync(string text, string pathToFile)
        {
            var config = SpeechConfig.FromSubscription(_configuration["AzureSpeechKey"], _configuration["AzureRegion"]);
            using var audioConfig = AudioConfig.FromWavFileOutput(pathToFile);
            using var synthesizer = new SpeechSynthesizer(config, audioConfig);
            var result = await synthesizer.SpeakTextAsync(text);
            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine($"Speech synthesized to speaker for text [{text}]");
                return true;
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                }
            }

            return false;
        }
    }
}