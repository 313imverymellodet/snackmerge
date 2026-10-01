using UnityEngine;

// One snack in the jar: a 2D physics circle carrying a 3D model.
public class Item : MonoBehaviour
{
    public int Tier;
    public Rigidbody2D Rb;
    public CircleCollider2D Col;
    public Transform Model;
    public float Age, OverTime;
    public bool Merging, Dropped;
    float pop;                     // spawn pop-in
    public int Serial;             // creation order: settles which of a touching pair performs the merge
    static int serials;
    static PhysicsMaterial2D mat;

    public float R => Snacks.All[Tier].r;
    public float Top => transform.position.y + R;

    public static Item Create(int tier, Vector2 pos, Transform parent, bool physics)
    {
        var def = Snacks.All[tier];
        var go = new GameObject("snack_" + def.id);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var it = go.AddComponent<Item>();
        it.Tier = tier;
        it.Serial = ++serials;
        // model: fit its biggest face-on extent to the circle
        var m = Kit.Spawn(def.model, 1f, go.transform);
        m.transform.localRotation = Quaternion.Euler(def.euler);
        var b = Kit.WorldBounds(m);
        float ext = Mathf.Max(b.size.x, b.size.y);
        m.transform.localScale = Vector3.one * (def.r * 2.08f / Mathf.Max(0.01f, ext));
        b = Kit.WorldBounds(m);
        m.transform.position += (Vector3)pos - b.center;
        it.Model = m.transform;
        foreach (var r in m.GetComponentsInChildren<Renderer>()) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }
        if (physics) it.EnablePhysics();
        it.pop = 0.18f;
        return it;
    }

    public void EnablePhysics()
    {
        if (Rb) return;
        if (!mat) mat = new PhysicsMaterial2D("snack") { friction = 0.35f, bounciness = 0.08f };
        Col = gameObject.AddComponent<CircleCollider2D>();
        Col.radius = R; Col.sharedMaterial = mat;
        Rb = gameObject.AddComponent<Rigidbody2D>();
        Rb.mass = R * R * 4f;
        Rb.gravityScale = 1.7f;
        Rb.angularDamping = 0.6f; Rb.linearDamping = 0.05f;
        Rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        Rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        Dropped = true;
    }

    void Update()
    {
        if (pop > 0)
        {
            pop -= Time.deltaTime;
            float k = 1f - Mathf.Clamp01(pop / 0.18f);
            transform.localScale = Vector3.one * Kit.EaseOutBack(k);
            if (pop <= 0) transform.localScale = Vector3.one;
        }
    }

    void OnCollisionEnter2D(Collision2D c) => Touch(c);
    void OnCollisionStay2D(Collision2D c) => Touch(c);

    void Touch(Collision2D c)
    {
        if (Merging) return;
        var o = c.collider.GetComponent<Item>();
        if (o == null || o.Merging || o.Tier != Tier) return;
        // the older item resolves the pair so it only merges once
        if (Serial > o.Serial) return;
        Game.I.Merge(this, o);
    }
}
