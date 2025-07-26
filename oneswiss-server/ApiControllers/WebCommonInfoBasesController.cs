using System.Text;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Dto.WebCommonInfoBases;
using OneSwiss.Server.Hubs;
using OneSwiss.Server.Services;
using Org.BouncyCastle.Cms;

namespace OneSwiss.Server.ApiControllers;

[ApiController]
[Route("api/[controller]")]
public class WebCommonInfoBasesController(
    AppDbContext dbContext,
    LdapService ldapService,
    ILogger<WebCommonInfoBasesController> logger) : ControllerBase
{
    [HttpHead]
    public IActionResult Cap() 
        => Ok();
    
    [HttpGet("CheckInfoBases")]
    public IActionResult CheckInfoBases(
        [ValidateNever] string clientId, 
        [ValidateNever] string infoBasesCheckCode)
    {
        if (!Guid.TryParse(clientId, out var clientIdGuid) ||
            !Guid.TryParse(infoBasesCheckCode, out var infoBasesCheckCodeGuid))
            return BadRequest();

        // Если ID пустой, то сразу отправляем на обновление
        if (clientIdGuid == Guid.Empty)
            return Ok(new WebCommonInfoBaseResponse<CheckInfoBasesResponse>(new CheckInfoBasesResponse
                { InfoBasesChanged = true }));
        
        var onecClient = dbContext.OnecClients
            .AsNoTracking()
            .Include(c => c.InfoBasesList)
            .FirstOrDefault(x => x.Id == clientIdGuid);
        
        // Если модель клиента по ID не найдена или ID списка отличается от текущего у модели, то тоже на обновление
        if (onecClient == null || onecClient.InfoBasesList?.ListId != infoBasesCheckCodeGuid.ToString())
            return Ok(new WebCommonInfoBaseResponse<CheckInfoBasesResponse>(new CheckInfoBasesResponse
                { InfoBasesChanged = true }));
        
        return Ok(WebCommonInfoBaseResponse<CheckInfoBasesResponse>.CreateResponse(data =>
        {
            data.InfoBasesChanged = false;
            data.Url = string.Empty;
        }));
    }

    [Authorize(Policy = "WebCommonInfoBases")]
    [HttpGet("GetInfoBases")]
    public async Task<IActionResult> GetInfoBases(string clientId, string infoBasesCheckCode)
    {
        if (!Guid.TryParse(clientId, out var clientIdGuid) ||
            !Guid.TryParse(infoBasesCheckCode, out var infoBasesCheckCodeGuid))
            return BadRequest();

        var infoBases = string.Empty;

        if (clientIdGuid == Guid.Empty)
        {
            var userName = Request.HttpContext.User.Identity?.Name;
            
            if (userName != null)
            {
                var foundUsers = ldapService.SearchUsers(userName);
                
                // Если пользователь не найден или найдено больше одного, то хз, кто это вообще. Отправим пустую информацию по спискам
                if (foundUsers.Count == 1)
                {
                    var foundUser = foundUsers[0];
                    
                    var onecClient = await dbContext.OnecClients
                        .AsNoTracking()
                        .Include(c => c.InfoBasesList).ThenInclude(c => c.InfoBases)
                        .FirstOrDefaultAsync(c => c.InternalId == foundUser.Sid);
                    
                    if (onecClient != null)
                    {
                        var builder = new StringBuilder();

                        foreach (var infoBase in onecClient.InfoBasesList?.InfoBases ?? [])
                            builder.AppendLine(infoBase.IBasesContent);
                        
                        infoBases = builder.ToString();
                    }
                }
            }
        }
        
        return Ok(WebCommonInfoBaseResponse<GetInfoBasesResponse>.CreateResponse(data =>
        {
            data.ClientId = clientIdGuid.ToString();
            data.InfoBases = infoBases;
            data.InfoBasesCheckCode = string.Empty;
        }));
    }
}