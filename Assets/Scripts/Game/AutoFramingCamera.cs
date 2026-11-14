using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Camera))]
public class AutoFramingCamera : MonoBehaviour
{
    public GameObject targetModel1;
    public GameObject targetModel2;
    public GameObject targetModel3;

    public UnityEvent HideUI;

    public float transitionDuration = 1.5f;
    public float framingPadding = 1.2f;
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Camera cam;
    private Vector3 defaultOffset = new Vector3(1f, 0.75f, -1f).normalized;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void Start()
    {
        if (targetModel1 != null)
        {
            FrameModel(targetModel1, false, 1.0f, false);
        }
    }

    public void TriggerTransitionToModel2()
    {
        if (targetModel2 != null)
        {
            FrameModel(targetModel2, true, transitionDuration, true);
        }
    }

    public void TriggerTransitionToModel3()
    {
        if (targetModel3 != null)
        {
            FrameModel(targetModel3, true, transitionDuration, true);
        }
    }

    public void TriggerTransitionToModel1()
    {
        if (targetModel1 != null)
        {
            FrameModel(targetModel1, true, transitionDuration, true);
        }
    }

    public void ResetPosition()
    {
        if (targetModel1 != null)
        {
            // Setzt das Event hier explizit auf false, sodass HideUI beim Reset nicht aufgerufen wird
            FrameModel(targetModel1, true, transitionDuration, false);
        }
    }

    public void FrameModel(GameObject model, bool smooth = false, float duration = 1.0f, bool triggerEvent = true)
    {
        if (model == null) return;

        // Löst das Unity Event nur aus, wenn triggerEvent true ist
        if (triggerEvent)
        {
            HideUI?.Invoke();
        }

        Bounds bounds = CalculateModelBounds(model);
        Vector3 targetCenter = bounds.center;
        float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

        float verticalFOV = cam.fieldOfView * Mathf.Deg2Rad;
        float distance = (maxDimension * framingPadding) / (2f * Mathf.Tan(verticalFOV / 2f));

        if (cam.aspect < 1f)
        {
            distance /= cam.aspect;
        }

        Vector3 targetCameraPosition = targetCenter + (defaultOffset * distance);

        if (smooth)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothTransition(targetCameraPosition, targetCenter, duration));
        }
        else
        {
            transform.position = targetCameraPosition;
            transform.LookAt(targetCenter);
        }
    }

    private Bounds CalculateModelBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    private IEnumerator SmoothTransition(Vector3 endPos, Vector3 endLookTarget, float duration)
    {
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        transform.position = endPos;
        transform.LookAt(endLookTarget);
        Quaternion endRot = transform.rotation;

        transform.position = startPos;
        transform.rotation = startRot;

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            float curveValue = transitionCurve.Evaluate(t);

            transform.position = Vector3.Lerp(startPos, endPos, curveValue);
            transform.rotation = Quaternion.Slerp(startRot, endRot, curveValue);

            yield return null;
        }

        transform.position = endPos;
        transform.rotation = endRot;
    }
}