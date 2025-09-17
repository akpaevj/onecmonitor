using System.Xml;

namespace OneSwiss.Server.Services.CrServerProxy;

public class RequestStreamProcessor(string outputFilePath, string repositoryName) : IDisposable
{
    private readonly string _outputFilePath = outputFilePath ?? throw new ArgumentNullException(nameof(outputFilePath));
    
    public async Task<RequestDetails> ProcessAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await using var inputStream = context.Request.Body;
        await using var outputStream = new FileStream(
            _outputFilePath,
            FileMode.Open,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true
        );
        
        var readerSettings = new XmlReaderSettings
        {
            Async = true,
            IgnoreWhitespace = true,
            IgnoreComments = true
        };

        var writerSettings = new XmlWriterSettings
        {
            Async = true,
            Indent = false,
            CloseOutput = true,
            WriteEndDocumentOnClose = true,
            NewLineHandling = NewLineHandling.None
        };

        using var xmlReader = XmlReader.Create(inputStream, readerSettings);
        await using var xmlWriter = XmlWriter.Create(outputStream, writerSettings);

        var details = await CopyXmlAsync(xmlReader, xmlWriter, cancellationToken);

        await xmlWriter.WriteEndDocumentAsync();
        await xmlWriter.FlushAsync();

        return details;
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
                    if (context.ThisTextIsComment)
                    {
                        context.ThisTextIsComment = false;
                        details.Comment = await reader.GetValueAsync();
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
        public bool NeedReadComment { get; set; }
        public bool ThisTextIsComment { get; set; }
        public bool NeedFixAliasElementValue { get; set; }
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

    ~RequestStreamProcessor()
    {
        ReleaseUnmanagedResources();
    }
}