using System.Globalization;
using System.Text;
using NodaTime;
using OneSTools.BracketsFile;
using OneSwiss.Agent.Extensions;
using OneSwiss.Common.EventLog;
using OneSwiss.V8.Platform.Brackets;
using BracketsParser = OneSwiss.V8.Platform.Brackets.BracketsParser;

namespace OneSwiss.Agent.Services.EventLog.BracketsReader;

internal class LgpReader : IDisposable
{
    private readonly LgfDataProvider _lgfDataProvider;
    
    private readonly StreamReader _streamReader;
    private readonly IEnumerable<BracketValue> _bracketsStream;

    public LgpReader(string lgpPath, LgfDataProvider lgfDataProvider)
    {
        _streamReader = new StreamReader(lgpPath);
        SkipSignature();
        
        var parser = new BracketsParser();
        _bracketsStream = parser.ParseStream(_streamReader);

        _lgfDataProvider = lgfDataProvider;
    }

    private void SkipSignature()
    {
        _streamReader.ReadLine();
        _streamReader.ReadLine();
        _streamReader.ReadLine();
    }

    public IEnumerable<EventLogItem> ReadStream(InfoBaseInfo infoBaseInfo, DateTime lastEventDateTime, CancellationToken cancellationToken)
    {
        foreach (var bracketValue in _bracketsStream)
        {
            var values = bracketValue.ObjectValue;

            var eventDate = DateTime.ParseExact(values[0].StringValue, "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture);
            eventDate = DateTime.SpecifyKind(eventDate, DateTimeKind.Local);
            
            if (eventDate <= lastEventDateTime)
                continue;
            
            var item = new EventLogItem
            {
                Id = Guid.NewGuid(),
                InfoBaseId = infoBaseInfo.InfoBaseId,
                InfoBaseName = infoBaseInfo.Name,
                Date = eventDate,
                TransactionStatus = values[1].StringValue
            };

            var transactionInfo = values[2].ObjectValue;
            // ReSharper disable once PossibleLossOfFraction
            item.TransactionDateTime = new DateTime().AddSeconds(Convert.ToInt64(transactionInfo[0].StringValue, 16) / 10000);
            item.TransactionID = Convert.ToInt64(transactionInfo[1].StringValue, 16);

            var userInfo = _lgfDataProvider.GetReferencedObjectValue(ObjectType.Users, int.Parse(values[3].StringValue),
                cancellationToken);
            item.User = userInfo.Reference;
            item.UserName = userInfo.ObjectValue;
            
            item.Computer = _lgfDataProvider.GetObjectValue(ObjectType.Computers, int.Parse(values[4].StringValue),
                cancellationToken);
            
            item.ApplicationName = _lgfDataProvider.GetObjectValue(ObjectType.Applications, int.Parse(values[5].StringValue),
                cancellationToken);
            item.Connection = values[6].StringValue;
            item.Event = _lgfDataProvider.GetObjectValue(ObjectType.Events, int.Parse(values[7].StringValue),
                cancellationToken);
            item.Level = values[8].StringValue;
            item.Comment = values[9].StringValue;
            
            var metadataInfo = _lgfDataProvider.GetReferencedObjectValue(ObjectType.Metadata, int.Parse(values[10].StringValue),
                cancellationToken);
            item.Metadata = metadataInfo.Reference;
            item.MetadataPresentation = metadataInfo.ObjectValue;
            item.Data = values[11].StringValue;
            item.DataPresentation = values[12].StringValue;
            item.ServerName = _lgfDataProvider.GetObjectValue(ObjectType.Servers, int.Parse(values[13].StringValue),
                cancellationToken);
            
            var portStr = _lgfDataProvider.GetObjectValue(ObjectType.Ports, int.Parse(values[14].StringValue),
                cancellationToken);
            if (int.TryParse(portStr, out var port))
                item.Port = port;
            
            var syncStr = _lgfDataProvider.GetObjectValue(ObjectType.SyncPorts, int.Parse(values[15].StringValue),
                cancellationToken);
            if (int.TryParse(syncStr, out var syncPort))
                item.SyncPort = syncPort;
            
            item.Session =  values[16].StringValue;

            yield return item;
        }
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
            _streamReader.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~LgpReader()
    {
        Dispose(false);
    }
}