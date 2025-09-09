using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http.Extensions;

namespace OneSwiss.Server.Extensions;

public static class HttpContextExtensions
{
    public static HttpRequestMessage CreateProxyHttpRequest(this HttpContext context, Stream body)
    {
        var request = context.Request;

        long bodySize = 0;

        var requestMessage = new HttpRequestMessage(new HttpMethod(request.Method), "");
        var requestMethod = request.Method;
        if (!HttpMethods.IsGet(requestMethod) &&
            !HttpMethods.IsHead(requestMethod) &&
            !HttpMethods.IsDelete(requestMethod) &&
            !HttpMethods.IsTrace(requestMethod))
        {
            bodySize  = body.Length;
            requestMessage.Content = new StreamContent(body);
        }

        // Copy the request headers
        foreach (var header in request.Headers)
            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
        
        requestMessage.Content!.Headers.ContentLength = bodySize;

        return requestMessage;
    }
    
    public static async Task CopyProxyHttpResponse(this HttpContext context, HttpResponseMessage responseMessage)
    {
        ArgumentNullException.ThrowIfNull(responseMessage);

        var response = context.Response;

        response.StatusCode = (int)responseMessage.StatusCode;
        foreach (var header in responseMessage.Headers)
            response.Headers[header.Key] = header.Value.ToArray();

        foreach (var header in responseMessage.Content.Headers)
            response.Headers[header.Key] = header.Value.ToArray();

        // SendAsync removes chunking from the response. This removes the header so it doesn't expect a chunked response.
        response.Headers.Remove("transfer-encoding");

        await using var responseStream = await responseMessage.Content.ReadAsStreamAsync();
        await responseStream.CopyToAsync(response.Body, context.RequestAborted);
    }
}