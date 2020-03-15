cd %~dp0
dotnet publish -r win-x64 -c Release --self-contained=true SimpleCron /p:PublishSingleFile=true
pause