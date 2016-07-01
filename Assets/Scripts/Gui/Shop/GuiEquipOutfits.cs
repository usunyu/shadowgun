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

class OutfitColors
{
	internal static Color m_PreviewColorGradTop = new Color(255.0f/255, 250.0f/255, 240.0f/255);
	internal static Color m_PreviewColorGradBot = new Color(50.0f/255, 150.0f/255, 120.0f/255);
}

public class CSkinModels
{
	Transform m_Transform;
	GameObject m_ActiveModel;
	ShopItemId m_ActiveId = ShopItemId.EmptyId;

	public void Init()
	{
		//pozice 
		const string dummyPos = "/PlayerDummy";
		GameObject goEquip = GameObject.Find("/PlayerDummy");
		if (!goEquip)
		{
			Debug.LogWarning("Failed to find '" + dummyPos + "'. Player skin will not be shown or place properly.");
			return;
		}
		m_Transform = goEquip.transform;
	}

	public void ShowSkin(ShopItemId skinId)
	{
		//Debug.Log("ShowActiveModel begin:"  + skinId + " curr id " + m_ActiveId + " model: " + ((m_ActiveModel != null) ? m_ActiveModel.name : "none"));

		if (m_ActiveId == skinId)
			return;

		//hide previous skin
		HideSkin();

		if (skinId.IsEmpty())
			return;

		//store id	
		m_ActiveId = skinId;

		m_ActiveModel = ShopDataBridge.Instance.GetOutfitModel(skinId);

		if (m_ActiveModel != null)
		{
			int HUDLayer = LayerMask.NameToLayer("HUD");

			RenderUtils.EnableRenderer(m_ActiveModel, true);

			Renderer[] renderers = m_ActiveModel.GetComponentsInChildren<Renderer>();

			foreach (Renderer r in renderers)
			{
				r.gameObject.layer = HUDLayer;
				r.useLightProbes = false;
				r.material = Object.Instantiate(r.material) as Material;
				r.material.SetFloat("_LightProbesLightingAmount", 0);
				r.material.SetVector("_FakeProbeTopColor", OutfitColors.m_PreviewColorGradTop);
				r.material.SetVector("_FakeProbeBotColor", OutfitColors.m_PreviewColorGradBot);
			}

			// prevent glitches caused by late update of animation - @see BUG #330  Character in A pose when player enters the equip section
			if (null != m_ActiveModel.GetComponent<Animation>())
			{
				m_ActiveModel.GetComponent<Animation>().Sample();
			}

			UpdatePos();
		}
		else
			Debug.LogError("Failed to get model for skin: " + skinId.Id + " " + skinId.ItemType);

		//Debug.Log("ShowActiveModel end "  + skinId + " curr id " + m_ActiveId + " model: " + ((m_ActiveModel != null) ? m_ActiveModel.name : "none"));
	}

	public void HideSkin()
	{
		//hide previous skin
		if (m_ActiveModel != null)
		{
			RenderUtils.EnableRenderer(m_ActiveModel, false);
			m_ActiveModel = null;
		}
		m_ActiveId = ShopItemId.EmptyId;
	}

	void UpdatePos()
	{
		if (m_ActiveModel != null && m_Transform != null)
		{
			float modelHeight = 2; // TODO: calc this from model bounds ?
			float screenScale = Screen.height*0.46f;
			Vector3 pos = Vector3.zero;

			pos.x += m_Transform.position.x*modelHeight*screenScale;
			pos.y -= (0.5f - m_Transform.position.y)*modelHeight*screenScale;
			pos.z += m_Transform.position.z;

			m_ActiveModel.transform.position = pos;
			m_ActiveModel.transform.rotation = m_Transform.rotation;
			m_ActiveModel.transform.localScale = m_Transform.localScale*screenScale;
		}
	}

	public Transform GetActiveHeadTransform()
	{
		if (m_ActiveModel != null)
		{
			Transform headTr = m_ActiveModel.transform.FindChild("pelvis/stomach/Ribs/chest/neck/head/Hat_Target");
			return headTr;
		}
		else
			return null;
	}

	public Transform GetWeaponHolderTransform()
	{
		if (m_ActiveModel != null)
		{
			Transform tr = m_ActiveModel.transform.FindChild("pelvis/stomach/Ribs/chest/Rshoulder/Rarm/Rforearm/Rwrist/Rweaponholder");
			return tr;
		}
		else
			return null;
	}
};

class CAccessoryModel
{
	GameObject m_ActiveModel;
	ShopItemId m_ActiveId = ShopItemId.EmptyId;

	public void Show(ShopItemId id, Transform posTransform)
	{
		//Debug.Log("ShowActiveModel begin "  + id + " curr id " + m_ActiveId + " model: " + ((m_ActiveModel != null) ? m_ActiveModel.name : "none"));
		//Debug.Log("Show accessory: " + id);

		if (m_ActiveId == id)
			return;

		//hide prev model
		HideActiveModel();

		//store id			
		m_ActiveId = id;

		//for hats we may want to hide model by this way				
		if (id == ShopItemId.EmptyId)
			return;

		//model
		m_ActiveModel = null;
		GameObject go = ShopDataBridge.Instance.GetOutfitModel(id);

		if (go != null)
		{
			int HUDLayer = LayerMask.NameToLayer("HUD");

			//create new instance from temp model
			m_ActiveModel = Object.Instantiate(go) as GameObject;
			m_ActiveModel.layer = HUDLayer;

			//destroy temp model
			Object.Destroy(go);

			Renderer[] renderers = m_ActiveModel.GetComponentsInChildren<Renderer>();

			foreach (Renderer r in renderers)
			{
				if (r.transform.name == "Muzzle")
				{
					r.enabled = false;
					continue;
				}

				r.useLightProbes = false;
				r.gameObject.layer = HUDLayer;
				r.material = Object.Instantiate(r.material) as Material;

				r.material.SetFloat("_SHLightingScale", 0);
				r.material.SetVector("_FakeProbeTopColor", OutfitColors.m_PreviewColorGradTop);
				r.material.SetVector("_FakeProbeBotColor", OutfitColors.m_PreviewColorGradBot);
			}

			Transform tr = m_ActiveModel.transform;
			tr.parent = posTransform;
			tr.localPosition = Vector3.zero;
			tr.localRotation = Quaternion.identity;
			tr.localScale = Vector3.one;

			RenderUtils.EnableRenderer(m_ActiveModel, true);
		}
		else
			Debug.LogError("Failed to get model for: " + id);

		//Debug.Log("ShowActiveModel end "  + id + " curr id " + m_ActiveId + " model: " + ((m_ActiveModel != null) ? m_ActiveModel.name : "none"));
	}

	public void HideActiveModel()
	{
		//Debug.Log("HideActiveModel " +  ((m_ActiveModel != null) ? m_ActiveModel.name : "none"));
		if (m_ActiveModel != null)
		{
			m_ActiveModel.transform.parent = null;
			RenderUtils.EnableRenderer(m_ActiveModel, false);
			//destroy model
			Object.Destroy(m_ActiveModel);
			m_ActiveModel = null;
		}
		m_ActiveId = ShopItemId.EmptyId;
	}
};

class RenderUtils
{
	public static void EnableRenderer(GameObject go, bool on)
	{
		if (go == null)
			return;

		//Debug.Log("EnableRenderer " + go);

		Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
		foreach (Renderer r in renderers)
		{
			r.enabled = on;
		}
	}
}
