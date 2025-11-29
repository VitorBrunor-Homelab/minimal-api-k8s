using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// --- PASSO 1: Configurar a conexão com o Redis ---

// Tenta pegar a string de conexão das variáveis de ambiente (Kubernetes)
// Se não encontrar, usa o valor padrão "redis:6379" com abortConnect=false para não crashar
var connectionString = builder.Configuration.GetConnectionString("Redis") 
                       ?? "redis:6379,abortConnect=false";

// Log para ajudar no debug (aparece no 'kubectl logs')
Console.WriteLine($"[LOG] Conectando no Redis: {connectionString}");

Console.WriteLine("[LOG] ta rodando hein");

// Cria a conexão e injeta como Singleton
var redisConnection = ConnectionMultiplexer.Connect(connectionString);
builder.Services.AddSingleton<IConnectionMultiplexer>(redisConnection);

// --- PASSO 2: Configurar o CORS ---
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

// --- PASSO 3: Ligar o CORS ---
app.UseCors(MyAllowSpecificOrigins);

// --- PASSO 4: Definir os Endpoints ---
app.MapGet("/", (IConnectionMultiplexer redis) => 
{
    IDatabase db = redis.GetDatabase();
    string cacheKey = "minha-mensagem";
    string? valorDoCache = db.StringGet(cacheKey);

    // Se tiver no cache, retorna rápido
    if (!string.IsNullOrEmpty(valorDoCache))
    {
        return Results.Text($"[DO CACHE]: {valorDoCache}");
    }

    // Se não, gera novo valor e salva no cache por 10 segundos
    string valorDoBanco = $"Aplicacao .NET rodando no K3s! Sucesso! (Gerado em: {DateTime.Now:HH:mm:ss})";
    db.StringSet(cacheKey, valorDoBanco, TimeSpan.FromSeconds(10));
    
    return Results.Text($"[DO BANCO DE DADOS]: {valorDoBanco}");
});

app.Run();