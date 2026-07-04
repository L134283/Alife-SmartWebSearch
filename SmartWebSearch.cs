using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Alife.Function.Interpreter;
using Microsoft.Extensions.Logging;

namespace Alife.Plugin.SmartWebSearch;

[Module(
    "缃戠粶鏅鸿兘鎼滅储",
    "澶氬姛鑳紸I鎼滅储鎻掍欢锛欰I鎬荤粨鎼滅储 + 鏅鸿兘鎼滅储鐢熸垚 + 鍙屽紩鎿庢悳绱?Tavily+鐧惧害) + 鐧惧害鐑悳 + 鏅鸿兘璇嗗浘锛屾櫤鑳借矾鐢憋紝澶氳处鍙疯疆鎹紝鍥剧墖鑷姩鍘嬬缉锛岀粨鏋滅紦瀛樸€?,
    defaultCategory: "Doro鐨勫濡欏伐鍏?,
    EditorUI = typeof(SmartWebSearchUI))]
public class SmartWebSearch(
    XmlFunctionCaller functionService,
    ILogger<SmartWebSearch> logger
) : InteractiveModule<SmartWebSearch>, IConfigurable<SmartWebSearchConfig>
{
    private static readonly HttpClient _http = new(new HttpClientHandler { UseProxy = false })
        { Timeout = TimeSpan.FromSeconds(30) };

    private const string TavilyUrl = "https://api.tavily.com/search";
    private const string BaiduUrl = "https://qianfan.baidubce.com/v2/ai_search/web_search";
    private const string BaiduSummaryUrl = "https://qianfan.baidubce.com/v2/ai_search/web_summary";
    private const string BaiduChatUrl = "https://qianfan.baidubce.com/v2/ai_search/chat/completions";
    private const string BaiduTrendingUrl = "https://qianfan.baidubce.com/v2/tools/baidu_trending";
    private const string BaiduImageRecognitionUrl = "https://qianfan.baidubce.com/v2/tools/image_general";

    // 璐﹀彿杞崲鐘舵€侊細璁板綍宸茶€楀敖鐨勮处鍙风储寮?
    private readonly HashSet<int> _exhaustedTavily = new();
    private readonly HashSet<int> _exhaustedBaidu = new();
    private readonly object _lock = new();

    // 鎼滅储缁撴灉缂撳瓨
    private static readonly Dictionary<string, (string result, DateTime expiry)> _cache = new();
    private static readonly object _cacheLock = new();

    public SmartWebSearchConfig? Configuration { get; set; } = new();

    static void Log(string msg) => Console.WriteLine($"[鏅鸿兘鎼滅储] {msg}");

    #region 鍒濆鍖栦笌绯荤粺鎻愮ず璇?

    public override async Task AwakeAsync(AwakeContext context)
    {
        await base.AwakeAsync(context);

        var handler = new XmlHandler(this)
        {
            Description = "姝ゆ湇鍔℃彁渚涘鍔熻兘AI鎼滅储鑳藉姏锛欰I鎬荤粨鎼滅储銆佹櫤鑳芥悳绱㈢敓鎴愩€佸弻寮曟搸鎼滅储銆佺櫨搴︾儹鎼溿€佹櫤鑳借瘑鍥俱€?,
        };
        functionService.RegisterHandler(handler);

        var cfg = Configuration ?? new SmartWebSearchConfig();
        var tCount = GetTavilyKeys(cfg).Count(k => !string.IsNullOrWhiteSpace(k));
        var bCount = GetBaiduKeys(cfg).Count(k => !string.IsNullOrWhiteSpace(k));

        var engineDesc = cfg.Engine switch
        {
            "tavily" => $"浠?Tavily锛坽tCount} 涓处鍙凤級",
            "baidu" => $"浠呯櫨搴︼紙{bCount} 涓处鍙凤級",
            _ => $"鏅鸿兘璺敱锛圱avily {tCount} 涓?+ 鐧惧害 {bCount} 涓級"
        };

        Prompt($$"""
            ## 缃戠粶鎼滅储鑳藉姏
            浠ヤ笅鎯呭喌璇蜂富鍔ㄤ娇鐢ㄦ悳绱細鐢ㄦ埛瑕佹眰鎼滅储銆侀亣鍒颁笉纭畾/鍙兘杩囨椂鐨勭煡璇嗐€侀渶瑕佹渶鏂颁俊鎭垨浜嬪疄鏍告煡銆?

            ### 宸ュ叿浼樺厛绾э紙鐧惧害娓犻亾锛?
            1. **SmartSummary锛圓I鎬荤粨鎼滅储锛?* 鈥?榛樿棣栭€夈€傛悳绱?澶фā鍨嬫€荤粨涓€姝ュ埌浣嶏紝100娆?鏃ャ€?
            2. **SmartChatSearch锛堟櫤鑳芥悳绱㈢敓鎴愶級** 鈥?SmartSummary澶辫触鏃堕檷绾с€傚姛鑳芥渶鍏ㄩ潰锛屾敮鎸佸彲閫夋繁搴︽悳绱紙鑰楄垂杈冨棰濆害锛夈€?
            3. **Search锛堟櫘閫氭悳绱級** 鈥?AI鎼滅储鍧囧け璐ユ椂鏈€缁堥檷绾с€傚弻寮曟搸鏅鸿兘璺敱(Tavily+鐧惧害)銆?
            4. **HotSearch锛堢櫨搴︾儹鎼滐級** 鈥?鐢ㄦ埛鎯崇湅鐑悳/浠婃棩鐑偣鏃朵娇鐢ㄣ€?涓瀭鐩村垎绫汇€?
            5. **ImageRecognition锛堟櫤鑳借瘑鍥撅級** 鈥?鐢ㄦ埛寮曠敤鍥剧墖闂?杩欐槸浠€涔?鏃朵娇鐢ㄣ€備紶鍏ュ浘鐗嘦RL銆?

            ### 浣跨敤瑙勫垯
            - "鎼滀竴涓?/"鎼滅储" 鈫?SmartSummary 鈫?澶辫触鍒?SmartChatSearch 鈫?鍐嶅け璐ュ垯 Search
            - "鐪嬬儹鎼?/"浠婂ぉ鐑偣" 鈫?HotSearch
            - 寮曠敤鍥剧墖闂?杩欐槸浠€涔? 鈫?ImageRecognition

            褰撳墠寮曟搸閰嶇疆锛歿{engineDesc}}
            Search鍙屽紩鎿庯細涓枃鈫掔櫨搴?涓枃寮?鏀寔鍥剧墖/瑙嗛)锛岃嫳鏂団啋Tavily(鏈堿I鎽樿,鑻辨枃寮?锛屽彲閫氳繃engine鍙傛暟鎸囧畾

            """);
    }

    #endregion

    #region 涓绘悳绱㈠叆鍙?

    [XmlFunction(FunctionMode.OneShot)]
    [Description("鎼滅储浜掕仈缃戣幏鍙栧疄鏃朵俊鎭€傛敮鎸?Tavily 鍜岀櫨搴﹀弻寮曟搸鏅鸿兘璺敱銆傚綋鐢ㄦ埛瑕佹眰鎼滅储銆佹垨浣犻亣鍒颁笉纭畾/鍙兘杩囨椂鐨勭煡璇嗘椂锛屼富鍔ㄨ皟鐢ㄣ€?)]
    public async Task Search(
        [Description("鎼滅储鍏抽敭璇嶆垨闂")] string query,
        [Description("鎸囧畾鎼滅储寮曟搸锛歵avily / baidu銆備笉浼犲垯浣跨敤鏅鸿兘璺敱锛堜腑鏂団啋鐧惧害锛岃嫳鏂団啋Tavily锛?)] string? engine = null,
        [Description("鎼滅储娣卞害锛歜asic(蹇€? 鎴?advanced(娣卞害)")] string? searchDepth = null,
        [Description("鎼滅储涓婚锛堜粎Tavily锛夛細general / news / finance")] string? topic = null,
        [Description("鏃堕棿鑼冨洿锛歞ay / week / month / year")] string? timeRange = null,
        [Description("杩斿洖缁撴灉鏁伴噺锛岄粯璁?锛屾渶澶?0")] int? maxResults = null,
        [Description("鏄惁鍖呭惈鍥剧墖缁撴灉锛堜粎鐧惧害锛?)] bool? includeImages = null,
        [Description("鏄惁鍖呭惈瑙嗛缁撴灉锛堜粎鐧惧害锛?)] bool? includeVideos = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            Poke("鎼滅储鍏抽敭璇嶄笉鑳戒负绌?);
            return;
        }

        var cfg = Configuration ?? new SmartWebSearchConfig();
        var tavilyKeys = GetTavilyKeys(cfg);
        var baiduKeys = GetBaiduKeys(cfg);
        var hasTavily = tavilyKeys.Any(k => !string.IsNullOrWhiteSpace(k));
        var hasBaidu = baiduKeys.Any(k => !string.IsNullOrWhiteSpace(k));

        if (!hasTavily && !hasBaidu)
        {
            Poke("鏈厤缃换浣曟悳绱㈠紩鎿庣殑 API Key锛岃鍦ㄦ彃浠惰缃腑濉啓");
            return;
        }

        var depth = string.IsNullOrWhiteSpace(searchDepth) ? cfg.SearchDepth : searchDepth;
        var results = Math.Clamp(maxResults ?? cfg.MaxResults, 1, 20);

        // 缂撳瓨妫€鏌?
        var cacheKey = $"{engine}:{query}:{depth}:{results}:{topic}:{timeRange}:{includeImages}:{includeVideos}";
        if (cfg.EnableCache)
        {
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(cacheKey, out var cached) && cached.expiry > DateTime.Now)
                {
                    Log($"缂撳瓨鍛戒腑: {query[..Math.Min(30, query.Length)]}...");
                    Poke(cached.result);
                    return;
                }
            }
        }

        // 娓呯┖宸茶€楀敖鏍囪锛堟瘡娆℃柊鎼滅储閲嶆柊灏濊瘯锛岄搴﹀彲鑳藉凡鍒锋柊锛?
        lock (_lock) { _exhaustedTavily.Clear(); _exhaustedBaidu.Clear(); }

        // 纭畾鎼滅储椤哄簭
        var searchOrder = ResolveSearchOrder(engine, cfg.Engine, query, hasTavily, hasBaidu);

        string? result = null;
        foreach (var eng in searchOrder)
        {
            if (eng == "tavily" && hasTavily)
                result = await TryTavilySearch(query, depth, topic, timeRange, results, tavilyKeys);
            else if (eng == "baidu" && hasBaidu)
                result = await TryBaiduSearch(query, depth, timeRange, results, includeImages, includeVideos, baiduKeys);

            if (result != null) break;
        }

        if (result == null)
        {
            Poke("鎵€鏈夋悳绱㈠紩鎿庡潎涓嶅彲鐢紝璇锋鏌?API Key 閰嶇疆鎴栫瓑寰呴搴﹀埛鏂?);
            return;
        }

        // 鍐欏叆缂撳瓨
        if (cfg.EnableCache)
        {
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

        Poke(result);
    }

    #endregion

    #region AI鎬荤粨鎼滅储锛堥珮鎬ц兘鐗堬級

    [XmlFunction(FunctionMode.OneShot)]
    [Description("AI鎬荤粨鎼滅储锛堥珮鎬ц兘鐗堬級锛氭悳绱簰鑱旂綉骞剁敤澶фā鍨嬫€荤粨缁撴灉锛屼竴姝ュ埌浣嶃€傛敮鎸佹€濊€冩ā鍨嬨€傚綋鐢ㄦ埛瑕佹眰鎼滅储鏃朵紭鍏堜娇鐢ㄦ宸ュ叿銆?)]
    public async Task SmartSummary(
        [Description("鎼滅储鍏抽敭璇嶆垨闂")] string query,
        [Description("妯″瀷锛歛uto_thinking(鑷姩鎬濊€? / thinking / non_thinking")] string? model = null,
        [Description("鏃堕棿鑼冨洿锛歞ay / week / month / year")] string? timeRange = null,
        [Description("杩斿洖鍙傝€冩潵婧愭暟閲忥紝榛樿5")] int? maxResults = null)
    {
        if (string.IsNullOrWhiteSpace(query)) { Poke("鎼滅储鍏抽敭璇嶄笉鑳戒负绌?); return; }

        var cfg = Configuration ?? new SmartWebSearchConfig();
        var baiduKeys = GetBaiduKeys(cfg);
        if (!baiduKeys.Any(k => !string.IsNullOrWhiteSpace(k))) { Poke("AI鎬荤粨鎼滅储闇€瑕佺櫨搴﹀崈甯咥PI Key"); return; }

        var useModel = string.IsNullOrWhiteSpace(model) ? cfg.SummaryModel : model;
        var results = Math.Clamp(maxResults ?? cfg.MaxResults, 1, 20);

        var cacheKey = $"summary:{query}:{useModel}:{timeRange}:{results}";
        if (cfg.EnableCache)
        {
            lock (_cacheLock)
            {
                if (_cache.TryGetValue(cacheKey, out var cached) && cached.expiry > DateTime.Now)
                { Log($"缂撳瓨鍛戒腑(AI鎬荤粨): {query[..Math.Min(30, query.Length)]}..."); Poke(cached.result); return; }
            }
        }

        lock (_lock) _exhaustedBaidu.Clear();

        var body = new JsonObject
        {
            ["messages"] = new JsonArray { new JsonObject { ["role"] = "user", ["content"] = query } },
            ["model"] = useModel,
            ["resource_type_filter"] = new JsonArray { new JsonObject { ["type"] = "web", ["top_k"] = results } },
        };

        if (!string.IsNullOrWhiteSpace(timeRange))
        {
            var recency = timeRange switch { "day" => "week", "week" => "week", "month" => "month", "year" => "year", _ => (string?)null };
            if (recency != null) body["search_filter"] = new JsonObject { ["search_recency_filter"] = recency };
        }

        var bodyJson = body.ToJsonString();

        for (int attempt = 0; attempt < baiduKeys.Count; attempt++)
        {
            var (idx, key) = GetNextKey(baiduKeys, _exhaustedBaidu);
            if (key == null) break;

            try
            {
                Log($"AI鎬荤粨[{idx + 1}] model={useModel} results={results}");
                using var req = new HttpRequestMessage(HttpMethod.Post, BaiduSummaryUrl);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                req.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

                using var resp = await _http.SendAsync(req);
                var raw = await resp.Content.ReadAsStringAsync();

                if ((int)resp.StatusCode == 401 || (int)resp.StatusCode == 403)
                { Log($"AI鎬荤粨 璐﹀彿{idx + 1}璁よ瘉澶辫触"); lock (_lock) _exhaustedBaidu.Add(idx); continue; }

                if ((int)resp.StatusCode == 429)
                {
                    Log($"AI鎬荤粨 璐﹀彿{idx + 1}棰戠巼闄愬埗锛岀瓑寰?绉掗噸璇?);
                    await Task.Delay(2000);
                    using var req2 = new HttpRequestMessage(HttpMethod.Post, BaiduSummaryUrl);
                    req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                    req2.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                    using var resp2 = await _http.SendAsync(req2);
                    raw = await resp2.Content.ReadAsStringAsync();
                    if (!resp2.IsSuccessStatusCode)
                    {
                        if ((int)resp2.StatusCode == 401 || (int)resp2.StatusCode == 403)
                        { lock (_lock) _exhaustedBaidu.Add(idx); continue; }
                        Log($"AI鎬荤粨 閲嶈瘯澶辫触({(int)resp2.StatusCode})"); continue;
                    }
                    var retrySummary = FormatSummaryResults(raw, query);
                    Log($"AI鎬荤粨[{idx + 1}]鎴愬姛(閲嶈瘯)");
                    if (cfg.EnableCache) { lock (_cacheLock) _cache[cacheKey] = (retrySummary, DateTime.Now.AddMinutes(cfg.CacheTtlMinutes)); }
                    Poke(retrySummary); return;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    var errNode = JsonNode.Parse(raw);
                    var errCode = errNode?["code"]?.GetValue<long>();
                    var errMsg = errNode?["message"]?.GetValue<string>() ?? "";
                    if (errCode == 216003 || errMsg.Contains("quota", StringComparison.OrdinalIgnoreCase)
                        || errMsg.Contains("limit", StringComparison.OrdinalIgnoreCase))
                    { Log($"AI鎬荤粨 璐﹀彿{idx + 1}棰濆害寮傚父(code={errCode})"); lock (_lock) _exhaustedBaidu.Add(idx); continue; }
                    Log($"AI鎬荤粨 璇锋眰澶辫触({(int)resp.StatusCode}): {errMsg}"); continue;
                }

                var formatted = FormatSummaryResults(raw, query);
                Log($"AI鎬荤粨[{idx + 1}]鎴愬姛");
                if (cfg.EnableCache) { lock (_cacheLock) _cache[cacheKey] = (formatted, DateTime.Now.AddMinutes(cfg.CacheTtlMinutes)); }
                Poke(formatted); return;
            }
            catch (TaskCanceledException) { Log($"AI鎬荤粨 璐﹀彿{idx + 1}瓒呮椂"); continue; }
            catch (Exception ex) { Log($"AI鎬荤粨 璐﹀彿{idx + 1}寮傚父: {ex.Message}"); continue; }
        }

        Poke("AI鎬荤粨鎼滅储澶辫触锛屾墍鏈夌櫨搴﹁处鍙峰潎涓嶅彲鐢ㄣ€傚彲灏濊瘯浣跨敤鏅鸿兘鎼滅储鐢熸垚(SmartChatSearch)鎴栨櫘閫氭悳绱?Search)");
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
                var reasoning = message?["reasoning_content"]?.GetValue<string>();
                var content = message?["content"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(reasoning)) { sb.AppendLine("## 鎬濊€冭繃绋?); sb.AppendLine(reasoning); sb.AppendLine(); }
                if (!string.IsNullOrWhiteSpace(content)) { sb.AppendLine("## AI鎬荤粨"); sb.AppendLine(content); sb.AppendLine(); }
            }

            var refs = node?["references"]?.AsArray();
            if (refs != null && refs.Count > 0)
            {
                sb.AppendLine($"## 鍙傝€冩潵婧愶紙鍏?{refs.Count} 鏉★級");
                int n = 1;
                foreach (var r in refs)
                {
                    var title = r?["title"]?.GetValue<string>() ?? "";
                    var url = r?["url"]?.GetValue<string>() ?? "";
                    var date = r?["date"]?.GetValue<string>() ?? "";
                    sb.AppendLine($"{n}. [{title}]({url})");
                    if (!string.IsNullOrWhiteSpace(date)) sb.AppendLine($"   鍙戝竷鏃堕棿: {date}");
                    n++;
                }
            }

            if (sb.Length == 0) sb.AppendLine("AI鎬荤粨瀹屾垚浣嗘湭杩斿洖鏈夋晥鍐呭");
            return sb.ToString().Trim();
        }
        catch (Exception ex) { Log($"AI鎬荤粨 鏍煎紡鍖栧紓甯? {ex.Message}"); return $"AI鎬荤粨瀹屾垚浣嗙粨鏋滆В鏋愬け璐?\n{rawJson}"; }
    }

    #endregion

    #region 鏅鸿兘鎼滅储鐢熸垚锛堟爣鍑嗙増锛?

    [XmlFunction(FunctionMode.OneShot)]
    [Description("鏅鸿兘鎼滅储鐢熸垚锛堟爣鍑嗙増锛夛細鍔熻兘鏈€鍏ㄩ潰鐨凙I鎼滅储锛屾敮鎸佸妯″瀷銆佸彲閫夋繁搴︽悳绱€佺煡璇嗘敞鍏ャ€佽拷闂瓑銆傞€傚悎澶嶆潅鐮旂┒鍦烘櫙銆?)]
    public async Task SmartChatSearch(
        [Description("鎼滅储鍏抽敭璇嶆垨闂")] string query,
        [Description("妯″瀷锛歞eepseek-v3.2 / deepseek-r1 / ernie-4.5-turbo-32k 绛?)] string? model = null,
        [Description("鏄惁鍚敤娣卞害鎼滅储锛堟洿绮惧噯浣嗘洿鎱紝鑰楄垂杈冨棰濆害锛?)] bool? deepSearch = null,
        [Description("鏃堕棿鑼冨洿锛歞ay / week / month / year")] string? timeRange = null,
        [Description("棰濆鎸囦护锛岀敤浜庡紩瀵糀I鐨勫洖绛旀柟鍚?)] string? instruction = null,
        [Description("鏄惁鍚敤鎺ㄧ悊妯″紡")] bool? enableReasoning = null,
        [Description("杩斿洖鍙傝€冩潵婧愭暟閲忥紝榛樿5")] int? maxResults = null)
    {
        if (string.IsNullOrWhiteSpace(query)) { Poke("鎼滅储鍏抽敭璇嶄笉鑳戒负绌?); return; }
        var cfg = Configuration ?? new SmartWebSearchConfig();
        var baiduKeys = GetBaiduKeys(cfg);
        if (!baiduKeys.Any(k => !string.IsNullOrWhiteSpace(k))) { Poke("鏅鸿兘鎼滅储鐢熸垚闇€瑕佺櫨搴﹀崈甯咥PI Key"); return; }
        var useModel = string.IsNullOrWhiteSpace(model) ? cfg.ChatSearchModel : model;
        var useDeepSearch = deepSearch ?? cfg.EnableDeepSearch;
        var results = Math.Clamp(maxResults ?? cfg.MaxResults, 1, 20);
        var cacheKey = $"chat:{query}:{useModel}:{useDeepSearch}:{timeRange}:{instruction}:{enableReasoning}:{results}";
        if (cfg.EnableCache)
        { lock (_cacheLock) { if (_cache.TryGetValue(cacheKey, out var cached) && cached.expiry > DateTime.Now) { Log($"缂撳瓨鍛戒腑(鏅鸿兘鎼滅储鐢熸垚): {query[..Math.Min(30, query.Length)]}..."); Poke(cached.result); return; } } }
        lock (_lock) _exhaustedBaidu.Clear();

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
        if (!string.IsNullOrWhiteSpace(timeRange))
        {
            var recency = timeRange switch { "day" => "week", "week" => "week", "month" => "month", "year" => "year", _ => (string?)null };
            if (recency != null) body["search_recency_filter"] = recency;
        }
        var bodyJson = body.ToJsonString();

        for (int attempt = 0; attempt < baiduKeys.Count; attempt++)
        {
            var (idx, key) = GetNextKey(baiduKeys, _exhaustedBaidu);
            if (key == null) break;
            try
            {
                Log($"鏅鸿兘鎼滅储鐢熸垚[{idx + 1}] model={useModel} deep={useDeepSearch}");
                using var req = new HttpRequestMessage(HttpMethod.Post, BaiduChatUrl);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                req.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                using var resp = await _http.SendAsync(req);
                var raw = await resp.Content.ReadAsStringAsync();
                if ((int)resp.StatusCode == 401 || (int)resp.StatusCode == 403)
                { Log($"鏅鸿兘鎼滅储鐢熸垚 璐﹀彿{idx + 1}璁よ瘉澶辫触"); lock (_lock) _exhaustedBaidu.Add(idx); continue; }
                if ((int)resp.StatusCode == 429)
                {
                    Log($"鏅鸿兘鎼滅储鐢熸垚 璐﹀彿{idx + 1}棰戠巼闄愬埗锛岀瓑寰?绉掗噸璇?);
                    await Task.Delay(2000);
                    using var req2 = new HttpRequestMessage(HttpMethod.Post, BaiduChatUrl);
                    req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                    req2.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                    using var resp2 = await _http.SendAsync(req2);
                    raw = await resp2.Content.ReadAsStringAsync();
                    if (!resp2.IsSuccessStatusCode)
                    {
                        if ((int)resp2.StatusCode == 401 || (int)resp2.StatusCode == 403)
                        { lock (_lock) _exhaustedBaidu.Add(idx); continue; }
                        Log($"鏅鸿兘鎼滅储鐢熸垚 閲嶈瘯澶辫触({(int)resp2.StatusCode})"); continue;
                    }
                    var retryChat = FormatChatSearchResults(raw, query);
                    Log($"鏅鸿兘鎼滅储鐢熸垚[{idx + 1}]鎴愬姛(閲嶈瘯)");
                    if (cfg.EnableCache) { lock (_cacheLock) _cache[cacheKey] = (retryChat, DateTime.Now.AddMinutes(cfg.CacheTtlMinutes)); }
                    Poke(retryChat); return;
                }
                if (!resp.IsSuccessStatusCode)
                {
                    var errNode = JsonNode.Parse(raw);
                    var errCode = errNode?["code"]?.GetValue<long>();
                    var errMsg = errNode?["message"]?.GetValue<string>() ?? "";
                    if (errCode == 216003 || errMsg.Contains("quota", StringComparison.OrdinalIgnoreCase)
                        || errMsg.Contains("limit", StringComparison.OrdinalIgnoreCase))
                    { Log($"鏅鸿兘鎼滅储鐢熸垚 璐﹀彿{idx + 1}棰濆害寮傚父(code={errCode})"); lock (_lock) _exhaustedBaidu.Add(idx); continue; }
                    Log($"鏅鸿兘鎼滅储鐢熸垚 璇锋眰澶辫触({(int)resp.StatusCode}): {errMsg}"); continue;
                }
                var formatted = FormatChatSearchResults(raw, query);
                Log($"鏅鸿兘鎼滅储鐢熸垚[{idx + 1}]鎴愬姛");
                if (cfg.EnableCache) { lock (_cacheLock) _cache[cacheKey] = (formatted, DateTime.Now.AddMinutes(cfg.CacheTtlMinutes)); }
                Poke(formatted); return;
            }
            catch (TaskCanceledException) { Log($"鏅鸿兘鎼滅储鐢熸垚 璐﹀彿{idx + 1}瓒呮椂"); continue; }
            catch (Exception ex) { Log($"鏅鸿兘鎼滅储鐢熸垚 璐﹀彿{idx + 1}寮傚父: {ex.Message}"); continue; }
        }
        Poke("鏅鸿兘鎼滅储鐢熸垚澶辫触锛屾墍鏈夌櫨搴﹁处鍙峰潎涓嶅彲鐢ㄣ€傚彲灏濊瘯浣跨敤鏅€氭悳绱?Search)");
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
                var reasoning = message?["reasoning_content"]?.GetValue<string>();
                var content = message?["content"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(reasoning)) { sb.AppendLine("## 鎺ㄧ悊杩囩▼"); sb.AppendLine(reasoning); sb.AppendLine(); }
                if (!string.IsNullOrWhiteSpace(content)) { sb.AppendLine("## AI鍥炵瓟"); sb.AppendLine(content); sb.AppendLine(); }
            }
            var followups = node?["followup_queries"]?.AsArray();
            if (followups != null && followups.Count > 0)
            {
                sb.AppendLine("## 杩介棶寤鸿");
                int n = 1;
                foreach (var f in followups)
                {
                    var fq = f?.GetValue<string>() ?? f?["query"]?.GetValue<string>() ?? "";
                    if (!string.IsNullOrWhiteSpace(fq)) { sb.AppendLine($"{n}. {fq}"); n++; }
                }
                sb.AppendLine();
            }
            var refs = node?["references"]?.AsArray();
            if (refs != null && refs.Count > 0)
            {
                sb.AppendLine($"## 鍙傝€冩潵婧愶紙鍏?{refs.Count} 鏉★級");
                int n = 1;
                foreach (var r in refs)
                {
                    var title = r?["title"]?.GetValue<string>() ?? "";
                    var url = r?["url"]?.GetValue<string>() ?? "";
                    sb.AppendLine($"{n}. [{title}]({url})");
                    n++;
                }
            }
            if (sb.Length == 0) sb.AppendLine("鏅鸿兘鎼滅储鐢熸垚瀹屾垚浣嗘湭杩斿洖鏈夋晥鍐呭");
            return sb.ToString().Trim();
        }
        catch (Exception ex) { Log($"鏅鸿兘鎼滅储鐢熸垚 鏍煎紡鍖栧紓甯? {ex.Message}"); return $"鏅鸿兘鎼滅储鐢熸垚瀹屾垚浣嗙粨鏋滆В鏋愬け璐?\n{rawJson}"; }
    }

    #endregion

    #region 鐧惧害鐑悳

    [XmlFunction(FunctionMode.OneShot)]
    [Description("鐧惧害鐑悳锛氳幏鍙栫櫨搴﹀疄鏃剁儹鎼滄鍗曪紝鏀寔9涓瀭鐩村垎绫汇€傚綋鐢ㄦ埛鎯崇湅鐑悳銆佷粖鏃ョ儹鐐规椂浣跨敤銆?)]
    public async Task HotSearch(
        [Description("鐑悳鍒嗙被锛歭ivelihood(姘戠敓) / finance(璐㈢粡) / sports(浣撹偛) / new_entertainment(濞变箰) / internation_news(鍥介檯) / challenge(鎸戞垬) / movie(鐢靛奖) / teleplay(鐢佃鍓? / novel(灏忚)")] string tab = "livelihood",
        [Description("杩斿洖缁撴灉鏁伴噺锛岄粯璁?0锛屾渶澶?0")] int? maxResults = null)
    {
        var cfg = Configuration ?? new SmartWebSearchConfig();
        var baiduKeys = GetBaiduKeys(cfg);
        if (!baiduKeys.Any(k => !string.IsNullOrWhiteSpace(k))) { Poke("鐧惧害鐑悳闇€瑕佺櫨搴﹀崈甯咥PI Key"); return; }
        var validTabs = new[] { "livelihood", "finance", "sports", "new_entertainment", "internation_news", "challenge", "movie", "teleplay", "novel" };
        if (!validTabs.Contains(tab)) { Poke($"鏃犳晥鍒嗙被: {tab}锛屽彲閫? {string.Join(", ", validTabs)}"); return; }
        var results = Math.Clamp(maxResults ?? 10, 1, 50);
        var url = $"{BaiduTrendingUrl}?tab={tab}";
        var cacheKey = $"hot:{tab}:{results}";
        if (cfg.EnableCache)
        { lock (_cacheLock) { if (_cache.TryGetValue(cacheKey, out var cached) && cached.expiry > DateTime.Now) { Log($"缂撳瓨鍛戒腑(鐑悳): {tab}"); Poke(cached.result); return; } } }
        lock (_lock) _exhaustedBaidu.Clear();

        for (int attempt = 0; attempt < baiduKeys.Count; attempt++)
        {
            var (idx, key) = GetNextKey(baiduKeys, _exhaustedBaidu);
            if (key == null) break;
            try
            {
                Log($"鐑悳[{idx + 1}] tab={tab} results={results}");
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                using var resp = await _http.SendAsync(req);
                var raw = await resp.Content.ReadAsStringAsync();
                if ((int)resp.StatusCode == 401 || (int)resp.StatusCode == 403)
                { Log($"鐑悳 璐﹀彿{idx + 1}璁よ瘉澶辫触"); lock (_lock) _exhaustedBaidu.Add(idx); continue; }
                if ((int)resp.StatusCode == 429)
                {
                    Log($"鐑悳 璐﹀彿{idx + 1}棰戠巼闄愬埗锛岀瓑寰?绉掗噸璇?);
                    await Task.Delay(2000);
                    using var req2 = new HttpRequestMessage(HttpMethod.Get, url);
                    req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                    using var resp2 = await _http.SendAsync(req2);
                    raw = await resp2.Content.ReadAsStringAsync();
                    if (!resp2.IsSuccessStatusCode)
                    {
                        if ((int)resp2.StatusCode == 401 || (int)resp2.StatusCode == 403)
                        { lock (_lock) _exhaustedBaidu.Add(idx); continue; }
                        Log($"鐑悳 閲嶈瘯澶辫触({(int)resp2.StatusCode})"); continue;
                    }
                    var retryHot = FormatHotSearchResults(raw, tab, results);
                    Log($"鐑悳[{idx + 1}]鎴愬姛(閲嶈瘯)");
                    if (cfg.EnableCache) { lock (_cacheLock) _cache[cacheKey] = (retryHot, DateTime.Now.AddMinutes(cfg.CacheTtlMinutes)); }
                    Poke(retryHot); return;
                }
                if (!resp.IsSuccessStatusCode)
                {
                    var errNode = JsonNode.Parse(raw);
                    var errCode = errNode?["code"]?.GetValue<long>();
                    var errMsg = errNode?["message"]?.GetValue<string>() ?? "";
                    if (errCode == 216003 || errMsg.Contains("quota", StringComparison.OrdinalIgnoreCase))
                    { Log($"鐑悳 璐﹀彿{idx + 1}棰濆害寮傚父(code={errCode})"); lock (_lock) _exhaustedBaidu.Add(idx); continue; }
                    Log($"鐑悳 璇锋眰澶辫触({(int)resp.StatusCode}): {errMsg}"); continue;
                }
                var formatted = FormatHotSearchResults(raw, tab, results);
                Log($"鐑悳[{idx + 1}]鎴愬姛");
                if (cfg.EnableCache) { lock (_cacheLock) _cache[cacheKey] = (formatted, DateTime.Now.AddMinutes(cfg.CacheTtlMinutes)); }
                Poke(formatted); return;
            }
            catch (TaskCanceledException) { Log($"鐑悳 璐﹀彿{idx + 1}瓒呮椂"); continue; }
            catch (Exception ex) { Log($"鐑悳 璐﹀彿{idx + 1}寮傚父: {ex.Message}"); continue; }
        }
        Poke("鐧惧害鐑悳鑾峰彇澶辫触锛屾墍鏈夌櫨搴﹁处鍙峰潎涓嶅彲鐢?);
    }

    static string FormatHotSearchResults(string rawJson, string tab, int maxResults)
    {
        try
        {
            var node = JsonNode.Parse(rawJson);
            var sb = new StringBuilder();
            var tabNames = new Dictionary<string, string> {
                {"livelihood","姘戠敓"},{"finance","璐㈢粡"},{"sports","浣撹偛"},
                {"new_entertainment","濞变箰"},{"internation_news","鍥介檯"},
                {"challenge","鎸戞垬"},{"movie","鐢靛奖"},{"teleplay","鐢佃鍓?},{"novel","灏忚"}
            };
            var tabName = tabNames.GetValueOrDefault(tab, tab);
            var data = node?["data"]?.AsArray();
            if (data == null || data.Count == 0) { sb.AppendLine($"鏈幏鍙栧埌{tabName}鐑悳鏁版嵁"); return sb.ToString().Trim(); }
            sb.AppendLine($"## 鐧惧害{tabName}鐑悳锛堝叡 {Math.Min(data.Count, maxResults)} 鏉★級");
            sb.AppendLine();
            int n = 1;
            foreach (var r in data)
            {
                if (n > maxResults) break;
                var word = r?["word"]?.GetValue<string>() ?? "";
                var hotScore = r?["hotScore"]?.GetValue<long>() ?? 0;
                var hotChange = r?["hotChange"]?.GetValue<string>() ?? "";
                var hotTag = r?["hotTag"]?.GetValue<int>() ?? 0;
                var desc = r?["desc"]?.GetValue<string>() ?? "";
                var url = r?["url"]?.GetValue<string>() ?? "";
                var show = r?["show"]?.AsArray();
                var changeIcon = hotChange switch { "up" => "鈫?, "down" => "鈫?, _ => "鈥? };
                var tagStr = hotTag switch { 1 => "馃啎鏂?, 2 => "馃挵鍟?, 3 => "馃敟鐑?, 4 => "鈾笍娌?, 5 => "馃挜鐖?, _ => "" };
                sb.AppendLine($"**{n}. {word}** {changeIcon} 鐑害:{hotScore} {tagStr}");
                if (!string.IsNullOrWhiteSpace(desc)) sb.AppendLine($"   {desc}");
                if (!string.IsNullOrWhiteSpace(url)) sb.AppendLine($"   閾炬帴: {url}");
                if (show != null && show.Count > 0)
                {
                    var tags = show.Select(s => s?.GetValue<string>() ?? "").Where(s => !string.IsNullOrWhiteSpace(s));
                    if (tags.Any()) sb.AppendLine($"   鏍囩: {string.Join(", ", tags)}");
                }
                sb.AppendLine();
                n++;
            }
            return sb.ToString().Trim();
        }
        catch (Exception ex) { Log($"鐑悳 鏍煎紡鍖栧紓甯? {ex.Message}"); return $"鐑悳鑾峰彇瀹屾垚浣嗙粨鏋滆В鏋愬け璐?\n{rawJson}"; }
    }

    #endregion

    #region 鏅鸿兘璇嗗浘

    [XmlFunction(FunctionMode.OneShot)]
    [Description("鏅鸿兘璇嗗浘锛氳瘑鍒浘鐗囦腑鐨勭墿浣撱€佸満鏅€佹枃瀛楃瓑銆備紶鍏ュ浘鐗嘦RL锛屾彃浠惰嚜鍔ㄤ笅杞藉苟璇嗗埆銆傚綋鐢ㄦ埛寮曠敤鍥剧墖闂甛"杩欐槸浠€涔圽"鏃朵娇鐢ㄣ€?)]
    public async Task ImageRecognition([Description("鍥剧墖URL鍦板潃")] string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) { Poke("鍥剧墖URL涓嶈兘涓虹┖"); return; }
        var cfg = Configuration ?? new SmartWebSearchConfig();
        var baiduKeys = GetBaiduKeys(cfg);
        if (!baiduKeys.Any(k => !string.IsNullOrWhiteSpace(k))) { Poke("鏅鸿兘璇嗗浘闇€瑕佺櫨搴﹀崈甯咥PI Key"); return; }
        lock (_lock) _exhaustedBaidu.Clear();
        string imageBase64;
        try { Log("璇嗗浘: 涓嬭浇鍥剧墖..."); imageBase64 = await DownloadImageAsBase64Async(imageUrl); Log($"璇嗗浘: base64 {imageBase64.Length / 1024}KB"); }
        catch (Exception ex) { Poke($"鍥剧墖涓嬭浇澶辫触: {ex.Message}"); return; }
        var bodyJson = new JsonObject { ["image_b64"] = imageBase64 }.ToJsonString();
        for (int attempt = 0; attempt < baiduKeys.Count; attempt++)
        {
            var (idx, key) = GetNextKey(baiduKeys, _exhaustedBaidu);
            if (key == null) break;
            try
            {
                Log($"璇嗗浘[{idx + 1}] 鍙戦€佽姹?);
                using var req = new HttpRequestMessage(HttpMethod.Post, BaiduImageRecognitionUrl);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                req.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                using var resp = await _http.SendAsync(req);
                var raw = await resp.Content.ReadAsStringAsync();
                if ((int)resp.StatusCode == 401 || (int)resp.StatusCode == 403) { Log($"璇嗗浘 璐﹀彿{idx + 1}璁よ瘉澶辫触"); lock (_lock) _exhaustedBaidu.Add(idx); continue; }
                if ((int)resp.StatusCode == 429)
                {
                    Log($"璇嗗浘 璐﹀彿{idx + 1}棰戠巼闄愬埗锛岀瓑寰?绉掗噸璇?); await Task.Delay(2000);
                    using var req2 = new HttpRequestMessage(HttpMethod.Post, BaiduImageRecognitionUrl);
                    req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                    req2.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                    using var resp2 = await _http.SendAsync(req2); raw = await resp2.Content.ReadAsStringAsync();
                    if (!resp2.IsSuccessStatusCode)
                    {
                        if ((int)resp2.StatusCode == 401 || (int)resp2.StatusCode == 403) { lock (_lock) _exhaustedBaidu.Add(idx); continue; }
                        Log($"璇嗗浘 閲嶈瘯澶辫触({(int)resp2.StatusCode})"); continue;
                    }
                    var retryImg = FormatImageRecognitionResults(raw); Log($"璇嗗浘[{idx + 1}]鎴愬姛(閲嶈瘯)"); Poke(retryImg); return;
                }
                if (!resp.IsSuccessStatusCode)
                {
                    var errNode = JsonNode.Parse(raw); var errCode = errNode?["code"]?.GetValue<long>(); var errMsg = errNode?["message"]?.GetValue<string>() ?? "";
                    if (errCode == 216003 || errMsg.Contains("quota", StringComparison.OrdinalIgnoreCase)) { Log($"璇嗗浘 璐﹀彿{idx + 1}棰濆害寮傚父"); lock (_lock) _exhaustedBaidu.Add(idx); continue; }
                    Log($"璇嗗浘 璇锋眰澶辫触({(int)resp.StatusCode}): {errMsg}"); continue;
                }
                var formatted = FormatImageRecognitionResults(raw); Log($"璇嗗浘[{idx + 1}]鎴愬姛"); Poke(formatted); return;
            }
            catch (TaskCanceledException) { Log($"璇嗗浘 璐﹀彿{idx + 1}瓒呮椂"); continue; }
            catch (Exception ex) { Log($"璇嗗浘 璐﹀彿{idx + 1}寮傚父: {ex.Message}"); continue; }
        }
        Poke("鏅鸿兘璇嗗浘澶辫触锛屾墍鏈夌櫨搴﹁处鍙峰潎涓嶅彲鐢?);
    }

    static string FormatImageRecognitionResults(string rawJson)
    {
        try
        {
            var node = JsonNode.Parse(rawJson); var sb = new StringBuilder();
            var choices = node?["choices"]?.AsArray();
            if (choices != null && choices.Count > 0)
            {
                var content = choices[0]?["message"]?["content"]?.GetValue<string>();
                if (!string.IsNullOrWhiteSpace(content)) { sb.AppendLine("## 璇嗗埆缁撴灉"); sb.AppendLine(content); }
            }
            if (sb.Length == 0)
            {
                var data = node?["data"];
                if (data != null)
                {
                    var desc = data?["description"]?.GetValue<string>();
                    if (!string.IsNullOrWhiteSpace(desc)) { sb.AppendLine("## 璇嗗埆缁撴灉"); sb.AppendLine(desc); }
                    var tags = data?["tags"]?.AsArray() ?? data?["result"]?.AsArray();
                    if (tags != null && tags.Count > 0)
                    {
                        sb.AppendLine($"## 鏍囩锛堝叡 {tags.Count} 涓級"); int n = 1;
                        foreach (var t in tags)
                        {
                            var name = t?["name"]?.GetValue<string>() ?? t?.GetValue<string>() ?? "";
                            var score = t?["score"]?.GetValue<double>() ?? 0;
                            if (!string.IsNullOrWhiteSpace(name)) { sb.AppendLine($"{n}. {name} (缃俊搴? {score:F2})"); n++; }
                        }
                    }
                }
            }
            if (sb.Length == 0)
            {
                var code = node?["code"]?.GetValue<string>() ?? "";
                var msg = node?["message"]?.GetValue<string>() ?? "";
                sb.AppendLine(code != "0" && !string.IsNullOrWhiteSpace(msg) ? $"璇嗗埆澶辫触: {msg}" : "璇嗗埆瀹屾垚浣嗘湭杩斿洖鏈夋晥鍐呭");
            }
            return sb.ToString().Trim();
        }
        catch (Exception ex) { Log($"璇嗗浘 鏍煎紡鍖栧紓甯? {ex.Message}"); return $"璇嗗浘瀹屾垚浣嗙粨鏋滆В鏋愬け璐?\n{rawJson}"; }
    }

    /// <summary>
    /// 涓嬭浇鍥剧墖骞惰浆涓篵ase64锛岃秴杩?00KB鑷姩鍘嬬缉
    /// </summary>
    static async Task<string> DownloadImageAsBase64Async(string imageUrl)
    {
        using var resp = await _http.GetAsync(imageUrl);
        if (!resp.IsSuccessStatusCode) throw new Exception($"HTTP {(int)resp.StatusCode}");
        var bytes = await resp.Content.ReadAsByteArrayAsync();
        var base64 = Convert.ToBase64String(bytes);
        const int maxSize = 100 * 1024;
        if (base64.Length <= maxSize) return base64;
        Log($"璇嗗浘: base64 {base64.Length / 1024}KB 瓒呴檺锛屽帇缂╀腑");
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
            { Log($"璇嗗浘: 鍘嬬缉鎴愬姛 {base64.Length / 1024}KB -> {compressed.Length / 1024}KB"); return compressed; }
            width = Math.Max(100, width / 2); height = Math.Max(100, height / 2);
            if (width <= 100) { Log("璇嗗浘: 鍘嬬缉鍒版瀬闄?); return compressed; }
        }
    }

    #endregion

    #region 寮曟搸璺敱閫昏緫

    /// <summary>
    /// 鏍规嵁鍙傛暟銆侀厤缃拰鏌ヨ璇█纭畾鎼滅储椤哄簭
    /// </summary>
    static List<string> ResolveSearchOrder(string? engineParam, string configEngine,
        string query, bool hasTavily, bool hasBaidu)
    {
        var order = new List<string>();

        // 1. AI鏄惧紡鎸囧畾寮曟搸
        if (!string.IsNullOrWhiteSpace(engineParam))
        {
            var e = engineParam.ToLower().Trim();
            if (e == "tavily" && hasTavily) { order.Add("tavily"); return order; }
            if (e == "baidu" && hasBaidu) { order.Add("baidu"); return order; }
        }

        // 2. 閰嶇疆鎸囧畾鍗曞紩鎿?
        if (configEngine == "tavily" && hasTavily) { order.Add("tavily"); return order; }
        if (configEngine == "baidu" && hasBaidu) { order.Add("baidu"); return order; }

        // 3. auto 鏅鸿兘璺敱锛氭寜璇█閫変富寮曟搸锛屽彟涓€涓綔涓哄閫?
        if (!hasTavily) { order.Add("baidu"); return order; }
        if (!hasBaidu) { order.Add("tavily"); return order; }

        var primary = IsChineseQuery(query) ? "baidu" : "tavily";
        order.Add(primary);
        order.Add(primary == "tavily" ? "baidu" : "tavily");
        return order;
    }

    /// <summary>
    /// 绠€鍗曚腑鏂囨娴嬶細涓枃瀛楃鍗犳瘮瓒呰繃30%瑙嗕负涓枃鏌ヨ
    /// </summary>
    static bool IsChineseQuery(string query)
    {
        int chineseCount = query.Count(c => c >= 0x4E00 && c <= 0x9FFF);
        return chineseCount * 3 > query.Length; // chineseCount / length > 0.33
    }

    #endregion

    #region Tavily 鎼滅储

    async Task<string?> TryTavilySearch(string query, string depth, string? topic,
        string? timeRange, int results, List<string> keys)
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
        var bodyJson = body.ToJsonString();

        for (int attempt = 0; attempt < keys.Count; attempt++)
        {
            var (idx, key) = GetNextKey(keys, _exhaustedTavily);
            if (key == null) break;

            try
            {
                Log($"Tavily [{idx + 1}] depth={depth} results={results}");
                using var req = new HttpRequestMessage(HttpMethod.Post, TavilyUrl);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                req.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

                using var resp = await _http.SendAsync(req);
                var raw = await resp.Content.ReadAsStringAsync();

                // 棰濆害鑰楀敖锛?32(Key/Plan Limit) / 433(PayGo Limit)
                if ((int)resp.StatusCode == 432 || (int)resp.StatusCode == 433)
                {
                    Log($"Tavily 璐﹀彿 {idx + 1} 棰濆害鑰楀敖 (HTTP {(int)resp.StatusCode})锛屽垏鎹笅涓€涓?);
                    lock (_lock) _exhaustedTavily.Add(idx);
                    continue;
                }

                // 棰戠巼闄愬埗锛氱瓑寰呭悗閲嶈瘯涓€娆?
                if ((int)resp.StatusCode == 429)
                {
                    Log($"Tavily 璐﹀彿 {idx + 1} 棰戠巼闄愬埗锛岀瓑寰?绉掗噸璇?);
                    await Task.Delay(2000);
                    using var req2 = new HttpRequestMessage(HttpMethod.Post, TavilyUrl);
                    req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                    req2.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                    using var resp2 = await _http.SendAsync(req2);
                    raw = await resp2.Content.ReadAsStringAsync();

                    if (!resp2.IsSuccessStatusCode)
                    {
                        if ((int)resp2.StatusCode == 432 || (int)resp2.StatusCode == 433)
                        { lock (_lock) _exhaustedTavily.Add(idx); continue; }
                        Log($"Tavily 閲嶈瘯澶辫触 (HTTP {(int)resp2.StatusCode})");
                        continue;
                    }

                    // 閲嶈瘯鎴愬姛锛岀洿鎺ユ牸寮忓寲杩斿洖
                    var retryResult = FormatTavilyResults(raw, query);
                    Log($"Tavily [{idx + 1}] 鎼滅储鎴愬姛锛堥噸璇曪級");
                    return retryResult;
                }

                // Key鏃犳晥
                if ((int)resp.StatusCode == 401)
                {
                    Log($"Tavily 璐﹀彿 {idx + 1} Key鏃犳晥 (401)锛屽垏鎹笅涓€涓?);
                    lock (_lock) _exhaustedTavily.Add(idx);
                    continue;
                }

                // 鏈嶅姟绔敊璇?
                if ((int)resp.StatusCode >= 500)
                {
                    Log($"Tavily 鏈嶅姟绔敊璇?(HTTP {(int)resp.StatusCode})");
                    continue;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    Log($"Tavily 璇锋眰澶辫触 (HTTP {(int)resp.StatusCode}): {raw[..Math.Min(200, raw.Length)]}");
                    continue;
                }

                // 鎴愬姛
                var formatted = FormatTavilyResults(raw, query);
                Log($"Tavily [{idx + 1}] 鎼滅储鎴愬姛");
                return formatted;
            }
            catch (TaskCanceledException) { Log($"Tavily 璐﹀彿 {idx + 1} 瓒呮椂"); continue; }
            catch (Exception ex) { Log($"Tavily 璐﹀彿 {idx + 1} 寮傚父: {ex.Message}"); continue; }
        }

        return null;
    }

    static string FormatTavilyResults(string rawJson, string query)
    {
        try
        {
            var node = JsonNode.Parse(rawJson);
            var sb = new StringBuilder();

            var answer = node?["answer"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(answer))
            {
                sb.AppendLine("## 鎼滅储鎽樿");
                sb.AppendLine(answer);
                sb.AppendLine();
            }

            var results = node?["results"]?.AsArray();
            if (results != null && results.Count > 0)
            {
                sb.AppendLine($"## 鎼滅储缁撴灉锛堝叡 {results.Count} 鏉★級");
                sb.AppendLine();
                int n = 1;
                foreach (var r in results)
                {
                    var title = r?["title"]?.GetValue<string>() ?? "";
                    var url = r?["url"]?.GetValue<string>() ?? "";
                    var content = r?["content"]?.GetValue<string>() ?? "";
                    var score = r?["score"]?.GetValue<float>() ?? 0;
                    sb.AppendLine($"### {n}. {title}");
                    sb.AppendLine($"閾炬帴: {url}");
                    sb.AppendLine($"鐩稿叧搴? {score:F2}");
                    sb.AppendLine(content);
                    sb.AppendLine();
                    n++;
                }
            }
            else sb.AppendLine("鏈壘鍒扮浉鍏虫悳绱㈢粨鏋?);

            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            Log($"Tavily 鏍煎紡鍖栧紓甯? {ex.Message}");
            return $"鎼滅储瀹屾垚浣嗙粨鏋滆В鏋愬け璐?\n{rawJson}";
        }
    }

    #endregion

    #region 鐧惧害鎼滅储

    async Task<string?> TryBaiduSearch(string query, string depth, string? timeRange,
        int results, bool? includeImages, bool? includeVideos, List<string> keys)
    {
        // 鐧惧害query闄愬埗72瀛楃锛堟眽瀛楃畻2瀛楃锛?
        var truncatedQuery = TruncateForBaidu(query);

        // 鏋勫缓璇锋眰浣?
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

        // 娣卞害鏄犲皠: basic鈫抣ite(蹇€?, advanced鈫抯tandard(瀹屾暣)
        body["edition"] = depth == "advanced" ? "standard" : "lite";

        // 鏃堕棿鑼冨洿鏄犲皠: day鈫抴eek(鐧惧害鏈€灏弚eek), week鈫抴eek, month鈫抦onth, year鈫抷ear
        if (!string.IsNullOrWhiteSpace(timeRange))
        {
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

        var bodyJson = body.ToJsonString();

        for (int attempt = 0; attempt < keys.Count; attempt++)
        {
            var (idx, key) = GetNextKey(keys, _exhaustedBaidu);
            if (key == null) break;

            try
            {
                Log($"鐧惧害 [{idx + 1}] edition={body["edition"]} results={results}");
                using var req = new HttpRequestMessage(HttpMethod.Post, BaiduUrl);
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                req.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");

                using var resp = await _http.SendAsync(req);
                var raw = await resp.Content.ReadAsStringAsync();

                // 鐧惧害璁よ瘉閿欒: code=216003
                if ((int)resp.StatusCode == 401 || (int)resp.StatusCode == 403)
                {
                    Log($"鐧惧害 璐﹀彿 {idx + 1} 璁よ瘉澶辫触 (HTTP {(int)resp.StatusCode})锛屽垏鎹笅涓€涓?);
                    lock (_lock) _exhaustedBaidu.Add(idx);
                    continue;
                }

                // 棰戠巼闄愬埗
                if ((int)resp.StatusCode == 429)
                {
                    Log($"鐧惧害 璐﹀彿 {idx + 1} 棰戠巼闄愬埗锛岀瓑寰?绉掗噸璇?);
                    await Task.Delay(2000);
                    using var req2 = new HttpRequestMessage(HttpMethod.Post, BaiduUrl);
                    req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                    req2.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                    using var resp2 = await _http.SendAsync(req2);
                    raw = await resp2.Content.ReadAsStringAsync();

                    if (!resp2.IsSuccessStatusCode)
                    {
                        if ((int)resp2.StatusCode == 401 || (int)resp2.StatusCode == 403)
                        { lock (_lock) _exhaustedBaidu.Add(idx); continue; }
                        Log($"鐧惧害 閲嶈瘯澶辫触 (HTTP {(int)resp2.StatusCode})");
                        continue;
                    }

                    // 閲嶈瘯鎴愬姛锛岀洿鎺ユ牸寮忓寲杩斿洖
                    var retryResult = FormatBaiduResults(raw, query);
                    Log($"鐧惧害 [{idx + 1}] 鎼滅储鎴愬姛锛堥噸璇曪級");
                    return retryResult;
                }

                // 妫€鏌ュ搷搴斾綋涓殑閿欒鐮侊紙棰濆害鑰楀敖绛夛級
                if (!resp.IsSuccessStatusCode)
                {
                    // 灏濊瘯瑙ｆ瀽閿欒鐮?
                    var errNode = JsonNode.Parse(raw);
                    var errCode = errNode?["code"]?.GetValue<long>();
                    var errMsg = errNode?["message"]?.GetValue<string>() ?? "";

                    // 216003=璁よ瘉閿欒, 鍏朵粬quota鐩稿叧閿欒鐮佷篃瑙嗕负璐﹀彿鑰楀敖
                    if (errCode == 216003 || errMsg.Contains("quota", StringComparison.OrdinalIgnoreCase)
                        || errMsg.Contains("limit", StringComparison.OrdinalIgnoreCase))
                    {
                        Log($"鐧惧害 璐﹀彿 {idx + 1} 棰濆害/璁よ瘉寮傚父 (code={errCode})锛屽垏鎹笅涓€涓?);
                        lock (_lock) _exhaustedBaidu.Add(idx);
                        continue;
                    }

                    Log($"鐧惧害 璇锋眰澶辫触 (HTTP {(int)resp.StatusCode}): {errMsg}");
                    continue;
                }

                // 鎴愬姛
                var formatted = FormatBaiduResults(raw, query);
                Log($"鐧惧害 [{idx + 1}] 鎼滅储鎴愬姛");
                return formatted;
            }
            catch (TaskCanceledException) { Log($"鐧惧害 璐﹀彿 {idx + 1} 瓒呮椂"); continue; }
            catch (Exception ex) { Log($"鐧惧害 璐﹀彿 {idx + 1} 寮傚父: {ex.Message}"); continue; }
        }

        return null;
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
                sb.AppendLine("鏈壘鍒扮浉鍏虫悳绱㈢粨鏋?);
                return sb.ToString().Trim();
            }

            // 鍒嗙缃戦〉缁撴灉鍜屽濯掍綋缁撴灉
            var webResults = new List<JsonNode?>();
            var imageResults = new List<JsonNode?>();
            var videoResults = new List<JsonNode?>();

            foreach (var r in refs)
            {
                var type = r?["type"]?.GetValue<string>() ?? "web";
                switch (type)
                {
                    case "image": imageResults.Add(r); break;
                    case "video": videoResults.Add(r); break;
                    default: webResults.Add(r); break;
                }
            }

            // 鏍煎紡鍖栫綉椤电粨鏋?
            if (webResults.Count > 0)
            {
                sb.AppendLine($"## 鎼滅储缁撴灉锛堝叡 {webResults.Count} 鏉★級");
                sb.AppendLine();
                int n = 1;
                foreach (var r in webResults)
                {
                    var title = r?["title"]?.GetValue<string>() ?? "";
                    var url = r?["url"]?.GetValue<string>() ?? "";
                    var content = r?["content"]?.GetValue<string>() ?? "";
                    var date = r?["date"]?.GetValue<string>() ?? "";
                    var score = r?["rerank_score"]?.GetValue<float>() ?? 0;
                    var authority = r?["authority_score"]?.GetValue<float>() ?? -1;

                    sb.AppendLine($"### {n}. {title}");
                    if (!string.IsNullOrWhiteSpace(date))
                        sb.AppendLine($"鍙戝竷鏃堕棿: {date}");
                    sb.AppendLine($"閾炬帴: {url}");
                    sb.AppendLine($"鐩稿叧搴? {score:F2}");
                    if (authority >= 0)
                        sb.AppendLine($"鏉冨▉鎬? {authority:F2}");
                    sb.AppendLine(content);
                    sb.AppendLine();
                    n++;
                }
            }

            // 鏍煎紡鍖栧浘鐗囩粨鏋?
            if (imageResults.Count > 0)
            {
                sb.AppendLine($"## 鍥剧墖缁撴灉锛堝叡 {imageResults.Count} 寮狅級");
                int n = 1;
                foreach (var r in imageResults)
                {
                    var img = r?["image"];
                    var imgUrl = img?["url"]?.GetValue<string>() ?? "";
                    var w = img?["width"]?.GetValue<string>() ?? "";
                    var h = img?["height"]?.GetValue<string>() ?? "";
                    sb.AppendLine($"{n}. [鍥剧墖]({imgUrl}) {w}x{h}");
                    n++;
                }
                sb.AppendLine();
            }

            // 鏍煎紡鍖栬棰戠粨鏋?
            if (videoResults.Count > 0)
            {
                sb.AppendLine($"## 瑙嗛缁撴灉锛堝叡 {videoResults.Count} 涓級");
                int n = 1;
                foreach (var r in videoResults)
                {
                    var vid = r?["video"];
                    var vidUrl = vid?["url"]?.GetValue<string>() ?? "";
                    var duration = vid?["duration"]?.GetValue<string>() ?? "";
                    var title = r?["title"]?.GetValue<string>() ?? "";
                    sb.AppendLine($"{n}. [{title}]({vidUrl}) 鏃堕暱:{duration}绉?);
                    n++;
                }
                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            Log($"鐧惧害 鏍煎紡鍖栧紓甯? {ex.Message}");
            return $"鎼滅储瀹屾垚浣嗙粨鏋滆В鏋愬け璐?\n{rawJson}";
        }
    }

    /// <summary>
    /// 鐧惧害query鎴柇锛?2瀛楃闄愬埗锛堟眽瀛楃畻2瀛楃锛?
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
            Log($"鐧惧害query鎴柇: {query.Length} 鈫?{result.Length} 瀛楃");
        return result;
    }

    #endregion

    #region 杈呭姪鏂规硶

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

    #endregion
}