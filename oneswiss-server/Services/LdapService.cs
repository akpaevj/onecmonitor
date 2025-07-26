using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
using Kerberos.NET.Crypto;
using Kerberos.NET.Entities.Pac;
using Microsoft.EntityFrameworkCore;
using OneSwiss.Server.Models;

namespace OneSwiss.Server.Services;

public class LdapService : IDisposable
{
    private readonly LdapConnection _connection;
    private readonly ILogger<LdapService> _logger;

    public LdapService(IDbContextFactory<AppDbContext> contextFactory, ILogger<LdapService> logger)
    {
        _logger = logger;
        
        using var context = contextFactory.CreateDbContext();
        var settings = context.LdapSettings.Include(c => c.Credentials).FirstOrDefault();
        _connection = CreateConnection(settings!);
        _connection.Bind();
    }
    
    public static LdapConnection CreateConnection(LdapSettings ldapSettings)
    {
        var connection = new LdapConnection(new LdapDirectoryIdentifier(ldapSettings.Server, 3268));
        connection.AutoBind = true;
        connection.SessionOptions.ProtocolVersion = 3;
        connection.AuthType = AuthType.Basic;
        connection.Credential = new NetworkCredential(ldapSettings.Credentials!.User,  ldapSettings.Credentials!.Password);

        return connection;
    }

    public List<LdapUser> SearchUsers(string searchTerm)
    {
        var users = new List<LdapUser>();
        
        try
        {
            var filter = $"(&(objectClass=user)(|(samAccountName=*{searchTerm}*)(displayName=*{searchTerm}*)))";
            
            var searchRequest = new SearchRequest(
                null,
                filter,
                SearchScope.Subtree,
                "uid", "objectSID", "cn", "samAccountName", "mail", "displayName", "givenName", "sn");

            var response = (SearchResponse)_connection.SendRequest(searchRequest, TimeSpan.FromSeconds(5));

            users.AddRange(from SearchResultEntry entry in response.Entries
            select new LdapUser
            {
                Sid = GetPropertyValue(entry, "objectSID") ?? GetPropertyValue(entry, "uid"),
                CommonName = GetPropertyValue(entry, "cn"),
                SamAccountName = GetPropertyValue(entry, "samAccountName"),
                Email = GetPropertyValue(entry, "mail"),
                DisplayName = GetPropertyValue(entry, "displayName"),
                FirstName = GetPropertyValue(entry, "givenName"),
                LastName = GetPropertyValue(entry, "sn")
            } );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при поиске пользователей в LDAP");
            throw;
        }

        return users.Where(c => !string.IsNullOrEmpty(c.DisplayName)).ToList();
    }
    
    private static string? GetPropertyValue(SearchResultEntry result, string propertyName)
    {
        if (!result.Attributes.Contains(propertyName)) 
            return "";
        
        var value = result.Attributes[propertyName]?[0];

        return propertyName.Equals("objectSID", StringComparison.InvariantCultureIgnoreCase) ? 
            ConvertSidToString(value as byte[]) : value?.ToString();
    }

    private static string ConvertSidToString(byte[]? sidBytes)
    {
        var str = new StringBuilder();
        str.Append("S-");
    
        try
        {
            // Получаем версию (должна быть 1)
            str.Append(sidBytes![0]);
            str.Append('-');
        
            // Получаем количество подавторитетов (последние 6 байт массива - authority)
            int count = sidBytes[1];
        
            // Получаем authority (big-endian 48-битное число)
            long authority = 0;
            for (var i = 2; i <= 7; i++)
            {
                authority <<= 8;
                authority += sidBytes[i];
            }
            str.Append(authority);
        
            // Получаем подавторитеты (каждый по 4 байта, little-endian)
            var offset = 8;
            for (var j = 0; j < count; j++)
            {
                var subAuthority = BitConverter.ToUInt32(sidBytes, offset);
                str.Append('-');
                str.Append(subAuthority);
                offset += 4;
            }
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid SID byte array", ex);
        }
    
        return str.ToString();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}