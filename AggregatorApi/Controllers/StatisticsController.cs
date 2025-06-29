using AggregatorApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AggregatorApi.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class StatisticsController(IApiStatisticsService ApiStatisticsService) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(ApiStatisticsService.GetAll());
}
