using System.Threading.Channels;
using Grpc.Core;

namespace chat_dotnet.Services;

public class MessageBroadcaster : IMessageBroadcaster
{
    private readonly ILogger<MessageBroadcaster> _logger;
    private readonly SemaphoreSlim _clientsLock = new SemaphoreSlim(1);
    private readonly Dictionary<string, Channel<ChatMessage>> _clients = new Dictionary<string, Channel<ChatMessage>>();
    private readonly Channel<ChatMessage> _channel = Channel.CreateUnbounded<ChatMessage>();

    public MessageBroadcaster(ILogger<MessageBroadcaster> logger)
    {
        _logger = logger;
        StartBroadcastingLoop();
    }

    private void StartBroadcastingLoop()
    {
        Task.Run(async () => {
            while (await _channel.Reader.WaitToReadAsync())
            {
                var message = await _channel.Reader.ReadAsync();
                await _clientsLock.WaitAsync();
                // TODO: do not use locks instead use a channel for adding / removing client
                // TODO: remove this try block and handle exception near WriteAsync
                try
                {
                    _logger.LogInformation("Broadcasting message {} to {} clients", message, _clients.Count);
                    // TODO: do not echo to the sender
                    foreach (var client in _clients)
                    {
                        await client.Value.Writer.WriteAsync(message);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while broadcasting message {}", message);
                }
                finally
                {
                    _clientsLock.Release();
                }
            }
        });
    }

    public async Task AddClient(string client, Channel<ChatMessage> channel)
    {
        _logger.LogInformation("Adding a new client {}", client);
        await _clientsLock.WaitAsync();
        try
        {
            _clients.Add(client, channel);
        }
        finally
        {
            _clientsLock.Release();
        }
    }

    public async Task RemoveClient(string client)
    {
        _logger.LogInformation("Removing client {}", client);
        await _clientsLock.WaitAsync();
        try
        {
            _clients.Remove(client);
        }
        finally
        {
            _clientsLock.Release();
        }
    }

    // Internal queue is needed to serialize calls to BroadcastMessage from multi-threaded gRPC environment
    // Alternative was to move the processing inside BroadcastMessage but the client loop in that case should be protected by a lock which will lead to high lock contention
    public async Task BroadcastMessage(ChatMessage message)
    {
        await _channel.Writer.WriteAsync(message);
    }
}