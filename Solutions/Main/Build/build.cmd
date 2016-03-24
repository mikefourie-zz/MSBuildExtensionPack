SET NET_FRAMEWORK_DIR=%WINDIR%\Microsoft.NET\Framework\v4.0.30319
CALL "%VS120COMNTOOLS%..\..\VC\vcvarsall.bat"
msbuild.exe build.proj /v:normal /m /p:TrackFileAccess=false;RunCodeAnalysis=false;RTWRelease=true /clp:PerformanceSummary /flp:logfile="log.txt";v=diag /nologo