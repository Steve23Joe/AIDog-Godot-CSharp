using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public class SkillData
{
	public int Level = 0;
	public int Exp = 0;
	public float BaseSuccessRate = 0.15f;
}

public class DogSaveData
{
	public string DogName = "小黑";
	public int Trust = 50;
	public int Energy = 80;
	public string Mood = "curious";
	public Dictionary<string, SkillData> Skills = new Dictionary<string, SkillData>();
	public List<string> Memories = new List<string>();
}

public class DogMemory
{
	public const string SavePath = "user://aidog_save.json";

	public string DogName = "小黑";

	public int Trust = 50;
	public int Energy = 80;
	public string Mood = "curious";

	public Dictionary<string, SkillData> Skills = new Dictionary<string, SkillData>();

	public List<string> Memories = new List<string>();

	private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
	{
		WriteIndented = true,
		IncludeFields = true
	};

	public DogMemory()
	{
		ResetToDefault();
	}

	public void ResetToDefault()
	{
		DogName = "小黑";
		Trust = 50;
		Energy = 80;
		Mood = "curious";

		Skills = new Dictionary<string, SkillData>
		{
			{ "sit", new SkillData { Level = 0, Exp = 0, BaseSuccessRate = 0.20f } },
			{ "come", new SkillData { Level = 0, Exp = 0, BaseSuccessRate = 0.45f } },
			{ "stay", new SkillData { Level = 0, Exp = 0, BaseSuccessRate = 0.15f } },
			{ "fetch", new SkillData { Level = 0, Exp = 0, BaseSuccessRate = 0.10f } },
			{ "follow", new SkillData { Level = 0, Exp = 0, BaseSuccessRate = 0.50f } },
			{ "play", new SkillData { Level = 0, Exp = 0, BaseSuccessRate = 0.70f } },
			{ "sleep", new SkillData { Level = 0, Exp = 0, BaseSuccessRate = 0.80f } }
		};

		Memories = new List<string>();
	}

	public float GetSuccessRate(string command)
	{
		EnsureSkillExists(command);

		if (!Skills.ContainsKey(command))
		{
			return 0.05f;
		}

		SkillData skill = Skills[command];

		float rate = skill.BaseSuccessRate;
		rate += skill.Level * 0.12f;

		if (Trust >= 80)
		{
			rate += 0.10f;
		}
		else if (Trust <= 25)
		{
			rate -= 0.15f;
		}

		if (Energy <= 20)
		{
			rate -= 0.20f;
		}

		return Mathf.Clamp(rate, 0.05f, 0.98f);
	}

	public void AddMemory(string text)
	{
		string timeText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
		Memories.Add($"[{timeText}] {text}");

		if (Memories.Count > 12)
		{
			Memories.RemoveAt(0);
		}
	}

	public void RewardCommand(string command, bool success)
	{
		EnsureSkillExists(command);

		if (!Skills.ContainsKey(command))
		{
			return;
		}

		int expGain = success ? 25 : 12;

		Skills[command].Exp += expGain;
		Trust += success ? 3 : 1;
		Mood = success ? "happy" : "encouraged";

		if (Skills[command].Exp >= 100)
		{
			Skills[command].Exp -= 100;
			Skills[command].Level += 1;
			AddMemory($"{DogName} learned better how to perform command: {command}");
		}

		Trust = Mathf.Clamp(Trust, 0, 100);
		AddMemory($"{DogName} was rewarded after command: {command}");
	}

	public void ScoldCommand(string command, bool success)
	{
		EnsureSkillExists(command);

		Trust -= success ? 4 : 8;
		Mood = "nervous";
		Trust = Mathf.Clamp(Trust, 0, 100);

		AddMemory($"{DogName} was scolded after command: {command}");
	}

	public string GetSkillSummary()
	{
		string result = "";

		foreach (var pair in Skills)
		{
			string name = pair.Key;
			SkillData skill = pair.Value;
			result += $"{name}: Lv.{skill.Level} Exp {skill.Exp}/100 Success {Mathf.RoundToInt(GetSuccessRate(name) * 100)}%\n";
		}

		return result;
	}

	public string GetMemorySummary()
	{
		return GetRecentMemorySummary(Memories.Count);
	}

	public string GetRecentMemorySummary(int maxCount)
	{
		if (Memories.Count == 0)
		{
			return "No memories yet.";
		}

		int safeMaxCount = Math.Max(maxCount, 0);
		int startIndex = Math.Max(Memories.Count - safeMaxCount, 0);
		string result = "";

		for (int i = startIndex; i < Memories.Count; i++)
		{
			result += "- " + Memories[i] + "\n";
		}

		return result;
	}

	public bool SaveToFile()
	{
		try
		{
			DogSaveData saveData = new DogSaveData
			{
				DogName = DogName,
				Trust = Trust,
				Energy = Energy,
				Mood = Mood,
				Skills = Skills,
				Memories = Memories
			};

			string json = JsonSerializer.Serialize(saveData, _jsonOptions);

			using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);

			if (file == null)
			{
				GD.PrintErr("Failed to open save file. Error: " + FileAccess.GetOpenError());
				return false;
			}

			file.StoreString(json);
			GD.Print("Dog memory saved to: " + SavePath);
			return true;
		}
		catch (Exception e)
		{
			GD.PrintErr("Save failed: " + e.Message);
			return false;
		}
	}

	public bool LoadFromFile()
	{
		try
		{
			if (!FileAccess.FileExists(SavePath))
			{
				GD.Print("No save file found. Using default dog memory.");
				return false;
			}

			using FileAccess file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Read);

			if (file == null)
			{
				GD.PrintErr("Failed to open save file. Error: " + FileAccess.GetOpenError());
				return false;
			}

			string json = file.GetAsText();

			DogSaveData saveData = JsonSerializer.Deserialize<DogSaveData>(json, _jsonOptions);

			if (saveData == null)
			{
				GD.PrintErr("Save file is empty or invalid.");
				return false;
			}

			DogName = saveData.DogName;
			Trust = saveData.Trust;
			Energy = saveData.Energy;
			Mood = saveData.Mood;
			Skills = saveData.Skills ?? new Dictionary<string, SkillData>();
			Memories = saveData.Memories ?? new List<string>();

			EnsureAllDefaultSkillsExist();

			Trust = Mathf.Clamp(Trust, 0, 100);
			Energy = Mathf.Clamp(Energy, 0, 100);

			GD.Print("Dog memory loaded from: " + SavePath);
			return true;
		}
		catch (Exception e)
		{
			GD.PrintErr("Load failed: " + e.Message);
			return false;
		}
	}

	public bool DeleteSaveFile()
	{
		try
		{
			if (!FileAccess.FileExists(SavePath))
			{
				return true;
			}

			Error error = DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(SavePath));

			if (error != Error.Ok)
			{
				GD.PrintErr("Failed to delete save file. Error: " + error);
				return false;
			}

			GD.Print("Save file deleted.");
			return true;
		}
		catch (Exception e)
		{
			GD.PrintErr("Delete save failed: " + e.Message);
			return false;
		}
	}

	private void EnsureSkillExists(string command)
	{
		if (string.IsNullOrEmpty(command))
		{
			return;
		}

		if (!Skills.ContainsKey(command))
		{
			Skills[command] = new SkillData
			{
				Level = 0,
				Exp = 0,
				BaseSuccessRate = 0.10f
			};
		}
	}

	private void EnsureAllDefaultSkillsExist()
	{
		AddDefaultSkillIfMissing("sit", 0.20f);
		AddDefaultSkillIfMissing("come", 0.45f);
		AddDefaultSkillIfMissing("stay", 0.15f);
		AddDefaultSkillIfMissing("fetch", 0.10f);
		AddDefaultSkillIfMissing("follow", 0.50f);
		AddDefaultSkillIfMissing("play", 0.70f);
		AddDefaultSkillIfMissing("sleep", 0.80f);
	}

	private void AddDefaultSkillIfMissing(string command, float baseRate)
	{
		if (!Skills.ContainsKey(command))
		{
			Skills[command] = new SkillData
			{
				Level = 0,
				Exp = 0,
				BaseSuccessRate = baseRate
			};
		}
	}
}
