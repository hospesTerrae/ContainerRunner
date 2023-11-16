using System.Threading.Channels;
using ContainerRunner.Models;

namespace ContainerRunner.Services.Queue;

public class UpQueue : AbstractBackgroundQueue<Image>
{
    private static readonly UnboundedChannelOptions _options = new();

    public UpQueue(ILogger<UpQueue> logger) : base(_options, logger)
    {
    }
}