using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AI.Models;
public class AIChatMessageBase : ChatMessage, INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(backingField, value))
			return false;

		backingField = value;
		OnPropertyChanged(propertyName);
		return true;
	}
}

public enum AIChatResponseType
{
	Text,
	Html
}

public class AIChatMessageModel : ChatMessage, INotifyPropertyChanged
{
	public string ChatRole => Role.ToString();

	string _streamingText = string.Empty;
	public string StreamingText
	{
		get => _streamingText;
		set => SetProperty(ref _streamingText, value);
	}

	private AIChatResponseType _responseType = AIChatResponseType.Text;
	public AIChatResponseType ResponseType
	{
		get => _responseType;
		set => SetProperty(ref _responseType, value);
	}

	public AIChatMessageModel(ChatRole role, string? content, AIChatResponseType responseType = AIChatResponseType.Text) : base(role, content)
	{
		ResponseType = responseType;
		StreamingText = content;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(backingField, value))
			return false;

		backingField = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	public static AIChatMessageModel Create(ChatRole role, string? content, AIChatResponseType responseType = AIChatResponseType.Text)
		=> new AIChatMessageModel(role, content, responseType);
}