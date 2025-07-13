using OneSTools.FileDatabase;

namespace OneSwiss.V8.ConfigurationRepository;

public class ConfigurationRepositoryReader(string path) : IDisposable
{
    private readonly FileDatabaseConnection _connection = new(path);

    public List<string> ReadUsers()
    {
        OpenIfNeed();

        var table = _connection.Tables.FirstOrDefault(c => c.Name == "USERS");
        if (table == null)
            throw new Exception("Таблица пользователей не обнаружена");

        var fieldNumber = table.Fields.ToList().FindIndex(c => c.Name == "NAME");
        if (fieldNumber == -1)
            throw new Exception("Колонка имя пользователя не обнаружена");

        return table.Rows.Select(c => c[fieldNumber].ToString()).ToList()!;
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