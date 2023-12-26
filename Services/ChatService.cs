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

    public override async Task SendAndReceiveMessages(IAsyncStreamReader<ChatMessage> requestStream, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
    {
        Console.WriteLine($"INFO: Received message request from {context.Peer} with host {context.Host}");
        await foreach (var message in requestStream.ReadAllAsync())
        {
            await responseStream.WriteAsync(message);
        }
    }
}
