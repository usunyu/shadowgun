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
using System.Reflection;
using LitJson;

//
// NOTE:
//
// If you create any new specialization of SettingsManager<> consider adding its initialization from
// cloud service to SettingsCloudSync.UpdateSettingsManagers()
//

public abstract class SettingsManager<T, Key> : ScriptableObject where T : Settings<Key>
{
	// -----------
	public T Get(Key key)
	{
		T val;

		if (!Objects.TryGetValue(key, out val))
		{
			Debug.LogWarning("SettingsManager: Can't find settings for " + key);
			return default(T);
		}
		else
			return val;
	}

	// -----------
	public T[] GetAll()
	{
		T[] objects = new T[Objects.Count];
		Objects.Values.CopyTo(objects, 0);

		return objects;
	}

	public T FindByGUID(int GUID)
	{
		foreach (KeyValuePair<Key, T> pair in Objects)
			if (pair.Value.GUID == GUID)
				return pair.Value;

		return null;
	}

	// --------------------------------------------------------------------------------------------
	// P R I V A T E / P R O T E C T E D    P A R T
	// --------------------------------------------------------------------------------------------
	protected Dictionary<Key, T> Objects = new Dictionary<Key, T>();

	// ----------
	// Use this for initialization
	protected bool Init(string resourcePath)
	{
		GameObject prefab = GameObject.Find(resourcePath);
		if (prefab != null)
		{
			GameObject.DontDestroyOnLoad(prefab);
		}
		else
		{
			Debug.Log("Loading: " + resourcePath);
			prefab = Resources.Load(resourcePath) as GameObject;
		}

		if (prefab == null)
		{
			Debug.LogWarning("Cant find prefab: " + resourcePath);
			return false;
		}

		T[] objects = prefab.GetComponentsInChildren<T>(true);
		//Debug.Log( objects.Length + " settings found!");

		// insert all objects
		foreach (T obj in objects)
		{
			if (Objects.ContainsKey(obj.ID))
				Debug.LogWarning("Duplicite obj id: " + obj.ToString());
			else
				Objects[obj.ID] = obj;
		}
		return true;
	}

	bool UpdateObjectField(FieldInfo updatedField, System.Object updatedObject, JsonData jsonData)
	{
		JsonData valueData = null;

		try
		{
			valueData = jsonData[updatedField.Name];
		}

		catch
		{
			Debug.LogWarning("Unable to get value data (" + updatedField.Name + ")" + " for shopItem : " + updatedObject);

			return false;
		}

		try
		{
			System.Object obj = updatedField.GetValue(updatedObject);

			if (obj is int)
			{
				updatedField.SetValue(updatedObject, (int)valueData);
			}
			else if (obj is bool)
			{
				updatedField.SetValue(updatedObject, (bool)valueData);
			}
			else if (obj is string)
			{
				updatedField.SetValue(updatedObject, (string)valueData);
			}
			else if (obj is float)
			{
				double valDouble = (double)valueData;
				//Debug.Log( valDouble );
				//updatedField.SetValue(updatedObject,(double)valueData);
				updatedField.SetValue(updatedObject, (float)valDouble);
			}

			// todo: needs general solution
			else if (updatedField.FieldType == typeof (List<E_PlatformShop>))
			{
				List<E_PlatformShop> list = new List<E_PlatformShop>();

				if (valueData != null)
				{
					for (int i = 0; i < valueData.Count; i++)
					{
						list.Add((E_PlatformShop)(int)valueData[i]);
					}
				}

				updatedField.SetValue(updatedObject, list);
			}

			else
			{
				System.Type type = obj.GetType();

				if (type.IsArray) // array
				{
					// todo : add array support
				}
				else if (type.IsClass) // class
				{
					if (typeof (IList).IsAssignableFrom(type)) // list
					{
						IList list = obj as IList;
						list.Clear();

						// TODO : do we need convert JsonData to the json string?
						IList items = JsonMapper.ToObject(type, valueData.ToJson()) as IList;

						foreach (var item in items)
						{
							list.Add(item);
						}
					}
					else
					{
						UpdateObjectFromJSONDesc(updatedField.GetValue(updatedObject), valueData);
					}
				}
				else
				{
					Debug.LogError("SettingsManager.UpdateObjectField() : unsupported field type");
				}
			}
		}

		catch
		{
			Debug.LogError("Unable to set value (" + updatedField.Name + ")" + " for shopItem : " + updatedObject + "( value = " + valueData + ")");

			return false;
		}

		return true;
	}

	public bool UpdateFromJSONDesc(Dictionary<int, JsonData> data)
	{
		bool res = true;

		foreach (KeyValuePair<Key, T> curr in Objects)
		{
			int guid = curr.Value.GUID;

			if (!data.ContainsKey(guid))
			{
				Debug.LogWarning("SettingsManager<>: UpdateFromJSONDesc(): item with GUID:" + guid + " not found");

				res = false;

				continue;
			}

			JsonData currData = data[guid];

			UpdateObjectFromJSONDesc(curr.Value, currData);
		}

		return res;
	}

	void UpdateObjectFromJSONDesc(System.Object obj, JsonData jsonData)
	{
		foreach (FieldInfo currField in obj.GetType().GetFields())
		{
			foreach (System.Attribute currAttr in currField.GetCustomAttributes(true))
			{
				if (currAttr is OnlineShopItemAttribute)
				{
					// TODO: currently not supported

					break;
				}
				else if (currAttr is OnlineShopItemProperty)
				{
					UpdateObjectField(currField, obj, jsonData);

					break;
				}
			}
		}
	}
}
