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

#if !UNITY_EDITOR
	#define USE_CIPHERING
#endif

using UnityEngine;

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public class MathUtils
{
	//-----------------------------------------------------------------------------------------------------------------
	public static float SanitizeDegrees(float Angle)
	{
		float DegNegFlip = -0.0001f;
		float DegPosFlip = 360.00f - 0.0001f;

		if (Angle < DegNegFlip)
		{
			Angle += 360.0f;
		}
		else if (Angle > DegPosFlip)
		{
			Angle -= 360.0f;
		}

		return Angle;
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static float SanitizeRadians(float Angle)
	{
		float RadNegFlip = -0.0001f;
		float RadPosFlip = 2.0f*Mathf.PI - 0.0001f;

		if (Angle < RadNegFlip)
		{
			Angle += 2.0f*Mathf.PI;
		}
		else if (Angle > RadPosFlip)
		{
			Angle -= 2.0f*Mathf.PI;
		}
		return Angle;
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 AnglesToVector(Vector3 RefForward, Vector3 RefUp, float AngleH, float AngleV)
	{
		float cA = Mathf.Cos(AngleV);
		float sA = Mathf.Sin(AngleV);
		float cL = Mathf.Cos(AngleH);
		float sL = Mathf.Sin(AngleH);

		float x = cA*cL;
		float y = cA*sL;
		float z = sA;

		Vector3 tmp = Vector3.Cross(RefUp, RefForward);
		Vector3 res = x*RefForward + y*tmp + z*RefUp;

		return Vector3.Normalize(res);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void VectorToAngles(Vector3 RefForward, Vector3 RefUp, Vector3 Vec, ref float AngleH, ref float AngleV)
	{
		float dot = Vector3.Dot(Vec, RefUp);
		Vector3 nDir = Vec - dot*RefUp;

		dot = Mathf.Clamp(dot, -1.0f, +1.0f);
		AngleV = Mathf.Asin(dot);
		dot = Vector3.SqrMagnitude(nDir);

		if (dot < 1.0e-4f)
		{
			AngleH = 0.0f;
		}
		else
		{
			nDir /= Mathf.Sqrt(dot);
			dot = Vector3.Dot(nDir, RefForward);
			dot = Mathf.Clamp(dot, -1.0f, +1.0f);

			Vector3 tmp = Vector3.Cross(nDir, RefForward);

			AngleH = Vector3.Dot(tmp, RefUp) > 0.0f ? -Mathf.Acos(dot) : +Mathf.Acos(dot);
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 RandomVectorInsideCone(Vector3 ConeAxis, float ConeAngle)
	{
		// random vector around "up" axis...

		float a = Random.Range(0.0f, 360.0f*Mathf.Deg2Rad);
		float b = Random.Range(0.0f, ConeAngle*Mathf.Deg2Rad); // should be in range [0,180]

		float tmp = Mathf.Sin(b);
		Vector3 vec = new Vector3(Mathf.Sin(a)*tmp, Mathf.Cos(a)*tmp, Mathf.Cos(b));

		// transform vector to "cone" space...

		return Quaternion.LookRotation(ConeAxis)*vec;
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static bool InRange(float Val, float Min, float Max)
	{
		return (Min <= Val) && (Val <= Max);
	}

	//-----------------------------------------------------------------------------------------------------------------
	//reversible cipher algorithm (C = CipherValue(A,B) => A = CipherValue(C,B))
	public static uint CipherValue(uint value, uint xor)
	{
#if USE_CIPHERING
			return ~((~value) ^ xor);		//not-xor-not
		#else
		return value;
#endif
	}

	//float => float
	public static float CipherValue(float value, uint xor)
	{
#if USE_CIPHERING
			byte[]	bytes = System.BitConverter.GetBytes(value);	//float => uint32
			uint	v = System.BitConverter.ToUInt32(bytes, 0);
			
			uint	uint_result = CipherValue(v, xor);				//mangle it
			
			bytes = System.BitConverter.GetBytes(uint_result);		//convert back to float
			
			return System.BitConverter.ToSingle(bytes, 0);
		#else
		return value;
#endif
	}

	//-----------------------------------------------------------------------------------------------------------------
	//	static public IEnumerable< int > FibonacciUpTo( int Max )   // foreach ( int x in MathUtils.FibonacciUpTo(100) )
	//	{                                                           // {
	//		int  prev;                                              // }
	//		int  curr = 0;
	//		int  next = 1;
	//		
	//		do
	//		{
	//			yield return curr;
	//			
	//			prev = curr;
	//			curr = next;
	//			next = prev + curr;
	//		}
	//		while ( curr < Max );
	//	}
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public class Matrix // various helpers for 'Matrix4x4'...
{
	//-----------------------------------------------------------------------------------------------------------------
	public static Matrix4x4 CreateTranslation(Vector3 Origin)
	{
		return new Matrix4x4
		{
			m00 = 1.0f,
			m01 = 0.0f,
			m02 = 0.0f,
			m03 = Origin.x,
			m10 = 0.0f,
			m11 = 1.0f,
			m12 = 0.0f,
			m13 = Origin.y,
			m20 = 0.0f,
			m21 = 0.0f,
			m22 = 1.0f,
			m23 = Origin.z,
			m30 = 0.0f,
			m31 = 0.0f,
			m32 = 0.0f,
			m33 = 1.0f
		};
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Matrix4x4 CreateTranslation(float OriginX, float OriginY, float OriginZ)
	{
		return new Matrix4x4
		{
			m00 = 1.0f,
			m01 = 0.0f,
			m02 = 0.0f,
			m03 = OriginX,
			m10 = 0.0f,
			m11 = 1.0f,
			m12 = 0.0f,
			m13 = OriginY,
			m20 = 0.0f,
			m21 = 0.0f,
			m22 = 1.0f,
			m23 = OriginZ,
			m30 = 0.0f,
			m31 = 0.0f,
			m32 = 0.0f,
			m33 = 1.0f
		};
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Matrix4x4 CreateScale(Vector3 Scale)
	{
		return new Matrix4x4
		{
			m00 = Scale.x,
			m01 = 0.0f,
			m02 = 0.0f,
			m03 = 0.0f,
			m10 = 0.0f,
			m11 = Scale.y,
			m12 = 0.0f,
			m13 = 0.0f,
			m20 = 0.0f,
			m21 = 0.0f,
			m22 = Scale.z,
			m23 = 0.0f,
			m30 = 0.0f,
			m31 = 0.0f,
			m32 = 0.0f,
			m33 = 1.0f
		};
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Matrix4x4 CreateScale(float Scale)
	{
		return new Matrix4x4
		{
			m00 = Scale,
			m01 = 0.0f,
			m02 = 0.0f,
			m03 = 0.0f,
			m10 = 0.0f,
			m11 = Scale,
			m12 = 0.0f,
			m13 = 0.0f,
			m20 = 0.0f,
			m21 = 0.0f,
			m22 = Scale,
			m23 = 0.0f,
			m30 = 0.0f,
			m31 = 0.0f,
			m32 = 0.0f,
			m33 = 1.0f
		};
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Matrix4x4 CreateScale(float ScaleX, float ScaleY, float ScaleZ)
	{
		return new Matrix4x4
		{
			m00 = ScaleX,
			m01 = 0.0f,
			m02 = 0.0f,
			m03 = 0.0f,
			m10 = 0.0f,
			m11 = ScaleY,
			m12 = 0.0f,
			m13 = 0.0f,
			m20 = 0.0f,
			m21 = 0.0f,
			m22 = ScaleZ,
			m23 = 0.0f,
			m30 = 0.0f,
			m31 = 0.0f,
			m32 = 0.0f,
			m33 = 1.0f
		};
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Matrix4x4 Create(Quaternion Q)
	{
		float x2 = Q.x + Q.x;
		float y2 = Q.y + Q.y;
		float z2 = Q.z + Q.z;

		float x2x = x2*Q.x;
		float x2y = x2*Q.y;
		float x2z = x2*Q.z;
		float x2w = x2*Q.w;
		float y2y = y2*Q.y;
		float y2z = y2*Q.z;
		float y2w = y2*Q.w;
		float z2z = z2*Q.z;
		float z2w = z2*Q.w;

		Matrix4x4 mat = new Matrix4x4();

		mat.m00 = 1.0f - (y2y + z2z);
		mat.m01 = (x2y - z2w);
		mat.m02 = (x2z + y2w);
		mat.m03 = 0.0f;

		mat.m10 = (x2y + z2w);
		mat.m11 = 1.0f - (x2x + z2z);
		mat.m12 = (y2z - x2w);
		mat.m13 = 0.0f;

		mat.m20 = (x2z - y2w);
		mat.m21 = (y2z + x2w);
		mat.m22 = 1.0f - (x2x + y2y);
		mat.m23 = 0.0f;

		mat.m30 = 0.0f;
		mat.m31 = 0.0f;
		mat.m32 = 0.0f;
		mat.m33 = 1.0f;

		return mat;
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Matrix4x4 Create(Vector3 Origin, Vector3 AxisX, Vector3 AxisY, Vector3 AxisZ)
	{
		return new Matrix4x4
		{
			m00 = AxisX.x,
			m01 = AxisY.x,
			m02 = AxisZ.x,
			m03 = Origin.x,
			m10 = AxisX.y,
			m11 = AxisY.y,
			m12 = AxisZ.y,
			m13 = Origin.y,
			m20 = AxisX.z,
			m21 = AxisY.z,
			m22 = AxisZ.z,
			m23 = Origin.z,
			m30 = 0.0f,
			m31 = 0.0f,
			m32 = 0.0f,
			m33 = 1.0f
		};
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 GetAxis(Matrix4x4 Mat, int AxisIndex)
	{
		return Mat.GetColumn(AxisIndex);
		//	return new Vector3( Mat[0,AxisIdx], Mat[1,AxisIdx], Mat[2,AxisIdx] );
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 GetAxisX(Matrix4x4 Mat)
	{
		return new Vector3(Mat.m00, Mat.m10, Mat.m20);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 GetAxisY(Matrix4x4 Mat)
	{
		return new Vector3(Mat.m01, Mat.m11, Mat.m21);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 GetAxisZ(Matrix4x4 Mat)
	{
		return new Vector3(Mat.m02, Mat.m12, Mat.m22);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void SetAxisX(ref Matrix4x4 Mat, Vector3 Axis)
	{
		Mat.m00 = Axis.x;
		Mat.m10 = Axis.y;
		Mat.m20 = Axis.z;
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void SetAxisY(ref Matrix4x4 Mat, Vector3 Axis)
	{
		Mat.m01 = Axis.x;
		Mat.m11 = Axis.y;
		Mat.m21 = Axis.z;
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void SetAxisZ(ref Matrix4x4 Mat, Vector3 Axis)
	{
		Mat.m02 = Axis.x;
		Mat.m12 = Axis.y;
		Mat.m22 = Axis.z;
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void SetAxis(ref Matrix4x4 Mat, Vector3 Axis, int Index)
	{
		Mat[0, Index] = Axis.x;
		Mat[1, Index] = Axis.y;
		Mat[2, Index] = Axis.z;
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 GetOrigin(Matrix4x4 Mat)
	{
		return new Vector3(Mat.m03, Mat.m13, Mat.m23);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void SetOrigin(ref Matrix4x4 Mat, Vector3 Origin)
	{
		Mat.m03 = Origin.x;
		Mat.m13 = Origin.y;
		Mat.m23 = Origin.z;
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 GetEulerAngles(Matrix4x4 Mat)
	{
		Vector3 res = Vector3.zero;

		if (Mat.m21 < 0.999f)
		{
			if (Mat.m21 > -0.999f)
			{
				res.x = MathUtils.SanitizeRadians(Mathf.Asin(-Mat.m21));
				res.y = MathUtils.SanitizeRadians(Mathf.Atan2(Mat.m20, Mat.m22));
				res.z = MathUtils.SanitizeRadians(Mathf.Atan2(Mat.m01, Mat.m11));
			}
			else
			{
				res.x = Mathf.PI*0.5f;
				res.y = MathUtils.SanitizeRadians(Mathf.Atan2(Mat.m10, Mat.m00));
			}
		}
		else
		{
			res.x = Mathf.PI*1.5f;
			res.y = MathUtils.SanitizeRadians(Mathf.Atan2(-Mat.m10, Mat.m00));
		}

		return res;
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void SetEulerAngles(ref Matrix4x4 Mat, Vector3 Angles)
	{
		SetEulerAngles(ref Mat, Angles.x, Angles.y, Angles.z);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void SetEulerAngles(ref Matrix4x4 Mat, float X, float Y, float Z)
	{
		float cx = Mathf.Cos(X);
		float sx = Mathf.Sin(X);
		float cy = Mathf.Cos(Y);
		float sy = Mathf.Sin(Y);
		float cz = Mathf.Cos(Z);
		float sz = Mathf.Sin(Z);

		Mat.m00 = sx*sy*sz + cy*cz;
		Mat.m10 = cz*sx*sy - cy*sz;
		Mat.m20 = cx*sy;

		Mat.m01 = cx*sz;
		Mat.m11 = cx*cz;
		Mat.m21 = -sx;

		Mat.m02 = cy*sx*sz - cz*sy;
		Mat.m12 = cy*cz*sx + sy*sz;
		Mat.m22 = cx*cy;
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 GetScale(Matrix4x4 Mat)
	{
		return new Vector3(Mathf.Sqrt(Mat.m00*Mat.m00 + Mat.m10*Mat.m10 + Mat.m20*Mat.m20),
						   // length of axis X
						   Mathf.Sqrt(Mat.m01*Mat.m01 + Mat.m11*Mat.m11 + Mat.m21*Mat.m21),
						   // length of axis Y
						   Mathf.Sqrt(Mat.m02*Mat.m02 + Mat.m12*Mat.m12 + Mat.m22*Mat.m22)); // length of axis Z
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static float GetScaleX(Matrix4x4 Mat)
	{
		return Mathf.Sqrt(Mat.m00*Mat.m00 + Mat.m10*Mat.m10 + Mat.m20*Mat.m20);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static float GetScaleY(Matrix4x4 Mat)
	{
		return Mathf.Sqrt(Mat.m01*Mat.m01 + Mat.m11*Mat.m11 + Mat.m21*Mat.m21);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static float GetScaleZ(Matrix4x4 Mat)
	{
		return Mathf.Sqrt(Mat.m02*Mat.m02 + Mat.m12*Mat.m12 + Mat.m22*Mat.m22);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 RemoveScale(ref Matrix4x4 Mat)
	{
		Vector3 scale = GetScale(Mat);

		if (scale.x != 0.0f)
		{
			float invX = 1.0f/scale.x;

			Mat.m00 *= invX;
			Mat.m10 *= invX;
			Mat.m20 *= invX;
		}

		if (scale.y != 0.0f)
		{
			float invY = 1.0f/scale.y;

			Mat.m01 *= invY;
			Mat.m11 *= invY;
			Mat.m21 *= invY;
		}

		if (scale.z != 0.0f)
		{
			float invZ = 1.0f/scale.z;

			Mat.m02 *= invZ;
			Mat.m12 *= invZ;
			Mat.m22 *= invZ;
		}

		return scale;
	}
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public class Quat // various helpers for 'Quaternion'...
{
	//-----------------------------------------------------------------------------------------------------------------
	public static Quaternion Create(Matrix4x4 Mat)
	{
		Quaternion res = new Quaternion();

		res.x = Mathf.Sqrt(Mathf.Max(0.0f, 1.0f + Mat.m00 - Mat.m11 - Mat.m22))/2.0f;
		res.y = Mathf.Sqrt(Mathf.Max(0.0f, 1.0f - Mat.m00 + Mat.m11 - Mat.m22))/2.0f;
		res.z = Mathf.Sqrt(Mathf.Max(0.0f, 1.0f - Mat.m00 - Mat.m11 + Mat.m22))/2.0f;
		res.w = Mathf.Sqrt(Mathf.Max(0.0f, 1.0f + Mat.m00 + Mat.m11 + Mat.m22))/2.0f;

		res.x *= Mathf.Sign(res.x*(Mat.m21 - Mat.m12));
		res.y *= Mathf.Sign(res.y*(Mat.m02 - Mat.m20));
		res.z *= Mathf.Sign(res.z*(Mat.m10 - Mat.m01));

		return res;
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 GetAxis(Quaternion Q, int AxisIndex)
	{
		switch (AxisIndex)
		{
		case 0:
			return GetAxisX(Q);
		case 1:
			return GetAxisY(Q);
		}
		return GetAxisZ(Q);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 GetAxisX(Quaternion Q)
	{
		float Tx = Q.x + Q.x;
		float Ty = Q.y + Q.y;
		float Tz = Q.z + Q.z;

		float x = 1.0f - (Q.y*Ty + Q.z*Tz);
		float y = Q.y*Tx + Q.w*Tz;
		float z = Q.z*Tx - Q.w*Ty;

		return new Vector3(x, y, z);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 GetAxisY(Quaternion Q)
	{
		float Tx = Q.x + Q.x;
		float Ty = Q.y + Q.y;
		float Tz = Q.z + Q.z;

		float x = Q.y*Tx - Q.w*Tz;
		float y = 1.0f - (Q.x*Tx + Q.z*Tz);
		float z = Q.z*Ty + Q.w*Tx;

		return new Vector3(x, y, z);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 GetAxisZ(Quaternion Q)
	{
		float Tx = Q.x + Q.x;
		float Ty = Q.y + Q.y;

		float x = Q.z*Tx + Q.w*Ty;
		float y = Q.z*Ty - Q.w*Tx;
		float z = 1.0f - (Q.x*Tx + Q.y*Ty);

		return new Vector3(x, y, z);
	}
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public class ClosestPoint // computes closest point betwean...
{
	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 PointRay(Vector3 P,
								   // point
								   Vector3 RayO,
								   Vector3 RayD) // ray origin & direction
	{
		float t = Vector3.Dot(RayD, P - RayO);
		float sqrLng = Vector3.SqrMagnitude(RayD);

		return RayO + (RayD*(t/sqrLng));
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 PointSegment(Vector3 P,
									   // point
									   Vector3 S0,
									   Vector3 S1) // end-points of line-segment
	{
		Vector3 dir = S1 - S0;
		Vector3 diff = P - S0;
		float t = Vector3.Dot(diff, dir);

		if (t <= 0.0f)
		{
			return S0;
		}
		else
		{
			float sqrLng = Vector3.SqrMagnitude(dir);

			return (t >= sqrLng) ? (S1) : (S0 + dir*(t/sqrLng));
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 PointBounds(Vector3 P,
									  // point
									  Bounds B) // axis-aligned bounding box
	{
		Vector3 res = P;
		Vector3 diff = P - B.center;

		// always returns closest-point on box (even if 'P' is inside it)
		//	res.x -= Mathf.Sign( diff.x ) * ( Mathf.Abs(diff.x) - B.extents.x );
		//	res.y -= Mathf.Sign( diff.y ) * ( Mathf.Abs(diff.y) - B.extents.y );
		//	res.z -= Mathf.Sign( diff.z ) * ( Mathf.Abs(diff.z) - B.extents.z );

		// returns 'P' if it is inside given box or closest-point on it if 'P' is outside
		res.x -= Mathf.Sign(diff.x)*Mathf.Max(0.0f, Mathf.Abs(diff.x) - B.extents.x);
		res.y -= Mathf.Sign(diff.y)*Mathf.Max(0.0f, Mathf.Abs(diff.y) - B.extents.y);
		res.z -= Mathf.Sign(diff.z)*Mathf.Max(0.0f, Mathf.Abs(diff.z) - B.extents.z);

		return res;
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static Vector3 PointBoundsCenter(Vector3 P,
											// point
											Bounds B) // axis-aligned bounding box
	{
		Vector3 res = P;
		Vector3 diff = P - B.center;

		float xCoef = B.extents.x/Mathf.Abs(diff.x);
		float yCoef = B.extents.y/Mathf.Abs(diff.y);
		float zCoef = B.extents.z/Mathf.Abs(diff.z);

		float coef = Mathf.Min(xCoef, yCoef, zCoef);
		coef = Mathf.Max(0.0f, 1.0f - coef);

		res -= coef*diff;

		return res; // intersection of ray and axis-aligned-box ( ray from 'P' to center of 'B' )
	}

	//-----------------------------------------------------------------------------------------------------------------
	//the returned point is a point on line1 and it's the closest point to line2 (equals to start1 when the lines are parallel)
	public static Vector3 PointOfTwoLines(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2)
	{
		Vector3 u = end1 - start1;
		Vector3 v = end2 - start2;
		Vector3 w = start1 - start2;
		float a = Vector3.Dot(u, u); // always >= 0
		float b = Vector3.Dot(u, v);
		float c = Vector3.Dot(v, v); // always >= 0
		float d = Vector3.Dot(u, w);
		float e = Vector3.Dot(v, w);
		float D = a*c - b*b; // always >= 0
		float sc;

		// compute the line parameters of the two closest points
		if (D < Mathf.Epsilon) // the lines are almost parallel
			sc = 0.0f;
		else
			sc = (b*e - c*d)/D;

		//clip the result to the line1 segment
		if (sc < 0)
			return start1;
		if (sc*sc > u.sqrMagnitude)
			return end1;

		Vector3 p = start1 + u*sc;

		return p; //return the closest point on line1
	}

	//-----------------------------------------------------------------------------------------------------------------
	//-----------------------------------------------------------------------------------------------------------------
	//computes two points on line1 and line2 which are closest; returns false if the lines are parallel (and still returns two points)
	public static bool PointsOnLines(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2, out Vector3 p1, out Vector3 p2)
	{
		Vector3 u = end1 - start1;
		Vector3 v = end2 - start2;
		Vector3 w = start1 - start2;
		float a = Vector3.Dot(u, u); // always >= 0
		float b = Vector3.Dot(u, v);
		float c = Vector3.Dot(v, v); // always >= 0
		float d = Vector3.Dot(u, w);
		float e = Vector3.Dot(v, w);
		float D = a*c - b*b; // always >= 0
		float sc, tc;

		// compute the line parameters of the two closest points
		if (D < Mathf.Epsilon) // the lines are almost parallel
		{
			sc = 0.0f;
			tc = (b > c ? d/b : e/c); // use the largest denominator
		}
		else
		{
			sc = (b*e - c*d)/D;
			tc = (a*e - b*d)/D;
		}

		p1 = start1 + u*sc;
		p2 = start2 + v*tc;

		return D >= Mathf.Epsilon; //return false if the lines are parallel
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static float DistanceBetweenLines(Vector3 start1, Vector3 end1, Vector3 start2, Vector3 end2)
	{
		Vector3 p1, p2;

		PointsOnLines(start1, end1, start2, end2, out p1, out p2);

		return (p2 - p1).magnitude; //return the closest distance
	}
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public class SquareDistance // computes squared distance between...
{
	//-----------------------------------------------------------------------------------------------------------------
	public static float PointRay(Vector3 P,
								 // point
								 Vector3 RayO,
								 Vector3 RayD) // ray origin & direction
	{
		return Vector3.SqrMagnitude(P - ClosestPoint.PointRay(P, RayO, RayD));
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static float PointSegment(Vector3 P,
									 // point
									 Vector3 S0,
									 Vector3 S1) // end-points of line-segment
	{
		return Vector3.SqrMagnitude(P - ClosestPoint.PointSegment(P, S0, S1));
	}
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

public class Roots // computes real root(s) of equations...
{
	public const float ZeroEps = 1.0e-6f;

	//-----------------------------------------------------------------------------------------------------------------
	public static float Linear(float A, float B) // A*x + B = 0
	{
		if (Mathf.Abs(A) > ZeroEps)
		{
			return -B/A;
		}
		return 0.0f;
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static int Quadratic(float A, float B, float C, float[] X) // A*x^2 + B*x + C = 0
	{
		// quadratic (monic form 'x^2 + p*x + q = 0' with my "pico"-optimization)...
		if (Mathf.Abs(A) > ZeroEps)
		{
			float p = -B/A;
			float q = C/A;
			float sqrD = p*p - 4.0f*q;

			if (sqrD > 0.0f)
			{
				float D = Mathf.Sqrt(sqrD);

				X[0] = (p - D)*0.5f;
				X[1] = (p + D)*0.5f;

				return 2;
			}
			else if (sqrD < 0.0f)
			{
				return 0;
			}

			X[0] = p*0.5f;
		}
		// linear (no quadratic term)...
		else
		{
			X[0] = Linear(B, C);
		}

		return 1;
	}

//	//-----------------------------------------------------------------------------------------------------------------
//	static public int Quadratic( float A, float B, float C, float [] X ) // A*x^2 + B*x + C = 0
//	{
//		// quadratic...
//		if ( Mathf.Abs(A) > ZeroEps )
//		{
//			float sqrD = B * B - 4.0f * A * C;
//			
//			if ( sqrD > 0.0f )
//			{
//				float  D     = Mathf.Sqrt( sqrD );
//				float  inv2A = 0.5f / A;
//				
//				X[0] = ( -B - D ) * inv2A;
//				X[1] = ( -B + D ) * inv2A;
//				
//				return 2;
//			}
//			else if ( sqrD < 0.0f )
//			{
//				return 0;
//			}
//			
//			X[0] = -B / ( 2.0f * A );
//		}
//		// linear (no quadratic term)...
//		else
//		{
//			X[0] = Linear( B, C );
//		}
//		
//		return 1;
//	}
}

///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

//Used for a frustum representation and various checks against it
//Usage: setup the camera first with actual parameters, than test the intersections
public class Frustum
{
	public enum E_Location
	{
		Outside,
		Intersect,
		Inside
	};

	static Vector3 CameraPos, CameraDir; //camera position and direction
	static Vector3 X, Y, Z; //the camera referential
	static float Near, Far, /*Width, Height,*/ Ratio, Tan; //frustum limits

	//Setup all parameters of camera. 

	public static void SetupCamera(GameCamera camera, float far = -1)
	{
		SetCameraInternals(camera.GetCurrentFov(),
						   camera.MainCamera.aspect,
						   camera.MainCamera.nearClipPlane,
						   far == -1 ? camera.MainCamera.farClipPlane : far);
		SetCameraTransform(camera.CameraPosition, camera.CameraForward, camera.CameraRight, camera.CameraUp);
	}

	//Each time the perspective definitions change, for instance when a window is resized, this function should be called.
	//The function stores all the information, and computes the width and height of the rectangular sections of the near plane and stores them in height (near height) and width (near width)
	public static void SetCameraInternals(float fov, float ratio, float near, float far)
	{
		//store the information
		Ratio = ratio;
		Near = near;
		Far = far;

		//compute width and height of the near section
		Tan = Mathf.Tan(Mathf.Deg2Rad*fov*0.5f);
		//Height	= near * Tan;
		//Width	= Height * ratio;		//not used
	}

	//This function takes three vectors that contain: the position of the camera, a target point at 'far' distance and the up vector. 
	//Each time the camera position or orientation changes, this function should be called as well.
	public static void SetCameraTransform(Vector3 pos, Vector3 target, Vector3 up)
	{
		CameraPos = pos;
		CameraDir = (target - pos).normalized;

		//Compute the Z axis of the camera referential. This axis points in the same direction from the looking direction
		Z = CameraDir;

		//X axis of camera with given "up" vector and Z axis
		X = Vector3.Cross(Z, up);

		//The real "up" vector is the dot product of X and Z
		Y = Vector3.Cross(X, Z);
	}

	//This function takes four vectors that contain: the position of the camera, normalized forward (dir), right and up vectors.
	//Each time the camera position or orientation changes, this function should be called as well.
	public static void SetCameraTransform(Vector3 pos, Vector3 forward, Vector3 right, Vector3 up)
	{
		CameraPos = pos;
		CameraDir = forward;

		Z = forward;
		X = right;
		Y = up;
	}

	//Test whether the point lies inside the frustum.
	public static E_Location PointInFrustum(Vector3 p)
	{
		float pcz, pcx, pcy, aux;

		//compute vector from camera position to p
		Vector3 v = p - CameraPos;

		//compute and test the Z coordinate
		pcz = Vector3.Dot(v, Z); //Vector3.Dot(v, -Z);
		if (pcz < Near || pcz > Far)
			return E_Location.Outside;

		//compute and test the Y coordinate
		pcy = Vector3.Dot(v, Y);
		aux = pcz*Tan;
		if (pcy > aux || pcy < -aux)
			return E_Location.Outside;

		//compute and test the X coordinate
		pcx = Vector3.Dot(v, X);
		aux = aux*Ratio;
		if (pcx > aux || pcx < -aux)
			return E_Location.Outside;

		return E_Location.Inside;
	}

	//																																       o
	//Test whether the line intersects (or lies inside) the frustum.																    +-----+
	//NOTE:  For performance reasons we're not doing a precise Intersect/Inside test. The function returns Inside in both cases.	    |/    |
	//FIXME: This function is not accurate, returns outside in case the line crosses corners near the frustum edge!	Something like ->  o+-----+
	public static E_Location LineInFrustumFast(Vector3 lineStart, Vector3 lineEnd)
	{
		//check if any of the line point lies inside
		if (PointInFrustum(lineStart) != E_Location.Outside)
			return E_Location.Inside;

		if (PointInFrustum(lineEnd) != E_Location.Outside)
						//NOTE: if interested whether the whole line lies Inside, we'd need to check both lineStart and lineEnd before returning...
			return E_Location.Inside;

		//compute the nearest point between line and camera direction
		Vector3 p = ClosestPoint.PointOfTwoLines(lineStart, lineEnd, CameraPos, CameraPos + CameraDir*Far);

		return PointInFrustum(p);
	}

	//Intersect a segment and a plane
	//Output: p = the intersect point (when it exists, i.e. when this function returns 1)
	//Return: Outside   = disjoint (no intersection)
	//        Intersect = intersection in the unique point 'p'
	//        Inside    = the segment lies in the plane
	public static E_Location IntersectSegmentPlane(Vector3 lineStart, Vector3 lineEnd, Plane plane, out Vector3 p)
	{
		Vector3 u = lineEnd - lineStart;
		Vector3 w = lineStart + (plane.normal*plane.distance);

		float D = Vector3.Dot(plane.normal, u);
		float N = -Vector3.Dot(plane.normal, w);

		if (Mathf.Abs(D) < Mathf.Epsilon) //segment is parallel to plane
		{
			p = Vector3.zero;

			if (N == 0) //segment lies in plane
				return E_Location.Inside;
			else
				return E_Location.Outside; //no intersection
		}

		// they are not parallel compute intersect param
		float sI = N/D;
		if (sI < 0 || sI > 1)
		{
			p = Vector3.zero;
			return E_Location.Outside; //no intersection
		}

		p = lineStart + sI*u; //compute segment intersect point

		return E_Location.Intersect;
	}

	//Test whether the line intersects (or lies inside) the frustum.
	//Beny: It seems that the planes created by GeometryUtility.CalculateFrustumPlanes() are not always correct.
	//      Calculate it here and compare with their results!
	public static E_Location LineInFrustum(Camera camera, Vector3 lineStart, Vector3 lineEnd)
	{
		if (PointInFrustum(lineStart) != E_Location.Outside)
			return E_Location.Intersect; // or inside ?!?

		if (PointInFrustum(lineEnd) != E_Location.Outside)
			return E_Location.Inside;

		E_Location result = E_Location.Inside;
		Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);

		foreach (Plane plane in planes)
		{
			float ds = plane.GetDistanceToPoint(lineStart);
			float de = plane.GetDistanceToPoint(lineEnd);

			if (ds < 0.0f && de < 0.0f) // both behind the plane
				return E_Location.Outside;

			if (ds*de < 0.0f) // on different sides of plane
			{
				Vector3 ip = lineStart + (lineEnd - lineStart)*(ds/(ds - de));

				if (ds < 0.0f)
					lineStart = ip;
				else
					lineEnd = ip;

				result = E_Location.Intersect;
			}
		}

		return result;
	}
}

//#####################################################################################################################
