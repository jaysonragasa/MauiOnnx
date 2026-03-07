using AI.ChatClientProviders;
using AI.ChatClientProviders.Onnx;
using Microsoft.ML.OnnxRuntimeGenAI;

namespace MauiOnnxAiTooling.AI.ChatClientProviders.Onnx;

public class OnnxPhi3ClientBase : OnnxChatClientBase, IChatClientProvider
{
	public override string DownloadBaseUrl { get; set; } = "https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-onnx/resolve/main/cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4";
	public override string BaseLLM { get; set; } = "phi3-mini-4k-instruct-cpu-int4-rtn-block-32-acc-level-4.onnx";
	public override string BaseConfigFile { get; set; } = "genai_config.json";

	/// <summary>
	/// Gets or sets a dictionary mapping file names to their download targets.
	/// Dictionary(actualfiletodownload, targetfilename)
	/// </summary>
	public override Dictionary<string, string> FilesToDownload { get; set; } = new Dictionary<string, string>()
	{
		{ "genai_config.json", "genai_config.json" },
		{ "tokenizer.json", "tokenizer.json" },
		{ "tokenizer_config.json", "tokenizer_config.json" },
		{ "special_tokens_map.json", "special_tokens_map.json" },
		{ "added_tokens.json", "added_tokens.json" },
		{ "phi3-mini-4k-instruct-cpu-int4-rtn-block-32-acc-level-4.onnx", "phi3-mini-4k-instruct-cpu-int4-rtn-block-32-acc-level-4.onnx" },
		{ "phi3-mini-4k-instruct-cpu-int4-rtn-block-32-acc-level-4.onnx.data", "phi3-mini-4k-instruct-cpu-int4-rtn-block-32-acc-level-4.onnx.data" }
	};
}
