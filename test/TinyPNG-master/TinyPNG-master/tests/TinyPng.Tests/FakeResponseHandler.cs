using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace TinyPng.Tests
{
    public class FakeResponseHandler : DelegatingHandler
    {
        private readonly Dictionary<Uri, HttpResponseMessage> _FakeGetResponses = new Dictionary<Uri, HttpResponseMessage>();
        private readonly Dictionary<Uri, HttpResponseMessage> _FakePostResponses = new Dictionary<Uri, HttpResponseMessage>();


        public void AddFakeGetResponse(Uri uri, HttpResponseMessage responseMessage)
        {
            _FakeGetResponses.Add(uri, responseMessage);
        }
        public void AddFakePostResponse(Uri uri, HttpResponseMessage responseMessage)
        {
            _FakePostResponses.Add(uri, responseMessage);
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (request.Method == HttpMethod.Get && _FakeGetResponses.ContainsKey(request.RequestUri))
            {
                return _FakeGetResponses[request.RequestUri];
            }
            if (request.Method == HttpMethod.Post &&_FakePostResponses.ContainsKey(request.RequestUri))
            {
                return _FakePostResponses[request.RequestUri];
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.NotFound) { RequestMessage = request };
            }

        }
    }
}
