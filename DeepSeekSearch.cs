using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Alife.Function.FunctionCaller;

namespace Alife.Plugin.SmartWebSearch;

public partial class SmartWebSearch
{
    const string DeepSeekSearchUrl = "https://api.deepseek.com/v1/tools/search";
    const int DeepSeekMaxQueryLength = 128;
    const int DeepSeekMaxContentLength = 6000;

    [XmlFunction(FunctionMode.OneShot)]
    [Description("DeepSeek 专用网页搜索，返回标题、链接、长摘要与相关性评分。支持 site:、-site:、引号、lang:、after:、before:；关键词最多 128 个字符。不是聊天接口。")]
    public async Task DeepSeekSearch(
        [Description("搜索关键词，1～128 个 Unicode 字符，保留精确短语及站点等搜索语法")] string query,
        [Description("返回条数，1～50；默认使用插件设置")] int? maxResults = null,
        [Description("可选：触发搜索的原始问题，最多 128 个字符，有助于理解搜索意图")] string? originalQuestion = null,
        [Description("是否同时读取网页正文；默认使用插件设置，关闭时仍有长摘要")] bool? includeContent = null)
    {
        SmartWebSearchConfig cfg = Configuration ?? new SmartWebSearchConfig();
        if (cfg.IsEngineEnabled("deepseek") == false)
        {
            interactor.Poke("DeepSeek 搜索已关闭，请先在插件设置中勾选 DeepSeek 引擎。");
            return;
        }

        (string? result, string? error) = await CallDeepSeekSearchAsync(
            query, maxResults ?? cfg.MaxResults, originalQuestion,
            includeContent ?? cfg.DeepSeekIncludeContent, cfg);
        interactor.Poke(result ?? error ?? "DeepSeek 搜索未完成，请稍后重试。");
    }

    async Task<string?> TryDeepSeekSearch(string query, int results, SmartWebSearchConfig cfg)
    {
        (string? result, string? error) = await CallDeepSeekSearchAsync(
            query, results, null, cfg.DeepSeekIncludeContent, cfg);
        if (error != null)
            Log(error);
        return result;
    }

    async Task<(string? result, string? error)> CallDeepSeekSearchAsync(
        string query, int results, string? originalQuestion, bool includeContent,
        SmartWebSearchConfig cfg)
    {
        DestroyCancellationToken.ThrowIfCancellationRequested();
        string? validationError = ValidateDeepSeekText(query, "搜索关键词");
        if (validationError != null)
            return (null, validationError);

        if (string.IsNullOrWhiteSpace(originalQuestion) == false)
        {
            validationError = ValidateDeepSeekText(originalQuestion, "原始问题");
            if (validationError != null)
                return (null, validationError);
        }

        string apiKey = (cfg.DeepSeekApiKey ?? "").Trim();
        if (apiKey.Length == 0)
            return (null, "DeepSeek 搜索需要 API Key，请在插件设置中填写 DeepSeek 平台的 API Key。");

        JsonObject body = BuildDeepSeekSearchBody(query, results, originalQuestion, includeContent);
        string bodyJson = body.ToJsonString();
        string keyScope = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));
        string cacheKey = $"deepseek:v1:{keyScope}:{bodyJson}";
        if (TryGetCached(cfg, cacheKey, out string cachedResult))
            return (cachedResult, null);

        try
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                using HttpRequestMessage request = new(HttpMethod.Post, DeepSeekSearchUrl);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = new StringContent(bodyJson, Encoding.UTF8, "application/json");
                using HttpResponseMessage response = await _http.SendAsync(request, DestroyCancellationToken);

                if (response.IsSuccessStatusCode == false)
                {
                    int status = (int)response.StatusCode;
                    bool transient = response.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
                        || status >= 500;
                    if (attempt == 0 && transient)
                    {
                        await Task.Delay(1000, DestroyCancellationToken);
                        continue;
                    }
                    return (null, DeepSeekHttpError(response.StatusCode));
                }

                string rawJson = await response.Content.ReadAsStringAsync(DestroyCancellationToken);
                string formatted = FormatDeepSeekResults(rawJson, query.Trim(), includeContent);
                SaveCache(cfg, cacheKey, formatted);
                return (formatted, null);
            }
        }
        catch (OperationCanceledException) when (DestroyCancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return (null, "DeepSeek 搜索超时，请稍后重试；需要正文时也可以先关闭「读取网页正文」。");
        }
        catch (HttpRequestException)
        {
            return (null, "DeepSeek 搜索连接失败，请检查网络后重试。");
        }
        catch (JsonException)
        {
            return (null, "DeepSeek 返回的搜索数据格式异常，未作为搜索结果使用，请稍后重试。");
        }
        catch (FormatException)
        {
            return (null, "DeepSeek API Key 格式不正确，请重新粘贴完整密钥。");
        }

        return (null, "DeepSeek 搜索暂时不可用，请稍后重试。");
    }

    static string? ValidateDeepSeekText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return $"{fieldName}不能为空。";

        int length = 0;
        foreach (Rune character in value.Trim().EnumerateRunes())
        {
            if (++length > DeepSeekMaxQueryLength)
                return $"{fieldName}超过 DeepSeek 的 128 字符限制，请精简后重试（不会自动截断关键词）。";
        }
        return null;
    }

    static JsonObject BuildDeepSeekSearchBody(string query, int results, string? originalQuestion, bool includeContent)
    {
        JsonObject body = new()
        {
            ["query"] = query.Trim(),
            ["count"] = Math.Clamp(results, 1, 50),
            ["need_content"] = includeContent,
        };
        if (string.IsNullOrWhiteSpace(originalQuestion) == false)
            body["ori_question"] = originalQuestion.Trim();
        return body;
    }

    static string DeepSeekHttpError(HttpStatusCode status) => status switch
    {
        HttpStatusCode.BadRequest => "DeepSeek 搜索参数无效，请检查关键词和原始问题是否在 1～128 字符内。",
        HttpStatusCode.Unauthorized => "DeepSeek API Key 缺失或无效，请在插件设置中检查密钥。",
        HttpStatusCode.PaymentRequired => "DeepSeek 账号余额不足，请先在 DeepSeek 平台充值后重试。",
        HttpStatusCode.Forbidden => "DeepSeek 搜索请求被拒绝，请检查账号权限或稍后重试。",
        HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => "DeepSeek 搜索服务超时，请稍后重试。",
        HttpStatusCode.RequestEntityTooLarge => "DeepSeek 搜索请求过大，请缩短关键词或原始问题。",
        HttpStatusCode.TooManyRequests => "DeepSeek 搜索过于频繁，请稍后再试。",
        _ => $"DeepSeek 搜索暂时不可用（HTTP {(int)status}），请稍后重试。",
    };

    static string FormatDeepSeekResults(string rawJson, string query, bool includeContent)
    {
        using JsonDocument document = JsonDocument.Parse(rawJson);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object
            || root.TryGetProperty("results", out JsonElement results) == false
            || results.ValueKind != JsonValueKind.Array)
            throw new JsonException("DeepSeek search response must contain a results array.");

        List<string> entries = new();
        foreach (JsonElement item in results.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
                continue;
            string title = ReadDeepSeekText(item, "title");
            string url = ReadDeepSeekText(item, "url");
            if (string.IsNullOrWhiteSpace(title) || Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) == false
                || uri.Scheme is not ("http" or "https"))
                continue;

            StringBuilder entry = new();
            entry.AppendLine($"### {entries.Count + 1}. {title.Replace('\r', ' ').Replace('\n', ' ').Trim()}");
            entry.AppendLine($"链接：{url}");
            string site = ReadDeepSeekText(item, "site_name");
            if (site.Length > 0)
                entry.AppendLine($"站点：{site}");
            string date = ReadDeepSeekText(item, "date");
            if (DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTimeOffset published))
                entry.AppendLine($"发布时间：{published.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture)}");
            if (item.TryGetProperty("rerank_score", out JsonElement scoreValue)
                && scoreValue.ValueKind == JsonValueKind.Number
                && scoreValue.TryGetDouble(out double score) && score is >= 0 and <= 3)
                entry.AppendLine($"综合评分：{score.ToString("0.00", CultureInfo.InvariantCulture)}/3（≥1.5 为优质结果）");
            if (item.TryGetProperty("authority", out JsonElement authorityValue)
                && authorityValue.ValueKind == JsonValueKind.Number
                && authorityValue.TryGetInt32(out int authority) && authority is >= 0 and <= 4)
                entry.AppendLine($"权威性：{authority}/4");

            string summary = ReadDeepSeekText(item, "summary");
            if (string.IsNullOrWhiteSpace(summary))
                summary = ReadDeepSeekText(item, "snippet");
            if (summary.Length > 0)
                entry.AppendLine($"摘要：{summary}");

            string content = includeContent ? ReadDeepSeekText(item, "content") : "";
            if (content.Length > 0)
            {
                entry.AppendLine("网页正文：");
                if (content.Length <= DeepSeekMaxContentLength)
                    entry.AppendLine(content);
                else
                {
                    int length = char.IsHighSurrogate(content[DeepSeekMaxContentLength - 1])
                        ? DeepSeekMaxContentLength - 1 : DeepSeekMaxContentLength;
                    entry.AppendLine(content[..length]);
                    entry.AppendLine("（正文过长，仅展示前 6000 字符；其余内容未读取给模型。）");
                }
            }
            entries.Add(entry.ToString().TrimEnd());
        }

        if (results.GetArrayLength() > 0 && entries.Count == 0)
            throw new JsonException("DeepSeek search results have no usable title and web URL.");

        StringBuilder output = new();
        output.AppendLine($"## DeepSeek 搜索结果（共 {entries.Count} 条）");
        output.AppendLine($"查询：{query}");
        if (entries.Count == 0)
            output.AppendLine("没有找到匹配网页。可调整关键词、放宽站点或日期限制后重试，不代表搜索接口出错。");
        else
            output.AppendLine(string.Join("\n\n", entries));
        return output.ToString().Trim();
    }

    static string ReadDeepSeekText(JsonElement item, string propertyName) =>
        item.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? "" : "";
}
