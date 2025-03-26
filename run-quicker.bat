:: 创建临时文件来保存输出
set "temp_file=%TEMP%\quicker_output.txt"
cmd.exe /c "C:\Program Files\Quicker\QuickerStarter.exe" runaction:1b0e1f5d-634d-476a-acee-bfbca76c27e0   > "%temp_file%" 2>&1
@REM timeout /t 3 > nul
type "%temp_file%"

:: 清理临时文件
del "%temp_file%" 2>nul