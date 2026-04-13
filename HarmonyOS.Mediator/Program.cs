using Telegram.Bot;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddHttpClient();

var botToken = "8744008836:AAHBFN3LV4eJymXDwVutr5DJLuBtS8L5Zv8";
var ngrokUrl = "https://bennie-subcarinate-feyly.ngrok-free.dev"; 

builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
    var webhookUrl = $"{ngrokUrl}/api/bot";
    
    await botClient.SetWebhook(webhookUrl);
    
    Console.WriteLine($"[INFO] Вебхук успешно установлен на: {webhookUrl}");
}

app.MapControllers();

app.Run();