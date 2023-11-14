using ContainerRunner.Models;
using ContainerRunner.Models.Exceptions;
using ContainerRunner.Services.DockerApi;
using ContainerRunner.Services.Queue;
using Microsoft.AspNetCore.Mvc;

namespace ContainerRunner.Controllers;

[ApiController]
[Route("[controller]")]
public class ContainersController : ControllerBase
{
    private readonly IDockerApiService _dockerApiService;
    private IBackgroundQueue<Image> _upQueue;
    private IBackgroundQueue<Container> _downQueue;

    public ContainersController(IDockerApiService dockerApiService, IBackgroundQueue<Image> upQueue,
        IBackgroundQueue<Container> downQueue)
    {
        _dockerApiService = dockerApiService;
        _upQueue = upQueue;
        _downQueue = downQueue;
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
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(Reason), 404)]
    [Route("status")]
    public async Task<IEnumerable<ContainerStatus>> GetInfo([FromQuery] string name)
    {
        var containers = await _dockerApiService.GetContainers();
        var statuses = containers.Select(c => new ContainerStatus
        {
            Image = c.Image,
            Status = c.Status,
            State = c.State,
            ContainerId = c.ID,
            ContainerName = c.Names.FirstOrDefault()
        });

        return statuses;
    }
}