using System;

public class DogCommand
{
	public string RawText = "";
	public string Intent = "unknown";
	public string Target = "";
	public string Tone = "neutral";
	public float Confidence = 0.5f;
	public string Explanation = "";
}

public static class LocalAiDogParser
{
	public static DogCommand Parse(string text, DogMemory memory)
	{
		string lower = text.ToLower();

		DogCommand command = new DogCommand();
		command.RawText = text;
		command.Intent = "unknown";
		command.Target = "";
		command.Tone = DetectTone(lower);
		command.Confidence = 0.55f;

		if (ContainsAny(lower, "坐", "坐下", "sit"))
		{
			command.Intent = "sit";
			command.Explanation = "AI thinks the player wants the dog to sit.";
		}
		else if (ContainsAny(lower, "过来", "回来", "来这里", "come"))
		{
			command.Intent = "come";
			command.Explanation = "AI thinks the player wants the dog to come back.";
		}
		else if (ContainsAny(lower, "别动", "等一下", "等待", "stay"))
		{
			command.Intent = "stay";
			command.Explanation = "AI thinks the player wants the dog to stay.";
		}
		else if (ContainsAny(lower, "球", "捡", "拿回来", "叼", "fetch", "ball"))
		{
			command.Intent = "fetch";
			command.Target = "ball";
			command.Explanation = "AI thinks the player wants the dog to fetch the ball.";
		}
		else if (ContainsAny(lower, "跟着", "跟随", "follow"))
		{
			command.Intent = "follow";
			command.Explanation = "AI thinks the player wants the dog to follow.";
		}
		else if (ContainsAny(lower, "玩", "play"))
		{
			command.Intent = "play";
			command.Explanation = "AI thinks the player wants to play with the dog.";
		}
		else if (ContainsAny(lower, "睡", "休息", "sleep"))
		{
			command.Intent = "sleep";
			command.Explanation = "AI thinks the player wants the dog to sleep.";
		}
		else
		{
			command.Intent = "unknown";
			command.Explanation = "AI cannot understand this command.";
			command.Confidence = 0.2f;
			return command;
		}

		if (text.Contains(memory.DogName))
		{
			command.Confidence += 0.15f;
		}

		if (command.Tone == "friendly")
		{
			command.Confidence += 0.10f;
		}
		else if (command.Tone == "harsh")
		{
			command.Confidence -= 0.10f;
		}

		command.Confidence = Math.Clamp(command.Confidence, 0.1f, 0.98f);
		return command;
	}

	private static bool ContainsAny(string text, params string[] keywords)
	{
		foreach (string keyword in keywords)
		{
			if (text.Contains(keyword))
			{
				return true;
			}
		}

		return false;
	}

	private static string DetectTone(string text)
	{
		if (ContainsAny(text, "乖", "好狗", "拜托", "please", "good"))
		{
			return "friendly";
		}

		if (ContainsAny(text, "笨", "快点", "不听话", "bad", "stupid"))
		{
			return "harsh";
		}

		return "neutral";
	}
}
