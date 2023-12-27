using System.Threading.Channels;
using Grpc.Core;

namespace chat_dotnet.Services;

public class MessageBroadcaster : IMessageBroadcaster
{
    private readonly ILogger<MessageBroadcaster> _logger;
    private readonly SemaphoreSlim _clientsLock = new SemaphoreSlim(1);
    private readonly List<Tuple<IServerStreamWriter<ChatMessage>, CancellationToken>> _clients = new List<Tuple<IServerStreamWriter<ChatMessage>, CancellationToken>>();
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
                try
                {
                    _logger.LogInformation("Attempting to broadcast message {} to {} clients", message, _clients.Count);
                    int numSuccessfulBroadcasts = 0;
                    for (int i = _clients.Count - 1; i >= 0; i--) {
                        var client = _clients[i];
                        // TODO: do not echo to the sender
                        // TODO: do not use locks instead use a channel for adding / removing client
                        // TODO: solve the problem of slow clients
                        if (client.Item2.IsCancellationRequested)
                        {
                            _clients.RemoveAt(i);
                        }
                        else
                        {
                            try
                            {
                                await client.Item1.WriteAsync(message, client.Item2);
                                numSuccessfulBroadcasts++;
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, "Error while broadcasting message to client {}", client);
                                _clients.RemoveAt(i);
                            }
                        }
                    }
                    _logger.LogInformation("Successfully broadcast message {} to {} clients", message, numSuccessfulBroadcasts);
                }
                finally
                {
                    _clientsLock.Release();
                }
            }
        });
    }

    public async Task AddClient(IServerStreamWriter<ChatMessage> client, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adding a new client {}", client);
        await _clientsLock.WaitAsync();
        try
        {
            _clients.Add(new Tuple<IServerStreamWriter<ChatMessage>, CancellationToken>(client, cancellationToken));
        }
        finally
        {
            _clientsLock.Release();
        }
    }

    public async Task BroadcastMessage(ChatMessage message)
    {
        await _channel.Writer.WriteAsync(message);
    }
}