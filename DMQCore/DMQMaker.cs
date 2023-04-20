using Serilog;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Drawing.Processing;
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace DMQCore
{
    public class DMQMaker
    {
        public ISImage? FinalImage { get; private set; }

        private ISImage? uploadImage;
        private ISImage? signature;
        private ISImage? quotes;
        private FontFamily fontFamily;

        public string Text = "";

        public string Font {
            get { return fontFamily.Name; }
            set { LoadFont(value); }
        }
        public int TextFontSize = 30;

        public int FinalSize = 800;
        public int QuotesSize = 85;
        public int SignatureSize = 160;
        public int TextSizeWidth = 600;
        public int TextSizeHeight = 170;

        public int QuotesOffsetX = 0;
        public int QuotesOffsetY = 0;
        public int SignatureOffsetX = 0;
        public int SignatureOffsetY = 0;
        public int TextOffsetX = 0;
        public int TextOffsetY = 0;

        public DMQMaker(bool loadStandardPrerequisites = true)
        {
            if (loadStandardPrerequisites)
                LoadStandardPrerequisites();
        }

        public void LoadStandardPrerequisites()
        {
            Log.Information("Loading Standard Prerequisites");
            Assembly assembly = Assembly.GetExecutingAssembly();
            using Stream? signatureStream = assembly.GetManifestResourceStream("DMQCore.Materials.Signature.png");
            using Stream? quotesStream = assembly.GetManifestResourceStream("DMQCore.Materials.Quotes.png");

            if (signatureStream == null || quotesStream == null)
            {
                const string msg = "Cannot get required materials";
                Log.Fatal(msg);
                throw new FileLoadException(msg);
            }

            signature = new(signatureStream);
            quotes = new(quotesStream);
            LoadFont("", useDefault: true);
        }

        public void Clear(bool full = false)
        {
            Log.Information("Clearing DMQMaker state");
            uploadImage = null;
            FinalImage = null;

            if (full)
            {
                signature = null;
                quotes = null;
                Log.Debug("State cleared fully");
            }
        }

        private void LoadFont(string fontName, bool useDefault = false)
        {
            Log.Debug("Loading font " + fontName);
            if (useDefault || !SystemFonts.TryGet(fontName, out fontFamily))
            {
                if (!useDefault) Log.Error($"Couldn't find font {Font}. Default font is used instead.");
                Assembly assembly = Assembly.GetExecutingAssembly();
                using Stream? fontStream = assembly.GetManifestResourceStream("DMQCore.Materials.times.ttf");
                FontCollection collection = new();
                if (fontStream == null)
                {
                    const string msg = "Cannot get default font";
                    Log.Fatal(msg);
                    throw new FileLoadException(msg);
                }
                fontFamily = collection.Add(fontStream);
            }
        }

        public static List<string> GetFonts()
        {
            var result = new List<string>(SystemFonts.Collection.Families.Count());
            foreach(var family in SystemFonts.Collection.Families)
            {
                result.Add(family.Name);
            }
            return result;
        }

        public void LoadImage(string path)
        {
            Log.Debug("Loading uploaded");
            uploadImage = LoadISImage(path);
        }

        public void LoadImage(Stream stream)
        {
            Log.Debug("Loading uploaded");
            uploadImage = new(stream);
        }

        public void LoadImage(byte[] data)
        {
            Log.Debug("Loading uploaded");
            uploadImage = new(data);
        }

        public void LoadSignature(string path = "Signature.png")
        {
            Log.Debug("Loading signature");
            signature = LoadISImage(path);
        }

        public void LoadQuotes(string path = "Quotes.svg")
        {
            Log.Debug("Loading quotes");
            quotes = LoadISImage(path);
        }

        private ISImage LoadISImage(string path)
        {
            Log.Information("Loading image from path: " + path);
            if (!File.Exists(path))
                throw new ArgumentException("Cannot load image. Invalid path: " + path);

            var image = new ISImage(path);

            return image;
        }

        public void MakeImage()
        {
            if (uploadImage == null || quotes == null || signature == null)
            {
                Log.Warning("Making image without required material");
                return;
            }

            Log.Debug("Making image");

            var textOptions = MakeTextOptions(1.2f, TextFontSize);

            var dsaf = TextMeasurer.Measure(Text, textOptions);
            if (dsaf.Height > 150)
                textOptions = MakeTextOptions(1f, TextFontSize);
            else if (dsaf.Height < 90)
                textOptions = MakeTextOptions(1.2f, TextFontSize + 3);

            var image = new ISImage(new Image<Rgb24>(FinalSize, FinalSize));

            var commonResizeOtions = new ResizeOptions() { Mode = ResizeMode.Pad, Position = AnchorPositionMode.TopLeft, PadColor = Color.White };

            var resUpload = uploadImage.Clone((x) =>
            {
                commonResizeOtions.Size = new Size(FinalSize);
                x.Resize(commonResizeOtions);
            });
            var resSignature = signature.Clone((x) =>
            {
                commonResizeOtions.Size = new Size(SignatureSize);
                x.Resize(commonResizeOtions);
            });
            var resQuotes = quotes.Clone((x) =>
            {
                commonResizeOtions.Size = new Size(QuotesSize);
                x.Resize(commonResizeOtions);
            });
            
            image.Image.Mutate((x) =>
            { x
                .DrawImage(resUpload.Image, new Point(0, 0), 1f)
                .DrawText(textOptions, Text, Color.Black)
                .DrawImage(resQuotes.Image, new Point(20 + QuotesOffsetX, 510 + QuotesOffsetY), 0.2f)
                .DrawImage(resSignature.Image, new Point(530 + SignatureOffsetX, 680 + SignatureOffsetY), 1f)
            ;});

            FinalImage = image;
        }

        private TextOptions MakeTextOptions(float lineSpacing, int fontSize)
        {
            var font = fontFamily.CreateFont(fontSize);
            return new TextOptions(font)
            {
                Dpi = 72,
                KerningMode = KerningMode.Standard,
                WrappingLength = TextSizeWidth,
                Origin = new System.Numerics.Vector2(120 + TextOffsetX, 610 + TextOffsetY),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                LineSpacing = lineSpacing,
            };
        }
    }
}