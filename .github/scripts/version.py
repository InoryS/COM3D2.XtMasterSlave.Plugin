#!/usr/bin/env python3
# -*- coding: utf-8 -*-
# Function: 读取插件版本号 / Print the plugin version
"""打印插件版本号。

唯一来源是 COM3D2.XTMasterSlave.Plugin.cs 里的 PLUGIN_VERSION 常量 ——
游戏里齿轮菜单显示的版本号、PluginVersion 特性声明的版本都取它。
发版时用它和 tag 比对，避免漏改版本号。
The single source is the PLUGIN_VERSION constant in COM3D2.XTMasterSlave.Plugin.cs.
The gear menu shows it and the PluginVersion attribute declares it. A release compares
it against the tag so a forgotten bump fails the build instead of shipping a wrong version.

改版本号时要动的地方（本脚本会校验它们彼此一致）：
  - `public readonly static string PLUGIN_VERSION = "x.y.z.w";`
  - `[PluginName(...), PluginVersion("x.y.z.w")]`，COM3D2 / CM3D2 两个分支各一处
"""

import os
import re
import sys

PLUGIN_SOURCE = os.path.join("COM3D2.XTMasterSlave.Plugin", "COM3D2.XTMasterSlave.Plugin.cs")

PLUGIN_VERSION_PATTERN = re.compile(r'PLUGIN_VERSION\s*=\s*"([^"]+)"')
PLUGIN_ATTR_PATTERN = re.compile(r'PluginVersion\s*\(\s*"([^"]+)"\s*\)')


def main():
    if not os.path.isfile(PLUGIN_SOURCE):
        sys.exit(f"::error::file not found: {PLUGIN_SOURCE}")

    with open(PLUGIN_SOURCE, encoding="utf-8-sig") as f:
        source = f.read()

    match = PLUGIN_VERSION_PATTERN.search(source)
    if not match:
        sys.exit(f"::error::PLUGIN_VERSION constant not found in {PLUGIN_SOURCE}")
    version = match.group(1)

    # PluginVersion 特性出现在 COM3D2 / CM3D2 两个 #if 分支里，两处都必须与常量一致，
    # 否则游戏里显示的版本和菜单里的版本会不一样
    # The PluginVersion attribute sits in both the COM3D2 and the CM3D2 #if branch; both
    # must agree with the constant, otherwise the game reports two different versions
    attrs = PLUGIN_ATTR_PATTERN.findall(source)
    if not attrs:
        sys.exit(f"::error::PluginVersion attribute not found in {PLUGIN_SOURCE}")
    mismatched = sorted({a for a in attrs if a != version})
    if mismatched:
        sys.exit(f"::error::version mismatch in {PLUGIN_SOURCE}: "
                 f"PLUGIN_VERSION={version}, PluginVersion attribute(s)={mismatched}")

    print(version)


if __name__ == "__main__":
    main()
