using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "PartRegistry", menuName = "SpaceAgency/Part Registry")]
public class PartRegistry : ScriptableObject
{
    public List<RocketPartData> allParts = new List<RocketPartData>();

    // Hilfsmethode für dein 5-Slot-Pfeilmenü
    public List<RocketPartData> GetPartsBySlot(string slotName)
    {
        return allParts.Where(p => p.slot == slotName).ToList();
    }

    public RocketPartData GetPartById(string id)
    {
        return allParts.FirstOrDefault(p => p.id == id);
    }
}
