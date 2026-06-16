using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Valve.Newtonsoft.Json.Linq;

namespace xsoverlay_tweak.Utils
{
    internal class Update
    {
        private const string repo = "chaixshot/xsoverlay-tweak";
        private const string GitHubRepoUrl = $"https://github.com/{repo}";
        private const string GitHubReleasesUrl = $"https://github.com/{repo}/releases";
        private const string GitHubLatestReleaseApi = $"https://api.github.com/repos/{repo}/releases/latest";

        private static async Task<string> GetLatestVersionAsync()
        {
            using HttpClient client = new()
            {
                Timeout = TimeSpan.FromSeconds(3)
            };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("xsoverlay-tweak");
            string response = await client.GetStringAsync(GitHubLatestReleaseApi);
            JObject responseData = JObject.Parse(response);

            string latestVersionRaw = responseData["tag_name"]?.ToString() ?? string.Empty;
            string latestVersion = string.IsNullOrEmpty(latestVersionRaw)
                ? string.Empty
                : Regex.Replace(latestVersionRaw, "[^0-9.]", string.Empty);

            return latestVersion;
        }

        public static async void CheckForUpdate(bool doReturn)
        {
            string currentVersion = MyPluginInfo.PLUGIN_VERSION;
            string latestVersion;
            try
            {
                latestVersion = await GetLatestVersionAsync();
            }
            catch (Exception ex)
            {
                if (doReturn)
                    Notification.Send(MyPluginInfo.PLUGIN_NAME, $"Update Check Failed:\n\"{ex.Message}\"");
                return;
            }

            if (!string.IsNullOrEmpty(latestVersion) && latestVersion != currentVersion)
                Notification.Send(MyPluginInfo.PLUGIN_NAME, $"A new version of {MyPluginInfo.PLUGIN_NAME} <b>{latestVersion}</b> is available.\nYou are currently using version <b>{MyPluginInfo.PLUGIN_VERSION}</b>.");
            else if (doReturn)
                Notification.Send(MyPluginInfo.PLUGIN_NAME, $"Your <b>{MyPluginInfo.PLUGIN_NAME}</b> is the lasted version.");
        }

        public static void OpenGitHubPage()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = GitHubRepoUrl,
                    UseShellExecute = true
                });
                Notification.Send(MyPluginInfo.PLUGIN_NAME, "Opening GitHub page in default browser...");
            }
            catch (Exception ex)
            {
                Notification.Send(MyPluginInfo.PLUGIN_NAME, $"Failed to open GitHub page:\n\"{ex.Message}\"");
            }
        }
    }
}
