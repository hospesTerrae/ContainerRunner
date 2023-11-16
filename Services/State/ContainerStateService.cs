using System.Collections.Concurrent;
using ContainerRunner.Enums;
using ContainerRunner.Exceptions;

namespace ContainerRunner.Services.State;

public class ContainerStateService : IContainerStateService
{
    private static readonly ConcurrentDictionary<string, ContainerState> _dict = new();
    private readonly ILogger<ContainerStateService> _logger;

    public ContainerStateService(ILogger<ContainerStateService> logger)
    {
        _logger = logger;
    }

    public ContainerState GetStatus(string containerId)
    {
        _logger.Log(LogLevel.Debug, $"Requesting container [{containerId}] status");
        if (_dict.TryGetValue(containerId, out var status))
            return status;

        throw new ContainerNotFoundException($"Container {containerId} not found");
    }

    public void UpdateStatus(string containerId, ContainerState newStatus)
    {
        _logger.Log(LogLevel.Debug, $"Updating container [{containerId}] status to [{newStatus}]");
        _dict[containerId] = newStatus;
    }

    public Dictionary<string, string> GetContainersStatuses(ContainerState? state = null)
    {
        if (state == null)
            return _dict.ToDictionary(kvp => kvp.Key, kvp => Enum.GetName(typeof(ContainerState), kvp.Value),
                _dict.Comparer);

        return _dict.Where(pair => pair.Value == state).ToDictionary(kvp => kvp.Key,
            kvp => Enum.GetName(typeof(ContainerState), kvp.Value),
            _dict.Comparer);
    }
}