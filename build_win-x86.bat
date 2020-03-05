cd %~dp0
dotnet publish -r win-x86 -c Release /p:PublishSingleFile=true
pause