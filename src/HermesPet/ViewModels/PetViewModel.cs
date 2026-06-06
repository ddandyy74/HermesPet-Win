using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HermesPet.ViewModels
{
    /// <summary>
    /// 宠物角色类型
    /// 对应 5 个不同的宠物角色
    /// </summary>
    public enum PetType
    {
        Clawd,      // Claude - 橘色小龙虾
        Cloud,      // DirectAPI - 云朵小精灵
        Fomo,       // OpenClaw - 白色小狐狸
        Pegasus,    // Hermes - 飞马
        Coco        // Codex - 代码终端精灵
    }
    
    /// <summary>
    /// 宠物姿势/动作
    /// 参考 macOS: ClawdPose
    /// </summary>
    public enum PetPose
    {
        Rest,       // 空闲
        Walk,       // 行走
        LookLeft,   // 向左看
        LookRight,  // 向右看
        ArmsUp,     // 举手（惊吓/求救）
        Talk        // 说话
    }
    
    /// <summary>
    /// 宠物状态
    /// </summary>
    public enum PetState
    {
        Idle,       // 空闲
        Working,    // 工作中（AI 生成中）
        Walking,    // 行走中
        Talking     // 说话中
    }
    
    /// <summary>
    /// 宠物 ViewModel
    /// 管理宠物的状态、动作、角色切换
    /// 
    /// 参考 macOS: FomoSprite.swift, ModeSprite.swift
    /// </summary>
    public partial class PetViewModel : ObservableObject
    {
        // ========== 宠物属性 ==========
        
        /// <summary>
        /// 当前宠物角色
        /// </summary>
        [ObservableProperty]
        private PetType _currentPetType = PetType.Fomo;
        
        /// <summary>
        /// 当前姿势
        /// </summary>
        [ObservableProperty]
        private PetPose _currentPose = PetPose.Rest;
        
        /// <summary>
        /// 当前状态
        /// </summary>
        [ObservableProperty]
        private PetState _currentState = PetState.Idle;
        
        /// <summary>
        /// 是否正在行走
        /// </summary>
        [ObservableProperty]
        private bool _isWalking;
        
        /// <summary>
        /// 是否正在工作（AI 生成中）
        /// </summary>
        [ObservableProperty]
        private bool _isWorking;
        
        // ========== 窗口属性 ==========
        
        /// <summary>
        /// 窗口位置
        /// </summary>
        [ObservableProperty]
        private System.Windows.Point _windowPosition = new System.Windows.Point(100, 100);
        
        /// <summary>
        /// 是否允许点击穿透
        /// </summary>
        [ObservableProperty]
        private bool _isClickThrough;
        
        // ========== 台词属性（M2.4 实现）==========
        
        /// <summary>
        /// 当前台词
        /// </summary>
        [ObservableProperty]
        private string _currentSpeech = string.Empty;
        
        /// <summary>
        /// 是否显示台词气泡
        /// </summary>
        [ObservableProperty]
        private bool _showSpeechBubble;
        
        // ========== 辅助属性 ==========
        
        /// <summary>
        /// 当前宠物名称（用于显示）
        /// </summary>
        public string CurrentPetName => CurrentPetType switch
        {
            PetType.Clawd => "Clawd",
            PetType.Cloud => "Cloud",
            PetType.Fomo => "Fomo",
            PetType.Pegasus => "Pegasus",
            PetType.Coco => "Coco",
            _ => "Unknown"
        };
        
        public PetViewModel()
        {
            // 初始化默认状态
            CurrentPetType = PetType.Fomo;
            CurrentPose = PetPose.Rest;
            CurrentState = PetState.Idle;
            IsWalking = false;
            IsWorking = false;
            IsClickThrough = false;
        }
        
        // ========== 命令 ==========
        
        /// <summary>
        /// 切换到下一个宠物
        /// </summary>
        [RelayCommand]
        private void NextPet()
        {
            var nextIndex = ((int)CurrentPetType + 1) % 5;
            CurrentPetType = (PetType)nextIndex;
            OnPropertyChanged(nameof(CurrentPetName));
        }
        
        /// <summary>
        /// 切换到上一个宠物
        /// </summary>
        [RelayCommand]
        private void PreviousPet()
        {
            var prevIndex = ((int)CurrentPetType + 4) % 5; // +4 ≡ -1 mod 5
            CurrentPetType = (PetType)prevIndex;
            OnPropertyChanged(nameof(CurrentPetName));
        }
        
        /// <summary>
        /// 设置宠物姿势
        /// </summary>
        [RelayCommand]
        private void SetPose(PetPose pose)
        {
            CurrentPose = pose;
        }
        
        /// <summary>
        /// 开始行走
        /// </summary>
        [RelayCommand]
        private void StartWalking()
        {
            IsWalking = true;
            CurrentState = PetState.Walking;
        }
        
        /// <summary>
        /// 停止行走
        /// </summary>
        [RelayCommand]
        private void StopWalking()
        {
            IsWalking = false;
            CurrentState = PetState.Idle;
        }
        
        /// <summary>
        /// 开始工作（AI 生成中）
        /// </summary>
        [RelayCommand]
        private void StartWorking()
        {
            IsWorking = true;
            CurrentState = PetState.Working;
            CurrentPose = PetPose.ArmsUp; // 工作时举手姿势
        }
        
        /// <summary>
        /// 停止工作
        /// </summary>
        [RelayCommand]
        private void StopWorking()
        {
            IsWorking = false;
            CurrentState = PetState.Idle;
            CurrentPose = PetPose.Rest;
        }
        
        /// <summary>
        /// 切换点击穿透状态
        /// </summary>
        [RelayCommand]
        private void ToggleClickThrough()
        {
            IsClickThrough = !IsClickThrough;
        }
        
        // ========== 辅助方法 ==========
        
        /// <summary>
        /// 设置窗口位置
        /// </summary>
        public void SetWindowPosition(double x, double y)
        {
            WindowPosition = new System.Windows.Point(x, y);
        }
        
        /// <summary>
        /// 根据对话模式切换宠物
        /// </summary>
        public void SetPetByMode(Models.AgentMode mode)
        {
            CurrentPetType = mode switch
            {
                Models.AgentMode.ClaudeCode => PetType.Clawd,
                Models.AgentMode.Hermes => PetType.Pegasus,
                Models.AgentMode.OnlineAI => PetType.Cloud,
                Models.AgentMode.OpenClaw => PetType.Fomo,
                Models.AgentMode.Codex => PetType.Coco,
                _ => PetType.Fomo
            };
            OnPropertyChanged(nameof(CurrentPetName));
        }
    }
}
