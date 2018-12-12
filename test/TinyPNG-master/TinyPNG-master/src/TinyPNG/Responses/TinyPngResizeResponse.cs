using System.Net.Http;

namespace TinyPng.Responses
{
    public class TinyPngResizeResponse : TinyPngImageResponse
    {
        public TinyPngResizeResponse(HttpResponseMessage msg) : base(msg)
        {

        }
    }
}
