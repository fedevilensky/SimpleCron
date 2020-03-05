using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cronos;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SimpleCron
{
    public enum Type { Cron, Timer }
    public class Commands
    {
        private CronExpression _cronExpression;

        [JsonConverter(typeof(StringEnumConverter))]
        public Type command_type { get; set; } = Type.Timer;
        public RepeatAfter repeat_after { get; set; } = new RepeatAfter();
        public List<string> commands { get; set; } = new List<string>();
        public CancellationTokenSource CancellationSource { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string cron_expression { get; set; }

        public CronExpression CronExpression
        {
            get
            {
                if (_cronExpression == null && cron_expression != null)
                {
                    _cronExpression = CronExpression.Parse(cron_expression, CronFormat.IncludeSeconds);
                }

                return _cronExpression;
            }
        }

        public static Commands LoadFromJson(string path = @".\commands.json")
        {
            try
            {
                using (var reader = new StreamReader(new FileStream(path, FileMode.Open)))
                {
                    var json = reader.ReadToEnd();
                    var commands = JsonConvert.DeserializeObject<Commands>(json);
                    commands.LastWriteTime = File.GetLastWriteTime(path);

                    return commands;
                }
            }
            catch (Exception)
            {
                return new Commands { repeat_after = new RepeatAfter { minutes = 5 }, commands = new List<string>() };
            }
        }
    }
}