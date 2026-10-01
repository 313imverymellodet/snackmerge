using System.Collections.Generic;
using UnityEngine;

// Pooled sprite particles: juice bursts and confetti.
public static class FX
{
    class P { public SpriteRenderer sr; public Vector3 v; public float life, max, size, grow, grav, spin, drag; public Color c; public bool on; }
    static readonly List<P> pool = new List<P>();
    static Sprite disc, glow, square;
    static Transform root;

    public static void Init(Transform parent)
    {
        root = new GameObject("FX").transform; root.SetParent(parent, false);
        disc = Sprite.Create(Kit.Disc, new Rect(0, 0, Kit.Disc.width, Kit.Disc.height), new Vector2(.5f, .5f), Kit.Disc.width);
        glow = Sprite.Create(Kit.Glow, new Rect(0, 0, Kit.Glow.width, Kit.Glow.height), new Vector2(.5f, .5f), Kit.Glow.width);
        var t = new Texture2D(4, 4); var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = Color.white; t.SetPixels(px); t.Apply();
        square = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f), 4);
        for (int i = 0; i < 260; i++)
        {
            var sr = new GameObject("p").AddComponent<SpriteRenderer>();
            sr.sharedMaterial = Kit.UnlitAlpha; sr.sortingOrder = 10;
            sr.transform.SetParent(root, false); sr.gameObject.SetActive(false);
            pool.Add(new P { sr = sr });
        }
    }

    static P Get() { foreach (var p in pool) if (!p.on) return p; return null; }

    static void Emit(Vector3 at, Sprite s, Color c, Vector3 v, float life, float size, float grow, float grav, float drag)
    {
        var p = Get(); if (p == null) return;
        p.on = true; p.sr.gameObject.SetActive(true); p.sr.sprite = s; p.sr.color = c;
        p.sr.transform.position = new Vector3(at.x, at.y, -1.5f); p.v = v; p.life = p.max = life; p.size = size; p.grow = grow; p.grav = grav; p.drag = drag; p.c = c;
        p.spin = Random.Range(-500f, 500f);
        p.sr.transform.localScale = Vector3.one * size;
    }

    // juicy splash: droplets + a soft flash
    public static void Burst(Vector3 at, Color c, int n, float scale)
    {
        Emit(at, glow, Kit.A(Color.Lerp(c, Color.white, 0.4f), 0.8f), Vector3.zero, 0.3f, 1.2f * scale, 5f * scale, 0, 0);
        for (int i = 0; i < n; i++)
        {
            var d = (Vector3)Random.insideUnitCircle.normalized;
            Emit(at + d * 0.3f * scale, disc, Color.Lerp(c, Color.white, Random.value * 0.3f), d * Random.Range(2f, 6f) * Mathf.Sqrt(scale) + Vector3.up * 2f, Random.Range(0.35f, 0.7f), Random.Range(0.1f, 0.24f) * Mathf.Sqrt(scale), -0.2f, 12f, 1.2f);
        }
    }

    static readonly Color[] party = { Kit.Hex("#FF5A6E"), Kit.Hex("#FFD84A"), Kit.Hex("#5BE37D"), Kit.Hex("#3FA9F5"), Kit.Hex("#B57BFF"), Color.white };
    public static void Confetti(Vector3 at, int n)
    {
        for (int i = 0; i < n; i++)
        {
            var v = new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 1.5f), 0) * Random.Range(4f, 10f);
            Emit(at, square, party[i % party.Length], v, Random.Range(1.2f, 2.2f), Random.Range(0.12f, 0.22f), 0, 9f, 1f);
        }
    }

    public static void Tick(float dt)
    {
        foreach (var p in pool)
        {
            if (!p.on) continue;
            p.life -= dt;
            if (p.life <= 0) { p.on = false; p.sr.gameObject.SetActive(false); continue; }
            p.v.y -= p.grav * dt;
            p.v *= Mathf.Exp(-p.drag * dt);
            var tr = p.sr.transform;
            tr.position += p.v * dt;
            p.size = Mathf.Max(0.01f, p.size + p.grow * dt);
            tr.localScale = Vector3.one * p.size;
            tr.rotation = Quaternion.Euler(0, 0, p.spin * (p.max - p.life));
            p.sr.color = Kit.A(p.c, p.c.a * Mathf.Clamp01(p.life / p.max * 2f));
        }
    }
}
