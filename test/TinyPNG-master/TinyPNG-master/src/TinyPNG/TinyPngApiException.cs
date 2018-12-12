using System;

namespace TinyPng
{
    public class TinyPngApiException : Exception
    {
        public int StatusCode { get; }
        public string StatusReasonPhrase { get; }
        public string ErrorTitle { get; }
        public string ErrorMessage { get; }


        public TinyPngApiException(int statusCode, string statusReasonPhrase, string errorTitle, string errorMessage)
        {
            ErrorTitle = errorTitle;
            ErrorMessage = errorMessage;
            StatusCode = statusCode;
            StatusReasonPhrase = statusReasonPhrase;

            Data.Add(nameof(ErrorTitle), ErrorTitle);
            Data.Add(nameof(ErrorMessage), ErrorMessage);
            Data.Add(nameof(StatusCode), StatusCode);
            Data.Add(nameof(StatusReasonPhrase), StatusReasonPhrase);
        }

        public override string Message => 
            $"Api Service returned a non-success status code when attempting an operation on an image: {StatusCode} - {StatusReasonPhrase}. {ErrorTitle}, {ErrorMessage}";

    }
}
