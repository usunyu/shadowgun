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
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class GUIEditorLayout
{
	private readonly int		PrimaryButton    = 0;
	private readonly List<int>	SecondaryButtons = new List<int>() {1, 2};
	
	static int				m_BackgroundTextureSize		= 1;
	static Texture2D		m_BackgroundTexture;
	static Color[]			m_BackgroundTextureColors;
	static Color			m_BackgroundColor			= Color.grey;
	static int              m_DefaultLabelWidth         = 100;
	
    string[]				m_ZoomButtonsSelStrings 	= new string[] {"+","-"};
	
	int						m_GizmoSelGridInt 			= 0;
    string[]				m_GizmoSelStrings 			= new string[] {"Move","Resize","Rotate","Scale"};
	
	bool					m_MouseDown;
		
	Vector2					m_PlatformShift				= new Vector2(0.0f, 0.0f);
	Vector2					m_BackgroundPos				= new Vector2();
	Vector2					m_TopLeftPos				= new Vector2();

	bool					m_Pan						= false;
	Vector2					m_PanOrig					= new Vector2();
	Vector2					m_PanMouse					= new Vector2();
		
	Rect					m_PlatformRectangle			= new Rect();
		
	float					m_ZoomFactor				= 1.0f;
	
	float					m_BackgroundWidth;
	float					m_BackgroundHeight;

	GameObject				m_SelectedGameObject		= null;
	
	struct LayoutInfo
	{
		public GUIBase_Layout   Layout;
		public GUIBase_Pivot    Pivot;
		public GUIBase_Platform Platform;
	}
	
	List<LayoutInfo>		m_Layouts					= new List<LayoutInfo>();
	LayoutInfo				m_Layout					= new LayoutInfo();
	GUIBase_Widget			m_SelectedWidget			= null;
	
	private static Vector2  m_ScrollPos                 = new Vector2();
	
	struct S_SelectedObject
	{
		public GUIBase_Element	m_Element;
		
		public Vector3			m_Offset;
		public Vector3			m_Scale;
		public float			m_RotAngle;
		public Vector2			m_Size;
	};
	
	ArrayList				m_SelectedObjects;
		
	struct S_LayoutDscr
	{
		public GUIBase_Layout		m_Layout;
		public GUIBase_Widget[]		m_Widgets;
	}

	S_LayoutDscr[]			m_PrecachedLayouts			= null;
	
	string					m_PlatformName;
	int						m_PlatformXRes;
	int						m_PlatformYRes;
	
	Vector2					m_PlatformPos	= new Vector2();

	Hashtable				m_FreezeMap		= new Hashtable();
	
	GUIEditorGizmo			m_Gizmo			= new GUIEditorGizmo();
	bool					m_GizmoVisible	= false;
	bool					m_GizmoTouched	= false;
	Vector2					m_GizmoTouchPos;
	float					m_GizmoRotation	= 0.0f;
	Vector2					m_GizmoScale	= new Vector2(1.0f, 1.0f);
	
	float					DEFAULT_ROT_ANGLE_MOUSE_DELTA = 20.0f;
	
	static bool	 			m_OnlySelectedLayouts	= true;
	//static bool	 			m_UseNewTextRendering	= false;
	
	//static public GUIBase_FontEx	m_NewFont;
	//static SystemLanguage selectedlanguage = SystemLanguage.Unknown;
	
		
	//
	// ctor
	//
	public GUIEditorLayout ()
	{
		m_SelectedObjects	= new ArrayList();
	}
		
	void Initialize()
	{
		if (! m_BackgroundTexture)
		{
			m_BackgroundTexture			= new Texture2D(m_BackgroundTextureSize, m_BackgroundTextureSize, TextureFormat.ARGB32, false);
			m_BackgroundTextureColors	= new Color[m_BackgroundTextureSize*m_BackgroundTextureSize];
		
			// Prepare background texture
			for (int i = 0; i < m_BackgroundTextureSize; ++i)
			{
				for (int j = 0; j < m_BackgroundTextureSize; ++j)
				{
				   m_BackgroundTextureColors[i*m_BackgroundTextureSize + j] = m_BackgroundColor;
				}
			}

			m_BackgroundTexture.SetPixels(m_BackgroundTextureColors, 0);
			m_BackgroundTexture.Apply(false);
		}

       	/*if(m_NewFont == null)
        {
			selectedlanguage 		= TextDatabase.instance.databaseLangugae;
        	GameObject guiManager = GameObject.Find("GuiManager");
			if(guiManager != null)
			{
				MFGuiManager mfGuiManager = guiManager.GetComponent<MFGuiManager>();
				if(mfGuiManager != null)
				{
					GUIBase_FontEx font = mfGuiManager.GetFontForLanguage(TextDatabase.instance.databaseLangugae);
					if(font != null)
					{
						m_NewFont = Object.Instantiate(font) as GUIBase_FontEx;
						m_NewFont.gameObject.hideFlags = HideFlags.HideAndDontSave;
						//GUIBase_Label.m_FontEx = m_NewFont;
					}
				}
			}
        }*/
	}
		
	// 
	// OnGUI()
	//
	public void OnGUI(ref bool mouseDown)
	{
		Initialize();

		// Update selection by Hierarchy view
		UpdateSelectionByHiearchyView();
		
		// store input params
		m_MouseDown	= mouseDown;

		// Process input - mouse click, movement...
		ProcessInput();
		
		EditorGUILayout.BeginVertical();
			// Show Top Panel
			RenderTopPanel();

			EditorGUILayout.BeginHorizontal();
				// Show Left Panel
				RenderLeftPanel();

				// Show Right Panel
				RenderRightPanel();

			EditorGUILayout.EndHorizontal();
		EditorGUILayout.EndVertical();

		mouseDown = m_MouseDown;
	}
	
	//
	// UpdateSelectionByHiearchyView()
	//
	void UpdateSelectionByHiearchyView()
	{
		bool	updateFlag = false;
		
		if (m_SelectedGameObject != Selection.activeGameObject)
		{
			updateFlag = true;
		}
		else if (Selection.gameObjects != null)
		{			
			// early test (number of previously selected widgets and currently selected)
			if (Selection.gameObjects.Length != m_SelectedObjects.Count)
			{
				updateFlag = true;
			}
			else
			{
				// check if selection has been changed from last update
				
				foreach (S_SelectedObject sObject in m_SelectedObjects)
				{
					if (sObject.m_Element)
					{
						if (! Selection.Contains(sObject.m_Element.gameObject))
						{
							updateFlag = true;
							break;
						}
					}
				}
			}
		}

		if (updateFlag)
		{
			m_SelectedGameObject = Selection.activeGameObject;
			
			// reset old selection
			m_SelectedWidget  = null;
			m_Layout.Layout   = null;
			m_Layout.Pivot    = null;
			m_Layout.Platform = null;
			m_Layouts.Clear();
		
			m_SelectedObjects.RemoveRange(0, m_SelectedObjects.Count);
			
			// reset gizmo
			m_Gizmo.Reset();
			m_GizmoRotation = 0.0f;
			
			// Which platform, layouts and widgets are selected ?			

			if (m_SelectedGameObject != null)
			{
				m_SelectedWidget = m_SelectedGameObject.GetComponent<GUIBase_Widget>();
				
				if (m_SelectedWidget)
				{
					m_Layout.Layout   = GUIEditorUtils.FindLayoutForWidget(m_SelectedWidget);
					m_Layout.Platform = GUIEditorUtils.FindPlatformForLayout(m_Layout.Layout);
				}
				else
				{
					m_Layout.Layout = m_SelectedGameObject.GetComponent<GUIBase_Layout>();
					
					if (m_Layout.Layout)
					{
						m_SelectedWidget	= null;
						m_Layout.Platform	= GUIEditorUtils.FindPlatformForLayout(m_Layout.Layout);
					}
					else
					{
						m_Layout.Platform	= m_SelectedGameObject.GetComponent<GUIBase_Platform>();
						
						if (!m_Layout.Platform)
						{
							m_Layout.Pivot	= m_SelectedGameObject.GetComponent<GUIBase_Pivot>();
							
							if (m_Layout.Pivot)
							{
								m_Layout.Platform = GUIEditorUtils.FindPlatformForPivot(m_Layout.Pivot);
							}
						}
					}
				}
			}
			
#if false
			if (m_Layout.Platform == null)
			{
				GuiView[] views = m_SelectedGameObject.GetComponents<GuiView>();
				
				foreach (var view in views)
				{
					if (view.Layout != null)
					{
						var layout = new LayoutInfo() {
							Layout   = view.Layout,
							Pivot    = view.Pivot,
							Platform = view.Platform
						};
						m_Layouts.Add(layout);
					}
				}
				
				/*foreach (var info in m_Layouts)
				{
					if (info.Layout == m_Layout.Layout)
					{
						m_Layout = info;
					}
				}*/
			}
#endif
			
			// Handle multiselection
			if (Selection.gameObjects != null)
			{
				foreach (GameObject gObj in Selection.gameObjects)
				{
					GUIBase_Element element = gObj.GetComponent<GUIBase_Element>();
					
					if (element)
					{
						S_SelectedObject	sObject = new S_SelectedObject();
					
						sObject.m_Element	= element;
						m_SelectedObjects.Add(sObject);
					}
				}	
			}
			
			// Precache platform's layouts
			PrecacheLayouts();
			
			// precache some other data
			if (m_Layout.Platform)
			{
				m_PlatformName = m_Layout.Platform.name;
				m_PlatformXRes = m_Layout.Platform.m_Width;
				m_PlatformYRes = m_Layout.Platform.m_Height;
			}
			else
			{
				m_PlatformName	= "";
				m_PlatformXRes	= 0;
				m_PlatformYRes	= 0;
			}
		}
	}
	
	//
	// Precache layouts of selected platform
	//
	void PrecacheLayouts()
	{
		m_PrecachedLayouts = null;
		
		if (m_Layouts.Count > 0)
		{
			m_PrecachedLayouts = new S_LayoutDscr[m_Layouts.Count];
			
			foreach (var info in m_Layouts)
			{
				LayoutInsertSort(m_PrecachedLayouts, info.Layout);
				m_Layout.Platform = info.Platform;
				m_Layout.Pivot    = info.Pivot;
			}
		}
		else
		if (m_Layout.Platform)
		{
			GUIBase_Layout[] layouts = m_Layout.Platform.GetComponentsInChildren<GUIBase_Layout>();

			if (layouts.Length != 0)
			{
				m_PrecachedLayouts = new S_LayoutDscr[layouts.Length];

				for (int i = 0; i < layouts.Length; ++i)
				{
					LayoutInsertSort(m_PrecachedLayouts, layouts[i]);
				}
			}
		}
		
		if (m_PrecachedLayouts != null)
		{
			for (int i = 0; i < m_PrecachedLayouts.Length; ++i)
			{
				GUIBase_Widget[] widgets = m_PrecachedLayouts[i].m_Layout.GetComponentsInChildren<GUIBase_Widget>();
				
				if ((widgets != null) && (widgets.Length > 0))
				{
					m_PrecachedLayouts[i].m_Widgets = new GUIBase_Widget[widgets.Length];
					
					widgets.CopyTo(m_PrecachedLayouts[i].m_Widgets, 0);
				}
			}
		}
	}

	//
	// Insert sort of layout to array
	//
	void LayoutInsertSort(S_LayoutDscr[] sortedLayouts, GUIBase_Layout layout)
	{
		for (int i = 0; i < sortedLayouts.Length; ++i)
		{
			if (sortedLayouts[i].m_Layout == null)
			{
				sortedLayouts[i].m_Layout = layout;
				break;
			}
			else
			{
				// Comparison of names - removed
				//if (string.Compare(sortedLayouts[i].m_Layout.name, layout.name) < 0)
				
				// sort by explicit layer index
				if (sortedLayouts[i].m_Layout.m_LayoutLayer > layout.m_LayoutLayer)
				{
					// move rest of array to the end
					for (int j = sortedLayouts.Length - 2; j >= i ; --j)
					{
						sortedLayouts[j + 1] = sortedLayouts[j];
					}
					
					sortedLayouts[i].m_Layout = layout;
					break;
				}
			}
		}
	}
	
	void RenderTopPanel()
	{
		//GUIEditorUtils.LookLikeControls();
		EditorGUIUtility.labelWidth = 0;
		EditorGUIUtility.fieldWidth = 0;

		EditorGUILayout.BeginHorizontal(GUILayout.Height(28));
			GUILayout.FlexibleSpace();
			GUILayout.FlexibleSpace();
			//GUILayout.Space(310);
			//EditorGUILayout.Separator();
			ShowGizmoMode();
			GUILayout.FlexibleSpace();
			ShowFreeze();
			EditorGUILayout.Separator();
			ShowSelectionOnly();
			EditorGUILayout.Separator();
			ShowZoom();
			EditorGUILayout.Separator();
		EditorGUILayout.EndVertical();
	}

	//
	// Show params of layout etc.
	//
	void RenderLeftPanel()
	{
		GUIEditorUtils.LookLikeControls(m_DefaultLabelWidth);

		// Vertical frame with fixed width
		m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos, GUILayout.Width(300));
		EditorGUILayout.BeginVertical();
		
		ShowPlatformParams();
		EditorGUILayout.Separator();

		ShowLayoutParams();
		EditorGUILayout.Separator();
		
		ShowWidgetPosSize();
		ShowWidgetCopyPaste();
		EditorGUILayout.Separator();

		// Reset button
		ShowResetButtons();
		EditorGUILayout.Separator();
		
		ShowActivateDeactivate();
		EditorGUILayout.Separator();

		ShowNewTextRendering();
		
		EditorGUILayout.EndVertical();
		EditorGUILayout.EndScrollView();
	}

	void ShowNewTextRendering()
	{
		GUILayout.Label("Reload text database", EditorStyles.boldLabel);
		GUILayout.BeginHorizontal();
			EditorGUI.BeginChangeCheck();
				int idx = System.Array.IndexOf(GuiOptions.convertLanguageToSysLanguage, TextDatabase.GetLanguage());
				GuiOptions.E_Language language = idx != -1 ? (GuiOptions.E_Language)idx : GuiOptions.E_Language.English;
				language = (GuiOptions.E_Language)EditorGUILayout.EnumPopup(language, GUI.skin.button, GUILayout.Height(18));
			if (EditorGUI.EndChangeCheck() == true)
			{
				TextDatabase.SetLanguage(GuiOptions.convertLanguageToSysLanguage[(int)language]);
			}
		
			EditorGUI.BeginChangeCheck();
				GUILayout.Button("Reload", GUILayout.Height(18));
			if (EditorGUI.EndChangeCheck() == true)
			{
				TextDatabase.instance.Reload();
			}
		GUILayout.EndHorizontal();
	}

	void ShowSelectionOnly()
	{
		GUIEditorUtils.LookLikeControls(120);
		m_OnlySelectedLayouts = EditorGUILayout.Toggle("Show only selection", m_OnlySelectedLayouts, GUILayout.Width(130));
		GUIEditorUtils.LookLikeControls(m_DefaultLabelWidth);
	}
	
	void ShowActivateDeactivate()
	{
		EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);

		EditorGUILayout.BeginHorizontal();

		if (GUILayout.Button("Hide"))
		{
			SetActiveSelection(false, false);
		}
		else if (GUILayout.Button("Hide all"))
		{
			SetActiveSelection(false, true);
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Show"))
		{
			SetActiveSelection(true, false);
		}
		else if (GUILayout.Button("Show all"))
		{
			SetActiveSelection(true, true);
		}
		EditorGUILayout.EndHorizontal();
	}
	
	//
	// ShowZoom
	//
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
	
	//
	// ShowPlatformParams()
	//
	void ShowPlatformParams()
	{		
		GUIEditorUtils.LookLikeControls(85);
			EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Platform", EditorStyles.boldLabel, GUILayout.Width(80));
				EditorGUILayout.LabelField(m_PlatformName);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.LabelField("X res", m_PlatformXRes.ToString());
			EditorGUILayout.LabelField("Y res", m_PlatformYRes.ToString());
		GUIEditorUtils.LookLikeControls(m_DefaultLabelWidth);
	}

	//
	// ShowLayoutParams()
	//
	void ShowLayoutParams()
	{
		int selected = -1;
		List<string>          labels = new List<string>();
		List<GUIBase_Layout> layouts = new List<GUIBase_Layout>();
		if (m_PrecachedLayouts != null)
		{
			foreach (S_LayoutDscr desc in m_PrecachedLayouts)
			{
				if (desc.m_Layout != null)
				{
					if (desc.m_Layout == m_Layout.Layout)
					{
						selected = layouts.Count;
					}
					string name = desc.m_Layout.GetFullName();
					int   begin = name.IndexOf('/') + 1;
					int     end = name.LastIndexOf(',');
					labels.Add(name.Substring(begin, end - begin));
					layouts.Add(desc.m_Layout);
				}
			}
		}
		
		EditorGUI.BeginChangeCheck();
			GUIEditorUtils.LookLikeControls();
				EditorGUILayout.LabelField("Layout", EditorStyles.boldLabel);
				selected = EditorGUILayout.Popup(selected, labels.ToArray());
			GUIEditorUtils.LookLikeControls(m_DefaultLabelWidth);
		if (EditorGUI.EndChangeCheck() == true && selected >= 0)
		{
			Selection.activeGameObject = layouts[selected].gameObject;
		}
	}
	
	//
	// ShowWidgetPosSize()
	//
	void ShowWidgetPosSize()
	{		
		string			sName			= "nothing";
		GUIBase_Widget	selectedWidget	= null;
		
		if ((m_SelectedObjects.Count == 1) && m_SelectedWidget)
		{
			selectedWidget	= m_SelectedWidget;
			sName			= selectedWidget.name;
		}
		else if (m_SelectedObjects.Count > 1)
		{
			sName = "multiselection";
		}
		
		EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Selected widget", EditorStyles.boldLabel, GUILayout.Width(110));
			EditorGUILayout.LabelField(sName);
		EditorGUILayout.EndHorizontal();
		
		//
		// Enable modification of U,V,Width,Height
		//
		
		int x		= 0;
		int y 		= 0;
		int width	= 0;
		int height	= 0;
		
		if (selectedWidget)
		{
			Vector3 sPos = selectedWidget.transform.position;
			
			x = (int)sPos.x;
			y = (int)sPos.y;
			
			width	= (int)selectedWidget.GetWidth();
			height	= (int)selectedWidget.GetHeight();
		}
		
		bool	changeFlag = false;
		
		int  oldX = x;
		int  oldY = y;
		int  oldW = width;
		int  oldH = height;
		
		EditorGUI.BeginDisabledGroup(selectedWidget ? false : true);
			GUIEditorUtils.LookLikeControls(20);
			if (m_PlatformXRes > 0 && m_PlatformYRes > 0)
			{
			//EditorGUILayout.BeginHorizontal();
				x = EditorGUILayout.IntSlider("X", x, -m_PlatformXRes, m_PlatformXRes * 2);
				y = EditorGUILayout.IntSlider("Y", y, -m_PlatformYRes, m_PlatformYRes * 2);
			//EditorGUILayout.EndHorizontal();
			//EditorGUILayout.BeginHorizontal();
				width  = EditorGUILayout.IntSlider("W", width,  1, m_PlatformXRes * 2);
				height = EditorGUILayout.IntSlider("H", height, 1, m_PlatformYRes * 2);
			//EditorGUILayout.EndHorizontal();
			}
			else
			{
				x = EditorGUILayout.IntField("X", x);
				y = EditorGUILayout.IntField("Y", y);
				width  = EditorGUILayout.IntField("W", width);
				height = EditorGUILayout.IntField("H", height);
			}
			GUIEditorUtils.LookLikeControls(m_DefaultLabelWidth);
		EditorGUI.EndDisabledGroup();
	
		width	= Mathf.Abs(width);
		height	= Mathf.Abs(height);
		
		if ((oldX != x) || (oldY != y) || (oldW != width) || (oldH != height))
		{
			changeFlag = true;
		}
		
		// Change selected widget size and position ?
		if (selectedWidget && changeFlag)
		{
			Vector3 sPos	= new Vector3();
			
			sPos	= selectedWidget.transform.position;
			sPos.x	= Mathf.RoundToInt(sPos.x);
			sPos.y	= Mathf.RoundToInt(sPos.y);
			sPos.z	= Mathf.RoundToInt(sPos.z);
	
			if (!AnimationMode.InAnimationMode())
			{
				selectedWidget.transform.position	= sPos;
				selectedWidget.SetScreenSize(width, height);
				
				EditorUtility.SetDirty(selectedWidget);
			}
		}
	}
	
	//
	// Show reset button
	//
	void ShowResetButtons()
	{
		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Reset Position"))
		{
			if (m_SelectedWidget)
			{
				if (!m_Layout.Layout)
				{
					m_Layout.Layout = GUIEditorUtils.FindLayoutForWidget(m_SelectedWidget);
				}
				
				ResetWidgetPos(m_SelectedWidget, m_Layout.Layout);
			}
		}
		
		if (GUILayout.Button("Reset Rotation"))
		{
			ResetWidgetRotation(m_SelectedWidget);
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Reset Size"))
		{
			if (m_SelectedWidget)
			{
				if (!m_Layout.Layout)
				{
					m_Layout.Layout = GUIEditorUtils.FindLayoutForWidget(m_SelectedWidget);
				}
				
				if (m_Layout.Layout)
				{
					ResetWidgetSize(m_SelectedWidget);
				}
			}
		}

		if (GUILayout.Button("Reset Scale"))
		{
			ResetWidgetScale(m_SelectedWidget);
		}
		EditorGUILayout.EndHorizontal();
	}
	
	//
	// Recursive reset of position
	//
	void ResetWidgetPos(GUIBase_Widget widget, GUIBase_Layout layout)
	{
		if (widget && layout)
		{
			GUIBase_Widget[]	widgets = widget.GetComponentsInChildren<GUIBase_Widget>();

			// Set position to center of layout
			Vector3				pos = new Vector3(Mathf.RoundToInt((float)m_PlatformXRes / 2.0f), Mathf.RoundToInt((float)m_PlatformYRes / 2.0f), 0.0f);
			
			if (widgets != null)
			{
				pos.x = Mathf.RoundToInt(pos.x);
				pos.y = Mathf.RoundToInt(pos.y);
				pos.z = Mathf.RoundToInt(pos.z);
					
				foreach (GUIBase_Widget w in widgets)
				{
					w.transform.position = pos;
					EditorUtility.SetDirty(w);
				}
			}
		}
	}
	
	//
	// Recursive reset of rotation
	//
	void ResetWidgetRotation(GUIBase_Widget widget)
	{
		if (widget)
		{
			GUIBase_Widget[] widgets = widget.GetComponentsInChildren<GUIBase_Widget>();

			if (widgets != null)
			{
				foreach (GUIBase_Widget w in widgets)
				{
					w.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 0.0f);

					EditorUtility.SetDirty(w);
				}
			}
		}
	}
	
	//
	// Recursive reset of scale
	//
	void ResetWidgetScale(GUIBase_Widget widget)
	{
		if (widget)
		{
			GUIBase_Widget[]	widgets = widget.GetComponentsInChildren<GUIBase_Widget>();

			if (widgets != null)
			{
				foreach (GUIBase_Widget w in widgets)
				{
					w.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);

					EditorUtility.SetDirty(w);
				}
			}
		}
	}
	
	//
	// Recursive reset of size
	//
	void ResetWidgetSize(GUIBase_Widget widget)
	{
		if (widget)
		{
			GUIBase_Widget[]	widgets = widget.GetComponentsInChildren<GUIBase_Widget>();

			if (widgets != null)
			{
				foreach (GUIBase_Widget w in widgets)
				{
					GUIBase_Label	label = w.GetComponent<GUIBase_Label>();

					// special case is label
					if (label != null)
					{
						if (label.isValid)
						{
							Texture2D	texture		= label.fontTexture;
							Vector2     textSize    = label.textSize;
							w.SetScreenSize(textSize.x, textSize.y * texture.height);
						}
					}
					else // other types of widgets
					{
						Texture2D	texture		= (Texture2D)w.GetTexture();
						float		texWidth	= texture.width;
						float		texHeight	= texture.height;
	
						float		pixelsX	    = w.m_InTexSize.x * texWidth;
						float		pixelsY	    = w.m_InTexSize.y * texHeight;
					
						w.SetScreenSize(pixelsX, pixelsY);
					}

					EditorUtility.SetDirty(w);
				}
			}
		}
	}
	
	//
	// Show freeze
	//
	void ShowFreeze()
	{
		bool freezed = GetSelectedObjectFreezeFlag();
				
		GUIEditorUtils.LookLikeControls(55);
		freezed = EditorGUILayout.Toggle("Freezed", freezed, GUILayout.Width(65));
		GUIEditorUtils.LookLikeControls(m_DefaultLabelWidth);
		
		SetFreezeFlagToSelectedObjects(freezed);
	}

	// 
	// ShowGizmoMode
	//
	void ShowGizmoMode()
	{
		m_GizmoSelGridInt = GUILayout.Toolbar(m_GizmoSelGridInt, m_GizmoSelStrings, GUILayout.Width(250));
		
		switch (m_GizmoSelGridInt)
		{
		case 0:
			m_Gizmo.SetGizmoMode(GUIEditorGizmo.E_OperationMode.E_OM_MOVE);
			break;

		case 1:
			m_Gizmo.SetGizmoMode(GUIEditorGizmo.E_OperationMode.E_OM_SIZE);
			break;

		case 2:
			m_Gizmo.SetGizmoMode(GUIEditorGizmo.E_OperationMode.E_OM_ROTATE);
			break;

		case 3:
			m_Gizmo.SetGizmoMode(GUIEditorGizmo.E_OperationMode.E_OM_SCALE);
			break;
			
		default:
			Debug.LogError("Unsupported case");
			break;
		}
	}
	
	//
	// freeze flag for selected widget/layout/pivot
	//
	bool GetSelectedObjectFreezeFlag()
	{
		for (int i = 0; i < m_SelectedObjects.Count; ++i)
		{
			S_SelectedObject sObj = (S_SelectedObject)m_SelectedObjects[i];
			
			if (sObj.m_Element && GetFreezeFlag(sObj.m_Element.gameObject))
			{
				return true;
			}
		}
		
		return false;
	}
	
	//
	// Set freezed flag to selected objects and to its children
	//
	void SetFreezeFlagToSelectedObjects(bool f)
	{
		for (int i = 0; i < m_SelectedObjects.Count; ++i)
		{
			S_SelectedObject sObj = (S_SelectedObject)m_SelectedObjects[i];
			
			if (sObj.m_Element)
			{
				SetFreezeFlagRecursive(sObj.m_Element.gameObject, f);
			}
		}
	}
		
	void SetActiveSelection(bool activate, bool children)
	{
		Debug.Log("Editor: Selection " + (activate ? "activated " : "deactivated") + (children ? " with all children" : "") );
		
		GUIBase_Platform platformSelected = null;
		if(Selection.activeGameObject)
			platformSelected = Selection.activeGameObject.GetComponent<GUIBase_Platform>();
		
		if( platformSelected )
		{
			platformSelected.gameObject.SetActive(activate);
			
			return;
		}
		
		for (int i = 0; i < m_SelectedObjects.Count; ++i)
		{
			S_SelectedObject sObj = (S_SelectedObject)m_SelectedObjects[i];
			
			if (sObj.m_Element)
			{
				sObj.m_Element.gameObject.SetActive(activate);
				//SetFreezeFlagRecursive(sObj.m_Pivot.gameObject, f);
			}
		}
		
		m_SelectedGameObject = null;
		UpdateSelectionByHiearchyView();
	}
	
	//
	// Show layout
	//
	void RenderRightPanel()
	{
		EditorGUILayout.BeginVertical(GUILayout.MinWidth(300));
		
		Vector2 platformPos = new Vector2();

		if (m_BackgroundTexture)
		{
			EditorGUILayout.Separator();

			Rect	r = GUILayoutUtility.GetLastRect();

			//r.x	+= 10;
			//r.y	+= r.height + 10;
					
			// Draw background (slightly bigger than layout)
			m_BackgroundWidth	= m_PlatformXRes * 1.5f;
			m_BackgroundWidth	= Mathf.Clamp(m_BackgroundWidth, GUIEditorUtils.MIN_BACKGROUND_SIZE, m_BackgroundWidth);
			r.width				= m_BackgroundWidth * m_ZoomFactor * (1.0f + m_PlatformShift.x);

			m_BackgroundHeight	= m_PlatformYRes * 1.5f;
			m_BackgroundHeight	= Mathf.Clamp(m_BackgroundHeight, GUIEditorUtils.MIN_BACKGROUND_SIZE, m_BackgroundHeight);
			r.height			= m_BackgroundHeight * m_ZoomFactor * (1.0f + m_PlatformShift.y);
		
			Graphics.DrawTexture(r, m_BackgroundTexture);
		
			if (Event.current.type == EventType.Repaint)
			{
				m_PlatformRectangle = r;
				
				GetPlatformPosOnScreen(r.x, r.y, out m_BackgroundPos.x, out m_BackgroundPos.y);
				
				m_BackgroundPos.x = m_BackgroundPos.x - m_BackgroundWidth * m_ZoomFactor / 6.0f;
				m_BackgroundPos.y = m_BackgroundPos.y - m_BackgroundHeight * m_ZoomFactor / 6.0f;
			}
		
			if (m_Layout.Platform)
			{
				// Draw platform (rectangle)		
				GetPlatformPosOnScreen(r.x, r.y, out platformPos.x, out platformPos.y);
		
				// Platform pos on screen ?
				if (Event.current.type == EventType.Repaint)
				{
					m_PlatformPos = platformPos;
				}

				Rect	a				= new Rect(0.0f, 0.0f, m_PlatformXRes, m_PlatformYRes);
				Vector2	platformShift	= new Vector2(m_PlatformShift.x * m_BackgroundWidth * m_ZoomFactor, m_PlatformShift.y * m_BackgroundHeight * m_ZoomFactor);
				
				m_TopLeftPos.x	= m_PlatformRectangle.x;
				m_TopLeftPos.y	= m_PlatformRectangle.y;
				
				GUIEditorUtils.SetClipPoint(m_TopLeftPos);
	
				GUIEditorUtils.DrawRect(platformPos, a, 0.0f, m_ZoomFactor, platformShift, true, GUIEditorUtils.COLOR_LAYOUT_AREA, GUIEditorUtils.COLOR_AREA_POINT, GUIEditorUtils.COLOR_AREA_SELECTED_POINT);
				
				if (m_PrecachedLayouts != null)
				{
					foreach (S_LayoutDscr lDscr in m_PrecachedLayouts)
					{
						if(m_OnlySelectedLayouts && !IsSomeObjectFromLayoutSelected(lDscr.m_Layout) )
						{
							continue;
						}
						
						// Draw widgets
						DrawWidgetsToLayout(lDscr, platformPos, platformShift);
					}
					
					// draw rectangles for selected layouts
					for (int i = 0; i < m_SelectedObjects.Count; ++i)
					{
						S_SelectedObject  sObj = (S_SelectedObject)m_SelectedObjects[i];
						GUIBase_Layout sLayout = sObj.m_Element as GUIBase_Layout;
						if (sLayout)
						{
							Vector3	pos		= sLayout.transform.position;
							float	width	= m_PlatformXRes * sLayout.transform.lossyScale.x;
							float	height	= m_PlatformYRes * sLayout.transform.lossyScale.y;
							Rect	lArea	= new Rect(pos.x, pos.y, width, height);

							GUIEditorUtils.DrawRect(platformPos, lArea, sLayout.transform.eulerAngles.z, m_ZoomFactor, platformShift, true, GUIEditorUtils.COLOR_AREA_EDGE, GUIEditorUtils.COLOR_AREA_POINT, GUIEditorUtils.COLOR_AREA_SELECTED_POINT);
						}
					}
				}
			}
		}
		else
		{
			Debug.Log("Plugin error - can't show background texture");
		}
				
		DrawSelectedGizmo(m_PlatformPos);
				
		EditorGUILayout.EndVertical();
	}
	
	
	bool IsSomeObjectFromLayoutSelected(GUIBase_Layout layout)
	{
		for (int i = 0; i < m_SelectedObjects.Count; ++i)
		{
			S_SelectedObject	sObj = (S_SelectedObject)m_SelectedObjects[i];
			
			if (sObj.m_Element == layout)
			{
				return true;
			}
			else if(sObj.m_Element && sObj.m_Element.gameObject.GetFirstComponentUpward<GUIBase_Layout>() == layout)
			{
				return true;				
			}
		}
		return false;
	}
	
	//
	// Draw gizmo for selected object
	//
	void DrawSelectedGizmo(Vector2 platformPos)
	{
		m_GizmoVisible	= false;
		
		if (m_SelectedWidget || m_Layout.Pivot || m_Layout.Layout)
		{
			m_GizmoVisible = true;
		}
		
		if (m_GizmoVisible)
		{
			Transform	tr	= m_SelectedGameObject.transform;
			Vector2		pos	= new Vector2(tr.position.x + m_PlatformShift.x * m_BackgroundWidth, tr.position.y + m_PlatformShift.y * m_BackgroundHeight);
			
			pos *= m_ZoomFactor;
			pos += platformPos;
						
			m_Gizmo.SetPos(pos);
			m_Gizmo.SetRot(m_GizmoRotation);
			m_Gizmo.SetScale(new Vector2(m_GizmoScale.x, m_GizmoScale.y));

			m_Gizmo.Render();
		}
	}
	
	//
	// DrawWidgetsToLayout()
	//
	void DrawWidgetsToLayout(S_LayoutDscr lDscr, Vector2 platformPos, Vector2 platformShift)
	{
		if (lDscr.m_Widgets != null)
		{
			for (int lIdx = 0; lIdx < GUIEditorUtils.MAX_LAYERS; ++lIdx)
			{
				foreach (GUIBase_Widget w in lDscr.m_Widgets)
				{
					if (w && (w.m_GuiWidgetLayer == lIdx))
					{
						// render widget to layout
						RenderWidget(w, platformPos, platformShift);
					}
				}
			}

			// draw rectangles for selected widgets
			for (int i = 0; i < m_SelectedObjects.Count; ++i)
			{
				GUIBase_Widget		w = ((S_SelectedObject)m_SelectedObjects[i]).m_Element as GUIBase_Widget;
				
				if (w)
				{
					Transform		trans   = w.transform;
					Vector3			pos		= trans.position;
					Vector3			scale   = trans.lossyScale;
					float			width	= w.GetWidth()  * scale.x;
					float			height	= w.GetHeight() * scale.y;
					Rect			a		= new Rect(pos.x - width * 0.5f, pos.y - height * 0.5f, width, height);
	
					// Draw touchable area?
					bool			drawTouchableArea			= false;
					float			touchableAreaWidthScale		= 1.0f;
					float			touchableAreaHeightScale	= 1.0f;
						
					GUIBase_Button	button = w.GetComponent<GUIBase_Button>();
						
					if (button)
					{
						touchableAreaWidthScale		= button.m_TouchableAreaWidthScale;
						touchableAreaHeightScale	= button.m_TouchableAreaHeightScale;
						drawTouchableArea			= true;
					}
					else
					{
						GUIBase_Slider	slider = w.GetComponent<GUIBase_Slider>();
							
						if (slider)
						{
							touchableAreaWidthScale		= slider.m_TouchableAreaWidthScale;
							touchableAreaHeightScale	= slider.m_TouchableAreaHeightScale;
							drawTouchableArea			= true;
						}
					}
		
					if (drawTouchableArea)
					{
						float	newWidth	= a.width * touchableAreaWidthScale;
						float	newHeight	= a.height * touchableAreaHeightScale;
				
						Rect	touchArea	= new Rect(a.x - (newWidth - a.width) * 0.5f, a.y - (newHeight - a.height) * 0.5f, newWidth, newHeight);
								
						GUIEditorUtils.DrawRect(platformPos, touchArea, trans.eulerAngles.z, m_ZoomFactor, platformShift, true, GUIEditorUtils.COLOR_AREA_TOUCHABLE_EDGE, GUIEditorUtils.COLOR_AREA_POINT, GUIEditorUtils.COLOR_AREA_SELECTED_POINT);
					}

					GUIBase_Label label = w.GetComponent<GUIBase_Label>();
					if(label != null)
					{
						Vector3 leftUp = label.GetLeftUpPos(w);
						a = new Rect(leftUp.x, leftUp.y, width, height);

						GUIBase_LabelBoundaries bounds = w.GetComponent<GUIBase_LabelBoundaries>();
						if (bounds != null && bounds.enabled == true)
						{
							float	newWidth	= bounds.Width  * scale.x;
							float	newHeight	= bounds.Height * scale.y;
							
							Vector3	newPos		= label.GetLeftUpPos(pos, newWidth, newHeight, Vector3.one);
							Rect	boundArea	= new Rect(newPos.x, newPos.y, newWidth, newHeight);
									
							GUIEditorUtils.DrawRect(platformPos, boundArea, trans.eulerAngles.z, m_ZoomFactor, platformShift, true, Color.yellow, GUIEditorUtils.COLOR_AREA_POINT, GUIEditorUtils.COLOR_AREA_SELECTED_POINT);
						}
					}

					GUIEditorUtils.DrawRect(platformPos, a, trans.eulerAngles.z, trans.position, m_ZoomFactor, platformShift, true, GUIEditorUtils.COLOR_AREA_EDGE, GUIEditorUtils.COLOR_AREA_POINT, GUIEditorUtils.COLOR_AREA_SELECTED_POINT);
				}
			}
		}
	}	

	//
	// RenderWidget
	//
	void RenderWidget(GUIBase_Widget w, Vector2 platformPos, Vector2 platformShift)
	{			
		Transform trans = w.transform;
		float  rotAngle = trans.eulerAngles.z;
		Vector2   pivot = Vector2.zero;

		if (rotAngle > 0.0f)
		{
			pivot = new Vector2(
				Mathf.RoundToInt(trans.position.x * m_ZoomFactor + platformPos.x + platformShift.x),
				Mathf.RoundToInt(trans.position.y * m_ZoomFactor + platformPos.y + platformShift.y)
				);
			GUIUtility.RotateAroundPivot(rotAngle, pivot);
		}

		if (w.GetComponent<GUIBase_Number>())
		{
			RenderNumberWidget(w, platformPos, platformShift);
		}
		else if (w.GetComponent<GUIBase_Counter>())
		{
			RenderCounterWidget(w, platformPos, platformShift);
		}
		else if (w.GetComponent<GUIBase_Label>())
		{
			GUIBase_Label label = w.GetComponent<GUIBase_Label>();
		
			if(label.useFontEx)
			{
				RenderLabelWidgetNew(w, platformPos, platformShift);
			}
			else
			{
				RenderLabelWidget(w, platformPos, platformShift);
			}
		}
		else
		{
			Vector3		pos			= trans.position;
			float		width		= w.GetWidth();
			float		height		= w.GetHeight();
			Vector2		inTexPos	= w.m_InTexPos;
			Vector2		inTexSize	= w.m_InTexSize;
			
			RenderBasicWidget(w, pos, width, height, trans.lossyScale, inTexPos, inTexSize, platformPos, platformShift);
		}

		if (rotAngle > 0.0f)
		{
			GUIUtility.RotateAroundPivot(-rotAngle, pivot);
		}
	}

	//
	// RenderBasicWidget
	//
	void RenderBasicWidget(GUIBase_Widget w, Vector3 pos, float width, float height, Vector3 lossyScale, Vector2 inTexPos, Vector2 inTexSize, Vector2 platformPos, Vector2 platformShift)
	{	
		Texture2D texture  = w.GetTexture() as Texture2D;
		Material  material = w.GetMaterial();
		
		if (texture == null || material == null)
		{
			return;
		}

		RenderSprite(texture, pos, width, height, lossyScale, inTexPos, inTexSize, platformPos, platformShift, w.m_Grid9, w.Color, GetClipRectangle(w));
	}

	public Rect GetClipRectangle(GUIBase_Widget widget)
	{
		return default(Rect);
/*
		Rect rect = widget.GetRectInScreenCoords();
		GUIBase_Widget parent = GetParent(widget);
		if (parent != null && parent.ClipChildren == true)
		{
			rect = rect.Intersect(GetClipRectangle(parent));
		}
		return rect;
*/
	}
	
	public GUIBase_Widget GetParent(GUIBase_Widget widget)
	{
		Transform  trans = widget.transform;
		Transform parent = trans.parent;
		return parent != null ? parent.GetComponent<GUIBase_Widget>() : null;
	}
	
	//
	// RenderBasicWidget
	//
	void RenderSprite(Texture texture, Vector3 pos, float width, float height, Vector3 lossyScale, Vector2 inTexPos, Vector2 inTexSize, Vector2 platformPos, Vector2 platformShift, MFGuiGrid9 grid9, Color color, Rect clipRect)
	{		
		// deduce actual bounds
		float[] xAxis, yAxis;
		/*int nonEmpty =*/ grid9.ComputeSegments(out xAxis, out yAxis);

		float  originWidth = inTexSize.x * texture.width;
		float originHeight = inTexSize.y * texture.height;
		float    widthMult = originWidth  / width;
		float   heightMult = originHeight / height;

		for (int yIdx = 0; yIdx < 3; ++yIdx)
		{
			float y1 = yAxis[yIdx];
			float y2 = yAxis[yIdx + 1];

			// do not create quads for empty row
			float posY1, posY2;
			ComputeRealValues(yIdx, height, y1 * heightMult, y2 * heightMult, out posY1, out posY2);
			if (posY2 - posY1 <= 0.0f)
				continue;

			for (int xIdx = 0; xIdx < 3; ++xIdx)
			{
				float x1 = xAxis[xIdx];
				float x2 = xAxis[xIdx + 1];

				// do not create quads for empty column
				float posX1, posX2;
				ComputeRealValues(xIdx, width, x1 * widthMult, x2 * widthMult, out posX1, out posX2);
				if (posX2 - posX1 <= 0.0f)
					continue;

				// compute quad rect
				Rect screenRect = new Rect(pos.x + posX1 * lossyScale.x, pos.y + posY1 * lossyScale.y, (posX2 - posX1) * lossyScale.x, (posY2 - posY1) * lossyScale.y);
		
				screenRect.x      -= width  * 0.5f * lossyScale.x;
				screenRect.y      -= height * 0.5f * lossyScale.y;

				//screenRect = screenRect.Intersect(clipRect);
				
				screenRect.x      *= m_ZoomFactor;
				screenRect.y      *= m_ZoomFactor;
				screenRect.width  *= m_ZoomFactor;
				screenRect.height *= m_ZoomFactor;

				screenRect.x += platformPos.x + platformShift.x;
				screenRect.y += platformPos.y + platformShift.y;

				// compute quad UVs
				float uvX1, uvX2;
				ComputeRealValues(xIdx, inTexSize.x, x1, x2, out uvX1, out uvX2);

				float uvY1, uvY2;
				ComputeRealValues(yIdx, inTexSize.y, y1, y2, out uvY1, out uvY2);

				Vector2  quadTexPos = new Vector2(inTexPos.x + uvX1, inTexPos.y + uvY1);
				Vector2 quadTexSize = new Vector2(uvX2 - uvX1, uvY2 - uvY1);

				// render quad now
				RenderQuad(texture, screenRect, quadTexPos, quadTexSize, color);
			}
		}
	}

	private void ComputeRealValues(int idx, float maxValue, float val1, float val2, out float res1, out float res2)
	{
		val1 *= maxValue;
		val2 *= maxValue;
		
		switch (idx)
		{
		case 0:
			res1 = val1;
			res2 = val2;
			break;
		case 1:
		default:
			res1 = val1;
			res2 = maxValue - val2;
			break;
		case 2:
			res1 = maxValue - val1;
			res2 = maxValue - val2;
			break;
		}
	}

	void RenderQuad(Texture texture, Rect screenRect, Vector2 inTexPos, Vector2 inTexSize, Color color)
	{
		//
		// Prepare source rectangle
		//
		
		// calc clip of source rectangle
		float clipX = 0.0f;	// how much of rectangle is visible (0.0f ... no clip, 1.0f = everything clipped)
		float clipY = 0.0f;
		
		// BACHA !!!
		// BACHA !!!
		// BACHA !!!
		// Stupidni clip - clipuji jen kdyz neni widget narotovany!!!
		// Protoze pres stejne muzu renderovat jen rectangly do rectanglu. Takze se s tim neseru...
		
		//if (rotAngle == 0.0f)
		{
			if (screenRect.x < m_BackgroundPos.x)
			{
				if ((screenRect.x + screenRect.width) <= m_BackgroundPos.x)
				{
					clipX = 1.0f;
				}
				else
				{
					clipX = (m_BackgroundPos.x - screenRect.x) / screenRect.width;
				}
			}
		
			if (screenRect.y < m_BackgroundPos.y)
			{
				if ((screenRect.y + screenRect.height) <= m_BackgroundPos.y)
				{
					clipY = 1.0f;
				}
				else
				{
					clipY = (m_BackgroundPos.y - screenRect.y) / screenRect.height;
				}
			}
		}

		if (clipX < 1.0f && clipY < 1.0f)
		{
			// at first correction of destination rectangle
			
			screenRect.x		+= clipX * screenRect.width;
			screenRect.width	-= clipX * screenRect.width;
			
			screenRect.y		+= clipY * screenRect.height;
			screenRect.height	-= clipY * screenRect.height;
			
			// and now source rectangle itself
			
			float	posX	= Mathf.Clamp(inTexPos.x, 0.0f, 1.0f);
			float	posY	= Mathf.Clamp(inTexPos.y, 0.0f, 1.0f);
					
			float	sizeX	= inTexSize.x;
			float	sizeY	= inTexSize.y;

			posX	+= clipX * sizeX;
			sizeX	*= (1.0f - clipX);
			
			posY	+= clipY * sizeY;
			sizeY	*= (1.0f - clipY);
			
			posY	= 1.0f - posY - sizeY;
					
			Rect sourceRect = new Rect(posX, posY, sizeX, sizeY);
			
			Color oldColor = GUI.color;
			GUI.color = color;
			GUI.DrawTextureWithTexCoords(screenRect, texture, sourceRect);
			GUI.color = oldColor;
		}
	}

	//
	// RenderNumberWidget
	//
	void RenderNumberWidget(GUIBase_Widget w, Vector2 platformPos, Vector2 platformShift)
	{
		Transform       trans       = w.transform;
		GUIBase_Number	wn			= w.GetComponent<GUIBase_Number>();
		int				numDigits	= wn.numberDigits;
		
		Vector3 lossyScale = new Vector3(1.0f, trans.lossyScale.y, trans.lossyScale.z);

		float		width		= w.GetWidth() * trans.lossyScale.x / numDigits;
		float		height		= w.GetHeight();// * trans.lossyScale.y;
		Vector2		inTexPos	= w.m_InTexPos;
		Vector2		inTexSize	= w.m_InTexSize;
		
		// render number '9' instead of '0'
		inTexPos.x	+= inTexSize.x * 9.0f;
		
		float		halfWidth	= w.GetWidth() * trans.lossyScale.x / 2.0f;
		Vector3		deltaPos	= new Vector3();
		Vector2		d			= new Vector2();
		Vector3		rightPos	= new Vector3();
		//float		angle		= -trans.eulerAngles.z * Mathf.Deg2Rad;
		
		deltaPos.x = halfWidth;// * Mathf.Cos(angle);
		//deltaPos.y = -halfWidth * Mathf.Sin(angle);
		
		d			= deltaPos / (numDigits * 0.5f);
		rightPos	= trans.position + deltaPos;
		
		for (int i = 0; i < numDigits; ++i)
		{
			RenderBasicWidget(w, new Vector2(rightPos.x - (i + 0.5f) * d.x, rightPos.y /*- (i + 0.5f) * d.y*/), width, height, lossyScale, inTexPos, inTexSize, platformPos, platformShift);
		}
	}
	
	//
	// RenderCounterWidget
	//
	void RenderCounterWidget(GUIBase_Widget w, Vector2 platformPos, Vector2 platformShift)
	{
		Transform       trans       = w.transform;
		GUIBase_Counter	wc			= w.GetComponent<GUIBase_Counter>();
		int				numDigits	= wc.m_MaxCount;		
		
		Vector3 lossyScale = new Vector3(1.0f, trans.lossyScale.y, trans.lossyScale.z);

		float		width		= w.GetWidth() * trans.lossyScale.x / numDigits;
		float		height		= w.GetHeight();// * trans.lossyScale.y;
		Vector2		inTexPos	= w.m_InTexPos;
		Vector2		inTexSize	= w.m_InTexSize;
		
		// render first sprite only
		GUIBase_Widget	sprite	= wc.GetSpriteWidget(0);
		
		if (sprite)
		{
			inTexPos	= sprite.m_InTexPos;
			inTexSize	= sprite.m_InTexSize;
		}

		float		halfWidth	= w.GetWidth() * trans.lossyScale.x / 2.0f;
		Vector3		deltaPos	= new Vector3();
		Vector2		d			= new Vector2();
		Vector3		leftPos		= new Vector3();
		//float		angle		= -trans.eulerAngles.z * Mathf.Deg2Rad;

		deltaPos.x = halfWidth;// * Mathf.Cos(angle);
		//deltaPos.y = -halfWidth * Mathf.Sin(angle);
		
		d		= deltaPos / (numDigits * 0.5f);
		leftPos = trans.position - deltaPos;
		
		for (int i = 0; i < numDigits; ++i)
		{
			RenderBasicWidget(w, new Vector2(leftPos.x + (i + 0.5f) * d.x, leftPos.y /*+ (i + 0.5f) * d.y*/), width, height, lossyScale, inTexPos, inTexSize, platformPos, platformShift);
		}
	}
	
	//
	// RenderLabelWidget
	//
	void RenderLabelWidget(GUIBase_Widget w, Vector2 platformPos, Vector2 platformShift)
	{
		GUIBase_Label	labelWidget = w.GetComponent<GUIBase_Label>();
		
		string text = labelWidget.Text;
		if (string.IsNullOrEmpty(text) == false && labelWidget.Uppercase == true)
		{
			text = text.ToUpper();
		}

		Vector3 lossyScale = Vector3.one;

		if (labelWidget.isValid == true)
		{
			if(labelWidget.GenerateRunTimeData() == true)
			{
				EditorUtility.SetDirty(labelWidget);
			}
			
			Transform	trans			= w.transform;
			float		totalWidth		= 0.0f;
			float		onScreenWidth	= 1.0f;
			//float		onScreenHeight	= 1.0f;
			float		widthMult		= 1.0f;
			
			// 1) Calc width of text
            //Vector2 textSize;
            //if(labelWidget.m_Font.GetTextSize(text, out textSize, false) == true)
            //    totalWidth = textSize.x;

			Texture2D	texture		= labelWidget.fontTexture;

			if(texture == null)
			{
				Debug.LogWarning("Missing texture" + MFDebugUtils.GetFullName(w.gameObject) );
			}


			totalWidth = labelWidget.lineSize.x;
			float lineHeight = labelWidget.lineSize.y * texture.height * trans.lossyScale.y;
			float lineSpace  = labelWidget.lineSpace  * texture.height * trans.lossyScale.y;
			//Debug.Log("---- lineHeight + " + lineHeight + " lineSpace + " + lineSpace);
			// 2) 
			onScreenWidth	= w.GetWidth()  * trans.lossyScale.x;
			//onScreenHeight	= w.GetHeight() * trans.lossyScale.y;
			
			if (totalWidth != 0.0f)
			{
				widthMult	= onScreenWidth / totalWidth;
				
				//Debug.Log("WidthMult = " + widthMult);
				
				// 3) Start from left side
				Vector3		leftPos		= new Vector3();
				
				//leftPos = trans.position - deltaPos;
				int 		numOfLine = 0;
				Vector3 	origLeftPos = labelWidget.GetLeftUpPos(w);
				leftPos 	= origLeftPos;
				leftPos.y  += lineHeight * 0.5f; // textHeight*0.5f;

				bool multiline = GUIBase_Label.IsMultiline(text);
				if(multiline == true)
				{
					leftPos = GUIBase_Label.SetupCursorForTextAlign(leftPos, text, 0, labelWidget.alignment, labelWidget.font, origLeftPos.x, totalWidth, widthMult);
				}

				Vector3 dir = trans.position - leftPos;
				leftPos     = trans.position - dir;

				Vector2		inTexPos	= new Vector2();
				Vector2		inTexSize	= new Vector2();

				for (int i = 0; i < text.Length; ++i)
				{
					float width;

					switch (text[i])
					{
					case '\n':
						numOfLine++;
						leftPos 	= origLeftPos;
						if(multiline == true)
						{
							leftPos = GUIBase_Label.SetupCursorForTextAlign(leftPos, text, i+1, labelWidget.alignment, labelWidget.font, origLeftPos.x, totalWidth, widthMult);
						}

						leftPos.y += (numOfLine*(lineHeight+lineSpace)) + (lineHeight * 0.5f);

						dir        = trans.position - leftPos;
						leftPos    = trans.position - dir;
						break;
					default:
						GUIBase_Font font = labelWidget.font as GUIBase_Font;
						if (font && font.GetCharDscr(text[i], out width, ref inTexPos, ref inTexSize))
						{
							width *= widthMult;
							//Debug.Log("Old Font renderer :: Width " + width);

							leftPos.x += 0.5f * width;

							//Debug.Log("-" + text[i] + "', char idx = " + (int)text[i] + ": w = " + width + ", posX = " + inTexPos.x + ", posY = " + inTexPos.y + ", sizeX = " + inTexSize.x + ", sizeY = " + inTexSize.y);
							RenderBasicWidget(w, leftPos, width, lineHeight, lossyScale, inTexPos, inTexSize, platformPos, platformShift);

							leftPos.x += 0.5f * width;
						}
						else
						{
							//Debug.LogWarning("Old Font renderer :: Width " + width);
						}
						break;
					}
				}
			}
		}
	}

	//
	// RenderLabelWidget
	//
	void RenderLabelWidgetNew(GUIBase_Widget widget, Vector2 platformPos, Vector2 platformShift)
	{
		GUIBase_Label	labelWidget = widget.GetComponent<GUIBase_Label>();
		GUIBase_FontEx	font = labelWidget.font as GUIBase_FontEx;
		Texture			fontTexture = null;
		if (font && font.fontMaterial)
		{
			fontTexture = font.fontMaterial.mainTexture;
		}

		if (labelWidget.isValid == true && font != null && fontTexture != null)
		{
			if(labelWidget.GenerateRunTimeData() == true)
			{
				EditorUtility.SetDirty(labelWidget);
			}

			Vector3 lossyScale = Vector3.one;

			float		totalWidth		= 0.0f;
			float		onScreenWidth	= 1.0f;
			//float		onScreenHeight	= 1.0f;
			float		widthMult		= 1.0f;

			// 1) Calc width of text
            //Vector2 textSize;
            //if(labelWidget.m_Font.GetTextSize(text, out textSize, false) == true)
            //    totalWidth = textSize.x;

			Texture2D	texture		= labelWidget.fontTexture;

			if(texture == null)
			{
				Debug.LogWarning("Missing texture" + MFDebugUtils.GetFullName(widget.gameObject) );
			}

			totalWidth = labelWidget.lineSize.x;
			float heightScale = texture.height;
			float lineHeight = labelWidget.lineSize.y * heightScale;
			float lineSpace  = labelWidget.lineSpace  * heightScale;
			//Debug.Log("**** lineHeight + " + lineHeight + " lineSpace + " + lineSpace);

			// 2)
			onScreenWidth	= widget.GetWidth();
			//onScreenHeight	= w.GetHeight() * trans.lossyScale.y;
			
			if (totalWidth != 0.0f)
			{
				widthMult	= onScreenWidth / totalWidth;
				
				//Debug.Log("WidthMult = " + widthMult);
				
				// 3) Start from left side
				Vector3		leftPos		= new Vector3();
				
				//leftPos = trans.position - deltaPos;
				int 		numOfLine = 0;
				Vector3 	origLeftPos = labelWidget.GetLeftUpPos(widget);
				leftPos 	= origLeftPos;
				leftPos.y  += lineHeight * 0.5f; // textHeight*0.5f;

				string text = labelWidget.Text;
				if (string.IsNullOrEmpty(text) == false && labelWidget.Uppercase == true)
				{
					text = text.ToUpper();
				}

				bool multiline = GUIBase_Label.IsMultiline(text);
				if(multiline == true)
				{
					leftPos = GUIBase_Label.SetupCursorForTextAlign(leftPos, text, 0, labelWidget.alignment, font, origLeftPos.x, totalWidth, widthMult);
				}

				Transform trans = widget.transform;
				Vector3 dir = trans.position - leftPos;
				leftPos     = trans.position - dir;

				Vector2		inTexPos	= new Vector2();
				Vector2		inTexSize	= new Vector2();
				//float 	fontHeight  = font.GetFontHeight();

				for (int i = 0; i < text.Length; ++i)
				{
					float width;

					switch (text[i])
					{
					case '\n':
						numOfLine++;
						leftPos 	= origLeftPos;
						if(multiline == true)
						{
							leftPos = GUIBase_Label.SetupCursorForTextAlign(leftPos, text, i+1, labelWidget.alignment, font, origLeftPos.x, totalWidth, widthMult);
						}

						leftPos.y += (numOfLine*(lineHeight+lineSpace)) + (lineHeight * 0.5f);

						dir        = trans.position - leftPos;
						leftPos    = trans.position - dir;
						break;
					default:

						Rect screenRect, sourceRect;
						if(font.GetCharDescription(text[i], out width, out screenRect, out sourceRect, true, false))
						//if (labelWidget.m_Font.GetCharDscr(text[i], out width, ref inTexPos, ref inTexSize))
						{
							width *= widthMult;

							inTexPos	= new Vector2(sourceRect.x, sourceRect.y);
							inTexSize	= new Vector2(sourceRect.width, sourceRect.height);

							//leftPos.x += 0.5f * width * Mathf.Cos(angle);
							//leftPos.y -= 0.5f * width * Mathf.Sin(angle);

							//Debug.Log("**** screenRect = " + screenRect);
							//Debug.Log("****" + text[i] + "', char idx = " + (int)text[i] + ": w = " + width + ", lineHeight = " + lineHeight + ", screenRect.width = " + screenRect.width + ", screenRect.height = " + screenRect.height);

							screenRect.x      = widthMult   * screenRect.x;//     /fontHeight;
							screenRect.y      = heightScale * screenRect.y;//	  /fontHeight;
							screenRect.width  = widthMult   * screenRect.width;// /fontHeight;
							screenRect.height = heightScale * screenRect.height;///fontHeight;

							Vector2	inCharSize    = new Vector2(screenRect.width, screenRect.height);
							Vector2	inCharCenter  = new Vector2(leftPos.x, leftPos.y) + new Vector2((screenRect.width + screenRect.x)*0.5f, screenRect.y*0.5f);

							//Debug.Log("****" + text[i] + "', char idx = " + (int)text[i] + ": w = " + width + ", posX = " + inTexPos.x + ", posY = " + inTexPos.y + ", sizeX = " + inTexSize.x + ", sizeY = " + inTexSize.y);
							//Debug.Log("****" + text[i] + "', char idx = " + (int)text[i] + ": w = " + width + ", inCharSize.x = " + inCharSize.x + ", inCharSize.y = " + inCharSize.y);
							RenderSprite(fontTexture, inCharCenter, inCharSize.x, inCharSize.y, lossyScale, inTexPos, inTexSize, platformPos, platformShift, widget.m_Grid9, widget.Color, GetClipRectangle(widget));

							//leftPos.x += 0.5f * width * Mathf.Cos(angle);
							//leftPos.y -= 0.5f * width * Mathf.Sin(angle);

							leftPos.x += width;
						}
						else
						{
							Debug.LogWarning("New Font renderer :: Width " + width);
						}

						break;
					}
				}
			}
		}
	}
	//
	// Process input
	//
	void ProcessInput()
	{
		Vector2	mousePos = Event.current.mousePosition;
		
		mousePos.x -= m_PlatformShift.x * m_BackgroundWidth * m_ZoomFactor;
		mousePos.y -= m_PlatformShift.y * m_BackgroundHeight * m_ZoomFactor;
		
		Vector3	layoutMousePos	= new Vector3();
			
		GetMousePosToLayoutPos(mousePos.x, mousePos.y, out layoutMousePos.x, out layoutMousePos.y);
		//Debug.Log("Layout pos " + posX + "," + posY);

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
			
			m_PlatformShift.x += pan;
			m_PlatformShift.x = Mathf.Clamp(m_PlatformShift.x, -1.0f, 0.0f);
		}
		else if (e.type == EventType.MouseDown && SecondaryButtons.Contains(e.button))
		{
			// Pan with platform
			m_Pan		= true;
			m_PanOrig	= m_PlatformShift;
			m_PanMouse	= Event.current.mousePosition;
		}
		else if (e.type == EventType.MouseUp && SecondaryButtons.Contains(e.button))
		{
			m_Pan = false;
		}
		// Pan ?
		else if (m_Pan && (e.type == EventType.MouseDrag) && SecondaryButtons.Contains(e.button))
		{
			Vector2 mouseDelta	= new Vector2();
					
			mouseDelta	= Event.current.mousePosition - m_PanMouse;
			mouseDelta /= m_ZoomFactor;
					
			mouseDelta.x /= m_BackgroundWidth;
			mouseDelta.y /= m_BackgroundHeight;
					
			m_PlatformShift = m_PanOrig + mouseDelta;
							
			// clamp platform shift
			m_PlatformShift.x = Mathf.Clamp(m_PlatformShift.x, -1.0f, 0.0f);
			m_PlatformShift.y = Mathf.Clamp(m_PlatformShift.y, -1.0f, 0.0f);
		}
		else if (e.type == EventType.KeyDown)
		{
			Vector2		pos = new Vector3(0.0f, 0.0f);
			
			switch (e.keyCode)
			{
			case KeyCode.UpArrow:
				pos.y = -1.0f;
				break;
				
			case KeyCode.DownArrow:
				pos.y = 1.0f;
				break;
				
			case KeyCode.LeftArrow:
				pos.x = -1.0f;
				break;
				
			case KeyCode.RightArrow:
				pos.x = 1.0f;
				break;
			}
			
			StepMoveSelectedObjects(pos);
		}
		else if (e.type == EventType.MouseDown && (e.button == PrimaryButton))
		{
			m_MouseDown		= true;
			
			if ((mousePos.x + m_PlatformShift.x * m_BackgroundWidth * m_ZoomFactor >= m_BackgroundPos.x) &&
				(mousePos.y + m_PlatformShift.y * m_BackgroundHeight * m_ZoomFactor >= m_BackgroundPos.y))
			{
				// selection of point of already selected object ?
				m_GizmoTouched = false;
				
				// returns true if gizmo was touched
				// in such case:
				//		m_GizmoTouchMode	... if it was touched on pivot, on axe x or y etc.
				//		m_GizmoTouchPos 	... position of mouse in the moment of touch
				
				if (m_GizmoVisible)
				{
					m_GizmoTouched = m_Gizmo.IsTouched(Event.current.mousePosition);
		
					if (m_GizmoTouched)
					{
						m_GizmoTouchPos = layoutMousePos;
						
						PrepareSelectedObjectsForTransform(layoutMousePos);
						
						m_GizmoRotation		= 0.0f;
						m_GizmoScale.x		= 1.0f;
						m_GizmoScale.y		= 1.0f;
					}
				}

				if (! m_GizmoTouched)
				{
					if (!SelectObjectByPos(layoutMousePos))
					{
						ClearSelectedObjects();
					}
				}
			}
			
			if (m_GizmoTouched)
			{
				GUIEditorUtils.RegisterSceneUndo("Move/scale/rotate Gizmo");
			}
		}
		else if (e.type == EventType.MouseUp && (e.button == PrimaryButton))
		{
			if (m_GizmoTouched)
			{
				GUIEditorUtils.RegisterSceneUndo("Move Gizmo");
			
				m_GizmoTouched				= false;
				m_GizmoScale.x				= 1.0f;
				m_GizmoScale.y				= 1.0f;
			}
			
			m_MouseDown					= false;
		}
		else
		{
			if (m_GizmoVisible && m_GizmoTouched)
			{
				GUIEditorGizmo.E_OperationMode	oMode = m_Gizmo.GetGizmoMode();
				
				switch (oMode)
				{
				case GUIEditorGizmo.E_OperationMode.E_OM_MOVE:
					MoveSelectedObjects(layoutMousePos);
					break;
					
				case GUIEditorGizmo.E_OperationMode.E_OM_SCALE:
					ScaleSelectedObjects(layoutMousePos);
					break;

				case GUIEditorGizmo.E_OperationMode.E_OM_ROTATE:
					RotateSelectedObjects(layoutMousePos);
					break;

				case GUIEditorGizmo.E_OperationMode.E_OM_SIZE:
					ResizeSelectedObjects(layoutMousePos);
					break;

				default:
					Debug.LogError("Unsupported case");
					break;
				}
			}
		}	
	}
	
	//
	// MoveSelectedObjects
	//
	void MoveSelectedObjects(Vector2 layoutMousePos)
	{
		GUIEditorGizmo.E_SelectedPart	selPart = m_Gizmo.GetSelectedPart();
				
		//
		// TODO - tady je nadherne videt jak je to na hovno, kdyz nemam ke vsem selektnutelnym objektum stejny pristup
		// musim 3x duplikovat kod
		//
		// TODO - navic tady chybi pripadna rotace!
		//

		for (int i = 0; i < m_SelectedObjects.Count; ++i)
		{
			S_SelectedObject sObject = (S_SelectedObject)m_SelectedObjects[i];
			if (sObject.m_Element == null)
				continue;
			
			Transform tr = sObject.m_Element.transform;
			Vector3  tmp = tr.position;
			
			// Some axe restriction?
			
			if (selPart == GUIEditorGizmo.E_SelectedPart.E_SP_AXE_X)
			{
				tmp.x	= layoutMousePos.x - sObject.m_Offset.x;
			}
			else if (selPart == GUIEditorGizmo.E_SelectedPart.E_SP_AXE_Y)
			{
				tmp.y	= layoutMousePos.y - sObject.m_Offset.y;
			}
			else
			{
				tmp.x	= layoutMousePos.x - sObject.m_Offset.x;
				tmp.y	= layoutMousePos.y - sObject.m_Offset.y;
			}
			
			tmp.x = Mathf.RoundToInt(tmp.x);
			tmp.y = Mathf.RoundToInt(tmp.y);
			tmp.z = Mathf.RoundToInt(tmp.z);
			
			tr.position = tmp;
		}
	}
	
	//
	// StepMoveSelectedObjects
	//
	void StepMoveSelectedObjects(Vector2 step)
	{
		for (int i = 0; i < m_SelectedObjects.Count; ++i)
		{
			S_SelectedObject sObject = (S_SelectedObject)m_SelectedObjects[i];
			if (sObject.m_Element == null)
				continue;
			
			Transform tr = sObject.m_Element.transform;
			Vector3  tmp = tr.position;
			
			tmp.x += step.x;
			tmp.y += step.y;
			
			tmp.x = Mathf.RoundToInt(tmp.x);
			tmp.y = Mathf.RoundToInt(tmp.y);
			tmp.z = Mathf.RoundToInt(tmp.z);
			
			tr.position = tmp;
		}
	}
	
	//
	// ScaleSelectedObjects
	//
	void ScaleSelectedObjects(Vector2 layoutMousePos)
	{
		GUIEditorGizmo.E_SelectedPart	selPart = m_Gizmo.GetSelectedPart();
		
		Vector2	deltaMousePos		= new Vector2(layoutMousePos.x - m_GizmoTouchPos.x, -(layoutMousePos.y - m_GizmoTouchPos.y));
		
		m_GizmoScale.x = 1.0f + deltaMousePos.x * m_ZoomFactor / m_Gizmo.GetDefaultAxeLength();
		m_GizmoScale.y = 1.0f + deltaMousePos.y * m_ZoomFactor / m_Gizmo.GetDefaultAxeLength();
		
		// Omezeni pohybu v jedne z os ?
		if (selPart == GUIEditorGizmo.E_SelectedPart.E_SP_AXE_X)
		{
			m_GizmoScale.y = 1.0f;
		}
		else if (selPart == GUIEditorGizmo.E_SelectedPart.E_SP_AXE_Y)
		{
			m_GizmoScale.x = 1.0f;
		}
		else
		{
			// keep same ratio
			m_GizmoScale.x += -1.0f + m_GizmoScale.y;
			m_GizmoScale.y = m_GizmoScale.x;
		}
		
		//Debug.Log("scale = " + m_GizmoScaleDelta.x + ", " + m_GizmoScaleDelta.y);
		
		// Change scale of all selected objects
		
		for (int i = 0; i < m_SelectedObjects.Count; ++i)
		{
			S_SelectedObject	sObject = (S_SelectedObject)m_SelectedObjects[i];

			Vector3	sScale = new Vector3(sObject.m_Scale.x * m_GizmoScale.x, sObject.m_Scale.y * m_GizmoScale.y, sObject.m_Scale.z);
	
			if (sObject.m_Element)
			{
				sObject.m_Element.transform.localScale = sScale;
			}
		}
	}
	
	//
	// RotateSelectedObjects
	//
	void RotateSelectedObjects(Vector2 layoutMousePos)
	{
		Vector2	deltaMousePos = new Vector2(layoutMousePos.x - m_GizmoTouchPos.x, -(layoutMousePos.y - m_GizmoTouchPos.y));
		
		m_GizmoRotation = (deltaMousePos.x + deltaMousePos.y) * DEFAULT_ROT_ANGLE_MOUSE_DELTA * Mathf.Deg2Rad;
		
		//Debug.Log("rotAngle = " + m_GizmoRotation);
		
		// Change scale of all selected objects
		
		for (int i = 0; i < m_SelectedObjects.Count; ++i)
		{
			S_SelectedObject	sObject = (S_SelectedObject)m_SelectedObjects[i];
			
			float	rotAngle	= sObject.m_RotAngle + m_GizmoRotation;
			Vector3	eulerAngles	= new Vector3(0.0f, 0.0f, rotAngle);
			
			if (sObject.m_Element)
			{
				sObject.m_Element.transform.localEulerAngles = eulerAngles;
			}
		}		
	}
		
	//
	// ResizeSelectedObjects
	//
	void ResizeSelectedObjects(Vector2 layoutMousePos)
	{
		GUIEditorGizmo.E_SelectedPart	selPart = m_Gizmo.GetSelectedPart();
		
		Vector2	deltaMousePos		= new Vector2(layoutMousePos.x - m_GizmoTouchPos.x, -(layoutMousePos.y - m_GizmoTouchPos.y));
		
		m_GizmoScale.x = 1.0f + deltaMousePos.x * m_ZoomFactor / m_Gizmo.GetDefaultAxeLength();
		m_GizmoScale.y = 1.0f + deltaMousePos.y * m_ZoomFactor / m_Gizmo.GetDefaultAxeLength();
		
		// Omezeni pohybu v jedne z os ?
		if (selPart == GUIEditorGizmo.E_SelectedPart.E_SP_AXE_X)
		{
			m_GizmoScale.y = 1.0f;
		}
		else if (selPart == GUIEditorGizmo.E_SelectedPart.E_SP_AXE_Y)
		{
			m_GizmoScale.x = 1.0f;
		}
		else
		{
			// keep same ratio
			m_GizmoScale.x += -1.0f + m_GizmoScale.y;
			m_GizmoScale.y = m_GizmoScale.x;
		}
		
		//Debug.Log("scale = " + m_GizmoScaleDelta.x + ", " + m_GizmoScaleDelta.y);
		
		// Change scale of all selected objects
		
		for (int i = 0; i < m_SelectedObjects.Count; ++i)
		{
			S_SelectedObject sObject = (S_SelectedObject)m_SelectedObjects[i];
			GUIBase_Widget   sWidget = sObject.m_Element as GUIBase_Widget;
			if (sWidget)
			{
				sWidget.SetScreenSize(
					sObject.m_Size.x * m_GizmoScale.x,
					sObject.m_Size.y * m_GizmoScale.y
				);
			}
		}
	}

	//
	// PrepareSelectedObjectsForTransform
	//
	void PrepareSelectedObjectsForTransform(Vector3 layoutClickPos)
	{
		for (int i = 0; i < m_SelectedObjects.Count; ++i)
		{
			S_SelectedObject	sObj = (S_SelectedObject)m_SelectedObjects[i];
			
			if (sObj.m_Element)
			{
				sObj.m_Offset	= layoutClickPos - sObj.m_Element.transform.position;
				sObj.m_Scale	= sObj.m_Element.transform.localScale;
				sObj.m_RotAngle	= sObj.m_Element.transform.eulerAngles.z;

				GUIBase_Widget widget = sObj.m_Element as GUIBase_Widget;
				if (widget)
				{
					sObj.m_Size = new Vector2(widget.GetWidth(), widget.GetHeight());
				}
				
				m_SelectedObjects[i] = sObj;
			}
		}
		
		GUIEditorUtils.RegisterSceneUndo("Select widget or layout");
	}
	
	//
	// SelectObjectByPos
	//
	bool SelectObjectByPos(Vector2 layoutClickPos)
	{
		if (m_PrecachedLayouts == null)
		{
			return false;
		}
		
		foreach (S_LayoutDscr lDscr in m_PrecachedLayouts)
		{
			if(m_OnlySelectedLayouts && !IsSomeObjectFromLayoutSelected(lDscr.m_Layout) )
			{
				continue;
			}

			if (lDscr.m_Widgets != null)
			{
				for (int lIdx = GUIEditorUtils.MAX_LAYERS - 1; lIdx >= 0; --lIdx)
				{
					foreach (GUIBase_Widget w in lDscr.m_Widgets)
					{
						if (w.m_GuiWidgetLayer == lIdx)
						{
							if (!GetFreezeFlag(w.gameObject))
							{
								Transform trans = w.transform;
								if (IsClickInside(trans.position, trans.lossyScale, trans.rotation.z, w.GetWidth(), w.GetHeight(), layoutClickPos))
								{
									if (IsWidgetSelected(w) == -1)
									{
										S_SelectedObject	sObj = new S_SelectedObject();
								
										sObj.m_Element		= w;
	
										m_SelectedObjects.Add(sObj);
									}
								
									Selection.activeGameObject	= w.gameObject;
									m_SelectedWidget			= w;
							
									GUIEditorUtils.RegisterSceneUndo("Select widget");
							
									// Update of selected objects
									UpdateSelectionByHiearchyView();
					
									return true;
								}
							}
						}
					}
				}
			}
		}

		return false;
	}
		
	//
	// ClearSelectedObjects
	//
	void ClearSelectedObjects()
	{
		m_Gizmo.Reset();
		
		// clear old selection
		m_SelectedObjects.RemoveRange(0, m_SelectedObjects.Count);
		
		if (m_SelectedWidget == Selection.activeGameObject)
		{
			Selection.activeGameObject = null;
		}
		
		m_SelectedWidget = null;
		
		if (m_Layout.Layout && !GetFreezeFlag(m_Layout.Layout.gameObject))
		{
			Selection.activeGameObject = m_Layout.Layout.gameObject;
		}
		else if (m_Layout.Platform)
		{
			Selection.activeGameObject = m_Layout.Platform.gameObject;
		}
	}

	//
	// IsClickInside (widget, platform ?)
	//
	bool IsClickInside(Vector3 pos, Vector3 lossyScale, float angle, float width, float height, Vector2 layoutClickPos)
	{
		width  *= lossyScale.x;
		height *= lossyScale.y;

		Rect     rect = new Rect(pos.x - width * 0.5f, pos.y - height * 0.5f, width, height);
		Vector2 point = new Vector2(layoutClickPos.x/* * Mathf.Cos(angle)*/, layoutClickPos.y/* * Mathf.Sin(angle)*/);

		if ((rect.xMin <= point.x) && (rect.xMax >= point.x) &&
			(rect.yMin <= point.y) && (rect.yMax >= point.y))
		{
			return true;
		}
		
		return false;
	}

	//
	// IsWidgetSelected()
	// returns index of selected widget or -1 (if it is not selected)
	int IsWidgetSelected(GUIBase_Widget w)
	{
		for (int i = 0; i < m_SelectedObjects.Count; ++i)
		{
			if (((S_SelectedObject)m_SelectedObjects[i]).m_Element == w)
			{
				return i;
			}
		}
		
		return -1;
	}

	//
	// GetPlatformPosOnScreen
	//
	void GetPlatformPosOnScreen(float posX, float posY, out float platformPosX, out float platformPosY)
	{
		platformPosX = posX + ((m_BackgroundWidth * 0.5f) - (m_PlatformXRes * 0.5f)) * m_ZoomFactor;
		platformPosY = posY + ((m_BackgroundHeight * 0.5f) - (m_PlatformYRes * 0.5f)) * m_ZoomFactor;
	}

	//
	// GetMousePosToLayoutPos
	//
	void GetMousePosToLayoutPos(float mousePosX, float mousePosY, out float posX, out float posY)
	{
		posX = (mousePosX - m_PlatformPos.x) / m_ZoomFactor;
		posY = (mousePosY - m_PlatformPos.y) / m_ZoomFactor;
	}

	//
	// CheckDistToPoint
	//
	void CheckDistToPoint(int idx, Vector2 point, Vector2 mousePos, ref float minDist, ref int minDistIdx, float selectionDistTreshold)
	{
		Vector2	deltaPos = new Vector2();
		
		deltaPos = mousePos - point;
		deltaPos *= m_ZoomFactor;
		
		if ((Mathf.Abs(deltaPos.x) <= selectionDistTreshold) &&
			(Mathf.Abs(deltaPos.y) <= selectionDistTreshold))
		{
			float tmpDist = deltaPos.magnitude;
				
			if (tmpDist < minDist)
			{
				minDist		= tmpDist;
				minDistIdx	= idx;
			}
		}
	}
	
	//
	// Set freeze flag to object
	//
	void SetFreezeFlagRecursive(GameObject gObj, bool f)
	{
		GUIBase_Element[]	elements = gObj.GetComponentsInChildren<GUIBase_Element>();
		
		foreach (var element in elements)
		{
			SetFreezeFlag(element.gameObject, f);
		}
	}
	
	void SetFreezeFlag(GameObject gObj, bool f)
	{
		if (f)
		{
			if (!GetFreezeFlag(gObj))
			{
				m_FreezeMap.Add(gObj, true);
			}
		}
		else
		{
			if (GetFreezeFlag(gObj))
			{
				m_FreezeMap.Remove(gObj);
			}
		}
	}
	
	//
	// Returns true if object is freezed
	//
	bool GetFreezeFlag(GameObject gObj)
	{
		return m_FreezeMap.Contains(gObj);
	}
	
	void ShowWidgetCopyPaste()
	{
		EditorGUILayout.BeginHorizontal();
		
		if (GUILayout.Button("Copy Widget"))
		{
			GUIEditorWidget.CopyFromWidget(m_SelectedWidget, GUIEditorWidget.m_EditedAreaCopy);			
			Debug.Log("Copy: stored UV from widget: '" + m_SelectedWidget.name + "'");
		}
		else 
		if (GUILayout.Button("Paste Widget"))
		{
			if (m_SelectedWidget)
			{
				GUIEditorWidget.UpdateWidgetByArea(m_SelectedWidget, GUIEditorWidget.m_EditedAreaCopy, true);
				Debug.Log("Paste: applied on widget: '" + m_SelectedWidget.name + "'");
			}
		}
		
		EditorGUILayout.EndHorizontal();
	}
	
}
