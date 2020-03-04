# DumbedDownCron
A dumbed down cron done in c#, no need to install

Just build it and run it. It will use any json places inside the folder the exe is placed, or any of it's subfolders. You can rename, delete, add or modify files on the fly and the program will know what to do, no need to restart it.

An example json file could be:
```
{
  "repeat_after"{
    "days":1,
    "hours":5,
    "seconds":300
  },
  "commands": ["notepad hello_world.txt", "code"]
}
```

The json must have a "commands" list, as seen on the commands_template.json file. "repeat_after" is optional.

Scheduling commands is not yet implemented.
