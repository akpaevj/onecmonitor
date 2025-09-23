namespace OneSwiss.Server.Dto.ErrorLoggingService;

public class ReportStackItem
{
    public string Module { get; set; } = string.Empty;
    public long Line { get; set; }
    public string Code { get; set; } = string.Empty;
}