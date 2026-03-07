using Microsoft.Extensions.AI;

namespace AI.ChatClientProviders.LlamaServer;

public class LlamaServerClient : IChatClientProvider, IChatClient
{
	public string DownloadBaseUrl { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public string BaseLLM { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public string BaseConfigFile { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public Dictionary<string, string> FilesToDownload { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public CancellationTokenSource CancellationTokenSource { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

	public Task CancelResponse()
	{
		throw new NotImplementedException();
	}

	public void Dispose()
	{
		throw new NotImplementedException();
	}

	public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}

	public object? GetService(Type serviceType, object? serviceKey = null)
	{
		throw new NotImplementedException();
	}

	public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}

	public bool InitializeModel(string modelPath)
	{
		throw new NotImplementedException();
	}
}
