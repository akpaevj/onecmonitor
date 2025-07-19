using System.ComponentModel.DataAnnotations;

namespace OneSwiss.Server.Models;

public enum TjEventAnalyzeType
{
    [Display(Name = "Граф взаимоблокировки")]
    DeadlockOnManagedLocksGraph
}