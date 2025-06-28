using Microsoft.AspNetCore.Mvc;

namespace AggregatorApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AggregateController : ControllerBase
    {      
        [HttpGet]
        public IActionResult Get()
        {
            throw new NotImplementedException("This API is not implemented yet.");
        }
    }
}
