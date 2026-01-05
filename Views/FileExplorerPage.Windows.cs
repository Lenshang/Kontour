#if WINDOWS
using Windows.Media.Core;
using Windows.Media.Playback;
using KontourApp.ViewModels;
using Microsoft.Maui.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;

namespace KontourApp.Views;

public partial class FileExplorerPage
{
    private MediaPlayer? _mediaPlayer;
    private IDispatcherTimer? _positionTimer;

    partial void PlatformSpecificInit()
    {
        // Windows平台初始化
        System.Diagnostics.Debug.WriteLine("✓ FileExplorerPage Windows 平台初始化开始");
        
        // 延迟初始化音频播放器，等待BindingContext设置好
        Dispatcher.Dispatch(() =>
        {
            System.Diagnostics.Debug.WriteLine($"BindingContext 类型: {BindingContext?.GetType().Name ?? "null"}");
            InitializeAudioPlayer();
            InitializeKeyboardAccelerators();
            DisableButtonTooltips();
        });
        
        System.Diagnostics.Debug.WriteLine("✓ FileExplorerPage Windows 平台初始化完成");
    }
    
    private void DisableButtonTooltips()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine("========== 禁用按钮Tooltip ==========");
            
            // 订阅按钮的HandlerChanged事件
            if (NavigateBackButton != null)
            {
                NavigateBackButton.HandlerChanged += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine("NavigateBackButton Handler已变更");
                    DisableButtonTooltip(NavigateBackButton, "返回按钮");
                };
                // 立即尝试禁用
                DisableButtonTooltip(NavigateBackButton, "返回按钮");
            }
            
            if (NavigateUpButton != null)
            {
                NavigateUpButton.HandlerChanged += (s, e) =>
                {
                    System.Diagnostics.Debug.WriteLine("NavigateUpButton Handler已变更");
                    DisableButtonTooltip(NavigateUpButton, "向上按钮");
                };
                // 立即尝试禁用
                DisableButtonTooltip(NavigateUpButton, "向上按钮");
            }
            
            // 延迟再次尝试
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(500);
                System.Diagnostics.Debug.WriteLine("延迟500ms后再次尝试禁用Tooltip");
                DisableButtonTooltip(NavigateBackButton, "返回按钮(延迟)");
                DisableButtonTooltip(NavigateUpButton, "向上按钮(延迟)");
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"✗ 禁用Tooltip失败: {ex.Message}");
        }
    }
    
    private void DisableButtonTooltip(Button? button, string buttonName)
    {
        try
        {
            var logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "tooltip_debug.txt");
                
            void Log(string message)
            {
                var logMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
                System.Diagnostics.Debug.WriteLine(logMessage);
                try { File.AppendAllText(logFile, logMessage + Environment.NewLine); } catch { }
            }
                
            if (button == null)
            {
                Log($"\u2717 {buttonName}: \u6309\u94ae\u4e3anull");
                return;
            }
                
            if (button.Handler?.PlatformView is not Microsoft.UI.Xaml.Controls.Button platformBtn)
            {
                Log($"\u2717 {buttonName}: PlatformView\u672a\u5c31\u7eea");
                return;
            }
                
            // \u68c0\u67e5\u5f53\u524dtooltip
            var currentTooltip = Microsoft.UI.Xaml.Controls.ToolTipService.GetToolTip(platformBtn);
            Log($"\u5f53\u524d{buttonName}\u7684Tooltip: {currentTooltip?.ToString() ?? "null"}");
                
            // \u8bbe\u7f6e\u4e3anull
            Microsoft.UI.Xaml.Controls.ToolTipService.SetToolTip(platformBtn, null);
                
            // \u7981\u7528AutomationProperties
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(platformBtn, "");
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetHelpText(platformBtn, "");
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAcceleratorKey(platformBtn, "");
            Microsoft.UI.Xaml.Automation.AutomationProperties.SetAccessKey(platformBtn, "");
            Log($"\u5df2\u7981\u7528{buttonName}\u7684AutomationProperties");
                
            // \u68c0\u67e5\u662f\u5426\u6709KeyboardAccelerators
            if (platformBtn.KeyboardAccelerators.Count > 0)
            {
                Log($"{buttonName}\u6709 {platformBtn.KeyboardAccelerators.Count} \u4e2a\u952e\u76d8\u52a0\u901f\u5668");
                platformBtn.KeyboardAccelerators.Clear();
                Log($"\u5df2\u6e05\u9664{buttonName}\u7684\u952e\u76d8\u52a0\u901f\u5668");
            }
            
            // 监听鼠标事件，动态关闭tooltip
            platformBtn.PointerEntered += (s, e) =>
            {
                try
                {
                    Microsoft.UI.Xaml.Controls.ToolTipService.SetToolTip(platformBtn, null);
                    Log($"鼠标进入{buttonName}，关闭Tooltip");
                }
                catch { }
            };
                
            // \u9a8c\u8bc1\u662f\u5426\u8bbe\u7f6e\u6210\u529f
            var afterTooltip = Microsoft.UI.Xaml.Controls.ToolTipService.GetToolTip(platformBtn);
            Log($"\u8bbe\u7f6e\u540e{buttonName}\u7684Tooltip: {afterTooltip?.ToString() ?? "null"}");
                
            if (afterTooltip == null)
            {
                Log($"\u2713 {buttonName}: Tooltip\u5df2\u7981\u7528");
            }
            else
            {
                Log($"\u2717 {buttonName}: Tooltip\u7981\u7528\u5931\u8d25");
            }
        }
        catch (Exception ex)
        {
            var logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "tooltip_debug.txt");
            var errorMsg = $"\u2717 \u7981\u7528{buttonName}Tooltip\u5f02\u5e38: {ex.Message}";
            System.Diagnostics.Debug.WriteLine(errorMsg);
            try { File.AppendAllText(logFile, errorMsg + Environment.NewLine); } catch { }
        }
    }
    
    private void InitializeKeyboardAccelerators()
    {
        // 已禁用键盘加速器功能，避免自动显示tooltip
        // 用户可以直接点击按钮或使用鼠标操作
        System.Diagnostics.Debug.WriteLine("键盘加速器已禁用（避免tooltip干扰）");
    }
    
    private Microsoft.Maui.Controls.Window? GetParentMauiWindow()
    {
        var parent = this.Parent;
        while (parent != null)
        {
            if (parent is Microsoft.Maui.Controls.Window window)
                return window;
            parent = parent.Parent;
        }
        return null;
    }
    
    private void InitializeAudioPlayer()
    {
        System.Diagnostics.Debug.WriteLine("InitializeAudioPlayer 开始");
        
        if (BindingContext is FileExplorerViewModel viewModel)
        {
            System.Diagnostics.Debug.WriteLine("ViewModel 找到，订阅事件");
            
            // 订阅ViewModel的音频控制事件
            viewModel.OnPlayAudio += PlayAudio;
            viewModel.OnPauseAudio += PauseAudio;
            viewModel.OnStopAudio += StopAudio;
            viewModel.OnVolumeChanged += VolumeChanged;
            viewModel.OnSeekTo += SeekTo;
            
            System.Diagnostics.Debug.WriteLine("OnPlayAudio 事件已订阅");
            
            // 创建位置更新定时器
            _positionTimer = Dispatcher.CreateTimer();
            _positionTimer.Interval = TimeSpan.FromMilliseconds(100);
            _positionTimer.Tick += (s, e) =>
            {
                if (_mediaPlayer != null && viewModel != null)
                {
                    var position = _mediaPlayer.PlaybackSession.Position.TotalSeconds;
                    var duration = _mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds;
                    viewModel.UpdatePlaybackPosition(position, duration);
                }
            };
            
            System.Diagnostics.Debug.WriteLine("定时器创建完成");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("警告: BindingContext 不是 FileExplorerViewModel");
        }
    }
    
    private void PlayAudio(string filePath)
    {
        System.Diagnostics.Debug.WriteLine($"[Windows] PlayAudio 被调用: {filePath}");
        
        try
        {
            // 如果已有播放器，先停止
            if (_mediaPlayer != null)
            {
                System.Diagnostics.Debug.WriteLine("[Windows] 停止并释放旧播放器");
                _mediaPlayer.Pause();
                _mediaPlayer.Dispose();
            }
            
            System.Diagnostics.Debug.WriteLine($"[Windows] 创建新的 MediaPlayer");
            
            // 创建新的MediaPlayer
            _mediaPlayer = new MediaPlayer
            {
                Source = MediaSource.CreateFromUri(new Uri(filePath)),
                Volume = (BindingContext as FileExplorerViewModel)?.Volume ?? 0.5
            };
            
            System.Diagnostics.Debug.WriteLine($"[Windows] MediaPlayer 创建完成，音量: {_mediaPlayer.Volume}");
            
            // 订阅播放状态变化
            _mediaPlayer.MediaOpened += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("[Windows] MediaOpened 事件触发，开始播放");
                _mediaPlayer.Play();
                _positionTimer?.Start();
            };
            
            _mediaPlayer.MediaEnded += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine("[Windows] MediaEnded 事件触发");
                _positionTimer?.Stop();
                if (BindingContext is FileExplorerViewModel vm)
                {
                    vm.IsPlaying = false;
                    vm.StatusMessage = "播放完成";
                }
            };
            
            _mediaPlayer.MediaFailed += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[Windows] MediaFailed 事件触发: {e.ErrorMessage}");
                _positionTimer?.Stop();
                if (BindingContext is FileExplorerViewModel vm)
                {
                    vm.IsPlaying = false;
                    vm.StatusMessage = $"播放失败: {e.ErrorMessage}";
                }
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Windows] 播放音频异常: {ex.Message}\n{ex.StackTrace}");
            if (BindingContext is FileExplorerViewModel vm)
            {
                vm.StatusMessage = $"播放失败: {ex.Message}";
            }
        }
    }
    
    private void PauseAudio()
    {
        _mediaPlayer?.Pause();
        _positionTimer?.Stop();
    }
    
    private void StopAudio()
    {
        _positionTimer?.Stop();
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Pause();
            _mediaPlayer.PlaybackSession.Position = TimeSpan.Zero;
        }
    }
    
    private void VolumeChanged(double volume)
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Volume = volume;
        }
    }
    
    private void SeekTo(double position)
    {
        if (_mediaPlayer != null)
        {
            _mediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(position);
        }
    }
}
#endif
