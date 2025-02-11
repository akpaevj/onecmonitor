using Microsoft.EntityFrameworkCore;

namespace OnecMonitor.Server.Models;

public class V8Configuration : DatabaseObject
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string DataPath { get; set; }  = string.Empty;
    public bool IsUpdate { get; set; } = false;
    public bool IsExtension { get; set; } = false;
    public bool IsConfiguration { get; set; } = false;
    
    public virtual List<UpdateInfoBaseTask> UpdateTasks { get; set; } = [];

    public override string ToString()
    {
        var postfix = "";
        
        if (IsUpdate)
            postfix = "обновление";
        else if (IsExtension)
            postfix = "расширение";
        else if (IsConfiguration)
            postfix = "конфигурация";
        
        return $"{Name} ({Version}, {postfix})";
    }
}