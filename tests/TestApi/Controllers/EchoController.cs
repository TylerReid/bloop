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

    [HttpGet]
    [Route("query")]
    public IActionResult Echo([FromQuery]string something)
    {
        return Ok(something);
    }

    [HttpPost]
    [Route("form")]
    public IActionResult Echo([FromForm]Dictionary<string, string> form)
    {
        return Ok(form);
    }
}