using FileService.Dto;
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

    [HttpPost]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<StoredFileDto>> Upload(IFormFile file)
    {
        if (file.Length == 0 || !IsSafeName(file.FileName))
        {
            return BadRequest();
        }

        await using var stream = file.OpenReadStream();
        await _storage.Save(file.FileName, stream);
        return new StoredFileDto(file.FileName);
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
