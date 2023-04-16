using DMQCore;
using Microsoft.Win32;
using Serilog;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace DMQEditor
{
    public partial class MainWindow : Window
    {
        private DMQMaker Maker;

        string imageDiractory = "";
        string imageName = "";

        public MainWindow()
        {
            string[] args = Environment.GetCommandLineArgs();
            byte verbosity =
                #if DEBUG
                1
                #else
                2
                #endif
            ;

            if (args.Length >= 2 && args[1].ToLower().Contains("-v"))
            {
                if (byte.TryParse(args[1].ToLower().Trim().Replace("-v", ""), out byte converted))
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
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            Log.Verbose("MainWindow Loaded");
            UpdateCheck.CheckForUpdateAsync();
        }

        static void CreateLogger(byte verbosity = 1)
        {
            var logConfig = new LoggerConfiguration()
            .WriteTo.File("log.txt");

            switch (verbosity)
            {
                case 0:
                    logConfig = logConfig.MinimumLevel.Verbose();
                    break;
                case 1:
                    logConfig = logConfig.MinimumLevel.Debug();
                    break;
                case 2:
                    logConfig = logConfig.MinimumLevel.Information();
                    break;
                case 3:
                    logConfig = logConfig.MinimumLevel.Warning();
                    break;
            }

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
                    Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.tif;*.tga;*.webp;*.psd;*.dib;*.ico;*.svg)|" +
                  "*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tiff;*.tif;*.tga;*.webp;*.psd;*.dib;*.ico;*.svg|" +
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
                Maker.MakeImage();
                UpdatePreview();
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
            Log.Debug("Updating preview");
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

            ImagePreview.Source = bitmap;
        }

        private void InputText_Changed(object sender, TextChangedEventArgs e)
        {
            Log.Verbose("Text changed");
            Maker.Text = InputText.Text;
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
                "Portable Network Graphics (*.png)|*.png|" +
                "Joint Photographic Experts Group (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                "All files|*.*"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                Maker.MakeImage();

                Log.Information("Saving to file:" + saveFileDialog.FileName);
                Maker.FinalImage.Save(saveFileDialog.FileName);
            }
        }

        private void RefreshBtn_Click(object sender, RoutedEventArgs e)
        {
            Maker.MakeImage();
            UpdatePreview();
        }
    }
}
