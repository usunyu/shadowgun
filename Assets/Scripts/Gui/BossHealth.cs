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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBossHealth
{
	float HealthPercent { get; }
};

[AddComponentMenu("GUI/Game/BossHealth")]
public class BossHealth : MonoBehaviour
{
	public enum E_BossName
	{
		Robolobster,
		Driller,
		DrSimon,
		Dropship,
	}
	static string[] s_BossLabelNames = {"Label_Robolobster", "Label_Driller", "Label_DrSimon", "Label_Dropship"};

	//------------------------------ public inspector properties
	public float ShowDelay = 2.5f;
	public E_BossName BossName;
	public AnimationClip hitAnimClip;

	//------------------------------
	IBossHealth boss;

	bool m_Initialised = false;
	bool m_Visible = false;
	//bool hitAnimRunning = false;
	GUIBase_Pivot m_Pivot;
	GUIBase_Layout m_Layout;
	GUIBase_ProgressBar m_ProgressBar;

	float currentHP = 1.0f;
	float animStartHP = 1.0f; //pro slerpovani actualni hodnoty animaace
	float animTargetHP; //pro slerpovani actualni hodnoty animaace
	float animSpeed = 0.6f; //kolik relativniho zdravi ma ubyt za sec		
	float curAnimTime; //actualni prubeh animace  
	float animTimeLength; //delka animzace ubyti helthbary [sec]
	const float minDelayTime = 0.5f; //minimalni pauza mezi activaci a ukazanim baru
	float showTime;
	bool m_Suspended = false;

	static ArrayList Instancies = new ArrayList();

	//-------------- manager
	static void RegisterInstance(BossHealth s)
	{
		Instancies.Add(s);
	}

	static void UnregisterInstance(BossHealth s)
	{
		Instancies.Remove(s);
	}

	public static void SuspendAllRunning(bool suspend)
	{
		foreach (BossHealth s in Instancies)
		{
			s.Suspend(suspend);
		}
	}

	// ==================================================================================================
	// === Default MoneBehaviour interface ==============================================================
	public void Awake()
	{
		boss = GameObjectUtils.GetComponentWithInterface<IBossHealth>(gameObject);
		if (boss == null)
		{
			Debug.LogError("BossHealth :: There have to be the component which implements IBossHealth interface");
			Debug.LogError("BossHealth :: Health-bar will not be updated ... ");
		}

		RegisterInstance(this);
	}

	void OnDestroy()
	{
		HideHealthBar();
		CancelInvoke();
		UnregisterInstance(this);
	}

	public void OnDisable()
	{
		HideHealthBar();
		CancelInvoke();
	}

	public void OnEnable()
	{
		if (boss == null)
			return;

		InvokeRepeating("HealhBarUpdate", ShowDelay > minDelayTime ? ShowDelay : minDelayTime, 0.1f);
		currentHP = 1.0f;
		animTargetHP = currentHP;
		animStartHP = currentHP;
		curAnimTime = 0;
		m_Suspended = false;

		if (m_ProgressBar)
		{
			m_ProgressBar.SetValue(currentHP);
		}

		//hitAnimRunning = false;
	}

	// ==================================================================================================
	// === Main update functions ========================================================================
	void HealhBarUpdate()
	{
		if (!m_Initialised)
		{
			InitHealthBar();
		}

		if (!m_Visible)
		{
			ShowHealthBar();
		}

		//ppokud jsme srazili zdravi na nulu, ukonci corutinu, skryj bar
		if (currentHP <= 0)
		{
			//Debug.Log("Boss dead");
			HideHealthBar();
			CancelInvoke();
			return;
		}

		//pokud doslo ke zmene zdravi aganta, spusti novou animaci
		float agentHP = boss.HealthPercent;
		if (animTargetHP > agentHP)
		{
			animTargetHP = agentHP;

			float runningAnimProgressTime = 0;
			if (curAnimTime > 0)
				runningAnimProgressTime = animTimeLength - curAnimTime; //kolik casu jsme uz odecetli

			animTimeLength = (animStartHP - animTargetHP)/animSpeed;
			curAnimTime = animTimeLength - runningAnimProgressTime;

			//srovnej s actualni interpolaci
			while (currentHP < Mathfx.Hermite(animStartHP, animTargetHP, Mathf.Clamp(1.0f - (curAnimTime/animTimeLength), 0, 1)))
			{
				curAnimTime -= 0.05f;
				//Debug.Log("Adjusting health anim time: currentHP " + currentHP + " newHP " + Hermite(animStartHP, animTargetHP, Mathf.Clamp(1.0f - (curAnimTime/animTimeLength), 0, 1)) );
			}

			//Debug.Log( "curAnimTime: " +  curAnimTime + " animStartHP: " + animStartHP + " animTargetHP: " + animTargetHP + " runningAnimProgressTime: " + 	runningAnimProgressTime + " animTimeLength: " + animTimeLength);

			//spusti animaci zasahu
			if (hitAnimClip)
				m_ProgressBar.PlayAnimClip(hitAnimClip);
			//PlayHitAnim();
		}
	}

	void Update()
	{
		if (m_Suspended)
			return;

		UpdateBarAnim();
	}

	void UpdateBarAnim()
	{
		if (currentHP != animTargetHP)
		{
			//do not set value immediately, animate toward target value
			if (curAnimTime > 0)
			{
				curAnimTime -= Time.deltaTime*animSpeed;
				float timeRatio = Mathf.Clamp(1.0f - (curAnimTime/animTimeLength), 0, 1);
				currentHP = Mathfx.Hermite(animStartHP, animTargetHP, timeRatio);
				UpdateBar();

				if (curAnimTime <= 0)
				{
					curAnimTime = 0;
					animStartHP = currentHP;

					//ukonci animaci zasahu
					//StopHitAnim();
				}
			}
		}
	}

	// ==================================================================================================
	// === internal GUI functions =======================================================================
	internal void InitHealthBar()
	{
		m_Pivot = MFGuiManager.Instance.GetPivot("HealthBar");
		m_Layout = GuiBaseUtils.GetLayout("HealthBar_Layout", m_Pivot);
		m_ProgressBar = GuiBaseUtils.PrepareProgressBar(m_Layout, "GUIBase_ProgressBar");

		m_Initialised = true;
	}

	internal void ShowHealthBar()
	{
		m_Visible = true;
		MFGuiManager.Instance.ShowLayout(m_Layout, true);
		ShowName();
	}

	internal void HideHealthBar()
	{
		if (m_Visible)
		{
			m_Visible = false;
			//StopHitAnim();
			if (MFGuiManager.Instance != null && m_Layout != null)
			{
				MFGuiManager.Instance.ShowLayout(m_Layout, false);
			}
		}
	}

	internal void ShowName()
	{
		string labelName = s_BossLabelNames[(int)BossName];
		//Debug.Log(labelName + " " + BossName);
		GUIBase_Widget tmpWidget = m_Layout.GetWidget(labelName);
		MFGuiManager.Instance.ShowWidget(tmpWidget, true, true);
	}

	internal void UpdateBar()
	{
		m_ProgressBar.SetValue(currentHP);
	}

	void Suspend(bool suspend)
	{
		if (m_Initialised && m_Visible)
		{
			//m_Pivot.Show(!suspend);
			m_Layout.ShowImmediate(!suspend, false);
			m_Suspended = suspend;
		}
	}

	/*void PlayHitAnim()
	{
		if(hitAnimClip != null && !hitAnimRunning)
		{
			m_ProgressBar.PlayAnimClip( hitAnimClip );	
			hitAnimRunning = true;
		}
	}*/

	/*void StopHitAnim()
	{
		if(hitAnimClip != null && hitAnimRunning)
		{
			m_ProgressBar.StopAnimClip(true);	
			hitAnimRunning = false;
		}
	}*/
}
