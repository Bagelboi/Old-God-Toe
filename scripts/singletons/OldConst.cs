using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using Vector3 = Godot.Vector3;

public partial class OldConst : Node
{
	public enum Marker {ROUND, CROSS, ROUND_GHOST, CROSS_GHOST};
	public enum Gametype {LOCAL, ONLINE}

	public const string PASS = "test";
	public static Vector3 index2Vec(in double size, in double id) {
		return new Vector3((float)Math.Floor(id / (size*size)), (float)Math.Floor(id % (size * size) / size), (float)(id % size));
	}

	public static int vec2Index(in double size, in Vector3 pos) {
		return (int)(pos.X * (size * size) + pos.Y * size + pos.Z);
	}

	public static Marker getEnemyMarker(Marker marker_t) {
		switch(marker_t) {
			case Marker.ROUND:
				return Marker.CROSS;
			case Marker.CROSS:
				return Marker.ROUND;
		}
		return Marker.ROUND_GHOST;
	}

	public static Color getMarkerColor(Marker marker_t) {
		switch(marker_t) {
			case Marker.ROUND:
				return new Color(0.67f, 0.89f, 1, 1 );
			case Marker.CROSS:
				return new Color(0.67f, 1, 0.67f, 1);
		}
		return new Color(1,1,1,1);
	}

	public static Vector3[] cardinals = {
			new Vector3(1, 0, 0),   // right
			new Vector3(0, 1, 0),   // up
			new Vector3(0, 0, 1),   // forward
			new Vector3(1, 1, 0),   // up_right
			new Vector3(-1, 1, 0),  // up_left
			new Vector3(1, 0, 1),   // right_forward
			new Vector3(-1, 0, 1), // left_forward
			new Vector3(0, 1, 1), //forward up
			new Vector3(0, -1, 1), //forward down
			new Vector3(1, -1, 1), // left down forward
			new Vector3(-1, -1, 1), // right down forward 
			new Vector3(1, 1, 1), // left up forward
			new Vector3(-1, 1, 1), // right up forward
		};

	private static string AUDIO_PATH = "res://resources/audio/";
	public static string STATIC_PATH = "res://resources/static/";
	public static AudioStreamOggVorbis getOggStream(in string name) {
		return AudioStreamOggVorbis.LoadFromFile(AUDIO_PATH + name + ".ogg");
	}

	public static void saveProfile(int slot, PlayerData data, bool makedefault = true) {
		ConfigFile conf = new ConfigFile();
		conf.LoadEncryptedPass( "user://data.sav", PASS );
		if (makedefault)
			conf.SetValue("default", "slot", slot);
		conf.SetValue("data", slot.ToString(), data);
		conf.SaveEncryptedPass("user://data.sav", PASS);
	}

	public static PlayerData loadProfile(int slot) {
		ConfigFile conf = new ConfigFile();
		if ( conf.LoadEncryptedPass( "user://data.sav", PASS ) == Error.Ok )
			return (PlayerData) conf.GetValue("data", slot.ToString(), new PlayerData());
		return new PlayerData();
	}

	public static List<PlayerAppearance> createRandomAppearance(in PlayerData data, int amount, int amount_bot) {
		List<PlayerAppearance> playerAppearances = new List<PlayerAppearance>();
		amount = Math.Max(0, amount - amount_bot);
		string[] names = {"Alejandro","Hiro","Dimitri","Raj","Sven","Marco","Kofi","Enzo","Leif","Tao","Amara","Yumi","Natasha","Priya","Freya","Carmen","Zara","Anika","Ingrid","Mei"};
		int skin_amount;
		using ( var skin_csv = FileAccess.Open(STATIC_PATH + "skin.txt", FileAccess.ModeFlags.Read) ) {
			skin_amount = skin_csv.GetAsText().Split('\n').Length;
			skin_csv.Close();
		}
		GD.Print(amount, "-", amount_bot);
		for (int i = 0; i < amount+1; i++)
			playerAppearances.Add(data.appearance);
		for (int i = 0; i < amount_bot; i++) {
			playerAppearances.Add( new PlayerAppearance {
				name = names[new Random().Next(0, names.Length-1)],
				skin = new Random().Next(0, skin_amount),
				badge = new Random().Next(0, data.appearance.badge)
			});
		}
		return playerAppearances;
	}

	public static void popTutorial(in SceneTree tree, string tutfile) {
		Node tut = tree.Root.FindChild("Tutorial", false, false);
		if (tut != null)
			tut.QueueFree();
		Tutorial ni = GD.Load<PackedScene>("res://scenes/tutorial.tscn").Instantiate<Tutorial>();
		tree.Root.AddChild(ni);
		ni.loadSlides(tutfile);
	}
}
