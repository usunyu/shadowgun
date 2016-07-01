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

using E_LevelOp   = LevelToolsData.E_LevelOperation;
using T_OpItem    = LevelToolsData.ObjectModification;
using E_ObjectOp  = LevelToolsData.E_ObjectModification;

public class LevelToolsEditor : EditorWindow {

    public GUIContent[] LevelOperationNames = {
        new GUIContent("LightMaps"),
        new GUIContent("PVS"),
        new GUIContent("NavMesh"),
    };



    private LevelToolsData          m_ToolsData;
    private T_OpItem                m_NewOpItem    = new T_OpItem();
    private E_LevelOp               m_Mode         = E_LevelOp.Lightening;
    private bool                    m_ValidScene   = false;
    private string                  m_Scene;


	// Add menu item to the Window menu
	[MenuItem ("Window/Level Tools")]
	static void Init () {
		// Get existing open window or if none, make a new one:
		//EditorWindow.GetWindow<SelectionTools> (false, "Selection Tools");
		EditorWindow.GetWindow<LevelToolsEditor> (false, "Level Tools");
	}

    internal void Initialize()
    {
        m_ToolsData = null;
        m_ValidScene = false;

        GameObject game = GameObject.Find("hra");
        if(game != null)
        {
            m_ValidScene = true;
            m_ToolsData  = game.GetComponent<LevelToolsData>();
            // dont create until we need it.
//            if(m_ToolsData == null)
//            {
//                m_ToolsData = game.AddComponent<LevelToolsData>() as LevelToolsData;
//            }
        }
        else
        {
            Debug.LogWarning("LevelTools.Initialize - I can't find GameObject with name 'hra'");
        }

        m_Scene = EditorApplication.currentScene;
    }

    void OnEnable()
    {
        Initialize();
    }

    void Update()
    {
        if(m_Scene != EditorApplication.currentScene)
        {
            Initialize();
            this.Repaint();
        }
    }

	// Implement your own editor GUI here.
	void OnGUI () {

        if(m_ValidScene == false)
            return;

        if(m_ToolsData == null)
        {
            m_Mode = (E_LevelOp)GUILayout.Toolbar ((int)m_Mode, LevelOperationNames, "LargeButton");
            VizualizeEmptyDialog();
            return;
        }

        bool orig_gui_enabled = GUI.enabled;
        GUI.enabled = (m_ToolsData.m_ActiveOperation == E_LevelOp.None);

        m_Mode = (E_LevelOp)GUILayout.Toolbar ((int)m_Mode, LevelOperationNames, "LargeButton");

        GUI.changed = false;

        VizualizeList(GetListByMode(m_Mode));
        /*
        switch(m_Mode)
        {
            case E_LevelOp.Lightening:
                VizualizeList(ref m_ToolsData.m_LighteningMod);
                break;
            case E_LevelOp.PVS:
                VizualizeList(ref m_ToolsData.m_PVSMod);
                break;
            case E_LevelOp.Navmesh:
                VizualizeList(ref m_ToolsData.m_NavMeshMod);
                break;
            default:
                Debug.LogError("LevelTools.OnGUI - invalid operation mode");
                break;
        };
         */
        if(GUI.changed == true)
        {
            //Debug.Log("Gui change ...");
            EditorUtility.SetDirty(m_ToolsData);
        }

        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if(GUILayout.Button("Clear",GUILayout.Width(100)) == true)
            {
                GetListByMode(m_Mode).Clear();
                EditorUtility.SetDirty(m_ToolsData);
            }

            GUILayout.Space(50);

            // Do actual selected operation
            if(GUILayout.Button("DoIt",GUILayout.Width(100)) == true)
            {
                DoIt(GetListByMode(m_Mode), m_ToolsData.m_BackupDtata);
                m_ToolsData.m_ActiveOperation = m_Mode;
            }

            GUI.enabled = !GUI.enabled;
            if(GUILayout.Button("Back", GUILayout.Width(100)) == true)
            {
                BackDoIt(m_ToolsData.m_BackupDtata);
                m_ToolsData.m_BackupDtata.Clear();
                m_ToolsData.m_ActiveOperation = E_LevelOp.None;
            }
        EditorGUILayout.EndHorizontal();

        GUI.enabled = orig_gui_enabled;
	}


    private void ShowEditorOperation(T_OpItem inModification)
    {
        bool disableModificators = inModification.m_GameObject == null;
        bool orig_gui_enabled = true;
        EditorGUILayout.BeginHorizontal();
            inModification.m_GameObject    = EditorGUILayout.ObjectField(inModification.m_GameObject, typeof(GameObject), true) as GameObject;
            orig_gui_enabled = GUI.enabled;
            GUI.enabled = orig_gui_enabled && !disableModificators;
            inModification.m_Operation     = (E_ObjectOp)EditorGUILayout.EnumPopup(inModification.m_Operation, GUILayout.Width(100));
            inModification.m_ApplyOnChild  = GUILayout.Toggle(inModification.m_ApplyOnChild, "Childs", GUILayout.Width(100));
            GUI.enabled = orig_gui_enabled;
        EditorGUILayout.EndHorizontal();
    }

    private List<T_OpItem> GetListByMode(E_LevelOp inMode)
    {
        switch(inMode)
        {
            case E_LevelOp.Lightening:
                return m_ToolsData.m_LighteningMod;
            case E_LevelOp.PVS:
                return m_ToolsData.m_PVSMod;
            case E_LevelOp.Navmesh:
                return m_ToolsData.m_NavMeshMod;
            default:
                Debug.LogError("LevelTools.OnGUI - invalid operation mode");
                return null;
        };
    }

    public void VizualizeList(List<T_OpItem> inList)
    {
        if(inList != null) {
            for (int i = 0; i < inList.Count; i++) {
                ShowEditorOperation(inList[i]);
                if(inList[i].m_GameObject == null)
                {
                    inList.RemoveAt(i); i--;
                }
            }
        }

        ShowEditorOperation(m_NewOpItem);
        if(m_NewOpItem.m_GameObject != null)
        {
            inList.Add(m_NewOpItem);
            m_NewOpItem = new T_OpItem();
        }
    }

    private void VizualizeEmptyDialog()
    {
        ShowEditorOperation(m_NewOpItem);
        if(m_NewOpItem.m_GameObject != null)
        {
            CreateToolData();
            GetListByMode(m_Mode).Add(m_NewOpItem);
            m_NewOpItem = new T_OpItem();
        }
    }

    private void CreateToolData()
    {
        MFDebugUtils.Assert(m_ToolsData  == null);
        MFDebugUtils.Assert(m_ValidScene == true);

        GameObject game = GameObject.Find("hra");
        MFDebugUtils.Assert(game != null);

        m_ToolsData  = game.GetComponent<LevelToolsData>();
        MFDebugUtils.Assert(m_ToolsData  == null);
        if(m_ToolsData == null)
        {
            m_ToolsData = game.AddComponent<LevelToolsData>() as LevelToolsData;
        }
    }


    private void DoIt(List<T_OpItem> inList, List<T_OpItem> inBackup)
    {
        foreach (T_OpItem item in inList)
            DoModification(item.m_GameObject, item.m_Operation, item.m_ApplyOnChild, inBackup);
    }

    private void BackDoIt(List<T_OpItem> inList)
    {
        for (int i = inList.Count-1; i >= 0; --i )
            DoModification(inList[i].m_GameObject, LevelToolsData.GetOpposite(inList[i].m_Operation), inList[i].m_ApplyOnChild, null);
    }

    private void DoModification(GameObject inGameObject, E_ObjectOp inOperation, bool inRecursive, List<T_OpItem> inBackup)
    {
        if(inGameObject == null)
            return;

        bool changed = false;
        switch(inOperation)
        {
            case E_ObjectOp.Disable:
                if(inGameObject.activeSelf != false)
                {
                    inGameObject.SetActive(false);
                    changed = true;
                }
                break;
            case E_ObjectOp.Enable:
                if(inGameObject.activeSelf != true)
                {
                    inGameObject.SetActive(true);
                    changed = true;
                }
                break;
            case E_ObjectOp.Static:
                if(inGameObject.isStatic != true)
                {
                    inGameObject.isStatic = true;
                    changed = true;
                }
                break;
            case E_ObjectOp.Dynamic:
                if(inGameObject.isStatic != false)
                {
                    inGameObject.isStatic = false;
                    changed = true;
                }
                break;
            default:
                Debug.LogError("LevelTools.DoModification - invalid object operation");
                break;
        }

        if(inBackup != null && changed == true)
        {
            inBackup.Add(new T_OpItem( inGameObject, inOperation, false) );
        }

        if(inRecursive == false)
            return;

		Transform trans = inGameObject.transform;
        foreach (Transform child in trans)
            DoModification(child.gameObject, inOperation, inRecursive, inBackup);
    }


	/*
	private void ChangeVisibility(bool inValue, bool inRecursive)
	{
		foreach (Transform transform in Selection.transforms) 
		{
			// toggles the visibility of this gameobject and all it's children
			GameObject go = transform.gameObject;
			if(inRecursive == true)
			{
			    Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
				Undo.RecordObject(renderers, go.name + (inValue ? " Show" : " Hide"));
			    foreach (Renderer r in renderers) {
			        r.enabled = inValue;
			    }
			}
			else
			{
			    Renderer renderer = go.GetComponent<Renderer>();
				if(renderer != null)
				{
					Undo.RecordObject(renderer, go.name + (inValue ? " Show" : " Hide"));
			        renderer.enabled = inValue;
				}
			}
		}
	}
	
	private void ChangeActivity(bool inValue, bool inRecursive)
	{
		foreach (Transform transform in Selection.transforms)
		{
			GameObject go = transform.gameObject;

			//Undo.RecordObject(go, go.name + (val ? " Activate" : " Deactivate"));
			if(inRecursive == true)
			{
				go.SetActiveRecursively(inValue);
			}
			else
			{
				go.active = inValue;
			}
		}
	}
	
	private void SetLayer(int inNewLayer)
	{
		foreach (GameObject go in Selection.gameObjects)
		{
			Undo.RecordObject(go, go.name + " Change Layer" );
			go.layer = inNewLayer;
		}
	}
	*/
}