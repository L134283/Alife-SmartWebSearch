using System;
using Alife.Framework;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using AntDesign;

namespace Alife.Plugin.SmartWebSearch;

public partial class SmartWebSearchUI : ModuleUIBase<SmartWebSearch, SmartWebSearchConfig>
{
    protected override void BuildRenderTree(RenderTreeBuilder b)
    {
        if (Configuration == null)
        {
            b.AddContent(0, "Configuration NULL");
            return;
        }

        int i = 0;

        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "style",
            "background:#fafafa;padding:24px;border-radius:12px;border:1px solid #f0f0f0;max-width:680px;");

        // 标题
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "style", "font-size:18px;font-weight:bold;margin-bottom:4px;");
        b.AddContent(i++, "🔍 网络智能搜索");
        b.CloseElement();

        // 说明
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "style",
            "font-size:12px;color:#555;background:#e6f7ff;padding:10px 12px;border-radius:8px;margin-bottom:16px;line-height:1.7;white-space:pre-line;");
        b.AddContent(i++, "📌 多功能AI搜索：AI总结搜索(高性能版) + 智能搜索生成(标准版) + 双引擎搜索 + 百度热搜 + 智能识图\n📌 百度渠道优先级：AI总结搜索 > 智能搜索生成 > 普通搜索\n📌 每个引擎支持最多 4 组 API Key，额度耗尽自动轮换\n📌 修改配置后需重新加载模块（设置→插件→刷新）");
        b.CloseElement();

        // === 引擎选择 ===
        SectionTitle(b, ref i, "⚙️ 引擎模式");
        AddEngineSelect(b, ref i, "搜索引擎模式", Configuration.Engine, v => Configuration.Engine = v);
        AddHint(b, ref i, "auto：智能路由（中文→百度，英文→Tavily，配额耗尽自动切换）\ntavily：仅用 Tavily\nbaidu：仅用百度");

        // === Tavily API Key 配置 ===
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "style", "margin-top:20px;");
        SectionTitle(b, ref i, "🔑 Tavily API Key");
        AddHint(b, ref i, "Tavily API Key 格式为 tvly-xxxxx，在 https://app.tavily.com 免费注册获取（每月1000次额度）\n认证方式：Bearer Token，和百度一致");

        AddPassword(b, ref i, "Tavily Key 1（主账号）", Configuration.TavilyApiKey1, v => Configuration.TavilyApiKey1 = v);
        AddHint(b, ref i, "默认使用此账号，额度耗尽后自动切换到下一个");

        AddCollapsibleGroup(b, ref i, "Tavily Key 2（备用）", () =>
            AddPassword(b, ref i, "Tavily Key 2", Configuration.TavilyApiKey2 ?? "", v => Configuration.TavilyApiKey2 = v));
        AddCollapsibleGroup(b, ref i, "Tavily Key 3（备用）", () =>
            AddPassword(b, ref i, "Tavily Key 3", Configuration.TavilyApiKey3 ?? "", v => Configuration.TavilyApiKey3 = v));
        AddCollapsibleGroup(b, ref i, "Tavily Key 4（备用）", () =>
            AddPassword(b, ref i, "Tavily Key 4", Configuration.TavilyApiKey4 ?? "", v => Configuration.TavilyApiKey4 = v));
        b.CloseElement();

        // === 百度 API Key 配置 ===
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "style", "margin-top:20px;");
        SectionTitle(b, ref i, "🔑 百度千帆 API Key");
        AddHint(b, ref i, "百度千帆 API Key 创建地址：https://console.bce.baidu.com/qianfan/ais/console/apiKey\n注意可以不开后付费、不实名、注册就能够白嫖额度\n⚠️ 百度的后付费最好别开，否则可能导致欠款等问题\n免费额度：AI总结搜索/智能搜索生成 100次/天，普通搜索 50次/天，识图 100次/天，热搜 10次/天\n认证方式：Bearer Token");

        AddPassword(b, ref i, "百度 Key 1（主账号）", Configuration.BaiduApiKey1, v => Configuration.BaiduApiKey1 = v);
        AddHint(b, ref i, "默认使用此账号，额度耗尽后自动切换到下一个");

        AddCollapsibleGroup(b, ref i, "百度 Key 2（备用）", () =>
            AddPassword(b, ref i, "百度 Key 2", Configuration.BaiduApiKey2 ?? "", v => Configuration.BaiduApiKey2 = v));
        AddCollapsibleGroup(b, ref i, "百度 Key 3（备用）", () =>
            AddPassword(b, ref i, "百度 Key 3", Configuration.BaiduApiKey3 ?? "", v => Configuration.BaiduApiKey3 = v));
        AddCollapsibleGroup(b, ref i, "百度 Key 4（备用）", () =>
            AddPassword(b, ref i, "百度 Key 4", Configuration.BaiduApiKey4 ?? "", v => Configuration.BaiduApiKey4 = v));
        b.CloseElement();

        // === 搜索设置 ===
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "style", "margin-top:20px;");
        SectionTitle(b, ref i, "📋 搜索设置");

        AddInput(b, ref i, "默认返回结果数量", Configuration.MaxResults.ToString(), v =>
        {
            if (int.TryParse(v, out var n))
                Configuration.MaxResults = Math.Clamp(n, 1, 20);
        });
        AddHint(b, ref i, "每次搜索返回的网页结果条数，默认 5，范围 1~20。AI 调用时也可动态指定");

        AddDepthSelect(b, ref i, "默认搜索深度", Configuration.SearchDepth, v => Configuration.SearchDepth = v);
        AddHint(b, ref i, "Tavily: basic=1额度/次, advanced=2额度/次\n百度: basic→lite(快速版), advanced→standard(完整版)");
        b.CloseElement();

        // === AI搜索设置 ===
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "style", "margin-top:20px;");
        SectionTitle(b, ref i, "🤖 AI搜索设置");
        AddHint(b, ref i, "AI总结搜索(高性能版)和智能搜索生成(标准版)的模型配置\n高性能版：搜索+大模型总结一步到位，免费100次/日\n标准版：功能最全面，支持可选深度搜索、知识注入、追问等");

        AddInput(b, ref i, "高性能版模型", Configuration.SummaryModel, v => Configuration.SummaryModel = v);
        AddHint(b, ref i, "auto_thinking(自动思考，推荐) / thinking / non_thinking");

        AddInput(b, ref i, "智能搜索生成模型(标准版)", Configuration.ChatSearchModel, v => Configuration.ChatSearchModel = v);
        AddHint(b, ref i, "deepseek-v3.2(推荐) / deepseek-r1 / ernie-4.5-turbo-32k 等");

        AddToggle(b, ref i, "启用深度搜索（智能搜索生成）", Configuration.EnableDeepSearch, v => Configuration.EnableDeepSearch = v);
        AddHint(b, ref i, "启用后智能搜索生成会更精准但更慢\n⚠️ 每次深度搜索会花费较多额度，请谨慎使用");
        b.CloseElement();

        // === 缓存设置 ===
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "style", "margin-top:20px;");
        SectionTitle(b, ref i, "💾 缓存设置");

        AddToggle(b, ref i, "启用搜索缓存", Configuration.EnableCache, v => Configuration.EnableCache = v);
        AddHint(b, ref i, "启用后，相同查询在缓存有效期内不重复调用API，节省额度");

        AddInput(b, ref i, "缓存过期时间（分钟）", Configuration.CacheTtlMinutes.ToString(), v =>
        {
            if (int.TryParse(v, out var n))
                Configuration.CacheTtlMinutes = Math.Clamp(n, 1, 60);
        });
        AddHint(b, ref i, "缓存结果的保留时间，默认 5 分钟");
        b.CloseElement();

        b.CloseElement(); // root div
    }

    void AddEngineSelect(RenderTreeBuilder b, ref int seq, string label, string value, Action<string> setter)
    {
        AddLabel(b, ref seq, label);
        b.OpenElement(seq++, "select");
        b.AddAttribute(seq++, "style",
            "width:100%;padding:6px 10px;border:1px solid #d9d9d9;border-radius:6px;" +
            "font-size:13px;background:#fff;font-family:inherit;color:#333;");
        b.AddAttribute(seq++, "value", value);
        b.AddAttribute(seq++, "onchange",
            EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                setter(e.Value?.ToString() ?? "auto")));

        var options = new[] {
            ("auto", "auto - 智能路由（中文→百度，英文→Tavily）"),
            ("tavily", "tavily - 仅 Tavily"),
            ("baidu", "baidu - 仅百度"),
        };
        foreach (var (val, text) in options)
        {
            b.OpenElement(seq++, "option");
            b.AddAttribute(seq++, "value", val);
            if (val == value) b.AddAttribute(seq++, "selected", true);
            b.AddContent(seq++, text);
            b.CloseElement();
        }
        b.CloseElement();
    }

    void AddToggle(RenderTreeBuilder b, ref int seq, string label, bool value, Action<bool> setter)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "style", "display:flex;align-items:center;gap:8px;margin-bottom:6px;");
        b.OpenElement(seq++, "input");
        b.AddAttribute(seq++, "type", "checkbox");
        b.AddAttribute(seq++, "checked", value);
        b.AddAttribute(seq++, "style", "width:16px;height:16px;cursor:pointer;");
        b.AddAttribute(seq++, "onchange",
            EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                setter(e.Value is true)));
        b.CloseElement();
        b.OpenElement(seq++, "span");
        b.AddAttribute(seq++, "style", "font-size:13px;font-weight:bold;color:#444;");
        b.AddContent(seq++, label);
        b.CloseElement();
        b.CloseElement();
    }

    void AddCollapsibleGroup(RenderTreeBuilder b, ref int seq, string title, Action renderContent)
    {
        b.OpenElement(seq++, "details");
        b.AddAttribute(seq++, "style",
            "margin:4px 0;border:1px solid #e8e8e8;border-radius:6px;padding:6px 10px;background:#fff;");
        b.OpenElement(seq++, "summary");
        b.AddAttribute(seq++, "style",
            "cursor:pointer;font-weight:bold;font-size:13px;color:#666;padding:2px 0;user-select:none;");
        b.AddContent(seq++, $"⬇️ {title}");
        b.CloseElement();
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "style", "padding:8px 0 4px;");
        renderContent();
        b.CloseElement();
        b.CloseElement();
    }

    void AddDepthSelect(RenderTreeBuilder b, ref int seq, string label, string value, Action<string> setter)
    {
        AddLabel(b, ref seq, label);
        b.OpenElement(seq++, "select");
        b.AddAttribute(seq++, "style",
            "width:100%;padding:6px 10px;border:1px solid #d9d9d9;border-radius:6px;" +
            "font-size:13px;background:#fff;font-family:inherit;color:#333;");
        b.AddAttribute(seq++, "value", value);
        b.AddAttribute(seq++, "onchange",
            EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                setter(e.Value?.ToString() ?? "basic")));
        var options = new[] {
            ("basic", "basic - 快速搜索（Tavily 1额度 / 百度 lite版）"),
            ("advanced", "advanced - 深度搜索（Tavily 2额度 / 百度 standard版）"),
        };
        foreach (var (val, text) in options)
        {
            b.OpenElement(seq++, "option");
            b.AddAttribute(seq++, "value", val);
            if (val == value) b.AddAttribute(seq++, "selected", true);
            b.AddContent(seq++, text);
            b.CloseElement();
        }
        b.CloseElement();
    }

    void SectionTitle(RenderTreeBuilder b, ref int seq, string text)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "style",
            "font-size:14px;font-weight:bold;color:#555;margin:0 0 8px;" +
            "border-bottom:1px solid #e0e0e0;padding-bottom:4px;");
        b.AddContent(seq++, text);
        b.CloseElement();
    }

    void AddHint(RenderTreeBuilder b, ref int seq, string text)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "style",
            "font-size:11px;color:#999;margin:0 0 10px 2px;line-height:1.5;white-space:pre-line;");
        b.AddContent(seq++, text);
        b.CloseElement();
    }

    void AddLabel(RenderTreeBuilder b, ref int seq, string text)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "style", "font-weight:bold;margin-bottom:3px;font-size:13px;color:#444;");
        b.AddContent(seq++, text);
        b.CloseElement();
    }

    void AddInput(RenderTreeBuilder b, ref int seq, string label, string value, Action<string> setter)
    {
        AddLabel(b, ref seq, label);
        b.OpenComponent<Input<string>>(seq++);
        b.AddAttribute(seq++, "Value", value);
        b.AddAttribute(seq++, "ValueChanged",
            EventCallback.Factory.Create<string>(this, setter));
        b.CloseComponent();
    }

    void AddPassword(RenderTreeBuilder b, ref int seq, string label, string value, Action<string> setter)
    {
        AddLabel(b, ref seq, label);
        b.OpenComponent<InputPassword>(seq++);
        b.AddAttribute(seq++, "Value", value);
        b.AddAttribute(seq++, "ValueChanged",
            EventCallback.Factory.Create<string>(this, setter));
        b.CloseComponent();
    }
}