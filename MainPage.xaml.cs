namespace KontourApp;

public partial class MainPage : ContentPage
{
	int count = 0;

	public MainPage()
	{
		InitializeComponent();
		UpdateStatus("应用程序已启动");
	}

	private void OnCounterClicked(object? sender, EventArgs e)
	{
		count++;

		CounterBtn.Text = $"点击计数器 ({count})";
		UpdateStatus($"按钮已被点击 {count} 次");

		SemanticScreenReader.Announce(CounterBtn.Text);
	}

	private void UpdateStatus(string message)
	{
		if (StatusLabel != null)
		{
			StatusLabel.Text = $"{DateTime.Now:HH:mm:ss} - {message}";
		}
	}
}
