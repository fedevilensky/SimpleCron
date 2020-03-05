cd %~dp0
dotnet publish -r win-x86 -c Release --self-contained=true DumbedDownCron /p:PublishSingleFile=true
pause