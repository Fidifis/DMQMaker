using DMQCore;
using Microsoft.Win32;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace DMQEditor
{
    public partial class MainWindow : Window
    {
        private readonly object threadSync = new();
        private CancellationTokenSource refreshCts = new();
        private DMQMaker Maker;

        private Image? image;
        private Image? finalImage;

        private string dmqText = "";
        private DMQParams dmqParams = new();

        string imageDiractory = "";
        string imageName = "";

        string? font = null;

        bool dontChange = false;

        bool hasChanged = false;

        public MainWindow()
        {
            string[] args = Environment.GetCommandLineArgs();
            sbyte verbosity =
                #if DEBUG
                1
                #else
                -1
                #endif
            ;

            if (args.Length >= 2 && args[1].ToLower().Contains("-v"))
            {
                if (sbyte.TryParse(args[1].ToLower().Trim().Replace("-v", ""), out sbyte converted))
                {
                    verbosity = converted;
                }
                else
                {
                    Console.WriteLine("-v parametr is incorect. Expected -v0 or -v1");
                }
            }

            CreateLogger(verbosity);
            Log.Verbose("MainWindow Constructor");

            Maker = new();

            dontChange = true;
            InitializeComponent();
            InitValues();
            dontChange = false;

            List<string> systemFonts = ["", .. DMQMaker.GetFonts()];
            FontsDropdown.ItemsSource = systemFonts;
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            Log.Verbose("MainWindow Loaded");
            UpdateCheck.CheckForUpdateAsync();
        }

        static void CreateLogger(sbyte verbosity = 1)
        {
            if (verbosity == -1)
                return;

            var logConfig = new LoggerConfiguration()
            .WriteTo.File("log.txt");

            logConfig = verbosity switch
            {
                0 => logConfig.MinimumLevel.Verbose(),
                1 => logConfig.MinimumLevel.Debug(),
                2 => logConfig.MinimumLevel.Information(),
                3 => logConfig.MinimumLevel.Warning(),
                4 => logConfig.MinimumLevel.Error(),
                _ => logConfig.MinimumLevel.Debug(),
            };
            Log.Logger = logConfig.CreateLogger();
        }

        private void InitValues()
        {
            FontSizeBox.Text = dmqParams.TextSize.ToString();
            FontSizeSlide.Value = dmqParams.TextSize;

            QuoteOffsetXBox.Text = dmqParams.QuotesSize.ToString();
            QuotesOffsetXSlide.Value = dmqParams.QuotesSize;

            QuoteOffsetYBox.Text = dmqParams.TextAreaPercentage.ToString();
            QuotesOffsetYSlide.Value = dmqParams.TextAreaPercentage;

            SignatureOffsetXBox.Text = dmqParams.SignatureSize.ToString();
            SignatureOffsetXSlide.Value = dmqParams.SignatureSize;

            SignatureOffsetYBox.Text = dmqParams.SignatureOffsetY.ToString();
            SignatureOffsetYSlide.Value = dmqParams.SignatureOffsetY;

            TextOffsetXBox.Text = dmqParams.TextPaddingX.ToString();
            TextOffsetXSlide.Value = dmqParams.TextPaddingX;

            TextOffsetYBox.Text = dmqParams.TextOffsetY.ToString();
            TextOffsetYSlide.Value = dmqParams.TextOffsetY;

            TextResolutionX.Text = dmqParams.ResolutionX.ToString();
            TextResolutionY.Text = dmqParams.ResolutionY.ToString();
        }

        private void OpenBtn_Click(object sender, RoutedEventArgs e)
        {
            Log.Verbose("Open button clicked");
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Select an image file",
                    Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.tif;*.tga;*.webp)|" +
                  "*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.tif;*.tga;*.webp|" +
                  "All files (*.*)|*.*"
                };
                if (openFileDialog.ShowDialog() != true)
                {
                    Log.Verbose("Open file dialog canceled");
                    return;
                }
                string path = openFileDialog.FileName;
                Log.Verbose("Open file dialog result " + path);

                imageDiractory = Path.GetDirectoryName(imageDiractory) ?? "";
                imageName = Path.GetFileName(imageName) ?? "";

                image = Image.Load(path);
                Refresh();
            }
            catch (Exception ex)
            {
                string msg = $"Fail on image load. {ex.Message}";
                Log.Fatal(msg);
                MessageBox.Show(msg);
            }
        }

        private void UpdatePreview()
        {
            Log.Verbose("Updating preview");
            if (finalImage == null)
            {
                Log.Debug("Cannot update. No final image");
                return;
            }

            using var stream = new MemoryStream();
            finalImage.Save(stream, PngFormat.Instance);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            ImagePreview.Dispatcher.BeginInvoke(() => ImagePreview.Source = bitmap);
        }

        private void InputText_Changed(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            Log.Verbose("Text changed");
            dmqText = InputText.Text == "" ? " " : InputText.Text;
            Refresh();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            Log.Verbose("Save button clicked");
            if (finalImage == null)
            {
                const string msg = "Image cannot be saved, because it is empty.";
                Log.Error(msg);
                MessageBox.Show(msg);
                return;
            }

            var saveFileDialog = new SaveFileDialog()
            {
                InitialDirectory = imageDiractory,
                FileName = imageName,
                OverwritePrompt = true,
                Title = "Save Image",
                Filter =
                "Joint Photographic Experts Group (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                "Portable Network Graphics (*.png)|*.png|" +
                "All files|*.*"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                MakeImage();

                Log.Information("Saving to file:" + saveFileDialog.FileName);
                finalImage.Save(saveFileDialog.FileName);
            }
        }

        private void MakeImage()
        {
            if (image == null)
            {
                Log.Debug("Cannot make image. Nothing uploaded");
                return;
            }
            finalImage = Maker.MakeImage(image, dmqText, dmqParams, new FontParam() { FontName = font });
        }


        private void Refresh()
        {
            if (hasChanged)
                return;
            hasChanged = true;

            refreshCts.Cancel();
            refreshCts = new CancellationTokenSource();
            var token = refreshCts.Token;

            Task.Run(() =>
            {
                if (token.IsCancellationRequested)
                    return;

                lock (threadSync)
                {
                    if (token.IsCancellationRequested)
                        return;

                    hasChanged = false;
                    MakeImage();
                    UpdatePreview();
                }
            }, token);
        }

        private void FontsDropdown_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Log.Verbose("Font changed");
            font = FontsDropdown.SelectedValue.ToString()!;
            Refresh();
        }

        private void FontSizeSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dontChange)
                return;

            FontSizeBox.Text = ((int)FontSizeSlide.Value).ToString();
            Refresh();
        }

        private void QuotesOffsetXSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dontChange)
                return;

            QuoteOffsetXBox.Text = (QuotesOffsetXSlide.Value).ToString();
            Refresh();
        }

        private void QuotesOffsetYSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dontChange)
                return;

            QuoteOffsetYBox.Text = QuotesOffsetYSlide.Value.ToString();
            Refresh();
        }

        private void SignatureOffsetXSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dontChange)
                return;

            SignatureOffsetXBox.Text = (SignatureOffsetXSlide.Value).ToString();
            Refresh();
        }

        private void SignatureOffsetYSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dontChange)
                return;

            SignatureOffsetYBox.Text = (SignatureOffsetYSlide.Value).ToString();
            Refresh();
        }

        private void TextOffsetXSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dontChange)
                return;

            TextOffsetXBox.Text = (TextOffsetXSlide.Value).ToString();
            Refresh();
        }

        private void TextOffsetYSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dontChange)
                return;

            TextOffsetYBox.Text = (TextOffsetYSlide.Value).ToString();
            Refresh();
        }

        private void FontSizeBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (dontChange)
                return;

            dontChange = true;
            if (int.TryParse(FontSizeBox.Text, out int asInt))
            {
                Log.Verbose("Font size changed");
                dmqParams.TextSize = asInt;
                FontSizeSlide.Value = asInt;
                Refresh();
            }
            else
                Log.Warning("Invalid input for font size. Not a number.");

            dontChange = false;
        }

        private void QuoteOffsetXBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (dontChange)
                return;

            dontChange = true;
            if (float.TryParse(QuoteOffsetXBox.Text, out float asFloat))
            {
                Log.Verbose("Quotes offset X changed");
                dmqParams.QuotesSize = asFloat;
                QuotesOffsetXSlide.Value = asFloat;
                Refresh();
            }
            else
                Log.Warning("Invalid input for quotes offset X. Not a number.");

            dontChange = false;
        }

        private void QuoteOffsetYBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (dontChange)
                return;

            dontChange = true;
            if (float.TryParse(QuoteOffsetYBox.Text, out float asFloat))
            {
                Log.Verbose("Quotes offset Y changed");
                dmqParams.TextAreaPercentage = asFloat;
                QuotesOffsetYSlide.Value = asFloat;
                Refresh();
            }
            else
                Log.Warning("Invalid input for quotes offset Y. Not a number.");

            dontChange = false;
        }

        private void SignatureOffsetXBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (dontChange)
                return;

            dontChange = true;
            if (float.TryParse(SignatureOffsetXBox.Text, out float asFloat))
            {
                Log.Verbose("Signature offset X changed");
                dmqParams.SignatureSize = asFloat;
                SignatureOffsetXSlide.Value = asFloat;
                Refresh();
            }
            else
                Log.Warning("Invalid input for signature offset X. Not a number.");

            dontChange = false;
        }

        private void SignatureOffsetYBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (dontChange)
                return;

            dontChange = true;
            if (float.TryParse(SignatureOffsetYBox.Text, out float asFloat))
            {
                Log.Verbose("Signature offset Y changed");
                dmqParams.SignatureOffsetY = asFloat;
                SignatureOffsetYSlide.Value = asFloat;
                Refresh();
            }
            else
                Log.Warning("Invalid input for signature offset Y. Not a number.");

            dontChange = false;
        }

        private void TextOffsetXBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (dontChange)
                return;

            dontChange = true;
            if (float.TryParse(TextOffsetXBox.Text, out float asFloat))
            {
                Log.Verbose("Text offset X changed");
                dmqParams.TextPaddingX = asFloat;
                TextOffsetXSlide.Value = asFloat;
                Refresh();
            }
            else
                Log.Warning("Invalid input for text offset X. Not a number.");

            dontChange = false;
        }

        private void TextOffsetYBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (dontChange)
                return;

            dontChange = true;
            if (float.TryParse(TextOffsetYBox.Text, out float asFloat))
            {
                Log.Verbose("Text offset Y changed");
                dmqParams.TextOffsetY = asFloat;
                TextOffsetYSlide.Value = asFloat;
                Refresh();
            }
            else
                Log.Warning("Invalid input for text offset Y. Not a number.");

            dontChange = false;
        }

        private void TextResolutionX_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (dontChange)
                return;

            dontChange = true;
            if (int.TryParse(TextResolutionX.Text, out int asInt) && asInt > 0)
            {
                Log.Verbose("Text Resolution X changed");
                dmqParams.ResolutionX = asInt;
                Refresh();
            }
            else
                Log.Warning("Invalid input for text resolution X. Not a valid number.");

            dontChange = false;
        }

        private void TextResolutionY_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (dontChange)
                return;

            dontChange = true;
            if (int.TryParse(TextResolutionY.Text, out int asInt) && asInt > 0)
            {
                Log.Verbose("Text Resolution Y changed");
                dmqParams.ResolutionY = asInt;
                Refresh();
            }
            else
                Log.Warning("Invalid input for text resolution Y. Not a valid number.");

            dontChange = false;
        }

        private void SetSquareBtn_click(object sender, RoutedEventArgs e)
        {
            TextResolutionX.Text = "1080";
            TextResolutionY.Text = "1080";
        }
        private void SetVerticalBtn_click(object sender, RoutedEventArgs e)
        {
            TextResolutionX.Text = "1080";
            TextResolutionY.Text = "1380";
        }
    }
}
