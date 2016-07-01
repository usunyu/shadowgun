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

/// C# example
using UnityEditor;
using UnityEngine;

class GizmoCover
{
	static public bool ShowNotSelectedCovers = false;
	/// The RenderLightGizmo function will be called if the light is not selected.
	/// The gizmo is drawn when picking.
	[DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.Pickable)]
	static void DrawGizmoCover (Cover cover, GizmoType gizmoType)
	{
		if(ShowNotSelectedCovers == false)
			return;

        Cover c = cover;
        Vector3  center = c.transform.position;
        Vector3  right = c.transform.right * c.transform.lossyScale.x * 0.5f;
        Vector3  up = c.transform.up * c.transform.lossyScale.y;

        Vector3[] verts = { center + up - right, center + up + right, center + right, center - right };
        Handles.DrawSolidRectangleWithOutline(verts,new Color(1,1,1,0.2f), new Color(0,0,0,1));

		Color oldColor = Handles.color;
        Handles.color = Color.blue;

        if (c.CoverFlags.Get(Cover.E_CoverFlags.LeftCrouch))
        {
            Handles.ArrowCap(0, center - (right * 1.1f) + (Vector3.up * 1.0f), c.transform.rotation, 0.5f);
        }
        if (c.CoverFlags.Get(Cover.E_CoverFlags.LeftStand))
        {
            Handles.ArrowCap(0, center - (right * 1.1f) + (Vector3.up * 1.8f), c.transform.rotation, 0.5f);
        }
        if (c.CoverFlags.Get(Cover.E_CoverFlags.RightCrouch))
        {
            Handles.ArrowCap(0, center + (right * 1.1f) + (Vector3.up * 1.0f), c.transform.rotation, 0.5f);
        }
        if (c.CoverFlags.Get(Cover.E_CoverFlags.RightStand))
        {
            Handles.ArrowCap(0, center + (right * 1.1f) + (Vector3.up * 1.8f), c.transform.rotation, 0.5f);
        }
        if (c.CoverFlags.Get(Cover.E_CoverFlags.UpCrouch))
        {
            Handles.ArrowCap(0, center + (Vector3.up * 1.3f), c.transform.rotation, 0.5f);
        }

        Handles.color = oldColor;
	}
/*
// Draw the gizmo if it is selected or a child of the selection.
// This is the most common way to render a gizmo
[DrawGizmo (GizmoType.SelectedOrChild)]
*/
	// Draw the gizmo only if it is the active object.
	[DrawGizmo (GizmoType.Active)]
	static void DrawGizmoCover_Active(Cover cover, GizmoType gizmoType)
	{
        Cover c = cover;
        Vector3  center = c.transform.position;
        Vector3  right = c.transform.right * c.transform.lossyScale.x * 0.5f;
        Vector3  up = c.transform.up * c.transform.lossyScale.y;

        Vector3[] verts = { center + up - right, center + up + right, center + right, center - right };
        Handles.DrawSolidRectangleWithOutline(verts, new Color(1,1,0,0.2f), new Color(0,0,0,1));

		Color oldColor = Handles.color;
        Handles.color = Color.blue;

        if (c.CoverFlags.Get(Cover.E_CoverFlags.LeftCrouch))
        {
            Handles.ArrowCap(0, center - (right * 1.1f) + (Vector3.up * 1.0f), c.transform.rotation, 0.5f);
        }
        if (c.CoverFlags.Get(Cover.E_CoverFlags.LeftStand))
        {
            Handles.ArrowCap(0, center - (right * 1.1f) + (Vector3.up * 1.8f), c.transform.rotation, 0.5f);
        }
        if (c.CoverFlags.Get(Cover.E_CoverFlags.RightCrouch))
        {
            Handles.ArrowCap(0, center + (right * 1.1f) + (Vector3.up * 1.0f), c.transform.rotation, 0.5f);
        }
        if (c.CoverFlags.Get(Cover.E_CoverFlags.RightStand))
        {
            Handles.ArrowCap(0, center + (right * 1.1f) + (Vector3.up * 1.8f), c.transform.rotation, 0.5f);
        }
        if (c.CoverFlags.Get(Cover.E_CoverFlags.UpCrouch))
        {
            Handles.ArrowCap(0, center + (Vector3.up * 1.3f), c.transform.rotation, 0.5f);
        }

        Handles.color = oldColor;
	}
}