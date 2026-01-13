# SPTarkov.Server.Core.xml 介绍

本文件为 `SPTarkov.Server.Core` 程序集的 XML 文档注释汇总，主要覆盖服务器回调、路由处理、业务事件与工具类说明。它不是代码本体，但能快速了解服务端“能处理什么请求/事件”。

## 内容结构
- `<member>` 节点通常以 `M:`（方法）、`F:`（字段）等前缀记录注释。
- 重点信息集中在 `summary` 内，包含诸如 “Handle client/xxx” 的接口路由描述，以及“Handle Xxx event”的业务事件描述。

## 接口能力概览（由 summary 统计）
- 共有成员注释约 2590 条（包含方法、字段、工具类等）。
- 带有 `Handle` 且能识别为路由路径的条目约 237 条，唯一接口路径约 155 条。
- 另有约 105 条为“事件处理/业务动作”，没有显式 URL 路径。

### 顶层接口分组
- `client/*`（约 131 条）：绝大多数客户端业务接口。
- `singleplayer/*`（约 16 条）：单机/本地化扩展接口。
- 其他：`files/*`、`launcher/*`、`push/*`、`raid/*`、`game/*`、`match/*` 等。

## 主要接口分组（client/*）
- 账户与外观：`client/account/customization`、`client/customization/*`
- 成就：`client/achievement/list`、`client/achievement/statistic`
- 预设与构建：`client/builds/*`（武器/装备/弹匣保存、删除、列表）
- 社交与聊天：`client/friend/*`、`client/chatServer/list`
- 游戏会话：`client/game/*`（档案创建、选择、昵称校验、keepalive、logout）
- 市场与交易：`client/ragfair/*`、`client/trading/api/*`、`client/insurance/items/list/cost`
- 物品与手册：`client/items`、`client/items/prices*`、`client/handbook/*`
- 藏身处：`client/hideout/*`
- 邮件与对话：`client/dialogue`、`client/mail/dialog/*`、`client/mail/msg/send`
- 匹配与组队：`client/match/*`（组队、邀请、准备、local start/end 等）
- 地图与天气：`client/locations`、`client/location/*`、`client/localGame/weather`、`client/weather`
- 语言与本地化：`client/languages`、`client/locale`、`client/menu/locale`
- 报告与统计：`client/report/send`、`client/reports/*`、`client/analytics/event-disconnect`
- 其他：`client/checkVersion`、`client/server/list`、`client/settings`、`client/survey/*`、`client/notifier/channel/create`

## 单机与扩展接口（singleplayer/*）
- 单机设置：`singleplayer/settings/*`（Bot 难度/上限/行为、Raid 时间、版本信息等）
- 资源与模块：`singleplayer/bundles`、`singleplayer/clientmods`、`singleplayer/moddedTraders`
- 日志与发布：`singleplayer/log`、`singleplayer/release`、`singleplayer/enableBSGlogging`
- 其他：`singleplayer/bosstypes`、`singleplayer/scav/traitorscavhostile`

## 其他接口
- 启动器：`launcher/profiles`、`launcher/profile/info`
- 资源包：`files/bundle`
- 通知推送：`push/notifier/get`、`push/notifier/getwebsocket`
- Raid/匹配：`raid/profile/scavsave`、`match/group/start_game`、`game/profile/items/moving`

## 事件/业务处理（无显式 URL 的 Handle）
- 背包与物品事件：拆分/移除/交换、快捷栏绑定、折叠武器、装填弹药、开随机容器、检视物品
- 角色恢复与消耗：`Eat`、`Heal`、`RestoreHealth`
- 任务与成就：接受/完成/上交、重复任务变更、成就触发与奖励
- 藏身处流程：升级开始/完成、放入/取出物品、区域开关、生产开始/完成、Scav Case、QTE
- 市场与交易事件：跳蚤新增/续费/购买、修理（商人/修理包）、保险、批量出售等
- 系统维护：档案迁移/备份、Trader 刷新、季节事件启用、文本翻译、撤离声望处理

## 备注
- 该 XML 文件仅反映注释信息，不包含请求参数/响应体的细节。
- 部分路径为历史兼容或特殊用途（如 `push/notifier/*`、`files/bundle`），需结合实际路由实现确认。
