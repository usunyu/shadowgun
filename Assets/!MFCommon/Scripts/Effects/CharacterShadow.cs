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

public class CharacterShadow : MonoBehaviour
{
	struct S_CurrAnimInfo
	{
//		public int		m_AnimIndex;
		public string m_AnimId;
		public float m_NormTime;
	};

	struct S_AnimIndexRec
	{
		public int m_StartFrameInAtlas;
		public int m_NumFrames;
	};

	public Material m_Material;
	public Mesh m_ShadowPlaneMesh;
	public float m_GroundOffset = 0.05f;
	public int m_ShadowTexFPS = 7;
	public int m_ShadowTexNumTiles = 32;
	public float m_PelvisAOSphereRadius = 0.4f;
	public float m_FootAOSphereRadius = 0.4f;
	float m_OrthoHalfExt = 1;
	float m_AOPlaneScale = 1;

	// Must be same as value used for generating shadow textures atlas
	MeshFilter m_MeshFilter;
	MeshRenderer m_MeshRenderer;
	GameObject m_GameObject;
	bool m_InitOK = false;
	Transform m_PelvisTransform;
	Transform m_LFootTransform;
	Transform m_RFootTransform;

	Dictionary<string, S_AnimIndexRec> m_AnimInfo = new Dictionary<string, S_AnimIndexRec>();

	void Awake()
	{
		//m_InitOK = DoInit();
	}

	void OnEnable()
	{
		if (null != m_GameObject)
		{
			m_GameObject.SetActive(true);
		}
	}

	void OnDisable()
	{
		if (null != m_GameObject)
		{
			m_GameObject.SetActive(false);
		}
	}

	void Start()
	{
		m_InitOK = DoInit();
	}

	bool DoInit()
	{
		m_GameObject = new GameObject("ShadowPlaneGameObject");

		m_GameObject.AddComponent<MeshFilter>();
		m_GameObject.AddComponent<MeshRenderer>();

		Transform goTrans = m_GameObject.transform;
		goTrans.parent = transform;
		goTrans.localEulerAngles = new Vector3(-90, 0, 180);
		goTrans.localPosition = new Vector3(0, m_GroundOffset, 0);

		m_MeshFilter = (MeshFilter)m_GameObject.GetComponent(typeof (MeshFilter));
		m_MeshRenderer = (MeshRenderer)m_GameObject.GetComponent(typeof (MeshRenderer));

		m_MeshFilter.name = "characterShadowPlane";
		m_MeshFilter.mesh = m_ShadowPlaneMesh;

		m_MeshRenderer.material = m_Material;

		Transform trans = transform;
		m_PelvisTransform = trans.Find("pelvis");
		m_LFootTransform = trans.Find("pelvis/Lthigh/Lcalf/Lfoot");
		m_RFootTransform = trans.Find("pelvis/Rthigh/Rcalf/Rfoot");

		if (!m_PelvisTransform || !m_RFootTransform || !m_LFootTransform)
		{
			return false;
		}

		//InitAnimIndex();		

		return true;
	}

	void LateUpdate()
	{
		if (!m_InitOK)
		{
			return;
		}

//		UpdateTextureBasedShadow(); // we currently don't use this path
		UpdateAOBasedShadow();
	}

	void UpdateTextureBasedShadow()
	{
		S_CurrAnimInfo currAnim = GetCurrAnimInfo();
		Material material = m_MeshRenderer.material;

		Vector3 newPos = m_PelvisTransform.position;
		Vector3 scale;

		Transform goTrans = m_GameObject.transform;
		newPos.y = goTrans.position.y;

		scale.x = m_OrthoHalfExt/m_MeshFilter.mesh.bounds.extents[0];
		scale.y = 1;
		scale.z = m_OrthoHalfExt/m_MeshFilter.mesh.bounds.extents[1];

		goTrans.position = newPos;
		goTrans.localScale = scale;

		if (material)
		{
			Vector4 tilesInfo;
			Vector4 srcDstTile;

			srcDstTile.x = m_ShadowTexNumTiles - 1;
			srcDstTile.y = m_ShadowTexNumTiles - 1;
			srcDstTile.z = 0;
			srcDstTile.w = 0;

			tilesInfo.x = m_ShadowTexNumTiles;
			tilesInfo.y = m_ShadowTexNumTiles;
			tilesInfo.z = 0;
			tilesInfo.w = 0;

			S_AnimIndexRec rec;

			if (currAnim.m_AnimId.Length > 0 && m_AnimInfo.TryGetValue(currAnim.m_AnimId, out rec))
			{
				int numFrames = rec.m_NumFrames;
				float currFrame = currAnim.m_NormTime*numFrames;
				int currFrameInt = Mathf.FloorToInt(currFrame);
				float fracFrame = currFrame - currFrameInt;
				int currFrameInAtlas = rec.m_StartFrameInAtlas + currFrameInt;
				int nextFrameInAtlas = rec.m_StartFrameInAtlas + (currFrameInt + 1)%numFrames;

				srcDstTile.x = currFrameInAtlas%m_ShadowTexNumTiles;
				srcDstTile.y = currFrameInAtlas/m_ShadowTexNumTiles;

				srcDstTile.z = nextFrameInAtlas%m_ShadowTexNumTiles;
				srcDstTile.w = nextFrameInAtlas/m_ShadowTexNumTiles;

				//			Debug.Log(name + " frame idx = " + currFrameInAtlas + " tileInfo " + srcDstTile + " name " + currAnim.m_AnimId + " t = " + fracFrame + " num frames " + numFrames);

				tilesInfo.z = fracFrame;
			}

			material.SetVector("_NumTexTilesAndLerpInfo", tilesInfo);
			material.SetVector("_SrcTileDstTile", srcDstTile);
		}
	}

	void UpdateAOBasedShadow()
	{
		Material material = m_MeshRenderer.material;
		Vector3 pelvisPos = Vector3.zero;
		Vector3 lfootPos = Vector3.zero;
		Vector3 rfootPos = Vector3.zero;

		pelvisPos = m_PelvisTransform.position;
		lfootPos = m_LFootTransform.position;
		rfootPos = m_RFootTransform.position;

		Vector3 scale = Vector3.one;

		scale.x = scale.y = m_AOPlaneScale;

		m_GameObject.transform.localScale = scale;

		if (material)
		{
			Vector4 AOSphere0, AOSphere1, AOSphere2;

			AOSphere0.x = pelvisPos.x;
			AOSphere0.y = pelvisPos.y + m_GroundOffset;
			AOSphere0.z = pelvisPos.z;
			AOSphere0.w = m_PelvisAOSphereRadius;

			AOSphere1.x = lfootPos.x;
			AOSphere1.y = lfootPos.y + m_FootAOSphereRadius*0.8f;
			AOSphere1.z = lfootPos.z;
			AOSphere1.w = m_FootAOSphereRadius;

			AOSphere2.x = rfootPos.x;
			AOSphere2.y = rfootPos.y + m_FootAOSphereRadius*0.8f;
			AOSphere2.z = rfootPos.z;
			AOSphere2.w = m_FootAOSphereRadius;

			material.SetVector("_Sphere0", AOSphere0);
			material.SetVector("_Sphere1", AOSphere1);
			material.SetVector("_Sphere2", AOSphere2);
		}
	}

	S_CurrAnimInfo GetCurrAnimInfo()
	{
		Animation anim = gameObject.GetComponent("Animation") as Animation;

		S_CurrAnimInfo res;

		res.m_AnimId = "";
		res.m_NormTime = 0;

		if (anim)
		{
			float maxWeight = 0;

			//
			// NOTE:
			//
			// This iteration over all animations is obviously far from optimal. 
			// We should check if it is possible to iterate over active anims
			// instead.
			//

			foreach (AnimationState animState in anim)
			{
				if (animState.enabled && animState.weight > 0)
				{
					if (animState.weight > maxWeight)
					{
						res.m_AnimId = animState.name;
						res.m_NormTime = animState.normalizedTime - Mathf.Floor(animState.normalizedTime);

						maxWeight = animState.weight;
					}
				}
			}
		}
		else
		{
			Debug.LogError(name + " No anim");
		}

		return res;
	}

	bool InitAnimIndex()
	{
		Animation anim = gameObject.GetComponent("Animation") as Animation;

		if (anim)
		{
			int totalFrames = 0;

			foreach (AnimationState animState in anim)
			{
				int numFrames = Mathf.CeilToInt(animState.clip.length*m_ShadowTexFPS);
				S_AnimIndexRec rec;

				rec.m_StartFrameInAtlas = totalFrames;
				rec.m_NumFrames = numFrames;

				totalFrames += numFrames;

				m_AnimInfo[animState.name] = rec;
			}

//			Debug.Log("Initialized anim-index, num recs = " + m_AnimInfo.Count);

			return true;
		}

		return true;
	}
}
