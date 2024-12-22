using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Photino.Blazor;
using Autofac.Extensions.DependencyInjection;
using System.Collections.Generic;
using Serilog;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Velopack;
using System.Runtime.InteropServices;

//https://github.com/dotnet/docfx


namespace LeaderAnalytics.Observer.Desktop;

class Program
{
    // We never load the config directly from this folder - we copy from this folder to configFilePath if the development config file does not exist there.
    private static readonly string configFileSourceFolder = "O:\\LeaderAnalytics\\Config\\Observer.Desktop"; 
    internal static TaskCompletionSource<bool> tcs = new();
    internal static DownloadQueueManager downloadQueueManager;

    [STAThread]
    public static void Main(string[] args)
    {
        LeaderAnalytics.Core.EnvironmentName environmentName = LeaderAnalytics.Core.RuntimeEnvironment.GetEnvironmentName();
        OSPlatform os = FindOSPlatform();
        string configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LeaderAnalytics", "Vyntix", "Observer.Desktop");
        string configSource = environmentName == EnvironmentName.production ? AppContext.BaseDirectory : configFileSourceFolder;
        ConfigHelper.CopyConfigFromSource(environmentName, configSource, configFilePath); 
        string logFolder = "logs/"; // fallback location if we cannot read config
        Exception startupEx = null;
        IConfigurationRoot appConfig = null;
        PhotinoBlazorApp app = null;

        // Configure logging

        try
        {
            appConfig = ConfigHelper.BuildConfig(environmentName, configFilePath).Result;  // Creates configFilePath if it does not exist.
            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(appConfig).CreateLogger();
        }
        catch (Exception ex)
        {
            startupEx = ex;
        }

        if (startupEx != null)
        {
            Log.Logger = new LoggerConfiguration()
           .WriteTo.File(logFolder, rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information)
           .Enrich.FromLogContext()
           .WriteTo.Console()
           .CreateLogger();

            Log.Fatal("An exception occured during startup configuration.  Program execution will not continue.");
            Log.Fatal(startupEx.ToString());
            Log.CloseAndFlush();
            Thread.Sleep(1000);
            return;
        }

        // Build app

        try
        {
            VelopackApp.Build().Run();
            Log.Information("VelopackApp ran successfully.");
            Log.Information("Operating system is {o}", os.ToString());
            Log.Information("ConfigFilePath is {c}", configFilePath);
            AppState appState = new(new UserSettingsService(configFilePath), os, appConfig["ProgramUpdateURL"]);  // UserSettings read from disk and loaded here.
            string apiKey = appConfig["FredAPI_Key"];
            IEnumerable<IEndPointConfiguration> endPoints = appConfig.GetSection("EndPoints").Get<IEnumerable<EndPointConfiguration>>();
            var builder = PhotinoBlazorAppBuilder.CreateDefault(args);
            // Cannot call UseServiceProviderFactory() on PhotinoBlazorAppBuilder since it does not implement IHostBuilder.
            // Add the Autofac container to the Photino service collection and inject it as needed.
            ContainerBuilder containerBuilder = new();
            RegistrationHelper registrationHelper = new RegistrationHelper(containerBuilder);
            registrationHelper.RegisterEndPoints(endPoints);
            registrationHelper.AddFredDownloaderServices();
            containerBuilder.RegisterInstance(appState);
            containerBuilder.RegisterModule(new LeaderAnalytics.AdaptiveClient.EntityFrameworkCore.AutofacModule());
            
            if (!(endPoints?.Any(x => x.IsActive) ?? false))
                throw new Exception("No active endPoints were found.  Make sure one or more endPoints are defined in appsettings.json and that IsActive is set to true for one endPoint only.");
            else if (endPoints.Count(x => x.IsActive) > 1)
                throw new Exception("Only one endPoint can be active at a time.  Check the EndPoints section in appsettings.json and make sure IsActive is set to True for one endPoint only.");

            containerBuilder.RegisterInstance(endPoints.First(x => x.IsActive)).SingleInstance();

            containerBuilder.Register<DownloadQueueManager>((c, p) => 
            {
                IComponentContext cxt = c.Resolve<IComponentContext>();
                IAdaptiveClient<IAPI_Manifest> serviceClient = cxt.Resolve<IAdaptiveClient<IAPI_Manifest>>();
                ILogger<DownloadQueueManager> logger = cxt.Resolve<ILogger<DownloadQueueManager>>();
                return new DownloadQueueManager(serviceClient, tcs, logger);
            }).SingleInstance();

            containerBuilder.Register<Action<string>>((c,p) =>
            {
                IComponentContext cxt = c.Resolve<IComponentContext>();
                DownloadQueueManager dm = cxt.Resolve<DownloadQueueManager>();
                return x => dm.OnDownloadStatusMessage(x); 
            }).SingleInstance();

            builder.RootComponents.Add<App>("#app");
            FredClientConfig config = new FredClientConfig { MaxDownloadRetries = 3, ErrorDelay = 2000, MaxRequestsPerMinute = 60 };
            builder.Services.AddFredClient().UseAPIKey(apiKey).UseConfig(x => config);
            builder.Services.AddLogging(x => x.AddConsole().AddSerilog());
            builder.Services.AddMudServices();
            builder.Services.AddMessageBoxBlazor();
            builder.Services.AddLeaderPivot();
            builder.Services.AddSingleton(new MudThemeProvider());
            containerBuilder.Populate(builder.Services);
            IContainer container = containerBuilder.Build();
            builder.Services.AddSingleton(typeof(IContainer), container);
            app = builder.Build();
            

            IContainer autofac = app.Services.GetRequiredService<IContainer>();
            ILifetimeScope scope = autofac.BeginLifetimeScope();
            downloadQueueManager = scope.Resolve<DownloadQueueManager>();

            // Start polling the download queue 
            Task.Run(downloadQueueManager.StartQueueProcessing); 
            app.MainWindow.SetIconFile("icon.ico").SetTitle("Observer").SetSize(new System.Drawing.Size(1200,800)); //width,height
            app.MainWindow.RegisterWindowClosingHandler((x, y) => Task.Run(async () => await MainWindowClosing(x, y)).Result);
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
            //  AppDomain.CurrentDomain.UnhandledException += (sender, error) => HandleException(app, error.ExceptionObject as Exception);
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

    private static async Task<bool> MainWindowClosing(object sender, EventArgs e)
    {
        Log.Debug("App shutdown has been requested.  Stopping DownloadQueueManager");
        downloadQueueManager.ShutDown();
        Log.Debug("App shutdown has been requested.  Waiting for in-process download jobs to complete.");
        await tcs.Task;
        Log.Debug("App shutdown has been requested. All in-process download jobs have ended normally.  App will shut down.");
        return false; // true prevents window from closing
    }

    private static OSPlatform FindOSPlatform()
    {
        OSPlatform platform;

        if (OperatingSystem.IsWindows())
            platform = OSPlatform.Windows;
        else if (OperatingSystem.IsIOS())
            platform = OSPlatform.OSX;
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
            platform = OSPlatform.Linux;
        else
            throw new Exception("Your operating system is not supported.");

        return platform;
    }
}

