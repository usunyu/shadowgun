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

//
// GUI Editor utilities and constants
//	(.. to avoid duplication of code in GUIEditorWidget and GUIEditorLayout)
//

using UnityEngine;
using UnityEditor;
using System.Collections;

public class GUIEditorUtils
{
	public static int		MAX_LAYERS					= 10;

	public static float		MIN_SCALE_FACTOR			= 0.125f;
	public static float		MAX_SCALE_FACTOR			= 8.0f;
	
	public static float		WHEEL_PAN_MULTIPLICATOR		= 0.01f;
	
	public static float		MIN_EPSILON_POINT_DELTA		= 0.01f;
	
	public static int		MIN_BACKGROUND_SIZE			= 100;
	
	public static Color		COLOR_AREA_EDGE				= Color.red;
	public static Color		COLOR_AREA_POINT			= Color.black;
	public static Color		COLOR_AREA_SELECTED_POINT	= Color.red;
	public static Color		COLOR_LAYOUT_AREA			= Color.black;
	public static Color		COLOR_AREA_TOUCHABLE_EDGE	= Color.yellow;
	public static Color		COLOR_AREA_SLICE			= Color.grey;

	static Vector2			s_TopLeftCorner				= new Vector2(0.0f, 0.0f);

	//
	// Set clip point
	//
	public static void SetClipPoint(Vector2 topLeftPos)
	{
		s_TopLeftCorner = topLeftPos;
	}
		
	//---------------------------------------------------------------------------------------------
	//
	// FindLayoutForWidget
	//
	// Finds layout where widget is laying (GUIBase_Widget should be child of GUIBase_Layout or of another GUIBase_Widget)
	//
	//---------------------------------------------------------------------------------------------
	public static GUIBase_Layout FindLayoutForWidget(GUIBase_Widget widget)
	{
		if (widget)
		{
			// Iterate over all layouts from scene
			GUIBase_Layout[]	layouts		= Object.FindObjectsOfType(typeof(GUIBase_Layout)) as GUIBase_Layout[];
		
			foreach (GUIBase_Layout l in layouts)
			{
				// Enumerate widgets of this layout
				GUIBase_Widget[]	children = l.GetComponentsInChildren<GUIBase_Widget>();
				
				foreach (GUIBase_Widget w in children)
				{
					if (w == widget)
					{
						return l;
					}
				}
			}

			Transform parent = widget.transform.parent;
			while (parent != null)
			{
				GUIBase_Layout layout = parent.GetComponent<GUIBase_Layout>();
				if (layout != null)
					return layout;
				parent = parent.parent;
			}
		}
		
		return null;
	}
	
	//---------------------------------------------------------------------------------------------
	//
	// FindPlatformForLayout
	//
	// Finds platform where layout is laying (GUIBase_Layout should be child of GUIBase_Platform)
	//
	//---------------------------------------------------------------------------------------------
	public static GUIBase_Platform FindPlatformForLayout(GUIBase_Layout layout)
	{
		if (layout)
		{
			// Iterate over all platform from scene
			GUIBase_Platform[]	platforms		= Object.FindObjectsOfType(typeof(GUIBase_Platform)) as GUIBase_Platform[];
		
			foreach (GUIBase_Platform p in platforms)
			{
				// Enumerate layouts of this platform
				GUIBase_Layout[]	children = p.GetComponentsInChildren<GUIBase_Layout>();
		
				foreach (GUIBase_Layout l in children)
				{
					if (l == layout)
					{
						return p;
					}
				}
			}
		}
		
		return null;
	}

	//---------------------------------------------------------------------------------------------
	//
	// FindPlatformForPivot
	//
	// Finds platform where Pivot is laying (Pivot should be child of GUIBase_Platform)
	//
	//---------------------------------------------------------------------------------------------
	public static GUIBase_Platform FindPlatformForPivot(GUIBase_Pivot pivot)
	{
		if (pivot)
		{
			// Iterate over all platform from scene
			GUIBase_Platform[]	platforms		= Object.FindObjectsOfType(typeof(GUIBase_Platform)) as GUIBase_Platform[];
		
			foreach (GUIBase_Platform p in platforms)
			{
				// Enumerate pivots of this platform
				GUIBase_Pivot[]	children = p.GetComponentsInChildren<GUIBase_Pivot>();
		
				foreach (GUIBase_Pivot c in children)
				{
					if (c == pivot)
					{
						return p;
					}
				}
			}
		}
		
		return null;
	}

	public static Vector2 Rect_Center2(Rect area)
	{
		return new Vector2((area.x + area.width * 0.5f), (area.y + area.height * 0.5f));
	}
	public static Vector2 Rect_Center3(Rect area)
	{
		return new Vector3((area.x + area.width * 0.5f), (area.y + area.height * 0.5f), 0.0f);
	}
	//---------------------------------------------------------------------------------------------
	//
	// Draw Rectangle (area) relatively to (posX, posY).
	// Also draws points to drag etc. if necessary.
	//
	//---------------------------------------------------------------------------------------------
	public static void DrawRect(Vector2 platformPos, Rect area, float rotAngle,
								float zoomFactor, Vector2 layoutShift, Color edgeColor)
	{
		DrawRect(platformPos, area, rotAngle, zoomFactor, layoutShift, true, edgeColor, GUIEditorUtils.COLOR_AREA_POINT, GUIEditorUtils.COLOR_AREA_SELECTED_POINT);
	}
	public static void DrawRect(Vector2 platformPos, Rect area, float rotAngle,
								float zoomFactor, Vector2 layoutShift,
								bool drawEdges,
	                            Color edgeColor, Color pointColor, Color selectedPointColor,
								bool drawCornerPoints = false, float pointSize = 1.0f, int selectedPointIdx = -1)
	{
		Vector3	centerPos    = Rect_Center3(area);
		DrawRect(platformPos, area, rotAngle, centerPos, zoomFactor, layoutShift, drawEdges, edgeColor, pointColor, selectedPointColor, drawCornerPoints, pointSize, selectedPointIdx);
	}

	public static void DrawRect(Vector2 platformPos, Rect area, float rotAngle, Vector3 rotOrigin,
								float zoomFactor, Vector2 layoutShift,
								bool drawEdges,
	                            Color edgeColor, Color pointColor, Color selectedPointColor,
								bool drawCornerPoints = false, float pointSize = 1.0f, int selectedPointIdx = -1)
	{
		Vector3 rectCenter = Rect_Center3(area);
		Vector3 offset = rotOrigin-rectCenter;

		rotOrigin  = rotOrigin  * zoomFactor + new Vector3(platformPos.x + layoutShift.x, platformPos.y + layoutShift.y, 0.0f);
		rectCenter = rectCenter * zoomFactor + new Vector3(platformPos.x + layoutShift.x, platformPos.y + layoutShift.y, 0.0f);

		Matrix4x4	tM			= new Matrix4x4();
		Quaternion	rotQuat		= new Quaternion(); 
		
		rotQuat.eulerAngles = new Vector3(0.0f, 0.0f, rotAngle);
		
		tM.SetTRS(rotOrigin, rotQuat, new Vector3(1.0f, 1.0f, 1.0f));
		
		Vector3	tmp_TL = (new Vector3(-area.width * 0.5f, -area.height * 0.5f, 0.0f) - offset) * zoomFactor;
		Vector3	tmp_TR = (new Vector3( area.width * 0.5f, -area.height * 0.5f, 0.0f) - offset) * zoomFactor;
		Vector3	tmp_BR = (new Vector3( area.width * 0.5f,  area.height * 0.5f, 0.0f) - offset) * zoomFactor;
		Vector3	tmp_BL = (new Vector3(-area.width * 0.5f,  area.height * 0.5f, 0.0f) - offset) * zoomFactor;

		Vector3 v_TL = tM.MultiplyPoint(tmp_TL);
		Vector3 v_TR = tM.MultiplyPoint(tmp_TR);
		Vector3 v_BR = tM.MultiplyPoint(tmp_BR);
		Vector3 v_BL = tM.MultiplyPoint(tmp_BL);

		// Draw points
		if (drawCornerPoints)
		{
			if (IsPointVisible(v_TL))
			{
				DrawPoint(v_TL, pointSize, (selectedPointIdx != 0) ? pointColor : selectedPointColor);
			}
			
			if (IsPointVisible(v_TR))
			{
				DrawPoint(v_TR, pointSize, (selectedPointIdx != 1) ? pointColor : selectedPointColor);
			}
			
			if (IsPointVisible(v_BR))
			{
				DrawPoint(v_BR, pointSize, (selectedPointIdx != 2) ? pointColor : selectedPointColor);
			}
			
			if (IsPointVisible(v_BL))
			{
				DrawPoint(v_BL, pointSize, (selectedPointIdx != 3) ? pointColor : selectedPointColor);
			}
		}
				
		// Draw edges
		if (drawEdges)
		{
			Handles.color = edgeColor;
			
			DrawClippedLine(v_TL, v_TR);
			DrawClippedLine(v_TR, v_BR);
			DrawClippedLine(v_BR, v_BL);
			DrawClippedLine(v_BL, v_TL);
		}
	}
	
	//---------------------------------------------------------------------------------------------
	//
	// Test if point of area was selected
	//
	//---------------------------------------------------------------------------------------------
	public static bool IsPointTouched(Vector2 mouseClickPos, Vector2 pos, float width, float height, float selectionThreshold)
	{
		Vector2		tmpPos		= new Vector2(pos.x + width, pos.y + height);
		
		if ((Mathf.Abs(tmpPos.x - mouseClickPos.x) <= selectionThreshold) &&
			(Mathf.Abs(tmpPos.y - mouseClickPos.y) <= selectionThreshold))
		{
			return true;
		}
		
		return false;
	}
	
	//---------------------------------------------------------------------------------------------
	//
	// Draw Point
	//
	//---------------------------------------------------------------------------------------------
	static void DrawPoint(Vector3 point, float size, Color color)
	{
		Handles.color		= color;
		float	halfSize	= size * 0.5f; 

		Handles.DrawLine(new Vector2 (point.x - halfSize, point.y - halfSize), new Vector2 (point.x - halfSize, point.y + halfSize));
		Handles.DrawLine(new Vector2 (point.x - halfSize, point.y + halfSize), new Vector2 (point.x + halfSize, point.y + halfSize));
		Handles.DrawLine(new Vector2 (point.x + halfSize, point.y + halfSize), new Vector2 (point.x + halfSize, point.y - halfSize));
		Handles.DrawLine(new Vector2 (point.x + halfSize, point.y - halfSize), new Vector2 (point.x - halfSize, point.y - halfSize));
	}
	
	//---------------------------------------------------------------------------------------------
	//
	// Clip and draw line
	//
	//---------------------------------------------------------------------------------------------
	public static void DrawClippedLine(Vector3 p0, Vector3 p1)
	{
		// slightly modified Cohen–Sutherland line clipping
		
		if (p0.x >= s_TopLeftCorner.x && p0.y >= s_TopLeftCorner.y &&
		    p1.x >= s_TopLeftCorner.x && p1.y >= s_TopLeftCorner.y)
		{
			// trivial accept
			Handles.DrawLine(p0, p1);
		}
		else if ((p0.x < s_TopLeftCorner.x && p1.x < s_TopLeftCorner.x) ||
		         (p0.y < s_TopLeftCorner.y && p1.y < s_TopLeftCorner.y))
		{
			// trivial reject
			return;
		}
		else
		{
			// clip
			if (p0.x < s_TopLeftCorner.x)
			{
				float	lx	= p1.x - p0.x;
				float	ly	= p1.y - p0.y;
				float	k	= (s_TopLeftCorner.x - p0.x) / lx;
				
				DrawClippedLine(new Vector3(lx*k + p0.x, ly*k + p0.y, 0.0f), p1);
			}
			else if (p1.x < s_TopLeftCorner.x)
			{
				float	lx	= p0.x - p1.x;
				float	ly	= p0.y - p1.y;
				float	k	= (s_TopLeftCorner.x - p1.x) / lx;
				
				DrawClippedLine(p0, new Vector3(lx*k + p1.x, ly*k + p1.y, 0.0f));
			}
			else if (p0.y < s_TopLeftCorner.y)
			{
				float	lx	= p1.x - p0.x;
				float	ly	= p1.y - p0.y;
				float	k	= (s_TopLeftCorner.y - p0.y) / ly;
				
				DrawClippedLine(new Vector3(lx*k + p0.x, ly*k + p0.y, 0.0f), p1);
			}
			else if (p1.y < s_TopLeftCorner.y)
			{
				float	lx	= p0.x - p1.x;
				float	ly	= p0.y - p1.y;
				float	k	= (s_TopLeftCorner.y - p1.y) / ly;
				
				DrawClippedLine(p0, new Vector3(lx*k + p1.x, ly*k + p1.y, 0.0f));
			}
		}
	}
	
	//---------------------------------------------------------------------------------------------
	//
	// IsPointVisible
	//	returns true if point is inside "visible area" (right down from leftTopClipPoint)
	//
	//---------------------------------------------------------------------------------------------
	static bool IsPointVisible(Vector3 pos)
	{
		return (pos.x >= s_TopLeftCorner.x && pos.y >= s_TopLeftCorner.y);
	}
	
#region Deprececated Unity interface

	///Keeps the compatibility with already deprecated GUIEditorUtils.LookLikeControls();
	public static void LookLikeControls()
	{
		//The implementation still keeps the compatibility with the old system when the 0 value is provided.
		EditorGUIUtility.labelWidth = 0f;	//the real value should be 150
		EditorGUIUtility.fieldWidth = 0f;	//the real value should be 50
	}

	///Keeps the compatibility with already deprecated GUIEditorUtils.LookLikeControls(float labelWidth);
	public static void LookLikeControls(float labelWidth)
	{
		//The implementation still keeps the compatibility with the old system when the 0 value is provided.
		EditorGUIUtility.labelWidth = labelWidth;
		EditorGUIUtility.fieldWidth = 0f;	//the real value should be 50
	}

	///Keeps the compatibility with already deprecated GUIEditorUtils.LookLikeInspector(float labelWidth);
	public static void LookLikeInspector()
	{
		EditorGUIUtility.labelWidth = 0f;	//the real value should be 150
		EditorGUIUtility.fieldWidth = 0f;	//the real value should be 50
	}

	/// <summary>
	/// This function is just a warning-less wrapper around the already deprecated Undo.RegisterSceneUndo editor method.
	/// The problem is, that the original code can't be fixed in an easy way as the Undo logic changed completely.
	/// The old logic defines some global restore points (Scene undos) while the new one records various specific events
	/// like per-object creation, deletion, modification etc.
	/// 
	/// The problem is that no one is realy using the affected tools these days but at the same time we would like
	/// to keep them (maybe someone will need them in the future) but we need to beat these disturbing warnings.
	/// </summary>
	public static void RegisterSceneUndo(string description)
	{
#pragma warning disable 618
		Undo.RegisterSceneUndo(description);
#pragma warning restore 618
	}

#endregion
}