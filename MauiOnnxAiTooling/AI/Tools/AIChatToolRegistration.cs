using System.Collections.Concurrent;

namespace AI.Tools;
public class AIChatToolRegistration
{
	private readonly ConcurrentDictionary<string, IAIChatTool> _tools = new(StringComparer.OrdinalIgnoreCase);

	public AIChatToolRegistration(IEnumerable<IAIChatTool> tools)
	{
		foreach (var tool in tools)
		{
			RegisterTool(tool);
		}
	}

	public void RegisterTool(IAIChatTool aichattool)
	{
		if (aichattool is null || aichattool?.tool is null) return;

		_tools[aichattool.tool] = aichattool;
	}

	public IAIChatTool? GetTool(string name)
	{
		_tools.TryGetValue(name, out var tool);
		return tool;
	}

	public IEnumerable<IAIChatTool> GetAllTools()
	{
		return _tools.Values;
	}

	public string GetToolsSystemPrompt()
	{
		var prompt = @"## Tools
### You have access to the following tools to reply back for method execution.  " + Environment.NewLine;

		foreach (var tool in _tools.Values)
		{
			prompt += $"tool: `{tool.tool}`  {Environment.NewLine}";
			prompt += $"description: {tool.description}  {Environment.NewLine}";
			prompt += $"pseudo_parameters: `{tool.pseudo_parameters}`  {Environment.NewLine}{Environment.NewLine}";
		}

		return prompt;
	}
}
