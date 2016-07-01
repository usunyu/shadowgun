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
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(AgentDebugInfo))]
internal class InspectorAgentDebugInfo : Editor
{
    static bool ShowBase = true;
    static bool MoveInfo = true;
    static bool ShowGoap = true;
    static bool ShowWorldState = true;
    static bool ShowAnim = true;

    public override void OnInspectorGUI()
    {
        DrawBase();
        EditorGUILayout.Space();
    }

    void DrawBase()
    {
        AgentDebugInfo debugInfo = target as AgentDebugInfo;
        AgentHuman agent =  debugInfo.GetComponent<AgentHuman>();
		
		if (agent.Transform == null)
			return;
		
        BlackBoard bb = agent.BlackBoard;

        GUILayout.BeginVertical();
        //GUILayout.Label("Base Settings", "OL Title" );
        ShowBase = EditorGUILayout.Foldout(ShowBase, "Base Setings ");

        if (ShowBase && agent.WeaponComponent)
        {
            EditorGUILayout.FloatField("Health ", bb.Health, GUILayout.Width(250), GUILayout.ExpandWidth(false));
            EditorGUILayout.EnumPopup("Weapon", agent.WeaponComponent.CurrentWeapon, GUILayout.Width(250), GUILayout.ExpandWidth(false));

            EditorGUILayout.ObjectField("Melee Target ", bb.Desires.MeleeTarget, typeof(GameObject), true, GUILayout.Width(250), GUILayout.ExpandWidth(false));
        }

        GUILayout.Space(10);
        GUILayout.EndVertical();


        GUILayout.BeginVertical();
        MoveInfo = EditorGUILayout.Foldout(MoveInfo, "Move info ");

        if (MoveInfo)
        {
            EditorGUILayout.EnumPopup("Motion", agent.BlackBoard.MotionType, GUILayout.Width(250), GUILayout.ExpandWidth(false));
            EditorGUILayout.EnumPopup("Move", agent.BlackBoard.MoveType, GUILayout.Width(250), GUILayout.ExpandWidth(false));
            EditorGUILayout.FloatField("Speed ", agent.BlackBoard.Speed, GUILayout.Width(250), GUILayout.ExpandWidth(false));
			EditorGUILayout.Vector3Field("Rotation", agent.Forward, GUILayout.Width(250), GUILayout.ExpandWidth(false));
			EditorGUILayout.Vector3Field("Fire Dir", agent.BlackBoard.FireDir, GUILayout.Width(250), GUILayout.ExpandWidth(false));
			
        }

        GUILayout.Space(10);
        GUILayout.EndVertical();
        
        GUILayout.BeginVertical();
        ShowGoap = EditorGUILayout.Foldout(ShowGoap, "Goap Manager ");

        if (ShowGoap)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", "OL Title", GUILayout.Width(100));
            GUILayout.Label("Relevancy", "OL Title", GUILayout.Width(60));
            GUILayout.Label("Enabled", "OL Title", GUILayout.Width(60));
            GUILayout.EndHorizontal();

            foreach (E_GOAPGoals key in System.Enum.GetValues(typeof(E_GOAPGoals)))
            {
                GOAPGoal goal = agent.GetGOAPGoal(key);
                if (goal == null)
                    continue;

                GUILayout.BeginHorizontal();

                if (goal.Active)
                    GUILayout.Label(goal.GoalType.ToString(), EditorStyles.whiteLabel, GUILayout.Width(100), GUILayout.ExpandWidth(false));
                else
                    GUILayout.Label(goal.GoalType.ToString(), GUILayout.Width(100), GUILayout.ExpandWidth(false));

                EditorGUILayout.LabelField(goal.GoalRelevancy.ToString(), "", GUILayout.Width(60), GUILayout.ExpandWidth(false));
                EditorGUILayout.LabelField((goal.IsDisabled() == false).ToString(), "", GUILayout.Width(60), GUILayout.ExpandWidth(false));
                GUILayout.EndHorizontal();

            }

        }
        GUILayout.Space(10);

        GUILayout.EndVertical();

        
        GUILayout.BeginVertical();
        ShowWorldState = EditorGUILayout.Foldout(ShowWorldState, "World States ");

        if (ShowWorldState && agent.WorldState != null)
        {
            for (E_PropKey key = E_PropKey.Start; key < E_PropKey.Count; key ++ )
            {
                if (agent.WorldState == null)
                    continue;

                WorldStateProp prop = agent.WorldState.GetWSProperty(key);

                if (prop == null)
                    continue;

                switch (prop.PropType)
                {
                    case E_PropType.Bool:
                        EditorGUILayout.LabelField(prop.PropName, prop.GetBool().ToString(), GUILayout.Width(250), GUILayout.ExpandWidth(false));
                        break;
                    case E_PropType.Int:
                        EditorGUILayout.LabelField(prop.PropName, prop.GetBool().ToString(), GUILayout.Width(250), GUILayout.ExpandWidth(false));
                        break;
                    case E_PropType.Float:
                        EditorGUILayout.LabelField(prop.PropName, prop.GetFloat().ToString(), GUILayout.Width(250), GUILayout.ExpandWidth(false));
                        break;
                    case E_PropType.Vector:
                        EditorGUILayout.LabelField(prop.PropName, prop.GetVector().ToString(), GUILayout.Width(250), GUILayout.ExpandWidth(false));
                        break;
                    case E_PropType.Agent:
                        EditorGUILayout.LabelField(prop.PropName, prop.GetAgent().name, GUILayout.Width(250), GUILayout.ExpandWidth(false));
                        break;
                    case E_PropType.CoverState:
                        EditorGUILayout.LabelField(prop.PropName, prop.GetCoverState().ToString(), GUILayout.Width(250), GUILayout.ExpandWidth(false));
                        break;
                    default:
                        Debug.LogError("Unknow prop type " + prop.PropType);
                        break;
                }

            }
        }
        GUILayout.Space(10);
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        
        ShowAnim = EditorGUILayout.Foldout(ShowAnim, "Animations");

        if (ShowAnim && agent.AnimComponent != null)
        {
            EditorGUILayout.LabelField("Current State: ",  agent.AnimComponent.CurrentAnimState != null? agent.AnimComponent.CurrentAnimState.ToString() : " none", GUILayout.Width(250), GUILayout.ExpandWidth(false));


            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", "OL Title", GUILayout.Width(200));
            GUILayout.Label("Layer", "OL Title", GUILayout.Width(40));
            GUILayout.Label("Weight", "OL Title", GUILayout.Width(100));
            GUILayout.Label("Time", "OL Title", GUILayout.Width(100));
            GUILayout.EndHorizontal();

            if (Application.isPlaying)
            {
                foreach (AnimationState state in agent.GetComponent<Animation>())
                {
                    if (agent.GetComponent<Animation>().IsPlaying(state.clip.name) == false)
                        continue;

                    GUILayout.BeginHorizontal();

                    GUILayout.Label(state.name, GUILayout.Width(200), GUILayout.ExpandWidth(false));

                    EditorGUILayout.LabelField(state.layer.ToString(), "", GUILayout.Width(40), GUILayout.ExpandWidth(false));
                    EditorGUILayout.LabelField(state.weight.ToString(), "", GUILayout.Width(100), GUILayout.ExpandWidth(false));
                    EditorGUILayout.LabelField(state.time.ToString(), "", GUILayout.Width(100), GUILayout.ExpandWidth(false));
                    GUILayout.EndHorizontal();
                }
            }
        }
        GUILayout.Space(10);

        GUILayout.EndVertical();
/*
        GUILayout.BeginVertical();
        EditorGUI.indentLevel++;
        GUILayout.Label("User Settings", "OL Title");
        interaction.EntryTransform = EditorGUILayout.ObjectField("Entry position", interaction.EntryTransform, typeof(Transform), true, GUILayout.Width(250), GUILayout.ExpandWidth(false)) as Transform;
        interaction.LeaveTransform = EditorGUILayout.ObjectField("Leave position", interaction.LeaveTransform, typeof(Transform), true, GUILayout.Width(250), GUILayout.ExpandWidth(false)) as Transform;
        interaction.UserAnimationClip = EditorGUILayout.ObjectField("User animation", interaction.UserAnimationClip, typeof(AnimationClip), false, GUILayout.Width(250), GUILayout.ExpandWidth(false)) as AnimationClip;
        EditorGUI.indentLevel--;
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        EditorGUI.indentLevel++;
        GUILayout.Label("Cutscene Settings", "OL Title");
        interaction.CutsceneCamera = EditorGUILayout.ObjectField("Camera", interaction.CutsceneCamera, typeof(GameObject), true, GUILayout.Width(250), GUILayout.ExpandWidth(false)) as GameObject;
        interaction.CameraAnim = EditorGUILayout.ObjectField("Animation", interaction.CameraAnim, typeof(AnimationClip), true, GUILayout.Width(250), GUILayout.ExpandWidth(false)) as AnimationClip;
        interaction.FadeInTime= EditorGUILayout.FloatField("Fade In", interaction.FadeInTime, GUILayout.Width(250), GUILayout.ExpandWidth(false));
        interaction.FadeOutTime = EditorGUILayout.FloatField("Fade out", interaction.FadeOutTime, GUILayout.Width(250), GUILayout.ExpandWidth(false));
        EditorGUI.indentLevel--;
        GUILayout.EndVertical();*/

    }

    public void OnSceneGUI()
    {
        AgentDebugInfo debugInfo = target as AgentDebugInfo;
        AgentHuman agent = debugInfo.GetComponent<AgentHuman>();

        if (agent.BlackBoard.Desires.CoverNear.Cover)
        {
            Handles.color = Color.white;
            if (agent.BlackBoard.Desires.CoverNear.LeftCoverValidity > 0)
                Handles.SphereCap(0, agent.BlackBoard.Desires.CoverNear.Cover.LeftEdge, agent.BlackBoard.Desires.CoverNear.Cover.Transform.rotation, 0.3f);
            if (agent.BlackBoard.Desires.CoverNear.RightCoverValidity > 0)
                Handles.SphereCap(0, agent.BlackBoard.Desires.CoverNear.Cover.RightEdge, agent.BlackBoard.Desires.CoverNear.Cover.Transform.rotation, 0.3f);
            if (agent.BlackBoard.Desires.CoverNear.MiddleCoverValidity > 0)
                Handles.SphereCap(0, agent.BlackBoard.Desires.CoverNear.Cover.Position, agent.BlackBoard.Desires.CoverNear.Cover.Transform.rotation, 0.3f);
        }
        if (agent.BlackBoard.Desires.CoverSelected)
        {
            Handles.color = Color.green;
            if (agent.BlackBoard.Desires.CoverPosition == E_CoverDirection.Left)
                Handles.SphereCap(0, agent.BlackBoard.Desires.CoverSelected.LeftEdge, agent.BlackBoard.Desires.CoverSelected.Transform.rotation, 0.32f);
            else if (agent.BlackBoard.Desires.CoverPosition == E_CoverDirection.Right)
                Handles.SphereCap(0, agent.BlackBoard.Desires.CoverSelected.RightEdge, agent.BlackBoard.Desires.CoverSelected.Transform.rotation, 0.32f);
            else
                Handles.SphereCap(0, agent.BlackBoard.Desires.CoverSelected.Position, agent.BlackBoard.Desires.CoverSelected.Transform.rotation, 0.32f);
        }
    }

}
