using ContainerRunner.Models;
using ContainerRunner.Services.DockerApi;
using Microsoft.AspNetCore.Mvc;

namespace ContainerRunner.Controllers;

[ApiController]
[Route("[controller]")]
public class ContainersController : ControllerBase
{
    private IDockerApiService _dockerApiService;

    public ContainersController(IDockerApiService dockerApiService)
    {
        _dockerApiService = dockerApiService;
    }

    [HttpPost]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(Reason), 404)]
    [Route("start")]
    public async Task Start([FromBody] Image image)
    {
        var cancellationToken = new CancellationToken();
        await _dockerApiService.CreateContainer(image, cancellationToken);
    }

    [HttpDelete]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(Reason), 404)]
    [Route("stop")]
    public async Task Stop([FromBody] Container container)
    {
        await _dockerApiService.DeleteContainer(container, new CancellationToken());
    }

    [HttpGet]
    [ProducesResponseType(200)]
    [ProducesResponseType(typeof(Reason), 404)]
    [Route("status")]
    public async Task<IEnumerable<ContainerStatus>> GetInfo([FromQuery] string name)
    {
        var containers = await _dockerApiService.GetContainers();
        var statuses = containers.Select(c => new ContainerStatus()
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