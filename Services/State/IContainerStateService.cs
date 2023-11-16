using ContainerRunner.Enums;

namespace ContainerRunner.Services.State;

public interface IContainerStateService
{
    ContainerState GetStatus(string containerId);
    void UpdateStatus(string containerId, ContainerState newStatus);
    Dictionary<string, string> GetAllStatuses();
}