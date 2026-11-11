using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.IO.Pipes;
using System.Linq;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Resolvers;
using Vector2 = Godot.Vector2;
using Vector3 = Godot.Vector3;

//this isnt SOLID its LIQUID
public partial class Gameboard : Node3D
{
	public static Vector3 BOX_DIST = new Vector3(6f,6.5f,6.2f);
	public static Vector3 MARKER_POS = new Vector3(2.8f, -4.2f, -3.2f);
	PackedScene[] marker_scns = {
		GD.Load<PackedScene>("res://scenes/objects/demon.tscn"),
		GD.Load<PackedScene>("res://scenes/objects/beast.tscn"),
		GD.Load<PackedScene>("res://scenes/objects/demon_stone.tscn"),
		GD.Load<PackedScene>("res://scenes/objects/beast_stone.tscn")
	}; 

	PackedScene box_scn = GD.Load<PackedScene>("res://scenes/objects/box.tscn");
	PackedScene player_scn = GD.Load<PackedScene>("res://scenes/objects/player.tscn");

	Dictionary<int, Player> players = new Dictionary<int, Player>();
	Dictionary<int, OldConst.Marker> markers_placed = new Dictionary<int, OldConst.Marker>(); // markers by type
	int cur_turn = 0;

	List<int> turn_order = new List<int>();
	public static int size = 3;

	public static OldConst.Gametype game_t = OldConst.Gametype.LOCAL;

	bool round_won = false;

	public GameMenu menu;


	//child nodes
	Label state_display;
	Node3D node_boxes;
	CsgMesh3D circle;

	MeshInstance3D box_outline;
	GpuParticles3D particle_restart;
	GpuParticles3D particle_spawn;

	AudioStreamPlayer3D local_sound;

	AstralProjector astral_projector;

	Sprite3D marker_last;

	TextureRect turn_marker;

	//AudioStreamPlayer ambient_sound;


	public static bool posInBounds(in Vector3 pos) {
		return !(pos.X >= size || pos.X < 0 || pos.Y >= size || pos.Y < 0 || pos.Z >= size || pos.Z < 0);
	}

	private bool markerIsSame(in int index, in OldConst.Marker marker_t, in bool emptySpacePass = false) {
		return emptySpacePass ? !markers_placed.ContainsKey(index) || marker_t == markers_placed[index] : markers_placed.ContainsKey(index) && marker_t == markers_placed[index];
	}

	public int checkDir4Markers(OldConst.Marker marker_t, in int index, in Vector3 direction) {
		int marker_c = -1;
		int i_index = index;
		Vector3 cur_pos;
		do {
			if (markers_placed.ContainsKey(i_index) && markers_placed[i_index] != marker_t) {
				break;
			}
			cur_pos = OldConst.index2Vec(size, i_index) + direction;
			i_index = OldConst.vec2Index(size, cur_pos);
			marker_c++;
		} while(marker_c < size && posInBounds(cur_pos)); 
		return marker_c;
	}

	public void doWin(in int uid) {
		round_won = true;
		EmitSignal(SignalName.sigSetupTurn, -1);
		if (uid > -1) {
			Player winner = players[uid];
			if ( (game_t == OldConst.Gametype.ONLINE && winner.isCurrentPlayer() ) || ( game_t == OldConst.Gametype.LOCAL && minBots > 0 && !winner.isAI() ) ) {
				setDisplayState($"You won!");
				Player.addWin(game_t, size, 100);
			}
			else
				setDisplayState( $"{ ((Label)winner.pointBoard.FindChild("PlyName")).Text } wins." );
			Label points = (Label)winner.pointBoard.FindChild("Points", true, false);
			points.Text = $"{int.Parse( Regex.Match(points.Text, @"[0-9].").Value ) + 1} points";
		} else
			setDisplayState( $"Tie game." );
		Timer time = new Godot.Timer();
		time.Timeout += () => {clearBoard(); time.QueueFree();};
		GetTree().Root.AddChild(time);
		time.Start(1.5);
	}

	public bool checkWin(in int index) {
		Vector3 pos = OldConst.index2Vec(size, index);
		OldConst.Marker marker_t = markers_placed[index];
		//GD.Print(marker_t);
		foreach (Vector3 direction in OldConst.cardinals)
		{
			int marker_c = 0;
			Vector3 cur_pos = pos + direction;
			while(marker_c < size-1 && posInBounds(cur_pos)) {
				if (markerIsSame(OldConst.vec2Index(size, cur_pos), marker_t))
					marker_c++;
				else
					break;
				cur_pos += direction;
			}
			//GD.Print(marker_c, cur_pos);
			if (marker_c >= size-1)
				return true;
			else {
				int marker_cm = 0; //mirrored
				Vector3 direction_cm = direction * -1;
				cur_pos = pos + direction_cm;
				while(marker_cm < size-1 && posInBounds(cur_pos)) {
					if (markerIsSame(OldConst.vec2Index(size, cur_pos), marker_t))
						marker_cm++;
					else
						break;
					cur_pos += direction_cm;
				}
				//GD.Print(marker_cm, cur_pos, "inverse");
				if (marker_c + marker_cm >= size-1)
					return true;
			}
		}
		return false;
	}

	public bool checkDraw() {
		return markers_placed.Count >= Math.Pow(size, 3);
	}

	public ImmutableArray<Player.MoveList> getPossibleMoves(in int index, in OldConst.Marker marker_t) {
		Vector3 pos = OldConst.index2Vec(size, index);
		List<Player.MoveList> possible_m = new List<Player.MoveList>();
		//GD.Print(marker_t);
		for (int i = 0; i < OldConst.cardinals.Length; i++)
		{
			Player.MoveList moveList = new Player.MoveList();
			moveList.set(-1, i);
			Vector3 direction = OldConst.cardinals[i];
			int marker_c = 0;
			Vector3 cur_pos = pos + direction;
			while(marker_c < size-1 && posInBounds(cur_pos)) {
				if (markerIsSame(OldConst.vec2Index(size, cur_pos), marker_t, true)) 
					marker_c++;
				else
					break;
				cur_pos += direction;
			}
			//GD.Print(marker_c, cur_pos);
			if (marker_c >= size-1) {
				moveList.move_t = 0; // straight on
			}	
			else {
				int marker_cm = 0; //mirrored
				Vector3 direction_cm = direction * -1;
				cur_pos = pos + direction_cm;
				while(marker_cm < size-1 && posInBounds(cur_pos)) {
					if (markerIsSame(OldConst.vec2Index(size, cur_pos), marker_t, true))
						marker_cm++;
					else
						break;
					cur_pos += direction_cm;
				}
				//GD.Print(marker_cm, cur_pos, "inverse");
				if (marker_cm >= size-1)
					moveList.move_t = 2; // inverse of straight on
				else if (marker_c + marker_cm == size-1)
					moveList.move_t = 1; // both sides
			}
			if (moveList.move_t > -1)
				possible_m.Add(moveList);
		}
		GD.Print(possible_m.ToArray().ToImmutableArray().Length, "cm tamanho da rola");
		return possible_m.ToArray().ToImmutableArray();
	}

	//AI

	public void processAI(in Player ply) {
		if (ply.thunker != null && ply.isAI()) {
			if (game_t == OldConst.Gametype.LOCAL || (game_t == OldConst.Gametype.ONLINE && Multiplayer.IsServer())) {
				_sigConfirmSelectBox(ply.userid, 
					ply.thunker.figureItOut( aiGetOpenMarkers(), 
						aiGetIndexWeights(ply.marker_t), 
						aiGetIndexWeights(OldConst.getEnemyMarker(ply.marker_t))));
			}
		}
	}

	public Dictionary<int, PlayerAI.IndexWeight> aiGetIndexWeights(OldConst.Marker marker_t) {
		
		Dictionary<int, PlayerAI.IndexWeight> weights = new Dictionary<int, PlayerAI.IndexWeight>();
		foreach ( KeyValuePair<int, OldConst.Marker> marker_i in markers_placed.Where( (marker) => marker.Value == marker_t )) {
			Vector3 pos = OldConst.index2Vec(size, marker_i.Key);
			for (int i = 0; i < OldConst.cardinals.Length; i++)
			{
				Vector3 direction = OldConst.cardinals[i];
				int marker_c = 0;
				Vector3 cur_pos = pos + direction;
				while(marker_c < size-1 && posInBounds(cur_pos)) {
					if (markerIsSame(OldConst.vec2Index(size, cur_pos), marker_t, true)) 
						marker_c++;
					else
						break;
					cur_pos += direction;
				}

				if (marker_c >= size-1) {
					int index = OldConst.vec2Index(size, cur_pos - direction);
					if (!weights.ContainsKey(index))
						weights[index] = new PlayerAI.IndexWeight();
					weights[index].intersections += 1;
					weights[index].compare_markers(marker_c + 1, i, 1);	
				}	

				else {
					int marker_cm = 0; //mirrored
					Vector3 direction_cm = direction * -1;
					Vector3 cur_pos_cm = pos + direction_cm;
					while(marker_cm < size-1 && posInBounds(cur_pos_cm)) {
						if (markerIsSame(OldConst.vec2Index(size, cur_pos_cm), marker_t, true))
							marker_cm++;
						else
							break;
						cur_pos_cm += direction_cm;
					}
					//GD.Print(marker_cm, cur_pos, "inverse");
					
					if (marker_cm >= size-1) {
						int index = OldConst.vec2Index(size, cur_pos_cm - direction_cm);
						if (!weights.ContainsKey(index))
							weights[index] = new PlayerAI.IndexWeight();
						weights[index].intersections += 1;
						weights[index].compare_markers(marker_cm+1, i, 0);	
					}
					else if (marker_c + marker_cm == size-1) {
						//both sides
						int index = OldConst.vec2Index(size, cur_pos - direction);
						int index_cm = OldConst.vec2Index(size, cur_pos_cm - direction_cm);

						if (!weights.ContainsKey(index))
							weights[index] = new PlayerAI.IndexWeight();
						if (!weights.ContainsKey(index_cm))
							weights[index_cm] = new PlayerAI.IndexWeight();

						weights[index].intersections += 1;
						weights[index].compare_markers(marker_c+1, i, 1);	
						weights[index_cm].intersections += 1;
						weights[index_cm].compare_markers(marker_cm+1, i, 0);	
					}
				}
			}
		}
		return weights;
	}

	public HashSet<int> aiGetOpenMarkers() {
		List<int> open = new List<int>();
		int max_boxes = (int)Math.Pow(size, 3) - 1;
		for (int i = 0; i < max_boxes; i++)
		{
			if (!markers_placed.ContainsKey(i))
				open.Add(i);
		}
		return open.ToHashSet();
	}

	//AI

	private int nextTurn() {
		ImmutableList<int> turns = turn_order.ToImmutableList();
		int cur_index = turns.FindIndex( (i) => i == cur_turn );
		if (cur_index == -1 || cur_index+1 == turns.Count) {
			return turns[0];
		}
		return turns[cur_index + 1];
	}

	private void setupTurn(int selected_box_id = -1, int marker = (int)OldConst.Marker.ROUND_GHOST) {
		outlineMarker(null);
		astral_projector.resetPlays();
		if (!players.Keys.ToImmutableList().Contains(cur_turn))
			cur_turn = players.Keys.ToImmutableList()[0];
		Player ply_now = players[cur_turn];
		Player ply_future = players[nextTurn()];
		cur_turn = nextTurn();
		if (game_t == OldConst.Gametype.LOCAL) { //local MP
			string append_cpu = "";
			if (!ply_future.isAI()) {
				ply_now.camera.Current = false;
				ply_future.camera.Current = true;
			} else 
				append_cpu = "(CPU)";
			setDisplayState( $"{ply_future.appearance.name}{append_cpu}({ply_future.userid})'s turn");
		} else if ( game_t == OldConst.Gametype.ONLINE )
			setDisplayState( !ply_future.IsMultiplayerAuthority() ? $"{ply_future.appearance.name}({ply_future.userid})'s turn" : "Your turn");
		ply_future.playSound("yourturn");
		turn_marker.Reparent(ply_future.pointBoard.GetNode<HBoxContainer>("VBoxContainer/HBoxContainer"));
		EmitSignal(SignalName.sigSetupTurn, selected_box_id, marker);
	}

	private void setupTurnOrder() {
		turn_order.Clear();
		List<int> turn_order_filter = players.Keys.ToList();
		OldConst.Marker cur_marker = players[players.Keys.ToArray()[0]].marker_t;
		while (turn_order.Count < players.Count) {
			for(int i = 0; i < turn_order_filter.Count; i++) {
				int uid = turn_order_filter[i];
				if (players[uid].marker_t == cur_marker && !turn_order.Contains(uid)) {
					turn_order.Add(uid);
					turn_order_filter.RemoveAt(i);
					cur_marker = OldConst.getEnemyMarker(cur_marker);
					break;
				}
			}
		}
	}

	private void setupBoard(bool init=false) {
		int i = 0;
		for (int x = 0; x < size; x++)
		{
			for (int y = 0; y < size; y++)
			{
				for (int z = 0; z < size; z++)
				{
					Box newbox = box_scn.Instantiate<Box>();
					newbox.Position = new Vector3(x,y,z) * BOX_DIST;
					newbox.Name = i.ToString();
					node_boxes.AddChild(newbox);
					i++;
				}			
			}			
		}
		// map bounds
		if (init) {
			StaticBody3D collider = GetNode<StaticBody3D>("boundary");
			Vector3 newsize = BOX_DIST*size*6;
			collider.Position = newsize / newsize.Length();
			collider.Scale = newsize;
		} else {
			markers_placed.Clear();
			Tween tw = rescaleBoxes(new Vector3(1f, 1f, 1f), 2, Tween.EaseType.Out);
			tw.Finished += () => { round_won = false; setupTurn(); };
			reposBoxes( new Vector3(0f,0f,0f), 2 ).Play();
			tw.Play();
		}
	}

	private void clearBoard() {
		effectsRestart();
		outlineMarker(null);
		setLastMarker(null);
		markers_placed.Clear();
		astral_projector.resetPlays();
		foreach(Player ply in players.Values) {
			ply.clearGhost();
		}
		Tween tw = rescaleBoxes(new Vector3(0.001f, 0.001f, 0.001f), 4);
		tw.Finished += () => {
			Godot.Collections.Array<Node> nodebox_children = node_boxes.GetChildren();
			for (int i = 0; i < nodebox_children.Count; i++)
			{
				node_boxes.RemoveChild(nodebox_children[i]);
				nodebox_children[i].QueueFree();
			}
			effectsCrush( node_boxes.GlobalPosition );
			setupBoard();
		};
		reposBoxes( new Vector3(size/2,size/2,size/2) * BOX_DIST, 4 ).Play();
		tw.Play();

	}

	private Player addPlayer(int uid = -1, OldConst.Marker marker = OldConst.Marker.ROUND_GHOST, bool isAi = false) {
			Player ply = player_scn.Instantiate<Player>();
			ply.userid = uid < 0 ? players.Count : uid;
			if (isAi) {
				PlayerAI ai = new PlayerAI();
				ply.AddChild(ai);
				ply.thunker = ai;
				ply.sigProcessAI += _sigProcessAI;
			}
			ply.sigConfirmSelectBox += _sigConfirmSelectBox;
			ply.sigGetPossibleMoves += _sigGetPossibleMoves;
			sigSetupTurn += ply._sigSetupTurn;
			if (marker ==  OldConst.Marker.ROUND_GHOST) {
				if (players.Count > 0) {
					ply.marker_t = OldConst.getEnemyMarker(players[ players.Keys.ToArray()[ players.Count-1 ] ].marker_t);
				} else
					ply.marker_t = OldConst.Marker.ROUND;
			} else 
				ply.marker_t = marker;
			ply.Name = ply.userid.ToString();
			ply.initGhost(astral_projector);
			players.Add(ply.userid, ply);
			AddChild(ply);
			//extra stuff
			if ((game_t == OldConst.Gametype.ONLINE && ply.IsMultiplayerAuthority()) || game_t == OldConst.Gametype.LOCAL && !isAi) {
				sigToggleMenu += ply._sigToggleMenu;
				menu.sigCloseMenu += ply._sigToggleMenu;
			}
			ply.RemoveChild(ply.pointBoard);
			GridContainer gridpoints = GetNode<GridContainer>("CounterGrid/GridContainer");
			gridpoints.AddChild(ply.pointBoard);
			gridpoints.Columns = players.Count;
			return ply;

	}

	private void removePlayer(int uid) {
		if (players.ContainsKey(uid)) {
			Player ply = players[uid];
			sigSetupTurn -= ply._sigSetupTurn;
			if ((game_t == OldConst.Gametype.ONLINE && ply.IsMultiplayerAuthority()) || game_t == OldConst.Gametype.LOCAL && !ply.isAI()) {
				sigToggleMenu -= ply._sigToggleMenu;
				menu.sigCloseMenu -= ply._sigToggleMenu;
			}
			ply.pointBoard.QueueFree();
			ply.QueueFree();
			players.Remove(uid);
			GetNode<GridContainer>("CounterGrid/GridContainer").Columns = players.Count;
		}
	}

	private void placeMarker(OldConst.Marker marker_t, int box_id) {
		GD.Print("Placed marker(", marker_t, ") at index ", box_id);
		markers_placed[ box_id ] = marker_t;
		AnimatedMarker marker = marker_scns[ (int)marker_t ].Instantiate<AnimatedMarker>();
		node_boxes.FindChild(box_id.ToString(), false, false).AddChild( marker );
		marker.Position = MARKER_POS;
		effectsSpawn( marker.GlobalPosition );
		marker.Spawn();
		setLastMarker(marker.GlobalPosition);
	}

	private void outlineMarker(Vector3? pos) {
		if (pos != null) {
			box_outline.Show();
			box_outline.Position = (Vector3)pos * BOX_DIST;
		} else 
			box_outline.Hide();
	}

	public void _sigProcessAI(int uid) {
		if (players.ContainsKey(uid) && cur_turn == uid) {
			Timer time = new Godot.Timer();
			time.Timeout += () => {processAI(players[uid]); time.QueueFree();};
			GetTree().Root.AddChild(time);
			time.Start(0.5);
		} 
	}
	public void _sigConfirmSelectBox(int userid, int selected_box_id) {
		GD.Print(cur_turn, " ", userid);
		if (!round_won && cur_turn == userid && selected_box_id > -1 && !markers_placed.ContainsKey(selected_box_id)) {
			if (game_t == OldConst.Gametype.LOCAL) {
				placeMarker( players[userid].marker_t, selected_box_id );
				if (checkDraw())
					doWin(-1);
				else if (checkWin(selected_box_id))
					doWin(userid);	
				else
					setupTurn(selected_box_id, (int)players[userid].marker_t);
			} else if (game_t == OldConst.Gametype.ONLINE) {
				Rpc( "rpcPlaceMarker",  (int)players[userid].marker_t, selected_box_id, userid);
			}
		} else
			players[userid].playSound("failconfirm");

	}

	public void _sigGetPossibleMoves(int userid, int selected_box_id) {
		outlineMarker(null);
		if (!round_won && selected_box_id > -1) { //&& !markers_placed.ContainsKey(selected_box_id)) {
			Player ply = players[userid];
			outlineMarker( OldConst.index2Vec(size, selected_box_id) );
			ImmutableArray<Player.MoveList>? moves = null;
			if (ply.possible_marker != OldConst.Marker.ROUND_GHOST && !(markers_placed.ContainsKey(selected_box_id) && markers_placed[selected_box_id] != ply.possible_marker)) {
				//create new moves or get already existing
				moves = !astral_projector.possible_plays[ply.possible_marker].ContainsKey(selected_box_id) ? getPossibleMoves(selected_box_id, ply.possible_marker) : astral_projector.possible_plays[ply.possible_marker][selected_box_id];
			} 
			GD.Print(moves);
			ply.setPossiblePlay(selected_box_id, moves);
		}
	}

	public void quitGame() {
		if (game_t == OldConst.Gametype.ONLINE) {
			discAndQuitToMenu();
		}
		else if (game_t == OldConst.Gametype.LOCAL) {
			discAndQuitToMenu(false);
		}
	}

	[Signal]
	public delegate void sigSetupTurnEventHandler(int selected_box_id, int marker);

	[Signal]
	public delegate void sigToggleMenuEventHandler(bool hidden);


	//ONLINE LOGIC//
	private ENetMultiplayerPeer peer;

	public static int maxClients = 2;

	public static int minBots = 1;
	public static bool isHost = false;
	public static string IP = "127.0.0.1";
	public static int PORT = 9001;

	public static int upnp_mode = 0;

	private UpnpHelper upnp_helper;

	bool lobby_loaded = false;
	private Godot.Collections.Dictionary<int, string> appearance_queue;

	private async void addPeer(int id) {
		if (!round_won || lobby_loaded || players.Count > maxClients - 1) {
			peer.DisconnectPeer(id);
			return;
		}
		await ToSignal( GetTree().CreateTimer(1), SceneTreeTimer.SignalName.Timeout);
		string conn_str = $"({players.Count+1}/{maxClients})";
		setDisplayState($"{id} connected to {IP}:{PORT} " + conn_str);
		Rpc("rpcSendDisplay", "Waiting for players " + conn_str);
		addPlayer(id);
		GD.Print(this);
		if (players.Count > maxClients - 1) {
			var ply_cereal_ids = new Godot.Collections.Array<int>();
			var ply_cereal_markers = new Godot.Collections.Array<int>();
			players.Values.ToList().ForEach( (p) => {ply_cereal_ids.Add( p.userid ); ply_cereal_markers.Add((int)p.marker_t); } );
			Rpc("rpcSetupGame", ply_cereal_ids, ply_cereal_markers, size);
		}

	}

	private void discAndQuitToMenu(bool closepeer = true) {
		if (closepeer)
			peer.Close();
		Input.MouseMode = Input.MouseModeEnum.Visible;
		GetTree().ChangeSceneToPacked(GD.Load<PackedScene>("res://scenes/mainmenu.tscn"));
	}

	private async void removePeer(int id) {
		if (players.Count < 1 || !players.ContainsKey(id)) {
			if (GetMultiplayerAuthority() == id)
				discAndQuitToMenu(false);
			return;
		}

		if (!lobby_loaded) {
			if (Multiplayer.IsServer()) {
				removePlayer(id);
				string conn_str = $"({players.Count}/{maxClients})";
				setDisplayState($"{id} disconnected " + conn_str);
				Rpc("rpcSendDisplay", "Waiting for players " + conn_str);
			}
			if (id == 1)
				discAndQuitToMenu();
		} else {
			if (id == 1)
				discAndQuitToMenu();
			else {
				round_won = true;
				removePlayer(id);
				if(Multiplayer.IsServer()) {
					if (players.Count < 2) {
						discAndQuitToMenu();
					}
					OldConst.Marker cur_mark = OldConst.Marker.ROUND;
					Godot.Collections.Dictionary<int, int> markers_rpc = new Godot.Collections.Dictionary<int, int>();
					foreach (Player ply in players.Values)
					{
						ply.marker_t = cur_mark;
						markers_rpc.Add(ply.userid, (int)cur_mark);
						cur_mark = OldConst.getEnemyMarker(cur_mark);	
					}
					Rpc("rpcRestartAfterDisconnect", markers_rpc);
					await ToSignal( GetTree().CreateTimer(2), SceneTreeTimer.SignalName.Timeout);
					rpcRestartAfterDisconnect(markers_rpc);
				}
			}
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void rpcSetupGame(Godot.Collections.Array<int> serialized_ids, Godot.Collections.Array<int> serialized_markers , int _size) {

		size = _size;
		lobby_loaded = true;
		setupBoard(true);
		if (!Multiplayer.IsServer()) {
			for (int i = 0; i < serialized_ids.Count; i++)
			{
				addPlayer( serialized_ids[i], (OldConst.Marker)serialized_markers[i] );
			}
			RpcId(1, "rpcQueuePlyCosmetic", players.First( (kv) => kv.Value.isCurrentPlayer() ).Key, PlayerAppearance.toPackedString(OldConst.loadProfile(Player.saveSlot).appearance) );
		} else {
			setupTurnOrder();
			Rpc("rpcSetupTurnOrder", turn_order.ToArray());
		}
		//Player.loadPlayerCosmetics(players.Values.ToImmutableArray());
		//setupTurnOrder();
		//setupTurn();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void rpcQueuePlyCosmetic(int id, string appearance_str) {
		if (appearance_queue.Count < maxClients && players.ContainsKey(id)) {
			appearance_queue.Add(id, appearance_str);
		}
		if (appearance_queue.Count >= maxClients) {
			rpcLoadPlyCosmetic(appearance_queue);
			Rpc("rpcLoadPlyCosmetic", appearance_queue);
		}
	}
	[Rpc(MultiplayerApi.RpcMode.Authority)]
	private void rpcLoadPlyCosmetic(Godot.Collections.Dictionary<int,string> apps) {
		foreach( KeyValuePair<int, string> app in apps ) {
			players[app.Key].appearance = PlayerAppearance.fromPackedString( app.Value );
		}
		Player.loadPlayerCosmetics(players.Values.ToImmutableArray());
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, TransferChannel = 1)]
	private void rpcSendDisplay(string str) {
		setDisplayState(str);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void rpcPlaceMarker(int marker, int box_id, int uid) {
		placeMarker( (OldConst.Marker) marker, box_id );
		if (Multiplayer.IsServer()) {
			if (checkDraw())
				Rpc( "doRpcWin", -1 );
			else if (checkWin(box_id))
				Rpc( "doRpcWin", uid );
			else
				Rpc("rpcSetupTurn", box_id, marker);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void rpcSetupTurn(int box_id, int marker) {
		setupTurn(box_id, marker);
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
	private void doRpcWin(int uid) {
		doWin(uid);
	}

	[Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
	private void rpcSetupTurnOrder(int[] turns) {
		turn_order = turns.ToList();
		setupTurn();
		round_won = false;
		fadeLoadingScreen();
	}

	[Rpc(MultiplayerApi.RpcMode.Authority)]
	private void rpcRestartAfterDisconnect(Godot.Collections.Dictionary<int, int> markers) {
		foreach (int uid in markers.Keys) {
			OldConst.Marker cur_mark = (OldConst.Marker)markers[uid];
			Player ply = players[uid];
			ply.marker_t = cur_mark;
			((TextureRect)ply.pointBoard.FindChild("Marker", true, false)).Texture = ResourceLoader.Load<Texture2D>( "res://resources/ui/marker_" + (cur_mark == OldConst.Marker.ROUND ? 'o' : 'x') + ".png" );	
		}		
		if (Multiplayer.IsServer()) {
			setupTurnOrder();
			Rpc("rpcSetupTurnOrder", turn_order.ToArray());
		}
	}


	[Rpc(MultiplayerApi.RpcMode.AnyPeer)]
	private void clientQuitGame(int uid) {
		peer.DisconnectPeer(uid);
	}

	//ONLINE LOGIC//

	//GUI
		private void setDisplayState(in string state) {
			state_display.Text = state;
			Label flash = state_display.GetNode<Label>("Flash");
			flash.Text = state;
			flash.SelfModulate = new Color(1,1,1,1);
			Tween tw = flash.CreateTween();
			tw.TweenProperty(flash, "self_modulate:a", 0, 0.25);
			tw.Play();
		}

	//GUI

	//EFFECTS

		private void setLastMarker(in Vector3? pos) {
			if (pos == null)
				marker_last.Hide();
			else {
				marker_last.Show();
				Tween tw = marker_last.CreateTween();
				tw.TweenProperty(marker_last, "position", (Vector3)pos, 0.2);
				tw.Play();
			}
		}

		private void effectsRestart() {
			Tween tw = magicNShiet(true);
			circle.Position = new Vector3(0,3.5f,0) * (size-2);
			circle.Scale = new Vector3(19,19,19) * (size-2);
			particle_restart.ProcessMaterial.Set("scale_max", size-2);
			particle_restart.ProcessMaterial.Set("scale_min", size-2);
			particle_restart.ProcessMaterial.Set("initial_velocity_min", size);
			particle_restart.ProcessMaterial.Set("initial_velocity_max", size);
			particle_restart.Position = new Vector3(size/2, size, size/2) * BOX_DIST + new Vector3(2, -4, -2);
			tw.Finished += () => particle_restart.Restart();
			tw.Play();
			playSound("restart", circle.GlobalPosition, 32);
		}

		private Tween magicNShiet(bool reverse=false) {
			Tween tw = circle.CreateTween();
			int col = reverse ? 1 : 0;
			tw.TweenProperty(circle.MaterialOverride, "albedo_color:a" , col, 1.2);
			tw.TweenProperty(particle_restart.GetNode<DirectionalLight3D>("GlazedLight"), "light_energy", reverse ? 2 : 0.2, 0.4);
			return tw;
		}

		private void effectsSpawn(in Vector3 pos) {
			particle_spawn.Position = pos;
			particle_spawn.Restart();
			playSound("spawn", pos, 20);
		}

		private void effectsCrush(in Vector3 pos) {
			particle_spawn.Position = pos;
			particle_spawn.Restart();
			playSound("crush", pos, 200);
		}

		private Tween rescaleBoxes(in Vector3 scale, in float time, Tween.EaseType tw_ease = Tween.EaseType.In) {
			Tween tw = node_boxes.CreateTween();
			tw.TweenProperty(node_boxes, "scale", scale, time);
			tw.SetEase(tw_ease);
			tw.SetTrans(Tween.TransitionType.Quart);
			return tw;
		}

		private Tween reposBoxes(in Vector3 pos, in float time) {
			Tween tw = node_boxes.CreateTween();
			tw.TweenProperty(node_boxes, "position", pos, time);
			return tw;
		}

		private void playSound(in string name, in Vector3 loc, in float dist) {
			local_sound.Stream = OldConst.getOggStream(name);
			local_sound.Position = loc;
			local_sound.UnitSize = dist;
			local_sound.Play();
		}

		private void fadeLoadingScreen() {
			TextureRect texrect = GetNodeOrNull<TextureRect>("Vortex");
			if (texrect != null) {
				Tween tw = texrect.CreateTween();
				tw.TweenProperty(texrect, "self_modulate:a", 0, 0.8);
				tw.Finished += () => texrect.QueueFree();
				tw.Play();
				turn_marker.Visible = true;
				menu.loadan(); 
			}
		}

	//EFFECTS


	public override void _Ready()
	{
		node_boxes = GetNode<Node3D>("boxes");
		state_display = GetNode<Label>("StateDisplay");
		particle_restart = GetNode<GpuParticles3D>("ParticleRestart");
		circle = GetNode<CsgMesh3D>("ParticleRestart/Circle");
		particle_spawn = GetNode<GpuParticles3D>("ParticleSpawn");
		box_outline = GetNode<MeshInstance3D>("MeshOutline");
		local_sound = GetNode<AudioStreamPlayer3D>("Noisescape");
		astral_projector = GetNode<AstralProjector>("AstralProjector");
		marker_last = GetNode<Sprite3D>("LastMarker");
		turn_marker = GetNode<TextureRect>("TurnMarker");
		//ambient_sound = GetNode<AudioStreamPlayer>("Soundscape");
		menu = GD.Load<PackedScene>("res://scenes/objects/game_menu.tscn").Instantiate<GameMenu>();
		menu.sigQuitGameButton += quitGame;
		AddChild(menu);
		astral_projector.initGhost(size, OldConst.Marker.ROUND, marker_scns[(int)OldConst.Marker.ROUND_GHOST].Instantiate<Node3D>());
		astral_projector.initGhost(size, OldConst.Marker.CROSS, marker_scns[(int)OldConst.Marker.CROSS_GHOST].Instantiate<Node3D>());
		particle_restart.Finished += () => magicNShiet(false).Play();
		if (game_t == OldConst.Gametype.ONLINE) {
			round_won = true; // disable markers before everyone joins
			peer = new ENetMultiplayerPeer();
			if (isHost) {
				
				appearance_queue = new Godot.Collections.Dictionary<int, string>
				{
					{ 1, PlayerAppearance.toPackedString(OldConst.loadProfile(Player.saveSlot).appearance) }
				};
				if (upnp_mode > 0) {
					setDisplayState("Discovering UPNP devices...");
					upnp_helper = GD.Load<PackedScene>("res://scenes/upnp_helper.tscn").Instantiate<UpnpHelper>();
					upnp_helper.sigUpnpComplete += (code) => {
						try {
							string ip = upnp_helper.getExternalIP();
							if (code == Upnp.UpnpResult.Success.ToString() && ip.Length > 1) {
								IP = ip;
								createServer();
							}
							else {
								setDisplayState("Failed to create mapping");
								discAndQuitToMenu();
							}
						}
						catch {
							discAndQuitToMenu();
						}
					}; 
					upnp_helper.sigUpnpDeleteMapping += (code) => discAndQuitToMenu();
					upnp_helper.sigUpnpUpdateIP += (ip) => {
						if (ip.Length <= 0)
							discAndQuitToMenu();	
					};
					upnp_helper.setup();
				} else 
					createServer();

			} else {
				peer.CreateClient(IP, PORT);
				Multiplayer.MultiplayerPeer = peer;
				Name = GetMultiplayerAuthority().ToString();
				Multiplayer.PeerDisconnected += new MultiplayerApi.PeerDisconnectedEventHandler( (id) => removePeer((int)id) );
			}
			//client = Multiplayer.
		} else {
			setupBoard(true);
			var appearances = OldConst.createRandomAppearance(OldConst.loadProfile(Player.saveSlot), maxClients-1, minBots);
			for (int i = 0; i < maxClients; i++)
			{
				Player ply = addPlayer(-1, OldConst.Marker.ROUND_GHOST, i > maxClients - minBots - 1);	
				ply.appearance = appearances[i];
			}
			Player.loadPlayerCosmetics(players.Values.ToImmutableArray());
			setupTurnOrder();
			setupTurn();
			node_boxes = GetNode<Node3D>("boxes");
			fadeLoadingScreen();
		}
	}

	private void createServer() {
				try {
					peer.SetBindIP(IP);	
					peer.CreateServer(PORT, maxClients);
				} catch {
					discAndQuitToMenu();
				}
				if (peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Disconnected) {
					setDisplayState("Failed to create server");
					discAndQuitToMenu();
				}
				setDisplayState($"Server listening on {IP}:{PORT}");
				Multiplayer.MultiplayerPeer = peer;
				GD.Print("hosting server");
				Name = $"{1}";
				addPeer(1);
				Multiplayer.PeerConnected += new MultiplayerApi.PeerConnectedEventHandler( (id) => addPeer((int)id) );
				Multiplayer.PeerDisconnected += new MultiplayerApi.PeerDisconnectedEventHandler( (id) => removePeer((int)id) );
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		circle.RotateY(0.01f);
		if ( Input.IsActionJustReleased("quit") ) {
			menu.toggle();
			EmitSignal(SignalName.sigToggleMenu, !menu.Visible);
		}
	}

	
	
}
