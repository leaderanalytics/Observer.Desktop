namespace LeaderAnalytics.Observer.Desktop;

public static class ConfigHelper
{
    public const string ConfigFileFolder = "O:\\LeaderAnalytics\\Config\\Vyntix.Fred.FredClient";


    public async static Task<IConfigurationRoot> BuildConfig(EnvironmentName envName, string configFilePath)
    {
        // if we are in development, load the appsettings file in the out-of-repo location.
        // if we are in prod, load appsettings.production.json and populate the secrets 
        // from the azure vault.

        string sourceConfigFilePath = string.Empty;

        if (envName == LeaderAnalytics.Core.EnvironmentName.local || envName == LeaderAnalytics.Core.EnvironmentName.development)
            sourceConfigFilePath = ConfigFileFolder;

        string targetFileName = Path.Combine(configFilePath, $"appsettings.{envName}.json");


        if (!File.Exists(targetFileName))
        {
            // If the target does not exist copy it.
            string sourceFileName = Path.Combine(sourceConfigFilePath, $"appsettings.{envName}.json");
            
            try
            {
                if(!Directory.Exists(configFilePath)) 
                    Directory.CreateDirectory(configFilePath);
                
                
                File.Copy(sourceFileName, targetFileName);
            }
            catch (Exception ex) 
            {
                throw new Exception($"An error occured while attempting to create folder {configFilePath} or copy a configuration file from source folder {sourceFileName} to target folder {targetFileName}.  See inner exception.", ex);
            }
        }

        var cfg = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile(targetFileName, optional: false)
                    .AddEnvironmentVariables().Build();

        if (envName == LeaderAnalytics.Core.EnvironmentName.production)
        {
            
        }

        return cfg;
    }
}
