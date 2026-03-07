using Microsoft.Extensions.AI;
using Microsoft.ML.OnnxRuntimeGenAI;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace AI.ChatClientProviders.Onnx;

public class OnnxChatClientBase : IChatClient
{
	#region fields
	private Microsoft.ML.OnnxRuntimeGenAI.Model? _model;
	private Tokenizer? _tokenizer;
	private ChatClientMetadata? _metadata;
	private CancellationTokenSource? _cts;
	#endregion

	#region properties
	public CancellationTokenSource CancellationTokenSource
	{
		get => _cts;
		set => _cts = value;
	}

	public ChatClientMetadata Metadata => _metadata ?? new ChatClientMetadata();

	public virtual string DownloadBaseUrl { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public virtual string BaseLLM { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public virtual string BaseConfigFile { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	public virtual Dictionary<string, string> FilesToDownload { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
	#endregion

	#region ctor
	public OnnxChatClientBase()
	{

	}
	#endregion

	#region static
	public static OnnxChatClientBase Create(string modelPath)
	{
		var client = new OnnxChatClientBase();
		client.InitializeModel(modelPath);
		return client;
	}
	#endregion

	public virtual bool InitializeModel(string modelPath)
	{
		try
		{
			_model = new Microsoft.ML.OnnxRuntimeGenAI.Model(modelPath);
			_tokenizer = new Tokenizer(_model);
			_metadata = new ChatClientMetadata(
				"MauiOnnxAiTooling",
				new Uri(""),
				"GenAI Model"
			);
			return true;
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"Failed to load model: {ex.Message}", ex);
		}
	}

	public virtual void Dispose()
	{
		_tokenizer?.Dispose();
		_model?.Dispose();
	}

	public virtual async Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> chatMessages, 
		ChatOptions? options = null, 
		CancellationToken cancellationToken = default)
	{
		var sb = new StringBuilder();

		// Use ConfigureAwait(false) to avoid locking the UI thread while buffering
		await foreach (var update in GetStreamingResponseAsync(chatMessages, options, cancellationToken).ConfigureAwait(false))
		{
			if (!string.IsNullOrEmpty(update.Text))
			{
				sb.Append(update.Text);
			}
		}

		var fullText = sb.ToString().Trim();

		return new ChatResponse(new[] { new ChatMessage(ChatRole.Assistant, fullText) });
	}

	public virtual async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> chatMessages, 
		ChatOptions? options = null, 
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		if (_model == null || _tokenizer == null) throw new InvalidOperationException("Model not initialized.");

		// Move the entire heavy lifting to a background thread ONCE
		var responseQueue = System.Threading.Channels.Channel.CreateUnbounded<string>();

		_ = Task.Run(() =>
		{
			try
			{
				using var generatorParams = new GeneratorParams(_model);
				var prompt = BuildPrompt(chatMessages, options);
				using var sequences = _tokenizer.Encode(prompt);

				// Map Microsoft.Extensions.AI.ChatOptions to ONNX Params
				var maxTokens = options?.MaxOutputTokens ?? 2048;
				generatorParams.SetSearchOption("max_length", maxTokens);
				generatorParams.SetSearchOption("temperature", options?.Temperature ?? 0.7f);

				using var generator = new Generator(_model, generatorParams);
				generator.AppendTokenSequences(sequences);

				using var tokenizerStream = _tokenizer.CreateStream();

				while (!generator.IsDone() && !cancellationToken.IsCancellationRequested)
				{
					generator.GenerateNextToken();
					var newTokenId = generator.GetSequence(0)[^1];
					var decodedChunk = tokenizerStream.Decode(newTokenId);

					if (!string.IsNullOrEmpty(decodedChunk))
						responseQueue.Writer.TryWrite(decodedChunk);
				}
			}
			catch (Exception ex) { /* Log or handle */ }
			finally { responseQueue.Writer.Complete(); }
		}, cancellationToken);

		// Stream from the channel back to the caller
		await foreach (var chunk in responseQueue.Reader.ReadAllAsync(cancellationToken))
		{
			yield return new ChatResponseUpdate
			{
				Role = ChatRole.Assistant,
				Contents = [new TextContent(chunk)]
			};
		}
	}

	public virtual object? GetService(Type serviceType, object? serviceKey = null)
	{
		return serviceType == typeof(IChatClient) ? this : null;
	}

	public virtual string BuildPrompt(IEnumerable<ChatMessage> messages, ChatOptions? options = null)
	{
		//var sb = new StringBuilder();

		//foreach (var msg in messages)
		//{
		//	if (msg.Role == ChatRole.System)
		//	{
		//		sb.Append($"<|system|>\n{msg.Text}<|end|>\n");
		//	}
		//	else if (msg.Role == ChatRole.User)
		//	{
		//		sb.Append($"<|user|>\n{msg.Text}<|end|>\n");
		//	}
		//	else if (msg.Role == ChatRole.Assistant)
		//	{
		//		// Handle previous tool calls in history
		//		var funcCall = msg.Contents.OfType<FunctionCallContent>().FirstOrDefault();
		//		if (funcCall != null)
		//		{
		//			var jsonCall = JsonSerializer.Serialize(new { name = funcCall.Name, arguments = funcCall.Arguments });
		//			sb.Append($"<|assistant|>\n >!{jsonCall}<|end|>\n");
		//		}
		//		else
		//		{
		//			sb.Append($"<|assistant|>\n >!{msg.Text}<|end|>\n");
		//		}
		//	}
		//	else if (msg.Role == ChatRole.Tool)
		//	{
		//		var result = msg.Contents.OfType<FunctionResultContent>().FirstOrDefault();
		//		if (result != null)
		//		{
		//			var resultJson = JsonSerializer.Serialize(result.Result);
		//			sb.Append($"<|tool|>\n{resultJson}<|end|>\n");
		//		}
		//	}
		//}
		//sb.Append("<|assistant|>\n");
		//return sb.ToString();

		// Consider moving this to a 'IPromptTemplate' strategy
		var sb = new StringBuilder();
		foreach (var msg in messages)
		{
			var roleTag = msg.Role.Value.ToLower() switch
			{
				"system" => "system",
				"user" => "user",
				"assistant" => "assistant",
				_ => "user"
			};
			sb.Append($"<|{roleTag}|>\n{msg.Text}<|end|>\n");
		}
		sb.Append("<|assistant|>\n");
		return sb.ToString();
	}

	public virtual async Task CancelResponse()
	{
		_cts?.Cancel();
	}
}
