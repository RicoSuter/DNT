@echo off
msbuild

..\..\bin\nuget.exe pack .\Solution.nuspec
..\..\bin\nuget.exe add .\Pkg_3.1.0.0.nupkg -Source ..\..\repository
