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

[AddComponentMenu("GUI/Game/GuiSubtitles")]
[RequireComponent(typeof (AudioSource))]
public class GuiSubtitles : MonoBehaviour
{
/*
	[System.Serializable]	
	public class SubtitleLine
	{
		public GUIBase_Layout 	Line;
		public float			Time; //in msec
	}
*/
	[System.Serializable]
	public class SubtitleLineEx
	{
		public int TextID;
		public float Time; //in msec
	}

	//--------------------

	//public GUIBase_Layout	Background;
	public AudioClip Voice;
	//public SubtitleLine[] 	Sequence = new SubtitleLine[0];
	public SubtitleLineEx[] SequenceEx = new SubtitleLineEx[0];
	public bool ForceWalkOnPlayer = false;
	public bool ForceShow = false; //change to public if we need subtitles that shows when subtiitles are disabled in options
	//public bool 			Once = true;

	//-----------------private
//    private AudioSource 	Audio;
//	private bool 			mRunning = false;
//	private int 			mCurrentLine = -1;
//	private int 			mNumRuns = 0;  //kolikrat byl titulkovac spusten (prehral sekvenci)
//	public bool Running{get{return mRunning; }}

//	static ArrayList Instancies  = new ArrayList();

//    public  Color           m_TextColor;
//    public  Font            m_SubtitleFont;
//    private GUIStyle        m_SubtitleStyle;
//    private GUIContent      m_CurrentContent;
//    private Rect            m_TargetRectangle;

	public bool hasAnyText
	{
		get { return (SequenceEx.Length > 0); }
	}

	//-------------- manager
	/*
	static void RegisterSubtitlesInstance(GuiSubtitles s)
	{
		Instancies.Add(s);
	}
	
	static void UnregisterSubtitlesInstance(GuiSubtitles s)
	{
		Instancies.Remove(s);
	}
	*/

	public static void DeactivateAllRuning()
	{
		GuiSubtitlesRenderer.Deactivate();
/*
		foreach(GuiSubtitles s in Instancies)
		{
			s.Deactivate();
		}
*/
	}

	public static void ShowAllRunning(bool show)
	{
		GuiSubtitlesRenderer.ShowAllRunning(show);

//		foreach(GuiSubtitles s in Instancies)
//			s.ShowRunning(show);
	}

	/*
	void Awake()
	{
        Audio = audio;
		Audio.playOnAwake = false;
		
		RegisterSubtitlesInstance(this);
	}

	void OnDestroy() 
	{
		UnregisterSubtitlesInstance(this);
	}	
	*/

	void Start()
	{
		InitializeEvents();
		enabled = false;
	}

	void InitializeEvents()
	{
	}

	/*void ResetEvents()
    {
        foreach (GameEvent gameEvent in GameEvents)
            GameBlackboard.Instance.GameEvents.RemoveEventChangeHandler(gameEvent.Name, EventHandler);
    }*/

	void Activate()
	{
		GuiSubtitlesRenderer.ShowSubtitles(this);
		/*
		//Debug.Log("Activate - RunSubtitlesSequence");
		if(!mRunning)
		{
            if(SequenceEx.Length > 0)
            {
                StartCoroutine("RunSubtitlesSequenceEx");
            }
            else
            {
                StartCoroutine("RunSubtitlesSequence");
            }
		}
        */
	}

	/*
    void Deactivate()
	{
		//Debug.Log("Dectivate - RunSubtitlesSequence: " + name + ", running:  ", mRunning);
		if(mRunning)
		{
			StopAllCoroutines();
			
			ShowRunning(false);
			
			//stop voice sound
			if(Audio && Audio.clip)
			{
				//Debug.Log("Stopping audio");
				Audio.Stop();
			}
			
			mCurrentLine = -1;
			mRunning = false;
			OnSequenceEnd();
		}
	}
	*/

	/*
	void ShowRunning(bool show)
	{
		if(mRunning)
		{
			//TODO: tady by to chtelo asi okamzite skryti misto prehrati out animaci.
			//hide background
			if(Background)
				MFGuiManager.Instance.ShowLayout(Background, show);	
				
			//hide layout
			//if(mCurrentLine != -1)
			//	MFGuiManager.Instance.ShowLayout(Sequence[mCurrentLine].Line, show);
            //m_CurrentContent = null;
		}
	}
	*/

	/*
	IEnumerator RunSubtitlesSequence()
    {		
		//if(Once && mNumRuns > 0)
		//	yield break;
			
		mRunning = true;
		mNumRuns++;
		mCurrentLine = -1;
		
		OnSequenceBegin();
		
		//zobraz spolecne gui pro celou sekvenci a pockej dokud neni cele zobrazeno
		if(Background && (GuiOptions.subtitles || ForceShow))
		{
            yield return StartCoroutine(  HideBackGround()  );
        }
		
		//spusti voiceover pro tuto sekvenci titulku
		if(Voice)
		{
			Audio.clip = Voice;
			Audio.Play();
		}		
		
		//postupne zobraz jednotlive radky
		if(GuiOptions.subtitles  || ForceShow)
		{
			foreach(SubtitleLine l in Sequence)
			{
				mCurrentLine++;
				//skip empty lines
				if(!l.Line)
					continue;
				
				//show line
				MFGuiManager.Instance.ShowLayout(l.Line, true);
				
				//wait with next line
				yield return new WaitForSeconds(l.Time);
				
				//hide layout
				MFGuiManager.Instance.ShowLayout(l.Line, false);
				
				//wait till layout anim ends
				if(l.Line)
				{
					yield return new WaitForSeconds(0.1F);			
					while(!l.Line.HideDone)
					{
						yield return new WaitForSeconds(0.1F);
					}
				}
			}
		}

		//skryj bacground
		if(Background)
		{
            yield return StartCoroutine(  HideBackGround()  );
        }
		
		//pockej dokud neskonci i audio
		while(Audio.isPlaying)
		{
			yield return new WaitForSeconds(0.2f);	
		}
		
		mRunning = false;
		OnSequenceEnd();
	}

    IEnumerator RunSubtitlesSequenceEx()
    {
        mRunning = true;
        mNumRuns++;
        mCurrentLine = -1;

        OnSequenceBegin();

        //zobraz spolecne gui pro celou sekvenci a pockej dokud neni cele zobrazeno
        if(Background && (GuiOptions.subtitles || ForceShow))
        {
            yield return StartCoroutine(  ShowBackGround()  );
        }

        //spusti voiceover pro tuto sekvenci titulku
        if(Voice)
        {
            Audio.clip = Voice;
            Audio.Play();
        }

        //postupne zobraz jednotlive radky
        if(GuiOptions.subtitles  || ForceShow)
        {
            foreach(SubtitleLineEx l in SequenceEx)
            {
                mCurrentLine++;

                //show line
                //m_CurrentContent = new GUIContent(TextDatabase.instance[l.TextID]);

                //wait with next line
                yield return new WaitForSeconds(l.Time);

                //hide layout
                //m_CurrentContent = null;

                // small wait
                yield return new WaitForSeconds(0.3F);
            }
        }

        //skryj bacground
        if(Background)
        {
            yield return StartCoroutine(  HideBackGround()  );
        }
        
        //pockej dokud neskonci i audio
        while(Audio.isPlaying)
        {
            yield return new WaitForSeconds(0.2f);  
        }
        
        mRunning = false;
        OnSequenceEnd();
    }
	*/

/*
    void OnGUI ()
    {
        //Debug.Log("GuiSubtitles.OnGUI " + name );

        if(m_SubtitleStyle == null)
        {
            SetupStyle();
            m_TargetRectangle = ComputeTargetRectange();
        }

        if(m_CurrentContent != null)
        {
            //Rect textRectangle = ComputeTargetRectange();

            //Vector2 pivotPoint = new Vector2(Screen.width*boxPos.x,Screen.height*boxPos.y);
            //GUIUtility.ScaleAroundPivot (scale, pivotPoint);
            Rect renderRect = m_TargetRectangle;
            GUI.Box     (renderRect, m_CurrentContent,  m_SubtitleStyle);
            //GUI.Label   (textRectangle, m_CurrentContent,  m_SubtitleStyle);
            //GUI.TextArea(textRectangle, m_CurrentContent,  m_SubtitleStyle);
        }
    }
     */

	/*
    private Rect ComputeTargetRectange()
    {
        if(Background != null)
        {
            GUIBase_Widget backgroundImg = Background.GetWidget("Subtitle_Sprite");
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
	/*
	void OnSequenceBegin()
	{
		enabled = true;
		if(ForceWalkOnPlayer)
		{
			//Debug.Log("Zpomaleni pohybu playera zapnuto");
            Player.Instance.Owner.BlackBoard.ComlinkOn = true;
            Player.Instance.Owner.BlackBoard.Desires.WeaponTriggerOn = false;
        	GuiHUD.Instance.HideWeaponControls();			
		}
	}
	
	void OnSequenceEnd()
	{
		enabled = false;
		if(ForceWalkOnPlayer)
		{
			//Debug.Log("zpomaleni pohybu playera vypnuto");
            Player.Instance.Owner.BlackBoard.ComlinkOn = false;
        	GuiHUD.Instance.ShowWeaponControls();			
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

    internal IEnumerator ShowBackGround()
    {
        MFGuiManager.Instance.ShowLayout(Background, true);

        //Debug.Log("Waiting for Bacgroud ");
        //wait till pivot anim ends
        {
            yield return new WaitForSeconds(0.1F);
            while(!Background.ShowDone)
            {
                yield return new WaitForSeconds(0.1F);
            }
        }
        //Debug.Log("Bacgroud shown");
    }

    internal IEnumerator HideBackGround()
    {
        MFGuiManager.Instance.ShowLayout(Background, false);
        //wait till background anim ends
        {
            yield return new WaitForSeconds(0.1F);
            while(!Background.HideDone)
            {
                yield return new WaitForSeconds(0.1F);
            }
            //Debug.Log("Bacgroud hide done");
        }
    }
    */
}
