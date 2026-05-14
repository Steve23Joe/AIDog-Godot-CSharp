using Godot;

public partial class DogController : CharacterBody2D
{
	public enum DogState
	{
		Idle,
		Sit,
		Stay,
		Come,
		Follow,
		FetchGoToBall,
		FetchReturn,
		Play,
		Wander,
		Sleep,
		Confused
	}

	[Export] public float Speed = 150.0f;
	[Export] public Rect2 MoveBounds = new Rect2(480, 60, 440, 420);

	private const float BodyRadius = 24.0f;

	public DogState State = DogState.Idle;

	public Vector2 OwnerPosition = Vector2.Zero;
	public Vector2 BallPosition = Vector2.Zero;

	public bool IsCarryingBall = false;

	private double _stateTimer = 0.0;
	private Vector2 _playDirection = Vector2.Right;
	private Vector2 _wanderTarget = Vector2.Zero;

	public override void _Ready()
	{
		QueueRedraw();
	}

	public override void _PhysicsProcess(double delta)
	{
		switch (State)
		{
			case DogState.Idle:
				Velocity = Vector2.Zero;
				break;

			case DogState.Sit:
				Velocity = Vector2.Zero;
				break;

			case DogState.Stay:
				Velocity = Vector2.Zero;
				break;

			case DogState.Come:
				MoveTowardTarget(OwnerPosition, 35.0f);
				break;

			case DogState.Follow:
				FollowOwner();
				break;

			case DogState.FetchGoToBall:
				MoveTowardBall();
				break;

			case DogState.FetchReturn:
				ReturnBallToOwner();
				break;

			case DogState.Play:
				PlayAround(delta);
				break;

			case DogState.Wander:
				MoveTowardTarget(_wanderTarget, 18.0f);
				break;

			case DogState.Sleep:
				Velocity = Vector2.Zero;
				break;

			case DogState.Confused:
				ConfusedMove(delta);
				break;
		}

		MoveAndSlide();
		ClampDogInsideBounds();
		QueueRedraw();
	}

	public void StartSit()
	{
		State = DogState.Sit;
		_stateTimer = 0.0;
	}

	public void StartStay()
	{
		State = DogState.Stay;
		_stateTimer = 0.0;
	}

	public void StartCome()
	{
		State = DogState.Come;
		_stateTimer = 0.0;
	}

	public void StartFollow()
	{
		State = DogState.Follow;
		_stateTimer = 0.0;
	}

	public void StartFetch()
	{
		State = DogState.FetchGoToBall;
		IsCarryingBall = false;
		_stateTimer = 0.0;
	}

	public void StartPlay()
	{
		State = DogState.Play;
		_stateTimer = 2.5;
		_playDirection = new Vector2(1, -0.4f).Normalized();
	}

	public void StartSleep()
	{
		State = DogState.Sleep;
		_stateTimer = 0.0;
	}

	public void StartWander(Vector2 target)
	{
		_wanderTarget = ClampPointInsideBounds(target);
		State = DogState.Wander;
		_stateTimer = 0.0;
	}

	public void StartGoToBall()
	{
		IsCarryingBall = false;
		StartWander(BallPosition);
	}

	public void StartConfused()
	{
		State = DogState.Confused;
		_stateTimer = 1.5;
		_playDirection = new Vector2(-1, 0.6f).Normalized();
	}

	public void SetMoveBounds(Rect2 bounds)
	{
		MoveBounds = bounds;
		ClampDogInsideBounds();
	}

	private Vector2 ClampPointInsideBounds(Vector2 point)
	{
		float minX = MoveBounds.Position.X + BodyRadius;
		float minY = MoveBounds.Position.Y + BodyRadius;
		float maxX = MoveBounds.Position.X + MoveBounds.Size.X - BodyRadius;
		float maxY = MoveBounds.Position.Y + MoveBounds.Size.Y - BodyRadius;

		return new Vector2(
			Mathf.Clamp(point.X, minX, maxX),
			Mathf.Clamp(point.Y, minY, maxY)
		);
	}

	private void ClampDogInsideBounds()
	{
		Vector2 clampedPosition = ClampPointInsideBounds(GlobalPosition);

		if (!clampedPosition.IsEqualApprox(GlobalPosition))
		{
			GlobalPosition = clampedPosition;
			Velocity = Vector2.Zero;
		}
	}

	private void MoveTowardTarget(Vector2 target, float stopDistance)
	{
		target = ClampPointInsideBounds(target);
		float distance = GlobalPosition.DistanceTo(target);

		if (distance <= stopDistance)
		{
			Velocity = Vector2.Zero;
			State = DogState.Idle;
			return;
		}

		Vector2 direction = GlobalPosition.DirectionTo(target);
		Velocity = direction * Speed;
	}

	private void FollowOwner()
	{
		Vector2 target = ClampPointInsideBounds(OwnerPosition);
		float distance = GlobalPosition.DistanceTo(target);

		if (distance <= 80.0f)
		{
			Velocity = Vector2.Zero;
			return;
		}

		Vector2 direction = GlobalPosition.DirectionTo(target);
		Velocity = direction * Speed;
	}

	private void MoveTowardBall()
	{
		Vector2 target = ClampPointInsideBounds(BallPosition);
		float distance = GlobalPosition.DistanceTo(target);

		if (distance <= 22.0f)
		{
			IsCarryingBall = true;
			State = DogState.FetchReturn;
			return;
		}

		Vector2 direction = GlobalPosition.DirectionTo(target);
		Velocity = direction * Speed;
	}

	private void ReturnBallToOwner()
	{
		Vector2 target = ClampPointInsideBounds(OwnerPosition);
		float distance = GlobalPosition.DistanceTo(target);

		if (distance <= 35.0f)
		{
			IsCarryingBall = false;
			State = DogState.Idle;
			return;
		}

		Vector2 direction = GlobalPosition.DirectionTo(target);
		Velocity = direction * Speed;
	}

	private void PlayAround(double delta)
	{
		_stateTimer -= delta;

		if (_stateTimer <= 0)
		{
			State = DogState.Idle;
			Velocity = Vector2.Zero;
			return;
		}

		Velocity = _playDirection * Speed * 0.7f;
	}

	private void ConfusedMove(double delta)
	{
		_stateTimer -= delta;

		if (_stateTimer <= 0)
		{
			State = DogState.Idle;
			Velocity = Vector2.Zero;
			return;
		}

		Velocity = _playDirection * Speed * 0.5f;
	}

	public override void _Draw()
	{
		Color bodyColor = new Color(0.95f, 0.65f, 0.25f);
		Color earColor = new Color(0.55f, 0.28f, 0.12f);
		Color eyeColor = Colors.Black;

		if (State == DogState.Confused)
		{
			bodyColor = new Color(1.0f, 0.35f, 0.25f);
		}
		else if (State == DogState.Sleep)
		{
			bodyColor = new Color(0.55f, 0.55f, 0.65f);
		}
		else if (State == DogState.Sit)
		{
			bodyColor = new Color(1.0f, 0.78f, 0.30f);
		}

		DrawCircle(Vector2.Zero, 22.0f, bodyColor);

		DrawCircle(new Vector2(-12, -15), 8.0f, earColor);
		DrawCircle(new Vector2(12, -15), 8.0f, earColor);

		DrawCircle(new Vector2(-7, -5), 3.0f, eyeColor);
		DrawCircle(new Vector2(7, -5), 3.0f, eyeColor);

		DrawCircle(new Vector2(0, 5), 4.0f, Colors.Black);

		Vector2 tailStart = new Vector2(-20, 4);
		Vector2 tailEnd = new Vector2(-38, -8);

		if (State == DogState.Play || State == DogState.Follow || State == DogState.Wander)
		{
			tailEnd = new Vector2(-40, -16);
		}

		DrawLine(tailStart, tailEnd, earColor, 5.0f);
	}
}
