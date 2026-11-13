using System;
using System.Collections.Generic;
using System.Linq;

namespace Alife.Plugin.SmartWebSearch;

public class SmartWebSearchConfig
{
    // 保留旧字段用于读取已有配置；新配置以 EnabledEngines 为准。
    public string Engine { get; set; } = "anysearch";

    // null 表示尚未迁移的旧配置；空列表表示用户主动关闭了全部网页搜索引擎。
    public List<string>? EnabledEngines { get; set; }

    // === Tavily 配置 ===
    // Tavily 端点固定: https://api.tavily.com/search
    // 认证: Bearer Token, 免费额度 1000 credits/月
    public string TavilyApiKey1 { get; set; } = "";
    public string TavilyApiKey2 { get; set; } = "";
    public string TavilyApiKey3 { get; set; } = "";
    public string TavilyApiKey4 { get; set; } = "";

    // === 百度配置 ===
    // 百度端点固定: https://qianfan.baidubce.com/v2/ai_search/web_search
    // 认证: Bearer Token (千帆API Key)
    // 免费额度: AI总结搜索/智能搜索生成 100次/天, 普通搜索 50次/天, 识图 100次/天, 热搜 10次/天
    public string BaiduApiKey1 { get; set; } = "";
    public string BaiduApiKey2 { get; set; } = "";
    public string BaiduApiKey3 { get; set; } = "";
    public string BaiduApiKey4 { get; set; } = "";

    // === AnySearch 配置 ===
    // AnySearch 端点固定: https://api.anysearch.com/v1/search
    // 认证: 可空。留空走匿名模式(免Key，速率/配额较低，开箱即用)；配置后走 Bearer 认证(更高额度)
    // 免费注册: https://anysearch.com/console/api-keys，Key 格式 as_sk_xxx
    public string AnySearchApiKey { get; set; } = "";

    // DeepSeek 独立网页搜索接口，不是聊天模型；使用 DeepSeek 平台的 API Key。
    public string DeepSeekApiKey { get; set; } = "";
    // 关闭时仍会返回长摘要；仅需要完整网页内容时开启，避免不必要的上下文占用。
    public bool DeepSeekIncludeContent { get; set; } = false;

    // === 通用搜索设置 ===
    // 每次搜索返回的网页结果数量，默认 5
    public int MaxResults { get; set; } = 5;

    // 搜索深度: Tavily用 basic(1额度)/advanced(2额度), 百度用 lite(快速)/standard(完整)
    public string SearchDepth { get; set; } = "basic";

    // === AI搜索设置 ===
    // 高性能版智能搜索生成使用的模型：auto_thinking(自动思考) / thinking / non_thinking
    // 限时免费，搜索+大模型总结一步到位
    public string SummaryModel { get; set; } = "auto_thinking";

    // 标准版智能搜索生成使用的模型：ernie-4.5-turbo-32k（百度自研，默认，免开通最稳）
    // 也可用 deepseek-v4-flash / deepseek-v4-pro（DeepSeek 系列需账号在千帆开通，未开通会报 account_overdue）
    // 功能最全面，支持可选深度搜索、知识注入、追问等
    // 注：deepseek-v3.2 / deepseek-r1 等旧模型已停用，配置为旧名时插件会自动回退到默认模型
    public string ChatSearchModel { get; set; } = "ernie-4.5-turbo-32k";

    // 是否启用深度搜索（智能搜索生成专用，更精准但更慢，耗费较多额度）
    public bool EnableDeepSearch { get; set; } = false;

    // === 缓存设置 ===
    // 启用后相同查询在TTL内不重复调用API，节省额度
    public bool EnableCache { get; set; } = true;

    // 缓存过期时间（分钟），默认5分钟
    public int CacheTtlMinutes { get; set; } = 5;

    // === 图片出处搜索设置 ===
    // 独立开关：开启后注入 SearchSource 工具（以图搜源），
    // 番剧截图→作品名+集数+时间点(trace.moe)，插画/同人本/漫画→作品名/画师/原图链接(SauceNAO)
    public bool EnableSourceSearch { get; set; } = true;

    // SauceNAO API Key（可选：无Key自动走网页匿名模式，配额低；有Key走JSON API，约200次/天更稳定）
    // 免费注册：https://saucenao.com → 登录后 https://saucenao.com/user.php?page=search-api 获取
    // 认证方式：api_key 参数（非 Header）
    public string SauceNaoApiKey1 { get; set; } = "";
    public string SauceNaoApiKey2 { get; set; } = "";

    // trace.moe Token（可选：免Key匿名即可用，匿名配额较低；Token 可提高配额）
    // 认证方式：x-trace-token 请求头
    public string TraceMoeApiKey1 { get; set; } = "";
    public string TraceMoeApiKey2 { get; set; } = "";

    // === 注入设置 ===
    // 隐式注入（4.0 新特性）：开启后函数文档不直接注入系统提示词，
    // AI 需先调用 <smartwebsearch/> 按需加载（省 token，渐进式）；
    // 关闭则显式注入（默认），功能说明直接可用。
    public bool ImplicitInjection { get; set; } = false;

    static readonly string[] supportedEngines = { "anysearch", "tavily", "baidu", "deepseek" };

    public IReadOnlyList<string> GetEnabledEngines()
    {
        IEnumerable<string> selected = (IEnumerable<string>?)EnabledEngines ??
            (string.Equals(Engine, "auto", StringComparison.OrdinalIgnoreCase)
                ? new[] { "anysearch", "tavily", "baidu" }
                : new[] { string.IsNullOrWhiteSpace(Engine) ? "anysearch" : Engine });
        return selected
            .Where(id => string.IsNullOrWhiteSpace(id) == false)
            .Select(id => id.Trim().ToLowerInvariant())
            .Where(id => supportedEngines.Contains(id, StringComparer.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public bool IsEngineEnabled(string engine) =>
        GetEnabledEngines().Contains(engine, StringComparer.OrdinalIgnoreCase);

    public void SetEngineEnabled(string engine, bool enabled)
    {
        string id = engine.Trim().ToLowerInvariant();
        if (supportedEngines.Contains(id, StringComparer.Ordinal) == false)
            throw new ArgumentOutOfRangeException(nameof(engine), "不支持的搜索引擎");

        List<string> selected = GetEnabledEngines().ToList();
        if (enabled && selected.Contains(id) == false)
            selected.Add(id);
        else if (enabled == false)
            selected.Remove(id);
        EnabledEngines = selected;
        Engine = selected.Count == 1 ? selected[0] : selected.Count == 0 ? "none" : "auto";
    }
}
