namespace LeaderAnalytics.Observer.Desktop;

public static class ConfigHelper
{

    public async static Task<IConfigurationRoot> BuildConfig(EnvironmentName envName, string configFilePath)
    {
        string targetFileName = Path.Combine(configFilePath, $"appsettings.{envName}.json");
        
        var cfg = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile(targetFileName, optional: false)
                    .AddEnvironmentVariables().Build();
        return cfg;
    }

    public static void CopyConfigFromSource(EnvironmentName envName, string sourceFolder, string destFolder)
    {
        string targetFileName = Path.Combine(destFolder, $"appsettings.{envName}.json");

        // If the config file does not exist, copy it from the source folder
        if (!File.Exists(targetFileName))
        {
            string sourceFileName = Path.Combine(sourceFolder, $"appsettings.{envName}.json");
            
            try
            {
                if (!Directory.Exists(destFolder))
                    Directory.CreateDirectory(destFolder);


                File.Copy(sourceFileName, targetFileName);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occured while attempting to create folder {destFolder} or copy a configuration file from source folder {sourceFolder} to target folder {destFolder}.  See inner exception.", ex);
            }
        }
    }
}
