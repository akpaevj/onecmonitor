using OneSwiss.V8.Designer.Models;

namespace OneSwiss.V8.Designer.Batch;

public class ConfigRepositoryReportReader : IDisposable
{
    private readonly StreamReader _streamReader;
    private ConfigRepositoryReportItem? _currentItem;
    private string? _currentLine;
    
    public bool EndOfFile => _streamReader.EndOfStream;
    public ConfigRepositoryReportHeader Header { get; } = new();

    public ConfigRepositoryReportReader(string reportPath)
    {
        _streamReader = new StreamReader(reportPath);
        ReadHeader();
    }

    private void ReadHeader()
    {
        Header.ReportBy = _streamReader.ReadLine()!;
        
        var date = _streamReader.ReadLine()!;
        var time = _streamReader.ReadLine()!;
        Header.CreatedAt = DateTime.Parse($"{date} {time}");

        _currentLine = _streamReader.ReadLine();
    }

    public async Task<ConfigRepositoryReportItem> NextItem(CancellationToken cancellationToken)
    {
        if (EndOfFile)
            throw new Exception("Достигнут конец отчета");
        
        var current = _currentItem;
        _currentItem = await ReadNextItem(cancellationToken);

        return current!;
    }

    private async Task<ConfigRepositoryReportItem> ReadNextItem(CancellationToken cancellationToken)
    {
        var item = new ConfigRepositoryReportItem();

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_currentLine!.StartsWith("Версия:"))
                item.Version = int.Parse(GetKeyValueParameter());
            else if (_currentLine!.StartsWith("Версия конфигурации:"))
                item.ConfigurationVersion = GetKeyValueParameter();
            else if (_currentLine!.StartsWith("Пользователь:"))
                item.ConfigurationVersion = GetKeyValueParameter();
            else if (_currentLine!.StartsWith("Дата создания:"))
            {
                var date = GetKeyValueParameter();
                await ReadNextLine(cancellationToken);
                var time = GetKeyValueParameter();
                
                item.CreatedAt = DateTime.Parse($"{date} {time}");
            }
            else if (_currentLine!.StartsWith("Комментарий:"))
            {
                await ReadNextLine(cancellationToken);
                item.Comment = _currentLine;
            }
            else if (_currentLine!.StartsWith("\tУдалены"))
            {
                item.Deleted = await ReadChanges(cancellationToken);
                await ReadNextLine(cancellationToken);
            }
            else if (_currentLine!.StartsWith("\tДобавлены"))
            {
                item.Added = await ReadChanges(cancellationToken);
                await ReadNextLine(cancellationToken);
            }
            else if (_currentLine!.StartsWith("\tИзменены"))
            {
                item.Changed = await ReadChanges(cancellationToken);
                await ReadNextLine(cancellationToken);
            }
            else
                throw new Exception("Неожиданная структура отчета");

            await ReadNextLine(cancellationToken);

            // Это начало следующего элемента или конец файла
            if (string.IsNullOrEmpty(_currentLine) || _currentLine.StartsWith("Версия:"))
                break;
        }
        
        return item;
    }

    private async Task<List<string>> ReadChanges(CancellationToken cancellationToken)
    {
        var changes = new List<string>();

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await _streamReader.ReadLineAsync(cancellationToken);
            if (string.IsNullOrEmpty(line))
                break;
            
            changes.Add(line);
        }

        return changes;
    }

    private string GetKeyValueParameter()
    {
        var ch = _currentLine!.IndexOf(':');

        return _currentLine[(ch + 1)..].Trim();
    }
    
    private async Task ReadNextLine(CancellationToken cancellationToken)
        => _currentLine = await _streamReader.ReadLineAsync(cancellationToken);

    public void Dispose()
    {
        _streamReader.Dispose();
    }
}