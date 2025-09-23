using System.Text;
using OneSTools.BracketsFile;

namespace OneSwiss.V8.Platform.Brackets;

public class BracketsParser : IDisposable
{
    private readonly StringBuilder _stringBuilder = new();
    private bool _disposed;
    
    public List<BracketValue> ParseSingle(string input)
    {
        var result = Parse(input);
        if (result.Count != 1)
            throw new Exception("Ожидался один объект");

        return result[0].ObjectValue;
    }
    
    public List<BracketValue> Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [];

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
        using var reader = new StreamReader(stream);
        
        return ParseAll(reader);
    }
    
    public List<BracketValue> Parse(StreamReader reader)
        => ParseAll(reader);
    
    public IEnumerable<BracketValue> ParseStream(string input)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(input));
        using var reader = new StreamReader(stream);
        
        foreach (var obj in ParseStream(reader))
            yield return obj;
    }
    
    public IEnumerable<BracketValue> ParseStream(StreamReader reader)
    {
        var context = new ParserContext(reader);
        
        SkipWhitespace(context);
        
        /*if (Peek(context) == '{')
        {
            // Одиночный объект
            yield return ParseObject(context);
            yield break;
        }*/
        
        while (!context.IsEndOfStream())
        {
            // Если мы продолжим чтение из потока, то следующим символом получим запятую, пропустим ее
            if (Peek(context) == ',')
            {
                context.Read();
                SkipWhitespace(context);
            }
            
            yield return ParseObject(context);
            
            SkipWhitespace(context);
            
            if (Peek(context) == ',')
            {
                context.Read();
                SkipWhitespace(context);
            }
            else if (!context.IsEndOfStream())
                throw new FormatException($"Ожидалась запятая ',' в позиции {context.Position}");
        }
    }
    
    private List<BracketValue> ParseAll(StreamReader reader)
        => ParseStream(reader).ToList();

    private BracketValue ParseObject(ParserContext context)
    {
        var values = new List<BracketValue>();
        
        Expect(context, '{');
        SkipWhitespace(context);
        
        if (Peek(context) == '}')
        {
            context.Read();
            return BracketValue.CreateObject(values);
        }
        
        while (!context.IsEndOfStream() && Peek(context) != '}')
        {
            values.Add(ParseValue(context));
            
            SkipWhitespace(context);
            
            if (Peek(context) == ',')
            {
                context.Read();
                SkipWhitespace(context);
            }
            else if (Peek(context) != '}')
                throw new FormatException($"Ожидалась запятая ',' или закрывающая скобка '}}' в позиции {context.Position}");
        }
        
        Expect(context, '}');
        return BracketValue.CreateObject(values);
    }

    private BracketValue ParseValue(ParserContext context)
    {
        SkipWhitespace(context);
        
        if (context.IsEndOfStream())
            throw new FormatException($"Неожиданный конец входных данных");
        
        if (Peek(context) == ',')
            return BracketValue.CreateEmpty();
        
        var current = Peek(context);

        return current switch
        {
            '\'' or '"' => BracketValue.CreateString(ParseString(context)),
            '{' => ParseObject(context),
            _ => BracketValue.CreateString(ParsePlainString(context))
        };
    }

    private string ParseString(ParserContext context)
    {
        var quoteChar = Peek(context);
        context.Read(); // Пропускаем открывающую кавычку
        
        _stringBuilder.Clear();
        
        while (!context.IsEndOfStream())
        {
            var current = context.Read();
            
            if (current == quoteChar)
            {
                // Проверяем экранирование
                if (Peek(context) == quoteChar)
                {
                    _stringBuilder.Append(quoteChar);
                    context.Read(); // Пропускаем вторую кавычку
                }
                else
                    break; // Закрывающая кавычка
            }
            else
                _stringBuilder.Append(current);
        }
        
        return _stringBuilder.ToString();
    }

    private string ParsePlainString(ParserContext context)
    {
        _stringBuilder.Clear();
        
        while (!context.IsEndOfStream())
        {
            var current = Peek(context);
            
            if (current == ',' || current == '}' || char.IsWhiteSpace(current))
                break;
            
            _stringBuilder.Append(context.Read());
        }
        
        return _stringBuilder.ToString();
    }

    private static void SkipWhitespace(ParserContext context)
    {
        while (!context.IsEndOfStream() && char.IsWhiteSpace(Peek(context)))
            context.Read();
    }

    private static char Peek(ParserContext context) => context.Peek();

    private static void Expect(ParserContext context, char expected)
    {
        var actual = context.Read();
        
        if (actual != expected)
            throw new FormatException($"Ожидался символ '{expected}', но получен '{actual}' в позиции {context.Position}");
    }
    
    private class ParserContext(StreamReader reader)
    {
        private int? _nextChar;

        public long Position => reader.GetPosition();

        public char Peek()
        {
            _nextChar ??= ReadNext();
            return _nextChar == -1 ? '\0' : (char)_nextChar.Value;
        }

        public char Read()
        {
            char result;
            
            if (_nextChar != null)
            {
                result = (char)_nextChar.Value;
                _nextChar = null;
            }
            else
            {
                var read = ReadNext();
                result = read == -1 ? '\0' : (char)read;
            }
            
            return result;
        }

        private int ReadNext()
        {
            int result;
            
            while(true)
            {
                result = reader.Read();
                if (result != 0)
                    break;
            }
            
            return result;
        }

        public bool IsEndOfStream()
        {
            if (_nextChar != null)
                return _nextChar == -1;
            
            _nextChar = ReadNext();
            return _nextChar == -1;
        }
    }

    public void Dispose()
    {
        if (_disposed) 
            return;
        
        _stringBuilder.Clear();
        _disposed = true;
    }
}