using System;
using System.Collections.Generic;
using System.Globalization;
using Alife.Framework;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using AntDesign;

namespace Alife.Plugin.SmartWebSearch;

public partial class SmartWebSearchUI : ModuleUIBase<SmartWebSearch, SmartWebSearchConfig>
{
    const string Css = """
.sws-workspace {
    width: 100%; min-width: 0; color: var(--alife-text);
    font-family: 'Segoe UI', 'PingFang SC', 'Microsoft YaHei', sans-serif;
    font-size: 13px; line-height: 1.65; container-type: inline-size; overflow-wrap: anywhere;
}
.sws-workspace, .sws-workspace * { box-sizing: border-box; }
.sws-workspace h2, .sws-workspace h3, .sws-workspace p { margin: 0; }
.sws-hero {
    display: flex; align-items: flex-start; justify-content: space-between;
    flex-wrap: wrap; gap: 14px; padding: 22px; margin-bottom: 14px;
    background: linear-gradient(125deg, var(--alife-primary-bg), var(--alife-bg-container));
    border: 1px solid var(--alife-primary-border); border-radius: var(--alife-radius-lg);
}
.sws-hero-heading { display: flex; align-items: center; gap: 12px; min-width: 0; }
.sws-hero-heading > div { min-width: 0; }
.sws-hero-icon {
    display: grid; place-items: center; flex: 0 0 44px; width: 44px; height: 44px;
    border-radius: var(--alife-radius); background: var(--alife-primary-bg);
    color: var(--alife-primary); font-size: 25px; font-weight: 600;
}
.sws-workspace .sws-title { font-size: 21px; font-weight: 650; line-height: 1.35; letter-spacing: .2px; }
.sws-subtitle { margin-top: 4px !important; color: var(--alife-text-secondary); }
.sws-scope {
    flex-shrink: 0; display: inline-flex; align-items: center; gap: 6px; max-width: 100%;
    padding: 4px 10px; border: 1px solid var(--alife-border); border-radius: 999px;
    background: var(--alife-bg-container); color: var(--alife-text-secondary); font-size: 12px;
}
.sws-steps {
    display: grid; grid-template-columns: repeat(3, minmax(0, 1fr));
    gap: 10px; margin-bottom: 14px;
}
.sws-step {
    display: flex; align-items: flex-start; gap: 10px; padding: 12px 14px;
    border: 1px solid var(--alife-border); border-radius: var(--alife-radius);
    background: var(--alife-bg-container); min-width: 0;
}
.sws-step-number {
    display: grid; place-items: center; width: 24px; height: 24px; flex: 0 0 24px;
    background: var(--alife-primary-bg); color: var(--alife-primary);
    border-radius: 50%; font-weight: 650; font-variant-numeric: tabular-nums;
}
.sws-step strong { display: block; font-size: 13px; font-weight: 600; }
.sws-step small { display: block; color: var(--alife-text-secondary); font-size: 12px; }
.sws-section {
    margin-top: 14px; padding: 18px; border: 1px solid var(--alife-border);
    border-radius: var(--alife-radius-lg); background: var(--alife-bg-container);
    box-shadow: var(--alife-shadow-card); min-width: 0;
}
.sws-section-heading { display: flex; align-items: flex-start; justify-content: space-between; flex-wrap: wrap; gap: 10px; margin-bottom: 14px; }
.sws-workspace .sws-section-title { font-size: 15px; font-weight: 650; line-height: 1.5; }
.sws-section-caption { color: var(--alife-text-secondary); font-size: 12px; margin-top: 3px !important; }
.sws-pill {
    display: inline-flex; align-items: center; padding: 3px 9px; border-radius: 999px;
    background: var(--alife-bg-subtle); border: 1px solid var(--alife-border);
    font-size: 11px; font-weight: 500; color: var(--alife-text-secondary); white-space: nowrap;
}
.sws-pill-selected { background: var(--alife-primary-bg); border-color: var(--alife-primary-border); color: var(--alife-primary); }
.sws-pill-warning { background: var(--alife-warning-bg); border-color: var(--alife-warning-border); color: var(--alife-text); }
.sws-engine-grid, .sws-credentials-grid, .sws-fields-grid {
    display: grid; grid-template-columns: repeat(auto-fit, minmax(min(100%, 260px), 1fr)); gap: 12px;
}
.sws-engine-card {
    display: block; min-width: 0; padding: 15px; cursor: pointer;
    background: var(--alife-bg-container); border: 1px solid var(--alife-border);
    border-radius: var(--alife-radius); transition: border-color .16s, background .16s;
}
.sws-engine-card:hover { border-color: var(--alife-primary-border); background: var(--alife-bg-hover); }
.sws-engine-card.sws-engine-selected { border-color: var(--alife-primary); background: var(--alife-primary-bg); }
.sws-engine-card:focus-within { outline: 2px solid var(--alife-primary); outline-offset: 2px; }
.sws-engine-top { display: flex; align-items: center; flex-wrap: wrap; column-gap: 9px; row-gap: 5px; }
.sws-engine-check, .sws-toggle-check { width: 17px; height: 17px; flex: 0 0 17px; accent-color: var(--alife-primary); cursor: pointer; margin: 0; }
.sws-engine-name { font-size: 14px; font-weight: 650; }
.sws-engine-category { margin-left: auto; color: var(--alife-text-secondary); font-size: 11px; }
.sws-engine-description { display: block; margin: 7px 0 10px 26px !important; color: var(--alife-text-secondary); font-size: 12px; }
.sws-engine-state { margin-left: 26px; max-width: calc(100% - 26px); white-space: normal; }
.sws-notice {
    display: flex; align-items: flex-start; gap: 9px; padding: 11px 13px; margin-top: 12px;
    border-radius: var(--alife-radius-sm); border: 1px solid var(--alife-primary-border);
    background: var(--alife-primary-bg); color: var(--alife-text-secondary); font-size: 12px;
}
.sws-notice-warning { border-color: var(--alife-warning-border); background: var(--alife-warning-bg); color: var(--alife-text); }
.sws-notice-symbol { flex: 0 0 auto; font-weight: 650; color: var(--alife-primary); }
.sws-notice-warning .sws-notice-symbol { color: var(--alife-warning); }
.sws-credentials {
    padding: 15px; border-radius: var(--alife-radius); border: 1px solid var(--alife-border);
    background: var(--alife-bg-subtle); min-width: 0;
}
.sws-credentials-title { display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 8px; margin-bottom: 10px; font-weight: 650; }
.sws-link { color: var(--alife-primary); text-decoration: none; font-size: 12px; }
.sws-link:hover { color: var(--alife-primary-hover); text-decoration: underline; }
.sws-link:focus-visible, .sws-control:focus-visible { outline: 2px solid var(--alife-primary); outline-offset: 2px; }
.sws-field { display: block; min-width: 0; margin-bottom: 12px; }
.sws-field-label { display: block; margin-bottom: 5px; font-size: 12px; font-weight: 600; color: var(--alife-text); }
.sws-control {
    display: block; width: 100%; min-width: 0; min-height: 37px; padding: 8px 10px;
    color: var(--alife-text); background: var(--alife-bg-container);
    border: 1px solid var(--alife-border); border-radius: var(--alife-radius-sm);
    font: inherit; font-size: 13px; transition: border-color .16s;
}
.sws-control:hover, .sws-control:focus { border-color: var(--alife-primary); }
.sws-control::placeholder { color: var(--alife-text-tertiary); }
.sws-control[type='password'] { font-family: inherit; }
.sws-control[type='number'] { font-variant-numeric: tabular-nums; }
.sws-field-hint, .sws-helper { display: block; margin-top: 5px !important; color: var(--alife-text-secondary); font-size: 12px; overflow-wrap: anywhere; }
.sws-helper + .sws-helper { margin-top: 8px !important; }
.sws-helper strong { color: var(--alife-text); font-weight: 600; }
.sws-toggle {
    display: flex; align-items: flex-start; gap: 10px; padding: 10px 0; min-width: 0; cursor: pointer;
}
.sws-toggle-check { margin-top: 3px; }
.sws-toggle-title { display: block; color: var(--alife-text); font-weight: 600; font-size: 13px; }
.sws-toggle-description { display: block; color: var(--alife-text-secondary); font-size: 12px; margin-top: 3px; }
.sws-divider { height: 1px; background: var(--alife-border); margin: 14px 0; }
.sws-disclosure { margin-top: 10px; min-width: 0; border: 1px solid var(--alife-border); border-radius: var(--alife-radius); overflow: hidden; }
.sws-disclosure.sws-disclosure-compact { background: var(--alife-bg-container); }
.sws-workspace .sws-disclosure-button.ant-btn {
    display: block !important; width: 100% !important; height: auto !important;
    min-height: 44px; padding: 12px 14px !important; text-align: left;
    white-space: normal !important; border: 0 !important; border-radius: 0 !important;
    color: var(--alife-text) !important; background: var(--alife-bg-subtle) !important;
    box-shadow: none !important;
}
.sws-workspace .sws-disclosure-button.ant-btn:hover { background: var(--alife-bg-hover) !important; }
.sws-workspace .sws-disclosure-button.ant-btn:focus-visible { outline: 2px solid var(--alife-primary); outline-offset: -2px; }
.sws-disclosure-line { display: flex; align-items: center; justify-content: space-between; gap: 10px; width: 100%; }
.sws-disclosure-label { font-weight: 600; font-size: 13px; }
.sws-disclosure-action { flex-shrink: 0; color: var(--alife-primary); font-size: 12px; }
.sws-disclosure-caption { display: block; color: var(--alife-text-secondary); font-size: 12px; margin-top: 3px; line-height: 1.6; }
.sws-disclosure-content { padding: 14px; border-top: 1px solid var(--alife-border); }
.sws-source-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(min(100%, 170px), 1fr)); gap: 9px; margin: 5px 0 12px; }
.sws-source-item { padding: 10px; background: var(--alife-bg-subtle); border: 1px solid var(--alife-border); border-radius: var(--alife-radius-sm); }
.sws-source-item strong { display: block; font-size: 12px; font-weight: 600; }
.sws-source-item span { color: var(--alife-text-secondary); font-size: 12px; }
.sws-footer {
    display: flex; align-items: flex-start; gap: 10px; padding: 14px 4px 2px;
    color: var(--alife-text-secondary); font-size: 12px;
}
.sws-footer strong { color: var(--alife-text); font-weight: 600; }
.sws-security { margin-top: 8px !important; color: var(--alife-text-tertiary); }
@container (max-width: 580px) {
    .sws-hero { padding: 17px; }
    .sws-section { padding: 14px; }
    .sws-workspace .sws-title { font-size: 18px; }
    .sws-steps { grid-template-columns: 1fr; gap: 7px; }
    .sws-step { padding: 9px 12px; }
    .sws-step-number { width: 21px; height: 21px; flex-basis: 21px; font-size: 12px; }
}
@container (max-width: 380px) {
    .sws-engine-category { width: calc(100% - 26px); margin-left: 26px; }
}
@media (prefers-reduced-motion: reduce) {
    .sws-workspace * { transition: none !important; }
}
""";

    readonly Dictionary<string, bool> disclosureStates = new(StringComparer.Ordinal);

    protected override void BuildRenderTree(RenderTreeBuilder b)
    {
        b.OpenElement(0, "style");
        b.AddContent(1, Css);
        b.CloseElement();

        int sequence = 2;
        b.OpenElement(sequence++, "div");
        b.AddAttribute(sequence++, "class", "sws-workspace");
        b.AddAttribute(sequence++, "data-sws-view", "search-workspace");

        RenderHeader(b, ref sequence);
        RenderSteps(b, ref sequence);

        EngineCardInfo[] engines = CreateEngineCards();
        List<string> pendingKeys = new();
        int selectedCount = 0;
        foreach (EngineCardInfo engine in engines)
        {
            if (Configuration.IsEngineEnabled(engine.Id) == false) continue;
            selectedCount++;
            if (engine.Anonymous == false && engine.KeyCount == 0)
                pendingKeys.Add(engine.Name);
        }

        b.OpenElement(sequence++, "section");
        b.AddAttribute(sequence++, "class", "sws-section");
        RenderSectionHeader(b, ref sequence, "1. 选择搜索引擎",
            "可以多选。AI 根据查询语言自动选择，失败时再尝试下一个；不是每次把所有引擎同时搜一遍。",
            $"已选 {selectedCount} 个");
        b.OpenElement(sequence++, "div");
        b.AddAttribute(sequence++, "class", "sws-engine-grid");
        b.AddAttribute(sequence++, "role", "group");
        b.AddAttribute(sequence++, "aria-label", "选择一个或多个网页搜索引擎");
        foreach (EngineCardInfo engine in engines)
            RenderEngineCard(b, ref sequence, engine);
        b.CloseElement();

        if (selectedCount == 0)
        {
            AddNotice(b, ref sequence, "还没有选择网页搜索引擎。勾选上方任意一个即可开始配置；全部不选时，网页搜索不会发送请求。图片搜源仍可单独使用。", true);
        }
        else if (pendingKeys.Count > 0)
        {
            AddNotice(b, ref sequence, $"待填写 API Key：{string.Join("、", pendingKeys)}。这些引擎会暂时跳过；请在下方完成配置。状态只检查是否填写，不代表已通过连通性测试。", true);
        }
        else
        {
            AddNotice(b, ref sequence, "已勾选的引擎已具备调用条件；未勾选的引擎不会被调用，也不会向 AI 注入对应的工具说明。实际可用性以服务响应为准。");
        }
        b.CloseElement();

        b.OpenElement(sequence++, "section");
        b.AddAttribute(sequence++, "class", "sws-section");
        RenderSectionHeader(b, ref sequence, "2. 配置所选引擎",
            "只显示你勾选的渠道。关闭渠道不会删除密钥，重新勾选后仍可继续使用。");
        if (selectedCount == 0)
        {
            AddHelper(b, ref sequence, "先在上方选一个引擎，密钥设置就会出现在这里。");
        }
        else
        {
            b.OpenElement(sequence++, "div");
            b.AddAttribute(sequence++, "class", "sws-credentials-grid");
            foreach (EngineCardInfo engine in engines)
            {
                if (Configuration.IsEngineEnabled(engine.Id) == false) continue;
                b.OpenElement(sequence++, "section");
                b.SetKey(engine.Id);
                b.AddAttribute(sequence++, "class", "sws-credentials");
                b.AddAttribute(sequence++, "data-sws-credentials", engine.Id);
                b.AddContent(sequence++, engine.Credentials);
                b.CloseElement();
            }
            b.CloseElement();
        }
        b.CloseElement();

        b.OpenElement(sequence++, "section");
        b.AddAttribute(sequence++, "class", "sws-section");
        RenderSectionHeader(b, ref sequence, "3. 日常搜索设置", "不确定怎么选？保留默认值就好。");
        b.OpenElement(sequence++, "div");
        b.AddAttribute(sequence++, "class", "sws-fields-grid");
        AddSelectField(b, ref sequence, "返回多少条结果", Configuration.MaxResults.ToString(CultureInfo.InvariantCulture),
            value =>
            {
                if (int.TryParse(value, out int count))
                    Configuration.MaxResults = Math.Clamp(count, 1, 20);
            },
            new[] { ("5", "5 条 · 简洁（推荐）"), ("10", "10 条 · 更全面"), ("20", "20 条 · 更多参考") },
            "各服务可能有自己的条数上限。更多结果会占用更多 AI 上下文。", "max-results");
        if (Configuration.IsEngineEnabled("tavily") || Configuration.IsEngineEnabled("baidu"))
        {
            AddSelectField(b, ref sequence, "搜索深度", Configuration.SearchDepth,
                value => Configuration.SearchDepth = value,
                new[] { ("basic", "快速搜索 · 日常够用（推荐）"), ("advanced", "深度搜索 · 更慢，可能消耗更多额度") },
                "仅影响 Tavily 和百度网页搜索，不影响 DeepSeek。", "search-depth");
        }
        b.CloseElement();
        AddHelper(b, ref sequence, "使用时直接对 AI 说：“帮我搜索最新消息，并附上来源链接。”不需要自己输入工具命令。");
        b.CloseElement();

        b.OpenElement(sequence++, "section");
        b.AddAttribute(sequence++, "class", "sws-section");
        RenderSectionHeader(b, ref sequence, "图片出处搜索", "独立于网页引擎选择，用图片找作品、画师或相关网页。", Configuration.EnableSourceSearch ? "已开启" : "已关闭");
        AddToggle(b, ref sequence, "启用图片出处搜索", Configuration.EnableSourceSearch,
            value => Configuration.EnableSourceSearch = value,
            "关闭后不向 AI 提供图片搜源工具；已填写的密钥仍会保留。", "source-search");
        RenderDisclosure(b, ref sequence, "source-settings", "图片搜源设置与说明",
            "默认可以尝试匿名搜源；需要更多额度时再填写对应密钥。",
            RenderSourceSettings);
        b.CloseElement();

        RenderDisclosure(b, ref sequence, "advanced-settings", "高级选项",
            "缓存、AI 工具说明和百度 AI 搜索模型。默认设置适合大多数用户。",
            RenderAdvancedSettings);

        RenderFooter(b, ref sequence);
        b.CloseElement();
    }

    sealed record EngineCardInfo(
        string Id, string Name, string Category, string Description,
        int KeyCount, bool Anonymous, RenderFragment Credentials);

    EngineCardInfo[] CreateEngineCards() =>
    [
        new("anysearch", "AnySearch", "可匿名尝试",
            "通用与垂直检索，还支持批量搜索和网页正文提取。",
            CountKeys(Configuration.AnySearchApiKey), true, RenderAnySearchCredentials),
        new("tavily", "Tavily", "英文与 AI 摘要",
            "面向 AI 的网页搜索，可获取内容摘要。需要 Tavily 密钥。",
            CountKeys(Configuration.TavilyApiKey1, Configuration.TavilyApiKey2, Configuration.TavilyApiKey3, Configuration.TavilyApiKey4),
            false, RenderTavilyCredentials),
        new("baidu", "百度千帆", "中文与扩展功能",
            "中文网页搜索，以及 AI 总结、热搜和识图。需要千帆密钥。",
            CountKeys(Configuration.BaiduApiKey1, Configuration.BaiduApiKey2, Configuration.BaiduApiKey3, Configuration.BaiduApiKey4),
            false, RenderBaiduCredentials),
        new("deepseek", "DeepSeek", "搜索专用 API",
            "返回带排序分数的网页结果和长摘要，可按需获取正文。",
            CountKeys(Configuration.DeepSeekApiKey), false, RenderDeepSeekCredentials)
    ];

    void RenderHeader(RenderTreeBuilder b, ref int sequence)
    {
        b.OpenElement(sequence++, "header");
        b.AddAttribute(sequence++, "class", "sws-hero");
        b.OpenElement(sequence++, "div");
        b.AddAttribute(sequence++, "class", "sws-hero-heading");
        b.OpenElement(sequence++, "span");
        b.AddAttribute(sequence++, "class", "sws-hero-icon");
        b.AddAttribute(sequence++, "aria-hidden", "true");
        b.AddContent(sequence++, "⌕");
        b.CloseElement();
        b.OpenElement(sequence++, "div");
        b.OpenElement(sequence++, "h2");
        b.AddAttribute(sequence++, "class", "sws-title");
        b.AddContent(sequence++, "网络搜索工作台");
        b.CloseElement();
        b.OpenElement(sequence++, "p");
        b.AddAttribute(sequence++, "class", "sws-subtitle");
        b.AddContent(sequence++, "把实时信息交给 AI，把选择权留给你。");
        b.CloseElement();
        b.CloseElement();
        b.CloseElement();
        b.OpenElement(sequence++, "span");
        b.AddAttribute(sequence++, "class", "sws-scope");
        b.AddContent(sequence++, Character == null ? "当前：全局配置" : $"当前：{Character.Name} 的配置");
        b.CloseElement();
        b.CloseElement();
    }

    static void RenderSteps(RenderTreeBuilder b, ref int sequence)
    {
        b.OpenElement(sequence++, "div");
        b.AddAttribute(sequence++, "class", "sws-steps");
        AddStep(b, ref sequence, "1", "选择引擎", "勾选需要使用的渠道");
        AddStep(b, ref sequence, "2", "填写密钥", "只配置所选渠道即可");
        AddStep(b, ref sequence, "3", "保存并使用", "点页面下方的应用按钮");
        b.CloseElement();
    }

    static void AddStep(RenderTreeBuilder b, ref int sequence, string number, string title, string description)
    {
        b.OpenElement(sequence++, "div");
        b.AddAttribute(sequence++, "class", "sws-step");
        b.OpenElement(sequence++, "span");
        b.AddAttribute(sequence++, "class", "sws-step-number");
        b.AddContent(sequence++, number);
        b.CloseElement();
        b.OpenElement(sequence++, "div");
        b.OpenElement(sequence++, "strong");
        b.AddContent(sequence++, title);
        b.CloseElement();
        b.OpenElement(sequence++, "small");
        b.AddContent(sequence++, description);
        b.CloseElement();
        b.CloseElement();
        b.CloseElement();
    }

    void RenderEngineCard(RenderTreeBuilder b, ref int sequence, EngineCardInfo engine)
    {
        bool selected = Configuration.IsEngineEnabled(engine.Id);
        string status = selected
            ? engine.KeyCount > 0 ? "已选 · 已填 Key" : engine.Anonymous ? "已选 · 可匿名尝试" : "已选 · 待填 Key"
            : engine.KeyCount > 0 ? "未选 · 密钥已保留" : "未选择";
        string statusClass = selected && engine.Anonymous == false && engine.KeyCount == 0
            ? "sws-pill sws-pill-warning"
            : selected ? "sws-pill sws-pill-selected" : "sws-pill";

        b.OpenElement(sequence++, "label");
        b.SetKey(engine.Id);
        b.AddAttribute(sequence++, "class", selected ? "sws-engine-card sws-engine-selected" : "sws-engine-card");
        b.AddAttribute(sequence++, "data-sws-engine", engine.Id);
        b.OpenElement(sequence++, "span");
        b.AddAttribute(sequence++, "class", "sws-engine-top");
        b.OpenElement(sequence++, "input");
        b.AddAttribute(sequence++, "class", "sws-engine-check");
        b.AddAttribute(sequence++, "type", "checkbox");
        b.AddAttribute(sequence++, "checked", selected);
        b.AddAttribute(sequence++, "aria-label", $"使用 {engine.Name} 搜索引擎");
        b.AddAttribute(sequence++, "onchange",
            EventCallback.Factory.Create<ChangeEventArgs>(this,
                e => Configuration.SetEngineEnabled(engine.Id, e.Value is true)));
        b.SetUpdatesAttributeName("checked");
        b.CloseElement();
        b.OpenElement(sequence++, "span");
        b.AddAttribute(sequence++, "class", "sws-engine-name");
        b.AddContent(sequence++, engine.Name);
        b.CloseElement();
        b.OpenElement(sequence++, "span");
        b.AddAttribute(sequence++, "class", "sws-engine-category");
        b.AddContent(sequence++, engine.Category);
        b.CloseElement();
        b.CloseElement();
        b.OpenElement(sequence++, "span");
        b.AddAttribute(sequence++, "class", "sws-engine-description");
        b.AddContent(sequence++, engine.Description);
        b.CloseElement();
        b.OpenElement(sequence++, "span");
        b.AddAttribute(sequence++, "class", "sws-engine-state " + statusClass);
        b.AddContent(sequence++, status);
        b.CloseElement();
        b.CloseElement();
    }

    void RenderAnySearchCredentials(RenderTreeBuilder b)
    {
        int sequence = 0;
        AddCredentialHeading(b, ref sequence, "AnySearch", "获取密钥", "https://anysearch.com/console/api-keys");
        AddInputField(b, ref sequence, "API Key（可留空）", Configuration.AnySearchApiKey,
            value => Configuration.AnySearchApiKey = value, "password",
            "留空尝试匿名模式；填写密钥后按账号额度使用。匿名渠道不保证始终可用。", "anysearch-key",
            "可选，格式通常为 as_sk_…");
        AddHelper(b, ref sequence, "支持通用/垂直检索、批量搜索、正文提取。对于重要事实，请让 AI 附上来源并核对原文。");
    }

    void RenderTavilyCredentials(RenderTreeBuilder b)
    {
        int sequence = 0;
        AddCredentialHeading(b, ref sequence, "Tavily", "获取密钥", "https://app.tavily.com");
        AddInputField(b, ref sequence, "API Key（主账号）", Configuration.TavilyApiKey1,
            value => Configuration.TavilyApiKey1 = value, "password",
            "已有备用账号也会参与轮换；以控制台显示的额度和计费为准。", "tavily-key-1", "粘贴 tvly-… 密钥");
        RenderDisclosure(b, ref sequence, "tavily-backup", "备用账号",
            $"已填 {CountKeys(Configuration.TavilyApiKey2, Configuration.TavilyApiKey3, Configuration.TavilyApiKey4)} / 3 · 可选，额度耗尽后自动轮换",
            content =>
            {
                int index = 0;
                AddInputField(content, ref index, "备用 Key 2", Configuration.TavilyApiKey2, value => Configuration.TavilyApiKey2 = value, "password", field: "tavily-key-2");
                AddInputField(content, ref index, "备用 Key 3", Configuration.TavilyApiKey3, value => Configuration.TavilyApiKey3 = value, "password", field: "tavily-key-3");
                AddInputField(content, ref index, "备用 Key 4", Configuration.TavilyApiKey4, value => Configuration.TavilyApiKey4 = value, "password", field: "tavily-key-4");
            }, true);
    }

    void RenderBaiduCredentials(RenderTreeBuilder b)
    {
        int sequence = 0;
        AddCredentialHeading(b, ref sequence, "百度千帆", "获取密钥", "https://console.bce.baidu.com/qianfan/ais/console/apiKey");
        AddInputField(b, ref sequence, "千帆 API Key（主账号）", Configuration.BaiduApiKey1,
            value => Configuration.BaiduApiKey1 = value, "password",
            "使用千帆平台的 API Key，不是百度账号密码。请先检查账号额度与后付费设置。", "baidu-key-1", "粘贴千帆 API Key");
        RenderDisclosure(b, ref sequence, "baidu-backup", "备用账号",
            $"已填 {CountKeys(Configuration.BaiduApiKey2, Configuration.BaiduApiKey3, Configuration.BaiduApiKey4)} / 3 · 可选，额度耗尽后自动轮换",
            content =>
            {
                int index = 0;
                AddInputField(content, ref index, "备用 Key 2", Configuration.BaiduApiKey2, value => Configuration.BaiduApiKey2 = value, "password", field: "baidu-key-2");
                AddInputField(content, ref index, "备用 Key 3", Configuration.BaiduApiKey3, value => Configuration.BaiduApiKey3 = value, "password", field: "baidu-key-3");
                AddInputField(content, ref index, "备用 Key 4", Configuration.BaiduApiKey4, value => Configuration.BaiduApiKey4 = value, "password", field: "baidu-key-4");
            }, true);
    }

    void RenderDeepSeekCredentials(RenderTreeBuilder b)
    {
        int sequence = 0;
        AddCredentialHeading(b, ref sequence, "DeepSeek", "获取密钥", "https://platform.deepseek.com/api_keys");
        AddInputField(b, ref sequence, "DeepSeek API Key", Configuration.DeepSeekApiKey,
            value => Configuration.DeepSeekApiKey = value, "password",
            "使用 DeepSeek 官方平台密钥，调用搜索专用 API；不需要填写聊天模型名。额度与费用以官方平台为准。", "deepseek-key", "粘贴 DeepSeek API Key");
        AddToggle(b, ref sequence, "同时获取网页正文", Configuration.DeepSeekIncludeContent,
            value => Configuration.DeepSeekIncludeContent = value,
            "默认只读取搜索结果与长摘要，较简洁。开启后尝试获取正文，给 AI 更多参考，但返回内容更长。", "deepseek-content");
        AddExternalLink(b, ref sequence, "查看搜索 API 官方文档 ↗", "https://api-docs.deepseek.com/zh-cn/api/search");
    }

    void RenderSourceSettings(RenderTreeBuilder b)
    {
        int sequence = 0;
        b.OpenElement(sequence++, "div");
        b.AddAttribute(sequence++, "class", "sws-source-grid");
        AddSourceItem(b, ref sequence, "插画 / 漫画", "找画师、作品与原图：SauceNAO");
        AddSourceItem(b, ref sequence, "番剧 / 动画截图", "找作品、集数与时间点：trace.moe");
        AddSourceItem(b, ref sequence, "照片 / 游戏截图", "找相关网页：Yandex");
        b.CloseElement();
        AddHelper(b, ref sequence, "直接把图片发给 AI，问“这张图出自哪里？”即可。Yandex 使用网页接口且只支持图片 URL，可能遇到限流或验证码；结果需要核对来源。");
        AddDivider(b, ref sequence);
        b.OpenElement(sequence++, "div");
        b.AddAttribute(sequence++, "class", "sws-fields-grid");
        AddInputField(b, ref sequence, "SauceNAO API Key（可选）", Configuration.SauceNaoApiKey1,
            value => Configuration.SauceNaoApiKey1 = value, "password",
            "留空走网页匿名方式；填写后使用 JSON API。", "saucenao-key-1");
        AddInputField(b, ref sequence, "trace.moe Token（可选）", Configuration.TraceMoeApiKey1,
            value => Configuration.TraceMoeApiKey1 = value, "password",
            "留空也可尝试匿名查询。", "tracemoe-key-1");
        b.CloseElement();
        RenderDisclosure(b, ref sequence, "source-backup", "图片搜源备用密钥", "可选，主账号不可用时参与轮换。",
            content =>
            {
                int index = 0;
                AddInputField(content, ref index, "SauceNAO 备用 Key 2", Configuration.SauceNaoApiKey2,
                    value => Configuration.SauceNaoApiKey2 = value, "password", field: "saucenao-key-2");
                AddInputField(content, ref index, "trace.moe 备用 Token 2", Configuration.TraceMoeApiKey2,
                    value => Configuration.TraceMoeApiKey2 = value, "password", field: "tracemoe-key-2");
            }, true);
        b.OpenElement(sequence++, "p");
        b.AddAttribute(sequence++, "class", "sws-helper");
        AddExternalLink(b, ref sequence, "SauceNAO 密钥页面 ↗", "https://saucenao.com/user.php?page=search-api");
        b.AddContent(sequence++, "　");
        AddExternalLink(b, ref sequence, "trace.moe 官网 ↗", "https://trace.moe");
        b.CloseElement();
    }

    void RenderAdvancedSettings(RenderTreeBuilder b)
    {
        int sequence = 0;
        AddToggle(b, ref sequence, "缓存重复搜索", Configuration.EnableCache,
            value => Configuration.EnableCache = value,
            "相同查询在有效期内直接复用结果，节省请求。需要追踪即时变化时，可关闭缓存。", "enable-cache");
        AddInputField(b, ref sequence, "缓存保留时间（分钟）", Configuration.CacheTtlMinutes.ToString(CultureInfo.InvariantCulture),
            value =>
            {
                if (int.TryParse(value, out int minutes))
                    Configuration.CacheTtlMinutes = Math.Clamp(minutes, 1, 60);
            }, "number", "默认 5 分钟，范围 1–60 分钟。", "cache-minutes", minimum: 1, maximum: 60);
        AddDivider(b, ref sequence);
        AddToggle(b, ref sequence, "按需加载 AI 工具说明（省上下文）", Configuration.ImplicitInjection,
            value => Configuration.ImplicitInjection = value,
            "开启后 AI 先调用 <SmartWebSearch/> 再读取具体工具说明；关闭时直接提供所选渠道的说明。改动后需重新激活角色。", "implicit-injection");
        if (Configuration.IsEngineEnabled("baidu") == false)
            return;
        RenderDisclosure(b, ref sequence, "baidu-ai-settings", "百度 AI 搜索模型", "仅对百度千帆的 AI 总结与智能搜索生成生效，不是 DeepSeek 搜索 API 设置。",
            content =>
            {
                int index = 0;
                AddSelectField(content, ref index, "高性能版思考模式", Configuration.SummaryModel,
                    value => Configuration.SummaryModel = value,
                    new[] { ("auto_thinking", "自动判断（推荐）"), ("thinking", "启用思考"), ("non_thinking", "不启用思考") },
                    "保留默认即可；实际模型可用性以百度账号为准。", "summary-model");
                AddInputField(content, ref index, "智能搜索生成模型（标准版）", Configuration.ChatSearchModel,
                    value => Configuration.ChatSearchModel = value, "text",
                    "默认 ernie-4.5-turbo-32k。使用千帆提供的 DeepSeek 模型需在千帆账号开通；与上面的 DeepSeek 官方 API Key 互不通用。", "chat-search-model");
                AddToggle(content, ref index, "标准版启用深度搜索", Configuration.EnableDeepSearch,
                    value => Configuration.EnableDeepSearch = value,
                    "更精细，但可能更慢且消耗更多额度；日常使用建议关闭。", "baidu-deep-search");
            }, true);
    }

    void RenderFooter(RenderTreeBuilder b, ref int sequence)
    {
        b.OpenElement(sequence++, "footer");
        b.AddAttribute(sequence++, "class", "sws-footer");
        b.OpenElement(sequence++, "span");
        b.AddAttribute(sequence++, "aria-hidden", "true");
        b.AddContent(sequence++, "✓");
        b.CloseElement();
        b.OpenElement(sequence++, "div");
        b.OpenElement(sequence++, "p");
        b.OpenElement(sequence++, "strong");
        b.AddContent(sequence++, "别忘了保存：");
        b.CloseElement();
        b.AddContent(sequence++, Character == null
            ? "点击页面下方“应用到全局”，未设置独立配置的角色将使用此配置。"
            : "只给当前角色使用，点“应用到角色”；想作为默认设置，点“应用到全局”。");
        b.CloseElement();
        b.OpenElement(sequence++, "p");
        b.AddContent(sequence++, "修改引擎选择、图片搜源开关或工具说明加载方式后，请重新激活使用该配置的角色，让 AI 工具列表同步更新。");
        b.CloseElement();
        b.OpenElement(sequence++, "p");
        b.AddAttribute(sequence++, "class", "sws-security");
        b.AddContent(sequence++, "密钥会保存在 Alife 的本地配置文件中。不要把密钥、配置文件或含密钥的截图公开分享。");
        b.CloseElement();
        b.CloseElement();
        b.CloseElement();
    }

    // 各分区独立闭合，RenderFragment 的序号与父层隔离，减少条件渲染对输入状态的影响。
    static void RenderSectionHeader(RenderTreeBuilder b, ref int sequence, string title, string caption, string? pill = null)
    {
        b.OpenElement(sequence++, "div");
        b.AddAttribute(sequence++, "class", "sws-section-heading");
        b.OpenElement(sequence++, "div");
        b.OpenElement(sequence++, "h3");
        b.AddAttribute(sequence++, "class", "sws-section-title");
        b.AddContent(sequence++, title);
        b.CloseElement();
        b.OpenElement(sequence++, "p");
        b.AddAttribute(sequence++, "class", "sws-section-caption");
        b.AddContent(sequence++, caption);
        b.CloseElement();
        b.CloseElement();
        if (string.IsNullOrEmpty(pill) == false)
        {
            b.OpenElement(sequence++, "span");
            b.AddAttribute(sequence++, "class", "sws-pill");
            b.AddContent(sequence++, pill);
            b.CloseElement();
        }
        b.CloseElement();
    }

    void RenderDisclosure(RenderTreeBuilder b, ref int sequence, string key, string title,
        string caption, RenderFragment content, bool compact = false)
    {
        bool expanded = disclosureStates.TryGetValue(key, out bool current) && current;
        b.OpenElement(sequence++, "section");
        b.AddAttribute(sequence++, "class", compact ? "sws-disclosure sws-disclosure-compact" : "sws-disclosure");
        b.AddAttribute(sequence++, "data-sws-disclosure", key);
        b.OpenComponent<Button>(sequence++);
        b.AddAttribute(sequence++, "Class", "sws-disclosure-button");
        b.AddAttribute(sequence++, "Type", ButtonType.Text);
        b.AddAttribute(sequence++, "HtmlType", "button");
        b.AddAttribute(sequence++, "NoSpanWrap", true);
        b.AddAttribute(sequence++, "AriaLabel", $"{title}，{(expanded ? "收起" : "展开")}");
        b.AddAttribute(sequence++, "OnClick",
            EventCallback.Factory.Create<MouseEventArgs>(this, _ => ToggleDisclosure(key)));
        b.AddAttribute(sequence++, "ChildContent", (RenderFragment)(heading =>
        {
            int index = 0;
            heading.OpenElement(index++, "span");
            heading.AddAttribute(index++, "class", "sws-disclosure-line");
            heading.OpenElement(index++, "span");
            heading.AddAttribute(index++, "class", "sws-disclosure-label");
            heading.AddContent(index++, title);
            heading.CloseElement();
            heading.OpenElement(index++, "span");
            heading.AddAttribute(index++, "class", "sws-disclosure-action");
            heading.AddContent(index++, expanded ? "收起 −" : "展开 +");
            heading.CloseElement();
            heading.CloseElement();
            heading.OpenElement(index++, "span");
            heading.AddAttribute(index++, "class", "sws-disclosure-caption");
            heading.AddContent(index++, caption);
            heading.CloseElement();
        }));
        b.CloseComponent();
        if (expanded)
        {
            b.OpenElement(sequence++, "div");
            b.AddAttribute(sequence++, "class", "sws-disclosure-content");
            b.AddContent(sequence++, content);
            b.CloseElement();
        }
        b.CloseElement();
    }

    void ToggleDisclosure(string key)
    {
        bool expanded = disclosureStates.TryGetValue(key, out bool current) && current;
        disclosureStates[key] = !expanded;
    }

    static int CountKeys(params string?[] keys)
    {
        int count = 0;
        foreach (string? key in keys)
            if (string.IsNullOrWhiteSpace(key) == false) count++;
        return count;
    }

    static void AddCredentialHeading(RenderTreeBuilder b, ref int sequence, string name, string linkText, string linkUrl)
    {
        b.OpenElement(sequence++, "div");
        b.AddAttribute(sequence++, "class", "sws-credentials-title");
        b.OpenElement(sequence++, "span");
        b.AddContent(sequence++, name);
        b.CloseElement();
        AddExternalLink(b, ref sequence, linkText + " ↗", linkUrl);
        b.CloseElement();
    }

    void AddInputField(RenderTreeBuilder b, ref int sequence, string label, string? value, Action<string> setter,
        string type = "text", string? hint = null, string? field = null, string? placeholder = null,
        int? minimum = null, int? maximum = null)
    {
        b.OpenElement(sequence++, "label");
        b.AddAttribute(sequence++, "class", "sws-field");
        b.OpenElement(sequence++, "span");
        b.AddAttribute(sequence++, "class", "sws-field-label");
        b.AddContent(sequence++, label);
        b.CloseElement();
        b.OpenElement(sequence++, "input");
        b.AddAttribute(sequence++, "class", "sws-control");
        b.AddAttribute(sequence++, "type", type);
        b.AddAttribute(sequence++, "value", value ?? "");
        b.AddAttribute(sequence++, "data-sws-field", field);
        b.AddAttribute(sequence++, "aria-label", label);
        if (string.IsNullOrEmpty(placeholder) == false)
            b.AddAttribute(sequence++, "placeholder", placeholder);
        if (type == "password")
        {
            b.AddAttribute(sequence++, "autocomplete", "off");
            b.AddAttribute(sequence++, "spellcheck", "false");
        }
        if (minimum.HasValue) b.AddAttribute(sequence++, "min", minimum.Value);
        if (maximum.HasValue) b.AddAttribute(sequence++, "max", maximum.Value);
        b.AddAttribute(sequence++, "oninput",
            EventCallback.Factory.Create<ChangeEventArgs>(this, e => setter(e.Value?.ToString() ?? "")));
        b.SetUpdatesAttributeName("value");
        b.CloseElement();
        if (string.IsNullOrEmpty(hint) == false)
        {
            b.OpenElement(sequence++, "span");
            b.AddAttribute(sequence++, "class", "sws-field-hint");
            b.AddContent(sequence++, hint);
            b.CloseElement();
        }
        b.CloseElement();
    }

    void AddSelectField(RenderTreeBuilder b, ref int sequence, string label, string? value,
        Action<string> setter, (string Value, string Text)[] options, string? hint = null, string? field = null)
    {
        b.OpenElement(sequence++, "label");
        b.AddAttribute(sequence++, "class", "sws-field");
        b.OpenElement(sequence++, "span");
        b.AddAttribute(sequence++, "class", "sws-field-label");
        b.AddContent(sequence++, label);
        b.CloseElement();
        b.OpenElement(sequence++, "select");
        b.AddAttribute(sequence++, "class", "sws-control");
        b.AddAttribute(sequence++, "value", value);
        b.AddAttribute(sequence++, "aria-label", label);
        b.AddAttribute(sequence++, "data-sws-field", field);
        b.AddAttribute(sequence++, "onchange",
            EventCallback.Factory.Create<ChangeEventArgs>(this, e => setter(e.Value?.ToString() ?? "")));
        b.SetUpdatesAttributeName("value");
        bool hasCurrentOption = false;
        foreach ((string optionValue, string text) in options)
        {
            bool selected = string.Equals(optionValue, value, StringComparison.Ordinal);
            hasCurrentOption |= selected;
            b.OpenElement(sequence++, "option");
            b.AddAttribute(sequence++, "value", optionValue);
            b.AddAttribute(sequence++, "selected", selected);
            b.AddContent(sequence++, text);
            b.CloseElement();
        }
        if (hasCurrentOption == false && string.IsNullOrWhiteSpace(value) == false)
        {
            b.OpenElement(sequence++, "option");
            b.AddAttribute(sequence++, "value", value);
            b.AddAttribute(sequence++, "selected", true);
            b.AddContent(sequence++, $"已有设置：{value}");
            b.CloseElement();
        }
        b.CloseElement();
        if (string.IsNullOrEmpty(hint) == false)
        {
            b.OpenElement(sequence++, "span");
            b.AddAttribute(sequence++, "class", "sws-field-hint");
            b.AddContent(sequence++, hint);
            b.CloseElement();
        }
        b.CloseElement();
    }

    void AddToggle(RenderTreeBuilder b, ref int sequence, string title, bool value, Action<bool> setter,
        string description, string field)
    {
        b.OpenElement(sequence++, "label");
        b.AddAttribute(sequence++, "class", "sws-toggle");
        b.OpenElement(sequence++, "input");
        b.AddAttribute(sequence++, "class", "sws-toggle-check");
        b.AddAttribute(sequence++, "type", "checkbox");
        b.AddAttribute(sequence++, "checked", value);
        b.AddAttribute(sequence++, "aria-label", title);
        b.AddAttribute(sequence++, "data-sws-field", field);
        b.AddAttribute(sequence++, "onchange",
            EventCallback.Factory.Create<ChangeEventArgs>(this, e => setter(e.Value is true)));
        b.SetUpdatesAttributeName("checked");
        b.CloseElement();
        b.OpenElement(sequence++, "span");
        b.OpenElement(sequence++, "span");
        b.AddAttribute(sequence++, "class", "sws-toggle-title");
        b.AddContent(sequence++, title);
        b.CloseElement();
        b.OpenElement(sequence++, "span");
        b.AddAttribute(sequence++, "class", "sws-toggle-description");
        b.AddContent(sequence++, description);
        b.CloseElement();
        b.CloseElement();
        b.CloseElement();
    }

    static void AddNotice(RenderTreeBuilder b, ref int sequence, string text, bool warning = false)
    {
        b.OpenElement(sequence++, "div");
        b.AddAttribute(sequence++, "class", warning ? "sws-notice sws-notice-warning" : "sws-notice");
        b.AddAttribute(sequence++, "role", "status");
        b.AddAttribute(sequence++, "aria-live", "polite");
        b.OpenElement(sequence++, "span");
        b.AddAttribute(sequence++, "class", "sws-notice-symbol");
        b.AddAttribute(sequence++, "aria-hidden", "true");
        b.AddContent(sequence++, warning ? "!" : "i");
        b.CloseElement();
        b.OpenElement(sequence++, "span");
        b.AddContent(sequence++, text);
        b.CloseElement();
        b.CloseElement();
    }

    static void AddHelper(RenderTreeBuilder b, ref int sequence, string text)
    {
        b.OpenElement(sequence++, "p");
        b.AddAttribute(sequence++, "class", "sws-helper");
        b.AddContent(sequence++, text);
        b.CloseElement();
    }

    static void AddExternalLink(RenderTreeBuilder b, ref int sequence, string text, string url)
    {
        b.OpenElement(sequence++, "a");
        b.AddAttribute(sequence++, "class", "sws-link");
        b.AddAttribute(sequence++, "href", url);
        b.AddAttribute(sequence++, "target", "_blank");
        b.AddAttribute(sequence++, "rel", "noopener noreferrer");
        b.AddContent(sequence++, text);
        b.CloseElement();
    }

    static void AddDivider(RenderTreeBuilder b, ref int sequence)
    {
        b.OpenElement(sequence++, "div");
        b.AddAttribute(sequence++, "class", "sws-divider");
        b.AddAttribute(sequence++, "aria-hidden", "true");
        b.CloseElement();
    }

    static void AddSourceItem(RenderTreeBuilder b, ref int sequence, string title, string description)
    {
        b.OpenElement(sequence++, "div");
        b.AddAttribute(sequence++, "class", "sws-source-item");
        b.OpenElement(sequence++, "strong");
        b.AddContent(sequence++, title);
        b.CloseElement();
        b.OpenElement(sequence++, "span");
        b.AddContent(sequence++, description);
        b.CloseElement();
        b.CloseElement();
    }
}
