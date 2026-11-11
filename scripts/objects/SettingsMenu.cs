using Godot;
using System;
using System.Linq.Expressions;

public partial class SettingsMenu : ScrollContainer
{

	private bool loadin = true;
	public void saveConfig() {
		ConfigFile conf = new ConfigFile();
		conf.SetValue("cfg", "volume", GetNode<Slider>("Container/VolumeSlider").Value);
		conf.SetValue("cfg", "hideply", GetNode<CheckButton>("Container/HidePlyButton").ButtonPressed);
		conf.SetValue("cfg", "color", GetNode<CheckButton>("Container/ColorButton").ButtonPressed);
		conf.SetValue("cfg", "color_box", GetNode<ColorPicker>("Container/Colors/BoxPicker").Color);
		conf.SetValue("cfg", "color_sky", GetNode<ColorPicker>("Container/Colors/SkyboxPicker").Color);
		conf.Save("user://config.cfg");
	}

	public override void _Ready()
	{
		CheckButton touchbtn = GetNode<CheckButton>("Container/HideTouchControlsButton");
		if (DisplayServer.IsTouchscreenAvailable())
			touchbtn.ButtonPressed = !TouchScreenControls.enabled;
		else
			touchbtn.Hide();
		Loadan(false);
	}

	public void Loadan(bool ingame) { 
		loadin = true;
		ConfigFile conf = new ConfigFile();
		if ( conf.Load( "user://config.cfg") == Error.Ok ) {
			GetNode<Slider>("Container/VolumeSlider").Value = (double)conf.GetValue("cfg", "volume", 100);
			GetNode<CheckButton>("Container/HidePlyButton").ButtonPressed = (bool)conf.GetValue("cfg", "hideply", false);
			GetNode<CheckButton>("Container/ColorButton").ButtonPressed = (bool)conf.GetValue("cfg", "color", false);
			GetNode<ColorPicker>("Container/Colors/BoxPicker").Color = (Color)conf.GetValue("cfg", "color_box", new Color(1,1,1,1));
			GetNode<ColorPicker>("Container/Colors/SkyboxPicker").Color = (Color)conf.GetValue("cfg", "color_sky", new Color(1,1,1,1));
		}
		if (ingame) {
			hideplys(GetNode<CheckButton>("Container/HidePlyButton").ButtonPressed);
			bool colorable = GetNode<CheckButton>("Container/ColorButton").ButtonPressed;
			initColorMats(colorable);
		}
		loadin = false;
	}

	private void hideplys(bool toggled_on) {
		foreach( Player ply in GetTree().GetNodesInGroup("player")) {
			MeshInstance3D mesh = ply.GetNode<MeshInstance3D>("Camera/Mesh");
			if (!toggled_on)
			 	mesh.Show();
			else
				mesh.Hide();
		}
	}

	private void _on_volume_slider_value_changed(double value)
	{
		AudioServer.SetBusVolumeDb(0, (float)(20 * Math.Log10(value/100)));
		if (!loadin)
			GetNode<AudioStreamPlayer>("Container/VolumeSlider/Test").Play();
	}


	private void _on_hide_ply_button_toggled(bool toggled_on)
	{
		hideplys(toggled_on);
	}

	private void _on_hide_touch_controls_button_toggled(bool toggled_on)
	{
		TouchScreenControls.enabled = !toggled_on;
		((TouchScreenControls)GetTree().Root.FindChild("TouchControls", true, false)).toggle(!toggled_on);
	}


	private void _on_close_button_up()
	{
		saveConfig();
		Hide();
	}

	private void initColorMats(bool colorable = false) {
		try {
			string box_tex =  "res://resources/2d/" + (colorable ? "box_tex_colorable.jpg" : "box_tex.jpg");
			string skybox_tex = "res://resources/2d/" + (colorable ? "skybox_colorable.png" : "skybox.png");
			ResourceLoader.Load<ShaderMaterial>("res://scenes/objects/box.tscn::ShaderMaterial_em5kt").SetShaderParameter("texture_albedo", ResourceLoader.Load<Texture2D>(box_tex));
			ResourceLoader.Load<ShaderMaterial>("res://scenes/objects/gameboard/skybox_gameboard.tscn::ShaderMaterial_p857o").SetShaderParameter("source_panorama", ResourceLoader.Load<Texture2D>(skybox_tex));
			if (colorable)
				applyColors( GetNode<ColorPicker>("Container/Colors/BoxPicker").Color, GetNode<ColorPicker>("Container/Colors/SkyboxPicker").Color );
			else 
				applyColors( new Color(1,1,1,1), new Color(1,1,1,1) );
		} catch{}
	}

	private void applyColors(Color? boxes, Color? skybox) {
		try {
			if (boxes != null) {
				ResourceLoader.Load<ShaderMaterial>("res://scenes/objects/box.tscn::ShaderMaterial_em5kt").SetShaderParameter("albedo", (Color)boxes);
			}
			if (skybox != null) {
				ResourceLoader.Load<ShaderMaterial>("res://scenes/objects/gameboard/skybox_gameboard.tscn::ShaderMaterial_p857o").SetShaderParameter("albedo", (Color)skybox);
			}
		}
		catch{}
	}

	private void _on_color_button_toggled(bool toggled_on)
	{
		GetNode<VBoxContainer>("Container/Colors").Visible = toggled_on;
		initColorMats(toggled_on);
	}
	
	private void _on_box_picker_color_changed(Color color)
	{
		applyColors(color, null);
	}


	private void _on_skybox_picker_color_changed(Color color)
	{
		applyColors(null, color);
	}

}


