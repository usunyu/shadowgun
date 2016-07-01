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
using System;

public class Mathfx
{
	public static Vector3 InterpolateCatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
	{
		float t2 = t*t;
		float t3 = t2*t;

		return 0.5f*(2*p1 + (p2 - p0)*t + (2*p0 - 5*p1 + 4*p2 - p3)*t2 + (3*p1 - 3*p2 + p3 - p0)*t3);
	}

	public static float Hermite(float start, float end, float value)
	{
		return Mathf.Lerp(start, end, value*value*(3.0f - 2.0f*value));
	}

	public static Vector3 Hermite(Vector3 start, Vector3 end, float value)
	{
		return new Vector3(Hermite(start.x, end.x, value), Hermite(start.y, end.y, value), Hermite(start.z, end.z, value));
	}

	public static float Sinerp(float start, float end, float value)
	{
		return Mathf.Lerp(start, end, Mathf.Sin(value*Mathf.PI*0.5f));
	}

	public static Vector3 Sinerp(Vector3 start, Vector3 end, float value)
	{
		return new Vector3(Sinerp(start.x, end.x, value), Sinerp(start.y, end.y, value), Sinerp(start.z, end.z, value));
	}

	public static float Coserp(float start, float end, float value)
	{
		return Mathf.Lerp(start, end, 1.0f - Mathf.Cos(value*Mathf.PI*0.5f));
	}

	public static Vector3 Coserp(Vector3 start, Vector3 end, float value)
	{
		return new Vector3(Coserp(start.x, end.x, value), Coserp(start.y, end.y, value), Coserp(start.z, end.z, value));
	}

	public static float Berp(float start, float end, float value)
	{
		value = Mathf.Clamp01(value);
		value = (Mathf.Sin(value*Mathf.PI*(0.2f + 2.5f*value*value*value))*Mathf.Pow(1f - value, 2.2f) + value)*(1f + (1.2f*(1f - value)));
		return start + (end - start)*value;
	}

	public static float SmoothStep(float x, float min, float max)
	{
		x = Mathf.Clamp(x, min, max);
		float v1 = (x - min)/(max - min);
		float v2 = (x - min)/(max - min);
		return -2*v1*v1*v1 + 3*v2*v2;
	}

	public static float Lerp(float start, float end, float value)
	{
		return ((1.0f - value)*start) + (value*end);
	}

	public static Vector3 NearestPoint(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
	{
		Vector3 lineDirection = Vector3.Normalize(lineEnd - lineStart);
		float closestPoint = Vector3.Dot((lineStart - point), lineDirection)/Vector3.Dot(lineDirection, lineDirection);
		return lineStart + (closestPoint*lineDirection);
	}

	public static float DistanceFromPointToVector(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
	{
		Vector3 online = NearestPoint(lineStart, lineEnd, point);
		return (point - online).magnitude;
	}

	public static Vector3 NearestPointStrict(Vector3 lineStart, Vector3 lineEnd, Vector3 point)
	{
		Vector3 fullDirection = lineEnd - lineStart;
		Vector3 lineDirection = Vector3.Normalize(fullDirection);
		float closestPoint = Vector3.Dot((point - lineStart), lineDirection)/Vector3.Dot(lineDirection, lineDirection);
		return lineStart + (Mathf.Clamp(closestPoint, 0.0f, Vector3.Magnitude(fullDirection))*lineDirection);
	}

	public static float Bounce(float x)
	{
		return Mathf.Abs(Mathf.Sin(6.28f*(x + 1f)*(x + 1f))*(1f - x));
	}

	// test for value that is near specified float (due to floating point inprecision)
	// all thanks to Opless for this!
	public static bool Approx(float val, float about, float range)
	{
		return ((Mathf.Abs(val - about) < range));
	}

	// test if a Vector3 is close to another Vector3 (due to floating point inprecision)
	// compares the square of the distance to the square of the range as this 
	// avoids calculating a square root which is much slower than squaring the range
	public static bool Approx(Vector3 val, Vector3 about, float range)
	{
		return ((val - about).sqrMagnitude < range*range);
	}

	/*
     * CLerp - Circular Lerp - is like lerp but handles the wraparound from 0 to 360.
     * This is useful when interpolating eulerAngles and the object
     * crosses the 0/360 boundary.  The standard Lerp function causes the object
     * to rotate in the wrong direction and looks stupid. Clerp fixes that.
     */

	public static float Clerp(float start, float end, float value)
	{
		float min = 0.0f;
		float max = 360.0f;
		float half = Mathf.Abs((max - min)/2.0f); //half the distance between min and max
		float retval = 0.0f;
		float diff = 0.0f;

		if ((end - start) < -half)
		{
			diff = ((max - start) + end)*value;
			retval = start + diff;
		}
		else if ((end - start) > half)
		{
			diff = -((max - end) + start)*value;
			retval = start + diff;
		}
		else
			retval = start + (end - start)*value;

		// Debug.Log("Start: "  + start + "   End: " + end + "  Value: " + value + "  Half: " + half + "  Diff: " + diff + "  Retval: " + retval);
		return retval;
	}

	public static Vector3 BezierSpline(Vector3[] pts, float pm)
	{
		float rawParam = pm*pts.Length;
		int currLeg = Mathf.Clamp(Mathf.FloorToInt(rawParam), 0, pts.Length - 1);
		float t = rawParam - currLeg;

		Vector3 st;
		Vector3 en;
		Vector3 ctrl;

		if (currLeg == 0)
		{
			st = pts[0];
			en = (pts[1] + pts[0])*0.5f;
			return Vector3.Lerp(st, en, t);
		}
		else if (currLeg == (pts.Length - 1))
		{
			int pBound = pts.Length - 1;
			st = (pts[pBound - 1] + pts[pBound])*0.5f;
			en = pts[pBound];
			return Vector3.Lerp(st, en, t);
		}
		else
		{
			st = (pts[currLeg - 1] + pts[currLeg])*0.5f;
			en = (pts[currLeg + 1] + pts[currLeg])*0.5f;
			ctrl = pts[currLeg];
			return BezInterp(st, en, ctrl, t);
		}
	}

	public static Vector3 SmoothCurveDirection(Vector3[] pts, float pm)
	{
		float rawParam = pm*pts.Length;
		int currLeg = Mathf.Clamp(Mathf.FloorToInt(rawParam), 0, pts.Length - 1);
		float t = rawParam - currLeg;

		Vector3 st;
		Vector3 en;
		Vector3 ctrl;

		if (currLeg == 0)
		{
			st = pts[0];
			en = (pts[1] + pts[0])*0.5f;
			return en - st;
		}
		else if (currLeg == (pts.Length - 1))
		{
			int pBound = pts.Length - 1;
			st = (pts[pBound - 1] + pts[pBound])*0.5f;
			en = pts[pBound];
			return en - st;
		}
		else
		{
			st = (pts[currLeg - 1] + pts[currLeg])*0.5f;
			en = (pts[currLeg + 1] + pts[currLeg])*0.5f;
			ctrl = pts[currLeg];
			return BezDirection(st, en, ctrl, t);
		}
	}

	static Vector3 BezInterp(Vector3 st, Vector3 en, Vector3 ctrl, float t)
	{
		float d = 1.0f - t;
		return d*d*st + 2.0f*d*t*ctrl + t*t*en;
	}

	static Vector3 BezDirection(Vector3 st, Vector3 en, Vector3 ctrl, float t)
	{
		return (2.0f*st - 4.0f*ctrl + 2.0f*en)*t + 2.0f*ctrl - 2.0f*st;
	}

	public static E_Direction GetDirectionToVector(Transform transform, Vector3 dir)
	{
		float dotForward = Vector3.Dot(transform.forward, dir);
		float dotRight = Vector3.Dot(transform.right, dir);

		if (dotForward > 0.5f)
			return E_Direction.Forward;

		if (dotForward < -0.5f)
			return E_Direction.Backward;

		if (dotRight > 0.5f)
			return E_Direction.Right;

		return E_Direction.Left;
	}

	public static Vector3 GetBestPositionFromPath(Vector3 start, Vector3[] path, int numberofCheckpoint, float minDistance, float maxDistance)
	{
		Vector3 finalPos = start;
		Vector3 previousPos = start;
		float len = 0;

		for (int i = 0; i < numberofCheckpoint; i++)
		{
			float dist = (path[i] - previousPos).magnitude;
			len += dist;
			if (len > maxDistance)
			{
				return previousPos + (path[i] - previousPos).normalized*UnityEngine.Random.Range(0, dist - (len - maxDistance));
			}
			finalPos = previousPos;
			previousPos = path[i];
		}

		return finalPos; // return last - 1 pos
	}

	public static void Matrix_SetPos(ref Matrix4x4 inoutMatrix, Vector3 inPos)
	{
		inoutMatrix.m03 = inPos.x;
		inoutMatrix.m13 = inPos.y;
		inoutMatrix.m23 = inPos.z;
	}

	public static Vector3 Matrix_GetPos(Matrix4x4 inMatrix)
	{
		return new Vector3(inMatrix.m03, inMatrix.m13, inMatrix.m23);
	}

	public static void Matrix_SetScale(ref Matrix4x4 inoutMatrix, Vector3 inScale)
	{
		inoutMatrix.m00 = inScale.x;
		inoutMatrix.m11 = inScale.y;
		inoutMatrix.m22 = inScale.z;
	}

	public static Vector3 Matrix_GetScale(Matrix4x4 inMatrix)
	{
		return new Vector3(inMatrix.m00, inMatrix.m11, inMatrix.m22);
	}

	public static void Matrix_SetEulerAngles(ref Matrix4x4 inoutMatrix, Vector3 inEulerAngles)
	{
		float cx = Mathf.Cos(inEulerAngles.x);
		float sx = Mathf.Sin(inEulerAngles.x);
		float cy = Mathf.Cos(inEulerAngles.y);
		float sy = Mathf.Sin(inEulerAngles.y);
		float cz = Mathf.Cos(inEulerAngles.z);
		float sz = Mathf.Sin(inEulerAngles.z);

		inoutMatrix.m00 = cy*cz + sx*sy*sz;
		inoutMatrix.m10 = cz*sx*sy - cy*sz;
		inoutMatrix.m20 = cx*sy;

		inoutMatrix.m01 = cx*sz;
		inoutMatrix.m11 = cx*cz;
		inoutMatrix.m21 = -sx;

		inoutMatrix.m02 = -cz*sy + cy*sx*sz;
		inoutMatrix.m12 = cy*cz*sx + sy*sz;
		inoutMatrix.m22 = cx*cy;
	}

	public static Vector3 Matrix_GetEulerAngles(Matrix4x4 inMatrix)
	{
		Vector3 v = Vector3.zero;
		// from http://www.geometrictools.com/Documentation/EulerAngles.pdf
		// YXZ order
		if (inMatrix.m21 < 0.999F) // some fudge for imprecision
		{
			if (inMatrix.m21 > -0.999F) // some fudge for imprecision
			{
				v.x = Mathf.Asin(-inMatrix.m21);
				v.y = Mathf.Atan2(inMatrix.m20, inMatrix.m22);
				v.z = Mathf.Atan2(inMatrix.m01, inMatrix.m11);
				SanitizeEuler(ref v);
				return v;
			}
			else
			{
				// WARNING.  Not unique.  YA - ZA = atan2(r01,r00)
				v.x = Mathf.PI*0.5F;
				v.y = Mathf.Atan2(inMatrix.m10, inMatrix.m00);
				v.z = 0.0F;
				SanitizeEuler(ref v);

				return v;
			}
		}
		else
		{
			// WARNING.  Not unique.  YA + ZA = atan2(-r01,r00)
			v.x = -Mathf.PI*0.5F;
			v.y = Mathf.Atan2(-inMatrix.m10, inMatrix.m00);
			v.z = 0.0F;
			SanitizeEuler(ref v);
			return v;
		}
	}

	internal static void SanitizeEuler(ref Vector3 euler)
	{
		float negativeFlip = -0.0001F;
		float positiveFlip = (Mathf.PI*2.0F) - 0.0001F;

		if (euler.x < negativeFlip)
			euler.x += 2.0f*Mathf.PI;
		else if (euler.x > positiveFlip)
			euler.x -= 2.0f*Mathf.PI;

		if (euler.y < negativeFlip)
			euler.y += 2.0f*Mathf.PI;
		else if (euler.y > positiveFlip)
			euler.y -= 2.0f*Mathf.PI;

		if (euler.z < negativeFlip)
			euler.z += 2.0f*Mathf.PI;
		else if (euler.z > positiveFlip)
			euler.z -= 2.0f*Mathf.PI;
	}
}
