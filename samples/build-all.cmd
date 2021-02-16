@echo off

cd packages\Pkg_1
call build.cmd
cd ..\..

cd packages\Pkg_2
call build.cmd
cd ..\..

cd packages\Pkg_3
call build.cmd
cd ..\..

cd packages\Pkg_4
call build.cmd
cd ..\..


