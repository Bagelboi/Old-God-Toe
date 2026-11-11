using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerData : Resource
{
    [Export]
    public Vector2[] wins_cpu {get;set;} = {new Vector2(3,0)};
    [Export]
    public Vector2[] wins_online {get;set;} = {new Vector2(3,0)};
    [Export]
    public PlayerAppearance appearance {get;set;} = new PlayerAppearance();

}
