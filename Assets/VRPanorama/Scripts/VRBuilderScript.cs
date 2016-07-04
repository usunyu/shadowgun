using UnityEngine;
using System.Collections;
using VRPanorama;

namespace VRPanorama
{
public class VRBuilderScript : MonoBehaviour 
{
	public GameObject obj;
	public Vector3 spawnPoint;
	
	
	public void BuildObject()
	{
		Instantiate(obj, spawnPoint, Quaternion.identity);
	}
}
}
