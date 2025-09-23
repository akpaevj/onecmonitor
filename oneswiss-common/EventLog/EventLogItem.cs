using System.Text.Json;

namespace OneSwiss.Common.EventLog;

public class EventLogItem
{
    public Guid Id { get; set; }
    public DateTime TtlDate { get; set; }
    public string InfoBaseId { get; set; }
    public string InfoBaseName { get; set; }
    public string Level { get; set; }
    public DateTime Date { get; set; }
    public string ApplicationName { get; set; }
    public string Event { get; set; }
    public string User { get; set; }
    public string UserName { get; set; }
    public string Computer { get; set; }
    public string Metadata { get; set; }
    public string MetadataPresentation { get; set; }
    public string Comment { get; set; }
    public string Data { get; set; }
    public string DataPresentation { get; set; }
    public string TransactionStatus { get; set; }
    public DateTime TransactionDateTime { get; set; }
    public long TransactionID { get; set; }
    public string Connection { get; set; }
    public string Session { get; set; }
    public string ServerName { get; set; }
    public int Port { get; set; }
    public int SyncPort { get; set; }
    public string SessionDataSeparation { get; set; }
    public string SessionDataSeparationPresentation { get; set; }
}