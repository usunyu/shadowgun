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
using System.Reflection;
using System.Collections.Generic;

namespace Tween
{
	public class Tweener
	{
		readonly static BindingFlags m_BindingFlags
						= BindingFlags.Instance
						  | BindingFlags.Static
						  | BindingFlags.Public
						  | BindingFlags.NonPublic
						  | BindingFlags.GetField
						  | BindingFlags.SetField;
		//| BindingFlags.GetProperty
		//| BindingFlags.SetProperty;

		public struct Tween
		{
			public object Object;
			public string FieldName;
			public FieldInfo FieldInfo;
			public float TweenFrom;
			public float TweenTo;
			public float StartTime;
			public float Duration;
			public EasingFunc Easing;
			public TweenDelegate Callback;
		}

		public delegate void TweenDelegate(Tween tween, bool finished);

		// PRIVATE MEMBERS

		List<Tween> m_Tweens = new List<Tween>();

		// GETTERS/SETTERS

		public bool IsTweening
		{
			get { return m_Tweens.Count > 0 ? true : false; }
		}

		// PUBLIC METHODS

		public bool TweenFromTo(object obj,
								string fieldName,
								float tweenFrom,
								float tweenTo,
								float duration,
								EasingFunc easing = null,
								TweenDelegate callback = null)
		{
			if (obj == null)
				return false;

			FieldInfo fieldInfo = GetField(obj, fieldName);
			if (fieldInfo == null)
				return false;

			PrepareTween(obj, fieldInfo, tweenFrom, tweenTo, duration, easing, callback);

			return true;
		}

		public bool TweenFrom(object obj,
							  string fieldName,
							  float tweenFrom,
							  float duration,
							  EasingFunc easing = null,
							  TweenDelegate callback = null)
		{
			if (obj == null)
				return false;

			FieldInfo fieldInfo = GetField(obj, fieldName);
			if (fieldInfo == null)
				return false;

			PrepareTween(obj, fieldInfo, tweenFrom, (float)fieldInfo.GetValue(obj), duration, easing, callback);

			return true;
		}

		public bool TweenTo(object obj, string fieldName, float tweenTo, float duration, EasingFunc easing = null, TweenDelegate callback = null)
		{
			if (obj == null)
				return false;

			FieldInfo fieldInfo = GetField(obj, fieldName);
			if (fieldInfo == null)
				return false;

			PrepareTween(obj, fieldInfo, (float)fieldInfo.GetValue(obj), tweenTo, duration, easing, callback);

			return true;
		}

		public void StopTween(object obj, string fieldName, bool jumpToEnd)
		{
			int idx = FindTween(obj, fieldName);
			if (idx < 0)
				return;

			FinishTween(idx, jumpToEnd);
		}

		public void StopTweens(bool jumpToEnd)
		{
			while (m_Tweens.Count > 0)
			{
				FinishTween(0, jumpToEnd);
			}
		}

		public void UpdateTweens()
		{
			int count = m_Tweens.Count;

			for (int idx = count - 1; idx >= 0; --idx)
			{
				Tween tween = m_Tweens[idx];
				float delta = Time.time - tween.StartTime;
				if (delta < tween.Duration)
				{
					float value = tween.Easing(delta, tween.TweenFrom, tween.TweenTo - tween.TweenFrom, tween.Duration);
					tween.FieldInfo.SetValue(tween.Object, (float)System.Math.Round(value, 4));

					if (tween.Callback != null)
					{
						tween.Callback(tween, false);
					}
				}
				else
				{
					FinishTween(idx, true);
				}

				if (m_Tweens.Count < count)
					break;
			}
		}

		// PRIVATE METHODS

		int FindTween(object obj, string fieldName)
		{
			for (int idx = 0, count = m_Tweens.Count; idx < count; ++idx)
			{
				if (m_Tweens[idx].Object == obj &&
					m_Tweens[idx].FieldName == fieldName)
					return idx;
			}
			return -1;
		}

		void PrepareTween(object obj,
						  FieldInfo fieldInfo,
						  float tweenFrom,
						  float tweenTo,
						  float duration,
						  EasingFunc easing,
						  TweenDelegate callback)
		{
			int idx = FindTween(obj, fieldInfo.Name);
			if (idx < 0)
			{
				idx = m_Tweens.Count;
				m_Tweens.Add(new Tween());
			}

			Tween tween = m_Tweens[idx];
			tween.Object = obj;
			tween.FieldName = fieldInfo.Name;
			tween.FieldInfo = fieldInfo;
			if (tween.TweenFrom != tweenFrom || tween.TweenTo != tweenTo)
			{
				tween.TweenFrom = tweenFrom;
				tween.TweenTo = tweenTo;
				tween.StartTime = Time.time;
			}
			tween.Duration = duration;
			tween.Easing = easing ?? Easing.Linear.EaseNone;
			tween.Callback = callback;
			m_Tweens[idx] = tween;
		}

		void FinishTween(int idx, bool jumpToEnd)
		{
			if (idx < 0 || idx >= m_Tweens.Count)
				return;

			Tween tween = m_Tweens[idx];
			m_Tweens.RemoveAt(idx);

			if (jumpToEnd == true)
			{
				tween.FieldInfo.SetValue(tween.Object, tween.TweenTo);
			}

			if (tween.Callback != null)
			{
				tween.Callback(tween, true);
			}
		}

		FieldInfo GetField(object obj, string fieldName)
		{
			return obj != null ? GetField(obj.GetType(), fieldName) : null;
		}

		FieldInfo GetField(System.Type type, string fieldName)
		{
			if (type == null)
				return null;

			FieldInfo fieldInfo = type.GetField(fieldName, m_BindingFlags);
			if (fieldInfo != null)
				return fieldInfo;

			return GetField(type.BaseType, fieldName);
		}
	}
}
