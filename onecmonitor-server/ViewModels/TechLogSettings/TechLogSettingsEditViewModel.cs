using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OnecMonitor.Server.ViewModels.TechLogSettings;

public class TechLogSettingsEditViewModel
{
    public Guid Id { get; set; }
    public bool Enabled { get; set; } = false;
    public string ClickHouseHost { get; set; } = string.Empty;
    public int ClickHousePort { get; set; } = 8123;
    public string ClickHouseDatabase { get; set; } = "onecmonitor_techlog";
    public string ClickHouseUser { get; set; } = "default";
    [ValidateNever]
    public string ClickHousePassword { get; set; } = string.Empty;
}