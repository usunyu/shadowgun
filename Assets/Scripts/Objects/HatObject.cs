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

//
public class HatObject : MonoBehaviour
{
	public AudioClip HitSound;
	public Material DiffuseMaterial;
	public Material FadeoutMaterial;

	GameObject GameObj;
	AgentHuman Owner;
	float DestroyTime;

	//
	void Awake()
	{
		GameObj = gameObject;
		DestroyTime = 0;

		if (GameObj.transform.parent)
			Owner = GameObj.transform.parent.gameObject.GetFirstComponentUpward<AgentHuman>();
	}

	//
	public void OnProjectileHit(Projectile projectile)
	{
		if (!Owner && GameObj.transform.parent)
			Owner = GameObj.transform.parent.gameObject.GetFirstComponentUpward<AgentHuman>();

		if ((Owner != null) && (Owner.IsFriend(projectile.Agent) == true))
		{
			projectile.ignoreThisHit = true;
			return;
		}

		Vector3 impulse = (projectile.Transform.forward*(projectile.Impulse*0.005f)) + (Vector3.up*(projectile.Impulse*0.002f));

//		Debug.Log ("HatObject, impulse=" + impulse.ToString("F5") + ", projectile.Impulse=" + projectile.Impulse.ToString("F5") + ", impulse.magnitude=" + impulse.magnitude);
//		Debug.DrawLine(GameObj.transform.position, GameObj.transform.position + impulse, Color.red, 5.0f);

		if (uLink.Network.isServer)
		{
			if (Owner)
			{
				//send impulse to clients
				Owner.NetworkView.RPC("ShotOffHat", uLink.RPCMode.Others, impulse, false);

				//shot off the hat on server
				Owner.ShotOffHat(impulse, true);
			}
		}
		else
		{
			//apply impulse to a hat which is already shot off
			if (null == GameObj.transform.parent)
			{
				GameObj.GetComponent<Rigidbody>().isKinematic = false;
				GameObj.GetComponent<Rigidbody>().useGravity = true;

				GameObj.GetComponent<Rigidbody>().AddForce(impulse, ForceMode.Impulse);
			}
			else
			{
				return; // on client and still on head --> do nothing
			}
		}

		DestroyTime = Time.timeSinceLevelLoad + 20; //destroy hat after 20 seconds
	}

	//
	public void Update()
	{
		if (Owner)
		{
			if (GameObj.GetComponent<Rigidbody>().useGravity && !GameObj.GetComponent<Rigidbody>().IsSleeping())
			{
//				Debug.Log ("HatObject.Update() : hat=" + name + ", velocity=" + GameObj.rigidbody.velocity);

				if (GameObj.transform.position.y < (Owner.Position.y - 10.0f)) //when we're too much below our owner, sleep the physics
				{
					GameObj.GetComponent<Rigidbody>().Sleep();
//					Debug.Log ("SLEEP HatObject.Update() : hat=" + name + ", velocity=" + GameObj.rigidbody.velocity + ", pos=" + GameObj.transform.position);
				}
			}
		}

		if (DestroyTime > 0 && Time.timeSinceLevelLoad > DestroyTime)
		{
			if (Owner)
				Owner.DestroyHat();
			else
				Destroy(GameObj);
		}
	}

	//
	public void SetFadeoutMaterial(float timeOfs, float invert, float duration)
	{
		Renderer renderer = this.GameObj.GetComponent<Renderer>();

		if (FadeoutMaterial != null && renderer != null)
		{
			renderer.material = FadeoutMaterial;
			renderer.material.SetFloat("_TimeOffs", timeOfs);
			renderer.material.SetFloat("_Invert", invert);
			renderer.material.SetFloat("_Duration", duration);
		}
	}

	public void SetInvisibleMaterial(float amount)
	{
		Renderer renderer = this.GameObj.GetComponent<Renderer>();

		if (renderer != null)
		{
			renderer.material = CombatEffectsManager.Instance.InvisibleEffectMaterial;
			renderer.material.SetTexture("_MainTex", DiffuseMaterial.GetTexture("_MainTex"));
			//renderer.material.SetTexture("_BumpMap", DiffuseMaterial.GetTexture("_BumpMap"));
		}

		UpdateEffectAmount(amount);
	}

	public void UpdateEffectAmount(float amount)
	{
		Renderer renderer = this.GameObj.GetComponent<Renderer>();

		if (null != renderer && null != renderer.material)
		{
			renderer.material.SetFloat("_EffectAmount", amount);
		}
	}

	//
	public void SetDefaultMaterial()
	{
		Renderer renderer = this.GameObj.GetComponent<Renderer>();

		if (DiffuseMaterial != null && renderer != null)
		{
			renderer.material = DiffuseMaterial;
		}
	}
}
