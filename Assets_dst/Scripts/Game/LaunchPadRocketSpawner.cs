using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchPadRocketSpawner : MonoBehaviour
{
    public PartRegistry registry;
    public string launchPadName = "SpaceAgency_LaunchPad";
    public float padHeightFraction = 0.28f;
    public bool retryUntilPadFound = true;
    public float retrySeconds = 2f;

    private GameObject spawnedRocket;

    private void Start()
    {
        StartCoroutine(SpawnWhenReady());
    }

    private IEnumerator SpawnWhenReady()
    {
        float deadline = Time.realtimeSinceStartup + retrySeconds;
        Transform pad = null;

        while (pad == null && (retryUntilPadFound || Time.realtimeSinceStartup <= deadline))
        {
            GameObject padObj = GameObject.Find(launchPadName);
            if (padObj != null) pad = padObj.transform;
            if (pad == null) yield return null;
        }

        if (pad == null)
        {
            Debug.LogWarning($"[LaunchPadRocketSpawner] '{launchPadName}' not found.");
            yield break;
        }

        SpawnOnPad(pad);
    }

    public void Respawn()
    {
        Transform pad = null;
        GameObject padObj = GameObject.Find(launchPadName);
        if (padObj != null) pad = padObj.transform;
        if (pad != null) SpawnOnPad(pad);
    }

    private void SpawnOnPad(Transform pad)
    {
        if (spawnedRocket != null)
        {
            Destroy(spawnedRocket);
            spawnedRocket = null;
        }

        PartRegistry reg = RocketAssembly.LoadRegistry(registry);
        if (reg == null)
        {
            Debug.LogWarning("[LaunchPadRocketSpawner] PartRegistry missing.");
            return;
        }

        RocketSaveData save = RocketAssembly.LoadSave();
        List<RocketPartData> parts = RocketAssembly.ResolveParts(reg, save);
        if (parts.Count == 0)
        {
            Debug.LogWarning("[LaunchPadRocketSpawner] No rocket parts to spawn.");
            return;
        }

        spawnedRocket = new GameObject("PlayerRocket");
        spawnedRocket.transform.SetPositionAndRotation(pad.position, Quaternion.identity);

        RocketAssembly.BuildStackedRocket(spawnedRocket.transform, parts, out _);
        RocketAssembly.PlaceOnPad(spawnedRocket.transform, pad, padHeightFraction);
    }
}
