# 网络智能搜索 (SmartWebSearch)

多功能AI搜索插件，集成百度千帆AI搜索能力 + Tavily双引擎搜索。

## 功能特性

- **AnySearch 搜索家族（默认引擎，免Key即用）**：
  - **AnySearch**：通用/垂直搜索，免Key匿名可用，支持垂直领域标签（`tag`）+ 参数（`params`）精准搜索
  - **AnySearchBatchSearch**：并行批量搜索 1-5 个查询（Tavily/百度没有的能力）
  - **ExtractWebpage**：网页正文提取转 Markdown（适合完整阅读网页文章/文档）
  - **GetSubDomains**：垂直领域目录查询（如 finance.quote），先查参数再精准搜
- **AI总结搜索（高性能版）**：搜索+大模型总结一步到位，支持思考模型，免费100次/天
- **智能搜索生成（标准版）**：功能最全面的AI搜索，支持可选深度搜索、知识注入、追问等，免费100次/天
- **双引擎搜索**：Tavily（英文强+AI摘要）+ 百度（中文强+图片视频），智能路由，免费50次/天
- **百度热搜**：9个垂直分类热搜榜单（民生/财经/体育/娱乐/国际/挑战/电影/电视剧/小说），免费10次/天
- **智能识图**：传入图片URL自动下载识别，超100KB自动压缩，免费100次/天
- **图片出处搜索（以图搜源，独立开关）**：
  - **SearchSource**：一个工具三引擎——**SauceNAO**（插画/同人本/漫画/pixiv/danbooru/nhentai 综合库，反查**作品名、画师名**、原图链接；**免Key可用**，自动降级网页匿名模式）+ **Yandex**（相似网页搜源：游戏截图/照片/通用图，挖出包含此图的网页，免Key网页接口）+ **trace.moe**（番剧/动画截图专精，精确到作品名+集数+时间点，免Key即用）
  - **auto = 交叉验证模式**：SauceNAO × Yandex 双引擎无条件并行，两边一致的作品/角色/画师/来源信息更可靠（输出自带交叉验证提示，AI 综合比对后下结论）；SauceNAO 低相似度时自动加入 trace.moe（番剧场景）
  - AI 按图片类型显式选引擎：动画画面/问"什么番"→tracemoe，插画/本子→saucenao，游戏截图/通用图→yandex
  - Yandex 无官方API走网页接口（.ru 域 + cookie 会话 + 浏览器请求头），内置 10 秒最小间隔限流防验证码，仅支持图片URL方式（data URI 自动跳过）
  - 多语言标题（中文>日文>罗马音>英文）、成人内容标记、AniList 详情链接、画面预览
- **多账号轮换**：每个引擎支持最多4组API Key（出处搜索引擎为2组），额度耗尽自动切换
- **结果缓存**：相同查询在TTL内不重复调用API，节省额度

## 工具优先级

**默认（仅 AnySearch 引擎）**：AnySearch（通用/垂直搜索）→ AnySearchBatchSearch（批量并行）→ ExtractWebpage（正文提取）→ GetSubDomains（垂直目录）

**百度渠道（Auto 多渠道开启时）**：
1. **SmartSummary**（AI总结搜索）— 默认首选，搜索+总结一步到位
2. **SmartChatSearch**（智能搜索生成）— SmartSummary失败时降级，功能最全面
3. **Search**（普通搜索）— AI搜索均失败时最终降级，双引擎智能路由，AnySearch 免Key兜底
4. **HotSearch**（百度热搜）— 用户想看热搜/今日热点时使用
5. **ImageRecognition**（智能识图）— 用户引用图片问"这是什么"（识别图里有什么）时使用
6. **SearchSource**（图片出处搜索）— 用户发图问"出处/什么番/第几集/谁画的/求原图/找本子"（找图的来源）时使用，与识图区分

## 引擎对比（Search工具）

| 特性 | AnySearch | Tavily | 百度搜索 |
|------|-----------|--------|---------|
| 认证方式 | 匿名免Key / Bearer Token | Bearer Token | Bearer Token |
| 免费额度 | 匿名较低（开箱即用）/ 注册Key提额 | 1000 credits/月 | 50次/天 |
| AI摘要 | 无 | 有（answer字段）| 无 |
| 图片/视频 | 不支持 | 不支持 | 支持 |
| 垂直领域搜索 | 支持（tag+params，17个领域）| 不支持 | 无 |
| 批量并行搜索 | 支持（1-5个查询）| 不支持 | 无 |
| 网页正文提取 | 支持（转Markdown）| 不支持 | 无 |
| 中文搜索 | 强（zone=cn）| 一般 | 强 |
| 英文搜索 | 强（zone=intl）| 强 | 一般 |
| Query限制 | 无 | 无 | 72字符（汉字算2字符）|

## 配置说明

### 引擎模式
- `anysearch`：仅用 AnySearch（默认），免Key匿名即可用，开箱即用
- `auto`：智能路由（多渠道同时开启时），中文→百度，英文→Tavily，AnySearch 免Key兜底
- `tavily`：仅用 Tavily
- `baidu`：仅用百度

### API Key 获取
- **AnySearch**：https://anysearch.com/console/api-keys 免费注册（可留空走免Key匿名模式，速率/配额较低）
- **Tavily**：https://app.tavily.com 免费注册，Key格式 `tvly-xxxxx`
- **百度千帆**：https://console.bce.baidu.com/qianfan/ais/console/apiKey 创建API Key
  - 无需实名、无需开启后付费，注册即可白嫖每日免费额度
  - ⚠️ 百度后付费最好别开，否则可能导致欠款
- **SauceNAO**（图片出处搜索）：https://saucenao.com 免费注册，登录后在 `user.php?page=search-api` 页面获取 API Key
  - 免Key自动走网页匿名模式（约4次/30秒、100次/天）；配 Key 走 JSON API 约 200 次/天，更稳定
- **trace.moe**（番剧场景识别）：免Key匿名即可用（配额较低），Token 可提额

### 百度免费额度
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
| Engine | 引擎模式 anysearch/auto/tavily/baidu | anysearch |
| AnySearchApiKey | AnySearch API Key（留空走免Key匿名模式）| 空 |
| MaxResults | 每次返回结果数量 | 5 |
| SearchDepth | 搜索深度 basic/advanced | basic |
| SummaryModel | 高性能版模型 | auto_thinking |
| ChatSearchModel | 标准版模型 | ernie-4.5-turbo-32k |
| EnableDeepSearch | 启用深度搜索（耗费较多额度）| false |
| EnableSourceSearch | 图片出处搜索独立开关（关闭后 SearchSource 不注入）| true |
| SauceNaoApiKey1/2 | SauceNAO API Key（可留空走网页匿名模式，配 Key 提额更稳）| 空 |
| TraceMoeApiKey1/2 | trace.moe Token（可留空走匿名）| 空 |
| EnableCache | 启用结果缓存 | true |
| CacheTtlMinutes | 缓存过期时间 | 5分钟 |
| ImplicitInjection | 隐式注入（函数文档按需加载，省token）| false |

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
| engine | string? | tavily/baidu，不传则智能路由 |
| searchDepth | string? | basic/advanced |
| topic | string? | 仅Tavily：general/news/finance |
| timeRange | string? | day/week/month/year |
| maxResults | int? | 返回结果数量，默认5 |
| includeImages | bool? | 仅百度：包含图片结果 |
| includeVideos | bool? | 仅百度：包含视频结果 |

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
├── SmartWebSearch.cs                  # 主模块（10个工具+智能路由+缓存+图片压缩）
├── SmartWebSearchConfig.cs            # 配置类
├── SmartWebSearchUI_razor.g.cs        # UI界面
├── VERSION.txt                        # 版本号
└── README.md                          # 说明文档
```

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