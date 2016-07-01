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

public interface IViewOwner
{
	void DoCommand(string inCommand);

	void ShowScreen(string inScreenName, bool inClearStack = false); // TODO I don't like strings...
	GuiPopup ShowPopup(string inPopupName, string inCaption, string inText, PopupHandler inHandler = null);
	void Back();

	void Exit();

	bool IsTopView(GuiView inView);
	bool IsAnyScreenVisible();
	bool IsAnyPopupVisible();
}

public abstract class GuiView : MonoBehaviour
{
	// GETTERS / SETTERS

	public bool IsInitialized { get; private set; }
	public bool IsVisible { get; private set; }
	public bool IsEnabled { get; private set; }
	public IViewOwner Owner { get; private set; }

	// ABSTRACT INTERFACE

	public abstract GUIBase_Layout Layout { get; }

	protected virtual void OnViewInit()
	{
	}

	protected virtual void OnViewShow()
	{
	}

	protected virtual void OnViewHide()
	{
	}

	protected virtual void OnViewUpdate()
	{
	}

	protected virtual void OnViewEnable()
	{
	}

	protected virtual void OnViewDisable()
	{
	}

	protected virtual GUIBase_Widget OnViewHitTest(ref Vector2 point)
	{
		return null;
	}

	protected virtual bool OnViewProcessInput(ref IInputEvent evt)
	{
		return false;
	}

	protected virtual void OnViewReset()
	{
	}

	protected virtual void OnViewDestroy()
	{
	}

	// PUBLIC METHODS

	public void InitView()
	{
		if (IsInitialized == true)
			return;

		OnViewInit();

		IsInitialized = true;
	}

	public void ShowView(IViewOwner owner)
	{
		if (IsInitialized == false)
			return;
		if (Owner != null)
			return;

		if (IsVisible == true)
			return;
		IsVisible = true;

		Owner = owner;

		if (Layout != null)
		{
			Layout.Show(true);
		}

		OnViewShow();
	}

	public void HideView(IViewOwner owner)
	{
		if (IsInitialized == false)
			return;
		if (Owner != owner)
			return;

		if (IsVisible == false)
			return;
		IsVisible = false;

		OnViewHide();

		if (Layout != null)
		{
			Layout.Show(false);
		}

		Owner = null;
	}

	public void UpdateView()
	{
		if (IsInitialized == false)
			return;
		if (IsVisible == false)
			return;

		OnViewUpdate();
	}

	public void EnableView()
	{
		if (IsInitialized == false)
			return;

		if (IsEnabled == true)
			return;
		IsEnabled = true;

		if (Layout != null)
		{
			Layout.InputEnabled = true;
		}

		OnViewEnable();
	}

	public void DisableView()
	{
		if (IsInitialized == false)
			return;

		if (IsEnabled == false)
			return;
		IsEnabled = false;

		if (Layout != null)
		{
			Layout.InputEnabled = false;
		}

		OnViewDisable();
	}

	public GUIBase_Widget HitTest(ref Vector2 point)
	{
		if (IsInitialized == false)
			return null;
		if (IsVisible == false)
			return null;

		return OnViewHitTest(ref point);
	}

	public bool ProcessInput(ref IInputEvent evt)
	{
		if (IsInitialized == false)
			return false;
		if (IsVisible == false)
			return false;
		if (IsEnabled == false)
			return false;

		return OnViewProcessInput(ref evt);
	}

	public void ResetView()
	{
		if (IsInitialized == false)
			return;

		OnViewReset();
	}

	public void DestroyView()
	{
		if (IsInitialized == false)
			return;

		OnViewDestroy();

		IsInitialized = false;
	}

	// MONOBEHAVIOUR INTERFACE

	protected void OnDestroy()
	{
		DestroyView();
	}

	// OBJECT INTERFACE

	public override bool Equals(object other)
	{
		GuiView view = other as GuiView;
		return view != null ? Layout == view.Layout : false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	// -----------------------------------------------------------------------------------------------------------------	
	// dbg/tmp Keyboard support...
	public enum E_KeyBoardMode
	{
		Default,
		Email,
		Password,
	};
#if UNITY_EDITOR || UNITY_IPHONE || UNITY_ANDROID
	GUIBase_Button m_ActiveInput;
	//private TouchScreenKeyboard 	m_Keyboard;
	public bool isInputActive
	{
		get { return m_ActiveInput != null; }
	}
#else
	public bool 					isInputActive { get { return false; } }
#endif

	public delegate void KeyboardClose(GUIBase_Button inInput, string inText, bool inInputCanceled);

	public bool ShowKeyboard(GUIBase_Button inInput,
							 E_KeyBoardMode inMode,
							 KeyboardClose inCloseDelegate,
							 string inText,
							 int inMaxTextLength = -1)
	{
		return ShowKeyboard(inInput, inMode, inCloseDelegate, inText, string.Empty, inMaxTextLength);
	}

	public bool ShowKeyboard(GUIBase_Button inInput,
							 E_KeyBoardMode inMode,
							 KeyboardClose inCloseDelegate,
							 string inText,
							 string inPlaceholder,
							 int inMaxTextLength = -1)
	{
#if UNITY_EDITOR
		m_ActiveInput = null;
		Debug.LogError("ShowKeyboard not implemented !!!");
		return false;
#elif UNITY_IPHONE || UNITY_ANDROID
		return ShowTouchScreenKeyboard(inInput, inMode, inCloseDelegate, inText, inPlaceholder, inMaxTextLength);
#else
		Debug.LogError("ShowKeyboard not implemented !!!");
		return false;
#endif
	}

#if UNITY_IPHONE || UNITY_ANDROID
	bool ShowTouchScreenKeyboard(GUIBase_Button inInput,
								 E_KeyBoardMode inMode,
								 KeyboardClose inCloseDelegate,
								 string inText,
								 string inPlaceholder,
								 int inMaxTextLength)
	{
		if (m_ActiveInput != null || inInput == null || inCloseDelegate == null)
			return false;

		/// setup arguments for Touch keyboard...	
		TouchScreenKeyboardType keyboardType = inMode == E_KeyBoardMode.Email
															   ? TouchScreenKeyboardType.EmailAddress
															   : inMode == E_KeyBoardMode.Password
																				 ? TouchScreenKeyboardType.Default
																				 : TouchScreenKeyboardType.ASCIICapable;

		bool autocorrection = false; //inMode != E_KeyBoardMode.Password;
		bool secure = inMode == E_KeyBoardMode.Password;

		// open keyboard...
		TouchScreenKeyboard keyboard = TouchScreenKeyboard.Open(inText, keyboardType, autocorrection, false, secure, false, inPlaceholder);
		if (keyboard == null)
			return false;

		m_ActiveInput = inInput;
		StartCoroutine(ProcessKeyboardInput(keyboard, inCloseDelegate, inText, inMaxTextLength));
		return true;
	}

	IEnumerator ProcessKeyboardInput(TouchScreenKeyboard inKeyboard, KeyboardClose inCloseDelegate, string inText, int inMaxTextLength)
	{
		//Debug.Log("ProcessKeyboardInput - Mark 1");
		bool canceled = false;

		while (!inKeyboard.done)
		{
			if (inKeyboard.active == false)
			{
				canceled = true;
				break;
			}

			if (inMaxTextLength > 0 && inKeyboard.text.Length > inMaxTextLength)
			{
				inKeyboard.text = inKeyboard.text.Substring(0, inMaxTextLength);
			}

			//Debug.Log("ProcessKeyboardInput - Mark 2");
			yield return new WaitForEndOfFrame();
		}

		string keyboardText = inKeyboard.text;
		//Debug.Log("ProcessKeyboardInput - Mark 4 " + keyboardText);

		if (canceled == true || inKeyboard.wasCanceled == true)
		{
			//Debug.Log("ProcessKeyboardInput - Mark 5 ");
			canceled = true;
			keyboardText = inText;
		}

		//Debug.Log("ProcessKeyboardInput - Mark 6 ");
		try
		{
			inCloseDelegate(m_ActiveInput, keyboardText, canceled);
		}
		catch (System.Exception e)
		{
			Debug.LogError("!!!! Exception during call inCloseDelegate: \n" + e.ToString());
		}

		m_ActiveInput = null;
		//m_Keyboard    = null;
		//Debug.Log("ProcessKeyboardInput - Mark 7 ");
	}
#endif
}
