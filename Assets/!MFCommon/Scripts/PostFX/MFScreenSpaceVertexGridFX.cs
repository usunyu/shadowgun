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
using LRUCache;

[AddComponentMenu("Image Effects/MADFINGER screen space vertex grid FX")]
public class MFScreenSpaceVertexGridFX : ImageEffectBase
{
	const int MAX_GLOWS = 4;

	public struct S_CacheRec
	{
		public bool m_IsBlocked;
		public float m_QueryTime;
	};

	struct S_Flashbang
	{
		public Color m_Color;
		public float m_StartTime;
		public float m_Intensity;
		public float m_Duration;
	};

	int m_ScreenGridXRes = 30;
	int m_ScreenGridYRes = 25;
	MeshFilter m_MeshFilter;
	MeshRenderer m_MeshRenderer;
	Mesh m_Mesh;
	bool m_InitOK = false;
	GameObject m_GameObj = null;
	ScreenSpaceGlowEmitter[] m_ActiveGlows = new ScreenSpaceGlowEmitter[4];
	int m_NumActiveGlows = 0;
	Vector4 m_GlowsIntensityMask;
	public float m_DirFadeoutStrength = 80;
	public float m_MaxVisQueryResultAge = 0.25f;
	public float m_FlashbangPeakTime = 0.15f;
	public float m_FlashbangDuration = 6.0f;
	List<S_Flashbang> m_Flashbangs = new List<S_Flashbang>();
	LRUCache<int, S_CacheRec> m_GlowsVisCache;
	bool m_PrevDbgBtnState = false;
	public static MFScreenSpaceVertexGridFX Instance = null;

	void Awake()
	{
		Instance = this;

		m_GlowsVisCache = new LRUCache<int, S_CacheRec>(128);
		m_InitOK = DoInit();
	}

	void OnDestroy()
	{
		Instance = null;
	}

	public void InternalUpdate()
	{
		SelectActiveGlows();

//		DbgSpawnFlashbang();
	}

	public int NumActiveGlows()
	{
		return m_NumActiveGlows;
	}

	public bool AnyEffectActive()
	{
		return m_NumActiveGlows > 0 || m_Flashbangs.Count > 0;
	}

	void SelectActiveGlows()
	{
		m_NumActiveGlows = 0;

		if (Camera.main == null)
		{
			return;
		}

		Camera cam = Camera.main;
		Vector3 camPos = cam.transform.position;
		S_CacheRec visRec = new S_CacheRec();
		float currTime = Time.time;

		foreach (ScreenSpaceGlowEmitter curr in ScreenSpaceGlowEmitter.ms_Instances)
		{
			Vector3 glowPos = curr.transform.position;

			if (Vector3.Distance(camPos, glowPos) > curr.m_MaxVisDist)
			{
				continue;
			}

			Vector3 viewportPos = cam.WorldToViewportPoint(glowPos);

			if (viewportPos.z < 0)
			{
				continue;
			}

			if (viewportPos.x < 0 || viewportPos.x > 1 ||
				viewportPos.y < 0 || viewportPos.y > 1)
			{
				continue;
			}

			if (m_NumActiveGlows >= MAX_GLOWS)
			{
				return;
			}

			bool isBlocked = false;
			bool updateVisState = true;

			if (m_GlowsVisCache.get(curr.m_InstanceID, ref visRec))
			{
				float age = currTime - visRec.m_QueryTime;

				MFDebugUtils.Assert(age >= 0.0f);

				if (age <= m_MaxVisQueryResultAge)
				{
					isBlocked = visRec.m_IsBlocked;
					updateVisState = false;
				}
			}

			if (updateVisState)
			{
				Vector3 dir = glowPos - camPos;

				isBlocked = Physics.Raycast(camPos, dir, 1, curr.m_ColLayerMask);

				visRec.m_IsBlocked = isBlocked;
				visRec.m_QueryTime = currTime;

				m_GlowsVisCache.add(curr.m_InstanceID, visRec);
			}

			if (!isBlocked)
			{
				Vector3 toViewer = camPos - glowPos;
				Vector3 ldir = curr.transform.forward;
				float dist = toViewer.magnitude;

				toViewer.Normalize();

				float dirFadeout = RemapValue(Mathf.Clamp01(Vector3.Dot(toViewer, ldir)), Mathf.Cos(curr.m_ConeAngle*Mathf.Deg2Rad/2), 1, 0, 1);
				float ndist = Mathf.Clamp01(dist/curr.m_MaxVisDist);
				float intensityMod = dirFadeout*(1 - ndist*ndist);

				//Debug.Log(intensityMod);

				if (intensityMod > 0.001f)
				{
					m_ActiveGlows[m_NumActiveGlows++] = curr;
				}
			}
		}
	}

	void SetGlowShaderParams(int glowIdx, ScreenSpaceGlowEmitter glowInfo, Vector3 camDir, Vector3 camPos)
	{
		Vector3 ldir = glowInfo.transform.forward;
		Vector3 lpos = glowInfo.transform.position;
		Vector4 paramSet0 = lpos;
		Vector4 paramSet1 = glowInfo.m_Color*glowInfo.m_Intensity*Mathf.Pow(Mathf.Clamp01(Vector3.Dot(-camDir, ldir)), m_DirFadeoutStrength);
		Matrix4x4 glowParams = Matrix4x4.zero;

		Vector3 toViewer = camPos - lpos;
		float dist = toViewer.magnitude;

		toViewer.Normalize();

		float dirFadeout = RemapValue(Mathf.Clamp01(Vector3.Dot(toViewer, ldir)), Mathf.Cos(glowInfo.m_ConeAngle*Mathf.Deg2Rad/2), 1, 0, 1);
		float ndist = Mathf.Clamp01(dist/glowInfo.m_MaxVisDist);

		dirFadeout = Mathf.Pow(dirFadeout, glowInfo.m_DirIntensityFallof);

		m_GlowsIntensityMask[glowIdx] = dirFadeout*(1 - ndist*ndist);

		glowParams.SetRow(0, paramSet0);
		glowParams.SetRow(1, paramSet1);
		glowParams.SetRow(2, ldir);

		switch (glowIdx)
		{
		case 0:
		{
			material.SetMatrix("_Glow0Params", glowParams);
		}
			break;

		case 1:
		{
			material.SetMatrix("_Glow1Params", glowParams);
		}
			break;

		case 2:
		{
			material.SetMatrix("_Glow2Params", glowParams);
		}
			break;

		case 3:
		{
			material.SetMatrix("_Glow3Params", glowParams);
		}
			break;

		default:
			MFDebugUtils.Assert(false);
			break;
		}
	}

	void SetupActiveGlowsShaderParams(Vector3 camDir, Vector3 camPos)
	{
		for (int i = 0; i < m_NumActiveGlows; i++)
		{
			SetGlowShaderParams(i, m_ActiveGlows[i], camDir, camPos);
		}
	}

	void ResetMaterialGlowParams()
	{
		material.SetMatrix("_Glow0Params", Matrix4x4.zero);
		material.SetMatrix("_Glow1Params", Matrix4x4.zero);
		material.SetMatrix("_Glow2Params", Matrix4x4.zero);
		material.SetMatrix("_Glow3Params", Matrix4x4.zero);

		m_GlowsIntensityMask = Vector3.zero;
	}

	float Impulse(float k, float x)
	{
		float h = k*x;

		return Mathf.Clamp01(h*Mathf.Exp(1.0f - h));
	}

	float FlashBangFunc(float t, float tPeak, float tDuration)
	{
		float tDrop = tDuration*0.5f;

		float k0 = 1.0f/tPeak;
		float k1 = -1.0f/(tDuration - tDrop);
		float v0 = t*k0;
		float v1 = tDuration/(tDuration - tDrop) + t*k1;

		return Mathf.Clamp01(Mathf.Min(v0, v1));
	}

	Vector4 CalcGlobalColor()
	{
		Vector4 res = Vector4.zero;
		int numFlashbangs = m_Flashbangs.Count;
		float currTime = Time.time;

		for (int idx = numFlashbangs - 1; idx >= 0; idx--)
		{
			S_Flashbang curr = m_Flashbangs[idx];
			float age = (currTime - curr.m_StartTime);
			float currI = FlashBangFunc(age, m_FlashbangPeakTime, curr.m_Duration)*curr.m_Intensity;

			if (age > m_FlashbangDuration)
			{
				m_Flashbangs.RemoveAt(idx);
				continue;
			}

			res.x += currI*curr.m_Color.r;
			res.y += currI*curr.m_Color.g;
			res.z += currI*curr.m_Color.b;
		}

		return res;
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		PostFXTracking.ScreenspaceLightFXEffectActive = true;

		if (!m_InitOK || !Camera.main)
		{
			Graphics.Blit(source, destination);

			Debug.LogError("Screen space vertex grid FX not initialized correctly");

			return;
		}

		Camera cam = Camera.main;
		Vector3 camDir = -cam.cameraToWorldMatrix.GetRow(2);
		Vector3 camPos = cam.transform.position;

		ResetMaterialGlowParams();
		SetupActiveGlowsShaderParams(camDir, camPos);

		Matrix4x4 unprojTM = cam.projectionMatrix*cam.worldToCameraMatrix;

		material.SetMatrix("_UnprojectTM", unprojTM.inverse);
		material.SetVector("_GlowsIntensityMask", m_GlowsIntensityMask);
		material.SetVector("_GlobalColor", CalcGlobalColor());

		RenderTexture prevActiveRT = RenderTexture.active;
		RenderTexture.active = destination;

		material.mainTexture = source;

		if (material.SetPass(0))
		{
			Graphics.DrawMeshNow(m_Mesh, Matrix4x4.identity);
		}
		else
		{
			Debug.LogError("Unable to set material pass");
		}

		RenderTexture.active = prevActiveRT;
	}

	bool DoInit()
	{
		m_GameObj = new GameObject();
		shader = Shader.Find("MADFINGER/PostFX/ScreenSpaceLightFX");

		if (!shader)
		{
			Debug.LogError("Unable to get ScreenSpaceLightFX shader");
		}

		MFDebugUtils.Assert(shader);

		if (!InitMeshes())
		{
			return false;
		}

		m_GameObj.SetActive(false);

		return true;
	}

	bool InitMeshes()
	{
		MFDebugUtils.Assert(m_ScreenGridXRes > 1);
		MFDebugUtils.Assert(m_ScreenGridYRes > 1);

		m_GameObj.AddComponent<MeshFilter>();
		m_GameObj.AddComponent<MeshRenderer>();

		m_MeshFilter = (MeshFilter)m_GameObj.GetComponent(typeof (MeshFilter));
		m_MeshRenderer = (MeshRenderer)m_GameObj.GetComponent(typeof (MeshRenderer));

		MFDebugUtils.Assert(m_MeshFilter);
		MFDebugUtils.Assert(m_MeshRenderer);

		m_MeshRenderer.GetComponent<Renderer>().material = material;
		m_MeshRenderer.GetComponent<Renderer>().enabled = true;
		m_MeshRenderer.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
		m_MeshRenderer.GetComponent<Renderer>().receiveShadows = false;

		m_Mesh = m_MeshFilter.mesh;

		int numVerts = m_ScreenGridXRes*m_ScreenGridYRes;
		int numTris = (m_ScreenGridXRes - 1)*(m_ScreenGridYRes - 1)*2;

		Vector3[] verts = new Vector3[numVerts];
		Vector2[] uv = new Vector2[numVerts]; // we fill UVs even if it is not used by shader to make Unity happy
		int[] tris = new int[numTris*3];

		for (int y = 0; y < m_ScreenGridYRes; y++)
		{
			for (int x = 0; x < m_ScreenGridXRes; x++)
			{
				int idx = y*m_ScreenGridXRes + x;

				verts[idx].x = (float)x/(m_ScreenGridXRes - 1);
				verts[idx].y = (float)y/(m_ScreenGridYRes - 1);
				verts[idx].z = 0;

				uv[idx].x = verts[idx].x;
				uv[idx].y = verts[idx].y;
			}
		}

		int currIdx = 0;

		for (int y = 0; y < m_ScreenGridYRes - 1; y++)
		{
			for (int x = 0; x < m_ScreenGridXRes - 1; x++)
			{
				// 0   1
				// +---+
				// |   |
				// +---+
				// 3   2

				int i0 = x + y*m_ScreenGridXRes;
				int i1 = (x + 1) + y*m_ScreenGridXRes;
				int i2 = (x + 1) + (y + 1)*m_ScreenGridXRes;
				int i3 = x + (y + 1)*m_ScreenGridXRes;

				tris[currIdx++] = i3;
				tris[currIdx++] = i1;
				tris[currIdx++] = i0;

				tris[currIdx++] = i3;
				tris[currIdx++] = i2;
				tris[currIdx++] = i1;
			}
		}

		m_Mesh.vertices = verts;
		m_Mesh.uv = uv;
		m_Mesh.triangles = tris;
		m_Mesh.name = "screenspace grid";

		return true;
	}

	float RemapValue(float v, float r0, float r1, float p0, float p1)
	{
		v = Mathf.Max(v, r0);
		v = Mathf.Min(v, r1);

		return p0 + (p1 - p0)*(v - r0)/(r1 - r0);
	}

	public void SpawnFlashbang(Color col, float intensity)
	{
		S_Flashbang f = new S_Flashbang();

		f.m_Color = col;
		f.m_Intensity = intensity;
		f.m_StartTime = Time.time;
		f.m_Duration = m_FlashbangDuration;

		m_Flashbangs.Add(f);
	}

	public void SpawnFlashbang(Color col, float intensity, float duration)
	{
		S_Flashbang f = new S_Flashbang();

		f.m_Color = col;
		f.m_Intensity = intensity;
		f.m_StartTime = Time.time;
		f.m_Duration = duration;

		m_Flashbangs.Add(f);
	}

	public int NumActiveFlashbangs()
	{
		return m_Flashbangs.Count;
	}

	void DbgSpawnFlashbang()
	{
		bool dbgBtnState = Input.GetKeyDown(KeyCode.F1);

		if (dbgBtnState && !m_PrevDbgBtnState)
		{
			SpawnFlashbang(Color.white, 1);
		}

		m_PrevDbgBtnState = dbgBtnState;
	}
}
