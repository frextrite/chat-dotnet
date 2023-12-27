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
                    _logger.LogInformation("Broadcasting message {} to {} clients", message, _clients.Count);
                    foreach (var client in _clients) {
                        // TODO: do not echo to the sender
                        // TODO: do not use locks instead use a channel for adding / removing client
                        // TODO: solve the problem of slow clients
                        if (!client.Item2.IsCancellationRequested)
                        {
                            await client.Item1.WriteAsync(message, client.Item2);
                        }
                        // TODO: else remove this client
                        // TODO: handle race between iscancellationrequested and writeasync
                    }
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