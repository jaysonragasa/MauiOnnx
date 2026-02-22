using AI.ChatClientProviders;
using AI.Models;
using AI.Tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.AI;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Windows.Input;

namespace ViewModels;
public class AIChatViewModel : ObservableObject
{
	#region fields
	private readonly IChatClientProvider _chatClient;
	private string _modelPath = string.Empty;
	private AIChatToolRegistration _toolRegistry;
	private CancellationTokenSource _cts = null;
	#endregion

	#region properties
	private ObservableCollection<AIChatMessageModel> _messages = new();
	public ObservableCollection<AIChatMessageModel> Messages
	{
		get => _messages;
		set => SetProperty(ref _messages, value);
	}


	private string _userInput = string.Empty;
	public string UserInput
	{
		get => _userInput;
		set => SetProperty(ref _userInput, value);
	}

	private string _statusMessage = "Ready to load model.";
	public string StatusMessage
	{
		get => _statusMessage;
		set => SetProperty(ref _statusMessage, value);
	}

	private bool _isModelLoaded;
	public bool IsModelLoaded
	{
		get => _isModelLoaded;
		set => SetProperty(ref _isModelLoaded, value);
	}

	public AIChatSettingsViewModel AISettings { get; private set; }

	private bool _isProcessing;
	public bool IsProcessing
	{
		get => _isProcessing;
		set => SetProperty(ref _isProcessing, value);
	}
	#endregion

	#region commands
	public ICommand SendMessageCommand => new AsyncRelayCommand(SendMessageAsync);
	public ICommand StopResponseCommand => new RelayCommand(StopResponse);
	#endregion

	#region ctors
	public AIChatViewModel(
		IChatClientProvider onnxChatClient,
		AIChatSettingsViewModel aisettings,
		AIChatToolRegistration toolRegistry)
	{
		_chatClient = onnxChatClient;
		AISettings = aisettings;
		_toolRegistry = toolRegistry;
	}
	#endregion

	#region overridden protected methods
	#endregion

	#region command methods
	private async Task SendMessageAsync()
	{
		if (string.IsNullOrWhiteSpace(UserInput) || _chatClient == null)
			return;

		IsProcessing = true;

		_cts = new CancellationTokenSource();

		// recontext (refresh memory)
		var fullContext = new List<AIChatMessageModel>();
		{
			// add system message
			fullContext.Add(new AIChatMessageModel(ChatRole.System, this.AISettings.SystemPrompt));
			// add previous messages
			//foreach (var msg in Messages)
			//	fullContext.Add(new AIChatMessageModel(msg.Role, msg.StreamingText));
			// add user input
			var userMsg = new AIChatMessageModel(ChatRole.User, UserInput);
			Messages.Add(userMsg); // add to UI
			fullContext.Add(userMsg); // add to AI context
		}

		string currentInput = UserInput;
		UserInput = string.Empty;

		string toolJSON = string.Empty;

		try
		{
			// prepare assistant response
			var assistantMsg = new AIChatMessageModel(ChatRole.Assistant, "");
			Messages.Add(assistantMsg);

			await Task.Delay(1);

			StringBuilder sb = new StringBuilder();
			string text = string.Empty;
			string st = string.Empty; // "s"antized "t"ext
			string role = string.Empty;
			bool startCommand = false;
			bool startFriendlyMessage = false;
			bool startTool = false;
			bool startEnding = false;
			bool validResponseStart = false;

			try
			{
				await foreach (var token in _chatClient.GetStreamingResponseAsync(fullContext, cancellationToken: _cts.Token))
				{
					role = token.Role?.Value?.ToString() ?? "";

					text = token.Text;
					st = text.Trim();
					sb.Append(text);

					System.Diagnostics.Debug.WriteLine("token=" + token);

					if(!AISettings.EnableTooling)
					{
						assistantMsg.StreamingText += text;
						continue;
					}

					// start of command
					if (st == ">")
					{
						startCommand = true;
						validResponseStart = true;

						continue;
					}

					//// if command started
					//if (startCommand)
					//    if (st == "!")
					//    {
					//        validResponseStart = true;
					//        continue;
					//    }

					// check commands
					if (startCommand && validResponseStart)
					{
						if (st == "!")
						{
							startFriendlyMessage = true;
							startTool = false;
							startEnding = false;
							continue;
						}
						else if (st == "#")
						{
							startTool = true;
							startFriendlyMessage = false;
							startEnding = false;
							continue;
						}
						else if (st == "END")
						{
							startEnding = true;
							startFriendlyMessage = false;
							startTool = false;
							continue;
						}
					}

					if (startCommand && validResponseStart && startFriendlyMessage)
						assistantMsg.StreamingText += text;
					if (startCommand && validResponseStart && startTool)
						toolJSON += text;
					if (startCommand && validResponseStart && startEnding)
						StopResponse();
				}
			}
			catch (System.OperationCanceledException)
			{
				// chat response stopped.
			}

			// store raw, not the post processed msg
			var rawmsg = new AIChatMessageModel(ChatRole.Assistant, sb.ToString());
			fullContext.Add(rawmsg);

			if (string.IsNullOrEmpty(toolJSON))
				return;

			// execute tool
			{
				string json = SanitizeJson(toolJSON);

				if (IsValidJson(json))
				{
					json = $"[{json}]";

					//if(toolJSON)
					//string json = SanitizeJson(toolJSON);

					//var tools = System.Text.Json.JsonSerializer.Deserialize<List<AIChatTool>>(json);
					var tools = System.Text.Json.JsonSerializer.Deserialize<List<AIChatToolModel>>(json);
					if (tools is null) return;
					foreach (var tool in tools)
					{
						if (tool is not null && string.IsNullOrWhiteSpace(tool.tool))
							return;
						if (tool is not null && tool.parameters is null)
							return;

						// execute tool
						foreach (var thetool in _toolRegistry.GetAllTools())
						{
							if (tool.tool == thetool.tool)
							{
								//string param = tool.parameters[0].GetRawText();

								var result = await thetool.ExecuteAsync(tool.parameters);

								var msg = new AIChatMessageModel(ChatRole.Assistant, result);
								Messages.Add(msg);

								break;
							}
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			Messages.Add(new AIChatMessageModel(ChatRole.System, $"Error: {ex.Message}\r\ntoolJson: {toolJSON}"));
		}
		finally
		{
			IsProcessing = false;
		}
	}

	private void StopResponse()
	{
		if (_cts != null && !_cts.IsCancellationRequested)
		{
			_cts.Cancel();
		}
	}
	#endregion

	#region public methods

	#endregion

	#region private methods
	public async Task AutoLoadModelAsync()
	{
		var targetDir = Path.Combine(FileSystem.AppDataDirectory, "GenAI_Model");
		var targetConfig = Path.Combine(targetDir, "genai_config.json");
		var targetModel = Path.Combine(targetDir, _chatClient.BaseLLM);

		if (File.Exists(targetConfig) && File.Exists(targetModel))
		{
			try
			{
				MainThread.BeginInvokeOnMainThread(() => StatusMessage = "Found existing model. Loading...");

				_modelPath = targetDir;

				await Task.Run(async () =>
				{
					IsModelLoaded = _chatClient.InitializeModel(_modelPath);
				});

				if (IsModelLoaded)
					MainThread.BeginInvokeOnMainThread(() =>
					{
						StatusMessage = $"Model Loaded from: {_modelPath}";
						Messages.Add(AIChatMessageModel.Create(ChatRole.System, "Existing Model Auto-Loaded. Ready!"));
					});
			}
			catch (Exception ex)
			{
				MainThread.BeginInvokeOnMainThread(() => StatusMessage = $"Auto-load failed: {ex.Message}");
			}
		}
	}

	public static string SanitizeJson(string input)
	{
		if (string.IsNullOrEmpty(input))
			return input;

		var sb = new StringBuilder(input.Length);

		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];

			// Skip BOM
			if (c == '\uFEFF')
				continue;

			// Skip null characters
			if (c == '\0')
				continue;

			// Replace smart quotes
			if (c == '“' || c == '”')
			{
				sb.Append('"');
				continue;
			}

			if (c == '‘' || c == '’')
			{
				sb.Append('\'');
				continue;
			}

			// Remove invalid control characters
			// Allow: \t (9), \n (10), \r (13)
			if (c < 32 && c != '\t' && c != '\n' && c != '\r')
				continue;

			sb.Append(c);
		}

		return sb.ToString();
	}

	/// <summary>
	/// Checks if a string is valid JSON.
	/// Returns true if valid, false otherwise.
	/// </summary>
	public static bool IsValidJson(string json)
	{
		if (string.IsNullOrWhiteSpace(json))
			return false;

		try
		{
			using var doc = JsonDocument.Parse(json);
			return true;
		}
		catch (System.Text.Json.JsonException jex)
		{
			return false;
		}
	}
	#endregion
}
