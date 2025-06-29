# AggregatorApi

## Overview

AggregatorApi is an ASP.NET Core service that aggregates data from multiple external APIs (OpenMeteo, NewsApi, NobelPrize) and provides a unified endpoint for clients to retrieve, filter, and sort the combined data. It also exposes request statistics and supports caching, parallelism, and JWT authentication.

## Features

- Aggregates data from OpenMeteo, NewsApi, and NobelPrize APIs in parallel
- Unified endpoint for aggregated data
- Filtering and sorting by date, category, and other fields
- Error handling and fallback for unavailable APIs
- In-memory caching to reduce redundant API calls
- Request statistics endpoint (total requests, average response time, performance buckets)
- JWT authentication (demo token endpoint)
- Background service for performance anomaly logging

## Endpoints

### 1. Get Aggregated Data

**GET** `/Aggregate`  
**Authorization:** Bearer JWT required

#### Query Parameters

| Name       | Type     | Description                                 |
|------------|----------|---------------------------------------------|
| date       | string   | Filter by date (ISO 8601, optional)         |
| category   | string   | Filter by category (optional)               |
| sortBy     | string   | Field to sort by (date, title, etc.)        |
| descending | bool     | Sort descending (optional)                  |

#### Example

```
GET /Aggregate?category=Weather&sortBy=date&descending=true
```

#### Response

```json
{
  "items": [
    {
      "source": "NewsApi",
      "title": "Storm Warning",
      "description": "Heavy rain expected...",
      "date": "2024-06-01T12:00:00Z",
      "category": "Weather"
    }
    // ...
  ],
  "errorMessages": [
    "Weather API unavailable"
  ]
}
```

### 2. Get API Request Statistics

**GET** `/Statistics`  
**Authorization:** Bearer JWT required

#### Response

```json
[
  {
    "apiName": "NewsApi",
    "totalRequests": 120,
    "failedCount": 2,
    "averageResponseMs": 150,
    "fastCount": 80,
    "averageCount": 30,
    "slowCount": 10
  }
  // ...
]
```

### 3. Get Demo JWT Token

**GET** `/Authentication`

Returns a short-lived JWT token for testing.  
**No authentication required.**

#### Response

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

## Input/Output Formats

- All endpoints use JSON for request and response bodies.
- Dates are in ISO 8601 format.

## Setup & Configuration

1. **Clone the repository**
```sh
git clone https://github.com/yourusername/AggregatorApi.git
cd AggregatorApi
```

2. **Configure API Keys**
   - Set your NewsApi key in `appsettings.json` under `NewsApi:ApiKey`.
   - Set JWT settings in `appsettings.json` under `Jwt`.

3. **Run the application**
```sh
dotnet run --project AggregatorApi/AggregatorApi.csproj
```

4. **Run tests**
```sh
dotnet test
```

## Caching

- In-memory caching is used for each API client to reduce redundant external API calls.

## Error Handling

- If an external API is unavailable, its error message is included in the `errorMessages` array in the response.
- The service continues to return data from available sources.

## Authentication

- All endpoints except `/Authentication` require a JWT Bearer token.
- Use `/Authentication` to obtain a demo token for testing.

## Performance & Parallelism

- Data fetching from external APIs is performed in parallel to minimize response time.
- Request statistics are tracked in memory and exposed via the statistics endpoint.

## License

MIT
