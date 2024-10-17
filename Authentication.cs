using Godot;
using LootLocker.Requests;

public partial class Authentication : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		LootLockerSDKManager.StartGuestSession((response) =>
		{
			if (!response.success)
			{
				GD.Print("failed", response.errorData);
				return;
			}
			GD.Print("success", LLlibs.ZeroDepJson.Json.Serialize(response));
		});
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
