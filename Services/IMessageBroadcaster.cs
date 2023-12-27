using Grpc.Core;

namespace chat_dotnet.Services;

public interface IMessageBroadcaster
{
    Task AddClient(IServerStreamWriter<ChatMessage> client);
    Task BroadcastMessage(ChatMessage message);
}