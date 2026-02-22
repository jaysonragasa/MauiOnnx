using Microsoft.Extensions.AI;

namespace AI.ChatClientProviders;
public interface IChatClientProvider : IChatClient
{
	string DownloadBaseUrl { get; set; }
	string BaseLLM { get; set; }
	Dictionary<string, string> FilesToDownload { get; set; }
	bool InitializeModel(string modelPath);
}