using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI : MonoBehaviour
{
    public static UI I;
    Canvas canvas; CanvasScaler scaler;
    RectTransform root, hud, screens, pops;
    Font F => Kit.Font;
    public static readonly Color Cocoa = Kit.Hex("#5A3A2A"), Berry = Kit.Hex("#FF5E7E"), Mint = Kit.Hex("#3FC79A"), Honey = Kit.Hex("#FFB833"),
        Cream = Kit.Hex("#FFF7EC"), Soft = new Color(0.35f, 0.23f, 0.16f, 0.7f), Panel = new Color(1f, 0.97f, 0.92f, 0.92f);

    // pointer for aiming/dropping (screen space)
    public Vector2? Pointer => zone ? zone.Pointer : null;
    public Vector2 LastPointer => zone ? zone.Last : Vector2.zero;
    public bool Released { get { if (zone && zone.Released) { zone.Released = false; return true; } return false; } }
    PlayZone zone;

    Text scoreText, bestText, bannerText, bannerSub, toastText, rankText, shakeText, popText;
    Image nextIcon, warnImg;
    readonly List<Image> evo = new List<Image>();
    RectTransform shakeBtn, popBtn;
    float bannerT, toastT;
    bool landscapeHud;

    static readonly Dictionary<string, Sprite> icons = new Dictionary<string, Sprite>();
    public static Sprite Icon(string n) { if (!icons.TryGetValue(n, out var s)) icons[n] = s = Resources.Load<Sprite>("Icons/" + n); return s; }
    static Sprite disc, ring;
    static Sprite Spr(Texture2D t) => Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, .5f));

    public void Init()
    {
        I = this;
        var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>().pixelDragThreshold = 4; es.AddComponent<StandaloneInputModule>();
        canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1080, 1920);
        gameObject.AddComponent<GraphicRaycaster>();
        root = (RectTransform)transform;
        disc = Spr(Kit.Disc); ring = Spr(Kit.Ring);

        var z = Fill("zone", root);
        z.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        zone = z.gameObject.AddComponent<PlayZone>();
        z.gameObject.SetActive(false);

        warnImg = Fill("warn", root).gameObject.AddComponent<Image>();
        warnImg.sprite = Spr(Vignette()); warnImg.color = new Color(1, 0.1f, 0.2f, 0); warnImg.raycastTarget = false;
        pops = Fill("pops", root);
        BuildHud();
        screens = Fill("screens", root);

        bannerText = Txt(root, "", 104, new Vector2(.5f, .62f), Vector2.zero, Berry, TextAnchor.MiddleCenter, 1400);
        bannerText.fontStyle = FontStyle.Italic; Outline(bannerText, 5, Color.white);
        bannerSub = Txt(root, "", 54, new Vector2(.5f, .62f), new Vector2(0, -95), Cocoa, TextAnchor.MiddleCenter, 1400);
        bannerSub.fontStyle = FontStyle.Normal; Outline(bannerSub, 3, Color.white);
        bannerText.gameObject.SetActive(false); bannerSub.gameObject.SetActive(false);
        toastText = Txt(root, "", 40, new Vector2(.5f, .5f), new Vector2(0, 260), Cocoa, TextAnchor.MiddleCenter, 1200);
        Outline(toastText, 3, Color.white); toastText.gameObject.SetActive(false);
    }

    // ---------------------------------------------------------------- building blocks
    RectTransform Rect(string n, Transform p, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(n, typeof(RectTransform)); go.transform.SetParent(p, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(.5f, .5f); rt.anchoredPosition = pos; rt.sizeDelta = size;
        return rt;
    }
    RectTransform Fill(string n, Transform p)
    {
        var rt = Rect(n, p, Vector2.zero, Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; return rt;
    }
    RectTransform Box(Transform p, Vector2 anchor, Vector2 pos, Vector2 size, Color c, bool ray = false)
    {
        var rt = Rect("box", p, anchor, pos, size);
        var img = rt.gameObject.AddComponent<Image>(); img.sprite = Kit.RoundedSprite; img.type = Image.Type.Sliced; img.color = c; img.raycastTarget = ray;
        return rt;
    }
    Image Img(Transform p, Sprite s, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var rt = Rect("img", p, anchor, pos, size);
        var img = rt.gameObject.AddComponent<Image>(); img.sprite = s; img.preserveAspect = true; img.raycastTarget = false; return img;
    }
    Text Txt(Transform p, string s, int size, Vector2 anchor, Vector2 pos, Color c, TextAnchor align = TextAnchor.MiddleCenter, float w = 700)
    {
        size = Mathf.Max(size, 30);   // readable floor: Lilita below this turns to mush on phones and short desktop windows
        var rt = Rect("txt", p, anchor, pos, new Vector2(w, size * 1.4f));
        var t = rt.gameObject.AddComponent<Text>();
        t.font = F; t.fontSize = size; t.fontStyle = FontStyle.Normal; t.alignment = align; t.color = c; t.text = s;
        t.raycastTarget = false; t.horizontalOverflow = HorizontalWrapMode.Overflow; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }
    static void Outline(Text t, float d, Color? c = null) { var o = t.gameObject.AddComponent<Outline>(); o.effectColor = c ?? new Color(0.35f, 0.2f, 0.12f, 0.85f); o.effectDistance = new Vector2(d, -d); }
    Button Btn(Transform p, string label, Vector2 anchor, Vector2 pos, Vector2 size, Color bg, Color fg, Action onClick, int fs = 48)
    {
        var rt = Box(p, anchor, pos, size, bg, true);
        var lip = Box(rt, new Vector2(.5f, 0), new Vector2(0, -7), new Vector2(size.x, 18), Color.Lerp(bg, Cocoa, 0.35f));
        lip.SetAsFirstSibling(); lip.pivot = new Vector2(.5f, 0);
        var b = rt.gameObject.AddComponent<Button>(); b.targetGraphic = rt.GetComponent<Image>();
        b.onClick.AddListener(() => { Sfx.I.Click(); onClick(); });
        rt.gameObject.AddComponent<Press>();
        var t = Txt(rt, label, fs, new Vector2(.5f, .5f), Vector2.zero, fg, TextAnchor.MiddleCenter, size.x);
        t.fontStyle = FontStyle.Italic;
        return b;
    }

    static Texture2D Vignette()
    {
        int n = 64; var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float dx = Mathf.Abs(x / (n - 1f) - .5f) * 2f, dy = Mathf.Abs(y / (n - 1f) - .5f) * 2f;
            float d = Mathf.Max(dx, dy);
            t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01((d - 0.6f) / 0.4f)));
        }
        t.Apply(); t.wrapMode = TextureWrapMode.Clamp; return t;
    }

    // ---------------------------------------------------------------- HUD
    void BuildHud()
    {
        if (hud) Destroy(hud.gameObject);
        evo.Clear();
        hud = Fill("hud", root);
        hud.SetSiblingIndex(2);
        landscapeHud = Landscape;
        var g = Game.I;
        if (!landscapeHud)
        {
            // portrait: score on top, next at top right, ladder + powers along the bottom
            var sb = Box(hud, new Vector2(.5f, 1), new Vector2(0, -105), new Vector2(460, 150), Panel);
            scoreText = Txt(sb, "0", 92, new Vector2(.5f, .5f), new Vector2(0, 14), Cocoa); scoreText.fontStyle = FontStyle.Italic;
            bestText = Txt(sb, "", 30, new Vector2(.5f, .5f), new Vector2(0, -48), Soft);
            var nb = Box(hud, new Vector2(1, 1), new Vector2(-130, -105), new Vector2(190, 190), Panel);
            Txt(nb, "NEXT", 30, new Vector2(.5f, 1), new Vector2(0, -26), Soft);
            nextIcon = Img(nb, null, new Vector2(.5f, .5f), new Vector2(0, -14), new Vector2(120, 120));
            Btn(hud, "II", new Vector2(0, 1), new Vector2(95, -105), new Vector2(120, 120), Panel, Cocoa, () => ShowPause(), 50);
            var eb = Box(hud, new Vector2(.5f, 0), new Vector2(0, 250), new Vector2(1000, 100), new Color(1, 1, 1, 0.55f));
            for (int i = 0; i < Snacks.All.Length; i++) evo.Add(Img(eb, null, new Vector2(.5f, .5f), new Vector2((i - 5) * 88, 0), new Vector2(80, 80)));
            shakeBtn = PowerBtn(new Vector2(.5f, 0), new Vector2(-250, 115), "SHAKE", () => Game.I.UseShake(), out shakeText);
            popBtn = PowerBtn(new Vector2(.5f, 0), new Vector2(250, 115), "POP", () => Game.I.UsePop(), out popText);
        }
        else
        {
            // landscape: score + next on the left of the jar, ladder + powers on the right
            // the jar spans about +-645 reference units; panels sit just outside it
            var sb = Box(hud, new Vector2(.5f, .5f), new Vector2(-880, 420), new Vector2(400, 170), Panel);
            scoreText = Txt(sb, "0", 100, new Vector2(.5f, .5f), new Vector2(0, 16), Cocoa); scoreText.fontStyle = FontStyle.Italic;
            bestText = Txt(sb, "", 32, new Vector2(.5f, .5f), new Vector2(0, -54), Soft);
            var nb = Box(hud, new Vector2(.5f, .5f), new Vector2(-820, 120), new Vector2(260, 260), Panel);
            Txt(nb, "NEXT", 34, new Vector2(.5f, 1), new Vector2(0, -32), Soft);
            nextIcon = Img(nb, null, new Vector2(.5f, .5f), new Vector2(0, -16), new Vector2(170, 170));
            Btn(hud, "II", new Vector2(0, 1), new Vector2(95, -95), new Vector2(120, 120), Panel, Cocoa, () => ShowPause(), 50);
            var eb = Box(hud, new Vector2(.5f, .5f), new Vector2(870, 250), new Vector2(380, 520), new Color(1, 1, 1, 0.55f));
            for (int i = 0; i < Snacks.All.Length; i++) evo.Add(Img(eb, null, new Vector2(.5f, .5f), new Vector2((i % 3 - 1) * 112, 190 - (i / 3) * 112), new Vector2(100, 100)));
            shakeBtn = PowerBtn(new Vector2(.5f, .5f), new Vector2(860, -150), "SHAKE", () => Game.I.UseShake(), out shakeText);
            popBtn = PowerBtn(new Vector2(.5f, .5f), new Vector2(860, -310), "POP", () => Game.I.UsePop(), out popText);
        }
        SetEvolution();
        bool playing = g != null && g.State == Game.St.Play;
        hud.gameObject.SetActive(playing);
        if (playing) { SetNext(g.NextTier); SetPowers(); }
    }

    RectTransform PowerBtn(Vector2 anchor, Vector2 pos, string label, Action a, out Text count)
    {
        var b = Btn(hud, label, anchor, pos, new Vector2(330, 120), Honey, Cocoa, a, 46);
        var badge = Img(b.transform, disc, new Vector2(1, 1), new Vector2(-14, -14), new Vector2(64, 64)); badge.color = Berry;
        count = Txt(badge.transform, "1", 36, new Vector2(.5f, .5f), Vector2.zero, Color.white, TextAnchor.MiddleCenter, 64);
        return (RectTransform)b.transform;
    }

    public static bool Landscape => (float)UnityEngine.Screen.width / Mathf.Max(1, UnityEngine.Screen.height) > 1.05f;

    public void ShowHud(bool on)
    {
        hud.gameObject.SetActive(on);
        zone.gameObject.SetActive(on);
        if (!on) Coach(null, false);
        if (on) { SetNext(Game.I.NextTier); SetPowers(); SetEvolution(); }
    }

    public void SetNext(int tier) { if (nextIcon) { nextIcon.sprite = Icon(Snacks.All[tier].id); nextIcon.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, tier / 4f); } }

    public void SetEvolution()
    {
        var g = Game.I;
        for (int i = 0; i < evo.Count; i++)
        {
            bool known = g != null && (g.Save.discovered & (1 << i)) != 0;
            evo[i].sprite = Icon(Snacks.All[i].id);
            evo[i].color = known ? Color.white : new Color(0.35f, 0.25f, 0.2f, 0.35f);
            evo[i].rectTransform.localScale = Vector3.one * (g != null && g.State == Game.St.Play && i == g.MaxTier ? 1.15f : 1f);
        }
    }

    public void SetPowers()
    {
        var g = Game.I;
        bool ads = WebBridge.AdsAvailable;
        shakeText.text = g.Shakes > 0 ? g.Shakes.ToString() : ads ? "AD" : "0";
        popText.text = g.Pops > 0 ? g.Pops.ToString() : ads ? "AD" : "0";
        shakeText.fontSize = g.Shakes > 0 ? 36 : 24; popText.fontSize = g.Pops > 0 ? 36 : 24;
        popBtn.GetComponent<Image>().color = g.PopMode ? Berry : Honey;
        shakeBtn.GetComponent<Image>().color = g.Shakes > 0 || ads ? Honey : new Color(0.8f, 0.75f, 0.7f, 1f);
    }

    public void UpdateHud(float warn)
    {
        var g = Game.I;
        scoreText.text = g.Score.ToString("N0");
        int best = g.Daily ? (g.Save.bestDailyDay == Game.Day() ? g.Save.bestDaily : 0) : g.Save.best;
        bestText.text = (g.Daily ? "DAILY BEST " : "BEST ") + Mathf.Max(best, g.Score).ToString("N0");
        warnImg.color = new Color(1, 0.1f, 0.2f, warn * (0.35f + 0.25f * Mathf.Sin(Time.time * 12f)));
        if (Input.GetKeyDown(KeyCode.Escape)) ShowPause();
    }

    // floating "+120" at the merge point
    public void PopScore(Vector3 world, int pts, string tag)
    {
        var sp = Game.I.Cam.WorldToScreenPoint(world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(pops, sp, null, out var lp);
        var half = pops.rect.size * 0.5f;
        lp.x = Mathf.Clamp(lp.x, -half.x + 220, half.x - 220);   // keep combo text on screen
        var t = Txt(pops, "+" + pts + (tag != null ? "\n" + tag : ""), tag != null ? 58 : 46, new Vector2(.5f, .5f), lp, tag != null ? Berry : Cocoa, TextAnchor.MiddleCenter, 600);
        t.fontStyle = FontStyle.Italic; Outline(t, 3, Color.white);
        StartCoroutine(Float(t));
    }
    IEnumerator Float(Text t)
    {
        float k = 0; var rt = t.rectTransform; var p0 = rt.anchoredPosition; var c = t.color;
        while (k < 1f && t)
        {
            k += Time.unscaledDeltaTime / 0.9f;
            rt.anchoredPosition = p0 + new Vector2(0, 140 * Kit.EaseOutBack(Mathf.Min(1, k * 1.6f)) * 0.8f);
            rt.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(0.4f, 1.15f, k / 0.15f) : Mathf.Lerp(1.15f, 1f, (k - 0.15f) * 3f));
            t.color = Kit.A(c, Mathf.Clamp01((1f - k) * 3f));
            yield return null;
        }
        if (t) Destroy(t.gameObject);
    }

    public void Banner(string title, string sub)
    {
        bannerText.text = title; bannerSub.text = sub; bannerT = 1.8f;
        bannerText.gameObject.SetActive(true); bannerSub.gameObject.SetActive(!string.IsNullOrEmpty(sub));
    }
    public void Toast(string s) { toastText.text = s; toastT = 2.2f; toastText.gameObject.SetActive(true); }

    // ---------------------------------------------------------------- first-game coaching
    // One short line at a time, shown in place while the player plays (no rules screen to read first).
    RectTransform coach, coachHand; Text coachText; Coroutine coachAnim;
    public void Coach(string msg, bool hand)
    {
        if (string.IsNullOrEmpty(msg)) { if (coach) coach.gameObject.SetActive(false); return; }
        if (!coach)
        {
            coach = Box(root, new Vector2(.5f, 0), Vector2.zero, new Vector2(940, 120), new Color(0.35f, 0.23f, 0.16f, 0.84f));
            coach.SetSiblingIndex(screens.GetSiblingIndex());
            coachText = Txt(coach, "", 46, new Vector2(.5f, .5f), Vector2.zero, Color.white, TextAnchor.MiddleCenter, 900);
            var h = Img(coach, disc, new Vector2(.5f, 1), new Vector2(0, 95), new Vector2(76, 76)); h.color = Kit.A(Color.white, 0.95f);
            var r = Img(h.transform, ring, new Vector2(.5f, .5f), Vector2.zero, new Vector2(110, 110)); r.color = Berry;
            coachHand = h.rectTransform;
        }
        // middle of the (still empty) jar: clear of the drop point at the top and the pile at the bottom
        coach.anchorMin = coach.anchorMax = new Vector2(.5f, .5f);
        coach.anchoredPosition = new Vector2(0, Landscape ? 40 : 120);
        coachText.text = msg;
        coachHand.gameObject.SetActive(hand);
        coach.gameObject.SetActive(true);
        if (coachAnim != null) StopCoroutine(coachAnim);
        coachAnim = StartCoroutine(CoachAnim());
    }
    IEnumerator CoachAnim()
    {
        float t0 = Time.unscaledTime;
        while (coach && coach.gameObject.activeSelf)
        {
            float t = Time.unscaledTime - t0;
            coach.localScale = Vector3.one * (t < 0.25f ? Kit.EaseOutBack(t / 0.25f) : 1f + Mathf.Sin(t * 3f) * 0.015f);
            if (coachHand.gameObject.activeSelf)
            {
                // drag left and right, then a "tap" squash to show the drop
                float cyc = t % 2.4f;
                float x = cyc < 1.8f ? Mathf.Sin(cyc / 1.8f * Mathf.PI * 2f) * 300f : 0f;
                float sq = cyc >= 1.8f ? 1f - Mathf.Sin((cyc - 1.8f) / 0.6f * Mathf.PI) * 0.3f : 1f;
                coachHand.anchoredPosition = new Vector2(x, 95);
                coachHand.localScale = Vector3.one * sq;
            }
            yield return null;
        }
        coachAnim = null;
    }

    // ---------------------------------------------------------------- screens
    RectTransform Screen(bool dim = true)
    {
        foreach (Transform c in screens) Destroy(c.gameObject);
        var s = Fill("screen", screens);
        if (dim) { var img = s.gameObject.AddComponent<Image>(); img.color = new Color(1f, 0.93f, 0.86f, 0.86f); }
        return s;
    }
    public void CloseScreens() { foreach (Transform c in screens) Destroy(c.gameObject); rankText = null; }

    IEnumerator Pop(RectTransform r, float delay = 0)
    {
        r.localScale = Vector3.zero;
        float k = -delay / 0.28f;
        while (k < 1f) { k += Time.unscaledDeltaTime / 0.28f; r.localScale = Vector3.one * Kit.EaseOutBack(Mathf.Clamp01(k)); yield return null; }
        r.localScale = Vector3.one;
    }
    IEnumerator Pulse(Transform t) { while (t) { t.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.035f); yield return null; } }
    IEnumerator Bob(RectTransform t, float phase)
    {
        var p0 = t.anchoredPosition;
        while (t) { t.anchoredPosition = p0 + new Vector2(0, Mathf.Sin(Time.unscaledTime * 2.4f + phase) * 12f); t.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.unscaledTime * 1.7f + phase) * 6f); yield return null; }
    }

    Text Title(Transform p, string s, float y, int size, Color c, float x = 0)
    {
        var sh = Txt(p, s, size, new Vector2(.5f, 1), new Vector2(x + 7, y - 10), Cocoa, TextAnchor.MiddleCenter, 1400); sh.fontStyle = FontStyle.Italic;
        var t = Txt(p, s, size, new Vector2(.5f, 1), new Vector2(x, y), c, TextAnchor.MiddleCenter, 1400);
        t.fontStyle = FontStyle.Italic; Outline(t, 4, Color.white);
        return t;
    }

    RectTransform Column(RectTransform s)
    {
        // a portrait column: centred on phones, right of the jar in landscape
        var p = Rect("col", s, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1080, 1920));
        if (Landscape) { p.anchorMin = p.anchorMax = new Vector2(1, .5f); p.pivot = new Vector2(1, .5f); p.anchoredPosition = new Vector2(-20, 0); }
        return p;
    }

    public void ShowMenu()
    {
        ShowHud(false);
        var s = Column(Screen(false));
        var g = Game.I;
        var logo = Box(s, new Vector2(.5f, 1), new Vector2(0, -330), new Vector2(940, 430), Panel);
        Title(logo, "SNACK", -110, 170, Berry);
        Title(logo, "MERGE", -265, 170, Honey);
        // a few snacks bobbing round the logo
        int[] deco = { 0, 3, 5, 7, 10 };
        Vector2[] at = { new Vector2(-400, 150), new Vector2(410, 160), new Vector2(-420, -150), new Vector2(430, -140), new Vector2(0, 230) };
        for (int i = 0; i < deco.Length; i++) { var im = Img(logo, Icon(Snacks.All[deco[i]].id), new Vector2(.5f, .5f), at[i], new Vector2(150, 150)); StartCoroutine(Bob(im.rectTransform, i)); }
        Txt(s, "drop  -  match  -  merge  -  make a WATERMELON!", 34, new Vector2(.5f, 1), new Vector2(0, -600), Cocoa, TextAnchor.MiddleCenter, 1000);

        var play = Btn(s, "PLAY", new Vector2(.5f, 0), new Vector2(0, 900), new Vector2(700, 190), Berry, Color.white, () => g.StartGame(false), 96);
        StartCoroutine(Pulse(play.transform));
        Txt(s, "BEST  " + g.Save.best.ToString("N0"), 38, new Vector2(.5f, 0), new Vector2(0, 775), Cocoa);
        int dailyBest = g.Save.bestDailyDay == Game.Day() ? g.Save.bestDaily : 0;
        Btn(s, "DAILY JAR", new Vector2(.5f, 0), new Vector2(0, 640), new Vector2(700, 150), Mint, Color.white, () => g.StartGame(true), 64);
        Txt(s, "same snacks for everyone today" + (dailyBest > 0 ? "   -   your best " + dailyBest.ToString("N0") : ""), 30, new Vector2(.5f, 0), new Vector2(0, 535), Soft, TextAnchor.MiddleCenter, 1000);
        Btn(s, "LEADERBOARD", new Vector2(.5f, 0), new Vector2(0, 400), new Vector2(700, 120), Honey, Cocoa, () => WebBridge.ShowBoard("daily"), 48);
        Btn(s, "HOW TO PLAY", new Vector2(.5f, 0), new Vector2(-180, 250), new Vector2(340, 110), Panel, Cocoa, ShowHowTo, 36);
        Btn(s, g.Save.muted ? "SOUND OFF" : "SOUND ON", new Vector2(.5f, 0), new Vector2(180, 250), new Vector2(340, 110), Panel, Cocoa, () => { g.ToggleMute(); ShowMenu(); }, 36);
        int found = 0; for (int i = 0; i < Snacks.All.Length; i++) if ((g.Save.discovered & (1 << i)) != 0) found++;
        Txt(s, "SNACKS DISCOVERED  " + found + " / " + Snacks.All.Length, 30, new Vector2(.5f, 0), new Vector2(0, 140), Soft, TextAnchor.MiddleCenter, 1000);
    }

    public void ShowHowTo()
    {
        var s = Column(Screen());
        Title(s, "HOW TO PLAY", -220, 104, Berry);
        // the ladder
        var lad = Box(s, new Vector2(.5f, 1), new Vector2(0, -480), new Vector2(980, 260), Panel);
        for (int i = 0; i < Snacks.All.Length; i++) Img(lad, Icon(Snacks.All[i].id), new Vector2(.5f, .5f), new Vector2((i - 5) * 86, i % 2 == 0 ? 30 : -30), new Vector2(84, 84));
        string[] rows =
        {
            "Aim with your finger or mouse,\nlet go (or click) to DROP",
            "Two of the same snack MERGE\ninto the next one up the ladder",
            "Chain merges fast for COMBO points",
            "Two WATERMELONS = JACKPOT!",
            "Don't let snacks sit above the\nred line - the jar overflows!",
            "SHAKE and POP save a messy jar.\nDAILY JAR: everyone gets the same snacks",
        };
        for (int i = 0; i < rows.Length; i++)
        {
            var row = Box(s, new Vector2(.5f, 1), new Vector2(0, -690 - i * 150), new Vector2(960, 130), new Color(1, 1, 1, 0.7f));
            var t = Txt(row, rows[i], 34, new Vector2(.5f, .5f), Vector2.zero, Cocoa, TextAnchor.MiddleCenter, 920);
            t.lineSpacing = 1.05f;
            StartCoroutine(Pop(row, 0.05f * i));
        }
        Btn(s, "LET'S COOK!", new Vector2(.5f, 0), new Vector2(0, 150), new Vector2(600, 150), Berry, Color.white, () =>
        {
            Game.I.Save.howto = true; Game.I.Persist();
            if (Game.I.State == Game.St.Menu) ShowMenu(); else CloseScreens();
        }, 60);
    }

    public void ShowPause()
    {
        var g = Game.I;
        if (g.State != Game.St.Play) return;
        Time.timeScale = 0;
        WebBridge.Gameplay(false);
        var s = Column(Screen());
        Title(s, "PAUSED", -560, 130, Berry);
        Btn(s, "RESUME", new Vector2(.5f, .5f), new Vector2(0, 160), new Vector2(600, 160), Mint, Color.white, () => { Time.timeScale = 1; CloseScreens(); WebBridge.Gameplay(true); }, 64);
        Btn(s, "RESTART", new Vector2(.5f, .5f), new Vector2(0, -40), new Vector2(600, 130), Panel, Cocoa, () => { Time.timeScale = 1; g.StartGame(g.Daily); }, 48);
        Btn(s, "MENU", new Vector2(.5f, .5f), new Vector2(0, -210), new Vector2(600, 130), Panel, Berry, () => { Time.timeScale = 1; WebBridge.Event("quit_midgame", g.Drops); g.Quit(); }, 48);
    }

    public void ShowOver(bool best)
    {
        ShowHud(false);
        var s = Column(Screen());
        var g = Game.I;
        Title(s, best ? "NEW BEST!" : "JAR FULL!", -230, 120, best ? Mint : Berry);
        var big = Txt(s, g.Score.ToString("N0"), 160, new Vector2(.5f, 1), new Vector2(0, -420), Cocoa, TextAnchor.MiddleCenter, 1000);
        big.fontStyle = FontStyle.Italic; Outline(big, 5, Color.white);
        Txt(s, g.Daily ? "DAILY JAR  -  " + DateTime.UtcNow.ToString("MMM d").ToUpper() : "CLASSIC", 36, new Vector2(.5f, 1), new Vector2(0, -540), Soft);
        rankText = Txt(s, "", 38, new Vector2(.5f, 1), new Vector2(0, -600), Mint, TextAnchor.MiddleCenter, 1000);
        if (lastRank != null) ApplyRank();
        var mb = Box(s, new Vector2(.5f, 1), new Vector2(0, -790), new Vector2(620, 250), Panel);
        Txt(mb, "BIGGEST SNACK", 32, new Vector2(.5f, 1), new Vector2(0, -36), Soft);
        Img(mb, Icon(Snacks.All[g.MaxTier].id), new Vector2(.5f, .5f), new Vector2(-150, -20), new Vector2(150, 150));
        Txt(mb, Snacks.All[g.MaxTier].name, 50, new Vector2(.5f, .5f), new Vector2(90, -20), Cocoa, TextAnchor.MiddleCenter, 400).fontStyle = FontStyle.Italic;

        float y = 720;
        if (g.Continues > 0)
        {
            var c = Btn(s, WebBridge.AdsAvailable ? "CONTINUE  (AD)" : "CONTINUE", new Vector2(.5f, 0), new Vector2(0, y), new Vector2(700, 150), Mint, Color.white, () => g.Continue(), 56);
            StartCoroutine(Pulse(c.transform));
            y -= 175;
        }
        var again = Btn(s, "PLAY AGAIN", new Vector2(.5f, 0), new Vector2(0, y), new Vector2(700, 160), Berry, Color.white, () => g.StartGame(g.Daily), 66);
        if (g.Continues <= 0) StartCoroutine(Pulse(again.transform));
        y -= 170;
        float rx = 0, mx = 240;
        if (!WebBridge.OnPortal)
        {
            var share = Btn(s, "SHARE", new Vector2(.5f, 0), new Vector2(-240, y), new Vector2(220, 120), Panel, Cocoa, () => { }, 40);
            share.gameObject.AddComponent<ShareOnPress>().Text = () => g.ShareText();
        }
        else { rx = -125; mx = 125; }
        Btn(s, "RANKS", new Vector2(.5f, 0), new Vector2(rx, y), new Vector2(220, 120), Honey, Cocoa, () => WebBridge.ShowBoard(g.Daily ? "daily" : "classic"), 40);
        Btn(s, "MENU", new Vector2(.5f, 0), new Vector2(mx, y), new Vector2(220, 120), Panel, Cocoa, () => g.Quit(), 40);
    }

    Game.RankMsg lastRank;
    public void SetRank(Game.RankMsg m) { lastRank = m; ApplyRank(); }
    public void ClearRank() => lastRank = null;
    void ApplyRank()
    {
        if (!rankText || lastRank == null) return;
        if (lastRank.rank > 0) { rankText.text = (lastRank.mode == "daily" ? "TODAY'S RANK  #" : "WORLD RANK  #") + lastRank.rank + " OF " + lastRank.total; rankText.color = lastRank.rank <= 10 ? Berry : Mint; }
        else if (!string.IsNullOrEmpty(lastRank.error) && lastRank.error != "skipped") { rankText.text = "SAVED ON THIS DEVICE"; rankText.color = Soft; }
    }

    // ---------------------------------------------------------------- per frame
    void Update()
    {
        float aspect = (float)UnityEngine.Screen.width / Mathf.Max(1, UnityEngine.Screen.height);
        scaler.matchWidthOrHeight = aspect > 0.75f ? 1f : 0f;
        if (Landscape != landscapeHud)
        {
            BuildHud();
            if (Game.I && Game.I.State == Game.St.Menu && screens.childCount > 0) ShowMenu();
        }
        float udt = Time.unscaledDeltaTime;
        if (bannerT > 0)
        {
            bannerT -= udt;
            float k = bannerT > 1.5f ? (1.8f - bannerT) / 0.3f : 1f;
            bannerText.rectTransform.localScale = Vector3.one * Kit.EaseOutBack(Mathf.Clamp01(k));
            float a = Mathf.Clamp01(bannerT * 2f);
            bannerText.color = Kit.A(bannerText.color, a); bannerSub.color = Kit.A(bannerSub.color, a);
            if (bannerT <= 0) { bannerText.gameObject.SetActive(false); bannerSub.gameObject.SetActive(false); }
        }
        if (toastT > 0)
        {
            toastT -= udt;
            toastText.color = Kit.A(toastText.color, Mathf.Clamp01(toastT * 2f));
            if (toastT <= 0) toastText.gameObject.SetActive(false);
        }
    }
}

// Aim while the finger/mouse is down (desktop: the mouse aims even without pressing); release drops.
public class PlayZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public bool Released;
    public Vector2 Last;
    bool down;
    public Vector2? Pointer
    {
        get
        {
            if (down) return Last;
            if (!Input.touchSupported && Input.mousePresent && UnityEngine.Screen.safeArea.Contains(Input.mousePosition)) return (Vector2)Input.mousePosition;
            return null;
        }
    }
    public void OnPointerDown(PointerEventData e) { down = true; Last = e.position; }
    public void OnDrag(PointerEventData e) { Last = e.position; }
    public void OnPointerUp(PointerEventData e) { if (!down) return; down = false; Last = e.position; Released = true; }
    void OnDisable() { down = false; Released = false; }
}
