using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;

namespace DumbedDownCron
{
    class PseudoCron
    {
        private int defaultWaitingTime = 5 * MinuteToMilliseconds;
        private const int DayToMilliseconds = 86400000;
        private const int HourToMilliseconds = 3600000;
        private const int MinuteToMilliseconds = 60000;
        private const int SecondToMilliseconds = 1000;
        private readonly Dictionary<string, Commands> _commands = new Dictionary<string, Commands>();
        private FileSystemWatcher _watcher;
        private const string FileRegex = @"^(?!commands_template.json).*\.json$";
        private static readonly Regex Regex = new Regex(FileRegex);

        public PseudoCron()
        {
            DoFirstFileSearch();
            InitFsWatcher();
        }

        private void DoFirstFileSearch()
        {

            foreach (var path in Directory.EnumerateFiles(@".\", "*.json", SearchOption.AllDirectories))
            {

                try
                {
                    AddCommands(path);
                }
                catch (Exception)
                {
                    //ignore
                }

                
            }
        }

        private void RunTasks(Commands cmd)
        {
            Task.Run(() =>
                {
                    while (true)
                    {
                        var tasks = MakeTasks(cmd);
                        try
                        {
                            Task.WaitAll(tasks, cmd.CancellationSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        catch (Exception)
                        {
                            // ignored
                        }
                    }
                }
                , cmd.CancellationSource.Token);

        }

        private void InitFsWatcher()
        {
            _watcher = new FileSystemWatcher(@".\")
            {
                IncludeSubdirectories = true,
                Filter = "*.json",
                NotifyFilter = NotifyFilters.LastAccess
                               | NotifyFilters.LastWrite
                               | NotifyFilters.FileName
                               | NotifyFilters.DirectoryName,
                EnableRaisingEvents = true
            };

            _watcher.Changed += CommandFileChanged;
            _watcher.Created += CommandFileCreated;
            _watcher.Deleted += CommandFileDeleted;
            _watcher.Renamed += CommandFileRenamed;
        }

        private void CommandFileRenamed(object source, RenamedEventArgs e)
        {
            var oldName = Path.GetFileName(e.OldFullPath);
            var newName = Path.GetFileName(e.FullPath);
            if (Regex.IsMatch(newName))
            {
                _commands[newName] = _commands[oldName];
            }
            else
            {
                _commands[oldName].CancellationSource.Cancel();
            }
            _commands.Remove(oldName);
        }

        private void CommandFileDeleted(object source, FileSystemEventArgs e)
        {
            var fileName = Path.GetFileName(e.FullPath);
            RemoveCommands(fileName);
        }

        private void RemoveCommands(string fileName)
        {
            if (!_commands.ContainsKey(fileName)) return;
            _commands[fileName].CancellationSource.Cancel();
            _commands.Remove(fileName);
        }

        private void CommandFileCreated(object source, FileSystemEventArgs e)
        {
            var fullPath = e.FullPath;
            AddCommands(fullPath);
        }

        private void AddCommands(string fullPath)
        {
            try
            {
                var fileName = Path.GetFileName(fullPath);
                if (!Regex.IsMatch(fileName))
                    return;
                var cmd = Commands.LoadFromJson(fullPath);
                if (cmd.commands.Count == 0)
                    return;
                cmd.CancellationSource = new CancellationTokenSource();

                _commands[fileName] = cmd;

                RunTasks(cmd);
            }
            catch (Exception)
            {
                //ignore
            }
        }

        private void CommandFileChanged(object source, FileSystemEventArgs e)
        {
            var fileName = Path.GetFileName(e.FullPath);
            if (!Regex.IsMatch(fileName)) return;
            RemoveCommands(fileName);
            AddCommands(e.FullPath);
        }

        private Task[] MakeTasks(Commands json)
        {
            var tasks = new Task[json.commands.Count + 1];
            var waitingTime = CalculateWaitingTime(json.repeat_after);
            tasks[0] = Task.Delay(waitingTime);
            var i = 1;
            foreach (var command in json.commands)
            {
                tasks[i] = new Task(() => Runner.RunCommand(command));
                tasks[i++].Start();
            }

            return tasks;
        }

        private int CalculateWaitingTime(RepeatAfter repeatAfter)
        {
            var ret = -1;
            if (repeatAfter != null)
            {
                ret = repeatAfter.days * DayToMilliseconds
                          + repeatAfter.hours * HourToMilliseconds
                          + repeatAfter.minutes * MinuteToMilliseconds
                          + repeatAfter.seconds * SecondToMilliseconds;
            }
            return (ret > 0) ? ret : defaultWaitingTime;
        }
    }
}
