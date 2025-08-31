
using Avalonia;
using Avalonia.iOS;
using Avalonia.ReactiveUI;
using Avalonia.WebView.iOS;

namespace ProjectIgnite;

[Foundation.Register("AppDelegate")]
public partial class AppDelegate : AvaloniaAppDelegate<App>
{
    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder)
            .WithInterFont()
            .UseReactiveUI()
            .UseIosWebView();
    }
}