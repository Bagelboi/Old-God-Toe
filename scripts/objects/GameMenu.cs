using Godot;
using System;

public partial class GameMenu : Panel
{

	private SettingsMenu settingsMenu;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		settingsMenu = GD.Load<PackedScene>("res://scenes/objects/setting_options.tscn").Instantiate<SettingsMenu>();
		AddChild(settingsMenu);
	}

	public void loadan() {
		settingsMenu.Loadan(true);
	}

	public void toggle() {
		if (Visible) {
			settingsMenu.Hide();
			Hide();
		} else {
			settingsMenu.Hide();
			Show();
		}
	}
	
	private void _on_resume_button_button_up()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		settingsMenu.Hide();
		Hide();
		EmitSignal(SignalName.sigCloseMenu, true);
	}


	private void _on_settings_button_button_up()
	{
		settingsMenu.Visible = !settingsMenu.Visible;
	}

	private void _on_quit_button_button_up()
	{
		EmitSignal(SignalName.sigQuitGameButton);
	}
	
	private void _on_tutorial_button_button_up()
	{
		OldConst.popTutorial(GetTree(), "game");
	}

	[Signal]
	public delegate void sigQuitGameButtonEventHandler();
	[Signal]
	public delegate void sigCloseMenuEventHandler(bool hidden);

}
