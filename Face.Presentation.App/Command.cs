using System;
using System.Diagnostics;
using System.Text;

namespace Face.Presentation.App
{
    internal class Command
    {
        internal StringBuilder lastStandardErrorOutput = new StringBuilder();
        internal StringBuilder lastStandardOutput = new StringBuilder();
        internal Process process = new Process();

        public Command(string commandPath, string arguments, string workingDirectory)
        {
            process.StartInfo = new ProcessStartInfo(commandPath);
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }

            process.ErrorDataReceived += (s, e) => Log(lastStandardErrorOutput, e.Data);
            process.OutputDataReceived += (s, e) => Log(lastStandardOutput, e.Data);
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
        }

        public string WaitForResult()
        {
            process.WaitForExit();
            
            return lastStandardOutput.ToString().Trim();;
        }

        public int ExitCode => process.ExitCode;

        private static void Log(StringBuilder output, string message)
        {
            var msg = message?.TrimEnd() ?? string.Empty;
            Console.WriteLine(msg);
            output.AppendLine(msg);
        }
    }
}