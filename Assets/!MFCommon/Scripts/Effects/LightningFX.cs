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

public class LightningFX : MonoBehaviour
{
	struct S_EmitPlaneInfo
	{
		public Vector3 m_Base;
		public Vector3 m_AxisU;
		public Vector3 m_AxisV;
	};

	public Vector3 m_Extents = new Vector3(3, 3, 3);
	public bool m_EmitPlaneX = true;
	public bool m_EmitPlaneY = false;
	public bool m_EmitPlaneZ = false;
	public bool m_EmitParallelLines = true;
	public int m_MaxLines = 20;
	public Vector4 m_NoiseAmplitudes = new Vector4(2, 1, 0.5f, 0.125f);
	public Vector4 m_NoiseFrequencies = new Vector4(4, 8, 16, 32);
	public Vector4 m_NoiseSpeeds = new Vector4(3.2f, 2.3f, 0.5f, 1);
	public Vector2 m_Amplitude = new Vector2(0.2f, 0.01f);
	public Vector2 m_DurationOnOff = new Vector2(0.1f, 2);
	public float m_LinesWidth = 0.2f;
	public float m_InvWaveSpeed = 0.025f;
	public Color m_Color = new Color(102.0f/255, 127.0f/255, 253.0f/255, 1);

	MeshFilter m_MeshFilter;
	MeshRenderer m_MeshRenderer;
	Mesh m_Mesh;
	Material m_Material;

	const float m_MinLineLength = 0.5f;
	const int m_MaxLinePtSearchIters = 32;
	const int m_MaxLineSegments = 32;

	S_EmitPlaneInfo[] m_EmitPlanes = new S_EmitPlaneInfo[6];
	int m_NumEmitPlanes = 0;
	int m_NumEmitAxes = 0;

	void Awake()
	{
		m_Material = Resources.Load("effects/m_lightning_bolt", typeof (Material)) as Material;

		if (m_Material)
		{
			m_Material = Instantiate(m_Material) as Material;
		}

		if (!m_Material)
		{
			Debug.LogError("Cannot load lighting bolt material");
		}

		MFDebugUtils.Assert(m_Material);

		InitMeshes();

		Generate();

		if (m_Material)
		{
			SetMaterialParams();
		}
	}

	bool InitMeshes()
	{
		gameObject.AddComponent<MeshFilter>();
		gameObject.AddComponent<MeshRenderer>();

		m_MeshFilter = (MeshFilter)GetComponent(typeof (MeshFilter));
		m_MeshRenderer = (MeshRenderer)GetComponent(typeof (MeshRenderer));

		m_MeshRenderer.GetComponent<Renderer>().material = m_Material;
		m_MeshRenderer.GetComponent<Renderer>().enabled = true;
		m_MeshRenderer.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		m_MeshRenderer.GetComponent<Renderer>().receiveShadows = false;

		m_Mesh = m_MeshFilter.mesh;

		return true;
	}

	void InitInternalMeshBuffers(int numLines)
	{
		Vector3[] verts = new Vector3[numLines*m_MaxLineSegments*4];
		Vector3[] normals = new Vector3[numLines*m_MaxLineSegments*4];
		Vector2[] uv = new Vector2[numLines*m_MaxLineSegments*4];
		Vector2[] uv2 = new Vector2[numLines*m_MaxLineSegments*4];
		int[] tris = new int[numLines*m_MaxLineSegments*2*3];
		float invNSeg = 1.0f/m_MaxLineSegments;

		for (int i = 0; i < numLines; i++)
		{
			float instanceId = i;

			for (int j = 0; j < m_MaxLineSegments; j++)
			{
				int currVOffs = (i*m_MaxLineSegments + j)*4;
				int currIOffs = (i*m_MaxLineSegments + j)*6;

				uv[currVOffs].x = 0;
				uv[currVOffs].y = 1;

				uv[currVOffs + 1].x = 0;
				uv[currVOffs + 1].y = -1;

				uv[currVOffs + 2].x = 0;
				uv[currVOffs + 2].y = 1;

				uv[currVOffs + 3].x = 0;
				uv[currVOffs + 3].y = -1;

				uv2[currVOffs].x = instanceId;
				uv2[currVOffs].y = j*invNSeg;

				uv2[currVOffs + 1].x = instanceId;
				uv2[currVOffs + 1].y = j*invNSeg;

				uv2[currVOffs + 2].x = instanceId;
				uv2[currVOffs + 2].y = (j + 1)*invNSeg;

				uv2[currVOffs + 3].x = instanceId;
				uv2[currVOffs + 3].y = (j + 1)*invNSeg;

				tris[currIOffs] = currVOffs;
				tris[currIOffs + 1] = currVOffs + 1;
				tris[currIOffs + 2] = currVOffs + 2;
				tris[currIOffs + 3] = currVOffs + 2;
				tris[currIOffs + 4] = currVOffs + 1;
				tris[currIOffs + 5] = currVOffs + 3;
			}
		}

		for (int i = 0; i < verts.Length; i++)
		{
			verts[i] = Vector3.zero;
			normals[i] = Vector3.zero;
		}

		m_Mesh.Clear();
		m_Mesh.vertices = verts;
		m_Mesh.normals = normals;
		m_Mesh.uv = uv;
		m_Mesh.uv2 = uv2;
		m_Mesh.triangles = tris;
	}

	void SetupLineSegmentLinear(int idx, int startSeg, int numSegments, Vector3 startPt, Vector3 endPt)
	{
		Vector3[] verts = m_Mesh.vertices;
		Vector3[] normals = m_Mesh.normals;
		Vector2[] uv = m_Mesh.uv;
		int segOffs = idx*m_MaxLineSegments*4;
		int endSeg = startSeg + numSegments;

		MFDebugUtils.Assert(idx < m_MaxLines);
		MFDebugUtils.Assert(numSegments > 0);

		for (int i = 0; i < startSeg; i++)
		{
			int offs = segOffs + i*4;

			verts[offs++] = startPt;
			verts[offs++] = startPt;
			verts[offs++] = startPt;
			verts[offs++] = startPt;
		}

		Vector3 segDiff = (endPt - startPt)/numSegments;
		Vector3 dir = segDiff;

		Vector3 currSegStart = startPt;
		Vector3 currSegEnd = startPt + segDiff;
		float currU = 0;
//		float	du				= 1.0f / numSegments;

		for (int i = startSeg; i < endSeg; i++)
		{
			int offs = segOffs + i*4;

			verts[offs] = currSegStart;
			uv[offs].x = currU;
			normals[offs++] = dir;

			verts[offs] = currSegStart;
			uv[offs].x = currU;
			normals[offs++] = dir;

//			currU += du;
			currU += (currSegEnd - currSegStart).magnitude;

			verts[offs] = currSegEnd;
			uv[offs].x = currU;
			normals[offs++] = dir;

			verts[offs] = currSegEnd;
			uv[offs].x = currU;
			normals[offs++] = dir;

			currSegStart = currSegEnd;
			currSegEnd = currSegEnd + segDiff;
		}

		for (int i = endSeg; i < m_MaxLineSegments; i++)
		{
			int offs = segOffs + i*4;

			verts[offs] = endPt;
		}

		m_Mesh.vertices = verts;
		m_Mesh.normals = normals;
		m_Mesh.uv = uv;
	}

	void SetupLineSegmentRadial(int idx, Vector3 center, float radius, float angle, Matrix4x4 m)
	{
		int numSegments = m_MaxLineSegments;
		Vector3[] pts = new Vector3[numSegments + 1];
		float dAngle = angle/numSegments;

		MFDebugUtils.Assert(idx < m_MaxLines);

		for (int i = 0; i < pts.Length; i++)
		{
			Vector4 currPt;

			currPt.x = radius*Mathf.Sin(i*dAngle);
			currPt.y = 0;
			currPt.z = radius*Mathf.Cos(i*dAngle);
			currPt.w = 1;

			pts[i] = m*currPt;
		}

		Vector3[] verts = m_Mesh.vertices;
		Vector3[] normals = m_Mesh.normals;
		Vector2[] uv = m_Mesh.uv;
		int segOffs = idx*m_MaxLineSegments*4;
		float currU = 0;
		float du = 1.0f/numSegments;

		for (int i = 0; i < numSegments; i++)
		{
			int offs = segOffs + i*4;
			Vector3 dir = pts[i + 1] - pts[i];

			verts[offs] = pts[i];
			uv[offs].x = currU;
			normals[offs++] = dir;

			verts[offs] = pts[i];
			uv[offs].x = currU;
			normals[offs++] = dir;

			currU += du;

			dir = pts[(i + 2)%pts.Length] - pts[i + 1];

			verts[offs] = pts[i + 1];
			uv[offs].x = currU;
			normals[offs++] = dir;

			verts[offs] = pts[i + 1];
			uv[offs].x = currU;
			normals[offs++] = dir;
		}

		m_Mesh.vertices = verts;
		m_Mesh.normals = normals;
		m_Mesh.uv = uv;
	}

	void InitEmitPlanes()
	{
		Vector3 ext = m_Extents*0.5f;

		m_NumEmitPlanes = 0;
		m_NumEmitAxes = 0;

		if (m_EmitPlaneX)
		{
			m_EmitPlanes[m_NumEmitPlanes].m_Base = new Vector3(ext.x, 0, 0);
			m_EmitPlanes[m_NumEmitPlanes].m_AxisU = new Vector3(0, 0, 1)*ext.z;
			m_EmitPlanes[m_NumEmitPlanes].m_AxisV = new Vector3(0, 1, 0)*ext.y;

			m_NumEmitPlanes++;

			m_EmitPlanes[m_NumEmitPlanes].m_Base = new Vector3(-ext.x, 0, 0);
			m_EmitPlanes[m_NumEmitPlanes].m_AxisU = new Vector3(0, 0, 1)*ext.z;
			m_EmitPlanes[m_NumEmitPlanes].m_AxisV = new Vector3(0, 1, 0)*ext.y;

			m_NumEmitPlanes++;
			m_NumEmitAxes++;
		}

		if (m_EmitPlaneY)
		{
			m_EmitPlanes[m_NumEmitPlanes].m_Base = new Vector3(0, ext.y, 0);
			m_EmitPlanes[m_NumEmitPlanes].m_AxisU = new Vector3(1, 0, 0)*ext.x;
			m_EmitPlanes[m_NumEmitPlanes].m_AxisV = new Vector3(0, 0, 1)*ext.z;

			m_NumEmitPlanes++;

			m_EmitPlanes[m_NumEmitPlanes].m_Base = new Vector3(0, -ext.y, 0);
			m_EmitPlanes[m_NumEmitPlanes].m_AxisU = new Vector3(1, 0, 0)*ext.x;
			m_EmitPlanes[m_NumEmitPlanes].m_AxisV = new Vector3(0, 0, 1)*ext.z;

			m_NumEmitPlanes++;
			m_NumEmitAxes++;
		}

		if (m_EmitPlaneZ)
		{
			m_EmitPlanes[m_NumEmitPlanes].m_Base = new Vector3(0, 0, ext.z);
			m_EmitPlanes[m_NumEmitPlanes].m_AxisU = new Vector3(1, 0, 0)*ext.x;
			m_EmitPlanes[m_NumEmitPlanes].m_AxisV = new Vector3(0, 1, 0)*ext.y;

			m_NumEmitPlanes++;

			m_EmitPlanes[m_NumEmitPlanes].m_Base = new Vector3(0, 0, -ext.z);
			m_EmitPlanes[m_NumEmitPlanes].m_AxisU = new Vector3(1, 0, 0)*ext.x;
			m_EmitPlanes[m_NumEmitPlanes].m_AxisV = new Vector3(0, 1, 0)*ext.y;

			m_NumEmitPlanes++;
			m_NumEmitAxes++;
		}
	}

	void GeneratePts(out Vector3 pt0, out Vector3 pt1)
	{
		if (m_NumEmitPlanes > 0)
		{
			int i = (Random.Range(0, 9999)%m_NumEmitAxes)*2;
			float u = Random.Range(-1.0f, 1.0f);
			float v = Random.Range(-1.0f, 1.0f);

			pt0 = m_EmitPlanes[i].m_Base + m_EmitPlanes[i].m_AxisU*u + m_EmitPlanes[i].m_AxisV*v;

			if (!m_EmitParallelLines)
			{
				u = Random.Range(-1.0f, 1.0f);
				v = Random.Range(-1.0f, 1.0f);
			}

			i++;

			pt1 = m_EmitPlanes[i].m_Base + m_EmitPlanes[i].m_AxisU*u + m_EmitPlanes[i].m_AxisV*v;
		}
		else
		{
			pt0 = Vector3.zero;
			pt1 = Vector3.zero;
		}
	}

	void Generate()
	{
		int numLines = m_MaxLines;

		InitEmitPlanes();
		InitInternalMeshBuffers(numLines);

		for (int i = 0; i < numLines; i++)
		{
			Vector3 startPt;
			Vector3 endPt;

			GeneratePts(out startPt, out endPt);

			SetupLineSegmentLinear(i, 0, m_MaxLineSegments, startPt, endPt);
		}

		m_Mesh.RecalculateBounds();
	}

	void SetMaterialParams()
	{
		Vector4 duration = Vector4.zero;
		Vector4 amplitude = Vector4.zero;
		Vector4 otherParams = Vector4.zero;

		duration.x = m_DurationOnOff.x;
		duration.y = m_DurationOnOff.y;

		amplitude.x = m_Amplitude.x;
		amplitude.y = m_Amplitude.y;

		otherParams.x = m_LinesWidth;
		otherParams.y = m_InvWaveSpeed;
		otherParams.z = (float)Screen.width/Screen.height;

		m_Material.SetVector("_Duration", duration);
		m_Material.SetVector("_Amplitude", amplitude);
		m_Material.SetVector("_NoiseFreqs", m_NoiseFrequencies);
		m_Material.SetVector("_NoiseSpeeds", m_NoiseSpeeds);
		m_Material.SetVector("_NoiseAmps", m_NoiseAmplitudes);
		m_Material.SetVector("_OtherParams", otherParams);
		m_Material.SetColor("_Color", m_Color);
	}

	void LateUpdate()
	{
		if (m_Material)
		{
			SetMaterialParams();
		}
	}

	void OnDrawGizmos()
	{
		Matrix4x4 m = transform.localToWorldMatrix;

		Gizmos.color = Color.green;
		Gizmos.matrix = m;

		Gizmos.DrawWireCube(Vector3.zero, m_Extents);
	}

	void OnEnable()
	{
		if (m_MeshRenderer)
		{
			m_MeshRenderer.enabled = true;
		}
	}

	void OnDisable()
	{
		if (m_MeshRenderer)
		{
			m_MeshRenderer.enabled = false;
		}
	}
}
