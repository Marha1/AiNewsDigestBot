# 🤖 AiNewsDigestBot

Telegram бот для сбора и агрегации новостей с использованием AI.

## 📋 О проекте

Бот собирает новости из RSS-лент и NewsAPI, определяет темы с помощью AI, формирует персонализированные дайджесты и отправляет их в Telegram.

### Возможности
- 📰 Сбор новостей из RSS и NewsAPI
- 🧠 Определение темы статьи с помощью AI
- 📊 Персонализированные дайджесты
- 🔍 Поиск по новостям
- ⚙️ Настройка времени рассылки
- 📋 Управление подписками на темы

### Технологии
- .NET 8
- PostgreSQL
- Entity Framework Core
- Telegram Bot API
- Hangfire (фоновые задачи)
- Docker

---

## 🚀 Быстрый старт

### Требования
- [Docker](https://www.docker.com/products/docker-desktop/)
- [Git](https://git-scm.com/)

### 1. Клонировать репозиторий
git clone https://github.com/Marha1/AiNewsDigestBot.git
cd AiNewsDigestBot

### 2. Настроить переменные окружения
cp .env.example .env

Отредактируйте .env и добавьте реальные токены:

TELEGRAM_TOKEN=ваш_токен_бота_от_BotFather
OPENAI_API_KEY=ваш_ключ_openai
NEWSAPI_KEY=ваш_ключ_newsapi

Где взять токены:

| Переменная | Описание | Где взять |
|-----------|----------|-----------|
| TELEGRAM_TOKEN | Токен Telegram бота | @BotFather в Telegram |
| OPENAI_API_KEY | Ключ OpenAI API | platform.openai.com |
| NEWSAPI_KEY | Ключ NewsAPI | newsapi.org |

### 3. Запустить через Docker
docker-compose up -d --build

### 4. Проверить работу
- Health check: http://localhost:8080/health
- Hangfire Dashboard: http://localhost:8080/hangfire
- Telegram бот: Найдите бота в Telegram и отправьте /start

### 5. Остановить
docker-compose down

---

## 📝 Команды бота

| Команда | Описание |
|---------|----------|
| /start | Начало работы |
| /topics | Выбор тем для подписки |
| /digest | Получить дайджест |
| /latest | Последние новости |
| /search [запрос] | Поиск новостей |
| /settings | Настройки |
| /help | Помощь |

---

## 🏗️ Архитектура

┌─────────────────────────────────────────────────────────────┐
│                      Telegram Bot                          │
│                 (обработка команд)                         │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                    Core Services                           │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐       │
│  │  Parser     │  │  Digest     │  │  Search     │       │
│  │  RSS/NewsAPI│  │  Scheduler  │  │  Articles   │       │
│  └─────────────┘  └─────────────┘  └─────────────┘       │
└─────────────────────┬───────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│              PostgreSQL Database                          │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐    │
│  │ Articles │ │ Sources  │ │ Users    │ │Settings  │    │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘    │
└─────────────────────────────────────────────────────────────┘

---

## 🐳 Docker команды

| Команда | Описание |
|---------|----------|
| docker-compose up -d | Запустить в фоне |
| docker-compose down | Остановить |
| docker-compose logs -f | Логи в реальном времени |
| docker-compose restart bot | Перезапустить бота |
| docker exec -it ainnewsbot_app bash | Войти в контейнер бота |
| docker exec -it ainnewsbot_postgres psql -U postgres -d AiNewsbot | Войти в PostgreSQL |

---

## 🧪 Разработка

### Локальный запуск (без Docker)

1. Установите PostgreSQL локально
2. Создайте БД AiNewsbot
3. Настройте appsettings.Development.json:

{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=AiNewsbot;Username=postgres;Password=ваш_пароль"
  },
  "Telegram": {
    "Token": "ваш_токен"
  }
}

4. Запустите:
dotnet run --project AiNewsDigestBot.Host

### Миграции

# Добавить миграцию
dotnet ef migrations add MigrationName --project AiNewsDigestBot.Host

# Обновить БД
dotnet ef database update --project AiNewsDigestBot.Host

---

## 📊 Мониторинг

- Hangfire Dashboard: http://localhost:8080/hangfire
- Health Check: http://localhost:8080/health
