# Провайдеры QuickTranslate

Приложение поддерживает работу с различными AI-провайдерами через OpenAI-совместимый API.

## Поддерживаемые провайдеры

### 1. OpenAI Compatible (OpenAI, z.ai, Groq, Together и другие)

Все провайдеры, использующие OpenAI-совместимый API формат.

**Примеры:**
- OpenAI (GPT-3.5, GPT-4, GPT-4o)
- z.ai (Z.ai)
- Groq
- Together AI
- Perplexity
- DeepSeek
- И другие совместимые провайдеры

**Настройка:**
1. Тип провайдера: `OpenAI Compatible`
2. Base URL: URL API провайдера (например: `https://api.z.ai/v1`, `https://api.groq.com/openai/v1`)
3. API Key: Ваш API ключ
4. Model: Название модели (например: `gpt-4o-mini`, `llama3-8b-8192`)

### 2. Ollama (Локальный AI)

Запуск моделей локально на вашем компьютере.

**Настройка:**
1. Установите Ollama с сайта [ollama.ai](https://ollama.ai)
2. Скачайте модель: `ollama pull llama3`
3. Запустите Ollama: `ollama serve`

**В приложении:**
1. Тип провайдера: `Ollama`
2. Base URL: `http://localhost:11434/v1`
3. API Key: (оставьте пустым)
4. Model: `llama3` или другая установленная модель

### 3. Anthropic Claude

**Настройка:**
1. Тип провайдера: `Anthropic Claude`
2. Base URL: `https://api.anthropic.com/v1`
3. API Key: Ваш Anthropic API ключ
4. Model: `claude-3-haiku-20240307`, `claude-3-sonnet-20240229`, `claude-3-opus-20240229`

### 4. Google Gemini

**Настройка:**
1. Тип провайдера: `Google Gemini`
2. Base URL: `https://generativelanguage.googleapis.com/v1`
3. API Key: Ваш Google API ключ
4. Model: `gemini-pro`, `gemini-pro-vision`

## Примеры настройки популярных провайдеров

### z.ai
```
Тип: OpenAI Compatible
Base URL: https://api.z.ai/v1
API Key: sk-xxxxx
Model: gpt-4o-mini (или другая доступная модель)
```

### Groq
```
Тип: OpenAI Compatible
Base URL: https://api.groq.com/openai/v1
API Key: gsk_xxxxx
Model: llama3-8b-8192
```

### OpenAI
```
Тип: OpenAI Compatible
Base URL: https://api.openai.com/v1
API Key: sk-xxxxx
Model: gpt-4o-mini
```

### Ollama
```
Тип: Ollama
Base URL: http://localhost:11434/v1
API Key: (оставить пустым)
Model: llama3
```

## Устранение неполадок

### Тест подключения не проходит

1. **Проверьте URL**: Убедитесь, что URL правильный и включает версию API (обычно `/v1`)
2. **Проверьте API ключ**: Убедитесь, что ключ правильный и активен
3. **Проверьте модель**: Убедитесь, что название модели верное для выбранного провайдера
4. **Проверьте сеть**: Убедитесь, что нет проблем с интернет-соединением
5. **Проверьте лимиты**: Некоторые провайдеры имеют лимиты на бесплатных тарифах

### Timeout (Таймаут)

Увеличьте значение `Timeout` в настройках провайдера (по умолчанию 60 секунд):
- Для больших моделей: 120-180 секунд
- Для локальных моделей (Ollama): 60-120 секунд

### Ошибка API 401
Проверьте API ключ. Возможно, он истек или недействителен.

### Ошибка API 404
Проверьте Base URL. Возможно, он неправильный или не содержит путь к API.

### Ошибка API 429
Слишком много запросов. Подождите или увеличьте интервал между запросами.

## Получение API ключей

### OpenAI
1. Зарегистрируйтесь на [platform.openai.com](https://platform.openai.com)
2. Перейдите в Settings > API Keys
3. Создайте новый ключ

### z.ai
1. Зарегистрируйтесь на сайте z.ai
2. Получите API ключ в личном кабинете

### Groq
1. Зарегистрируйтесь на [console.groq.com](https://console.groq.com)
2. Создайте API ключ в разделе Keys

### Anthropic
1. Зарегистрируйтесь на [console.anthropic.com](https://console.anthropic.com)
2. Создайте API ключ в разделе API Keys

### Google
1. Зарегистрируйтесь на [makersuite.google.com](https://makersuite.google.com)
2. Создайте проект в Google Cloud Console
3. Включите Gemini API
4. Создайте API ключ
