using System;
using System.Diagnostics;

namespace DumbedDownCron
{
    public class Runner
    {
        public static void RunCommand(string cmd, ProcessWindowStyle windowStyle = ProcessWindowStyle.Hidden)
        {
            var process = new Process();
            var startInfo = new ProcessStartInfo
            {
                WindowStyle = windowStyle,
                FileName = "cmd.exe",
                Arguments = $"/c {cmd}",
                UseShellExecute = true,
                CreateNoWindow = windowStyle == ProcessWindowStyle.Hidden
            };
            process.StartInfo = startInfo;
            try
            {
                process.Start();
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}