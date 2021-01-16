@echo off
pushd %USERPROFILE%\.bamboo & dotnet clibamwallet.dll %* & popd