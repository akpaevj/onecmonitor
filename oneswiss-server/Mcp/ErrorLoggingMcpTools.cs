using System.ComponentModel;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using OneSwiss.Server.Dto.ErrorLoggingService;
using OneSwiss.Server.Helpers;

namespace OneSwiss.Server.Mcp;

[McpServerToolType]
public sealed class ErrorLoggingMcpTools(AppDbContext dbContext, ILogger<ErrorLoggingMcpTools> logger)
{
    [McpServerTool(Name = "error_logging_get_settings", ReadOnly = true)]
    [Description("Настройки сервиса регистрации ошибок 1С: включен ли прием отчетов от информационных баз, " +
                  "сообщение пользователю при возникновении ошибки, срок хранения отчетов в днях.")]
    [Authorize]
    public async Task<ErrorLoggingSettingsResult> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await dbContext.ErrorLoggingServiceSettings
            .AsNoTracking()
            .OrderBy(c => c.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return new ErrorLoggingSettingsResult(
            settings?.Enabled ?? false,
            settings?.Message ?? string.Empty,
            settings?.ReportsTtl ?? 30);
    }

    [McpServerTool(Name = "error_logging_list_report_groups", ReadOnly = true)]
    [Description("Список групп отчетов об ошибках 1С, сгруппированных по хэшу стека вызовов. Показывает, какие " +
                  "ошибки встречаются чаще всего: количество вхождений, даты первого и последнего появления, " +
                  "конфигурацию, версию платформы и текст ошибки. Используй для обзора проблем перед просмотром " +
                  "деталей конкретных отчетов через error_logging_list_reports с фильтром по hash.")]
    [Authorize(Roles = Roles.ReadErrorLoggingReports)]
    public async Task<IReadOnlyList<ErrorReportGroupResult>> ListReportGroups(CancellationToken cancellationToken)
    {
        var summaries = await dbContext.ErrorReports
            .AsNoTracking()
            .Select(c => new { c.Id, c.CreatedAt, c.Hash })
            .ToListAsync(cancellationToken);

        var groups = summaries
            .GroupBy(c => Convert.ToHexString(c.Hash))
            .Select(g =>
            {
                var latest = g.OrderByDescending(c => c.CreatedAt).First();
                return new
                {
                    HashHex = g.Key,
                    Count = g.Count(),
                    FirstSeen = g.Min(c => c.CreatedAt),
                    LastSeen = latest.CreatedAt,
                    LastId = latest.Id
                };
            })
            .ToList();

        var lastIds = groups.Select(g => g.LastId).ToList();
        var lastReports = await dbContext.ErrorReports
            .AsNoTracking()
            .Where(c => lastIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Report })
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var result = new List<ErrorReportGroupResult>(groups.Count);

        foreach (var g in groups)
        {
            if (!lastReports.TryGetValue(g.LastId, out var entity))
                continue;

            var report = DeserializeReport(entity.Report);
            if (report == null)
                continue;

            var error = report.ErrorInfo.ApplicationErrorInfo.Errors.FirstOrDefault();

            result.Add(new ErrorReportGroupResult(
                g.HashHex,
                g.Count,
                g.FirstSeen,
                g.LastSeen,
                report.ConfigInfo.Name,
                report.ConfigInfo.Version,
                report.ServerInfo.AppVersion,
                report.AdditionalInfo ?? string.Empty,
                error?.Text ?? string.Empty));
        }

        return result.OrderByDescending(c => c.LastSeen).ToList();
    }

    [McpServerTool(Name = "error_logging_list_reports", ReadOnly = true)]
    [Description("Список отдельных отчетов об ошибках 1С с возможностью фильтрации по хэшу группы " +
                  "(см. error_logging_list_report_groups). Без фильтра возвращает все отчеты.")]
    [Authorize(Roles = Roles.ReadErrorLoggingReports)]
    public async Task<IReadOnlyList<ErrorReportListResult>> ListReports(
        [Description("Хэш группы отчетов (шестнадцатеричная строка) для фильтрации, полученный из " +
                      "error_logging_list_report_groups. Необязателен.")]
        string? hash = null,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ErrorReports.AsNoTracking();

        if (!string.IsNullOrEmpty(hash))
        {
            byte[] hashBytes;
            try
            {
                hashBytes = Convert.FromHexString(hash);
            }
            catch (FormatException)
            {
                throw new McpException("Некорректный идентификатор группы");
            }

            query = query.Where(c => c.Hash == hashBytes);
        }

        var entities = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new { c.Id, c.Report })
            .ToListAsync(cancellationToken);

        var result = new List<ErrorReportListResult>(entities.Count);

        foreach (var entity in entities)
        {
            var report = DeserializeReport(entity.Report);
            if (report == null)
                continue;

            result.Add(new ErrorReportListResult(
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

    [McpServerTool(Name = "error_logging_get_report_details", ReadOnly = true)]
    [Description("Подробная информация об одном отчете об ошибке 1С: клиент, сервер, конфигурация, текст ошибки, " +
                  "категории и стек вызовов. Идентификатор отчета берется из error_logging_list_reports. " +
                  "Скриншот в ответ не включается - доступен только признак его наличия.")]
    [Authorize(Roles = Roles.ReadErrorLoggingReports)]
    public async Task<ErrorReportDetailsResult> GetReportDetails(
        [Description("Идентификатор отчета об ошибке (GUID)")]
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.ErrorReports
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (entity == null)
            throw new McpException("Отчет об ошибке не найден");

        var report = DeserializeReport(entity.Report);
        if (report == null)
            throw new McpException("Не удалось разобрать содержимое отчета");

        var error = report.ErrorInfo.ApplicationErrorInfo.Errors.FirstOrDefault();

        return new ErrorReportDetailsResult(
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
            entity.Screenshot?.Length > 0);
    }

    private ReportRoot? DeserializeReport(string reportJson)
    {
        try
        {
            return JsonSerializer.Deserialize<ReportRoot>(reportJson, ErrorReportsHelper.ReportSerializerOptions);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Ошибка разбора отчета об ошибке (MCP)");
            return null;
        }
    }
}

public sealed record ErrorLoggingSettingsResult(bool Enabled, string Message, int ReportsTtlDays);

public sealed record ErrorReportGroupResult(
    string Hash,
    int Count,
    DateTime FirstSeen,
    DateTime LastSeen,
    string Configuration,
    string ConfigurationVersion,
    string PlatformVersion,
    string AdditionalInfo,
    string ErrorText);

public sealed record ErrorReportListResult(
    Guid Id,
    DateTime Date,
    string Configuration,
    string ConfigurationVersion,
    string PlatformVersion,
    string UserName,
    string AdditionalInfo);

public sealed record ErrorReportDetailsResult(
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
    bool HasScreenshot);
