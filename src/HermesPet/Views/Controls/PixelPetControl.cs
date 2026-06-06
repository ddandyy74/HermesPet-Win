using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HermesPet.ViewModels;

namespace HermesPet.Views.Controls
{
    /// <summary>
    /// 像素宠物控件
    /// 使用 WriteableBitmap 实现帧动画
    /// 
    /// 设计要点：
    /// - 使用 WriteableBitmap 作为渲染表面
    /// - 每帧使用代码绘制像素矩形
    /// - 帧率 ~6.67 FPS（150ms/帧）
    /// - 支持 5 个宠物角色
    /// 
    /// 参考 macOS: FomoSprite.swift, ModeSprite.swift
    /// 
    /// 关键约束：
    /// - 性能 P0: 图片使用 BitmapImage.Freeze()
    /// - 使用 DispatcherTimer 驱动动画
    /// </summary>
    public class PixelPetControl : System.Windows.Controls.Control
    {
        // ========== 依赖属性 ==========
        
        /// <summary>
        /// 宠物类型
        /// </summary>
        public static readonly DependencyProperty PetTypeProperty =
            DependencyProperty.Register(
                nameof(PetType),
                typeof(PetType),
                typeof(PixelPetControl),
                new PropertyMetadata(PetType.Fomo, OnPetTypeChanged));
        
        /// <summary>
        /// 宠物姿势
        /// </summary>
        public static readonly DependencyProperty PoseProperty =
            DependencyProperty.Register(
                nameof(Pose),
                typeof(PetPose),
                typeof(PixelPetControl),
                new PropertyMetadata(PetPose.Rest, OnPoseChanged));
        
        /// <summary>
        /// 是否正在行走
        /// </summary>
        public static readonly DependencyProperty IsWalkingProperty =
            DependencyProperty.Register(
                nameof(IsWalking),
                typeof(bool),
                typeof(PixelPetControl),
                new PropertyMetadata(false, OnIsWalkingChanged));
        
        /// <summary>
        /// 是否启用动画
        /// </summary>
        public static readonly DependencyProperty IsAnimatedProperty =
            DependencyProperty.Register(
                nameof(IsAnimated),
                typeof(bool),
                typeof(PixelPetControl),
                new PropertyMetadata(true, OnIsAnimatedChanged));
        
        // ========== 属性访问器 ==========
        
        public PetType PetType
        {
            get => (PetType)GetValue(PetTypeProperty);
            set => SetValue(PetTypeProperty, value);
        }
        
        public PetPose Pose
        {
            get => (PetPose)GetValue(PoseProperty);
            set => SetValue(PoseProperty, value);
        }
        
        public bool IsWalking
        {
            get => (bool)GetValue(IsWalkingProperty);
            set => SetValue(IsWalkingProperty, value);
        }
        
        public bool IsAnimated
        {
            get => (bool)GetValue(IsAnimatedProperty);
            set => SetValue(IsAnimatedProperty, value);
        }
        
        // ========== 私有字段 ==========
        
        private WriteableBitmap? _bitmap;
        private DispatcherTimer? _animationTimer;
        private int _frameCount;
        private double _animationTime;
        
        // 帧率：~6.67 FPS = 150ms/帧
        private const double FrameInterval = 150.0 / 1000.0; // 秒
        
        // 宠物尺寸（viewBox 14×10）
        private const int ViewBoxWidth = 14;
        private const int ViewBoxHeight = 10;
        
        // ========== 构造函数 ==========
        
        static PixelPetControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(PixelPetControl),
                new FrameworkPropertyMetadata(typeof(PixelPetControl)));
        }
        
        public PixelPetControl()
        {
            // 初始化位图
            _bitmap = new WriteableBitmap(ViewBoxWidth * 10, ViewBoxHeight * 10, 96, 96, PixelFormats.Bgra32, null);
            
            // 初始化动画定时器
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(FrameInterval)
            };
            _animationTimer.Tick += OnAnimationTimerTick;
            
            // 初始渲染
            RenderFrame();
        }
        
        // ========== 属性变化处理 ==========
        
        private static void OnPetTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PixelPetControl control)
            {
                control.RenderFrame();
            }
        }
        
        private static void OnPoseChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PixelPetControl control)
            {
                control.RenderFrame();
            }
        }
        
        private static void OnIsWalkingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PixelPetControl control)
            {
                control.UpdateAnimationState();
            }
        }
        
        private static void OnIsAnimatedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PixelPetControl control)
            {
                control.UpdateAnimationState();
            }
        }
        
        // ========== 动画控制 ==========
        
        private void UpdateAnimationState()
        {
            if (IsAnimated && IsWalking)
            {
                _animationTimer?.Start();
            }
            else
            {
                _animationTimer?.Stop();
            }
        }
        
        private void OnAnimationTimerTick(object? sender, EventArgs e)
        {
            _animationTime += FrameInterval;
            _frameCount++;
            
            RenderFrame();
        }
        
        // ========== 渲染逻辑 ==========
        
        /// <summary>
        /// 渲染当前帧
        /// </summary>
        private void RenderFrame()
        {
            if (_bitmap == null) return;
            
            // 确保在 UI 线程上执行
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.InvokeAsync(RenderFrame);
                return;
            }
            
            try
            {
                // 锁定位图进行写入
                _bitmap.Lock();
                
                // 清空画布
                var rect = new Int32Rect(0, 0, _bitmap.PixelWidth, _bitmap.PixelHeight);
                var clearColor = Colors.Transparent;
                _bitmap.Clear(clearColor);
                
                // 根据宠物类型渲染
                switch (PetType)
                {
                    case PetType.Clawd:
                        RenderClawd();
                        break;
                    case PetType.Cloud:
                        RenderCloud();
                        break;
                    case PetType.Fomo:
                        RenderFomo();
                        break;
                    case PetType.Pegasus:
                        RenderPegasus();
                        break;
                    case PetType.Coco:
                        RenderCoco();
                        break;
                }
                
                // 解锁位图
                _bitmap.Unlock();
                
                // 触发重绘
                InvalidateVisual();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RenderFrame error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 绘制像素矩形
        /// </summary>
        private void DrawRect(int x, int y, int width, int height, System.Windows.Media.Color color)
        {
            if (_bitmap == null) return;
            
            // 转换坐标（放大 10 倍）
            int scaledX = x * 10;
            int scaledY = y * 10;
            int scaledWidth = width * 10;
            int scaledHeight = height * 10;
            
            // 填充矩形
            for (int py = scaledY; py < scaledY + scaledHeight && py < _bitmap.PixelHeight; py++)
            {
                for (int px = scaledX; px < scaledX + scaledWidth && px < _bitmap.PixelWidth; px++)
                {
                    _bitmap.SetPixel(px, py, color);
                }
            }
        }
        
        // ========== 宠物渲染（简化版占位符）==========
        
        /// <summary>
        /// 渲染 Clawd（橘色小龙虾）
        /// TODO: 实现完整的像素艺术
        /// </summary>
        private void RenderClawd()
        {
            // 占位符：橘色矩形
            DrawRect(2, 2, 10, 6, Colors.Orange);
            
            // 眼睛
            DrawRect(4, 4, 1, 1, Colors.Black);
            DrawRect(9, 4, 1, 1, Colors.Black);
        }
        
        /// <summary>
        /// 渲染 Cloud（云朵小精灵）
        /// TODO: 实现完整的像素艺术
        /// </summary>
        private void RenderCloud()
        {
            // 占位符：浅蓝色矩形
            DrawRect(2, 2, 10, 6, Colors.LightBlue);
            
            // 眼睛
            DrawRect(4, 4, 1, 1, Colors.Black);
            DrawRect(9, 4, 1, 1, Colors.Black);
        }
        
        /// <summary>
        /// 渲染 Fomo（白色小狐狸）
        /// 参考 macOS: FomoSprite.swift
        /// TODO: 实现完整的像素艺术
        /// </summary>
        private void RenderFomo()
        {
            // 占位符：白色矩形
            DrawRect(2, 2, 10, 6, Colors.White);
            
            // 耳朵
            DrawRect(3, 0, 2, 2, Colors.White);
            DrawRect(9, 0, 2, 2, Colors.White);
            
            // 眼睛
            DrawRect(4, 4, 1, 1, Colors.Black);
            DrawRect(9, 4, 1, 1, Colors.Black);
            
            // 鼻子
            DrawRect(6, 5, 2, 1, Colors.Pink);
        }
        
        /// <summary>
        /// 渲染 Pegasus（飞马）
        /// TODO: 实现完整的像素艺术
        /// </summary>
        private void RenderPegasus()
        {
            // 占位符：紫色矩形
            DrawRect(2, 2, 10, 6, Colors.MediumPurple);
            
            // 翅膀
            DrawRect(0, 3, 2, 3, Colors.LightGray);
            DrawRect(12, 3, 2, 3, Colors.LightGray);
            
            // 眼睛
            DrawRect(4, 4, 1, 1, Colors.Black);
            DrawRect(9, 4, 1, 1, Colors.Black);
        }
        
        /// <summary>
        /// 渲染 Coco（代码终端精灵）
        /// TODO: 实现完整的像素艺术
        /// </summary>
        private void RenderCoco()
        {
            // 占位符：深蓝色矩形
            DrawRect(2, 2, 10, 6, System.Windows.Media.Color.FromRgb(30, 30, 50));
            
            // 终端光标
            DrawRect(4, 4, 2, 1, Colors.LimeGreen);
        }
        
        // ========== 重写方法 ==========
        
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            
            if (_bitmap != null)
            {
                // 计算渲染位置（居中）
                var x = (ActualWidth - _bitmap.Width) / 2;
                var y = (ActualHeight - _bitmap.Height) / 2;
                
                // 绘制位图
                drawingContext.DrawImage(_bitmap, new Rect(x, y, _bitmap.Width, _bitmap.Height));
            }
        }
        
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            
            // 启动动画
            if (IsAnimated && IsWalking)
            {
                _animationTimer?.Start();
            }
            
            // 订阅卸载事件
            Unloaded += OnUnloaded;
        }
        
        // ========== 清理资源 ==========
        
        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            // 停止定时器
            _animationTimer?.Stop();
            _animationTimer = null;
        }
    }
    
    // ========== 辅助扩展方法 ==========
    
    /// <summary>
    /// WriteableBitmap 扩展方法
    /// </summary>
    internal static class WriteableBitmapExtensions
    {
        /// <summary>
        /// 清空位图
        /// </summary>
        public static void Clear(this WriteableBitmap bitmap, System.Windows.Media.Color color)
        {
            var rect = new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);
            var colorValue = BitConverter.ToInt32(new byte[] { color.B, color.G, color.R, color.A }, 0);
            
            unsafe
            {
                var ptr = bitmap.BackBuffer;
                var stride = bitmap.BackBufferStride;
                
                for (int y = 0; y < bitmap.PixelHeight; y++)
                {
                    var rowPtr = ptr + y * stride;
                    for (int x = 0; x < bitmap.PixelWidth; x++)
                    {
                        *((int*)rowPtr + x) = colorValue;
                    }
                }
            }
        }
        
        /// <summary>
        /// 设置单个像素
        /// </summary>
        public static void SetPixel(this WriteableBitmap bitmap, int x, int y, System.Windows.Media.Color color)
        {
            if (x < 0 || x >= bitmap.PixelWidth || y < 0 || y >= bitmap.PixelHeight)
                return;
            
            unsafe
            {
                var ptr = bitmap.BackBuffer + y * bitmap.BackBufferStride + x * 4;
                *((int*)ptr) = BitConverter.ToInt32(new byte[] { color.B, color.G, color.R, color.A }, 0);
            }
        }
    }
}
