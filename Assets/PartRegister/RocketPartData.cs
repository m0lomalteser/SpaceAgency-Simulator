using UnityEngine;

[CreateAssetMenu(fileName = "NewRocketPart", menuName = "SpaceAgency/Rocket Part")]
public class RocketPartData : ScriptableObject
{
    [Header("Identifikation")]
    public string id;
    public string slot;
    public string partName;
    [TextArea] public string description;

    [Header("Spielwerte")]
    public float weightKg;
    public float fuelUnits;
    public float thrustKn;
    public float efficiencyPercent;
    public float durabilityHp;
    public int costUsd;

    [Header("Visuals")]
    public GameObject modelPrefab;
    public Texture2D mainTexture;
}
