using HnBestStories.Services;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMemoryCache();

// Typed HttpClient + typed implementation (evita CS1643)
builder.Services
    .AddHttpClient<IHnService, HnService>()
    .ConfigureHttpClient(client =>
    {
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        client.Timeout = TimeSpan.FromSeconds(5);
    });

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();