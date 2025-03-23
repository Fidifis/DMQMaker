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
    public sealed class DMQMaker : IDisposable
    {
        private readonly Image DefaultQuotes;
        private readonly Image DefaultSignature;
        private readonly FontFamily DefaultFont;

        public DMQMaker(Image? quotes = null, Image? signature = null)
        {
            Log.Verbose("Invoke DMQMaker Constructor");

            DefaultQuotes = quotes switch
            {
                null => LoadInternalImage("DMQCore.Materials.Quotes.png"),
                _ => quotes,
            };
            DefaultSignature = signature switch
            {
                null => LoadInternalImage("DMQCore.Materials.Signature.png"),
                _ => signature,
            };
            DefaultFont = LoadFont(null);
        }

        private static Image LoadInternalImage(string internalPath)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Log.Debug($"Image not supplied. Loading built-in {internalPath}");
            using Stream? quotesStream = assembly.GetManifestResourceStream(internalPath);
            if (quotesStream == null)
            {
                string msg = $"Cannot get built-in image {internalPath}";
                Log.Fatal(msg);
                throw new FileLoadException(msg);
            }
            return Image.Load(quotesStream);
        }

        private static FontFamily LoadFont(string? fontName)
        {
            Log.Debug("Loading font " + fontName);
            if (fontName == null || !SystemFonts.TryGet(fontName, out FontFamily fontFamily))
            {
                if (fontName != null) Log.Error($"Couldn't find font {fontName}. Default font is used instead.");
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
            return fontFamily;
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

        public Image MakeImage(Image image, string text, DMQParams paramz, string? font = null)
        {
            if (text.Length == 0)
            {
                string msg = $"Given text is empty";
                Log.Fatal(msg);
                throw new ArgumentException(msg);
            }

            FontFamily fontFam = LoadFont(font);

            Log.Debug("Making image");

            var textOrigin = new System.Numerics.Vector2(paramz.ResolutionX / 2 * paramz.TextPaddingX, paramz.ResolutionY / 2 * paramz.TextPaddingY);
            var textOptions = MakeTextOptions(fontFam, textOrigin, paramz);

            //var dsaf = TextMeasurer.MeasureSize(Text, textOptions);
            //if (dsaf.Height > 150)
            //    textOptions = MakeTextOptions(1f, TextFontSize);
            //else if (dsaf.Height < 90)
            //    textOptions = MakeTextOptions(1.2f, TextFontSize + 3);

            var finalImage = new Image<Rgb24>(paramz.ResolutionX, paramz.ResolutionY);

            var commonResizeOtions = new ResizeOptions() { Mode = ResizeMode.Pad, Position = AnchorPositionMode.TopLeft, PadColor = Color.White };

            var resImage = image.Clone((x) =>
            {
                commonResizeOtions.Size = new Size(paramz.ResolutionX);
                x.Resize(commonResizeOtions);
            });
            var resSignature = DefaultSignature.Clone((x) =>
            {
                commonResizeOtions.Size = new Size((int)(paramz.ResolutionX / 5f * paramz.SignatureSize));
                x.Resize(commonResizeOtions);
            });
            var resQuotes = DefaultQuotes.Clone((x) =>
            {
                commonResizeOtions.Size = new Size((int)(paramz.ResolutionX / 10f * paramz.QuotesSize));
                x.Resize(commonResizeOtions);
            });

            var textMeasure = TextMeasurer.MeasureSize(text, textOptions);

            finalImage.Mutate((x) =>
            { x
                .DrawImage(resImage, new Point(0, 0), 1f)
                .DrawText(textOptions, text, Color.Black)
                .DrawImage(resQuotes, new Point((int)(paramz.ResolutionX / 2f - resQuotes.Width / 2f), (int)(paramz.ResolutionY * (1 - paramz.TextAreaPercentage) - resQuotes.Height / 2f)), 1f)
                .DrawImage(resSignature, new Point((int)(paramz.ResolutionX / 2f - resSignature.Width / 2f), (int)(textOrigin.Y + textMeasure.Height)), 1f)
            ;});

            return finalImage;
        }

        private static RichTextOptions MakeTextOptions(FontFamily fontFamily, System.Numerics.Vector2 origin, DMQParams paramz)
        {
            var font = fontFamily.CreateFont(paramz.TextSize);
            return new RichTextOptions(font)
                {
                    Dpi = 72,
                    KerningMode = KerningMode.Standard,
                    WrappingLength = paramz.ResolutionX * (1 - paramz.TextPaddingX),
                    Origin = origin,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    LineSpacing = paramz.LineSpacing,
                    TextAlignment = TextAlignment.Center,
                };
        }

        public void Dispose()
        {
            DefaultQuotes.Dispose();
            DefaultSignature.Dispose();
        }
    }
}