using HarmonyLib;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using XSOverlay.Websockets.API;

namespace xsoverlay_tweak.Utils
{
    internal class GlobalizeJsModule
    {
        [HarmonyPatch(typeof(ApiHandler), "InitializeAPI")]
        [HarmonyPostfix]
        public static void PatchCoreJavaScriptFiles()
        {
            string baseDir = @".\XSOverlay_Data\StreamingAssets\Plugins\Applications\UserInterface\Default\";
            if (!Directory.Exists(baseDir)) return;

            // Relative directory paths to target
            string[] targetDirectories = {
                @"BrowserAddressBar\js",
                @"Keyboard\js",
                @"Notification\js",
                @"Settings\js",
                @"Tooltip\js",
                @"Toolbars\js",
                @"WindowSettings\js",
            };

            foreach (string relativePath in targetDirectories)
            {
                string dirPath = Path.Combine(baseDir, relativePath);

                if (Directory.Exists(dirPath))
                {
                    string[] jsFiles = Directory.GetFiles(dirPath, "*.js", SearchOption.TopDirectoryOnly); // Search for all *.js files inside the directory

                    foreach (string filePath in jsFiles)
                    {
                        GlobalizeAllJsElements(filePath);
                    }
                }
            }
        }

        /// <summary>
        /// Scans a JS file for top-level functions, variables, and imported module namespaces, pushing them to window scope.
        /// </summary>
        private static void GlobalizeAllJsElements(string fullPath)
        {
            if (!File.Exists(fullPath)) return;

            string content = File.ReadAllText(fullPath);

            // Idempotency check: Don't process if our footer is already there
            if (content.Contains("// MOD_GLOBALIZATION_FOOTER")) return;

            // Pattern accounts for optional indentation and export keywords
            string pattern = @"^(?:function|(?:const|let|var)|import\s+\*\s+as)\s+([a-zA-Z0-9_]+)\b";

            MatchCollection matches = Regex.Matches(content, pattern, RegexOptions.Multiline);

            if (matches.Count > 0)
            {
                StringBuilder footer = new();
                footer.AppendLine("\n\n// MOD_GLOBALIZATION_FOOTER");

                for (int i = 0; i < matches.Count; i++)
                {
                    string elementName = matches[i].Groups[1].Value;

                    // Use a live getter/setter definition so changes sync bi-directionally.
                    // Falls back to a standard assignment if defineProperty fails.
                    footer.AppendLine($@"
try {{
    Object.defineProperty(window, '{elementName}', {{
        get: () => {elementName},
        set: (v) => {{ try {{ {elementName} = v; }} catch(e) {{}} }},
        configurable: true,
        enumerable: true
    }});
}} catch(e) {{ window.{elementName} = {elementName}; }}");
                }

                File.WriteAllText(fullPath, content + footer.ToString());
            }
        }
    }
}