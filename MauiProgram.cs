using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace KontourApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		try
		{
			Debug.WriteLine("开始创建MAUI应用...");
			var builder = MauiApp.CreateBuilder();
			builder
				.UseMauiApp<App>()
				.ConfigureFonts(fonts =>
				{
					fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
					fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				});

#if DEBUG
			builder.Logging.AddDebug();
#endif

			Debug.WriteLine("MAUI应用创建成功");
			return builder.Build();
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"创建MAUI应用失败: {ex.Message}");
			Debug.WriteLine($"堆栈跟踪: {ex.StackTrace}");
			throw;
		}
	}
}
