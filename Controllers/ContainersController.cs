using ContainerRunner.Enums;
using ContainerRunner.Models;
using ContainerRunner.Models.Exceptions;
using ContainerRunner.Services.Queue;
using ContainerRunner.Services.State;
using Microsoft.AspNetCore.Mvc;

namespace ContainerRunner.Controllers;

[ApiController]
[Route("[controller]")]
public class ContainersController : ControllerBase
{
    private readonly IContainerStateService _containerStateService;
    private readonly IBackgroundQueue<Container> _downQueue;
    private readonly IBackgroundQueue<Image> _upQueue;

    public ContainersController(IBackgroundQueue<Image> upQueue,
        IBackgroundQueue<Container> downQueue, IContainerStateService containerStateService)
    {
        _upQueue = upQueue;
        _downQueue = downQueue;
        _containerStateService = containerStateService;
    }

    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(Reason), 404)]
    [Route("start")]
    public async Task Start([FromBody] Image image)
    {
        await _upQueue.EnqueueAsync(image);
    }

    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(Reason), 404)]
    [Route("statusAll")]
    public Dictionary<string, string> GetInfoAll()
    {
        return _containerStateService.GetContainersStatuses();
    }

    [HttpDelete]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(Reason), 404)]
    [Route("stop")]
    public async Task Stop([FromBody] Container container)
    {
        var status = _containerStateService.GetStatus(container.Id);
        if (status == ContainerState.Running)
        {
            _containerStateService.UpdateStatus(container.Id, ContainerState.EnqueuedToStop);
            await _downQueue.EnqueueAsync(container);
        }
    }

    [HttpDelete]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(Reason), 404)]
    [Route("stopAll")]
    public async Task<List<string>> StopAll()
    {
        var containers = _containerStateService.GetContainersStatuses(ContainerState.Running).Keys.ToList();

        foreach (var container in containers)
        {
            _containerStateService.UpdateStatus(container, ContainerState.EnqueuedToStop);
            await _downQueue.EnqueueAsync(new Container { Id = container });
        }

        return containers;
    }
}