﻿using DMQCore;
using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DMQEditor
{
    public partial class MainWindow : Window
    {
        private readonly object threadSync = new();
        private DMQMaker Maker;

        string imageDiractory = "";
        string imageName = "";

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
            dontChange = false;

            List<string> systemFonts = new() { "" };
            systemFonts.AddRange(DMQMaker.GetFonts());
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

                Maker.LoadImage(path);
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
            if (Maker.FinalImage == null)
            {
                Log.Debug("Cannot update. No final image");
                return;
            }

            using var stream = new MemoryStream();
            Maker.FinalImage.CopyToStream(stream);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            ImagePreview.Dispatcher.BeginInvoke(() => ImagePreview.Source = bitmap);
        }

        private void InputText_Changed(object sender, TextChangedEventArgs e)
        {
            Log.Verbose("Text changed");
            Maker.Text = InputText.Text;
            Refresh();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            Log.Verbose("Save button clicked");
            if (Maker.FinalImage == null)
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
                Maker.MakeImage();

                Log.Information("Saving to file:" + saveFileDialog.FileName);
                Maker.FinalImage.Save(saveFileDialog.FileName);
            }
        }

        private void Refresh()
        {
            if (hasChanged)
                return;
            hasChanged = true;
            Task.Run(() =>
            {
                lock (threadSync)
                {
                    hasChanged = false;
                    Maker.MakeImage();
                    UpdatePreview();
                }
            });
        }

        private void FontsDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Log.Verbose("Font changed");
            Maker.Font = FontsDropdown.SelectedValue.ToString()!;
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

            QuoteOffsetXBox.Text = ((int)QuotesOffsetXSlide.Value).ToString();
            Refresh();
        }

        private void QuotesOffsetYSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dontChange)
                return;

            QuoteOffsetYBox.Text = ((int)QuotesOffsetYSlide.Value).ToString();
            Refresh();
        }

        private void SignatureOffsetXSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dontChange)
                return;

            SignatureOffsetXBox.Text = ((int)SignatureOffsetXSlide.Value).ToString();
            Refresh();
        }

        private void SignatureOffsetYSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dontChange)
                return;

            SignatureOffsetYBox.Text = ((int)SignatureOffsetYSlide.Value).ToString();
            Refresh();
        }

        private void TextOffsetXSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dontChange)
                return;

            TextOffsetXBox.Text = ((int)TextOffsetXSlide.Value).ToString();
            Refresh();
        }

        private void TextOffsetYSlide_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (dontChange)
                return;

            TextOffsetYBox.Text = ((int)TextOffsetYSlide.Value).ToString();
            Refresh();
        }

        private void FontSizeBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            dontChange = true;
            if (int.TryParse(FontSizeBox.Text, out int asInt))
            {
                Log.Verbose("Font size changed");
                Maker.TextFontSize = asInt;
                FontSizeSlide.Value = asInt;
                Refresh();
            }
            else
                Log.Warning("Invalid input for font size. Not a number.");

            dontChange = false;
        }

        private void QuoteOffsetXBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            dontChange = true;
            if (int.TryParse(QuoteOffsetXBox.Text, out int asInt))
            {
                Log.Verbose("Quotes offset X changed");
                Maker.QuotesOffsetX = asInt;
                QuotesOffsetXSlide.Value = asInt;
                Refresh();
            }
            else
                Log.Warning("Invalid input for quotes offset X. Not a number.");

            dontChange = false;
        }

        private void QuoteOffsetYBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            dontChange = true;
            if (int.TryParse(QuoteOffsetYBox.Text, out int asInt))
            {
                Log.Verbose("Quotes offset Y changed");
                Maker.QuotesOffsetY = asInt;
                QuotesOffsetYSlide.Value = asInt;
                Refresh();
            }
            else
                Log.Warning("Invalid input for quotes offset Y. Not a number.");

            dontChange = false;
        }

        private void SignatureOffsetXBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            dontChange = true;
            if (int.TryParse(SignatureOffsetXBox.Text, out int asInt))
            {
                Log.Verbose("Signature offset X changed");
                Maker.SignatureOffsetX = asInt;
                SignatureOffsetXSlide.Value = asInt;
                Refresh();
            }
            else
                Log.Warning("Invalid input for signature offset X. Not a number.");

            dontChange = false;
        }

        private void SignatureOffsetYBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            dontChange = true;
            if (int.TryParse(SignatureOffsetYBox.Text, out int asInt))
            {
                Log.Verbose("Signature offset Y changed");
                Maker.SignatureOffsetY = asInt;
                SignatureOffsetYSlide.Value = asInt;
                Refresh();
            }
            else
                Log.Warning("Invalid input for signature offset Y. Not a number.");

            dontChange = false;
        }

        private void TextOffsetXBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            dontChange = true;
            if (int.TryParse(TextOffsetXBox.Text, out int asInt))
            {
                Log.Verbose("Text offset X changed");
                Maker.TextOffsetX = asInt;
                TextOffsetXSlide.Value = asInt;
                Refresh();
            }
            else
                Log.Warning("Invalid input for text offset X. Not a number.");

            dontChange = false;
        }

        private void TextOffsetYBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            dontChange = true;
            if (int.TryParse(TextOffsetYBox.Text, out int asInt))
            {
                Log.Verbose("Text offset Y changed");
                Maker.TextOffsetY = asInt;
                TextOffsetYSlide.Value = asInt;
                Refresh();
            }
            else
                Log.Warning("Invalid input for text offset Y. Not a number.");

            dontChange = false;
        }
    }
}
