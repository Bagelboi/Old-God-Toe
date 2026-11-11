using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

public partial class ProfileRoom : Node3D
{
	enum State
	{
		SKIN=0,
		SCORE=1,
		BADGENAME=3,
		APPLY=2
	}
	int _curstate = 0;
	int curState { 
		get { return _curstate; } 
		set { if (value < 0) 
				_curstate = 3;
			  else if (value > 3)
				_curstate = 0;
			  else 
			  	_curstate = value;  } }
	Tween state_switch_tween;

	string[] states = {
		"SetSkin", "SetScore", "SetApply",  "SetBadgeName"
	};
	private ImmutableArray<string> data_skins;
	private ImmutableArray<string> data_badges;
	Camera3D camera;
	PlayerData ply_dat;



	public override void _Ready()
	{
		ply_dat = OldConst.loadProfile(Player.saveSlot);
		GetNode<OptionButton>("SetApply/Control/Container/OptionCont/LoadButton").Select(Player.saveSlot);
		camera = GetNode<Camera3D>("Camera");
		curState = (int)State.BADGENAME;
		moveToState(curState);
		VBoxContainer btns = GetNode<VBoxContainer>("ControlMain/BtnSwitch");
		btns.GetNode<Button>("SetApply").ButtonUp += () => moveToState((int)State.APPLY);
		btns.GetNode<Button>("SetBadgeName").ButtonUp += () => moveToState((int)State.BADGENAME);
		btns.GetNode<Button>("SetScore").ButtonUp += () => moveToState((int)State.SCORE);
		btns.GetNode<Button>("SetSkin").ButtonUp += () => moveToState((int)State.SKIN);
		using ( var skin_csv = FileAccess.Open(OldConst.STATIC_PATH + "skin.txt", FileAccess.ModeFlags.Read) ) {
			data_skins = skin_csv.GetAsText().Split('\n').ToImmutableArray();
			skin_csv.Close();
		}
		using ( var badge_csv = FileAccess.Open(OldConst.STATIC_PATH + "badge.txt", FileAccess.ModeFlags.Read) ) {
			data_badges = badge_csv.GetAsText().Split('\n').ToImmutableArray();
			GD.Print(data_badges);
			badge_csv.Close();
		}
		GetNode<MenuButton>("SetSkin/Control/SkinSelect").GetPopup().IdPressed += _sigSkinSelect;
		PopupMenu badgepop = GetNode<MenuButton>("SetBadgeName/Control/BadgeSelect").GetPopup();
		badgepop.IdPressed += _sigSelectBadge;
		scroll_board = GetNode<ScrollContainer>("SetScore/ViewportScoreboard/Scroll");
		initAll();
	}

	private void initAll() {
		PopupMenu badgepop = GetNode<MenuButton>("SetBadgeName/Control/BadgeSelect").GetPopup();
		badgepop.Clear(true);
		for (int i = 0; i < data_badges.Length; i++)
		{
			string badge = data_badges[i];
			badge = badge.Trim();
			string[] fields = badge.Split(',');
			Texture2D tex = fields[1].Length > 0 ? ResourceLoader.Load<Texture2D>( PlayerAppearance.BADGE_PATH + fields[1] + ".png" ) : new Texture2D();
			string info = fields[0] + " | " + (fields[2].Length > 0 ? fields[2] + " wins(CPU) or " : "") + (fields[3].Length > 0 ? fields[3] + " wins(Online)" : "");
			badgepop.AddIconItem(tex, info, i);
			badgepop.SetItemDisabled(badgepop.GetItemIndex(i), !isBadgeEligible( fields[2].Length > 0 ? fields[2].ToInt() : 0, fields[3].Length > 0 ? fields[3].ToInt() : 0 ));
		}
		populateScoreboard();
		updateBadgename();
		switchSkin(ply_dat.appearance.skin);
	}

	private void moveToState(int state) {
		if (state_switch_tween == null) {
			GD.Print(state, " ", state_switch_tween);
			Node stateNode = GetNode<Node>( states[state] );
			foreach (String node_name in states)
			{
				Node n = GetNode<Node>( node_name );
				n.GetNode<Control>("Control").Hide();
			}
			foreach( Node btn in GetNode<VBoxContainer>("ControlMain/BtnSwitch").GetChildren()) {
				if (btn.Name != "Close") {
					((Button)btn).Disabled = btn.Name == states[state];
				}
			}
			curState = state;
			Marker3D cam_marker = stateNode.GetNode<Marker3D>("Cam");
			state_switch_tween = camera.CreateTween();
			state_switch_tween.SetEase(Tween.EaseType.Out);
			state_switch_tween.SetTrans(Tween.TransitionType.Quart);
			state_switch_tween.SetParallel(true);
			state_switch_tween.TweenProperty(camera, "rotation", cam_marker.Rotation, 0.5);
			state_switch_tween.TweenProperty(camera, "position", cam_marker.Position, 0.5);
			state_switch_tween.Finished += () => { stateNode.GetNode<Control>("Control").Show(); state_switch_tween = null;};
			state_switch_tween.Play();
		}

	}



	private void _on_close_button_up()
	{
		OldConst.saveProfile(Player.saveSlot, ply_dat);
		GetTree().ChangeSceneToPacked(GD.Load<PackedScene>("res://scenes/mainmenu.tscn"));
	}

	//SKIN
	private void switchSkin(int id)
	{
		StandardMaterial3D skin_mat = ResourceLoader.Load<StandardMaterial3D>(PlayerAppearance.SKIN_PATH + data_skins[id].TrimEnd() + ".tres");
		GetNode<MeshInstance3D>("Mesh/skinrobe").SetSurfaceOverrideMaterial(0, skin_mat);
		GetNode<MeshInstance3D>("Mesh/skinmask").SetSurfaceOverrideMaterial(0, skin_mat);
		ply_dat.appearance.skin = id;
	}
	
	private void _sigSkinSelect(long id) {
		switchSkin((int)id);
	}
	//SKIN

	//BADGENAME

	private void updateBadgename() {
		GetNode<Label3D>("SetBadgeName/Label").Text = ply_dat.appearance.name + '\n' + data_badges[ ply_dat.appearance.badge ].Split(',')[0];
	}

	private void _on_name_edit_text_submitted(string new_text)
	{
		new_text = new_text.Trim().Substr(0, Math.Min(16, new_text.Length));
		if (new_text == "" || new_text.Length < 1)
			new_text = "Name";
		ply_dat.appearance.name = new_text;
		updateBadgename();
	}

	private void _sigSelectBadge(long id) {
		ply_dat.appearance.badge = (int)id;
		updateBadgename();
	}

	private bool isBadgeEligible(int wins_cpu, int wins_online) {
		try {
			return (0f + ply_dat.wins_cpu.Aggregate( (a, b) => a+b).Y ) >= wins_cpu || (0f + ply_dat.wins_online.Aggregate( (a, b) => a+b).Y >= wins_online);	
		} catch {
			return false;
		}
	}

	//BADGENAME

	//SCOREBOARD
	ScrollContainer scroll_board;
	private void _on_drag_page_value_changed(double value)
	{
		scroll_board.ScrollHorizontal = (int)value;
	}

	private VBoxContainer createScore(VBoxContainer basemodel, int numboxes, int cpu, int online) {
		VBoxContainer newscore = (VBoxContainer)basemodel.Duplicate();
		newscore.Name = numboxes.ToString();
		newscore.GetNode<Label>("Size").Text = numboxes + "x" + numboxes;
		newscore.GetNode<Label>("WinsCPU").Text = cpu.ToString();
		newscore.GetNode<Label>("WinsOnline").Text = online.ToString();
		return newscore;
	}

	private void populateScoreboard() {
		VBoxContainer basemodel = scroll_board.GetNode<VBoxContainer>("ScoresCont/Header");
		VBoxContainer total = scroll_board.GetNode<VBoxContainer>("ScoresCont/Total");
		foreach (VBoxContainer scoren in scroll_board.GetNode("ScoresCont").GetChildren()) {
			if (scoren != basemodel && scoren != total)
				scoren.QueueFree();
		}
		System.Collections.Generic.Dictionary<int, int[]> wins = new System.Collections.Generic.Dictionary<int, int[]>();
		foreach (Vector2 win in ply_dat.wins_cpu)
		{
			int index = (int)win.X;
			wins[index] = new int[2] {(int)win.Y, 0};
		}
		foreach (Vector2 win in ply_dat.wins_online)
		{
			int index = (int)win.X;
			if (!wins.ContainsKey(index))
				wins[index] = new int[2] {0, (int)win.Y};
			else
				wins[index][1] = (int)win.Y;
		}
		foreach( int score in wins.Keys.OrderBy(w => w) )
			basemodel.GetParent().AddChild(createScore(basemodel, score, wins[score][0], wins[score][1]));
		try {
			total.GetNode<Label>("WinsCPU").Text = ply_dat.wins_cpu.Aggregate( (a, b) => a+b).Y.ToString();
		} catch {}
		try {
			total.GetNode<Label>("WinsOnline").Text = ply_dat.wins_online.Aggregate( (a, b) => a+b).Y.ToString();
		} catch {}
	}
	
	//SCOREBOARD
	
	//APPLY
	private void _on_button_save_button_up()
	{
		OldConst.saveProfile(Player.saveSlot, ply_dat);
	}
	
	private void _on_reset_button_button_up()
	{
		GetNode<PopupPanel>("SetApply/Control/ResetPanel").Show();

	}

	private void _on_reset_yes()
	{
		GetNode<PopupPanel>("SetApply/Control/ResetPanel").Hide();
		SetProcess(true);
		ply_dat = new PlayerData();
		OldConst.saveProfile(Player.saveSlot, ply_dat); 
		initAll();
	}
	
	private void _on_reset_no()
	{
		GetNode<PopupPanel>("SetApply/Control/ResetPanel").Hide();
	}

	private void _on_load_button_item_selected(long index)
	{
		Player.saveSlot = (int)index;
		ply_dat = OldConst.loadProfile((int)index);
		initAll();
	}

	//APPLY
	
	private void _on_tut_button_button_up()
	{
		OldConst.popTutorial(GetTree(), "profile/" + states[curState].ToLower().Substring(3));
	}

}



















