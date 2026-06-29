# BetterPCL

基于 [PCL N Edition](https://github.com/MUXUE1230/PCL-N) 社区二次开发的 Minecraft 启动器，新增 **4399 / 网易社区账号登录** 和 **NetEase Java 版 MITM 本地代理**。

## 新增功能

### 🔐 社区账号登录
- **4399 登录** — 账号密码 + 手机验证码双模式，调用 ptlogin.4399.com 真实 API
- **网易登录** — 邮箱密码 + 手机验证码双模式，调用 NetEase MKey API (`service.mkey.163.com`)
- 登录档案自动持久化，支持多账号切换

### 🌐 NetEase Java 代理
- 搜索 NetEase 网络游戏服务器
- 创建 / 选择游戏角色
- 启动本地 **MITM 代理**（纯代理模式，不启动白端客户端）
- 在 Minecraft Java Edition 连接 `127.0.0.1:{端口}` 即可加入服务器
- 会话管理：查看运行中的代理、停止会话

## 编译

```bash
dotnet restore
dotnet build "Plain Craft Launcher 2/Plain Craft Launcher 2.csproj" -c Release
```

依赖：.NET 10.0 SDK、Windows（WPF）、OpenNEL SDK。

## 项目结构

| 目录 | 说明 |
|------|------|
| `Plain Craft Launcher 2/` | WPF 主程序（启动器 UI） |
| `PCL.Core/` | 启动器核心库 |
| `PCL.Online/` | OpenNEL 集成层（登录 + 代理服务） |
| `PCL.Application/` | 应用层（档案管理、Minecraft 下载等） |

## License

Apache 2.0 — 详见 [LICENSE](./LICENSE)

---

*Copyright © 龙腾猫跃 2016. Community fork with additions.*
