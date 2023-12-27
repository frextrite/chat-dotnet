using Grpc.Core;

namespace chat_dotnet.Services;

public class ChatterService : Chatter.ChatterBase
{
    private readonly ILogger<ChatterService> _logger;
    private readonly IMessageBroadcaster _broadcaster;

    public ChatterService(ILogger<ChatterService> logger, IMessageBroadcaster broadcaster)
    {
        _logger = logger;
        _broadcaster = broadcaster;
    }

    public override Task<HealthReply> ProbeHealth(HealthRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Received health probe request from {} with host {}", context.Peer, context.Host);
        return Task.FromResult(new HealthReply{});
    }

    public override async Task SendAndReceiveMessages(IAsyncStreamReader<ChatMessage> requestStream, IServerStreamWriter<ChatMessage> responseStream, ServerCallContext context)
    {
        _logger.LogInformation("Received message request from {} with host {}", context.Peer, context.Host);
        await _broadcaster.AddClient(responseStream, context.CancellationToken);
        while (await requestStream.MoveNext())
        {
            var message = requestStream.Current;
            await _broadcaster.BroadcastMessage(message);
        }
    }
}
