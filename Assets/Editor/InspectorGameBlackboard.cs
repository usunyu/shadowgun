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
using System.Collections.Generic;

/*
  /*  public override void OnInspectorGUI()
    {
        Cover c = target as Cover;

        EditorGUILayout.LabelField("Cover Flags", "Set Cover options" );
        EditorGUI.indentLevel++;
        GUILayout.BeginHorizontal();
        GUILayout.Space(90);
        EditorGUILayout.LabelField("Left", "", GUILayout.Width(40));
        EditorGUILayout.LabelField("Middle", "", GUILayout.Width(40));
        EditorGUILayout.LabelField("Right", "", GUILayout.Width(40));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        EditorGUI.indentLevel++;
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Crouch", "", GUILayout.Width(0));
        c.CoverFlags.Set((int)Cover.E_CoverFlags.LeftCrouch, EditorGUILayout.Toggle("", c.CoverFlags.Get((int)Cover.E_CoverFlags.LeftCrouch), GUILayout.Width(40)));
        c.CoverFlags.Set((int)Cover.E_CoverFlags.UpCrouch, EditorGUILayout.Toggle("", c.CoverFlags.Get((int)Cover.E_CoverFlags.UpCrouch),GUILayout.Width(40)));
        c.CoverFlags.Set((int)Cover.E_CoverFlags.RightCrouch, EditorGUILayout.Toggle("", c.CoverFlags.Get((int)Cover.E_CoverFlags.RightCrouch),GUILayout.Width(40)));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Stand", "", GUILayout.Width(0));
        c.CoverFlags.Set((int)Cover.E_CoverFlags.LeftStand, EditorGUILayout.Toggle("", c.CoverFlags.Get((int)Cover.E_CoverFlags.LeftStand), GUILayout.Width(40)));
        GUILayout.Space(45);
        c.CoverFlags.Set((int)Cover.E_CoverFlags.RightStand, EditorGUILayout.Toggle("", c.CoverFlags.Get((int)Cover.E_CoverFlags.RightStand), GUILayout.Width(40)));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        EditorGUI.indentLevel--;
        EditorGUI.indentLevel--;


        if (GUI.changed)
            EditorUtility.SetDirty(target);
    }

 */
[CustomEditor(typeof(GameBlackboard))]
internal class InspectorGameBlackboard : Editor
{
    public override void OnInspectorGUI()
    {
        GameBlackboard gb = target as GameBlackboard;

        if (Application.isPlaying)
        {
            OnInspectorGuiEx();
            return;
        }

        const float kFrameWidth = 80;
        const float kDeleteWidth = 17;

        GUILayout.BeginVertical("OL box NoExpand");
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Game Events", "OL Title");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Name", "OL Title");
        GUILayout.Label("State", "OL Title", GUILayout.Width(kFrameWidth));
        GUILayout.Label(GUIContent.none, "OL Title", GUILayout.Width(kDeleteWidth));
        GUILayout.EndHorizontal();


        GUIStyle gs = "OL TextField";

        GameEvents events = gb.GameEvents;


        Dictionary<string, GameEvents.E_State> updatedEvents = new Dictionary<string, GameEvents.E_State>();

        
        
        for(int i = 0; i < events.Count;i++)
        {
            EditorGUILayout.BeginHorizontal();

            string key = EditorGUILayout.TextField(events.Names[i], gs, GUILayout.MinWidth(30));
            GameEvents.E_State state = (GameEvents.E_State)EditorGUILayout.EnumPopup(events.GetState(events.Names[i]), GUILayout.Width(kFrameWidth));

            if (GUILayout.Button(GUIContent.none, "OL Minus", GUILayout.Width(kDeleteWidth)) == false)
            {
                try { updatedEvents.Add(key, state); }
                catch { updatedEvents.Add(events.Names[i], events.GetState(events.Names[i])); }
            }

            EditorGUILayout.EndHorizontal();
        }

        GUILayout.Space(5);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button(GUIContent.none, "OL Plus", GUILayout.Width(kDeleteWidth)))
        {
            updatedEvents.Add("_NewEvent" + events.Count , GameEvents.E_State.False);
        }

        if (GUI.changed || events.Count != updatedEvents.Count)
        {
            //gb.ClearAllGameEvents();

            events.Clear();

            foreach (KeyValuePair<string, GameEvents.E_State> pair in updatedEvents)
                events.Add(pair.Key, pair.Value);

            EditorUtility.SetDirty(gb);
        }


        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    void OnInspectorGuiEx()
    {
        GameBlackboard gb = target as GameBlackboard;

        const float kFrameWidth = 80;

        EditorGUILayout.Space();
        GUILayout.BeginHorizontal();
        GUILayout.Space(5);
        GUILayout.BeginVertical();
        // Column headers
        GUILayout.BeginHorizontal();
        GUILayout.Label("Name", "OL Title");
        GUILayout.Label("State", "OL Title", GUILayout.Width(kFrameWidth));
        GUILayout.EndHorizontal();


        GUIStyle gs = "OL TextField";

        GameEvents events = gb.GameEvents;

        GUILayout.BeginVertical("OL box NoExpand");

        for (int i = 0; i < events.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(events.Names[i], gs, GUILayout.MinWidth(30));
            GameEvents.E_State s = (GameEvents.E_State)EditorGUILayout.EnumPopup(events.GetState(events.Names[i]), GUILayout.Width(kFrameWidth));

            if(s != events.GetState(events.Names[i]))
                events.Update(events.Names[i],s);

            EditorGUILayout.EndHorizontal();
        }

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        EditorGUILayout.Space();


    }
}
