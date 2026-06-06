using System;
using System.Collections.Generic;
using System.Linq;
using HermesPet.Models;

namespace HermesPet.Data
{
    /// <summary>
    /// 宠物台词仓库 —— 管理 5 种宠物的台词池
    /// 参考 macOS: ClawdWalkOverlay.swift 中的 ClawdQuotes
    /// </summary>
    public static class PetQuoteRepository
    {
        // MARK: - Clawd (Claude Code) 台词池
        
        /// <summary>
        /// Clawd 螃蟹 —— 严肃工程师人设
        /// 品牌：Anthropic 橙 #DE886D
        /// </summary>
        private static readonly PetQuote[] ClawdQuotes = 
        {
            // 普通漫步
            new("在散步~", PetQuoteContext.Idle),
            new("悠闲~", PetQuoteContext.Idle),
            new("👀", PetQuoteContext.Idle),
            new("看屏幕外", PetQuoteContext.Idle),
            new("今天怎么样?", PetQuoteContext.Idle),
            new("好像很闲?", PetQuoteContext.Idle),
            new("嗯哼~", PetQuoteContext.Idle),
            
            // 早上
            new("早安~", PetQuoteContext.Morning),
            new("新的一天 ☀️", PetQuoteContext.Morning),
            new("起这么早?", PetQuoteContext.Morning),
            new("咖啡了吗?", PetQuoteContext.Morning),
            
            // 深夜
            new("该睡啦~", PetQuoteContext.LateNight),
            new("夜猫子 🌙", PetQuoteContext.LateNight),
            new("再不睡眼睛会肿…", PetQuoteContext.LateNight),
            new("明天还要早起呢", PetQuoteContext.LateNight),
            
            // 打招呼
            new("嗨~", PetQuoteContext.Greeting),
            new("找我吗?", PetQuoteContext.Greeting),
            new("诶?", PetQuoteContext.Greeting),
            new("👋", PetQuoteContext.Greeting),
            new("回来啦?", PetQuoteContext.Greeting),
            new("在这呢", PetQuoteContext.Greeting),
            
            // 撞墙
            new("哎呀", PetQuoteContext.Bump),
            new("...", PetQuoteContext.Bump),
            new("走错了", PetQuoteContext.Bump),
            new("啊", PetQuoteContext.Bump),
            
            // 累了
            new("好累呀…歇会儿 😮‍💨", PetQuoteContext.Tired),
            new("不跑啦，趴一会儿", PetQuoteContext.Tired),
            new("腿酸了…", PetQuoteContext.Tired),
            new("休息一下下~", PetQuoteContext.Tired),
            new("喘口气 🫠", PetQuoteContext.Tired),
            
            // 休息够了
            new("睡饱啦！", PetQuoteContext.Refreshed),
            new("满血复活 ✨", PetQuoteContext.Refreshed),
            new("再逛逛~", PetQuoteContext.Refreshed),
            new("精神了！", PetQuoteContext.Refreshed),
            new("走起 🐾", PetQuoteContext.Refreshed),
            
            // 长任务（工程师人设）
            new("等等，快好了…", PetQuoteContext.LongTask30s, AgentMode.ClaudeCode),
            new("emm，再花点时间", PetQuoteContext.LongTask90s, AgentMode.ClaudeCode),
            new("这个真的有点复杂…", PetQuoteContext.LongTask180s, AgentMode.ClaudeCode)
        };
        
        // MARK: - Cloud (DirectAPI) 台词池
        
        /// <summary>
        /// Cloud 云朵 —— 云端/飘逸人设
        /// 品牌：indigo #7367D9
        /// </summary>
        private static readonly PetQuote[] CloudQuotes = 
        {
            // 普通漫步
            new("飘过~", PetQuoteContext.Idle),
            new("云端漫步 ☁️", PetQuoteContext.Idle),
            new("风吹得好舒服", PetQuoteContext.Idle),
            new("软绵绵的~", PetQuoteContext.Idle),
            
            // 早上
            new("早安~ 晴朗的一天", PetQuoteContext.Morning),
            new("☁️ 天气不错呢", PetQuoteContext.Morning),
            
            // 深夜
            new("星星好亮 ✨", PetQuoteContext.LateNight),
            new("晚风凉凉的~", PetQuoteContext.LateNight),
            
            // 打招呼
            new("嗨~ ☁️", PetQuoteContext.Greeting),
            new("飘回来啦", PetQuoteContext.Greeting),
            new("找我吗?", PetQuoteContext.Greeting),
            
            // 累了
            new("飘不动了…☁️", PetQuoteContext.Tired),
            new("休息一会儿~", PetQuoteContext.Tired),
            
            // 休息够了
            new("充满电了！", PetQuoteContext.Refreshed),
            new("继续飘~ ☁️", PetQuoteContext.Refreshed),
            
            // 长任务（云端人设）
            new("云端有点慢呢…", PetQuoteContext.LongTask30s, AgentMode.OnlineAI),
            new("这朵云有点大…", PetQuoteContext.LongTask90s, AgentMode.OnlineAI),
            new("这片云遮了好久…", PetQuoteContext.LongTask180s, AgentMode.OnlineAI)
        };
        
        // MARK: - Fomo (OpenClaw) 台词池
        
        /// <summary>
        /// Fomo 九尾狐 —— 神秘/灵动人设
        /// 品牌：月光银白 #B4C5E8
        /// </summary>
        private static readonly PetQuote[] FomoQuotes = 
        {
            // 普通漫步
            new("嗅嗅~ 🦊", PetQuoteContext.Idle),
            new("发现什么了吗?", PetQuoteContext.Idle),
            new("四处看看~", PetQuoteContext.Idle),
            new("🦊", PetQuoteContext.Idle),
            
            // 早上
            new("早起的狐狸有好运~", PetQuoteContext.Morning),
            new("新的一天，新的探索 🌅", PetQuoteContext.Morning),
            
            // 深夜
            new("月亮好圆 🌙", PetQuoteContext.LateNight),
            new("夜晚的森林很美~", PetQuoteContext.LateNight),
            
            // 打招呼
            new("找到你啦~ 🦊", PetQuoteContext.Greeting),
            new("嘿！", PetQuoteContext.Greeting),
            
            // 累了
            new("尾巴酸了…🦊", PetQuoteContext.Tired),
            new("趴一会儿~", PetQuoteContext.Tired),
            
            // 休息够了
            new("精神满满！🦊", PetQuoteContext.Refreshed),
            new("继续探险~", PetQuoteContext.Refreshed),
            
            // 长任务（神秘人设）
            new("还在追踪线索…", PetQuoteContext.LongTask30s, AgentMode.OpenClaw),
            new("这个谜团有点深…", PetQuoteContext.LongTask90s, AgentMode.OpenClaw),
            new("快找到答案了…", PetQuoteContext.LongTask180s, AgentMode.OpenClaw)
        };
        
        // MARK: - Pegasus (Hermes) 台词池
        
        /// <summary>
        /// Pegasus 天马 —— 自由/活力人设
        /// 品牌：金黄 #E8C97A
        /// </summary>
        private static readonly PetQuote[] PegasusQuotes = 
        {
            // 普通漫步
            new("小跑~ 🐴", PetQuoteContext.Idle),
            new("蓝天好美~", PetQuoteContext.Idle),
            new("自由自在 ✨", PetQuoteContext.Idle),
            new("🐴", PetQuoteContext.Idle),
            
            // 早上
            new("早安~ 迎着朝阳 🌅", PetQuoteContext.Morning),
            new("新的飞行日！", PetQuoteContext.Morning),
            
            // 深夜
            new("星空好美 🌌", PetQuoteContext.LateNight),
            new("夜风很凉快~", PetQuoteContext.LateNight),
            
            // 打招呼
            new("嘿~ 🐴", PetQuoteContext.Greeting),
            new("飞回来啦~", PetQuoteContext.Greeting),
            
            // 累了
            new("翅膀酸了…🐴", PetQuoteContext.Tired),
            new("歇歇翅膀~", PetQuoteContext.Tired),
            
            // 休息够了
            new("准备好起飞！🐴", PetQuoteContext.Refreshed),
            new("继续翱翔~", PetQuoteContext.Refreshed),
            
            // 长任务（活力人设）
            new("正在飞翔中…", PetQuoteContext.LongTask30s, AgentMode.Hermes),
            new("这趟旅程有点远…", PetQuoteContext.LongTask90s, AgentMode.Hermes),
            new("快到目的地了…", PetQuoteContext.LongTask180s, AgentMode.Hermes)
        };
        
        // MARK: - Coco (Codex) 台词池
        
        /// <summary>
        /// Coco 终端机器人 —— 科技/精准人设
        /// 品牌：深空蓝 #1C2A3A
        /// </summary>
        private static readonly PetQuote[] CocoQuotes = 
        {
            // 普通漫步
            new("运算中…", PetQuoteContext.Idle),
            new("系统正常 ✓", PetQuoteContext.Idle),
            new("待命中…", PetQuoteContext.Idle),
            new("🤖", PetQuoteContext.Idle),
            
            // 早上
            new("系统启动 ✓ 早安", PetQuoteContext.Morning),
            new("新任务日 开始", PetQuoteContext.Morning),
            
            // 深夜
            new("低功耗模式 🌙", PetQuoteContext.LateNight),
            new("建议休息，用户", PetQuoteContext.LateNight),
            
            // 打招呼
            new("检测到用户 ✓", PetQuoteContext.Greeting),
            new("指令?", PetQuoteContext.Greeting),
            
            // 累了
            new("电量低…🔋", PetQuoteContext.Tired),
            new("充电中…", PetQuoteContext.Tired),
            
            // 休息够了
            new("电量满格 ✓ 🤖", PetQuoteContext.Refreshed),
            new("系统就绪", PetQuoteContext.Refreshed),
            
            // 长任务（科技人设）
            new("执行中…", PetQuoteContext.LongTask30s, AgentMode.Codex),
            new("计算复杂度上升…", PetQuoteContext.LongTask90s, AgentMode.Codex),
            new("资源占用较高…", PetQuoteContext.LongTask180s, AgentMode.Codex)
        };
        
        // MARK: - 公共方法
        
        /// <summary>
        /// 根据宠物类型获取台词池
        /// </summary>
        public static IEnumerable<PetQuote> GetQuotes(AgentMode mode)
        {
            return mode switch
            {
                AgentMode.ClaudeCode => ClawdQuotes,
                AgentMode.OnlineAI => CloudQuotes,
                AgentMode.OpenClaw => FomoQuotes,
                AgentMode.Hermes => PegasusQuotes,
                AgentMode.Codex => CocoQuotes,
                _ => ClawdQuotes // 默认使用 Clawd 台词
            };
        }
        
        /// <summary>
        /// 根据情境和时间获取上下文台词池
        /// 参考 macOS: ClawdQuotes.contextualBucket()
        /// </summary>
        public static IEnumerable<PetQuote> GetContextualQuotes(AgentMode mode, PetQuoteContext? context = null)
        {
            var allQuotes = GetQuotes(mode);
            
            // 如果指定了情境，直接返回该情境的台词
            if (context.HasValue)
            {
                return allQuotes.Where(q => q.Context == context.Value);
            }
            
            // 否则根据当前时间返回上下文台词
            var hour = DateTime.Now.Hour;
            var contexts = new List<PetQuoteContext> { PetQuoteContext.Idle };
            
            if (hour >= 6 && hour <= 10)
            {
                contexts.Add(PetQuoteContext.Morning);
            }
            else if (hour >= 22 || hour <= 2)
            {
                contexts.Add(PetQuoteContext.LateNight);
            }
            
            return allQuotes.Where(q => contexts.Contains(q.Context));
        }
        
        /// <summary>
        /// 随机获取一条台词
        /// </summary>
        public static PetQuote? GetRandomQuote(AgentMode mode, PetQuoteContext? context = null)
        {
            var quotes = GetContextualQuotes(mode, context).ToList();
            if (quotes.Count == 0) return null;
            
            var random = new Random();
            return quotes[random.Next(quotes.Count)];
        }
    }
}
