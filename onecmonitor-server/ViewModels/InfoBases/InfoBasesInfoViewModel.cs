using System.ComponentModel;

namespace OnecMonitor.Server.ViewModels.InfoBases;

[DisplayName("Информация об информационных базах")]
public class InfoBasesInfoViewModel
{
    [DisplayName("Строка поиска")]
    public string SearchString { get; set; } = string.Empty;
    [DisplayName("Информационные базы")]
    public List<InfoBaseViewModel> InfoBases { get; set; } = [];
}