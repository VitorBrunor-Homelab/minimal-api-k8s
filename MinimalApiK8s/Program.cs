using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Redis") 
                       ?? "redis:6379,abortConnect=false";

Console.WriteLine($"[LOG] Conectando no Redis: {connectionString}");

Console.WriteLine("[LOG] ta rodando hein");

var redisConnection = ConnectionMultiplexer.Connect(connectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.AllowAnyOrigin()
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

var app = builder.Build();

app.UseCors(MyAllowSpecificOrigins);

app.MapGet("/", (IConnectionMultiplexer redis) => 
{
    IDatabase db = redis.GetDatabase();
    string cacheKey = "minha-mensagem";
    string? valorDoCache = db.StringGet(cacheKey);

    if (!string.IsNullOrEmpty(valorDoCache))
    {
        return Results.Text($"[DO CACHE]: {valorDoCache}");
    }

    string valorDoBanco = $"Aplicacao .NET rodando no K3s! Sucesso! (Gerado em: {DateTime.Now:HH:mm:ss})";
    db.StringSet(cacheKey, valorDoBanco, TimeSpan.FromSeconds(10));
    
    return Results.Text($"[DO BANCO DE DADOS]: {valorDoBanco}");
});

app.Run();