using System;

namespace HermesPet.Models
{
    /// <summary>
    /// 宠物台词情境 —— 定义台词出现的时机
    /// </summary>
    public enum PetQuoteContext
    {
        /// <summary>普通漫步时随机显示</summary>
        Idle,
        
        /// <summary>早上 6-10 点</summary>
        Morning,
        
        /// <summary>深夜 22-2 点</summary>
        LateNight,
        
        /// <summary>鼠标靠近时的招呼</summary>
        Greeting,
        
        /// <summary>撞到屏幕边缘</summary>
        Bump,
        
        /// <summary>跑累了进入休息态</summary>
        Tired,
        
        /// <summary>休息够了起身</summary>
        Refreshed,
        
        /// <summary>长任务进行中（30s）</summary>
        LongTask30s,
        
        /// <summary>长任务进行中（90s）</summary>
        LongTask90s,
        
        /// <summary>长任务进行中（180s）</summary>
        LongTask180s
    }
    
    /// <summary>
    /// 宠物台词数据模型
    /// 参考 macOS: ClawdWalkOverlay.swift 中的 ClawdQuotes
    /// </summary>
    public class PetQuote
    {
        /// <summary>
        /// 台词文本内容
        /// </summary>
        public string Text { get; }
        
        /// <summary>
        /// 台词所属情境
        /// </summary>
        public PetQuoteContext Context { get; }
        
        /// <summary>
        /// 台词适用的 AI 模式（null 表示所有模式通用）
        /// </summary>
        public AgentMode? Mode { get; }
        
        /// <summary>
        /// 台词显示时长（秒），默认 3 秒
        /// </summary>
        public double Duration { get; }
        
        public PetQuote(string text, PetQuoteContext context, AgentMode? mode = null, double duration = 3.0)
        {
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Context = context;
            Mode = mode;
            Duration = duration;
        }
        
        public override string ToString() => $"[{Context}] {Text}";
    }
}
