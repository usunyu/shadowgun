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

using UnityEngine;

// using System.Collections.Generic;

//#####################################################################################################################

public class DebugDraw
{
	#region constants /////////////////////////////////////////////////////////////////////////////////////////////////

	const int MinDetails = 12;
	const int MidDetails = 24;
	const int MaxDetails = 36;

	readonly static Vector3[] DiamondCorner =
	{
		new Vector3(-1.0f, 0.0f, 0.0f),
		new Vector3(+1.0f, 0.0f, 0.0f),
		new Vector3(0.0f, -1.0f, 0.0f),
		new Vector3(0.0f, +1.0f, 0.0f),
		new Vector3(0.0f, 0.0f, -1.0f),
		new Vector3(0.0f, 0.0f, +1.0f)
	};

	readonly static Vector3[] BoxCorner =
	{
		new Vector3(-0.5f, -0.5f, -0.5f),
		new Vector3(0.5f, -0.5f, -0.5f),
		new Vector3(0.5f, -0.5f, 0.5f),
		new Vector3(-0.5f, -0.5f, 0.5f),
		new Vector3(-0.5f, 0.5f, -0.5f),
		new Vector3(0.5f, 0.5f, -0.5f),
		new Vector3(0.5f, 0.5f, 0.5f),
		new Vector3(-0.5f, 0.5f, 0.5f)
	};

	#endregion

	#region fields ////////////////////////////////////////////////////////////////////////////////////////////////////

	// number of segments of circle approx
	static int m_SegNum;

	// increment in angle
	static float m_AngleStep;

	// display-time used for next primitives
	static float m_DisplayTime = 0.0f;

	// display-time used for next primitives
	static bool m_DepthTest = true;

	// precomputed sin/cos multiplied by radius
	static float[] m_SinTable = new float[MaxDetails];
	static float[] m_CosTable = new float[MaxDetails];

	// rotations applied to capsules along X/Y dir
	static Matrix4x4[] m_CapDirRot = new Matrix4x4[2] {Matrix4x4.identity, Matrix4x4.identity};

	#endregion

	#region properties ////////////////////////////////////////////////////////////////////////////////////////////////

	public static float DisplayTime
	{
		get { return m_DisplayTime; }
		set { m_DisplayTime = Mathf.Max(0.0f, value); }
	}

	public static bool DepthTest
	{
		get { return m_DepthTest; }
		set { m_DepthTest = value; }
	}

	#endregion

	#region methods ///////////////////////////////////////////////////////////////////////////////////////////////////

	//-----------------------------------------------------------------------------------------------------------------
	static DebugDraw()
	{
		Matrix.SetEulerAngles(ref m_CapDirRot[0], 0.0f, 90.0f*Mathf.Deg2Rad, 0.0f);
		Matrix.SetEulerAngles(ref m_CapDirRot[1], 90.0f*Mathf.Deg2Rad, 0.0f, 0.0f);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Box(Color Col, BoxCollider BoxColl, float Scale = 1.0f)
	{
		Matrix4x4 l2w = BoxColl.transform.localToWorldMatrix;
		Vector3 ext = BoxColl.size*Scale;

		Matrix.SetOrigin(ref l2w, l2w.MultiplyPoint3x4(BoxColl.center));

		Box(Col, ext.x, ext.y, ext.z, l2w);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Box(Color Col, Bounds AxisAlignedBox)
	{
		Box(Col, AxisAlignedBox.min, AxisAlignedBox.max);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Box(Color Col, Vector3 Min, Vector3 Max)
	{
		Vector3[] v = new Vector3[8];
		Vector3 c = (Max + Min)*0.5f;
		Vector3 e = (Max - Min);

		for (int i = 0; i < 8; ++i)
		{
			v[i] = c + Vector3.Scale(e, BoxCorner[i]);
		}

		Box(Col, v);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Box(Color Col, float Width, float Height, float Depth, Vector3 Center)
	{
		Vector3[] v = new Vector3[8];
		Vector3 e = new Vector3(Width, Height, Depth);

		for (int i = 0; i < 8; ++i)
		{
			v[i] = Center + Vector3.Scale(e, BoxCorner[i]);
		}

		Box(Col, v);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Box(Color Col, float Width, float Height, float Depth, Matrix4x4 Local2World)
	{
		Vector3[] v = new Vector3[8];
		Vector3 e = new Vector3(Width, Height, Depth);

		for (int i = 0; i < 8; ++i)
		{
			v[i] = Local2World.MultiplyPoint3x4(Vector3.Scale(e, BoxCorner[i]));
		}

		Box(Col, v);
	}

	//-----------------------------------------------------------------------------------------------------------------
	static void Box(Color Col, Vector3[] Corners)
	{
		Line(Col, Corners[0], Corners[1]);
		Line(Col, Corners[1], Corners[2]);
		Line(Col, Corners[2], Corners[3]);
		Line(Col, Corners[3], Corners[0]);

		Line(Col, Corners[4], Corners[5]);
		Line(Col, Corners[5], Corners[6]);
		Line(Col, Corners[6], Corners[7]);
		Line(Col, Corners[7], Corners[4]);

		Line(Col, Corners[0], Corners[4]);
		Line(Col, Corners[1], Corners[5]);
		Line(Col, Corners[2], Corners[6]);
		Line(Col, Corners[3], Corners[7]);

		//	GL.Begin( GL.LINES );
		//		
		//		GL.Color( Col );
		//		
		//		GL.Vertex( Corners[0] );   GL.Vertex( Corners[1] );
		//		GL.Vertex( Corners[1] );   GL.Vertex( Corners[2] );
		//		GL.Vertex( Corners[2] );   GL.Vertex( Corners[3] );
		//		GL.Vertex( Corners[3] );   GL.Vertex( Corners[0] );
		//		
		//		GL.Vertex( Corners[4] );   GL.Vertex( Corners[5] );
		//		GL.Vertex( Corners[5] );   GL.Vertex( Corners[6] );
		//		GL.Vertex( Corners[6] );   GL.Vertex( Corners[7] );
		//		GL.Vertex( Corners[7] );   GL.Vertex( Corners[4] );
		//		
		//		GL.Vertex( Corners[0] );   GL.Vertex( Corners[4] );
		//		GL.Vertex( Corners[1] );   GL.Vertex( Corners[5] );
		//		GL.Vertex( Corners[2] );   GL.Vertex( Corners[6] );
		//		GL.Vertex( Corners[3] );   GL.Vertex( Corners[7] );
		//		
		//	GL.End();
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Capsule(Color Col, CapsuleCollider CapColl, float Scale = 1.0f)
	{
		Matrix4x4 l2w = CapColl.transform.localToWorldMatrix;
		float height = Mathf.Max(0.0f, CapColl.height - 2.0f*CapColl.radius);

		Matrix.SetOrigin(ref l2w, l2w.MultiplyPoint3x4(CapColl.center));

		if (CapColl.direction != 2) // necessary only for dir X and Y
		{
			l2w *= m_CapDirRot[CapColl.direction];
		}

		Capsule(Col, CapColl.radius*Scale, height*Scale, l2w);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Capsule(Color Col, float Radius, Vector3 From, Vector3 To)
	{
		Vector3 x = new Vector3();
		Vector3 y = new Vector3();
		Vector3 z = To - From;
		float lng = Vector3.Magnitude(z);

		Vector3.OrthoNormalize(ref z, ref x, ref y);

		Vector3 o = From + z*(lng*0.5f);
		Matrix4x4 l2w = Matrix.Create(o, x, y, z);

		Capsule(Col, Radius, lng, l2w);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Capsule(Color Col, float Radius, float Height, Matrix4x4 Local2World)
	{
		int i;
		Vector3 v0, v1, u0, u1;
		float tmp0, tmp1;
		float height2 = Height*0.5f;

		// sin/cos table premultiplied by radius...

		SetDetails(Radius);

		for (i = 0; i < m_SegNum; ++i)
		{
			m_CosTable[i] = GetCos(i)*Radius;
			m_SinTable[i] = GetSin(i)*Radius;
		}

		// cylinder torso...

		TransformToWorld(out v0, Local2World, +Radius, 0.0f, +height2);
		TransformToWorld(out v1, Local2World, +Radius, 0.0f, -height2);

		Line(Col, v0, v1);

		TransformToWorld(out v0, Local2World, -Radius, 0.0f, +height2);
		TransformToWorld(out v1, Local2World, -Radius, 0.0f, -height2);

		Line(Col, v0, v1);

		TransformToWorld(out v0, Local2World, 0.0f, +Radius, +height2);
		TransformToWorld(out v1, Local2World, 0.0f, +Radius, -height2);

		Line(Col, v0, v1);

		TransformToWorld(out v0, Local2World, 0.0f, -Radius, +height2);
		TransformToWorld(out v1, Local2World, 0.0f, -Radius, -height2);

		Line(Col, v0, v1);

		// top/bottom cylinder circle...

		i = m_SegNum - 1;

		TransformToWorld(out u1, Local2World, m_CosTable[i], m_SinTable[i], -height2);
		TransformToWorld(out v1, Local2World, m_CosTable[i], m_SinTable[i], +height2);

		for (i = 0; i < m_SegNum; ++i)
		{
			u0 = u1;
			tmp0 = m_CosTable[i];
			v0 = v1;
			tmp1 = m_SinTable[i];

			TransformToWorld(out u1, Local2World, tmp0, tmp1, -height2);
			TransformToWorld(out v1, Local2World, tmp0, tmp1, +height2);

			Line(Col, u0, u1);
			Line(Col, v0, v1);
		}

		// top end-cap...

		int num = m_SegNum/2;

		tmp0 = m_CosTable[0];
		tmp1 = m_SinTable[0] + height2;

		TransformToWorld(out u1, Local2World, 0.0f, tmp0, tmp1);
		TransformToWorld(out v1, Local2World, tmp0, 0.0f, tmp1);

		for (i = 1; i <= num; ++i)
		{
			u0 = u1;
			tmp0 = m_CosTable[i];
			v0 = v1;
			tmp1 = m_SinTable[i] + height2;

			TransformToWorld(out u1, Local2World, 0.0f, tmp0, tmp1);
			TransformToWorld(out v1, Local2World, tmp0, 0.0f, tmp1);

			Line(Col, u0, u1);
			Line(Col, v0, v1);
		}

		// bottom end-cap...

		tmp0 = m_CosTable[0];
		tmp1 = -m_SinTable[0] - height2;

		TransformToWorld(out u1, Local2World, 0.0f, tmp0, tmp1);
		TransformToWorld(out v1, Local2World, tmp0, 0.0f, tmp1);

		for (i = 1; i <= num; ++i)
		{
			u0 = u1;
			tmp0 = m_CosTable[i];
			v0 = v1;
			tmp1 = -m_SinTable[i] - height2;

			TransformToWorld(out u1, Local2World, 0.0f, tmp0, tmp1);
			TransformToWorld(out v1, Local2World, tmp0, 0.0f, tmp1);

			Line(Col, u0, u1);
			Line(Col, v0, v1);
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Collider(Color Col, Collider Coll, float Scale = 1.0f)
	{
		BoxCollider box = Coll as BoxCollider;

		if (box != null)
		{
			Box(Col, box, Scale);
			return;
		}

		CapsuleCollider capsule = Coll as CapsuleCollider;

		if (capsule != null)
		{
			Capsule(Col, capsule, Scale);
			return;
		}

		SphereCollider sphere = Coll as SphereCollider;

		if (sphere != null)
		{
			Sphere(Col, sphere, Scale);
			return;
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void CoordSystem(float Size, Matrix4x4 Local2World)
	{
		Vector3 x = Matrix.GetAxisX(Local2World);
		Vector3 y = Matrix.GetAxisY(Local2World);
		Vector3 z = Matrix.GetAxisZ(Local2World);
		Vector3 o = Matrix.GetOrigin(Local2World);

		x *= Size;
		y *= Size;
		z *= Size;

		Line(Color.red, o, o + x);
		Line(Color.green, o, o + y);
		Line(Color.blue, o, o + z);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Diamond(Color Col, float Size, Vector3 Center)
	{
		Vector3[] v = new Vector3[6];

		for (int i = 0; i < 6; ++i)
		{
			v[i] = Center + Size*DiamondCorner[i];
		}

		Line(Col, v[0], v[2]);
		Line(Col, v[1], v[3]);
		Line(Col, v[2], v[1]);
		Line(Col, v[3], v[0]);

		Line(Col, v[4], v[0]);
		Line(Col, v[4], v[1]);
		Line(Col, v[4], v[2]);
		Line(Col, v[4], v[3]);

		Line(Col, v[5], v[0]);
		Line(Col, v[5], v[1]);
		Line(Col, v[5], v[2]);
		Line(Col, v[5], v[3]);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Line(Color Col, Vector3 From, Vector3 To)
	{
		Debug.DrawLine(From, To, Col, m_DisplayTime, m_DepthTest);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Line(Color Col, Vector3 From, Vector3 To, Matrix4x4 Local2World)
	{
		Line(Col, Local2World.MultiplyPoint3x4(From), Local2World.MultiplyPoint3x4(To));
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void LineOriented(Color Col, Vector3 From, Vector3 To, float ArrowSize = 0.1f)
	{
		Vector3 a;
		Vector3 u = new Vector3();
		Vector3 v = new Vector3();
		Vector3 dir = To - From;

		Vector3.OrthoNormalize(ref dir, ref u, ref v);

		a = To - dir*ArrowSize;
		u *= 0.4f*ArrowSize;
		v *= 0.4f*ArrowSize;

		Line(Col, To, From);
		Line(Col, To, a + u);
		Line(Col, To, a - u);
		Line(Col, To, a + v);
		Line(Col, To, a - v);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void LineOriented(Color Col, Vector3 From, Vector3 To, Matrix4x4 Local2World)
	{
		LineOriented(Col, Local2World.MultiplyPoint3x4(From), Local2World.MultiplyPoint3x4(To));
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Sphere(Color Col, SphereCollider SphColl, float Scale = 1.0f)
	{
		Matrix4x4 l2w = SphColl.transform.localToWorldMatrix;

		Matrix.SetOrigin(ref l2w, l2w.MultiplyPoint3x4(SphColl.center));

		Sphere(Col, SphColl.radius*Scale, l2w);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Sphere(Color Col, float Radius, Vector3 Center)
	{
		Sphere(Col, Radius, Matrix.CreateTranslation(Center));
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Sphere(Color Col, float Radius, Matrix4x4 Local2World)
	{
		Vector3 a, aPrev;
		Vector3 b, bPrev;
		Vector3 c, cPrev;
		Matrix4x4 mat = Local2World*Matrix.CreateScale(Radius);

		SetDetails(Radius);

		float iCos = GetCos(0);
		float iSin = GetSin(0);

		TransformToWorld(out a, mat, iCos, iSin, 0.0f);
		TransformToWorld(out b, mat, iCos, 0.0f, iSin);
		TransformToWorld(out c, mat, 0.0f, iCos, iSin);

		for (int i = m_SegNum; i >= 0; --i)
		{
			aPrev = a;
			bPrev = b;
			cPrev = c;
			iCos = GetCos(i);
			iSin = GetSin(i);

			TransformToWorld(out a, mat, iCos, iSin, 0.0f);
			TransformToWorld(out b, mat, iCos, 0.0f, iSin);
			TransformToWorld(out c, mat, 0.0f, iCos, iSin);

			Line(Col, a, aPrev);
			Line(Col, b, bPrev);
			Line(Col, c, cPrev);
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	static void SetDetails(float Radius)
	{
		m_SegNum = Radius < 0.05f ? MinDetails : (Radius < 0.3f ? MidDetails : MaxDetails);
		m_AngleStep = (2.0f*Mathf.PI)/m_SegNum;
	}

	//-----------------------------------------------------------------------------------------------------------------
	static float GetSin(int Idx)
	{
		return Mathf.Sin(m_AngleStep*Idx);
	}

	//-----------------------------------------------------------------------------------------------------------------
	static float GetCos(int Idx)
	{
		return Mathf.Cos(m_AngleStep*Idx);
	}

	//-----------------------------------------------------------------------------------------------------------------
	static void TransformToWorld(out Vector3 Res, Matrix4x4 Mat, float X, float Y, float Z)
	{
		Res.x = X*Mat.m00 + Y*Mat.m01 + Z*Mat.m02 + Mat.m03;
		Res.y = X*Mat.m10 + Y*Mat.m11 + Z*Mat.m12 + Mat.m13;
		Res.z = X*Mat.m20 + Y*Mat.m21 + Z*Mat.m22 + Mat.m23;
	}

	#endregion
}

//#####################################################################################################################
/*

public class SphereCreator
{
	#region structures ////////////////////////////////////////////////////////////////////////////////////////////////
	
	private struct Triangle
	{
		public   int   m_Idx0;
		public   int   m_Idx1;
		public   int   m_Idx2;
		
		public Triangle( int Idx0, int Idx1, int Idx2 )
		{
			m_Idx0 = Idx0;   m_Idx1 = Idx1;   m_Idx2 = Idx2;
		}
	}
	
	#endregion
	#region fields ////////////////////////////////////////////////////////////////////////////////////////////////////
	
	//
	private   float                     m_Radius;
	//
	private   List< Triangle >          m_Triangles = new List< Triangle >();
	//
	private   List< Vector3 >           m_Vertices = new List< Vector3 >();
	//
	private   Dictionary< long, int >   m_MiddlePointIndexCache = new Dictionary< long, int >();
	
	#endregion
	#region methods ///////////////////////////////////////////////////////////////////////////////////////////////////
	
	//-----------------------------------------------------------------------------------------------------------------
	public void Create( int RecursionLevel, float Radius )
	{
		m_Triangles.Clear();
		m_Vertices.Clear();
		m_MiddlePointIndexCache.Clear();
		
		m_Radius = Radius;
		
	//	InitAsHexahedron();
		InitAsIcosahedron();
	//	InitAsOctahedron();
		
		Refine( RecursionLevel );
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	public void Display( Color Col )
	{
		foreach ( Triangle tri in m_Triangles )
		{
			DebugDraw.Line( Col, m_Vertices[tri.m_Idx0], m_Vertices[tri.m_Idx1] );
			DebugDraw.Line( Col, m_Vertices[tri.m_Idx1], m_Vertices[tri.m_Idx2] );
			DebugDraw.Line( Col, m_Vertices[tri.m_Idx2], m_Vertices[tri.m_Idx0] );
		}
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	private void InitAsHexahedron() // cube
	{
		AddVertex( new Vector3( -1.0f, -1.0f, -1.0f ) );
		AddVertex( new Vector3(  1.0f, -1.0f, -1.0f ) );
		AddVertex( new Vector3(  1.0f, -1.0f,  1.0f ) );
		AddVertex( new Vector3( -1.0f, -1.0f,  1.0f ) );
		AddVertex( new Vector3( -1.0f,  1.0f, -1.0f ) );
		AddVertex( new Vector3(  1.0f,  1.0f, -1.0f ) );
		AddVertex( new Vector3(  1.0f,  1.0f,  1.0f ) );
		AddVertex( new Vector3( -1.0f,  1.0f,  1.0f ) );
		
		m_Triangles.Add( new Triangle( 1, 2, 3 ) );
		m_Triangles.Add( new Triangle( 3, 0, 1 ) );
		m_Triangles.Add( new Triangle( 0, 1, 5 ) );
		m_Triangles.Add( new Triangle( 5, 4, 0 ) );
		m_Triangles.Add( new Triangle( 1, 2, 6 ) );
		m_Triangles.Add( new Triangle( 6, 5, 1 ) );
		m_Triangles.Add( new Triangle( 2, 3, 7 ) );
		m_Triangles.Add( new Triangle( 7, 6, 2 ) );
		m_Triangles.Add( new Triangle( 3, 7, 4 ) );
		m_Triangles.Add( new Triangle( 4, 0, 3 ) );
		m_Triangles.Add( new Triangle( 4, 5, 6 ) );
		m_Triangles.Add( new Triangle( 6, 7, 4 ) );
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	private void InitAsOctahedron()
	{
		float  a = 1.0f / ( 2.0f * Mathf.Sqrt( 2.0f ) );
		float  b = 1.0f / ( 2.0f );
		
		AddVertex( new Vector3( 0.0f,    b, 0.0f ) );
		AddVertex( new Vector3(   -a, 0.0f,   -a ) );
		AddVertex( new Vector3(    a, 0.0f,   -a ) );
		AddVertex( new Vector3(    a, 0.0f,    a ) );
		AddVertex( new Vector3(   -a, 0.0f,    a ) );
		AddVertex( new Vector3( 0.0f,   -b, 0.0f ) );
		
		m_Triangles.Add( new Triangle( 0, 1, 2 ) );
		m_Triangles.Add( new Triangle( 0, 2, 3 ) );
		m_Triangles.Add( new Triangle( 0, 3, 4 ) );
		m_Triangles.Add( new Triangle( 0, 4, 1 ) );
		m_Triangles.Add( new Triangle( 5, 2, 1 ) );
		m_Triangles.Add( new Triangle( 5, 3, 2 ) );
		m_Triangles.Add( new Triangle( 5, 4, 3 ) );
		m_Triangles.Add( new Triangle( 5, 1, 4 ) );
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	private void InitAsIcosahedron()
	{
	//	vertices[ 0].x =  0.0f;
	//	vertices[ 0].y =  0.0f;
	//	vertices[ 0].z = +1.0f;
	//	vertices[11].x =  0.0f;
	//	vertices[11].y =  0.0f;
	//	vertices[11].z = -1.0f;
	//
	//	float  inc = Mathf.Deg2Rad * 72.0f;
	//	float  phi = Mathf.Deg2Rad * 26.56505f;
	//
	//	float  a    = 0.0f;
	//	float  tmp0 = Mathf.Sin( phi );
	//	float  tmp1 = Mathf.Cos( phi );
	//
	//	for ( i = 1; i < 6; ++i, a += inc )
	//	{
	//		vertices[i].x = Mathf.Cos( a ) * tmp1;
	//		vertices[i].y = Mathf.Sin( a ) * tmp1;
	//		vertices[i].z = tmp0;
	//	}
	//
	//	a    = Mathf.Deg2Rad * 36.0f;
	//	tmp0 = Mathf.Sin( -phi );
	//	tmp1 = Mathf.Cos( -phi );
	//
	//	for ( i = 6; i < 11; ++i, a += inc )
	//	{
	//		vertices[i].x = Mathf.Cos( a ) * tmp1;
	//		vertices[i].y = Mathf.Sin( a ) * tmp1;
	//		vertices[i].z = tmp0;
	//	}
		
		AddVertex( new Vector3(  0.000f,  1.000f,  0.000f ) );
		AddVertex( new Vector3(  0.894f,  0.447f,  0.000f ) );
		AddVertex( new Vector3(  0.276f,  0.447f,  0.851f ) );
		AddVertex( new Vector3( -0.724f,  0.447f,  0.526f ) );
		AddVertex( new Vector3( -0.724f,  0.447f, -0.526f ) );
		AddVertex( new Vector3(  0.276f,  0.447f, -0.851f ) );
		AddVertex( new Vector3(  0.724f, -0.447f,  0.526f ) );
		AddVertex( new Vector3( -0.276f, -0.447f,  0.851f ) );
		AddVertex( new Vector3( -0.894f, -0.447f,  0.000f ) );
		AddVertex( new Vector3( -0.276f, -0.447f, -0.851f ) );
		AddVertex( new Vector3(  0.724f, -0.447f, -0.526f ) );
		AddVertex( new Vector3(  0.000f, -1.000f,  0.000f ) );
		
		m_Triangles.Add( new Triangle(  0,  1,  2 ) );
		m_Triangles.Add( new Triangle(  0,  2,  3 ) );
		m_Triangles.Add( new Triangle(  0,  3,  4 ) );
		m_Triangles.Add( new Triangle(  0,  4,  5 ) );
		m_Triangles.Add( new Triangle(  0,  5,  1 ) );
		m_Triangles.Add( new Triangle( 11,  6,  7 ) );
		m_Triangles.Add( new Triangle( 11,  7,  8 ) );
		m_Triangles.Add( new Triangle( 11,  8,  9 ) );
		m_Triangles.Add( new Triangle( 11,  9, 10 ) );
		m_Triangles.Add( new Triangle( 11, 10,  6 ) );
		m_Triangles.Add( new Triangle(  1,  2,  6 ) );
		m_Triangles.Add( new Triangle(  2,  3,  7 ) );
		m_Triangles.Add( new Triangle(  3,  4,  8 ) );
		m_Triangles.Add( new Triangle(  4,  5,  9 ) );
		m_Triangles.Add( new Triangle(  5,  1, 10 ) );
		m_Triangles.Add( new Triangle(  6,  7,  2 ) );
		m_Triangles.Add( new Triangle(  7,  8,  3 ) );
		m_Triangles.Add( new Triangle(  8,  9,  4 ) );
		m_Triangles.Add( new Triangle(  9, 10,  5 ) );
		m_Triangles.Add( new Triangle( 10,  6,  1 ) );
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	private void Refine( int RecursionLevel )
	{
		// T(i+1) = 4 * T(i)      ...  T(n) = T(0) * 4 ^ n  ...  4^n = 1 << ( 2 * n )
		// V(i+1) = 4 * V(i) - 6  ...  V(n) = T(n) / 2 + 2
		
		int  tNum = m_Triangles.Count * ( 1 << ( 2 * RecursionLevel ) );
		int  vNum = tNum / 2 + 2;
		
		m_Vertices.Capacity  = vNum;
		m_Triangles.Capacity = tNum;
		
		List< Triangle > temp = new List< Triangle >( tNum );
		
		for ( int i = 0; i < RecursionLevel; ++i )
		{
			temp.Clear();
			
			foreach ( Triangle t in m_Triangles )
			{
				int  midA = GetMiddleVertexIndex( t.m_Idx0, t.m_Idx1 );
				int  midB = GetMiddleVertexIndex( t.m_Idx1, t.m_Idx2 );
				int  midC = GetMiddleVertexIndex( t.m_Idx2, t.m_Idx0 );
				
				temp.Add( new Triangle( t.m_Idx0, midA, midC ) );
				temp.Add( new Triangle( t.m_Idx1, midB, midA ) );
				temp.Add( new Triangle( t.m_Idx2, midC, midB ) );
				temp.Add( new Triangle(     midA, midB, midC ) );
			}
			
			MiscUtils.Swap( ref temp, ref m_Triangles );
		}
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	private int AddVertex( Vector3 V )
	{
		int    idx = m_Vertices.Count;
		float  tmp = m_Radius / Vector3.Magnitude( V );
		
		m_Vertices.Add( V * tmp );
		
		return idx;
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	private int GetMiddleVertexIndex( int Idx0, int Idx1 )
	{
		// try to find between already exiting...
		
		MiscUtils.Sort( ref Idx0, ref Idx1 );
		
		int   idx;
		long  key = ((long)Idx0 << 32) + Idx1;
		
		if ( m_MiddlePointIndexCache.TryGetValue( key, out idx ) )
		{
			return idx;
		}
		
		// not found --> create a new one...
		
		Vector3  v0  = m_Vertices[ Idx0 ];
		Vector3  v1  = m_Vertices[ Idx1 ];
		Vector3  mid = ( v0 + v1 ) / 2.0f;
		
		idx = AddVertex( mid );
		
		m_MiddlePointIndexCache.Add( key, idx );
		
		return idx;
	}
	
	#endregion
}

/**/
