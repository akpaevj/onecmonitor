using Microsoft.EntityFrameworkCore;

namespace OnecMonitor.Server.Models;

public class V8File : DatabaseObject
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string DataPath { get; set; }  = string.Empty;
    public bool IsUpdate { get; set; } = false;
    public bool IsExtension { get; set; } = false;
    public bool IsConfiguration { get; set; } = false;
    public bool IsExternalDataProcessor { get; set; } = false;

    public override string ToString()
    {
        var postfix = "";
        
        if (IsUpdate)
            postfix = "обновление";
        else if (IsExtension)
            postfix = "расширение";
        else if (IsConfiguration)
            postfix = "конфигурация";
        else if (IsExternalDataProcessor)
            postfix = "внешняя обработка";
        
        return $"{Name} ({Version}, {postfix})";
    }
}