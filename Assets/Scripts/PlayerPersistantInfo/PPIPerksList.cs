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
using System.Collections;
using System.Collections.Generic;
using BitStream = uLink.BitStream;

public class PPIList<T>
{
	//
	// Don't change identifiers of members of this class, otherwise it will break online-shopping service. If you really
	// need to change it, modify appropriate constants in ShopUtl.java (located in cloud service servlet src code)
	//

	public List<T> Items = new List<T>();

	public void Write(BitStream stream)
	{
		stream.WriteInt16((short)Items.Count);

		foreach (T id in Items)
			stream.Write<T>(id);
	}

	public void Read(BitStream stream)
	{
		int count = (int)stream.ReadInt16();

		for (int i = 0; i < count; i++)
		{
			T id = stream.Read<T>();
			Items.Add(id);
		}
	}

	public static void Serialize(BitStream stream, object value, params object[] args)
	{
		PPIList<T> ppi = (PPIList<T>)value;
		ppi.Write(stream);
	}

	public static object Deserialize(BitStream stream, params object[] args)
	{
		PPIList<T> ppi = new PPIList<T>();
		ppi.Read(stream);
		return ppi;
	}
}
