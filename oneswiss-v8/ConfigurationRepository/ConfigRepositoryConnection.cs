using OneSTools.FileDatabase;
using OneSTools.FileDatabase.Extensions;
using OneSwiss.V8.ConfigurationRepository.Models;

namespace OneSwiss.V8.ConfigurationRepository;

public class ConfigRepositoryConnection(string path) : IDisposable
{
    private readonly string _dbpath = Path.Combine(path, "1cv8ddb.1CD");
    private FileDatabaseConnection? _connection;

    public bool DatabaseExists => File.Exists(_dbpath);

    public Guid ReadId()
    {
        OpenIfNeed();

        var firstRow = _connection!["DEPOT"].Rows.FirstOrDefault();
        if (firstRow == null)
            throw new Exception("Таблица DEPOT не содержит записей");

        return firstRow["DEPOTID"].AsGuid();
    }

    public List<ConfigRepositoryUser> ReadUsers()
    {
        OpenIfNeed();

        var table = _connection!.Tables.FirstOrDefault(c => c.Name == "USERS");
        if (table == null)
            throw new Exception("Таблица пользователей не обнаружена");

        var fieldNumber = table.Fields.ToList().FindIndex(c => c.Name == "NAME");
        if (fieldNumber == -1)
            throw new Exception("Колонка имя пользователя не обнаружена");

        return _connection["USERS"].Rows.Select(c => new ConfigRepositoryUser
        {
            Id = c["USERID"].AsGuid(),
            Name = c["NAME"].AsString()
        }).Where(c => !string.IsNullOrEmpty(c.Name)).ToList();
    }

    public IEnumerable<ConfigRepositoryVersion> ReadVersions(int startVersion = -1)
    {
        OpenIfNeed();

        var table = _connection!.Tables.FirstOrDefault(c => c.Name == "VERSIONS");
        if (table == null)
            throw new Exception("Таблица версий не обнаружена");

        var users = ReadUsers().ToDictionary(c => c.Id, c => c);

        foreach (var tableRow in _connection["VERSIONS"].Rows)
        {
            var userId = tableRow["USERID"].AsGuid();
            if (userId == Guid.Empty)
                continue;

            var version = (decimal)tableRow["VERNUM"];
            if (startVersion > -1 && version < startVersion)
                continue;

            yield return new ConfigRepositoryVersion
            {
                Number = (int)version,
                Comment = tableRow["COMMENT"]?.ToString() ?? string.Empty,
                User = users[userId],
                DateTime = (DateTime)tableRow["VERDATE"]
            };
        }
    }
    
    public IEnumerable<ConfigRepositoryVersion> ReadVersions(List<ConfigRepositoryUser> users, int startVersion = -1)
    {
        OpenIfNeed();

        var table = _connection!.Tables.FirstOrDefault(c => c.Name == "VERSIONS");
        if (table == null)
            throw new Exception("Таблица версий не обнаружена");

        var u = users.ToDictionary(c => c.Id, c => c);

        foreach (var tableRow in _connection["VERSIONS"].Rows)
        {
            var userId = tableRow["USERID"].AsGuid();
            if (userId == Guid.Empty)
                continue;

            var version = (decimal)tableRow["VERNUM"];
            if (startVersion > -1 && version < startVersion)
                continue;
            
            var code = (string?)tableRow["CODE"];

            yield return new ConfigRepositoryVersion
            {
                ConfigVersion = code ?? string.Empty,
                Number = (int)version,
                Comment = tableRow["COMMENT"]?.ToString() ?? string.Empty,
                User = u[userId],
                DateTime = (DateTime)tableRow["VERDATE"]
            };
        }
    }

    private void OpenIfNeed()
    {
        _connection ??= new FileDatabaseConnection(_dbpath);

        if (!_connection.Opened)
            _connection.Open();
    }
    
    public void Dispose()
    {
        _connection?.Dispose();
    }
}