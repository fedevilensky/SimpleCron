using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cronos;
using NLog;

namespace Cron
{
    public class CronWorker
    {
        private const int DefaultWaitingTime = 5 * MinuteToMilliseconds;
        private const int DayToMilliseconds = 86400000;
        private const int HourToMilliseconds = 3600000;
        private const int MinuteToMilliseconds = 60000;
        private const int SecondToMilliseconds = 1000;
        private readonly Dictionary<string, Commands> _commands = new Dictionary<string, Commands>();
        private FileSystemWatcher _watcher;
        private static readonly Regex JsonRegex = new Regex(@"^(?!commands_template.json).*\.json$");
        private static readonly Regex YamlRegex = new Regex(@"^(?!(commands_template.yml|commands_template.yaml)).*\.(yml|yaml)$");
        private static CronWorker _instance;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void Start()
        {
            if (_instance == null)
                _instance = new CronWorker();
        }

        private CronWorker()
        {
            Log.Info("Initializing");
            DoFirstFileSearch();
            InitFsWatcher();
            Log.Info("Finished initializing");
        }

        private void DoFirstFileSearch()
        {
            DoFirstJsonSearch();
            DoFirstYamlSearch();
        }

        private void DoFirstJsonSearch()
        {
            foreach (var path in Directory.EnumerateFiles(@"./", "*.json", SearchOption.AllDirectories))
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

        private void DoFirstYamlSearch()
        {
            foreach (var path in Directory.EnumerateFiles(@"./", "*.yaml", SearchOption.AllDirectories))
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
            foreach (var path in Directory.EnumerateFiles(@"./", "*.yml", SearchOption.AllDirectories))
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

        private void InitFsWatcher()
        {
            _watcher = new FileSystemWatcher(@"./")
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
            Log.Debug($"CommandFileRenamed: {e.FullPath}");
            var oldName = Path.GetFileName(e.OldFullPath);
            var newName = Path.GetFileName(e.FullPath);
            if (JsonRegex.IsMatch(newName))
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
            Log.Debug($"CommandFileDeleted: {e.FullPath}");

            var fileName = Path.GetFileName(e.FullPath);
            RemoveCommands(fileName);
        }

        private void CommandFileCreated(object source, FileSystemEventArgs e)
        {
            Log.Debug($"CommandFileCreated: {e.FullPath}");

            var fullPath = e.FullPath;
            AddCommands(fullPath);
        }

        private void CommandFileChanged(object source, FileSystemEventArgs e)
        {

            var fileName = Path.GetFileName(e.FullPath);
            if (!JsonRegex.IsMatch(fileName)) return;
            if (_commands.ContainsKey(fileName))
            {
                //May receive notification twice, so if it was not modified, it shouldn't do anything
                if (_commands[fileName].LastWriteTime == File.GetLastWriteTime(e.FullPath)) return;
                RemoveCommands(fileName);
            }

            if (AddCommands(e.FullPath))
            {
                Log.Debug($"CommandFileChanged: {e.FullPath}");
            }

        }

        private void RemoveCommands(string fileName)
        {
            if (!_commands.ContainsKey(fileName)) return;
            _commands[fileName].CancellationSource.Cancel();
            _commands.Remove(fileName);
        }

        private bool AddCommands(string fullPath)
        {
            try
            {
                var fileName = Path.GetFileName(fullPath);
                if (JsonRegex.IsMatch(fileName))
                {
                    //Should not add a command that already exists
                    return AddJsonCommands(fullPath, fileName);
                }
                if (YamlRegex.IsMatch(fileName))
                {
                    return AddYamlCommands(fullPath, fileName);
                }

                return false;
            }
            catch (Exception)
            {
                return false; //ignore
            }
        }

        private bool AddJsonCommands(string fullPath, string fileName)
        {
            if (_commands.ContainsKey(fileName)) return false;

            var cmd = Commands.LoadFromJson(fullPath);

            return AddCommandToDictAndRun(fileName, cmd);
        }
        private bool AddYamlCommands(string fullPath, string fileName)
        {
            if (_commands.ContainsKey(fileName)) return false;

            var cmd = Commands.LoadFromYaml(fullPath);

            return AddCommandToDictAndRun(fileName, cmd);
        }

        private bool AddCommandToDictAndRun(string fileName, Commands cmd)
        {
            if (cmd.commands.Count == 0) return true;

            cmd.CancellationSource = new CancellationTokenSource();

            _commands[fileName] = cmd;
            switch (cmd.command_type)
            {
                case Type.Timer:
                    RunPeriodicalTasks(cmd);
                    break;
                case Type.Cron:
                    RunCron(cmd);
                    break;
            }

            return true;
        }

        private void RunPeriodicalTasks(Commands cmd)
        {
            Task.Run(() =>
                {
                    while (true)
                    {
                        var tasks = MakePeriodicalTasks(cmd);
                        try
                        {
                            Task.WaitAll(tasks, cmd.CancellationSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        catch (Exception e)
                        {
                            Log.Fatal(e);
                        }
                    }
                }
                , cmd.CancellationSource.Token);

        }
        private void RunCron(Commands cmd)
        {
            Task.Run(() =>
                {
                    while (true)
                    {
                        DateTime? next;
                        try
                        {
                            next = cmd.CronExpression.GetNextOccurrence(DateTime.UtcNow);
                        }
                        catch (CronFormatException)
                        {
                            return;
                        }
                        catch (Exception e)
                        {
                            Log.Fatal(e);
                            return;
                        }

                        if (!next.HasValue) return;

                        Thread.Sleep(next.Value.Subtract(DateTime.UtcNow));
                        var tasks = MakeCronTasks(cmd);
                        try
                        {
                            Task.WaitAll(tasks, cmd.CancellationSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            return;
                        }
                        catch (Exception e)
                        {
                            Log.Fatal(e);
                        }
                    }
                }
                , cmd.CancellationSource.Token);
        }

        private Task[] MakeCronTasks(Commands cmd)
        {
            var tasks = new Task[cmd.commands.Count + 1];
            tasks[0] = Task.Delay(1 * SecondToMilliseconds);
            var i = 1;
            foreach (var command in cmd.commands)
            {
                tasks[i] = new Task(() => Runner.RunCommand(command));
                tasks[i++].Start();
            }

            return tasks;
        }


        private Task[] MakePeriodicalTasks(Commands json)
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
            return (ret > 0) ? ret : DefaultWaitingTime;
        }
    }
}
