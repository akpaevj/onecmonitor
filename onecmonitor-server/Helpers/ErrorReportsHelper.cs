using System.Text.Json;
using OnecMonitor.Server.Dto.ErrorLoggingService;

namespace OnecMonitor.Server.Helpers;

public abstract class ErrorReportsHelper
{
    public static JsonSerializerOptions ReportSerializerOptions { get; } = new()
    {
        Converters =
        {
            new ReportStackItemConverter(),
            new ReportExtensionConverter(),
            new ReportErrorConverter()
        }
    };
}