using KontourApp.ViewModels;
using KontourApp.Models;
using KontourApp.Controls;
using Microsoft.Maui.Controls;

namespace KontourApp.Views;

public partial class FileExplorerPage : ContentPage
{
	private const double MinLeftColumnWidth = 150;
	private const double MaxLeftColumnWidth = 600;
	private const double WidthStep = 50; // 每次调整的宽度
	private Window? _window;
	
	public FileExplorerPage()
	{
		InitializeComponent();
		
		// 调试输出
		System.Diagnostics.Debug.WriteLine("========== FileExplorerPage 初始化开始 ==========");
		System.Diagnostics.Debug.WriteLine($"BindingContext 类型: {BindingContext?.GetType().Name ?? "null"}");
		
		// 注册键盘事件处理
		this.HandlerChanged += OnHandlerChanged;
		
		PlatformSpecificInit();
		
		// 订阅ViewModel的滚动事件 - 需要在Loaded之后
		Loaded += (s, e) =>
		{
			if (BindingContext is FileExplorerViewModel viewModel)
			{
				System.Diagnostics.Debug.WriteLine("订阅 OnScrollToNode 事件");
				viewModel.OnScrollToNode += ScrollToNode;
				
				// 加载并应用窗口设置
				ApplyWindowSettings(viewModel);
			}
			
			// 监听窗口大小变化
			_window = Parent as Window ?? FindParentWindow();
			if (_window != null)
			{
				System.Diagnostics.Debug.WriteLine("开始监听窗口大小变化");
				_window.SizeChanged += OnWindowSizeChanged;
			}
		};
		
		Unloaded += (s, e) =>
		{
			if (_window != null)
			{
				_window.SizeChanged -= OnWindowSizeChanged;
			}
		};
		
		System.Diagnostics.Debug.WriteLine("========== FileExplorerPage 初始化完成 ==========");
	}
	
	private Window? FindParentWindow()
	{
		Element? current = this;
		while (current != null)
		{
			if (current.Parent is Window window)
			{
				return window;
			}
			current = current.Parent;
		}
		return null;
	}
	
	private void ApplyWindowSettings(FileExplorerViewModel viewModel)
	{
		try
		{
			var settings = viewModel.LoadWindowSettings();
			
			// 应用树形宽度
			if (settings.treeWidth >= MinLeftColumnWidth && settings.treeWidth <= MaxLeftColumnWidth)
			{
				LeftColumn.Width = new GridLength(settings.treeWidth, GridUnitType.Absolute);
				System.Diagnostics.Debug.WriteLine($"恢复树形宽度: {settings.treeWidth}px");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"应用窗口设置失败: {ex.Message}");
		}
	}
	
	private void OnWindowSizeChanged(object? sender, EventArgs e)
	{
		try
		{
			if (_window != null && BindingContext is FileExplorerViewModel viewModel)
			{
				var width = _window.Width;
				var height = _window.Height;
				var treeWidth = LeftColumn.Width.Value;
				
				// 保存窗口设置
				viewModel.SaveWindowSettings(width, height, treeWidth);
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"保存窗口大小失败: {ex.Message}");
		}
	}

	// Partial方法，由平台特定代码实现
	partial void PlatformSpecificInit();
	
	// 键盘事件处理
	private void OnHandlerChanged(object? sender, EventArgs e)
	{
		// 方法1：在 ContentPage 上注册
		if (Handler?.PlatformView is Microsoft.UI.Xaml.FrameworkElement platformView)
		{
			System.Diagnostics.Debug.WriteLine("========== 注册 Windows 键盘事件 ==========");
			
			// 设置为可获得焦点
			platformView.IsTabStop = true;
			platformView.AllowFocusOnInteraction = true;
			
			// 注册键盘事件
			platformView.KeyDown += OnPlatformKeyDown;
			
			// 获得焦点事件
			platformView.GotFocus += (s, args) =>
			{
				System.Diagnostics.Debug.WriteLine("✓ Page 已获得焦点");
			};
			
			platformView.LostFocus += (s, args) =>
			{
				System.Diagnostics.Debug.WriteLine("✗ Page 失去焦点");
			};
			
			// 尝试立即获得焦点
			MainThread.BeginInvokeOnMainThread(() =>
			{
				try
				{
					var focused = platformView.Focus(Microsoft.UI.Xaml.FocusState.Programmatic);
					System.Diagnostics.Debug.WriteLine($"设置焦点结果: {focused}");
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"设置焦点失败: {ex.Message}");
				}
			});
			
			System.Diagnostics.Debug.WriteLine("键盘事件注册完成");
		}
		else
		{
			System.Diagnostics.Debug.WriteLine("警告: PlatformView 不是 FrameworkElement");
		}
		
		// 方法2：也尝试在 FileListView 上注册
		if (FileListView?.Handler?.PlatformView is Microsoft.UI.Xaml.Controls.ListViewBase listView)
		{
			System.Diagnostics.Debug.WriteLine("在 FileListView 上注册键盘事件");
			listView.KeyDown += OnPlatformKeyDown;
		}
	}
	
	private void OnPlatformKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
	{
		try
		{
			System.Diagnostics.Debug.WriteLine($"键盘按下: {e.Key}");
			
			if (BindingContext is FileExplorerViewModel viewModel)
			{
				if (e.Key == Windows.System.VirtualKey.Down)
				{
					System.Diagnostics.Debug.WriteLine("检测到下方向键");
					viewModel.SelectNextFile();
					e.Handled = true;
				}
				else if (e.Key == Windows.System.VirtualKey.Up)
				{
					System.Diagnostics.Debug.WriteLine("检测到上方向键");
					viewModel.SelectPreviousFile();
					e.Handled = true;
				}
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"键盘事件处理失败: {ex.Message}");
		}
	}
	
	private void OnProgressSliderDragCompleted(object? sender, EventArgs e)
	{
		if (sender is Slider slider && BindingContext is FileExplorerViewModel viewModel)
		{
			viewModel.SeekToPosition(slider.Value);
		}
	}
	
	private async void ScrollToNode(DirectoryTreeNode targetNode)
	{
		try
		{
			System.Diagnostics.Debug.WriteLine($"开始滚动到节点: {targetNode.Name}");
			
			// 等待一小段时间让UI更新
			await Task.Delay(100);
			
			// 查找TreeNodeView
			var treeNodeView = FindTreeNodeView(TreeScrollView, targetNode);
			
			if (treeNodeView?.NodeBorder != null)
			{
				System.Diagnostics.Debug.WriteLine($"找到TreeNodeView，开始滚动");
				
				// 滚动到元素位置
				await TreeScrollView.ScrollToAsync(treeNodeView.NodeBorder, ScrollToPosition.Center, true);
				
				System.Diagnostics.Debug.WriteLine("滚动完成");
			}
			else
			{
				System.Diagnostics.Debug.WriteLine("未找到TreeNodeView或NodeBorder");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"滚动失败: {ex.Message}");
		}
	}
	
	private TreeNodeView? FindTreeNodeView(Element parent, DirectoryTreeNode targetNode)
	{
		if (parent is TreeNodeView treeNodeView && treeNodeView.Node == targetNode)
		{
			return treeNodeView;
		}
		
		// 递归查找子元素
		if (parent is Layout layout)
		{
			foreach (var child in layout.Children)
			{
				if (child is Element element)
				{
					var result = FindTreeNodeView(element, targetNode);
					if (result != null)
					{
						return result;
					}
				}
			}
		}
		else if (parent is ScrollView scrollView && scrollView.Content is Element content)
		{
			return FindTreeNodeView(content, targetNode);
		}
		else if (parent is ContentView contentView && contentView.Content is Element contentElement)
		{
			return FindTreeNodeView(contentElement, targetNode);
		}
		
		return null;
	}
	
	private void OnTreeWidthDecrease(object? sender, EventArgs e)
	{
		try
		{
			var currentWidth = LeftColumn.Width.Value;
			var newWidth = currentWidth - WidthStep;
			
			// 限制最小宽度
			newWidth = Math.Max(MinLeftColumnWidth, newWidth);
			
			if (Math.Abs(newWidth - currentWidth) > 0.1)
			{
				LeftColumn.Width = new GridLength(newWidth, GridUnitType.Absolute);
				System.Diagnostics.Debug.WriteLine($"树宽度减小: {currentWidth:F0}px → {newWidth:F0}px");
				
				// 保存设置
				SaveTreeWidth();
			}
			else
			{
				System.Diagnostics.Debug.WriteLine($"已达到最小宽度: {MinLeftColumnWidth}px");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"调整树宽度失败: {ex.Message}");
		}
	}
	
	private void OnTreeWidthIncrease(object? sender, EventArgs e)
	{
		try
		{
			var currentWidth = LeftColumn.Width.Value;
			var newWidth = currentWidth + WidthStep;
			
			// 限制最大宽度
			newWidth = Math.Min(MaxLeftColumnWidth, newWidth);
			
			if (Math.Abs(newWidth - currentWidth) > 0.1)
			{
				LeftColumn.Width = new GridLength(newWidth, GridUnitType.Absolute);
				System.Diagnostics.Debug.WriteLine($"树宽度增大: {currentWidth:F0}px → {newWidth:F0}px");
				
				// 保存设置
				SaveTreeWidth();
			}
			else
			{
				System.Diagnostics.Debug.WriteLine($"已达到最大宽度: {MaxLeftColumnWidth}px");
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"调整树宽度失败: {ex.Message}");
		}
	}
	
	private void SaveTreeWidth()
	{
		try
		{
			if (_window != null && BindingContext is FileExplorerViewModel viewModel)
			{
				var width = _window.Width;
				var height = _window.Height;
				var treeWidth = LeftColumn.Width.Value;
				
				viewModel.SaveWindowSettings(width, height, treeWidth);
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"保存树宽度失败: {ex.Message}");
		}
	}
}
