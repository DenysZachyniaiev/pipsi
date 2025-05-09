using Microsoft.AspNetCore.Mvc;
using WebApp.Data;
using WebApp.Models;

[ApiController]
public class ApiController : ControllerBase
{
    private readonly AppDbContext context;

    public ApiController(AppDbContext context)
    {
        this.context = context;
    }
    [Route("api/Students")]
    [HttpGet]
    public ActionResult<IEnumerable<Student>> GetStudents()
    {
        var students = context.Students.ToList();
        return Ok(students);
    }
}