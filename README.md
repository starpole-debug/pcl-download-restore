# PCL 下载恢复补丁（第三方）

修改已有的 PCL **2.99.0.0**，恢复被禁用的下载入口。第三方独立工具，非 PCL 官方项目。

原项目：[Meloong-Git/PCL](https://github.com/Meloong-Git/PCL)，作者：**龙腾猫跃**，[赞助原作者](https://meloong.com/afd/a/LTCat)。

## 下载与使用

**[下载补丁 v2 ZIP](https://github.com/starpole-debug/pcl-download-restore/raw/refs/heads/main/downloads/PCL-2.99.0.0-download-restore-v2.zip)**

1. 完整解压 ZIP，不要只取出 PS1。
2. 完全退出 PCL，双击 `Run.cmd`。
3. 选择你已有的 PCL EXE，等待提示“下载功能补丁已应用”。
4. 运行原路径的 EXE，进入“下载”。

仅支持程序集版本 **2.99.0.0** 且方法结构兼容的文件。**没有原 EXE 固定哈希白名单**；版本或结构不兼容会停止。补丁会生成相邻的 `.pcl-backup-时间-随机值.bak` 备份，再原子替换 EXE。请保留备份。

**桌面出现白色图标的 `.bak` 是原始备份。** Windows 可能重新排列替换后的 EXE 图标；按文件名、扩展名和路径确认，运行 `.exe`。补丁不会移动游戏目录。

## 还原

在解压目录打开 PowerShell：

```powershell
.\Restore-PCL.ps1 -Path '你的启动器完整路径.exe' -Restore
```

多个备份时增加 `-Backup '备份完整路径.bak'`。也可向 `Run.cmd` 传入相同参数。还原前会保留当前 EXE。

## 原理与范围

编译期限制使部分方法体缺失，不能仅翻转一个布尔值。工具使用 dnlib 从公开源码构建的 `restore-methods.dat` 导入五个方法体，重新映射引用并审计其他方法、资源和字段常量。来源程序集只用于读取方法体，不会被执行。

恢复内容：整合包导入与安装分派、MC 版本列表、版本修改页 MC 选项、整合包版本安装按钮。补丁不替换原登录与更新方法；官方更新可能覆盖补丁，亦不屏蔽强制更新。不保证下载源持续可用或所有在线功能正常。

已测试：Windows PowerShell 5 应用与还原、同版本不同哈希文件、重复应用、拒绝不支持版本；UI 实际完成 b1.8.1 下载和安装，客户端 SHA1 与清单一致。没有完成新微软登录或所有整合包、加载器组合验证。详见 [验证记录](package/verification-result.txt) 与 [使用说明](package/使用说明.md)。

脚本不读取账号配置，不上传文件。内部来源文件校验和原文件变化检查不属于原 EXE 哈希白名单。

## 源码与构建

- `helper/`：命令行入口；`analyzer/`：方法恢复和审计。
- `package/`：完整运行目录；`downloads/`：可分享 ZIP。
- `payload-source/PCL/`：方法体来源的完整公开源码，含 MeloongCore 源码及修改。

在 Windows 安装 .NET 9 SDK 或更新版本后：

```powershell
.\build-package.ps1
```

来源基于 PCL 标签 `2.99.0.0`，提交 `6b9a50df911c9d68b69b24c979efba477fb14b67`；MeloongCore 提交 `98157d65c7627721c816c02e8be96ac4fe61d615`。修改包括 `RestrictMcDownload=False`、独立互斥标识及第三方名称与关于页署名。运行 `payload-source/PCL/build-restore.ps1` 可重建来源程序集。若替换 payload，需同步更新 helper 中 payload 校验值并重新构建；不承诺不同构建环境生成字节完全一致的程序集。

## 许可

这是公开源码的第三方项目，包含不同许可的组件。**整个仓库不能统一视为 MIT 或 OSI 认证开源软件。** PCL 源码及其派生方法体遵循 [PCL 原许可及指南](LICENCE)，dnlib 遵循 [MIT](licenses/dnlib-LICENSE.txt)。本仓库独立编写的补丁脚本、helper 和 analyzer 按 [MIT](LICENSE-tooling) 提供；该许可不覆盖 PCL、MeloongCore 或 dnlib。其他上游文件保留各自许可。

分享补丁时请同时提供本仓库地址及完整源码、署名和许可；不要分享个人登录配置或个人启动器文件。
