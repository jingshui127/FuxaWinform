# FUXA Desktop CS 使用手册

## 简介

FUXA Desktop CS 是一个基于 C# + WebView2 的 FUXA 桌面客户端应用程序。它将 FUXA 服务器和浏览器客户端打包成一个独立的 Windows 桌面应用，无需安装 Node.js 或其他依赖即可运行。

## 系统要求

- **操作系统**: Windows 10 或 Windows 11 (64位)
- **运行时**: .NET 10.0 或更高版本（已包含在发布包中）
- **浏览器组件**: Microsoft Edge WebView2 Runtime（已自动安装或包含）

## 目录结构

```
FUXA-Desktop-CS/
├── FUXADesktop.exe          # 主程序入口
├── app-config.json          # 应用程序配置文件
├── favicon.ico             # 应用程序图标
├── nodejs/                 # 内置 Node.js 运行时
│   └── node.exe
├── server/                 # FUXA 服务器代码
│   ├── main.js
│   └── ...
├── client/                 # FUXA 前端代码
│   └── dist/
├── Log/                    # 日志文件目录
└── _appdata/              # FUXA 数据存储目录
    └── settings.js
```

## 快速开始

### 1. 启动应用程序

双击 `FUXADesktop.exe` 启动应用程序。

### 2. 启动过程

应用程序启动时会显示加载界面，包含以下步骤：
1. **正在启动 FUXA 服务器...** - 启动内置的 Node.js 服务器
2. **正在初始化浏览器...** - 初始化 WebView2 浏览器组件
3. **正在加载 FUXA...** - 加载 FUXA 界面

#### 启动界面特点
- **旋转动画**：Logo 会显示旋转动画，提升视觉体验
- **状态显示**：实时显示当前启动阶段
- **秒数计数**：显示启动耗时（秒）
- **版本信息**：显示应用程序版本号

#### Logo 加载优先级
应用程序会按以下顺序尝试加载 Logo：
1. 配置文件中指定的 `logoPath`（默认为 `favicon.ico`）
2. `favicon.ico`（通用图标名称）
3. `fuxa-logo.ico`（兼容旧版本）

如果所有路径都找不到 Logo，会显示默认的旋转加载图标。

### 3. 访问 FUXA

启动完成后，应用程序会自动显示 FUXA 界面。默认访问地址为 `http://localhost:1881`。

## 配置说明

### 配置文件位置

配置文件 `app-config.json` 位于应用程序根目录，与 `FUXADesktop.exe` 同级。

**配置文件查找顺序：**
1. 首先查找当前工作目录下的 `app-config.json`
2. 如果找不到，则使用应用程序目录下的 `app-config.json`

### 配置文件 (app-config.json)

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
  "logoPath": "favicon.ico",
  "versionText": "v1.4.0",
  "loadingMessages": {
    "startingServer": "正在启动 {AppName} 服务器...",
    "initializingBrowser": "正在初始化浏览器...",
    "loadingApp": "正在加载 {AppName}...",
    "serverRestarting": "服务器连接丢失，正在重启...",
    "reloadingApp": "正在重新加载 {AppName}..."
  },
  "colors": {
    "backgroundColor": "#F5F7FA",
    "backgroundColor2": "#EBF0F5",
    "textColor": "#3C3C3C",
    "versionColor": "#969696",
    "spinnerColor": "#007BFF",
    "spinnerBackgroundColor": "#DCDCDC"
  }
}
```

### 配置项说明

#### 基本设置

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `appName` | 应用程序名称（用于启动画面、错误提示等） | FUXA |
| `windowTitle` | 窗口标题 | FUXA - Process Visualization |
| `logoPath` | 启动界面 Logo 路径 | favicon.ico |
| `versionText` | 版本号文本（显示格式：{AppName} {versionText}） | v1.3.0 |

#### 窗口设置 (windowSettings)

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `windowSettings.width` | 窗口初始宽度 | 1400 |
| `windowSettings.height` | 窗口初始高度 | 900 |
| `windowSettings.minWidth` | 窗口最小宽度 | 800 |
| `windowSettings.minHeight` | 窗口最小高度 | 600 |

#### 服务器设置 (serverSettings)

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `serverSettings.nodePath` | Node.js 可执行文件路径（相对应用程序目录） | nodejs/node.exe |
| `serverSettings.serverScript` | FUXA 服务器启动脚本路径（相对应用程序目录） | server/main.js |
| `serverSettings.port` | FUXA 服务器端口号 | 1881 |
| `serverSettings.host` | FUXA 服务器主机地址 | localhost |
| `serverSettings.stopServerOnExit` | 退出时是否停止后台服务器 | true |
| `serverSettings.askBeforeStopServer` | 退出时是否询问是否停止服务器 | false |

#### 界面设置

| 配置项 | 说明 | 默认值 |
|--------|------|--------|
| `loadingMessages` | 各阶段提示文本 | - |
| `colors` | 界面颜色配置 | - |

## 日志查看

应用程序日志存储在 `Log/` 目录下，文件命名格式为 `server_YYYYMMDD_HHMMSS.log`。

日志内容包括：
- 服务器启动/停止记录
- HTTP 请求记录
- 错误和异常信息
- 调试信息

## 故障排除

### 1. 应用程序无法启动

**问题**: 双击 FUXADesktop.exe 没有反应

**解决方案**:
1. 检查是否安装了 .NET 10.0 运行时
2. 检查 `nodejs/node.exe` 是否存在
3. 检查 `server/main.js` 是否存在
4. 查看 `Log/` 目录下的日志文件

### 2. 服务器启动失败

**问题**: 显示"服务器启动失败"错误

**解决方案**:
1. 检查端口 1881 是否被其他程序占用
2. 查看日志文件了解详细错误信息
3. 检查 `_appdata/settings.js` 配置文件是否正确

### 3. 界面显示空白

**问题**: 加载完成后界面空白

**解决方案**:
1. 检查 `client/dist/` 目录是否存在前端文件
2. 查看日志文件是否有错误信息
3. 尝试刷新页面（按 F5）

### 4. 应用程序闪退

**问题**: 应用程序启动后很快退出

**解决方案**:
1. 检查系统是否满足最低要求
2. 检查 WebView2 Runtime 是否已安装
3. 查看 Windows 事件查看器中的错误信息

### 5. 服务器连接丢失

**问题**: 运行过程中显示"服务器连接丢失"

**解决方案**:
1. 应用程序会自动尝试重启服务器
2. 如果自动重启失败，请手动重启应用程序
3. 检查系统资源是否充足（内存、CPU）

## 高级功能

### 自动健康检查

应用程序每 5 秒检查一次服务器状态。如果连续 3 次检查失败，会自动尝试重启服务器。

### 服务器重启

如果服务器意外停止，应用程序会：
1. 显示加载界面
2. 尝试重新启动服务器
3. 重新加载 FUXA 界面

### 多实例支持

如果端口 1881 已被其他 FUXA 实例占用，应用程序会直接连接到现有服务器，不会强制终止其他进程。

### 打印功能

应用程序支持打印功能：
- **快捷键**: 按 `Ctrl+P` 打开打印对话框
- **打印设置**: 支持打印背景颜色和图像

### 退出选项

通过配置 `serverSettings.stopServerOnExit` 和 `serverSettings.askBeforeStopServer` 可以控制退出行为。

**注意**: `askBeforeStopServer` 优先级高于 `stopServerOnExit`。如果设置为 `true`，关闭窗口时会先显示确认对话框。

#### 配置组合说明

| 配置组合 | 行为 |
|---------|------|
| `askBeforeStopServer: true` | **优先**：关闭窗口时显示确认对话框，用户可选择「是/否/取消」 |
| `stopServerOnExit: true`, `askBeforeStopServer: false` | 退出时自动停止服务器（默认） |
| `stopServerOnExit: false`, `askBeforeStopServer: false` | 退出时不停止服务器，保持后台运行 |

#### 确认对话框说明

当 `askBeforeStopServer: true` 时，关闭窗口会显示以下对话框：

```
确定要退出并停止服务器吗？

选择 '是'：退出并停止服务器
选择 '否'：退出但不停止服务器
选择 '取消'：取消退出

[是] [否] [取消]
```

- **是** → 停止服务器并退出应用程序
- **否** → 不停止服务器，直接退出应用程序
- **取消** → 取消关闭操作，保持应用程序运行

#### 配置示例

**1. 默认配置（自动停止服务器）**
```json
"serverSettings": {
    "stopServerOnExit": true,
    "askBeforeStopServer": false
}
```

**2. 退出时询问用户**
```json
"serverSettings": {
    "stopServerOnExit": true,
    "askBeforeStopServer": true
}
```

**3. 保持服务器后台运行（不询问）**
```json
"serverSettings": {
    "stopServerOnExit": false,
    "askBeforeStopServer": false
}
```

## 数据存储

FUXA 的数据存储在 `_appdata/` 目录下：
- `settings.js` - 应用程序设置
- `projects/` - 项目文件
- `Log/` - FUXA 内部日志

## 更新说明

### 更新应用程序

1. 备份 `_appdata/` 目录
2. 下载新版本并解压
3. 将备份的 `_appdata/` 复制到新版本目录
4. 启动新版本

### 更新 FUXA 服务器

1. 停止应用程序
2. 备份 `_appdata/` 目录
3. 替换 `server/` 和 `client/` 目录
4. 启动应用程序

## 技术说明

### 架构

- **前端**: WebView2 (Microsoft Edge 内核)
- **后端**: Node.js + Express + Socket.IO
- **进程通信**: HTTP/WebSocket

### 端口使用

- **1881**: FUXA Web 服务器端口
- **随机端口**: WebView2 调试端口（如启用调试）

### 进程管理

- 主进程: FUXADesktop.exe (C# WinForms)
- 子进程: node.exe (FUXA 服务器)
- 子进程在父进程退出时会自动终止

## 安全说明

1. 默认监听 127.0.0.1:1881，仅本地可访问
2. 如需远程访问，请修改 `_appdata/settings.js` 中的 `uiHost` 配置
3. 建议在生产环境中使用反向代理和 HTTPS

## 支持与反馈

如有问题，请查看日志文件或联系技术支持。

---

**版本**: v1.3.2  
**最后更新**: 2026-03-02
