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

public class PPIOutfits
{
	// don't add any other data members here - otherwise it would break online shopping service

	public E_SkinID Skin = E_SkinID.Skin01_Soldier;
	public E_HatID Hat = E_HatID.None;

	public void Write(BitStream stream)
	{
		stream.Write<E_SkinID>(Skin);
		stream.Write<E_HatID>(Hat);
	}

	public void Read(BitStream stream)
	{
		Skin = stream.Read<E_SkinID>();
		Hat = stream.Read<E_HatID>();
	}

	public static void Serialize(BitStream stream, object value, params object[] args)
	{
		PPIOutfits ppi = (PPIOutfits)value;
		ppi.Write(stream);
	}

	public static object Deserialize(BitStream stream, params object[] args)
	{
		PPIOutfits ppi = new PPIOutfits();
		ppi.Read(stream);
		return ppi;
	}
}
