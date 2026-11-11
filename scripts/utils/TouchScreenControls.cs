using Godot;
using System;

public partial class TouchScreenControls : Control
{
	public static bool enabled = false;

	public override void _Ready()
	{
		enabled = DisplayServer.IsTouchscreenAvailable();
		toggle(enabled);
	}

	public void toggle(bool enable) {
		if (!enable)
			Hide();
		else
			Show();
	}

	private void pressAndRelease(string act) {
		Input.ActionPress(act);
		Input.ActionRelease(act);
	}


	private void _on_teleport_button_up()
	{
		pressAndRelease("teleport");
	}


	private void _on_ghost_back_button_up()
	{
		pressAndRelease("cycle_play_bck");
	}


	private void _on_ghost_toggle_button_up()
	{
		pressAndRelease("toggle_play");
	}


	private void _on_ghost_forward_button_up()
	{
		pressAndRelease("cycle_play_fwd");
	}


	private void _on_menu_button_up()
	{
		pressAndRelease("quit");
	}


	private void _on_confirm_button_up()
	{
		pressAndRelease("confirm_select");
	}

	private void _on_button_down_button_up()
	{
		Input.ActionRelease("move_down");
	}


	private void _on_button_up_button_up()
	{
		Input.ActionRelease("move_up");
	}

	private void _on_button_up_button_down()
	{
		Input.ActionPress("move_up");
	}


	private void _on_button_down_button_down()
	{
		Input.ActionPress("move_down");
	}

}







