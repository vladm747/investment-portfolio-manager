using Microsoft.AspNetCore.Mvc;

namespace PortfolioManager.API.Controllers;

public class PortfolioController : ControllerBase
{
    [Route("api/[controller]")]
    [HttpGet]
    public IActionResult Index()
    {
        return Ok("It works!");
    }
}