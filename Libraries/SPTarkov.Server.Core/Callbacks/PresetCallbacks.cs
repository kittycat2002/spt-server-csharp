using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;

namespace SPTarkov.Server.Core.Callbacks;

[Injectable(TypePriority = OnLoadOrder.PresetCallbacks)]
public class PresetCallbacks(PresetController presetController) : IOnLoad
{
    public Task OnLoad(CancellationToken stoppingToken)
    {
        presetController.Initialize();
        return Task.CompletedTask;
    }
}
