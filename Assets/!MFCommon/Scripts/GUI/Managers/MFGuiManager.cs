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

/*
 
 MFGuiManager

 Hlavni classa Gui. Singleton, ktery vytvari a drzi v sobe vsechny MFGuiRenderery.
 Widget ktery se chce renderovat se zaregistruje do MFGuiManagera pomoci RegisterWidget.
 Tato funkce mu vrati Renderer, ktery se pro widget bude pouzivat (renderer se sdili pro vsechny widgety stejneho layeru a materialu).
 
*/

using UnityEngine;
//using System;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("GUI/Hierarchy/MFGuiManager")]
public class MFGuiManager : MonoBehaviour
{
	public LayerMask UILayer = 0;
	public int DrawDepth = 10;

	public static MFGuiManager Instance;

	//
	// private data
	//

	static int RENDER_QUEUE_BASE = 10000;

	List<GUIBase_Platform> m_Platforms = new List<GUIBase_Platform>();
	List<GUIBase_Layout> m_Layouts = new List<GUIBase_Layout>();

	public Camera m_UiCamera;
	GameObject m_UiCameraHolder;

	AudioSource m_AudioSource;

	Dictionary<ulong, MFGuiRenderer> m_GUIRenderers;
	List<MFGuiRenderer> m_RenderersForActivation = new List<MFGuiRenderer>();
	List<S_ObjectToChangeVisibility> m_ObjectsToChangeVisibility;

	public struct S_ObjectToChangeVisibility
	{
		public GameObject m_GObj;
		public bool m_Visible;
		public bool m_Recursive;

		public S_ObjectToChangeVisibility(GameObject gObj, bool show, bool recursive)
		{
			m_GObj = gObj;
			m_Visible = show;
			m_Recursive = recursive;
		}
	};

	WidgetAnimation.Base m_ActiveAnimation;
	Queue<WidgetAnimation.Base> m_Animations = new Queue<WidgetAnimation.Base>();
	float m_NextAnimationDelay;
	Vector2 m_CachedScreenSize;

	//---------------------------------------------------------

	void OnLevelWasLoaded(int level)
	{
		if (Game.Instance && Game.Instance.AppType == Game.E_AppType.DedicatedServer)
		{
			DestroyImmediate(gameObject);
			return;
		}
	}

	void Awake()
	{
		if (Instance)
		{
			Destroy(this.gameObject);
			return;
		}

		Instance = this;

		m_UiCameraHolder = new GameObject("UI Camera");
		m_UiCameraHolder.AddComponent<Camera>();
		m_UiCamera = m_UiCameraHolder.GetComponent<Camera>();
		m_UiCamera.clearFlags = CameraClearFlags.Depth;
		m_UiCamera.nearClipPlane = 0.3f;
		m_UiCamera.farClipPlane = 600.0f;
		m_UiCamera.depth = DrawDepth;
		m_UiCamera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
		m_UiCamera.orthographic = true;
		m_UiCamera.orthographicSize = Screen.height*0.5f;
		m_UiCamera.cullingMask = UILayer;
		m_UiCamera.transform.position = new Vector3(0.0f, 0.0f, -100.0f);

		// hash table of GUI Renderers
		m_GUIRenderers = new Dictionary<ulong, MFGuiRenderer>();

		m_CachedScreenSize = new Vector2(Screen.width, Screen.height);
	}

	void OnDestroy()
	{
		if (uLink.Network.isClient == false)
			return;

		if (m_ObjectsToChangeVisibility != null)
			m_ObjectsToChangeVisibility.Clear();
		m_RenderersForActivation.Clear();
		m_Layouts.Clear();

		if (m_GUIRenderers != null)
			m_GUIRenderers.Clear();
	}

	//---------------------------------------------------------
	void Start()
	{
//		Debug.Log("Screen.width = " + Screen.width + ", Screen.Height = " + Screen.height);

		m_ObjectsToChangeVisibility = new List<S_ObjectToChangeVisibility>();
	}

	//---------------------------------------------------------
	public static bool IsAnimating
	{
		get
		{
			if (Instance == null)
				return false;
			return Instance.m_Animations.Count > 0;
		}
	}

	public static WidgetAnimation.NumericButton AnimateWidget(GUIBase_Button button, int source, int target)
	{
		if (Instance == null)
			return default(WidgetAnimation.NumericButton);

		WidgetAnimation.NumericButton animation = new WidgetAnimation.NumericButton(button, source, target);
		Instance.m_Animations.Enqueue(animation);

		return animation;
	}

	public static WidgetAnimation.NumericLabel AnimateWidget(GUIBase_Label label, float source, float target)
	{
		if (Instance == null)
			return default(WidgetAnimation.NumericLabel);

		WidgetAnimation.NumericLabel animation = new WidgetAnimation.NumericLabel(label, source, target);
		Instance.m_Animations.Enqueue(animation);

		return animation;
	}

	public static WidgetAnimation.Label AnimateWidget(GUIBase_Label label, string text)
	{
		if (Instance == null)
			return default(WidgetAnimation.Label);

		WidgetAnimation.Label animation = new WidgetAnimation.Label(label, text);
		Instance.m_Animations.Enqueue(animation);

		return animation;
	}

	public static WidgetAnimation.Widget AnimateWidget(GUIBase_Widget widget)
	{
		if (Instance == null)
			return default(WidgetAnimation.Widget);

		WidgetAnimation.Widget animation = new WidgetAnimation.Widget(widget);
		Instance.m_Animations.Enqueue(animation);

		return animation;
	}

	public static void FlushAnimations()
	{
		if (Instance == null)
			return;

		if (Instance.m_ActiveAnimation != null)
		{
			Instance.m_ActiveAnimation.ForceFinish();
			Instance.m_ActiveAnimation.Update();
			Instance.m_ActiveAnimation = null;
		}

		while (Instance.m_Animations.Count > 0)
		{
			WidgetAnimation.Base animation = Instance.m_Animations.Dequeue();
			animation.Start();
			animation.ForceFinish();
			animation.Update();
		}

		Instance.m_NextAnimationDelay = 0.0f;
	}

	//---------------------------------------------------------
	void FixedUpdate()
	{
		if (m_UiCamera.enabled == false)
			return;

#if UNITY_STANDALONE
		if ((double)m_CachedScreenSize.x != (double)Screen.width || (double)m_CachedScreenSize.y != (double)Screen.height)
		{
			ScreenSizeChanged();
		}
#endif

		if (m_NextAnimationDelay > 0.0f)
		{
			m_NextAnimationDelay -= Time.deltaTime;
			if (m_NextAnimationDelay > 0.0f)
				return;
		}

		if (m_ActiveAnimation == null && m_Animations.Count > 0)
		{
			m_ActiveAnimation = m_Animations.Dequeue();

			if (m_ActiveAnimation != null)
			{
				m_ActiveAnimation.Start();
			}
		}

		if (m_ActiveAnimation != null)
		{
			m_ActiveAnimation.Update();

			if (m_ActiveAnimation.Visible == false || m_ActiveAnimation.Finished == true)
			{
				m_ActiveAnimation = null;
				m_NextAnimationDelay = 0.1f;
			}
		}
	}

	void ScreenSizeChanged()
	{
		m_CachedScreenSize.x = Screen.width;
		m_CachedScreenSize.y = Screen.height;

		m_UiCamera.orthographicSize = Screen.height*0.5f;

		foreach (var platform in m_Platforms)
		{
			CalculatePlatformSize(platform);
			foreach (var layout in platform.Layouts)
			{
				layout.OnElementVisible();
			}
		}
	}

	//---------------------------------------------------------
	// Pokud ma zaregistrovany stejny material a dany material ma totozne renderQueueIdx (ma se renderevoat ve stejnem momentu), prida widget do tohoto rendereru
	// Jinak naklonuje material a vytvori pro nej novy renderer, ktery vrati.
	public MFGuiRenderer RegisterWidget(GUIBase_Widget w, Material material, int renderQueueIdx)
	{
		MFGuiRenderer guiRenderer = null;

		//Debug.Log("Widget " + w.name + " layout ID " + w.GetLayoutUniqueId());

		if (material)
		{
			ulong rendererId = CalcRendererKey(renderQueueIdx, w.GetLayoutUniqueId(), material);

			if (!m_GUIRenderers.TryGetValue(rendererId, out guiRenderer))
			{
				//GameObject	guiRendererHolder = new GameObject("MF Gui Renderer - " + w.name);
				GameObject guiRendererHolder = new GameObject("MFGuiRenderer-" + material.GetInstanceID() + "-" + renderQueueIdx + "-" + w.name);

				guiRenderer = guiRendererHolder.AddComponent<MFGuiRenderer>() as MFGuiRenderer;

//				guiRenderer.allocBlockSize		= 10;
				guiRenderer.plane = MFGuiRenderer.SPRITE_PLANE.XY;
//				guiRenderer.autoUpdateBounds	= true;
				guiRenderer.UILayer = UILayer;
				guiRenderer.ZeroLocation = MFGuiRenderer.ZeroLocationEnum.UpperLeft;

				for (int i = 0; i < sizeof (int)*8; i++)
				{
					if ((UILayer.value & (1 << i)) == (1 << i))
					{
						guiRendererHolder.layer = i;
						break;
					}
				}

				// prepare new material (clone original and use new renderQueueIdx)
				Material newMaterial = (Material)Instantiate(material);

				newMaterial.renderQueue = RENDER_QUEUE_BASE + renderQueueIdx;
				newMaterial.name = material.name + "-" + material.GetInstanceID() + "-" + renderQueueIdx + "-" + w.name;

				// set material
				guiRenderer.material = newMaterial;

				// Add new GUI Renderer to container
				m_GUIRenderers.Add(rendererId, guiRenderer);
			}
		}

		if (guiRenderer != null && guiRenderer != w.GetGuiRenderer())
		{
			guiRenderer.RegisterWidget(w);
		}

		return guiRenderer;
	}

	public bool UnRegisterWidget(GUIBase_Widget inWidget, MFGuiRenderer inGuiRenderer)
	{
		if (inGuiRenderer != null)
		{
			if (0 == inGuiRenderer.UnRegisterWidget(inWidget))
			{
				foreach (KeyValuePair<ulong, MFGuiRenderer> entry in m_GUIRenderers)
				{
					if (entry.Value == inGuiRenderer)
					{
						m_GUIRenderers.Remove(entry.Key);
						Destroy(inGuiRenderer.gameObject);
						break;
					}
				}
			}
			return true;
		}
		return false;
	}

	//---------------------------------------------------------
	public void RegisterLayout(GUIBase_Layout layout)
	{
		if (m_Layouts.Contains(layout) == true)
			return;
		m_Layouts.Add(layout);
	}

	//---------------------------------------------------------
	public void RegisterPlatform(GUIBase_Platform platform)
	{
		if (m_Platforms.Contains(platform) == true)
			return;

		m_Platforms.Add(platform);

		platform.Pivots = platform.GetComponentsInChildren<GUIBase_Pivot>();
		platform.Layouts = platform.GetComponentsInChildren<GUIBase_Layout>();

		CalculatePlatformSize(platform);
	}

	public void UnregisterPlatform(GUIBase_Platform platform)
	{
		int idx = m_Platforms.IndexOf(platform);
		if (idx == -1)
			return;

		m_Platforms.RemoveAt(idx);
	}

	public static bool ForcePreserveAspectRatio
	{
		get
		{
#if UNITY_IPHONE
			// It looks that pads belong into the iPhone family too, thus the following "iPhone" condition is useless
			if (SystemInfo.operatingSystem.Contains("iPhone"))
			{
				if (
					UnityEngine.iOS.Device.generation != UnityEngine.iOS.DeviceGeneration.iPhone5 &&
					UnityEngine.iOS.Device.generation != UnityEngine.iOS.DeviceGeneration.iPhone5C &&
					UnityEngine.iOS.Device.generation != UnityEngine.iOS.DeviceGeneration.iPhone5S &&
					UnityEngine.iOS.Device.generation != UnityEngine.iOS.DeviceGeneration.iPodTouch5Gen &&
					UnityEngine.iOS.Device.generation != UnityEngine.iOS.DeviceGeneration.iPhone6 &&
					UnityEngine.iOS.Device.generation != UnityEngine.iOS.DeviceGeneration.iPhone6Plus
					)
				{
					return true;
				}
			}
#elif UNITY_ANDROID
			// We can't really look for the exact aspect ratios on Android. One problem is the bottom system menu which is configurable on various devices.
			// Sometimes it is always displayed and reduces the screen height which affects the aspect ratio. Sometimes it displays as overlay.
			// This is really a tricky thing. Additionaly we can't really expect what devices and what screen aspect rations are currently
			// (or will come in the future) on the market.


			// Thus, let's try a simple approach. For the wide screens, scale the dialog height down to fit the screen height, for the "tall" screens, let's
			// maintain the aspect ratio and correct the layout by centering the pop-ups menu on the screen. The magic constant was defined by the first 4:3
			// tablet on the Android market. It has resolution 2048x1536 (1.333) which is perfect example of 4:3. But it reduces the application screen by the
			// bottom system menu to 2048x1440 (1.4222). Additionally, there are 3:2 devices (including old iPhones) which are still not considered as wide
			// screens. Thus, let's set the value to 1.52. The standard wide-screen ratio is 1.7777.

			float deviceAspectRatio = (float)Screen.width / (float)Screen.height;
			if (deviceAspectRatio <= 1.52f)
				return true;
#endif

			return false;
		}
	}

	void CalculatePlatformSize(GUIBase_Platform platform)
	{
		//Spocitame cim prenasobit vysku aby sedela na vysku scutecne obrazovky.
		//Drzime aspect ratio, takze prenasobime sirku stejnou konstantou.
		//Pote spocitame rozil mezi vyslednou sirkou a sirkou obrazovky a o polovinu to posuneme v x-ose (pokud je sirka vetsi nez obrazovka, orezeme, pokud memsi, posuneme na stred).

		// Fit menu on screen
		Vector3 scale = new Vector3((float)Screen.width/platform.m_Width, (float)Screen.height/platform.m_Height, 1.0f);

		Transform trans = platform.transform;

		if (ForcePreserveAspectRatio)
		{
			Vector3 platformShift = trans.position;
			//platformShift.x	= (scale.x - scale.y) * platform.m_Width ;
			platformShift.y	= (scale.y - scale.x) * platform.m_Height * 0.5f;

			//m_UiCamera.transform.position = new Vector3(-, m_UiCamera.transform.position.y, m_UiCamera.transform.position.z);
			scale.y = scale.x;
			trans.position = platformShift;
		}

		//	Debug.Log("Screen: " + Screen.width + ":" + Screen.height + ", platform: " + platform.m_Width + ":" + platform.m_Height + " Scale = " + scale.x + "," + scale.y);

		//Debug.Log("scale = " + scale.x + ", " + scale.y + ", " + scale.z);			
		trans.localScale = scale;

		// search for all GUIBase_Layouts and GUIBase_Pivots under this platform

		foreach (GUIBase_Layout l in platform.Layouts)
		{
			l.SetPlatformSize(platform.m_Width, platform.m_Height, scale.x, scale.y);
		}
	}

	//---------------------------------------------------------
	public GUIBase_Platform FindPlatform(string platformName)
	{
		GameObject gObj = GameObject.Find(platformName);

		if (gObj)
		{
			GUIBase_Platform platform = gObj.GetComponent<GUIBase_Platform>();

			return platform;
		}

		return null;
	}

	//---------------------------------------------------------
	public GUIBase_Platform GetPlatform(GUIBase_Layout layout)
	{
		return m_Platforms.Find(obj => System.Array.IndexOf(obj.Layouts, layout) != -1);
	}

	//---------------------------------------------------------
	public GUIBase_Platform GetPlatform(GUIBase_Pivot pivot)
	{
		return m_Platforms.Find(obj => System.Array.IndexOf(obj.Pivots, pivot) != -1);
	}

	//---------------------------------------------------------
	public GUIBase_Layout GetLayout(string name)
	{
		foreach (var layout in m_Layouts)
		{
			if (layout != null && layout.name == name)
			{
				return layout;
			}
		}
		return null;
	}

	public void RegisterRendererForActivation(MFGuiRenderer renderer)
	{
		if (m_RenderersForActivation.Contains(renderer) == true)
			return;
		if (m_RenderersForActivation.Count == m_RenderersForActivation.Capacity)
		{
			m_RenderersForActivation.Capacity += 10;
		}
		m_RenderersForActivation.Add(renderer);
	}

	//---------------------------------------------------------
	void LateUpdate()
	{
		if (m_UiCamera.enabled == false)
			return;

#if UNITY_EDITOR
// AX : Debug code for testing localizations...
		if (Input.GetKeyDown(KeyCode.Keypad0))
		{
			Debug.Log("Input.GetKeyDown(KeyCode.Keypad0) -> English.Old");
			TextDatabase.instance.Reload(SystemLanguage.English);
			OnLanguageChanged("English.Old");
		}
		else if (Input.GetKeyDown(KeyCode.Keypad1))
		{
			Debug.Log("Input.GetKeyDown(KeyCode.Keypad1) -> English");
			TextDatabase.instance.Reload(SystemLanguage.English);
			OnLanguageChanged("English");
		}
		else if (Input.GetKeyDown(KeyCode.Keypad2))
		{
			Debug.Log("Input.GetKeyDown(KeyCode.Keypad2) -> German");
			TextDatabase.instance.Reload(SystemLanguage.German);
			OnLanguageChanged("German");
		}
		else if (Input.GetKeyDown(KeyCode.Keypad3))
		{
			Debug.Log("Input.GetKeyDown(KeyCode.Keypad3) -> French");
			TextDatabase.instance.Reload(SystemLanguage.French);
			OnLanguageChanged("French");
		}
		else if (Input.GetKeyDown(KeyCode.Keypad4))
		{
			Debug.Log("Input.GetKeyDown(KeyCode.Keypad4) -> Italian");
			TextDatabase.instance.Reload(SystemLanguage.Italian);
			OnLanguageChanged("Italian");
		}
		else if (Input.GetKeyDown(KeyCode.Keypad5))
		{
			Debug.Log("Input.GetKeyDown(KeyCode.Keypad5) -> Spanish");
			TextDatabase.instance.Reload(SystemLanguage.Spanish);
			OnLanguageChanged("Spanish");
		}
		else if (Input.GetKeyDown(KeyCode.Keypad6))
		{
			Debug.Log("Input.GetKeyDown(KeyCode.Keypad6) -> Russian");
			TextDatabase.instance.Reload(SystemLanguage.Russian);
			OnLanguageChanged("Russian");
		}
		else if (Input.GetKeyDown(KeyCode.Keypad7))
		{
			Debug.Log("Input.GetKeyDown(KeyCode.Keypad7) -> Chinese");
			TextDatabase.instance.Reload(SystemLanguage.Chinese);
			OnLanguageChanged("Chinese");
		}
		else if (Input.GetKeyDown(KeyCode.Keypad8))
		{
			Debug.Log("Input.GetKeyDown(KeyCode.Keypad8) -> Japanese");
			TextDatabase.instance.Reload(SystemLanguage.Japanese);
			OnLanguageChanged("Japanese");
		}
		else if (Input.GetKeyDown(KeyCode.Keypad9))
		{
			Debug.Log("Input.GetKeyDown(KeyCode.Keypad9) -> Korean");
			TextDatabase.instance.Reload(SystemLanguage.Korean);
			OnLanguageChanged("Korean");
		}

#endif //UNITY_EDITOR

		// update layouts
		foreach (var layout in m_Layouts)
		{
			if (layout == null)
				continue;

			if (layout.IsVisible() == true || layout.IsDirty == true)
			{
				layout.GUIUpdate(1.0f);
			}
		}

		// Show/Hide widgets (in order of demands)
		if (m_ObjectsToChangeVisibility != null && m_ObjectsToChangeVisibility.Count > 0)
		{
			List<S_ObjectToChangeVisibility> objectsToChangeVisibility = new List<S_ObjectToChangeVisibility>(m_ObjectsToChangeVisibility.Count);
			objectsToChangeVisibility.AddRange(m_ObjectsToChangeVisibility);
			m_ObjectsToChangeVisibility.RemoveRange(0, m_ObjectsToChangeVisibility.Count);

			foreach (var vObj in objectsToChangeVisibility)
			{
				GUIBase_Layout layout = vObj.m_GObj.GetComponent<GUIBase_Layout>();
				if (layout != null)
				{
					layout.ShowImmediate(vObj.m_Visible);
				}
				else
				{
					GUIBase_Widget widget = vObj.m_GObj.GetComponent<GUIBase_Widget>();
					if (widget != null)
					{
						widget.ShowImmediate(vObj.m_Visible, vObj.m_Recursive);
					}
				}
			}
		}

		// activate / deactivate GUI renderers acording to its active sprites
		if (m_RenderersForActivation.Count > 0)
		{
			foreach (MFGuiRenderer renderer in m_RenderersForActivation)
			{
				if (renderer == null)
					continue;

				bool shouldBeActive = renderer.IsAnySpriteActive();
				GameObject renderObj = renderer.gameObject;

				if (shouldBeActive != renderObj.activeSelf)
				{
					renderObj.SetActive(shouldBeActive);
				}
			}
			m_RenderersForActivation.Clear();
		}
	}

	//---------------------------------------------------------
	// hide all its widgets
	public void HideAllLayouts()
	{
		foreach (var layout in m_Layouts)
		{
			if (layout != null)
			{
				layout.Show(false);
			}
		}
	}

	//---------------------------------------------------------	
	public void Show(GUIBase_Element element, bool state, bool recursive)
	{
		if (element is GUIBase_Pivot)
		{
			GUIBase_Pivot pivot = (GUIBase_Pivot)element;
			pivot.Show(state);
		}
		else
		{
			GameObject gameObject = element.gameObject;

			for (int idx = 0; idx < m_ObjectsToChangeVisibility.Count; ++idx)
			{
				if (m_ObjectsToChangeVisibility[idx].m_GObj == gameObject &&
					m_ObjectsToChangeVisibility[idx].m_Visible == state &&
					m_ObjectsToChangeVisibility[idx].m_Recursive == recursive)
				{
					m_ObjectsToChangeVisibility.RemoveAt(idx);
					break;
				}
			}

			S_ObjectToChangeVisibility obj = new S_ObjectToChangeVisibility(gameObject, state, recursive);

			m_ObjectsToChangeVisibility.Add(obj);
		}
	}

	//---------------------------------------------------------	
	void ShowLayout(string name, bool show)
	{
		foreach (var layout in m_Layouts)
		{
			if (layout != null && layout.name == name)
			{
				layout.Show(show, true);
			}
		}
	}

	// we need to keep this for back-compatibility
	// remove it later
	public void ShowWidget(GUIBase_Widget widget, bool show, bool recursive)
	{
		if (widget != null)
		{
			widget.Show(show, recursive);
		}
	}

	// we need to keep this for back-compatibility
	// remove it later
	public void ShowLayout(GUIBase_Layout layout, bool show)
	{
		if (layout != null)
		{
			layout.Show(show);
		}
	}

	// we need to keep this for back-compatibility
	// remove it later
	public void ShowPivot(GUIBase_Pivot pivot, bool show)
	{
		if (pivot)
		{
			pivot.Show(show);
		}
	}

	//---------------------------------------------------------	
	public GUIBase_Pivot GetPivot(string name)
	{
		GameObject gObj = GameObject.Find(name);

		if (gObj)
		{
			GUIBase_Pivot pivot = gObj.GetComponent<GUIBase_Pivot>();

			if (pivot == null)
			{
				Debug.LogError("Can't find PIVOT '" + name + "'. There is object with that name in the scene, but it is not GUIBase_Pivot.");
			}

			return pivot;
		}
		else
		{
			Debug.LogError("Can't find PIVOT '" + name + "'");
		}

		return null;
	}

	//---------------------------------------------------------	
	public void PlayOneShot(AudioClip clip)
	{
		if (m_AudioSource == null)
		{
			m_AudioSource = GetComponent<AudioSource>();
			if (m_AudioSource == null)
			{
				m_AudioSource = gameObject.AddComponent<AudioSource>();
			}
		}
		m_AudioSource.PlayOneShot(clip, AudioListener.volume); //Added volume, otherwise in webplayer it was always playing on max
	}

	ulong CalcRendererKey(int queueIdx, int layoutId, Material mat)
	{
		MFDebugUtils.Assert(layoutId < 65536 && queueIdx < 65535);

		ulong res = ((ulong)mat.GetInstanceID() << 32) + ((ulong)layoutId << 16) + (ulong)queueIdx;

		return res;
	}

	public static void OnLanguageChanged(string inNewLanguage)
	{
		MFFontManager.Prepare(inNewLanguage);

		// reinitialize all labels...
		GUIBase_Label[] labels = Object.FindObjectsOfType(typeof (GUIBase_Label)) as GUIBase_Label[];
		foreach (GUIBase_Label label in labels)
		{
			label.OnLanguageChanged(inNewLanguage);
		}

		// reinitialize all text areas...
		GUIBase_TextArea[] textAreas = Object.FindObjectsOfType(typeof (GUIBase_TextArea)) as GUIBase_TextArea[];
		foreach (GUIBase_TextArea text in textAreas)
		{
			text.OnLanguageChanged(inNewLanguage);
		}
	}
}
