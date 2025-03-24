namespace DMQCore
{
    public struct DMQParams
    {
        public float TextOffsetY { get; set; } = 0f;
        public int TextSize { get; set; } = 27;
        public float SignatureOffsetY { get; set; } = 0f;
        public float SignatureSize { get; set; } = 1f;
        public float QuotesSize { get; set; } = 1f;
        public float TextAreaPercentage { get; set; } = 0.4f;
        public float TextPaddingX { get; set; } = 0.1f;
        public float TextPaddingY { get; set; } = 0.1f;
        public float LineSpacing { get; set; } = 1.5f;
        public int ResolutionX { get; set; } = 1080;
        public int ResolutionY { get; set; } = 1080;

        public DMQParams() { }
    }
}
