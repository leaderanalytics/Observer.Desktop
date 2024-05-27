using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace LeaderAnalytics.Observer.Desktop;

public class CustomErrorBoundry : ErrorBoundary
{
    [Inject] public ILogger<CustomErrorBoundry> logger { get; set; }
    
    public new Exception? CurrentException => base.CurrentException;

    public string ErrorMessage { get; set; }

    protected override async Task OnErrorAsync(Exception exception)
    {
        ErrorMessage = $"An error was caught by the ErrorBoundry global exception handler. The exception is: {exception.ToString()}"; 
        Console.WriteLine(ErrorMessage);
        logger.LogError(ErrorMessage);
        await base.OnErrorAsync(exception);
    }
}
