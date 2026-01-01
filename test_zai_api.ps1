# Тест API z.ai через PowerShell
# Скопируйте этот код в PowerShell и выполните

$apiKey = Read-Host "df678106de9b48d887d18508ff8c45c5.fLjMNlmpPIpqxEUs"
$endpoint = "https://api.z.ai/api/anthropic/chat/completions"

Write-Host ""
Write-Host "Тестирование модели: glm-4-32b-0414-128k" -ForegroundColor Cyan
Write-Host ""

$body = @{
    model = "glm-4-32b-0414-128k"
    messages = @(
        @{
            role = "user"
            content = "Translate to Russian: Hello, world!"
        }
    )
    max_tokens = 100
} | ConvertTo-Json -Depth 10

Write-Host "Запрос:" -ForegroundColor Yellow
Write-Host $body
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri $endpoint -Method Post -Headers @{
        "Authorization" = "Bearer $apiKey"
        "Content-Type" = "application/json"
    } -Body $body -ErrorAction Stop

    Write-Host "Ответ:" -ForegroundColor Green
    Write-Host ($response | ConvertTo-Json -Depth 10)
    Write-Host ""

    $choices = $response.choices
    $content = $choices[0].message.content

    Write-Host "Перевод:" -ForegroundColor Cyan
    Write-Host $content

} catch {
    Write-Host "Ошибка:" -ForegroundColor Red
    Write-Host $_.Exception.Message

    if ($_.ErrorDetails) {
        Write-Host "Детали:"
        Write-Host $_.ErrorDetails.Message
    }
}
