# Kontour

一个基于 .NET MAUI 开发的桌面端音频文件预览工具，专为 KONTAKT 乐器库浏览和音频文件管理而设计。

## 功能特性

### 🎵 音频格式支持
- 常见音频格式：WAV、MP3、OGG
- KONTAKT 乐器格式：NKI、NKM、NKSN
- VST 预设格式：FXP
- MIDI 文件：MID

### 🎹 KONTAKT 乐器预览
- 自动查找 `.previews` 文件夹中的预览音频
- 支持一键播放乐器预览
- 无需加载完整乐器库即可试听

### 📁 文件管理
- 驱动器快速访问
- 树形目录导航
- 文件过滤器（按扩展名筛选）
- 收藏夹功能
- 拖拽支持（Windows 平台）

### 🌍 多语言支持
- 简体中文
- 繁体中文
- English
- 日本語
- 한국어

### 🎚️ 音频播放控制
- 播放/暂停
- 进度条拖动
- 音量调节
- 实时时间显示

## 系统要求

- **操作系统**：Windows 10 (build 19041) 或更高版本
- **.NET 版本**：.NET 10.0
- **平台**：Windows 桌面端

## 快速开始

### 构建项目

```bash
# 使用 Visual Studio 2022 打开解决方案
Kontour.sln

# 或使用命令行构建
dotnet build
```

### 运行项目

```bash
# 调试模式
.\run-debug.bat

# 或使用 dotnet 命令
dotnet run
```

### 发布应用

```bash
# 使用发布脚本
.\publish.bat
```

## 使用说明

### 基本操作
1. **选择驱动器**：在顶部下拉菜单中选择要浏览的驱动器
2. **导航目录**：
   - 左侧树形结构：展开/折叠目录
   - 右侧文件列表：双击进入文件夹
3. **播放音频**：单击音频文件或 KONTAKT 乐器文件即可自动播放

### 文件过滤
- 使用顶部的扩展名过滤器勾选需要显示的文件类型
- 支持同时显示/隐藏文件夹

### 收藏夹
- 在左侧树形目录中，右键或使用工具栏将常用目录添加到收藏夹
- 点击收藏夹项目快速导航

### KONTAKT 乐器预览
- 对于 `.nki`、`.nkm`、`.nksn` 文件，工具会自动在同级目录的 `.previews` 文件夹中查找同名的音频文件
- 优先级：`.ogg` > `.mp3` > `.wav`

## 项目结构

```
Kontour/
├── Behaviors/          # 行为（拖拽功能）
├── Controls/           # 自定义控件
├── Converters/         # 值转换器
├── Models/             # 数据模型
├── Services/           # 业务服务
├── ViewModels/         # 视图模型
├── Views/              # 视图页面
├── Resources/          # 资源文件
│   ├── Localization/   # 多语言资源
│   ├── Styles/         # 样式文件
│   └── Raw/            # 原始资源
└── Platforms/          # 平台特定代码
    └── Windows/        # Windows 平台实现
```

## 技术栈

- **框架**：.NET 10.0 + MAUI
- **架构模式**：MVVM
- **UI 框架**：MAUI Controls
- **目标平台**：Windows 桌面端

## 开发计划

- [ ] 支持更多音频格式
- [ ] 添加音频波形显示
- [ ] 支持播放列表
- [ ] 添加批量转换功能
- [ ] 跨平台支持（macOS、Linux）

## 许可证

本项目采用 MIT 许可证。

## 联系方式

如有问题或建议，欢迎提交 Issue 或 Pull Request。
