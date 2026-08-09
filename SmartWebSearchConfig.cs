namespace Alife.Plugin.SmartWebSearch;

public class SmartWebSearchConfig
{
    // === 引擎模式 ===
    // "tavily" = 仅Tavily, "baidu" = 仅百度, "auto" = 智能路由(中文→百度, 英文→Tavily, 配额耗尽自动切换)
    public string Engine { get; set; } = "auto";

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

    // === 通用搜索设置 ===
    // 每次搜索返回的网页结果数量，默认 5
    public int MaxResults { get; set; } = 5;

    // 搜索深度: Tavily用 basic(1额度)/advanced(2额度), 百度用 lite(快速)/standard(完整)
    public string SearchDepth { get; set; } = "basic";

    // === AI搜索设置 ===
    // 高性能版智能搜索生成使用的模型：auto_thinking(自动思考) / thinking / non_thinking
    // 限时免费，搜索+大模型总结一步到位
    public string SummaryModel { get; set; } = "auto_thinking";

    // 标准版智能搜索生成使用的模型：deepseek-v3.2 / deepseek-r1 / ernie-4.5-turbo-32k 等
    // 功能最全面，支持可选深度搜索、知识注入、追问等
    public string ChatSearchModel { get; set; } = "deepseek-v3.2";

    // 是否启用深度搜索（智能搜索生成专用，更精准但更慢，耗费较多额度）
    public bool EnableDeepSearch { get; set; } = false;

    // === 缓存设置 ===
    // 启用后相同查询在TTL内不重复调用API，节省额度
    public bool EnableCache { get; set; } = true;

    // 缓存过期时间（分钟），默认5分钟
    public int CacheTtlMinutes { get; set; } = 5;

    // === 注入设置 ===
    // 隐式注入（4.0 新特性）：开启后函数文档不直接注入系统提示词，
    // AI 需先调用 <smartwebsearch/> 按需加载（省 token，渐进式）；
    // 关闭则显式注入（默认），功能说明直接可用。
    public bool ImplicitInjection { get; set; } = false;
}