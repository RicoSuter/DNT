namespace TinyPng.ResizeOperations
{
    public class ScaleWidthResizeOperation : ResizeOperation
    {
        public ScaleWidthResizeOperation(int width) : base (ResizeType.Scale, width, null) { }
    }
}
