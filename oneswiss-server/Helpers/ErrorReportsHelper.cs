using System.Text.Json;
using OneSwiss.Server.Dto.ErrorLoggingService;

namespace OneSwiss.Server.Helpers;

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