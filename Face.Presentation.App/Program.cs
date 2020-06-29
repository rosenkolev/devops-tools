using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Face.Presentation.App.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Face.Presentation.App
{
    public class Program
    {
        private static IHost _host = null;
        
        private static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        public static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine("Current os supported");
                AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
                Build((config, logger) => new Application(logger, config)).Start();
            }
            else
            {
                Console.WriteLine("Current OS is not supported!");
            }
        }

        public static void OnProcessExit(object sender, EventArgs e)
        {
            if (_host != null)
            {
                _host.Stop();
            }
        }

        private static IHost Build(Func<IConfiguration, ILogger, IHost> initialize)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AssemblyDirectory)
                .AddJsonFile("appsettings.json", false, false)
                .Build();

            Console.WriteLine(config.ToString());

            var loggerFactory = LoggerFactory.Create(builder =>
                builder
                    .SetMinimumLevel(LogLevel.Debug)
                    .AddConsole());

            var logger = loggerFactory.CreateLogger(string.Empty);

            _host = initialize(config, logger);

            Console.WriteLine("Application builded");

            return _host;
        }
    }
}
