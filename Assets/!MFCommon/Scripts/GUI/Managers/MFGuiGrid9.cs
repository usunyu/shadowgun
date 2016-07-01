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

[System.Serializable]
public class MFGuiGrid9
{
	public bool Enabled = false;
	public float LeftSlice = 0.33f;
	public float TopSlice = 0.33f;
	public float RightSlice = 0.33f;
	public float BottomSlice = 0.33f;

	public int ComputeSegments(out float[] xAxis, out float[] yAxis)
	{
		// deduce horizontal offsets
		xAxis = new float[4];
		xAxis[0] = 0.0f;
		xAxis[1] = Enabled == true ? LeftSlice : 0.0f;
		xAxis[2] = Enabled == true ? RightSlice : 0.0f;
		xAxis[3] = 0.0f;

		int xCount = 1;
		if (xAxis[1] > 0.0f)
			++xCount;
		if (xAxis[2] > 0.0f)
			++xCount;

		// deduce vertical offsets
		yAxis = new float[4];
		yAxis[0] = 0.0f;
		yAxis[1] = Enabled == true ? TopSlice : 0.0f;
		yAxis[2] = Enabled == true ? BottomSlice : 0.0f;
		yAxis[3] = 0.0f;

		int yCount = 1;
		if (yAxis[1] > 0.0f)
			++yCount;
		if (yAxis[2] > 0.0f)
			++yCount;

		return xCount*yCount;
	}
}

public class MFGuiGrid9Cached
{
	public float[] x;
	public float[] y;
	public byte c;

	public MFGuiGrid9Cached()
	{
		x = new float[4];
		y = new float[4];
		c = 1;
	}

	public MFGuiGrid9Cached(MFGuiGrid9 other)
	{
		c = (byte)other.ComputeSegments(out x, out y);
	}
}
