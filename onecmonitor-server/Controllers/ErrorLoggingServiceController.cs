using System.IO.Compression;
using System.Text;
using System.Text.Json;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Server.Dto.ErrorLoggingService;
using OnecMonitor.Server.Models;
using OnecMonitor.Server.ViewModels.ErrorLoggingService;
using OnecMonitor.Server.ViewModels.TechLogSettings;

namespace OnecMonitor.Server.Controllers;

public class ErrorLoggingServiceController(
    AppDbContext dbContext, 
    IMapper mapper, 
    ILogger<ErrorLoggingServiceController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Settings(CancellationToken cancellationToken)
    {
        var settings = await dbContext.ErrorLoggingServiceSettings
            .ProjectTo<ErrorLoggingServiceSettingsViewModel>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        
        return View(settings);
    }

    [HttpPost]
    public async Task<IActionResult> SaveSettings()
    {
        throw new NotImplementedException();
    }
    
    #region API
    
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters =
        {
            new ReportStackItemConverter(),
            new ReportExtensionConverter(),
            new ReportErrorConverter()
        }
    };
    
    [HttpPost("getInfo")]
    public IActionResult GetInfo([FromBody]GetInfoRequest request)
    {
        var content = JsonSerializer.Serialize(new GetInfoResponse
        {
            NeedSendReport = true,
            UserMessage = "Ошибка будет автоматически отправлена в отдел автоматизации учета",
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
            var report = JsonSerializer.Deserialize<ReportRoot>(reportData, _jsonOptions);
        
            if (report == null)
                logger.LogError($"Ошибка разбора отчета об ошибке:\n {reportData}");
            else
            {
                var model = new ErrorReport
                {
                    Id = Guid.NewGuid(),
                    Date = report.Time,
                    ServerVersion = report.ServerInfo.AppVersion,
                    Configuration = report.ConfigInfo.Name,
                    ConfigurationVersion = report.ConfigInfo.Version,
                    UserName = report.SessionInfo.UserName,
                    Body = reportData
                };

                if (report.Screenshot?.File != null)
                {
                    var screenshotPath = Path.Combine(unpackingPath, report.Screenshot.File);
                    model.Screenshot = await System.IO.File.ReadAllBytesAsync(screenshotPath, cancellationToken);
                }
        
                dbContext.ErrorReports.Add(model);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка обработки отчета об ошибке");
        }

        return new EmptyResult();
    }

    #endregion
}