namespace Lambda
{
    public struct Event
    {
        public string text { get; set; }
        public string s3Bucket { get; set; }
        public string s3Key { get; set; }
        public string resultS3Key { get; set; }
        public int[] resolution { get; set; }
    }
}
