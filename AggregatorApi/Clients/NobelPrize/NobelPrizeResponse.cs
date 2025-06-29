using AggregatorApi.Models;
using Microsoft.Extensions.Caching.Hybrid;
using System.Text.Json.Serialization;

namespace AggregatorApi.Clients.NobelPrize;

public class NobelPrizeResponse
{
    [JsonPropertyName("nobelPrizes")]
    public List<NobelPrize>? NobelPrizes { get; set; }
}

public class NobelPrize
{
    [JsonPropertyName("awardYear")]
    public string? AwardYear { get; set; }
    [JsonPropertyName("category")]
    public Category? Category { get; set; }
    [JsonPropertyName("categoryFullName")]
    public CategoryFullName? CategoryFullName { get; set; }
    [JsonPropertyName("dateAwarded")]
    public string? DateAwarded { get; set; }
    [JsonPropertyName("laureates")]
    public List<Laureate>? Laureates { get; set; }
}

public class Category
{
    [JsonPropertyName("en")]
    public string? En { get; set; }
}

public class CategoryFullName
{
    [JsonPropertyName("en")]
    public string? En { get; set; }
}

public class Laureate
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    [JsonPropertyName("knownName")]
    public KnownName? KnownName { get; set; }
    [JsonPropertyName("fullName")]
    public FullName? FullName { get; set; }
    [JsonPropertyName("motivation")]
    public Motivation? Motivation { get; set; }
}

public class KnownName
{
    [JsonPropertyName("en")]
    public string? En { get; set; }
}

public class FullName
{
    [JsonPropertyName("en")]
    public string? En { get; set; }
}

public class Motivation
{
    [JsonPropertyName("en")]
    public string? En { get; set; }
}
