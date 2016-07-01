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

[AddComponentMenu("Weapons/WeaponBase")]
[RequireComponent(typeof (AudioSource))]
public abstract class WeaponBase : MonoBehaviour
{
	[System.Serializable]
	public class SoundsInfo
	{
		public AudioClip SoundShow;
		public AudioClip SoundReload;
		public AudioClip[] SoundFire = new AudioClip[0];
		public AudioClip[] SoundSilencer = new AudioClip[0];
	}

	public SoundsInfo SoundsSetupAI = new SoundsInfo();
	public SoundsInfo SoundsSetupPlayer = new SoundsInfo();

	public GameObject Muzzle;
	public ParticleSystem Shells;
	public Renderer Renderer;
	//public float ChangeFOV = 0;				//0 means use the default FOV (do not change it)
	public Material DiffuseMaterial;
	public Material FadeoutMaterial;
	public bool UseFireUp = false;

	protected WeaponSettings Settings;

	protected AgentHuman Owner;

	protected int _AmmoInClip;
	protected int _AmmoInWeapon;

	protected Transform TransformShot;

	protected GameObject GameObject;
	protected Transform Transform;
	protected AudioSource Audio;
	protected Rigidbody RBody;

	protected Quaternion Temp = new Quaternion();

	protected float TimeReload;
	protected float TimeSwitch;

	protected Projectile.InitSettings InitProjSettings;

	float Busy;
	float Firing;

	public E_WeaponID WeaponID
	{
		get { return Settings.ID; }
	}

	public E_WeaponType WeaponType
	{
		get { return Settings.WeaponType; }
	}

	public float FireFovModificator
	{
		get { return Settings.BaseData.FireFovModificator*FireFovModif; }
	}

	public float CoverFovModificator
	{
		get { return Settings.BaseData.CoverFovModificator; }
	}

	public float CoverFireFovModificator
	{
		get { return Settings.BaseData.CoverFireFovModificator*FireFovModif; }
	}

	public float Damage
	{
		get { return Settings.BaseData.Damage*DamageModif; }
	}

	public float Dispersion
	{
		get { return Settings.BaseData.Dispersion*DispersionModif; }
	}

	public float FireTime
	{
		get { return Settings.BaseData.FireTime*FireTimeModif; }
	}

	public float BulletSpeed
	{
		get { return Settings.BaseData.Speed*BulletSpeedModif; }
	}

	public int MaxAmmoInClip
	{
		get { return MaxAmmoInClipRecomputed; }
	}

	public int MaxAmmoInWeapon
	{
		get { return MaxAmmoRecomputed; }
	}

	public bool HasSilencer
	{
		get { return Silencer; }
	}

	float FireFovModif;
	float DamageModif;
	float DispersionModif;
	float FireTimeModif;
	float BulletSpeedModif;
	int MaxAmmoInClipRecomputed;
	bool Silencer;
	int MaxAmmoRecomputed;

	public int RechargeAmmoCount
	{
		get { return Settings.RechargeAmmoCount; }
	}

	public int ClipAmmo
	{
		get { return _AmmoInClip; }
		protected set { _AmmoInClip = Mathf.Clamp(value, 0, MaxAmmoInClip); }
	}

	public int WeaponAmmo
	{
		get { return _AmmoInWeapon; }
		protected set { _AmmoInWeapon = Mathf.Clamp(value, 0, MaxAmmoInWeapon); }
	}

	public bool HasAnyAmmo
	{
		get { return (ClipAmmo + WeaponAmmo) > 0; }
	}

	public virtual bool IsFull
	{
		get { return WeaponAmmo == MaxAmmoInWeapon; }
	}

	public bool IsFullyLoaded
	{
		get { return ClipAmmo == MaxAmmoInClip; }
	}

	public bool IsFullAuto
	{
		get { return Settings.FullAuto; }
	}

	public bool IsCriticalAmmo
	{
		get { return Settings.BaseData.CriticalAmmo >= ClipAmmo; }
	}

	public bool IsOutOfAmmo
	{
		get { return ClipAmmo == 0 && WeaponAmmo == 0; }
	}

	public Vector3 ShotPos
	{
		get { return TransformShot.position; }
	}

	public bool IsBusy()
	{
		return Owner.IsOwner ? Busy > Time.timeSinceLevelLoad : false;
	}

	public bool IsFiring()
	{
		return Owner.IsOwner ? Firing > Time.timeSinceLevelLoad : false;
	}

	public bool IsProxy
	{
		get { return Owner.IsProxy; }
	}

	public void SetBusy(float busyTime)
	{
		Busy = Mathf.Max(Time.timeSinceLevelLoad + busyTime, Busy);
	}

	public void SetFiring(float fireTime)
	{
		Firing = Mathf.Max(Time.timeSinceLevelLoad + fireTime, Firing);
	}

	public float GetBusyTime()
	{
		return Busy - Time.timeSinceLevelLoad;
	} //returns negative values when not busy

	public virtual bool PreparedForFire
	{
		get { return true; }
	}

	public virtual float PreparedForFireProgress
	{
		get { return -1f; }
	}

	public virtual void PrepareForFire(bool Prepare)
	{
	}

	// -----------------

	// -----
	public void Initialize(AgentHuman owner, E_WeaponID id)
	{
		Settings = WeaponSettingsManager.Instance.Get(id);
		InitProjSettings = new Projectile.InitSettings();
		InitProjSettings.WeaponID = Settings.ID;
		InitProjSettings.Impulse = Settings.BaseData.Impulse;
		InitProjSettings.Speed = Settings.BaseData.Speed; // to have valid valid values here if InitializeModifiersFromUpgrades fails
		InitProjSettings.Damage = Settings.BaseData.Damage;

		InitializeModifiersFromUpgrades(owner);
		_AmmoInClip = MaxAmmoInClip;
		_AmmoInWeapon = MaxAmmoInWeapon;
		GameObject = gameObject;
		Transform = transform;
		Audio = GetComponent<AudioSource>();
		RBody = GetComponent<Rigidbody>();
//		Renderer		= renderer;
		TransformShot = transform.FindChild("Shoot");
	}

	// -----
	public void InitializeModifiersFromUpgrades(AgentHuman owner)
	{
		MaxAmmoInClipRecomputed = Settings.BaseData.MaxAmmoInClip;
		MaxAmmoRecomputed = Settings.BaseData.MaxAmmoInWeapon;
		FireFovModif = 1.0f;
		DamageModif = 1.0f;
		DispersionModif = 1.0f;
		FireTimeModif = 1.0f;
		BulletSpeedModif = 1.0f;
		Silencer = false;

		if (!owner)
			return;

		PlayerPersistantInfo ppi = PPIManager.Instance.GetPPI(owner.networkView.owner);
		if (ppi == null)
		{
			Debug.LogWarning("Can't find PPI");
			return;
		}

		if (ppi.Upgrades.OwnsUpgrade(E_UpgradeID.AmmoHolsters))
		{
			if ((Settings.ID == E_WeaponID.Launcher1) || (Settings.ID == E_WeaponID.Launcher2) || (Settings.ID == E_WeaponID.Launcher3))
				++MaxAmmoRecomputed; // only 1 rocket for launcher
			else
			{
				int value = (MaxAmmoRecomputed <= 0) ? MaxAmmoInClipRecomputed : MaxAmmoRecomputed;

				value = Mathf.RoundToInt(1.33f*value);
				if ((value > 20) && ((value%5) != 0))
				{
					value = 5*((value/5) + 1);
				}

				if (MaxAmmoRecomputed <= 0)
					MaxAmmoInClipRecomputed = value;
				else
					MaxAmmoRecomputed = value;
			}
		}

		float maxAmmoInClipModif = 1.0f;
		foreach (WeaponSettings.Upgrade upgrade in Settings.Upgrades)
		{
			if (!upgrade.Disabled && ppi.EquipList.OwnsWeaponUpgrade(Settings.ID, upgrade.ID))
			{
				switch (upgrade.ID)
				{
				case E_WeaponUpgradeID.AimingFov:
					FireFovModif += upgrade.Modifier;
					break;
				case E_WeaponUpgradeID.BulletSpeed:
					BulletSpeedModif += upgrade.Modifier;
					break;
				case E_WeaponUpgradeID.ClipSize:
					maxAmmoInClipModif += upgrade.Modifier;
					break;
				case E_WeaponUpgradeID.Damage:
					DamageModif += upgrade.Modifier;
					break;
				case E_WeaponUpgradeID.Dispersion:
					DispersionModif += upgrade.Modifier;
					break;
				case E_WeaponUpgradeID.FireTime:
					FireTimeModif += upgrade.Modifier;
					break;
				case E_WeaponUpgradeID.AmmoSize:
					MaxAmmoRecomputed += (int)upgrade.Modifier;
					break;
				case E_WeaponUpgradeID.Silencer:
					Silencer = true;
					break;
				default:
					Debug.LogWarning("Unhandled enum: " + upgrade.ID);
					break;
				}
			}
		}
		MaxAmmoInClipRecomputed = Mathf.CeilToInt(MaxAmmoInClip*maxAmmoInClipModif);
		InitProjSettings.Speed = BulletSpeed;
		InitProjSettings.Damage = Damage;
		InitProjSettings.Homing = false;
		InitProjSettings.Silencer = HasSilencer;
	}

	void OnDestroy()
	{
		SoundsSetupAI = null;
		SoundsSetupPlayer = null;

		Muzzle = null;
		Shells = null;
	}

	public virtual void Reset(bool unlink)
	{
		Busy = 0;
		Firing = 0;
		_AmmoInClip = MaxAmmoInClip;
		_AmmoInWeapon = MaxAmmoInWeapon;

		if (unlink)
			UnlinkFromOwner();

		ShowWeapon(false);
	}

	public virtual void LinkToOwner(AgentHuman owner, Transform linkTo)
	{
		//Debug.Log(name + " link to owner", this);
		Owner = owner;
		SetParent(linkTo);

		InitializeModifiersFromUpgrades(owner);

		if (RBody != null)
		{
			RBody.isKinematic = true;
		}

		Renderer.probeAnchor = owner.TransformTarget;
						//We're setting the lightProbeAnchor to the same object for agent's models, hat and weapon

		Transform.localPosition = Vector3.zero;
		Transform.localRotation = Quaternion.identity;

		//get timers based on owner animations !
		TimeReload = Owner.AnimSet.GetWeaponAnimTime(Settings.WeaponType, E_WeaponAction.Reload);
		TimeSwitch = Owner.AnimSet.GetWeaponAnimTime(Settings.WeaponType, E_WeaponAction.Switch);

		ShowWeapon(true);
	}

	public virtual void UnlinkFromOwner()
	{
		//Debug.Log(name + " Unlink to owner", this);

		Owner = null;
		SetParent(null);

		Transform.localPosition = Vector3.zero;
		Transform.localRotation = Quaternion.identity;

		if (RBody != null)
		{
			RBody.isKinematic = true;
		}

		ShowWeapon(false);
	}

	public void Drop()
	{
		if (RBody != null)
		{
			int num = 3;
			Vector3 linV = Vector3.zero;
			Vector3 angV = Vector3.zero;
			Transform tr = Transform.parent;

			while ((tr != null) && (num-- > 0))
			{
				if ((tr.GetComponent<Collider>() != null) && (tr.GetComponent<Rigidbody>() != null))
				{
					linV = tr.GetComponent<Rigidbody>().velocity;
					angV = tr.GetComponent<Rigidbody>().angularVelocity;
					break;
				}
				tr = tr.parent;
			}

			RBody.isKinematic = false;
			RBody.velocity = linV;
			RBody.angularVelocity = angV;

			Transform.parent = null;
		}
	}

	public void SetParent(Transform parent)
	{
		Transform.parent = parent;
	}

	public void ShowModel(bool show)
	{
		Renderer.enabled = show;
	}

	public virtual void Fire(Vector3 direction)
	{
		Fire(TransformShot.position, direction);
	}

	public virtual void Fire(Vector3 fromPos, Vector3 direction)
	{
		if (!IsProxy && (ClipAmmo == 0 || IsBusy()))
		{
			//Debug.Log(Time.timeSinceLevelLoad + " BUSY " + name + "busy " + Busy);
			return;
		}

		// Debug.Log(Time.timeSinceLevelLoad + " Fire from weaponm " + name + "busy " + Busy);

		PlaySoundFire();

		SpawnProjectile(fromPos, direction);

		if (uLink.Network.isClient && Muzzle)
		{
			float f = (HasSilencer) ? Random.Range(0.35f, 0.5f) : Random.Range(0.95f, 1.1f);
			Muzzle.transform.localScale = new Vector3(f, f, f);
			Muzzle.transform.localEulerAngles = new Vector3(Muzzle.transform.localEulerAngles.x,
															Muzzle.transform.localEulerAngles.y,
															Random.Range(0, 50));
			Muzzle.SetActive(true);

			//Debug.Log(Time.timeSinceLevelLoad + " muzzle show");
		}

		if (uLink.Network.isClient && Shells && (DeviceInfo.PerformanceGrade != DeviceInfo.Performance.Low))
			Shells.Emit(1);

		if (!PlayerControlsDrone.Enabled)
		{
			DecreaseAmmo();
		}

		SetBusy(FireTime);
		SetFiring(FireTime);
	}

	protected virtual void SpawnProjectile(Vector3 fromPos, Vector3 direction)
	{
		//	float dispersion = Owner.IsInCover ? Settings.BaseData.Dispersion * 0.5f : Settings.BaseData.Dispersion;
		//
		//	Temp.SetLookRotation(direction);
		//	Temp.eulerAngles = new Vector3(Temp.eulerAngles.x + Random.Range(-dispersion, dispersion), Temp.eulerAngles.y + Random.Range(-dispersion, dispersion), 0);
		//
		//	InitProjSettings.Agent = Owner;
		//	InitProjSettings.IgnoreTransform = Owner.Transform;
		//
		//	ProjectileManager.Instance.SpawnProjectile(Settings.ProjectileType, fromPos, Temp * Vector3.forward, InitProjSettings);

		float dispersion = Owner.IsInCover ? Dispersion*0.666f : Dispersion;

		dispersion *= Owner.GadgetsComponent.GetActiveBoostModifier(E_ItemBoosterBehaviour.Accuracy);

		direction = MathUtils.RandomVectorInsideCone(direction, dispersion);

		InitProjSettings.Agent = Owner;
		InitProjSettings.IgnoreTransform = Owner.Transform;

		if (Settings.ProjectileType != E_ProjectileType.Rocket)
			InitProjSettings.Damage = Damage*Owner.GadgetsComponent.GetActiveBoostModifier(E_ItemBoosterBehaviour.Damage);
		else
			InitProjSettings.Damage = Damage;

		ProjectileManager.Instance.SpawnProjectile(Settings.ProjectileType, fromPos, direction, InitProjSettings);
	}

	protected virtual void PlaySoundFire()
	{
		if (Owner.IsServer)
			return;

		AudioClip[] SoundFire;

		if (Owner.IsOwner)
		{
			if (Silencer)
				SoundFire = SoundsSetupPlayer.SoundSilencer;
			else
				SoundFire = SoundsSetupPlayer.SoundFire;
		}
		else
		{
			if (Silencer)
				SoundFire = SoundsSetupAI.SoundSilencer;
			else
				SoundFire = SoundsSetupAI.SoundFire;
		}

		if (SoundFire.Length > 0)
		{
			AudioClip s = SoundFire[Random.Range(0, SoundFire.Length)];
			if (Audio.isPlaying)
			{
				Audio.Stop();
				if (Audio.clip != s)
					Audio.clip = s;
				Audio.Play();
			}
			else
			{
				Audio.clip = s;
				Audio.Play();
			}
		}
	}

	public virtual void Reload()
	{
		if (Owner.IsOwner)
			Owner.SoundPlay(SoundsSetupPlayer.SoundReload);
		else if (Owner.IsProxy)
			Owner.SoundPlay(SoundsSetupAI.SoundReload);

		if (MaxAmmoInWeapon == -1)
		{
			_AmmoInClip = MaxAmmoInClip;
		}
		else if (_AmmoInWeapon > 0)
		{
			int needAmmoToReload = MaxAmmoInClip - _AmmoInClip;

			if (_AmmoInWeapon < needAmmoToReload)
			{
				_AmmoInClip += _AmmoInWeapon;
				_AmmoInWeapon = 0;
			}
			else
			{
				_AmmoInClip = MaxAmmoInClip;
				_AmmoInWeapon -= needAmmoToReload;
			}
		}

		SetBusy(TimeReload);
	}

	public virtual void WeaponArm(float busyTime = -1)
	{
		if (Owner.IsOwner)
			Owner.SoundPlay(SoundsSetupPlayer.SoundShow);
		else if (Owner.IsProxy)
			Owner.SoundPlay(SoundsSetupAI.SoundShow);

		if (busyTime < 0)
			SetBusy(TimeSwitch);
		else
			SetBusy(busyTime);
	}

	public virtual void DecreaseAmmo()
	{
		_AmmoInClip--;
	}

	public virtual void AddAmmo(int ammo)
	{
		if (ammo == -1)
		{
			_AmmoInClip = MaxAmmoInClip;
			_AmmoInWeapon = MaxAmmoInWeapon;
		}
		else
		{
			if (_AmmoInWeapon + ammo > MaxAmmoInWeapon)
				_AmmoInWeapon = MaxAmmoInWeapon;
			else
				_AmmoInWeapon += ammo;
		}
	}

	public void SetAmmo(int ammoInClip, int ammoInWeapon)
	{
		//Debug.Log("Set ammo " + ammoInClip + " " + ammoInWeapon);
		_AmmoInClip = ammoInClip;
		_AmmoInWeapon = ammoInWeapon;
	}

	public void ShowWeapon(bool show)
	{
		GameObject.SetActive(show);

		if (Muzzle)
			Muzzle.SetActive(false);

		if (show)
			StartCoroutine(UpdateFireEffect());
		else
			StopCoroutine("UpdateFireEffect");

		if (Shells && (DeviceInfo.PerformanceGrade != DeviceInfo.Performance.Low))
		{
			if (show)
				Shells.Play();
			else
				Shells.Stop();
		}
	}

	protected virtual IEnumerator UpdateFireEffect()
	{
		while (true)
		{
			if (Muzzle && Muzzle.activeSelf)
			{
				yield return new WaitForSeconds(Random.Range(0.03f, 0.08f));

				// Debug.Log(Time.timeSinceLevelLoad + " muzzle hide");
				Muzzle.SetActive(false);
			}
			yield return new WaitForEndOfFrame();

			/* if (Shells && Shells.emit)
            {
                yield return new WaitForEndOfFrame();
                Shells.emit = false;
            }*/
		}
	}

	//
	public void SetFadeoutMaterial(float timeOfs, float invert, float duration)
	{
		if (FadeoutMaterial != null && Renderer != null)
		{
			Renderer.material = FadeoutMaterial;
			Renderer.material.SetFloat("_TimeOffs", timeOfs);
			Renderer.material.SetFloat("_Invert", invert);
			Renderer.material.SetFloat("_Duration", duration);
		}
	}

	//
	public void SetInvisibleMaterial(float amount)
	{
		if (Renderer != null)
		{
			CombatEffectsManager.Instance.InvisibleEffectMaterial.SetTexture("_MainTex", DiffuseMaterial.GetTexture("_MainTex"));

			Renderer.material = CombatEffectsManager.Instance.InvisibleEffectMaterial;
			Renderer.material.SetTexture("_MainTex", DiffuseMaterial.GetTexture("_MainTex"));
			//Renderer.material.SetTexture("_BumpMap", DiffuseMaterial.GetTexture("_BumpMap"));
			//Renderer.material.SetFloat( "_EffectAmount", amount );
		}

		UpdateEffectAmount(amount);
	}

	public void UpdateEffectAmount(float amount)
	{
		if (null != Renderer && null != Renderer.material)
		{
			Renderer.material.SetFloat("_EffectAmount", amount);
		}
	}

	//
	public void SetDefaultMaterial()
	{
		if (DiffuseMaterial != null && Renderer != null)
		{
			Renderer.material = DiffuseMaterial;
		}
	}
}
