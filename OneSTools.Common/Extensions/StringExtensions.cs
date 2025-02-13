using System.Net;

namespace OneSTools.Common.Extensions;

public static class StringExtensions
{
    public static bool IsLocalHost(this string str)
    {
        try
        {
            var hostIps = Dns.GetHostAddresses(str);
            var localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            
            foreach (var hostIp in hostIps)
            {
                if (IPAddress.IsLoopback(hostIp)) 
                    return true;

                if (localIPs.Contains(hostIp))
                    return true;
            }
        }
        catch
        {
            // ignored
        }

        return false;
    }
}