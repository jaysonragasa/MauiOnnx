using Microsoft.Extensions.AI;

namespace AI.ChatClientProviders;
public interface IChatClientProvider : IChatClient
{
	CancellationTokenSource CancellationTokenSource { get; set; }
	string DownloadBaseUrl { get; set; }
	string BaseLLM { get; set; }
	string BaseConfigFile { get; set; }
	Dictionary<string, string> FilesToDownload { get; set; }
	bool InitializeModel(string modelPath);
	Task CancelResponse();
}