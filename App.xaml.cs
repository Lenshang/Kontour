using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace KontourApp;

public partial class App : Application
{
	public App()
	{
		try
		{
			Debug.WriteLine("开始初始化App...");
			InitializeComponent();
			Debug.WriteLine("App初始化成功");
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"App初始化失败: {ex.Message}");
			Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
			throw;
		}
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		try
		{
			Debug.WriteLine("开始创建 Window...");
			var window = new Window(new AppShell());
				
			// 加载并应用窗口大小设置
			var viewModel = new ViewModels.FileExplorerViewModel();
			var settings = viewModel.LoadWindowSettings();
				
			// 设置窗口大小
			window.Width = settings.width;
			window.Height = settings.height;
				
			Debug.WriteLine($"设置窗口大小: {settings.width}x{settings.height}");
			Debug.WriteLine("Window创建成功");
			return window;
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"Window创建失败: {ex.Message}");
			Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
			throw;
		}
	}
}