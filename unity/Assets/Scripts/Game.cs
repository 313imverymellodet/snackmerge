using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    public int best, bestDaily, bestDailyDay, games, maxTier = 0;
    public bool muted, howto;
    public int discovered = 1;          // bitmask of tiers ever made
}

// SNACK MERGE: drop snacks into the jar; two of a kind merge into the next one up.
// CLASSIC uses a random drop order; DAILY uses the same seeded order for everyone today.
public class Game : MonoBehaviour
{
    public static Game I;
    public enum St { Menu, Play, Over }
    public St State = St.Menu;
    public SaveData Save;
    public Camera Cam;
    public bool Daily, Dev;
    public int Score, Drops, Combo, MaxTier, Shakes, Pops, Continues;
    public bool PopMode;
    public int CurTier, NextTier;
    public readonly List<Item> Items = new List<Item>();

    public const float W = 7.0f, H = 9.6f, Danger = 8.5f, DropY = 10.8f;
    Transform world, jar, guide;
    Item held;
    float aimX, dropCd, comboT, overT, shake, playTime, camX;
    System.Random rng;
    Light sun;
    LineRenderer dangerLine;

    void Awake()
    {
        I = this;
        Application.targetFrameRate = -1;
        QualitySettings.antiAliasing = 4; QualitySettings.pixelLightCount = 0;
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = false;
#endif
        var url = Application.absoluteURL ?? "";
        Dev = url.Contains("dev=1");
        DevCam.Install(Dev);
        var json = PlayerPrefs.GetString("sm_save", "");
        Save = string.IsNullOrEmpty(json) ? new SaveData() : JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
        if (Dev && url.Contains("fresh=1")) Save = new SaveData();

        Physics2D.gravity = new Vector2(0, -9.81f);
        Physics2D.velocityIterations = 10; Physics2D.positionIterations = 6;
        gameObject.AddComponent<Sfx>();
        Sfx.I.SetMuted(Save.muted);
        new GameObject("WebBridge").AddComponent<WebBridge>();
        Cam = Camera.main;
        Cam.orthographic = true; Cam.nearClipPlane = 0.1f; Cam.farClipPlane = 100f;
        Cam.transform.position = new Vector3(0, 6, -20); Cam.transform.rotation = Quaternion.identity;
        Cam.clearFlags = CameraClearFlags.SolidColor; Cam.backgroundColor = Kit.Hex("#FFE9D6");
        sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional; sun.intensity = 1.05f; sun.color = Kit.Hex("#FFF6EA");
        sun.transform.rotation = Quaternion.Euler(35, -30, 0); sun.shadows = LightShadows.None;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Kit.Hex("#FFF1E6"); RenderSettings.ambientEquatorColor = Kit.Hex("#E9C9B8"); RenderSettings.ambientGroundColor = Kit.Hex("#B08878");
        world = new GameObject("World").transform;
        BuildJar();
        FX.Init(world);
        new GameObject("UI").AddComponent<UI>().Init();
        GoMenu();
        if (!Save.howto) UI.I.ShowHowTo();
        WebBridge.Ready();
    }

    public void Persist() { PlayerPrefs.SetString("sm_save", JsonUtility.ToJson(Save)); PlayerPrefs.Save(); }

    // ------------------------------------------------------------------ the jar
    void BuildJar()
    {
        jar = new GameObject("Jar").transform; jar.SetParent(world, false);
        var wood = Mat(Kit.Hex("#C98B5B"), Kit.Noise(64, Kit.Hex("#C98B5B"), Kit.Hex("#B37548"), 0.25f, 3));
        var woodDark = Mat(Kit.Hex("#A86A40"));
        var back = Mat(Kit.Hex("#FFF7EC"), Dots(Kit.Hex("#FFF7EC"), Kit.Hex("#FFE3CC")));
        back.mainTextureScale = new Vector2(3, 4);
        back.color = Kit.Hex("#FFEBDA");   // warm cream rather than stark white under the light
        Box("back", new Vector3(0, H / 2f, 1.2f), new Vector3(W, H + 0.4f, 0.2f), back);
        Box("floor", new Vector3(0, -0.35f, 0), new Vector3(W + 1.2f, 0.7f, 2.6f), wood);
        Box("wallL", new Vector3(-W / 2f - 0.3f, H / 2f, 0), new Vector3(0.6f, H + 0.6f, 2.6f), wood);
        Box("wallR", new Vector3(W / 2f + 0.3f, H / 2f, 0), new Vector3(0.6f, H + 0.6f, 2.6f), wood);
        Box("lipL", new Vector3(-W / 2f - 0.3f, H + 0.35f, 0), new Vector3(0.9f, 0.25f, 2.8f), woodDark);
        Box("lipR", new Vector3(W / 2f + 0.3f, H + 0.35f, 0), new Vector3(0.9f, 0.25f, 2.8f), woodDark);
        // physics walls
        Wall(new Vector2(0, -0.5f), new Vector2(W + 4, 1f));
        Wall(new Vector2(-W / 2f - 0.5f, H), new Vector2(1f, H * 2.4f));
        Wall(new Vector2(W / 2f + 0.5f, H), new Vector2(1f, H * 2.4f));
        // danger line
        dangerLine = new GameObject("danger").AddComponent<LineRenderer>();
        dangerLine.transform.SetParent(jar, false);
        dangerLine.useWorldSpace = true; dangerLine.positionCount = 2;
        dangerLine.SetPositions(new[] { new Vector3(-W / 2f, Danger, -0.2f), new Vector3(W / 2f, Danger, -0.2f) });
        dangerLine.widthMultiplier = 0.07f; dangerLine.sharedMaterial = new Material(Kit.UnlitAlpha);
        dangerLine.textureMode = LineTextureMode.Tile; dangerLine.sharedMaterial.mainTexture = Dash();
        dangerLine.startColor = dangerLine.endColor = new Color(1f, 0.35f, 0.4f, 0.55f);
        // drop guide
        var g = new GameObject("guide").AddComponent<LineRenderer>();
        g.useWorldSpace = false; g.positionCount = 2; g.widthMultiplier = 0.05f;
        g.sharedMaterial = new Material(Kit.UnlitAlpha) { mainTexture = Dash() }; g.textureMode = LineTextureMode.Tile;
        g.startColor = g.endColor = new Color(1, 1, 1, 0.7f);
        guide = g.transform; guide.SetParent(world, false);
        // backdrop: big soft blobs
        var rnd = new System.Random(5);
        Color[] cols = { Kit.Hex("#FFD4C2"), Kit.Hex("#FFE3A8"), Kit.Hex("#D9F2D0"), Kit.Hex("#FFD0E4") };
        for (int i = 0; i < 14; i++)
        {
            var blob = Kit.MeshObject("blob", Kit.UVSphere);
            blob.transform.SetParent(world, false);
            float x = (float)(rnd.NextDouble() - 0.5) * 40f, y = (float)rnd.NextDouble() * 22f - 6f;
            if (Mathf.Abs(x) < W / 2f + 1.5f) x += Mathf.Sign(x == 0 ? 1 : x) * 6f;
            blob.transform.position = new Vector3(x, y, 8f + (float)rnd.NextDouble() * 4f);
            blob.transform.localScale = new Vector3(1f, 1f, 0.2f) * (1.2f + (float)rnd.NextDouble() * 2.2f);
            var mr = blob.GetComponent<MeshRenderer>(); mr.sharedMaterial = Mat(cols[i % cols.Length]); mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    static Material Mat(Color c, Texture t = null)
    {
        var m = new Material(Shader.Find("Standard")) { color = Color.white };
        if (t) m.mainTexture = t; else m.color = c;
        m.SetFloat("_Glossiness", 0.12f);
        return m;
    }
    void Box(string n, Vector3 c, Vector3 s, Material m)
    {
        var go = Kit.MeshObject(n, BoxMesh(s));
        go.transform.SetParent(jar, false); go.transform.localPosition = c;
        go.GetComponent<MeshRenderer>().sharedMaterial = m;
    }
    void Wall(Vector2 c, Vector2 s)
    {
        var go = new GameObject("wall"); go.transform.SetParent(jar, false); go.transform.position = c;
        var bc = go.AddComponent<BoxCollider2D>(); bc.size = s;
        bc.sharedMaterial = new PhysicsMaterial2D { friction = 0.4f, bounciness = 0.05f };
    }
    static Mesh BoxMesh(Vector3 s)
    {
        var m = new Mesh();
        float x = s.x / 2, y = s.y / 2, z = s.z / 2;
        Vector3[] c = { new Vector3(-x, -y, -z), new Vector3(x, -y, -z), new Vector3(x, y, -z), new Vector3(-x, y, -z), new Vector3(-x, -y, z), new Vector3(x, -y, z), new Vector3(x, y, z), new Vector3(-x, y, z) };
        int[][] f = { new[] { 0, 3, 2, 1 }, new[] { 5, 6, 7, 4 }, new[] { 4, 7, 3, 0 }, new[] { 1, 2, 6, 5 }, new[] { 3, 7, 6, 2 }, new[] { 4, 0, 1, 5 } };
        Vector3[] n = { Vector3.back, Vector3.forward, Vector3.left, Vector3.right, Vector3.up, Vector3.down };
        var v = new Vector3[24]; var nn = new Vector3[24]; var uv = new Vector2[24]; var tri = new int[36];
        for (int i = 0; i < 6; i++)
        {
            for (int k = 0; k < 4; k++) { v[i * 4 + k] = c[f[i][k]]; nn[i * 4 + k] = n[i]; }
            uv[i * 4] = new Vector2(0, 0); uv[i * 4 + 1] = new Vector2(0, 1); uv[i * 4 + 2] = new Vector2(1, 1); uv[i * 4 + 3] = new Vector2(1, 0);
            int b = i * 4, t = i * 6; tri[t] = b; tri[t + 1] = b + 1; tri[t + 2] = b + 2; tri[t + 3] = b; tri[t + 4] = b + 2; tri[t + 5] = b + 3;
        }
        m.vertices = v; m.normals = nn; m.uv = uv; m.triangles = tri; m.RecalculateBounds();
        return m;
    }
    static Texture2D Dots(Color a, Color b)
    {
        int n = 64; var t = new Texture2D(n, n, TextureFormat.RGBA32, true);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float d1 = Vector2.Distance(new Vector2(x, y), new Vector2(16, 16)), d2 = Vector2.Distance(new Vector2(x, y), new Vector2(48, 48));
            t.SetPixel(x, y, Mathf.Min(d1, d2) < 6 ? b : a);
        }
        t.Apply(); t.wrapMode = TextureWrapMode.Repeat; return t;
    }
    static Texture2D dash;
    static Texture2D Dash()
    {
        if (dash) return dash;
        dash = new Texture2D(8, 1, TextureFormat.RGBA32, false);
        for (int i = 0; i < 8; i++) dash.SetPixel(i, 0, i < 5 ? Color.white : new Color(1, 1, 1, 0));
        dash.Apply(); dash.wrapMode = TextureWrapMode.Repeat; dash.filterMode = FilterMode.Point;
        return dash;
    }

    // ------------------------------------------------------------------ flow
    public void GoMenu()
    {
        State = St.Menu;
        Time.timeScale = 1;
        Clear();
        // a little still life in the jar behind the title
        var r = new System.Random(3);
        for (int i = 0; i < 14; i++)
        {
            int t = r.Next(0, 7);
            var it = Item.Create(t, new Vector2((float)(r.NextDouble() - 0.5) * (W - 2f), 1f + i * 0.9f), world, true);
            Items.Add(it);
        }
        UI.I.ShowMenu();
        WebBridge.Gameplay(false);
    }

    void Clear()
    {
        foreach (var it in Items) if (it) Destroy(it.gameObject);
        Items.Clear();
        if (held) Destroy(held.gameObject);
        held = null;
    }

    public void StartGame(bool daily)
    {
        Daily = daily;
        Clear();
        Score = 0; Drops = 0; Combo = 0; MaxTier = 0; overT = 0; playTime = 0;
        Shakes = 1; Pops = 1; Continues = 1; PopMode = false;
        int seed = daily ? Day() * 7919 : Environment.TickCount;
        rng = new System.Random(seed);
        CurTier = Roll(); NextTier = Roll();
        aimX = 0; dropCd = 0.3f;
        State = St.Play;
        SpawnHeld();
        Save.games++; Persist();
        UI.I.CloseScreens();
        UI.I.ShowHud(true);
        Sfx.I.StartMusic();
        WebBridge.Gameplay(true);
        WebBridge.RunStart(daily ? "daily" : "classic");
        WebBridge.Event(daily ? "start_daily" : "start_classic");
    }

    public static int Day() { var d = DateTime.UtcNow; return d.Year * 10000 + d.Month * 100 + d.Day; }

    int Roll()
    {
        // early drops stay tiny; bigger ones come in once the jar has something to work with
        int max = Drops < 3 ? 3 : Snacks.DropTiers;
        return rng.Next(0, max);
    }

    void SpawnHeld()
    {
        held = Item.Create(CurTier, new Vector2(aimX, DropY), world, false);
    }

    void Drop()
    {
        if (held == null || dropCd > 0) return;
        float r = held.R;
        aimX = Mathf.Clamp(aimX, -W / 2f + r, W / 2f - r);
        held.transform.position = new Vector3(aimX, DropY, 0);
        held.EnablePhysics();
        held.Rb.linearVelocity = new Vector2(0, -2f);
        Items.Add(held);
        held = null;
        Drops++;
        Sfx.I.Drop(CurTier);
        CurTier = NextTier; NextTier = Roll();
        dropCd = 0.45f;
        UI.I.SetNext(NextTier);
    }

    public void Merge(Item a, Item b)
    {
        if (State == St.Over) return;
        a.Merging = b.Merging = true;
        int t = a.Tier;
        var pos = (a.transform.position + b.transform.position) / 2f;
        var vel = (a.Rb.linearVelocity + b.Rb.linearVelocity) / 2f;
        Items.Remove(a); Items.Remove(b);
        Destroy(a.gameObject); Destroy(b.gameObject);
        var def = Snacks.All[t];
        if (State != St.Play)
        {
            // the menu's still life merges quietly: no score, sound or discoveries
            if (t < Snacks.All.Length - 1) { var q = Item.Create(t + 1, pos, world, true); q.Rb.linearVelocity = vel * 0.5f; Items.Add(q); }
            FX.Burst(pos, def.color, 8, 0.6f);
            return;
        }
        comboT = 1.3f; Combo++;
        int mult = Mathf.Clamp(Combo, 1, 10);
        if (t == Snacks.All.Length - 1)
        {
            // two watermelons: the jackpot
            int bonus = 500 * mult;
            AddScore(bonus, pos, "JACKPOT!");
            FX.Burst(pos, def.color, 60, 1.6f); FX.Confetti(pos, 80);
            Sfx.I.Jackpot(); Shake(0.8f);
            WebBridge.Vibrate(120);
            return;
        }
        int nt = t + 1;
        var it = Item.Create(nt, pos, world, true);
        it.Rb.linearVelocity = vel * 0.5f + Vector2.up * 1.2f;
        Items.Add(it);
        int pts = Snacks.Points(nt) * mult;
        AddScore(pts, pos, Combo >= 2 ? "x" + Combo + " COMBO" : null);
        FX.Burst(pos, Snacks.All[nt].color, 10 + nt * 3, 0.6f + nt * 0.12f);
        Sfx.I.Merge(nt, Combo);
        if (nt >= 6) Shake(0.15f + nt * 0.04f);
        WebBridge.Vibrate(8 + nt * 4);
        if (nt > MaxTier) MaxTier = nt;
        bool fresh = (Save.discovered & (1 << nt)) == 0;
        if (fresh)
        {
            Save.discovered |= 1 << nt;
            UI.I.Banner("NEW SNACK!", Snacks.All[nt].name);
            Sfx.I.Discover();
            Persist();
        }
        UI.I.SetEvolution();
    }

    void AddScore(int pts, Vector3 at, string tag)
    {
        Score += pts;
        UI.I.PopScore(at, pts, tag);
    }

    public void Shake(float a) => shake = Mathf.Min(1f, shake + a);

    // ------------------------------------------------------------------ powers
    public void UseShake()
    {
        if (State != St.Play) return;
        if (Shakes > 0) { Shakes--; DoShake(); return; }
        if (WebBridge.AdsAvailable) WebBridge.I.ShowRewarded(ok => { if (ok) DoShake(); });
        else UI.I.Toast("NO SHAKES LEFT");
    }
    void DoShake()
    {
        foreach (var it in Items) if (it && it.Rb) it.Rb.AddForce(new Vector2(UnityEngine.Random.Range(-1f, 1f) * 6f, UnityEngine.Random.Range(4f, 9f)) * it.Rb.mass, ForceMode2D.Impulse);
        Shake(0.7f); Sfx.I.Shake();
        UI.I.SetPowers();
    }

    public void UsePop()
    {
        if (State != St.Play) return;
        if (Pops > 0) { PopMode = !PopMode; UI.I.SetPowers(); if (PopMode) UI.I.Toast("TAP A SNACK TO POP IT"); return; }
        if (WebBridge.AdsAvailable) WebBridge.I.ShowRewarded(ok => { if (ok) { Pops++; PopMode = true; UI.I.SetPowers(); UI.I.Toast("TAP A SNACK TO POP IT"); } });
        else UI.I.Toast("NO POPS LEFT");
    }
    void PopAt(Vector2 p)
    {
        Item best = null; float bd = 9f;
        foreach (var it in Items)
        {
            if (!it || it.Merging) continue;
            float d = Vector2.Distance(p, it.transform.position) - it.R;
            if (d < bd) { bd = d; best = it; }
        }
        if (best == null || bd > 0.4f) return;
        Pops--; PopMode = false;
        Items.Remove(best);
        FX.Burst(best.transform.position, Snacks.All[best.Tier].color, 24, 1f);
        Sfx.I.Pop();
        Destroy(best.gameObject);
        UI.I.SetPowers();
    }

    public void Continue()
    {
        if (State != St.Over || Continues <= 0) return;
        Action go = () =>
        {
            Continues--;
            // clear everything poking above two thirds of the jar
            for (int i = Items.Count - 1; i >= 0; i--)
                if (Items[i].Top > H * 0.62f) { FX.Burst(Items[i].transform.position, Snacks.All[Items[i].Tier].color, 14, 0.8f); Destroy(Items[i].gameObject); Items.RemoveAt(i); }
            foreach (var it in Items) if (it && it.Rb) it.Rb.simulated = true;
            overT = 0; State = St.Play; dropCd = 0.5f;
            if (held == null) SpawnHeld();
            UI.I.CloseScreens(); UI.I.ShowHud(true);
            WebBridge.Gameplay(true);
        };
        if (WebBridge.AdsAvailable) WebBridge.I.ShowRewarded(ok => { if (ok) go(); });
        else go();
    }

    void GameOver()
    {
        State = St.Over;
        foreach (var it in Items) if (it && it.Rb) it.Rb.simulated = false;
        if (held) { Destroy(held.gameObject); held = null; }
        Sfx.I.Over();
        Shake(0.5f);
        bool best = false;
        if (Daily)
        {
            if (Save.bestDailyDay != Day()) { Save.bestDailyDay = Day(); Save.bestDaily = 0; }
            if (Score > Save.bestDaily) { Save.bestDaily = Score; best = true; }
        }
        else if (Score > Save.best) { Save.best = Score; best = true; }
        if (MaxTier > Save.maxTier) Save.maxTier = MaxTier;
        Persist();
        UI.I.ClearRank();
        WebBridge.RunSubmit(Daily ? "daily" : "classic", Score, Drops, MaxTier);
        WebBridge.Gameplay(false);
        WebBridge.Event(Daily ? "over_daily" : "over_classic", Score);
        StartCoroutine(ShowOver(best));
    }

    IEnumerator ShowOver(bool best)
    {
        yield return new WaitForSecondsRealtime(1.1f);
        UI.I.ShowOver(best);
    }

    // ------------------------------------------------------------------ frame
    void Update()
    {
        float dt = Time.deltaTime;
        if (Input.anyKeyDown || Input.touchCount > 0) Sfx.I.StartMusic();
        FX.Tick(dt);
        if (State != St.Play) { FitCamera(); return; }
        playTime += dt;
        dropCd -= dt;
        if (comboT > 0) { comboT -= dt; if (comboT <= 0) Combo = 0; }

        // aim: follow the pointer while it's down; keyboard nudges
        var ptr = UI.I.Pointer;
        if (ptr.HasValue && !PopMode) aimX = Cam.ScreenToWorldPoint(ptr.Value).x;
        float k = (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) ? 1 : 0);
        aimX += k * 7f * dt;
        if (held == null && dropCd <= 0) SpawnHeld();
        if (held)
        {
            float r = held.R;
            aimX = Mathf.Clamp(aimX, -W / 2f + r, W / 2f - r);
            held.transform.position = Vector3.Lerp(held.transform.position, new Vector3(aimX, DropY, 0), 1f - Mathf.Exp(-dt * 30f));
            held.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 3f) * 8f);
        }
        if (UI.I.Released) { if (PopMode) PopAt(Cam.ScreenToWorldPoint(UI.I.LastPointer)); else Drop(); }
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) Drop();

        // guide line under the held snack
        guide.gameObject.SetActive(held != null);
        if (held)
        {
            var lr = guide.GetComponent<LineRenderer>();
            lr.SetPosition(0, new Vector3(aimX, DropY - held.R, -0.1f));
            lr.SetPosition(1, new Vector3(aimX, 0, -0.1f));
        }

        // danger: something settled above the line for too long ends the run
        bool over = false;
        foreach (var it in Items)
        {
            if (!it || it.Merging) continue;
            it.Age += dt;
            if (it.Age > 1.2f && it.Top > Danger) { it.OverTime += dt; if (it.OverTime > 0.1f) over = true; }
            else it.OverTime = 0;
        }
        overT = over ? overT + dt : Mathf.Max(0, overT - dt * 2f);
        float warn = Mathf.Clamp01(overT / 2.4f);
        dangerLine.startColor = dangerLine.endColor = Color.Lerp(new Color(1f, 0.35f, 0.4f, 0.45f), new Color(1f, 0.1f, 0.15f, 1f), over ? 0.5f + 0.5f * Mathf.Sin(Time.time * 14f) : 0f);
        dangerLine.widthMultiplier = over ? 0.11f : 0.07f;
        if (over && Mathf.Repeat(Time.time, 0.5f) < dt) Sfx.I.Warn();
        if (overT > 2.4f) GameOver();
        UI.I.UpdateHud(warn);
        FitCamera();
    }

    void FitCamera()
    {
        float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
        bool portrait = aspect < 0.9f;
        // portrait: jar fills the width with room for the HUD above and powers below
        float needW = W + 1.6f, needH = portrait ? H + 6.4f : H + 3.9f;
        float size = Mathf.Max(needH / 2f, needW / 2f / aspect);
        Cam.orthographicSize = size;
        float cy = portrait ? H / 2f + 1.1f : H / 2f + 0.75f;
        shake = Mathf.Max(0, shake - Time.unscaledDeltaTime * 2.5f);
        var sh = new Vector3(Mathf.PerlinNoise(Time.time * 35f, 0) - .5f, Mathf.PerlinNoise(0, Time.time * 35f) - .5f, 0) * shake * shake * 0.6f;
        // landscape menus: slide the jar left so the menu column on the right doesn't cover it
        float cx = UI.Landscape && State != St.Play ? size * 2f * (640f / 1920f) : 0f;
        camX = Mathf.Lerp(camX, cx, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 6f));
        Cam.transform.position = new Vector3(camX, cy, -20) + sh;
    }

    public string ShareText() => "I scored " + Score.ToString("N0") + " in SNACK MERGE" + (Daily ? " (daily jar)" : "") + " and made a " + Snacks.All[MaxTier].name + "! Can you beat it?";

    [Serializable] public class RankMsg { public int rank, total, best; public string error, mode; }
    public void OnRank(string json)
    {
        var m = JsonUtility.FromJson<RankMsg>(json);
        if (m != null) UI.I.SetRank(m);
    }

    // dev: SendMessage("Game", "DevFill", "") tips a pile of mixed snacks into the jar
    public void DevFill(string _)
    {
        if (!Dev || State != St.Play) return;
        for (int i = 0; i < 26; i++)
        {
            int t = UnityEngine.Random.Range(0, 7);
            var it = Item.Create(t, new Vector2(UnityEngine.Random.Range(-W / 2f + 1f, W / 2f - 1f), H + i * 0.5f), world, true);
            it.Serial += 100000;
            Items.Add(it);
        }
    }

    public void ToggleMute() { Save.muted = !Save.muted; Sfx.I.SetMuted(Save.muted); Persist(); }
    public void Quit() { Time.timeScale = 1; GoMenu(); }
}
