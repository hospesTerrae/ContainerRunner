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
    private readonly IBackgroundQueue<Image> _upQueue;
    private readonly IBackgroundQueue<Container> _downQueue;
    private readonly IContainerStateService _containerStateService;

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
        var cancellationToken = new CancellationToken();
        _upQueue.Enqueue(image, cancellationToken);
    }

    [HttpDelete]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(Reason), 404)]
    [Route("stop")]
    public async Task Stop([FromBody] Container container)
    {
        await _downQueue.Enqueue(container, new CancellationToken());
    }

    [HttpGet]
    [ProducesResponseType(typeof(ContainerState),200)]
    [ProducesResponseType(typeof(Reason), 404)]
    [Route("status")]
    public async Task<ContainerState> GetInfo([FromQuery] string containerId)
    {
        return _containerStateService.GetStatus(containerId);
    }

    [HttpGet]
    [ProducesResponseType(typeof(Dictionary<string, ContainerState>),200)]
    [ProducesResponseType(typeof(Reason), 404)]
    [Route("statusAll")]
    public async Task<Dictionary<string, ContainerState>> GetInfoAll()
    {
        return _containerStateService.GetAllStatuses();
    }
}