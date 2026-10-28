#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Function: 组装发布 zip / Assemble the release zip
"""组装发布 zip。

解压到 COM3D2\\Sybaris\\UnityInjector\\ 即可，包内布局与插件运行时读取的路径一致：

    COM3D2.XTMasterSlave.Plugin.dll     插件本体
    COM3D2.XtMasterSlave.IKO132.xdll    IKCtrl v1.32 的 IK 模块（Assembly.LoadFile 动态加载）
    Config/                             默认配置 + 预设
    orginal readme/                     原作者帮助文档

Config/ 与 orginal readme/ 逐字节复制，不做任何转码。仓库里的 Config/*.ini 是 Shift-JIS(CP932)，
插件侧的 XtIni 按 BOM → 严格 UTF-8 → CP932 的顺序解码，两种编码都能读；但打包时把 CP932 转成
UTF-8 正是 v0.0.6.1 / v0.0.6.2 读坏配置的根因（老版本走 PluginExt 的 Win32 API，只认 ANSI 代码页），
所以这里原样搬运。

游戏本体的程序集（Assembly-CSharp*、UnityEngine*）和 Sybaris 的 UnityInjector.dll 一律不进包 ——
它们由游戏提供。PluginExt.dll 也不进包，它是多个 ExPluginBase 插件共用的依赖，由用户单独安装。

The zip mirrors the layout the plugin expects after extracting into COM3D2\\Sybaris\\UnityInjector\\.
Config/ and orginal readme/ are copied byte for byte and never transcoded: the ini files in the repo
are Shift-JIS(CP932) and XtIni decodes BOM -> strict UTF-8 -> CP932, so both work. Transcoding at
package time is exactly what corrupted the config in v0.0.6.1 / v0.0.6.2, so the bytes pass through.
Game assemblies (Assembly-CSharp*, UnityEngine*) and UnityInjector.dll never ship; the game provides
them. PluginExt.dll is left out too: it is shared by every ExPluginBase plugin and installed separately.
"""

import argparse
import os
import sys
import zipfile

# 打包进 zip 根目录的两个构建产物 / The two build outputs that go to the zip root
#
# 构建产物是 COM3D2.XTMasterSlave.Plugin.dll（csproj 的 AssemblyName，大写 TM），但历次发布
# 和 README 里用的都是小写 t 的 COM3D2.XtMasterSlave.Plugin.dll。程序集标识不受文件名影响
# （仍然是 XTMasterSlave，IKO132 和旧的 IK 模块都按这个标识绑定），这里只是沿用发布时的小写
# 文件名 —— 否则老用户升级时旧文件不会被覆盖，两份 dll 会被 UnityInjector 各加载一次。
#
# The build output is COM3D2.XTMasterSlave.Plugin.dll (the csproj AssemblyName), but every past
# release and the README use the lowercase-t name. The assembly identity is unaffected by the file
# name, so the archive keeps the historical one: otherwise an upgrade would leave the old dll in
# place next to the new one and UnityInjector would load the plugin twice.
PLUGIN_DLL = "COM3D2.XtMasterSlave.Plugin.dll"
IK_XDLL = "COM3D2.XtMasterSlave.IKO132.xdll"

# 仓库里随包分发的目录 / Repo directories shipped inside the archive
PAYLOAD_DIRS = ["Config", "orginal readme"]

# 绝对不能进包的运行期依赖（游戏 / Sybaris / 共用插件提供）/ Runtime deps that must never ship
FORBIDDEN_NAMES = {
    "assembly-csharp.dll",
    "assembly-csharp-firstpass.dll",
    "unityengine.dll",
    "unityengine.ui.dll",
    "unityinjector.dll",
    "pluginext.dll",
    "jsonfx.json.dll",
}

# 固定时间戳，保证同样的输入产出同样的 zip / Fixed timestamp so identical inputs give identical zips
FIXED_DATE_TIME = (2020, 1, 1, 0, 0, 0)


def add_dir(zf, name):
    info = zipfile.ZipInfo(name.rstrip("/") + "/", date_time=FIXED_DATE_TIME)
    info.external_attr = (0o40755 << 16) | 0x10
    info.compress_type = zipfile.ZIP_STORED
    zf.writestr(info, b"")


def add_file(zf, data, arcname):
    info = zipfile.ZipInfo(arcname, date_time=FIXED_DATE_TIME)
    info.external_attr = 0o100644 << 16
    info.compress_type = zipfile.ZIP_DEFLATED
    zf.writestr(info, data, compresslevel=9)


def read_bytes(path):
    with open(path, "rb") as f:
        return f.read()


def payload_entries(repo_root):
    """(arcname, bytes) 列表，目录项对应 bytes=None / (arcname, bytes) pairs, None for a directory."""
    entries = []
    for dirname in PAYLOAD_DIRS:
        src_dir = os.path.join(repo_root, dirname)
        if not os.path.isdir(src_dir):
            sys.exit(f"::error::payload directory not found: {src_dir}")
        for root, dirs, files in os.walk(src_dir):
            dirs.sort()
            files.sort()
            rel_root = os.path.relpath(root, repo_root)
            # 目录项也要写，否则空目录（User_SaveData）在解压后不存在
            # Directory entries matter too, otherwise empty ones such as User_SaveData vanish
            entries.append((rel_root, None))
            for name in files:
                full = os.path.join(root, name)
                entries.append((os.path.join(rel_root, name), read_bytes(full)))
    return entries


def main():
    parser = argparse.ArgumentParser(description="Assemble the release zip")
    parser.add_argument("--version", required=True, help="例如 0.0.6.2 / e.g. 0.0.6.2")
    parser.add_argument("--plugin-dll", required=True, help="构建出的插件本体 / built plugin dll")
    parser.add_argument("--ik-dll", required=True, help="构建出的 IKO132.xdll / built IK module")
    parser.add_argument("--repo-root", default=".", help="仓库根目录 / repository root")
    parser.add_argument("--output-dir", default="dist", help="zip 输出目录 / output directory")
    args = parser.parse_args()

    repo_root = os.path.abspath(args.repo_root)

    for path in (args.plugin_dll, args.ik_dll):
        if not os.path.isfile(path):
            sys.exit(f"::error::build output not found: {path}")

    # zip 名沿用历史发布：COM3D2.XtMasterSlave.Plugin-<version>.zip
    # The name follows the existing releases
    os.makedirs(os.path.join(repo_root, args.output_dir), exist_ok=True)
    zip_path = os.path.join(repo_root, args.output_dir,
                            f"COM3D2.XtMasterSlave.Plugin-{args.version}.zip")

    with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        add_file(zf, read_bytes(args.plugin_dll), PLUGIN_DLL)
        add_file(zf, read_bytes(args.ik_dll), IK_XDLL)
        for arcname, data in payload_entries(repo_root):
            if data is None:
                add_dir(zf, arcname)
            else:
                add_file(zf, data, arcname)

    # 自检：两个产物必须在根目录，不许有 pdb，不许混进游戏 / Sybaris / 共用插件的程序集
    # Sanity check: both outputs at the root, no pdb, no game / Sybaris / shared plugin assemblies
    with zipfile.ZipFile(zip_path) as zf:
        names = zf.namelist()
    for required in (PLUGIN_DLL, IK_XDLL, "Config/XtMasterSlave.ini"):
        if required not in names:
            sys.exit(f"::error::{required} missing from {zip_path}")
    bad = [n for n in names
           if n.lower().endswith(".pdb") or os.path.basename(n).lower() in FORBIDDEN_NAMES]
    if bad:
        sys.exit(f"::error::unexpected entries in {zip_path}: {bad}")

    size = os.path.getsize(zip_path)
    print(f"built {zip_path} ({len(names)} entries, {size} bytes)")
    print(f"  root files: {PLUGIN_DLL}, {IK_XDLL}")


if __name__ == "__main__":
    main()
