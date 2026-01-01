@echo off
echo.
echo ══════════════════════════════════════════════════
echo   ДИАГНОСТИКА: ГДЕ ЗАПУСКАЕТСЯ ПРИЛОЖЕНИЕ
echo ══════════════════════════════════════════════════
echo.

echo [1] Поиск всех QuickTranslate.Desktop.exe...
echo.

setlocal enabledelayedexpansion

for /r "D:\" %%F in (QuickTranslate.Desktop.exe) do (
    echo Найден: %%F
    for %%A in ("%%F") do (
        echo   Дата: %%~tA
        echo   Размер: %%~zA байт
    )
    echo.
)

echo [2] Поиск всех QuickTranslate.Core.dll...
echo.

for /r "D:\" %%F in (QuickTranslate.Core.dll) do (
    echo Найден: %%F
    for %%A in ("%%F") do (
        echo   Дата: %%~tA
        echo   Размер: %%~zA байт
    )
    echo.
)

echo [3] Проверка процесса...
echo.

tasklist /FI "IMAGENAME eq QuickTranslate.Desktop.exe" 2>nul
if %errorlevel% equ 0 (
    echo.
    echo ✓ Процесс QuickTranslate.Desktop.exe запущен

    echo.
    echo Получение полного пути процесса...
    for /f "tokens=2 delims==" %%F in ('wmic process where "name='QuickTranslate.Desktop.exe'" get executablepath /value 2^>nul ^| find "="') do (
        echo Путь: %%F
    )
) else (
    echo ✗ Процесс QuickTranslate.Desktop.exe НЕ запущен
)
echo.

echo [4] Проверка DLL в проекте...
echo.

set PROJECT_DLL=QuickTranslate.Desktop\bin\Release\net8.0-windows\QuickTranslate.Core.dll
if exist "%PROJECT_DLL%" (
    echo ✓ DLL найдена в проекте
    for %%A in ("%PROJECT_DLL%") do echo   Дата: %%~tA
) else (
    echo ✗ DLL НЕ найдена в проекте: %PROJECT_DLL%
)
echo.

echo [5] Анализ...
echo.
echo Возможные проблемы:
echo.
echo 1. Приложение запускается из другой папки
echo    - Посмотрите на пункт [3] выше
echo    - Если путь отличается от проекта - вот причина!
echo.
echo 2. В системе несколько версий приложения
echo    - Посмотрите на пункт [1] и [2]
echo    - Удалите старые версии
echo.
echo 3. Приложение установлено через инсталлятор
echo    - Проверьте папку: C:\Program Files\
echo    - Или: C:\Users\%USERNAME%\AppData\Local\...
echo.
echo [6] Решение:
echo.
echo Если процесс запущен из другого места:
echo 1. Закройте приложение
echo 2. Удалите старые версии (см. список выше)
echo 3. Запустите из папки проекта:
echo    QuickTranslate.Desktop\bin\Release\net8.0-windows\QuickTranslate.Desktop.exe
echo.

echo ══════════════════════════════════════════════════
pause
