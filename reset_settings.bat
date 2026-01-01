@echo off
chcp 65001 >nul
echo Сброс настроек QuickTranslate...
echo.

set APPDATA=%LOCALAPPDATA%\QuickTranslate

if exist "%APPDATA%\settings.json" (
    echo Найден файл настроек: %APPDATA%\settings.json
    echo.
    echo Создание резервной копии...
    copy "%APPDATA%\settings.json" "%APPDATA%\settings.json.backup" >nul 2>&1
    echo.
    echo Удаление файла настроек...
    del "%APPDATA%\settings.json"
    echo ✓ Файл настроек удален
) else (
    echo ✗ Файл настроек не найден: %APPDATA%\settings.json
)

echo.
if exist "%APPDATA%\history.json" (
    echo Также можно очистить историю переводов.
    set /p CLEAR_HISTORY="Удалить историю? (y/n): "
    if /i "%CLEAR_HISTORY%"=="y" (
        copy "%APPDATA%\history.json" "%APPDATA%\history.json.backup" >nul 2>&1
        del "%APPDATA%\history.json"
        echo ✓ История удалена
    )
)

echo.
echo Сброс завершен.
echo При следующем запуске будут созданы настройки по умолчанию.
echo.
pause
