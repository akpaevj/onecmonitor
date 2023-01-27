using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using OnecMonitor.Server.Converters.Json;

namespace OnecMonitor.Server.Models
{
    public class TjEvent
    {
        [JsonProperty("id")]
        public Guid Id { get; set; } = Guid.NewGuid();
        [JsonProperty("start_date_time")]
        [JsonConverter(typeof(DateTimeToUtcDateTime64))]
        public DateTime StartDateTime => DateTime.AddMicroseconds(-Duration);
        [JsonProperty("date_time")]
        [JsonConverter(typeof(DateTimeToUtcDateTime64))]
        public DateTime DateTime { get; set; } = DateTime.MinValue;
        [JsonProperty("duration")]
        public long Duration { get; set; } = 0;
        [JsonProperty("event_name")]
        public string EventName { get; set; } = "";
        [JsonProperty("level")]
        public int Level { get; set; } = 0;
        [JsonProperty("session_id")]
        public string SessionId {
            get
            {
                if (Properties.TryGetValue("SessionId", out var val))
                    return val;
                else
                    return "";
            }
        }
        [JsonProperty("call_id")]
        public string CallId
        {
            get
            {
                if (Properties.TryGetValue("CallID", out var val))
                    return val;
                else
                    return "";
            }
        }
        [JsonProperty("t_client_id")]
        public int TClientId
        {
            get
            {
                if (Properties.TryGetValue("t:clientID", out var val) && int.TryParse(val, out var parsed))
                    return parsed;
                else
                    return 0;
            }
        }
        [JsonProperty("t_computer_name")]
        public string TComputerName
        {
            get
            {
                if (Properties.TryGetValue("t:computerName", out var val))
                    return val;
                else
                    return "";
            }
        }
        [JsonProperty("dst_client_id")]
        public int DstClientId
        {
            get
            {
                if (Properties.TryGetValue("DstClientID", out var val) && int.TryParse(val, out var parsed))
                    return parsed;
                else
                    return 0;
            }
        }
        [JsonProperty("usr")]
        public string Usr
        {
            get
            {
                if (Properties.TryGetValue("Usr", out var val))
                    return val;
                else
                    return string.Empty;
            }
        }
        [JsonProperty("t_connect_id")]
        public string TConnectId
        {
            get
            {
                if (Properties.TryGetValue("t:connectID", out var val))
                    return val;
                else
                    return "";
            }
        }
        [JsonProperty("p_process_name")]
        public string PProcessName
        {
            get
            {
                if (Properties.TryGetValue("p:processName", out var val))
                    return val;
                else
                    return "";
            }
        }
        [JsonProperty("i_name")]
        public string IName
        {
            get
            {
                if (Properties.TryGetValue("IName", out var val))
                    return val;
                else
                    return "";
            }
        }
        [JsonProperty("m_name")]
        public string MName
        {
            get
            {
                if (Properties.TryGetValue("MName", out var val))
                    return val;
                else
                    return "";
            }
        }
        [JsonProperty("wait_connections")]
        public int[] WaitConnections
        {
            get
            {
                if (Properties.TryGetValue("WaitConnections", out var val) && val.Length > 0)
                    return val.Split(',').Select(c => int.Parse(c.Trim())).ToArray();
                else
                    return Array.Empty<int>();
            }
        }
        [JsonProperty("locks")]
        public string[] Locks
        {
            get
            {
                if (Properties.TryGetValue("Locks", out var val) && val.Length > 0)
                    return val.Split(',').Select(c => c.Trim()).ToArray();
                else
                    return Array.Empty<string>();
            }
        }
        [JsonProperty("props")]
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        [JsonProperty("_agent_id")]
        public string AgentId { get; set; } = Guid.Empty.ToString();
        [JsonProperty("_seance_id")]
        public string SeanceId { get; set; } = Guid.Empty.ToString();
        [JsonProperty("_file")]
        public string File { get; set; } = string.Empty;
        [JsonProperty("_folder")]
        public string Folder { get; set; } = string.Empty;
        [JsonProperty("_end_position")]
        public long EndPosition { get; set; }
        [JsonProperty("_version")]
        [JsonConverter(typeof(DateTimeToUtcDateTime64))]
        public DateTime Version => DateTime.UtcNow;

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
