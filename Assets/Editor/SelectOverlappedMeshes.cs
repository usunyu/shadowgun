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

class SelectOverlappedMeshes
{
	private static float	ms_PosEps = 0.01f;
	private static float	ms_RotEps = 0.1f;

	[MenuItem("MADFINGER/Select overlapped meshes")]
	static void SelectMeshes()
	{
		Dictionary<string,List<MeshFilter>>	meshFiltersByInstance	= new Dictionary<string,List<MeshFilter>>();
		Dictionary<int,int>					processedMeshFilters	= new Dictionary<int,int>();
		
		GameObject[]	objs = Object.FindObjectsOfType<GameObject>();
		int				numMeshFilters = 0;
				
  		foreach (GameObject o in objs)
  		{
			Transform transform = o.transform;
			if (!transform)
			{
				continue;
			}

			Component[] meshfilter = transform.GetComponentsInChildren(typeof(MeshFilter));
         	
         	for (int i = 0; i < meshfilter.Length; i++)
         	{
				MeshFilter meshFilter = meshfilter[i] as MeshFilter;
				
				if (processedMeshFilters.ContainsKey(meshFilter.GetInstanceID()))
				{
					continue;
				}
				
				processedMeshFilters.Add(meshFilter.GetInstanceID(),0);
				
				if (meshFilter && meshFilter.sharedMesh)
				{
					List<MeshFilter> 	meshFilters = null;
					string				key = meshFilter.sharedMesh.name;
						
					if (meshFiltersByInstance.TryGetValue(key,out meshFilters))
					{
						meshFilters.Add(meshFilter);
					}
					else
					{
						meshFilters = new List<MeshFilter>();
						
						meshFilters.Add(meshFilter);
						meshFiltersByInstance.Add(key,meshFilters);
					}
					
					numMeshFilters++;
				
//					Debug.Log(meshFilter.sharedMesh.name);
				}
			}				
  		}	
		
		
		Dictionary<string,List<MeshFilter>>.KeyCollection keys = meshFiltersByInstance.Keys;
		
		List<Transform>	overlappedObjects = new List<Transform>();
		
		foreach (string currKey in keys)
		{
			List<MeshFilter> meshFilters = null;
								
			meshFiltersByInstance.TryGetValue(currKey,out meshFilters);
			
			MFDebugUtils.Assert(meshFilters != null);
			
			if (meshFilters.Count > 1)
			{
				MeshFilter firstMesh = meshFilters[0];
								
				for (int i = 1; i < meshFilters.Count; i++)
				{
					MeshFilter nextMesh = meshFilters[i];
					
					if (firstMesh.sharedMesh.vertices.Length == nextMesh.sharedMesh.vertices.Length &&
					    firstMesh.sharedMesh.triangles.Length == nextMesh.sharedMesh.triangles.Length)
					{										
						if ((firstMesh.transform.position - nextMesh.transform.position).magnitude < ms_PosEps)
						{
							Vector3 eulerFirst	= firstMesh.transform.eulerAngles;			
							Vector3 eulerNext	= nextMesh.transform.eulerAngles;
							
							if (Mathf.Abs(eulerFirst.x - eulerNext.x) < ms_RotEps &&
							    Mathf.Abs(eulerFirst.y - eulerNext.y) < ms_RotEps &&
							    Mathf.Abs(eulerFirst.z - eulerNext.z) < ms_RotEps)
							{							    
								if (nextMesh.transform.gameObject)
								{
									Debug.Log("Overlapped meshes detected at position : " + firstMesh.transform.position + " x " + nextMesh.transform.position);// + " " + firstMesh.sharedMesh.name + nextMesh.sharedMesh.name);
							
									overlappedObjects.Add(nextMesh.transform);						
								}
							}
						}
					}
				}
			}				
		}		
		
		if (overlappedObjects.Count > 0)
		{
			Object [] newSelection = new Object[overlappedObjects.Count];
				
			for (int i = 0; i < overlappedObjects.Count; i++)
			{
				newSelection[i] = overlappedObjects[i].gameObject;
			}
			
			Selection.objects = newSelection;
			
			Debug.Log(overlappedObjects.Count + " overlapped mesh(es) found");
		}
		else
		{
			Debug.Log("No overlapped meshes found");
		}
		
		//Debug.Log(meshFiltersByInstance.Count + "/" + numMeshFilters);
	}

};