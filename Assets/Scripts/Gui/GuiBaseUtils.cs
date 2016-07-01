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
using DateTime = System.DateTime;
using TimeSpan = System.TimeSpan;
using Regex = System.Text.RegularExpressions.Regex;
using RegexOptions = System.Text.RegularExpressions.RegexOptions;

public static class GuiBaseUtils
{
	public static E_Hint PendingHint = E_Hint.Max;

	public readonly static int NOW = 02040222; //	NOW
	public readonly static int TODAY = 02040223; //	TODAY
	public readonly static int YESTERDAY = 02040224; //	YESTERDAY
	public readonly static int X_Y_AGO = 02040225; //	{0} {1} AGO
	public readonly static int MINUTES = 02040226; //	MINUTES
	public readonly static int HOURS = 02040227; //	HOURS
	public readonly static int DAYS = 02040228; //	DAYS
	public readonly static int WEEKS = 02040229; //	WEEKS
	public readonly static int MONTHS = 02040230; //	MONTHS
	public readonly static int HOUR = 02040241; //	HOUR
	public readonly static int MINUTE = 02040242; //	MINUTE
	public readonly static int TOMORROW = 02040244; //	TOMORROW
	public readonly static int SUNDAY = 02040245; //	SUNDAY

	public static string FixNickname(string nickname, string username, bool filterSwearwords = true)
	{
		if (string.IsNullOrEmpty(nickname) == true)
			return nickname;

		nickname = nickname.CollapseWhitespaces(true) // collapse inner whitespaces and trim leading and trailing whitespaces
						   .RemoveDiacritics() // remove diacritics
						   .AsciiOnly(false); // remove extended ascii chars

		if (filterSwearwords == true)
		{
			nickname = nickname.FilterSwearWords(true); // filter out swear words
		}

		return string.IsNullOrEmpty(nickname) ? username : nickname;
	}

	public static string FixNameForGui(string name)
	{
		if (string.IsNullOrEmpty(name) == true)
			return "";

		// trim leading and trailing whitespaces
		name = name.Trim();

		// normalize to maximize the chance to get the characters contained in our font texture
		name = name.MFNormalize();

		if (name.Length <= CloudUser.MAX_ACCOUNT_NAME_LENGTH)
			return name;
		return string.Format("{0}...", name.Substring(0, CloudUser.MAX_ACCOUNT_NAME_LENGTH - 3));
	}

	public static string TrimLongText(string text, int maxLength, bool removeNewLines = true)
	{
		if (string.IsNullOrEmpty(text) == true)
			return "";

		// trim leading and trailing whitespaces
		text = text.Trim();

		if (removeNewLines == true)
		{
			text = text.Replace("\n", " ");
		}

		if (text.Length <= maxLength)
			return text;
		int idx = text.LastIndexOf(" ", maxLength - 3);
		if (idx >= 10)
			return string.Format("{0}...", text.Substring(0, idx).Trim());
		return string.Format("{0}...", text.Substring(0, maxLength - 3).Trim());
	}

	public static string GetCleanName(string name)
	{
		return Regex.Replace(name, @"[\W_]+", "_").ToLower();
	}

	//---------------------------------------------------------
	public static double DateToEpoch(DateTime date)
	{
		return (date - new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
	}

	//---------------------------------------------------------
	public static DateTime EpochToDate(double epoch)
	{
		return new DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc) + TimeSpan.FromSeconds(epoch);
	}

	//---------------------------------------------------------
	public static string EpochToString(double epoch)
	{
		DateTime date = GuiBaseUtils.EpochToDate(epoch);
		TimeSpan span = CloudDateTime.UtcNow - date;

		int mins = Mathf.RoundToInt((float)span.TotalMinutes);
		int hours = Mathf.RoundToInt((float)span.TotalHours);
		int days = Mathf.RoundToInt((float)(DateTime.Today - date.Date).TotalDays);

		if (mins < 1)
		{
			return TextDatabase.instance[NOW];
		}
		else if (mins == 1)
		{
			return string.Format(TextDatabase.instance[X_Y_AGO], mins, TextDatabase.instance[MINUTE]);
		}
		else if (mins <= 60)
		{
			return string.Format(TextDatabase.instance[X_Y_AGO], mins, TextDatabase.instance[MINUTES]);
		}
		else if (hours == 1)
		{
			return string.Format(TextDatabase.instance[X_Y_AGO], hours, TextDatabase.instance[HOUR]);
		}
		else if (hours <= 6)
		{
			return string.Format(TextDatabase.instance[X_Y_AGO], hours, TextDatabase.instance[HOURS]);
		}
		else if (days == 0)
		{
			return TextDatabase.instance[TODAY];
		}
		else if (days == 1)
		{
			return TextDatabase.instance[YESTERDAY];
		}
		else if (days <= 7)
		{
			return string.Format(TextDatabase.instance[X_Y_AGO], days, TextDatabase.instance[DAYS]);
		}

		return date.ToShortRegionalString();
	}

	//---------------------------------------------------------
	public static string DateToDaysString(DateTime date)
	{
		int days = Mathf.RoundToInt((float)(date.Date - DateTime.Today).TotalDays);

		if (days >= -7 && days < -1)
		{
			return TextDatabase.instance[SUNDAY + (int)date.DayOfWeek];
		}
		else if (days == -1)
		{
			return TextDatabase.instance[YESTERDAY];
		}
		else if (days == 0)
		{
			return TextDatabase.instance[TODAY];
		}
		else if (days == 1)
		{
			return TextDatabase.instance[TOMORROW];
		}
		else if (days > 1 && days <= 7)
		{
			return TextDatabase.instance[SUNDAY + (int)date.DayOfWeek];
		}

		return date.ToShortRegionalString();
	}

	//---------------------------------------------------------
	public static string[] GenerateUsernames(string basename, bool includebase)
	{
		if (string.IsNullOrEmpty(basename) == true)
			return new string[0];

		string pattern = "[ !#$%&'*+-/=?^_`{}|~.]";
		string name = Regex.Replace(basename.RemoveDiacritics().ToLower(), pattern, "");
		DateTime date = CloudDateTime.UtcNow;
		string[,] chars = new string[,]
		{
			{"a", "4"},
			{"e", "3"},
			{"l", "1"},
			{"s", "5"},
			{"t", "7"},
			{"o", "0"}
		};

		List<string> names = new List<string>();
		if (includebase == true)
		{
			AddUsername(name, ref names);
			if (name.Length > 1)
			{
				string hacker = name;

				for (int idx = 0; idx < chars.GetLength(0); ++idx)
				{
					if (name.IndexOf(chars[idx, 0], 1) != -1)
					{
						string temp = name.Replace(chars[idx, 0], chars[idx, 1]);
						AddUsername(name[0] + temp.Substring(1), ref names);
					}
					hacker = hacker.Replace(chars[idx, 0], chars[idx, 1]);
				}

				if (hacker != name)
				{
					AddUsername(name[0] + hacker.Substring(1), ref names);
				}
			}
		}
		AddUsername(name + date.Year, ref names);
		AddUsername(name + date.Hour + date.Day + date.Minute, ref names);
		AddUsername(name + date.Minute + date.Second + date.Day, ref names);
		AddUsername(name + date.Day + date.Second + date.Millisecond, ref names);
		AddUsername(name + Random.Range(1, int.MaxValue), ref names);

		return names.ToArray();
	}

	static void AddUsername(string name, ref List<string> names)
	{
		while (name.Length < CloudUser.MIN_ACCOUNT_NAME_LENGTH)
		{
			name += Random.Range(0, 10).ToString();
		}
		if (names.Contains(name) == false)
		{
			names.Add(name);
		}
	}

	//---------------------------------------------------------
	public static GuiPopupMessageBox ShowMessageBox(int captionID, int textID, PopupHandler handler = null)
	{
		return ShowMessageBox(TextDatabase.instance[captionID], TextDatabase.instance[textID], handler);
	}

	public static GuiPopupMessageBox ShowMessageBox(string caption, string text, PopupHandler handler = null)
	{
		if (GuiFrontendMain.IsVisible == true)
		{
			return GuiFrontendMain.ShowMessageBox(caption, text, handler);
		}
		else if (GuiFrontendIngame.IsVisible == true || GuiFrontendIngame.IsHudVisible == true)
		{
			return GuiFrontendIngame.ShowMessageBox(caption, text, handler);
		}

		return null;
	}

	public static void HideMessageBox(GuiPopupMessageBox popup)
	{
		HidePopup(popup);
	}

	//---------------------------------------------------------
	public static GuiPopupConfirmDialog ShowConfirmDialog(int captionID, int textID, PopupHandler handler = null)
	{
		return ShowConfirmDialog(TextDatabase.instance[captionID], TextDatabase.instance[textID], handler);
	}

	public static GuiPopupConfirmDialog ShowConfirmDialog(string caption, string text, PopupHandler handler = null)
	{
		if (GuiFrontendMain.IsVisible == true)
		{
			return GuiFrontendMain.ShowConfirmDialog(caption, text, handler);
		}
		else if (GuiFrontendIngame.IsVisible == true || GuiFrontendIngame.IsHudVisible == true)
		{
			return GuiFrontendIngame.ShowConfirmDialog(caption, text, handler);
		}

		return null;
	}

	public static void HideConfirmDialog(GuiPopupConfirmDialog popup)
	{
		HidePopup(popup);
	}

	//---------------------------------------------------------
	public static void HidePopup(GuiPopup popup)
	{
		if (popup != null)
		{
			popup.ForceClose();
		}
	}

	//---------------------------------------------------------
	public static T GetControl<T>(GUIBase_Layout layout, string widgetName, bool verbose = true) where T : GUIBase_Callback
	{
		GUIBase_Widget widget = layout != null ? layout.GetWidget(widgetName, verbose) : null;
		if (widget == null)
		{
			if (verbose == true)
			{
				if (layout != null)
				{
					Debug.LogError("Can't find widget '" + widgetName + "' within layout '" + layout.name + "'");
				}
				else
				{
					Debug.LogError("Thers is not any layout specified while trying to find widget '" + widgetName + "'");
				}
			}
			return null;
		}
		return widget.GetComponent<T>();
	}

	//---------------------------------------------------------
	public static void RegisterButtonDelegate(GUIBase_Pivot pivot,
											  string layoutName,
											  string buttonName,
											  GUIBase_Button.TouchDelegate touch,
											  GUIBase_Button.ReleaseDelegate release)
	{
		if (pivot)
		{
			GUIBase_Layout layout = GetLayout(layoutName, pivot);

			if (layout)
			{
				RegisterButtonDelegate(layout, buttonName, touch, release);
			}
			else
			{
				Debug.LogError("Can't find layout '" + layoutName);
			}
		}
	}

	//---------------------------------------------------------
	public static GUIBase_Layout GetLayout(string layoutName, GUIBase_Pivot pivot)
	{
		if (pivot)
		{
			GUIBase_Layout[] layouts = pivot.GetComponentsInChildren<GUIBase_Layout>();

			foreach (GUIBase_Layout layout in layouts)
			{
				if (layout.name == layoutName)
				{
					return layout;
				}
			}
		}

		Debug.LogError("Can't find layout '" + layoutName + "'");

		return null;
	}

	//---------------------------------------------------------
	public static GUIBase_Button RegisterButtonDelegate(GUIBase_Layout layout,
														string buttonName,
														GUIBase_Button.TouchDelegate touch,
														GUIBase_Button.ReleaseDelegate release)
	{
		GUIBase_Button control = GetControl<GUIBase_Button>(layout, buttonName);
		if (control != null)
		{
			RegisterButtonDelegate(control, touch, release);
		}
		else
		{
			Debug.LogError("Can't find button '" + buttonName + "'");
		}
		return control;
	}

	//---------------------------------------------------------
	public static GUIBase_Button RegisterButtonDelegate(GUIBase_Layout layout,
														string buttonName,
														GUIBase_Button.TouchDelegate2 touch,
														GUIBase_Button.ReleaseDelegate2 release,
														GUIBase_Button.CancelDelegate2 cancel)
	{
		GUIBase_Button control = GetControl<GUIBase_Button>(layout, buttonName);
		if (control != null)
		{
			RegisterButtonDelegate(control, touch, release, cancel);
		}
		else
		{
			Debug.LogError("Can't find button '" + buttonName);
		}
		return control;
	}

	//---------------------------------------------------------
	public static void RegisterButtonDelegate(GUIBase_Button button,
											  GUIBase_Button.TouchDelegate2 touch,
											  GUIBase_Button.ReleaseDelegate2 release,
											  GUIBase_Button.CancelDelegate2 cancel)
	{
		if (button != null)
		{
			button.RegisterTouchDelegate2(touch);
			button.RegisterReleaseDelegate2(release);
			button.RegisterCancelDelegate2(cancel);
		}
		else
		{
			Debug.LogError("Invalid agrument - button (null) ");
		}
	}

	//---------------------------------------------------------
	public static void RegisterButtonDelegate(GUIBase_Button button, GUIBase_Button.TouchDelegate touch, GUIBase_Button.ReleaseDelegate release)
	{
		if (button != null)
		{
			button.RegisterTouchDelegate(touch);
			button.RegisterReleaseDelegate(release);
		}
		else
		{
			Debug.LogError("Invalid agrument - button (null)");
		}
	}

	//---------------------------------------------------------
	public static void RegisterButtonDelegate3(GUIBase_Button button,
											   GUIBase_Button.TouchDelegate3 touch,
											   GUIBase_Button.TouchDelegate3 release,
											   GUIBase_Button.TouchDelegate3 cancel)
	{
		if (button != null)
		{
			button.RegisterTouchDelegate3(touch);
			button.RegisterReleaseDelegate3(release);
			button.RegisterCancelDelegate3(cancel);
		}
		else
		{
			Debug.LogError("Invalid agrument - button (null)");
		}
	}

	//---------------------------------------------------------
	public static void RegisterButtonDelegate3(GUIBase_Layout layout,
											   string buttonName,
											   GUIBase_Button.TouchDelegate3 touch,
											   GUIBase_Button.TouchDelegate3 release,
											   GUIBase_Button.TouchDelegate3 cancel)
	{
		GUIBase_Button control = GetControl<GUIBase_Button>(layout, buttonName);
		if (control != null)
		{
			RegisterButtonDelegate3(control, touch, release, cancel);
		}
		else
		{
			Debug.LogError("Can't find button '" + buttonName + "'");
		}
	}

	//---------------------------------------------------------
	public static GUIBase_Button GetButton(GUIBase_Layout layout, string buttonName)
	{
		GUIBase_Button control = GetControl<GUIBase_Button>(layout, buttonName);
		if (control == null)
		{
			Debug.LogWarning("Can't find button '" + buttonName + "'");
		}
		return control;
	}

	//---------------------------------------------------------
	public static GUIBase_Roller RegisterRollerDelegate(GUIBase_Layout layout, string rollerName, GUIBase_Roller.ChangeDelegate inChanged)
	{
		GUIBase_Roller control = GetControl<GUIBase_Roller>(layout, rollerName);
		if (control != null)
		{
			control.RegisterDelegate(inChanged);
		}
		else
		{
			Debug.LogError("Can't find roller '" + rollerName);
		}
		return control;
	}

	//---------------------------------------------------------
	public static GUIBase_Slider RegisterSliderDelegate(GUIBase_Layout layout, string sliderName, GUIBase_Slider.ChangeValueDelegate d)
	{
		GUIBase_Slider control = GetControl<GUIBase_Slider>(layout, sliderName);
		if (control != null)
		{
			control.RegisterChangeValueDelegate(d);
		}
		else
		{
			Debug.LogError("Can't find slider '" + sliderName + "'");
		}
		return control;
	}

	//---------------------------------------------------------
	public static GUIBase_Switch RegisterSwitchDelegate(GUIBase_Layout layout, string switchName, GUIBase_Switch.SwitchDelegate d)
	{
		GUIBase_Switch control = GetControl<GUIBase_Switch>(layout, switchName);
		if (control != null)
		{
			control.RegisterDelegate(d);
		}
		else
		{
			Debug.LogError("Can't find switch '" + switchName + "'");
		}
		return control;
	}

	//---------------------------------------------------------
	public static GUIBase_Sprite PrepareSprite(GUIBase_Layout layout, string name)
	{
		GUIBase_Sprite control = GetControl<GUIBase_Sprite>(layout, name);
		if (control == null)
		{
			Debug.LogWarning("Can't find sprite '" + name + "'");
		}
		return control;
	}

	//---------------------------------------------------------
	public static GUIBase_Label PrepareLabel(GUIBase_Layout layout, string name)
	{
		GUIBase_Label control = GetControl<GUIBase_Label>(layout, name);
		if (control == null)
		{
			Debug.LogWarning("Can't find label '" + name + "'");
		}
		return control;
	}

	//---------------------------------------------------------
	public static GUIBase_TextArea PrepareTextArea(GUIBase_Layout layout, string name)
	{
		GUIBase_TextArea control = GetControl<GUIBase_TextArea>(layout, name);
		if (control == null)
		{
			Debug.LogWarning("Can't find textArea '" + name + "'");
		}
		return control;
	}

	//---------------------------------------------------------
	public static void RegisterFocusDelegate(GUIBase_Pivot pivot, string layoutName, GUIBase_Layout.FocusDelegate d)
	{
		if (pivot)
		{
			GUIBase_Layout layout = GetLayout(layoutName, pivot);

			if (layout)
			{
				RegisterFocusDelegate(layout, d);
			}
			else
			{
				Debug.LogError("Can't find layout '" + layoutName);
			}
		}
	}

	//---------------------------------------------------------
	public static void RegisterFocusDelegate(GUIBase_Layout layout, GUIBase_Layout.FocusDelegate d)
	{
		if (layout)
		{
			layout.RegisterFocusDelegate(d);
		}
	}

	//---------------------------------------------------------
	public static GUIBase_Number PrepareNumber(GUIBase_Layout layout, string name)
	{
		GUIBase_Number control = GetControl<GUIBase_Number>(layout, name);
		if (control == null)
		{
			Debug.LogError("Can't find number '" + name + "'");
		}
		return control;
	}

	//---------------------------------------------------------
	public static GUIBase_ProgressBar PrepareProgressBar(GUIBase_Layout layout, string name)
	{
		GUIBase_ProgressBar control = GetControl<GUIBase_ProgressBar>(layout, name);
		if (control == null)
		{
			Debug.LogError("Can't find progressBar '" + name + "'");
		}
		return control;
	}

	//---------------------------------------------------------
	public static GUIBase_Enum RegisterEnumDelegate(GUIBase_Layout layout, string name, GUIBase_Enum.ChangeValueDelegate d)
	{
		return PrepareEnum(layout, name, d);
	}

	public static GUIBase_Enum PrepareEnum(GUIBase_Layout layout, string name, GUIBase_Enum.ChangeValueDelegate d)
	{
		GUIBase_Enum control = GetControl<GUIBase_Enum>(layout, name);
		if (control != null)
		{
			control.RegisterDelegate(d);
		}
		else
		{
			Debug.LogError("Can't find enum '" + name + "'");
		}
		return control;
	}

	//---------------------------------------------------------OBSOLOTE
	/*static public void ShowPivotWidgets(GUIBase_Pivot p, bool show)
	{
		p.Show(show);
		
		GUIBase_Widget[]	labs = p.GetComponentsInChildren<GUIBase_Widget>();
		foreach (GUIBase_Widget w in labs)
		{
			w.Show(show, true);
		}			
	}*/

	//---------------------------------------------------------
	public static T GetChild<T>(MonoBehaviour guiObject, string name, bool verbose = true) where T : MonoBehaviour
	{
		T[] controls = guiObject.GetComponentsInChildren<T>();
		foreach (var control in controls)
		{
			if (control.name == name)
				return control;
		}
		if (verbose == true)
		{
			Debug.LogWarning("Can't find " + typeof (T).Name + " '" + name + "' in '" + guiObject.GetFullName('.') + "'");
		}
		return null;
	}

	//---------------------------------------------------------
	public static GUIBase_Label GetChildLabel(GUIBase_Widget widget, string name, bool verbose = true)
	{
		return GetChild<GUIBase_Label>(widget, name, verbose);
	}

	//---------------------------------------------------------
	public static GUIBase_Sprite GetChildSprite(GUIBase_Widget widget, string name)
	{
		return GetChild<GUIBase_Sprite>(widget, name);
	}

	//---------------------------------------------------------
	public static GUIBase_Counter GetChildCounter(GUIBase_Widget widget, string name)
	{
		return GetChild<GUIBase_Counter>(widget, name);
	}

	//---------------------------------------------------------
	public static GUIBase_Number GetChildNumber(GUIBase_Button btn, string name)
	{
		return GetChild<GUIBase_Number>(btn, name);
	}

	//---------------------------------------------------------
	public static GUIBase_Number GetChildNumber(GUIBase_Widget widget, string name)
	{
		return GetChild<GUIBase_Number>(widget, name);
	}

	//---------------------------------------------------------
	public static GUIBase_Button GetChildButton(GUIBase_Widget widget, string name)
	{
		return GetChild<GUIBase_Button>(widget, name);
	}

	//---------------------------------------------------------
	public static T FindLayoutWidget<T>(GUIBase_Layout parentLayout, string name) where T : MonoBehaviour
	{
		T[] controls = parentLayout.gameObject.GetComponentsInChildren<T>();
		foreach (var control in controls)
		{
			if (control.name == name)
				return control;
		}
		Debug.LogWarning("Can't find " + typeof (T).Name + " '" + name + "' in '" + parentLayout.transform.GetFullName('.') + "'");
		return null;
	}

#if UNITY_EDITOR
	//---------------------------------------------------------
	public static void RenderRect(Rect rect, Color color)
	{
		Color oldColor = Gizmos.color;
		Gizmos.color = color;

		Vector2 pos = rect.center;
		Vector2 size = new Vector2(rect.width*0.5f, rect.height*0.5f);
		Vector3 offset = new Vector3(Screen.width*0.5f, Screen.height*0.5f, 20.0f);
		Vector3 tl = new Vector3(pos.x - size.x, Screen.height - pos.y - size.y, 0.0f) - offset;
		Vector3 tr = new Vector3(pos.x + size.x, Screen.height - pos.y - size.y, 0.0f) - offset;
		Vector3 br = new Vector3(pos.x + size.x, Screen.height - pos.y + size.y, 0.0f) - offset;
		Vector3 bl = new Vector3(pos.x - size.x, Screen.height - pos.y + size.y, 0.0f) - offset;
		Gizmos.DrawLine(tl, tr);
		Gizmos.DrawLine(tr, br);
		Gizmos.DrawLine(br, bl);
		Gizmos.DrawLine(bl, tl);

		Gizmos.color = oldColor;
	}
#endif
}
