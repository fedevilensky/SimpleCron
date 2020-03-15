cd %~dp0
dotnet publish -r osx-x64 -c Release --self-contained=true DumbedDownCron /p:PublishSingleFile=true
pause