# Spider Codex

Windows 桌面启动器。运行后播放透明蛛丝动画，点击 SPIDERMAN WORKSPACE 打开 Windows 注册的 Codex 应用。按 Esc 或点击右上角 × 可关闭动画。

## 下载

**[下载 Windows x64 · v0.1.0-beta](https://github.com/Iris-change/spider-codex/releases/download/v0.1.0-beta/Spider-Codex-Windows-x64.zip)** · [发布说明](https://github.com/Iris-change/spider-codex/releases/tag/v0.1.0-beta)

## 动画演示

![手腕发射蛛丝、蛛网铺开并收拢成蜘蛛侠头像](docs/wrist-demo.gif)

[观看清晰版短片](https://github.com/Iris-change/spider-codex/blob/main/docs/wrist-demo.mp4)

## 构建与运行

需要 64 位 Windows、系统里的 .NET Framework 4.x 构建工具，以及通过 Microsoft Store 安装的 Codex。Codex 的注册应用 ID 为 `OpenAI.Codex_2p2nqsd0c76g0!App`。

在项目目录运行 PowerShell：

```powershell
.\build.ps1
```

运行包解压后，双击 `Spider-Shortcut.exe`。透明蛛丝动画、迈尔斯头像、进入按钮及桌面图标都随包提供。创建桌面快捷方式时可使用 `Spider-Codex.ico`。请让 `detail-transparent-frames` 和 exe 保持在同一目录。

## 许可与素材

启动器源码和构建脚本采用 [MIT](LICENSE)。动画帧和图标随本项目提供，用于展示和运行启动器。MIT 的授权范围限于代码。蜘蛛侠、迈尔斯及相关标志的权利归各自权利人。

## 测试版

已在本机验证动画和原生 Codex 跳转。尚待在全新 Windows 环境验证冷启动及未安装时的提示。
