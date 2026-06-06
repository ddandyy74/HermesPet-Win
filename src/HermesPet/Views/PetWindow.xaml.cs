using System;
using System.Windows;
using System.Windows.Input;
using HermesPet.ViewModels;

namespace HermesPet.Views
{
    /// <summary>
    /// 宠物窗口 —— 显示桌面宠物精灵
    /// 
    /// 设计要点：
    /// - 透明窗口，无标题栏，始终置顶
    /// - 支持鼠标拖动
    /// - 支持点击穿透（IsHitTestVisible 切换）
    /// - 宠物精灵显示在中心
    /// - 台词气泡显示在上方（M2.4 实现）
    /// 
    /// 参考 macOS: PetView.swift
    /// 
    /// 关键约束：
    /// - TDR-002: 宠物交互使用 HitTest/IsHitTestVisible
    /// - 性能 P0: 图片使用 BitmapImage.Freeze()
    /// </summary>
    public partial class PetWindow : Window
    {
        private readonly PetViewModel _viewModel;
        
        /// <summary>
        /// 是否允许鼠标穿透（默认 false，宠物可交互）
        /// </summary>
        public bool IsClickThrough
        {
            get => !RootGrid.IsHitTestVisible;
            set => RootGrid.IsHitTestVisible = !value;
        }
        
        public PetWindow(PetViewModel viewModel)
        {
            InitializeComponent();
            
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            DataContext = _viewModel;
            
            // 初始化位置
            Left = 100;
            Top = 100;
            
            // 初始化交互状态
            IsClickThrough = false;
            
            // 订阅 ViewModel 事件
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            
            // 窗口关闭时清理
            Closed += OnClosed;
        }
        
        /// <summary>
        /// 宠物可以被拖动
        /// </summary>
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!IsClickThrough)
            {
                DragMove();
            }
            base.OnMouseLeftButtonDown(e);
        }
        
        /// <summary>
        /// 鼠标右键菜单（可选功能）
        /// </summary>
        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            // TODO: 显示右键菜单（切换宠物、设置等）
            base.OnMouseRightButtonUp(e);
        }
        
        /// <summary>
        /// ViewModel 属性变化处理
        /// </summary>
        private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // 确保在 UI 线程上执行
            Dispatcher.InvokeAsync(() =>
            {
                switch (e.PropertyName)
                {
                    case nameof(PetViewModel.IsClickThrough):
                        IsClickThrough = _viewModel.IsClickThrough;
                        break;
                        
                    case nameof(PetViewModel.WindowPosition):
                        Left = _viewModel.WindowPosition.X;
                        Top = _viewModel.WindowPosition.Y;
                        break;
                        
                    // TODO: 其他属性变化处理
                }
            });
        }
        
        /// <summary>
        /// 窗口关闭时清理资源
        /// </summary>
        private void OnClosed(object? sender, EventArgs e)
        {
            _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
            Closed -= OnClosed;
        }
    }
}
