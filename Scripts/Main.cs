using Godot;
using System;

public partial class Main : Node2D
{
	private DogController _dog;
	private DogMemory _memory = new DogMemory();
	private RandomNumberGenerator _rng = new RandomNumberGenerator();

	private Rect2 _playArea = new Rect2(480, 60, 440, 420);
	private Vector2 _ownerPosition = new Vector2(560, 320);
	private Vector2 _ballPosition = new Vector2(800, 330);

	private LineEdit _input;
	private Label _statsLabel;
	private RichTextLabel _logLabel;

	private DogCommand _lastCommand;
	private bool _lastCommandSucceeded = false;
	private bool _hasPendingFeedback = false;

	public override void _Ready()
	{
		_rng.Randomize();

		bool loaded = _memory.LoadFromFile();

		CreateWorld();
		CreateUi();

		AddLog("Demo started.");

		if (loaded)
		{
			AddLog("Loaded saved dog memory.");
		}
		else
		{
			AddLog("No save file found. Created a new dog memory.");
		}

		AddLog("Try typing: 小黑，坐下");
		AddLog("Try typing: 小黑，把球捡回来");
		AddLog("Try typing: 乖狗狗，过来");
		AddLog("After each command, click Reward or Scold.");
		AddLog("V1: Dog memory will be saved after Reward or Scold.");

		UpdateStats();
	}

	public override void _Process(double delta)
	{
		if (_dog != null)
		{
			_ownerPosition = ClampPointToPlayArea(_ownerPosition, 24.0f);
			_ballPosition = ClampPointToPlayArea(_ballPosition, 13.0f);
			_dog.OwnerPosition = _ownerPosition;
			_dog.BallPosition = _ballPosition;

			if (_dog.IsCarryingBall)
			{
				_ballPosition = ClampPointToPlayArea(_dog.GlobalPosition + new Vector2(24, -10), 13.0f);
			}
			else if (_dog.State == DogController.DogState.Idle && _dog.GlobalPosition.DistanceTo(_ownerPosition) < 45.0f)
			{
				if (_lastCommand != null && _lastCommand.Intent == "fetch" && _lastCommandSucceeded)
				{
					_ballPosition = ClampPointToPlayArea(_ownerPosition + new Vector2(60, 30), 13.0f);
				}
			}
		}

		QueueRedraw();
	}

	private void CreateWorld()
	{
		_dog = new DogController();
		_dog.Name = "AIDog";
		_dog.GlobalPosition = new Vector2(680, 320);
		_dog.SetMoveBounds(_playArea);

		CollisionShape2D collision = new CollisionShape2D();
		CircleShape2D shape = new CircleShape2D();
		shape.Radius = 22.0f;
		collision.Shape = shape;
		_dog.AddChild(collision);

		AddChild(_dog);
	}

	private void CreateUi()
	{
		CanvasLayer canvas = new CanvasLayer();
		AddChild(canvas);

		PanelContainer panel = new PanelContainer();
		panel.Position = new Vector2(20, 20);
		panel.CustomMinimumSize = new Vector2(420, 500);
		canvas.AddChild(panel);

		VBoxContainer vbox = new VBoxContainer();
		panel.AddChild(vbox);

		Label title = new Label();
		title.Text = "AI Dog Training Demo";
		vbox.AddChild(title);

		_statsLabel = new Label();
		_statsLabel.Text = "";
		vbox.AddChild(_statsLabel);

		_input = new LineEdit();
		_input.PlaceholderText = "输入指令：小黑，坐下 / 小黑，把球捡回来";
		vbox.AddChild(_input);

		HBoxContainer row = new HBoxContainer();
		vbox.AddChild(row);

		Button sendButton = new Button();
		sendButton.Text = "Send Command";
		sendButton.Pressed += OnSendCommandPressed;
		row.AddChild(sendButton);

		Button rewardButton = new Button();
		rewardButton.Text = "Reward";
		rewardButton.Pressed += OnRewardPressed;
		row.AddChild(rewardButton);

		Button scoldButton = new Button();
		scoldButton.Text = "Scold";
		scoldButton.Pressed += OnScoldPressed;
		row.AddChild(scoldButton);

		Button resetBallButton = new Button();
		resetBallButton.Text = "Throw Ball";
		resetBallButton.Pressed += OnThrowBallPressed;
		row.AddChild(resetBallButton);

		HBoxContainer saveRow = new HBoxContainer();
		vbox.AddChild(saveRow);

		Button saveButton = new Button();
		saveButton.Text = "Save Memory";
		saveButton.Pressed += OnSaveMemoryPressed;
		saveRow.AddChild(saveButton);

		Button loadButton = new Button();
		loadButton.Text = "Load Memory";
		loadButton.Pressed += OnLoadMemoryPressed;
		saveRow.AddChild(loadButton);

		Button resetMemoryButton = new Button();
		resetMemoryButton.Text = "Reset Memory";
		resetMemoryButton.Pressed += OnResetMemoryPressed;
		saveRow.AddChild(resetMemoryButton);

		_logLabel = new RichTextLabel();
		_logLabel.CustomMinimumSize = new Vector2(390, 160);
		_logLabel.Text = "";
		vbox.AddChild(_logLabel);
	}

	private void OnSendCommandPressed()
	{
		string text = _input.Text.Trim();

		if (string.IsNullOrEmpty(text))
		{
			AddLog("Please type a command first.");
			return;
		}

		DogCommand command = LocalAiDogParser.Parse(text, _memory);
		_lastCommand = command;
		_hasPendingFeedback = true;

		AddLog("");
		AddLog("Player: " + text);
		AddLog($"AI parsed -> intent: {command.Intent}, target: {command.Target}, tone: {command.Tone}, confidence: {Mathf.RoundToInt(command.Confidence * 100)}%");
		AddLog(command.Explanation);

		if (command.Intent == "unknown")
		{
			_dog.StartConfused();
			_lastCommandSucceeded = false;
			_memory.Mood = "confused";
			_memory.AddMemory($"{_memory.DogName} heard something unclear.");
			UpdateStats();
			return;
		}

		float skillRate = _memory.GetSuccessRate(command.Intent);
		float finalRate = skillRate * (0.65f + 0.35f * command.Confidence);
		finalRate = Mathf.Clamp(finalRate, 0.03f, 0.98f);

		float roll = _rng.Randf();
		bool success = roll <= finalRate;

		_lastCommandSucceeded = success;

		AddLog($"Skill success rate: {Mathf.RoundToInt(skillRate * 100)}%, final rate after AI confidence: {Mathf.RoundToInt(finalRate * 100)}%");

		if (success)
		{
			ExecuteDogCommand(command);
			AddLog($"{_memory.DogName} understood and tried to perform the command.");
		}
		else
		{
			_dog.StartConfused();
			_memory.Mood = "confused";
			AddLog($"{_memory.DogName} failed this time, but can still learn from training.");
		}

		_memory.Energy -= 3;
		_memory.Energy = Mathf.Clamp(_memory.Energy, 0, 100);

		UpdateStats();
	}

	private void ExecuteDogCommand(DogCommand command)
	{
		switch (command.Intent)
		{
			case "sit":
				_dog.StartSit();
				_memory.Mood = "focused";
				break;

			case "come":
				_dog.StartCome();
				_memory.Mood = "focused";
				break;

			case "stay":
				_dog.StartStay();
				_memory.Mood = "focused";
				break;

			case "fetch":
				_dog.StartFetch();
				_memory.Mood = "excited";
				break;

			case "follow":
				_dog.StartFollow();
				_memory.Mood = "loyal";
				break;

			case "play":
				_dog.StartPlay();
				_memory.Mood = "happy";
				break;

			case "sleep":
				_dog.StartSleep();
				_memory.Mood = "sleepy";
				_memory.Energy += 15;
				_memory.Energy = Mathf.Clamp(_memory.Energy, 0, 100);
				break;

			default:
				_dog.StartConfused();
				_memory.Mood = "confused";
				break;
		}
	}

	private void OnRewardPressed()
	{
		if (!_hasPendingFeedback || _lastCommand == null)
		{
			AddLog("No recent command to reward.");
			return;
		}

		if (_lastCommand.Intent == "unknown")
		{
			AddLog("Reward ignored because the command was unknown.");
			return;
		}

		_memory.RewardCommand(_lastCommand.Intent, _lastCommandSucceeded);

		if (_lastCommandSucceeded)
		{
			AddLog($"Reward: {_memory.DogName} becomes more confident with '{_lastCommand.Intent}'.");
		}
		else
		{
			AddLog($"Reward after failure: {_memory.DogName} still learns a little and trusts you more.");
		}

		_hasPendingFeedback = false;

		bool saved = _memory.SaveToFile();

		if (saved)
		{
			AddLog("Auto-saved dog memory after reward.");
		}
		else
		{
			AddLog("Auto-save failed after reward.");
		}

		UpdateStats();
	}

	private void OnScoldPressed()
	{
		if (!_hasPendingFeedback || _lastCommand == null)
		{
			AddLog("No recent command to scold.");
			return;
		}

		if (_lastCommand.Intent == "unknown")
		{
			_memory.Trust -= 4;
			_memory.Trust = Mathf.Clamp(_memory.Trust, 0, 100);
			_memory.Mood = "nervous";
			_memory.AddMemory($"{_memory.DogName} was scolded after an unknown command.");

			AddLog($"{_memory.DogName} feels nervous because it did not understand but was scolded.");

			_hasPendingFeedback = false;

			bool unknownCommandSaved = _memory.SaveToFile();

			if (unknownCommandSaved)
			{
				AddLog("Auto-saved dog memory after unknown-command scold.");
			}
			else
			{
				AddLog("Auto-save failed after unknown-command scold.");
			}

			UpdateStats();
			return;
		}

		_memory.ScoldCommand(_lastCommand.Intent, _lastCommandSucceeded);

		AddLog($"Scold: {_memory.DogName}'s trust decreases. It may become less responsive.");
		_hasPendingFeedback = false;

		bool scoldSaved = _memory.SaveToFile();

		if (scoldSaved)
		{
			AddLog("Auto-saved dog memory after scold.");
		}
		else
		{
			AddLog("Auto-save failed after scold.");
		}

		UpdateStats();
	}

	private void OnThrowBallPressed()
	{
		_ballPosition = GetRandomPointInPlayArea(13.0f);

		AddLog("You threw the ball to a new position.");
		QueueRedraw();
	}

	private void OnSaveMemoryPressed()
	{
		bool saved = _memory.SaveToFile();

		if (saved)
		{
			AddLog("Manual save completed.");
		}
		else
		{
			AddLog("Manual save failed. Check Godot output panel.");
		}

		UpdateStats();
	}

	private void OnLoadMemoryPressed()
	{
		bool loaded = _memory.LoadFromFile();

		if (loaded)
		{
			AddLog("Manual load completed.");
		}
		else
		{
			AddLog("Manual load failed or no save file exists.");
		}

		UpdateStats();
	}

	private void OnResetMemoryPressed()
	{
		_memory.ResetToDefault();
		_memory.DeleteSaveFile();

		AddLog("Dog memory has been reset. Save file deleted.");
		UpdateStats();
	}

	private Vector2 GetRandomPointInPlayArea(float margin)
	{
		return new Vector2(
			(float)_rng.RandfRange(_playArea.Position.X + margin, _playArea.Position.X + _playArea.Size.X - margin),
			(float)_rng.RandfRange(_playArea.Position.Y + margin, _playArea.Position.Y + _playArea.Size.Y - margin)
		);
	}

	private Vector2 ClampPointToPlayArea(Vector2 point, float margin)
	{
		float minX = _playArea.Position.X + margin;
		float minY = _playArea.Position.Y + margin;
		float maxX = _playArea.Position.X + _playArea.Size.X - margin;
		float maxY = _playArea.Position.Y + _playArea.Size.Y - margin;

		return new Vector2(
			Mathf.Clamp(point.X, minX, maxX),
			Mathf.Clamp(point.Y, minY, maxY)
		);
	}

	private void UpdateStats()
	{
		string text = "";
		text += $"Dog Name: {_memory.DogName}\n";
		text += $"Mood: {_memory.Mood}\n";
		text += $"Trust: {_memory.Trust}/100\n";
		text += $"Energy: {_memory.Energy}/100\n";
		text += "\nSkills:\n";
		text += _memory.GetSkillSummary();
		text += "\nRecent Memories:\n";
		text += _memory.GetMemorySummary();

		_statsLabel.Text = text;
	}

	private void AddLog(string text)
	{
		_logLabel.AppendText(text + "\n");
	}

	public override void _Draw()
	{
		DrawBackground();
		DrawOwner();
		DrawBall();
		DrawHints();
	}

	private void DrawBackground()
	{
		Rect2 screen = new Rect2(Vector2.Zero, new Vector2(960, 540));
		Rect2 uiFrame = new Rect2(new Vector2(12, 12), new Vector2(436, 516));

		DrawRect(screen, new Color(0.10f, 0.12f, 0.15f));
		DrawRect(uiFrame, new Color(0.08f, 0.10f, 0.13f));
		DrawRect(uiFrame, new Color(0.28f, 0.34f, 0.42f), false, 2.0f);
		DrawRect(_playArea, new Color(0.15f, 0.20f, 0.18f));
		DrawRect(_playArea, new Color(0.35f, 0.72f, 0.45f), false, 3.0f);
	}

	private void DrawOwner()
	{
		Vector2 ownerPosition = ClampPointToPlayArea(_ownerPosition, 24.0f);

		DrawCircle(ownerPosition, 24.0f, new Color(0.25f, 0.55f, 1.0f));
		DrawCircle(ownerPosition + new Vector2(-7, -5), 3.0f, Colors.Black);
		DrawCircle(ownerPosition + new Vector2(7, -5), 3.0f, Colors.Black);
	}

	private void DrawBall()
	{
		Vector2 ballPosition = ClampPointToPlayArea(_ballPosition, 13.0f);

		DrawCircle(ballPosition, 13.0f, new Color(1.0f, 0.35f, 0.15f));
		DrawCircle(ballPosition, 5.0f, new Color(1.0f, 0.85f, 0.25f));
	}

	private void DrawHints()
	{
		// Visual hints are drawn by simple shapes only.
		// UI text is handled by Label and RichTextLabel.
	}
}
