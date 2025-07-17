using System.IO.Compression;
using System.Text;
using System.Text.Json;
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
    NotificationsService  notificationsService,
    ILogger<ErrorLoggingServiceController> logger) : ControllerBase
{
    [HttpPost("getInfo")]
    public IActionResult GetInfo([FromBody]GetInfoRequest request)
    {
        var settings = dbContext.ErrorLoggingServiceSettings.AsNoTracking().FirstOrDefault();
        var needSend = settings?.Enabled ?? false;
        
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

        try
        {
            var unpackingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            
            var file = Request.Form.Files[0];
            await using var stream = file.OpenReadStream();
            ZipFile.ExtractToDirectory(stream, unpackingPath);

            var reportPath = Path.Combine(unpackingPath, "report.json");
            var reportData = await System.IO.File.ReadAllTextAsync(reportPath, cancellationToken);
            var report = JsonSerializer.Deserialize<ReportRoot>(reportData, ErrorReportsHelper.ReportSerializerOptions);
        
            if (report == null)
                logger.LogError($"Ошибка разбора отчета об ошибке:\n {reportData}");
            else
            {
                var model = new ErrorReport
                {
                    Id = Guid.NewGuid(),
                    CreatedAt = DateTime.Now,
                    Report = reportData
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

        return new EmptyResult();
    }
}