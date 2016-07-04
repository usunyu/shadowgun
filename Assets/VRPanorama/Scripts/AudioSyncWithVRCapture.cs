using UnityEngine;
using System.Collections;
namespace VRPanorama {

public class AudioSyncWithVRCapture : MonoBehaviour {

	// Use this for initialization
	public bool triggerAudio = true;
	void Start () {

	
	}
	
	// Update is called once per frame
	void LateUpdate () {
		if (triggerAudio) GetComponent<AudioSource>().Play();
		triggerAudio = false;
	
	}
}
}
