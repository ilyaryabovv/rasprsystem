using FileService.Storage;
using Microsoft.AspNetCore.Mvc;

namespace FileService.Controllers;

[ApiController]
[Route("api/files")]
public class FilesController : ControllerBase
{
    private readonly IFileStorage _storage;

    public FilesController(IFileStorage storage)
    {
        _storage = storage;
    }

    [HttpGet("{name}")]
    public async Task<IActionResult> Download(string name)
    {
        if (!IsSafeName(name))
        {
            return BadRequest();
        }

        var content = await _storage.Read(name);
        return content is null ? NotFound() : File(content, "text/csv", name);
    }

    private static bool IsSafeName(string name)
    {
        return name.Length > 0
            && name.Length <= 120
            && name.All(character => char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_');
    }
}
