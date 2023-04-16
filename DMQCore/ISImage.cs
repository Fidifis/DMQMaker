using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using System.IO;
using System;

namespace DMQCore
{
    /// <summary>
    /// "ImageSharp Image"
    /// Special class that serves as ImageSharp.Image class wrapper for automatic dispose.
    /// </summary>
    public class ISImage
    {
        internal Image Image { get; private set; }

        internal ISImage(Image image)
        {
            Image = image;
        }

        internal ISImage(string filePath)
            : this(Image.Load(filePath))
        { }

        internal ISImage(Stream stream)
            : this(Image.Load(stream))
        { }

        internal ISImage(byte[] data)
            : this(Image.Load(data))
        { }

        public ISImage Clone() => new(Image.Clone((x) => { }));

        public ISImage Clone(Action<IImageProcessingContext> operation) => new(Image.Clone(operation));

        public string ToBase64String() => Image.ToBase64String(PngFormat.Instance);

        public void CopyToStream(Stream stream) => Image.Save(stream, PngFormat.Instance);

        public void Save(string path) => Image.Save(path);

        ~ISImage()
        {
            Image.Dispose();
        }
    }
}
