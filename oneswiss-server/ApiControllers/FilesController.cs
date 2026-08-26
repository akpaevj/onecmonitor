using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Common.Extensions;
using OneSwiss.Server.Models;
using OneSwiss.Server.Services;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/files")]
public class FilesController(AppDbContext dbContext, FilesProvider filesProvider, ILogger<FilesController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<IReadOnlyList<FileListItem>> GetList(CancellationToken cancellationToken)
    {
        var rows = await dbContext.Files
            .AsNoTracking()
            .OrderBy(f => f.FileType)
            .ThenBy(f => f.Name)
            .Select(f => new RawFileItem(
                f.Id,
                f.Name,
                f.Version,
                f.FileType,
                f.DataPath))
            .ToListAsync(cancellationToken);

        return rows
            .Select(ToListItem)
            .ToList();
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = $"{Roles.ReadMaintenanceTasks},{Roles.WriteMaintenanceTasks}")]
    public async Task<ActionResult<FileListItem>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await dbContext.Files
            .AsNoTracking()
            .Where(f => f.Id == id)
            .Select(f => new RawFileItem(
                f.Id,
                f.Name,
                f.Version,
                f.FileType,
                f.DataPath))
            .SingleOrDefaultAsync(cancellationToken);

        if (item == null)
            return NotFound();

        return Ok(ToListItem(item));
    }

    [HttpPost]
    [Authorize(Roles = Roles.WriteMaintenanceTasks)]
    [IgnoreAntiforgeryToken]
    [RequestSizeLimit(long.MaxValue)]
    public async Task<ActionResult<FileListItem>> Create([FromForm] CreateFileRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length <= 0)
            return BadRequest("Прикрепите файл");

        if (string.IsNullOrWhiteSpace(request.Version))
            return BadRequest("Не указана версия");

        var fileType = ParseFileType(request.File.FileName);
        if (fileType == null)
            return BadRequest("Недопустимый тип файла. Разрешены: .cf, .cfe, .cfu, .epf, .ospx");

        var name = string.IsNullOrWhiteSpace(request.Name)
            ? Path.GetFileNameWithoutExtension(request.File.FileName)
            : request.Name.Trim();

        if (string.IsNullOrWhiteSpace(name))
            return BadRequest("Не заполнено наименование");

        var extension = Path.GetExtension(request.File.FileName).TrimStart('.').ToLowerInvariant();
        var dataPath = $"{Guid.NewGuid()}.{extension}";

        try
        {
            await using (var stream = filesProvider.OpenDataStream(dataPath))
            {
                await request.File.CopyToAsync(stream, cancellationToken);
            }

            var entity = new Models.File
            {
                Name = name,
                Version = request.Version.Trim(),
                FileType = fileType.Value,
                DataPath = dataPath
            };

            dbContext.Files.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);

            return Ok(ToListItem(entity));
        }
        catch (OperationCanceledException)
        {
            filesProvider.DeleteDataFile(dataPath);
            throw;
        }
        catch (Exception e)
        {
            filesProvider.DeleteDataFile(dataPath);
            logger.LogError(e, "Ошибка загрузки файла {FileName}", request.File.FileName);
            return Problem("Не удалось сохранить файл", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.WriteMaintenanceTasks)]
    public async Task<ActionResult<FileListItem>> Update(Guid id, [FromBody] UpdateFileRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Files.SingleOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (entity == null)
            return NotFound();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Не заполнено наименование");

        if (string.IsNullOrWhiteSpace(request.Version))
            return BadRequest("Не указана версия");

        entity.Name = request.Name.Trim();
        entity.Version = request.Version.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToListItem(entity));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.WriteMaintenanceTasks)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Files.SingleOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (entity == null)
            return NotFound();

        var dataPath = entity.DataPath;

        dbContext.Files.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        filesProvider.DeleteDataFile(dataPath);

        return NoContent();
    }

    private static FileType? ParseFileType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToUpperInvariant();

        return extension switch
        {
            ".CF" => FileType.Cf,
            ".CFE" => FileType.Cfe,
            ".CFU" => FileType.Cfu,
            ".EPF" => FileType.Epf,
            ".OSPX" => FileType.Ospx,
            _ => null
        };
    }

    private static FileListItem ToListItem(RawFileItem f)
    {
        return new FileListItem(
            f.Id,
            f.Name,
            f.Version,
            f.FileType.ToString(),
            f.FileType.GetDisplay(),
            f.DataPath);
    }

    private static FileListItem ToListItem(Models.File f)
    {
        return new FileListItem(
            f.Id,
            f.Name,
            f.Version,
            f.FileType.ToString(),
            f.FileType.GetDisplay(),
            f.DataPath);
    }

    private sealed record RawFileItem(
        Guid Id,
        string Name,
        string Version,
        FileType FileType,
        string DataPath);

    public sealed record FileListItem(
        Guid Id,
        string Name,
        string Version,
        string FileType,
        string FileTypeDisplay,
        string DataPath);

    public sealed class CreateFileRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public IFormFile? File { get; set; }
    }

    public sealed record UpdateFileRequest(
        string Name,
        string Version);
}
