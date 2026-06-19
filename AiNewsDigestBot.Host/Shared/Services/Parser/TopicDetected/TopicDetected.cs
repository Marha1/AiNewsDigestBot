using System.Text.RegularExpressions;
using AiNewsDigestBot.Host.Shared.Data.Enums;

namespace AiNewsDigestBot.Host.Shared.Services.Parser.TopicDetected;

public class TopicDetector
{
    private readonly Dictionary<Topic, string[]> _keywords = new()
    {
        [Topic.Technology] = new[]
        {
            // Русские
            "технологии", "техника", "софт", "приложение", "цифровой", "компьютер", "гаджет", "телефон", "ноутбук",
            "интернет", "сайт", "онлайн", "смартфон", "планшет", "устройство", "экран", "дисплей",
            "процессор", "память", "накопитель", "батарея", "зарядка", "беспроводной", "bluetooth",
            "робот", "робототехника", "дрон", "беспилотник", "электромобиль", "автомобиль",
            "патентование", "патент", "решений", "алгоритмов", "it", "айти",
            // Английские
            "technology", "tech", "software", "app", "digital", "computer", "gadget", "phone", "laptop",
            "internet", "web", "online", "smartphone", "tablet", "device", "screen", "display",
            "processor", "memory", "storage", "battery", "charging", "wireless", "patent",
            "it", "algorithm", "solution"
        },

        [Topic.ArtificialIntelligence] = new[]
        {
            // Русские
            "искусственный интеллект", "нейросеть", "нейронная", "машинное обучение", "чатбот",
            "глубокое обучение", "большая языковая модель", "генеративный", "классификация", "распознавание",
            "алгоритм", "анализ данных", "предиктивная аналитика", "автоматизация", "роботизация",
            "интеллектуальный", "самообучающийся", "модель", "llm", "gpt",
            // Английские
            "artificial intelligence", "machine learning", "neural", "gpt", "chatbot",
            "deep learning", "large language model", "generative", "classification", "recognition",
            "algorithm", "data analysis", "predictive analytics", "automation", "intelligent",
            "self-learning", "model"
        },

        [Topic.Programming] = new[]
        {
            // Русские
            "программирование", "разработчик", "код", "библиотека", "фреймворк",
            "база данных", "sql", "nosql", "postgresql", "mongodb", "redis", "бэкенд", "фронтенд",
            "веб-разработка", "мобильная разработка", "отладка", "рефакторинг", "api",
            // Английские
            "programming", "coding", "developer", "github", "framework", "library", "code",
            "database", "backend", "frontend", "web development", "mobile development", "debugging",
            "refactoring", "python", "java", "javascript", "typescript", "rust", "swift", "kotlin"
        },

        [Topic.Science] = new[]
        {
            // Русские 
            "наука", "исследование", "открытие", "ученый", "космос", "физика", "биология",
            "химия", "химический", "лаборатория", "лабораторный", "эксперимент", "экспериментальный",
            "ген", "генетика", "генетический", "молекула", "молекулярный", "клетка", "клеточный",
            "эволюция", "эволюционный", "астрономия", "астрономический", "телескоп", "микроскоп",
            "квантовый", "квантовая", "квантовое", "университет", "институт", "научный", "научная",
            "феномен", "гипотеза", "теория", "формула", "расчет", "вычисление", "моделирование",
            "нейтрино", "осцилляции", "галактика", "черная дыра", "титан", "сатурн",
            "археология", "археологи", "раскопки", "вилла", "древний", "история",
            "микоризных", "экосистем", "климата", "биологи", "грибных",
            // Английские
            "science", "research", "study", "discovery", "scientist", "space", "physics", "biology",
            "chemistry", "laboratory", "experiment", "gene", "genetics", "molecular", "cell",
            "evolution", "astronomy", "telescope", "microscope", "quantum", "university", "institute",
            "scientific", "phenomenon", "hypothesis", "theory", "formula", "calculation", "modeling",
            "neutrino", "oscillation", "galaxy", "black hole", "titan", "saturn",
            "archaeology", "archaeologist", "excavation", "ancient", "history",
            "mycorrhizal", "ecosystem", "climate", "biologist"
        },

        [Topic.Business] = new[]
        {
            // Русские
            "бизнес", "рынок", "акции", "экономика", "компания", "стартап", "инвестиции", "финансы",
            "деньги", "прибыль", "продажи", "сделка", "миллион", "миллиард", "тираж",
            "недвижимость", "жилье", "квартира", "дом", "рынок недвижимости",
            "продажа", "продавать", "собственность", "имущество", "банк", "кредит", "ипотека",
            "налог", "бюджет", "доход", "расход", "выручка", "капитал", "инвестор", "венчурный",
            "копий", "продаж", "доллар", "евро", "рубль",
            // Английские
            "business", "market", "stock", "economy", "company", "startup", "invest", "finance",
            "money", "profit", "sales", "deal", "million", "billion", "copies",
            "real estate", "property", "bank", "credit", "mortgage", "tax", "budget", "income",
            "revenue", "capital", "investor", "venture"
        },

        [Topic.Health] = new[]
        {
            // Русские 
            "здоровье", "медицина", "болезнь", "вакцина", "лечение", "врач", "больница", "вирус",
            "опасно", "опасным", "вред", "вредно", "риск", "риски", "продукты", "питание",
            "еда", "пища", "диетолог", "здоровое питание", "бактерии", "инфекция",
            "пациент", "пациента", "пациентов", "симптом", "симптомы", "диагноз", "диагностика",
            "лекарство", "препарат", "таблетки", "хирург", "операция", "клиника", "поликлиника",
            "анализ", "анализы", "болеть", "заболевание", "здоровый", "здоровое",
            "медицинский", "медицинская", "осмотр", "обследование", "выздоровление", "иммунитет",
            "прививка", "вакцинация", "эпидемия", "пандемия", "карантин", "самоизоляция", "ковид",
            "боль", "скрежет", "зубов", "жевательных", "мышц", "лечения", "бруксизм", "лицевая боль",
            // Английские
            "health", "medical", "disease", "vaccine", "covid", "treatment", "doctor", "hospital",
            "virus", "risk", "patient", "symptom", "diagnosis", "diagnostics",
            "medicine", "drug", "surgery", "clinic", "analysis", "illness", "healthy",
            "immunity", "vaccination", "epidemic", "pandemic", "quarantine",
            "pain", "teeth", "muscle", "bruxism", "facial pain"
        },

        [Topic.Sports] = new[]
        {
            // Русские
            "спорт", "футбол", "баскетбол", "теннис", "матч", "команда", "чемпионат", "турнир",
            "хоккей", "волейбол", "бокс", "единоборства", "бег", "легкая атлетика", "плавание",
            "олимпиада", "чемпион", "победитель", "тренер", "игрок", "спортсмен", "болельщик",
            "стадион", "тренировка", "соревнование", "кубок", "лига", "дисквалификация",
            "крикет", "капитан", "сборная", "тест", "таблица", "турнирная таблица", "сборная",
            // Английские
            "football", "soccer", "basketball", "tennis", "sport", "match", "team", "game",
            "championship", "tournament", "hockey", "volleyball", "boxing", "athletics", "swimming",
            "olympics", "champion", "winner", "coach", "player", "athlete", "fan",
            "stadium", "training", "competition", "cup", "league", "cricket",
            "captain", "national team", "test match", "wicket", "rangers", "salzburg"
        },

        [Topic.Gaming] = new[]
        {
            // Русские 
            "игра", "гейминг", "плейстейшен", "ксбокс", "консоль", "компьютерные игры",
            "steam", "разработчик игр", "геймдев", "инди-игра", "мобильная игра",
            "киберспорт", "стриминг", "трансляция", "обзор игры", "релиз игры",
            "дополнение", "патч", "обновление", "баг", "глитч", "уровень", "персонаж",
            "сезон", "перезапуск", "ремейк", "переиздание", "геймплей",
            "экшен", "ролевая игра", "шутер", "стратегия", "симулятор",
            "overwatch", "forza", "warhammer", "call of duty", "battlefield",
            "marathon", "detroit", "luna abyss", "bungie", "quantic dream",
            // Английские
            "gaming", "playstation", "xbox", "nintendo", "console",
            "steam", "game developer", "indie game", "mobile game",
            "esports", "streaming", "game review", "game release",
            "expansion", "patch", "update", "bug", "glitch", "level", "character",
            "season", "reboot", "remake", "remaster", "gameplay",
            "action", "rpg", "shooter", "strategy", "simulator"
        },

        [Topic.Security] = new[]
        {
            // Русские
            "безопасность", "хак", "кибер", "уязвимость", "конфиденциальность", "утечка данных", "взлом",
            "обстрел", "ракетный удар",
            "дрон", "пострадал", "пострадали", "жертвы", "погиб", "погибли",
            "ранен", "ранены", "атака", "атаковали", "военный", "военные", "вооруженные силы",
            "фронт", "боевые действия", "вооруженный конфликт", "танкер", "корабль", "флот",
            "шифрование", "аутентификация", "пароль", "вредоносное по", "вирус", "троян", "фишинг",
            // Английские
            "security", "hack", "cyber", "breach", "vulnerability", "privacy", "data leak",
            "attack", "military", "armed forces", "front", "conflict",
            "encryption", "authentication", "password", "malware", "trojan", "phishing"
        },

        [Topic.Politics] = new[]
        {
            // Русские
            "политика", "правительство", "выборы", "президент", "парламент", "голосование", "министр",
            "власть", "депутат", "закон", "государство", "законодательство", "постановление", "указ",
            "россия", "москва", "посол", "восстановление отношений", "украина", "запад",
            "суд", "мошенничество", "осудили", "приговор", "дело", "расследование", "обвинение", "адвокат",
            "сша", "индия", "китай", "евросоюз", "нато", "дипломатия", "санкции",
            "протест", "заявил", "заявила", "потребовал", "потребовала", "переговоры",
            "министр иностранных дел", "мид", "посольство", "дипломат", "международный", "альянс",
            "лейборист", "консерватор", "премьер", "министр обороны", "отставка",
            // Английские
            "politics", "government", "election", "president", "parliament", "vote", "minister",
            "power", "deputy", "law", "state", "legislation", "decree",
            "russia", "moscow", "ambassador", "ukraine", "west",
            "court", "fraud", "convicted", "sentence", "case", "investigation", "charge", "lawyer",
            "usa", "india", "china", "eu", "nato", "diplomacy", "sanctions",
            "protest", "said", "demanded", "negotiations",
            "labour", "conservative", "prime minister", "defence secretary", "resignation"
        },

        [Topic.Entertainment] = new[]
        {
            // Русские 
            "кино", "фильм", "музыка", "звезда", "голливуд", "сериал", "телевидение", "шоу",
            "певец", "актер", "актриса", "концерт", "фестиваль", "премия", "оскар", "грамми",
            "клип", "трек", "альбом", "песня", "премьера", "кастинг", "режиссер", "продюсер",
            "юморист", "комик", "стендап", "телепередача", "ток-шоу", "реалити-шоу",
            "мультфильм", "пиксар", "дисней", "шрек", "тизер", "трейлер", "зрителей",
            "комедия", "юмор", "выступление", "тур",
            // Английские
            "movie", "film", "music", "celebrity", "hollywood", "netflix", "cinema", "tv",
            "singer", "actor", "actress", "concert", "festival", "award", "oscar", "grammy",
            "clip", "track", "album", "song", "premiere", "casting", "director", "producer",
            "comedian", "stand-up", "tv show", "talk show", "reality show",
            "animation", "pixar", "disney", "shrek", "teaser", "trailer", "viewers",
            "comedy", "humor", "performance", "tour"
        }
    };

    public Topic? Detect(string title, string? description = null)
    {
        var text = (title + " " + (description ?? "")).ToLower();
        var words = Regex.Split(text, @"[\s\p{P}]+")
            .Where(w => w.Length > 2)
            .ToHashSet();

        var matches = new Dictionary<Topic, int>();

        foreach (var (topic, keywords) in _keywords)
        {
            var count = keywords.Count(keyword =>
                words.Contains(keyword.ToLower()) ||
                text.Contains(keyword.ToLower() + " ") ||
                text.Contains(" " + keyword.ToLower())
            );

            if (count > 0)
                matches[topic] = count;
        }

        if (matches.Count == 0)
            return null;

        return matches.OrderByDescending(x => x.Value).First().Key;
    }
}