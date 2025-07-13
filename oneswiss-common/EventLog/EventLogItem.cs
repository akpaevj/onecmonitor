using System.Text.Json;

namespace OneSwiss.Common.EventLog;

public record EventLogItem
{
    public string InfoBaseName { get; set; }
    public string Level { get; set; }
    public string Date { get; set; }
    public string ApplicationName { get; set; }
    public string ApplicationPresentation { get; set; }
    public string Event { get; set; }
    public string EventPresentation { get; set; }
    public string User { get; set; }
    public string UserName { get; set; }
    public string Computer { get; set; }
    public JsonDocument Metadata { get; set; }
    public JsonDocument MetadataPresentation { get; set; }
    public string Comment { get; set; }
    public JsonDocument Data { get; set; }
    public JsonDocument DataPresentation { get; set; }
    public string TransactionStatus { get; set; }
    public string TransactionID { get; set; }
    public string Connection { get; set; }
    public string Session { get; set; }
    public string ServerName { get; set; }
    public string Port { get; set; }
    public string SyncPort { get; set; }
    public JsonDocument SessionDataSeparation { get; set; }
    public JsonDocument SessionDataSeparationPresentation { get; set; }
}