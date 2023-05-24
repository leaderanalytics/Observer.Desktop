using LeaderAnalytics.Observer.Fred.Services.Domain;
using MudBlazor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observer.Desktop.Pages;
public partial class BasePage : ComponentBase
{
    // This .cs file replaces the .razor file of the same name due to this bug:  https://github.com/dotnet/razor/issues/8755

    [Inject] protected MessageService MessageService { get; set; }
    [Inject] protected IDialogService DialogService { get; set; }
    [Inject] protected IContainer container { get; set; }
    protected IAdaptiveClient<IObserverAPI_Manifest> serviceClient;
    protected const string pagerFormat = "{first_item}-{last_item} of {all_items}";

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        ILifetimeScope scope = container.BeginLifetimeScope();
        serviceClient = scope.Resolve<IAdaptiveClient<IObserverAPI_Manifest>>();
    }
}
