# HnBestStories – Hacker News Best n Stories API (ASP.NET Core .NET 8)

API REST que devuelve las **mejores _n_ historias** de Hacker News (ordenadas por `score` descendente), consumiendo la API pública de HN y **evitando sobrecargarla** mediante cache, concurrencia limitada y reintentos.

## ✨ Endpoint

GET /api/stories/best?count={n}


**Parámetros**
- `count` (query): requerido. Rango permitido `1..100`.

**Respuesta (JSON)**
```json
[
  {
    "title": "A uBlock Origin update was rejected from the Chrome Web Store",
    "uri": "https://github.com/uBlockOrigin/uBlock-issues/issues/745",
    "postedBy": "ismaildonmez",
    "time": "2019-10-12T13:43:01+00:00",
    "score": 1716,
    "commentCount": 572
  }
]
Campos mapeados desde HN: title←item.title, uri←item.url, postedBy←item.by,
time (UNIX→ISO-8601) ←item.time, score←item.score, commentCount←item.descendants.

🧰 Requisitos
.NET 8 SDK

(Opcional) Docker 24+

(Opcional) VS Code con extensiones: C# Dev Kit, REST Client/Thunder Client.

🚀 Ejecutar en local
cd HnBestStories
dotnet restore
dotnet run
Swagger (desarrollo): http://localhost:5091/swagger

Ejemplo con curl:

curl "http://localhost:5091/api/stories/best?count=5"
Si ves el warning Failed to determine the https port for redirect:

O desactiva app.UseHttpsRedirection() en Program.cs, o

Configura HTTPS local:

dotnet dev-certs https --trust
dotnet run --urls "http://localhost:5091;https://localhost:7091"
Swagger: https://localhost:7091/swagger

🏗️ Estructura del proyecto

HnBestStories/
├─ Controllers/
│  └─ BestStoriesController.cs         # GET /api/stories/best
├─ Dtos/
│  └─ StoryDto.cs                      # Contrato de salida
├─ Services/
│  ├─ IHnService.cs                    # Abstracción del servicio HN
│  └─ HnService.cs                     # Implementación: cache, polly, concurrencia
├─ Program.cs                          # Composition Root (DI + pipeline)
└─ HnBestStories.csproj

⚙️ Implementación (resumen técnico)
HttpClientFactory: cliente tipado IHnService, HnService con timeout de 5s.

Polly v7:

WaitAndRetryAsync(3) con backoff exponencial.

IMemoryCache:

Cachea IDs de beststories por 60s.

Cachea cada item/{id} por 5 min (con SlidingExpiration 2 min).

Concurrencia limitada: SemaphoreSlim(12) para no saturar a HN.

Orden: se ordena por score desc antes de retornar (por robustez).

Validación: count entre 1 y 100 (400 si inválido).

Swagger habilitado en Development.

Valores clave que puedes ajustar en Services/HnService.cs:

BestIdsTtl = 60s

ItemTtl = 5min

Gate = new SemaphoreSlim(12)

Retries Polly (3 intentos, backoff 200ms*2^n)

HttpClient.Timeout = 5s (en Program.cs)

📝 Supuestos
count limitado a 1..100 para proteger al upstream y la API.

TTLs: IDs 60s; items 5min (balance entre frescura y eficiencia).

El endpoint solo devuelve historias (Type == "story").

time se expone en ISO-8601.

▶️ Cómo correr rápidamente

dotnet run --project HnBestStories
# luego abre http://localhost:5091/swagger
# o:
curl "http://localhost:5091/api/stories/best?count=10"