namespace AggregatorApi.Models;

public class AggregationRequest
{

    /// <summary>
    /// Date for filtering results (optional).
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Category for filtering results (optional).
    /// 
    public string? Category { get; set; }

    /// <summary>
    /// Sort field for results. Defaults to null (no specific sort).
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Indicates whether to sort results in descending order. Defaults to null (no specific order).
    /// </summary>
    public bool? Descending { get; set; }

}