namespace Lambda
{
    public struct Event
    {
        public string ImageBase64 { get; set; }
        public string Text { get; set; }
        public int[][]? Resolutions { get; set; }
    }
}
