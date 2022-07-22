using Microsoft.AspNetCore.Mvc;

namespace TestApi.Controllers;

[ApiController]
[Route("[controller]")]
public class EchoController : ControllerBase
{
    private readonly IHttpContextAccessor _context;

    public EchoController(IHttpContextAccessor context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult Echo()
    {
        var request = _context.HttpContext!.Request;
        return Ok(new 
        {
            request.Path,
            request.QueryString,
            request.Headers,
        });
    }
}