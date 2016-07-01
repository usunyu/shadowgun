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
// 
// Adopted from: http://www.codeproject.com/Articles/33528/Accelerating-Enum-Based-Dictionaries-with-Generic
// 
// 
// This should be fast and generic implementation of 'IEqualityComparer' for 'Enum' types. Useful for dictionaries
// that use 'Enum' as their keys because default implementation of comparer for enums (used if none specified in 
// constructor of dictionary) is quite slow.
// 
// Usage:
// 
//    var dict = new Dictionary< MyEnumType, string >( EnumComparer< MyEnumType >.Instance );
// 
// 
// But you can always write something like this (which would be faster by a few percents):
//
//    public class MyEnumTypeComparer : IEqualityComparer< MyEnumType >
//    {
//        public static readonly MyEnumTypeComparer Instance = new MyEnumTypeComparer();
//        
//        public bool Equals( MyEnumType x, MyEnumType y )
//        {
//            return x == y;
//        }
//        
//        public int GetHashCode( MyEnumType x )
//        {
//            return (int) x;
//        }
//    }
// 
//    var dict = new Dictionary< MyEnumType, string >( MyEnumTypeComparer.Instance );
// 
//#####################################################################################################################

#define EXPRESSION_TREE_IMPLEMENTATION

#if EXPRESSION_TREE_IMPLEMENTATION
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

#else
	using System;
	using System.Collections.Generic;
	using System.Reflection.Emit;
#endif

//#####################################################################################################################

public sealed class EnumComparer<T> : IEqualityComparer<T> where T : struct, IComparable, IConvertible, IFormattable
{
	public readonly static EnumComparer<T> Instance;
	readonly static Func<T, T, bool> Func_Equals;
	readonly static Func<T, int> Func_GetHashCode;

	//-----------------------------------------------------------------------------------------------------------------
	static EnumComparer()
	{
		Instance = new EnumComparer<T>();
		Func_Equals = GenerateFunc_Equals();
		Func_GetHashCode = GenerateFunc_GetHashCode();
	}

	//-----------------------------------------------------------------------------------------------------------------
	EnumComparer()
	{
		Type templateType = typeof (T);

		if (templateType.IsEnum == false)
		{
			string msg = string.Format("The type parameter {0} is not an Enum!", templateType);

			throw new NotSupportedException(msg);
		}

		Type underlyingType = Enum.GetUnderlyingType(templateType);
		Type[] supportedTypes = new Type[]
		{
			typeof (byte), typeof (short), typeof (int), typeof (long),
			typeof (sbyte), typeof (ushort), typeof (uint), typeof (ulong)
		};

		if (Array.IndexOf(supportedTypes, underlyingType) == -1)
		{
			string msg = string.Format("The underlying type of the type parameter {0} is {1} which is unsupported!",
									   templateType,
									   underlyingType);

			throw new NotSupportedException(msg);
		}
	}

	//-----------------------------------------------------------------------------------------------------------------
	public bool Equals(T x, T y)
	{
		return Func_Equals(x, y);
	}

	//-----------------------------------------------------------------------------------------------------------------
	public int GetHashCode(T x)
	{
		return Func_GetHashCode(x);
	}

#if EXPRESSION_TREE_IMPLEMENTATION

	//-----------------------------------------------------------------------------------------------------------------
	static Func<T, T, bool> GenerateFunc_Equals()
	{
		var xParam = Expression.Parameter(typeof (T), "x");
		var yParam = Expression.Parameter(typeof (T), "y");
		var equalExpression = Expression.Equal(xParam, yParam);

		return Expression.Lambda<Func<T, T, bool>>(equalExpression, new[] {xParam, yParam}).Compile();
	}

	//-----------------------------------------------------------------------------------------------------------------
	static Func<T, int> GenerateFunc_GetHashCode()
	{
		var xParam = Expression.Parameter(typeof (T), "x");
		var underlyingType = Enum.GetUnderlyingType(typeof (T));
		var convertExpression = Expression.Convert(xParam, underlyingType);

		var getHashCodeMethod = underlyingType.GetMethod("GetHashCode");
		var getHashCodeExpression = Expression.Call(convertExpression, getHashCodeMethod);

		return Expression.Lambda<Func<T, int>>(getHashCodeExpression, new[] {xParam}).Compile();
	}

#else

				//-----------------------------------------------------------------------------------------------------------------
	private static Func< T, T, bool > GenerateFunc_Equals()
	{
		var method = new DynamicMethod( typeof(T).Name + "_Equals",
		                                typeof(bool),
		                                new [] { typeof(T), typeof(T) },
		                                typeof(T),
		                                true);
		
		var generator = method.GetILGenerator();
		
		generator.Emit( OpCodes.Ldarg_0 );   // load x to stack
		generator.Emit( OpCodes.Ldarg_1 );   // load y to stack
		generator.Emit( OpCodes.Ceq     );   // x == y
		generator.Emit( OpCodes.Ret     );   // return result
		
		return ( Func<T,T,bool> ) method.CreateDelegate( typeof( Func<T,T,bool> ) );
	}
	
	//-----------------------------------------------------------------------------------------------------------------
	private static Func< T, int > GenerateFunc_GetHashCode()
	{
		var underlyingType    = Enum.GetUnderlyingType( typeof(T) );
		var getHashCodeMethod = underlyingType.GetMethod( "GetHashCode" );
		
		var method = new DynamicMethod( typeof(T).Name + "_GetHashCode",
		                                typeof(int),
		                                new [] { typeof(T) },
		                                typeof(T),
		                                true );
		
		var generator = method.GetILGenerator();
		var castValue = generator.DeclareLocal( underlyingType );
		
		generator.Emit( OpCodes.Ldarg_0                 );   // load 'x' to stack
		generator.Emit( OpCodes.Stloc_0                 );   // castValue = x
		generator.Emit( OpCodes.Ldloca_S, castValue     );   // load *castValue to stack
		generator.Emit( OpCodes.Call, getHashCodeMethod );   // castValue.GetHashCode()
		generator.Emit( OpCodes.Ret                     );   // return result
		
		return ( Func<T,int> ) method.CreateDelegate( typeof( Func<T,int> ) );
	}
	
#endif
}

//#####################################################################################################################
