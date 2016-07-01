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

/***************************************************************
 * Class Name : WorldState
 * Function   : Represents the world state to the GOAP controller in the game
 * Created by : Marek R.
 *
 **************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum E_PropType
{
	Invalid = -1,
	Bool,
	Int,
	Float,
	Vector,
	Agent,
	CoverState,
}

public enum E_PropKey
{
	Start = 0,
	Idling = Start,
	AtTargetPos,
	TargetNode,
	InDodge, //set true when goal is 
	CoverState,
	KillTarget,
	WeaponLoaded,
	WeaponChange,
	UseWorldObject,
	UseGadget,
	PlayAnim,
	Count
}

public enum RETURN_TYPES
{
	INVALID = -1,
	FALSE_RETURN,
	TRUE_RETURN
}

[System.Serializable]
public class WorldStateProp
{
	public E_PropKey PropKey; // { get {return PropKey;} set {PropKey = value;} }

	public string PropName
	{
		get { return PropKey.ToString(); }
	}

	public E_PropType PropType;
	public float Time;

	public bool Bool;
	public int Int;
	public float Float;
	public Vector3 Vector;
	public AgentHuman Agent;
	public E_CoverState CoverState;

	public WorldStateProp(bool state)
	{
		Bool = state;
		PropType = E_PropType.Bool;
	}

	public WorldStateProp(int state)
	{
		Int = state;
		PropType = E_PropType.Int;
	}

	public WorldStateProp(float state)
	{
		Float = state;
		PropType = E_PropType.Float;
	}

	public WorldStateProp(AgentHuman state)
	{
		Agent = state;
		PropType = E_PropType.Agent;
	}

	public WorldStateProp(UnityEngine.Vector3 vector)
	{
		Vector = vector;
		PropType = E_PropType.Vector;
	}

	public WorldStateProp(E_CoverState state)
	{
		CoverState = state;
		PropType = E_PropType.CoverState;
	}

	//public static implicit operator WorldStateProp(bool state) { return new WorldStateProp(state);}

	public bool GetBool()
	{
		return Bool;
	}

	public int GetInt()
	{
		return Int;
	}

	public float GetFloat()
	{
		return Float;
	}

	public UnityEngine.Vector3 GetVector()
	{
		return Vector;
	}

	public AgentHuman GetAgent()
	{
		return Agent;
	}

	public E_CoverState GetCoverState()
	{
		return CoverState;
	}

	public override bool Equals(System.Object o)
	{
		WorldStateProp otherProp = o as WorldStateProp;
		if (otherProp != null)
		{
			if (this.PropType != otherProp.PropType)
				return false; // different typs of values

			switch (this.PropType)
			{
			case E_PropType.Bool:
				return Bool == otherProp.Bool;
			case E_PropType.Int:
				return Int == otherProp.Int;
			case E_PropType.Float:
				return Float == otherProp.Float;
			case E_PropType.Vector:
				return Vector == otherProp.Vector;
			case E_PropType.Agent:
				return Agent == otherProp.Agent;
			case E_PropType.CoverState:
				return CoverState == otherProp.CoverState;
			default:
				return false;
			}
		}

		return false;
	}

	public override int GetHashCode()
	{
		return (this as object).GetHashCode();
	}

	public static bool operator ==(WorldStateProp prop, WorldStateProp other)
	{
		if ((prop as object) == null)
			return (other as object) == null;

		return prop.Equals(other as object);
	}

	public static bool operator !=(WorldStateProp prop, WorldStateProp other)
	{
		return !(prop == other);
	}

	public override string ToString()
	{
		String s = PropName + ": ";

		switch (this.PropType)
		{
		case E_PropType.Bool:
			s += Bool;
			break;
		case E_PropType.Int:
			s += Int;
			break;
		case E_PropType.Float:
			s += Float;
			break;
		case E_PropType.Vector:
			s += Vector;
			break;
		case E_PropType.Agent:
			s += Agent;
			break;
		case E_PropType.CoverState:
			s += CoverState;
			break;
		}

		return s;
	}
}
[System.Serializable]
public class WorldState
{
	internal WorldStateProp[] WorldStateProperties = new WorldStateProp[(int)E_PropKey.Count];

	public WorldStateProp GetWSProperty(E_PropKey key)
	{
		return (int)key < WorldStateProperties.Length ? WorldStateProperties[(int)key] : null;
	}

	public bool IsWSPropertySet(E_PropKey key)
	{
		return (int)key < WorldStateProperties.Length ? WorldStateProperties[(int)key] != null : false;
	}

	public void SetWSProperty(E_PropKey key, bool value)
	{
		if (IsWSPropertySet(key))
		{
			WorldStatePropFactory.Return(WorldStateProperties[(int)key]);
			WorldStateProperties[(int)key] = WorldStatePropFactory.Create(key, value);
		}
		else
			WorldStateProperties[(int)key] = WorldStatePropFactory.Create(key, value);
	}

	public void SetWSProperty(E_PropKey key, float value)
	{
		if (IsWSPropertySet(key))
		{
			WorldStatePropFactory.Return(WorldStateProperties[(int)key]);
			WorldStateProperties[(int)key] = WorldStatePropFactory.Create(key, value);
		}
		else
			WorldStateProperties[(int)key] = WorldStatePropFactory.Create(key, value);
	}

	public void SetWSProperty(E_PropKey key, int value)
	{
		if (IsWSPropertySet(key))
		{
			WorldStatePropFactory.Return(WorldStateProperties[(int)key]);
			WorldStateProperties[(int)key] = WorldStatePropFactory.Create(key, value);
		}
		else
			WorldStateProperties[(int)key] = WorldStatePropFactory.Create(key, value);
	}

	public void SetWSProperty(E_PropKey key, AgentHuman value)
	{
		if (IsWSPropertySet(key))
		{
			WorldStatePropFactory.Return(WorldStateProperties[(int)key]);
			WorldStateProperties[(int)key] = WorldStatePropFactory.Create(key, value);
		}
		else
			WorldStateProperties[(int)key] = WorldStatePropFactory.Create(key, value);
	}

	public void SetWSProperty(E_PropKey key, UnityEngine.Vector3 value)
	{
		if (IsWSPropertySet(key))
		{
			WorldStatePropFactory.Return(WorldStateProperties[(int)key]);
			WorldStateProperties[(int)key] = WorldStatePropFactory.Create(key, value);
		}
		else
			WorldStateProperties[(int)key] = WorldStatePropFactory.Create(key, value);
	}

	public void SetWSProperty(E_PropKey key, E_CoverState value)
	{
		if (IsWSPropertySet(key))
		{
			WorldStatePropFactory.Return(WorldStateProperties[(int)key]);
			WorldStateProperties[(int)key] = WorldStatePropFactory.Create(key, value);
		}
		else
			WorldStateProperties[(int)key] = WorldStatePropFactory.Create(key, value);
	}

	public void SetWSProperty(WorldStateProp other)
	{
		if (other == null)
			return;

		switch (other.PropType)
		{
		case E_PropType.Bool:
			SetWSProperty(other.PropKey, other.GetBool());
			break;
		case E_PropType.Int:
			SetWSProperty(other.PropKey, other.GetInt());
			break;
		case E_PropType.Float:
			SetWSProperty(other.PropKey, other.GetFloat());
			break;
		case E_PropType.Vector:
			SetWSProperty(other.PropKey, other.GetVector());
			break;
		case E_PropType.Agent:
			SetWSProperty(other.PropKey, other.GetAgent());
			break;
		case E_PropType.CoverState:
			SetWSProperty(other.PropKey, other.GetCoverState());
			break;
		default:
			Debug.LogError("error in SetWSProperty " + other.PropKey.ToString());
			break;
		}
	}

	public void ResetWSProperty(E_PropKey key)
	{
		//Debug.Log("Reset WS property " + key.ToString());
		if (IsWSPropertySet(key))
		{
			WorldStatePropFactory.Return(WorldStateProperties[(int)key]);
			WorldStateProperties[(int)key] = null;
		}
	}

	public void Reset()
	{
		//Debug.Log("Worldstate reset");

		for (int i = 0; i < WorldStateProperties.Length; i++)
		{
			if (WorldStateProperties[i] != null)
			{
				WorldStatePropFactory.Return(WorldStateProperties[i]);
				WorldStateProperties[i] = null;
			}
		}
	}

	public void CopyWorldState(WorldState otherState)
	{
		Reset();
		for (int i = 0; i < otherState.WorldStateProperties.Length; i++)
		{
			if (otherState.WorldStateProperties[i] != null)
				SetWSProperty(otherState.WorldStateProperties[i]);
		}
	}

	/**
	* Returns the number of world state properties that are different from the world state input
	* @param the other world state to check against the current world state
	*/

	public int GetNumWorldStateDifferences(WorldState otherState)
	{
		int count = 0;
		for (int i = 0; i < WorldStateProperties.Length; i++)
		{
			if (otherState.WorldStateProperties[i] != null && WorldStateProperties[i] != null)
			{
				if (!(WorldStateProperties[i] == otherState.WorldStateProperties[i]))
					count++;
			}
			else if (otherState.WorldStateProperties[i] != null || WorldStateProperties[i] != null)
				count++;
		}
		return count;
	}

	/**
	* Returns the number of world state properties that are different from the world state input
	* @param the other world state to check against the current world state
	*/

	public int GetNumUnsatisfiedWorldStateProps(WorldState otherState)
	{
		int count = 0;
		for (E_PropKey i = 0; i < E_PropKey.Count; i++)
		{
			if (IsWSPropertySet(i) == false)
				continue;

			if (!otherState.IsWSPropertySet(i))
				count++;

			if (!(GetWSProperty(i) == otherState.GetWSProperty(i))) //test 
				count++;
		}
		return count;
	}

	public override string ToString()
	{
		string s = "World state : ";

		for (E_PropKey i = E_PropKey.Idling; i < E_PropKey.Count; i++)
		{
			if (IsWSPropertySet(i))
				s += " " + GetWSProperty(i).ToString();
		}

		return s;
	}
}
