# COM3D2.XtMasterSlave.Plugin


Originally fixed and tested on COM3D2 2.39.1.

Separate builds are available for COM3D2 (2.x, Unity 5.6) and COM3D2.5 (3.x, Unity 2022.3).
The COM3D2.5 build still needs in-game validation; successful compilation is not a gameplay test.

<br>
<br>
<br>

This plugin allows you to link the maid to the master's bones, making the maid perform the master's motions, and vice versa.

You can call a sub-maid to stand in for the master during nighttime visits, or use the master as a back-up dancer, and do all sorts of things depending on your ideas.


<br>
<br>
<br>

Original author @NnAone2cmg https://x.com/NnAone2cmg at https://ux.getuploader.com/cm3d2_k/download/20

Modifier by https://git.coder.horse/ghorsington/XTMS.132

Modifier by https://github.com/customordermaid3d2/CM3D2.XtMasterSlave.Plugin

Modifier by InoryS

<br>
<br>
<br>

The readme says:
```
※本プラグインの改造、配布はご自由に。何かありましたらnn@ (@NnAone2cmg)まで 
```
So I suppose free to use, modify, and redistribute the source code.

But, it the author don't want me to do this, please let me know and I will delete this repo.


### How to install
This is a Sybaris UnityInjector plugin

Install the archive matching your game into its `Sybaris\UnityInjector` directory.

- COM3D2 2.x: use `COM3D2.XtMasterSlave.Plugin-<version>.zip`, containing both
  `COM3D2.XtMasterSlave.IKO132.xdll` and `COM3D2.XtMasterSlave.Plugin.dll`.
- COM3D2.5 3.x: use `COM3D2.5.XtMasterSlave.Plugin-<version>.zip`. Its IK adapter is built into
  `COM3D2.XtMasterSlave.Plugin.dll`; do not copy the 2.x IKO132 module into this installation.

The plugin DLL has the same name in both archives. **Do not install both builds into the same game.**
Back up customized `Config/` files before extracting; install only the plugin binaries to keep your settings.

and you also need PluginExt.dll (https://github.com/asm256/ExPluginBase/releases).

### Build

Run from the repository root with the .NET SDK installed:

```powershell
dotnet build -c Release
dotnet build -c COM3D25_UNITY_2022-Release
```

The first command builds the net35 plugin and IKO132 module. The second builds only the net48 plugin;
the solution excludes the 2.x-only IKO132 project from COM3D2.5 configurations.
CI and releases produce separate zip archives for each game version.










<br>
<br>
<br>
<br>
<br>
<br>




# COM3D2.XtMasterSlave.Plugin

最初针对 COM3D2 2.39.1 进行了修复和测试。

现在分别提供 COM3D2（2.x，Unity 5.6）和 COM3D2.5（3.x，Unity 2022.3）的构建。
COM3D2.5 版本仍需游戏内验证，编译通过不代表已经完成实机测试。

<br>
<br>
<br>

此插件允许您将女仆与主人的骨骼联系起来，让女仆执行主人的动作，反之亦然。

您可以在夜间拜访时叫一个副女仆代替主人，或者让主人作为伴舞，并根据您的想法做各种事情。

<br>
<br>
<br>

原作者@NnAone2cmg https://x.com/NnAone2cmg 于 https://ux.getuploader.com/cm3d2_k/download/20

由 https://git.coder.horse/ghorsington/XTMS.132 修改

由 https://github.com/customordermaid3d2/CM3D2.XtMasterSlave.Plugin 修改

由 InoryS 修改

<br>
<br>
<br>

自述文件说：
```
※本プラグインの改造、配布はご自由に。何かありましたらnn@ (@NnAone2cmg)まで 
```
所以我想可以自由使用、修改和重新分发源代码。

但是，如果作者不想让我这样做，请告诉我，我将删除这个存储库。


### 如何安装

这是一个 Sybaris UnityInjector 插件

请选择与你的游戏版本对应的安装包，解压到该游戏的 `Sybaris\UnityInjector` 目录。

- COM3D2 2.x：使用 `COM3D2.XtMasterSlave.Plugin-<version>.zip`，其中包含
  `COM3D2.XtMasterSlave.IKO132.xdll` 和 `COM3D2.XtMasterSlave.Plugin.dll`。
- COM3D2.5 3.x：使用 `COM3D2.5.XtMasterSlave.Plugin-<version>.zip`，IK 适配器已内置于
  `COM3D2.XtMasterSlave.Plugin.dll`，不要额外复制 2.x 的 IKO132 模块。

两个版本的插件 DLL 文件名相同，**不要在同一个游戏中混装**。
覆盖安装前请备份自定义的 `Config/`；如果要保留设置，只解压插件二进制文件。

您还需要 PluginExt.dll (https://github.com/asm256/ExPluginBase/releases)。

### 构建

安装 .NET SDK 后，在仓库根目录执行：

```powershell
dotnet build -c Release
dotnet build -c COM3D25_UNITY_2022-Release
```

第一条命令构建 net35 插件和 IKO132 模块；第二条只构建 net48 插件，不会构建 2.x 专用的 IKO132 项目。
CI 和发布流程会分别生成两个游戏版本的 zip 安装包。
