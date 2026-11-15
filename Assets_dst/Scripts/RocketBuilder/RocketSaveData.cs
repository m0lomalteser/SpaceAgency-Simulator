using System;
using System.Collections.Generic;

[Serializable]
public class RocketSaveData
{
    public string rocketName;
    public string savedAt;
    public int totalCost;
    public float totalWeightKg;
    public float totalFuelUnits;
    public float totalThrustKn;
    public float durabilityHp;
    public float explosionProbability;
    public List<SavedPartData> parts = new List<SavedPartData>();
}

[Serializable]
public class SavedPartData
{
    public string slot;
    public string id;
    public string partName;
    public int costUsd;
    public float weightKg;
    public float fuelUnits;
    public float thrustKn;
    public float durabilityHp;
}
