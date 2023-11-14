using System.Threading.Channels;
using ContainerRunner.Enums;
using ContainerRunner.Models;
using ContainerRunner.Services.State;

namespace ContainerRunner.Services.Queue;

public class ShutdownQueue : AbstractBackgroundQueue<Container>
{
    private static readonly UnboundedChannelOptions _options = new();
    private readonly IContainerStateService _containerStateService;

    public ShutdownQueue(ILogger<ShutdownQueue> logger, IContainerStateService containerStateService) : base(_options,
        logger)
    {
        _containerStateService = containerStateService;
    }

    public override void UpdateStatusAfterDequeued(Container item)
    {
        // updating status to enqueued only from running to prevent multiple attempts to stop the same container
        if (_containerStateService.GetStatus(item.Id) == ContainerState.Running)
            _containerStateService.UpdateStatus(item.Id, ContainerState.EnqueuedToStop);
    }
}