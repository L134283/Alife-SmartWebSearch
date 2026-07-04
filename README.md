# 网络智能搜索 (SmartWebSearch)

多功能AI搜索插件，集成百度千帆AI搜索能力 + Tavily双引擎搜索。

## 功能特性

- **AI总结搜索（高性能版）**：搜索+大模型总结一步到位，支持思考模型，免费100次/天
- **智能搜索生成（标准版）**：功能最全面的AI搜索，支持可选深度搜索、知识注入、追问等，免费100次/天
- **双引擎搜索**：Tavily（英文强+AI摘要）+ 百度（中文强+图片视频），智能路由，免费50次/天
- **百度热搜**：9个垂直分类热搜榜单（民生/财经/体育/娱乐/国际/挑战/电影/电视剧/小说），免费10次/天
- **智能识图**：传入图片URL自动下载识别，超100KB自动压缩，免费100次/天
- **多账号轮换**：每个引擎支持最多4组API Key，额度耗尽自动切换
- **结果缓存**：相同查询在TTL内不重复调用API，节省额度

## 工具优先级（百度渠道）

1. **SmartSummary**（AI总结搜索）— 默认首选，搜索+总结一步到位
2. **SmartChatSearch**（智能搜索生成）— SmartSummary失败时降级，功能最全面
3. **Search**（普通搜索）— AI搜索均失败时最终降级，双引擎智能路由
4. **HotSearch**（百度热搜）— 用户想看热搜/今日热点时使用
5. **ImageRecognition**（智能识图）— 用户引用图片问"这是什么"时使用

## 引擎对比（Search工具）

| 特性 | Tavily | 百度搜索 |
|------|--------|---------|
| 认证方式 | Bearer Token | Bearer Token |
| 免费额度 | 1000 credits/月 | 50次/天 |
| AI摘要 | 有（answer字段）| 无 |
| 图片/视频 | 不支持 | 支持 |
| 主题分类 | general/news/finance | 无 |
| 中文搜索 | 一般 | 强 |
| 英文搜索 | 强 | 一般 |
| Query限制 | 无 | 72字符（汉字算2字符）|

## 配置说明

### 引擎模式
- `auto`：智能路由（默认），中文→百度，英文→Tavily
- `tavily`：仅用 Tavily
- `baidu`：仅用百度

### API Key 获取
- **Tavily**：https://app.tavily.com 免费注册，Key格式 `tvly-xxxxx`
- **百度千帆**：https://console.bce.baidu.com/qianfan/ais/console/apiKey 创建API Key
  - 无需实名、无需开启后付费，注册即可白嫖每日免费额度
  - ⚠️ 百度后付费最好别开，否则可能导致欠款

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
| Engine | 引擎模式 auto/tavily/baidu | auto |
| MaxResults | 每次返回结果数量 | 5 |
| SearchDepth | 搜索深度 basic/advanced | basic |
| SummaryModel | 高性能版模型 | auto_thinking |
| ChatSearchModel | 标准版模型 | deepseek-v3.2 |
| EnableDeepSearch | 启用深度搜索（耗费较多额度）| false |
| EnableCache | 启用结果缓存 | true |
| CacheTtlMinutes | 缓存过期时间 | 5分钟 |

## 函数参数

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
| model | string? | deepseek-v3.2/deepseek-r1/ernie-4.5-turbo-32k等 |
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

## 文件结构

```
Alife.Plugin.SmartWebSearch/
├── Alife.Plugin.SmartWebSearch.json   # 插件清单
├── SmartWebSearch.cs                  # 主模块（5个工具+智能路由+缓存+图片压缩）
├── SmartWebSearchConfig.cs            # 配置类
├── SmartWebSearchUI_razor.g.cs        # UI界面
├── VERSION.txt                        # 版本号
└── README.md                          # 说明文档
```

## 版本历史

- **1.0.0** (2026-07-02)：多功能AI搜索：AI总结搜索(高性能版) + 智能搜索生成(标准版) + 双引擎搜索(Tavily+百度) + 百度热搜 + 智能识图
- **1.0.1** (2026-07-04)：修复 System.Drawing.Common 版本兼容性(9.0.0→>=9.0.0)；修复系统提示词函数文档重复注入