namespace TinyPng.ResizeOperations
{
    public class FitResizeOperation : ResizeOperation
    {
        public FitResizeOperation(int width, int height) : base(ResizeType.Fit, width, height) { }
    }
}
