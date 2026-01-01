@echo off
echo.
echo ══════════════════════════════════════════════════
echo   ОЧИСТКА КОРЗИНЫ
echo ══════════════════════════════════════════════════
echo.

echo [1] Очистка корзины...
powershell -Command "Clear-RecycleBin -Force -ErrorAction SilentlyContinue"
if %errorlevel% equ 0 (
    echo ✓ Корзина очищена успешно
) else (
    echo ⚠ Не удалось очистить корзину (может быть пустая)
)
echo.

echo [2] Поиск активного процесса...
wmic process where "name='QuickTranslate.Desktop.exe'" get executablepath,processid 2>nul
echo.

echo [3] Проверка системных папок...
if exist "C:\Program Files\QuickTranslate\QuickTranslate.Desktop.exe" (
    echo ✓ Найден в Program Files:
    echo   C:\Program Files\QuickTranslate\
)

if exist "%LOCALAPPDATA%\QuickTranslate\QuickTranslate.Desktop.exe" (
    echo ✓ Найден в LocalAppData:
    echo   %LOCALAPPDATA%\QuickTranslate\
)

if exist "%APPDATA%\QuickTranslate\QuickTranslate.Desktop.exe" (
    echo ✓ Найден в AppData (Roaming):
    echo   %APPDATA%\QuickTranslate\
)
echo.

echo [4] Проверка папки проекта...
if exist "QuickTranslate.Desktop\bin\Release\net8.0-windows\QuickTranslate.Desktop.exe" (
    echo ✓ Найден в проекте:
    echo   QuickTranslate.Desktop\bin\Release\net8.0-windows\
)
echo.

echo [5] Рекомендация:
echo.
echo Если процесс запущен из системной папки (Program Files или AppData):
echo   1. Закройте приложение
echo   2. Удалите системную версию
echo   3. Запустите из папки проекта:
echo      QuickTranslate.Desktop\bin\Release\net8.0-windows\QuickTranslate.Desktop.exe
echo.

echo ══════════════════════════════════════════════════
pause
