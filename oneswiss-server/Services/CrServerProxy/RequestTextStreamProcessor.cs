using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace OneSwiss.Server.Services.CrServerProxy;

public class XmlStreamProcessor
{
    /// <summary>
    /// Обрабатывает XML потоково, извлекая элементы, атрибуты и их значения
    /// </summary>
    /// <param name="inputStream">Входной поток с XML</param>
    /// <param name="outputStream">Выходной поток для записи результатов</param>
    public static void ProcessXmlStream(Stream inputStream, Stream outputStream)
    {
        using var reader = new StreamReader(inputStream, Encoding.UTF8);
        using var writer = new StreamWriter(outputStream, Encoding.UTF8, 1024, true);
        
        
    }

    
}

public class RequestTextStreamProcessor(string outputFilePath, string repositoryName) : IDisposable
{
    private ProcessorContext processorContext = new();
    private readonly string _outputFilePath = outputFilePath ?? throw new ArgumentNullException(nameof(outputFilePath));
    
    public async Task<RequestDetails> ProcessAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        await using var inputStream = context.Request.Body;
        await using var outputStream = new FileStream(
            _outputFilePath,
            FileMode.Open,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true
        );
        
        var details = new RequestDetails();
        
        using var reader = new StreamReader(inputStream);
        await using var writer = new StreamWriter(outputStream);
        
        var elementStack = new Stack<string>();
        var buffer = new StringBuilder();
        var inElement = false;
        var inAttribute = false;
        var inComment = false;
        var inCData = false;
        var currentElement = string.Empty;
        var currentAttribute = string.Empty;
        var quoteChar = '\0';

        int charCode;
        while ((charCode = reader.Read()) != -1)
        {
            var c = (char)charCode;
            
            if (processorContext.WriteDirectlyToOutput)
            {
                await writer.WriteAsync(c);
                continue;
            }

            switch (inComment)
            {
                // Обработка комментариев
                case false when !inCData && buffer.Length >= 3 && 
                                buffer.ToString(buffer.Length - 3, 3) == "<!--":
                    inComment = true;
                    buffer.Clear();
                    continue;
                case true:
                {
                    if (buffer.Length >= 2 && buffer.ToString(buffer.Length - 2, 2) == "-->")
                    {
                        inComment = false;
                        buffer.Clear();
                    }
                    else
                        buffer.Append(c);
                    continue;
                }
            }

            switch (inCData)
            {
                // Обработка CDATA
                case false when buffer.Length >= 8 && 
                                buffer.ToString(buffer.Length - 8, 8) == "<![CDATA[":
                    inCData = true;
                    buffer.Clear();
                    continue;
                case true:
                {
                    if (buffer.Length >= 2 && buffer.ToString(buffer.Length - 2, 2) == "]]")
                    {
                        inCData = false;
                        buffer.Clear();
                    }
                    else
                        buffer.Append(c);
                    continue;
                }
            }

            // Обработка элементов и атрибутов
            if (c == '<' && !inElement && !inAttribute)
            {
                inElement = true;
                buffer.Clear();
                buffer.Append(c);
            }
            else switch (inElement)
            {
                case true:
                {
                    buffer.Append(c);

                    // Конец открывающего тега
                    if (c == '>')
                    {
                        ProcessElement(buffer.ToString(), writer, elementStack, ref currentElement);
                        buffer.Clear();
                        inElement = false;
                    }
                    // Начало атрибута
                    else if (char.IsWhiteSpace(c) && !inAttribute)
                    {
                        ProcessElementStart(buffer.ToString(), writer, elementStack, ref currentElement);
                        buffer.Clear();
                        inAttribute = true;
                    }
                    else switch (inAttribute)
                    {
                        // Значение атрибута
                        case true when c is '\'' or '"' && quoteChar == '\0':
                            quoteChar = c;
                            break;
                        case true when c == quoteChar:
                            quoteChar = '\0';
                            ProcessAttribute(buffer.ToString(), writer, ref currentAttribute);
                            buffer.Clear();
                            break;
                        // Конец самозакрывающегося тега
                        default:
                        {
                            if (buffer.Length >= 2 && buffer.ToString(buffer.Length - 2, 2) == "/>")
                            {
                                ProcessElement(buffer.ToString(), writer, elementStack, ref currentElement);
                                buffer.Clear();
                                inElement = false;
                                inAttribute = false;
                            }

                            break;
                        }
                    }

                    break;
                }
                case false when !inAttribute:
                {
                    // Текстовое содержимое
                    if (!char.IsWhiteSpace(c))
                        buffer.Append(c);
                    else if (buffer.Length > 0)
                    {
                        ProcessTextContent(buffer.ToString(), writer, currentElement);
                        buffer.Clear();
                    }

                    break;
                }
            }
        }

        // Обработка оставшегося буфера
        if (buffer.Length <= 0) 
            return details;
        
        if (inElement)
            ProcessElement(buffer.ToString(), writer, elementStack, ref currentElement);
        else
            ProcessTextContent(buffer.ToString(), writer, currentElement);

        return details;
    }
    
    private static void ProcessElementStart(string data, StreamWriter writer, Stack<string> elementStack, ref string currentElement)
    {
        var elementContent = data.Trim().TrimStart('<').Trim();
        if (string.IsNullOrEmpty(elementContent)) return;

        // Извлекаем имя элемента (до первого пробела или /)
        var elementName = ExtractElementName(elementContent);

        if (string.IsNullOrEmpty(elementName)) 
            return;
        
        currentElement = elementName;
        elementStack.Push(elementName);
        writer.WriteLine($"Элемент: {elementName}");
    }

    private static void ProcessElement(string data, StreamWriter writer, Stack<string> elementStack, ref string currentElement)
    {
        var elementContent = data.Trim().TrimStart('<').TrimEnd('>').Trim();
        if (string.IsNullOrEmpty(elementContent)) return;

        // Закрывающий тег
        if (elementContent.StartsWith('/'))
        {
            var elementName = elementContent[1..].Trim();
            if (elementStack.Count <= 0 || elementStack.Peek() != elementName) 
                return;
            
            elementStack.Pop();
            currentElement = elementStack.Count > 0 ? elementStack.Peek() : string.Empty;
            return;
        }

        // Самозакрывающийся тег
        if (elementContent.EndsWith('/'))
        {
            elementContent = elementContent.TrimEnd('/').Trim();
            var elementName = ExtractElementName(elementContent);

            if (string.IsNullOrEmpty(elementName)) 
                return;
            
            writer.WriteLine($"Элемент: {elementName} (самозакрывающийся)");
            ProcessAttributes(elementContent, writer, elementName);
            return;
        }

        // Открывающий тег
        var name = ExtractElementName(elementContent);
        if (string.IsNullOrEmpty(name)) 
            return;
        
        currentElement = name;
        elementStack.Push(name);
        writer.WriteLine($"Элемент: {name}");
        ProcessAttributes(elementContent, writer, name);
    }

    private static void ProcessAttributes(string elementContent, StreamWriter writer, string elementName)
    {
        var parts = elementContent.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        
        for (var i = 1; i < parts.Length; i++)
        {
            var part = parts[i].Trim();
            if (string.IsNullOrEmpty(part)) continue;

            var equalIndex = part.IndexOf('=');
            if (equalIndex <= 0) 
                continue;
            
            var attrName = part[..equalIndex].Trim();
            var attrValue = part[(equalIndex + 1)..].Trim().Trim('\"', '\'');

            if (!string.IsNullOrEmpty(attrName) && !string.IsNullOrEmpty(attrValue))
                writer.WriteLine($"  Атрибут: {attrName} = {attrValue}");
        }
    }

    private static void ProcessAttribute(string data, StreamWriter writer, ref string currentAttribute)
    {
        var attrContent = data.Trim();
        if (string.IsNullOrEmpty(attrContent)) return;

        var equalIndex = attrContent.IndexOf('=');
        if (equalIndex > 0)
        {
            currentAttribute = attrContent[..equalIndex].Trim();
            var attrValue = attrContent[(equalIndex + 1)..].Trim().Trim('\"', '\'');
            
            writer.WriteLine($"  Атрибут: {currentAttribute} = {attrValue}");
        }
        else
            currentAttribute = attrContent;
    }

    private static void ProcessTextContent(string text, StreamWriter writer, string currentElement)
    {
        var cleanText = text.Trim();
        if (!string.IsNullOrEmpty(cleanText) && !string.IsNullOrEmpty(currentElement))
            writer.WriteLine($"  Значение элемента {currentElement}: {cleanText}");
    }

    private static string ExtractElementName(string elementContent)
    {
        // Извлекаем имя элемента (до первого пробела, /, >)
        var endIndex = elementContent.IndexOfAny([' ', '/', '>']);
        return endIndex > 0 ? elementContent[..endIndex] : elementContent;
    }
    
    private async Task<RequestDetails> CopyXmlAsync(XmlReader reader, XmlWriter writer, CancellationToken cancellationToken)
    {
        var depth = 0;
        var details = new RequestDetails();
        var context = new ProcessorContext();
        
        while (await reader.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (reader.NodeType)
            {
                case XmlNodeType.Element:
                    await writer.WriteStartElementAsync(reader.Prefix, reader.LocalName, reader.NamespaceURI);

                    if (details.IsDepotOpen && reader.LocalName == "alias")
                        context.NeedFixAliasElementValue = true;

                    if ((details.IsChangeVersion || details.IsCommit) && !context.NeedReadComment && reader.LocalName == "comment")
                    {
                        context.NeedReadComment = true;
                        context.ThisTextIsComment = true;
                    }
                    
                    await WriteAttributesAsync(context, details, reader, writer);
                    
                    if (reader.IsEmptyElement)
                        await writer.WriteEndElementAsync();
                    else
                        depth++;
                    
                    context.IsFirstElement = false;
                    
                    break;
                case XmlNodeType.Text:
                    var value = await reader.GetValueAsync();

                    if (context.ThisTextIsComment)
                    {
                        context.ThisTextIsComment = false;
                        details.Comment = value;
                    }

                    await WriteTextStreamingAsync(reader, writer);
                    break;

                case XmlNodeType.CDATA:
                    await writer.WriteCDataAsync(await reader.GetValueAsync());
                    break;

                case XmlNodeType.EntityReference:
                    await writer.WriteEntityRefAsync(reader.Name);
                    break;

                case XmlNodeType.ProcessingInstruction:
                case XmlNodeType.XmlDeclaration:
                    await writer.WriteProcessingInstructionAsync(reader.Name, await reader.GetValueAsync());
                    break;

                case XmlNodeType.Comment:
                    await writer.WriteCommentAsync(await reader.GetValueAsync());
                    break;

                case XmlNodeType.DocumentType:
                    await writer.WriteDocTypeAsync(
                        reader.Name,
                        reader.GetAttribute("PUBLIC"),
                        reader.GetAttribute("SYSTEM"),
                        await reader.GetValueAsync());
                    break;

                case XmlNodeType.Whitespace:
                case XmlNodeType.SignificantWhitespace:
                    await writer.WriteWhitespaceAsync(await reader.GetValueAsync());
                    break;

                case XmlNodeType.EndElement:
                    await writer.WriteEndElementAsync();
                    depth--;
                    if (depth % 100 == 0)
                        await writer.FlushAsync();
                    break;
            }
        }

        return details;
    }
    
    private static async Task WriteTextStreamingAsync(XmlReader reader, XmlWriter writer)
    {
        var buffer = new char[4096]; 
        int charsRead;
        
        while ((charsRead = await reader.ReadValueChunkAsync(buffer, 0, buffer.Length)) > 0)
            await writer.WriteCharsAsync(buffer, 0, charsRead);
    }
    
    private async Task WriteAttributesAsync(
        ProcessorContext context,
        RequestDetails requestDetails,
        XmlReader reader,
        XmlWriter writer)
    {
        if (reader.HasAttributes)
        {
            for (var i = 0; i < reader.AttributeCount; i++)
            {
                reader.MoveToAttribute(i);

                var attrValue = await reader.GetValueAsync();

                if (context.IsFirstElement && reader.LocalName == "name")
                {
                    requestDetails.RequestName = attrValue;
                    requestDetails.IsDepotOpen = attrValue.Equals("DevDepotAdmin_openDevDepot", StringComparison.InvariantCultureIgnoreCase);
                    requestDetails.IsCommit = attrValue.Equals("DevDepot_commitObjects", StringComparison.InvariantCultureIgnoreCase);
                    requestDetails.IsChangeVersion = attrValue.Equals("DevDepot_changeVersion", StringComparison.InvariantCultureIgnoreCase);
                }

                if (context.IsFirstElement && reader.LocalName == "alias" || context.NeedFixAliasElementValue && reader.LocalName == "value")
                {
                    context.NeedFixAliasElementValue = false;
                    
                    await writer.WriteAttributeStringAsync(reader.Prefix, reader.LocalName, 
                        reader.NamespaceURI, repositoryName);
                }
                else await writer.WriteAttributeStringAsync(reader.Prefix, reader.LocalName, 
                    reader.NamespaceURI, attrValue);
            }
            reader.MoveToElement();
        }
    }

    private class ProcessorContext
    {
        public bool IsFirstElement { get; set; } = true;
        public bool ReadName { get; set; } = false;
        public bool NeedReadComment { get; set; }
        public bool ThisTextIsComment { get; set; }
        public bool NeedFixAliasElementValue { get; set; }
        public bool WriteDirectlyToOutput { get; set; }
    }

    private void ReleaseUnmanagedResources()
    {
        if (File.Exists(_outputFilePath))
            File.Delete(_outputFilePath);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~RequestTextStreamProcessor()
    {
        ReleaseUnmanagedResources();
    }
}