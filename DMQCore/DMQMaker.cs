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
using System.Text.Json;

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

            DefaultQuotes = quotes ?? LoadInternalImage("DMQCore.Materials.Quotes.png");
            DefaultSignature = signature ?? LoadInternalImage("DMQCore.Materials.Signature.png");
            DefaultFont = LoadFont(new FontParam());
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

        private static FontFamily LoadFont(FontParam? font)
        {
            if (font.HasValue && font.Value.FontFilePath != null) {
              Log.Debug($"Loading font from {font.Value.FontFilePath}");
              FontCollection collection = new();
              return collection.Add(font.Value.FontFilePath);
            }

            Log.Debug("Loading system font " + (font.HasValue ? font.Value.FontName : "(no font specified)"));
            FontFamily fontFamily;
            if (!font.HasValue || font.Value.UseBuildIn || font.Value.FontName == null || !SystemFonts.TryGet(font.Value.FontName, out fontFamily))
            {
                if (font.HasValue && !font.Value.UseBuildIn)
                  Log.Error($"Couldn't find font {font.Value.FontName ?? "(no font specified)"}. Default font is used instead.");
                Log.Debug("Loading built-in font");

                Assembly assembly = Assembly.GetExecutingAssembly();
                using Stream? fontStream = assembly.GetManifestResourceStream("DMQCore.Materials.Merriweather.ttf");
                FontCollection collection = new();
                if (fontStream == null)
                {
                    const string msg = "Cannot get default font";
                    Log.Error(msg);
                    throw new FileLoadException(msg);
                }
                fontFamily = collection.Add(fontStream);
            }
            else {
              throw new Exception("No font loaded. This should never happen");
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

        public Image MakeImage(Image image, string text, DMQParams paramz, FontParam? font = null)
        {
            if (text.Length == 0)
            {
                string msg = $"Given text is empty";
                Log.Fatal(msg);
                throw new ArgumentException(msg);
            }

            FontFamily fontFam = LoadFont(font);

            Log.Debug("Making image");
            Log.Debug(JsonSerializer.Serialize(paramz));

            var finalImage = new Image<Rgb24>(paramz.ResolutionX, paramz.ResolutionY);

            var commonResizeOtions = new ResizeOptions() { Mode = ResizeMode.Crop, Position = AnchorPositionMode.Top, PadColor = Color.White };

            var textAreaOriginY = paramz.ResolutionY * (1 - paramz.TextAreaPercentage);

            Log.Debug($"textAreaOrigin {textAreaOriginY}");

            var resImage = image.Clone((x) =>
            {
                commonResizeOtions.Size = new Size(paramz.ResolutionX, (int)textAreaOriginY);
                x.Resize(commonResizeOtions);
            });
            var resSignature = DefaultSignature.Clone((x) =>
            {
                commonResizeOtions.Size = new Size((int)(paramz.ResolutionY / 6f * paramz.SignatureSize), 0);
                x.Resize(commonResizeOtions);
            });
            var resQuotes = DefaultQuotes.Clone((x) =>
            {
                commonResizeOtions.Size = new Size((int)(paramz.ResolutionY / 10f * paramz.QuotesSize), 0);
                x.Resize(commonResizeOtions);
            });

            var textOrigin = new System.Numerics.Vector2(paramz.ResolutionX / 2 + paramz.TextPaddingX, paramz.TextOffsetY + textAreaOriginY + (paramz.ResolutionY - textAreaOriginY) / 2 - resSignature.Height / 2);
            var textOptions = MakeTextOptions(fontFam, textOrigin, paramz);


            var textMeasure = TextMeasurer.MeasureSize(text, textOptions);

            finalImage.Mutate((x) =>
            { x
                .DrawImage(resImage, new Point(0, 0), 1f)
                .Fill(Brushes.Solid(Color.White), new RectangleF(0, textAreaOriginY, paramz.ResolutionX, paramz.ResolutionY - textAreaOriginY))
                .DrawText(textOptions, text, Color.Black)
                .DrawImage(resQuotes, new Point((int)(paramz.ResolutionX / 2f - resQuotes.Width / 2f), (int)(textAreaOriginY - resQuotes.Height / 2f)), 1f)
                .DrawImage(resSignature, new Point((int)(paramz.ResolutionX / 2f - resSignature.Width / 2f), (int)(textOrigin.Y + textMeasure.Height / 2 + 50 + paramz.SignatureOffsetY)), 1f)
            ;});

            return finalImage;
        }

        private static RichTextOptions MakeTextOptions(FontFamily fontFamily, System.Numerics.Vector2 origin, DMQParams paramz)
        {
            var font = fontFamily.CreateFont(paramz.ResolutionY * 0.0012345f * paramz.TextSize);
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
            Log.Verbose("DMQMaker disposed");
            DefaultQuotes.Dispose();
            DefaultSignature.Dispose();
        }
    }
}
