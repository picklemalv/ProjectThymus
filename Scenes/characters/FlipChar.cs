using Godot;
using System;

public partial class FlipChar : CharacterBody2D
{
	private AnimatedSprite2D sprite;

	public override void _Ready()
	{
		sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		sprite.FlipH = true;
		
	}
}
