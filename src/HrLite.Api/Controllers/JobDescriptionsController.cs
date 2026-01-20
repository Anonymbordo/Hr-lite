using Microsoft.AspNetCore.Mvc;
using HrLite.Application.Interfaces;

namespace HrLite.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JobDescriptionsController : ControllerBase
{
    private readonly IAiService _aiService;

    // Servisi içeri alıyoruz (Constructor Injection)
    public JobDescriptionsController(IAiService aiService)
    {
        _aiService = aiService;
    }

    // Düğmeye basılınca çalışacak metod
    [HttpGet("generate")]
    public async Task<IActionResult> Generate(string role, string department)
    {
        // Yapay zekaya işi devrediyoruz
        var result = await _aiService.GenerateJobDescriptionAsync(role, department);
        
        // Sonucu ekrana basıyoruz
        return Ok(new { Role = role, Department = department, AiResponse = result });
    }
}