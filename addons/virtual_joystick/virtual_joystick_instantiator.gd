@tool
extends Control

var scene

func _enter_tree():
	scene = preload("res://addons/virtual_joystick/virtual_joystick_scene.tscn").instantiate()
	add_child(scene)

func _exit_tree():
	scene.free()
