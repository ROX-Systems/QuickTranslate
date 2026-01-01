# Устранение проблем с открытием настроек

Если при нажатии на кнопку "Настройки" приложение закрывается или возникает ошибка, выполните следующие шаги:

## Быстрое решение

### 1. Сбросьте настройки
Самая частая причина проблемы - несовместимость старых настроек с новой версией приложения.

**Способ 1: Использовать скрипт (рекомендуется)**
1. Перейдите в папку с проектом: `D:\projects\CascadeProjects\windsurf-project\QuickTranslate\`
2. Запустите файл `reset_settings.bat`
3. Ответьте на вопросы скрипта
4. Запустите приложение снова

**Способ 2: Вручную**
1. Закройте приложение
2. Откройте папку: `%LOCALAPPDATA%\QuickTranslate`
   - Нажмите `Win + R`, введите `%LOCALAPPDATA%\QuickTranslate` и нажмите Enter
3. Удалите файл `settings.json`
4. Запустите приложение снова

### 2. Проверьте логи
Если сброс настроек не помог, проверьте логи:

1. Перейдите в папку: `%LOCALAPPDATA%\QuickTranslate\logs`
2. Откройте последний файл с названием `log-<дата>.txt`
3. Найдите строки с ошибками (ищите `[ERR]` или `[FTL]`)
4. Сохраните лог и отправьте разработчику

## Возможные причины ошибок

### 1. Недопустимый тип провайдера
**Проблема:** Старые настройки содержат провайдер с недопустимым значением `Type`.

**Признаки в логах:**
```
[WRN] Provider has invalid type 0, setting to OpenAI
[WRN] Provider type 0 not found in available types
```

**Решение:** Сброс настроек (см. выше)

### 2. Null-значения в настройках
**Проблема:** Некоторые поля провайдера не заполнены или равны null.

**Признаки в логах:**
```
[ERR] Object reference not set to an instance of an object
```

**Решение:** Сброс настроек (см. выше)

### 3. Отсутствие API ключа для провайдера, требующего аутентификацию
**Проблема:** Провайдер требует API ключ, но он не указан.

**Признаки:** Ошибка валидации при сохранении настроек

**Решение:** Добавьте API ключ в настройки провайдера

### 4. Циклическое обновление свойств
**Проблема:** Изменение типа провайдера вызывает автоматическое обновление других полей.

**Признаки:** Приложение зависает или медленно работает

**Решение:** Обновлено в версии 1.0.5 - добавлен флаг `_isUpdatingProperties`

### 5. Конфликт DisplayMemberPath и ItemTemplate в XAML
**Проблема:** В ComboBox XAML одновременно заданы `DisplayMemberPath` и `ItemTemplate`.

**Признаки в логах:**
```
System.InvalidOperationException: Нельзя одновременно задать значения DisplayMemberPath и ItemTemplate.
System.Windows.Markup.XamlParseException: Задание свойства "System.Windows.Controls.ItemsControl.ItemTemplate" вызвало исключение.
```

**Причина:** WPF не позволяет использовать оба свойства одновременно. `DisplayMemberPath` создает автоматический шаблон для отображения одного свойства, а `ItemTemplate` определяет кастомный шаблон.

**Решение:** Удалить `DisplayMemberPath` из ComboBox, если используется `ItemTemplate`.

**Пример:**
```xml
<!-- ❌ НЕПРАВИЛЬНО -->
<ComboBox ItemsSource="{Binding AvailableProviderTypes}"
          DisplayMemberPath="Name"
          SelectedItem="{Binding SelectedProviderType}">
    <ComboBox.ItemTemplate>
        <DataTemplate>
            <StackPanel>
                <TextBlock Text="{Binding Name}" FontWeight="SemiBold"/>
                <TextBlock Text="{Binding Description}" TextWrapping="Wrap"/>
            </StackPanel>
        </DataTemplate>
    </ComboBox.ItemTemplate>
</ComboBox>

<!-- ✅ ПРАВИЛЬНО -->
<ComboBox ItemsSource="{Binding AvailableProviderTypes}"
          SelectedItem="{Binding SelectedProviderType}">
    <ComboBox.ItemTemplate>
        <DataTemplate>
            <StackPanel>
                <TextBlock Text="{Binding Name}" FontWeight="SemiBold"/>
                <TextBlock Text="{Binding Description}" TextWrapping="Wrap"/>
            </StackPanel>
        </DataTemplate>
    </ComboBox.ItemTemplate>
</ComboBox>
```

## Пошаговая диагностика

Если проблема persists:

### Шаг 1: Проверьте настройки.json
Откройте файл `%LOCALAPPDATA%\QuickTranslate\settings.json` в текстовом редакторе.

Проверьте, что у каждого провайдера есть поле `Type`:
```json
{
  "Providers": [
    {
      "Id": "...",
      "Name": "OpenAI",
      "Type": 0,
      "BaseUrl": "https://api.openai.com/v1",
      "Model": "gpt-4o-mini",
      ...
    }
  ]
}
```

Значения `Type`:
- 0: OpenAI
- 1: Anthropic
- 2: Google
- 3: Ollama
- 4: Custom

### Шаг 2: Проверьте выходные данные
При запуске приложения в консоли или Debug Output (в Visual Studio) должны быть сообщения:

```
[INF] Opening settings window
[INF] SettingsViewModel: Starting initialization
[INF] Loading settings...
[INF] Loaded 1 providers
[INF] Setting active provider: OpenAI (Type: OpenAI)
[INF] Settings loaded successfully with 1 providers
[INF] SettingsWindow: Starting initialization
[INF] SettingsWindow: Initialization completed successfully
```

### Шаг 3: Запустите с отладкой
Если у вас есть исходный код, запустите приложение в режиме Debug в Visual Studio. Это позволит увидеть точное место ошибки.

## Настройка провайдера z.ai

После успешного исправления проблемы:

1. Откройте Настройки
2. В разделе "Поставщики AI" нажмите **Добавить**
3. Выберите тип: **OpenAI Compatible**
4. Заполните:
   - **Имя провайдера:** z.ai
   - **Base URL:** `https://api.z.ai/api/anthropic`
   - **API Key:** Ваш API ключ (например: `sk-xxxxx`)
   - **Model:** `gpt-4o-mini`, `glm-4-32b-0414-128k` или другая доступная модель
5. Нажмите **Проверить соединение**
6. Если проверка прошла успешно, нажмите **Сохранить всё**

> **Важно:** z.ai использует кастомный API endpoint `/api/anthropic`, который отличается от стандартного OpenAI URL. Поэтому тип провайдера должен быть "OpenAI Compatible", но Base URL указывается специальный.

> **Примечание:** Подробные инструкции по настройке различных провайдеров (z.ai, Groq, Together, Ollama и др.) см. в файле `PROVIDER_SETUP.md`

## Проблемы с API

### API returned status Unauthorized

**Причина:** Неверный API ключ или истек срок действия.

**Решение:**
1. Проверьте API ключ на сайте провайдера
2. Убедитесь, что ключ скопирован полностью (без лишних пробелов)
3. Проверьте, что ключ не истек и имеет необходимые права

### Empty response from API

**Возможные причины:**
1. Неверный Base URL
2. Неверное название модели
3. Проблемы с сетью
4. Несовместимый тип провайдера

**Решение:**
1. Проверьте правильность Base URL (см. `PROVIDER_SETUP.md`)
2. Убедитесь, что модель доступна на данном провайдере
3. Попробуйте другое название модели
4. Проверьте подключение к интернету
5. Используйте тип провайдера "OpenAI Compatible" вместо других типов
6. Проверьте URL в логах: `Sending health check to <URL>`

> **Примечание для z.ai:** Правильный Base URL: `https://api.z.ai/api/anthropic` (не `/api/paas/v4`)

### Проверка соединения не проходит

**Шаги диагностики:**

1. **Проверьте URL в логах**
   - Откройте `%LOCALAPPDATA%\QuickTranslate\logs\`
   - Найдите строку: `Sending health check to <URL>`
   - Убедитесь, что URL правильный

2. **Попробуйте сделать запрос через curl**

Для z.ai (правильный URL):
```bash
curl https://api.z.ai/api/anthropic/chat/completions \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4o-mini",
    "messages": [{"role": "user", "content": "Hello"}],
    "max_tokens": 10
  }'
```

Для стандартных OpenAI-compatible провайдеров:
```bash
curl https://api.openai.com/v1/chat/completions \
  -H "Authorization: Bearer YOUR_API_KEY" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "gpt-4o-mini",
    "messages": [{"role": "user", "content": "Hello"}],
    "max_tokens": 10
  }'
```

3. **Проверьте тип провайдера**
   - Для z.ai, Groq, Together и др. используйте "OpenAI Compatible"
   - Не используйте "Anthropic" или "Google" для этих провайдеров
   - **Важно для z.ai:** Используйте тип "OpenAI Compatible", но Base URL должен быть кастомным: `https://api.z.ai/api/anthropic`

### Настройки автоматически меняются

**Примечание:** Начиная с текущей версии, при переключении типа провайдера на "OpenAI Compatible" настройки больше не будут автоматически меняться. Вы можете настроить любой Base URL и модель.

## Связь с поддержкой

## Предотвращение проблем в будущем

1. **Всегда сохраняйте резервную копию настроек**
   - Файл: `%LOCALAPPDATA%\QuickTranslate\settings.json`
   - Скопируйте его перед обновлением приложения

2. **Не редактируйте settings.json вручную**
   - Это может привести к несовместимости

3. **Используйте UI для изменения настроек**
   - Все изменения должны вноситься через окно настроек

4. **Проверяйте провайдеры перед сохранением**
   - Используйте кнопку "Проверить соединение"
