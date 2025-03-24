using Serilog;
using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Windows;

namespace DMQEditor
{
    internal class UpdateCheck
    {
        public static async void CheckForUpdateAsync(bool forced = false)
        {
            Log.Information("Checking updates");

            var v = Assembly.GetExecutingAssembly().GetName().Version;
            if (v == null)
            {
                Log.Error("Failed to get assembly version");
                return;
            }

            string version = "v" + TrimToThreeDigitVersion(v.ToString());
            string content;

            try
            {
                using var client = new WebClient();
                client.Headers.Add(HttpRequestHeader.UserAgent, "DMQMaker_webclient");
                Log.Debug("Downloading github api info");
                content = await client.DownloadStringTaskAsync(new Uri("https://api.github.com/repos/Fidifis/DMQMaker/releases/latest"));
            }
            catch (WebException ex)
            {
                // Probbably no internet connection - dont show error message if not forcced
                if (forced)
                {
                    string msg = "Error when trying to check latest version. Check your internet connection." +
                        Environment.NewLine + ex.Message;
                    Log.Error(msg);
                    MessageBox.Show(msg,
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }
            catch (Exception ex)
            {
                string msg = "Error when trying to check latest version" + Environment.NewLine + ex.Message;
                Log.Error(msg);
                MessageBox.Show(msg,
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string latestVersion = GetValue(content, "tag_name");
            if (version != latestVersion)
            {
                Log.Information("New version available " + latestVersion);
                var result = MessageBox.Show("A newer version is available. " + Environment.NewLine + "Do you want to download it?", "Update", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        Log.Debug("Starting browser");
                        Process.Start(new ProcessStartInfo("https://github.com/Fidifis/DMQMaker/releases/latest") { UseShellExecute = true });
                    }
                    catch (Exception ex)
                    {
                        string msg = "Failed to open web browser." + Environment.NewLine + ex.Message;
                        Log.Error(msg);
                        MessageBox.Show(msg,
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else if (forced)
            {
                Log.Debug("No updates");
                MessageBox.Show("No updates");
            }
        }

        private static string GetValue(string content, string key)
        {
            int start, end;
            start = content.IndexOf(key, 0);
            end = content.IndexOf(",", start);
            return content[start..end].Split(':')[1].Trim().Replace("\"", "");
        }

        private static string TrimToThreeDigitVersion(string version)
        {
            var splited = version.Split('.');
            if (splited.Length <= 3)
                return version;

            string trimed = "";
            for (int i = 0; i < 3; i++)
            {
                if (i != 0)
                    trimed += '.';
                trimed += splited[i];
            }
            return trimed;
        }
    }
}
