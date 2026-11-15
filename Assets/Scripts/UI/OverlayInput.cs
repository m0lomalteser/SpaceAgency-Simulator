using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Kleiner Input-Wrapper, der sowohl das neue Input System als auch
/// die alte Input-API unterstützt (Active Input Handling = Both/New/Old).
/// </summary>
public static class OverlayInput
{
    public static bool EscapePressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
                return Keyboard.current.escapeKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.Escape);
        }
    }

    /// <summary>F1 - öffnet/schließt das NASA-Briefing.</summary>
    public static bool NasaPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
                return Keyboard.current.f1Key.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(KeyCode.F1);
        }
    }

    public static bool SubmitPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                return Keyboard.current.enterKey.wasPressedThisFrame ||
                       Keyboard.current.numpadEnterKey.wasPressedThisFrame ||
                       Keyboard.current.spaceKey.wasPressedThisFrame;
            }
#endif
            return Input.GetKeyDown(KeyCode.Return) ||
                   Input.GetKeyDown(KeyCode.KeypadEnter) ||
                   Input.GetKeyDown(KeyCode.Space);
        }
    }
}
