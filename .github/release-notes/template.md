# Note

Choose the archive matching your game; do not install both builds into the same game:

- `COM3D2.XtMasterSlave.Plugin-{{VERSION}}.zip`: COM3D2 2.x, with the IKO132 module.
- `COM3D2.5.XtMasterSlave.Plugin-{{VERSION}}.zip`, when available: COM3D2.5 3.x, with a built-in IK adapter and no IKO132 module.

The COM3D2 archive ships the IKCtrl v1.32 IK module (`COM3D2.XtMasterSlave.IKO132.xdll`). The plugin picks
its IK module at runtime by probing the game assembly, and every COM3D2 from 1.32 on (i.e. all 2.x)
has the `IKCtrlData.IKSettingData` type that selects this one. The 117 / 118 modules are only used by
pre-1.32 builds and are not included. The module is loaded by file name, so it has to stay in the same
folder as the plugin dll — the plugin does not fall back to anything else if it is missing.

# Install

This is a Sybaris / UnityInjector plugin. Extract the zip into `COM3D2\Sybaris\UnityInjector\`, so that
the files end up at:

```
COM3D2/Sybaris/UnityInjector/COM3D2.XtMasterSlave.Plugin.dll
COM3D2/Sybaris/UnityInjector/COM3D2.XtMasterSlave.IKO132.xdll
COM3D2/Sybaris/UnityInjector/Config/XtMasterSlave.ini
COM3D2/Sybaris/UnityInjector/Config/XtMasterSlave_Yotogi.ini
COM3D2/Sybaris/UnityInjector/Config/xtYotogiVoiceTable.csv
COM3D2/Sybaris/UnityInjector/orginal readme/
```

For COM3D2.5, use that game's `Sybaris\UnityInjector\` directory and omit the IKO132 module.
The plugin DLL has the same name in both archives, but the builds are not interchangeable.

`PluginExt.dll` is required and is **not** bundled — it is shared by every ExPluginBase plugin.
Get it from https://github.com/asm256/ExPluginBase/releases and put it in the same folder.

The archive also ships the default `Config/`. **If you already have a `Config/` you have customized,
back it up before extracting** — extracting over it replaces your settings with the defaults. To keep
your settings, extract only the plugin DLL and, for COM3D2 2.x, its IKO132 module.

The plugin writes the config ini files back as **UTF-8 with BOM**. Save them the same way when you edit
by hand: the reader also accepts BOM-less UTF-8 and Shift-JIS, but only a BOM states the encoding
outright — without one it has to guess, and a file saved in the wrong one shows up as garbled text.

# Changes

{{CHANGELOG}}

<br>
<br>
<br>

# 说明

请选择对应游戏的安装包，不要在同一个游戏中混装：

- `COM3D2.XtMasterSlave.Plugin-{{VERSION}}.zip`：COM3D2 2.x，附带 IKO132 模块。
- `COM3D2.5.XtMasterSlave.Plugin-{{VERSION}}.zip`（如有提供）：COM3D2.5 3.x，IK 适配器已内置，不含 IKO132 模块。

COM3D2 包内附带的是 IKCtrl v1.32 的 IK 模块（`COM3D2.XtMasterSlave.IKO132.xdll`）。插件在运行期探测游戏程序集
来决定用哪个模块，COM3D2 1.32 及以后（也就是所有 2.x）都有 `IKCtrlData.IKSettingData` 这个类型，选中的
就是它。117 / 118 只给 1.32 以前的版本用，不在包内。模块是按文件名加载的，所以它必须和插件 dll 放在
同一个目录里——缺了它插件不会回退到别的实现。

# 安装

这是一个 Sybaris / UnityInjector 插件。把 zip 解压到 `COM3D2\Sybaris\UnityInjector\`，最终文件位置应为：

```
COM3D2/Sybaris/UnityInjector/COM3D2.XtMasterSlave.Plugin.dll
COM3D2/Sybaris/UnityInjector/COM3D2.XtMasterSlave.IKO132.xdll
COM3D2/Sybaris/UnityInjector/Config/XtMasterSlave.ini
COM3D2/Sybaris/UnityInjector/Config/XtMasterSlave_Yotogi.ini
COM3D2/Sybaris/UnityInjector/Config/xtYotogiVoiceTable.csv
COM3D2/Sybaris/UnityInjector/orginal readme/
```

COM3D2.5 请使用该游戏的 `Sybaris\UnityInjector\` 目录，不要复制 IKO132 模块。
两个安装包的插件 DLL 文件名相同，但不能互换使用。

`PluginExt.dll` 是必需的，但**不在包内**——它由所有 ExPluginBase 插件共用。
请从 https://github.com/asm256/ExPluginBase/releases 下载后放进同一个目录。

包内还带有默认的 `Config/`。**如果你已经有一份改过的 `Config/`，解压前请先备份**——
直接覆盖会把你的设置换回默认值。想保留设置就只解压插件 DLL；COM3D2 2.x 还需解压 IKO132 模块。

插件回写配置 ini（`XtMasterSlave.ini`、`XtMasterSlave_Yotogi.ini`，以及
`Config/XtMasterSlave/User_SaveData/` 下的预设）时统一用 **UTF-8 with BOM**。手工编辑也请存成
UTF-8 with BOM——读取端同样接受无 BOM 的 UTF-8 和 Shift-JIS，但只有带 BOM 时它才能直接确定编码，
否则得靠猜，存错编码就会变成乱码。

# 更改

{{CHANGELOG_ZH}}

**Full Changelog**: {{FULL_CHANGELOG_URL}}
