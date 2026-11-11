using Godot;
using GdCol = Godot.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;
using System.Numerics;
using System.Xml;
using System.Net.Http;
using System.Data;
using CsvHelper;
using System.Linq;
using System.Runtime.CompilerServices;

public partial class Player : CharacterBody3D
{

	public static int saveSlot = 0;
	public struct MoveList {
		public int move_t;
		public int cardinal;
		
		public void set(int m, int c) {
			move_t = m; 
			cardinal = c;
		}

	}
	public const float Speed = 16.0f;
	public const float JumpVelocity = 4.5f;

	public Camera3D camera;
	// Get the gravity from the project settings to be synced with RigidBody nodes.
	private bool freelook = false;

	public PlayerAI thunker = null;
	private int selected_box_id = -1;
	public AstralProjector astral_projection;

	private int possible_play_i = 0;
	
	public OldConst.Marker possible_marker;
	
	private bool disable_ghost_input = false;
	public int userid = -1;

	public Control pointBoard;
	public OldConst.Marker marker_t = OldConst.Marker.ROUND;

	public Godot.Timer teleport;
	private bool is_menu_open = false;

	public PlayerAppearance appearance;

	private void inputHoldSelect() {
		if (Input.IsActionJustPressed("hold_select")) {
			freelook = true;
			Input.MouseMode = Input.MouseModeEnum.Confined;
		} else if (Input.IsActionJustReleased("hold_select")) {
			freelook = false;
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
	}

	private void inputSelectBox() {
		if (Input.IsActionJustReleased("select_box")) {
			int new_id = highlightBox(100);
			selectBoxId(new_id);
		}
	}

	private void inputConfirmSelect() {
		if (selected_box_id > -1 && Input.IsActionJustReleased("confirm_select")) {
			EmitSignal(SignalName.sigConfirmSelectBox, userid, selected_box_id);
		}
	}

	private void inputViewRotate() {
		if (!freelook) {
			Vector2 m_vel = Input.GetLastMouseVelocity() / -60;
			Vector2 ctrl_look = Input.GetVector("look_right", "look_left", "look_down", "look_up") * 2;
			if (ctrl_look != Vector2.Zero) 
				m_vel = ctrl_look;
			float rot_Y = Math.Clamp(m_vel.Y+camera.RotationDegrees.X, -90, 90);
			camera.RotationDegrees = new Vector3(rot_Y, camera.RotationDegrees.Y + m_vel.X, 0);

		}
	}

	private void inputMove() {
		Vector3 velocity = Velocity;
		Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_fwd", "move_bck");
		float zVel = Input.IsActionPressed("move_up") ? 1 : Input.IsActionPressed("move_down") ? -1 : 0;
		Vector3 direction = (camera.Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized() + new Vector3(0, zVel, 0);
		inputTeleport(direction);
		if (direction != Vector3.Zero)
		{
			velocity = direction * Speed;
		}
		else
		{
			velocity = Vector3.Zero;
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	private void inputGhostCycle() {
		if (!disable_ghost_input && possible_marker != OldConst.Marker.ROUND_GHOST && astral_projection.possible_plays[possible_marker].ContainsKey(selected_box_id) ) {
			ImmutableArray<MoveList> cur_plays = astral_projection.possible_plays[possible_marker][selected_box_id];
			int old_possible_play = possible_play_i;
			if (Input.IsActionJustReleased("cycle_play_bck"))
				possible_play_i = possible_play_i+1 >= cur_plays.Length ? 0 : possible_play_i+1;
			else if (Input.IsActionJustReleased("cycle_play_fwd"))
				possible_play_i = possible_play_i-1 < 0 ? cur_plays.Length - 1 : possible_play_i - 1;
			if (possible_play_i != old_possible_play) {
				moveListToGhost( cur_plays[possible_play_i] ); 
			}
			astral_projection.setPlayIndex(possible_play_i, possible_marker, selected_box_id, possible_marker != marker_t);
		}
	}

	private void inputGhostToggle() {
		if (astral_projection != null && (selected_box_id > -1 || !disable_ghost_input) && Input.IsActionJustReleased("toggle_play")) {
			if (possible_marker != OldConst.Marker.ROUND_GHOST) {
				OldConst.Marker enemy_m = OldConst.getEnemyMarker(possible_marker);
				if (enemy_m == marker_t)
					possible_marker = OldConst.Marker.ROUND_GHOST;
				else
					possible_marker = enemy_m; 
			} else
				possible_marker = marker_t;
			astral_projection.setGhostVisible(true, possible_marker);
			setGhostPos();
			EmitSignal(SignalName.sigGetPossibleMoves, userid, selected_box_id);
		}
	}

	private void inputTeleport(Vector3 dir_vel) {
		if (teleport.IsStopped() && Input.IsActionJustReleased("teleport") ) {
			if (selected_box_id > -1)
				Position = OldConst.index2Vec(Gameboard.size, selected_box_id) * Gameboard.BOX_DIST + new Vector3(2.6f, -3.2f, -3.4f);
			else {
				Velocity = dir_vel == Vector3.Zero ? camera.Transform.Basis * new Vector3(0,0,-40 * Speed) : dir_vel * Speed * 40;
				MoveAndSlide();
			}
			teleport.Start(0.5);
			TextureRect eye_tex = teleport.GetNode<TextureRect>("Tex");
			eye_tex.SelfModulate = new Color(1,1,1,0.05f);
			Tween tw = eye_tex.CreateTween();
			tw.TweenProperty(eye_tex, "self_modulate:a", 0, 0.4);
			teleport.GetNode<AudioStreamPlayer3D>("Sound").Play();
		}
	}

	private void setPointboardToColor() {
		pointBoard.Modulate = OldConst.getMarkerColor(marker_t) * ( Gameboard.game_t == OldConst.Gametype.ONLINE && isCurrentPlayer() ? new Color(0.8f, 0.8f, 0.8f, 1f) : new Color(1,1,1,1));
	}

	public void playSound(in string name) {
		AudioStreamPlayer local_sound = GetNode<AudioStreamPlayer>("PersonalSound");
		local_sound.Stream = OldConst.getOggStream(name);
		local_sound.Play();
	}

	private int highlightBox(in int rayl) {
		Vector2 mpos = GetViewport().GetMousePosition();
		Vector3 from = camera.ProjectRayOrigin(mpos);
		PhysicsRayQueryParameters3D physray = new PhysicsRayQueryParameters3D();
		physray.From = from;
		physray.To = from + camera.ProjectRayNormal(mpos) * rayl;
		physray.CollideWithAreas = true;
		GdCol.Dictionary results =  GetWorld3D().DirectSpaceState.IntersectRay(physray);
		if (results.ContainsKey("collider")) {
			Node3D hitbox = results["collider"].As<Node3D>();
			if (hitbox.GetParent().Name == "boxes") {
				//GD.Print(hitbox.Name);				
				return Convert.ToInt32(hitbox.Name);
			}
		}
		clearGhost();
		return -1;
	}

	private void selectBoxId(int new_id) {
		if (selected_box_id != new_id) {
			if (new_id < 0) {
				EmitSignal(SignalName.sigGetPossibleMoves, userid, -1);
			}
			else
				EmitSignal(SignalName.sigGetPossibleMoves, userid, new_id);
		}
		selected_box_id = new_id;
		if (possible_marker == OldConst.Marker.ROUND_GHOST || !astral_projection.possible_plays[possible_marker].ContainsKey(new_id) || astral_projection.possible_plays[possible_marker][new_id].Length < 1)
			clearGhost(true);
		else
			setGhostPos();
		GD.Print("Selected:", OldConst.index2Vec(Gameboard.size, selected_box_id), selected_box_id);
	}

	public bool isAI() {
		return thunker != null;
	}

	public bool isCurrentPlayer() {
		return !isAI() && Gameboard.game_t == OldConst.Gametype.ONLINE && IsMultiplayerAuthority() || (Gameboard.game_t == OldConst.Gametype.LOCAL && camera.Current);
	}

	// ghost markers
	public void initGhost(in AstralProjector ghost_marker) {
		astral_projection = ghost_marker;
	}

	
	
	public void setGhostPos() {
			if (possible_marker != OldConst.Marker.ROUND_GHOST && astral_projection != null) {
				astral_projection.setGhostPos( OldConst.index2Vec(Gameboard.size, selected_box_id) * Gameboard.BOX_DIST + new Vector3(2.6f, -3.2f, -3.4f), possible_marker);
			}
	}

	public void moveListToGhost(in MoveList move_l) {
			if (possible_marker != OldConst.Marker.ROUND_GHOST && astral_projection != null) {
				astral_projection.moveListToGhost(move_l, possible_marker);
			}
	}

	public void clearGhost(bool all=false) {
		if (astral_projection != null) {
			if (all)
				astral_projection.clearGhost();
			else 
				astral_projection.setGhostVisible(false, possible_marker);
			disable_ghost_input = true;
			possible_play_i = 0;
		}
	}

	public void setPossiblePlay(in int box_id, in ImmutableArray<MoveList>? cardinal_id) {
		possible_play_i = 0;
		if (cardinal_id != null) {
			if (!astral_projection.possible_plays[possible_marker].ContainsKey(box_id))
				astral_projection.possible_plays[possible_marker][box_id] = (ImmutableArray<MoveList>)cardinal_id;
			moveListToGhost( astral_projection.possible_plays[possible_marker][box_id][possible_play_i] );
		}
		astral_projection.setPlayIndex(possible_play_i, possible_marker, selected_box_id, possible_marker != marker_t);
		astral_projection.setGhostVisible(cardinal_id != null, possible_marker);
		disable_ghost_input = false;
	}


	[Signal]
	public delegate void sigConfirmSelectBoxEventHandler(int userid, int selected_box_id);

	[Signal]
	public delegate void sigGetPossibleMovesEventHandler(int userid, int selected_box_id);

	[Signal]
	public delegate void sigProcessAIEventHandler(int userid);

	public void _sigSetupTurn(int box_id = -1, int marker = (int)OldConst.Marker.ROUND_GHOST) {
		setPointboardToColor();
		OldConst.Marker new_mark = (OldConst.Marker)marker;
		if (new_mark == OldConst.Marker.ROUND_GHOST)
			new_mark = marker_t;
		possible_marker = new_mark;
		selected_box_id = -1;
		if (box_id > -1 && !isAI()) {
			selectBoxId(box_id);
		}
		if (isAI()) {
			EmitSignal(SignalName.sigProcessAI, userid);
		}
	}

	public void _sigToggleMenu(bool hidden) {
		if (hidden)
			Input.MouseMode = Input.MouseModeEnum.Captured;	
		else 
			Input.MouseMode = Input.MouseModeEnum.Visible;
		is_menu_open = !hidden;
	}
	
	// ONLINE LOGIC

	public partial class Serialized : Node {
		public OldConst.Marker marker_t;
		public int userid;
	}

	// ONLINE LOGIC
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable, TransferChannel = 2)]
	private void syncVisuals(Vector3 pos, Vector3 rotation) {
		Position = pos;
		Rotation = rotation;
	}
	
	// ONLINE LOGIC

	//COSMETICS

	public static void addWin(OldConst.Gametype gt, int size, int amount = 1) {
		PlayerData data = OldConst.loadProfile(saveSlot);
		if (gt == OldConst.Gametype.ONLINE) {
			int index = Array.FindIndex( data.wins_online, (vec) => vec.X == size );
			if (index > -1)
				data.wins_online[index] = new Vector2(size, data.wins_online[index].Y + amount);
			else 
				data.wins_online = data.wins_online.Append(new Vector2(size, amount)).ToArray();
		}
		else if (gt == OldConst.Gametype.LOCAL) {
			int index = Array.FindIndex( data.wins_cpu, (vec) => vec.X == size );
			if (index > -1)
				data.wins_cpu[index] = new Vector2(size, data.wins_cpu[index].Y + amount);
			else 
				data.wins_cpu = data.wins_cpu.Append(new Vector2(size, amount)).ToArray();
		}
		OldConst.saveProfile(saveSlot, data);
	}

	public static void loadPlayerCosmetics(in ImmutableArray<Player> plys) {
		using ( var skin_csv = FileAccess.Open(OldConst.STATIC_PATH + "skin.txt", FileAccess.ModeFlags.Read) ) {
			string[] data = skin_csv.GetAsText().Split('\n');
			foreach (Player ply in plys)
			{
				int skin = ply.appearance.skin;
				if (skin > 0) {
					ply.GetNode<MeshInstance3D>("Camera/Mesh").SetSurfaceOverrideMaterial(0, ResourceLoader.Load<StandardMaterial3D>(PlayerAppearance.SKIN_PATH + data[skin].TrimEnd() + ".tres"));
				}
			}
			skin_csv.Close();
		}
		using ( var badge_csv = FileAccess.Open(OldConst.STATIC_PATH + "badge.txt", FileAccess.ModeFlags.Read) ) {
			string[] data = badge_csv.GetAsText().Split('\n');
			foreach (Player ply in plys)
			{
				int badge = ply.appearance.badge;
				MeshInstance3D badge3d = ply.GetNode<MeshInstance3D>("Camera/Mesh/Badge");
				if (badge > 0) {
					badge3d.Show();
					Texture img = ResourceLoader.Load<Texture>( PlayerAppearance.BADGE_PATH + data[badge].Split(',')[1] + ".png" );
					badge3d.GetSurfaceOverrideMaterial(0).Set("albedo_texture", img);
				} else {
					badge3d.Hide();
				}
				ply.GetNode<MeshInstance3D>("Camera/Mesh").Scale = Vector3.One * (float)(0.166 + ply.appearance.badge / 50);
			}
			badge_csv.Close();
		}
		foreach(Player ply in plys) {
			((Label)ply.pointBoard.FindChild("PlyName", true, false)).Text = (!ply.isAI() ? $"{ply.appearance.name}" : $"{ply.appearance.name}(CPU)") + $"({ply.userid})";
			((Label)ply.pointBoard.FindChild("Points", true, false)).Text = $"{0} points";
			((TextureRect)ply.pointBoard.FindChild("Marker", true, false)).Texture = ResourceLoader.Load<Texture2D>( "res://resources/ui/marker_" + (ply.marker_t == OldConst.Marker.ROUND ? 'o' : 'x') + ".png" );
			if (Gameboard.game_t == OldConst.Gametype.ONLINE && ply.isCurrentPlayer()) {
				((Label)ply.pointBoard.FindChild("PlyName", true, false)).Text = "(You)";
			}
		}
	}

	private void spawnRandomPos() {
		Random rand  = new Random();
		int log = (int)Math.Log(userid + 2, 5);
		Position = new Vector3( rand.Next(5, 10) + log, rand.Next(-10, 10) + log/2, rand.Next(-10, -5) - log );
	}

	//COSMETICS

	public override void _Ready()
	{
		appearance = new PlayerAppearance();
		camera = GetNode<Camera3D>("Camera");
		Input.MouseMode = Input.MouseModeEnum.Captured;
		pointBoard = GD.Load<PackedScene>("res://scenes/objects/point_counter.tscn").Instantiate<Control>();
		teleport = GetNode<Timer>("TeleportTime");
		//((Label)pointBoard.FindChild("PlyName", true, false)).Text = (!isAI() ? $"{appearance.name}" : $"{appearance.name}(CPU)") + $"{userid}";
		//((Label)pointBoard.FindChild("Points", true, false)).Text = $"{0}";
		AddChild(pointBoard);

		if (Gameboard.game_t == OldConst.Gametype.ONLINE) {
			SetMultiplayerAuthority(userid);
			if (isCurrentPlayer()) {
				camera.MakeCurrent();
				((Label)pointBoard.FindChild("PlyName", true, false)).Text = "(You)";
				spawnRandomPos();
			}
		} else if (Gameboard.game_t == OldConst.Gametype.LOCAL) {
			spawnRandomPos();
		}
		possible_marker = marker_t;
	}

	public override void _Input(InputEvent @event) {
		if (!isCurrentPlayer()  && is_menu_open)
			return;
		if (@event is InputEventScreenTouch eventTouch)
		{
			if (!eventTouch.Pressed)
			{
				inputSelectBox();
			}
		}
	}

	public override void _Process(double delta)
	{
		if (isCurrentPlayer() && !is_menu_open) { //local mp solution
			inputViewRotate();
			inputHoldSelect();
			inputSelectBox();
			inputGhostCycle();
			inputConfirmSelect();
			inputGhostToggle();

		}
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (isCurrentPlayer() && !is_menu_open) { //local mp solution
			inputMove();
			if (Gameboard.game_t == OldConst.Gametype.ONLINE)
				Rpc("syncVisuals", Position, camera.Rotation);
		}
	}
}
