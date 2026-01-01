@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion

echo.
echo =========================================
echo   Z.AI API DIRECT TEST
echo =========================================
echo.

echo Enter your API Key for z.ai:
set /p API_KEY="API Key: "

if "%API_KEY%"=="" (
    echo.
    echo [ERROR] API Key not entered!
    pause
    exit /b 1
)

echo.
echo Select Model:
echo   1. glm-4-32b-0414-128k (was in first log)
echo   2. glm-4.7 (was in second log)
echo   3. gpt-4o-mini
echo.
set /p MODEL_CHOICE="Choice: "

if "%MODEL_CHOICE%"=="1" set MODEL=glm-4-32b-0414-128k
if "%MODEL_CHOICE%"=="2" set MODEL=glm-4.7
if "%MODEL_CHOICE%"=="3" set MODEL=gpt-4o-mini

if "%MODEL%"=="" set MODEL=glm-4-32b-0414-128k

echo.
echo Testing model: %MODEL%
echo.

set ENDPOINT=https://api.z.ai/api/anthropic/chat/completions
set JSON_FILE=%TEMP%\zai_test_%RANDOM%.json

echo Generating request...
echo {> "%JSON_FILE%"
echo   "model": "%MODEL%",>> "%JSON_FILE%"
echo   "messages": [>> "%JSON_FILE%"
echo     {>> "%JSON_FILE%"
echo       "role": "user",>> "%JSON_FILE%"
echo       "content": "Translate to Russian: Hello, world!">> "%JSON_FILE%"
echo     }>> "%JSON_FILE%"
echo   ],>> "%JSON_FILE%"
echo   "max_tokens": 100>> "%JSON_FILE%"
echo }>> "%JSON_FILE%"

echo.
echo =========================================
echo   REQUEST
echo =========================================
type "%JSON_FILE%"
echo.
echo.

echo =========================================
echo   SENDING...
echo =========================================
echo.

set RESPONSE_FILE=%TEMP%\zai_response_%RANDOM%.txt

curl -s -X POST "%ENDPOINT%" ^
  -H "Authorization: Bearer %API_KEY%" ^
  -H "Content-Type: application/json" ^
  -d @"%JSON_FILE%" ^
  -o "%RESPONSE_FILE%"

if %errorlevel% equ 0 (
    echo [OK] Response received
    echo.
    echo.
    echo =========================================
    echo   RESPONSE
    echo =========================================
    echo.
    type "%RESPONSE_FILE%"
    echo.
    echo.

    echo =========================================
    echo   ANALYSIS
    echo =========================================
    echo.

    findstr /C:"choices" "%RESPONSE_FILE%" >nul
    if !errorlevel! equ 0 (
        echo [OK] Response contains "choices" field
    ) else (
        echo [FAIL] Response does NOT contain "choices" field
    )

    findstr /C:"content" "%RESPONSE_FILE%" >nul
    if !errorlevel! equ 0 (
        echo [OK] Response contains "content" field
    ) else (
        echo [FAIL] Response does NOT contain "content" field
    )

    findstr /C:"error" "%RESPONSE_FILE%" >nul
    if !errorlevel! equ 0 (
        echo [WARNING] Response contains "error" field
        echo.
        echo Check error message above!
    )

) else (
    echo [ERROR] Failed to send request
    echo   Error code: %errorlevel%
)

echo.
echo.
echo =========================================
echo.
echo Request and response saved:
echo   - %JSON_FILE%
echo   - %RESPONSE_FILE%
echo.
pause
