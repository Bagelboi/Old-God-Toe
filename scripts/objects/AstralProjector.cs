using Godot;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using GdCol = Godot.Collections;

public partial class AstralProjector : Node3D
{
	private Dictionary<OldConst.Marker, Node3D> marker_p =  new Dictionary<OldConst.Marker, Node3D>();
	public Dictionary<OldConst.Marker, Dictionary<int, ImmutableArray<Player.MoveList>>> possible_plays = new Dictionary<OldConst.Marker, Dictionary<int, ImmutableArray<Player.MoveList>>>();

	private Label play_i;

	public void initGhost(in int board_size, OldConst.Marker marker_t, in Node3D ghost_marker) {
		AddChild(ghost_marker);
		possible_plays[marker_t] = new Dictionary<int, ImmutableArray<Player.MoveList>>();
		marker_p[marker_t] = ghost_marker;
		MeshInstance3D mesh = ghost_marker.GetChild<MeshInstance3D>(0);
		for (int i = 0; i < board_size-1; i++)
		{
		 	ghost_marker.AddChild( mesh.Duplicate() );
		}
 
		//setGhostVisible(false);
	}
	public void setGhostVisible(in bool vis, OldConst.Marker marker_t) {
		Node3D astral_projection = marker_t != OldConst.Marker.ROUND_GHOST ? marker_p[marker_t] : null;

		foreach (Node3D proj in marker_p.Values) {
			if (proj != null) {
				if (astral_projection != null && astral_projection == proj && vis)
					proj.Show();
				else
					proj.Hide();
			}
		}
	}
	
	
	public void setGhostPos(in Vector3 pos, OldConst.Marker marker_t) {
			Node3D astral_projection = marker_p[marker_t];
			if (astral_projection != null) {
				astral_projection.Position = pos;
			}
	}

	public void moveListToGhost(in Player.MoveList move_l, OldConst.Marker marker_t) {
			Node3D astral_projection = marker_p[marker_t];
			if (astral_projection != null) {
				Vector3 cur_cardinal = OldConst.cardinals[ move_l.cardinal ];
				int m_i = 0;
				GdCol.Array<Node> meshes = astral_projection.GetChildren();
				List<Vector3> squares = new List<Vector3> { new Vector3(0,0,0) };
				int board_size = Gameboard.size;
				int i_fwd = 0;
				int i_bck = 0;
				switch(move_l.move_t) {
					case 0:
						i_fwd = board_size-1;
						i_bck = 0;
						break;
					case 1:
						i_fwd = (board_size-1)/2;
						i_bck = i_fwd;
						break;
					case 2:
						i_fwd = 0;
						i_bck = board_size-1;
						break;
				}
				for (int i = 0; i < i_fwd; i++)
				{
					( (MeshInstance3D)meshes[m_i] ).Position = cur_cardinal * Gameboard.BOX_DIST * (i+1);
					m_i++;
				}
				for (int i = 0; i < i_bck; i++)
				{
					( (MeshInstance3D)meshes[m_i] ).Position = cur_cardinal * -1 * Gameboard.BOX_DIST * (i+1);
					m_i++;
				}
			}
	}

	public void clearGhost() {
		setGhostVisible(false, OldConst.Marker.ROUND_GHOST);
	}

	public void resetPlays() {
		foreach (OldConst.Marker move_k in possible_plays.Keys)
			possible_plays[move_k].Clear();
		clearGhost();
	}

	public void setPossiblePlay(in int box_id, in Player.MoveList ml, OldConst.Marker marker_t) {
		moveListToGhost( ml, marker_t );
		setGhostVisible(true, marker_t);
	}

	public void setPlayIndex(int index, OldConst.Marker marker, int box_id, bool badtouch = false) {
		string text = "N/A";
		if (box_id > -1 && marker != OldConst.Marker.ROUND_GHOST && possible_plays[marker].ContainsKey(box_id) && possible_plays[marker][box_id] != null) {
			text = $"Play({index+1}/{possible_plays[marker][box_id].Length})";
		}
		if (!badtouch)
			play_i.Modulate = new Color(1,1,1,1);
		else
			play_i.Modulate = new Color(1,0.4f,0.4f,1);
		play_i.Text = text;
	}

	public override void _Ready()
	{
		play_i = GetNode<Label>("PlayIndex");
	}


}
