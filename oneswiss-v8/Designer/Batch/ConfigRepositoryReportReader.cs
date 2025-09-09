using System.Text;
using System.Text.RegularExpressions;
using OneSwiss.V8.Designer.Models;

namespace OneSwiss.V8.Designer.Batch;

public partial class ConfigRepositoryReportReader : IDisposable
{
    private readonly StreamReader _streamReader;
    private string? _currentLine;

    public ConfigRepositoryReportReader(string reportPath)
    {
        _streamReader = new StreamReader(reportPath);
        ReadHeader().Wait();
    }

    public bool EndOfFile => _streamReader.EndOfStream;
    public ConfigRepositoryReportHeader Header { get; } = new();

    public void Dispose()
    {
        _streamReader.Dispose();
    }

    [GeneratedRegex(@"^(Добавлены|Изменены|Удалены)\s+\d+$", RegexOptions.Compiled | RegexOptions.ExplicitCapture)]
    private static partial Regex ChangesRegex();

    private async Task ReadHeader()
    {
        Header.ReportBy = await ReadField(false, "Отчет по версиям хранилища");

        var date = await ReadField(false, "Дата отчета");
        var time = await ReadField(false, "Время отчета");

        Header.CreatedAt = DateTime.Parse($"{date} {time}");

        await ReadNextLine();
    }

    private async Task<KeyValuePair<string, string>> ReadField(bool fromCurrentLine,
        CancellationToken cancellationToken = default)
    {
        if (!fromCurrentLine)
        {
            await ReadNextLine(cancellationToken);
            ThrowIfEndOfFile();
        }

        var kv = ReadKeyValueFromCurrentString();

        if (kv == null)
            throw new Exception($"Неожиданная строка при чтении отчета: {_currentLine}. Ожидалось поле");

        return (KeyValuePair<string, string>)kv;
    }

    private async Task<string> ReadField(bool fromCurrentLine, string expectedField,
        CancellationToken cancellationToken = default)
    {
        var nextField = await ReadField(fromCurrentLine, cancellationToken);

        if (!nextField.Key.Equals(expectedField, StringComparison.InvariantCultureIgnoreCase))
            throw new Exception($"Неожиданное поле при чтении отчета {nextField}. Ожидаемое поле: {expectedField}");

        return nextField.Value;
    }

    private bool CurrentLineIsField(string expectedField)
    {
        var kv = ReadKeyValueFromCurrentString();
        return kv?.Key == expectedField;
    }

    public async Task<ConfigRepositoryReportItem> NextItem(bool skipChanges = false,
        CancellationToken cancellationToken = default)
    {
        if (EndOfFile)
            throw new Exception("Достигнут конец отчета");

        return await ReadNextItem(skipChanges, cancellationToken);
    }

    private async Task<ConfigRepositoryReportItem> ReadNextItem(bool skipChanges = false,
        CancellationToken cancellationToken = default)
    {
        var item = new ConfigRepositoryReportItem();
        var readStarted = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            if (CurrentLineIsNull() || (CurrentLineIsField("Версия") && readStarted))
                break; // Это конец текущего элемента отчета

            if (CurrentLineIsEmpty())
            {
                await ReadNextLine(cancellationToken);
                continue; // Пустую строку просто пропускаем
            }

            readStarted = true;

            // Проверим, могут начаться секции Добавлены/Изменены/Удалены
            if (CurrentLineIsChanges())
            {
                if (skipChanges)
                {
                    await SkipToNextVersion(cancellationToken);
                    return item;
                }

                item.Added = await ReadChanges(cancellationToken);
                await ReadNextLine(cancellationToken);
                continue;
            }

            var (key, value) = await ReadField(true, cancellationToken);

            switch (key)
            {
                case "Версия":
                    item.Version = int.Parse(value);
                    break;
                case "Версия конфигурации":
                    item.ConfigurationVersion = value;
                    break;
                case "Пользователь":
                    item.User = value;
                    break;
                case "Метка":
                    item.Label = value;
                    break;
                case "Дата создания":
                {
                    var time = await ReadField(false, "Время создания", cancellationToken);
                    item.CreatedAt = DateTime.Parse($"{value} {time}");
                    break;
                }
                case "Комментарий":
                    item.Comment = await ReadComment(cancellationToken) ?? "";
                    continue;
                default:
                    throw new Exception("Неожиданная структура отчета");
            }

            await ReadNextLine(cancellationToken);
        }

        return item;
    }

    private async Task<List<string>> ReadChanges(CancellationToken cancellationToken)
    {
        var changes = new List<string>();

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await ReadNextLine(cancellationToken);

            if (string.IsNullOrEmpty(line))
                break;

            changes.Add(line);
        }

        return changes;
    }

    private async Task<string?> ReadNextLine(CancellationToken cancellationToken = default)
    {
        _currentLine = await _streamReader.ReadLineAsync(cancellationToken);
        _currentLine = _currentLine?.Trim();

        return _currentLine;
    }

    private async Task<string?> ReadComment(CancellationToken cancellationToken = default)
    {
        var data = new StringBuilder();

        while (true)
        {
            await ReadNextLine(cancellationToken);

            if (CurrentLineIsNull() || CurrentLineIsChanges())
                return data.ToString();

            data.AppendLine(_currentLine);
        }
    }

    private async Task SkipToNextVersion(CancellationToken cancellationToken = default)
    {
        while (!EndOfFile)
        {
            var line = await ReadNextLine(cancellationToken);

            if (line == null || CurrentLineIsField("Версия"))
                return;
        }
    }

    private bool CurrentLineIsChanges()
    {
        return ChangesRegex().IsMatch(_currentLine!);
    }

    private bool CurrentLineIsNull()
    {
        return _currentLine == null;
    }

    private bool CurrentLineIsEmpty()
    {
        return _currentLine?.Trim() == string.Empty;
    }

    private KeyValuePair<string, string>? ReadKeyValueFromCurrentString()
    {
        if (_currentLine == null)
            return null;

        var line = _currentLine!.Trim();

        var c = line.IndexOf(':');

        if (c == -1)
            return null;

        return new KeyValuePair<string, string>(line[..c].Trim(), line[(c + 1)..].Trim());
    }

    private void ThrowIfEndOfFile()
    {
        if (_currentLine == null)
            throw new Exception("Достигнут конец отчета");
    }
}