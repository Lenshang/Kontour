#if !WINDOWS
using Microsoft.Maui.Controls;

namespace KontourApp.Behaviors;

// 非Windows平台的空实现
public class FileDragDropBehavior : Behavior<Border>
{
    protected override void OnAttachedTo(Border bindable)
    {
        base.OnAttachedTo(bindable);
    }

    protected override void OnDetachingFrom(Border bindable)
    {
        base.OnDetachingFrom(bindable);
    }
}
#endif
