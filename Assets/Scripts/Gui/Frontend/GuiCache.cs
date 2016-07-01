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

public class GuiCache<T> where T : Component
{
	Dictionary<string, T> m_Cache = new Dictionary<string, T>();

	public T Register(string name, T instance)
	{
		if (instance == null)
			return null;

		T other;
		if (m_Cache.TryGetValue(name, out other) == true)
		{
			if (other.Equals(instance) == true)
			{
				Object.Destroy(instance);
				return other;
			}
		}

		m_Cache[name] = instance;

		return instance;
	}

	public void Clear()
	{
		m_Cache.Clear();
	}
}
