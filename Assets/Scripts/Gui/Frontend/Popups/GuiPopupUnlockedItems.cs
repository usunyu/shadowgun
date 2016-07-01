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

public class GuiPopupUnlockedItems : GuiPopup
{
	readonly static int MAX_ITEMS = 3;

	readonly static string CLOSE_BUTTON = "Close_Button";
	readonly static string SKIPANIM_BUTTON = "SkipAnim_Button";
	readonly static string ITEMS = "Items";
	readonly static string ITEM_NAME = "Item_{0}";
	readonly static string ITEM_IMAGE = "Item_Image";
	readonly static string ITEM_LABEL = "Item_Label";

	struct Item
	{
		public GUIBase_Widget Root;
		public GUIBase_Sprite Image;
		public GUIBase_Label Label;
	}

	// CONFIGURATION

	[SerializeField] AudioClip m_NotifySound;

	// PRIVATE MEMBERS

	Item[] m_Items = new Item[MAX_ITEMS];
	Vector2 m_ItemsPosition = new Vector2();
	Vector2 m_ItemsSize = new Vector2();
	float m_AnimationSpeed;

	// PUBLIC METHODS

	public void SetData(List<ResearchItem> items)
	{
		StartCoroutine(ShowItems_Coroutine(items));
	}

	// GUIPOPUP INTERFACE

	public override void SetCaption(string inCaption)
	{
	}

	public override void SetText(string inText)
	{
	}

	// GUIVIEW INTERFACE

	protected override void OnViewInit()
	{
		base.OnViewInit();

		GUIBase_Widget items = GetWidget(ITEMS);
		Transform itemsTrans = items.transform;

		m_ItemsPosition = itemsTrans.position;
		m_ItemsSize = new Vector2(items.GetWidth(), items.GetHeight());

		for (int idx = 0; idx < MAX_ITEMS; ++idx)
		{
			GUIBase_Widget root = itemsTrans.FindChild(string.Format(ITEM_NAME, idx)).GetComponent<GUIBase_Widget>();
			Transform rootTrans = root.transform;

			Transform image = rootTrans.FindChild(ITEM_IMAGE);
			Transform label = rootTrans.FindChild(ITEM_LABEL);

			Item item = new Item()
			{
				Root = root,
				Image = image != null ? image.GetComponent<GUIBase_Sprite>() : null,
				Label = label != null ? label.GetComponent<GUIBase_Label>() : null
			};

			m_Items[idx] = item;
		}
	}

	protected override void OnViewShow()
	{
		base.OnViewShow();

		m_AnimationSpeed = 1.0f;

		RegisterButtonDelegate(CLOSE_BUTTON, () => { ForceClose(); }, null);
		RegisterButtonDelegate(SKIPANIM_BUTTON, () => { m_AnimationSpeed = 0.05f; }, null);
	}

	protected override void OnViewHide()
	{
		StopAllCoroutines();

		RegisterButtonDelegate(CLOSE_BUTTON, null, null);
		RegisterButtonDelegate(SKIPANIM_BUTTON, null, null);

		base.OnViewHide();
	}

	// PRIVATE METHODS

	IEnumerator ShowItems_Coroutine(List<ResearchItem> items)
	{
		Vector2[] positions = new Vector2[items.Count];

		switch (items.Count)
		{
		case 1:
		{
			positions[0] = m_ItemsPosition;
		}
			break;
		case 2:
		{
			Vector2 pos = m_ItemsPosition;
			pos.x -= m_ItemsSize.x*0.25f;
			positions[0] = pos;

			pos = m_ItemsPosition;
			pos.x += m_ItemsSize.x*0.25f;
			positions[1] = pos;
		}
			break;
		default:
		{
			Vector2 pos = m_ItemsPosition;
			pos.x -= m_ItemsSize.x*0.425f;
			positions[0] = pos;

			positions[1] = m_ItemsPosition;

			pos = m_ItemsPosition;
			pos.x += m_ItemsSize.x*0.425f;
			positions[2] = pos;
		}
			break;
		}

		for (int idx = 0; idx < m_Items.Length; ++idx)
		{
			Item item = m_Items[idx];
			if (idx >= items.Count)
			{
				item.Root.Show(false, true);
				continue;
			}

			MFGuiManager.Instance.PlayOneShot(m_NotifySound);

			item.Image.Widget.FadeAlpha = 0.0f;
			item.Label.Widget.FadeAlpha = 0.0f;

			item.Image.Widget.CopyMaterialSettings(items[idx].GetImage());
			item.Label.SetNewText(TextDatabase.instance[items[idx].GetName()]);

			item.Root.transform.position = positions[idx];
			item.Root.SetModify(true);
			item.Root.Show(true, true);

			float duration = 0.4f;
			while (item.Image.Widget.FadeAlpha < 1.0f)
			{
				item.Image.Widget.FadeAlpha += 1.0f/duration*m_AnimationSpeed*Time.deltaTime;
				item.Label.Widget.FadeAlpha = item.Image.Widget.FadeAlpha;

				yield return new WaitForEndOfFrame();
			}
		}
	}
}
