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
  <a href="#截图">截图</a>
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

## 🚀 快速开始

### 下载安装

1. 从 [Releases](https://github.com/jingshui127/FuxaWinform/releases) 页面下载最新版本
2. 解压到任意目录
3. 双击 `FuxaWinform.exe` 启动

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

## 💻 系统要求

- **操作系统**: Windows 10 或 Windows 11 (64位)
- **运行时**: .NET 10.0（已包含在发布包中）
- **浏览器组件**: Microsoft Edge WebView2 Runtime（已自动安装或包含）

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
    "stopServerOnExit": true,
    "askBeforeStopServer": false
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

| 配置 | 说明 |
|------|------|
| `stopServerOnExit: true` | 退出时自动停止服务器 |
| `askBeforeStopServer: true` | 退出时询问是否停止服务器 |

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

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可证

本项目基于 MIT 许可证开源。

## 🙏 致谢

- [FUXA](https://github.com/frangoteam/FUXA) - 优秀的开源过程可视化平台
- [WebView2](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) - Microsoft Edge WebView2 控件

---

<p align="center">
  Made with ❤️ for FUXA Community
</p>
