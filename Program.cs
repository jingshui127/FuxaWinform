using Microsoft.Web.WebView2.Core;
using NewLife;
using NewLife.Log;
using NewLife.Model;
using NewLife.Remoting.Clients;
using Stardust;
using Stardust.Monitors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FUXADesktop
{
    static class Program
    {
        [DllImport("kernel32.dll")]
        private static extern bool SetErrorMode(int mode);

        [DllImport("kernel32.dll")]
        private static extern int GetErrorMode();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleTitle(string lpConsoleTitle);

        private const int SEM_NOGPFAULTERRORBOX = 0x0002;
        private const int SEM_NOALIGNMENTFAULTEXCEPT = 0x0004;
        private const int SEM_NOOPENFILEERRORBOX = 0x8000;
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int STARTF_USESHOWWINDOW = 0x00000001;
        private const int SW_SHOWNOACTIVATE = 4;
        private const int SW_SHOWMINNOACTIVE = 7;

        [STAThread]
        static void Main()
        {
            // 支持GB2312编码
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // 禁用错误对话框和子进程窗口 - 在应用程序整个生命周期内保持这些设置
            SetErrorMode(GetErrorMode() | SEM_NOGPFAULTERRORBOX | SEM_NOOPENFILEERRORBOX | SEM_NOALIGNMENTFAULTEXCEPT);

            // 设置环境变量以禁用子进程窗口
            Environment.SetEnvironmentVariable("__COMPAT_LAYER", "RUNASINVOKER");
            Environment.SetEnvironmentVariable("MSYS_NO_PATHCONV", "1");
            Environment.SetEnvironmentVariable("MSYS2_PATH_TYPE", "inherit");

            // 启用星尘日志
            XTrace.UseWinForm();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            XTrace.WriteLine("FuxaWinform 应用程序启动");

            // 启动后台线程检测并隐藏 CMD 窗口
            var hideCmdThread = new System.Threading.Thread(() =>
            {
                while (true)
                {
                    try
                    {
                        NativeMethods.HideConsoleWindows();
                    }
                    catch { }
                    System.Threading.Thread.Sleep(100);
                }
            });
            hideCmdThread.IsBackground = true;
            hideCmdThread.Start();

            try
            {
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
                throw;
            }
        }
    }

    internal static class NativeMethods
    {
        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int SW_HIDE = 0;

        public static void HideConsoleWindows()
        {
            try
            {
                EnumWindows((hWnd, lParam) =>
                {
                    if (IsWindowVisible(hWnd))
                    {
                        int length = GetWindowTextLength(hWnd);
                        if (length > 0)
                        {
                            System.Text.StringBuilder sb = new System.Text.StringBuilder(length + 1);
                            GetWindowText(hWnd, sb, sb.Capacity);
                            string title = sb.ToString();
                            if (title.Contains("cmd.exe") || title.Contains("Command Prompt"))
                            {
                                GetWindowThreadProcessId(hWnd, out uint processId);
                                if (processId == (uint)Environment.ProcessId)
                                {
                                    ShowWindow(hWnd, SW_HIDE);
                                }
                            }
                        }
                    }
                    return true;
                }, IntPtr.Zero);
            }
            catch { }
        }
    }

    public class MainForm : Form
    {
        // Windows API 函数声明
        [DllImport("kernel32.dll")]
        private static extern int SetErrorMode(int mode);

        [DllImport("kernel32.dll")]
        private static extern int GetErrorMode();

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const int SW_HIDE = 0;
        private const int SEM_NOGPFAULTERRORBOX = 0x0002;
        private const int SEM_NOALIGNMENTFAULTEXCEPT = 0x0004;
        private const int SEM_NOOPENFILEERRORBOX = 0x8000;

        #region UI Controls
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private Panel loadingPanel;
        private Label statusLabel;
        private PictureBox logoPictureBox;
        private Label spinnerLabel;
        private Label versionLabel;
        #endregion

        #region Server and Process Management
        private Process serverProcess;
        private string serverUrl;
        private System.Windows.Forms.Timer healthCheckTimer;
        private System.Windows.Forms.Timer startupTimer;
        private DateTime serverStartTime;
        private bool isServerTakenOver;
        #endregion

        #region Configuration and Logging
        private AppConfig config;
        private StringBuilder serverOutputLog = new StringBuilder();
        private StringBuilder serverErrorLog = new StringBuilder();
        private int healthCheckFailCount = 0;
        private const int MAX_HEALTH_CHECK_FAILS = 3;
        #endregion

        #region Performance Tracking
        private System.Diagnostics.Stopwatch totalStopwatch = new System.Diagnostics.Stopwatch();
        private System.Diagnostics.Stopwatch stageStopwatch = new System.Diagnostics.Stopwatch();
        private Dictionary<string, long> stageDurations = new Dictionary<string, long>();
        #endregion

        #region Stardust Monitoring

        private int rotationAngle = 0;
        #endregion

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

        #region Initialization
        private void InitializeConfiguration()
        {
            string configPath = Path.Combine(Directory.GetCurrentDirectory(), "app-config.json");
            if (!File.Exists(configPath))
            {
                configPath = Path.Combine(Application.StartupPath, "app-config.json");
            }
            config = AppConfig.Load(configPath);
            serverUrl = $"http://{config.ServerSettings.Host}:{config.ServerSettings.Port}";
        }

        private void InitializeUI()
        {
            Text = config.WindowTitle;
            Size = new Size(config.WindowSettings.Width, config.WindowSettings.Height);
            MinimumSize = new Size(config.WindowSettings.MinWidth, config.WindowSettings.MinHeight);

            try
            {
                string iconPath = Path.Combine(Application.StartupPath, "favicon.ico");
                if (File.Exists(iconPath))
                {
                    Icon = new Icon(iconPath);
                }
            }
            catch { }

            Color bgColor = ParseColor(config.Colors.BackgroundColor, Color.FromArgb(245, 247, 250));
            Color textColor = ParseColor(config.Colors.TextColor, Color.FromArgb(60, 60, 60));

            loadingPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ParseColor(config.Colors.BackgroundColor, Color.FromArgb(245, 247, 250))
            };

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

            logoPictureBox = new PictureBox
            {
                Size = new Size(120, 120),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            // 不设置 Image 属性，避免双重渲染
            // 而是在 Paint 事件中直接绘制
            Image logoImage = null;
            try
            {
                // 尝试多种路径加载 Logo
                string[] possiblePaths = new string[]
                {
                    Path.Combine(Application.StartupPath, config.LogoPath),
                    Path.Combine(Application.StartupPath, "favicon.ico"),
                    Path.Combine(Application.StartupPath, "fuxa-logo.ico")
                };
                
                foreach (string logoPath in possiblePaths)
                {
                    if (File.Exists(logoPath))
                    {
                        if (logoPath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var icon = new Icon(logoPath))
                            {
                                logoImage = icon.ToBitmap();
                                break;
                            }
                        }
                        else
                        {
                            logoImage = Image.FromFile(logoPath);
                            break;
                        }
                    }
                }
            }
            catch { }
            
            // 修改 Paint 事件，使用本地 logoImage 变量
            logoPictureBox.Paint += (s, e) =>
            {
                if (logoImage != null)
                {
                    // 保存当前状态
                    var state = e.Graphics.Save();
                    // 移动到中心点
                    e.Graphics.TranslateTransform(logoPictureBox.Width / 2, logoPictureBox.Height / 2);
                    // 旋转
                    e.Graphics.RotateTransform(rotationAngle);
                    // 绘制图片
                    e.Graphics.DrawImage(logoImage, -logoPictureBox.Width / 2, -logoPictureBox.Height / 2, logoPictureBox.Width, logoPictureBox.Height);
                    // 恢复状态
                    e.Graphics.Restore(state);
                }
                else
                {
                    // 绘制默认的加载动画
                    var state = e.Graphics.Save();
                    e.Graphics.TranslateTransform(logoPictureBox.Width / 2, logoPictureBox.Height / 2);
                    e.Graphics.RotateTransform(rotationAngle);
                    
                    // 绘制一个简单的加载图标
                    using (var pen = new Pen(ParseColor(config.Colors.SpinnerColor, Color.FromArgb(0, 123, 255)), 4))
                    {
                        e.Graphics.DrawArc(pen, -40, -40, 80, 80, 0, 270);
                    }
                    
                    e.Graphics.Restore(state);
                }
            };

            loadingPanel.Controls.Add(logoPictureBox);

            spinnerLabel = new Label
            {
                Text = "0",
                Font = new Font("Arial", 32, FontStyle.Bold),
                AutoSize = true,
                ForeColor = ParseColor(config.Colors.SpinnerColor, Color.FromArgb(0, 123, 255)),
                BackColor = Color.Transparent
            };
            loadingPanel.Controls.Add(spinnerLabel);

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

            UpdateLayout();
            this.Resize += (s, e) =>
            {
                UpdateLayout();
                // 窗口大小改变时强制 WebView2 刷新布局
                if (webView?.Visible == true && webView?.CoreWebView2 != null)
                {
                    // 强制 WebView2 重新布局和重绘
                    webView.SuspendLayout();
                    var bounds = webView.Bounds;
                    webView.SetBounds(bounds.X, bounds.Y, bounds.Width + 1, bounds.Height);
                    webView.SetBounds(bounds.X, bounds.Y, bounds.Width, bounds.Height);
                    webView.ResumeLayout(true);
                    webView.Refresh();
                }
            };
            InitializeStartupTimer();
            Controls.Add(loadingPanel);

            webView = new Microsoft.Web.WebView2.WinForms.WebView2
            {
                Dock = DockStyle.Fill,
                Visible = false,
                BackColor = ParseColor(config.Colors.BackgroundColor, Color.FromArgb(245, 247, 250))
            };
            Controls.Add(webView);
            
            // 确保 loadingPanel 始终在最上层
            loadingPanel.BringToFront();
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
            
            // 添加加载动画
            var animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = 50;
            animationTimer.Tick += (s, e) =>
            {
                rotationAngle += 5;
                if (rotationAngle >= 360)
                {
                    rotationAngle = 0;
                }
                if (logoPictureBox != null)
                {
                    logoPictureBox.Invalidate();
                }
            };
            animationTimer.Start();
        }

        private void InitializeEventHandlers()
        {
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;
        }
        #endregion

        #region UI Helpers
        private void UpdateLayout()
        {
            if (loadingPanel == null) return;

            int centerX = this.ClientSize.Width / 2;
            int centerY = this.ClientSize.Height / 2;

            if (statusLabel != null)
            {
                statusLabel.Location = new Point(centerX - statusLabel.Width / 2, 40);
            }

            if (logoPictureBox != null)
            {
                logoPictureBox.Location = new Point(centerX - logoPictureBox.Width / 2, centerY - 80);
            }

            if (spinnerLabel != null)
            {
                spinnerLabel.Location = new Point(centerX - spinnerLabel.Width / 2, centerY + 50);
            }

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

        private void StopStartupTimer()
        {
            if (startupTimer != null)
            {
                startupTimer.Stop();
                startupTimer.Dispose();
                startupTimer = null;
            }
        }
        #endregion

        #region Form Events
        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                // 启动总计时器
                totalStopwatch.Start();
                stageStopwatch.Start();
                
                loadingPanel.Visible = true;
                webView.Visible = false;

                XTrace.WriteLine($"[INFO] {config.AppName} Desktop CS Starting...");
                XTrace.WriteLine($"[INFO] Server URL: {serverUrl}");

                UpdateStatus(config.LoadingMessages.GetStartingServer(config.AppName));

                // 记录配置加载完成时间
                stageStopwatch.Stop();
                stageDurations.Add("Config Load", stageStopwatch.ElapsedMilliseconds);
                XTrace.WriteLine($"[PERF] Config Load: {stageStopwatch.ElapsedMilliseconds} ms");
                
                // 记录并行任务的开始时间
                DateTime parallelStartTime = DateTime.Now;
                
                // 启动服务器（已优化）
                var serverTask = StartServerAsync();
                
                // 立即开始初始化浏览器，不等待服务器完全启动
                UpdateStatus(config.LoadingMessages.InitializingBrowser);
                var browserTask = InitializeBrowserAsync();
                
                // 等待两个任务都完成
                await Task.WhenAll(serverTask, browserTask);
                
                bool serverStarted = serverTask.Result;
                if (!serverStarted)
                {
                    MessageBox.Show($"Failed to start {config.AppName} server.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }
                
                bool browserInitialized = browserTask.Result;
                if (!browserInitialized)
                {
                    MessageBox.Show($"Failed to initialize WebView2.\n\nPlease ensure WebView2 Runtime is installed.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }
                
                // 记录两个并行任务的总时间
                long parallelDuration = (long)(DateTime.Now - parallelStartTime).TotalMilliseconds;
                stageDurations.Add("Server Start", parallelDuration);
                stageDurations.Add("Browser Init", parallelDuration);
                XTrace.WriteLine($"[PERF] Server Start: {parallelDuration} ms (并行)");
                XTrace.WriteLine($"[PERF] Browser Init: {parallelDuration} ms (并行)");
                
                stageStopwatch.Restart();

                UpdateStatus(config.LoadingMessages.GetLoadingApp(config.AppName));
                var tcs = new TaskCompletionSource<bool>();

                webView.CoreWebView2.ContentLoading += (sender, e) =>
                {
                    var bgColor = ParseColor(config.Colors.BackgroundColor, Color.FromArgb(245, 247, 250));
                    string css = $"body {{ background-color: #{bgColor.R:X2}{bgColor.G:X2}{bgColor.B:X2} !important; }}";
                    webView.CoreWebView2.ExecuteScriptAsync(
                        $"(function() {{ var style = document.createElement('style'); style.textContent = '{css}'; document.head.appendChild(style); }})();"
                    );
                };

                webView.CoreWebView2.NavigationCompleted += (sender, e) =>
                {
                    WebView_NavigationCompleted(sender, e);
                    tcs.TrySetResult(true);
                };

                webView.CoreWebView2.Navigate(serverUrl);
                await tcs.Task;

                healthCheckTimer = new System.Windows.Forms.Timer();
                healthCheckTimer.Interval = 5000;
                healthCheckTimer.Tick += async (s, ev) => await HealthCheckAsync();
                healthCheckTimer.Start();

                StartClient();

                XTrace.WriteLine("[INFO] Application initialized successfully");
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
                MessageBox.Show($"Error: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isServerTakenOver)
            {
                XTrace.WriteLine("[INFO] Server was taken over, not stopping server. Exiting application...");
                healthCheckTimer?.Stop();
                return;
            }

            // 如果是应用程序启动的服务器，退出时总是停止服务器
            // 因为不停止的话，有时候FUXA会不正常运行
            XTrace.WriteLine("[INFO] Stopping server and exiting application...");

            e.Cancel = true;
            Task.Run(() =>
            {
                healthCheckTimer?.Stop();
                StopServer();

                this.Invoke(new Action(() =>
                {
                    FormClosing -= MainForm_FormClosing;
                    this.Close();
                }));
            });
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.P)
            {
                e.Handled = true;
                ShowPrintDialog();
            }
        }
        #endregion

        #region Browser
        private async Task<bool> InitializeBrowserAsync()
        {
            try
            {
                this.Invoke(new Action(() =>
                {
                    UpdateStatus(config.LoadingMessages.InitializingBrowser);
                }));

                var options = new CoreWebView2EnvironmentOptions()
                {
                    AdditionalBrowserArguments = "--disable-gpu --disable-software-rasterizer --disable-dev-shm-usage"
                };

                await webView.EnsureCoreWebView2Async(null);
                
                if (webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                    webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
                    webView.CoreWebView2.Settings.AreHostObjectsAllowed = true;
                    webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
                }

                ConfigurePrintSettings();

                XTrace.WriteLine("[INFO] WebView2 initialized successfully");
                return true;
            }
            catch (Exception ex)
            {
                XTrace.Log.Error("Failed to initialize WebView2: {0}", ex.Message);
                return false;
            }
        }

        private void ConfigurePrintSettings()
        {
            try
            {
                if (webView?.CoreWebView2 != null)
                {
                    XTrace.WriteLine("[INFO] Print settings configured");
                }
            }
            catch (Exception ex)
            {
                XTrace.Log.Warn("Failed to configure print settings: {0}", ex.Message);
            }
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            this.Invoke(new Action(async () =>
            {
                try
                {
                    // 记录页面加载时间
                    stageStopwatch.Stop();
                    stageDurations.Add("Page Load", stageStopwatch.ElapsedMilliseconds);
                    XTrace.WriteLine($"[PERF] Page Load: {stageStopwatch.ElapsedMilliseconds} ms");
                    
                    // 注入 CSS 来设置背景色，防止白屏
                    var bgColor = ParseColor(config.Colors.BackgroundColor, Color.FromArgb(245, 247, 250));
                    string css = $"body {{ background-color: #{bgColor.R:X2}{bgColor.G:X2}{bgColor.B:X2} !important; }} html {{ background-color: #{bgColor.R:X2}{bgColor.G:X2}{bgColor.B:X2} !important; }}";
                    await webView.CoreWebView2.ExecuteScriptAsync(
                        $"(function() {{ var style = document.createElement('style'); style.textContent = '{css}'; document.head.appendChild(style); }})();"
                    );

                    // 等待一小段时间确保 CSS 生效
                    await Task.Delay(100);

                    StopStartupTimer();
                    loadingPanel.Visible = false;
                    webView.Visible = true;
                    XTrace.WriteLine("[INFO] Page loaded successfully");
                    
                    // 记录总启动时间
                    totalStopwatch.Stop();
                    XTrace.WriteLine($"[PERF] Total Startup Time: {totalStopwatch.ElapsedMilliseconds} ms");
                    
                    // 输出所有阶段的性能统计
                    XTrace.WriteLine("[PERF] Performance Summary:");
                    foreach (var stage in stageDurations)
                    {
                        XTrace.WriteLine($"[PERF] - {stage.Key}: {stage.Value} ms ({(double)stage.Value / totalStopwatch.ElapsedMilliseconds * 100:F1}%)");
                    }
                }
                catch (Exception ex)
                {
                    XTrace.Log.Error("Error in WebView_NavigationCompleted: {0}", ex.Message);
                }
            }));
        }

        private void ShowPrintDialog()
        {
            try
            {
                if (webView?.CoreWebView2 != null)
                {
                    webView.CoreWebView2.ShowPrintUI(CoreWebView2PrintDialogKind.Browser);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打印失败: {ex.Message}", "打印错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task PrintToDefaultPrinterAsync()
        {
            try
            {
                if (webView?.CoreWebView2 != null)
                {
                    var printSettings = webView.CoreWebView2.Environment.CreatePrintSettings();
                    printSettings.ShouldPrintBackgrounds = true;
                    printSettings.ShouldPrintHeaderAndFooter = false;

                    var result = await webView.CoreWebView2.PrintAsync(printSettings);
                    if (result == CoreWebView2PrintStatus.Succeeded)
                    {
                        XTrace.WriteLine("[INFO] Print to default printer succeeded");
                    }
                    else
                    {
                        XTrace.Log.Warn("Print result: {0}", result);
                    }
                }
            }
            catch (Exception ex)
            {
                XTrace.Log.Error("Print failed: {0}", ex.Message);
            }
        }
        #endregion

        #region Server Management
        private async Task<bool> StartServerAsync(bool forceRestart = false)
        {
            try
            {
                var uri = new Uri(serverUrl);
                int port = uri.Port;

                // 快速检查服务器是否已经在运行（只检查1次，减少等待时间）
                bool serverRunning = await IsServerRunningAsync();

                if (!forceRestart && serverRunning)
                {
                    string message = "FUXA 服务已经启动，正在使用现有服务器...";
                    XTrace.WriteLine($"[INFO] Server already running on {serverUrl}, using existing server");
                    UpdateStatus(message);

                    isServerTakenOver = true;
                    serverStartTime = DateTime.Now;

                    int? pid = GetProcessIdByPort(port);
                    if (pid.HasValue)
                    {
                        XTrace.WriteLine($"[INFO] Using existing server with PID: {pid.Value}");
                        try
                        {
                            serverProcess = Process.GetProcessById(pid.Value);
                            this.Invoke(new Action(() =>
                            {
                                this.Text = $"{config.AppName} [接管] - PID: {pid.Value}";
                            }));
                        }
                        catch (Exception ex)
                        {
                    XTrace.Log.Warn("Failed to get process by ID: {0}", ex.Message);
                        }
                    }

                    return true;
                }

                int? existingPid = GetProcessIdByPort(port);
                if (existingPid.HasValue)
                {
                    XTrace.WriteLine($"[INFO] Port {port} is in use, killing process {existingPid.Value}...");
                    UpdateStatus($"端口 {port} 被占用，正在停止占用端口的进程...");
                    KillProcess(existingPid.Value);
                    await Task.Delay(1000); // 减少等待时间
                }

                string appDir = Application.StartupPath;
                // 统一路径分隔符为 Windows 风格
                string nodePath = Path.Combine(appDir, config.ServerSettings.NodePath.Replace('/', '\\'));
                string serverPath = Path.Combine(appDir, config.ServerSettings.ServerScript.Replace('/', '\\'));
                // 规范化路径
                nodePath = Path.GetFullPath(nodePath);
                serverPath = Path.GetFullPath(serverPath);

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

                serverOutputLog.Clear();
                serverErrorLog.Clear();

                string nodeDir = Path.GetDirectoryName(nodePath);
                string bundledNodeDir = Path.Combine(appDir, "nodejs");

                serverProcess = new Process();
                var psi = serverProcess.StartInfo;
                psi.FileName = nodePath;
                psi.Arguments = $"\"{serverPath}\"";
                psi.WorkingDirectory = nodeDir;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.EnvironmentVariables["PATH"] = bundledNodeDir + ";" + Environment.GetEnvironmentVariable("PATH");
                psi.EnvironmentVariables["NODE_ENV"] = "production"; // 设置生产环境，加速Node.js启动

                serverProcess.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        XTrace.WriteLine($"[OUT] {e.Data}");
                    }
                };

                serverProcess.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        XTrace.WriteLine($"[ERR] {e.Data}");
                    }
                };

                serverProcess.EnableRaisingEvents = true;
                serverProcess.Exited += (s, ev) =>
                {
                    XTrace.WriteLine($"[EXIT] Server process exited with code: {serverProcess?.ExitCode}");
                };

                serverProcess.Start();
                serverProcess.BeginOutputReadLine();
                serverProcess.BeginErrorReadLine();

                XTrace.WriteLine("[INFO] Waiting for server to start...");

                DateTime startTime = DateTime.Now;
                // 优化服务器启动检测逻辑
                for (int i = 0; i < 200; i++) // 减少总等待时间（从60秒到20秒）
                {
                    await Task.Delay(100); // 减少每次等待时间，提高响应速度

                    if (i % 50 == 0 && i > 0) // 减少状态更新频率
                    {
                        int seconds = (int)(DateTime.Now - startTime).TotalSeconds;
                        UpdateStatus($"服务器启动中... ({seconds}秒)");
                    }

                    if (serverProcess != null && serverProcess.HasExited)
                    {
                        string errorMsg = $"Server process exited prematurely with code: {serverProcess.ExitCode}\n\nError Log:\n{serverErrorLog}";
                        XTrace.WriteLine(errorMsg);
                        UpdateStatus("服务器启动失败");
                        MessageBox.Show(errorMsg, "Server Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    try
                    {
                        if (await IsServerRunningAsync())
                        {
                            int totalSeconds = (int)(DateTime.Now - startTime).TotalSeconds;
                            XTrace.WriteLine($"[INFO] Server started successfully after {totalSeconds} seconds");
                            UpdateStatus($"服务器启动成功 ({totalSeconds}秒)");
                            serverStartTime = DateTime.Now;

                            if (serverProcess != null)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    this.Text = $"{config.AppName} - PID: {serverProcess.Id}";
                                }));
                            }
                            return true;
                        }
                    }
                    catch { }
                }

                string timeoutMsg = $"Server failed to start within 20 seconds.\n\nOutput:\n{serverOutputLog}\n\nErrors:\n{serverErrorLog}";
                XTrace.WriteLine(timeoutMsg);
                UpdateStatus("服务器启动超时");
                MessageBox.Show(timeoutMsg, "Server Timeout", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            catch (Exception ex)
            {
                UpdateStatus("服务器启动失败");
                MessageBox.Show($"Error starting server: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private async Task<bool> IsServerRunningAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var response = await client.GetAsync(serverUrl);
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private void StopServer()
        {
            if (serverProcess != null)
            {
                try
                {
                    if (!serverProcess.HasExited)
                    {
                        XTrace.WriteLine($"[INFO] Stopping server process (PID: {serverProcess.Id})...");

                        try
                        {
                            serverProcess.Kill();
                            serverProcess.WaitForExit(2000);
                            XTrace.WriteLine("[INFO] Server process stopped");
                        }
                        catch (InvalidOperationException)
                        {
                            XTrace.WriteLine("[INFO] Server process already exiting");
                        }
                    }
                }
                catch (Exception ex)
                {
                    XTrace.Log.Warn("Error stopping server: {0}", ex.Message);
                }
                finally
                {
                    try
                    {
                        serverProcess?.Dispose();
                    }
                    catch { }
                    serverProcess = null;
                    serverStartTime = DateTime.MinValue;
                }
            }
        }

        private int? GetProcessIdByPort(int port)
        {
            try
            {
                var process = new Process();
                var psi = process.StartInfo;
                psi.FileName = "netstat.exe";
                psi.Arguments = "-ano";
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output))
                {
                    string[] lines = output.Split('\n');
                    foreach (string line in lines)
                    {
                        if (!string.IsNullOrEmpty(line) && line.Contains($":{port}") && line.Contains("LISTENING"))
                        {
                            string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0)
                            {
                                string pidStr = parts[parts.Length - 1];
                                XTrace.Log.Debug($"[DEBUG] Found PID: {pidStr}");
                                if (int.TryParse(pidStr, out int pid) && pid > 0)
                                {
                                    return pid;
                                }
                            }
                        }
                    }
                }

                XTrace.Log.Debug($"[DEBUG] No process found listening on port {port}");
            }
            catch (Exception ex)
            {
                XTrace.Log.Debug("Error getting process ID by port: {0}", ex.Message);
            }
            return null;
        }

        private void KillProcess(int pid)
        {
            try
            {
                Process process = Process.GetProcessById(pid);
                XTrace.WriteLine($"[INFO] Killing process {pid}...");
                process.Kill();
                process.WaitForExit(2000);
                XTrace.WriteLine($"[INFO] Process {pid} killed");
            }
            catch (Exception ex)
            {
                XTrace.Log.Debug("Error killing process {0}: {1}", pid, ex.Message);
            }
        }

        private bool IsFUXAProcessRunning()
        {
            try
            {
                var uri = new Uri(serverUrl);
                int port = uri.Port;

                var ipGlobalProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
                var activeListeners = ipGlobalProperties.GetActiveTcpListeners();

                var listener = activeListeners.FirstOrDefault(l => l.Port == port);

                if (listener != null)
                {
                    XTrace.WriteLine($"[INFO] Port {port} is already in use by another process");
                    return true;
                }
                else
                {
                    XTrace.WriteLine($"[INFO] Port {port} is not in use");
                    return false;
                }
            }
            catch (Exception ex)
            {
                XTrace.Log.Warn("Failed to check port: {0}", ex.Message);
                return false;
            }
        }
        #endregion

        #region Health Check
        private async Task HealthCheckAsync()
        {
            if (!this.IsHandleCreated)
            {
                return;
            }

            bool isRunning = await IsServerRunningAsync();
            if (isRunning)
            {
                if (healthCheckFailCount > 0)
                {
                    XTrace.WriteLine("Server health check recovered");
                    healthCheckFailCount = 0;
                }

                UpdateWindowTitle();
                return;
            }

            healthCheckFailCount++;
            XTrace.WriteLine($"Server health check failed ({healthCheckFailCount}/{MAX_HEALTH_CHECK_FAILS})");

            if (healthCheckFailCount >= MAX_HEALTH_CHECK_FAILS)
            {
                XTrace.WriteLine("Server health check failed multiple times, attempting to restart...");

                this.Invoke(new Action(() =>
                {
                    loadingPanel.Visible = true;
                    webView.Visible = false;
                    UpdateStatus("服务器健康检查失败，正在重新启动...");
                }));

                StopServer();
                await Task.Delay(3000);

                if (!await StartServerAsync(forceRestart: true))
                {
                    MessageBox.Show("Server connection lost and failed to restart.\n\nPlease check the logs folder for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }

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

        private void UpdateWindowTitle()
        {
            XTrace.Log.Debug("开始更新窗口标题...");
            if (this.IsHandleCreated && serverProcess != null && !serverProcess.HasExited)
            {
                TimeSpan uptime = DateTime.Now - serverStartTime;
                string uptimeString = $"{uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
                string statusText = isServerTakenOver ? "[接管]" : "[启动]";

                XTrace.Log.Debug("更新窗口标题为：{0} {1} - PID: {2} - Uptime: {3}", config.AppName, statusText, serverProcess.Id, uptimeString);
                this.Invoke(new Action(() =>
                {
                    this.Text = $"{config.AppName} {statusText} - PID: {serverProcess.Id} - Uptime: {uptimeString}";
                }));
                XTrace.Log.Debug("窗口标题更新完成");
            }
            else
            {
                XTrace.Log.Debug("跳过更新窗口标题，条件不满足");
            }
        }
        #endregion

        #region Stardust Monitoring
        static StarFactory _factory;
        static StarClient _Client;
        private static void StartClient()
        {
            var set = ClientSetting.Current;
            var server = set.Server;
            if (server.IsNullOrEmpty()) return;

            XTrace.WriteLine("初始化服务端地址：{0}", server);

            _factory = new StarFactory(server, "FUXA客户端", null)
            {
                Log = XTrace.Log,
            };

            var client = new StarClient(server)
            {
                Code = set.Code,
                Secret = set.Secret,
                ProductCode = _factory.AppId,
                Setting = set,

                Tracer = _factory.Tracer,
                Log = XTrace.Log,
            };

            client.Open();

            Host.RegisterExit(() => client.Logout("ApplicationExit"));

            _Client = client;
        }
        #endregion

        #region Resource Cleanup
        private void DisposeWebView()
        {
            try
            {
                XTrace.WriteLine("[INFO] Disposing WebView2 resources...");

                if (webView?.CoreWebView2 != null)
                {
                    try
                    {
                        webView.CoreWebView2.Navigate("about:blank");
                    }
                    catch { }
                }

                System.Threading.Thread.Sleep(500);

                if (webView != null)
                {
                    try
                    {
                        webView.Dispose();
                    }
                    catch { }
                }

                XTrace.WriteLine("[INFO] WebView2 resources disposed");
            }
            catch (Exception ex)
            {
                XTrace.Log.Warn("Error disposing WebView2: {0}", ex.Message);
            }
        }
        #endregion
    }
}
