# DumbedDownCron
A dumbed down cron done in c#, no need to install

Just run it. It will use any json (except for commands_template.json) placed inside the folder the exe is placed in, or any of it's subfolders. You can rename, delete, add or modify files on the fly and the program will know what to do, no need to restart it.

An example json file could be:
```
{
  "command_type":"timer"
  "repeat_after"{
    "days":1,
    "hours":5,
    "seconds":300
  },
  "commands": [
    "notepad hello_world.txt",
    "code"
    ]
}
```
Or
```
{
  "command_type":"cron",
  "cron_expression":"0 */3 * ? * *",
  "commands":[
    "code",
    "notepad"
  ]
}
```

The json **must** have a "commands" list, as seen on the commands_template.json file.<br>
By default, the task will be treated as if it's "command_type" was a timer. And as if the timer was set to repeat after 5 minutes have passed.

If you wish to use a cron expression, just use `"command_type": "cron"` and `"cron_expression":$your_expression` as seen in the example above. Should there be any problem with the expression, the json will be ignored.

### Up next 
I will try and extend it to work on a unix like environments (GNU/linux, macOS, et al.)