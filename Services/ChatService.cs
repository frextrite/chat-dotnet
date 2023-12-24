using Grpc.Core;

namespace chat_dotnet.Services;

public class ChatterService : Chatter.ChatterBase
{
    private readonly ILogger<ChatterService> _logger;
    public ChatterService(ILogger<ChatterService> logger)
    {
        _logger = logger;
    }

    public override Task<HealthReply> ProbeHealth(HealthRequest request, ServerCallContext context)
    {
        Console.WriteLine($"INFO: Received health probe request from {context.Peer} with host {context.Host}");
        return Task.FromResult(new HealthReply{});
    }
}
