# FuxaWinform

<p align="center">
  <img src="https://raw.githubusercontent.com/frangoteam/FUXA/main/client/src/favicon.ico" alt="FuxaWinform Logo" width="120" height="120">
</p>

<p align="center">
  <strong>FUXA 的 Windows 桌面客户端</strong>
</p>

<p align="center">
  <a href="#功能特性">功能特性</a> •
  <a href="#快速开始">快速开始</a> •
  <a href="#系统要求">系统要求</a> •
  <a href="#配置说明">配置说明</a> •
  <a href="#更新日志">更新日志</a>
</p>

---

## 📋 简介

**FuxaWinform** 是一个基于 C# + WebView2 的 FUXA 桌面客户端应用程序。它将 FUXA 服务器和浏览器客户端打包成一个独立的 Windows 桌面应用，无需安装 Node.js 或其他依赖即可运行。

[FUXA](https://github.com/frangoteam/FUXA) 是一个基于 Web 的过程可视化（SCADA/HMI/仪表板）软件，FuxaWinform 为其提供了一个原生的 Windows 桌面体验。

## ✨ 功能特性

- 🚀 **一键启动** - 内置 Node.js 运行时，无需额外安装
- 🖥️ **原生体验** - 基于 WebView2，提供流畅的桌面应用体验
- ⚙️ **灵活配置** - 支持通过 JSON 配置文件自定义应用行为
- 🖨️ **打印支持** - 内置打印功能，支持打印预览
- 🔄 **自动恢复** - 服务器异常时自动重启
- ⏱️ **启动计时** - 显示应用启动耗时
- 💾 **退出选项** - 可选择是否保留后台服务器运行
- 📊 **星尘监控** - 集成 NewLife.Stardust 分布式监控平台
- 🎯 **服务器接管** - 支持接管已运行的 FUXA 服务器
- 📈 **运行时长** - 窗口标题显示服务器 PID 和运行时长
- 🚫 **无窗口闪现** - 优化进程创建，避免 CMD 窗口闪现

## 🚀 快速开始

### 下载安装

1. 从 [Releases](https://github.com/jingshui127/FuxaWinform/releases) 页面下载最新版本
2. 解压到任意目录
3. 双击 `FuxaWinform.exe` 启动

### 系统要求

- **操作系统**: Windows 10 或 Windows 11 (64位)
- **运行时**: [.NET 桌面运行时 10.0](https://dotnet.microsoft.com/download/dotnet/10.0)（首次运行时需要安装）
- **浏览器组件**: Microsoft Edge WebView2 Runtime（Windows 10/11 通常已预装）

### 安装 .NET 10 运行时

如果启动时提示缺少 .NET 10 运行时，请按以下步骤安装：

#### 方法一：自动下载（推荐）

运行 `FuxaWinform.exe` 时，如果系统缺少 .NET 10，会弹出提示窗口：

```
┌─────────────────────────────────────────┐
│  无法启动此程序                          │
│                                         │
│  计算机中丢失 .NET 10 Runtime。          │
│  请安装 .NET 10 以解决此问题。           │
│                                         │
│  [ 下载 .NET 10 ]  [ 取消 ]             │
└─────────────────────────────────────────┘
```

点击「下载 .NET 10」按钮，会自动跳转到下载页面。

#### 方法二：手动下载安装

1. 访问 [.NET 10 下载页面](https://dotnet.microsoft.com/download/dotnet/10.0)
2. 下载 **.NET 桌面运行时 10.0.x**（Windows x64 版本）
   - 文件名：`windowsdesktop-runtime-10.0.x-win-x64.exe`
   - 大小：约 55 MB
3. 运行安装程序，按提示完成安装
4. 重新启动 `FuxaWinform.exe`

#### 方法三：离线安装

如果目标电脑无法联网，可以：
1. 提前下载 [.NET 桌面运行时 10.0.x](https://dotnet.microsoft.com/download/dotnet/thank-you/runtime-desktop-10.0.3-windows-x64-installer)
2. 将安装包与应用程序一起分发给客户
3. 客户先安装 .NET 运行时，再运行 FuxaWinform

### 验证安装

安装完成后，打开命令提示符，运行：
```cmd
dotnet --list-runtimes
```

如果看到以下输出，说明安装成功：
```
Microsoft.WindowsDesktop.App 10.0.3 [C:\Program Files\dotnet\shared\Microsoft.WindowsDesktop.App]
```

### 首次启动

应用程序启动时会显示加载界面：

```
┌─────────────────────────┐
│  正在启动 FUXA 服务器... │
│                         │
│      [Logo 图片]        │
│                         │
│         [ 8 ]           │  ← 启动秒数
│                         │
│      FUXA v1.3.0        │
└─────────────────────────┘
```

启动完成后自动进入 FUXA 主界面。

## 💻 详细系统要求

| 组件 | 要求 | 说明 |
|------|------|------|
| **操作系统** | Windows 10/11 (64位) | Windows 10 版本 1809 或更高 |
| **.NET 运行时** | .NET 桌面运行时 10.0 | [下载安装](#安装-net-10-运行时) |
| **WebView2** | Microsoft Edge WebView2 | Windows 11 已预装，Windows 10 可能需要安装 |
| **内存** | 4 GB 或更高 | 推荐 8 GB |
| **磁盘空间** | 500 MB 可用空间 | 包含应用程序和数据存储 |

## ⚙️ 配置说明

### 配置文件

编辑 `app-config.json` 来自定义应用行为：

```json
{
  "appName": "FUXA",
  "windowTitle": "FUXA - Process Visualization",
  "windowSettings": {
    "width": 1400,
    "height": 900,
    "minWidth": 800,
    "minHeight": 600
  },
  "serverSettings": {
    "nodePath": "nodejs/node.exe",
    "serverScript": "server/main.js",
    "port": 1881,
    "host": "localhost",
    "stopServerOnExit": true
  },
  "loadingMessages": {
    "startingServer": "正在启动 {AppName} 服务器...",
    "initializingBrowser": "正在初始化浏览器...",
    "loadingApp": "正在加载 {AppName}...",
    "serverRestarting": "服务器连接丢失，正在重新启动...",
    "reloadingApp": "正在重新加载 {AppName}..."
  },
  "colors": {
    "backgroundColor": "#F5F7FA",
    "textColor": "#3C3C3C",
    "spinnerColor": "#007BFF",
    "versionColor": "#969696"
  }
}
```

### 退出选项配置

应用程序会自动判断是否停止服务器：

| 情况 | 行为 |
|------|------|
| **接管服务器** | 检测到服务器已运行时自动接管，关闭应用时**不停止**服务器 |
| **启动服务器** | 应用启动了新服务器，关闭应用时**自动停止**服务器 |

### 服务器接管功能

当检测到 FUXA 服务器已在运行时，应用程序会自动接管该服务器：

- 窗口标题显示 `[接管]` 状态（启动的服务器显示 `[启动]`）
- 显示服务器 PID 和运行时长
- 关闭应用程序时不会停止已接管的服务器

### 自定义品牌

修改以下配置项可以自定义应用标识：

- `appName` - 应用名称
- `windowTitle` - 窗口标题
- `logoPath` - Logo 图片路径
- `versionText` - 版本号

## 📸 截图

<p align="center">
  <em>启动界面</em>
</p>

<p align="center">
  <em>FUXA 主界面</em>
</p>

## ⌨️ 快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl + P` | 打开打印对话框 |

## 📁 目录结构

```
FuxaWinform/
├── FuxaWinform.exe          # 主程序入口
├── app-config.json          # 应用程序配置文件
├── fuxa-logo.ico           # 应用程序图标
├── nodejs/                 # 内置 Node.js 运行时
│   └── node.exe
├── server/                 # FUXA 服务器代码
│   ├── main.js
│   └── ...
├── client/                 # FUXA 前端代码
│   └── dist/
├── logs/                   # 日志文件目录
└── _appdata/              # FUXA 数据存储目录
    └── settings.js
```

## 🔧 构建项目

### 环境要求

- .NET 10.0 SDK
- Visual Studio 2022 或 VS Code

### 构建步骤

```bash
# 克隆仓库
git clone https://github.com/jingshui127/FuxaWinform.git

# 进入项目目录
cd FuxaWinform

# 还原依赖
dotnet restore

# 构建项目
dotnet build --configuration Release

# 发布单文件版本
dotnet publish --configuration Release --self-contained true -r win-x64 /p:PublishSingleFile=true
```

## 📊 星尘监控 (Stardust)

FuxaWinform 集成了 [NewLife.Stardust](https://newlifex.com/blood/stardust) 分布式监控平台，可以实时跟踪应用运行状态和性能指标。

### 快速开始

1. **配置星尘平台地址**

   编辑 `stardust.config` 文件：
   ```json
   {
     "StarServer": "http://star.newlifex.com:6600",
     "AppName": "FuxaWinform",
     "Enable": true
   }
   ```

2. **查看监控数据**

   登录 [星尘平台](http://star.newlifex.com:6600)，在应用系统中可以看到 FuxaWinform 的运行状态：
   - 在线实例
   - 性能指标
   - 调用链路追踪
   - 异常日志

### 监控指标

| 埋点名称 | 说明 |
|---------|------|
| `FuxaWinform:MainForm_Load` | 应用启动耗时 |
| `FuxaWinform:StartServer` | 服务器启动耗时 |
| `FuxaWinform:LoadPage` | 页面加载耗时 |

### 自定义监控

在代码中使用 `Tracer` 添加自定义埋点：
```csharp
using var span = Tracer.Instance?.NewSpan("FuxaWinform:CustomOperation");
try
{
    // 业务逻辑
}
catch (Exception ex)
{
    span?.SetError(ex, null);
    throw;
}
```

## 📝 更新日志

### v1.3.1 (2026-03-01)

#### ✨ 新功能
- **服务器接管功能** - 支持接管已运行的 FUXA 服务器
  - 窗口标题显示 `[接管]` 或 `[启动]` 状态
  - 接管模式下关闭 APP 不会停止服务器
  - 显示服务器 PID 和运行时长

#### 🐛 问题修复
- **CMD 窗口闪现** - 优化进程创建，避免 CMD 窗口闪现
  - 使用 Windows API 设置进程创建标志
  - 注释掉 `MachineInfo.RegisterAsync()` 调用

#### 🔧 代码优化
- 重构 `Program.cs`，使用 `#region` 组织代码
- 优化启动流程，简化并行任务处理
- 改进健康检查机制
- 统一日志格式

### v1.3.0 (2026-02-28)

#### ✨ 新功能
- **星尘监控集成** - 集成 NewLife.Stardust 分布式监控平台
- **打印功能** - 支持打印预览和静默打印
- **自动恢复** - 服务器异常时自动重启
- **启动计时** - 显示应用启动耗时

#### 🎨 界面优化
- 全新的启动界面设计
- 支持自定义颜色和主题
- 响应式布局适配

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

本项目基于 MIT 许可证开源。

## 🙏 致谢

- [FUXA](https://github.com/frangoteam/FUXA) - 优秀的开源过程可视化平台
- [WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) - Microsoft Edge WebView2 控件
- [NewLife.Stardust](https://newlifex.com/blood/stardust) - 分布式监控平台

---

<p align="center">
  Made with ❤️ for FUXA Community
</p>
