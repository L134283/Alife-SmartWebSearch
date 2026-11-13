# 网络智能搜索 (SmartWebSearch)

让桌宠按需搜索网页、阅读文章、获取百度 AI 总结，也能从图片查找作品和来源。支持 **AnySearch、Tavily、百度千帆、DeepSeek** 四种网页搜索引擎自由勾选。

当前版本 **5.0.0**，需要 **Alife.Client ≥ 5.2.0**，并安装官方「函数调用」插件。安装包见 [Releases](https://github.com/L134283/Alife-SmartWebSearch/releases)，也可在插件市场搜索「网络智能搜索」安装。

## 快速开始

1. 在插件环境中安装或重载「网络智能搜索」，在需要使用的角色中启用该模块。
2. 打开模块配置，在「1. 选择搜索引擎」勾选需要的渠道；初次使用保留 **AnySearch** 即可尝试免 Key 搜索。
3. 在「2. 配置所选引擎」填写密钥；Tavily、百度、DeepSeek 需要对应平台的 API Key，AnySearch 可以留空尝试匿名模式。
4. 「3. 日常搜索设置」先保留默认值。需要查图片出处时，单独开启「图片出处搜索」。
5. 点击页面下方「应用到角色」或「应用到全局」保存；修改引擎选择、图片搜源开关或工具说明加载方式后，重新激活使用该配置的角色，让 AI 工具列表同步更新。

只填密钥不会自动启用引擎，还需要勾选对应卡片。面板中的「已填 Key」只表示配置已填写，不代表已经验证账号余额、网络或接口权限。匿名搜索也可能受限，实际额度与计费以各平台控制台为准。

## 功能特性

- **四引擎自由多选**：只使用已勾选且已配置的渠道；中文查询优先百度，非中文优先 Tavily，再按需尝试 DeepSeek、AnySearch，不会每次同时调用全部渠道。
- **AnySearch 搜索家族（默认引擎，免Key即用）**：
  - **AnySearch**：通用/垂直搜索，免Key匿名可用，支持垂直领域标签（`tag`）+ 参数（`params`）精准搜索
  - **AnySearchBatchSearch**：并行批量搜索 1-5 个查询（Tavily/百度没有的能力）
  - **ExtractWebpage**：网页正文提取转 Markdown（适合完整阅读网页文章/文档）
  - **GetSubDomains**：垂直领域目录查询（如 finance.quote），先查参数再精准搜
- **DeepSeek 专用网页搜索**：使用独立搜索 API，不是聊天模型；返回标题、链接、长摘要和相关性评分，可选获取网页正文，支持站点、语言、时间等搜索语法。
- **AI总结搜索（高性能版，百度渠道）**：搜索+大模型总结一步到位，支持思考模型。
- **智能搜索生成（标准版，百度渠道）**：支持可选深度搜索、知识注入、追问等。
- **普通网页搜索**：Tavily（英文搜索、AI摘要）+ 百度（中文搜索、图片视频）+ DeepSeek + AnySearch，仅在已选渠道内智能路由。
- **百度热搜**：9个垂直分类热搜榜单（民生/财经/体育/娱乐/国际/挑战/电影/电视剧/小说）。
- **智能识图（百度渠道）**：传入图片URL自动下载识别，超100KB自动压缩。
- **图片出处搜索（以图搜源，独立开关）**：
  - **SearchSource**：一个工具三引擎——**SauceNAO**（插画/同人本/漫画/pixiv/danbooru/nhentai 综合库，反查**作品名、画师名**、原图链接；**免Key可用**，自动降级网页匿名模式）+ **Yandex**（相似网页搜源：游戏截图/照片/通用图，挖出包含此图的网页，免Key网页接口）+ **trace.moe**（番剧/动画截图专精，精确到作品名+集数+时间点，免Key即用）
  - **auto = 交叉验证模式**：SauceNAO × Yandex 双引擎无条件并行，两边一致的作品/角色/画师/来源信息更可靠（输出自带交叉验证提示，AI 综合比对后下结论）；SauceNAO 低相似度时自动加入 trace.moe（番剧场景）
  - AI 按图片类型显式选引擎：动画画面/问"什么番"→tracemoe，插画/本子→saucenao，游戏截图/通用图→yandex
  - Yandex 无官方API走网页接口（.ru 域 + cookie 会话 + 浏览器请求头），内置 10 秒最小间隔限流防验证码，仅支持图片URL方式（data URI 自动跳过）
  - 多语言标题（中文>日文>罗马音>英文）、成人内容标记、AniList 详情链接、画面预览
- **多账号轮换**：Tavily、百度各支持最多4组API Key，SauceNAO、trace.moe各2组，额度耗尽后按可用账号轮换；AnySearch、DeepSeek各配置1个Key。
- **结果缓存**：按实际引擎、请求参数和账号隔离缓存，相同请求在TTL内复用结果，避免切换渠道后拿到另一渠道的旧结果。
- **小白友好配置**：选择引擎、填写密钥、日常设置分步排列；备用账号和高级选项可折叠，跟随客户端明暗主题，支持窄窗口。

百度与 Tavily 的免费额度随平台调整，DeepSeek 搜索按平台规则计费；请在开通服务前确认额度、余额和后付费设置。

## 工具优先级

工具说明按所选引擎提供；取消某个引擎后，对应工具不再注入，也不会作为自动兜底。已勾选但未填Key的收费/认证渠道会在普通搜索自动路由中跳过；显式指定未勾选、未配置或无效的引擎会明确提示，不会悄悄改用其他渠道。

- **Search**：普通搜索统一入口，只选一个引擎时仅使用它；多选时中文优先百度、非中文优先Tavily，然后按需尝试DeepSeek、AnySearch，每一步都受勾选与密钥配置约束。
- **AnySearch 家族**：AnySearch用于通用/垂直搜索，AnySearchBatchSearch用于批量查询，ExtractWebpage用于阅读全文，GetSubDomains用于先查垂直标签和必填参数。
- **DeepSeekSearch**：需要DeepSeek专用搜索语法、原始问题或网页正文时使用。
- **SmartSummary → SmartChatSearch → Search**：勾选百度后，AI总结优先使用SmartSummary；失败时可尝试SmartChatSearch，再用普通搜索。
- **HotSearch / ImageRecognition**：勾选百度后，用于热搜和识别图里有什么。
- **SearchSource**：由图片出处搜索独立开关控制，用于“出处/什么番/第几集/谁画的/求原图”，与百度识图区分；即使取消全部网页引擎，仍可单独使用。

## 引擎对比（Search工具）

| 特性 | AnySearch | Tavily | 百度千帆 | DeepSeek |
|------|-----------|--------|----------|----------|
| 认证方式 | 可匿名 / AnySearch Key | Tavily Key | 千帆 Key | DeepSeek官方Key |
| 额度与费用 | 匿名额度较低；账号额度以平台为准 | 以控制台额度、credits计费规则为准 | 以已开通服务和免费/后付费额度为准 | 独立网页搜索，按搜索请求计费 |
| 返回内容 | 标题、链接、摘要 | 搜索结果与AI摘要 | 网页结果，可含图片/视频 | 标题、链接、长摘要、相关性评分；可选正文 |
| 专项能力 | 垂直领域、1～5个批量查询、正文提取 | topic分类、搜索深度 | AI总结、智能搜索生成、热搜、识图 | site:/-site:、精确短语、lang:、after:/before:语法 |
| 常用场景 | 免Key入门、资料与垂直查询 | 英文网页与新闻 | 中文资料与百度专项工具 | 专用网页搜索、长摘要与正文参考 |
| 结果数量 | 最多10条 | Search最多20条 | Search最多20条 | Search最多20条；DeepSeekSearch最多50条 |
| 查询限制 | 以平台规则为准 | 以平台规则为准 | 72字符（汉字算2字符，超限会截断） | 1～128个Unicode字符，超限会提示精简，不会截断 |

DeepSeek搜索与百度千帆中的DeepSeek聊天模型不是同一服务，密钥不能混用；普通搜索深度选项只影响Tavily/百度，不影响DeepSeek。

## 配置说明

### 搜索引擎选择

在引擎卡片上勾选 `anysearch`、`tavily`、`baidu`、`deepseek`，可以单选或多选。AnySearch默认选中，密钥可空；其他三个引擎须填对应密钥。取消全部选择时，网页搜索与百度专项工具不会发送请求，图片搜源仍由独立开关控制。

配置文件中的 `EnabledEngines` 是新版本选择列表，例如 `["baidu", "deepseek"]`。旧 `Engine` 字段保留用于升级兼容：

- 旧 `anysearch` / `tavily` / `baidu` 配置保持原来的单引擎选择。
- 旧 `auto` 配置对应AnySearch、Tavily、百度，不会自动替你开启DeepSeek。
- 首次通过新面板修改选择后以 `EnabledEngines` 为准；空数组 `[]` 表示主动关闭全部网页引擎，而不是恢复默认。
- 原有API Key、图片搜源开关和搜索设置继续保留；取消引擎不会删除该引擎的密钥，重新勾选即可使用。

### API Key 获取

- **AnySearch**：[API Key控制台](https://anysearch.com/console/api-keys)（可留空尝试免Key匿名模式，速率/配额较低）。
- **Tavily**：[Tavily控制台](https://app.tavily.com)，Key格式 `tvly-xxxxx`。
- **百度千帆**：[创建API Key](https://console.bce.baidu.com/qianfan/ais/console/apiKey)，请确认所需服务已经开通，并检查免费额度与后付费设置。
- **DeepSeek**：[官方API Key页面](https://platform.deepseek.com/api_keys)，填写DeepSeek官方平台的密钥；不需要填写聊天模型名，搜索费用以平台规则为准，余额不足时需先处理账号余额。
  - 独立搜索端点为 `https://api.deepseek.com/v1/tools/search`，接口说明见[官方搜索文档](https://api-docs.deepseek.com/zh-cn/api/search)。
  - 「同时获取网页正文」默认关闭；关闭时仍有长摘要，开启后尝试返回正文，内容较长，适合需要详细阅读的场景，并不保证每个网页都能提取正文。
- **SauceNAO**（图片出处搜索）：[官方网站](https://saucenao.com)，登录后在 `user.php?page=search-api` 页面获取API Key。
  - 免Key自动走网页匿名模式（约4次/30秒、100次/天）；配 Key 走 JSON API 约 200 次/天，更稳定
- **trace.moe**（番剧场景识别）：免Key匿名即可用（配额较低），Token 可提额

### 百度免费额度

下表为历史免费配额参考，不保证每个账号始终可用；是否开通、剩余额度和后付费情况请以百度控制台为准。

| 工具 | 每日免费额度 |
|------|-------------|
| AI总结搜索（高性能版）| 100次/天 |
| 智能搜索生成（标准版）| 100次/天 |
| 普通搜索 | 50次/天 |
| 智能识图 | 100次/天 |
| 百度热搜 | 10次/天 |

### 配置项

| 参数 | 说明 | 默认值 |
|------|------|--------|
| EnabledEngines | 已选网页引擎列表；未迁移旧配置时按Engine读取，[]表示全部关闭 | 首次默认仅anysearch |
| Engine | 旧版引擎选择兼容字段；新面板使用EnabledEngines | anysearch |
| AnySearchApiKey | AnySearch API Key（留空走免Key匿名模式）| 空 |
| TavilyApiKey1/2/3/4 | Tavily主账号及最多3个备用Key | 空 |
| BaiduApiKey1/2/3/4 | 百度千帆主账号及最多3个备用Key | 空 |
| DeepSeekApiKey | DeepSeek官方平台API Key（使用专用网页搜索接口）| 空 |
| DeepSeekIncludeContent | 同时获取DeepSeek搜索结果的网页正文；关闭时仍有长摘要 | false |
| MaxResults | 每次返回结果数量 | 5 |
| SearchDepth | 搜索深度 basic/advanced，仅影响Tavily/百度网页搜索 | basic |
| SummaryModel | 高性能版模型 | auto_thinking |
| ChatSearchModel | 标准版模型 | ernie-4.5-turbo-32k |
| EnableDeepSearch | 启用深度搜索（耗费较多额度）| false |
| EnableSourceSearch | 图片出处搜索独立开关（关闭后 SearchSource 不注入）| true |
| SauceNaoApiKey1/2 | SauceNAO API Key（可留空走网页匿名模式，配 Key 提额更稳）| 空 |
| TraceMoeApiKey1/2 | trace.moe Token（可留空走匿名）| 空 |
| EnableCache | 启用结果缓存 | true |
| CacheTtlMinutes | 缓存过期时间 | 5分钟 |
| ImplicitInjection | 隐式注入（函数文档按需加载，省token）| false |

密钥保存在Alife的本地配置文件中，不是加密保险箱。请勿公开上传配置文件或含密钥的截图；给角色设置独立配置后，全局配置不会覆盖该角色的独立选择。

## 函数参数

### AnySearch（通用/垂直搜索，免Key默认引擎）
| 参数 | 类型 | 说明 |
|------|------|------|
| query | string | 搜索关键词（必填）|
| tag | string? | 垂直领域标签，如 finance.quote / academic.paper；不传走通用搜索 |
| paramsStr | string? | 垂直领域参数，JSON（`{"type":"stock","symbol":"AAPL"}`）或 key=value（`type=stock,symbol=AAPL`）|
| zone | string? | cn(中文)/intl(国际)，不传自动按语言判断 |
| language | string? | zh-CN/en 等，不传自动按语言判断 |
| maxResults | int? | 返回数量，默认5，最多10 |

### AnySearchBatchSearch（并行批量搜索）
| 参数 | 类型 | 说明 |
|------|------|------|
| queries | string | 查询数组 JSON，如 `[{"query":"关键词1","max_results":5},{"query":"关键词2"}]`，最多5个（必填）|
| tag | string? | 垂直领域标签（共享给所有查询）|
| paramsStr | string? | 垂直领域参数（共享）|
| zone | string? | 区域，不传自动按语言判断 |
| language | string? | 语言，不传自动按语言判断 |

### ExtractWebpage（网页正文提取）
| 参数 | 类型 | 说明 |
|------|------|------|
| url | string | 网页URL（必填），输出正文转Markdown；不支持 PDF/图片等二进制 |

### GetSubDomains（垂直领域目录）
| 参数 | 类型 | 说明 |
|------|------|------|
| domain | string | 领域名，逗号分隔可查多个（必填）。领域：general/resource/social_media/finance/academic/legal/health/business/security/ip/code/energy/environment/agriculture/travel/film/gaming |

### SmartSummary（AI总结搜索）
| 参数 | 类型 | 说明 |
|------|------|------|
| query | string | 搜索关键词（必填）|
| model | string? | auto_thinking/thinking/non_thinking |
| timeRange | string? | day/week/month/year |
| maxResults | int? | 参考来源数量，默认5 |

### SmartChatSearch（智能搜索生成）
| 参数 | 类型 | 说明 |
|------|------|------|
| query | string | 搜索关键词（必填）|
| model | string? | ernie-4.5-turbo-32k(默认，百度自研免开通最稳) / deepseek-v4-flash / deepseek-v4-pro 等（DeepSeek 系列需账号在千帆开通对应模型，未开通会报 account_overdue；deepseek-v3.2、deepseek-r1 已停用，填旧名会自动回退到默认模型） |
| deepSearch | bool? | 启用深度搜索（更精准但更慢，耗费较多额度）|
| timeRange | string? | day/week/month/year |
| instruction | string? | 额外指令，引导AI回答方向 |
| enableReasoning | bool? | 启用推理模式 |
| maxResults | int? | 参考来源数量，默认5 |

### Search（普通搜索）

| 参数 | 类型 | 说明 |
|------|------|------|
| query | string | 搜索关键词（必填）|
| engine | string? | anysearch/tavily/baidu/deepseek；不传则在已勾选且已配置的渠道中智能路由，显式指定不会自动换渠道 |
| searchDepth | string? | basic/advanced，仅影响Tavily/百度 |
| topic | string? | 仅Tavily：general/news/finance |
| timeRange | string? | Tavily/百度：day/week/month/year；DeepSeek按query中的after:/before:限制时间 |
| maxResults | int? | 返回结果数量，默认5，最多20（AnySearch最多10）|
| includeImages | bool? | 仅百度：包含图片结果 |
| includeVideos | bool? | 仅百度：包含视频结果 |

### DeepSeekSearch（专用网页搜索）

| 参数 | 类型 | 说明 |
|------|------|------|
| query | string | 搜索关键词，1～128个Unicode字符；支持site:/-site:、引号内精确短语、lang:、after:/before: |
| maxResults | int? | 返回数量，1～50；默认使用插件设置 |
| originalQuestion | string? | 触发搜索的原始问题，最多128个Unicode字符，帮助理解搜索意图 |
| includeContent | bool? | 是否同时获取网页正文；不传时使用DeepSeekIncludeContent设置 |

例如 `query="Alife 插件 site:github.com after:2026-01-01"`；超出长度限制时请精简关键词，不会自动截掉站点或时间条件。输出包含标题、链接、站点、日期、相关性评分和长摘要，开启正文选项后附带可获取的正文。没有结果、密钥错误或余额不足时会说明原因，不会把报错当成网页结果。

### HotSearch（百度热搜）
| 参数 | 类型 | 说明 |
|------|------|------|
| tab | string | 分类：livelihood/finance/sports/new_entertainment/internation_news/challenge/movie/teleplay/novel |
| maxResults | int? | 返回数量，默认10，最多50 |

### ImageRecognition（智能识图）
| 参数 | 类型 | 说明 |
|------|------|------|
| imageUrl | string | 图片URL地址（必填），插件自动下载+压缩 |

### SearchSource（图片出处搜索）
| 参数 | 类型 | 说明 |
|------|------|------|
| imageUrl | string | 图片URL地址（必填），插件自动下载（超4MB才压缩，保留画质；Yandex 用原始 URL 由其服务器抓图）|
| engine | string? | saucenao=插画/同人本/画师，tracemoe=番剧/动画截图，yandex=游戏截图/通用图，不传=交叉验证：SauceNAO×Yandex并行互证 + 低相似度补查trace.moe |
| maxResults | int? | 每个引擎返回结果数，默认3，最多6 |

输出内容：SauceNAO（标题/画师/所属作品/角色/原始来源/链接，pixiv 自动补作品与画师主页链接；**无Key自动降级网页匿名模式，画师/作品名照样能查**）+ trace.moe（多语言作品名/集数/时间点/AniList详情/画面预览）+ Yandex（包含此图的网页：标题/来源域名/描述/链接）。相似度低于40%的结果自动过滤。

引擎说明：
- **SauceNAO**：插画/同人本/本子/pixiv 作品反查（**画师名+作品名**+原图链接），免Key自动走网页匿名模式（配额低），配免费 API Key 提额更稳（约200次/天）
- **trace.moe**：番剧/动画截图专精（动画帧定位作品+集数+时间点），免Key即用；**仅当图片是动画画面或用户明确问"什么番/动漫"时使用**，动漫截图以外的图（插画/CG/照片）不出结果
- **Yandex**：相似网页搜源（哪些网页包含此图），游戏截图/照片/通用图唯一可行引擎，二次元图也能挖出收录页；网页接口免Key，内置限流（10秒/次）防验证码，被软拒绝时自动重置会话重试

## 文件结构

```
Alife.Plugin.SmartWebSearch/
├── Alife.Plugin.SmartWebSearch.json   # 市场注册清单
├── manifest.json                      # 插件依赖清单
├── SmartWebSearch.cs                  # 主模块（工具注册、智能路由、缓存、图片处理）
├── DeepSeekSearch.cs                  # DeepSeek专用网页搜索
├── SmartWebSearchConfig.cs            # 配置类
├── SmartWebSearchUI_razor.g.cs        # UI界面
├── VERSION.txt                        # 版本号
└── README.md                          # 说明文档
```

分发压缩包 `Alife.Plugin.SmartWebSearch.zip` 只包含插件源码（全部 `.cs`）、`manifest.json` 和 `VERSION.txt`，不包含密钥配置、README或市场清单；README与市场JSON保留在本地，供阅读与后续发布使用。

## 版本历史

- **1.0.0** (2026-07-02)：多功能AI搜索：AI总结搜索(高性能版) + 智能搜索生成(标准版) + 双引擎搜索(Tavily+百度) + 百度热搜 + 智能识图
- **1.0.1** (2026-07-04)：修复 System.Drawing.Common 版本兼容性(9.0.0→>=9.0.0)；修复系统提示词函数文档重复注入
- **1.0.2** (2026-07-11)：提示词瘦身 + 代码重构抽通用骨架 + 炼金主题UI
- **1.1.0** (2026-07-11)：UI 文本收敛（封印→配置/已注入→已启用/去 Section 前缀）+ 纯 CSS 曼陀罗光轮 + 五芒星 orb + 晶洞角饰视觉增强
- **4.0.0** (2026-08-09)：适配 Alife 4.0.0 框架（ChatBehaviour + IInteractor）；新增隐式注入开关（函数文档按需加载，省token）；系统提示词瘦身；新增 manifest.json；修复多处健壮性问题（429重试循环、图片下载无大小限制、百度错误码解析异常、JSON字段类型不匹配导致整批结果丢失）
- **4.2.0** (2026-08-10)：适配 Alife 4.2.0 框架（XmlHandler API 变更：Name 改为只读、构造函数需传 name，改用 `new XmlHandler("SmartWebSearch") { ... }` 初始化器写法）
- **4.3.0** (2026-08-21)：新增 AnySearch 搜索家族：AnySearch（免Key匿名即可用，支持垂直领域 tag+params 精准搜索）+ AnySearchBatchSearch（并行批量搜1-5个查询）+ ExtractWebpage（网页正文转Markdown）+ GetSubDomains（垂直领域目录）；引擎模式新增 anysearch 并默认单独使用，auto 多渠道时 AnySearch 免Key兜底；UI 新增 AnySearch 配置区块
- **4.5.0** (2026-09-29)：修复官方「函数调用」插件改用方法原名后（不再把函数名/参数名小写化），本插件用全小写名单过滤导致**单引擎模式（anysearch/baidu/tavily）工具被全部过滤为空**、系统提示词里只剩文档标题、AI 调用一律报「环境中没有该标签」的问题；过滤与参数裁剪改用 `nameof(方法名)` + `StringComparer.OrdinalIgnoreCase`，参数保留列表改用与 `Search` 形参一致的常量，不再依赖框架的大小写约定（框架侧再改命名规则也不会失配）；handler 名与隐式触发标签提示统一用 `HandlerName` 常量；顺带修复 auto 模式关闭出处搜索时 `SearchSource` 仍残留注册的问题；修复**「智能搜索生成」一直返回空结果**：百度已停用 `deepseek-v3.2`/`deepseek-r1` 等旧模型、DeepSeek 系列在部分账号上还需单独开通（返回 `account_overdue`），而接口用 HTTP 200 + `code=invalid_model/account_overdue` 报错、被插件当成功吞成"未返回有效内容"；现标准版默认模型改为实测最稳的 `ernie-4.5-turbo-32k`（百度自研、与搜索服务同源、免开通），旧模型名自动回退并提示，且 200 响应体里的业务错误会带上中文处理建议原样返回给 AI/用户（不再伪装成空结果）
- **4.4.0** (2026-08-22)：新增**图片出处搜索**（以图搜源，独立开关 EnableSourceSearch）：SearchSource 工具多引擎——SauceNAO（插画/同人本/本子反查画师名+作品名+原图链接，免Key自动走网页匿名模式，配免费API Key约200次/天更稳，2组轮换，pixiv 老链接规范化）+ Yandex（相似网页搜源：挖出包含此图的网页，游戏截图/照片/通用图唯一可行方案，.ru 域网页接口+cookie会话+浏览器请求头，内置10秒限流防验证码，软拒绝自动重置会话）+ trace.moe（番剧/动画截图定位作品名+集数+时间点，免Key，中>日>罗马音>英文多语言标题）；不传 engine = SauceNAO×Yandex 双引擎并行交叉验证（两边一致的作品/角色/画师/来源信息更可靠），番剧图（SauceNAO低相似度）自动补查 trace.moe；AI 按图片类型自动路由引擎（动画画面/问番→tracemoe，插画本子→saucenao，游戏截图/通用图→yandex），显式指定引擎严格执行；搜源专用图片下载通道（超4MB才压缩保留画质）；UI 新增出处搜索配置区块。（开发期曾引入后移除 IQDB 引擎：只返回图库链接、无画师/作品元数据）
- **5.0.0** (2026-11-13)：修复AnySearch错误结果与切换渠道后结果不匹配的问题；新增DeepSeek专用网页搜索及可选正文；四引擎可自由勾选，自动路由只使用已选渠道，图片搜源保持独立；配置面板分步整理、支持折叠与明暗主题，保留旧版密钥和引擎选择。需要Alife.Client ≥ 5.2.0。
