using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp1
{
     public class HttpHandler2 : HttpClientHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri != null && request.RequestUri.Host.Equals("api.openai.com", StringComparison.OrdinalIgnoreCase))
        {
            request.RequestUri = new Uri($"http://192.168.212.78:1234/v1/chat/completions");
        }

        return base.SendAsync(request, cancellationToken);
    }

        public static implicit operator HttpClient(HttpHandler2 v)
        {
            throw new NotImplementedException();
        }
    }
    }
