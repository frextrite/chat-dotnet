using System.Threading.Channels;
using Grpc.Core;

namespace chat_dotnet.Services;

public interface IMessageBroadcaster
{
    Task AddClient(string client, Channel<ChatMessage> channel);
    Task RemoveClient(string client);
    Task BroadcastMessage(ChatMessage message);
}