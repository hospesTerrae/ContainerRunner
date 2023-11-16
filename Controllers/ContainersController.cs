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
        await _upQueue.EnqueueAsync(image, default);
    }

    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(Reason), 404)]
    [Route("statusAll")]
    public async Task<Dictionary<string, string>> GetInfoAll()
    {
        return _containerStateService.GetAllStatuses();
    }

    [HttpDelete]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(Reason), 404)]
    [Route("stop")]
    public async Task Stop([FromBody] Container container)
    {
        await _downQueue.EnqueueAsync(container, new CancellationToken());
    }

    [HttpDelete]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(Reason), 404)]
    [Route("stopAll")]
    public async Task<List<string>> stopAll()
    {
        var containers = _containerStateService.GetAllStatuses().Keys.ToList();
        var ct = new CancellationToken();

        foreach (var container in containers) await _downQueue.EnqueueAsync(new Container { Id = container }, ct);

        return containers;
    }
}