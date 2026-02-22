using AI.ChatClientProviders;
using AI.Tools;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace ViewModels;

public class AIChatSettingsViewModel : ObservableObject
{
	#region fields
	IChatClientProvider? _chatClient;
	private readonly AIChatToolRegistration _toolRegistry;
	#endregion

	#region properties
	public string SystemPrompt => BuildSystemPrompt();

	public bool EnableTooling => false;

	private bool _isDownloading;
	public bool IsDownloading
	{
		get => _isDownloading;
		set
		{
			if (SetProperty(ref _isDownloading, value))
			{
				OnPropertyChanged(nameof(IsNotDownloading));
			}
		}
	}

	public bool IsNotDownloading => !IsDownloading;

	private string _downloadStatus = "";
	public string DownloadStatus
	{
		get => _downloadStatus;
		set => SetProperty(ref _downloadStatus, value);
	}

	private double _downloadProgress;
	public double DownloadProgress
	{
		get => _downloadProgress;
		set => SetProperty(ref _downloadProgress, value);
	}

	private string _statusMessage = "Ready to load model.";
	public string StatusMessage
	{
		get => _statusMessage;
		set => SetProperty(ref _statusMessage, value);
	}

	private string? _modelPath;
	public string? ModelPath
	{
		get => _modelPath;
		set => SetProperty(ref _modelPath, value);
	}

	private bool _isModelLoaded;
	public bool IsModelLoaded
	{
		get => _isModelLoaded;
		set => SetProperty(ref _isModelLoaded, value);
	}
	#endregion

	#region commands
	public ICommand DownloadModelCommand { get; private set; }
	public ICommand LoadLocalModelCommand { get; private set; }
	#endregion

	#region ctors
	public AIChatSettingsViewModel(
		IChatClientProvider onnxChatClient,
		AIChatToolRegistration toolRegistry)
	{
		_chatClient = onnxChatClient;
		_toolRegistry = toolRegistry;
		InitCommands();
	}
	#endregion

	#region overridden protected methods
	#endregion

	#region command methods
	private async Task DownloadModelAsync()
	{
		if (IsDownloading) return;
		if (_chatClient is null) return;

		try
		{
			IsDownloading = true;
			DownloadProgress = 0;
			StatusMessage = "Starting download...";

			var targetDir = Path.Combine(FileSystem.AppDataDirectory, "GenAI_Model");
			if (Directory.Exists(targetDir))
				Directory.Delete(targetDir, true);
			Directory.CreateDirectory(targetDir);

			using var client = new HttpClient();

			// Exception during initialization:
			// filesystem error: in file_size:
			// No such file or directory ["/data/user/0/com.companyname.essentialsai/files/GenAI_Model/phi3-mini-4k-instruct-cpu-int4-rtn-block-32-acc-level-4.onnx.data"]

			var totalFiles = _chatClient.FilesToDownload.Count;
			var currentFileIndex = 0;

			foreach (var file in _chatClient.FilesToDownload)
			{
				var remoteName = file.Key;
				var localName = file.Value;
				var url = $"{_chatClient.DownloadBaseUrl}/{remoteName}?download=true";
				var localPath = Path.Combine(targetDir, localName);

				DownloadStatus = $"Downloading {localName}... ({currentFileIndex + 1}/{totalFiles})";

				using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
				if (!response.IsSuccessStatusCode)
				{
					// Fallback: If long name fails, try "model.onnx"
					if (remoteName.EndsWith(".onnx"))
					{
						url = $"{_chatClient.DownloadBaseUrl}/model.onnx";
						response.Dispose(); // Dispose failed response
						var response2 = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
						if (response2.IsSuccessStatusCode)
						{
							// It was model.onnx after all
							using var stream = await response2.Content.ReadAsStreamAsync();
							using var fileStream = File.Create(localPath);
							await CopyStreamWithProgressAsync(stream, fileStream, response2.Content.Headers.ContentLength);
							currentFileIndex++;
							continue;
						}
					}
					else if (remoteName.EndsWith(".onnx.data"))
					{
						// Try model.onnx.data
						url = $"{_chatClient.DownloadBaseUrl}/model.onnx.data";
						response.Dispose();
						var response3 = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
						if (response3.IsSuccessStatusCode)
						{
							using var stream = await response3.Content.ReadAsStreamAsync();
							using var fileStream = File.Create(localPath);
							await CopyStreamWithProgressAsync(stream, fileStream, response3.Content.Headers.ContentLength);
							currentFileIndex++;
							continue;
						}
					}

					throw new Exception($"Failed to download {remoteName}: {response.StatusCode}");
				}

				using var contentStream = await response.Content.ReadAsStreamAsync();
				using var fileStreamOut = File.Create(localPath);

				await CopyStreamWithProgressAsync(contentStream, fileStreamOut, response.Content.Headers.ContentLength);
				currentFileIndex++;
			}

			DownloadStatus = "Download Complete. Loading Model...";

			// Load the model
			ModelPath = targetDir;
			await Task.Run(async () =>
			{
				(_chatClient as IDisposable)?.Dispose();
				IsModelLoaded = _chatClient.InitializeModel(ModelPath);
			});

			if (IsModelLoaded)
				StatusMessage = "Model Downloaded and Loaded.";

			//Messages.Clear();
			//Messages.Add(new ChatMessage(ChatRole.System, "Model Ready!"));
		}
		catch (Exception ex)
		{
			StatusMessage = $"Download Failed: {ex.Message}";
		}
		finally
		{
			IsDownloading = false;
			DownloadProgress = 0;
			DownloadStatus = "";
		}
	}

	//private async Task LoadLocalModelAsync()
	//{
	//	try
	//	{
	//		FileResult? result = null;

	//		result = await FilePicker.Default.PickAsync(new PickOptions
	//		{
	//			PickerTitle = "Select ONNX Model (inside model folder)",
	//			FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
	//			{
	//				{ DevicePlatform.WinUI, new[] { ".onnx" } },
	//				{ DevicePlatform.Android, new[] { "application/octet-stream" } },
	//				{ DevicePlatform.iOS, new[] { "public.data" } },
	//				{ DevicePlatform.MacCatalyst, new[] { "public.data" } }
	//			})
	//		});

	//		if (result != null)
	//		{
	//			StatusMessage = $"Selected file: {result.FileName}. Copying to App Data...";

	//			// 1. Define Target Directory in AppData
	//			var targetDir = Path.Combine(FileSystem.AppDataDirectory, "GenAI_Model");
	//			//if (Directory.Exists(targetDir))
	//			//    Directory.Delete(targetDir, true); // Clean previous model
	//			Directory.CreateDirectory(targetDir);

	//			// 2. Copy the Selected ONNX File
	//			// Rename to "model.onnx" to ensure GenAI finds it, as conventions usually expect this name 
	//			// or genai_config.json might implicitly expect it.
	//			var targetModelPath = Path.Combine(targetDir, "model.onnx");
	//			using (var sourceStream = await result.OpenReadAsync())
	//			using (var destStream = File.Create(targetModelPath))
	//			{
	//				await sourceStream.CopyToAsync(destStream);
	//			}

	//			// 3. Attempt to Copy Sibling Configuration Files (Best Effort)
	//			var sourceDir = Path.GetDirectoryName(result.FullPath);
	//			if (!string.IsNullOrEmpty(sourceDir) && Directory.Exists(sourceDir))
	//			{
	//				var filesToCopy = new[] { "genai_config.json", "tokenizer.json", "tokenizer_config.json" };
	//				foreach (var fileName in filesToCopy)
	//				{
	//					var sourceFile = Path.Combine(sourceDir, fileName);
	//					if (File.Exists(sourceFile))
	//					{
	//						var targetFile = Path.Combine(targetDir, fileName);
	//						File.Copy(sourceFile, targetFile, true);
	//					}
	//				}

	//				// Also look for external data files (*.onnx.data)
	//				// We copy them as is. Note: if model.onnx references them by specific name, 
	//				// renaming the main file to model.onnx shouldn't break that link *unless* the link is part of the filename convention.
	//				// Usually external data is just "model.onnx.data" or "model_q4f16.onnx.data".
	//				// If we rename the main file, we might arguably need to rename the data file too if it follows the "stem.data" convention?
	//				// Safe bet: Copy all .data files. If loading fails, we might need to match names.
	//				var dataFiles = Directory.GetFiles(sourceDir, "*.onnx.data");
	//				foreach (var dataFile in dataFiles)
	//				{
	//					var fileName = Path.GetFileName(dataFile);
	//					File.Copy(dataFile, Path.Combine(targetDir, fileName), true);
	//				}
	//			}
	//			else
	//			{
	//				StatusMessage += "\nWarning: Could not access source directory to copy config files. Model might fail if not self-contained.";
	//			}

	//			// 4. Verify Critical Config in Target
	//			var targetConfig = Path.Combine(targetDir, "genai_config.json");
	//			if (!File.Exists(targetConfig))
	//			{
	//				StatusMessage = "Error: 'genai_config.json' not found. Please ensure it was in the source folder alongside the .onnx file.";
	//				return;
	//			}

	//			ModelPath = targetDir; // Pass DIRECTORY
	//			StatusMessage = $"Loading GenAI model from: {targetDir}...";

	//			// Offload creation to BG thread
	//			await Task.Run(async () =>
	//			{
	//				// Dispose previous
	//				(_chatClient as IDisposable)?.Dispose();

	//				// Create GenAI Chat Client from the implementation-private copy
	//				IsModelLoaded = _chatClient.InitializeModel(ModelPath);
	//			});

	//			if (IsModelLoaded)
	//				StatusMessage = $"GenAI Model loaded successfully.\nLocation: {ModelPath}";

	//			//Messages.Clear();
	//			//Messages.Add(new ChatMessage(ChatRole.System, "GenAI Model Loaded & Copied. Ready to chat!"));
	//		}
	//	}
	//	catch (Exception ex)
	//	{
	//		StatusMessage = $"Error loading model: {ex.Message}";
	//		IsModelLoaded = false;
	//		(_chatClient as IDisposable)?.Dispose();
	//		_chatClient = null;
	//	}
	//}
	#endregion

	#region public methods
	public string BuildSystemPrompt()
	{
		string prompt = $"You will be a helpful friendly assistant. Today's date and time is {DateTime.Now.ToString()}" ; 

		if (EnableTooling)
		{
			prompt = @"# Mark Down Formatted Instructions
## Role
You will be a helpful friendly assistant. 
  
";
			prompt += "## Date  " + Environment.NewLine + "Date today: " + DateTime.Now + "  " + Environment.NewLine;

			prompt += _toolRegistry.GetToolsSystemPrompt();

			prompt += @"  
  
## Rule
### It's important to follow this protocol and the response format:  
1. Your will respond in a very strict format and will contain the following format on each and every reply you make.  
  
This is the format:  
```>! user_friendly_message ># json_data >END```

Breakdown if command:  
`>!` marks as the start of the command  
`>#` marks as the start of the tool  
`>END` marks as the end of the command
  
below is a valid example response:  
```>! Checking the current weather in Baguio ># { ""tool"": ""tool_name"", ""parameters"": [{ ""city"": ""Baguio"" }], ""toolresponseformattype"": ""html"" } >END```
  
2. Respond cleanly and respectfully. No extra details. Carefully follow the response format.
";

			return prompt;
		}
		else
			return prompt;
	}
	#endregion

	#region private methods
	void InitCommands()
	{
		DownloadModelCommand = new AsyncRelayCommand(DownloadModelAsync);
		//LoadLocalModelCommand = new AsyncRelayCommand(LoadLocalModelAsync);
	}

	private async Task CopyStreamWithProgressAsync(Stream source, Stream destination, long? totalBytes)
	{
		var buffer = new byte[8192];
		long totalRead = 0;
		int bytesRead;

		while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length)) > 0)
		{
			await destination.WriteAsync(buffer, 0, bytesRead);
			totalRead += bytesRead;

			if (totalBytes.HasValue)
			{
				DownloadProgress = (double)totalRead / totalBytes.Value;
			}
		}
	}
	#endregion
}
