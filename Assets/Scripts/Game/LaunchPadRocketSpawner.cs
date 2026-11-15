
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchPadRocketSpawner : MonoBehaviour
{
    public PartRegistry registry;

    public string launchPadName = "SpaceAgency_LaunchPad";

    [Header("Spawn Point")]
    public Transform spawnPoint;

    [Header("Rocket Size")]
    [Range(0.1f, 1.0f)]
    public float padHeightFraction = 0.45f;

    public float surfaceOffset = 0.05f;

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

        while (pad == null &&
              (retryUntilPadFound ||
               Time.realtimeSinceStartup <= deadline))
        {
            GameObject obj = GameObject.Find(launchPadName);

            if (obj != null)
                pad = obj.transform;

            if (pad == null)
                yield return null;
        }

        if (pad != null)
            SpawnOnPad(pad);
    }

    public void Respawn()
    {
        GameObject obj = GameObject.Find(launchPadName);

        if (obj != null)
            SpawnOnPad(obj.transform);
    }

    private void SpawnOnPad(Transform pad)
    {
        if (spawnedRocket != null)
        {
            Destroy(spawnedRocket);
            spawnedRocket = null;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("[RocketSpawner] SpawnPoint missing!");
            return;
        }

        Renderer padRenderer = pad.GetComponentInChildren<Renderer>();

        if (padRenderer == null)
        {
            Debug.LogError("[RocketSpawner] Pad renderer missing!");
            return;
        }

        PartRegistry reg = RocketAssembly.LoadRegistry(registry);

        if (reg == null)
            return;

        RocketSaveData save = RocketAssembly.LoadSave();

        List<RocketPartData> parts =
            RocketAssembly.ResolveParts(reg, save);

        if (parts == null || parts.Count == 0)
            return;

        // Use horizontal dimensions of the entire launchpad.
        // The tower height is deliberately ignored.
        Bounds padBounds = padRenderer.bounds;

        float padDiameter = Mathf.Min(
            padBounds.size.x,
            padBounds.size.z
        );

        // Create rocket
        spawnedRocket = new GameObject("PlayerRocket");

        spawnedRocket.transform.SetPositionAndRotation(
            spawnPoint.position,
            Quaternion.identity
        );

        RocketAssembly.BuildStackedRocket(
            spawnedRocket.transform,
            parts,
            out _
        );

        // Get rocket bounds
        Renderer[] renderers =
            spawnedRocket.GetComponentsInChildren<Renderer>();

        if (!GetBounds(renderers, out Bounds rocketBounds))
            return;

        // Calculate target rocket height
        float targetHeight = padDiameter * padHeightFraction;

        if (rocketBounds.size.y <= 0.0001f)
            return;

        float factor = targetHeight / rocketBounds.size.y;

        spawnedRocket.transform.localScale *= factor;

        // Update bounds after scaling
        Physics.SyncTransforms();

        renderers =
            spawnedRocket.GetComponentsInChildren<Renderer>();

        if (!GetBounds(renderers, out rocketBounds))
            return;

        // Align bottom center with spawn point
        Vector3 bottomCenter = new Vector3(
            rocketBounds.center.x,
            rocketBounds.min.y,
            rocketBounds.center.z
        );

        Vector3 target = spawnPoint.position +
                         Vector3.up * surfaceOffset;

        spawnedRocket.transform.position += target - bottomCenter;

        Debug.Log(
            $"[RocketSpawner] Spawned! " +
            $"Pad diameter: {padDiameter:F2}, " +
            $"Rocket height: {targetHeight:F2}, " +
            $"Scale factor: {factor:F2}"
        );
    }

    private bool GetBounds(
        Renderer[] renderers,
        out Bounds bounds)
    {
        bounds = new Bounds();
        bool found = false;

        foreach (Renderer r in renderers)
        {
            if (r == null || !r.enabled)
                continue;

            if (!found)
            {
                bounds = r.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        return found;
    }
}
