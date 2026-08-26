using System.Text;
using OneSwiss.Agent.Extensions;
using OneSwiss.V8.Platform.Brackets;
using BracketsParser = OneSwiss.V8.Platform.Brackets.BracketsParser;

namespace OneSwiss.Agent.Services.EventLog.BracketsReader;

internal class LgfDataProvider : IDisposable
{
    private readonly StreamReader _streamReader;
    private readonly BracketsParser _parser;
    private readonly IEnumerable<BracketValue> _bracketsStream;
    
    private readonly Dictionary<ObjectType, Dictionary<int, string>> _objects = new();
    private readonly Dictionary<ObjectType, Dictionary<int, LgfReference>> _referencedObjects = new();

    public LgfDataProvider(string path)
    {
        // 1Cv8.lgf остаётся открытым на запись у rphost/rmngr, пока информационная база живая -
        // без FileShare.ReadWrite чтение падает с ошибкой совместного доступа (см. аналогичный
        // паттерн в LgpReader).
        var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        _streamReader = new StreamReader(fs, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        SkipSignature();
        
        _parser = new BracketsParser();
        _bracketsStream = _parser.ParseStream(_streamReader);
    }
    
    private void SkipSignature()
    {
        _streamReader.ReadLine();
        _streamReader.ReadLine();
        _streamReader.ReadLine();
    }

    public string GetObjectValue(ObjectType objectType, int number, CancellationToken cancellationToken)
    {
        if (number == 0)
            return string.Empty;

        if (TryGetObjectValue(objectType, number, out var value))
            return value;
        
        ReadAll(cancellationToken);

        if (!TryGetObjectValue(objectType, number, out value))
            throw new Exception("Значение по типу и номеру объекта не найдено");

        return value;
    }

    public LgfReference GetReferencedObjectValue(ObjectType objectType, int number,
        CancellationToken cancellationToken)
    {
        if (number == 0)
            return LgfReference.EmptyInstance;

        if (TryGetReferencedObject(objectType, number, out var value))
            return value;
        
        ReadAll(cancellationToken);
        
        if (!TryGetReferencedObject(objectType, number, out value))
            throw new Exception("Описание ссылки по типу и номеру объекта не найдено");

        return value;
    }

    private bool TryGetObjectValue(ObjectType objectType, int number, out string value)
    {
        if (_objects.TryGetValue(objectType, out var numbers) && numbers.TryGetValue(number, out value!))
            return true;
        
        value = string.Empty;
        return false;
    }
    
    private bool TryGetReferencedObject(ObjectType objectType, int number, out LgfReference value)
    {
        if (_referencedObjects.TryGetValue(objectType, out var numbers) && numbers.TryGetValue(number, out value!))
            return true;
        
        value = null!;
        return false;
    }
    
    private void ReadAll(CancellationToken cancellationToken)
    {
        foreach (var item in _bracketsStream)
        {
            if (item.Type != BracketValueType.Object)
                throw new Exception("Объект LGF должен быть комплексным объектов");
            
            if (!int.TryParse(item.ObjectValue[0].StringValue, out var typeNumber))
                throw new Exception("Неизвестный тип объекта LGF");
            
            var itemObjectType = (ObjectType)typeNumber;
            
            if (itemObjectType >= ObjectType.Unknown)
                continue;

            if (itemObjectType is ObjectType.Users or ObjectType.Metadata)
            {
                var number = int.Parse(item.ObjectValue[3].StringValue);
                var value = new LgfReference(item.ObjectValue[2].StringValue, item.ObjectValue[1].StringValue);
                    
                AddValueToDictionary(_referencedObjects, itemObjectType, number, value);
            }
            else
            {
                var number = int.Parse(item.ObjectValue[2].StringValue);
                var value = item.ObjectValue[1].StringValue;
                    
                AddValueToDictionary(_objects, itemObjectType, number, value);
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private static void AddValueToDictionary<T>(Dictionary<ObjectType, Dictionary<int, T>> dict, ObjectType objectType, int number, T value)
    {
        if (!dict.ContainsKey(objectType))
            dict.Add(objectType, []);

        dict[objectType].Remove(number);
        dict[objectType].Add(number, value);
    }

    public void Dispose()
    {
        _streamReader.Dispose();
        _parser.Dispose();
    }
}