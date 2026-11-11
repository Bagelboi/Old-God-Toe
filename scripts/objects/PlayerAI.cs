using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

public partial class PlayerAI : Node
{

	public class IndexWeight {

		public class Comparer : IComparer<IndexWeight> {
			int IComparer<IndexWeight>.Compare(IndexWeight _a, IndexWeight _b) {
					if (_b.least_markers_to_win < _a.least_markers_to_win || _b.intersections > _a.intersections)
						return -1;
					else if (_b.least_markers_to_win > _a.least_markers_to_win || _b.intersections < _a.intersections)
						return 1;
					return 0;
			  }
		}
		public int intersections = 0;
		public int least_markers_to_win = 999;

		public int least_cardinal = -1;

		public int least_move_t = -1;

		public void compare_markers(int amount, int cardinal, int move_t) {
			if (amount < least_markers_to_win) {
			 least_cardinal = cardinal;
			 least_markers_to_win = amount;
			 least_move_t = move_t;
			}
		}

		public override string ToString() {
						int dir = least_move_t < 1 ? 1 : -1;
			String card_str = least_cardinal > -1 ? (OldConst.cardinals[least_cardinal]*dir).ToString() : "none";

			return $"Inter:{intersections} | Markers:{least_markers_to_win} | Cardinal:{card_str}";
		}

	}

	private struct Intent {
		public int cardinal_t;
		public int move_t;

		public int index; 
	}

	private int last_marker_placed = -1;

	float intercept_chance;
	float normal_chance;

	public PlayerAI(float _normal_chance = 0.9f, float _intercept_chance = 0.1f) {
		normal_chance = _normal_chance;
		intercept_chance = _intercept_chance;
	}

	private bool diceThrow(float perc) {
		return new Random(DateTime.Now.Millisecond).NextSingle() < perc;
	}

	public int figureItOut(in HashSet<int> openMarkers, 
		in Dictionary<int, IndexWeight> my_weights, 
		in Dictionary<int, IndexWeight> enemy_weights) {
		
		int new_pick = -1;
		if (openMarkers.Count > 1) {
			if (diceThrow(intercept_chance) && enemy_weights.Count > 0)
				new_pick = placeBest(enemy_weights, openMarkers);
			else if (diceThrow(normal_chance) && my_weights.Count > 0)
				new_pick = placeBest(my_weights, openMarkers);
		}

		if (new_pick < 0)
			new_pick = placeRandom(openMarkers);
		return new_pick;
	} 

	private int placeBest(Dictionary<int, IndexWeight> _weights, in HashSet<int> openMarkers) {
		IndexWeight.Comparer comparer = new IndexWeight.Comparer();
		Dictionary<int, IndexWeight> weights = _weights.OrderByDescending(w => w.Value, comparer).ToDictionary(w => w.Key, w => w.Value);
		foreach (KeyValuePair<int, IndexWeight> weight in weights)
		{
			IndexWeight val = weight.Value;
			if (val.least_cardinal > -1) {
				Vector3 dir = processCardinal(val.least_move_t, OldConst.cardinals[val.least_cardinal]);
				GD.Print(dir);
				Vector3 pos = OldConst.index2Vec(Gameboard.size, weight.Key) + dir;
				while (Gameboard.posInBounds(pos)) {
					int new_index = OldConst.vec2Index(Gameboard.size, pos);
					if (openMarkers.Contains(new_index))
						return new_index;
					pos += dir;
				}
			}
		}
		return -1;
	}

	private int placeRandom(in HashSet<int> openMarkers) {
		ImmutableList<int> m_list = openMarkers.ToImmutableList();
		return m_list.ItemRef( new Random(DateTime.Now.Millisecond).Next(0, m_list.Count-1) );
	}

	private static Vector3 processCardinal(in int move_t, in Vector3 cardinal) {
		switch (move_t)
		{
			case 1:
				return cardinal * new Vector3(-1,-1,-1);
		}
		return cardinal;
	}

}
