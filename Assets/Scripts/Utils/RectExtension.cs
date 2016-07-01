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

public static class RectExtension
{
	public static bool IsEmpty(this Rect rect)
	{
		return rect.width == 0 || rect.height == 0;
	}

	public static Rect Union(this Rect left, Rect right)
	{
		float x = Mathf.Min(left.xMin, right.xMin);
		float y = Mathf.Min(left.yMin, right.yMin);
		float w = Mathf.Max(0.0f, Mathf.Max(left.xMax, right.xMax) - x);
		float h = Mathf.Max(0.0f, Mathf.Max(left.yMax, right.yMax) - y);
		left.x = x;
		left.y = y;
		left.width = w;
		left.height = h;
		return left;
	}

	public static Rect Intersect(this Rect left, Rect right)
	{
		float x = Mathf.Max(left.xMin, right.xMin);
		float y = Mathf.Max(left.yMin, right.yMin);
		float w = Mathf.Max(0.0f, Mathf.Min(left.xMax, right.xMax) - x);
		float h = Mathf.Max(0.0f, Mathf.Min(left.yMax, right.yMax) - y);
		left.x = x;
		left.y = y;
		left.width = w;
		left.height = h;
		return left;
	}

	public static Rect Inflate(this Rect rect, float x, float y)
	{
		rect.x -= x;
		rect.y -= y;
		rect.width += x*2;
		rect.height += y*2;
		return rect;
	}

	public static Rect Deflate(this Rect rect, float x, float y)
	{
		return rect.Inflate(-x, -y);
	}

	public static Rect MakePixelPerfect(this Rect @this)
	{
		@this.xMin = Mathf.RoundToInt(@this.xMin - 0.5f);
		@this.yMin = Mathf.RoundToInt(@this.yMin - 0.5f);
		@this.xMax = Mathf.RoundToInt(@this.xMax + 0.5f);
		@this.yMax = Mathf.RoundToInt(@this.yMax + 0.5f);
		return @this;
	}
}
