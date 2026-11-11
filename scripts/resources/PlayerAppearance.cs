using Godot;
using System;

public partial class PlayerAppearance : Resource
{
    public static string SKIN_PATH = "res://resources/3d/materials/skin/";
    public static string BADGE_PATH = "res://resources/2d/badge/";

    [Export]
    public int skin {get;set;} = 0;
    [Export]
    public int badge {get;set;} = 0;    
    [Export]
    public string name {get;set;} = "Player";

    public static PlayerAppearance fromPackedString(string plyapp) {
        string[] plapplap = plyapp.Split('\n');
        return new PlayerAppearance {skin=int.Parse(plapplap[0]), badge=int.Parse(plapplap[1]), name = plapplap[2]};
    }

    public static string toPackedString(in PlayerAppearance plyapp) {
        return $"{plyapp.skin}\n{plyapp.badge}\n{plyapp.name}";
    }

}
