using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.EntityFrameworkCore;

namespace OnecMonitor.Server.Models;

public class UpdateInfoBaseTask : DatabaseObject
{
    [MaxLength(150)]
    public string Description { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; } = DateTime.MinValue;
    
    public virtual List<V8Configuration> Configurations { get; set; } = [];
    public virtual List<InfoBase> InfoBases { get; set; } = [];
    public virtual List<UpdateInfoBaseTaskResult> Results { get; set; } = [];
}