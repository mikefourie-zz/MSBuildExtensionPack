if exist "%programfiles%\Microsoft SDKs\Windows\v6.0A\Bin\sn.exe" ( goto SN1 ) else ( goto SN2 )
:SN1
    set snExe=%programfiles%\Microsoft SDKs\Windows\v6.0A\Bin\x64\sn.exe
    set snX86Exe=%programfiles%\Microsoft SDKs\Windows\v6.0A\Bin\sn.exe
    goto SN_Finish
:SN2
    set snExe=%ProgramW6432%\Microsoft SDKs\Windows\v6.0A\Bin\x64\sn.exe
    set snX86Exe=%ProgramW6432%\Microsoft SDKs\Windows\v6.0A\Bin\sn.exe
:SN_Finish


set regAsmExe=%systemroot%\Microsoft.NET\Framework\v2.0.50727\regasm.exe
if not "x86" == "%processor_architecture%" set regAsmExe=%systemroot%\Microsoft.NET\Framework64\v2.0.50727\regasm.exe
if not "x86" == "%processor_architecture%" set regAsmX86Exe=%systemroot%\Microsoft.NET\Framework\v2.0.50727\regasm.exe
