namespace SPTarkov.Server.Core.DI;

public interface IOnUpdate
{
    Task<bool> OnUpdate(CancellationToken stoppingToken, long secondsSinceLastRun);
}
