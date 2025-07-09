using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace OneSwiss.V8.Platform.Services;

public enum RagentDebugType
{
    [Display(Name = "Отключена")]
    None,
    [Display(Name = "TCP")]
    Tcp,
    [Display(Name = "HTTP")]
    Http
}