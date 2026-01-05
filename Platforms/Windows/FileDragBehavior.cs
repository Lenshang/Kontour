#if WINDOWS
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using KontourApp.Models;
using WinDataPackageOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation;

namespace KontourApp.Platforms.Windows;

public class FileDragBehavior : PlatformBehavior<CollectionView, FrameworkElement>
{
    protected override void OnAttachedTo(CollectionView bindable, FrameworkElement platformView)
    {
        base.OnAttachedTo(bindable, platformView);
        
        // 延迟设置，确保控件已完全加载
        platformView.Loaded += OnPlatformViewLoaded;
    }

    protected override void OnDetachedFrom(CollectionView bindable, FrameworkElement platformView)
    {
        platformView.Loaded -= OnPlatformViewLoaded;
        
        var listView = FindListViewBase(platformView);
        if (listView != null)
        {
            listView.DragItemsStarting -= OnDragItemsStarting;
        }
        
        base.OnDetachedFrom(bindable, platformView);
    }

    private void OnPlatformViewLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            var listView = FindListViewBase(element);
            if (listView != null)
            {
                listView.CanDragItems = true;
                listView.DragItemsStarting += OnDragItemsStarting;
                System.Diagnostics.Debug.WriteLine("✓ 成功设置文件拖拽功能");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"✗ 未找到ListViewBase，当前元素类型: {element.GetType().Name}");
            }
        }
    }

    private ListViewBase? FindListViewBase(DependencyObject element)
    {
        if (element is ListViewBase listView)
            return listView;

        var childCount = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(element);
        for (int i = 0; i < childCount; i++)
        {
            var child = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(element, i);
            var result = FindListViewBase(child);
            if (result != null)
                return result;
        }
        return null;
    }

    private async void OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (e.Items.FirstOrDefault() is FileItemModel fileItem)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"开始拖拽: {fileItem.FullPath}");

                if (System.IO.File.Exists(fileItem.FullPath))
                {
                    var storageFile = await StorageFile.GetFileFromPathAsync(fileItem.FullPath);
                    e.Data.SetStorageItems(new[] { storageFile });
                    e.Data.RequestedOperation = WinDataPackageOperation.Copy;
                    System.Diagnostics.Debug.WriteLine($"✓ 文件已添加到拖拽数据: {fileItem.Name}");
                }
                else if (System.IO.Directory.Exists(fileItem.FullPath))
                {
                    var storageFolder = await StorageFolder.GetFolderFromPathAsync(fileItem.FullPath);
                    e.Data.SetStorageItems(new[] { storageFolder });
                    e.Data.RequestedOperation = WinDataPackageOperation.Copy;
                    System.Diagnostics.Debug.WriteLine($"✓ 文件夹已添加到拖拽数据: {fileItem.Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"✗ 拖拽失败: {ex.Message}");
            }
        }
    }
}
#endif
