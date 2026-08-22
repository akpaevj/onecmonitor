using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Dto.ErrorLoggingService;
using OneSwiss.Server.Helpers;
using OneSwiss.Server.Models;
using OneSwiss.Server.Services;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/[controller]")]
public class ErrorLoggingServiceController(
    AppDbContext dbContext,
    NotificationsService notificationsService,
    ILogger<ErrorLoggingServiceController> logger) : ControllerBase
{
    [HttpGet("settings")]
    [Authorize]
    public async Task<ErrorLoggingServiceSettingsItem> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await dbContext.ErrorLoggingServiceSettings
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return new ErrorLoggingServiceSettingsItem(
            settings?.Enabled ?? false,
            settings?.Message ?? string.Empty,
            settings?.ReportsTtl ?? 30);
    }

    [HttpPut("settings")]
    [Authorize]
    public async Task<ActionResult<ErrorLoggingServiceSettingsItem>> SaveSettings(
        [FromBody] SaveErrorLoggingServiceSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var validationError = ValidateSettingsRequest(request);
        if (validationError != null)
            return validationError;

        var settings = await dbContext.ErrorLoggingServiceSettings
            .OrderBy(c => c.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (settings == null)
        {
            settings = new ErrorLoggingServiceSettings();
            dbContext.ErrorLoggingServiceSettings.Add(settings);
        }

        settings.Enabled = request.Enabled;
        settings.Message = request.Message.Trim();
        settings.ReportsTtl = request.ReportsTtl;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new ErrorLoggingServiceSettingsItem(
            settings.Enabled,
            settings.Message,
            settings.ReportsTtl));
    }

    [HttpGet("reports")]
    [Authorize(Roles = Roles.ReadErrorLoggingReports)]
    public async Task<IReadOnlyList<ErrorReportListItem>> GetReports(CancellationToken cancellationToken)
    {
        var entities = await dbContext.ErrorReports
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var result = new List<ErrorReportListItem>(entities.Count);

        foreach (var entity in entities)
        {
            var report = DeserializeReport(entity.Report);
            if (report == null)
                continue;

            result.Add(new ErrorReportListItem(
                entity.Id,
                report.Time,
                report.ConfigInfo.Name,
                report.ConfigInfo.Version,
                report.ServerInfo.AppVersion,
                report.SessionInfo.UserName,
                report.AdditionalInfo ?? string.Empty));
        }

        return result;
    }

    [HttpGet("reports/{id:guid}")]
    [Authorize(Roles = Roles.ReadErrorLoggingReports)]
    public async Task<ActionResult<ErrorReportDetailsItem>> GetReportDetails(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.ErrorReports
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (entity == null)
            return NotFound();

        var report = DeserializeReport(entity.Report);
        if (report == null)
            return BadRequest("Не удалось разобрать содержимое отчета");

        var error = report.ErrorInfo.ApplicationErrorInfo.Errors.FirstOrDefault();

        return Ok(new ErrorReportDetailsItem(
            entity.Id,
            report.Time,
            report.ConfigInfo.Name,
            report.ConfigInfo.Version,
            report.ConfigInfo.CompatibilityMode,
            report.ServerInfo.AppVersion,
            report.SessionInfo.UserName,
            report.SessionInfo.DataSeparation,
            report.ClientInfo.AppName,
            report.ClientInfo.AppVersion,
            report.ClientInfo.SystemInfo.OsVersion,
            report.ClientInfo.SystemInfo.FreeRam,
            report.ClientInfo.SystemInfo.FullRam,
            report.AdditionalInfo ?? string.Empty,
            error?.Text ?? string.Empty,
            error?.Categories ?? [],
            report.GetStack(),
            entity.Screenshot?.Length > 0 ? Convert.ToBase64String(entity.Screenshot) : null));
    }

    [HttpPost("getInfo")]
    public async Task<IActionResult> GetInfo([FromBody] GetInfoRequest request, CancellationToken cancellationToken)
    {
        var settings = await dbContext.ErrorLoggingServiceSettings.AsNoTracking()
            .OrderBy(c => c.Id)
            .SingleOrDefaultAsync(cancellationToken);
        var needSend = settings?.Enabled ?? false;

        if (needSend)
        {
            // Если отправлять надо, то найдем, была ли такая ошибка 
            var hash = GetHash(request.ClientStackHash ?? "", request.AppStackHash ?? "");
            needSend = !await dbContext.ErrorReports.AnyAsync(c => c.Hash == hash, cancellationToken);
        }

        var content = JsonSerializer.Serialize(new GetInfoResponse
        {
            NeedSendReport = needSend,
            UserMessage = settings?.Message,
            DumpType = 1
        });
        var contentData = Encoding.UTF8.GetBytes(content);

        Response.Headers.ContentLength = contentData.Length;

        return Content(content, "application/json; charset=utf-8");
    }

    [HttpPost("pushReport")]
    public async Task<IActionResult> PushReport(CancellationToken cancellationToken)
    {
        if (Request.Form.Files.Count <= 0)
            return new EmptyResult();

        var unpackingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        try
        {
            var file = Request.Form.Files[0];
            await using var stream = file.OpenReadStream();
            ZipFile.ExtractToDirectory(stream, unpackingPath);

            var reportPath = Path.Combine(unpackingPath, "report.json");
            var reportData = await System.IO.File.ReadAllTextAsync(reportPath, cancellationToken);
            var report = JsonSerializer.Deserialize<ReportRoot>(reportData, ErrorReportsHelper.ReportSerializerOptions);

            if (report == null)
            {
                logger.LogError($"Ошибка разбора отчета об ошибке:\n {reportData}");
            }
            else
            {
                // Сначала найдем отчет с таким же хешем
                var model = new ErrorReport
                {
                    CreatedAt = DateTime.UtcNow,
                    Report = reportData,
                    Hash = GetHash(report.ErrorInfo.SystemErrorInfo.ClientStackHash,
                        report.ErrorInfo.ApplicationErrorInfo.StackHash)
                };

                if (report.Screenshot?.File != null)
                {
                    var screenshotPath = Path.Combine(unpackingPath, report.Screenshot.File);
                    model.Screenshot = await System.IO.File.ReadAllBytesAsync(screenshotPath, cancellationToken);
                }

                dbContext.ErrorReports.Add(model);

                await dbContext.SaveChangesAsync(cancellationToken);
                await notificationsService.QueueErrorReportReceived(model.Id, cancellationToken);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка обработки отчета об ошибке");
        }
        finally
        {
            if (Directory.Exists(unpackingPath))
                Directory.Delete(unpackingPath, true);
        }

        return new EmptyResult();
    }

    private static ActionResult? ValidateSettingsRequest(SaveErrorLoggingServiceSettingsRequest request)
    {
        if (request.ReportsTtl <= 0)
            return new BadRequestObjectResult("Период хранения отчетов должен быть больше 0");

        if (request.Enabled && string.IsNullOrWhiteSpace(request.Message))
            return new BadRequestObjectResult("Не указано сообщение пользователю");

        return null;
    }

    private ReportRoot? DeserializeReport(string reportJson)
    {
        try
        {
            return JsonSerializer.Deserialize<ReportRoot>(reportJson, ErrorReportsHelper.ReportSerializerOptions);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка разбора отчета об ошибке");
            return null;
        }
    }

    private static byte[] GetHash(params string[] data)
    {
        var str = string.Join("", data);
        return MD5.HashData(Encoding.UTF8.GetBytes(str));
    }

    public sealed record SaveErrorLoggingServiceSettingsRequest(
        bool Enabled,
        string Message,
        int ReportsTtl);

    public sealed record ErrorLoggingServiceSettingsItem(
        bool Enabled,
        string Message,
        int ReportsTtl);

    public sealed record ErrorReportListItem(
        Guid Id,
        DateTime Date,
        string Configuration,
        string ConfigurationVersion,
        string PlatformVersion,
        string UserName,
        string AdditionalInfo);

    public sealed record ErrorReportDetailsItem(
        Guid Id,
        DateTime Date,
        string Configuration,
        string ConfigurationVersion,
        string CompatibilityMode,
        string PlatformVersion,
        string UserName,
        string DataSeparation,
        string AppName,
        string AppVersion,
        string OsVersion,
        long FreeRam,
        long FullRam,
        string AdditionalInfo,
        string ErrorText,
        string[] ErrorCategories,
        string Stack,
        string? ScreenshotBase64);
}
