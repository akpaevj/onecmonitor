using OneSTools.FileDatabase;
using OneSTools.FileDatabase.Extensions;
using OneSwiss.V8.ConfigurationRepository.Models;

namespace OneSwiss.V8.ConfigurationRepository;

public class ConfigRepositoryConnection(string path) : IDisposable
{
    private readonly FileDatabaseConnection _connection = new(path);

    public Guid ReadId()
    {
        OpenIfNeed();
        
        var firstRow = _connection["DEPOT"].Rows.FirstOrDefault();
        if (firstRow == null)
            throw new Exception("Таблица DEPOT не содержит записей");

        return firstRow["DEPOTID"].AsGuid();
    }
    
    public List<ConfigRepositoryUser> ReadUsers()
    {
        OpenIfNeed();
        
        var table = _connection.Tables.FirstOrDefault(c => c.Name == "USERS");
        if (table == null)
            throw new Exception("Таблица пользователей не обнаружена");

        var fieldNumber = table.Fields.ToList().FindIndex(c => c.Name == "NAME");
        if (fieldNumber == -1)
            throw new Exception("Колонка имя пользователя не обнаружена");

        return _connection["USERS"].Rows.Select(c => new ConfigRepositoryUser()
        {
            Id = c["USERID"].AsGuid(),
            Name = c["NAME"].AsString()
        }).Where(c => !string.IsNullOrEmpty(c.Name)).ToList();
    }

    private void OpenIfNeed()
    {
        if (!_connection.Opened)
            _connection.Open();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}