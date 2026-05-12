using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace HarmonyOS.Mediator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BotController : ControllerBase
{
    private readonly ITelegramBotClient _botClient;
    private readonly HttpClient _httpClient;

    public BotController(ITelegramBotClient botClient, IHttpClientFactory httpClientFactory)
    {
        _botClient = botClient;
        _httpClient = httpClientFactory.CreateClient();
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Update update)
    {
        // Нас интересуют только обычные текстовые сообщения
        if (update.Message?.Text == null)
            return Ok();

        var chatId = update.Message.Chat.Id; 
        var userId = update.Message.From!.Id;
        var isGroup = update.Message.Chat.Type is ChatType.Group or ChatType.Supergroup; 
        var messageText = update.Message.Text;

        Console.WriteLine($"[TG] Получено сообщение: {messageText}");

        // 1. Составляем жесткий промпт для нейросети
        var prompt = $@"Проанализируй текст: '{messageText}'.
           Если текст содержит пассивную агрессию, токсичность, грубость или переход на личности, верни JSON: {{ ""is_toxic"": true, ""alternative"": ""твой вежливый вариант"" }}.
           ВАЖНЫЕ ПРАВИЛА ДЛЯ ALTERNATIVE:
           1. Пиши как живой человек, современный IT-специалист.
           2. Никакой канцелярщины, излишней официальности и фраз вроде 'Мне жаль' или 'Давайте вместе найдем решение'.
           3. Текст должен быть кратким, дружелюбным, но по делу (например: 'Сроки поджимают, подскажи, когда сможешь скинуть отчет? Нужна ли помощь?').
           Если текст абсолютно нормальный, верни JSON: {{ ""is_toxic"": false }}.
           Верни ТОЛЬКО валидный JSON-объект без пояснений.";

        var ollamaRequest = new
        {
            model = "gemma2:9b",
            prompt = prompt,
            stream = false,
            format = "json" 
        };

        try
        {
            // 2. Стучимся в локальную нейронку
            var response = await _httpClient.PostAsJsonAsync("http://localhost:11434/api/generate", ollamaRequest);
            var resultString = await response.Content.ReadAsStringAsync();

            var ollamaResult = JsonSerializer.Deserialize<OllamaResponse>(resultString);
            
            // 3. Парсим ответ ИИ
            var aiAnalysis = JsonSerializer.Deserialize<AiAnalysisResult>(ollamaResult?.response ?? "{}");

            // 4. Если ИИ решил, что это токсично - бот вмешивается
            if (aiAnalysis != null && aiAnalysis.is_toxic)
            {
                var replyMessage = isGroup 
                    ? $"🚨 <b>Кажется, твое сообщение в группе прозвучало резковато.</b>\n<i>Как насчет такого варианта?</i>\n\n«{aiAnalysis.alternative}»"
                    : $"🚨 <b>Кажется, это прозвучало резковато.</b>\n<i>Как насчет такого варианта?</i>\n\n«{aiAnalysis.alternative}»";
    
                try
                {
                    await _botClient.SendMessage(
                        chatId: userId, 
                        text: replyMessage,
                        parseMode: ParseMode.Html
                    );
        
                    Console.WriteLine($"[AI] Отправили предупреждение в личку пользователю {userId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TG ERROR] Не удалось написать юзеру {userId} в ЛС. Скорее всего, он не активировал бота. Ошибка: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Ошибка обработки ИИ: {ex.Message}");
        }

        return Ok();
    }
}


public class OllamaResponse
{
    public string response { get; set; }
}

public class AiAnalysisResult
{
    public bool is_toxic { get; set; }
    public string alternative { get; set; }
}