using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace HermesPet.Views.Controls
{
    /// <summary>
    /// 宠物台词气泡控件
    /// 黑色背景 + 白字 + 橘色描边
    /// 
    /// 参考 macOS: ClawdWalkBubbleView
    /// </summary>
    public partial class SpeechBubbleControl : System.Windows.Controls.UserControl
    {
        // ========== 依赖属性 ==========
        
        /// <summary>
        /// 台词文本
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text),
                typeof(string),
                typeof(SpeechBubbleControl),
                new PropertyMetadata(string.Empty, OnTextChanged));
        
        /// <summary>
        /// 是否显示气泡
        /// </summary>
        public static readonly DependencyProperty BubbleVisibleProperty =
            DependencyProperty.Register(
                nameof(BubbleVisible),
                typeof(bool),
                typeof(SpeechBubbleControl),
                new PropertyMetadata(false, OnBubbleVisibleChanged));
        
        /// <summary>
        /// 描边颜色（根据宠物类型变化）
        /// </summary>
        public static readonly DependencyProperty BorderColorProperty =
            DependencyProperty.Register(
                nameof(BorderColor),
                typeof(System.Windows.Media.Color),
                typeof(SpeechBubbleControl),
                new PropertyMetadata(System.Windows.Media.Color.FromRgb(215, 120, 87), OnBorderColorChanged)); // 默认 Clawd 橘色
        
        // ========== 属性 ==========
        
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }
        
        public bool BubbleVisible
        {
            get => (bool)GetValue(BubbleVisibleProperty);
            set => SetValue(BubbleVisibleProperty, value);
        }
        
        public System.Windows.Media.Color BorderColor
        {
            get => (System.Windows.Media.Color)GetValue(BorderColorProperty);
            set => SetValue(BorderColorProperty, value);
        }
        
        // ========== 私有字段 ==========
        
        private Storyboard? _showAnimation;
        private Storyboard? _hideAnimation;
        private bool _isAnimating;
        
        // ========== 构造函数 ==========
        
        public SpeechBubbleControl()
        {
            InitializeComponent();
            
            // 设置 RenderTransform（动画需要）
            BubbleBorder.RenderTransform = new ScaleTransform(1, 1);
            BubbleBorder.RenderTransformOrigin = new System.Windows.Point(0.5, 1.0); // 底部中心为缩放原点
            
            // 加载动画资源
            _showAnimation = FindResource("ShowBubbleAnimation") as Storyboard;
            _hideAnimation = FindResource("HideBubbleAnimation") as Storyboard;
            
            if (_showAnimation != null)
            {
                _showAnimation.Completed += OnShowAnimationCompleted;
            }
            
            if (_hideAnimation != null)
            {
                _hideAnimation.Completed += OnHideAnimationCompleted;
            }
        }
        
        // ========== 属性变更回调 ==========
        
        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SpeechBubbleControl)d;
            
            // 文本为空时自动隐藏
            if (string.IsNullOrEmpty(control.Text))
            {
                control.BubbleVisible = false;
            }
        }
        
        private static void OnBubbleVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SpeechBubbleControl)d;
            var isVisible = (bool)e.NewValue;
            
            if (isVisible && !string.IsNullOrEmpty(control.Text))
            {
                control.ShowBubble();
            }
            else
            {
                control.HideBubble();
            }
        }
        
        private static void OnBorderColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (SpeechBubbleControl)d;
            var color = (System.Windows.Media.Color)e.NewValue;
            
            // 更新描边颜色（带透明度）
            control.BubbleBorder.BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromArgb(115, color.R, color.G, color.B));
        }
        
        // ========== 公共方法 ==========
        
        /// <summary>
        /// 显示气泡（带动画）
        /// </summary>
        public void ShowBubble()
        {
            if (_isAnimating || BubbleBorder.Visibility == Visibility.Visible)
                return;
            
            _isAnimating = true;
            BubbleBorder.Visibility = Visibility.Visible;
            _showAnimation?.Begin();
        }
        
        /// <summary>
        /// 隐藏气泡（带动画）
        /// </summary>
        public void HideBubble()
        {
            if (_isAnimating || BubbleBorder.Visibility == Visibility.Collapsed)
                return;
            
            _isAnimating = true;
            _hideAnimation?.Begin();
        }
        
        /// <summary>
        /// 立即显示（无动画）
        /// </summary>
        public void ShowImmediate()
        {
            _hideAnimation?.Stop();
            _showAnimation?.Stop();
            BubbleBorder.Visibility = Visibility.Visible;
            BubbleBorder.Opacity = 1;
            _isAnimating = false;
        }
        
        /// <summary>
        /// 立即隐藏（无动画）
        /// </summary>
        public void HideImmediate()
        {
            _hideAnimation?.Stop();
            _showAnimation?.Stop();
            BubbleBorder.Visibility = Visibility.Collapsed;
            BubbleBorder.Opacity = 0;
            _isAnimating = false;
        }
        
        // ========== 动画事件处理 ==========
        
        private void OnShowAnimationCompleted(object? sender, EventArgs e)
        {
            _isAnimating = false;
        }
        
        private void OnHideAnimationCompleted(object? sender, EventArgs e)
        {
            BubbleBorder.Visibility = Visibility.Collapsed;
            _isAnimating = false;
        }
        
        // ========== 清理 ==========
        
        public void Cleanup()
        {
            if (_showAnimation != null)
            {
                _showAnimation.Completed -= OnShowAnimationCompleted;
            }
            
            if (_hideAnimation != null)
            {
                _hideAnimation.Completed -= OnHideAnimationCompleted;
            }
        }
    }
}
