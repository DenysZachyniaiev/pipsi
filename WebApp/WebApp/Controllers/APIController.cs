using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using WebApp.Data;

[ApiController]
[Authorize]
public class ApiController : ControllerBase
{
    private readonly AppDbContext context;

    public ApiController(AppDbContext context)
    {
        this.context = context;
    }

    [Route("api/Students/preview")]
    [HttpGet]
    public IActionResult PreviewStudents()
    {
        var students = context.Students.ToList();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(students, options);

        return Content(json, "application/json", Encoding.UTF8);
    }

    [Route("api/Students/download")]
    [HttpGet]
    public IActionResult DownloadStudents()
    {
        var students = context.Students.ToList();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(students, options);
        var bytes = Encoding.UTF8.GetBytes(json);
        var stream = new MemoryStream(bytes);

        return File(stream, "application/json", "students.json");
    }
}