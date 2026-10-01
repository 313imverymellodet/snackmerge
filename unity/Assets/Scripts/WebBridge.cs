using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;

// Page integrations: portal ads, share sheet, haptics, leaderboards (snack.js).
public class WebBridge : MonoBehaviour
{
    public static WebBridge I;
    Action<bool> pending;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void SD_Rewarded(string goName);
    [DllImport("__Internal")] static extern void SD_Gameplay(int on);
    [DllImport("__Internal")] static extern void SD_Event(string name, int value);
    [DllImport("__Internal")] static extern void SD_Ready();
    [DllImport("__Internal")] static extern int SD_AdsAvailable();
    [DllImport("__Internal")] static extern void SM_ArmShare(string text);
    [DllImport("__Internal")] static extern void SM_Vibrate(int ms);
    [DllImport("__Internal")] static extern void SM_RunStart(string mode);
    [DllImport("__Internal")] static extern void SM_RunSubmit(string mode, int score, int drops, int maxTier);
    [DllImport("__Internal")] static extern void SM_ShowBoard(string mode);
#endif

    void Awake() { I = this; gameObject.name = "WebBridge"; }

    public static bool AdsAvailable
    {
        get
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return SD_AdsAvailable() == 1;
#else
            return false;
#endif
        }
    }

    public void ShowRewarded(Action<bool> done)
    {
        pending = done;
        Sfx.I.SetMuted(true);
#if UNITY_WEBGL && !UNITY_EDITOR
        SD_Rewarded(gameObject.name);
#else
        OnRewarded("1");
#endif
    }

    public void OnRewarded(string ok)
    {
        Sfx.I.SetMuted(Game.I.Save.muted);
        var cb = pending; pending = null;
        cb?.Invoke(ok == "1");
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    public static void Gameplay(bool on) => SD_Gameplay(on ? 1 : 0);
    public static void Event(string name, int value = 0) => SD_Event(name, value);
    public static void Ready() => SD_Ready();
    public static void Vibrate(int ms) => SM_Vibrate(ms);
    public static void RunStart(string mode) => SM_RunStart(mode);
    public static void RunSubmit(string mode, int score, int drops, int maxTier) => SM_RunSubmit(mode, score, drops, maxTier);
    public static void ShowBoard(string mode) => SM_ShowBoard(mode);
    public static void ArmShare(string text) => SM_ArmShare(text);
#else
    public static void Gameplay(bool on) { }
    public static void Event(string name, int value = 0) { }
    public static void Ready() { }
    public static void Vibrate(int ms) { }
    public static void RunStart(string mode) { }
    public static void RunSubmit(string mode, int score, int drops, int maxTier) { }
    public static void ShowBoard(string mode) { }
    public static void ArmShare(string text) { GUIUtility.systemCopyBuffer = text; }
#endif
}

public class ShareOnPress : MonoBehaviour, IPointerDownHandler
{
    public Func<string> Text;
    public void OnPointerDown(PointerEventData e) { if (Text != null) WebBridge.ArmShare(Text()); }
}
