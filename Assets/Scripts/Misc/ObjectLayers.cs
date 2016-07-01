//
// By using or accessing the source codes or any other information of the Game SHADOWGUN: DeadZone ("Game"),
// you ("You" or "Licensee") agree to be bound by all the terms and conditions of SHADOWGUN: DeadZone Public
// License Agreement (the "PLA") starting the day you access the "Game" under the Terms of the "PLA".
//
// You can review the most current version of the "PLA" at any time at: http://madfingergames.com/pla/deadzone
//
// If you don't agree to all the terms and conditions of the "PLA", you shouldn't, and aren't permitted
// to use or access the source codes or any other information of the "Game" supplied by MADFINGER Games, a.s.
//

//#####################################################################################################################

using UnityEngine;

//#####################################################################################################################

static class ObjectLayer
{
	public readonly static int Default;
	public readonly static int IgnoreRayCast;
	public readonly static int Ragdoll;
	public readonly static int PhysicsDefault;
	public readonly static int PhysicBody;
	public readonly static int PhysicsMetal;
	public readonly static int PhysicsWater;
	public readonly static int Hat;

	static ObjectLayer()
	{
		Default = LayerMask.NameToLayer("Default");
		IgnoreRayCast = LayerMask.NameToLayer("Ignore Raycast");
		Ragdoll = LayerMask.NameToLayer("Ragdoll");
		PhysicsDefault = LayerMask.NameToLayer("PhysicsDefault");
		PhysicBody = LayerMask.NameToLayer("PhysicBody"); // e.g. character-controllers
		PhysicsMetal = LayerMask.NameToLayer("PhysicsMetal");
		PhysicsWater = LayerMask.NameToLayer("PhysicsWater");
		Hat = LayerMask.NameToLayer("Hat");
	}
}

static class ObjectLayerMask
{
	public readonly static int Default;
	public readonly static int IgnoreRayCast;
	public readonly static int Ragdoll;
	public readonly static int PhysicsDefault;
	public readonly static int PhysicBody;
	public readonly static int PhysicsMetal;
	public readonly static int PhysicsWater;
	public readonly static int Hat;

	static ObjectLayerMask()
	{
		Default = 1 << ObjectLayer.Default;
		IgnoreRayCast = 1 << ObjectLayer.IgnoreRayCast;
		Ragdoll = 1 << ObjectLayer.Ragdoll;
		PhysicsDefault = 1 << ObjectLayer.PhysicsDefault;
		PhysicBody = 1 << ObjectLayer.PhysicBody;
		PhysicsMetal = 1 << ObjectLayer.PhysicsMetal;
		PhysicsWater = 1 << ObjectLayer.PhysicsWater;
		Hat = 1 << ObjectLayer.Hat;
	}
}

//#####################################################################################################################
