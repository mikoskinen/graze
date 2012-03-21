C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe ..\src\graze.sln /p:Configuration=Release
del *.nupkg
nuget pack graze.nuspec -Version 4.1.0
