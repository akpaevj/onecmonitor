using System.ComponentModel.DataAnnotations;

namespace OneSwiss.V8.Platform.RemoteAdministration;

public enum V8ClusterLoadBalancingMode
{
    [Display(Name = "Приоритет по производительности")]
    Performance,
    [Display(Name = "Приоритет по памяти")]
    Memory
}