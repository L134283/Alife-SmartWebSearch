using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using Alife.Framework;
using Alife.Function.FunctionCaller;

namespace Alife.Plugin.SmartWebSearch;

[Module(
    "网络智能搜索",
    "多功能AI搜索插件：AI总结搜索 + 智能搜索生成 + 双引擎搜索(Tavily+百度) + 百度热搜 + 智能识图，智能路由，多账号轮换，图片自动压缩，结果缓存。",
    defaultCategory: "Doro的妙妙工具",
    EditorUI = typeof(SmartWebSearchUI))]
public class SmartWebSearch(
    XmlFunctionCaller functionService,
    IInteractor<SmartWebSearch> interactor
) : ChatBehaviour, IConfigurable<SmartWebSearchConfig>
{
    private static readonly HttpClient _http = new(new HttpClientHandler { UseProxy = false })
        { Timeout = TimeSpan.FromSeconds(30) };

    // 识图下载专用客户端：图片可能较大，超时放宽到 60s
    private static readonly HttpClient _dlHttp = new(new HttpClientHandler { UseProxy = false })
        { Timeout = TimeSpan.FromSeconds(60) };

    private const string TavilyUrl = "https://api.tavily.com/search";
    private const string BaiduUrl = "https://qianfan.baidubce.com/v2/ai_search/web_search";
    private const string BaiduSummaryUrl = "https://qianfan.baidubce.com/v2/ai_search/web_summary";
    private const string BaiduChatUrl = "https://qianfan.baidubce.com/v2/ai_search/chat/completions";
    private const string BaiduTrendingUrl = "https://qianfan.baidubce.com/v2/tools/baidu_trending";
    private const string BaiduImageRecognitionUrl = "https://qianfan.baidubce.com/v2/tools/image_general";

    // 识图下载大小上限：防止下载超大文件打爆内存
    private const int MaxDownloadBytes = 20 * 1024 * 1024;

    // 账号轮换状态：记录已耗尽的账号索引
    private readonly HashSet<int> _exhaustedTavily = new();
    private readonly HashSet<int> _exhaustedBaidu = new();
    private readonly object _lock = new();

    // 搜索结果缓存
    private static readonly Dictionary<string, (string result, DateTime expiry)> _cache = new();
    private static readonly object _cacheLock = new();

    public SmartWebSearchConfig? Configuration { get; set; } = new();

    static void Log(string msg) => Console.WriteLine($"[智能搜索] {msg}");

    #region 初始化与系统提示词

    XmlHandler? _registeredHandler;

    protected override async Task OnAwake()
    {
        var cfg = Configuration ?? new SmartWebSearchConfig();
        var tCount = GetTavilyKeys(cfg).Count(k => !string.IsNullOrWhiteSpace(k));
        var bCount = GetBaiduKeys(cfg).Count(k => !string.IsNullOrWhiteSpace(k));

        var engineDesc = cfg.Engine switch
        {
            "tavily" => $"仅 Tavily（{tCount} 个账号）",
            "baidu" => $"仅百度（{bCount} 个账号）",
            _ => $"智能路由（Tavily {tCount} 个 + 百度 {bCount} 个）"
        };

        // 常时注入的硬规则：不依赖函数文档，AI 任何时候都需要（搜索时机、工具级联、引擎路由）。
        // 函数能力说明由 [Description] 自动注入，不再重复。
        var hardRules = $$"""
            ## 网络搜索
            - 主动搜索：用户要求、知识可能过时、需要最新信息或事实核查时，先调用搜索工具，再基于结果回答。
            - 工具级联：SmartSummary → SmartChatSearch → Search（前一失败或不适配再降级）。
            - 引擎：{{engineDesc}}。中文→百度，英文→Tavily。
            """;

        // 详细规则：与函数使用细节相关。显式模式直接注入（现状不变）；
        // 隐式模式放进 handler.Explanation，AI 调用 <smartwebsearch/> 后随文档一并加载，
        // 避免这些规则在开启隐式注入时仍常时占用 token。
        var detailedRules = """
            - 百度中文结果强、可带图片/视频（Search 传 includeImages/includeVideos）；Tavily 自带 AI 摘要、英文结果强；可传 engine 强制指定。
            - 深度搜索（advanced / deep_search）更准但更慢更费额度，非必要不主动加深，按配置默认即可。
            """;

        var implicitNote = cfg.ImplicitInjection
            ? "\n- 隐式注入已开启：需要搜索/热搜/识图时，先调用 <smartwebsearch/> 加载完整函数说明与调用方式，再按文档调用对应函数。"
            : "";

        RegisterFunctionHandlers(cfg, cfg.ImplicitInjection ? detailedRules : null);

        if (cfg.ImplicitInjection)
            interactor.Prompt(hardRules + implicitNote);
        else
            interactor.Prompt(hardRules + detailedRules + implicitNote);
    }

    /// <summary>
    /// 4.0：DocumentMode 控制函数文档的注入方式。
    /// 显式（默认）：完整函数文档直接注入系统提示词，AI 开箱即用；
    /// 隐式：只暴露触发标签 &lt;smartwebsearch/&gt;，AI 需先调用它按需加载文档（省 token，渐进式）。
    /// </summary>
    void RegisterFunctionHandlers(SmartWebSearchConfig cfg, string? explanation = null)
    {
        var discovered = new XmlHandler(this);
        var exposed = discovered.Functions;

        var documentMode = cfg.ImplicitInjection
            ? DocumentMode.Implicit
            : DocumentMode.Explicit;
        var handler = new XmlHandler
        {
            Name = "SmartWebSearch",
            Description = "网络智能搜索：AI总结搜索 + 智能搜索生成 + 双引擎搜索(Tavily+百度) + 百度热搜 + 智能识图，智能路由，多账号轮换。",
            // 隐式模式：详细规则随 <smartwebsearch/> 加载的文档一并输出；显式模式保持 null 避免重复注入
            Explanation = explanation,
            Functions = exposed,
        };
        _registeredHandler = handler;
        if (cfg.ImplicitInjection)
            Log("隐式注入已开启：AI 需先调用 <smartwebsearch/> 按需加载函数文档");
        functionService.RegisterHandler(handler, documentMode);
    }

    /// <summary>热重载/活动销毁时注销本模块注册的 XmlHandler，避免旧 handler 残留在函数表中。</summary>
    protected override Task OnDestroy()
    {
        if (_registeredHandler != null)
        {
            functionService.UnregisterHandler(_registeredHandler);
            _registeredHandler = null;
        }
        return Task.CompletedTask;
    }

    #endregion

    #region 主搜索入口

    [XmlFunction(FunctionMode.OneShot)]
    [Description("搜索互联网获取实时信息。支持 Tavily 和百度双引擎智能路由。当用户要求搜索、或你遇到不确定/可能过时的知识时，主动调用。")]
    public async Task Search(
        [Description("搜索关键词或问题")] string query,
        [Description("指定搜索引擎：tavily / baidu。不传则使用智能路由（中文→百度，英文→Tavily）")] string? engine = null,
        [Description("搜索深度：basic(快速) 或 advanced(深度)")] string? searchDepth = null,
        [Description("搜索主题（仅Tavily）：general / news / finance")] string? topic = null,
        [Description("时间范围：day / week / month / year")] string? timeRange = null,
        [Description("返回结果数量，默认5，最多20")] int? maxResults = null,
        [Description("是否包含图片结果（仅百度）")] bool? includeImages = null,
        [Description("是否包含视频结果（仅百度）")] bool? includeVideos = null)
    {
        if (string.IsNullOrWhiteSpace(query)) { interactor.Poke("搜索关键词不能为空"); return; }

        var cfg = Configuration ?? new SmartWebSearchConfig();
        var tavilyKeys = GetTavilyKeys(cfg);
        var baiduKeys = GetBaiduKeys(cfg);
        var hasTavily = tavilyKeys.Any(k => !string.IsNullOrWhiteSpace(k));
        var hasBaidu = baiduKeys.Any(k => !string.IsNullOrWhiteSpace(k));

        if (!hasTavily && !hasBaidu) { interactor.Poke("未配置任何搜索引擎的 API Key，请在插件设置中填写"); return; }

        var depth = string.IsNullOrWhiteSpace(searchDepth) ? cfg.SearchDepth : searchDepth;
        var results = Math.Clamp(maxResults ?? cfg.MaxResults, 1, 20);

        var cacheKey = $"search:{engine}:{query}:{depth}:{results}:{topic}:{timeRange}:{includeImages}:{includeVideos}";
        if (TryGetCached(cfg, cacheKey, out var cachedResult))
        {
            Log($"缓存命中: {query[..Math.Min(30, query.Length)]}...");
            interactor.Poke(cachedResult);
            return;
        }

        // 账号耗尽状态在各调用骨架内部清空（每次调用仅影响当次轮换），无需在此重复清理
        var searchOrder = ResolveSearchOrder(engine, cfg.Engine, query, hasTavily, hasBaidu);

        string? result = null;
        foreach (var eng in searchOrder)
        {
            if (eng == "tavily" && hasTavily)
                result = await TryTavilySearch(query, depth, topic, timeRange, results, cacheKey, cfg);
            else if (eng == "baidu" && hasBaidu)
                result = await TryBaiduSearch(query, depth, timeRange, results, includeImages, includeVideos, cacheKey, cfg);

            if (result != null) break;
        }

        if (result == null) { interactor.Poke("所有搜索引擎均不可用，请检查 API Key 配置或等待额度刷新"); return; }

        interactor.Poke(result);
    }

    #endregion

    #region AI总结搜索（高性能版）

    [XmlFunction(FunctionMode.OneShot)]
    [Description("AI总结搜索（高性能版）：搜索互联网并用大模型总结结果，一步到位。支持思考模型。当用户要求搜索时优先使用此工具。")]
    public async Task SmartSummary(
        [Description("搜索关键词或问题")] string query,
        [Description("模型：auto_thinking(自动思考) / thinking / non_thinking")] string? model = null,
        [Description("时间范围：day / week / month / year")] string? timeRange = null,
        [Description("返回参考来源数量，默认5")] int? maxResults = null)
    {
        if (string.IsNullOrWhiteSpace(query)) { interactor.Poke("搜索关键词不能为空"); return; }

        var cfg = Configuration ?? new SmartWebSearchConfig();
        if (!GetBaiduKeys(cfg).Any(k => !string.IsNullOrWhiteSpace(k)))
        { interactor.Poke("AI总结搜索需要百度千帆API Key"); return; }

        var useModel = string.IsNullOrWhiteSpace(model) ? cfg.SummaryModel : model;
        var results = Math.Clamp(maxResults ?? cfg.MaxResults, 1, 20);
        var cacheKey = $"summary:{query}:{useModel}:{timeRange}:{results}";

        var body = new JsonObject
        {
            ["messages"] = new JsonArray { new JsonObject { ["role"] = "user", ["content"] = query } },
            ["model"] = useModel,
            ["resource_type_filter"] = new JsonArray { new JsonObject { ["type"] = "web", ["top_k"] = results } },
        };
        ApplyTimeRange(body, timeRange);

        var result = await BaiduCallWithRotationAsync(BaiduSummaryUrl, body.ToJsonString(),
            cacheKey, "AI总结", raw => FormatSummaryResults(raw, query), cfg);

        interactor.Poke(result ?? "AI总结搜索失败，所有百度账号均不可用。可尝试使用智能搜索生成(SmartChatSearch)或普通搜索(Search)");
    }

    static string FormatSummaryResults(string rawJson, string query)
    {
        try
        {
            var node = JsonNode.Parse(rawJson);
            var sb = new StringBuilder();

            var choices = node?["choices"]?.AsArray();
            if (choices != null && choices.Count > 0)
            {
                var message = choices[0]?["message"];
                var reasoning = message?["reasoning_content"]?.ToString();
                var content = message?["content"]?.ToString();
                if (!string.IsNullOrWhiteSpace(reasoning)) { sb.AppendLine("## 思考过程"); sb.AppendLine(reasoning); sb.AppendLine(); }
                if (!string.IsNullOrWhiteSpace(content)) { sb.AppendLine("## AI总结"); sb.AppendLine(content); sb.AppendLine(); }
            }

            var refs = node?["references"]?.AsArray();
            if (refs != null && refs.Count > 0)
            {
                sb.AppendLine($"## 参考来源（共 {refs.Count} 条）");
                int n = 1;
                foreach (var r in refs)
                {
                    var title = r?["title"]?.ToString() ?? "";
                    var url = r?["url"]?.ToString() ?? "";
                    var date = r?["date"]?.ToString() ?? "";
                    sb.AppendLine($"{n}. [{title}]({url})");
                    if (!string.IsNullOrWhiteSpace(date)) sb.AppendLine($"   发布时间: {date}");
                    n++;
                }
            }

            if (sb.Length == 0) sb.AppendLine("AI总结完成但未返回有效内容");
            return sb.ToString().Trim();
        }
        catch (Exception ex) { Log($"AI总结 格式化异常: {ex.Message}"); return $"AI总结完成但结果解析失败:\n{rawJson}"; }
    }

    #endregion

    #region 智能搜索生成（标准版）

    [XmlFunction(FunctionMode.OneShot)]
    [Description("智能搜索生成（标准版）：功能最全面的AI搜索，支持多模型、可选深度搜索、知识注入、追问等。适合复杂研究场景。")]
    public async Task SmartChatSearch(
        [Description("搜索关键词或问题")] string query,
        [Description("模型：deepseek-v3.2 / deepseek-r1 / ernie-4.5-turbo-32k 等")] string? model = null,
        [Description("是否启用深度搜索（更精准但更慢，耗费较多额度）")] bool? deepSearch = null,
        [Description("时间范围：day / week / month / year")] string? timeRange = null,
        [Description("额外指令，用于引导AI的回答方向")] string? instruction = null,
        [Description("是否启用推理模式")] bool? enableReasoning = null,
        [Description("返回参考来源数量，默认5")] int? maxResults = null)
    {
        if (string.IsNullOrWhiteSpace(query)) { interactor.Poke("搜索关键词不能为空"); return; }

        var cfg = Configuration ?? new SmartWebSearchConfig();
        if (!GetBaiduKeys(cfg).Any(k => !string.IsNullOrWhiteSpace(k)))
        { interactor.Poke("智能搜索生成需要百度千帆API Key"); return; }

        var useModel = string.IsNullOrWhiteSpace(model) ? cfg.ChatSearchModel : model;
        var useDeepSearch = deepSearch ?? cfg.EnableDeepSearch;
        var results = Math.Clamp(maxResults ?? cfg.MaxResults, 1, 20);
        var cacheKey = $"chat:{query}:{useModel}:{useDeepSearch}:{timeRange}:{instruction}:{enableReasoning}:{results}";

        var body = new JsonObject
        {
            ["messages"] = new JsonArray { new JsonObject { ["role"] = "user", ["content"] = query } },
            ["model"] = useModel,
            ["search_source"] = "baidu_search_v2",
            ["resource_type_filter"] = new JsonArray { new JsonObject { ["type"] = "web", ["top_k"] = results } },
        };
        if (useDeepSearch) body["enable_deep_search"] = true;
        if (enableReasoning == true) body["enable_reasoning"] = true;
        if (!string.IsNullOrWhiteSpace(instruction)) body["instruction"] = instruction;
        ApplyTimeRange(body, timeRange);

        var result = await BaiduCallWithRotationAsync(BaiduChatUrl, body.ToJsonString(),
            cacheKey, "智能搜索生成", raw => FormatChatSearchResults(raw, query), cfg);

        interactor.Poke(result ?? "智能搜索生成失败，所有百度账号均不可用。可尝试使用普通搜索(Search)");
    }

    static string FormatChatSearchResults(string rawJson, string query)
    {
        try
        {
            var node = JsonNode.Parse(rawJson);
            var sb = new StringBuilder();
            var choices = node?["choices"]?.AsArray();
            if (choices != null && choices.Count > 0)
            {
                var message = choices[0]?["message"];
                var reasoning = message?["reasoning_content"]?.ToString();
                var content = message?["content"]?.ToString();
                if (!string.IsNullOrWhiteSpace(reasoning)) { sb.AppendLine("## 推理过程"); sb.AppendLine(reasoning); sb.AppendLine(); }
                if (!string.IsNullOrWhiteSpace(content)) { sb.AppendLine("## AI回答"); sb.AppendLine(content); sb.AppendLine(); }
            }
            var followups = node?["followup_queries"]?.AsArray();
            if (followups != null && followups.Count > 0)
            {
                sb.AppendLine("## 追问建议");
                int n = 1;
                foreach (var f in followups)
                {
                    var fq = f?.ToString() ?? f?["query"]?.ToString() ?? "";
                    if (!string.IsNullOrWhiteSpace(fq)) { sb.AppendLine($"{n}. {fq}"); n++; }
                }
                sb.AppendLine();
            }
            var refs = node?["references"]?.AsArray();
            if (refs != null && refs.Count > 0)
            {
                sb.AppendLine($"## 参考来源（共 {refs.Count} 条）");
                int n = 1;
                foreach (var r in refs)
                {
                    var title = r?["title"]?.ToString() ?? "";
                    var url = r?["url"]?.ToString() ?? "";
                    sb.AppendLine($"{n}. [{title}]({url})");
                    n++;
                }
            }
            if (sb.Length == 0) sb.AppendLine("智能搜索生成完成但未返回有效内容");
            return sb.ToString().Trim();
        }
        catch (Exception ex) { Log($"智能搜索生成 格式化异常: {ex.Message}"); return $"智能搜索生成完成但结果解析失败:\n{rawJson}"; }
    }

    #endregion

    #region 百度热搜

    [XmlFunction(FunctionMode.OneShot)]
    [Description("百度热搜：获取百度实时热搜榜单，支持9个垂直分类。当用户想看热搜、今日热点时使用。")]
    public async Task HotSearch(
        [Description("热搜分类：livelihood(民生) / finance(财经) / sports(体育) / new_entertainment(娱乐) / internation_news(国际) / challenge(挑战) / movie(电影) / teleplay(电视剧) / novel(小说)")] string tab = "livelihood",
        [Description("返回结果数量，默认10，最多50")] int? maxResults = null)
    {
        var cfg = Configuration ?? new SmartWebSearchConfig();
        if (!GetBaiduKeys(cfg).Any(k => !string.IsNullOrWhiteSpace(k))) { interactor.Poke("百度热搜需要百度千帆API Key"); return; }

        var validTabs = new[] { "livelihood", "finance", "sports", "new_entertainment", "internation_news", "challenge", "movie", "teleplay", "novel" };
        if (!validTabs.Contains(tab)) { interactor.Poke($"无效分类: {tab}，可选: {string.Join(", ", validTabs)}"); return; }

        var results = Math.Clamp(maxResults ?? 10, 1, 50);
        var cacheKey = $"hot:{tab}:{results}";
        var url = $"{BaiduTrendingUrl}?tab={tab}";

        var result = await BaiduCallWithRotationAsync(url, "", cacheKey, "热搜",
            raw => FormatHotSearchResults(raw, tab, results), cfg, isGet: true);

        interactor.Poke(result ?? "百度热搜获取失败，所有百度账号均不可用");
    }

    static string FormatHotSearchResults(string rawJson, string tab, int maxResults)
    {
        try
        {
            var node = JsonNode.Parse(rawJson);
            var sb = new StringBuilder();
            var tabNames = new Dictionary<string, string> {
                {"livelihood","民生"},{"finance","财经"},{"sports","体育"},
                {"new_entertainment","娱乐"},{"internation_news","国际"},
                {"challenge","挑战"},{"movie","电影"},{"teleplay","电视剧"},{"novel","小说"}
            };
            var tabName = tabNames.GetValueOrDefault(tab, tab);
            var data = node?["data"]?.AsArray();
            if (data == null || data.Count == 0) { sb.AppendLine($"未获取到{tabName}热搜数据"); return sb.ToString().Trim(); }
            sb.AppendLine($"## 百度{tabName}热搜（共 {Math.Min(data.Count, maxResults)} 条）");
            sb.AppendLine();
            int n = 1;
            foreach (var r in data)
            {
                if (n > maxResults) break;
                var word = r?["word"]?.ToString() ?? "";
                var hotScore = JLong(r, "hotScore");
                var hotChange = r?["hotChange"]?.ToString() ?? "";
                var hotTag = JInt(r, "hotTag");
                var desc = r?["desc"]?.ToString() ?? "";
                var url = r?["url"]?.ToString() ?? "";
                var show = r?["show"]?.AsArray();
                var changeIcon = hotChange switch { "up" => "↑", "down" => "↓", _ => "—" };
                var tagStr = hotTag switch { 1 => "🆕新", 2 => "💰商", 3 => "🔥热", 4 => "♨️沸", 5 => "💥爆", _ => "" };
                sb.AppendLine($"**{n}. {word}** {changeIcon} 热度:{hotScore} {tagStr}");
                if (!string.IsNullOrWhiteSpace(desc)) sb.AppendLine($"   {desc}");
                if (!string.IsNullOrWhiteSpace(url)) sb.AppendLine($"   链接: {url}");
                if (show != null && show.Count > 0)
                {
                    var tags = show.Select(s => s?.ToString() ?? "").Where(s => !string.IsNullOrWhiteSpace(s));
                    if (tags.Any()) sb.AppendLine($"   标签: {string.Join(", ", tags)}");
                }
                sb.AppendLine();
                n++;
            }
            return sb.ToString().Trim();
        }
        catch (Exception ex) { Log($"热搜 格式化异常: {ex.Message}"); return $"热搜获取完成但结果解析失败:\n{rawJson}"; }
    }

    #endregion

    #region 智能识图

    [XmlFunction(FunctionMode.OneShot)]
    [Description("智能识图：识别图片中的物体、场景、文字等。传入图片URL，插件自动下载并识别。当用户引用图片问\"这是什么\"时使用。")]
    public async Task ImageRecognition([Description("图片URL地址")] string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) { interactor.Poke("图片URL不能为空"); return; }

        var cfg = Configuration ?? new SmartWebSearchConfig();
        if (!GetBaiduKeys(cfg).Any(k => !string.IsNullOrWhiteSpace(k))) { interactor.Poke("智能识图需要百度千帆API Key"); return; }

        string imageBase64;
        try
        {
            Log("识图: 下载图片...");
            imageBase64 = await DownloadImageAsBase64Async(imageUrl);
            Log($"识图: base64 {imageBase64.Length / 1024}KB");
        }
        catch (Exception ex) { interactor.Poke($"图片下载失败: {ex.Message}"); return; }

        var bodyJson = new JsonObject { ["image_b64"] = imageBase64 }.ToJsonString();

        // 识图不缓存：模型可能返回不同细节
        var result = await BaiduCallWithRotationAsync(BaiduImageRecognitionUrl, bodyJson,
            cacheKey: null, "识图", raw => FormatImageRecognitionResults(raw), cfg);

        interactor.Poke(result ?? "智能识图失败，所有百度账号均不可用");
    }

    static string FormatImageRecognitionResults(string rawJson)
    {
        try
        {
            var node = JsonNode.Parse(rawJson); var sb = new StringBuilder();
            var choices = node?["choices"]?.AsArray();
            if (choices != null && choices.Count > 0)
            {
                var content = choices[0]?["message"]?["content"]?.ToString();
                if (!string.IsNullOrWhiteSpace(content)) { sb.AppendLine("## 识别结果"); sb.AppendLine(content); }
            }
            if (sb.Length == 0)
            {
                var data = node?["data"];
                if (data != null)
                {
                    var desc = data?["description"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(desc)) { sb.AppendLine("## 识别结果"); sb.AppendLine(desc); }
                    var tags = data?["tags"]?.AsArray() ?? data?["result"]?.AsArray();
                    if (tags != null && tags.Count > 0)
                    {
                        sb.AppendLine($"## 标签（共 {tags.Count} 个）"); int n = 1;
                        foreach (var t in tags)
                        {
                            var name = t?["name"]?.ToString() ?? t?.ToString() ?? "";
                            var score = JFloat(t, "score");
                            if (!string.IsNullOrWhiteSpace(name)) { sb.AppendLine($"{n}. {name} (置信度: {score:F2})"); n++; }
                        }
                    }
                }
            }
            if (sb.Length == 0)
            {
                var code = node?["code"]?.ToString() ?? "";
                var msg = node?["message"]?.ToString() ?? "";
                sb.AppendLine(code != "0" && !string.IsNullOrWhiteSpace(msg) ? $"识别失败: {msg}" : "识别完成但未返回有效内容");
            }
            return sb.ToString().Trim();
        }
        catch (Exception ex) { Log($"识图 格式化异常: {ex.Message}"); return $"识图完成但结果解析失败:\n{rawJson}"; }
    }

    /// <summary>
    /// 下载图片并转为base64，超过100KB自动压缩。
    /// 支持 HTTP(S) URL 与 data URI；单次下载限 20MB 防止内存打爆。
    /// </summary>
    static async Task<string> DownloadImageAsBase64Async(string imageUrl)
    {
        // data URI：直接解码，无需网络
        if (imageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var commaIdx = imageUrl.IndexOf(',');
            if (commaIdx < 0) throw new Exception("无效的 data URI");
            var meta = imageUrl[5..commaIdx];
            if (!meta.Contains("base64", StringComparison.OrdinalIgnoreCase))
                throw new Exception("仅支持 base64 编码的 data URI");
            byte[] dataBytes;
            try { dataBytes = Convert.FromBase64String(Uri.UnescapeDataString(imageUrl[(commaIdx + 1)..])); }
            catch (FormatException ex) { throw new Exception("data URI 的 Base64 内容无效", ex); }
            if (dataBytes.Length > MaxDownloadBytes)
                throw new Exception($"图片过大 ({dataBytes.Length / 1024 / 1024}MB > {MaxDownloadBytes / 1024 / 1024}MB)");
            return CompressToBase64(dataBytes);
        }

        using var req = new HttpRequestMessage(HttpMethod.Get, imageUrl);
        using var resp = await _dlHttp.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        if (!resp.IsSuccessStatusCode) throw new Exception($"HTTP {(int)resp.StatusCode}");

        // Content-Length 预检
        var contentLength = resp.Content.Headers.ContentLength;
        if (contentLength.HasValue && contentLength.Value > MaxDownloadBytes)
            throw new Exception($"图片过大 ({contentLength.Value / 1024 / 1024}MB > {MaxDownloadBytes / 1024 / 1024}MB)");

        // 流式读取并限长：服务器不返回 Content-Length 时也能兜住
        using var stream = await resp.Content.ReadAsStreamAsync();
        using var ms = new MemoryStream();
        var buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            total += read;
            if (total > MaxDownloadBytes)
                throw new Exception($"图片过大 (> {MaxDownloadBytes / 1024 / 1024}MB)");
            ms.Write(buffer, 0, read);
        }
        return CompressToBase64(ms.ToArray());
    }

    /// <summary>
    /// 转 base64；超过 100KB 循环降尺寸 + JPEG 压缩直到达标或到极限。
    /// </summary>
    static string CompressToBase64(byte[] bytes)
    {
        var base64 = Convert.ToBase64String(bytes);
        const int maxSize = 100 * 1024;
        if (base64.Length <= maxSize) return base64;

        Log($"识图: base64 {base64.Length / 1024}KB 超限，压缩中");
        using var ms = new MemoryStream(bytes);
        using var img = Image.FromStream(ms);
        int width = img.Width, height = img.Height;
        while (true)
        {
            using var bmp = new Bitmap(width, height);
            using var g = Graphics.FromImage(bmp);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(img, 0, 0, width, height);
            using var jpegMs = new MemoryStream();
            var jpegParams = new EncoderParameters(1);
            jpegParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 60L);
            var jpegCodec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            bmp.Save(jpegMs, jpegCodec, jpegParams);
            var compressed = Convert.ToBase64String(jpegMs.ToArray());
            if (compressed.Length <= maxSize)
            { Log($"识图: 压缩成功 {base64.Length / 1024}KB -> {compressed.Length / 1024}KB"); return compressed; }

            // 降一半尺寸继续；降到 100px 视为极限
            int nextWidth = Math.Max(100, width / 2);
            int nextHeight = Math.Max(100, height / 2);
            if (nextWidth == width && nextHeight == height)
            { Log("识图: 压缩到极限"); return compressed; }
            width = nextWidth; height = nextHeight;
        }
    }

    #endregion

    #region 引擎路由逻辑

    /// <summary>
    /// 根据参数、配置和查询语言确定搜索顺序
    /// </summary>
    static List<string> ResolveSearchOrder(string? engineParam, string configEngine,
        string query, bool hasTavily, bool hasBaidu)
    {
        var order = new List<string>();

        // 1. AI显式指定引擎
        if (!string.IsNullOrWhiteSpace(engineParam))
        {
            var e = engineParam.ToLower().Trim();
            if (e == "tavily" && hasTavily) { order.Add("tavily"); return order; }
            if (e == "baidu" && hasBaidu) { order.Add("baidu"); return order; }
        }

        // 2. 配置指定单引擎
        if (configEngine == "tavily" && hasTavily) { order.Add("tavily"); return order; }
        if (configEngine == "baidu" && hasBaidu) { order.Add("baidu"); return order; }

        // 3. auto 智能路由：按语言选主引擎，另一个作为备选
        if (!hasTavily) { order.Add("baidu"); return order; }
        if (!hasBaidu) { order.Add("tavily"); return order; }

        var primary = IsChineseQuery(query) ? "baidu" : "tavily";
        order.Add(primary);
        order.Add(primary == "tavily" ? "baidu" : "tavily");
        return order;
    }

    /// <summary>
    /// 简单中文检测：中文字符占比超过30%视为中文查询
    /// </summary>
    static bool IsChineseQuery(string query)
    {
        int chineseCount = query.Count(c => c >= 0x4E00 && c <= 0x9FFF);
        return chineseCount * 3 > query.Length; // chineseCount / length > 0.33
    }

    #endregion

    #region Tavily 搜索

    async Task<string?> TryTavilySearch(string query, string depth, string? topic,
        string? timeRange, int results, string cacheKey, SmartWebSearchConfig cfg)
    {
        var body = new JsonObject
        {
            ["query"] = query,
            ["search_depth"] = depth,
            ["max_results"] = results,
            ["include_answer"] = "basic",
        };
        if (!string.IsNullOrWhiteSpace(topic)) body["topic"] = topic;
        if (!string.IsNullOrWhiteSpace(timeRange)) body["time_range"] = timeRange;

        return await TavilyCallWithRotationAsync(TavilyUrl, body.ToJsonString(),
            cacheKey, "Tavily", raw => FormatTavilyResults(raw, query), cfg);
    }

    static string FormatTavilyResults(string rawJson, string query)
    {
        try
        {
            var node = JsonNode.Parse(rawJson);
            var sb = new StringBuilder();

            var answer = node?["answer"]?.ToString();
            if (!string.IsNullOrWhiteSpace(answer))
            {
                sb.AppendLine("## 搜索摘要");
                sb.AppendLine(answer);
                sb.AppendLine();
            }

            var results = node?["results"]?.AsArray();
            if (results != null && results.Count > 0)
            {
                sb.AppendLine($"## 搜索结果（共 {results.Count} 条）");
                sb.AppendLine();
                int n = 1;
                foreach (var r in results)
                {
                    var title = r?["title"]?.ToString() ?? "";
                    var url = r?["url"]?.ToString() ?? "";
                    var content = r?["content"]?.ToString() ?? "";
                    var score = JFloat(r, "score");
                    sb.AppendLine($"### {n}. {title}");
                    sb.AppendLine($"链接: {url}");
                    sb.AppendLine($"相关度: {score:F2}");
                    sb.AppendLine(content);
                    sb.AppendLine();
                    n++;
                }
            }
            else sb.AppendLine("未找到相关搜索结果");

            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            Log($"Tavily 格式化异常: {ex.Message}");
            return $"搜索完成但结果解析失败:\n{rawJson}";
        }
    }

    #endregion

    #region 百度搜索

    async Task<string?> TryBaiduSearch(string query, string depth, string? timeRange,
        int results, bool? includeImages, bool? includeVideos, string cacheKey, SmartWebSearchConfig cfg)
    {
        // 百度query限制72字符（汉字算2字符）
        var truncatedQuery = TruncateForBaidu(query);

        var resourceFilter = new JsonArray
        {
            new JsonObject { ["type"] = "web", ["top_k"] = results }
        };
        if (includeImages == true)
            resourceFilter.Add(new JsonObject { ["type"] = "image", ["top_k"] = Math.Min(5, results) });
        if (includeVideos == true)
            resourceFilter.Add(new JsonObject { ["type"] = "video", ["top_k"] = Math.Min(3, results) });

        var body = new JsonObject
        {
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "user", ["content"] = truncatedQuery }
            },
            ["search_source"] = "baidu_search_v2",
            ["resource_type_filter"] = resourceFilter,
        };

        // 深度映射: basic→lite(快速), advanced→standard(完整)
        body["edition"] = depth == "advanced" ? "standard" : "lite";
        ApplyTimeRange(body, timeRange);

        return await BaiduCallWithRotationAsync(BaiduUrl, body.ToJsonString(),
            cacheKey, "百度", raw => FormatBaiduResults(raw, query), cfg);
    }

    static string FormatBaiduResults(string rawJson, string query)
    {
        try
        {
            var node = JsonNode.Parse(rawJson);
            var sb = new StringBuilder();

            var refs = node?["references"]?.AsArray();
            if (refs == null || refs.Count == 0)
            {
                sb.AppendLine("未找到相关搜索结果");
                return sb.ToString().Trim();
            }

            // 分离网页结果和多媒体结果
            var webResults = new List<JsonNode?>();
            var imageResults = new List<JsonNode?>();
            var videoResults = new List<JsonNode?>();

            foreach (var r in refs)
            {
                var type = r?["type"]?.ToString() ?? "web";
                switch (type)
                {
                    case "image": imageResults.Add(r); break;
                    case "video": videoResults.Add(r); break;
                    default: webResults.Add(r); break;
                }
            }

            // 格式化网页结果
            if (webResults.Count > 0)
            {
                sb.AppendLine($"## 搜索结果（共 {webResults.Count} 条）");
                sb.AppendLine();
                int n = 1;
                foreach (var r in webResults)
                {
                    var title = r?["title"]?.ToString() ?? "";
                    var url = r?["url"]?.ToString() ?? "";
                    var content = r?["content"]?.ToString() ?? "";
                    var date = r?["date"]?.ToString() ?? "";
                    var score = JFloat(r, "rerank_score");
                    var authority = JFloat(r, "authority_score", -1);

                    sb.AppendLine($"### {n}. {title}");
                    if (!string.IsNullOrWhiteSpace(date))
                        sb.AppendLine($"发布时间: {date}");
                    sb.AppendLine($"链接: {url}");
                    sb.AppendLine($"相关度: {score:F2}");
                    if (authority >= 0)
                        sb.AppendLine($"权威性: {authority:F2}");
                    sb.AppendLine(content);
                    sb.AppendLine();
                    n++;
                }
            }

            // 格式化图片结果
            if (imageResults.Count > 0)
            {
                sb.AppendLine($"## 图片结果（共 {imageResults.Count} 张）");
                int n = 1;
                foreach (var r in imageResults)
                {
                    var img = r?["image"];
                    var imgUrl = img?["url"]?.ToString() ?? "";
                    var w = img?["width"]?.ToString() ?? "";
                    var h = img?["height"]?.ToString() ?? "";
                    sb.AppendLine($"{n}. [图片]({imgUrl}) {w}x{h}");
                    n++;
                }
                sb.AppendLine();
            }

            // 格式化视频结果
            if (videoResults.Count > 0)
            {
                sb.AppendLine($"## 视频结果（共 {videoResults.Count} 个）");
                int n = 1;
                foreach (var r in videoResults)
                {
                    var vid = r?["video"];
                    var vidUrl = vid?["url"]?.ToString() ?? "";
                    var duration = vid?["duration"]?.ToString() ?? "";
                    var title = r?["title"]?.ToString() ?? "";
                    sb.AppendLine($"{n}. [{title}]({vidUrl}) 时长:{duration}秒");
                    n++;
                }
                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            Log($"百度 格式化异常: {ex.Message}");
            return $"搜索完成但结果解析失败:\n{rawJson}";
        }
    }

    /// <summary>
    /// 百度query截断：72字符限制（汉字算2字符）
    /// </summary>
    static string TruncateForBaidu(string query)
    {
        int charCount = 0;
        var sb = new StringBuilder();
        foreach (var c in query)
        {
            int cost = c > 127 ? 2 : 1;
            if (charCount + cost > 72) break;
            sb.Append(c);
            charCount += cost;
        }
        var result = sb.ToString();
        if (result.Length < query.Length)
            Log($"百度query截断: {query.Length} → {result.Length} 字符");
        return result;
    }

    #endregion

    #region 通用调用骨架（百度）

    /// <summary>
    /// 百度通用调用骨架：账号轮换 + 401/403/429/额度异常 + 缓存读写 + 日志。
    /// cacheKey 为 null 时跳过缓存。
    /// </summary>
    async Task<string?> BaiduCallWithRotationAsync(
        string url, string bodyJson, string? cacheKey, string label,
        Func<string, string> formatter, SmartWebSearchConfig cfg, bool isGet = false)
    {
        // 缓存命中
        if (cacheKey != null && TryGetCached(cfg, cacheKey, out var cached))
        {
            Log($"缓存命中({label})");
            return cached;
        }

        // 每次调用开始时清空耗尽标记：额度可能已刷新，本次轮换重新评估
        lock (_lock) _exhaustedBaidu.Clear();

        var baiduKeys = GetBaiduKeys(cfg);
        for (int attempt = 0; attempt < baiduKeys.Count; attempt++)
        {
            var (idx, key) = GetNextKey(baiduKeys, _exhaustedBaidu);
            if (key == null) break;

            try
            {
                Log($"{label}[{idx + 1}]");
                using var req = new HttpRequestMessage(isGet ? HttpMethod.Get : HttpMethod.Post, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                if (!isGet) req.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

                using var resp = await _http.SendAsync(req);
                var raw = await resp.Content.ReadAsStringAsync();

                // 401/403：账号认证失败，切换下一个
                if ((int)resp.StatusCode == 401 || (int)resp.StatusCode == 403)
                {
                    Log($"{label} 账号{idx + 1}认证失败");
                    lock (_lock) _exhaustedBaidu.Add(idx);
                    continue;
                }

                // 429：频率限制，等待2秒后重试一次
                if ((int)resp.StatusCode == 429)
                {
                    Log($"{label} 账号{idx + 1}频率限制，等待2秒重试");
                    await Task.Delay(2000);
                    using var req2 = new HttpRequestMessage(isGet ? HttpMethod.Get : HttpMethod.Post, url);
                    req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                    if (!isGet) req2.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                    using var resp2 = await _http.SendAsync(req2);
                    raw = await resp2.Content.ReadAsStringAsync();

                    if (!resp2.IsSuccessStatusCode)
                    {
                        // 重试仍失败：认证/限流/额度都视为该账号暂时不可用，标记跳过，避免下一轮又打同一个 key
                        if ((int)resp2.StatusCode is 401 or 403 or 429
                            || IsBaiduQuotaError(raw))
                            lock (_lock) _exhaustedBaidu.Add(idx);
                        Log($"{label} 重试失败({(int)resp2.StatusCode})");
                        continue;
                    }

                    var retryResult = formatter(raw);
                    Log($"{label}[{idx + 1}]成功(重试)");
                    if (cacheKey != null) SaveCache(cfg, cacheKey, retryResult);
                    return retryResult;
                }

                // 额度耗尽 / 其他失败：尝试解析响应体错误码
                if (!resp.IsSuccessStatusCode)
                {
                    var (errCode, errMsg) = JsonError(raw);
                    if (errCode == "216003" || IsBaiduQuotaError(raw))
                    {
                        Log($"{label} 账号{idx + 1}额度/认证异常(code={errCode})，切换下一个");
                        lock (_lock) _exhaustedBaidu.Add(idx);
                        continue;
                    }
                    Log($"{label} 请求失败({(int)resp.StatusCode}): {errMsg}");
                    continue;
                }

                // 成功
                var formatted = formatter(raw);
                Log($"{label}[{idx + 1}]成功");
                if (cacheKey != null) SaveCache(cfg, cacheKey, formatted);
                return formatted;
            }
            catch (TaskCanceledException) { Log($"{label} 账号{idx + 1}超时"); continue; }
            catch (Exception ex) { Log($"{label} 账号{idx + 1}异常: {ex.Message}"); continue; }
        }

        return null;
    }

    #endregion

    #region 通用调用骨架（Tavily）

    /// <summary>
    /// Tavily 通用调用骨架：账号轮换 + 432/433额度耗尽 + 429重试 + 401无效Key + 5xx + 缓存读写 + 日志。
    /// cacheKey 为 null 时跳过缓存。
    /// </summary>
    async Task<string?> TavilyCallWithRotationAsync(
        string url, string bodyJson, string? cacheKey, string label,
        Func<string, string> formatter, SmartWebSearchConfig cfg)
    {
        if (cacheKey != null && TryGetCached(cfg, cacheKey, out var cached))
        {
            Log($"缓存命中({label})");
            return cached;
        }

        lock (_lock) _exhaustedTavily.Clear();

        var tavilyKeys = GetTavilyKeys(cfg);
        for (int attempt = 0; attempt < tavilyKeys.Count; attempt++)
        {
            var (idx, key) = GetNextKey(tavilyKeys, _exhaustedTavily);
            if (key == null) break;

            try
            {
                Log($"{label} [{idx + 1}]");
                using var req = new HttpRequestMessage(HttpMethod.Post, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                req.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

                using var resp = await _http.SendAsync(req);
                var raw = await resp.Content.ReadAsStringAsync();

                // 额度耗尽：432(Key/Plan Limit) / 433(PayGo Limit)
                if ((int)resp.StatusCode == 432 || (int)resp.StatusCode == 433)
                {
                    Log($"{label} 账号 {idx + 1} 额度耗尽 (HTTP {(int)resp.StatusCode})，切换下一个");
                    lock (_lock) _exhaustedTavily.Add(idx);
                    continue;
                }

                // 频率限制：等待后重试一次
                if ((int)resp.StatusCode == 429)
                {
                    Log($"{label} 账号 {idx + 1} 频率限制，等待2秒重试");
                    await Task.Delay(2000);
                    using var req2 = new HttpRequestMessage(HttpMethod.Post, url);
                    req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                    req2.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                    using var resp2 = await _http.SendAsync(req2);
                    raw = await resp2.Content.ReadAsStringAsync();

                    if (!resp2.IsSuccessStatusCode)
                    {
                        // 重试仍失败：限流/额度/认证都视为该账号暂时不可用，标记跳过，避免下一轮又打同一个 key
                        if ((int)resp2.StatusCode is 429 or 432 or 433 or 401)
                            lock (_lock) _exhaustedTavily.Add(idx);
                        Log($"{label} 重试失败 (HTTP {(int)resp2.StatusCode})");
                        continue;
                    }

                    var retryResult = formatter(raw);
                    Log($"{label} [{idx + 1}] 搜索成功（重试）");
                    if (cacheKey != null) SaveCache(cfg, cacheKey, retryResult);
                    return retryResult;
                }

                // Key无效
                if ((int)resp.StatusCode == 401)
                {
                    Log($"{label} 账号 {idx + 1} Key无效 (401)，切换下一个");
                    lock (_lock) _exhaustedTavily.Add(idx);
                    continue;
                }

                // 服务端错误
                if ((int)resp.StatusCode >= 500)
                {
                    Log($"{label} 服务端错误 (HTTP {(int)resp.StatusCode})");
                    continue;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    Log($"{label} 请求失败 (HTTP {(int)resp.StatusCode}): {raw[..Math.Min(200, raw.Length)]}");
                    continue;
                }

                // 成功
                var formatted = formatter(raw);
                Log($"{label} [{idx + 1}] 搜索成功");
                if (cacheKey != null) SaveCache(cfg, cacheKey, formatted);
                return formatted;
            }
            catch (TaskCanceledException) { Log($"{label} 账号 {idx + 1} 超时"); continue; }
            catch (Exception ex) { Log($"{label} 账号 {idx + 1} 异常: {ex.Message}"); continue; }
        }

        return null;
    }

    #endregion

    #region 缓存与辅助

    /// <summary>
    /// 检查缓存命中，命中返回 true 并通过 out 返回结果
    /// </summary>
    bool TryGetCached(SmartWebSearchConfig cfg, string cacheKey, out string result)
    {
        result = null!;
        if (!cfg.EnableCache) return false;
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(cacheKey, out var cached) && cached.expiry > DateTime.Now)
            {
                result = cached.result;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// 写入缓存，自动清理过期项
    /// </summary>
    void SaveCache(SmartWebSearchConfig cfg, string cacheKey, string result)
    {
        if (!cfg.EnableCache) return;
        lock (_cacheLock)
        {
            _cache[cacheKey] = (result, DateTime.Now.AddMinutes(cfg.CacheTtlMinutes));
            if (_cache.Count > 100)
            {
                var expired = _cache.Where(k => k.Value.expiry <= DateTime.Now)
                    .Select(k => k.Key).ToList();
                foreach (var k in expired) _cache.Remove(k);
            }
        }
    }

    /// <summary>
    /// 百度/智能搜索通用：把 day/week/month/year 映射到百度 search_recency_filter
    /// （百度最小粒度为 week，day→week）
    /// </summary>
    static void ApplyTimeRange(JsonObject body, string? timeRange)
    {
        if (string.IsNullOrWhiteSpace(timeRange)) return;
        var recency = timeRange switch
        {
            "day" => "week",
            "week" => "week",
            "month" => "month",
            "year" => "year",
            _ => (string?)null
        };
        if (recency != null) body["search_recency_filter"] = recency;
    }

    (int index, string? key) GetNextKey(List<string> keys, HashSet<int> exhausted)
    {
        lock (_lock)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(keys[i]) && !exhausted.Contains(i))
                    return (i, keys[i]);
            }
        }
        return (-1, null);
    }

    static List<string> GetTavilyKeys(SmartWebSearchConfig cfg) =>
        new() { cfg.TavilyApiKey1, cfg.TavilyApiKey2, cfg.TavilyApiKey3, cfg.TavilyApiKey4 };

    static List<string> GetBaiduKeys(SmartWebSearchConfig cfg) =>
        new() { cfg.BaiduApiKey1, cfg.BaiduApiKey2, cfg.BaiduApiKey3, cfg.BaiduApiKey4 };

    // ============ JSON 安全读取辅助 ============

    /// <summary>安全读取数字字段（字符串/数字均可），失败返回默认值，不抛异常。固定 InvariantCulture 解析，避免区域小数点差异。</summary>
    static float JFloat(JsonNode? node, string key, float def = 0)
        => float.TryParse(node?[key]?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : def;

    static long JLong(JsonNode? node, string key, long def = 0)
        => long.TryParse(node?[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : def;

    static int JInt(JsonNode? node, string key, int def = 0)
        => int.TryParse(node?[key]?.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : def;

    /// <summary>
    /// 安全解析百度错误响应，返回 (code, message)；解析失败时返回原始截断文本。
    /// code 以字符串形式返回，兼容数字/字符串两种编码。
    /// </summary>
    static (string? code, string message) JsonError(string raw)
    {
        try
        {
            var node = JsonNode.Parse(raw);
            var code = node?["code"]?.ToString();
            var message = node?["message"]?.ToString()
                ?? node?["error_msg"]?.ToString()
                ?? node?["error"]?.ToString() ?? "";
            return (code, message);
        }
        catch
        {
            return (null, raw.Length > 200 ? raw[..200] : raw);
        }
    }

    /// <summary>
    /// 百度错误消息是否属于额度耗尽/欠费类（quota / 额度 / 欠费）。
    /// 不用 "limit" 判断：普通参数错误也可能含 limit，避免误判切换账号。
    /// </summary>
    static bool IsBaiduQuotaError(string raw)
    {
        var (_, message) = JsonError(raw);
        return message.Contains("quota", StringComparison.OrdinalIgnoreCase)
            || message.Contains("额度", StringComparison.OrdinalIgnoreCase)
            || message.Contains("欠费", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
