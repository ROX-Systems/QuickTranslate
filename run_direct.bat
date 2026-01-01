@echo off
echo.
echo ══════════════════════════════════════════════════
echo   ПРЯМОЙ ЗАПУСК ИЗ ПРАВИЛЬНОЙ ПАПКИ
echo ══════════════════════════════════════════════════
echo.

set CORRECT_EXE=%~dp0QuickTranslate.Desktop\bin\Release\net8.0-windows\QuickTranslate.Desktop.exe
set CORRECT_DLL=%~dp0QuickTranslate.Desktop\bin\Release\net8.0-windows\QuickTranslate.Core.dll

echo Текущая папка: %~dp0
echo.

echo [1] Проверка правильных файлов...
if exist "%CORRECT_EXE%" (
    echo ✓ EXE найден: %CORRECT_EXE%
) else (
    echo ✗ EXE НЕ НАЙДЕН: %CORRECT_EXE%
    pause
    exit /b 1
)

if exist "%CORRECT_DLL%" (
    echo ✓ DLL найден: %CORRECT_DLL%
) else (
    echo ✗ DLL НЕ НАЙДЕН: %CORRECT_DLL%
    pause
    exit /b 1
)
echo.

echo [2] Завершение ВСЕХ процессов QuickTranslate...
taskkill /F /IM QuickTranslate.Desktop.exe >nul 2>&1
timeout /t 2 /nobreak >nul
echo.

echo [3] Установка переменных окружения для логов...
set LOCALAPPDATA=%LOCALAPPDATA%
echo.

echo [4] Запуск из правильной папки...
echo Путь: %CORRECT_EXE%
echo.
echo Внимание: Смотрите на заголовок окна!
echo Должно быть: "QuickTranslate v1.0.6 [DEBUG]"
echo.

echo [5] Приложение запущено. Теперь:
echo   1. Проверьте заголовок окна
echo   2. Попробуйте перевести текст (Ctrl+Shift+T)
echo   3. Проверьте логи (откроются через 10 сек)
echo.

start "" "%CORRECT_EXE%"

echo Ожидание 10 секунд...
timeout /t 10 /nobreak >nul

echo.
echo [6] Открытие логов...
explorer "%LOCALAPPDATA%\QuickTranslate\logs"

echo.
echo Проверьте логи на наличие:
echo   - "✓ Detailed API logging is ENABLED"
echo   - "=== API REQUEST ==="
echo   - "=== API RESPONSE ==="
echo.

echo ══════════════════════════════════════════════════
echo.
echo Если детальные логи ВСЕ РАВНО не появляются:
echo   Запустите find_app_location.bat
echo   И пришлите его вывод в чат!
echo.
pause
