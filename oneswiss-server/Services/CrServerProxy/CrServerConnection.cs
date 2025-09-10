using System.Net.Sockets;

namespace OneSwiss.Server.Services.CrServerProxy;

public class CrServerConnection(string key, string host, int port) : IDisposable
{
    private TcpClient? _tcpClient;
    private HttpClient? _httpClient;

    public string Key { get; } = key;
    public DateTime LastUsingTimestamp { get; private set; } = DateTime.MinValue;
    public bool Connected  => _tcpClient?.Connected ?? false;
    public bool Blocked { get; private set; }
    
    public async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (_tcpClient is { Connected: true }) 
            return await _httpClient!.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        await InitTcpClient(cancellationToken);
        InitHttpClient();
        
        var response = await _httpClient!.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        LastUsingTimestamp = DateTime.Now;

        return response;
    }
    
    public void BlockConnection()
        => Blocked = true;
    
    public void UnblockConnection()
        => Blocked = false;

    private async Task InitTcpClient(CancellationToken cancellationToken)
    {
        _tcpClient?.Dispose();

        const int bufferSize = 3 * 1024 * 1024;
                
        _tcpClient = new TcpClient
        {
            NoDelay = true,
            ReceiveBufferSize = bufferSize,
            SendBufferSize = bufferSize,
        };
        await _tcpClient.ConnectAsync(host, port, cancellationToken);
            
        var stream = _tcpClient.GetStream();
        await Handshake(stream, cancellationToken);
    }

    private void InitHttpClient()
    {
        var stream = _tcpClient!.GetStream();
        
        _httpClient?.Dispose();
            
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = (_, _) => ValueTask.FromResult<Stream>(stream),
            KeepAlivePingPolicy = HttpKeepAlivePingPolicy.Always
        };
        _httpClient = new HttpClient(handler);
        // Любое значение, что-бы обойти проверку объекта в clr. реальный адрес задается в сокете хэндлера клиента
        _httpClient.BaseAddress = new Uri("http://localhost");
    }
    
    private static async Task Handshake(NetworkStream serverStream, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024];
        
        // Рукопожатие клиента и tcp службы сервера хранилищ
        // 1. Пустой пакет от клиента к службе
        await serverStream.WriteAsync(Memory<byte>.Empty, cancellationToken);
        
        // 2. Константный ответ от службы к клиенту
        var expectedTcpResponse = new byte[] { 0x53, 0xf5, 0xc6, 0x1a, 0x7b };
        
        var read = await serverStream.ReadAsync(buffer, cancellationToken);
        
        if (read != 5)
            throw new Exception("Ошибка рукопожатия с сервером хранилищ. Ожидалось 5 байт ответа");

        var response = buffer[..5];
        
        if (Equals(response, expectedTcpResponse))
            throw new Exception(
                "Ошибка рукопожатия с сервером хранилищ. Принятые двоичные данные отличаются от ожидаемого ответа");
            
        // 3. Пустой пакет от клиента к службе
        await serverStream.WriteAsync(Memory<byte>.Empty, cancellationToken);
        
        // 4. Пакет константа + новый гуид от клиента к службе
        var id = Guid.NewGuid().ToByteArray();
        var proxyRequest = new byte[] { 0x22, 0x48, 0x55, 0xb5 }.Concat(id).ToArray();
        await serverStream.WriteAsync(proxyRequest, cancellationToken);
        
        // 5. Пустой ответ от службы к клиенту
        await serverStream.ReadExactlyAsync(Memory<byte>.Empty, cancellationToken);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing) 
            return;
        
        _tcpClient?.Dispose();
        _httpClient?.Dispose();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~CrServerConnection()
    {
        Dispose(false);
    }
}