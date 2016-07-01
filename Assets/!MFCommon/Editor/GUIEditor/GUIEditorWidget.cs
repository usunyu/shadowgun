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

//
// GUIEditorWidget
//	Window of GUIEditor plugin where user can modify UV of each GUI widget.
//
//

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class GUIEditorWidget
{
	private readonly int		PrimaryButton    = 0;
	private readonly List<int>	SecondaryButtons = new List<int>() {1, 2};

	public class EditedArea
	{
		public Rect       Area      = new Rect(0, 0, 100, 100);
		public bool       UsesGrid9 = false;
		public RectOffset Slices    = new RectOffset(10, 10, 10, 10);
		
		public void CopyFrom(EditedArea other)
		{
			Area.x        = other.Area.x;
			Area.y        = other.Area.y;
			Area.width    = other.Area.width;
			Area.height   = other.Area.height;
			UsesGrid9     = other.UsesGrid9;
			Slices.left   = other.Slices.left;
			Slices.right  = other.Slices.right;
			Slices.top    = other.Slices.top;
			Slices.bottom = other.Slices.bottom;
		}
	}
	
	static float		m_PointSize						= 15.0f;
	static Vector2      m_ScrollPos                     = new Vector2();
	static int          m_DefaultLabelWidth             = 80;
	
    string[]			m_ZoomButtonsSelStrings 		= new string[] {"+","-"};
		
	bool				m_MouseDown;
	
	GUIBase_Widget		m_SelectedWidget				= null;
	
	EditedArea			m_EditedArea					= new EditedArea();
	static public EditedArea 	m_EditedAreaCopy		= new EditedArea();
	
	bool				m_TexturePosPrepared			= false;
	Vector2				m_TexturePos					= new Vector2(0, 0);
	Vector2				m_TextureSize					= new Vector2(0.0f, 0.0f);
	
	int					m_SelectedPointIdx				= -1;
	
	float				m_ZoomFactor					= 1.0f;

	Vector2				m_PlatformShift					= new Vector2(0.0f, 0.0f);
	
	bool				m_Pan							= false;
	bool				m_WidgetDrag					= false;
	
	Vector2				m_DragOrig						= new Vector2();
	Vector2				m_DragMouse						= new Vector2();
	
	//---------------------------------------------------------------------------------------------
	//
	// ctor
	//
	//---------------------------------------------------------------------------------------------
	public GUIEditorWidget ()
	{
	}
	
	//---------------------------------------------------------------------------------------------
	// 
	// OnGUI
	//
	//---------------------------------------------------------------------------------------------
	public void OnGUI(ref bool mouseDown)
	{
		// Update selection by Hierarchy view
		UpdateSelectionByHiearchyView();
		
		// Update edited area
		UpdateEditedArea();
		
		// store input param
		m_MouseDown		= mouseDown;

		// Process input - mouse click, movement...
		ProcessInput();
		
		EditorGUILayout.BeginVertical();
			// Show Top Panel
			RenderTopPanel();

			// Render Left and Right panels of this plugin
			EditorGUILayout.BeginHorizontal();
		
				RenderLeftPanel();
				RenderRightPanel();
	
			EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();
		
		mouseDown		= m_MouseDown;
	}

	//---------------------------------------------------------------------------------------------
	//
	// UpdateSelectionByHiearchyView
	//
	// Take from Unity "Selection.activeGameObject" and select it internaly in plugin 
	//
	//---------------------------------------------------------------------------------------------
	void UpdateSelectionByHiearchyView()
	{
		if (Selection.activeGameObject)
		{
			GUIBase_Widget widget = Selection.activeGameObject.GetComponent<GUIBase_Widget>();
			
			if (widget != m_SelectedWidget)
			{
				m_SelectedWidget = widget;
				if (widget != null)
				{
					Vector2 offset = widget.m_InTexPos;
					if (widget.m_InTexSize.x < 0.0f) offset.x += widget.m_InTexSize.x;
					if (widget.m_InTexSize.y < 0.0f) offset.y += widget.m_InTexSize.y;
					SetPlatformShift(offset * -1.0f + new Vector2(0.02f, 0.02f));
				}
			}
		}
		else
		{
			m_SelectedWidget = null;
		}
	}
	
	//---------------------------------------------------------------------------------------------
	//
	// UpdateEditedArea
	//
	// Update m_EditedArea by values from m_SelectedWidget (its UV - InTexPos and InTexSize) 
	//
	//---------------------------------------------------------------------------------------------
	void UpdateEditedArea()
	{
		CopyFromWidget(m_SelectedWidget, m_EditedArea);
	}
	
	//zkopiruje widget parametry do edit arey
	static public void CopyFromWidget(GUIBase_Widget selWidget, EditedArea targetArea)
	{
		if (selWidget)
		{
			Texture	texture = selWidget.GetTexture();
						
			if (texture)
			{
				float width		= texture.width;
				float height	= texture.height;
								
				targetArea.Area.x      = selWidget.m_InTexPos.x * width;
				targetArea.Area.y      = selWidget.m_InTexPos.y * height;
									
				targetArea.Area.width  = selWidget.m_InTexSize.x * width;
				targetArea.Area.height = selWidget.m_InTexSize.y * height;

				targetArea.UsesGrid9     = selWidget.m_Grid9.Enabled;
				targetArea.Slices.left   = Mathf.RoundToInt(selWidget.m_Grid9.LeftSlice   * targetArea.Area.width);
				targetArea.Slices.top    = Mathf.RoundToInt(selWidget.m_Grid9.TopSlice    * targetArea.Area.height);
				targetArea.Slices.right  = Mathf.RoundToInt(selWidget.m_Grid9.RightSlice  * targetArea.Area.width);
				targetArea.Slices.bottom = Mathf.RoundToInt(selWidget.m_Grid9.BottomSlice * targetArea.Area.height);
			}
		}
	}
	
	void RenderTopPanel()
	{
		GUIEditorUtils.LookLikeControls();

		EditorGUILayout.BeginHorizontal(GUILayout.Height(28));
			GUILayout.FlexibleSpace();
			ShowZoom();
			EditorGUILayout.Separator();
		EditorGUILayout.EndVertical();
	}

	//---------------------------------------------------------------------------------------------
	//
	// Show Left Panel
	//		- parameters of selected widget
	//		- zoom buttons
	//		- etc.
	//
	//---------------------------------------------------------------------------------------------
	void RenderLeftPanel()
	{
		GUIEditorUtils.LookLikeControls(m_DefaultLabelWidth);

		// Show left panel with params
		m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos, GUILayout.Width(290));
			EditorGUILayout.BeginVertical();
				GUI.enabled = m_SelectedWidget ? true : false;
		
				// Edit widget params
				ShowWidgetParams();
				EditorGUILayout.Separator();

				ShowGrid9Params();
				EditorGUILayout.Separator();
		
				ShowCopyPaste();
				EditorGUILayout.Separator();
		
				GUI.enabled = true;
			EditorGUILayout.EndVertical();
		EditorGUILayout.EndScrollView();
	}
	
	//---------------------------------------------------------------------------------------------
	//
	// ShowWidgetParams
	//  Show UV and size of selected widget
	//
	//---------------------------------------------------------------------------------------------
	void ShowWidgetParams()
	{
		//
		// Enable modification of U,V,Width,Height
		//
		int	u = Mathf.RoundToInt(m_EditedArea.Area.x);
		int v = Mathf.RoundToInt(m_EditedArea.Area.y);
		int w = Mathf.RoundToInt((m_EditedArea.Area.x + m_EditedArea.Area.width) - u);
		int h = Mathf.RoundToInt((m_EditedArea.Area.y + m_EditedArea.Area.height) - v);
		
		int	oldU = u;
		int	oldV = v;
		int	oldW = w;
		int	oldH = h;
			
		Texture	texture = m_SelectedWidget ? m_SelectedWidget.GetTexture() : null;
		int    texWidth = texture ? texture.width  : 1024;
		int   texHeight = texture ? texture.height : 1024;
		
		EditorGUILayout.BeginVertical();
			GUIEditorUtils.LookLikeControls(20);
				EditorGUILayout.LabelField("Mapping", EditorStyles.boldLabel);
				u = EditorGUILayout.IntSlider("U", u, -texWidth,  texWidth  * 2);
				v = EditorGUILayout.IntSlider("V", v, -texHeight, texHeight * 2);
				w = EditorGUILayout.IntSlider("W",     w, -texWidth,  texWidth);
				h = EditorGUILayout.IntSlider("H",    h, -texHeight, texHeight);
			GUIEditorUtils.LookLikeControls(m_DefaultLabelWidth);
		EditorGUILayout.EndVertical();
	
		if (m_SelectedWidget)
		{
			m_EditedArea.Area.x      = u;
			m_EditedArea.Area.y      = v;
			m_EditedArea.Area.width  = w;
			m_EditedArea.Area.height = h;

			bool dirtyFlag = (oldU != u) || (oldV != v) || (oldW != w) || (oldH != h);

			UpdateWidgetByArea(m_SelectedWidget, m_EditedArea, dirtyFlag);
		}
	}
		
	void ShowGrid9Params()
	{
		int	u = Mathf.RoundToInt(m_EditedArea.Area.x);
		int v = Mathf.RoundToInt(m_EditedArea.Area.y);
		int w = Mathf.RoundToInt((m_EditedArea.Area.x + m_EditedArea.Area.width) - u);
		int h = Mathf.RoundToInt((m_EditedArea.Area.y + m_EditedArea.Area.height) - v);
		
		int gl = m_EditedArea.Slices.left;
		int gt = m_EditedArea.Slices.top;
		int gr = m_EditedArea.Slices.right;
		int gb = m_EditedArea.Slices.bottom;

		int	oldGl = gl;
		int	oldGt = gt;
		int	oldGr = gr;
		int	oldGb = gb;

		bool useGrid9    = m_EditedArea.UsesGrid9;
		bool oldUseGrid9 = useGrid9;
		
		EditorGUILayout.BeginVertical();
		
			useGrid9 = EditorGUILayout.BeginToggleGroup("Use Grid-9", useGrid9);
				gl = EditorGUILayout.IntSlider("Left Slice",   gl, 0, System.Math.Max(0, w - gr));
				gt = EditorGUILayout.IntSlider("Top Slice",    gt, 0, System.Math.Max(0, h - gb));
				gr = EditorGUILayout.IntSlider("Right Slice",  gr, 0, System.Math.Max(0, w - gl));
				gb = EditorGUILayout.IntSlider("Bottom Slice", gb, 0, System.Math.Max(0, h - gt));
			EditorGUILayout.EndToggleGroup();
		
		EditorGUILayout.EndVertical();
		
		if (m_SelectedWidget)
		{
			m_EditedArea.Slices.left   = gl;
			m_EditedArea.Slices.top    = gt;
			m_EditedArea.Slices.right  = gr;
			m_EditedArea.Slices.bottom = gb;
			
			m_EditedArea.UsesGrid9 = useGrid9;
			
			bool dirtyFlag = (oldUseGrid9 != useGrid9) || (oldGl != gl) || (oldGt != gt) || (oldGr != gr) || (oldGb != gb);
			
			UpdateWidgetByArea(m_SelectedWidget, m_EditedArea, dirtyFlag);
		}
	}
	
	//---------------------------------------------------------------------------------------------
	//
	// ShowZoom
	//	Show current Zoom and also buttons for changing it
	//
	//---------------------------------------------------------------------------------------------
	void ShowZoom()
	{
		GUIEditorUtils.LookLikeControls(40);
		EditorGUILayout.BeginHorizontal();
		
			m_ZoomFactor = EditorGUILayout.IntField("Zoom", Mathf.RoundToInt(m_ZoomFactor * 100.0f), GUILayout.Width(75)) / 100.0f;
			
			GUI.SetNextControlName("ZoomButtons");
			switch (GUILayout.Toolbar(-1, m_ZoomButtonsSelStrings, GUILayout.Width(50)))
			{
			case 0:
				m_ZoomFactor += 0.01f;
				GUI.FocusControl("ZoomButtons");
				break;
	
			case 1:
				m_ZoomFactor -= 0.01f;
				GUI.FocusControl("ZoomButtons");
				break;
			}
			
			m_ZoomFactor = Mathf.Clamp(m_ZoomFactor, GUIEditorUtils.MIN_SCALE_FACTOR, GUIEditorUtils.MAX_SCALE_FACTOR);

		EditorGUILayout.EndHorizontal();
		GUIEditorUtils.LookLikeControls(m_DefaultLabelWidth);
	}
	
	void ShowCopyPaste()
	{
		EditorGUILayout.BeginHorizontal();
		
		if (GUILayout.Button("Copy"))
		{
			m_EditedAreaCopy.CopyFrom(m_EditedArea);
			Debug.Log("Copy: widget area stored in memory " + m_EditedArea);
		}
		else if (GUILayout.Button("Paste"))
		{
			if (m_SelectedWidget)
			{
				bool dirtyFlag = !m_EditedArea.Area.Equals(m_EditedAreaCopy);
				m_EditedArea.CopyFrom(m_EditedAreaCopy);
				UpdateWidgetByArea(m_SelectedWidget, m_EditedArea, dirtyFlag);
				Debug.Log("Paste: area applied to selected widget " + m_EditedArea + " " + m_SelectedWidget.name);
			}
		}
		
		EditorGUILayout.EndHorizontal();
	}
	
	//---------------------------------------------------------------------------------------------
	//
	// Show selected widget and enable its editation
	//
	//	To right (main) window draws texture for current selected widget
	//  Also render area (rectangle) in the texture witch represents widget
	//
	//---------------------------------------------------------------------------------------------
	void RenderRightPanel()
	{
		// Show selected widget and enable its editation
		EditorGUILayout.BeginVertical(GUILayout.MinWidth(300));

		if (m_SelectedWidget)
		{
			Material mat = m_SelectedWidget.GetMaterial();
			
			if (mat && mat.mainTexture)
			{
				// Draw texture
				EditorGUILayout.Separator();

				Rect	r = GUILayoutUtility.GetLastRect();
				
				// Go a little bit far from border 
				//r.x	+= 10;
				//r.y	+= r.height + 10;
				
				// Take zoom into account
				r.width		= mat.mainTexture.width * m_ZoomFactor * (1.0f + m_PlatformShift.x);
				r.height	= mat.mainTexture.height * m_ZoomFactor * (1.0f + m_PlatformShift.y);
				
				Rect	sourceRect = new Rect(-m_PlatformShift.x, 0.0f, 1.0f + m_PlatformShift.x, 1.0f + m_PlatformShift.y);
				
				Graphics.DrawTexture(r, mat.mainTexture, sourceRect, 0, 0, 0, 0, mat);
				
				// Edit area ?
				if (Event.current.type == EventType.Repaint)	// values are not correct during other events !!! 
				{
					m_TexturePosPrepared = true;
			
					m_TexturePos.x = r.x;
					m_TexturePos.y = r.y;
					
					m_TextureSize.x	= mat.mainTexture.width;
					m_TextureSize.y	= mat.mainTexture.height;
					
					//if ((m_SelectedPointIdx != -1) || m_WidgetDrag)
					if (m_SelectedPointIdx != -1)
					{
						MoveWithSelectedVertex();
					}
				}
				
				// Draw area rectangle (of selected/edited widget)
				Rect	tmpArea		= new Rect(m_EditedArea.Area.x, m_EditedArea.Area.y, m_EditedArea.Area.width, m_EditedArea.Area.height);
				Vector2	layoutShift	= new Vector2(m_PlatformShift.x * m_TextureSize.x * m_ZoomFactor, m_PlatformShift.y * m_TextureSize.y * m_ZoomFactor);
				
				GUIEditorUtils.SetClipPoint(m_TexturePos);
				
				GUIEditorUtils.DrawRect(new Vector2(r.x, r.y), tmpArea, 0.0f, m_ZoomFactor, layoutShift, true, GUIEditorUtils.COLOR_AREA_EDGE, GUIEditorUtils.COLOR_AREA_POINT, GUIEditorUtils.COLOR_AREA_SELECTED_POINT, true, m_PointSize, m_SelectedPointIdx);
				
				//HACK rewrite to DrawLine() later !!!!
				if (m_EditedArea.UsesGrid9 == true)
				{
					GUIEditorUtils.DrawRect(new Vector2(r.x, r.y), new Rect(tmpArea.x + m_EditedArea.Slices.left, tmpArea.y, 0, tmpArea.height), 0.0f, m_ZoomFactor, layoutShift, GUIEditorUtils.COLOR_AREA_SLICE);
					GUIEditorUtils.DrawRect(new Vector2(r.x, r.y), new Rect(tmpArea.x + tmpArea.width - m_EditedArea.Slices.right, tmpArea.y, 0, tmpArea.height), 0.0f, m_ZoomFactor, layoutShift, GUIEditorUtils.COLOR_AREA_SLICE);
					GUIEditorUtils.DrawRect(new Vector2(r.x, r.y), new Rect(tmpArea.x, tmpArea.y + m_EditedArea.Slices.top, tmpArea.width, 0), 0.0f, m_ZoomFactor, layoutShift, GUIEditorUtils.COLOR_AREA_SLICE);
					GUIEditorUtils.DrawRect(new Vector2(r.x, r.y), new Rect(tmpArea.x, tmpArea.y + tmpArea.height - m_EditedArea.Slices.bottom, tmpArea.width, 0), 0.0f, m_ZoomFactor, layoutShift, GUIEditorUtils.COLOR_AREA_SLICE);
				}
				
				//GUIEditorUtils.DrawRect(new Vector2(r.x, r.y), new Rect(0, 0, r.width, r.height), 0.0f, 1.0f, new Vector2(0,0), GUIEditorUtils.COLOR_LAYOUT_AREA);
			}
		}

		EditorGUILayout.EndVertical();
	}
	
	//---------------------------------------------------------------------------------------------
	//
	// Move with selected vertex
	//
	//	Update edited area by the movement with the selected point of area
	//
	//---------------------------------------------------------------------------------------------
	void MoveWithSelectedVertex()
	{
		Vector2		mousePos	= Event.current.mousePosition;
		Vector2		pos			= new Vector2();
		Vector2		delta		= new Vector2();
		Texture		texture		= m_SelectedWidget.GetTexture();
								
		// new position of vertex (in texture space)
		pos.x = mousePos.x - m_TexturePos.x;
		pos.y = mousePos.y - m_TexturePos.y;

		pos.x /= m_ZoomFactor;
		pos.y /= m_ZoomFactor;
		
		pos.x -= m_PlatformShift.x * m_TextureSize.x;
		pos.y -= m_PlatformShift.y * m_TextureSize.y;

		pos.x = Mathf.Clamp(pos.x, 0.0f, texture.width);
		pos.y = Mathf.Clamp(pos.y, 0.0f, texture.height);
		
		switch (m_SelectedPointIdx)
		{
		case 0:
			delta.x				= pos.x - m_EditedArea.Area.x;
			delta.y				= pos.y - m_EditedArea.Area.y;
			
			m_EditedArea.Area.x		+= delta.x;
			m_EditedArea.Area.y		+= delta.y;
			
			m_EditedArea.Area.width	-= delta.x;
			m_EditedArea.Area.height	-= delta.y;
			break;
			
		case 1:
			delta.x				= pos.x - (m_EditedArea.Area.x + m_EditedArea.Area.width);
			delta.y				= pos.y - m_EditedArea.Area.y;
			
			m_EditedArea.Area.y		+= delta.y;
			
			m_EditedArea.Area.width	+= delta.x;
			m_EditedArea.Area.height	-= delta.y;
			break;
			
		case 2:
			delta.x				= pos.x - (m_EditedArea.Area.x + m_EditedArea.Area.width);
			delta.y				= pos.y - (m_EditedArea.Area.y + m_EditedArea.Area.height);
			
			m_EditedArea.Area.width	+= delta.x;
			m_EditedArea.Area.height	+= delta.y;
			break;

		case 3:
			delta.x				= pos.x - m_EditedArea.Area.x;
			delta.y				= pos.y - (m_EditedArea.Area.y + m_EditedArea.Area.height);
			
			m_EditedArea.Area.x		+= delta.x;
			
			m_EditedArea.Area.width	-= delta.x;
			m_EditedArea.Area.height += delta.y;
			break;
			
		default:
			// movement with whole widget
			delta.x				= pos.x - m_EditedArea.Area.x;
			delta.y				= pos.y - m_EditedArea.Area.y;
			
			m_EditedArea.Area.x		+= delta.x;
			m_EditedArea.Area.y		+= delta.y;
			break;
		}
		
		UpdateWidgetByArea(m_SelectedWidget, m_EditedArea, true);
	}
	
	//---------------------------------------------------------------------------------------------
	//
	// Update area (UV and size) in widget
	//	it means widget's inTexPos and inTexSize will be modified
	//
	//---------------------------------------------------------------------------------------------
	static public void UpdateWidgetByArea(GUIBase_Widget widget, EditedArea editedArea, bool dirtyFlag)
	{
		if (widget)
		{
			Texture	texture = widget.GetTexture();
			
			if (texture)
			{
				float 	width	= texture.width;
				float	height	= texture.height;
				
				widget.m_InTexPos.x  = editedArea.Area.x / width;
				widget.m_InTexPos.y  = editedArea.Area.y / height;
				
				widget.m_InTexSize.x = editedArea.Area.width / width;
				widget.m_InTexSize.y = editedArea.Area.height / height;
				
				widget.m_Grid9.Enabled     = editedArea.UsesGrid9;
				widget.m_Grid9.LeftSlice   = editedArea.Area.width  != 0.0f ? editedArea.Slices.left   / editedArea.Area.width  : 0.0f;
				widget.m_Grid9.TopSlice    = editedArea.Area.height != 0.0f ? editedArea.Slices.top    / editedArea.Area.height : 0.0f;
				widget.m_Grid9.RightSlice  = editedArea.Area.width  != 0.0f ? editedArea.Slices.right  / editedArea.Area.width  : 0.0f;
				widget.m_Grid9.BottomSlice = editedArea.Area.height != 0.0f ? editedArea.Slices.bottom / editedArea.Area.height : 0.0f;
				
				if (dirtyFlag)
				{
					EditorUtility.SetDirty(widget);
				}
			}
		}
	}
	
	//---------------------------------------------------------------------------------------------
	//
	// Process Input
	//
	// check witch point of selected widget will be dragged etc. 
	//
	//---------------------------------------------------------------------------------------------
	void ProcessInput()
	{
		Event e = Event.current;
		
		if (e.type == EventType.ScrollWheel)
		{
			// Zoom
			
			if (e.delta.y > 0.0f)
			{
				m_ZoomFactor -= 0.1f;
			}
			else if (e.delta.y < 0.0f)
			{
				m_ZoomFactor += 0.1f;
			}
			
			m_ZoomFactor = Mathf.Clamp(m_ZoomFactor, GUIEditorUtils.MIN_SCALE_FACTOR, GUIEditorUtils.MAX_SCALE_FACTOR);
			
			// Pan with "horizontal" wheel
			
			float	pan = e.delta.x * GUIEditorUtils.WHEEL_PAN_MULTIPLICATOR / m_ZoomFactor;

			SetPlatformShift(new Vector2(m_PlatformShift.x + pan, m_PlatformShift.y));
		}
		else if (e.type == EventType.MouseDown && SecondaryButtons.Contains(e.button))
		{
			// pan 
			m_Pan		= true;
			m_DragOrig	= m_PlatformShift;
			m_DragMouse	= Event.current.mousePosition;
		}
		else if (e.type == EventType.MouseUp && SecondaryButtons.Contains(e.button))
		{
			m_Pan = false;
		}
		// Pan ?
		else if (m_Pan && (e.type == EventType.MouseDrag) && SecondaryButtons.Contains(e.button))
		{
			if (m_TexturePosPrepared)
			{
				Vector2 mouseDelta	= new Vector2();
					
				mouseDelta	= Event.current.mousePosition - m_DragMouse;
				mouseDelta /= m_ZoomFactor;
					
				mouseDelta.x /= m_TextureSize.x;
				mouseDelta.y /= m_TextureSize.y;
					
				SetPlatformShift(m_DragOrig + mouseDelta);
			}
		}
		else if (e.type == EventType.MouseDown && e.button == PrimaryButton)
		{
			m_MouseDown = true;
			
			// Select vertex of edited rectangle?
			
			if (m_TexturePosPrepared)
			{
				Vector2	mousePos	= Event.current.mousePosition;
				
				if (mousePos.x >= m_TexturePos.x && mousePos.y >= m_TexturePos.y)
				{
					Vector2	pos		= new Vector2(m_TexturePos.x + m_EditedArea.Area.x * m_ZoomFactor, m_TexturePos.y + m_EditedArea.Area.y * m_ZoomFactor);
				
					pos.x	+= m_TextureSize.x * m_ZoomFactor * m_PlatformShift.x;
					pos.y	+= m_TextureSize.y * m_ZoomFactor * m_PlatformShift.y;
				
					if (GUIEditorUtils.IsPointTouched(mousePos, pos, 0, 0, m_PointSize * 0.5f))
					{
						m_SelectedPointIdx = 0;
						
						Undo.RecordObject(m_SelectedWidget, "Edit UV"); 
					}
					else if (GUIEditorUtils.IsPointTouched(mousePos, pos, m_EditedArea.Area.width * m_ZoomFactor, 0, m_PointSize * 0.5f))
					{
						m_SelectedPointIdx = 1;

						Undo.RecordObject(m_SelectedWidget, "Edit UV"); 
					}
					else if (GUIEditorUtils.IsPointTouched(mousePos, pos, m_EditedArea.Area.width * m_ZoomFactor, m_EditedArea.Area.height * m_ZoomFactor, m_PointSize * 0.5f))
					{
						m_SelectedPointIdx = 2;
						
						Undo.RecordObject(m_SelectedWidget, "Edit UV"); 
					}
					else if (GUIEditorUtils.IsPointTouched(mousePos, pos, 0, m_EditedArea.Area.height * m_ZoomFactor, m_PointSize * 0.5f))
					{
						m_SelectedPointIdx = 3;

						Undo.RecordObject(m_SelectedWidget, "Edit UV"); 
					}
					else
					{
						m_SelectedPointIdx = -1;
						
						//Debug.Log(mousePos.x);
						
						// Move with whole widget ?
						
						float	minX = Mathf.Min(pos.x, pos.x + m_EditedArea.Area.width * m_ZoomFactor);
						float	minY = Mathf.Min(pos.y, pos.y + m_EditedArea.Area.height * m_ZoomFactor);
						
						if ((mousePos.x >= minX) &&
						    (mousePos.y >= minY) &&
						    (mousePos.x < (minX + Mathf.Abs(m_EditedArea.Area.width) * m_ZoomFactor)) &&
						    (mousePos.y < (minY + Mathf.Abs(m_EditedArea.Area.height) * m_ZoomFactor)))   
						{
							//Debug.Log("mouse = "+ mousePos.x + ", minX = " + minX);
							
							m_WidgetDrag	= true;
							
							m_DragOrig.x	= m_EditedArea.Area.x;
							m_DragOrig.y	= m_EditedArea.Area.y;

							m_DragMouse		= mousePos;
						}
					}
				}
			}
		}
		else if (e.type == EventType.MouseUp && e.button == PrimaryButton)
		{			
			m_MouseDown				= false;
			m_SelectedPointIdx		= -1;
			m_TexturePosPrepared	= false;
			
			m_Pan					= false;
			m_WidgetDrag			= false;
		}
		else
		{
			// movement with widget ?
			if (m_WidgetDrag)
			{
				Vector2 mouseDelta	= new Vector2();
					
				mouseDelta	= Event.current.mousePosition - m_DragMouse;
				mouseDelta /= m_ZoomFactor;
				
				m_EditedArea.Area.x	= m_DragOrig.x + mouseDelta.x;
				m_EditedArea.Area.y	= m_DragOrig.y + mouseDelta.y;
				
				EditorUtility.SetDirty(m_SelectedWidget);
			}
		}
	}
	
	private void SetPlatformShift(Vector2 platformShift)
	{
		m_PlatformShift.x = Mathf.Clamp(platformShift.x, -1.0f, 0.0f);
		m_PlatformShift.y = Mathf.Clamp(platformShift.y, -1.0f, 0.0f);
	}
}
