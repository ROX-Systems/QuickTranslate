@echo off
echo.
echo ══════════════════════════════════════════════════
echo   ПРОВЕРКА ВЕРСИИ QUICKTRANSLATE
echo ══════════════════════════════════════════════════
echo.

set DLL_PATH=QuickTranslate.Desktop\bin\Release\net8.0-windows\QuickTranslate.Core.dll
set EXE_PATH=QuickTranslate.Desktop\bin\Release\net8.0-windows\QuickTranslate.Desktop.exe

echo [1] Проверка файлов сборки...
echo.

if exist "%DLL_PATH%" (
    echo ✓ DLL найдена: %DLL_PATH%
    for %%A in ("%DLL_PATH%") do echo   Дата изменения: %%~tA
    echo   Размер: %%~zA байт
) else (
    echo ✗ DLL НЕ НАЙДЕНА: %DLL_PATH%
)
echo.

if exist "%EXE_PATH%" (
    echo ✓ EXE найден: %EXE_PATH%
    for %%A in ("%EXE_PATH%") do echo   Дата изменения: %%~tA
    echo   Размер: %%~zA байт
) else (
    echo ✗ EXE НЕ НАЙДЕН: %EXE_PATH%
)
echo.

echo [2] Проверка кода в DLL...
echo.

powershell -Command "try { $bytes = [System.IO.File]::ReadAllBytes('%DLL_PATH%'); $text = [System.Text.Encoding]::ASCII.GetString($bytes); if ($text -like '*DEBUG LOGGING*') { Write-Host '✓ DLL содержит новую версию с DEBUG LOGGING' -ForegroundColor Green } else { Write-Host '✗ DLL НЕ содержит новую версию' -ForegroundColor Red } } catch { Write-Host '✗ Ошибка при чтении DLL' -ForegroundColor Red }"
echo.

echo [3] Проверка кода в EXE...
echo.

powershell -Command "try { $bytes = [System.IO.File]::ReadAllBytes('%EXE_PATH%'); $text = [System.Text.Encoding]::ASCII.GetString($bytes); if ($text -like '*DEBUG LOGGING*') { Write-Host '✓ EXE содержит новую версию с DEBUG LOGGING' -ForegroundColor Green } else { Write-Host '✗ EXE НЕ содержит новую версию' -ForegroundColor Red } } catch { Write-Host '✗ Ошибка при чтении EXE' -ForegroundColor Red }"
echo.

echo [4] Инструкция по запуску...
echo.
echo 1. Закройте все экземпляры QuickTranslate (включая из трея)
echo 2. Запустите приложение:
echo    %EXE_PATH%
echo 3. Проверьте заголовок окна - должно быть: "QuickTranslate v1.0.6 [DEBUG]"
echo 4. Попробуйте перевести текст
echo 5. Проверьте логи:
echo    %LOCALAPPDATA%\QuickTranslate\logs\
echo.
echo ══════════════════════════════════════════════════
pause
