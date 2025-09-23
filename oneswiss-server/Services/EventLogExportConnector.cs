using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Helpers;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.Services;

public class EventLogExportConnector(
    IDbContextFactory<AppDbContext> contextFactory,
    ILogger<EventLogExportConnector> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var context = await contextFactory.CreateDbContextAsync(stoppingToken);

                var currentSettings = await context.EventLogSettings
                    .AsNoTracking()
                    .SingleOrDefaultAsync(stoppingToken);
                
                var currentItems = await context.EventLogExportItems.ToListAsync(stoppingToken);

                if (currentSettings is { Enabled: true })
                {
                    var infoBases = await context.InfoBases
                        .AsNoTracking()
                        .ToListAsync(stoppingToken);
                    
                    var newItems = new List<EventLogExportItem>();
                    var toConnectInfoBases = infoBases.Where(c => Regex.IsMatch(c.InfoBaseName, currentSettings.InfoBaseNameRegex)).ToList();
                    
                    foreach (var connectInfoBase in toConnectInfoBases)
                    {
                        var existItem = currentItems.FirstOrDefault(c => c.InfoBaseId ==  connectInfoBase.Id);
                        
                        if (existItem == null)
                        {
                            var item = new EventLogExportItem
                            {
                                IsActive = true,
                                Ttl = currentSettings.DefaultTtl,
                                InfoBaseId = connectInfoBase.Id
                            };
                            newItems.Add(item);
                        }
                        else
                            newItems.Add(existItem);
                    }
                    
                    ModelHelper.UpdateDbSet(newItems, context);
                }

                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Ошибка подключения информационных баз к экспорту");
            }
            
            await Task.Delay(10 * 1000, stoppingToken);
        }
    }
}