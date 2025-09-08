namespace OneSwiss.Server.Dto.ErrorLoggingService;

public class ReportStackItem
{
    public string Module { get; set; }
    public long Line { get; set; }
    public string Code { get; set; }
}