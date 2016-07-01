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

using UnityEngine;
using System.Collections.Generic;

public static class WorldStatePropFactory
{
	static Queue<WorldStateProp> m_UnusedProps = new Queue<WorldStateProp>();

	public static WorldStateProp Create(E_PropKey key, bool state)
	{
		WorldStateProp p = null;

		if (m_UnusedProps.Count > 0)
		{
			p = m_UnusedProps.Dequeue();
			p.Bool = state;
			p.PropType = E_PropType.Bool;
		}
		else
			p = new WorldStateProp(state);

		p.Time = UnityEngine.Time.timeSinceLevelLoad;
		p.PropKey = key;
		return p;
	}

	public static WorldStateProp Create(E_PropKey key, int state)
	{
		WorldStateProp p;

		if (m_UnusedProps.Count > 0)
		{
			p = m_UnusedProps.Dequeue();
			p.Int = state;
			p.PropType = E_PropType.Int;
		}
		else
			p = new WorldStateProp(state);

		p.Time = UnityEngine.Time.timeSinceLevelLoad;
		p.PropKey = key;
		return p;
	}

	public static WorldStateProp Create(E_PropKey key, float state)
	{
		WorldStateProp p;

		if (m_UnusedProps.Count > 0)
		{
			p = m_UnusedProps.Dequeue();
			p.PropKey = key;
			p.Float = state;
		}
		else
			p = new WorldStateProp(state);

		p.Time = UnityEngine.Time.timeSinceLevelLoad;
		p.PropType = E_PropType.Float;
		return p;
	}

	public static WorldStateProp Create(E_PropKey key, AgentHuman state)
	{
		WorldStateProp p = null;

		if (m_UnusedProps.Count > 0)
		{
			p = m_UnusedProps.Dequeue();
			p.Agent = state;
			p.PropType = E_PropType.Agent;
		}
		else
			p = new WorldStateProp(state);

		p.Time = UnityEngine.Time.timeSinceLevelLoad;
		p.PropKey = key;
		return p;
	}

	public static WorldStateProp Create(E_PropKey key, UnityEngine.Vector3 vector)
	{
		WorldStateProp p = null;

		if (m_UnusedProps.Count > 0)
		{
			p = m_UnusedProps.Dequeue();
			p.Vector = vector;
			p.PropType = E_PropType.Vector;
		}
		else
			p = new WorldStateProp(vector);

		p.Time = UnityEngine.Time.timeSinceLevelLoad;
		p.PropKey = key;
		return p;
	}

	public static WorldStateProp Create(E_PropKey key, E_CoverState state)
	{
		WorldStateProp p = null;

		if (m_UnusedProps.Count > 0)
		{
			p = m_UnusedProps.Dequeue();
			p.CoverState = state;
			p.PropType = E_PropType.CoverState;
		}
		else
			p = new WorldStateProp(state);

		p.Time = UnityEngine.Time.timeSinceLevelLoad;
		p.PropKey = key;
		return p;
	}

	public static void Return(WorldStateProp prop)
	{
		prop.PropKey = E_PropKey.Count;
		m_UnusedProps.Enqueue(prop);
	}
}
