using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Xml.Resolvers;

public partial class AnimatedMarker : Node3D
{
	private AnimationPlayer animplayer;

	public void Spawn() {
		animplayer.Queue("spawn");
		animplayer.Queue("idle");

	}
	public override void _Ready()
	{
		animplayer = GetNode<AnimationPlayer>("AnimationPlayer");
	}

}
