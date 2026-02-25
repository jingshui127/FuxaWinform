using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace FUXADesktop
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        // UI Controls
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private Panel loadingPanel;
        private Label statusLabel;
        private PictureBox logoPictureBox;
        private Label spinnerLabel;
        private Label versionLabel;
        
        // Server and process management
        private Process serverProcess;
        private string serverUrl;
        private System.Windows.Forms.Timer healthCheckTimer;
        private System.Windows.Forms.Timer startupTimer;
        
        // Configuration
        private AppConfig config;
        
        // Logging
        private StringBuilder serverOutputLog = new StringBuilder();
        private StringBuilder serverErrorLog = new StringBuilder();
        private DateTime serverStartTime;
        private int healthCheckFailCount = 0;
        private const int MAX_HEALTH_CHECK_FAILS = 3;

        public MainForm()
        {
            try
            {
                InitializeConfiguration();
                InitializeUI();
                InitializeEventHandlers();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing form: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }
        }
        
        private void InitializeConfiguration()
        {
            // Load configuration - 先检查当前目录，再检查应用程序目录
            string configPath = Path.Combine(Directory.GetCurrentDirectory(), "app-config.json");
            if (!File.Exists(configPath))
            {
                configPath = Path.Combine(Application.StartupPath, "app-config.json");
            }
            config = AppConfig.Load(configPath);
            
            // 从配置构建服务器URL
            serverUrl = $"http://{config.ServerSettings.Host}:{config.ServerSettings.Port}";
        }
        
        private void InitializeUI()
        {
                
                // Apply window settings from config
                Text = config.WindowTitle;
                Size = new Size(config.WindowSettings.Width, config.WindowSettings.Height);
                MinimumSize = new Size(config.WindowSettings.MinWidth, config.WindowSettings.MinHeight);
                
                try
                {
                    string iconPath = Path.Combine(Application.StartupPath, "fuxa-logo.ico");
                    if (File.Exists(iconPath))
                    {
                        Icon = new Icon(iconPath);
                    }
                }
                catch { }

                // Parse colors from config
                Color bgColor = ParseColor(config.Colors.BackgroundColor, Color.FromArgb(245, 247, 250));
                Color textColor = ParseColor(config.Colors.TextColor, Color.FromArgb(60, 60, 60));

                // Create loading panel
                loadingPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = bgColor
                };

                // Status label at top
                statusLabel = new Label
                {
                    Text = config.LoadingMessages.StartingServer,
                    Font = new Font("Microsoft YaHei", 14, FontStyle.Regular),
                    AutoSize = true,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = textColor,
                    BackColor = Color.Transparent
                };
                loadingPanel.Controls.Add(statusLabel);

                // Logo
                logoPictureBox = new PictureBox
                {
                    Size = new Size(120, 120),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    BackColor = Color.Transparent
                };
                
                try
                {
                    string logoPath = Path.Combine(Application.StartupPath, config.LogoPath);
                    if (File.Exists(logoPath))
                    {
                        if (logoPath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var icon = new Icon(logoPath))
                            {
                                logoPictureBox.Image = icon.ToBitmap();
                            }
                        }
                        else
                        {
                            logoPictureBox.Image = Image.FromFile(logoPath);
                        }
                    }
                }
                catch { }
                
                loadingPanel.Controls.Add(logoPictureBox);

                // Spinner label - 显示启动计时
                spinnerLabel = new Label
                {
                    Text = "0",
                    Font = new Font("Arial", 32, FontStyle.Bold),
                    AutoSize = true,
                    ForeColor = ParseColor(config.Colors.SpinnerColor, Color.FromArgb(0, 123, 255)),
                    BackColor = Color.Transparent
                };
                loadingPanel.Controls.Add(spinnerLabel);

                // Version label
                versionLabel = new Label
                {
                    Text = config.GetFormattedVersionText(),
                    Font = new Font("Microsoft YaHei", 10, FontStyle.Regular),
                    AutoSize = true,
                    TextAlign = ContentAlignment.MiddleCenter,
                    ForeColor = ParseColor(config.Colors.VersionColor, Color.FromArgb(150, 150, 150)),
                    BackColor = Color.Transparent
                };
                loadingPanel.Controls.Add(versionLabel);

                // Initial layout
                UpdateLayout();

                // Handle resize
                this.Resize += (s, e) => UpdateLayout();

                // 启动计时器 - 每秒更新一次
                InitializeStartupTimer();

                Controls.Add(loadingPanel);

                // Create WebView2 (hidden initially)
                webView = new Microsoft.Web.WebView2.WinForms.WebView2
                {
                    Dock = DockStyle.Fill,
                    Visible = false
                };
                Controls.Add(webView);
        }
        
        private void InitializeStartupTimer()
        {
            startupTimer = new System.Windows.Forms.Timer();
            startupTimer.Interval = 1000;
            int seconds = 0;
            startupTimer.Tick += (s, e) =>
            {
                seconds++;
                if (spinnerLabel != null)
                {
                    spinnerLabel.Text = seconds.ToString();
                }
            };
            startupTimer.Start();
        }
        
        private void InitializeEventHandlers()
        {
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            
            // 添加键盘快捷键支持 (Ctrl+P 打印)
            this.KeyPreview = true;
            this.KeyDown += MainForm_KeyDown;
        }

        private void UpdateLayout()
        {
            if (loadingPanel == null) return;
            
            int centerX = this.ClientSize.Width / 2;
            int centerY = this.ClientSize.Height / 2;

            // Status label at top
            if (statusLabel != null)
            {
                statusLabel.Location = new Point(centerX - statusLabel.Width / 2, 40);
            }

            // Logo in center
            if (logoPictureBox != null)
            {
                logoPictureBox.Location = new Point(centerX - logoPictureBox.Width / 2, centerY - 80);
            }

            // Spinner below logo
            if (spinnerLabel != null)
            {
                spinnerLabel.Location = new Point(centerX - spinnerLabel.Width / 2, centerY + 50);
            }

            // Version at bottom
            if (versionLabel != null)
            {
                versionLabel.Location = new Point(centerX - versionLabel.Width / 2, this.ClientSize.Height - 60);
            }
        }

        private Color ParseColor(string colorString, Color defaultColor)
        {
            try
            {
                if (string.IsNullOrEmpty(colorString))
                    return defaultColor;
                
                if (colorString.StartsWith("#"))
                {
                    return ColorTranslator.FromHtml(colorString);
                }
            }
            catch { }
            return defaultColor;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                loadingPanel.Visible = true;
                webView.Visible = false;

                // 记录配置信息到控制台
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] ========================================");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] {config.AppName} Desktop CS Starting...");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] Server URL: {serverUrl}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] Node Path: {config.ServerSettings.NodePath}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] Server Script: {config.ServerSettings.ServerScript}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] ========================================");

                UpdateStatus(config.LoadingMessages.GetStartingServer(config.AppName));
                
                // 并行启动服务器和初始化浏览器
                var serverTask = Task.Run(async () => await StartServerAsync());
                var browserInitTask = InitializeBrowserAsync();
                
                // 等待两个任务都完成
                var results = await Task.WhenAll(serverTask, browserInitTask);
                bool serverStarted = results[0];
                bool browserInitialized = results[1];
                
                if (!serverStarted)
                {
                    MessageBox.Show($"Failed to start {config.AppName} server.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }

                if (!browserInitialized)
                {
                    MessageBox.Show($"Failed to initialize WebView2.\n\nPlease ensure WebView2 Runtime is installed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }

                UpdateStatus(config.LoadingMessages.GetLoadingApp(config.AppName));
                
                // 使用 TaskCompletionSource 等待页面加载完成
                var tcs = new TaskCompletionSource<bool>();
                
                webView.CoreWebView2.NavigationCompleted += (sender, e) =>
                {
                    WebView_NavigationCompleted(sender, e);
                    tcs.TrySetResult(true);
                };
                
                webView.CoreWebView2.Navigate(serverUrl);
                
                // 等待页面加载完成，最多等待30秒
                await Task.WhenAny(tcs.Task, Task.Delay(30000));
                
                // 启动健康检查定时器
                healthCheckTimer = new System.Windows.Forms.Timer();
                healthCheckTimer.Interval = 5000;
                healthCheckTimer.Tick += async (s, ev) => await HealthCheckAsync();
                healthCheckTimer.Start();
                
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] Application initialized successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private async Task<bool> InitializeBrowserAsync()
        {
            try
            {
                this.Invoke(new Action(() =>
                {
                    UpdateStatus(config.LoadingMessages.InitializingBrowser);
                }));
                
                await webView.EnsureCoreWebView2Async(null);
                
                // 配置打印设置
                ConfigurePrintSettings();
                
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] WebView2 initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [ERROR] Failed to initialize WebView2: {ex.Message}");
                return false;
            }
        }

        private void ConfigurePrintSettings()
        {
            try
            {
                // 配置打印设置
                if (webView?.CoreWebView2 != null)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] Print settings configured");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [WARN] Failed to configure print settings: {ex.Message}");
            }
        }
        
        // 显示打印对话框
        private void ShowPrintDialog()
        {
            try
            {
                if (webView?.CoreWebView2 != null)
                {
                    // 使用浏览器打印预览（效果更好）
                    webView.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打印失败: {ex.Message}", "打印错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        // 静默打印到默认打印机
        private async Task PrintToDefaultPrinterAsync()
        {
            try
            {
                if (webView?.CoreWebView2 != null)
                {
                    var printSettings = webView.CoreWebView2.Environment.CreatePrintSettings();
                    printSettings.ShouldPrintBackgrounds = true;  // 打印背景颜色和图像
                    printSettings.ShouldPrintHeaderAndFooter = false;
                    
                    var result = await webView.CoreWebView2.PrintAsync(printSettings);
                    if (result == CoreWebView2PrintStatus.Succeeded)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] Print to default printer succeeded");
                    }
                    else
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [WARN] Print result: {result}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [ERROR] Print failed: {ex.Message}");
            }
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                // 停止启动计时器
                StopStartupTimer();
                
                loadingPanel.Visible = false;
                webView.Visible = true;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] Page loaded successfully");
            }));
        }
        
        private void StopStartupTimer()
        {
            if (startupTimer != null)
            {
                startupTimer.Stop();
                startupTimer.Dispose();
                startupTimer = null;
            }
        }

        private void UpdateStatus(string message)
        {
            if (statusLabel != null)
            {
                this.Invoke(new Action(() =>
                {
                    statusLabel.Text = message;
                    UpdateLayout();
                }));
            }
        }

        private async Task HealthCheckAsync()
        {
            bool isRunning = await IsServerRunningAsync();
            
            if (!isRunning)
            {
                healthCheckFailCount++;
                Console.WriteLine($"Server health check failed ({healthCheckFailCount}/{MAX_HEALTH_CHECK_FAILS})");
                
                if (healthCheckFailCount >= MAX_HEALTH_CHECK_FAILS)
                {
                    Console.WriteLine("Server health check failed multiple times, attempting to restart...");
                    
                    this.Invoke(new Action(() =>
                    {
                        loadingPanel.Visible = true;
                        webView.Visible = false;
                        UpdateStatus(config.LoadingMessages.ServerRestarting);
                    }));
                    
                    // 只停止我们启动的进程，不kill其他进程
                    StopServer();
                    await Task.Delay(3000);
                    
                    if (!await StartServerAsync())
                    {
                        MessageBox.Show("Server connection lost and failed to restart.\n\nPlease check the logs folder for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Close();
                    }
                    else
                    {
                        healthCheckFailCount = 0;
                        
                        this.Invoke(new Action(() =>
                        {
                            UpdateStatus(config.LoadingMessages.GetReloadingApp(config.AppName));
                        }));
                        
                        await Task.Delay(2000);
                        
                        this.Invoke(new Action(() =>
                        {
                            loadingPanel.Visible = false;
                            webView.Visible = true;
                        }));
                        
                        webView.CoreWebView2?.Navigate(serverUrl);
                    }
                }
            }
            else
            {
                if (healthCheckFailCount > 0)
                {
                    Console.WriteLine("Server health check recovered");
                    healthCheckFailCount = 0;
                }
            }
        }

        private async Task<bool> StartServerAsync()
        {
            try
            {
                // 首先检查服务器是否已经在运行
                if (await IsServerRunningAsync())
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] Server already running on {serverUrl}, using existing server");
                    return true;
                }

                string appDir = Application.StartupPath;
                // 使用配置文件中的路径
                string nodePath = Path.Combine(appDir, config.ServerSettings.NodePath);
                string serverPath = Path.Combine(appDir, config.ServerSettings.ServerScript);
                string logDir = Path.Combine(appDir, "logs");
                string logFile = Path.Combine(logDir, $"server_{DateTime.Now:yyyyMMdd_HHmmss}.log");

                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                if (!File.Exists(nodePath))
                {
                    MessageBox.Show($"Node.js not found at: {nodePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                if (!File.Exists(serverPath))
                {
                    MessageBox.Show($"Server file not found at: {serverPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // 不再强制终止端口进程，如果端口被占用但不是FUXA，启动会失败并显示错误

                serverOutputLog.Clear();
                serverErrorLog.Clear();
                serverStartTime = DateTime.Now;

                var startInfo = new ProcessStartInfo
                {
                    FileName = nodePath,
                    Arguments = $"\"{serverPath}\"",
                    WorkingDirectory = appDir,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                string bundledNodeDir = Path.Combine(appDir, "nodejs");
                startInfo.EnvironmentVariables["PATH"] = bundledNodeDir + ";" + Environment.GetEnvironmentVariable("PATH");

                serverProcess = new Process();
                serverProcess.StartInfo = startInfo;
                
                serverProcess.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        string logLine = $"[{DateTime.Now:HH:mm:ss}] [OUT] {e.Data}";
                        serverOutputLog.AppendLine(logLine);
                        Console.WriteLine(logLine);
                        try { File.AppendAllText(logFile, logLine + Environment.NewLine); } catch { }
                    }
                };
                
                serverProcess.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        string logLine = $"[{DateTime.Now:HH:mm:ss}] [ERR] {e.Data}";
                        serverErrorLog.AppendLine(logLine);
                        Console.WriteLine(logLine);
                        try { File.AppendAllText(logFile, logLine + Environment.NewLine); } catch { }
                    }
                };

                serverProcess.EnableRaisingEvents = true;
                serverProcess.Exited += (s, ev) =>
                {
                    string exitLog = $"[{DateTime.Now:HH:mm:ss}] [EXIT] Server process exited with code: {serverProcess?.ExitCode}";
                    Console.WriteLine(exitLog);
                    try { File.AppendAllText(logFile, exitLog + Environment.NewLine); } catch { }
                };

                serverProcess.Start();
                serverProcess.BeginOutputReadLine();
                serverProcess.BeginErrorReadLine();

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] Waiting for server to start (max 60s)...");
                // 使用更频繁的检测间隔（每200ms检测一次），更快发现服务器就绪
                for (int i = 0; i < 300; i++)  // 300 * 200ms = 60秒
                {
                    await Task.Delay(200);
                    
                    // 每秒输出一次日志
                    if (i % 5 == 0 && i > 0)
                    {
                        int seconds = i / 5;
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] Still waiting for server... ({seconds}s)");
                    }
                    
                    if (await IsServerRunningAsync())
                    {
                        int totalSeconds = i * 200 / 1000;
                        string successLog = $"[{DateTime.Now:HH:mm:ss}] [INFO] Server started successfully after {totalSeconds} seconds";
                        Console.WriteLine(successLog);
                        try { File.AppendAllText(logFile, successLog + Environment.NewLine); } catch { }
                        return true;
                    }
                    
                    if (serverProcess.HasExited)
                    {
                        string errorMsg = $"Server process exited prematurely with code: {serverProcess.ExitCode}\n\nError Log:\n{serverErrorLog}";
                        Console.WriteLine(errorMsg);
                        try { File.AppendAllText(logFile, $"[{DateTime.Now:HH:mm:ss}] [FATAL] {errorMsg}" + Environment.NewLine); } catch { }
                        MessageBox.Show(errorMsg, "Server Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }

                string timeoutMsg = $"Server failed to start within 60 seconds.\n\nOutput:\n{serverOutputLog}\n\nErrors:\n{serverErrorLog}";
                Console.WriteLine(timeoutMsg);
                try { File.AppendAllText(logFile, $"[{DateTime.Now:HH:mm:ss}] [FATAL] {timeoutMsg}" + Environment.NewLine); } catch { }
                MessageBox.Show(timeoutMsg, "Server Timeout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting server: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private async Task<bool> IsServerRunningAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(3);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [DEBUG] Checking server at: {serverUrl}");
                    var response = await client.GetAsync(serverUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [DEBUG] Server is running (Status: {response.StatusCode})");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [DEBUG] Server returned status: {response.StatusCode}");
                    }
                }
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [DEBUG] Server check timeout (3s)");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [DEBUG] Server not reachable: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [DEBUG] Server check error: {ex.Message}");
            }
            return false;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+P 打开打印对话框
            if (e.Control && e.KeyCode == Keys.P)
            {
                e.Handled = true;
                ShowPrintDialog();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 检查是否需要询问用户
            if (config.ServerSettings.AskBeforeStopServer)
            {
                var result = MessageBox.Show(
                    $"是否停止 {config.AppName} 后台服务器？\n\n选择「是」将停止服务器并退出。\n选择「否」将退出但保持服务器运行。",
                    "确认退出",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);
                
                if (result == DialogResult.Cancel)
                {
                    e.Cancel = true; // 取消关闭
                    return;
                }
                else if (result == DialogResult.No)
                {
                    // 不停止服务器，直接退出（允许窗体关闭）
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] Exiting without stopping server (user chose No)");
                    return; // 不设置 e.Cancel，允许关闭
                }
                // 选择 "是" 则继续停止服务器并退出
            }
            else if (!config.ServerSettings.StopServerOnExit)
            {
                // 不询问且不需要停止服务器，直接退出
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] Exiting without stopping server (StopServerOnExit = false)");
                return; // 不设置 e.Cancel，允许关闭
            }
            
            // 需要停止服务器，使用异步方式避免阻塞UI
            e.Cancel = true; // 先取消关闭，等服务器停止后再关闭
            Task.Run(() =>
            {
                healthCheckTimer?.Stop();
                StopServer();
                
                // 服务器停止后，在主线程上关闭窗体
                this.Invoke(new Action(() =>
                {
                    FormClosing -= MainForm_FormClosing; // 移除事件处理程序避免递归
                    this.Close();
                }));
            });
        }

        private void StopServer()
        {
            if (serverProcess != null)
            {
                try
                {
                    if (!serverProcess.HasExited)
                    {
                        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] Stopping server process (PID: {serverProcess.Id})...");
                        
                        // Node.js 是控制台应用，没有主窗口，直接 Kill
                        try
                        {
                            serverProcess.Kill();
                            // 等待最多2秒
                            serverProcess.WaitForExit(2000);
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] Server process stopped");
                        }
                        catch (InvalidOperationException)
                        {
                            // 进程可能已经在退出过程中
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [INFO] Server process already exiting");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [WARN] Error stopping server: {ex.Message}");
                }
                finally
                {
                    try
                    {
                        serverProcess?.Dispose();
                    }
                    catch { }
                    serverProcess = null;
                }
            }
        }
    }
}
