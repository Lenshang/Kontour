using Microsoft.Maui.Controls;
using KontourApp.Models;
using KontourApp.ViewModels;
using KontourApp.Services;

namespace KontourApp.Controls;

public class TreeNodeView : ContentView
{
    public static readonly BindableProperty NodeProperty = BindableProperty.Create(
        nameof(Node),
        typeof(DirectoryTreeNode),
        typeof(TreeNodeView),
        null,
        propertyChanged: OnNodeChanged);

    public DirectoryTreeNode? Node
    {
        get => (DirectoryTreeNode?)GetValue(NodeProperty);
        set => SetValue(NodeProperty, value);
    }
    
    // 存储节点边框的引用，用于滚动定位
    public Border? NodeBorder { get; private set; }

    private static void OnNodeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is TreeNodeView view)
        {
            view.BuildView();
        }
    }

    private void BuildView()
    {
        if (Node == null) return;

        var stackLayout = new VerticalStackLayout { Spacing = 2 };

        // 创建当前节点的显示
        var nodeBorder = CreateNodeBorder(Node);
        stackLayout.Children.Add(nodeBorder);

        // 创建子节点容器
        var childrenContainer = new VerticalStackLayout
        {
            Spacing = 2,
            Margin = new Thickness(16, 0, 0, 0)
        };
        childrenContainer.SetBinding(IsVisibleProperty, new Binding("IsExpanded", source: Node));

        // 绑定子节点
        var childrenView = new VerticalStackLayout { Spacing = 2 };
        childrenView.SetBinding(BindableLayout.ItemsSourceProperty, new Binding("Children", source: Node));
        BindableLayout.SetItemTemplate(childrenView, new DataTemplate(() =>
        {
            var childTreeView = new TreeNodeView();
            childTreeView.SetBinding(NodeProperty, new Binding("."));
            return childTreeView;
        }));

        childrenContainer.Children.Add(childrenView);
        stackLayout.Children.Add(childrenContainer);

        Content = stackLayout;
    }

    private Border CreateNodeBorder(DirectoryTreeNode node)
    {
        var border = new Border { Padding = new Thickness(4, 2) };
        
        // 存储引用
        NodeBorder = border;
        
        // 背景色触发器
        var selectedTrigger = new DataTrigger(typeof(Border))
        {
            Binding = new Binding("IsSelected", source: node),
            Value = true
        };
        selectedTrigger.Setters.Add(new Setter
        {
            Property = BackgroundColorProperty,
            Value = Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#3730A3")
                : Color.FromArgb("#E0E7FF")
        });
        border.Triggers.Add(selectedTrigger);

        var grid = new Grid
        {
            Padding = new Thickness(4, 2),
            ColumnSpacing = 4,
            ColumnDefinitions =
            {
                new ColumnDefinition { Width = new GridLength(20) },
                new ColumnDefinition { Width = new GridLength(20) },
                new ColumnDefinition { Width = GridLength.Star }
            }
        };

        // 展开/折叠按钮
        var expandIcon = new Label
        {
            FontSize = 14,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center
        };
        expandIcon.SetBinding(Label.TextProperty, new Binding("Icon", source: node));
        expandIcon.SetBinding(IsVisibleProperty, new Binding("Children.Count", source: node, converter: new CountToBoolConverter()));
        
        var expandTapGesture = new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                // 直接调用ViewModel的命令
                var page = this.GetParentOfType<Page>();
                if (page?.BindingContext is FileExplorerViewModel viewModel)
                {
                    viewModel.TreeNodeExpandedCommand?.Execute(node);
                }
            })
        };
        expandIcon.GestureRecognizers.Add(expandTapGesture);
        
        Grid.SetColumn(expandIcon, 0);
        grid.Children.Add(expandIcon);

        // 文件夹图标
        var folderIcon = new Label
        {
            Text = "📁",
            FontSize = 14,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center
        };
        Grid.SetColumn(folderIcon, 1);
        grid.Children.Add(folderIcon);

        // 目录名称
        var nameLabel = new Label
        {
            FontSize = 11,
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        nameLabel.SetBinding(Label.TextProperty, new Binding("Name", source: node));
        
        var nameTrigger = new DataTrigger(typeof(Label))
        {
            Binding = new Binding("IsSelected", source: node),
            Value = true
        };
        nameTrigger.Setters.Add(new Setter 
        { 
            Property = Label.TextColorProperty, 
            Value = Application.Current?.RequestedTheme == AppTheme.Dark
                ? Colors.White
                : Colors.Black
        });
        nameTrigger.Setters.Add(new Setter { Property = Label.FontAttributesProperty, Value = FontAttributes.Bold });
        nameLabel.Triggers.Add(nameTrigger);

        var nameTapGesture = new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                // 直接调用ViewModel的命令
                var page = this.GetParentOfType<Page>();
                if (page?.BindingContext is FileExplorerViewModel viewModel)
                {
                    viewModel.TreeNodeSelectedCommand?.Execute(node);
                }
            })
        };
        nameLabel.GestureRecognizers.Add(nameTapGesture);
        
        // 添加右键菜单支持（使用FlyoutMenu）
        FlyoutBase.SetContextFlyout(nameLabel, CreateContextMenu(node));

        Grid.SetColumn(nameLabel, 2);
        grid.Children.Add(nameLabel);

        border.Content = grid;
        return border;
    }

    private MenuFlyout CreateContextMenu(DirectoryTreeNode node)
    {
        var flyout = new MenuFlyout();
        
        var addToFavoritesItem = new MenuFlyoutItem();
        
        // 初始化文本
        UpdateMenuItemText(addToFavoritesItem);
        
        // 订阅语言变化事件
        LocalizationService.Instance.LanguageChanged += () => UpdateMenuItemText(addToFavoritesItem);
        
        addToFavoritesItem.Clicked += (s, e) =>
        {
            var page = this.GetParentOfType<Page>();
            if (page?.BindingContext is FileExplorerViewModel viewModel)
            {
                viewModel.AddToFavoritesCommand?.Execute(node);
            }
        };
        
        flyout.Add(addToFavoritesItem);
        return flyout;
    }
    
    private void UpdateMenuItemText(MenuFlyoutItem item)
    {
        item.Text = LocalizationService.Instance["AddToFavorites"];
    }

    private class CountToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            return value is int count && count > 0;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

// 扩展方法帮助查找父元素
public static class ElementExtensions
{
    public static T? GetParentOfType<T>(this Element element) where T : Element
    {
        var parent = element.Parent;
        while (parent != null)
        {
            if (parent is T typedParent)
                return typedParent;
            parent = parent.Parent;
        }
        return null;
    }
}
