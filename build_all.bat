cd %~dp0
echo "building linux-x64"
dotnet publish -r linux-x64 -c Release --self-contained=true DumbedDownCron /p:PublishSingleFile=true
echo "building win-x64"
dotnet publish -r win-x64 -c Release --self-contained=true DumbedDownCron /p:PublishSingleFile=true
echo "building win-x86"
dotnet publish -r win-x86 -c Release --self-contained=true DumbedDownCron /p:PublishSingleFile=true
pause