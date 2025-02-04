using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Renci.SshNet;

namespace OneSTools.Common.Designer.Agent;

internal static class ShellStreamExtensions
{
    internal static async Task WaitDataAvailable(this ShellStream stream)
    {
        while (!stream.DataAvailable)
            await Task.Delay(100);
    }

    internal static async Task<DesignerAgentMessage[]> WriteCommand(this ShellStream stream, string command)
    {
        stream.WriteLine(command);
        await stream.WaitDataAvailable();

        var data = stream.Read();

        var response = JsonSerializer.Deserialize<DesignerAgentMessage[]>(data);
        
        if (response is null)
            throw new Exception("Failed to deserialize designer agent response");

        if (response.First().Type == "error")
            throw new Exception(response.First().Message);
        
        return response;
    }
}