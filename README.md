# HnBestStories – Hacker News “Best _n_ Stories” API (ASP.NET Core .NET 8)

A minimal REST API that returns the **best _n_ stories** from Hacker News, sorted by **descending `score`**.  
It consumes the public HN API and avoids overloading it via caching, limited concurrency, and retries.

---

## ✨ Endpoint

GET /api/stories/best?count={n}


**Query parameters**
- `count` (required): allowed range `1..100`.

**Response (JSON)**
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
```

### Field mapping (from HN item):

title ← item.title
uri ← item.url
postedBy ← item.by
time (UNIX → ISO-8601) ← item.time
score ← item.score
commentCount ← item.descendants


## 🧰 Requirements

.NET 8 SDK
(Optional) VS Code with extensions: C# Dev Kit, REST Client / Thunder Client

##🚀 Run locally

cd HnBestStories
dotnet restore
dotnet run
Swagger (Development): http://localhost:5091/swagger


## 🏗️ Project structure

HnBestStories/
├─ Controllers/
│  └─ BestStoriesController.cs      # GET /api/stories/best
├─ Dtos/
│  └─ StoryDto.cs                   # Output contract
├─ Services/
│  ├─ IHnService.cs                 # HN service abstraction
│  └─ HnService.cs                  # Cache, retries, concurrency limiting
├─ Program.cs                       # DI + middleware pipeline
└─ HnBestStories.csproj


## ⚙️ Implementation (technical summary)

HttpClientFactory: typed client IHnService, HnService with 5s timeout.
Polly v7:
WaitAndRetryAsync(3) with exponential backoff.
(Circuit breaker can be added as an enhancement.)
IMemoryCache:
Cache HN beststories IDs for 60s.
Cache each item/{id} for 5 min (with 2 min sliding).
Concurrency limit: SemaphoreSlim(12) to avoid hammering HN.
Sorting: explicitly sort by score desc before returning.
Validation: count must be in 1..100 (400 if invalid).
Swagger enabled in Development.
Tunable values (see Services/HnService.cs / Program.cs):
BestIdsTtl = 60s
ItemTtl = 5min
Gate = new SemaphoreSlim(12)
Polly retries: 3 attempts, 200ms * 2^n
HttpClient.Timeout = 5s

## 📝 Assumptions

count limited to 1..100 to protect both the upstream API and this service.
TTLs chosen to balance freshness and efficiency (IDs 60s; items 5min).
Only returns items with Type == "story".
time is exposed in ISO-8601.

## ▶️ Quick start

dotnet run --project HnBestStories
# then open:
#   http://localhost:5091/swagger
# or:
curl "http://localhost:5091/api/stories/best?count=10"