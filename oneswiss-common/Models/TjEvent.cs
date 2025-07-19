using System.Text.Json.Serialization;
using Newtonsoft.Json;
using OneSwiss.Common.Converters.Json;

namespace OneSwiss.Common.Models
{
    public class TjEvent
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonPropertyName("start_date_time")]
        public DateTime StartDateTime => DateTime.AddMicroseconds(-Duration);
        [JsonPropertyName("date_time")]
        public DateTime DateTime { get; set; } = DateTime.MinValue;
        [JsonPropertyName("duration")]
        public long Duration { get; set; } = 0;
        [JsonPropertyName("name")]
        public string EventName { get; set; } = "";
        [JsonPropertyName("level")]
        public int Level { get; set; } = 0;
        [JsonPropertyName("session_id")]
        public string SessionId => Properties.GetValueOrDefault("SessionID", "");

        [JsonPropertyName("call_id")]
        public string CallId => Properties.GetValueOrDefault("CallID", "");

        [JsonPropertyName("t_client_id")]
        public int TClientId
        {
            get
            {
                if (Properties.TryGetValue("t:clientID", out var val) && int.TryParse(val, out var parsed))
                    return parsed;
                return 0;
            }
        }
        [JsonPropertyName("t_computer_name")]
        public string TComputerName => Properties.GetValueOrDefault("t:computerName", "");

        [JsonPropertyName("dst_client_id")]
        public int DstClientId
        {
            get
            {
                if (Properties.TryGetValue("DstClientID", out var val) && int.TryParse(val, out var parsed))
                    return parsed;
                return 0;
            }
        }
        [JsonPropertyName("usr")]
        public string Usr => Properties.TryGetValue("Usr", out var val) ? val : string.Empty;

        [JsonPropertyName("t_connect_id")]
        public string TConnectId => Properties.GetValueOrDefault("t:connectID", "");

        [JsonPropertyName("p_process_name")]
        public string PProcessName => Properties.GetValueOrDefault("p:processName", "");

        [JsonPropertyName("i_name")]
        public string IName => Properties.GetValueOrDefault("IName", "");

        [JsonPropertyName("m_name")]
        public string MName => Properties.GetValueOrDefault("MName", "");

        [JsonPropertyName("wait_connections")]
        public int[] WaitConnections
        {
            get
            {
                if (Properties.TryGetValue("WaitConnections", out var val) && val.Length > 0)
                    return val.Split(',').Select(c => int.Parse(c.Trim())).ToArray();
                return [];
            }
        }
        [JsonPropertyName("locks")]
        public string[] Locks
        {
            get
            {
                if (Properties.TryGetValue("Locks", out var val) && val.Length > 0)
                    return val.Split(',').Select(c => c.Trim()).ToArray();
                return [];
            }
        }
        [JsonPropertyName("props")]
        public Dictionary<string, string> Properties { get; set; } = [];
        
        [JsonPropertyName("_agent_id")]
        public Guid AgentId { get; set; } = Guid.Empty;
        [JsonPropertyName("_seance_id")]
        public Guid SeanceId { get; set; } = Guid.Empty;
        [JsonPropertyName("_template_id")]
        public Guid TemplateId { get; set; } = Guid.Empty;
        [JsonPropertyName("_fileName")]
        public string FileName { get; set; } = string.Empty;
        [JsonPropertyName("_end_position")]
        public long EndPosition { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is TjEvent @event &&
                   Id.Equals(@event.Id);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public override string ToString()
            => EventName;
    }
}
