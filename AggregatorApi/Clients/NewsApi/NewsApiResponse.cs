using System.Text.Json.Serialization;

namespace AggregatorApi.Clients.NewsApi;

public class NewsApiResponse
{
    [JsonPropertyName("articles")]
    public List<Article>? Articles { get; set; }
}
public class Article
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    [JsonPropertyName("publishedAt")]
    public string? PublishedAt { get; set; }
}