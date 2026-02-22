using AI.ChatClientProviders;
using Microsoft.Extensions.AI;

namespace MauiOnnxAiTooling.AI.ChatClientProviders.Onnx;

public class Phi3ClientBase : IChatClientProvider
{
	public string DownloadBaseUrl { get; set; } = "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-onnx/resolve/main/cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4";
	public string BaseLLM { get; set; } = "phi3-mini-4k-instruct-cpu-int4-rtn-block-32-acc-level-4.onnx";

	/// <summary>
	/// Gets or sets a dictionary mapping file names to their download targets.
	/// Dictionary(actualfiletodownload, targetfilename)
	/// </summary>
	public Dictionary<string, string> FilesToDownload { get; set; } = new Dictionary<string, string>()
	{
		{ "genai_config.json", "genai_config.json" },
		{ "tokenizer.json", "tokenizer.json" },
		{ "tokenizer_config.json", "tokenizer_config.json" },
		{ "special_tokens_map.json", "special_tokens_map.json" },
		{ "added_tokens.json", "added_tokens.json" },
		{ "phi3-mini-4k-instruct-cpu-int4-rtn-block-32-acc-level-4.onnx", "phi3-mini-4k-instruct-cpu-int4-rtn-block-32-acc-level-4.onnx" },
		{ "phi3-mini-4k-instruct-cpu-int4-rtn-block-32-acc-level-4.onnx.data", "phi3-mini-4k-instruct-cpu-int4-rtn-block-32-acc-level-4.onnx.data" }
	};

	public virtual void Dispose()
	{ }

	public virtual Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
	{ return null; }

	public virtual object? GetService(Type serviceType, object? serviceKey = null)
	{ return null; }

	public virtual IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
	{ return null; }

	public virtual bool InitializeModel(string modelPath)
	{ return false; }
}
