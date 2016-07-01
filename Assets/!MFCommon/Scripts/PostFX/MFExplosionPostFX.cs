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

//[ExecuteInEditMode]
[AddComponentMenu("Image Effects/MADFINGER explosion FX")]
public class MFExplosionPostFX : ImageEffectBase
{
	struct S_WaveEmitter
	{
		public Vector2 m_Center;
		public float m_Amplitude;
		public float m_Frequency;
		public float m_Speed;
		public float m_DistAtt;
		public float m_StartTime;
		public float m_Duration;
		public int m_SlotIdx;
	};

	public struct S_WaveParams
	{
		public float m_Amplitude;
		public float m_Freq;
		public float m_Speed;
		public float m_Duration;
		public float m_Radius;
		public float m_Delay;
	};

	int m_ScreenGridXRes = 30;
	int m_ScreenGridYRes = 25;
	MeshFilter m_MeshFilter;
	MeshRenderer m_MeshRenderer;
	Mesh m_Mesh;
	int m_MaxWaves = 4; // limted by shader, don't change unless you know what you are doing
	bool m_InitOK = false;
	bool m_PrevDbgBtnState = false;
	GameObject m_GameObj = null;

	public float m_GrenadeWaveAmplitude = 0.3f;
	public float m_GrenadeWaveFreq = 20;
	public float m_GrenadeWaveSpeed = 1.4f;
	public float m_GrenadeWaveDuration = 1.5f;
	public float m_GrenadeWaveMaxRadius = 1.0f;

	public static MFExplosionPostFX Instance = null;

	List<S_WaveEmitter> m_ActiveWaves = new List<S_WaveEmitter>();
	Stack<int> m_FreeWaveEmitterSlots = new Stack<int>();

	void Awake()
	{
		Instance = this;
		enabled = false;
#if UNITY_IPHONE || UNITY_ANDROID
		enabled = true;
		m_InitOK = DoInit();
#endif
	}

	void OnDestroy()
	{
		Instance = null;
	}

	void LateUpdate()
	{
		UpdateEmitters();

//		DbgEmitWave();		
	}

	public void Reset()
	{
		m_ActiveWaves.Clear();
	}

	void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if (!m_InitOK)
		{
			Graphics.Blit(source, destination);

			Debug.LogError("Explosion PostFX subsystem not initialized correctly");
			return;
		}

//		RenderTexture prevRT 		= Camera.current.targetTexture;
		RenderTexture prevActiveRT = RenderTexture.active;

//		Camera.current.targetTexture = destination;
		RenderTexture.active = destination;

		Vector4 uvOffs = Vector4.zero;

		uvOffs.x = 0.5f/source.width;
		uvOffs.y = 0.5f/source.height;
		uvOffs.z = 1;
		uvOffs.w = (float)source.height/source.width;

		for (int i = 0; i < m_MaxWaves; i++)
		{
			SetWaveShaderParams(i, Vector4.zero, Vector4.zero);
		}

		material.mainTexture = source;
		material.SetVector("_UVOffsAndAspectScale", uvOffs);

		foreach (S_WaveEmitter currWave in m_ActiveWaves)
		{
			SetupWaveShaderParams(currWave);
		}

		if (material.SetPass(0))
		{
			Graphics.DrawMeshNow(m_Mesh, Matrix4x4.identity);
		}
		else
		{
			Debug.LogError("Unable to set material pass");
		}

		RenderTexture.active = prevActiveRT;
//		Camera.current.targetTexture	= prevRT;
	}

	bool DoInit()
	{
		m_GameObj = new GameObject();
		shader = Shader.Find("MADFINGER/PostFX/ExplosionFX");

		if (!shader)
		{
			Debug.LogError("Unable to get ExplosionFX shader");
		}

		MFDebugUtils.Assert(shader);

		if (!InitMeshes())
		{
			return false;
		}

		for (int i = 0; i < m_MaxWaves; i++)
		{
			m_FreeWaveEmitterSlots.Push(i);
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

	void UpdateEmitters()
	{
		float currTime = Time.time;

		for (int i = m_ActiveWaves.Count - 1; i >= 0; i--)
		{
			if ((currTime - m_ActiveWaves[i].m_StartTime) > m_ActiveWaves[i].m_Duration)
			{
//				Debug.Log("Removing wave emitter (slot " + m_ActiveWaves[i].m_SlotIdx + " )");

				m_FreeWaveEmitterSlots.Push(m_ActiveWaves[i].m_SlotIdx);
				m_ActiveWaves.RemoveAt(i);
			}
		}
	}

	void SetWaveShaderParams(int slotIdx, Vector4 paramSet0, Vector4 paramSet1)
	{
		switch (slotIdx)
		{
		case 0:
		{
			material.SetVector("_Wave0ParamSet0", paramSet0);
			material.SetVector("_Wave0ParamSet1", paramSet1);
		}
			break;

		case 1:
		{
			material.SetVector("_Wave1ParamSet0", paramSet0);
			material.SetVector("_Wave1ParamSet1", paramSet1);
		}
			break;

		case 2:
		{
			material.SetVector("_Wave2ParamSet0", paramSet0);
			material.SetVector("_Wave2ParamSet1", paramSet1);
		}
			break;

		case 3:
		{
			material.SetVector("_Wave3ParamSet0", paramSet0);
			material.SetVector("_Wave3ParamSet1", paramSet1);
		}
			break;

		default:
			MFDebugUtils.Assert(false);
			break;
		}
	}

	void SetupWaveShaderParams(S_WaveEmitter emitter)
	{
		//
		// paramsSet0.xy	- wave center (normalized coords)
		// paramsSet0.z		- wave amplitude
		// paramsSet0.w		- wave frequency
		//
		// paramsSet1.x		- wave distance attenuation
		// paramsSet1.y		- wave speed
		// paramsSet1.z		- wave start time
		//

//		const float MIN_ATT = 0.001f;

		Vector4 paramSet0 = Vector4.zero;
		Vector4 paramSet1 = Vector4.zero;

		paramSet0.x = emitter.m_Center.x;
		paramSet0.y = emitter.m_Center.y;
		paramSet0.z = emitter.m_Amplitude;
		paramSet0.w = emitter.m_Frequency;

		paramSet1.x = emitter.m_DistAtt;
		paramSet1.y = emitter.m_Speed;
		paramSet1.z = emitter.m_StartTime;
		paramSet1.w = 1; // (1 - MIN_ATT) / (MIN_ATT * emitter.m_Duration * emitter.m_Duration);

		SetWaveShaderParams(emitter.m_SlotIdx, paramSet0, paramSet1);
	}

	public void EmitGrenadeExplosionWave(Vector2 normScreenPos, S_WaveParams waveParams)
	{
		if (m_FreeWaveEmitterSlots.Count == 0)
		{
			Debug.LogWarning("Out of free wave-emitter slots");
			return;
		}

		int slotIdx = m_FreeWaveEmitterSlots.Pop();

		S_WaveEmitter emitter = new S_WaveEmitter();

		emitter.m_Center = normScreenPos;
		emitter.m_Amplitude = waveParams.m_Amplitude;
		emitter.m_Frequency = waveParams.m_Freq;
		emitter.m_Speed = waveParams.m_Speed;
		emitter.m_StartTime = Time.time + waveParams.m_Delay;
		emitter.m_Duration = waveParams.m_Duration + waveParams.m_Delay;
		emitter.m_DistAtt = 1.0f/waveParams.m_Radius;
		emitter.m_SlotIdx = slotIdx;

		//Debug.Log("Amp = " + emitter.m_Amplitude + " R = " + waveParams.m_Radius);

		m_ActiveWaves.Add(emitter);
	}

	public int NumActiveEffects()
	{
		return m_ActiveWaves.Count;
	}

	void DbgEmitWave()
	{
		bool dbgBtnState = Input.GetButton("Fire1");

		if (dbgBtnState && !m_PrevDbgBtnState)
		{
			Vector2 pos;
			S_WaveParams waveParams;

			pos.x = Random.Range(0.0f, 1.0f);
			pos.y = Random.Range(0.0f, 1.0f);

			waveParams.m_Amplitude = m_GrenadeWaveAmplitude;
			waveParams.m_Duration = m_GrenadeWaveDuration;
			waveParams.m_Freq = m_GrenadeWaveFreq;
			waveParams.m_Radius = m_GrenadeWaveMaxRadius;
			waveParams.m_Speed = m_GrenadeWaveSpeed;
			waveParams.m_Delay = 0;

			//Debug.Log("EmitGrenadeExplosion " + pos);

			EmitGrenadeExplosionWave(pos, waveParams);
		}

		m_PrevDbgBtnState = dbgBtnState;
	}
}
