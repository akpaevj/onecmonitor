using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using OneSTools.BracketsFile;
using OneSwiss.V8.Platform.Services;

namespace OneSwiss.Agent.Services.EventLog;

public class ClstWatcher : IDisposable
{
    private readonly RagentService _ragent;
    private readonly string _clstPath;
    private readonly Regex _regex;
    private readonly FileSystemWatcher _clstWatcher;
    private ConcurrentDictionary<string, InfoBaseInfo> _infoBases = [];
    
    public event EventHandler<InfoBaseInfo>? InfoBasesAdded;
    public event EventHandler<InfoBaseInfo>? InfoBasesDeleted;

    public ClstWatcher(RagentService ragent, string regex)
    {
        _ragent = ragent;
        _clstPath = Path.Combine(_ragent.ClusterCatalog, "1CV8Clst.lst");
        _regex = new Regex(regex, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        
        _clstWatcher = new FileSystemWatcher(_ragent.ClusterCatalog, "1CV8Clst.lst")
        {
            NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite
        };
    }

    public void Watch()
    {
        if (!File.Exists(_clstPath))
            throw new Exception("Couldn't find LST \"1CV8Clst.lst\" file");

        ReadInfoBasesAndRaiseEvents();
        
        _clstWatcher.Changed += ClstWatcher_Changed;
        _clstWatcher.EnableRaisingEvents = true;
    }

    private Dictionary<string, InfoBaseInfo> ReadInfoBases()
    {
        var items = new Dictionary<string, InfoBaseInfo>();
        
        using var stream = new FileStream(_clstPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream);

        var fileData = reader.ReadToEnd();
        var parsedData = BracketsParser.ParseBlock(fileData);

        var infoBasesNode = parsedData[2];
        int count = infoBasesNode[0];

        if (count <= 0) 
            return items;
        
        for (var i = 1; i <= count; i++)
        {
            var infoBaseNode = infoBasesNode[i];

            var elPath = Path.Combine(_ragent.ClusterCatalog, infoBaseNode[0], "1Cv8Log");
            string name = infoBaseNode[5];
                
            if (_regex.IsMatch(name))
                items.Add(elPath, new InfoBaseInfo(_ragent.Platform, elPath, name));
        }

        return items;
    }

    private void ReadInfoBasesAndRaiseEvents()
    {
        var newInfoBases = ReadInfoBases();

        var added = newInfoBases.Except(_infoBases);
        foreach (var (_, infoBaseInfo) in added)
            InfoBasesAdded?.Invoke(this, infoBaseInfo);

        var deleted = _infoBases.Except(newInfoBases);
        foreach (var (_, infoBaseInfo) in deleted)
            InfoBasesDeleted?.Invoke(this, infoBaseInfo);

        _infoBases.Clear();
        _infoBases = new ConcurrentDictionary<string, InfoBaseInfo>(_infoBases);
    }

    private void ClstWatcher_Changed(object sender, FileSystemEventArgs e)
        => ReadInfoBasesAndRaiseEvents();
    
    public void Dispose()
    {
        _clstWatcher.Dispose();
        GC.SuppressFinalize(this);
    }
}