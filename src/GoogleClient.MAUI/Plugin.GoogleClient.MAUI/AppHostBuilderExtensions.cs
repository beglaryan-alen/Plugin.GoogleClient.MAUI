using Microsoft.Maui.Hosting;
using Microsoft.Maui.LifecycleEvents;

namespace Plugin.GoogleClient.MAUI
{
    public static class AppHostBuilderExtensions
    {
        /// <summary>
        /// Automatically sets up lifecycle events and Maui Handlers
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static MauiAppBuilder UseGoogleLogin(this MauiAppBuilder builder, string serverClientId = null, string clientId = null)
        {
            builder.ConfigureLifecycleEvents(events =>
            {
#if ANDROID
                events.AddAndroid(android => android
                .OnActivityResult((activity, requestCode, resultCode, data) => GoogleClientManager.OnAuthCompleted(requestCode, resultCode, data))
                .OnCreate((activity, bundle) => GoogleClientManager.Initialize(activity, serverClientId, clientId)));
#elif IOS
                events.AddiOS(ios => ios
                .FinishedLaunching((app, options) =>
                {
                    GoogleClientManager.Initialize(clientId, null);
                    return false;
                })
                .OpenUrl((app, url, options) => GoogleClientManager.OnOpenUrl(app, url, options)));
#endif
            });
            return builder;
        }
    }
}
