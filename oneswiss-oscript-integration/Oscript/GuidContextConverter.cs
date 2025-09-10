using OneScript.Commons;
using OneScript.Contexts;
using OneScript.StandardLibrary;
using OneScript.StandardLibrary.Collections;
using OneScript.Values;
using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;

namespace OneSwiss.OneScript.Oscript;

public class NullableConverter<T> : IContextValueConverter<T?>
{
    public IValue ToIValue(T? obj)
    {
        return obj == null ? BslNullValue.Instance : ContextValuesMarshaller.ConvertReturnValue(obj);
    }

    public T? ToClr(IValue obj)
    {
        return (T?)ContextValuesMarshaller.ConvertToClrObject(obj);
    }
}

public class FilesContextConverter : IContextValueConverter<Dictionary<Guid, string>>
{
    public IValue ToIValue(Dictionary<Guid, string> obj)
    {
        return new MapImpl(obj.Select(c =>
            new KeyAndValueImpl(new GuidWrapper(c.Key.ToString()), ValueFactory.Create(c.Value))));
    }

    public Dictionary<Guid, string> ToClr(IValue obj)
    {
        if (obj is MapImpl arr)
            return arr.Select(c =>
            {
                if (c.Key is not GuidWrapper guidWrapper || c.Value is not BslStringValue path)
                    throw new Exception("Ошибка преобразования структуры в словарь файлов");

                return new KeyValuePair<Guid, string>(Guid.Parse(guidWrapper.ToString()), path.ToString());
            }).ToDictionary(c => c.Key, c => c.Value);

        throw new Exception($"{obj} не может быть преобразован в словарь файлов");
    }
}

public class ListContextConverter<T> : IContextValueConverter<List<T>>
{
    public IValue ToIValue(List<T> obj)
    {
        return new ArrayImpl(obj.Select(ContextValuesMarshaller.ConvertReturnValue));
    }

    public List<T> ToClr(IValue obj)
    {
        if (obj is ArrayImpl arr)
            return arr.Select(c => (T)ContextValuesMarshaller.ConvertToClrObject(c)).ToList();

        throw new Exception($"{obj} не может быть преобразован в список");
    }
}

public class ReadOnlyListContextConverter<T> : IContextValueConverter<IReadOnlyList<T>>
{
    public IValue ToIValue(IReadOnlyList<T> obj)
    {
        return new ArrayImpl(obj.Select(ContextValuesMarshaller.ConvertReturnValue));
    }

    public IReadOnlyList<T> ToClr(IValue obj)
    {
        if (obj is ArrayImpl arr)
            return arr.Select(c => (T)ContextValuesMarshaller.ConvertToClrObject(c)).ToList();

        throw new Exception($"{obj} не может быть преобразован в список");
    }
}

public class GuidContextConverter : IContextValueConverter<Guid?>
{
    public IValue ToIValue(Guid? obj)
    {
        return obj == null ? BslNullValue.Instance : new GuidWrapper(obj.ToString());
    }

    public Guid? ToClr(IValue obj)
    {
        return obj switch
        {
            BslNullValue => null,
            GuidWrapper v => (Guid)((IObjectWrapper)v).UnderlyingObject,
            _ => throw new Exception("Конвертация доступна только для GuidWrapper")
        };
    }
}