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

public abstract class GUIBase_FontBase : MonoBehaviour
{
	public abstract Material fontMaterial { get; }
/*
	public virtual float GetCharWidth(int cIdx)
	{
		return 0.0f;
	}

	public virtual float GetCharHeight(int cIdx)
	{
		return 0.0f;
	}

	//public virtual bool GetCharDscr(int cIdx, out float width, out float height, ref Vector2 inTexPos, ref Vector2 inTexSize)
	public virtual bool GetCharDscr(int cIdx, out float width, ref Vector2 inTexPos, ref Vector2 inTexSize)
	{
		width  = 0.0f;
		//height = 0.0f;
		return false;
	}

    public virtual bool GetTextSize(string inText, out Vector2 inSize, bool inTreatSpecialChars)
    {
		inSize = Vector2.zero;
		return false;
    }
    */
}
