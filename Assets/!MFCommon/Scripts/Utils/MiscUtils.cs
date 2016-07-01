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

//#####################################################################################################################

using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

//#####################################################################################################################

public class MiscUtils
{
	#region methods ///////////////////////////////////////////////////////////////////////////////////////////////////

	//-----------------------------------------------------------------------------------------------------------------
	// Fisher-Yates Shuffle
	//-----------------------------------------------------------------------------------------------------------------
	public static void Shuffle<T>(List<T> Array)
	{
		for (int i = Array.Count; i > 1; i--)
		{
			// pick random element to swap
			int j = UnityEngine.Random.Range(0, i); // 0 <= j <= i-1
			// swap with the "last" one
			T tmp = Array[j];
			Array[j] = Array[i - 1];
			Array[i - 1] = tmp;
		}
	}

//	//-----------------------------------------------------------------------------------------------------------------
//	static public T [] Join< T >( T [] A, T [] B )
//	{
//		T [] res = new T [ A.Length + B.Length ];
//		
//		Array.Copy( A, 0, res, 0,        A.Length );
//		Array.Copy( B, 0, res, A.Length, B.Length );
//		
//		return res;
//	}

	//-----------------------------------------------------------------------------------------------------------------
	// Swaps values of given objects.
	//-----------------------------------------------------------------------------------------------------------------
	public static void Swap<T>(ref T A, ref T B)
	{
		T tmp = A;
		A = B;
		B = tmp;
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Sorts two objects.
	//-----------------------------------------------------------------------------------------------------------------
	public static void Sort<T>(ref T A, ref T B) where T : IComparable
	{
		if (A.CompareTo(B) > 0)
			Swap(ref A, ref B);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Sort<T>(ref T A, ref T B, Comparison<T> Cmp)
	{
		if (Cmp(A, B) > 0)
			Swap(ref A, ref B);
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Sorts three objects.
	//-----------------------------------------------------------------------------------------------------------------
	public static void Sort<T>(ref T A, ref T B, ref T C) where T : IComparable
	{
		if (A.CompareTo(B) > 0)
			Swap(ref A, ref B);
		if (A.CompareTo(C) > 0)
			Swap(ref A, ref C);
		if (B.CompareTo(C) > 0)
			Swap(ref B, ref C);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public static void Sort<T>(ref T A, ref T B, ref T C, Comparison<T> Cmp)
	{
		if (Cmp(A, B) > 0)
			Swap(ref A, ref B);
		if (Cmp(A, C) > 0)
			Swap(ref A, ref C);
		if (Cmp(B, C) > 0)
			Swap(ref B, ref C);
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Returns random value from given enum.
	//-----------------------------------------------------------------------------------------------------------------
	public static T RandomEnum<T>()
	{
		T[] values = (T[])Enum.GetValues(typeof (T));
		int index = UnityEngine.Random.Range(0, values.Length);

		return values[index];
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Returns random value from given array.
	//-----------------------------------------------------------------------------------------------------------------
	public static T RandomValue<T>(T[] Values)
	{
		if (Values != null)
		{
			return Values[UnityEngine.Random.Range(0, Values.Length)];
		}
		return default(T);
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Creates instance of given type.                                                             !!! EXPERIMENTAL !!!
	//-----------------------------------------------------------------------------------------------------------------
	public static T Create<T>()
	{
		System.Type type = typeof (T);

		if ((type.IsValueType == true) || (type == typeof (string)))
		{
			return default(T);
		}
		
#if (true)

		else if (type.IsSubclassOf(typeof (UnityEngine.Object)))
		{
			return default(T); // don't create instance of anything "referenced" by Unity !!!
		}
		
#endif

		else
		{
			return (T)System.Activator.CreateInstance(type, true);
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Creates duplicate (deep copy) of given object.                                              !!! EXPERIMENTAL !!!
	//-----------------------------------------------------------------------------------------------------------------
	public static T DeepCopy<T>(T Obj)
	{
		return (T)CreateDeepCopy(Obj);
	}

	//-----------------------------------------------------------------------------------------------------------------
	static System.Object CreateDeepCopy(System.Object Obj)
	{
		if (Obj == null)
			return null;

		System.Type objType = Obj.GetType();

		if ((objType.IsValueType == true) || (objType == typeof (string)))
		{
			return Obj;
		}
		
#if (true)

		else if (objType.IsSubclassOf(typeof (UnityEngine.Object)))
		{
			return Obj; // don't duplicate anything "referenced" by Unity !!!
		}
		
#endif

		else if (objType.IsArray)
		{
			System.Array array = (System.Array)Obj;
			System.Type elementType = objType.GetElementType();
			System.Array arrayCopy = System.Array.CreateInstance(elementType, array.Length);

			for (int i = 0; i < array.Length; ++i)
			{
				arrayCopy.SetValue(CreateDeepCopy(array.GetValue(i)), i);
			}

			return arrayCopy;
		}

		System.Object objCopy = System.Activator.CreateInstance(objType, true);
		System.Reflection.FieldInfo[] objFields = objType.GetFields(System.Reflection.BindingFlags.Public |
																	System.Reflection.BindingFlags.NonPublic |
																	System.Reflection.BindingFlags.Instance);

		foreach (System.Reflection.FieldInfo field in objFields)
		{
			if ((field.FieldType.IsPrimitive == false) && (field.FieldType != typeof (string)))
			{
				System.Object fieldCopy = CreateDeepCopy(field.GetValue(Obj));

				field.SetValue(objCopy, fieldCopy);
			}
			else
			{
				field.SetValue(objCopy, field.GetValue(Obj));
			}
		}

		return objCopy;
	}

	//-----------------------------------------------------------------------------------------------------------------
	// Returns list with sub-classes inherited from base-class (implementing interface) in given assemblies.
	// If assemblies are not defined then "the executing one" will be used.
	//-----------------------------------------------------------------------------------------------------------------
	public static List<System.Type> GetSubClasses(Assembly[] Assemblies, System.Type Base, bool IncludeBase, bool IncludeAbstract)
	{
		if (Base == null)
			return null;

		List<System.Type> types = new List<System.Type>();

		if (IncludeBase)
		{
			types.Add(Base); // AddSubClass( Base, IncludeAbstract, types );
		}

		if (Assemblies == null)
		{
			GetSubClasses(Assembly.GetExecutingAssembly(), Base, IncludeAbstract, types);
		}
		else
		{
			foreach (Assembly asm in Assemblies)
			{
				GetSubClasses(asm, Base, IncludeAbstract, types);
			}
		}

		return types;
	}

	//-----------------------------------------------------------------------------------------------------------------
	static void GetSubClasses(Assembly Asm, System.Type Base, bool IncludeAbstract, List<System.Type> SubClasses)
	{
		foreach (System.Type t in Asm.GetTypes())
		{
			if ((t != Base) && (Base.IsAssignableFrom(t) == true))
			{
				AddSubClass(t, IncludeAbstract, SubClasses);
			}
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	static void AddSubClass(System.Type T, bool IncludeAbstract, List<System.Type> SubClasses)
	{
		if ((T.IsAbstract == false) || (IncludeAbstract == true))
		{
			SubClasses.Add(T);
		}
	}

	#endregion
}

//#####################################################################################################################
