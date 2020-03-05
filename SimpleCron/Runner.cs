using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NLog;

namespace SimpleCron
{
    public static class Runner
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static string _shell;
        private static string _argumentFlagIndicator;
        private static string Shell
        {
            get
            {
                if (_shell == null)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                    {
                        _shell = "bash";
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        _shell = "bash";
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        _shell = "bash";
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        _shell = "cmd.exe";
                    }
                }
                return _shell;
            }
        }

        private static string ArgumentFlagIndicator
        {
            get
            {
                if (_argumentFlagIndicator == null)
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
                    {
                        _argumentFlagIndicator = "-";
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        _argumentFlagIndicator = "-";
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        _argumentFlagIndicator = "-";
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        _argumentFlagIndicator = "/";
                    }
                }
                
                return _argumentFlagIndicator;
            }
        }

        public static void RunCommand(string cmd, ProcessWindowStyle windowStyle = ProcessWindowStyle.Hidden)
        {
            var process = new Process();
            var startInfo = new ProcessStartInfo
            {
                WindowStyle = windowStyle,
                FileName = $"{Shell}",
                Arguments = $"{ArgumentFlagIndicator}c \"{cmd}\"",
                UseShellExecute = true,
                CreateNoWindow = windowStyle == ProcessWindowStyle.Hidden
            };
            process.StartInfo = startInfo;
            try
            {
                process.Start();
                Log.Debug($"command: {cmd}");
            }
            catch (Exception e)
            {
                Log.Info<string>($"could not run command {cmd}, Exception thrown:{e}");
            }
        }
    }
}