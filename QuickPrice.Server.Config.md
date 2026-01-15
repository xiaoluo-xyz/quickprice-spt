# QuickPrice 服务端配置说明

本文档说明 `QuickPrice` 服务端模组的配置项与生效方式。

## 配置文件位置与加载逻辑

- 默认配置文件：`config.json`
- 加载路径优先级：
  1. 模组程序集同目录的 `config.json`
  2. SPT 服务端目录 `user/mods/QuickPrice/config.json`
- 配置会在启动时加载，并在请求处理/定时任务触发时根据文件修改时间进行热加载。

## 禁售列表黑名单配置（可选）

用于从“跳蚤禁售列表”中移除不需要显示禁售标签的物品。

- 文件名：`ragfair_ban_blacklist.json`
- 放置位置：与 `config.json` 同级（优先级同上）
- 说明：
  - `excludeItemIds`：按物品模板ID（tpl）精确移除
  - `excludeParentIds`：按父级ID移除其所有子孙物品（基于 `_parent` 递归）

示例：
```json
{
  "excludeItemIds": [],
  "excludeParentIds": []
}
```

> 注意：该文件读取后会缓存，修改后需要重启服务端生效。

## 配置项说明

> 时间相关配置均以 **秒** 为单位（如缓存超时/自动刷新/搜索延迟）。

| 配置项 | 类型 | 默认值 | 作用 | 备注 |
| --- | --- | --- | --- | --- |
| `Enabled` | bool | `true` | 是否启用服务端模组 | 关闭后跳过预加载与自动刷新 |
| `LogLevel` | string | `"Info"` | 日志级别 | 可选：`Debug` / `Info` / `Warning` / `Error`；`Off` / `None` / 空白 = 关闭日志 |
| `CacheTimeoutSeconds` | int | `300` | 动态价格缓存有效期（秒） | 超时后会触发刷新 |
| `AutoRefreshIntervalSeconds` | int | `300` | 自动刷新间隔（秒） | `0` 表示禁用自动刷新 |
| `OverrideClientConfig` | bool | `false` | 是否强制覆盖客户端配置 | 通过接口下发给客户端 |
| `PriceThreshold1` | int | `25000` | 价格分级阈值 1 | 白色 → 绿色 |
| `PriceThreshold2` | int | `45000` | 价格分级阈值 2 | 绿色 → 蓝色 |
| `PriceThreshold3` | int | `70000` | 价格分级阈值 3 | 蓝色 → 紫色 |
| `PriceThreshold4` | int | `100000` | 价格分级阈值 4 | 紫色 → 橙色 |
| `PriceThreshold5` | int | `250000` | 价格分级阈值 5 | 橙色 → 红色 |
| `EnableSearchTimeAdjustment` | bool | `false` | 是否启用搜索耗时调整 | 影响客户端搜索时间 |
| `SearchTimeRandomMin` | float | `0.0` | 搜索随机延迟最小值（秒） | 与随机最大值配合 |
| `SearchTimeRandomMax` | float | `1.0` | 搜索随机延迟最大值（秒） | 与随机最小值配合 |
| `SearchTimeLevel1` | float | `1.0` | 品质等级 1 搜索时间（秒） | Level 1 |
| `SearchTimeLevel2` | float | `2.0` | 品质等级 2 搜索时间（秒） | Level 2 |
| `SearchTimeLevel3` | float | `3.0` | 品质等级 3 搜索时间（秒） | Level 3 |
| `SearchTimeLevel4` | float | `4.0` | 品质等级 4 搜索时间（秒） | Level 4 |
| `SearchTimeLevel5` | float | `5.0` | 品质等级 5 搜索时间（秒） | Level 5 |
| `SearchTimeLevel6` | float | `6.0` | 品质等级 6 搜索时间（秒） | Level 6 |
| `PenetrationThreshold1` | int | `20` | 穿甲等级阈值 1 | 白色 → 绿色 |
| `PenetrationThreshold2` | int | `30` | 穿甲等级阈值 2 | 绿色 → 蓝色 |
| `PenetrationThreshold3` | int | `40` | 穿甲等级阈值 3 | 蓝色 → 紫色 |
| `PenetrationThreshold4` | int | `50` | 穿甲等级阈值 4 | 紫色 → 橙色 |
| `PenetrationThreshold5` | int | `60` | 穿甲等级阈值 5 | 橙色 → 红色 |
| `Notes` | string | `"QuickPrice 服务端配置文件"` | 配置说明 | 仅用于备注 |

## 日志相关说明

- 当前服务端日志通过 `ISptLogger` 输出（Debug/Info/Success/Warning/Error）。
- `LogLevel` 作为最低输出级别：`Debug` > `Info` > `Warning` > `Error`。
- `LogLevel` 为空或 `Off` / `None` 时会关闭 Info/Debug/Success 输出，但 **Warning/Error 仍会强制输出**。
- `Success` 按 `Info` 级别过滤。
- 若只想停用功能：将 `Enabled` 设为 `false`（会停止预加载与自动刷新）。

## 示例配置

```json
{
  "Enabled": true,
  "LogLevel": "Info",
  "CacheTimeoutSeconds": 300,
  "AutoRefreshIntervalSeconds": 300,
  "OverrideClientConfig": false,
  "PriceThreshold1": 25000,
  "PriceThreshold2": 45000,
  "PriceThreshold3": 70000,
  "PriceThreshold4": 100000,
  "PriceThreshold5": 250000,
  "EnableSearchTimeAdjustment": false,
  "SearchTimeRandomMin": 0.0,
  "SearchTimeRandomMax": 1.0,
  "SearchTimeLevel1": 1.0,
  "SearchTimeLevel2": 2.0,
  "SearchTimeLevel3": 3.0,
  "SearchTimeLevel4": 4.0,
  "SearchTimeLevel5": 5.0,
  "SearchTimeLevel6": 6.0,
  "PenetrationThreshold1": 20,
  "PenetrationThreshold2": 30,
  "PenetrationThreshold3": 40,
  "PenetrationThreshold4": 50,
  "PenetrationThreshold5": 60,
  "Notes": "QuickPrice server configuration"
}
```
