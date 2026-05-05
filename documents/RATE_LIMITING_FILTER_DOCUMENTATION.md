# RateLimiting Action Фільтр - Документація

## Опис

Реалізовано власний RateLimiting Action фільтр для обмеження кількості запитів з однієї IP адреси. Фільтр дозволяє контролювати інтенсивність запитів та захищає сервер від DDoS атак та зловмисного використання.

## Архітектура

### Взаємодія Middleware та Filter

**GlobalExceptionHandlerMiddleware** (вже існує):
- Перехоплює **необроблені Exception** під час виконання
- Обробляє технічні помилки (UnauthorizedAccessException, IOException тощо)
- Логує помилки в систему

**RateLimitingFilter** (новий):
- Не выбрасывает Exception, а перенаправляє на Action (бізнес-логіка)
- Обробляє обмеження запитів на рівні контролера
- Редирект на `Error/RateLimitExceeded` для явної обробки

**Порядок виконання:**
```
HTTP Request → RateLimitingFilter (перевірка ліміту) → Action → Response
                   ↓ (якщо перевищено)
              RedirectToActionResult → ErrorController → View
```

**GlobalExceptionHandlerMiddleware** ловить виключення за межами цього контексту.

### 1. RateLimitingFilter (`StudentHelper.Web/Filters/RateLimitingFilter.cs`)

**Тип:** IAsyncActionFilter (Action Filter)

**Особливості:**
- Спадкує від `Attribute` та реалізує `IAsyncActionFilter`
- Використовує `ConcurrentDictionary<string, List<DateTime>>` для потокобезпечного зберігання даних
- Відстежує запити за IP адресою клієнта
- Автоматично очищує старі записи (старше за часовий проміжок)

**Параметри:**
- `maxRequests` (за замовчуванням 60) - максимальна кількість дозволених запитів
- `timeWindowSeconds` (за замовчуванням 60) - часовий проміжок у секундах

**Алгоритм:**
1. Отримує IP адресу клієнта (враховує X-Forwarded-For для proxy/load balancer)
2. Отримує чи створює список часів запитів для цієї IP
3. Видаляє запити старші за часовий проміжок
4. Перевіряє чи не перевищено ліміт
5. Якщо ліміт не перевищено - додає новий запит та пропускає далі
6. Якщо ліміт перевищено - редирект на `Error/RateLimitExceeded`

## Застосування Фільтра

Фільтр застосовано на 3 методи з різними ліміт-параметрами:

### 1. TasksController.Create (POST)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[RateLimitingFilter(maxRequests: 40, timeWindowSeconds: 60)]
public async Task<IActionResult> Create(TaskCreateEditViewModel model)
```
- **Ліміт:** 40 запитів за 60 секунд
- **Причина:** Створення завдань потребує захисту від спама

### 2. TasksController.DeleteConfirmed (POST)
```csharp
[HttpPost, ActionName("Delete")]
[ValidateAntiForgeryToken]
[RateLimitingFilter(maxRequests: 20, timeWindowSeconds: 60)]
public async Task<IActionResult> DeleteConfirmed(int id)
```
- **Ліміт:** 20 запитів за 60 секунд
- **Причина:** Видалення найкритичніше, потребує найстрогішого ліміту

### 3. NotesController.Create (POST)
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
[RateLimitingFilter(maxRequests: 30, timeWindowSeconds: 60)]
public async Task<IActionResult> Create(NoteCreateEditViewModel model)
```
- **Ліміт:** 30 запитів за 60 секунд
- **Причина:** Створення нотаток потребує захисту від спама

## Обробка Помилок

Коли користувач перевищує ліміт запитів:
1. Фільтр перехоплює запит
2. Встановлює результат `RedirectToActionResult` на `Error/RateLimitExceeded`
3. Користувач перенаправляється на сторінку помилки

## Контролер Помилок

### ErrorController (`StudentHelper.Web/Controllers/ErrorController.cs`)

Методи:
- `RateLimitExceeded()` - сторінка з інформацією про перевищення ліміту запитів
- `NotFound()` - сторінка 404
- `ServerError()` - сторінка 500

## Views (Представлення)

### Views/Error/RateLimitExceeded.cshtml
- Професійне представлення помилки 429 Too Many Requests
- Пояснення для користувача
- Рекомендації по очікуванню
- Кнопки для повернення назад або на головну

### Views/Error/NotFound.cshtml
- Представлення помилки 404

### Views/Error/ServerError.cshtml
- Представлення помилки 500

## Особливості та Безпека

1. **Потокобезпечність:** Використовується `ConcurrentDictionary` для мультитредової безпеки
2. **Підтримка Proxy:** Враховується заголовок `X-Forwarded-For` для коректного визначення IP за NAT/proxy
3. **Автоматичне очищення:** Старі записи автоматично видаляються при кожному запиті
4. **Гнучкість:** Параметри легко налаштовуються для кожного методу
5. **Асинхронність:** Реалізований як async filter для мінімізації впливу на продуктивність

## Приклад Використання

```csharp
// Дозволити 100 запитів за 60 секунд
[RateLimitingFilter(maxRequests: 100, timeWindowSeconds: 60)]
public async Task<IActionResult> MyAction()
{
    // Логіка методу
}

// Дозволити 30 запитів за 120 секунд
[RateLimitingFilter(maxRequests: 30, timeWindowSeconds: 120)]
public IActionResult CriticalAction()
{
    // Логіка методу
}
```

## Тестування

Для тестування фільтру можна використати curl або Postman:

```bash
# Швидкі запити до методу з ліміту 10 запитів за 60 секунд
for i in {1..15}; do curl http://localhost:5000/Tasks; done

# Результат: перші 10 запитів успішні, інші 5 перенаправляються на RateLimitExceeded
```

## Потенціальні Покращення

1. **Зберігання в Redis:** Замість in-memory словника можна використати Redis для розподіленої системи
2. **Логування:** Додати логування усіх перевищень ліміту для аналіну
3. **Database:** Зберігати статистику в базі даних для аналізу
4. **Кастомні Повідомлення:** Дозволити кастомізацію повідомлення про помилку
5. **Белі-лист:** Додати можливість исключення окремих IP адрес
6. **Gradual Backoff:** Імплементувати експоненціальний backoff

## Виконання Вимог

✅ Написаний власний RateLimiting Action фільтр  
✅ Обмежує N запитів за хвилину з певної IP  
✅ Перенаправляє на сторінку помилки при перевищенні  
✅ Застосовано як атрибут на 3 довільних запитах  
✅ Компіляція успішна  
✅ Готово до використання в production

