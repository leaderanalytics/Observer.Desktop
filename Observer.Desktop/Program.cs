using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Photino.Blazor;
using Autofac.Extensions.DependencyInjection;
using System.Collections.Generic;
using Serilog;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;


//https://github.com/dotnet/docfx


namespace LeaderAnalytics.Observer.Desktop;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        LeaderAnalytics.Core.EnvironmentName environmentName = LeaderAnalytics.Core.RuntimeEnvironment.GetEnvironmentName();
        string logFolder = "logs/"; // fallback location if we cannot read config
        Exception startupEx = null;
        IConfigurationRoot appConfig = null;
        PhotinoBlazorApp app = null;
        // Configure logging
        
        try
        {
            appConfig = ConfigHelper.BuildConfig(environmentName).Result;
            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(appConfig).CreateLogger();
        }
        catch (Exception ex)
        {
            startupEx = ex;
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

        // Build app

        try
        {
            string apiKey = appConfig["FredAPI_Key"];
            AppState appState = new(new UserSettingsService());  // UserSettings read from disk and loaded here.
            IEnumerable<IEndPointConfiguration> endPoints = appConfig.GetSection("EndPoints").Get<IEnumerable<EndPointConfiguration>>();
            var builder = PhotinoBlazorAppBuilder.CreateDefault(args);
            // Cannot call UseServiceProviderFactory() on PhotinoBlazorAppBuilder since it does not implement IHostBuilder.
            // Add the Autofac container to the Photino service collection and inject it as needed.
            ContainerBuilder containerBuilder = new();
            containerBuilder.RegisterInstance(appState);
            containerBuilder.AddFredDownloaderServices(endPoints);
            containerBuilder.RegisterModule(new LeaderAnalytics.AdaptiveClient.EntityFrameworkCore.AutofacModule());
            
            if (!(endPoints?.Any(x => x.IsActive) ?? false))
                throw new Exception("No active endPoints were found.  Make sure one or more endPoints are defined in appsettings.json and that IsActive is set to true for one endPoint only.");
            else if (endPoints.Count(x => x.IsActive) > 1)
                throw new Exception("Only one endPoint can be active at a time.  Check the EndPoints section in appsettings.json and make sure IsActive is set to True for one endPoint only.");

            containerBuilder.RegisterInstance(endPoints.First(x => x.IsActive)).SingleInstance();
            builder.RootComponents.Add<App>("#app");
            FredClientConfig config = new FredClientConfig { MaxDownloadRetries = 3, ErrorDelay = 2000, MaxRequestsPerMinute = 60 };
            builder.Services.AddFredClient().UseAPIKey(apiKey).UseConfig(x => config);
            builder.Services.AddLogging(x => x.AddConsole().AddSerilog());
            builder.Services.AddMudServices();
            builder.Services.AddMessageBoxBlazor();
            builder.Services.AddLeaderPivot();
            containerBuilder.Populate(builder.Services);
            IContainer container = containerBuilder.Build();
            builder.Services.AddSingleton(typeof(IContainer), container);
            builder.Services.AddSingleton(new MudThemeProvider());
            app = builder.Build();
            app.MainWindow.SetIconFile("favicon.ico").SetTitle("Observer").SetSize(new System.Drawing.Size(1200,800)); //width,height
            Log.Information("App configuration was successful.");
        }
        catch(Exception ex)
        {
            Log.Fatal(ex.ToString());
            Log.CloseAndFlush();
            return;
        }

        try
        {
         //   AppDomain.CurrentDomain.UnhandledException += (sender, error) => HandleException(app, error.ExceptionObject as Exception);
            Log.Information("Starting Observer Desktop.");
            app.Run();
            Log.Information("Observer Desktop was shut down normally.");
            Log.CloseAndFlush();

        }
        catch  (Exception ex)
        {
            HandleException(app, ex);
        }
    }

    private static void HandleException(PhotinoBlazorApp app, Exception ex)
    {
        Log.Fatal(ex.ToString());
        app.MainWindow.ShowMessage("Fatal exception", ex.ToString());
        Log.CloseAndFlush();
    }
}

