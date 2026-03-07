using AI.ChatClientProviders;
using AI.ChatClientProviders.Onnx;

namespace MauiOnnxAiTooling.AI.ChatClientProviders.Onnx;

public class OnnxWizardVicunaClientBase : OnnxChatClientBase, IChatClientProvider
{
	public override string DownloadBaseUrl { get; set; } = "https://huggingface.co/sharpbai/Wizard-Vicuna-13B-Uncensored-HF-onnx/resolve/main";
	public override string BaseLLM { get; set; } = "decoder_model_merged.onnx";
	public override string BaseConfigFile { get; set; } = "genai_config.json";

	/// <summary>
	/// Gets or sets a dictionary mapping file names to their download targets.
	/// Dictionary(actualfiletodownload, targetfilename)
	/// </summary>
	public override Dictionary<string, string> FilesToDownload { get; set; } = new Dictionary<string, string>()
	{
		{ "config.json", "genai_config.json" },
		{ "tokenizer.json", "tokenizer.json" },
		{ "tokenizer.model", "tokenizer.model" },
		{ "tokenizer_config.json", "tokenizer_config.json" },
		{ "special_tokens_map.json", "special_tokens_map.json" },
		{ "generation_config.json", "generation_config.json" },
		{ "decoder_model_merged.onnx", "decoder_model_merged.onnx" },
		//{ "decoder_model_merged.onnx_data", "decoder_model_merged.onnx_data" }
	};
}
