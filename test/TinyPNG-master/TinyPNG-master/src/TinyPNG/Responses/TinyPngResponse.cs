using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace TinyPng.Responses
{
    public class TinyPngResponse
    {
        public HttpResponseMessage HttpResponseMessage { get; }

        private int compressionCount;

        public int CompressionCount => compressionCount;

        protected TinyPngResponse(HttpResponseMessage msg)
        {
            if (msg.Headers.TryGetValues("Compression-Count", out IEnumerable<string> compressionCountHeaders))
            {
                int.TryParse(compressionCountHeaders.First(), out compressionCount);
            }
            HttpResponseMessage = msg;
        }
    }
}
