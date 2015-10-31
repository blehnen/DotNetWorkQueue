Tools\nuget\NuGet.exe restore Source\DotNetWorkQueue.sln
Tools\nuget\NuGet.exe restore Source\Examples\Examples.sln

set MSBUILD="C:\Program Files (x86)\MSBuild\14.0\Bin\MSBuild.exe"

%MSBUILD% Source\DotNetWorkQueue.sln /p:Configuration="Debug"
%MSBUILD% Source\Examples\Examples.sln /p:Configuration="Debug"