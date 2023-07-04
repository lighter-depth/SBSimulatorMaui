using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Plugin.Maui.Audio;

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
            });

#if WINDOW
		Microsoft.Maui.Handlers.LabelHandler.Mapper.AppendToMapping("FontFamily", (handler, element) =>
		{
			if(element.Font.Family == "MPlus1pBlack")
			{
				const string MPlus1pBlackFamily = "ms-appx:///MPLUS1p-Black.ttf";
				handler.PlatformView.FontFamily = new Microsoft.UI.Xaml.Media.FontFamily(MPlus1pBlackFamily);

            }
		});
#endif

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
