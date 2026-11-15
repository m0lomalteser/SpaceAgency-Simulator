
using System;
using System.IO;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Rendering;

[Serializable]
public class LaunchRocketData
{
    public string rocketName;
    public float explosionProbability;
}

[Serializable]
public class LaunchAgencySaveData
{
    public int version;
    public string agencyName;
    public string logoPng;
    public long money;
    public float mood;
    public int day;
    public int month;
    public int year;
    public float timeOfDay;
    public int tutorialStep;
    public bool tutorialDone;
    public int totalLaunches;
    public int satelliteLaunches;
    public int satelliteSuccesses;
    public int satelliteFailures;
    public int moonLaunches;
    public int moonSuccesses;
    public int moonFailures;
    public int marsLaunches;
    public int marsSuccesses;
    public int marsFailures;
    public int consecutiveFailures;
    public int[] upgrades;
    public long lifetimeBudget;
    public long lifetimeSpent;
    public long lastBudget;
    public int pendingMission;
    public long pendingLaunchCost;
    public int lastResultMission;
    public int lastResultSuccess;
    public string lastResultText;

    // Wirtschaft / Regierungsgeld
    public long regierungsgeld;
    public long lastGovernmentGrant;
    public float lastGovernmentGrantMood;
    public string lastGovernmentGrantDate;
    public string lastAdvertisementPayoutDate;
    public string lastAdvertisementMoodDate;
    public int mannedMissionLaunches;
    public int mannedMissionSuccesses;
    public int mannedMissionFailures;
    public long mannedMissionCompensationPaid;

    public string createdAt;
    public string updatedAt;
}

public class Launch : MonoBehaviour
{
    [Header("1. LAUNCH FLOW")]
    public UnityEvent launchEvent;
    public bool launchInProgress;
    public bool tutorialMode;

    public float countdownDuration = 3f;
    public float ignitionDelay = 0.8f;
    public float maxFlightDuration = 7f;

    private bool isLaunching;
    private bool hasLaunched;

    [Header("2. ROCKET")]
    public string rocketObjectName = "PlayerRocket";
    public GameObject playerRocket;
    public Rigidbody rocketRigidbody;

    public float maxAcceleration = 50f;
    public float rocketSpawnTimeout = 10f;

    [Header("3. CAMERA")]
    public Camera launchCamera;
    public float cameraRotationSpeed = 7f;
    public bool enableCameraShake = true;
    public float cameraShakeIntensity = 0.12f;
    public float cameraShakeFrequency = 24f;
    public float cameraLookOffset = 0.5f;

    private Vector3 initialCameraPosition;
    private float currentShake;
    private bool cameraFollowing;
    private bool framingCameraWasEnabled;

    private AutoFramingCamera framingCamera;

    [Header("4. ENGINE EFFECTS")]
    public bool enableVisuals = true;
    public Vector3 engineOffset = new Vector3(0f, -0.5f, 0f);

    [Range(0.2f, 3f)]
    public float visualScale = 1f;

    private GameObject activeFlameEffect;
    private Material particleMaterial;
    private Texture2D particleTexture;

    [Header("5. UI")]
    public Text countdownText;
    public GameObject successCanvas;
    public GameObject failedCanvas;

    [Header("6. AUDIO")]
    public AudioSource audioSource;
    public AudioClip launchSound;
    public AudioClip explosionSound;

    [Header("7. FAILURE")]
    [Range(0f, 1f)]
    public float explosionFallbackProbability = 0.05f;

    [Header("8. ECONOMY")]
    [Tooltip("Mit gekauftem Reusable-Rockets-Upgrade bleibt die Rakete nach einem erfolgreichen Start erhalten.")]
    public bool respectReusableRockets = true;

    // =========================================================
    // MOOD SYSTEM
    // =========================================================

    private string AgencySavePath =>
        Path.Combine(
            Application.persistentDataPath,
            "space_agency_save.json"
        );

    private const float StartingMood = 40f;
    private const float MaximumMood = 40f;
    private const float MinimumMood = 0f;

    private const float SuccessMoodReward = 20f;
    private const float FailureMoodPenalty = 10f;

    private void UpdateAgencyMood(float change)
    {
        try
        {
            LaunchAgencySaveData data;

            if (File.Exists(AgencySavePath))
            {
                string json = File.ReadAllText(AgencySavePath);

                data = JsonUtility.FromJson<LaunchAgencySaveData>(json);

                if (data == null)
                {
                    Debug.LogError("[Launch] Could not read agency save!");
                    return;
                }

                // Falls das JSON noch keinen Mood-Wert enthält:
                if (!json.Contains("\"mood\""))
                {
                    data.mood = StartingMood;
                }
            }
            else
            {
                data = new LaunchAgencySaveData();
                data.mood = StartingMood;
            }

            // Mood aktualisieren und auf 0 bis 40 begrenzen.
            data.mood = Mathf.Clamp(
                data.mood + change,
                MinimumMood,
                MaximumMood
            );

            data.updatedAt = DateTime.Now.ToString(
                "yyyy-MM-dd HH:mm:ss"
            );

            string updatedJson = JsonUtility.ToJson(data, true);

            File.WriteAllText(AgencySavePath, updatedJson);

            Debug.Log(
                "[Launch] Agency Mood updated: " +
                data.mood +
                " (Change: " + change + ")"
            );
        }
        catch (Exception e)
        {
            Debug.LogError(
                "[Launch] Failed to update agency mood: " + e.Message
            );
        }
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    void Awake()
    {
        if (launchCamera == null)
            launchCamera = Camera.main;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);

        if (successCanvas != null)
            successCanvas.SetActive(false);

        if (failedCanvas != null)
            failedCanvas.SetActive(false);

        framingCamera = FindFirstObjectByType<AutoFramingCamera>();
    }

    void OnDestroy()
    {
        if (particleMaterial != null)
            Destroy(particleMaterial);

        if (particleTexture != null)
            Destroy(particleTexture);
    }

    // =========================================================
    // CAMERA
    // =========================================================

    void LateUpdate()
    {
        if (!cameraFollowing ||
            playerRocket == null ||
            launchCamera == null)
            return;

        float t = Time.time;

        Vector3 shake = Vector3.zero;

        if (enableCameraShake && currentShake > 0.001f)
        {
            shake.x = Mathf.PerlinNoise(
                t * cameraShakeFrequency, 0f
            ) - 0.5f;

            shake.y = Mathf.PerlinNoise(
                0f, t * cameraShakeFrequency
            ) - 0.5f;

            shake.z = Mathf.PerlinNoise(
                t * cameraShakeFrequency, 2f
            ) - 0.5f;

            shake *= currentShake;
        }

        launchCamera.transform.position =
            initialCameraPosition + shake;

        Vector3 target =
            playerRocket.transform.position +
            playerRocket.transform.up * cameraLookOffset;

        Vector3 direction =
            target - launchCamera.transform.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(
                direction.normalized,
                Vector3.up
            );

            float smooth = 1f - Mathf.Exp(
                -cameraRotationSpeed * Time.deltaTime
            );

            launchCamera.transform.rotation = Quaternion.Slerp(
                launchCamera.transform.rotation,
                targetRotation,
                smooth
            );
        }

        currentShake = Mathf.Lerp(
            currentShake,
            0f,
            Time.deltaTime * 2f
        );
    }

    void SetupCamera()
    {
        if (launchCamera == null)
            launchCamera = Camera.main;

        if (launchCamera != null)
        {
            initialCameraPosition =
                launchCamera.transform.position;

            cameraFollowing = true;
        }

        if (framingCamera != null)
        {
            framingCameraWasEnabled = framingCamera.enabled;
            framingCamera.enabled = false;
        }
    }

    // =========================================================
    // LAUNCH SEQUENCE
    // =========================================================

    public void LaunchRocket(bool isTutorial)
    {
        if (hasLaunched || isLaunching)
            return;

        tutorialMode = isTutorial;

        isLaunching = true;
        launchInProgress = true;

        if (successCanvas != null)
            successCanvas.SetActive(false);

        if (failedCanvas != null)
            failedCanvas.SetActive(false);

        launchEvent?.Invoke();

        if (framingCamera != null)
            framingCamera.TriggerTransitionToModel2();

        StartCoroutine(MainLaunchSequence());
    }

    IEnumerator MainLaunchSequence()
    {
        float timer = 0f;

        while (!FindRocket() && timer < rocketSpawnTimeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (playerRocket == null)
        {
            Debug.LogError("[Launch] Rocket not found!");

            isLaunching = false;
            launchInProgress = false;

            yield break;
        }

        hasLaunched = true;

        SetupRocketPhysicsForCountdown();
        SetupCamera();

        // COUNTDOWN

        if (countdownText != null)
            countdownText.gameObject.SetActive(true);

        for (int i = Mathf.CeilToInt(countdownDuration); i > 0; i--)
        {
            if (countdownText != null)
                countdownText.text = i.ToString();

            PlayBeep(800f, 0.2f);

            yield return new WaitForSeconds(1f);
        }

        if (countdownText != null)
        {
            countdownText.text = "IGNITION!";

            yield return new WaitForSeconds(0.6f);

            countdownText.gameObject.SetActive(false);
        }

        // IGNITION

        TriggerIgnitionEffects();

        currentShake = cameraShakeIntensity;

        yield return new WaitForSeconds(ignitionDelay);

        // LIFTOFF

        ReleaseRocketPhysics();

        float flightTimer = 0f;

        while (flightTimer < maxFlightDuration)
        {
            if (playerRocket == null || rocketRigidbody == null)
                break;

            rocketRigidbody.AddForce(
                playerRocket.transform.up * maxAcceleration,
                ForceMode.Acceleration
            );

            currentShake = Mathf.Lerp(
                currentShake,
                cameraShakeIntensity,
                Time.deltaTime * 3f
            );

            flightTimer += Time.fixedDeltaTime;

            yield return new WaitForFixedUpdate();
        }

        isLaunching = false;
        launchInProgress = false;

        if (playerRocket == null)
        {
            TeardownLaunch();
            yield break;
        }

        // SUCCESS OR EXPLOSION

        float probability = tutorialMode
            ? 0f
            : ReadExplosionProbability();

        if (UnityEngine.Random.value < probability)
            ExecuteExplosion();
        else
            ExecuteSuccess();
    }

    // =========================================================
    // ROCKET FINDER
    // =========================================================

    bool FindRocket()
    {
        if (playerRocket != null)
            return true;

        playerRocket = GameObject.Find(rocketObjectName);

        if (playerRocket == null)
        {
            foreach (GameObject obj in
                Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (obj.name == rocketObjectName ||
                    obj.name == rocketObjectName + "(Clone)" ||
                    obj.name == rocketObjectName + " (Clone)")
                {
                    if (obj.scene.IsValid())
                    {
                        playerRocket = obj;
                        break;
                    }
                }
            }
        }

        if (playerRocket != null)
        {
            rocketRigidbody =
                playerRocket.GetComponent<Rigidbody>();

            if (rocketRigidbody == null)
                rocketRigidbody =
                    playerRocket.AddComponent<Rigidbody>();

            return true;
        }

        return false;
    }

    float GetRocketWidth()
    {
        if (playerRocket == null)
            return 0.5f;

        Renderer[] renderers =
            playerRocket.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
            return 0.5f;

        Bounds bounds = renderers[0].bounds;

        foreach (Renderer r in renderers)
            bounds.Encapsulate(r.bounds);

        return Mathf.Max(
            Mathf.Max(bounds.size.x, bounds.size.z) * visualScale,
            0.15f
        );
    }

    // =========================================================
    // PHYSICS
    // =========================================================

    void SetupRocketPhysicsForCountdown()
    {
        playerRocket.transform.SetParent(null, true);

        rocketRigidbody.isKinematic = true;
        rocketRigidbody.useGravity = false;

        rocketRigidbody.linearVelocity = Vector3.zero;
        rocketRigidbody.angularVelocity = Vector3.zero;
    }

    void ReleaseRocketPhysics()
    {
        if (rocketRigidbody == null)
            return;

        rocketRigidbody.isKinematic = false;
        rocketRigidbody.useGravity = true;

        rocketRigidbody.interpolation =
            RigidbodyInterpolation.Interpolate;

        rocketRigidbody.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        if (audioSource != null)
        {
            if (launchSound != null)
            {
                audioSource.PlayOneShot(launchSound);
            }
            else
            {
                AudioClip rumble =
                    CreateProceduralRumble(4f, 60f, 0.9f);

                audioSource.PlayOneShot(rumble);

                Destroy(rumble, 5f);
            }
        }
    }

    // =========================================================
    // PARTICLE MATERIAL
    // =========================================================

    Material GetParticleMaterial()
    {
        if (particleMaterial != null)
            return particleMaterial;

        string pipeline =
            GraphicsSettings.currentRenderPipeline != null
                ? GraphicsSettings.currentRenderPipeline.GetType().FullName
                : "";

        string[] shaders;

        if (pipeline.Contains("Universal"))
        {
            shaders = new string[]
            {
                "Universal Render Pipeline/Particles/Unlit",
                "Universal Render Pipeline/Unlit",
                "Sprites/Default"
            };
        }
        else if (pipeline.Contains("HDRender"))
        {
            shaders = new string[]
            {
                "HDRP/Unlit",
                "Sprites/Default"
            };
        }
        else
        {
            shaders = new string[]
            {
                "Particles/Standard Unlit",
                "Legacy Shaders/Particles/Alpha Blended",
                "Sprites/Default",
                "Unlit/Transparent"
            };
        }

        Shader selectedShader = null;

        foreach (string shaderName in shaders)
        {
            Shader candidate = Shader.Find(shaderName);

            if (candidate != null && candidate.isSupported)
            {
                selectedShader = candidate;
                break;
            }
        }

        if (selectedShader == null)
        {
            Debug.LogError(
                "[Launch] No supported particle shader found!"
            );

            return null;
        }

        particleMaterial = new Material(selectedShader);

        particleMaterial.name =
            "Launch_Procedural_Particle_Material";

        if (selectedShader.name.Contains(
            "Universal Render Pipeline"))
        {
            particleMaterial.SetFloat("_Surface", 1f);
            particleMaterial.SetFloat("_Blend", 0f);
            particleMaterial.SetFloat("_ZWrite", 0f);

            particleMaterial.EnableKeyword(
                "_SURFACE_TYPE_TRANSPARENT"
            );

            particleMaterial.renderQueue = 3000;
        }

        particleTexture = CreateRadialTexture();

        if (particleMaterial.HasProperty("_BaseMap"))
            particleMaterial.SetTexture(
                "_BaseMap",
                particleTexture
            );

        if (particleMaterial.HasProperty("_MainTex"))
            particleMaterial.SetTexture(
                "_MainTex",
                particleTexture
            );

        if (particleMaterial.HasProperty("_BaseColor"))
            particleMaterial.SetColor("_BaseColor", Color.white);

        if (particleMaterial.HasProperty("_Color"))
            particleMaterial.SetColor("_Color", Color.white);

        Debug.Log(
            "[Launch] Particle Shader: " + selectedShader.name
        );

        return particleMaterial;
    }

    Texture2D CreateRadialTexture()
    {
        int resolution = 64;

        Texture2D texture = new Texture2D(
            resolution,
            resolution,
            TextureFormat.RGBA32,
            false,
            true
        );

        texture.name = "Launch_Radial_Particle_Texture";

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float u =
                    x / (float)(resolution - 1) * 2f - 1f;

                float v =
                    y / (float)(resolution - 1) * 2f - 1f;

                float distance = Mathf.Sqrt(u * u + v * v);

                float alpha = Mathf.Pow(
                    Mathf.Clamp01(1f - distance),
                    2.2f
                );

                texture.SetPixel(
                    x,
                    y,
                    new Color(1f, 1f, 1f, alpha)
                );
            }
        }

        texture.Apply();

        return texture;
    }

    // =========================================================
    // PARTICLE CREATION
    // =========================================================

    ParticleSystem CreateParticleSystem(
        string name,
        Transform parent,
        float lifeMin,
        float lifeMax,
        float speedMin,
        float speedMax,
        float sizeMin,
        float sizeMax,
        float emissionRate,
        float coneAngle,
        float coneRadius,
        Color startColor,
        Color endColor,
        AnimationCurve sizeCurve,
        float noiseStrength,
        bool loop = true,
        float gravity = 0f
    )
    {
        GameObject obj = new GameObject(name);

        obj.transform.SetParent(parent, false);

        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        ParticleSystem ps =
            obj.AddComponent<ParticleSystem>();

        var main = ps.main;

        main.playOnAwake = false;
        main.loop = loop;

        main.startLifetime = new ParticleSystem.MinMaxCurve(
            lifeMin,
            lifeMax
        );

        main.startSpeed = new ParticleSystem.MinMaxCurve(
            speedMin,
            speedMax
        );

        main.startSize = new ParticleSystem.MinMaxCurve(
            sizeMin,
            sizeMax
        );

        main.startColor = Color.white;
        main.gravityModifier = gravity;

        main.simulationSpace =
            ParticleSystemSimulationSpace.World;

        main.maxParticles = 10000;

        var emission = ps.emission;

        emission.enabled = true;
        emission.rateOverTime = emissionRate;

        // Exhaust points straight down.

        var shape = ps.shape;

        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;

        shape.angle = coneAngle;
        shape.radius = coneRadius;
        shape.radiusThickness = 1f;

        shape.rotation = new Vector3(90f, 0f, 0f);

        var color = ps.colorOverLifetime;

        color.enabled = true;

        color.color = new ParticleSystem.MinMaxGradient(
            CreateGradient(startColor, endColor)
        );

        var size = ps.sizeOverLifetime;

        size.enabled = true;

        size.size = new ParticleSystem.MinMaxCurve(
            1f,
            sizeCurve
        );

        var noise = ps.noise;

        noise.enabled = noiseStrength > 0f;

        if (noiseStrength > 0f)
        {
            noise.strength = noiseStrength;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.1f;
        }

        ParticleSystemRenderer renderer =
            ps.GetComponent<ParticleSystemRenderer>();

        renderer.renderMode =
            ParticleSystemRenderMode.Billboard;

        renderer.alignment =
            ParticleSystemRenderSpace.View;

        renderer.sortMode =
            ParticleSystemSortMode.Distance;

        Material mat = GetParticleMaterial();

        if (mat != null)
            renderer.sharedMaterial = mat;

        ps.Play();

        return ps;
    }

    Gradient CreateGradient(Color start, Color end)
    {
        Gradient gradient = new Gradient();

        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(start, 0f),
                new GradientColorKey(end, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(start.a, 0f),
                new GradientAlphaKey(end.a, 1f)
            }
        );

        return gradient;
    }

    // =========================================================
    // ENGINE EFFECTS
    // =========================================================

    void TriggerIgnitionEffects()
    {
        if (!enableVisuals || playerRocket == null)
            return;

        if (activeFlameEffect != null)
            Destroy(activeFlameEffect);

        float width = GetRocketWidth();

        activeFlameEffect =
            new GameObject("Rocket_Engine_Effects");

        activeFlameEffect.transform.SetParent(
            playerRocket.transform,
            false
        );

        activeFlameEffect.transform.localPosition = engineOffset;

        activeFlameEffect.transform.localRotation =
            Quaternion.identity;

        // 1. OUTER ORANGE FLAME

        CreateParticleSystem(
            "Outer_Orange_Flame",
            activeFlameEffect.transform,
            0.18f, 0.32f,
            width * 19f, width * 25f,
            width * 0.42f, width * 0.72f,
            180f,
            0.8f,
            width * 0.025f,
            new Color(1f, 0.45f, 0.035f, 1f),
            new Color(0.9f, 0.035f, 0.005f, 0f),
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0f),
            0f
        );

        // 2. YELLOW FLAME

        CreateParticleSystem(
            "Yellow_Flame",
            activeFlameEffect.transform,
            0.12f, 0.23f,
            width * 23f, width * 29f,
            width * 0.27f, width * 0.48f,
            140f,
            0.4f,
            width * 0.015f,
            new Color(1f, 0.88f, 0.15f, 1f),
            new Color(1f, 0.3f, 0.015f, 0f),
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0f),
            0f
        );

        // 3. WHITE HOT CORE

        CreateParticleSystem(
            "White_Hot_Core",
            activeFlameEffect.transform,
            0.07f, 0.14f,
            width * 27f, width * 34f,
            width * 0.12f, width * 0.24f,
            90f,
            0f,
            width * 0.005f,
            new Color(1f, 1f, 0.9f, 1f),
            new Color(1f, 0.75f, 0.25f, 0f),
            AnimationCurve.EaseInOut(0f, 1f, 1f, 0f),
            0f
        );

        // 4. EXHAUST SMOKE

        CreateParticleSystem(
            "Exhaust_Smoke",
            activeFlameEffect.transform,
            1.5f, 2.5f,
            width * 2f, width * 4f,
            width * 0.5f, width * 1.1f,
            42f,
            1.5f,
            width * 0.025f,
            new Color(0.42f, 0.45f, 0.48f, 0.55f),
            new Color(0.18f, 0.2f, 0.23f, 0f),
            AnimationCurve.EaseInOut(0f, 0.35f, 1f, 2.4f),
            0f
        );

        // 5. SPARKS

        CreateParticleSystem(
            "Engine_Sparks",
            activeFlameEffect.transform,
            0.2f, 0.55f,
            width * 12f, width * 21f,
            width * 0.025f, width * 0.055f,
            65f,
            2f,
            width * 0.025f,
            new Color(1f, 0.85f, 0.35f, 1f),
            new Color(1f, 0.12f, 0.01f, 0f),
            AnimationCurve.Linear(0f, 1f, 1f, 0f),
            0f,
            true,
            0.3f
        );

        // ENGINE LIGHT

        GameObject lightObject =
            new GameObject("Engine_Light");

        lightObject.transform.SetParent(
            activeFlameEffect.transform,
            false
        );

        Light engineLight =
            lightObject.AddComponent<Light>();

        engineLight.type = LightType.Point;

        engineLight.color =
            new Color(1f, 0.38f, 0.08f);

        engineLight.intensity = 2.5f;
        engineLight.range = width * 8f;
        engineLight.shadows = LightShadows.None;

        Debug.Log("[Launch] Straight engine flame activated.");
    }

    // =========================================================
    // EFFECT CLEANUP
    // =========================================================

    void StopAndDetachEffects()
    {
        if (activeFlameEffect == null)
            return;

        ParticleSystem[] systems =
            activeFlameEffect.GetComponentsInChildren<ParticleSystem>();

        activeFlameEffect.transform.SetParent(null, true);

        foreach (ParticleSystem ps in systems)
        {
            ps.Stop(
                false,
                ParticleSystemStopBehavior.StopEmitting
            );
        }

        Destroy(activeFlameEffect, 6f);

        activeFlameEffect = null;
    }

    void TeardownLaunch()
    {
        cameraFollowing = false;

        if (framingCamera != null)
            framingCamera.enabled = framingCameraWasEnabled;

        StopAndDetachEffects();
    }

    // =========================================================
    // EXPLOSION
    // =========================================================

    void ExecuteExplosion()
    {
        Debug.Log("[Launch] ROCKET EXPLOSION!");

        // MOOD: -10 for failed launch
        if (!tutorialMode)
            UpdateAgencyMood(-FailureMoodPenalty);

        Vector3 position = playerRocket != null
            ? playerRocket.transform.position
            : Vector3.zero;

        float scale = GetRocketWidth();

        TeardownLaunch();

        CreateExplosionEffect(position, scale);

        if (playerRocket != null)
        {
            Destroy(playerRocket);

            playerRocket = null;
            rocketRigidbody = null;
        }

        if (audioSource != null)
        {
            if (explosionSound != null)
            {
                audioSource.PlayOneShot(explosionSound);
            }
            else
            {
                AudioClip rumble =
                    CreateProceduralRumble(3f, 30f, 1f);

                audioSource.PlayOneShot(rumble);

                Destroy(rumble, 4f);
            }
        }

        StartCoroutine(FailureUISequence());
    }

    void CreateExplosionEffect(Vector3 position, float scale)
    {
        GameObject explosion =
            new GameObject("Rocket_Explosion");

        explosion.transform.position = position;

        CreateExplosionParticles(
            explosion.transform,
            "Explosion_Fire",
            110,
            scale * 0.5f,
            scale * 15f,
            scale * 0.1f,
            scale * 0.4f,
            0.7f,
            1.4f,
            new Color(1f, 0.9f, 0.55f, 1f),
            new Color(1f, 0.1f, 0.01f, 0f)
        );

        CreateExplosionParticles(
            explosion.transform,
            "Explosion_Smoke",
            45,
            scale * 0.7f,
            scale * 3f,
            scale * 0.5f,
            scale * 1.2f,
            1.5f,
            3f,
            new Color(0.28f, 0.3f, 0.34f, 0.8f),
            new Color(0.12f, 0.14f, 0.17f, 0f)
        );

        Light flash = explosion.AddComponent<Light>();

        flash.type = LightType.Point;
        flash.color = new Color(1f, 0.3f, 0.05f);
        flash.intensity = 5f;
        flash.range = scale * 12f;

        StartCoroutine(FadeExplosionLight(flash, 0.8f));

        Destroy(explosion, 5f);
    }

    void CreateExplosionParticles(
        Transform parent,
        string name,
        int count,
        float radius,
        float speed,
        float minSize,
        float maxSize,
        float minLifetime,
        float maxLifetime,
        Color startColor,
        Color endColor
    )
    {
        GameObject obj = new GameObject(name);

        obj.transform.SetParent(parent, false);

        ParticleSystem ps =
            obj.AddComponent<ParticleSystem>();

        var main = ps.main;

        main.loop = false;
        main.playOnAwake = false;
        main.duration = 0.2f;

        main.startLifetime = new ParticleSystem.MinMaxCurve(
            minLifetime,
            maxLifetime
        );

        main.startSpeed = new ParticleSystem.MinMaxCurve(
            speed * 0.5f,
            speed
        );

        main.startSize = new ParticleSystem.MinMaxCurve(
            minSize,
            maxSize
        );

        main.startColor = Color.white;

        main.simulationSpace =
            ParticleSystemSimulationSpace.World;

        main.maxParticles = 1000;

        var emission = ps.emission;

        emission.rateOverTime = 0f;

        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f, (short)count)
        });

        var shape = ps.shape;

        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
        shape.radiusThickness = 1f;

        var color = ps.colorOverLifetime;

        color.enabled = true;

        color.color = new ParticleSystem.MinMaxGradient(
            CreateGradient(startColor, endColor)
        );

        var size = ps.sizeOverLifetime;

        size.enabled = true;

        size.size = new ParticleSystem.MinMaxCurve(
            1f,
            AnimationCurve.EaseInOut(0f, 0.4f, 1f, 2f)
        );

        ParticleSystemRenderer renderer =
            ps.GetComponent<ParticleSystemRenderer>();

        renderer.renderMode =
            ParticleSystemRenderMode.Billboard;

        renderer.sortMode =
            ParticleSystemSortMode.Distance;

        Material mat = GetParticleMaterial();

        if (mat != null)
            renderer.sharedMaterial = mat;

        ps.Play();
    }

    IEnumerator FadeExplosionLight(
        Light light,
        float duration
    )
    {
        float startIntensity = light.intensity;
        float timer = 0f;

        while (timer < duration && light != null)
        {
            timer += Time.deltaTime;

            light.intensity = Mathf.Lerp(
                startIntensity,
                0f,
                timer / duration
            );

            yield return null;
        }
    }

    IEnumerator FailureUISequence()
    {
        yield return new WaitForSeconds(1.5f);

        InvokeSceneMethod("showUI");
        InvokeSceneMethod("ResetPosition");

        if (successCanvas != null)
            successCanvas.SetActive(false);

        if (failedCanvas != null)
            failedCanvas.SetActive(true);
    }

    // =========================================================
    // SUCCESS
    // =========================================================

    void ExecuteSuccess()
    {
        Debug.Log("[Launch] SUCCESS!");

        // MOOD: +20 for successful launch
        if (!tutorialMode)
            UpdateAgencyMood(SuccessMoodReward);

        TeardownLaunch();

        if (successCanvas != null)
            successCanvas.SetActive(true);

        if (failedCanvas != null)
            failedCanvas.SetActive(false);

        string path = Path.Combine(
            Application.persistentDataPath,
            "rocket_build.json"
        );

        ConsumeRocketUse(path);
    }

    // =========================================================
    // REUSABLE ROCKETS
    // =========================================================

    /// <summary>
    /// Verbraucht einen Start der gebauten Rakete. Ohne Reusable-Rockets-Upgrade
    /// wird die Build-Datei wie bisher gelöscht, mit Upgrade bleibt sie erhalten,
    /// bis die eingestellte Anzahl an Wiederverwendungen aufgebraucht ist.
    /// </summary>
    void ConsumeRocketUse(string path)
    {
        if (!File.Exists(path))
            return;

        AgencyEconomyManager economy = AgencyEconomyManager.ExistingInstance;

        bool reusable =
            respectReusableRockets &&
            economy != null &&
            economy.RocketsAreReusable;

        if (!reusable)
        {
            File.Delete(path);
            return;
        }

        int remaining = ReadReuseUsesRemaining(path);

        // Ältere Build-Dateien ohne Feld: einmalig mit dem konfigurierten Wert starten.
        if (remaining <= 0)
            remaining = economy.RocketReuseCount;

        remaining--;

        if (remaining > 0)
        {
            WriteReuseUsesRemaining(path, remaining);

            Debug.Log(
                "[Launch] Reusable rocket kept. Uses left: " + remaining
            );

            return;
        }

        File.Delete(path);

        Debug.Log("[Launch] Reusable rocket used up.");
    }

    static int ReadReuseUsesRemaining(string path)
    {
        try
        {
            RocketSaveData data = JsonUtility.FromJson<RocketSaveData>(
                File.ReadAllText(path)
            );

            return data != null ? data.reuseUsesRemaining : 0;
        }
        catch
        {
            return 0;
        }
    }

    static void WriteReuseUsesRemaining(string path, int remaining)
    {
        try
        {
            RocketSaveData data = JsonUtility.FromJson<RocketSaveData>(
                File.ReadAllText(path)
            );

            if (data == null)
                return;

            data.reuseUsesRemaining = remaining;

            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning(
                "[Launch] Could not update reusable rocket: " + e.Message
            );
        }
    }

    // =========================================================
    // AUDIO
    // =========================================================

    void PlayBeep(float frequency, float duration)
    {
        if (audioSource == null)
            return;

        int sampleRate = 44100;

        int samples = Mathf.RoundToInt(
            sampleRate * duration
        );

        AudioClip clip = AudioClip.Create(
            "Countdown_Beep",
            samples,
            1,
            sampleRate,
            false
        );

        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;

            data[i] =
                Mathf.Sin(2f * Mathf.PI * frequency * t) *
                Mathf.Exp(-t * 5f);
        }

        clip.SetData(data, 0);

        audioSource.PlayOneShot(clip);

        Destroy(clip, duration + 0.5f);
    }

    AudioClip CreateProceduralRumble(
        float duration,
        float frequency,
        float volume
    )
    {
        int sampleRate = 44100;

        int samples = Mathf.RoundToInt(
            sampleRate * duration
        );

        AudioClip clip = AudioClip.Create(
            "Rocket_Rumble",
            samples,
            1,
            sampleRate,
            false
        );

        float[] data = new float[samples];

        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;

            float noise =
                UnityEngine.Random.Range(-1f, 1f);

            float tone = Mathf.Sin(
                2f * Mathf.PI * frequency * t
            );

            data[i] =
                (noise * 0.7f + tone * 0.3f) *
                volume *
                Mathf.Exp(-t * 1.5f);
        }

        clip.SetData(data, 0);

        return clip;
    }

    // =========================================================
    // SAVE DATA
    // =========================================================

    float ReadExplosionProbability()
    {
        string path = Path.Combine(
            Application.persistentDataPath,
            "rocket_build.json"
        );

        if (!File.Exists(path))
            return explosionFallbackProbability;

        try
        {
            LaunchRocketData data =
                JsonUtility.FromJson<LaunchRocketData>(
                    File.ReadAllText(path)
                );

            return data != null
                ? Mathf.Clamp01(data.explosionProbability)
                : explosionFallbackProbability;
        }
        catch
        {
            return explosionFallbackProbability;
        }
    }

    // =========================================================
    // SCENE METHODS
    // =========================================================

    void InvokeSceneMethod(string methodName)
    {
        MonoBehaviour[] scripts =
            FindObjectsOfType<MonoBehaviour>(true);

        foreach (MonoBehaviour script in scripts)
        {
            if (script == null || script == this)
                continue;

            MethodInfo method = script.GetType().GetMethod(
                methodName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic,
                null,
                Type.EmptyTypes,
                null
            );

            if (method != null)
            {
                try
                {
                    method.Invoke(script, null);
                    return;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(
                        "[Launch] " + exception.Message
                    );
                }
            }
        }
    }
}
