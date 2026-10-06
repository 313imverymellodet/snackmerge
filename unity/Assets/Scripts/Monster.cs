using UnityEngine;

// The snack monster. It leans on the jar, craves one snack at a time and snatches it out with its tongue
// the moment you merge one. Wait too long and it gets grumpy and shakes the jar.
public class Monster : MonoBehaviour
{
    public static Monster I;
    public int Craving = -1;              // tier it wants (-1: thinking)
    public int Meals;
    public float Patience, PatienceMax;
    public bool Eating => eatT > 0;

    Transform body, head, eyeL, eyeR, pupilL, pupilR, lidL, lidR, mouth, tongueTip, bubble, bubbleItemHolder;
    LineRenderer tongue, ring;
    Material bodyMat, mouthMat;
    Item food;                            // snack on its way to the mouth
    Item bubbleItem;
    float eatT, chompT, blinkT = 2f, grumpT, thinkT, bob, growl;
    Vector3 foodFrom;
    static readonly Color Fur = Kit.Hex("#8A5CF6"), FurAngry = Kit.Hex("#E2463B"), Belly = Kit.Hex("#C9B6FF");
    const float EatTime = 0.55f;

    public static Monster Make(Transform parent)
    {
        var go = new GameObject("Monster");
        go.transform.SetParent(parent, false);
        var m = go.AddComponent<Monster>();
        I = m;
        m.Build();
        return m;
    }

    static Material Mat(Color c, float gloss = 0.25f)
    {
        var m = new Material(Shader.Find("Standard")) { color = c };
        m.SetFloat("_Glossiness", gloss);
        return m;
    }
    Transform Ball(string n, Transform parent, Vector3 pos, Vector3 scale, Material m)
    {
        var g = Kit.MeshObject(n, Kit.SphereMesh);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos; g.transform.localScale = scale;
        var r = g.GetComponent<MeshRenderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g.transform;
    }

    void Build()
    {
        bodyMat = Mat(Fur, 0.15f);
        var belly = Mat(Belly, 0.1f);
        var white = Mat(Color.white, 0.6f);
        var black = Mat(Kit.Hex("#1B1340"), 0.8f);
        var horn = Mat(Kit.Hex("#FFD84A"), 0.4f);
        mouthMat = Mat(Kit.Hex("#5A1030"), 0.3f);
        var pink = Mat(Kit.Hex("#FF6FA5"), 0.5f);

        body = new GameObject("body").transform; body.SetParent(transform, false);
        // a round furry blob with stubby arms; the head is the body (it's that kind of monster)
        head = Ball("head", body, Vector3.zero, new Vector3(2.5f, 2.3f, 1.8f), bodyMat);
        Ball("belly", body, new Vector3(0, -0.45f, -0.55f), new Vector3(1.5f, 1.2f, 0.7f), belly);
        Ball("hornL", body, new Vector3(-0.65f, 1.05f, 0.1f), new Vector3(0.28f, 0.6f, 0.28f), horn).localRotation = Quaternion.Euler(0, 0, 20);
        Ball("hornR", body, new Vector3(0.65f, 1.05f, 0.1f), new Vector3(0.28f, 0.6f, 0.28f), horn).localRotation = Quaternion.Euler(0, 0, -20);
        Ball("armL", body, new Vector3(-1.15f, -0.55f, -0.2f), new Vector3(0.75f, 0.45f, 0.5f), bodyMat).localRotation = Quaternion.Euler(0, 0, 25);
        Ball("armR", body, new Vector3(1.15f, -0.55f, -0.2f), new Vector3(0.75f, 0.45f, 0.5f), bodyMat).localRotation = Quaternion.Euler(0, 0, -25);
        eyeL = Ball("eyeL", body, new Vector3(-0.45f, 0.35f, -0.78f), Vector3.one * 0.62f, white);
        eyeR = Ball("eyeR", body, new Vector3(0.45f, 0.35f, -0.78f), Vector3.one * 0.62f, white);
        pupilL = Ball("pupilL", eyeL, new Vector3(0, 0, -0.38f), Vector3.one * 0.45f, black);
        pupilR = Ball("pupilR", eyeR, new Vector3(0, 0, -0.38f), Vector3.one * 0.45f, black);
        lidL = Ball("lidL", eyeL, new Vector3(0, 0.55f, -0.05f), new Vector3(1.1f, 0.2f, 1.1f), bodyMat);
        lidR = Ball("lidR", eyeR, new Vector3(0, 0.55f, -0.05f), new Vector3(1.1f, 0.2f, 1.1f), bodyMat);
        mouth = Ball("mouth", body, new Vector3(0, -0.3f, -0.82f), new Vector3(0.9f, 0.32f, 0.3f), mouthMat);
        Ball("toothL", mouth, new Vector3(-0.22f, 0.32f, -0.4f), new Vector3(0.16f, 0.32f, 0.3f), white);
        Ball("toothR", mouth, new Vector3(0.22f, 0.32f, -0.4f), new Vector3(0.16f, 0.32f, 0.3f), white);
        tongueTip = Ball("tongueTip", transform, Vector3.zero, Vector3.one * 0.38f, pink);
        tongueTip.gameObject.SetActive(false);
        tongue = new GameObject("tongue").AddComponent<LineRenderer>();
        tongue.transform.SetParent(transform, false);
        tongue.useWorldSpace = true; tongue.positionCount = 2; tongue.widthMultiplier = 0.26f;
        tongue.sharedMaterial = pink; tongue.numCapVertices = 6;
        tongue.enabled = false;

        // thought bubble: a white disc with the craved snack inside, ringed by its patience
        bubble = new GameObject("bubble").transform; bubble.SetParent(transform.parent, false);
        var disc = Kit.MeshObject("disc", Kit.QuadMesh);
        disc.transform.SetParent(bubble, false); disc.transform.localScale = Vector3.one * 1.9f; disc.transform.localPosition = new Vector3(0, 0, 0.2f);
        disc.GetComponent<MeshRenderer>().sharedMaterial = new Material(Kit.UnlitAlpha) { mainTexture = Kit.Disc, color = new Color(1, 1, 1, 0.96f) };
        for (int i = 0; i < 2; i++)
        {
            var dot = Kit.MeshObject("dot", Kit.QuadMesh);
            dot.transform.SetParent(bubble, false); dot.transform.localScale = Vector3.one * (0.42f - i * 0.14f);
            dot.transform.localPosition = new Vector3(0.75f + i * 0.4f, -0.95f - i * 0.38f, 0.2f);
            dot.GetComponent<MeshRenderer>().sharedMaterial = disc.GetComponent<MeshRenderer>().sharedMaterial;
        }
        ring = new GameObject("patience").AddComponent<LineRenderer>();
        ring.transform.SetParent(bubble, false);
        ring.useWorldSpace = false; ring.loop = false; ring.widthMultiplier = 0.12f; ring.numCapVertices = 4;
        ring.sharedMaterial = new Material(Kit.UnlitAlpha);
        bubbleItemHolder = new GameObject("want").transform; bubbleItemHolder.SetParent(bubble, false);
        bubbleItemHolder.localPosition = new Vector3(0, 0, -0.2f);
        bubble.gameObject.SetActive(false);
    }

    // ---------------------------------------------------------------- cravings
    public void Begin()
    {
        Meals = 0; Craving = -1; thinkT = 1.2f; grumpT = 0; eatT = 0;
        if (food) { Destroy(food.gameObject); food = null; }
        transform.localScale = Vector3.one;
        SetBubble(-1);
    }

    public void Idle() { Craving = -1; SetBubble(-1); if (food) { Destroy(food.gameObject); food = null; } eatT = 0; tongue.enabled = false; tongueTip.gameObject.SetActive(false); }

    void NewCraving()
    {
        var g = Game.I;
        int lo = Mathf.Min(2 + Meals / 3, 6), hi = Mathf.Min(lo + 2, 8);
        if (Meals == 0) { lo = 2; hi = 2; }
        // prefer something that isn't already sitting in the jar, so it has to be made
        int pick = -1;
        for (int tries = 0; tries < 12 && pick < 0; tries++)
        {
            int t = g.RandRange(lo, hi + 1);
            bool inJar = false;
            foreach (var it in g.Items) if (it && it.Special == 0 && it.Tier == t) { inJar = true; break; }
            if (!inJar || tries > 8) pick = t;
        }
        Craving = pick;
        PatienceMax = Patience = Mathf.Max(22f, 40f - Meals * 1.2f);
        SetBubble(Craving);
        Sfx.I.Think();
        g.OnCraving();
    }

    void SetBubble(int tier)
    {
        if (bubbleItem) Destroy(bubbleItem.gameObject);
        bubbleItem = null;
        bubble.gameObject.SetActive(tier >= 0);
        if (tier < 0) return;
        bubbleItemHolder.localScale = Vector3.one;   // Item.Create sizes the model in world units, so measure at scale 1
        bubbleItem = Item.Create(tier, Vector2.zero, bubbleItemHolder, false);
        bubbleItem.transform.localPosition = Vector3.zero;
        float s = 0.62f / Snacks.All[tier].r;
        bubbleItemHolder.localScale = Vector3.one * s;
        bubble.localScale = Vector3.zero;   // pops in (see Update)
    }

    public bool Wants(int tier) => Craving >= 0 && tier == Craving && !Eating && Game.I.State == Game.St.Play;

    // Snatch a freshly merged snack out of the jar.
    public int Eat(Item it)
    {
        food = it;
        food.Merging = true;   // no further merges, and the danger check ignores it
        if (food.Rb) { food.Rb.simulated = false; }
        foodFrom = food.transform.position;
        eatT = EatTime;
        int bonus = Mathf.RoundToInt(Snacks.Points(Craving) * 4 + 60 * Mathf.Clamp01(Patience / Mathf.Max(1f, PatienceMax)));
        Meals++;
        Craving = -1; SetBubble(-1); thinkT = 1.1f; grumpT = 0;
        Sfx.I.Slurp();
        return bonus;
    }

    // ---------------------------------------------------------------- frame
    void Update()
    {
        float dt = Time.deltaTime, t = Time.time;
        var g = Game.I;
        Layout();
        bool playing = g.State == Game.St.Play;

        // cravings run only during play
        if (playing && !Eating)
        {
            if (Craving < 0) { thinkT -= dt; if (thinkT <= 0) NewCraving(); }
            else
            {
                Patience -= dt;
                if (Patience <= 0) Grumpy();
            }
        }

        // tongue: out to the snack, then reel it in
        if (eatT > 0)
        {
            eatT -= dt;
            float k = 1f - eatT / EatTime;
            var mouthPos = mouth.position + new Vector3(0, 0, -0.2f);
            Vector3 tip;
            if (k < 0.35f) tip = Vector3.Lerp(mouthPos, foodFrom, Kit.EaseOutBack(k / 0.35f) * 0.98f);
            else
            {
                float r = (k - 0.35f) / 0.65f;
                tip = Vector3.Lerp(foodFrom, mouthPos, r * r);
                if (food) { food.transform.position = tip; food.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.25f, r); }
            }
            tongue.enabled = true; tongueTip.gameObject.SetActive(true);
            tongue.SetPosition(0, mouthPos); tongue.SetPosition(1, tip);
            tongueTip.position = tip;
            if (eatT <= 0)
            {
                tongue.enabled = false; tongueTip.gameObject.SetActive(false);
                if (food) { FX.Burst(mouthPos, Snacks.All[food.Tier].color, 18, 0.7f); Destroy(food.gameObject); food = null; }
                chompT = 0.6f;
                Sfx.I.Nom();
                WebBridge.Vibrate(25);
                transform.localScale = Vector3.one * (1f + 0.035f * Mathf.Min(Meals, 12));
            }
        }

        // mouth: chomps after a meal, gapes while the tongue is out, otherwise a little grin
        float open = 0.32f;
        if (eatT > 0) open = 0.75f;
        else if (chompT > 0) { chompT -= dt; open = 0.15f + Mathf.Abs(Mathf.Sin(chompT * 22f)) * 0.5f; }
        else if (Craving >= 0 && Patience < PatienceMax * 0.3f) open = 0.22f + Mathf.Sin(t * 9f) * 0.05f;   // impatient muttering
        var ms = mouth.localScale; ms.y = Mathf.Lerp(ms.y, open, 1f - Mathf.Exp(-dt * 20f)); mouth.localScale = ms;

        // eyes follow the snack in hand (or the food), blink now and then
        var look = g.LookTarget;
        foreach (var (eye, pupil) in new[] { (eyeL, pupilL), (eyeR, pupilR) })
        {
            var d = (look - eye.position); d.z = 0; d = Vector3.ClampMagnitude(d * 0.08f, 0.16f);
            pupil.localPosition = new Vector3(d.x / eye.localScale.x * 0.62f, d.y / eye.localScale.y * 0.62f, -0.38f);
        }
        blinkT -= dt;
        float lid = blinkT < 0.12f ? 0.05f : 0.55f;
        if (blinkT < 0) blinkT = Random.Range(1.8f, 4.5f);
        bool angry = growl > 0;
        if (angry) lid = 0.3f;
        lidL.localPosition = new Vector3(0, Mathf.Lerp(lidL.localPosition.y, lid, 1f - Mathf.Exp(-dt * 30f)), -0.05f);
        lidR.localPosition = lidL.localPosition;
        lidL.localRotation = Quaternion.Euler(0, 0, angry ? -18 : 0); lidR.localRotation = Quaternion.Euler(0, 0, angry ? 18 : 0);

        // colour: flushes red as patience runs out
        float heat = Craving >= 0 && PatienceMax > 0 ? Mathf.Clamp01(1f - Patience / (PatienceMax * 0.35f)) : 0f;
        if (growl > 0) { growl -= dt; heat = 1f; }
        bodyMat.color = Color.Lerp(Fur, FurAngry, heat * 0.85f);

        // idle bob, plus a happy wiggle while chomping and a stomp when grumpy
        bob += dt;
        float sx = 1f + Mathf.Sin(bob * 2.2f) * 0.02f, sy = 1f - Mathf.Sin(bob * 2.2f) * 0.02f;
        if (chompT > 0) { sx += Mathf.Sin(chompT * 30f) * 0.05f; }
        body.localScale = new Vector3(sx, sy, 1f);
        body.localRotation = Quaternion.Euler(0, 0, growl > 0 ? Mathf.Sin(t * 40f) * 6f : Mathf.Sin(bob * 1.3f) * 3f);

        // bubble pops in and wobbles; the ring shows patience
        if (bubble.gameObject.activeSelf)
        {
            bubble.localScale = Vector3.MoveTowards(bubble.localScale, Vector3.one * (1f + Mathf.Sin(t * 3f) * 0.03f), dt * 5f);
            bubbleItemHolder.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 1.5f) * 25f, Mathf.Sin(t * 2f) * 6f);
            float f = Mathf.Clamp01(Patience / Mathf.Max(1f, PatienceMax));
            int n = Mathf.Max(2, Mathf.CeilToInt(64 * f));
            ring.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float a = Mathf.PI / 2f - (i / 63f) * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 1.02f + new Vector3(0, 0, 0.1f));
            }
            var c = f > 0.5f ? Color.Lerp(Kit.Hex("#FFD84A"), Kit.Hex("#3FC79A"), (f - 0.5f) * 2f) : Color.Lerp(Kit.Hex("#E2463B"), Kit.Hex("#FFD84A"), f * 2f);
            if (f < 0.25f && Mathf.Repeat(t, 0.4f) < 0.2f) c = Color.white;
            ring.startColor = ring.endColor = c;
        }
    }

    void Grumpy()
    {
        growl = 1.0f;
        Patience = PatienceMax;   // same craving, another chance
        Game.I.MonsterStomp();
        Sfx.I.Grumble();
        UI.I.Toast("GRUMPY! IT WANTS A " + Snacks.All[Craving].name + "!");
        WebBridge.Event("monster_grumpy", Meals);
    }

    // Where it sits: beside the jar's right rim in landscape, peeking over the top-right corner on phones.
    void Layout()
    {
        bool land = UI.Landscape;
        float W = Game.W, H = Game.H;
        var target = land ? new Vector3(W / 2f + 2.15f, H - 0.15f, 1.6f) : new Vector3(W / 2f - 1.0f, H + 2.75f, 1.6f);
        float s = land ? 1f : 0.72f;
        // landscape menus put the menu column on the right: the monster hops onto the jar's left rim to stay in view
        if (land && Game.I.State == Game.St.Menu) { target = new Vector3(-W / 2f + 1.0f, H + 1.0f, 1.6f); s = 0.85f; }
        // phone menus cover the top of the jar with the logo: the monster sits inside the jar, between the tagline and PLAY
        if (!land && Game.I.State == Game.St.Menu) { target = new Vector3(0, 6.0f, 1.6f); s = 1.1f; }
        transform.localPosition = target;
        transform.localScale = Vector3.one * s * (1f + 0.035f * Mathf.Min(Meals, 12));
        bubble.localPosition = land ? new Vector3(W / 2f + 1.3f, H + 1.75f, 1.2f) : new Vector3(-0.6f, H + 2.75f, 1.2f);
    }
}
