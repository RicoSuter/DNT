namespace TinyPng.ResizeOperations
{
    public class ResizeOperation
    {
        public ResizeOperation(ResizeType type, int width, int height)
        {
            Method = type;
            Width = width;
            Height = height;
        }

        internal ResizeOperation(ResizeType type, int? width, int? height)
        {
            Method = type;
            Width = width;
            Height = height;
        }

        public int? Width { get; }
        public int? Height { get; }
        public ResizeType Method { get; }
    }

    public enum ResizeType
    {
        Fit,
        Scale,
        Cover
    }
}
