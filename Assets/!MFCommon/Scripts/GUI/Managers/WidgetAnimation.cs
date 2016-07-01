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
using Tweener = Tween.Tweener;
using Easing = Tween.Easing;

namespace WidgetAnimation
{
	public abstract class Base
	{
		protected static Tweener m_Tweener = new Tweener();

		GUIBase_Widget m_Widget;
		Transform m_Transform;
		Vector3 m_LocalScale;
		float m_Scale = 1.75f;
		float m_TargetScale;
		float m_Duration = 1.0f;
		AudioClip m_AudioClip;

		public GUIBase_Widget Widget
		{
			get { return m_Widget; }
		}

		public float Scale
		{
			get { return m_Scale; }
			set
			{
				m_Scale = value;
				SetScale(m_Scale);
			}
		}

		public float Duration
		{
			get { return m_Duration; }
			set { m_Duration = value; }
		}

		public AudioClip AudioClip
		{
			get { return m_AudioClip; }
			set { m_AudioClip = value; }
		}

		public bool Visible
		{
			get { return m_Widget != null ? m_Widget.Visible : false; }
		}

		public virtual bool Finished
		{
			get { return Mathf.Abs(m_TargetScale - m_Scale) <= float.Epsilon; }
		}

		protected virtual void OnStart(Tweener tweener)
		{
		}

		protected virtual void OnUpdate()
		{
		}

		protected Base(GUIBase_Widget widget)
		{
			m_Widget = widget;
			m_Transform = widget.transform;
			m_LocalScale = m_Transform.localScale;
			m_TargetScale = 1.0f;

			SetScale(m_TargetScale);
		}

		public void Start()
		{
			if (m_Tweener.IsTweening == true)
				return;

			if (m_Widget.Visible == true)
			{
				m_Tweener.TweenTo(this, "m_Scale", m_TargetScale, m_Duration, Easing.Sine.EaseOut);
			}

			OnStart(m_Widget.Visible ? m_Tweener : null);
		}

		public void Update()
		{
			if (m_Tweener.IsTweening == true)
			{
				m_Tweener.UpdateTweens();
			}

			OnUpdate();

			if (Visible == false || Finished == true)
			{
				m_Tweener.StopTweens(true);

				m_Scale = m_TargetScale;
			}
			else
			{
				MFGuiManager.Instance.PlayOneShot(m_AudioClip);
			}

			SetScale(m_Scale);

			m_Widget.SetModify(true);
		}

		public void ForceFinish()
		{
			m_Tweener.StopTweens(true);
		}

		void SetScale(float scale)
		{
			m_Transform.localScale = m_LocalScale*scale;
		}
	}

	public class Widget : Base
	{
		float m_Alpha;
		float m_TargetAlpha;

		public float Alpha
		{
			get { return m_Alpha; }
			set
			{
				m_Alpha = value;
				Widget.SetFadeAlpha(m_Alpha, true);
			}
		}

		public float TargetAlpha
		{
			get { return m_TargetAlpha; }
			set { m_TargetAlpha = value; }
		}

		public override bool Finished
		{
			get { return Mathf.Abs(m_TargetAlpha - m_Alpha) <= float.Epsilon ? base.Finished : false; }
		}

		public Widget(GUIBase_Widget widget)
						: base(widget)
		{
			m_Alpha = widget.FadeAlpha;
			m_TargetAlpha = widget.FadeAlpha;
		}

		protected override void OnStart(Tweener tweener)
		{
			if (tweener == null)
			{
				m_Alpha = m_TargetAlpha;
			}
			else
			{
				tweener.TweenTo(this, "m_Alpha", m_TargetAlpha, Duration, Easing.Sine.EaseOut);
			}
		}

		protected override void OnUpdate()
		{
			if (Finished == true)
			{
				m_Alpha = m_TargetAlpha;
			}

			Widget.SetFadeAlpha(m_Alpha, true);
		}
	}

	public class Label : Base
	{
		GUIBase_Label m_Label;

		public Label(GUIBase_Label label, string text)
						: base(label.Widget)
		{
			m_Label = label;
			m_Label.SetNewText(text);
		}
	}

	public abstract class NumericBase : Base
	{
		GUIBase_Label m_Label;
		float m_Value;
		float m_TargetValue;
		string m_Format = "{0:0}";

		public string Format
		{
			get { return m_Format; }
			set
			{
				m_Format = value;
				ShowValue(m_Value);
			}
		}

		public override bool Finished
		{
			get { return Mathf.Abs(m_TargetValue - m_Value) <= float.Epsilon ? base.Finished : false; }
		}

		protected virtual void SetText(string text)
		{
			m_Label.SetNewText(text);
		}

		public NumericBase(GUIBase_Label label, float source, float target)
						: base(label.Widget)
		{
			m_Label = label;
			m_Value = source;
			m_TargetValue = target;

			ShowValue(m_Value);
		}

		protected override void OnStart(Tweener tweener)
		{
			if (tweener == null)
			{
				m_Value = m_TargetValue;
			}
			else
			{
				tweener.TweenTo(this, "m_Value", m_TargetValue, Duration, Easing.Sine.EaseOut);
			}
		}

		protected override void OnUpdate()
		{
			if (Finished == true)
			{
				m_Value = m_TargetValue;
			}

			ShowValue(m_Value);
		}

		void ShowValue(float value)
		{
			SetText(string.Format(Format, value));
		}
	}

	public class NumericLabel : NumericBase
	{
		public NumericLabel(GUIBase_Label label, float source, float target)
						: base(label, source, target)
		{
		}
	}

	public class NumericButton : NumericLabel
	{
		GUIBase_Button m_Button;

		public NumericButton(GUIBase_Button button, float source, float target)
						: base(button.GetComponentInChildren<GUIBase_Label>(), source, target)
		{
			m_Button = button;
		}

		protected override void SetText(string text)
		{
			if (m_Button != null)
			{
				m_Button.SetNewText(text);
			}
			else
			{
				base.SetText(text);
			}
		}
	}
}
