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
using System.Collections.Generic;

public class ComponentSensors : MonoBehaviour
{
	SensorBase[] Sensors = new SensorBase[(int)E_SensorType.Count];

	AgentHuman Owner;

	float UpdateTime = 0;
	int UpdateSensorIndex = 0;

	// Use this for initialization
	void Awake()
	{
		Owner = GetComponent<AgentHuman>();
	}

	// Update is called once per frame
	void Update()
	{
		if (UpdateTime < Time.timeSinceLevelLoad)
		{
			if (Sensors[UpdateSensorIndex] != null && Sensors[UpdateSensorIndex].Active)
				Sensors[UpdateSensorIndex].Update();

			UpdateSensorIndex++;

			if (UpdateSensorIndex == Sensors.Length)
				UpdateSensorIndex = 0;

			UpdateTime = Time.timeSinceLevelLoad + 0.05f;
		}

		/*if (Owner.debugAI)
        {
            for(int i = 0; i < Sensors.Length;i++)
                if(Sensors[i] != null && Sensors[i].Active)
                    Sensors[i].DebugDraw();
        }*/
	}

	public void AddSensor(E_SensorType sensorType, bool activate)
	{
		SensorBase s = SensorFactory.Create(sensorType, Owner);

		s.Active = activate;
		Sensors[(int)sensorType] = s;
	}

	public void RemoveSensor(E_SensorType sensorType)
	{
		Sensors[(int)sensorType] = null;
	}

	public void RemoveAllSensors()
	{
		for (int i = 0; i < Sensors.Length; i++)
			Sensors[i] = null;
	}

	public void ActivateSensor(E_SensorType sensorType)
	{
		if (Sensors[(int)sensorType] != null)
			Sensors[(int)sensorType].Active = true;
		else
			Debug.LogError("Sensor " + sensorType + " : is not added, cannot active it");
	}

	public void ActivateAllSensors()
	{
		for (int i = 0; i < Sensors.Length; i++)
		{
			if (Sensors[i] != null)
				Sensors[i].Active = true;
		}
	}

	public void DeactivateSensor(E_SensorType sensorType)
	{
		if (Sensors[(int)sensorType] != null)
		{
			Sensors[(int)sensorType].Reset();
			Sensors[(int)sensorType].Active = false;
		}
	}

	public void DeactivateAllSensors()
	{
		for (int i = 0; i < Sensors.Length; i++)
		{
			if (Sensors[i] != null)
			{
				Sensors[i].Reset();
				Sensors[i].Active = false;
			}
		}
	}
}
