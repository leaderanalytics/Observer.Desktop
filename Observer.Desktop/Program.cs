using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Photino.Blazor;

namespace Observer.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        LeaderAnalytics.Core.EnvironmentName environmentName = LeaderAnalytics.Core.RuntimeEnvironment.GetEnvironmentName();
        string logFolder = "logs"; // fallback location if we cannot read config
        Exception startupEx = null;
        IConfigurationRoot appConfig = null;

        try
        {
            appConfig = ConfigHelper.BuildConfig(environmentName).Result;
        }
        catch (Exception ex)
        {
            startupEx = ex;
        }
        finally
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(logFolder, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
        }

        if (startupEx != null)
        {
            Log.Fatal("An exception occured during startup configuration.  Program execution will not continue.");
            Log.Fatal(startupEx.ToString());
            Log.CloseAndFlush();
            Thread.Sleep(1000);
            return;
        }
        string apiKey = appConfig["FredAPI_Key"];
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);
        builder.Services.AddLogging();
        builder.RootComponents.Add<App>("#app");
        builder.Services.AddFredClient().UseAPIKey(apiKey);
        builder.Services.AddLogging(x => x.AddConsole());
        builder.Services.AddSingleton<MessageService>();
        builder.Services.AddMudServices();
        builder.Services.AddLeaderPivot();
        //builder.RootComponents.Add<HeadOutlet>("head::after");
        var app = builder.Build();

        app.MainWindow
                .SetIconFile("favicon.ico")
                .SetTitle("Observer");

        AppDomain.CurrentDomain.UnhandledException += (sender, error) =>
        {
            app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString());
        };

        app.Run();
    }
}
