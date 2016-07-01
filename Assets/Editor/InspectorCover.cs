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
using UnityEditor;

[CustomEditor(typeof(Cover))]
internal class InspectorCover : Editor
{
	ArrayInspector< Transform > PartsInspector = new ArrayInspector< Transform >();
	
	private void OnCoverList( Cover.AgentsList List, float minW )
	{
		GUILayout.BeginVertical();
		
		if( List.Count > 0)
		{
			foreach( AgentHuman Agent in List )
			{
				EditorGUILayout.LabelField( Agent != null ? ( Agent.name ) : "none", "" , GUILayout.MinWidth(minW));
			}
		}
		else
		{
			EditorGUILayout.LabelField( "none", "", GUILayout.MinWidth(minW));
		}
		
		GUILayout.EndVertical();
	}
	
    public override void OnInspectorGUI()
    {
        Cover c = target as Cover;

        EditorGUILayout.LabelField("Base", "Set Cover options");
        EditorGUILayout.Space();
        c.CanJumpUp = EditorGUILayout.Toggle("Can jump up:", c.CanJumpUp);


        EditorGUILayout.LabelField("Cover Flags", "Set Cover options" );
        EditorGUILayout.Space();
		
		int space = 70;
		int minW = 10;
        
        GUILayout.BeginHorizontal();
        GUILayout.Space( space );
        EditorGUILayout.LabelField("Left", "", GUILayout.MinWidth(minW));
		GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Middle", "", GUILayout.MinWidth(minW));
		GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Right", "", GUILayout.MinWidth(minW));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Stand", "", GUILayout.Width( space ) );
        c.CoverFlags.Set(Cover.E_CoverFlags.LeftStand, EditorGUILayout.Toggle( "", c.CoverFlags.Get( Cover.E_CoverFlags.LeftStand ), GUILayout.MinWidth(minW)) );
		GUILayout.FlexibleSpace();
		GUI.enabled = false;
		EditorGUILayout.Toggle( "", false, GUILayout.MinWidth(minW) );
		GUI.enabled = true;
		GUILayout.FlexibleSpace();
        c.CoverFlags.Set(Cover.E_CoverFlags.RightStand, EditorGUILayout.Toggle( "", c.CoverFlags.Get( Cover.E_CoverFlags.RightStand ), GUILayout.MinWidth(minW) ) );
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();


        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Crouch", "", GUILayout.Width( space ) );
        c.CoverFlags.Set(Cover.E_CoverFlags.LeftCrouch, EditorGUILayout.Toggle("", c.CoverFlags.Get(Cover.E_CoverFlags.LeftCrouch), GUILayout.MinWidth(minW)));
		GUILayout.FlexibleSpace();
        c.CoverFlags.Set(Cover.E_CoverFlags.UpCrouch, EditorGUILayout.Toggle("", c.CoverFlags.Get(Cover.E_CoverFlags.UpCrouch), GUILayout.MinWidth(minW)));
		GUILayout.FlexibleSpace();
        c.CoverFlags.Set(Cover.E_CoverFlags.RightCrouch, EditorGUILayout.Toggle("", c.CoverFlags.Get(Cover.E_CoverFlags.RightCrouch), GUILayout.MinWidth(minW)));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField( "Locked by", "", GUILayout.Width( space ) );
		
		OnCoverList( c.LeftAgents, minW );
		GUILayout.FlexibleSpace();
		OnCoverList( c.MiddleAgents, minW );
		GUILayout.FlexibleSpace();
		OnCoverList( c.RightAgents, minW );
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

		PartsInspector.m_CreateItem  = TransformCreate;
		PartsInspector.m_DisplayItem = TransformDisplay;
		PartsInspector.Display( "Additional Cover Parts...", ref c.m_Parts );

		if (GUI.changed)
			EditorUtility.SetDirty(target);
	}

	private static Transform TransformCreate()
	{
		return null;
	}

	private static void TransformDisplay( int Index, ref Transform T )
	{
		GUIEditorUtils.LookLikeInspector();
		T = (Transform) EditorGUILayout.ObjectField( "Part #" + Index, T, typeof(Transform), true );
		GUIEditorUtils.LookLikeControls();
	}

    public void OnSceneGUI()
    {
	 		// moved into GizmoCover
	 		/*
        Cover c = target as Cover;
        Vector3  center = c.transform.position;
        Vector3  right = c.transform.right * c.transform.lossyScale.x * 0.5f;
        Vector3  up = c.transform.up * c.transform.lossyScale.y;

        Vector3[] verts = { center + up - right, center + up + right, center + right, center - right };
        Handles.DrawSolidRectangleWithOutline(verts,new Color(1,1,1,0.2f), new Color(0,0,0,1));

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
		  */
    }
}
