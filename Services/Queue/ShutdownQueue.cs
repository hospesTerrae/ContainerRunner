using System.Threading.Channels;
using ContainerRunner.Models;

namespace ContainerRunner.Services.Queue;

public class ShutdownQueue : AbstractBackgroundQueue<Container>
{
    private static readonly UnboundedChannelOptions _options = new();

    public ShutdownQueue(ILogger<ShutdownQueue> logger) : base(_options, logger)
    {
    }
}