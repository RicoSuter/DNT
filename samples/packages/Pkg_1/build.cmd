@echo off
msbuild

..\..\bin\nuget.exe pack .\Solution.nuspec
..\..\bin\nuget.exe add .\Pkg_1.1.0.0.nupkg -Source ..\..\repository