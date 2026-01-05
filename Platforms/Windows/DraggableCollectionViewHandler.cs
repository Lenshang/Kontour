using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using KontourApp.Controls;
using KontourApp.Models;
using Microsoft.Maui.Platform;
using Microsoft.Maui;
using Windows.Storage;
using Windows.ApplicationModel.DataTransfer;
using WinUIListViewSelectionMode = Microsoft.UI.Xaml.Controls.ListViewSelectionMode;
using WinUIDataPackage = Windows.ApplicationModel.DataTransfer.DataPackage;
using WinUIDataPackageOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation;

namespace KontourApp.Platforms.Windows;

public class DraggableCollectionViewHandler : ViewHandler<DraggableCollectionView, ListViewBase>
{
    public static IPropertyMapper<DraggableCollectionView, DraggableCollectionViewHandler> PropertyMapper = new PropertyMapper<DraggableCollectionView, DraggableCollectionViewHandler>(ViewHandler.ViewMapper)
    {
        [nameof(DraggableCollectionView.ItemsSource)] = MapItemsSource
    };

    public DraggableCollectionViewHandler() : base(PropertyMapper)
    {
    }

    protected override ListViewBase CreatePlatformView()
    {
        var listView = new Microsoft.UI.Xaml.Controls.ListView
        {
            CanDragItems = true,
            AllowDrop = true,
            SelectionMode = WinUIListViewSelectionMode.Single
        };
        return listView;
    }

    protected override void ConnectHandler(ListViewBase platformView)
    {
        base.ConnectHandler(platformView);

        platformView.DragItemsStarting += OnDragItemsStarting;
        platformView.DragOver += OnDragOver;
        platformView.Drop += OnDrop;

        UpdateItemsSource();
    }

    protected override void DisconnectHandler(ListViewBase platformView)
    {
        platformView.DragItemsStarting -= OnDragItemsStarting;
        platformView.DragOver -= OnDragOver;
        platformView.Drop -= OnDrop;

        base.DisconnectHandler(platformView);
    }

    private void UpdateItemsSource()
    {
        if (PlatformView != null && VirtualView?.ItemsSource != null)
        {
            PlatformView.ItemsSource = VirtualView.ItemsSource;
        }
    }

    public static void MapItemsSource(DraggableCollectionViewHandler handler, DraggableCollectionView view)
    {
        handler.UpdateItemsSource();
    }

    private async void OnDragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        if (VirtualView is not DraggableCollectionView draggableView) return;

        var item = e.Items.FirstOrDefault();
        if (item is FileItemModel fileItem)
        {
            try
            {
                if (System.IO.File.Exists(fileItem.FullPath))
                {
                    var storageFile = await StorageFile.GetFileFromPathAsync(fileItem.FullPath);
                    e.Data.SetStorageItems(new[] { storageFile });
                }
                else if (System.IO.Directory.Exists(fileItem.FullPath))
                {
                    var storageFolder = await StorageFolder.GetFolderFromPathAsync(fileItem.FullPath);
                    e.Data.SetStorageItems(new[] { storageFolder });
                }

                e.Data.RequestedOperation = WinUIDataPackageOperation.Copy;

                draggableView.OnDragStarting(new KontourApp.Controls.DragStartEventArgs
                {
                    Item = item,
                    FilePath = fileItem.FullPath
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"拖拽失败: {ex.Message}");
            }
        }
    }

    private void OnDragOver(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        if (VirtualView is DraggableCollectionView draggableView)
        {
            e.AcceptedOperation = WinUIDataPackageOperation.Copy;
            e.Handled = true;

            draggableView.OnDragOver(new KontourApp.Controls.DragEventArgs
            {
                Handled = true
            });
        }
    }

    private void OnDrop(object sender, Microsoft.UI.Xaml.DragEventArgs e)
    {
        if (VirtualView is DraggableCollectionView draggableView)
        {
            draggableView.OnDrop(new KontourApp.Controls.DropEventArgs());
        }
    }
}
