using System;
using System.Linq;
using System.Threading;
using NLog;
using SimpleCron;

namespace DumbedDownCron
{
    static class Program
    {
        static void Main(string[] args)
        {
            var minLevel = LogLevel.Info;

            var debugMode = args.Any(c => c.Equals("debug", StringComparison.CurrentCultureIgnoreCase));

            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            var logFile = new NLog.Targets.FileTarget("logfile") { FileName = ".log" };
            var logConsole = new NLog.Targets.ConsoleTarget("logconsole");

            // Rules for mapping loggers to targets            
            if (debugMode)
            {
                minLevel = LogLevel.Debug;
                config.AddRule(LogLevel.Trace, LogLevel.Fatal, logConsole);
            }
            config.AddRule(minLevel, LogLevel.Fatal, logFile);


            // Apply config           
            LogManager.Configuration = config;

            //
            Cron.Start();
            Thread.Sleep(Timeout.Infinite);
        }
    }
}
