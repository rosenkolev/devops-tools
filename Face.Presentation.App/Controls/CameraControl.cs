using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Face.Presentation.App.Controls
{
    public sealed class CameraControl
    {
        private readonly ILogger _logger;

        public CameraControl(ILogger logger)
        {
            _logger = logger;
        }
        
        public bool TakePicture(string pathToFile)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo("ffmpeg");
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Arguments = "-y -f video4linux2 -i /dev/video0 -s 1024x768 -v warning -input_format mjpeg -vframes 1 " + pathToFile;
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
    }
}