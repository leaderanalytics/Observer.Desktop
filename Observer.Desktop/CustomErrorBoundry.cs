using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace LeaderAnalytics.Observer.Desktop;

public class CustomErrorBoundry : ErrorBoundary
{
    [Inject] public ILogger<CustomErrorBoundry> logger { get; set; }


    protected override async Task OnErrorAsync(Exception exception)
    {
        string msg = $"An error was caught by the ErrorBoundry global exception handler. The exception is: {exception.ToString()}"; 
        Console.WriteLine(msg);
        logger.LogError(msg);
        await base.OnErrorAsync(exception);
    }
}
