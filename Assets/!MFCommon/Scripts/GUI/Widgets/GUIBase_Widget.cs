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

#define GUI_ENABLE_ANIMATIONS

using UnityEngine;
using System.Collections;

[AddComponentMenu("GUI/Widgets/Widget")]
public class GUIBase_Widget : GUIBase_Element
{
	[SerializeField] Material m_Material;

	// focus ID should be unique per layout
	[SerializeField] int m_FocusID = -1; // if it is -1, it is impossible get focus to widget
	[SerializeField] E_InputOpacity m_InputOpacity = E_InputOpacity.Opaque;

	// Area in texture
	public Vector2 m_InTexPos = new Vector2(0.4f, 0.4f);
	public Vector2 m_InTexSize = new Vector2(0.2f, 0.2f);
	public MFGuiGrid9 m_Grid9 = new MFGuiGrid9();

	// layer of widget (0 = main layer, 1 = first under main, etc...)
	public int m_GuiWidgetLayer = 0;

	// set proxy flag to first sprite to this value during initialization
	[System.NonSerialized] public bool CreateMainSprite = false;

	// is visible when Layout is going to be visible ?
	public bool m_VisibleOnLayoutShow = true;

	// Alpha pro Fade out effect
	[SerializeField] Color m_Color = Color.white;

	// clip linked widgets
	//[SerializeField] bool  m_ClipChildren = false;

	public enum E_TouchPhase
	{
		E_TP_NONE,
		E_TP_CLICK_BEGIN,
		E_TP_CLICK_RELEASE,
		E_TP_MOUSEOVER_BEGIN,
		E_TP_MOUSEOVER_END,

		E_TP_CLICK_RELEASE_KEYBOARD,
	};

	// delegate called from GUIUpdate
	public delegate void UpdateDelegate();

	//
	// Private section
	//

	// Size on screen
	[SerializeField] float m_Width = 1.0f;
	[SerializeField] float m_Height = 1.0f;

	GUIBase_Layout m_Layout;
	MFGuiRenderer m_GuiRenderer = null;

	Vector3 m_OrigPos = new Vector3();

	GUIBase_Callback m_Callback;
	UpdateDelegate m_UpdateDelegate;

	// Widget can contain more sprites
	struct S_Sprite
	{
		public MFGuiSprite m_Sprite;
		public bool m_IsVisible;

		// TODO matrix (transformation to the center of widget)
		public Vector3 m_Pos; // original position of widget in "world" space
		public float m_Width;
		public float m_Height;
		public MFGuiGrid9 m_Grid9;
	};

	S_Sprite[] m_Sprites;

	int ReservedSpritesSize
	{
		get { return m_Sprites != null ? m_Sprites.Length : 0; }
	}

	int m_UnusedSpriteIndex = 0;

	//Vector2				m_LayoutScale = new Vector2(1.0f, 1.0f);

	[System.NonSerialized] public bool m_TextScaleFix_HACK = false;
									   // AX :: this property is here only for fixing incorrect text render during scale animations...

#if GUI_ENABLE_ANIMATIONS
	Vector3 m_Pos;
	Vector3 m_Scale;
	float m_RotZ;
	Quaternion m_Quat;
#endif

	public Material Material
	{
		get { return m_Material; }
	}

	public int FocusID
	{
		get { return m_FocusID; }
	}

	public E_InputOpacity InputOpacity
	{
		get { return m_InputOpacity; }
	}

	public Color Color
	{
		get { return m_Color; }
		set { SetColor(value); }
	}

	public int ComputedWidgetLayer
	{
		get { return m_Layout ? m_Layout.m_LayoutLayer*GUIBase_Layout.MAX_LAYERS + m_GuiWidgetLayer : m_GuiWidgetLayer; }
	}

	//public bool           ClipChildren        { get { return m_ClipChildren; } set { if (m_ClipChildren != value) SetModify(true); m_ClipChildren = value; } }

	//---------------------------------------------------------
	public void Initialization(GUIBase_Layout parentLayout, Vector2 layoutScale)
	{
		m_Layout = parentLayout;
		m_OrigPos = transform.position;

		// Pro widget vrati spravny renderer
		// rozhoduje se podle materialu a renderQueue indexu (poradi kdy se ma renderovat)
		ChangeMaterial(m_Material);

		// Call initialization for "parent" type of widget
		if (m_Callback && m_Callback.TestFlag((int)GUIBase_Callback.E_CallbackType.E_CT_INIT))
		{
			m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_INIT, this);
		}
	}

	//---------------------------------------------------------
	void RegisterRenderer(Material material)
	{
		if (material == null)
		{
			Debug.LogError("GUIBase_Widget<" + this.GetFullName('.') + ">.RegisterRenderer() :: New Material is invalid !!!");
			return;
		}

		MFGuiRenderer newRenderer = MFGuiManager.Instance.RegisterWidget(this, material, ComputedWidgetLayer);
		if (newRenderer == null)
		{
			Debug.LogError("GUIBase_Widget<" + this.GetFullName('.') + ">.RegisterRenderer() :: Material renderer is missing !!!");
			return;
		}

		if (newRenderer != m_GuiRenderer)
		{
			MFGuiManager.Instance.UnRegisterWidget(this, m_GuiRenderer);
		}

		m_GuiRenderer = newRenderer;
		// Material pro widget mohl byt "modifikovan" zmenou render queue indexu
		m_Material = m_GuiRenderer.material;
	}

	//---------------------------------------------------------
	public void ChangeMaterial(Material inMaterial)
	{
		// remove sprites first
		RemoveSprites();

		// register renderer for new material
		RegisterRenderer(inMaterial);

		// Prepare main sprite (if there is any)
		AddMainSprite();

		// hide sprite if the widget is hidden
		if (Visible == false)
		{
			ShowSprite(0, false);
		}

		// set dirty flag
		SetModify();
	}

	//---------------------------------------------------------
	public void SetColor(Color color)
	{
		if (m_Color == color)
			return;

		m_Color = color;

		if (m_Callback && m_Callback.TestFlag((int)GUIBase_Callback.E_CallbackType.E_CT_COLOR))
		{
			m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_COLOR, m_Color);
		}

		SetModify();
	}

	//---------------------------------------------------------
	void OnDestroy()
	{
		m_Material = null;
	}

	//---------------------------------------------------------
	public void RegisterUpdateDelegate(UpdateDelegate f)
	{
		m_UpdateDelegate += f;
	}

	//---------------------------------------------------------
	public float GetWidth()
	{
		return m_Width;
	}

	//---------------------------------------------------------
	public float GetHeight()
	{
		return m_Height;
	}

	//---------------------------------------------------------
	public MFGuiRenderer GetGuiRenderer()
	{
		return m_GuiRenderer;
	}

	public Vector3 GetOrigPos()
	{
		return m_OrigPos;
	}

	/*public Rect GetClipRectangle()
	{
		Rect rect = GetRectInScreenCoords();
		GUIBase_Widget parent = Parent as GUIBase_Widget;
		if (parent != null && parent.m_ClipChildren == true)
		{
			rect = rect.Intersect(parent.GetClipRectangle());
		}
		return rect;
	}*/

	//---------------------------------------------------------
	public Material GetMaterial()
	{
		if (m_Material)
		{
			return m_Material;
		}

		return null;
	}

	//---------------------------------------------------------
	public Texture GetTexture()
	{
		if (m_Material)
		{
			return m_Material.mainTexture;
		}

		return null;
	}

	//---------------------------------------------------------
	// Makes a copy of material and U,V settings
	public void CopyMaterialSettings(GUIBase_Widget otherWidget)
	{
		if (otherWidget == null)
			return;
		if (otherWidget.m_Material == null || otherWidget.m_Material.mainTexture == null)
		{
			Debug.LogWarning("GUIBase_Widget.CopyMaterialSettings() :: Invalid source widget '" + otherWidget.GetFullName('.') + "'!!!",
							 otherWidget.gameObject);
			return;
		}

		// setup widget
		m_Material = otherWidget.m_Material;
		m_InTexPos = otherWidget.m_InTexPos;
		m_InTexSize = otherWidget.m_InTexSize;
		m_Grid9 = otherWidget.m_Grid9;

		// we don't need to call ChangeMaterial() if there is not any gui renderer assigned yet
		// we can wait until Initialize() call will do so
		if (m_GuiRenderer != null)
		{
			// reassign renderer and material
			ChangeMaterial(m_Material);
		}
	}

	//---------------------------------------------------------
	public int GetLayoutUniqueId()
	{
		return m_Layout ? m_Layout.GetUniqueId() : 0;
	}

	//---------------------------------------------------------
	MFGuiSprite AddSprite(Rect rect,
						  float rotAngle,
						  float depth,
						  int leftPixelX,
						  int bottomPixelY,
						  int pixelWidth,
						  int pixelHeight,
						  MFGuiGrid9 grid9 = null)
	{
		SetModify();

		return m_GuiRenderer.AddElement(rect, rotAngle, depth, leftPixelX, bottomPixelY, pixelWidth, pixelHeight, grid9);
	}

	//---------------------------------------------------------
	void UpdateSprite(MFGuiSprite sprite, Rect rect, float rotAngle, Vector2 scale, float depth, MFGuiGrid9 grid9)
	{
		m_GuiRenderer.UpdateSprite(sprite, rect, rotAngle, scale, depth, grid9);
	}

	//---------------------------------------------------------
	void ShowSprite(MFGuiSprite sprite)
	{
		if (sprite != null)
		{
			sprite.visible = true;
		}

		SetModify();
	}

	//---------------------------------------------------------
	void HideSprite(MFGuiSprite sprite)
	{
		if (sprite != null)
		{
			sprite.visible = false;
		}

		SetModify();
	}

	//---------------------------------------------------------
	public void SetScreenSize(float sizeX, float sizeY)
	{
		m_Width = Mathf.RoundToInt(sizeX);
		m_Height = Mathf.RoundToInt(sizeY);

		SetModify();
	}

	//---------------------------------------------------------
	public void PlaySound(AudioClip audioClip)
	{
		MFGuiManager.Instance.PlayOneShot(audioClip);
	}

	//---------------------------------------------------------
	public GUIBase_Layout GetLayout()
	{
		return m_Layout;
	}

	//---------------------------------------------------------
	public void GetTextureCoord(out float UVLeft, out float UVTop, out float UVWidth, out float UVHeight)
	{
		UVLeft = m_InTexPos.x;
		UVTop = m_InTexPos.y;
		UVWidth = m_InTexSize.x;
		UVHeight = m_InTexSize.y;
	}

	public void SetTextureCoords(int spriteIdx, int UVLeftPixel, int UVTopPixel, int UVWidthPixel, int UVHeightPixel)
	{
		if (m_GuiRenderer == null)
			return;

		Vector2 uv = m_GuiRenderer.PixelSpaceToUVSpace(UVLeftPixel, UVTopPixel);
		Vector2 wx = m_GuiRenderer.PixelSpaceToUVSpace(UVWidthPixel, UVHeightPixel);

		SetTextureCoords(spriteIdx, uv.x, uv.y, wx.x, wx.y);
	}

	public void SetTextureCoords(int spriteIdx, float UVLeft, float UVTop, float UVWidth, float UVHeight)
	{
		if (m_Sprites == null)
			return;
		if (m_Sprites.Length <= spriteIdx)
			return;

		MFGuiSprite sprite = m_Sprites[spriteIdx].m_Sprite;
		if (sprite == null)
			return;

		MFGuiUVCoords uvCoords = sprite.uvCoords;
		uvCoords.U = UVLeft;
		uvCoords.V = 1.0f - (UVTop + UVHeight);
		uvCoords.Width = UVWidth;
		uvCoords.Height = UVHeight;
		sprite.uvCoords = uvCoords;

		SetModify();
	}

	public Rect GetTextureCoord()
	{
		return new Rect(m_InTexPos.x, m_InTexPos.y, m_InTexSize.x, m_InTexSize.y);
	}

	//---------------------------------------------------------
	int AddMainSprite()
	{
		int resIdx = -1;
		if (CreateMainSprite == false)
			return resIdx;
		if (m_Sprites != null && m_Sprites.Length > 0)
			return 0;

		Texture texture = GetTexture();

		if (texture)
		{
			Transform trans = transform;
			Vector3 scale = trans.lossyScale;
			Vector3 rot = trans.eulerAngles;

			int texWidth = texture.width;
			int texHeight = texture.height;

			int texU = (int)(texWidth*m_InTexPos.x);
			int texV = (int)(texHeight*m_InTexPos.y);
			int texW = (int)(texWidth*m_InTexSize.x);
			int texH = (int)(texHeight*m_InTexSize.y);

			resIdx = AddSprite(new Vector2(m_OrigPos.x, m_OrigPos.y),
							   m_Width,
							   m_Height,
							   scale.x,
							   scale.y,
							   rot.z,
							   texU,
							   texV + texH,
							   texW,
							   texH,
							   m_Grid9);
		}

		return resIdx;
	}

	//---------------------------------------------------------
	void ReserveSprites(int size)
	{
		m_UnusedSpriteIndex = 0;
		m_Sprites = new S_Sprite[size];
	}

	void ReallocateSprites(int newSize)
	{
		if (m_Sprites == null || ReservedSpritesSize <= 0)
		{
			Debug.LogError("For first allocate use AllocateSprites");
			return;
		}

		if (newSize > ReservedSpritesSize)
		{
			S_Sprite[] tmpSprites = m_Sprites;
			m_Sprites = new S_Sprite[newSize];
			tmpSprites.CopyTo(m_Sprites, 0);
		}
		else
		{
			Debug.LogError(
						   "Sorry, reducing not implemented. Use AllocateSprites for creating entirely new buffer without copying, or add code handling reduction here.");
		}
	}

	//---------------------------------------------------------
	//public int AddSprite(Rect inSprite, Vector2 inScale, float inRotAngle, Rect inSpriteTextCoord)
	public int AddSprite(Vector2 centerSpritePos,
						 float width,
						 float height,
						 float scaleWidth,
						 float scaleHeight,
						 float rotAngle,
						 int texU,
						 int texV,
						 int texW,
						 int texH,
						 MFGuiGrid9 grid9 = null)
	{
		int resIdx = -1;

		float rx = centerSpritePos.x - 0.5f*width*scaleWidth;
		float ry = centerSpritePos.y - 0.5f*height*scaleHeight;

		MFGuiSprite sprite = AddSprite(
									   new Rect(rx, ry, width*scaleWidth, height*scaleHeight),
									   rotAngle,
									   ComputedWidgetLayer*-(1.0f/GUIBase_Layout.MAX_LAYERS),
									   texU,
									   texV,
									   texW,
									   texH,
									   grid9);

		//	Debug.Log(name + " depth = " + (-((float)m_Layout.m_LayoutLayer + (float)m_GuiWidgetLayer * 0.1f)));

		if (sprite != null)
		{
			if (m_Sprites == null)
			{
				ReserveSprites(1);
				resIdx = m_UnusedSpriteIndex++;
			}
			else if (ReservedSpritesSize > 0 && m_UnusedSpriteIndex < ReservedSpritesSize)
			{
				resIdx = m_UnusedSpriteIndex++;
			}
			else
			{
				// reallocate (add 1 sprite)
				ReallocateSprites(m_Sprites.Length + 1);
				resIdx = m_UnusedSpriteIndex++;
			}

			m_Sprites[resIdx].m_Sprite = sprite;
			m_Sprites[resIdx].m_IsVisible = false;
			m_Sprites[resIdx].m_Pos = centerSpritePos;
			m_Sprites[resIdx].m_Width = width;
			m_Sprites[resIdx].m_Height = height;
			m_Sprites[resIdx].m_Grid9 = grid9;

			// Show sprite ?
			ShowSprite(resIdx, Visible);
		}

		//Debug.Log("AddSprite = "+ resIdx +" for "+ gameObject.name);

		return resIdx;
	}

	public void PrepareSprites(int count)
	{
		if (m_GuiRenderer == null)
			return;
		if (m_Sprites != null && m_Sprites.Length == count)
			return;

		// prepare temp list
		S_Sprite[] sprites = new S_Sprite[count];

		int spriteIdx = 0;

		// remove all unwanted sprites
		if (m_Sprites != null)
		{
			for (int idx = 0; idx < m_Sprites.Length; ++idx)
			{
				if (m_Sprites[idx].m_Sprite == null)
					continue;

				if (spriteIdx < count)
				{
					sprites[spriteIdx++] = m_Sprites[idx];
				}
				else
				{
					m_GuiRenderer.RemoveSprite(m_Sprites[idx].m_Sprite);
				}
			}
		}

		// add new sprites if needed
		while (spriteIdx < count)
		{
			MFGuiSprite sprite = AddSprite(
										   new Rect(0, 0, 0, 0),
										   0,
										   ComputedWidgetLayer*-(1.0f/GUIBase_Layout.MAX_LAYERS),
										   0,
										   0,
										   0,
										   0,
										   null);

			sprites[spriteIdx++].m_Sprite = sprite;
		}

		// store new list
		m_Sprites = sprites;

		// update visibility
		for (int idx = 0; idx < m_Sprites.Length; ++idx)
		{
			ShowSprite(idx, Visible);
		}
	}

	public void RemoveSprites()
	{
		if (m_Sprites != null && m_GuiRenderer != null)
		{
			for (int i = 0; i < m_Sprites.Length; ++i)
			{
				if (m_Sprites[i].m_Sprite != null)
				{
					m_GuiRenderer.RemoveSprite(m_Sprites[i].m_Sprite);
				}
			}
		}

		m_Sprites = null;
	}

	//---------------------------------------------------------
	/*public void GUIInternal_ClearSprites()
	{
		m_Sprite = null;
	}*/

	//---------------------------------------------------------
	public MFGuiSprite GetSprite(int idx)
	{
		if (m_Sprites == null)
			return null;
		if (idx < 0 || idx >= m_Sprites.Length)
		{
			Debug.LogWarning(GetType().Name + "<" + this.GetFullName('.') + ">.GetSprite() :: Attempt to access invalid index '" + idx + "'!",
							 gameObject);
			return null;
		}

		return m_Sprites[idx].m_Sprite;
	}

	public bool IsSpriteIndexValid(int idx)
	{
		if (m_Sprites == null)
			return false;

		return idx >= 0 && idx < m_Sprites.Length;
	}

	//---------------------------------------------------------
	public void UpdateSpritePosAndSize(int idx, float posX, float posY, float width, float height)
	{
		if (m_Sprites == null)
			return;
		if (idx < 0 || idx >= m_Sprites.Length)
		{
			Debug.LogWarning(
							 GetType().Name + "<" + this.GetFullName('.') + ">.UpdateSpritePosAndSize() :: Attempt to access invalid index '" + idx + "'!",
							 gameObject);
			return;
		}

		m_Sprites[idx].m_Pos.x = posX;
		m_Sprites[idx].m_Pos.y = posY;
		m_Sprites[idx].m_Width = width;
		m_Sprites[idx].m_Height = height;

		SetModify();
	}

	//---------------------------------------------------------
	public void SetSpriteSize(int idx, float width, float height)
	{
		if (m_Sprites == null)
			return;
		if (idx < 0 || idx >= m_Sprites.Length)
		{
			Debug.LogWarning(
							 GetType().Name + "<" + this.GetFullName('.') + ">.UpdateSpritePosAndSize() :: Attempt to access invalid index '" + idx + "'!",
							 gameObject);
			return;
		}

		if (Mathf.RoundToInt(m_Sprites[idx].m_Width) == Mathf.RoundToInt(width) &&
			Mathf.RoundToInt(m_Sprites[idx].m_Height) == Mathf.RoundToInt(height))
		{
			return;
		}

		m_Sprites[idx].m_Width = width;
		m_Sprites[idx].m_Height = height;

		SetModify();
	}

	//---------------------------------------------------------
	public void SetSpritePos(Vector2 pos)
	{
		if (m_Sprites == null)
			return;
		if (m_Sprites.Length == 0)
		{
			Debug.LogWarning(GetType().Name + "<" + this.GetFullName('.') + ">.SetSpritePos() :: Attempt to access invalid index '0'!", gameObject);
			return;
		}

		m_Sprites[0].m_Pos.x = pos.x;
		m_Sprites[0].m_Pos.y = pos.y;

		SetModify();
	}

	//---------------------------------------------------------
	public void RegisterCallback(GUIBase_Callback obj, int clbkTypes)
					// where clbkTypes is combuination of GUIBase_Callback.E_CallbackType flags
	{
		m_Callback = obj;

		m_Callback.RegisterCallbackType(clbkTypes);
	}

	//---------------------------------------------------------
	public void PlayAnim(Animation animation,
						 GUIBase_Widget widget,
						 GUIBase_Platform.AnimFinishedDelegate finishDelegate = null,
						 int customIdx = -1)
	{
		m_Layout.PlayAnim(animation, widget, finishDelegate, customIdx);
	}

	//---------------------------------------------------------
	public void StopAnim(Animation animation)
	{
		m_Layout.StopAnim(animation);
	}

	//---------------------------------------------------------
	protected override void OnGUIUpdate(float parentAlpha)
	{
		if (IsDirty == false)
			return;

		if (m_UpdateDelegate != null)
		{
			m_UpdateDelegate();
		}

		if (m_Sprites != null)
		{
			Vector4 currColor = m_Color;
			Transform tr = gameObject.transform;
			Vector3 pos = tr.position;
			Vector3 scale = tr.lossyScale;
			Quaternion rot = tr.rotation; //faster way of detecting the rotation change (no need to decompose to euler angles)

			currColor.w *= FadeAlpha*parentAlpha;

			bool transformChanged = false;
			bool rotChanged = false;

#if	GUI_ENABLE_ANIMATIONS
			transformChanged = pos != m_Pos || scale != m_Scale;

			if (!transformChanged)
			{
				rotChanged = rot != m_Quat;
			}
#endif

#if GUI_ENABLE_ANIMATIONS
			m_Pos = pos;
			m_Scale = scale;
			m_Quat = rot;
#endif
			Vector2 deltaPos;
			Vector2 dPos;
			Vector2 rotDelta;

			if (rotChanged)
			{
				m_RotZ = m_Quat.eulerAngles.z; //decompose only when really necessary
			}

			float rot_z = m_RotZ;
			float angle = -rot_z*Mathf.Deg2Rad;

			deltaPos.x = pos.x - m_OrigPos.x;
			deltaPos.y = pos.y - m_OrigPos.y;

			for (int i = 0; i < m_Sprites.Length; ++i)
			{
				S_Sprite s = m_Sprites[i];

				if (s.m_IsVisible)
				{
					float width = s.m_Width; // * scale.x;
					float height = s.m_Height; // * scale.y;

					if (m_TextScaleFix_HACK == false)
					{
						dPos.x = s.m_Pos.x - m_OrigPos.x;
						dPos.y = s.m_Pos.y - m_OrigPos.y;
					}
					else
					{
						dPos.x = (s.m_Pos.x - m_OrigPos.x)*scale.x;
						dPos.y = (s.m_Pos.y - m_OrigPos.y)*scale.y;
					}

					float cos = Mathf.Cos(angle);
					float sin = Mathf.Sin(angle);

					rotDelta.x = dPos.x*cos + dPos.y*sin;
					rotDelta.y = -dPos.x*sin + dPos.y*cos;

					float rx = m_OrigPos.x + deltaPos.x + rotDelta.x;
					float ry = m_OrigPos.y + deltaPos.y + rotDelta.y;

					UpdateSprite(s.m_Sprite, new Rect(rx, ry, width, height), rot_z, scale, -(float)m_Layout.m_LayoutLayer, m_Grid9);

					s.m_Sprite.color = currColor;
				}
			}
		}
	}

	protected override void OnLayoutChanged()
	{
		RemoveSprites();
	}

	//---------------------------------------------------------
	//Vrati rectangle widgetu ve screen coordinatech.
	public void GetScreenCoords(out int x, out int y, out int w, out int h)
	{
		Transform trans = transform;
		Vector3 pos = trans.position;
		Vector3 scale = trans.lossyScale;

		float rx = pos.x;
		float ry = pos.y;
		float width = m_Width*scale.x;
		float height = m_Height*scale.y;

		rx -= width*0.5f;
		ry -= height*0.5f;

		//	rx /= m_LayoutScale.x;
		//	ry /= m_LayoutScale.y;

		x = (int)rx;
		y = (int)ry;
		w = (int)(width /*/ m_LayoutScale.x*/);
		h = (int)(height /*/ m_LayoutScale.y*/);
	}

	//---------------------------------------------------------
	//Vrati rectangle widgetu ve screen coordinatech.
	public Rect GetRectInScreenCoords()
	{
		Transform trans = transform;
		Vector3 pos = trans.position;
		Vector3 scale = trans.lossyScale;

		float width = m_Width*scale.x;
		float height = m_Height*scale.y;
		float rx = pos.x - width*0.5f;
		float ry = pos.y - height*0.5f;

		float x = (rx /*/ m_LayoutScale.x*/);
		float y = (ry /*/ m_LayoutScale.y*/);
		float w = (width /*/ m_LayoutScale.x*/);
		float h = (height /*/ m_LayoutScale.y*/);

		return new Rect(x, y, w, h);
	}

	//---------------------------------------------------------
	public bool IsTouchSensitive()
	{
		if (m_Callback)
		{
			return m_Callback.TestFlag((int)GUIBase_Callback.E_CallbackType.E_CT_ON_TOUCH_BEGIN);
		}

		return false;
	}

	//---------------------------------------------------------
	public void ShowImmediate(bool visible, bool recursive)
	{
		//Debug.Log(">>>> GUIBase_Widget<"+this.GetFullName('.')+">.ShowImmediate("+visible+", "+recursive+")");
		if (visible == true && DisallowShowRecursive == true)
		{
			recursive = false;
		}

		if (recursive)
		{
			GUIBase_Widget[] children = GetComponentsInChildren<GUIBase_Widget>();

			foreach (GUIBase_Widget widget in children)
			{
				widget.ShowImmediate(visible, false);
			}
		}
		else if (Visible != visible)
		{
			Visible = visible;

			if (m_Sprites != null)
			{
				for (int idx = 0; idx < m_Sprites.Length; ++idx)
				{
					ShowSprite(idx, visible);
				}
			}

			if (visible == true && m_Callback && m_Callback.TestFlag((int)GUIBase_Callback.E_CallbackType.E_CT_SHOW))
			{
				m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_SHOW, visible);
			}
			else if (visible == false && m_Callback && m_Callback.TestFlag((int)GUIBase_Callback.E_CallbackType.E_CT_HIDE))
			{
				m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_HIDE, visible);
			}
		}
	}

	//---------------------------------------------------------
	public void ShowSprite(int idx, bool showFlag)
	{
		if (m_Sprites != null)
		{
			if (idx >= 0 && idx < m_Sprites.Length)
			{
				m_Sprites[idx].m_IsVisible = showFlag;

				if (showFlag)
				{
					//Debug.Log("show sprite from '"+ gameObject.name +"', idx = "+idx);
					if (Visible == true)
						ShowSprite(m_Sprites[idx].m_Sprite);
				}
				else
				{
					//Debug.Log("hide sprite from '"+ gameObject.name +"', idx = "+idx);
					HideSprite(m_Sprites[idx].m_Sprite);
				}
			}
		}
	}

	//---------------------------------------------------------
	// Test if mouse is over widget
	public bool IsMouseOver(Vector2 clickPos)
	{
		int cx;
		int cy;
		int cw;
		int ch;

		GetScreenCoords(out cx, out cy, out cw, out ch);

		float scaleWidth = 1.0f;
		float scaleHeight = 1.0f;

		// Modify touch area
		if (m_Callback != null)
		{
			m_Callback.GetTouchAreaScale(out scaleWidth, out scaleHeight);
		}

		int newWidth = (int)(cw*scaleWidth);
		int newHeight = (int)(ch*scaleHeight);

		cx += (cw - newWidth)/2;
		cy += (ch - newHeight)/2;
		cw = newWidth;
		ch = newHeight;

//		Debug.Log(name +  " clickPos.x = " + clickPos.x + ", clickPos.y = " + clickPos.y + " cx " + cx + " cy " + cy);

		if ((clickPos.x >= cx) &&
			(clickPos.x < (cx + cw)) &&
			(clickPos.y >= cy) &&
			(clickPos.y < (cy + ch)))
		{
//			Debug.Log("true");
			return true;
		}

		return false;
	}

	//---------------------------------------------------------
	public bool HandleTouchEvent(E_TouchPhase touchPhase, object evt, bool isMouseOver = true)
	{
		switch (touchPhase)
		{
		case E_TouchPhase.E_TP_CLICK_BEGIN:
			return m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_TOUCH_BEGIN, evt);

		case E_TouchPhase.E_TP_NONE:
			return m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_TOUCH_UPDATE, evt);

		case E_TouchPhase.E_TP_CLICK_RELEASE:
			if (isMouseOver)
			{
				return m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_TOUCH_END, evt);
			}
			else
			{
				return m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_TOUCH_END_OUTSIDE, evt);
			}

		case E_TouchPhase.E_TP_MOUSEOVER_BEGIN:
			return m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_MOUSEOVER_BEGIN, evt);

		case E_TouchPhase.E_TP_MOUSEOVER_END:
			return m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_MOUSEOVER_END, evt);

		// HACK - tohle vzniklo kvuli problemum s rozlisenim inputu z klavesnice. Nevime jestli byla klavesa zmacknuta "nad" nebo "mimo" button. Samozrejme je vzdycky mimo... akorat nevime v jakem stavu je zrovna button (graficky stav)
		case E_TouchPhase.E_TP_CLICK_RELEASE_KEYBOARD:
			return m_Callback.Callback(GUIBase_Callback.E_CallbackType.E_CT_ON_TOUCH_END_KEYBOARD, evt);
		}

		return false;
	}
}
