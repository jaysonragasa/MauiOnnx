# MauiOnnxAiTooling

![Demo](https://raw.githubusercontent.com/jaysonragasa/MauiOnnx/refs/heads/main/MauiOnnxAiTooling/androidonnx.gif)

A cross-platform .NET MAUI application that integrates ONNX Runtime GenAI for on-device AI chat capabilities.

## Features

- **On-Device AI Chat**: Run AI models locally using ONNX Runtime GenAI
- **Model Download**: Download and manage AI models directly from the app
- **Streaming Responses**: Real-time streaming chat responses
- **Cross-Platform**: Supports Android, iOS, macOS Catalyst, and Windows
- **MVVM Architecture**: Clean separation using CommunityToolkit.Mvvm
- **Extensible Tool System**: Plugin architecture for custom AI tools

## Tech Stack

- **.NET 9.0** with .NET MAUI
- **Microsoft.ML.OnnxRuntimeGenAI** - ONNX Runtime for AI inference
- **Microsoft.Extensions.AI** - AI abstractions and interfaces
- **CommunityToolkit.Maui** & **CommunityToolkit.Mvvm** - MAUI extensions and MVVM helpers

## Platform Support

- Android (API 26+)
- iOS (15.0+)
- macOS Catalyst (15.0+)
- Windows (10.0.17763.0+)

## Getting Started

1. Clone the repository
2. Open `MauiOnnxAiTooling.sln` in Visual Studio 2022
3. Build and run on your target platform
4. Use the "Download Model" button to fetch an AI model
5. Start chatting!

## Architecture

- **AI/ChatClientProviders**: ONNX chat client implementations (Phi3, Llama)
- **AI/Models**: Data models for chat messages
- **AI/Tools**: Extensible tool registration system
- **ViewModels**: MVVM view models for chat and settings
- **MainPage.xaml**: Chat UI with streaming support

## License

MIT License - see [LICENSE](LICENSE) file for details
