call %~dp0SetEnv.cmd
echo x86
"%snX86Exe%" -Vr *,10d297e8e737fe34 
if not "x86" == "%processor_architecture%" echo x64 & "%snExe%" -Vr *,10d297e8e737fe34 

