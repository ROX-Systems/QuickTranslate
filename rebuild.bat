@echo off
echo.
echo ══════════════════════════════════════════════════
echo   ПЕРЕСБОРКА С ПОЛНОЙ ОЧИСТКОЙ
echo ══════════════════════════════════════════════════
echo.

echo [1] Остановка QuickTranslate...
taskkill /F /IM QuickTranslate.Desktop.exe >nul 2>&1
timeout /t 1 /nobreak >nul
echo.

echo [2] Удаление папок bin и obj...
rmdir /s /q "QuickTranslate.Desktop\bin" 2>nul
rmdir /s /q "QuickTranslate.Desktop\obj" 2>nul
rmdir /s /q "QuickTranslate.Core\bin" 2>nul
rmdir /s /q "QuickTranslate.Core\obj" 2>nul
echo Удалено.
echo.

echo [3] Пересборка решения...
dotnet clean QuickTranslate.sln
dotnet build QuickTranslate.sln --configuration Release --no-incremental
echo.

echo [4] Проверка результата...
if exist "QuickTranslate.Desktop\bin\Release\net8.0-windows\QuickTranslate.Core.dll" (
    echo ✓ DLL собрана успешно
) else (
    echo ✗ DLL НЕ собрана
)
echo.

if exist "QuickTranslate.Desktop\bin\Release\net8.0-windows\QuickTranslate.Desktop.exe" (
    echo ✓ EXE собран успешно
) else (
    echo ✗ EXE НЕ собран
)
echo.

echo ══════════════════════════════════════════════════
pause
