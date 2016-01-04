@echo off
cls
"TinyRest\.nuget\NuGet.exe" "Install" "FAKE" "-OutputDirectory" "TinyRest\packages" "-ExcludeVersion"
"TinyRest\packages\FAKE\tools\Fake.exe" "build.fsx" %2
