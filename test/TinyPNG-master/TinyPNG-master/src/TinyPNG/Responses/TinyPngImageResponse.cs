using System.Net.Http;

namespace TinyPng.Responses
{
    /// <summary>
    /// This is a response which contains actual image data
    /// </summary>
    public class TinyPngImageResponse : TinyPngResponse
    {
        public TinyPngImageResponse(HttpResponseMessage msg) : base(msg)
        {
        }
    }
}
