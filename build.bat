.nuget\NuGet.exe install vswhere -Prerelease -OutputDirectory .\packages\
.nuget\NuGet.exe restore
dotnet restore --configfile ./.nuget/NuGet.Config

call "%~dp0tools\msbuild.cmd" /t:Build /p:Configuration=Release /m