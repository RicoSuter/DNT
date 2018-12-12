namespace TinyPng.ResizeOperations
{
    public class ScaleHeightResizeOperation : ResizeOperation
    {
        public ScaleHeightResizeOperation(int height) : base(ResizeType.Scale, null, height) { }
    }
}
