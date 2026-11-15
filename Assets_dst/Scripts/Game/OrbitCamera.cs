using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("Ziel & Abstand")]
    [Tooltip("Das Objekt oder der Punkt, um den sich die Kamera dreht.")]
    public Transform target;
    [Tooltip("Der Standardabstand zum Zentrum.")]
    public float distance = 5.0f;

    [Header("Geschwindigkeit")]
    public float mouseSensitivity = 2.0f;
    public float touchSensitivity = 0.5f;

    [Header("Einschränkungen (Vertikal)")]
    public float minYLimit = -20f;
    public float maxYLimit = 80f;

    private float currentX = 0f;
    private float currentY = 0f;

    void Start()
    {
        // Aktuelle Rotation der Kamera beim Start auslesen
        Vector3 angles = transform.eulerAngles;
        currentX = angles.y;
        currentY = angles.x;

        // Falls kein Target zugewiesen ist, erstellen wir uns ein temporäres Zentrum
        if (target == null)
        {
            GameObject go = new GameObject("CameraTarget");
            target = go.transform;
            target.position = transform.position + transform.forward * distance;
        }
    }

    void LateUpdate()
    {
        if (!target) return;

        float inputX = 0f;
        float inputY = 0f;

        // 1. Touch-Eingabe (Mobile) prüfen
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;
                inputX = delta.x * touchSensitivity * 0.1f;
                inputY = delta.y * touchSensitivity * 0.1f;
            }
        }
        // 2. Maus-Eingabe (PC) prüfen (z.B. wenn linke oder rechte Maustaste gedrückt gehalten wird)
        else if (Input.GetMouseButton(0) || Input.GetMouseButton(1))
        {
            inputX = Input.GetAxis("Mouse X") * mouseSensitivity;
            inputY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        }

        // Werte aufaddieren
        currentX += inputX;
        currentY -= inputY; // Minus, damit es sich natürlich anfühlt (inverted Y)

        // Vertikale Drehung einschränken, damit die Kamera nicht über die Pole kippt
        currentY = Mathf.Clamp(currentY, minYLimit, maxYLimit);

        // Rotation berechnen
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        // Position berechnen: Vom Target aus um 'distance' nach hinten versetzen und rotieren
        Vector3 position = rotation * new Vector3(0.0f, 0.0f, -distance) + target.position;

        // Kamera transformieren
        transform.rotation = rotation;
        transform.position = position;
    }
}