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
using System.Collections;

public class ScaleAnimationOnObject : MonoBehaviour
{
	public enum Scaletype
	{
		Coserp,
		Sinerp,
	}

	public Vector3 ScalePeek = new Vector3(2, 2, 2);
	public Scaletype Type = Scaletype.Sinerp;
	public float Speed = 1;

	Transform MyTransform;
	Renderer Mesh;
	Vector3 Center;
	float ScaleTime;

	void Awake()
	{
		MyTransform = transform;
		Center = MyTransform.localScale;
	}

	void Start()
	{
		Mesh = GetComponent<Renderer>();
		ScaleTime = 0;
	}

	// Update is called once per frame
	void Update()
	{
		if (Mesh != null && Mesh.isVisible == false)
			return;

		ScaleTime += Speed*Time.deltaTime;

		switch (Type)
		{
		case Scaletype.Coserp:

			MyTransform.localScale = Center + ScalePeek*Mathf.Cos(ScaleTime*Mathf.PI);
			break;
		case Scaletype.Sinerp:
			MyTransform.localScale = Center + ScalePeek*Mathf.Sin(ScaleTime*Mathf.PI);
			break;
		}
	}
}
