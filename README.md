# RON Server Browser

Ready Or Not（严阵以待）Steam 大厅浏览器，基于 Steam Lobby 系统实现服务器列表的浏览、搜索和加入功能。

## 技术栈

| 项目 | 说明 |
|------|------|
| 框架 | .NET 8.0 (Windows) |
| UI | WPF (Windows Presentation Foundation) |
| Steam 集成 | [Facepunch.Steamworks](https://github.com/Facepunch/Facepunch.Steamworks) 2.3.3 |
| JSON 处理 | Newtonsoft.Json 13.0.3 |

## 构建与运行

### 前置要求

- Windows (x64)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (v17.13+)
- Steam 客户端（已登录且已安装 Ready Or Not）

### 构建

```bash
dotnet restore
dotnet build --configuration Release
```

### 运行

```bash
dotnet run --project RON_Server-Browser
```

### 发布

```bash
dotnet publish RON_Server-Browser/RON_Server-Browser.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=false
```

> 发布为自包含单文件 exe，无需用户额外安装 .NET 运行时。不启用裁剪以避免 Steamworks 反射问题。

## 项目结构

```
RON_Server-Browser/
├── .github/workflows/          # CI/CD 配置
│   ├── pr-check.yml            # PR 构建检查
│   └── release.yml             # 发布构建与 GitHub Release
├── RON_Server-Browser/         # 主项目
│   ├── Extensions/             # WPF 标记扩展
│   │   └── LocalizeExtension.cs
│   ├── Models/                 # 数据模型
│   │   ├── RonConfiguration.cs
│   │   └── ServerModel.cs
│   ├── Resources/Languages/    # 语言包
│   │   ├── en-US.json
│   │   └── zh-CN.json
│   ├── Services/               # 核心服务
│   │   ├── LocalizationService.cs
│   │   ├── SteamLauncher.cs
│   │   ├── SteamLobbyService.cs
│   │   └── SteamManager.cs
│   ├── App.xaml                # 应用入口
│   ├── MainWindow.xaml         # 主窗口 UI
│   └── MainWindow.xaml.cs      # 主窗口逻辑
├── RON_Server-Browser.sln      # 解决方案文件
└── LICENSE                     # MIT 许可证
```

## 配置

配置文件位于 `%AppData%/RON-Server-Browser/config.json`：

| 字段 | 说明 | 默认值 |
|------|------|--------|
| `GamePath` | 游戏安装路径 | 自动检测 Steam 默认安装位置 |
| `Language` | 界面语言 | `zh-CN` |

## 发布流程

推送 `v*` 格式的 tag 即可触发 GitHub Actions 自动构建并发布：

```bash
git tag v1.0.0
git push origin v1.0.0
```

发布产物为 `RON_Server-Browser-{version}-win-x64.zip`，包含自包含单文件可执行程序。

## 许可证

[MIT License](LICENSE)
