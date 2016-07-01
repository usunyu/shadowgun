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

//[ExecuteInEditMode]
public class Subtitles : MonoBehaviour
{
	public Color m_TextColor;
	public Font myFont;
	public GUIStyle cStyle; // = new GUIStyle(GUI.skin.box); //Copy the Default style for buttons;
	GUIStyle myStyle; // = new GUIStyle(GUI.skin.box); //Copy the Default style for buttons;
	GUIContent myContent;
	float lastChangeTime;

	Vector2 scale = new Vector2(2, 2);
	Vector2 pivotPoint;

	void Awake()
	{
	}

	// Use this for initialization
	void Start()
	{
		InvokeRepeating("RegenerateText", 2, 2);
		RegenerateText();
	}

	// Update is called once per frame
	void Update()
	{
	}

	void OnGUI()
	{
		if (myStyle == null)
		{
			//myStyle = cStyle;

			myStyle = new GUIStyle(GUI.skin.box); //Copy the Default style for buttons
			myStyle.font = myFont;
			myStyle.normal.textColor = m_TextColor;

			//        myStyle.fontSize = 30; //Change the Font size
			myStyle.wordWrap = true;
			myStyle.alignment = TextAnchor.UpperCenter;

			RegenerateText();
		}

		//GUI.Box (new Rect (0,Screen.height - 50,100,50), "Bottom-left");
		//GUI.Box (new Rect (Screen.width - 100,Screen.height - 50,100,50), "Bottom-right");

		// size of box in percentage...
		Vector2 boxSize = new Vector2(40, 10)/100.0f;
		// origin of box in percentages from screen...
		Vector2 boxPos = new Vector2(50, 80)/100.0f;
		// size of box in percentage from boxSize...
		Vector2 boxPivot = new Vector2(50, 50)/100.0f;

		float left = Screen.width*(boxPos.x - boxSize.x*boxPivot.x);
		float top = Screen.height*(boxPos.y - boxSize.y*boxPivot.y);

		float width = Screen.width*boxSize.x;
		float height = Screen.height*boxSize.y;

//        myStyle = new GUIStyle(GUI.skin.box); //Copy the Default style for buttons;
		//GUI.Box (new Rect (0,Screen.height - 50,100,50), "Bottom-left");
		//GUI.Box (new Rect (Screen.width - 100,Screen.height - 50,100,50), "Bottom-right");
/*
        string utf8 = "よばれる　　　　－　　　 　呼ばれる　　りゅうは、ごく　－　　　　 理由は、ごく　　ふつうの　　　　－　　　　普通の　　ひとがごく　　　－　　　　人がごく　　かんたんに　　　－" +
                      "簡単に　　しよう　　　　　－　　　　使用　　ほうほうを　　　－　　　　方法を　　まいにちの　　　－　　　　毎日の　　しごとにすぐ　　－" +
                      "仕事にすぐ　　やくだてることができることからきている。－　　役立てることができることからきている。 ";
        GUIContent content = new GUIContent(utf8);
*/
		//myContent = new GUIContent("\u547C\n");

		if (Time.realtimeSinceStartup > lastChangeTime + 2.0f)
		{
			//RegenerateText();
			lastChangeTime = Time.realtimeSinceStartup;
			//Debug.Log(" - RegenerateText - ");
		}

//        GUI.Box (new Rect (left, top, width, height), "Subtitles...\n New line", myStyle);

		/*
        // Make a group on the center of the screen
        GUI.BeginGroup (Rect (Screen.width / 2 - 50, Screen.height / 2 - 50, 100, 100));
        // All rectangles are now adjusted to the group. (0,0) is the topleft corner of the group.

        // We'll make a box so you can see where the group is on-screen.
        GUI.Box (Rect (0,0,100,100), "Group is here");
        GUI.Button (Rect (10,40,80,30), "Click me");

        // End the group we started above. This is very important to remember!
        GUI.EndGroup ();
        */

		if (GUI.Button(new Rect(Screen.width/2 - 25, Screen.height/2 - 25, 50, 50), "Big!"))
		{
			scale += new Vector2(0.5f, 0.5f);
		}

		Vector2 pivotPoint = new Vector2(Screen.width*boxPos.x, Screen.height*boxPos.y);
		GUIUtility.ScaleAroundPivot(scale, pivotPoint);
		GUI.Box(new Rect(left, top, width, height), myContent, myStyle);
	}

	void RegenerateText()
	{
		//Debug.Log ("RegenerateText");
		int text_size = Random.Range(25, 40);
		char[] text = new char[text_size + 1];

		for (int i = 0; i < text_size; i++)
		{
			text[i] = (char)Random.Range(0x3041, 0x309E);
		}

		text[text_size] = '\0';
		myContent = new GUIContent(new string(text));
	}
}
