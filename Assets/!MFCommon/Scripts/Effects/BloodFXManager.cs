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

public class BloodFXManager : MonoBehaviour
{
	public static BloodFXManager Instance;
	public GameObject m_BloodOverlayMeshGameObj;
	public Material m_Material;
	public Vector2 m_MinSize = new Vector2(0.5f, 0.5f);
	public Vector2 m_MaxSize = new Vector2(0.6f, 0.6f);
	public Vector2 m_DropMinSize = new Vector2(0.1f, 0.1f);
	public Vector2 m_DropMaxSize = new Vector2(0.2f, 0.2f);
	public float m_Duration = 5.0f;
	public float m_DropsDuration = 2.5f;
	public float m_DurationVariation = 0.1f;
	public float m_DropsDurationVariation = 0.1f;
	public uint m_MaxVisibleDrops = 20;
	public uint m_MaxVisibleSplashes = 10;
	public float m_HurtHealthThreshold = 0.8f;
	public float m_HeartBeatFreqScale = 1;
	protected MeshFilter m_MeshFilter;
	protected MeshRenderer m_MeshRenderer;
	protected Mesh m_Mesh;
	protected uint m_DecalsVersion = 0;
	protected uint m_BuffersVersion = 0;
	protected float m_AspectRatio = 1;
	protected bool m_PrevDbgBtnState = false;
	protected int m_NumBloodDropTexTiles = 3;
	protected uint m_NumSpawnedDrops = 0;
	protected uint m_NumSpawnedSplashes = 0;
	protected int m_CurrHurtLevel = 0;
	protected int m_NumHurtLevels = 6;
	protected float m_CurrHealth = 1;
	protected int[] m_DecalPosIdxPerHurtLevels = new int[] {3, 5, 0, 2, 4, 1};
	protected int m_DbgHurtLevel = -1;
	protected float m_DbgHealth = 1;
	protected GameObject m_BloodOverlayMeshGOInst;
	public int m_BloodDropsTexNumTiles = 2;

	struct DecalInfo
	{
		public Vector2 m_Pos;
		public Vector2 m_Size;
		public Vector2 m_UVMin;
		public Vector2 m_UVMax;
		public float m_Rot;
		public float m_SpawnTime;
		public float m_Duration;
		public bool m_IsDrop;
		public int m_Id;
	};

	List<DecalInfo> m_Decals = new List<DecalInfo>();

	// Use this for initialization
	void Awake()
	{
//		Debug.Log("Initialized BloodFX manager");

#if DEADZONE_DEDICATEDSERVER
		enabled = false;
#else // DEADZONE_DEDICATEDSERVER

		if (!Instance)
		{
			Instance = this;
			InitMeshes();

			m_AspectRatio = (float)Screen.width/Screen.height;
		}
#endif // DEADZONE_DEDICATEDSERVER
	}

	void LateUpdate()
	{
		if (Camera.main == null || !Camera.main.enabled)
		{
			if (m_BloodOverlayMeshGOInst && m_BloodOverlayMeshGOInst.activeSelf)
				m_BloodOverlayMeshGOInst.SetActive(false);
			return;
		}

		if (m_BloodOverlayMeshGOInst)
		{
			if (!m_BloodOverlayMeshGOInst.activeSelf)
				m_BloodOverlayMeshGOInst.SetActive(true);

			Vector4 shaderParams = Vector4.zero;

			shaderParams.x = m_CurrHealth*1.1f;
			shaderParams.y = m_HeartBeatFreqScale;

			m_BloodOverlayMeshGOInst.transform.position = Camera.main.transform.position;

			for (int i = 0; i < m_BloodOverlayMeshGOInst.GetComponent<Renderer>().materials.Length; i++)
			{
				m_BloodOverlayMeshGOInst.GetComponent<Renderer>().materials[i].SetVector("_Params", shaderParams);
			}
		}

//		DbgEmitDecals();		
//		DbgUpdateHurtLevel();
//		DbgUpdateHealth();

		KillOldDecals();

		UpdateHealthState();

		if (m_DecalsVersion != m_BuffersVersion)
		{
			UpdateMeshBuffers();

			m_BuffersVersion = m_DecalsVersion;
		}
	}

	void InitMeshes()
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

		if (m_BloodOverlayMeshGameObj)
		{
			m_BloodOverlayMeshGOInst = Object.Instantiate(m_BloodOverlayMeshGameObj, Vector3.zero, Quaternion.identity) as GameObject;

			MFDebugUtils.Assert(m_BloodOverlayMeshGOInst);
			MFDebugUtils.Assert(m_BloodOverlayMeshGameObj.GetComponent<Renderer>());

			m_BloodOverlayMeshGOInst.transform.eulerAngles = new Vector3(45, 45, 45);
		}
	}

	void UpdateMeshBuffers()
	{
		const float INF = 9999;

		int numVerts = m_Decals.Count*4 + 2; // 2 additional verts for bounds expansion
		int numTris = m_Decals.Count*2;
		Vector3[] newVerts = new Vector3[numVerts];
		Vector2[] newUVs = new Vector2[numVerts];
		Vector2[] newUV2s = new Vector2[numVerts];
		int[] newTris = new int[numTris*3];
		int currVIdx = 0;
		int currTriIdx = 0;

		foreach (DecalInfo currDecal in m_Decals)
		{
			newTris[currTriIdx++] = 3 + currVIdx;
			newTris[currTriIdx++] = 1 + currVIdx;
			newTris[currTriIdx++] = 0 + currVIdx;

			newTris[currTriIdx++] = 3 + currVIdx;
			newTris[currTriIdx++] = 2 + currVIdx;
			newTris[currTriIdx++] = 1 + currVIdx;

			float cosa = Mathf.Cos(currDecal.m_Rot);
			float sina = Mathf.Sin(currDecal.m_Rot);

			float px;
			float py;

			float timeOffs = -currDecal.m_SpawnTime;

			Vector2 size = currDecal.m_Size;
			Vector2 pos = currDecal.m_Pos - size*0.5f;
			float verticalSlide = currDecal.m_IsDrop ? 1 : 0;

			float cx = pos[0] + 0.5f*size[0];
			float cy = pos[1] + 0.5f*size[1];

			px = pos[0];
			py = pos[1];

			newVerts[currVIdx].x = cx + (px - cx)*cosa - (py - cy)*sina;
			newVerts[currVIdx].y = cy + ((px - cx)*sina + (py - cy)*cosa)*m_AspectRatio;
			newVerts[currVIdx].z = timeOffs;

			newUVs[currVIdx].x = currDecal.m_UVMin[0];
			newUVs[currVIdx].y = currDecal.m_UVMin[1];

			newUV2s[currVIdx].x = currDecal.m_Duration;
			newUV2s[currVIdx].y = verticalSlide;

			currVIdx++;

			px = pos[0] + size[0];
			py = pos[1];

			newVerts[currVIdx].x = cx + (px - cx)*cosa - (py - cy)*sina;
			newVerts[currVIdx].y = cy + ((px - cx)*sina + (py - cy)*cosa)*m_AspectRatio;
			newVerts[currVIdx].z = timeOffs;

			newUVs[currVIdx].x = currDecal.m_UVMax[0];
			newUVs[currVIdx].y = currDecal.m_UVMin[1];

			newUV2s[currVIdx].x = currDecal.m_Duration;
			newUV2s[currVIdx].y = verticalSlide;

			currVIdx++;

			px = pos[0] + size[0];
			py = pos[1] + size[1];

			newVerts[currVIdx].x = cx + (px - cx)*cosa - (py - cy)*sina;
			newVerts[currVIdx].y = cy + ((px - cx)*sina + (py - cy)*cosa)*m_AspectRatio;
			newVerts[currVIdx].z = timeOffs;

			newUVs[currVIdx].x = currDecal.m_UVMax[0];
			newUVs[currVIdx].y = currDecal.m_UVMax[1];

			newUV2s[currVIdx].x = currDecal.m_Duration;
			newUV2s[currVIdx].y = 0;

			currVIdx++;

			px = pos[0];
			py = pos[1] + size[1];

			newVerts[currVIdx].x = cx + (px - cx)*cosa - (py - cy)*sina;
			newVerts[currVIdx].y = cy + ((px - cx)*sina + (py - cy)*cosa)*m_AspectRatio;
			newVerts[currVIdx].z = timeOffs;

			newUVs[currVIdx].x = currDecal.m_UVMin[0];
			newUVs[currVIdx].y = currDecal.m_UVMax[1];

			newUV2s[currVIdx].x = currDecal.m_Duration;
			newUV2s[currVIdx].y = 0;

			currVIdx++;
		}

		//
		// add vertices to expand mesh bounds to 'infinity' to force mesh renderer to be always visible
		//

		newVerts[currVIdx].x = -INF;
		newVerts[currVIdx].y = -INF;
		newVerts[currVIdx].z = -INF;

		currVIdx++;

		newVerts[currVIdx].x = INF;
		newVerts[currVIdx].y = INF;
		newVerts[currVIdx].z = INF;

		currVIdx++;

		m_Mesh.Clear();

		m_Mesh.vertices = newVerts;
		m_Mesh.uv = newUVs;
		m_Mesh.uv2 = newUV2s;
		m_Mesh.triangles = newTris;

		m_Mesh.RecalculateBounds();

//		Debug.Log("Num verts = " + m_Mesh.vertices.Length + " Num tris = " + m_Mesh.triangles.Length / 3);
	}

	void KillOldDecals()
	{
		float currTime = Time.time;
		uint numKilled = 0;

		for (int idx = m_Decals.Count - 1; idx >= 0; idx--)
		{
			if ((currTime - m_Decals[idx].m_SpawnTime) > m_Decals[idx].m_Duration)
			{
				if (m_Decals[idx].m_IsDrop)
				{
					MFDebugUtils.Assert(m_NumSpawnedDrops > 0);
					m_NumSpawnedDrops--;
				}
				else
				{
					MFDebugUtils.Assert(m_NumSpawnedSplashes > 0);
					m_NumSpawnedSplashes--;
				}

				m_Decals.RemoveAt(idx);
				numKilled++;
			}
		}

		if (numKilled > 0)
		{
			m_DecalsVersion++;
		}
	}

	void KillDecalById(int id)
	{
		for (int idx = m_Decals.Count - 1; idx >= 0; idx--)
		{
			if (m_Decals[idx].m_Id == id)
			{
				m_Decals.RemoveAt(idx);
			}
		}
	}

	uint GetNumDropsForIntensity(float intensity)
	{
		return (uint)(intensity*8) + 1;
	}

	uint GetNumSplashesForIntensity(float intensity)
	{
		return (uint)(intensity*3) + 1;
	}

	Vector2 GetHeavySplatterPos(int idx)
	{
		MFDebugUtils.Assert(idx < 6);

		switch (idx)
		{
		case 0:
			return new Vector2(-1, 1);

		case 1:
			return new Vector2(0, 1);

		case 2:
			return new Vector2(1, 1);

		case 3:
			return new Vector2(-1, -1);

		case 4:
			return new Vector2(0, -1);

		case 5:
			return new Vector2(1, -1);
		}

		return new Vector2(0, 0);
	}

	Vector2 CalcHeavySplatterRandomPos()
	{
		/*
		Vector2	res = new Vector2(0,0);
		
		res[0] = Random.Range(-1.0f,1.0f);
		res[1] = Random.Range(0.0f,1.0f) < 0.5f ? -1.0f : 1.0f;

		return res;

	*/

		int rnd = (int)(Random.Range(0.0f, 1.0001f)*5);

		return GetHeavySplatterPos(rnd);
	}

	public void SpawnBloodDrops(uint cnt)
	{
		/*
		float currTime 	= Time.time;
		float dropUVSize= 1.0f / m_BloodDropsTexNumTiles;
		
		for (uint i = 0; i < cnt; i++)
		{
			if (m_NumSpawnedDrops >= m_MaxVisibleDrops)
			{
				break;
			}
			
			DecalInfo	newDecal	= new DecalInfo();
			float		size		= Random.Range(m_DropMinSize[0],m_DropMaxSize[0]);
			
			newDecal.m_Pos[0] = Random.Range(-1.0f,1.0f);
			newDecal.m_Pos[1] = Random.Range(-1.0f,1.0f);
			
			newDecal.m_Size[0] = size;
			newDecal.m_Size[1] = size;

			
			int 	rndU 	= Random.Range(0,m_BloodDropsTexNumTiles);
			int 	rndV 	= Random.Range(0,m_BloodDropsTexNumTiles);
			
			newDecal.m_UVMin[0] = rndU * dropUVSize;
			newDecal.m_UVMin[1] = rndV * dropUVSize;

			newDecal.m_UVMax[0] = newDecal.m_UVMin[0] + dropUVSize;
			newDecal.m_UVMax[1] = newDecal.m_UVMin[1] + dropUVSize;
			
			newDecal.m_Rot 			= 0;
			newDecal.m_SpawnTime	= currTime;
			newDecal.m_Duration		= m_DropsDuration + Random.Range(0,m_DropsDuration * m_DropsDurationVariation);
			newDecal.m_IsDrop		= true;
			newDecal.m_Id			= -1;
			
			m_NumSpawnedDrops++;
			
			m_Decals.Add(newDecal);
		}	
		
		m_DecalsVersion++;
		*/
	}

	void SpawnBloodSplashes(uint cnt)
	{
		/*
		for (uint i = 0; i < cnt; i++)
		{	
			if (m_NumSpawnedSplashes >= m_MaxVisibleSplashes)
			{
				break;
			}

			DecalInfo	newDecal	= new DecalInfo();
			float		size		= Random.Range(m_MinSize[0],m_MaxSize[0]);
			
			newDecal.m_Pos = CalcHeavySplatterRandomPos();
			
			newDecal.m_Size[0] = size;
			newDecal.m_Size[1] = size;
			
			newDecal.m_UVMin[0] = 0;
			newDecal.m_UVMin[1] = 0;

			newDecal.m_UVMax[0] = newDecal.m_UVMax[1] = 1 - 1.0f / m_NumBloodDropTexTiles;
			
			newDecal.m_Rot 			= Random.Range(0.0f,Mathf.PI * 2);
			newDecal.m_SpawnTime	= Time.time;
			newDecal.m_Duration		= m_Duration + Random.Range(0,m_Duration * m_DurationVariation);
			newDecal.m_IsDrop		= false;
			newDecal.m_Id			= -1;
			
			m_NumSpawnedSplashes++;
						
			m_Decals.Add(newDecal);		
		}			
		
		m_DecalsVersion++;
		*/
	}

	void SpawnBloodSplatterAuto(float intensity)
	{
		uint numDrops = GetNumDropsForIntensity(intensity);
		uint numSplashes = GetNumSplashesForIntensity(intensity);

		SpawnBloodDrops(numDrops);
		SpawnBloodSplashes(numSplashes);

		m_DecalsVersion++;
	}

	public void SetHealthNormalized(float value)
	{
		MFDebugUtils.Assert(value >= 0.0f && value <= 1.0f);

		m_CurrHealth = value;
	}

	int CalcHurtLevel(float health)
	{
		if (health >= m_HurtHealthThreshold)
		{
			return 0;
		}
		else
		{
			float hurtStep = m_HurtHealthThreshold/m_NumHurtLevels;
			int result = (int)Mathf.Ceil((m_HurtHealthThreshold - health)/hurtStep);

			return result > m_NumHurtLevels ? m_NumHurtLevels : result;
		}
	}

	void AddHurtDecal(int posIdx, float rot)
	{
		DecalInfo newDecal = new DecalInfo();
		float size = m_MaxSize[0];

		newDecal.m_Pos = GetHeavySplatterPos(posIdx);

		newDecal.m_Size[0] = size;
		newDecal.m_Size[1] = size;

		newDecal.m_UVMin[0] = 0;
		newDecal.m_UVMin[1] = 0;

		newDecal.m_UVMax[0] = newDecal.m_UVMax[1] = 1 - 1.0f/m_NumBloodDropTexTiles;

		newDecal.m_Rot = rot;
		newDecal.m_SpawnTime = Time.time;
		newDecal.m_Duration = 99999;
		newDecal.m_IsDrop = false;
		newDecal.m_Id = posIdx;

		m_Decals.Add(newDecal);
	}

	void UpdateHealthState()
	{
		/*
		int	hurtLevel = m_DbgHurtLevel < 0 ? CalcHurtLevel(m_CurrHealth) : m_DbgHurtLevel;
		
		if (hurtLevel != m_CurrHurtLevel)
		{
			//Debug.Log(hurtLevel);
			
			for (int i = 0; i < m_NumHurtLevels; i++)
			{
				KillDecalById(i);
			}
						
			for (int i = 0; i < hurtLevel; i++)
			{
				AddHurtDecal(m_DecalPosIdxPerHurtLevels[i],0);
				AddHurtDecal(m_DecalPosIdxPerHurtLevels[i],Mathf.PI / 7);
			}
			
			m_DecalsVersion++; 
			
			m_CurrHurtLevel = hurtLevel;
		}
		*/
	}

	void DbgEmitDecals()
	{
		bool dbgBtnState = Input.GetButton("Fire1");

		if (dbgBtnState && !m_PrevDbgBtnState)
		{
			SpawnBloodSplatterAuto(0.3f);
		}

		m_PrevDbgBtnState = dbgBtnState;
	}

	void DbgUpdateHurtLevel()
	{
		bool dbgBtnState = Input.GetButton("Fire1");

		if (dbgBtnState && !m_PrevDbgBtnState)
		{
			if (m_DbgHurtLevel < 0)
			{
				m_DbgHurtLevel = 0;
			}
			else
			{
				m_DbgHurtLevel = (m_DbgHurtLevel + 1)%(m_NumHurtLevels + 1);
			}

			//Debug.Log("Hurt level = " + m_DbgHurtLevel);
		}

		m_PrevDbgBtnState = dbgBtnState;
	}

	void DbgUpdateHealth()
	{
		bool dbgBtnState = Input.GetButton("Fire1");

		if (dbgBtnState && !m_PrevDbgBtnState)
		{
			m_DbgHealth -= 0.05f;

			if (m_DbgHealth < 0)
			{
				m_DbgHealth = 1;
			}

			//Debug.Log("Dbg health = " + m_DbgHealth);
		}

		m_PrevDbgBtnState = dbgBtnState;
	}
}
