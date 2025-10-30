using StackExchange.Redis;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// --- PASSO 1: Configurar a conexão com o Redis ---
var redisConnectionString = "my-redis-master.redis:6379";
var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

// --- PASSO 2: Configurar o CORS ---
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      policy =>
                      {
                          policy.WithOrigins("http://localhost:7008", // Ajuste para a porta do seu front local
                                             "http://front.homelab.local")
                                .AllowAnyHeader()
                                .AllowAnyMethod();
                      });
});

var app = builder.Build();

// --- PASSO 3: Ligar o CORS (A ORDEM CORRETA É AQUI!) ---
app.UseCors(MyAllowSpecificOrigins);

// --- PASSO 4: Definir os Endpoints ---
app.MapGet("/", (IConnectionMultiplexer redis, HttpContext context) => { // Adicionado HttpContext

    IDatabase db = redis.GetDatabase();
    string cacheKey = "minha-mensagem";
    string? valorDoCache = db.StringGet(cacheKey);

    // Usando Results.Text para garantir o Content-Type
    if (!string.IsNullOrEmpty(valorDoCache))
    {
        return Results.Text($"[DO CACHE]: {valorDoCache}");
    }

    string valorDoBanco = $"Aplicacao .NET 9 minima rodando no K3s! Sucesso! (Gerado em: {DateTime.Now:HH:mm:ss})";
    db.StringSet(cacheKey, valorDoBanco, TimeSpan.FromSeconds(10));
    return Results.Text($"[DO BANCO DE DADOS]: {valorDoBanco}");
});

app.Run();