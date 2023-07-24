using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI.Windowing;

namespace SBSimulatorMaui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("MPLUS1p-Bold.ttf", "MPlus1pBold");
                fonts.AddFont("MPLUS1p-Regular.ttf", "MPlus1pRegular");
                fonts.AddFont("MPLUS1p-Black.ttf", "MPlus1pBlack");
                fonts.AddFont("MPLUSRounded1c-Black.ttf", "MPlus1cBlack");
				fonts.AddFont("Renner_ 400 Book.ttf", "Renner");
            })
			.ConfigureLifecycleEvents(events => 
			{
				events.AddWindows(windowsLifecycleBuilder =>
				{
					windowsLifecycleBuilder.OnWindowCreated(window =>
					{
						var handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
						var id = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(handle);
						var appWindow = AppWindow.GetFromWindowId(id);
						appWindow.Closing += AppWindow_Closing;
					});
				});
			});



#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
	private static async void AppWindow_Closing(object sender, AppWindowClosingEventArgs e)
	{
		e.Cancel = true;
		await Server.CancelAsync();
        Application.Current.Quit();
	}
}
