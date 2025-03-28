using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OnecMonitor.Common.DTO;
using OnecMonitor.Common.Services;

namespace OnecMonitor.Server.Helpers;

public class TechLogHelper
{
    public static void UpdateTechLogSettings(IMapper mapper, TechLogRepositoryManager manager, AppDbContext dbContext)
    {
        var settings = dbContext.TechLogSettings
            .Include(c => c.Credentials)
            .Include(c => c.Dbms)
            .FirstOrDefault();
        
        var settingsDto = settings == null ? new TechLogSettingsDto() : mapper.Map<TechLogSettingsDto>(settings);
        manager.SetSettings(settingsDto);
    }
}