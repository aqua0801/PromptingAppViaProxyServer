using Microsoft.Extensions.Logging;
using Microsoft.Maui.LifecycleEvents;
using CommunityToolkit.Maui;
/*
 windows : dotnet publish -c Release -r win-x64 -f net9.0-windows10.0.19041.0 /p:PublishSingleFile=true /p:PublishReadyToRun=true /p:SelfContained=false -p:UseMonoRuntime=false
 android : dotnet publish -f net9.0-android -c Release

 */


namespace LlamaPromptingApp
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

            builder
                .UseMauiApp<App>()
                .ConfigureLifecycleEvents(events =>
                {
#if ANDROID
                events.AddAndroid(android => android
                    .OnCreate((activity, bundle) =>
                    {
                        activity.Window?.SetFlags(
                            Android.Views.WindowManagerFlags.Fullscreen,
                            Android.Views.WindowManagerFlags.Fullscreen);
                    }));
#endif
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
