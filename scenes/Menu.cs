using Godot;
using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public partial class Menu : Control
{
	// Called when the node enters the scene tree for the first time.

	int boxamount = 3;

	ScrollContainer ply_options;
	
	SettingsMenu settingsMenu;


	public override void _Ready()	{
		ply_options = GetNode<ScrollContainer>("PlayOptions");
		settingsMenu = GetNode<SettingsMenu>("SettingOptions");
		OptionButton ply_count_opt = ply_options.GetNode<OptionButton>("Container/PlayerCountOptions");
		OptionButton bot_count_opt = ply_options.GetNode<OptionButton>("Container/BotCountOptions");
		OptionButton upnp_mode_opt = ply_options.GetNode<OptionButton>("Container/Online/UpnpOptions");
		for(int i = 2; i < 9; i++) {
			ply_count_opt.AddItem($"{i} players", i-1);
		}
		ply_count_opt.Select(Gameboard.maxClients-2);
		bot_count_opt.AddItem("0 CPU", 1);
		bot_count_opt.AddItem("1 CPU", 2);
		bot_count_opt.AddItem("2 CPU", 3);
		bot_count_opt.AddItem("3 CPU", 4);
		bot_count_opt.AddItem("4 CPU", 5);
		bot_count_opt.Select(Gameboard.minBots);
		upnp_mode_opt.AddItem("UPNP off", 1);
		upnp_mode_opt.AddItem("UPNP on", 2);
		upnp_mode_opt.AddItem("UPNP on (IPV6)", 3);
		upnp_mode_opt.Select(0);
		balanceCPUClients();
		updateGameboardVars();
		loadSaveSlot();
	}

	private void loadSaveSlot() {
		var conf = new ConfigFile();
		Error msg = conf.LoadEncryptedPass( "user://data.sav", OldConst.PASS );
		if (msg == Error.Ok) {
			Player.saveSlot = (int)conf.GetValue("default", "slot", 0);
			GetNode<PopupPanel>("TutPanel").QueueFree();
		}
		else {
			Player.saveSlot = 0;
			if (msg == Error.FileNotFound) {
				OldConst.saveProfile(Player.saveSlot, new PlayerData());
				GetNode<PopupPanel>("TutPanel").Show();
			}
		}
	}

	private void balanceCPUClients() {
		OptionButton ply_count_opt = ply_options.GetNode<OptionButton>("Container/PlayerCountOptions");
		OptionButton bot_count_opt = ply_options.GetNode<OptionButton>("Container/BotCountOptions");
		if (bot_count_opt.Selected > ply_count_opt.Selected+1) {
			bot_count_opt.Select(ply_count_opt.Selected+1);
		}
	}

	private void updateGameboardVars() {
		Gameboard.size = boxamount;
		string ip = ply_options.GetNode<TextEdit>("Container/Online/TextIP").Text;
		Gameboard.IP = ip.Trim().Replace(" ", "");
		try {
			Gameboard.PORT = Math.Min(65535, Math.Abs( int.Parse( Regex.Match(  ply_options.GetNode<TextEdit>("Container/Online/TextPort").Text , @"[0-9]*").Value ) ) );
		} catch {
			Gameboard.PORT = 9001;
		}
	}

	private void startGame() {
		updateGameboardVars();
		GetTree().ChangeSceneToPacked(GD.Load<PackedScene>("res://scenes/objects/gameboard.tscn"));
	}

	private void _on_box_amount_changed(bool value_changed)
	{
		GD.Print(this); 
		if (value_changed) {
			boxamount = (int)ply_options.GetNode<Slider>("Container/BoxAmountSlider").Value;
			ply_options.GetNode<Label>("Container/BoxAmountLabel").Text = $"Box Amount:{boxamount}x{boxamount}x{boxamount}";
		}
	}

	private void _on_host_up()
	{
		Gameboard.game_t = OldConst.Gametype.ONLINE;
		Gameboard.isHost = true;
		Gameboard.size = boxamount;
		GetTree().ChangeSceneToPacked(GD.Load<PackedScene>("res://scenes/objects/gameboard.tscn"));
	}


	private void _on_join_up()
	{
		Gameboard.game_t = OldConst.Gametype.ONLINE;
		Gameboard.isHost = false;
		GetTree().ChangeSceneToPacked(GD.Load<PackedScene>("res://scenes/objects/gameboard.tscn"));
	}


	private void _on_play_button_button_up()
	{
		ply_options.Show();
	}


	private void _on_quit_button_button_up()
	{
		GetTree().Quit();
	}

	private void _on_settings_button_button_up()
	{
		settingsMenu.Visible = !settingsMenu.Visible;
	}
	private void _on_player_close_button_up()
	{
		ply_options.Hide();
	}


	private void _on_player_count_options_item_selected(long index)
	{
		Gameboard.maxClients = (int)index + 2;
		GD.Print(Gameboard.maxClients);
		balanceCPUClients();
	}

	private void _on_bot_count_options_item_selected(long index)
	{
		Gameboard.minBots = (int)index;
		GD.Print(Gameboard.minBots, " bots");
		balanceCPUClients();
	}


	private void _on_local_button_up()
	{
		Gameboard.game_t = OldConst.Gametype.LOCAL;
		Gameboard.minBots = 0;
		startGame();
	}


	private void _on_cpu_button_up()
	{
		Gameboard.game_t = OldConst.Gametype.LOCAL;
		Gameboard.minBots = Math.Max(1, Gameboard.minBots);
		startGame(); 
	}


	private void _on_host_button_up()
	{
		Gameboard.game_t = OldConst.Gametype.ONLINE;
		Gameboard.isHost = true;
		startGame();
	}


	private void _on_join_button_up()
	{
		Gameboard.game_t = OldConst.Gametype.ONLINE;
		Gameboard.isHost = false;
		startGame();
	}
	
	private void _on_user_button_button_up()
	{
		GetTree().ChangeSceneToPacked(GD.Load<PackedScene>("res://scenes/profile_room_actual.tscn"));
	}
	
	private void _on_tut_button_menu_button_up()
	{
		OldConst.popTutorial(GetTree(), "menu");
	}


	private void _on_tut_button_play_button_up()
	{
		OldConst.popTutorial(GetTree(), "play");
	}
	
	private void _on_upnp_options_item_selected(long index)
	{
		Gameboard.upnp_mode = (int)index;
		UpnpHelper.ipv6 = (int)index == 2;
		ply_options.GetNode<TextEdit>("Container/Online/TextIP").Editable = (int)index < 1;
		GD.Print(Gameboard.upnp_mode);
	}

	private void _on_tut_no_up()
	{
		GetNode<PopupPanel>("TutPanel").QueueFree();
	}


	private void _on_tut_yes_up()
	{
		GetNode<PopupPanel>("TutPanel").QueueFree();
		OldConst.popTutorial(GetTree(), "tutorial");
	}
}








