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

[AddComponentMenu("")]
[RequireComponent(typeof (AudioSource))]
public class GuiSubtitlesRenderer : MonoBehaviour
{
	public GUIBase_Layout m_Background;
	GUIBase_Label m_Label;
	//public  Color           m_TextColor;
	//public  Font            m_SubtitleFont;

	//private GUIStyle        m_SubtitleStyle;
	//private GUIContent      m_CurrentContent;
	//private Rect            m_TargetRectangle;

	AudioSource m_Audio;
	GuiSubtitles m_CurrentSubtitles;
	bool wasAudioPlaying = false;

	public static GuiSubtitlesRenderer Instance;

	void Awake()
	{
		useGUILayout = false;

		m_Audio = GetComponent<AudioSource>();
		m_Audio.playOnAwake = false;

		Instance = this;
	}

	void Start()
	{
		//Debug.Log(Time.realtimeSinceStartup + " Start");
		enabled = false;
		m_Label = m_Background.GetComponentInChildren<GUIBase_Label>();
		wasAudioPlaying = false;
	}

	public static void Deactivate()
	{
		if (Instance != null)
		{
			Instance.DeactivateInternal();
		}
		else
		{
			// log this situation...
		}
	}

	public static void ShowSubtitles(GuiSubtitles inSubtitles)
	{
		if (Instance != null)
		{
			Instance.ShowSubtitlesInternal(inSubtitles);
		}
		else
		{
			// log this situation...
		}
	}

	public static void ShowAllRunning(bool show)
	{
		if (Instance != null)
		{
			Instance.ShowAllRunningInternal(show);
		}
		else
		{
			// log this situation...
		}
	}

	void DeactivateInternal()
	{
		//Debug.Log(Time.realtimeSinceStartup + " DeactivateInternal");
		if (enabled == false)
			return;

		StopAllCoroutines();

		if (m_Background != null)
			MFGuiManager.Instance.ShowLayout(m_Background, false);

		//stop voice sound
		if (m_Audio && m_Audio.clip)
		{
			//Debug.Log("Stopping audio");
			m_Audio.Stop();
		}

		OnSequenceEnd();
	}

	internal void ShowSubtitlesInternal(GuiSubtitles inSubtitles)
	{
		//Debug.Log(Time.realtimeSinceStartup + " ShowSubtitlesInternal");
		if (m_CurrentSubtitles != null)
		{
			Deactivate();
		}

		m_CurrentSubtitles = inSubtitles;

		if (m_CurrentSubtitles != null)
		{
			enabled = true;

			if (m_Label)
			{
				m_Label.Clear();
			}

			StartCoroutine("RunSubtitlesSequence");
		}
	}

	internal void ShowAllRunningInternal(bool show)
	{
		//Debug.Log(Time.realtimeSinceStartup + " ShowAllRunningInternal");

		if (show == false || CanShowBackground() == true)
		{
			MFGuiManager.Instance.ShowLayout(m_Background, show);
			enabled = show;
		}
		else if (m_CurrentSubtitles != null)
		{
			enabled = show;
		}
	}

	IEnumerator RunSubtitlesSequence()
	{
		OnSequenceBegin();

		//zobraz spolecne gui pro celou sekvenci a pockej dokud neni cele zobrazeno
		if (CanShowBackground() == true)
		{
			yield return StartCoroutine(ShowBackGround());
		}

		//spusti voiceover pro tuto sekvenci titulku
		if (m_CurrentSubtitles.Voice)
		{
			m_Audio.clip = m_CurrentSubtitles.Voice;
			m_Audio.Play();
		}

		//postupne zobraz jednotlive radky
		if (GuiOptions.subtitles || m_CurrentSubtitles.ForceShow)
		{
			foreach (GuiSubtitles.SubtitleLineEx l in m_CurrentSubtitles.SequenceEx)
			{
				//show line
				//m_CurrentContent = new GUIContent(TextDatabase.instance[l.TextID]);
				m_Label.SetNewText(l.TextID);

				//wait with next line
				yield return new WaitForSeconds(l.Time);

				//hide layout
				//m_CurrentContent = null;
				m_Label.Clear();

				// small wait
				yield return new WaitForSeconds(0.3F);
			}
		}

		//skryj bacground
		if (m_Background)
		{
			yield return StartCoroutine(HideBackGround());
		}

		//pockej dokud neskonci i audio
		while (m_Audio.isPlaying)
		{
			yield return new WaitForSeconds(0.2f);
		}

		OnSequenceEnd();
	}

	void OnSequenceBegin()
	{
		//enabled            = true;
		if (m_CurrentSubtitles.ForceWalkOnPlayer)
		{
			//Debug.Log("Zpomaleni pohybu playera zapnuto");
			Player.LocalInstance.Owner.BlackBoard.Desires.WeaponTriggerOn = false;
			Player.LocalInstance.Owner.BlackBoard.Desires.WeaponTriggerUp = false;
			Player.LocalInstance.Owner.BlackBoard.Desires.WeaponTriggerUpDisabled = true;
			Player.LocalInstance.Owner.BlackBoard.Desires.MeleeTriggerOn = false;
			GuiHUD.Instance.HideWeaponControls();
		}
	}

	void OnSequenceEnd()
	{
		if (m_CurrentSubtitles.ForceWalkOnPlayer)
		{
			//Debug.Log("zpomaleni pohybu playera vypnuto");
			GuiHUD.Instance.ShowWeaponControls();
		}

		//pokud neni jeste background skryty, shovej ho alespon ted na konec
		if (m_Background && m_Background.IsVisible())
			MFGuiManager.Instance.ShowLayout(m_Background, false);

		//m_CurrentContent   = null;
		m_CurrentSubtitles = null;
		enabled = false;
		wasAudioPlaying = false;
	}

	internal bool CanShowBackground()
	{
		return (m_Background && m_CurrentSubtitles && m_CurrentSubtitles.hasAnyText && (GuiOptions.subtitles || m_CurrentSubtitles.ForceShow));
	}

	internal IEnumerator ShowBackGround()
	{
		MFGuiManager.Instance.ShowLayout(m_Background, true);

		//Debug.Log("Waiting for Bacgroud ");
		//wait till pivot anim ends
		{
			yield return new WaitForSeconds(0.1F);
			while (!m_Background.ShowDone)
			{
				yield return new WaitForSeconds(0.1F);
			}
		}
		//Debug.Log("Bacgroud shown");
	}

	internal IEnumerator HideBackGround()
	{
		MFGuiManager.Instance.ShowLayout(m_Background, false);
		//wait till background anim ends
		{
			yield return new WaitForSeconds(0.1F);
			while (!m_Background.HideDone)
			{
				yield return new WaitForSeconds(0.1F);
			}
			//Debug.Log("Bacgroud hide done");
		}
	}

	/*
    void OnGUI ()
    {
        //Debug.Log("GuiSubtitlesRenderer.OnGUI");
        if(m_SubtitleStyle == null)
        {
            SetupStyle();
            m_TargetRectangle = ComputeTargetRectangle();
        }

        if(m_CurrentContent != null && m_SubtitleStyle != null)
        {
            GUI.Box(m_TargetRectangle, m_CurrentContent, m_SubtitleStyle);
        }
    }
	*/
	/*
    internal void SetupStyle()
    {
        float coef = Mathf.Min( Screen.height, Screen.width) / 480.0f;

        //Debug.Log("SetupStyle");
        m_SubtitleStyle = new GUIStyle(GUI.skin.box); //Copy the Default style for buttons
        //m_SubtitleStyle = new GUIStyle(GUI.skin.label);
        m_SubtitleStyle.font = m_SubtitleFont;
        m_SubtitleStyle.fontStyle = FontStyle.Normal;
        m_SubtitleStyle.fontSize = (int) (18.0f*coef);
        m_SubtitleStyle.normal.background = null;
        m_SubtitleStyle.normal.textColor = m_TextColor;
        //m_SubtitleStyle.wordWrap  = true;
        m_SubtitleStyle.alignment = TextAnchor.MiddleCenter;
        //m_SubtitleStyle.alignment = TextAnchor.UpperCenter;
    }
	 */
	/*
    private Rect ComputeTargetRectangle()
    {
        if(m_Background != null)
        {
            GUIBase_Widget backgroundImg = m_Background.GetWidget("Subtitle_Sprite");
            if(backgroundImg != null)
            {
                return backgroundImg.GetRectInScreenCoords();
            }
        }

        float orig = 3.0f / 2.0f;
        float coef = ((float)Screen.width) / ((float)Screen.height);
        coef = orig/coef;

        // size of box in percentage...
        Vector2 boxSize  = new Vector2(50*coef, 14)/100.0f;
        // origin of box in percentages from screen...
        Vector2 boxPos   = new Vector2(50, 10)/100.0f;
        //Vector2 boxPos   = new Vector2(50, 10)/100.0f;
        // size of box in percentage from boxSize...
        Vector2 boxPivot = new Vector2(50, 50)/100.0f;

        float screenW    = Screen.width;  // 948;
        float screenH    = Screen.height; // 472;
        float left       = screenW * (boxPos.x - boxSize.x*boxPivot.x);
        float top        = screenH * (boxPos.y - boxSize.y*boxPivot.y);
        float width      = screenW * boxSize.x;
        float height     = screenH * boxSize.y;

        return new Rect (left, top, width, height);
    }
	*/

	public static void Suspend(bool suspend)
	{
		if (Instance != null)
			Instance.SuspendInternal(suspend);
	}

	void SuspendInternal(bool suspend)
	{
		if (m_Audio && m_Audio.clip)
		{
			if (suspend)
			{
				if (m_Audio.isPlaying)
				{
					wasAudioPlaying = true;
					m_Audio.Pause();
				}
			}
			else
			{
				if (wasAudioPlaying)
				{
					wasAudioPlaying = false;
					m_Audio.Play();
				}
			}
		}
	}
}
