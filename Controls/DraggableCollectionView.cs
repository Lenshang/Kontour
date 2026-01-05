using Microsoft.Maui.Controls;

namespace KontourApp.Controls;

public class DraggableCollectionView : CollectionView
{
    public static readonly BindableProperty EnableDragDropProperty =
        BindableProperty.Create(
            nameof(EnableDragDrop),
            typeof(bool),
            typeof(DraggableCollectionView),
            true);

    public bool EnableDragDrop
    {
        get => (bool)GetValue(EnableDragDropProperty);
        set => SetValue(EnableDragDropProperty, value);
    }

    public event EventHandler<DragStartEventArgs>? DragStarting;
    public event EventHandler<DragEventArgs>? DragOver;
    public event EventHandler<DropEventArgs>? Drop;

    public void OnDragStarting(DragStartEventArgs e)
    {
        DragStarting?.Invoke(this, e);
    }

    public void OnDragOver(DragEventArgs e)
    {
        DragOver?.Invoke(this, e);
    }

    public void OnDrop(DropEventArgs e)
    {
        Drop?.Invoke(this, e);
    }
}

public class DragStartEventArgs : EventArgs
{
    public object? Item { get; set; }
    public string? FilePath { get; set; }
}

public class DragEventArgs : EventArgs
{
    public bool Handled { get; set; }
}

public class DropEventArgs : EventArgs
{
    public string[]? Files { get; set; }
}
