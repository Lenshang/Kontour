#if WINDOWS
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Windows.Storage;
using KontourApp.Models;
using WinDragStartingEventArgs = Microsoft.UI.Xaml.DragStartingEventArgs;
using WinDataPackage = Windows.ApplicationModel.DataTransfer.DataPackage;
using WinDataPackageOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation;

namespace KontourApp.Behaviors;

public class FileDragDropBehavior : Behavior<Border>
{
    private Border? _border;
    private Microsoft.UI.Xaml.FrameworkElement? _nativeElement;

    protected override void OnAttachedTo(Border bindable)
    {
        base.OnAttachedTo(bindable);
        _border = bindable;
        
        System.Diagnostics.Debug.WriteLine("FileDragDropBehavior.OnAttachedTo 被调用");
        
        // 等待控件加载完成
        bindable.Loaded += OnBorderLoaded;
    }

    protected override void OnDetachingFrom(Border bindable)
    {
        bindable.Loaded -= OnBorderLoaded;
        
        if (_nativeElement != null)
        {
            _nativeElement.DragStarting -= OnDragStarting;
        }
        
        base.OnDetachingFrom(bindable);
    }

    private void OnBorderLoaded(object? sender, EventArgs e)
    {
        System.Diagnostics.Debug.WriteLine("FileDragDropBehavior.OnBorderLoaded 被调用");
        
        if (_border?.Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement nativeElement)
        {
            _nativeElement = nativeElement;
            System.Diagnostics.Debug.WriteLine($"✓ 获取到 Border 的 PlatformView: {nativeElement.GetType().FullName}");
            
            // 直接在 Border 的原生元素上设置拖拽
            nativeElement.CanDrag = true;
            nativeElement.DragStarting += OnDragStarting;
            
            System.Diagnostics.Debug.WriteLine($"✓ 已为 Border 设置拖拽功能，CanDrag = {nativeElement.CanDrag}");
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("✗ 无法获取 Border 的 PlatformView");
        }
    }

    private async void OnDragStarting(Microsoft.UI.Xaml.UIElement sender, WinDragStartingEventArgs args)
    {
        System.Diagnostics.Debug.WriteLine("========== OnDragStarting 被调用 ==========");
        
        // 获取绑定的 FileItemModel
        if (_border?.BindingContext is FileItemModel fileItem)
        {
            System.Diagnostics.Debug.WriteLine($"→ 正在拖拽文件: {fileItem.Name} ({fileItem.FullPath})");
            await SetDragData(args.Data, fileItem);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"✗ 无法获取 FileItemModel，BindingContext 类型: {_border?.BindingContext?.GetType().Name}");
        }
    }

    private async Task SetDragData(WinDataPackage dataPackage, FileItemModel fileItem)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"开始设置拖拽数据: {fileItem.FullPath}");

            if (System.IO.File.Exists(fileItem.FullPath))
            {
                var storageFile = await StorageFile.GetFileFromPathAsync(fileItem.FullPath);
                dataPackage.SetStorageItems(new[] { storageFile });
                dataPackage.RequestedOperation = WinDataPackageOperation.Copy;
                System.Diagnostics.Debug.WriteLine($"✓ 文件拖拽数据已设置: {fileItem.Name}");
            }
            else if (System.IO.Directory.Exists(fileItem.FullPath))
            {
                var storageFolder = await StorageFolder.GetFolderFromPathAsync(fileItem.FullPath);
                dataPackage.SetStorageItems(new[] { storageFolder });
                dataPackage.RequestedOperation = WinDataPackageOperation.Copy;
                System.Diagnostics.Debug.WriteLine($"✓ 文件夹拖拽数据已设置: {fileItem.Name}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"✗ 设置拖拽数据失败: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"✗ 堆栈: {ex.StackTrace}");
        }
    }
}
#endif
