using System.Text.Json;
using System.Threading.Channels;

namespace OneSwiss.V8.Designer.Agent;

public static class ChannelExtensions
{
    private static async Task<DesignerAgentMessage> ReadNextMessage(this Channel<DesignerAgentMessage> channel)
    {
        return await channel.Reader.ReadAsync();
    }

    public static async Task<DesignerAgentMessage> EnsureNextNotError(this Channel<DesignerAgentMessage> channel)
    {
        var next = await channel.ReadNextMessage();

        if (next.Type == "error")
            throw new Exception(next.Message);

        return next;
    }

    private static async Task<DesignerAgentMessage> EnsureNextType(this Channel<DesignerAgentMessage> channel,
        string type)
    {
        var next = await channel.EnsureNextNotError();

        if (next.Type != type)
            throw new Exception($"Неожиданный тип сообщения \"{next.Type}\" от агента конфигуратора");

        return next;
    }

    public static async Task<DesignerAgentMessage> EnsureNextSuccess(this Channel<DesignerAgentMessage> channel)
    {
        return await channel.EnsureNextType("success");
    }

    public static async Task<T> ReadNextMessage<T>(this Channel<DesignerAgentMessage> channel)
    {
        var next = await channel.EnsureNextSuccess();

        return JsonSerializer.Deserialize<T>(next.Body.RootElement.ToString())!;
    }

    public static async Task<List<DesignerAgentMessage>> ReadTillSuccess(this Channel<DesignerAgentMessage> channel)
    {
        var messages = new List<DesignerAgentMessage>();

        while (true)
        {
            var next = await channel.EnsureNextNotError();

            if (next.Type == "success")
                break;

            messages.Add(next);
        }

        return messages;
    }
}