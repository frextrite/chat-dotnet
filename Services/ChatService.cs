using System.Threading.Channels;
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

        var broadcastedMessages = Channel.CreateUnbounded<ChatMessage>();
        // token should get cancelled when SendAndReceiveMessages completes (returns or throws exception)
        // does this work?
        var writeTask = Task.Run(async () => {
            // throws exceptions and exits cleanly?
            while (await broadcastedMessages.Reader.WaitToReadAsync(context.CancellationToken))
            {
                var message = await broadcastedMessages.Reader.ReadAsync(context.CancellationToken);
                await responseStream.WriteAsync(message, context.CancellationToken);
            }
        });

        await _broadcaster.AddClient(context.Peer, broadcastedMessages);
        // gRPC docs mention MoveNext() cannot throw in service implementation
        // but it does...
        // https://github.com/grpc/grpc-dotnet/issues/1219
        // Throwing exception might still be valid, verify and remove try catch
        try
        {
            while (await requestStream.MoveNext())
            {
                var message = requestStream.Current;
                await _broadcaster.BroadcastMessage(message);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while receiving messages from client {}", context.Peer);
        }

        await _broadcaster.RemoveClient(context.Peer);
    }
}
