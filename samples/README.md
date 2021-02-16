# DNT Samples
## Sample Projects for dnt
## packages and solutions using these packages.

**These Solutions and Packages are used for testing.**

## Usage


Directories

\Packages	Source of NuGet packages

\Solutions	Sample solutions, that use the NuGet packages.

\repository	Storage for built packages.

## package dependencies

All packages are simply standing alone.
Except Pkg_1 depends on Pkg_3 and Pkg_4.


## How to build the packages

All packages are stored in their own directory. 
Every package is a Visual Studio Solution with at least 1 c# project.

The packages do not have any functions. They can be referenced by any solution.

In every package directory is a file "build.cmd". This file includes the commands to build and publish the package from source.


## How to create / use the testing packages

To use the packages create a new solution in Visual Studio or open the existing solutions in \samples\Solutions.


## How to reference the testing packages

The packages are stored in a directory "repository".
So there is no NuGet server (public or private) needed. 
This directory has to be included into the Visual Studio configuration:  options / NuGet-Package-Manager / Package-Sources


