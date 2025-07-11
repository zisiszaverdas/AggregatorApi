using AggregatorApi.Models;
using AggregatorApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorApi.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class AggregateController(IAggregationService AggregationService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] AggregationRequest query, CancellationToken ct) =>
        Ok(await AggregationService.GetAggregatedDataAsync(query, ct));
}
