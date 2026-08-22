using System;
using Alife.Framework;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using AntDesign;

namespace Alife.Plugin.SmartWebSearch;

public partial class SmartWebSearchUI : ModuleUIBase<SmartWebSearch, SmartWebSearchConfig>
{
    const string Css = @"
/* === 基础容器：深紫圣殿 + 粉光晕 + 曼陀罗光轮 === */
.sws-container {
    background:
        /* 曼陀罗：金色放射线 15° 间隔 */
        repeating-conic-gradient(
            from 0deg at 50% 50%,
            rgba(212,175,55,0.030) 0deg, rgba(212,175,55,0.030) 0.8deg,
            transparent 0.8deg, transparent 15deg
        ),
        /* 曼陀罗：粉色放射线 offset 7.5° 交错 */
        repeating-conic-gradient(
            from 7.5deg at 50% 50%,
            rgba(255,94,138,0.022) 0deg, rgba(255,94,138,0.022) 0.8deg,
            transparent 0.8deg, transparent 15deg
        ),
        /* 曼陀罗：银色放射线 offset 3.75° 交错 */
        repeating-conic-gradient(
            from 3.75deg at 50% 50%,
            rgba(196,181,212,0.018) 0deg, rgba(196,181,212,0.018) 0.5deg,
            transparent 0.5deg, transparent 7.5deg
        ),
        /* 粉光晕（左上） */
        radial-gradient(circle at 20% 10%, rgba(255,94,138,0.10) 0%, transparent 45%),
        /* 月华银光（右下） */
        radial-gradient(circle at 80% 85%, rgba(196,181,212,0.08) 0%, transparent 45%),
        /* 祭坛金心（中心） */
        radial-gradient(circle at 50% 50%, rgba(212,175,55,0.04) 0%, transparent 60%),
        /* 基底深紫渐变 */
        linear-gradient(135deg, #15081f 0%, #2a0e2f 100%);
    padding: 26px 24px;
    border-radius: 14px;
    border: 1px solid rgba(212,175,55,0.28);
    max-width: 680px;
    color: #fff5f1;
    font-family: 'Segoe UI', 'PingFang SC', 'Microsoft YaHei', sans-serif;
    position: relative;
    box-shadow: 0 0 36px rgba(255,94,138,0.18), inset 0 0 80px rgba(196,61,104,0.06);
}

/* 角落缓慢旋转的封印 */
.sws-container::before {
    content: '✦';
    position: absolute;
    top: 14px; right: 18px;
    font-size: 14px;
    color: rgba(212,175,55,0.35);
    animation: sws-sigil 22s linear infinite;
    pointer-events: none;
}
.sws-container::after {
    content: '⛤';
    position: absolute;
    bottom: 14px; left: 18px;
    font-size: 12px;
    color: rgba(196,181,212,0.25);
    animation: sws-sigil 28s linear infinite reverse;
    pointer-events: none;
}

/* === 头部圣徽 === */
.sws-header {
    text-align: center;
    margin-bottom: 18px;
    padding-top: 22px;
    padding-bottom: 14px;
    border-bottom: 1px solid rgba(212,175,55,0.22);
    position: relative;
}
/* 标题上方漂浮五芒星 orb */
.sws-header::before {
    content: '⛤';
    position: absolute;
    top: 0; left: 50%;
    transform: translateX(-50%);
    font-size: 16px;
    color: #d4af37;
    text-shadow:
        0 0 8px rgba(212,175,55,0.9),
        0 0 16px rgba(212,175,55,0.5),
        0 0 24px rgba(255,94,138,0.3);
    animation: sws-halo-gold 2.6s ease-in-out infinite, sws-orb-float 5s ease-in-out infinite;
    pointer-events: none;
    z-index: 1;
}
.sws-header::after {
    content: '◈ ◇ ◈';
    position: absolute;
    bottom: -7px; left: 50%;
    transform: translateX(-50%);
    background: linear-gradient(135deg, #15081f 0%, #2a0e2f 100%);
    padding: 0 10px;
    color: #d4af37;
    font-size: 10px;
    letter-spacing: 4px;
    text-shadow: 0 0 6px rgba(212,175,55,0.5);
}
.sws-title {
    font-size: 19px;
    font-weight: bold;
    color: #ff8fb1;
    letter-spacing: 2px;
    text-shadow: 0 0 12px rgba(255,94,138,0.5), 0 0 24px rgba(196,61,104,0.3);
    animation: sws-halo 3.2s ease-in-out infinite;
}
.sws-subtitle {
    font-size: 11px;
    color: #c4b5d4;
    margin-top: 7px;
    letter-spacing: 4px;
    opacity: 0.7;
}

/* === 引言卷轴 === */
.sws-intro {
    background: rgba(255,94,138,0.06);
    border: 1px solid rgba(255,94,138,0.20);
    border-left: 3px solid #ff5e8a;
    border-radius: 8px;
    padding: 12px 14px;
    margin-bottom: 18px;
    display: flex;
    gap: 12px;
    align-items: flex-start;
}
.sws-intro-sigil {
    color: #d4af37;
    font-size: 20px;
    text-shadow: 0 0 8px rgba(212,175,55,0.6);
    animation: sws-float 4s ease-in-out infinite;
    flex-shrink: 0;
}
.sws-intro-text {
    font-size: 12px;
    color: #e8def5;
    line-height: 1.75;
    white-space: pre-line;
    flex: 1;
}

/* === 章节封印 === */
.sws-section {
    margin-top: 18px;
    padding: 14px 16px 4px;
    background: rgba(21,8,31,0.45);
    border: 1px solid rgba(196,181,212,0.14);
    border-radius: 10px;
    position: relative;
    transition: border-color 0.3s, box-shadow 0.3s;
    overflow: hidden;
}
/* 右上角晶洞装饰：conic-gradient 模拟晶体切面折射 */
.sws-section::before {
    content: '';
    position: absolute;
    top: 0; right: 0;
    width: 48px; height: 48px;
    background:
        conic-gradient(
            from 225deg at 100% 0%,
            transparent 0deg,
            rgba(212,175,55,0.18) 8deg, transparent 16deg,
            rgba(255,94,138,0.14) 24deg, transparent 32deg,
            rgba(196,181,212,0.12) 40deg, transparent 48deg,
            rgba(212,175,55,0.10) 56deg, transparent 64deg,
            rgba(255,94,138,0.08) 72deg, transparent 90deg
        );
    border-radius: 0 10px 0 0;
    pointer-events: none;
    animation: sws-sigil 90s linear infinite;
    z-index: 0;
}
/* 左下角反向晶洞装饰 */
.sws-section::after {
    content: '';
    position: absolute;
    bottom: 0; left: 0;
    width: 36px; height: 36px;
    background:
        conic-gradient(
            from 45deg at 0% 100%,
            transparent 0deg,
            rgba(196,181,212,0.10) 8deg, transparent 16deg,
            rgba(212,175,55,0.12) 24deg, transparent 32deg,
            rgba(255,94,138,0.08) 40deg, transparent 48deg,
            rgba(196,181,212,0.06) 56deg, transparent 64deg,
            rgba(212,175,55,0.08) 72deg, transparent 90deg
        );
    border-radius: 0 0 0 10px;
    pointer-events: none;
    animation: sws-sigil 110s linear infinite reverse;
    z-index: 0;
}
.sws-section:hover {
    border-color: rgba(255,94,138,0.32);
    box-shadow: 0 0 16px rgba(255,94,138,0.10);
}
.sws-section > * { position: relative; z-index: 1; }
.sws-section-title {
    font-size: 14px;
    font-weight: bold;
    color: #ff8fb1;
    margin: 0 0 12px;
    padding-bottom: 7px;
    border-bottom: 1px solid rgba(212,175,55,0.25);
    display: flex;
    align-items: center;
    gap: 8px;
}
.sws-section-icon {
    color: #d4af37;
    font-size: 15px;
    text-shadow: 0 0 6px rgba(212,175,55,0.55);
}

/* === 状态徽标：圣光 orb === */
.sws-badge {
    display: inline-flex;
    align-items: center;
    font-size: 10px;
    padding: 2px 9px;
    border-radius: 12px;
    margin-left: auto;
    letter-spacing: 1px;
    font-weight: bold;
}
.sws-badge-on {
    background: rgba(255,94,138,0.18);
    color: #ff8fb1;
    border: 1px solid rgba(255,94,138,0.55);
    box-shadow: 0 0 10px rgba(255,94,138,0.4);
    animation: sws-halo 3s ease-in-out infinite;
}
.sws-badge-off {
    background: rgba(107,74,122,0.18);
    color: #6b4a7a;
    border: 1px solid rgba(107,74,122,0.4);
}

/* === 字段 === */
.sws-field { margin-bottom: 12px; }
.sws-label {
    font-size: 13px;
    font-weight: bold;
    color: #e8def5;
    margin-bottom: 4px;
}
.sws-hint {
    font-size: 11px;
    color: #a8748a;
    margin: 4px 0 10px 2px;
    line-height: 1.65;
    white-space: pre-line;
}

/* === AntDesign 覆盖（必须 !important） === */
.sws-container .ant-input,
.sws-container .ant-input-affix-wrapper {
    background: rgba(21,8,31,0.6) !important;
    border: 1px solid rgba(196,181,212,0.3) !important;
    color: #fff5f1 !important;
    border-radius: 6px !important;
    font-size: 13px !important;
}
.sws-container .ant-input:hover,
.sws-container .ant-input-affix-wrapper:hover {
    border-color: rgba(255,94,138,0.6) !important;
    box-shadow: 0 0 8px rgba(255,94,138,0.3) !important;
}
.sws-container .ant-input:focus,
.sws-container .ant-input-focused,
.sws-container .ant-input-affix-wrapper-focused {
    border-color: #ff5e8a !important;
    box-shadow: 0 0 12px rgba(255,94,138,0.5) !important;
}
.sws-container .ant-input::placeholder,
.sws-container .ant-input-affix-wrapper input::placeholder {
    color: rgba(168,116,138,0.5) !important;
}
.sws-container .ant-input-password-icon,
.sws-container .anticon-eye,
.sws-container .anticon-eye-invisible {
    color: #a8748a !important;
}
.sws-container .ant-input-affix-wrapper .ant-input {
    background: transparent !important;
    border: none !important;
    box-shadow: none !important;
}

/* === 自定义 select（占星罗盘） === */
.sws-select {
    width: 100%;
    padding: 7px 28px 7px 10px;
    background: rgba(21,8,31,0.6);
    border: 1px solid rgba(196,181,212,0.3);
    color: #fff5f1;
    border-radius: 6px;
    font-size: 13px;
    font-family: inherit;
    outline: none;
    cursor: pointer;
    appearance: none;
    -webkit-appearance: none;
    background-image:
        linear-gradient(45deg, transparent 50%, #d4af37 50%),
        linear-gradient(135deg, #d4af37 50%, transparent 50%);
    background-position: calc(100% - 14px) center, calc(100% - 9px) center;
    background-size: 5px 5px, 5px 5px;
    background-repeat: no-repeat;
    transition: border-color 0.25s, box-shadow 0.25s;
}
.sws-select:hover {
    border-color: rgba(255,94,138,0.6);
    box-shadow: 0 0 8px rgba(255,94,138,0.3);
}
.sws-select:focus {
    border-color: #ff5e8a;
    box-shadow: 0 0 12px rgba(255,94,138,0.5);
}
.sws-select option {
    background: #15081f;
    color: #fff5f1;
}

/* === Toggle：月相封印 === */
.sws-toggle {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 6px 0;
}
.sws-toggle input[type='checkbox'] {
    appearance: none;
    -webkit-appearance: none;
    width: 38px;
    height: 20px;
    background: rgba(107,74,122,0.3);
    border: 1px solid rgba(196,181,212,0.4);
    border-radius: 10px;
    cursor: pointer;
    position: relative;
    transition: all 0.3s;
    outline: none;
    flex-shrink: 0;
}
.sws-toggle input[type='checkbox']::before {
    content: '☾';
    position: absolute;
    top: 1px; left: 2px;
    width: 16px; height: 16px;
    background: #c4b5d4;
    color: #15081f;
    border-radius: 50%;
    font-size: 11px;
    line-height: 16px;
    text-align: center;
    transition: all 0.3s;
}
.sws-toggle input[type='checkbox']:checked {
    background: rgba(255,94,138,0.3);
    border-color: #ff5e8a;
    box-shadow: 0 0 8px rgba(255,94,138,0.5);
}
.sws-toggle input[type='checkbox']:checked::before {
    content: '✦';
    left: 18px;
    background: #ff5e8a;
    color: #fff5f1;
    text-shadow: 0 0 4px rgba(255,255,255,0.6);
}
.sws-toggle-label {
    font-size: 13px;
    font-weight: bold;
    color: #e8def5;
}

/* === 折叠封印 === */
.sws-details {
    margin: 8px 0;
    border: 1px solid rgba(196,181,212,0.2);
    border-radius: 6px;
    background: rgba(21,8,31,0.35);
    overflow: hidden;
    transition: border-color 0.25s;
}
.sws-details:hover { border-color: rgba(255,94,138,0.3); }
.sws-details summary {
    cursor: pointer;
    padding: 8px 12px;
    font-size: 12px;
    font-weight: bold;
    color: #a8748a;
    user-select: none;
    display: flex;
    align-items: center;
    gap: 8px;
    list-style: none;
}
.sws-details summary::-webkit-details-marker { display: none; }
.sws-arrow {
    display: inline-block;
    color: #d4af37;
    transition: transform 0.25s ease;
    text-shadow: 0 0 6px rgba(212,175,55,0.5);
    font-size: 11px;
}
.sws-details[open] .sws-arrow { transform: rotate(90deg); }
.sws-details[open] summary {
    border-bottom: 1px solid rgba(196,181,212,0.15);
    color: #ff8fb1;
}
.sws-details-content { padding: 10px 12px 4px; }

/* === 动画 === */
@keyframes sws-halo {
    0%, 100% { text-shadow: 0 0 12px rgba(255,94,138,0.5), 0 0 24px rgba(196,61,104,0.3); }
    50%      { text-shadow: 0 0 18px rgba(255,94,138,0.8), 0 0 36px rgba(196,61,104,0.5); }
}
@keyframes sws-halo-gold {
    0%, 100% {
        text-shadow:
            0 0 8px rgba(212,175,55,0.9),
            0 0 16px rgba(212,175,55,0.5),
            0 0 24px rgba(255,94,138,0.3);
    }
    50% {
        text-shadow:
            0 0 12px rgba(212,175,55,1.0),
            0 0 24px rgba(212,175,55,0.7),
            0 0 40px rgba(255,94,138,0.5);
    }
}
@keyframes sws-float {
    0%, 100% { transform: translateY(0); }
    50%      { transform: translateY(-3px); }
}
/* 五芒星专用漂浮：用 top 不动 transform，避免覆盖 translateX(-50%) */
@keyframes sws-orb-float {
    0%, 100% { top: 0; }
    50%      { top: -4px; }
}
@keyframes sws-sigil {
    from { transform: rotate(0deg); }
    to   { transform: rotate(360deg); }
}
";

    protected override void BuildRenderTree(RenderTreeBuilder b)
    {
        b.OpenElement(0, "style");
        b.AddContent(1, Css);
        b.CloseElement();

        if (Configuration == null)
        {
            b.AddContent(2, "Configuration NULL");
            return;
        }

        int i = 3;

        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "class", "sws-container");

        // === 头部圣徽 ===
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "class", "sws-header");
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "class", "sws-title");
        b.AddContent(i++, "✦ 网络智能搜索 ✦");
        b.CloseElement();
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "class", "sws-subtitle");
        b.AddContent(i++, "☉ ☿ ♀ ♂ ♃ ♄ ☾");
        b.CloseElement();
        b.CloseElement();

        // === 引言卷轴 ===
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "class", "sws-intro");
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "class", "sws-intro-sigil");
        b.AddContent(i++, "⛤");
        b.CloseElement();
        b.OpenElement(i++, "div");
        b.AddAttribute(i++, "class", "sws-intro-text");
        b.AddContent(i++, "🜂 多功能AI搜索：AnySearch家族 + AI总结(高性能) + 智能搜索生成(标准) + 双引擎搜索 + 百度热搜 + 智能识图 + 图片出处搜索\n🜂 AnySearch 免Key即用：通用/垂直搜索、批量搜索、网页正文提取、垂直领域目录\n🜂 图片出处搜索（独立开关）：番剧截图→作品+集数+时间点；插画/同人本→画师+作品名+原图链接；通用图/游戏截图→网页出处(Yandex)\n🜂 每引擎支持 4 组 API Key（AnySearch 可留空走匿名），额度耗尽自动轮换\n🜂 修改配置后需重新加载模块（设置→插件→刷新）");
        b.CloseElement();
        b.CloseElement();

        // === 引擎模式 ===
        int tavilyCount = CountKeys(Configuration.TavilyApiKey1, Configuration.TavilyApiKey2, Configuration.TavilyApiKey3, Configuration.TavilyApiKey4);
        int baiduCount = CountKeys(Configuration.BaiduApiKey1, Configuration.BaiduApiKey2, Configuration.BaiduApiKey3, Configuration.BaiduApiKey4);
        int anysearchCount = CountKeys(Configuration.AnySearchApiKey);

        RenderSection(b, ref i, "⚹", "引擎模式", null, () =>
        {
            AddSelectField(b, ref i, "搜索引擎模式", Configuration.Engine,
                v => Configuration.Engine = v,
                new[] {
                    ("anysearch", "anysearch · 仅 AnySearch（免Key，默认）"),
                    ("auto", "auto · 智能路由（多渠道同时开启时）"),
                    ("tavily", "tavily · 仅 Tavily"),
                    ("baidu", "baidu · 仅百度"),
                });
            AddHint(b, ref i, "单引擎模式只注入/启用该引擎的工具，其他引擎的工具不显示也不可调（省 token）；auto 多渠道时全部工具可用\n\nanysearch：仅 AnySearch（免Key，默认）\nauto：智能路由（多渠道同时开启时）\ntavily：仅 Tavily\nbaidu：仅百度");
        });

        // === AnySearch 配置 ===
        RenderSection(b, ref i, "✧", "AnySearch 配置", anysearchCount, () =>
        {
            AddHint(b, ref i, "免Key匿名即可用（速率/配额较低），开箱即用；注册免费Key提额：https://anysearch.com/console/api-keys\nKey 格式 as_sk_xxx，认证方式 Bearer Token（留空走免Key匿名模式）\n\n能力：AnySearch(通用+垂直搜索, tag+params) / AnySearchBatchSearch(并行批量1-5查询) / ExtractWebpage(网页正文转Markdown) / GetSubDomains(垂直领域目录)");
            AddPassword(b, ref i, "AnySearch API Key（可留空，走免Key匿名模式）", Configuration.AnySearchApiKey, v => Configuration.AnySearchApiKey = v);
        });

        // === Tavily 配置 ===
        RenderSection(b, ref i, "✦", "Tavily 配置", tavilyCount, () =>
        {
            AddHint(b, ref i, "Tavily API Key 格式 tvly-xxxxx，在 https://app.tavily.com 免费注册（每月1000次额度）\n认证方式：Bearer Token");
            AddPassword(b, ref i, "Tavily Key 1（主账号）", Configuration.TavilyApiKey1, v => Configuration.TavilyApiKey1 = v);
            AddHint(b, ref i, "默认使用此账号，额度耗尽后自动切换到下一个");
            AddCollapsibleGroup(b, ref i, "Tavily Key 2（备用）", () => AddPassword(b, ref i, "Tavily Key 2", Configuration.TavilyApiKey2 ?? "", v => Configuration.TavilyApiKey2 = v));
            AddCollapsibleGroup(b, ref i, "Tavily Key 3（备用）", () => AddPassword(b, ref i, "Tavily Key 3", Configuration.TavilyApiKey3 ?? "", v => Configuration.TavilyApiKey3 = v));
            AddCollapsibleGroup(b, ref i, "Tavily Key 4（备用）", () => AddPassword(b, ref i, "Tavily Key 4", Configuration.TavilyApiKey4 ?? "", v => Configuration.TavilyApiKey4 = v));
        });

        // === 百度配置 ===
        RenderSection(b, ref i, "☾", "百度千帆 配置", baiduCount, () =>
        {
            AddHint(b, ref i, "百度千帆 API Key 创建地址：https://console.bce.baidu.com/qianfan/ais/console/apiKey\n可不开后付费、不实名，注册即可白嫖每日免费额度\n⚠️ 百度的后付费最好别开，否则可能导致欠款\n免费额度：AI总结/智能搜索 100次/天，普通搜索 50次/天，识图 100次/天，热搜 10次/天\n认证方式：Bearer Token");
            AddPassword(b, ref i, "百度 Key 1（主账号）", Configuration.BaiduApiKey1, v => Configuration.BaiduApiKey1 = v);
            AddHint(b, ref i, "默认使用此账号，额度耗尽后自动切换到下一个");
            AddCollapsibleGroup(b, ref i, "百度 Key 2（备用）", () => AddPassword(b, ref i, "百度 Key 2", Configuration.BaiduApiKey2 ?? "", v => Configuration.BaiduApiKey2 = v));
            AddCollapsibleGroup(b, ref i, "百度 Key 3（备用）", () => AddPassword(b, ref i, "百度 Key 3", Configuration.BaiduApiKey3 ?? "", v => Configuration.BaiduApiKey3 = v));
            AddCollapsibleGroup(b, ref i, "百度 Key 4（备用）", () => AddPassword(b, ref i, "百度 Key 4", Configuration.BaiduApiKey4 ?? "", v => Configuration.BaiduApiKey4 = v));
        });

        // === 图片出处搜索 ===
        int sourceKeyCount = CountKeys(Configuration.SauceNaoApiKey1, Configuration.SauceNaoApiKey2, Configuration.TraceMoeApiKey1, Configuration.TraceMoeApiKey2);
        RenderSection(b, ref i, "✵", "图片出处搜索（以图搜源）", sourceKeyCount, () =>
        {
            AddToggle(b, ref i, "启用图片出处搜索（独立开关）", Configuration.EnableSourceSearch, v => Configuration.EnableSourceSearch = v);
            AddHint(b, ref i, "以图搜源：插画/同人本/本子→画师名+作品名+原图链接(SauceNAO)；番剧/动画截图→作品名+集数+时间点(trace.moe)；游戏截图/照片/通用图→相似网页出处(Yandex)\n不传 engine = SauceNAO×Yandex 双引擎并行交叉验证（两边一致的作品/角色/画师信息更可靠），番剧图自动补查 trace.moe\nYandex 为网页接口（无官方API）：自动维护 cookie 会话 + 10秒最小间隔限流防验证码，仅支持图片URL方式\n关闭后 SearchSource 工具不再注入提示词（省 token），重新加载模块后生效");
            AddHint(b, ref i, "SauceNAO 提供画师名/作品名等详细出处信息（插画/同人本/本子首选引擎）\n免Key也能用：自动走网页匿名模式（配额低：约4次/30秒、100次/天）\n免费注册提额（约200次/天，API更稳定）：https://saucenao.com 登录后在 user.php?page=search-api 获取 Key");
            AddPassword(b, ref i, "SauceNAO API Key 1", Configuration.SauceNaoApiKey1, v => Configuration.SauceNaoApiKey1 = v);
            AddCollapsibleGroup(b, ref i, "SauceNAO API Key 2（备用）", () => AddPassword(b, ref i, "SauceNAO API Key 2", Configuration.SauceNaoApiKey2 ?? "", v => Configuration.SauceNaoApiKey2 = v));
            AddHint(b, ref i, "trace.moe 免Key匿名即可用（配额较低）；Token 可提额\n认证方式：x-trace-token 请求头");
            AddPassword(b, ref i, "trace.moe Token 1（可留空走匿名）", Configuration.TraceMoeApiKey1, v => Configuration.TraceMoeApiKey1 = v);
            AddCollapsibleGroup(b, ref i, "trace.moe Token 2（备用）", () => AddPassword(b, ref i, "trace.moe Token 2", Configuration.TraceMoeApiKey2 ?? "", v => Configuration.TraceMoeApiKey2 = v));
        });

        // === 搜索设置 ===
        RenderSection(b, ref i, "◈", "搜索设置", null, () =>
        {
            AddInput(b, ref i, "默认返回结果数量", Configuration.MaxResults.ToString(), v =>
            {
                if (int.TryParse(v, out var n))
                    Configuration.MaxResults = Math.Clamp(n, 1, 20);
            });
            AddHint(b, ref i, "每次搜索返回的网页结果条数，默认 5，范围 1~20。AI 调用时也可动态指定");
            AddSelectField(b, ref i, "默认搜索深度", Configuration.SearchDepth, v => Configuration.SearchDepth = v,
                new[] {
                    ("basic", "basic · 快速（Tavily 1额度 / 百度 lite版）"),
                    ("advanced", "advanced · 深度（Tavily 2额度 / 百度 standard版）"),
                });
            AddHint(b, ref i, "深度搜索更精准但更慢，消耗更多额度");
        });

        // === AI 搜索设置 ===
        RenderSection(b, ref i, "⛤", "AI搜索设置", null, () =>
        {
            AddHint(b, ref i, "AI总结搜索(高性能版)和智能搜索生成(标准版)的模型配置\n高性能版：搜索+大模型总结一步到位，免费100次/日\n标准版：功能最全面，支持深度搜索、知识注入、追问");
            AddInput(b, ref i, "高性能版模型", Configuration.SummaryModel, v => Configuration.SummaryModel = v);
            AddHint(b, ref i, "auto_thinking(自动思考，推荐) / thinking / non_thinking");
            AddInput(b, ref i, "智能搜索生成模型(标准版)", Configuration.ChatSearchModel, v => Configuration.ChatSearchModel = v);
            AddHint(b, ref i, "deepseek-v3.2(推荐) / deepseek-r1 / ernie-4.5-turbo-32k 等");
            AddToggle(b, ref i, "启用深度搜索（智能搜索生成）", Configuration.EnableDeepSearch, v => Configuration.EnableDeepSearch = v);
            AddHint(b, ref i, "启用后智能搜索生成会更精准但更慢\n⚠️ 每次深度搜索会花费较多额度，请谨慎使用");
        });

        // === 缓存设置 ===
        RenderSection(b, ref i, "⥁", "缓存设置", null, () =>
        {
            AddToggle(b, ref i, "启用搜索缓存", Configuration.EnableCache, v => Configuration.EnableCache = v);
            AddHint(b, ref i, "启用后，相同查询在缓存有效期内不重复调用API，节省额度");
            AddInput(b, ref i, "缓存过期时间（分钟）", Configuration.CacheTtlMinutes.ToString(), v =>
            {
                if (int.TryParse(v, out var n))
                    Configuration.CacheTtlMinutes = Math.Clamp(n, 1, 60);
            });
            AddHint(b, ref i, "缓存结果的保留时间，默认 5 分钟");
        });

        // === 注入设置 ===
        RenderSection(b, ref i, "⇲", "注入设置", null, () =>
        {
            AddToggle(b, ref i, "隐式注入（省 token）", Configuration.ImplicitInjection, v => Configuration.ImplicitInjection = v);
            AddHint(b, ref i, "开启后函数文档不直接注入系统提示词，AI 需先调用 <smartwebsearch/> 按需加载（省 token，渐进式）；关闭则为显式注入（默认，功能说明直接可用）。改动需重载模块后生效");
        });

        b.CloseElement();
    }

    // === 渲染辅助 ===

    static int CountKeys(params string?[] keys)
    {
        int n = 0;
        foreach (var k in keys) if (!string.IsNullOrWhiteSpace(k)) n++;
        return n;
    }

    void RenderSection(RenderTreeBuilder b, ref int seq, string icon, string title,
        int? badgeCount, Action renderContent)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "class", "sws-section");

        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "class", "sws-section-title");

        b.OpenElement(seq++, "span");
        b.AddAttribute(seq++, "class", "sws-section-icon");
        b.AddContent(seq++, icon);
        b.CloseElement();

        b.AddContent(seq++, title);

        if (badgeCount.HasValue)
        {
            b.OpenElement(seq++, "span");
            b.AddAttribute(seq++, "class", badgeCount.Value > 0 ? "sws-badge sws-badge-on" : "sws-badge sws-badge-off");
            b.AddContent(seq++, badgeCount.Value > 0
                ? $"已启用 {badgeCount.Value}/4"
                : "未配置");
            b.CloseElement();
        }

        b.CloseElement();

        renderContent();
        b.CloseElement();
    }

    void AddSelectField(RenderTreeBuilder b, ref int seq, string label, string value,
        Action<string> setter, (string val, string text)[] options)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "class", "sws-field");

        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "class", "sws-label");
        b.AddContent(seq++, label);
        b.CloseElement();

        b.OpenElement(seq++, "select");
        b.AddAttribute(seq++, "class", "sws-select");
        b.AddAttribute(seq++, "value", value);
        b.AddAttribute(seq++, "onchange",
            EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                setter(e.Value?.ToString() ?? "")));

        foreach (var (val, text) in options)
        {
            b.OpenElement(seq++, "option");
            b.AddAttribute(seq++, "value", val);
            if (val == value) b.AddAttribute(seq++, "selected", true);
            b.AddContent(seq++, text);
            b.CloseElement();
        }
        b.CloseElement();

        b.CloseElement();
    }

    void AddInput(RenderTreeBuilder b, ref int seq, string label, string value, Action<string> setter)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "class", "sws-field");

        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "class", "sws-label");
        b.AddContent(seq++, label);
        b.CloseElement();

        b.OpenComponent<Input<string>>(seq++);
        b.AddAttribute(seq++, "Value", value);
        b.AddAttribute(seq++, "ValueChanged",
            EventCallback.Factory.Create<string>(this, setter));
        b.CloseComponent();

        b.CloseElement();
    }

    void AddPassword(RenderTreeBuilder b, ref int seq, string label, string value, Action<string> setter)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "class", "sws-field");

        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "class", "sws-label");
        b.AddContent(seq++, label);
        b.CloseElement();

        b.OpenComponent<InputPassword>(seq++);
        b.AddAttribute(seq++, "Value", value);
        b.AddAttribute(seq++, "ValueChanged",
            EventCallback.Factory.Create<string>(this, setter));
        b.CloseComponent();

        b.CloseElement();
    }

    void AddToggle(RenderTreeBuilder b, ref int seq, string label, bool value, Action<bool> setter)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "class", "sws-toggle");

        b.OpenElement(seq++, "input");
        b.AddAttribute(seq++, "type", "checkbox");
        b.AddAttribute(seq++, "checked", value);
        b.AddAttribute(seq++, "onchange",
            EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                setter(e.Value is true)));
        b.CloseElement();

        b.OpenElement(seq++, "span");
        b.AddAttribute(seq++, "class", "sws-toggle-label");
        b.AddContent(seq++, label);
        b.CloseElement();

        b.CloseElement();
    }

    void AddHint(RenderTreeBuilder b, ref int seq, string text)
    {
        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "class", "sws-hint");
        b.AddContent(seq++, text);
        b.CloseElement();
    }

    void AddCollapsibleGroup(RenderTreeBuilder b, ref int seq, string title, Action renderContent)
    {
        b.OpenElement(seq++, "details");
        b.AddAttribute(seq++, "class", "sws-details");

        b.OpenElement(seq++, "summary");
        b.OpenElement(seq++, "span");
        b.AddAttribute(seq++, "class", "sws-arrow");
        b.AddContent(seq++, "▶");
        b.CloseElement();
        b.AddContent(seq++, " " + title);
        b.CloseElement();

        b.OpenElement(seq++, "div");
        b.AddAttribute(seq++, "class", "sws-details-content");
        renderContent();
        b.CloseElement();

        b.CloseElement();
    }
}
