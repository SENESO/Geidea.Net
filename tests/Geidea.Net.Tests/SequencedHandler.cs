using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Geidea.Net.Tests
{
    /// <summary>
    /// Fake HttpMessageHandler that returns queued JSON responses and records requests.
    /// </summary>
    internal sealed class SequencedHandler : HttpMessageHandler
    {
        private readonly Queue<string> _responses;

        public List<RecordedRequest> Requests { get; } = new List<RecordedRequest>();

        public SequencedHandler(IEnumerable<string> jsonResponses)
        {
            _responses = new Queue<string>(jsonResponses);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync().ConfigureAwait(false);

            Requests.Add(new RecordedRequest
            {
                Method = request.Method.Method,
                Url = request.RequestUri.ToString(),
                Authorization = request.Headers.Authorization?.ToString(),
                Body = body
            });

            if (_responses.Count == 0)
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responses.Dequeue(), Encoding.UTF8, "application/json")
            };
        }

        public sealed class RecordedRequest
        {
            public string Method { get; set; }
            public string Url { get; set; }
            public string Authorization { get; set; }
            public string Body { get; set; }
        }
    }
}
