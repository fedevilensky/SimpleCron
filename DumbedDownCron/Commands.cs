using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace DumbedDownCron
{
    public class Commands
    {
        public RepeatAfter repeat_after { get; set; } = new RepeatAfter();
        public List<string> commands { get; set; } = new List<string>();
        public CancellationTokenSource CancellationSource { get; set; }

        public static Commands LoadFromJson(string path = @".\commands.json")
        {
            try
            {
                using (var reader = new StreamReader(new FileStream(path, FileMode.Open)))
                {
                    var json = reader.ReadToEnd();
                    var commands = JsonConvert.DeserializeObject<Commands>(json);
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