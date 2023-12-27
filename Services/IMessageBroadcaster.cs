using Grpc.Core;

namespace chat_dotnet.Services;

public interface IMessageBroadcaster
{
    Task AddClient(IServerStreamWriter<ChatMessage> client, CancellationToken cancellationToken);
    Task BroadcastMessage(ChatMessage message);
}