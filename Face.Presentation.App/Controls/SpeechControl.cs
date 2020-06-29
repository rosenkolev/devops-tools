using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;

namespace Face.Presentation.App.Controls
{
    public sealed class SpeechControl
    {
        private const string PathToAudio = "/home/pi/face.wav";

        private readonly SpeechConfig _config;
        private readonly ILogger _logger;

        public SpeechControl(ILogger logger, SpeechConfig config)
        {
            _config = config;
            _logger = logger;
        }

        public SpeechControl(ILogger logger, string key, string region)
            : this(logger, GetConfig(logger, key, region))
        {
        }

        private static SpeechConfig GetConfig(ILogger logger, string key, string region)
        {
            logger.LogDebug($"craete speech config region:{region}({key})");
            var config = SpeechConfig.FromSubscription(key, region);
            logger.LogDebug(config.EndpointId);
            logger.LogDebug(config.OutputFormat.ToString());
            return config;
        }

        public async Task<bool> SpeakAsync(string text)
        {
            if (await TextToWavFileAsync(text, PathToAudio))
            {
                return Play(PathToAudio);
            }

            return false;
        }

        private bool Play(string pathToFile)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo("aplay");
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Arguments = pathToFile;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
            process.ErrorDataReceived += (s, e) => _logger.LogError(e.Data);
            process.OutputDataReceived += (s, e) => _logger.LogDebug(e.Data);
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
            process.WaitForExit();
            return process.ExitCode == 0;
        }

        private async Task<bool> TextToWavFileAsync(string text, string pathToFile)
        {
            using var audioConfig = AudioConfig.FromWavFileOutput(pathToFile);
            using var synthesizer = new SpeechSynthesizer(_config, audioConfig);
            var result = await synthesizer.SpeakTextAsync(text);
            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                _logger.LogInformation($"Speech synthesized to speaker for text [{text}]");
                return true;
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                _logger.LogError($"CANCELED: Reason={cancellation.Reason}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    _logger.LogError($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                    _logger.LogError($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                }
            }

            return false;
        }
    }
}