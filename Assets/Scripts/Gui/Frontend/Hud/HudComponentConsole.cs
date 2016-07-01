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

//Trida pro vypisovani hlasek clientovi pri multiplayeru.
// Sklada se z nekolika radku (maxLines), novy message se vzdy zobrazi nahore a strsi posune dolu. Message se po uplynuti casu 'defTimeout' skryje. 
public class HudComponentConsole : HudComponent
{
	public static HudComponentConsole Instance;

	const float defTimeout = 5.0f;
	const int maxLines = 3;

	GUIBase_Pivot m_Pivot;
	GUIBase_Layout m_Layout;

	class LineInfo
	{
		public GUIBase_Label label = null;
		public string strMsg = "";
		public float timeout = -1;
		public Color color = Color.white;
	};

	LineInfo[] lines = new LineInfo[maxLines];

	protected override bool OnInit()
	{
		if (base.OnInit() == false)
			return false;

		string s_PivotName = "Console";
		m_Pivot = MFGuiManager.Instance.GetPivot(s_PivotName);
		if (!m_Pivot)
		{
			Debug.LogError("'" + s_PivotName + "' not found!");
			return false;
		}

		string s_Layout = "Console_Layout";
		m_Layout = m_Pivot.GetLayout(s_Layout);
		if (!m_Layout)
		{
			Debug.LogError("'" + s_Layout + "' not found!");
			return false;
		}

		for (int idx = 0; idx < maxLines; ++idx)
		{
			lines[idx] = new LineInfo();
			lines[idx].label = GuiBaseUtils.PrepareLabel(m_Layout, "Console_Line" + idx);
		}

		Instance = this;

		return true;
	}

	protected override void OnDestroy()
	{
		Instance = null;

		base.OnDestroy();
	}

	protected override void OnShow()
	{
		base.OnShow();

		MFGuiManager.Instance.ShowPivot(m_Pivot, true);
	}

	protected override void OnHide()
	{
		MFGuiManager.Instance.ShowPivot(m_Pivot, false);

		base.OnHide();
	}

	protected override void OnLateUpdate()
	{
		base.OnLateUpdate();

		UpdateLines();
	}

	public void ShowMessage(string strMsg, Color color)
	{
		//Debug.Log("ShowMessage " + strMsg);

		if (IsInitialized == false)
			return;

		//zobrazene message posun o pozici dolu
		for (int idx = maxLines - 2; idx >= 0; --idx)
		{
			LineInfo curr = lines[idx];
			LineInfo next = lines[idx + 1];

			next.strMsg = curr.strMsg;
			next.timeout = curr.timeout;
			next.color = curr.color;
		}

		LineInfo line = lines[0];
		line.strMsg = strMsg;
		line.timeout = Time.timeSinceLevelLoad + defTimeout;
		line.color = color;
	}

	public void Clear()
	{
		foreach (var line in lines)
		{
			ClearLine(line);
		}
	}

	void ClearLine(LineInfo line)
	{
		line.label.SetNewText("");
		line.timeout = -1;
		line.strMsg = "";
	}

	void UpdateLine(LineInfo line, float alpha)
	{
		line.label.SetNewText(line.strMsg);
		line.label.Widget.Color = new Color(line.color.r*alpha, line.color.g*alpha, line.color.b*alpha, line.color.a*alpha);
	}

	void UpdateLines()
	{
		for (int idx = 0; idx < maxLines; ++idx)
		{
			LineInfo line = lines[idx];
			if (line.timeout > 0 && Time.timeSinceLevelLoad > line.timeout)
			{
				ClearLine(line);
			}
			else
			{
				UpdateLine(line, 1.0f - idx/(float)(maxLines + 1));
			}
		}
	}
}
