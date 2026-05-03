using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Scopable
{
    public class ScopableApp : Application
    {
        [STAThread]
        public static void Main()
        {
            var app = new ScopableApp();
            app.Run(new MainWindow());
        }
    }

    public class MainWindow : Window
    {
        private readonly string _rootPath = @"C:\Users\Archon\Desktop\StillMod";
        private readonly string _menuPath = @"C:\Users\Archon\Desktop\StillMod\StillMenu";
        private readonly string _transcriptDir;
        private readonly string _threadFile;

        private readonly TextBox _chatInput = new TextBox();
        private readonly TextBox _chatOutput = new TextBox();
        private readonly TextBox _psInput = new TextBox();
        private readonly TextBox _psOutput = new TextBox();
        private readonly TextBlock _status = new TextBlock();
        private readonly ComboBox _themeCombo = new ComboBox();

        private string _previousResponseId = string.Empty;
        private string _lastWorkbenchOutput = string.Empty;

        public MainWindow()
        {
            _transcriptDir = Path.Combine(_menuPath, "transcripts");
            _threadFile = Path.Combine(_menuPath, "scopable_current_thread.txt");
            Directory.CreateDirectory(_transcriptDir);

            Title = "Scopable — StillMod Workbench";
            Width = 1440;
            Height = 920;
            MinWidth = 1150;
            MinHeight = 720;

            LoadThreadState();
            BuildUi();
            SetTheme("Parchment");
            LogStatus("Scopable ready.");
        }

        private void BuildUi()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition());
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(140) });

            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            root.ColumnDefinitions.Add(new ColumnDefinition());
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(450) });

            var header = BuildHeader();
            Grid.SetRow(header, 0);
            Grid.SetColumn(header, 0);
            Grid.SetColumnSpan(header, 3);

            var nav = BuildNav();
            Grid.SetRow(nav, 1);
            Grid.SetColumn(nav, 0);

            var chatPanel = BuildChatPanel();
            Grid.SetRow(chatPanel, 1);
            Grid.SetColumn(chatPanel, 1);

            var workbench = BuildWorkbenchPanel();
            Grid.SetRow(workbench, 1);
            Grid.SetColumn(workbench, 2);

            var statusPanel = BuildStatusPanel();
            Grid.SetRow(statusPanel, 2);
            Grid.SetColumn(statusPanel, 0);
            Grid.SetColumnSpan(statusPanel, 3);

            root.Children.Add(header);
            root.Children.Add(nav);
            root.Children.Add(chatPanel);
            root.Children.Add(workbench);
            root.Children.Add(statusPanel);

            Content = root;
        }

        private Border BuildHeader()
        {
            var dock = new DockPanel { Margin = new Thickness(14), LastChildFill = true };
            var title = new TextBlock
            {
                Text = "Scopable Cartographer Desk",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 18, 0)
            };

            _themeCombo.Items.Add("Parchment");
            _themeCombo.Items.Add("Slate");
            _themeCombo.SelectedIndex = 0;
            _themeCombo.Width = 140;
            _themeCombo.Margin = new Thickness(8, 0, 0, 0);
            _themeCombo.SelectionChanged += (_, __) => SetTheme(_themeCombo.SelectedItem?.ToString() ?? "Parchment");

            var right = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            right.Children.Add(new TextBlock { Text = "Theme", VerticalAlignment = VerticalAlignment.Center });
            right.Children.Add(_themeCombo);

            DockPanel.SetDock(right, Dock.Right);
            dock.Children.Add(right);
            dock.Children.Add(title);

            return new Border { Child = dock, CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Padding = new Thickness(8) };
        }

        private Border BuildNav()
        {
            var stack = new StackPanel { Margin = new Thickness(12), VerticalAlignment = VerticalAlignment.Stretch };
            stack.Children.Add(MakeActionButton("Root Check", async () => await RootCheckAsync()));
            stack.Children.Add(MakeActionButton("Bootstrap", async () => await BootstrapAsync()));
            stack.Children.Add(MakeActionButton("Build All", async () => await BuildAllAsync()));
            stack.Children.Add(MakeActionButton("Ask Output", async () => await AskOutputAsync()));
            stack.Children.Add(new Separator { Margin = new Thickness(0, 12, 0, 12) });
            stack.Children.Add(MakeActionButton("Git Init", async () => await RunPowerShellAsync("git init")));
            stack.Children.Add(MakeActionButton("Git Status", async () => await RunPowerShellAsync("git status")));
            stack.Children.Add(MakeActionButton("Git Commit", async () => await RunPowerShellAsync("git add -A; git commit -m \"scopable checkpoint\"")));
            stack.Children.Add(MakeActionButton("Git Push", async () => await RunPowerShellAsync("git push")));

            return new Border { Child = stack, Margin = new Thickness(8), CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1) };
        }

        private Border BuildChatPanel()
        {
            var grid = new Grid { Margin = new Thickness(8) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _chatOutput.IsReadOnly = true;
            _chatOutput.TextWrapping = TextWrapping.Wrap;
            _chatOutput.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _chatOutput.AcceptsReturn = true;
            _chatOutput.FontFamily = new FontFamily("Consolas");

            var sendRow = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            sendRow.ColumnDefinitions.Add(new ColumnDefinition());
            sendRow.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _chatInput.AcceptsReturn = true;
            _chatInput.Height = 90;
            _chatInput.TextWrapping = TextWrapping.Wrap;

            var send = new Button { Content = "Send", Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(14, 8, 14, 8) };
            send.Click += async (_, __) => await SendGptAsync(_chatInput.Text);

            Grid.SetColumn(_chatInput, 0);
            Grid.SetColumn(send, 1);
            sendRow.Children.Add(_chatInput);
            sendRow.Children.Add(send);

            Grid.SetRow(_chatOutput, 0);
            Grid.SetRow(sendRow, 1);
            grid.Children.Add(_chatOutput);
            grid.Children.Add(sendRow);

            return WrapWithTitle("GPT Console", grid);
        }

        private Border BuildWorkbenchPanel()
        {
            var grid = new Grid { Margin = new Thickness(8) };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _psOutput.IsReadOnly = true;
            _psOutput.AcceptsReturn = true;
            _psOutput.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            _psOutput.FontFamily = new FontFamily("Consolas");

            var row = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _psInput.Text = "git status";
            var run = new Button { Content = "Run", Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(14, 8, 14, 8) };
            run.Click += async (_, __) => await RunPowerShellAsync(_psInput.Text);

            Grid.SetColumn(_psInput, 0);
            Grid.SetColumn(run, 1);
            row.Children.Add(_psInput);
            row.Children.Add(run);

            Grid.SetRow(_psOutput, 0);
            Grid.SetRow(row, 1);
            grid.Children.Add(_psOutput);
            grid.Children.Add(row);

            return WrapWithTitle("PowerShell / Git Workbench", grid);
        }

        private Border BuildStatusPanel()
        {
            _status.TextWrapping = TextWrapping.Wrap;
            _status.FontFamily = new FontFamily("Consolas");
            _status.Margin = new Thickness(8);
            var scroll = new ScrollViewer { Content = _status, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            return WrapWithTitle("Status Log", scroll);
        }

        private Border WrapWithTitle(string title, UIElement content)
        {
            var stack = new DockPanel();
            var txt = new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.Bold, Margin = new Thickness(8) };
            DockPanel.SetDock(txt, Dock.Top);
            stack.Children.Add(txt);
            stack.Children.Add(content);
            return new Border { Child = stack, CornerRadius = new CornerRadius(8), BorderThickness = new Thickness(1), Padding = new Thickness(6), Margin = new Thickness(8) };
        }

        private Button MakeActionButton(string label, Func<Task> action)
        {
            var b = new Button { Content = label, Margin = new Thickness(0, 4, 0, 4), Padding = new Thickness(8) };
            b.Click += async (_, __) => await action();
            return b;
        }

        private async Task RootCheckAsync()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Location: {_rootPath}");
            if (Directory.Exists(_rootPath))
            {
                sb.AppendLine("Top files:");
                foreach (var file in Directory.GetFiles(_rootPath).Take(8)) sb.AppendLine(" - " + Path.GetFileName(file));
                sb.AppendLine(File.Exists(Path.Combine(_rootPath, "gradlew.bat")) ? "gradlew.bat exists." : "gradlew.bat not found.");
            }
            else sb.AppendLine("Root path not found.");
            await RunPowerShellAsync("git rev-parse --is-inside-work-tree", sb.ToString());
        }

        private async Task BootstrapAsync()
        {
            var script = @"
if (-not (Test-Path .git)) { git init }
$gitIgnore = @'
.env
*.env
*.key
*.pem
*.p12
secrets*
config/secrets*
OPENAI_API_KEY*
.gradle/
build/
out/
.idea/
*.user
StillMenu/backups/
StillMenu/transcripts/
StillMenu/*.settings
StillMenu/scopable_current_thread.txt
StillMenu/*crash.log
StillMenu/compile_*.log
'@
Set-Content -Path .gitignore -Value $gitIgnore -Encoding UTF8
if (Test-Path .\gradlew.bat) { Write-Output 'gradlew.bat exists.' } else { Write-Output 'gradlew.bat missing.' }
git status
";
            await RunPowerShellAsync(script);
        }

        private async Task BuildAllAsync()
        {
            var command = "if (Test-Path .\\gradlew.bat) { .\\gradlew.bat build --stacktrace --no-daemon } else { Write-Output 'gradlew.bat missing; build skipped.' }";
            await RunPowerShellAsync(command);
        }

        private async Task AskOutputAsync()
        {
            if (string.IsNullOrWhiteSpace(_lastWorkbenchOutput))
            {
                LogStatus("No recent workbench output to send.");
                return;
            }
            await SendGptAsync("Analyze this output and recommend next steps:\n\n" + _lastWorkbenchOutput);
        }

        private async Task RunPowerShellAsync(string command, string prefix = "")
        {
            var result = await RunProcessAsync("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{command.Replace("\"", "\\\"")}\"", _rootPath);
            var text = (prefix + "\n" + result).Trim();
            _psOutput.Text = text;
            _lastWorkbenchOutput = text;
            LogStatus("Workbench command completed.");
        }

        private async Task SendGptAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt)) return;
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                LogStatus("OPENAI_API_KEY is not set.");
                return;
            }

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

            var payload = new Dictionary<string, object>();
            payload["model"] = "gpt-4.1-mini";
            payload["input"] = prompt;
            if (!string.IsNullOrWhiteSpace(_previousResponseId)) payload["previous_response_id"] = _previousResponseId;

            var serializer = new JavaScriptSerializer();
            var json = serializer.Serialize(payload);
            var resp = await client.PostAsync("https://api.openai.com/v1/responses", new StringContent(json, Encoding.UTF8, "application/json"));
            var raw = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                LogStatus("GPT request failed: " + raw);
                return;
            }

            var map = serializer.Deserialize<Dictionary<string, object>>(raw);
            if (map != null && map.ContainsKey("id") && map["id"] != null) _previousResponseId = map["id"].ToString() ?? string.Empty;
            File.WriteAllText(_threadFile, _previousResponseId);

            var text = ExtractOutputText(raw, map);
            _chatOutput.Text += "\nYou: " + prompt + "\nGPT: " + text + "\n";
            _chatInput.Text = string.Empty;
            PersistTranscript(prompt, text);
            LogStatus("GPT response received.");
        }

        private static string ExtractOutputText(string raw, Dictionary<string, object> map)
        {
            if (map != null && map.ContainsKey("output_text") && map["output_text"] != null)
            {
                var direct = map["output_text"].ToString();
                if (!string.IsNullOrWhiteSpace(direct)) return direct;
            }

            var matches = Regex.Matches(raw, "\"text\"\s*:\s*\"([^\"]*)\"");
            if (matches.Count == 0) return "No text returned.";

            var lines = new List<string>();
            foreach (Match m in matches)
            {
                var val = m.Groups[1].Value.Replace("\\n", "\n").Replace("\\"", """);
                if (!string.IsNullOrWhiteSpace(val)) lines.Add(val);
            }
            return lines.Count > 0 ? string.Join("\n", lines) : "No text returned.";
        }

        private void PersistTranscript(string prompt, string response)
        {
            Directory.CreateDirectory(_transcriptDir);
            var file = Path.Combine(_transcriptDir, "transcript_" + DateTime.Now.ToString("yyyyMMdd") + ".log");
            File.AppendAllText(file, $"[{DateTime.Now:O}]\nYou: {prompt}\nGPT: {response}\n\n");
        }

        private void LoadThreadState()
        {
            if (File.Exists(_threadFile)) _previousResponseId = File.ReadAllText(_threadFile).Trim();
        }

        private void LogStatus(string message)
        {
            _status.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
        }

        private void SetTheme(string theme)
        {
            if (theme == "Slate")
            {
                Background = new SolidColorBrush(Color.FromRgb(31, 35, 41));
                Foreground = Brushes.Gainsboro;
            }
            else
            {
                Background = new SolidColorBrush(Color.FromRgb(244, 232, 209));
                Foreground = new SolidColorBrush(Color.FromRgb(50, 38, 18));
            }
        }

        private static async Task<string> RunProcessAsync(string fileName, string args, string workingDir)
        {
            var psi = new ProcessStartInfo(fileName, args)
            {
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p == null) return "Failed to start process.";
            string output = await p.StandardOutput.ReadToEndAsync();
            string error = await p.StandardError.ReadToEndAsync();
            await p.WaitForExitAsync();
            return (output + "\n" + error).Trim();
        }
    }
}
