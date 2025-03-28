using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using OnecMonitor.Common.Models;

namespace OnecMonitor.Server.ViewModels.Dbms;

public class DbmsEditViewModel
{
    public Guid Id { get; init; }
    public string Name { get; set; } = string.Empty;
    public DbmsType Type { get; set; }
    [ValidateNever] 
    public SelectList Types { get; set; } = null!;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
}