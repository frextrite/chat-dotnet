using System.Threading.Channels;
using Grpc.Core;

namespace chat_dotnet.Services;

public class MessageBroadcaster : IMessageBroadcaster
{
    private readonly SemaphoreSlim _clientsLock = new SemaphoreSlim(1);
    private readonly List<IServerStreamWriter<ChatMessage>> _clients = new List<IServerStreamWriter<ChatMessage>>();
    private readonly Channel<ChatMessage> _channel = Channel.CreateUnbounded<ChatMessage>();

    public MessageBroadcaster()
    {
        Task.Run(async () => {
            while (await _channel.Reader.WaitToReadAsync())
            {
                var message = await _channel.Reader.ReadAsync();
                await _clientsLock.WaitAsync();
                try
                {
                    Console.WriteLine($"INFO: Broadcasting message {message} to {_clients.Count} clients");
                    foreach (var client in _clients) {
                        // TODO: handle broken streams
                        // TODO: do not echo to the sender
                        // TODO: do not use locks instead use a channel for adding / removing client
                        // TODO: solve the problem of slow clients
                        await client.WriteAsync(message);
                    }
                }
                finally
                {
                    _clientsLock.Release();
                }
            }
        });
    }

    public async Task AddClient(IServerStreamWriter<ChatMessage> client)
    {
        Console.WriteLine($"INFO: Adding a new client {client}");
        await _clientsLock.WaitAsync();
        try
        {
            _clients.Add(client);
        }
        finally
        {
            _clientsLock.Release();
        }
    }

    public async Task BroadcastMessage(ChatMessage message)
    {
        Console.WriteLine($"INFO: Broadcasting message {message}");
        await _channel.Writer.WriteAsync(message);
    }
}