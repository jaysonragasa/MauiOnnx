using AI.ChatClientProviders;
using AI.ChatClientProviders.Onnx;
using AI.Tools;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using ViewModels;

namespace MauiOnnxAiTooling
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            builder.RegisterAppServices();

            return builder.Build();
        }

        public static MauiAppBuilder RegisterAppServices(this MauiAppBuilder mauiAppBuilder)
        {
			// AI Services
			mauiAppBuilder.Services.AddSingleton<IChatClientProvider, OnnxChatClient>();

			// Register Chat Tools
			//mauiAppBuilder.Services.AddSingleton<IAIChatTool, Infor.PublicSector.Mobile.Core.AI.Tools.Implementations.WorkOrderCountTool>();
			mauiAppBuilder.Services.AddSingleton<AIChatToolRegistration>();

			// AI ViewModels
			mauiAppBuilder.Services.AddTransient<AIChatViewModel>();
			mauiAppBuilder.Services.AddTransient<AIChatSettingsViewModel>();

			return mauiAppBuilder;
		}
    }
}