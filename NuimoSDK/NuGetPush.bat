@echo off
if [%1]==[] goto usage

del *.nupkg

nuget pack NuimoSDK.csproj -Prop Configuration=Release
nuget setApiKey %1 -Source https://www.nuget.org/api/v2/package
nuget push *.nupkg -Source https://www.nuget.org/api/v2/package

:usage
@echo Usage: %0 [API key]