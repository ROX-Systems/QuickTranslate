@echo off
echo.
echo ══════════════════════════════════════════════════
echo   ЗАПУСК QUICKTRANSLATE С НОВОЙ ВЕРСИЕЙ
echo ══════════════════════════════════════════════════
echo.

set EXE_PATH=QuickTranslate.Desktop\bin\Release\net8.0-windows\QuickTranslate.Desktop.exe
set LOG_DIR=%LOCALAPPDATA%\QuickTranslate\logs

echo [1] Проверка EXE файла...
if exist "%EXE_PATH%" (
    echo ✓ EXE найден
) else (
    echo ✗ EXE НЕ НАЙДЕН: %EXE_PATH%
    pause
    exit /b 1
)
echo.

echo [2] Завершение старых процессов...
taskkill /F /IM QuickTranslate.Desktop.exe >nul 2>&1
timeout /t 1 /nobreak >nul
echo.

echo [3] Запуск новой версии...
start "" "%EXE_PATH%"
echo.

echo [4] Ожидание запуска (5 сек)...
timeout /t 5 /nobreak >nul
echo.

echo [5] Проверка логов...
echo.
if exist "%LOG_DIR%\log-20260101.txt" (
    echo ✓ Логи найдены
    echo.
    echo Поиск сообщений новой версии...
    echo.
    powershell -Command "Get-Content '%LOG_DIR%\log-20260101.txt' | Select-String 'DEBUG LOGGING|=== API REQUEST|=== API RESPONSE' | Select-Object -Last 10"
) else (
    echo ✗ Логи НЕ НАЙДЕНЫ
    echo   Папка: %LOG_DIR%
)
echo.

echo [6] Инструкция:
echo.
echo 1. Проверьте заголовок окна приложения - должно быть:
echo    "QuickTranslate v1.0.6 [DEBUG]"
echo.
echo 2. Попробуйте перевести текст (Ctrl+Shift+T)
echo.
echo 3. Если появились новые логи с "=== API REQUEST",
echo    пришлите их в чат.
echo.

echo ══════════════════════════════════════════════════
echo.
echo Нажмите Enter для открытия папки с логами...
pause >nul
explorer "%LOG_DIR%"
