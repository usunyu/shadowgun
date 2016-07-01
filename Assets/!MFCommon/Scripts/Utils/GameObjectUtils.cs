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
using System;
using System.Collections;
using System.Collections.Generic;

class GameObjectUtils
{
	public static T GetFirstComponentUpward<T>(GameObject inGameObject) where T : Component
	{
		if (inGameObject == null)
			return null;

		T t = inGameObject.GetComponent<T>();

		if (t != null)
			return t;

		Transform parent = inGameObject.transform.parent;
		if (parent == null || parent.gameObject == null)
			return null;

		return GetFirstComponentUpward<T>(parent.gameObject);
	}

	public static T GetComponentWithInterface<T>(GameObject inGameObject) where T : class
	{
		foreach (Component comp in inGameObject.GetComponents<Component>())
		{
			T t = comp as T;
			if (t != null)
				return t;
		}

		return default(T);
	}

	public static T GetFirstComponentUpwardWithInterface<T>(GameObject inGameObject) where T : class
	{
		if (inGameObject != null)
		{
			foreach (Component comp in inGameObject.GetComponents<Component>())
			{
				T t = comp as T;
				if (t != null)
					return t;
			}

			Transform parent = inGameObject.transform.parent;
			if (parent != null && parent.gameObject != null)
			{
				return GetFirstComponentUpwardWithInterface<T>(parent.gameObject);
			}
		}

		return default(T);
	}

	public static T[] GetComponentsInChildrenWithInterface<T>(GameObject inGameObject, bool inIncludeInactive) where T : class
	{
		if (inGameObject != null)
		{
			MonoBehaviour[] childrens = inGameObject.GetComponentsInChildren<MonoBehaviour>(inIncludeInactive);
			T[] returnObjects = new T[childrens.Length];
			int index = 0;

			foreach (MonoBehaviour comp in childrens)
			{
				T t = comp as T;
				if (t != null)
				{
					returnObjects[index++] = t;
				}
			}

			if (index > 0)
			{
				Array.Resize(ref returnObjects, index);
				return returnObjects;
			}
		}

		return null;
	}

	public static string GetFullName(GameObject inObject, char separator = '/')
	{
		if (inObject)
		{
			if (inObject.transform.parent)
				return GetFullName(inObject.transform.parent.gameObject, separator) + separator + inObject.name;

			return inObject.name;
		}

		return "";
	}

	public static Transform FindChildByName(Transform inTransform, string inName)
	{
		foreach (Transform tr in inTransform)
		{
			if (tr.name == inName)
				return tr;

			Transform tr2 = FindChildByName(tr, inName);

			if (tr2 != null)
				return tr2;
		}

		return null;
	}
}

public static class GameObjectExtension
{
	public static T GetFirstComponentUpwardWithInterface<T>(this GameObject inGameObject) where T : class
	{
		return GameObjectUtils.GetFirstComponentUpwardWithInterface<T>(inGameObject);
	}

	public static T GetFirstComponentUpward<T>(this GameObject inGameObject) where T : Component
	{
		return GameObjectUtils.GetFirstComponentUpward<T>(inGameObject);
	}

	public static T GetComponentWithInterface<T>(this GameObject inGameObject) where T : class
	{
		return GameObjectUtils.GetComponentWithInterface<T>(inGameObject);
	}

	public static string GetFullName(this GameObject inGameObject)
	{
		return GameObjectUtils.GetFullName(inGameObject);
	}

	public static T[] GetComponentsInChildrenWithInterface<T>(this GameObject inGameObject) where T : class
	{
		return GetComponentsInChildrenWithInterface<T>(inGameObject, false);
	}

	public static T[] GetComponentsInChildrenWithInterface<T>(this GameObject inGameObject, bool inIncludeInactive) where T : class
	{
		return GameObjectUtils.GetComponentsInChildrenWithInterface<T>(inGameObject, inIncludeInactive);
	}
}

public static class ComponentExtension
{
	public static string GetFullName(this Component inComponent, char separator = '/')
	{
		return GameObjectUtils.GetFullName(inComponent ? inComponent.gameObject : null, separator) + ", " +
			   (inComponent ? inComponent.GetType().Name : "Invalid Component");
	}
}

public static class TransformExtension
{
	public static Transform FindChildByName(this Transform inTransform, string inName)
	{
		return GameObjectUtils.FindChildByName(inTransform, inName);
	}

	public static T GetChildComponent<T>(this Transform inTransform, string inName) where T : Component
	{
		Transform tr = GameObjectUtils.FindChildByName(inTransform, inName);

		return tr != null ? tr.GetComponent<T>() : null;
	}
}

public static class AnimationExtension
{
	public static bool Contains(this Animation inAnimation, AnimationClip inClip)
	{
		foreach (AnimationState state in inAnimation)
		{
			if (state.clip == inClip)
				return true;
		}

		return false;
	}
}
