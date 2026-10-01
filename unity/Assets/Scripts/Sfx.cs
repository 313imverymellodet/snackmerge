using UnityEngine;

// All audio is synthesized at boot: zero audio files.
public class Sfx : MonoBehaviour
{
    public static Sfx I;
    const int SR = 22050;
    const float TAU = Mathf.PI * 2f;
    AudioSource[] voices; int next;
    AudioSource music;
    AudioClip drop, merge, jackpot, discover, shake, pop, warn, over, click;
    public bool Muted { get; private set; }
    System.Random rnd = new System.Random(7);
    float N() => (float)(rnd.NextDouble() * 2 - 1);
    float lastMerge;

    void Awake()
    {
        I = this;
        voices = new AudioSource[12];
        for (int i = 0; i < voices.Length; i++) { voices[i] = gameObject.AddComponent<AudioSource>(); voices[i].playOnAwake = false; }
        music = gameObject.AddComponent<AudioSource>();
        music.loop = true; music.volume = 0.2f; music.playOnAwake = false;
        Build();
        music.clip = Music();
    }

    public void SetMuted(bool m) { Muted = m; AudioListener.volume = m ? 0 : 1; }
    public void StartMusic() { if (!music.isPlaying) music.Play(); }

    void Play(AudioClip c, float vol, float pitch = 1f)
    {
        var s = voices[next]; next = (next + 1) % voices.Length;
        s.pitch = pitch; s.PlayOneShot(c, vol);
    }

    // C major pentatonic steps up the ladder; combos push it higher
    static readonly float[] steps = { 1f, 1.122f, 1.26f, 1.498f, 1.682f, 2f, 2.245f, 2.52f, 2.997f, 3.364f, 4f };
    public void Drop(int tier) => Play(drop, 0.3f, 1.25f - tier * 0.08f);
    public void Merge(int tier, int combo)
    {
        if (Time.unscaledTime - lastMerge < 0.03f) return; lastMerge = Time.unscaledTime;
        float p = steps[Mathf.Clamp(tier + Mathf.Max(0, combo - 1), 0, steps.Length - 1)] * (tier >= 7 ? 0.5f : 1f);
        Play(merge, 0.42f + tier * 0.02f, p);
    }
    public void Jackpot() => Play(jackpot, 0.7f);
    public void Discover() => Play(discover, 0.55f);
    public void Shake() => Play(shake, 0.55f);
    public void Pop() => Play(pop, 0.5f);
    public void Warn() => Play(warn, 0.25f);
    public void Over() => Play(over, 0.6f);
    public void Click() => Play(click, 0.4f);

    static AudioClip Clip(string n, float[] d) { var c = AudioClip.Create(n, d.Length, 1, SR, false); c.SetData(d, 0); return c; }
    delegate float Gen(float t, float dt);
    static float[] R(float dur, Gen g)
    {
        int n = (int)(SR * dur); var d = new float[n]; float dt = 1f / SR;
        for (int i = 0; i < n; i++) d[i] = Mathf.Clamp(g(i * dt, dt) * Mathf.Clamp01((n - i) / (SR * 0.008f)), -1, 1);
        return d;
    }
    float[] Arp(float[] notes, float step, float tail, float vol)
    {
        float ph = 0;
        return R(step * notes.Length + tail, (t, dt) =>
        {
            int k = Mathf.Min((int)(t / step), notes.Length - 1);
            ph += TAU * notes[k] * dt;
            float lt = t - k * step;
            return (Mathf.Sin(ph) * 0.8f + Mathf.Sin(ph * 2f) * 0.15f) * Mathf.Exp(-lt * (k < notes.Length - 1 ? 9 : 3f)) * vol;
        });
    }

    void Build()
    {
        float ph = 0, lp = 0;
        // soft "plunk" into the jar
        drop = Clip("drop", R(0.14f, (t, dt) => { ph += TAU * Mathf.Lerp(520, 260, t / 0.14f) * dt; return Mathf.Sin(ph) * Mathf.Exp(-t * 26) * 0.8f; }));
        ph = 0;
        // juicy "bloop": a falling sine with a bubble wobble, pitched per tier at play time
        merge = Clip("merge", R(0.32f, (t, dt) => { ph += TAU * (523f * (1f + 0.35f * Mathf.Exp(-t * 22f)) + Mathf.Sin(t * 60f) * 18f) * dt; return (Mathf.Sin(ph) * 0.75f + Mathf.Sin(ph * 2f) * 0.18f) * Mathf.Exp(-t * 9f); }));
        jackpot = Clip("jackpot", Arp(new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f, 1568f, 2093f }, 0.07f, 1.2f, 0.45f));
        discover = Clip("discover", Arp(new[] { 783.99f, 987.77f, 1174.7f, 1568f }, 0.08f, 0.8f, 0.4f));
        lp = 0;
        shake = Clip("shake", R(0.6f, (t, dt) => { lp += (N() - lp) * 0.35f; return lp * (0.5f + 0.5f * Mathf.Sin(t * 70f)) * Mathf.Exp(-t * 3f); }));
        ph = 0; lp = 0;
        pop = Clip("pop", R(0.18f, (t, dt) => { lp += (N() - lp) * 0.6f; ph += TAU * Mathf.Lerp(900, 300, t / 0.18f) * dt; return (Mathf.Sin(ph) * 0.6f + lp * 0.4f * Mathf.Exp(-t * 40)) * Mathf.Exp(-t * 16); }));
        warn = Clip("warn", R(0.12f, (t, dt) => (Mathf.Sin(TAU * 880 * t) > 0 ? 0.4f : -0.4f) * Mathf.Exp(-t * 12)));
        ph = 0;
        over = Clip("over", R(1.1f, (t, dt) => { float f = t < 0.25f ? 392f : t < 0.5f ? 349.23f : t < 0.75f ? 311.13f : 261.63f; ph += TAU * f * dt; return (Mathf.Sin(ph) * 0.6f + (Mathf.Sin(ph) > 0 ? 0.12f : -0.12f)) * Mathf.Exp(-(t % 0.25f) * 4f) * (t < 0.75f ? 1f : Mathf.Exp(-(t - 0.75f) * 3f)); }));
        ph = 0;
        click = Clip("click", R(0.04f, (t, dt) => { ph += TAU * 1200 * dt; return Mathf.Sin(ph) * Mathf.Exp(-t * 90) * 0.6f; }));
    }

    // 96 bpm cosy kitchen lo-fi: Fmaj7 - Em7 - Dm7 - Cmaj7, soft keys, brushed hats, round bass.
    AudioClip Music()
    {
        float bpm = 96f, beat = 60f / bpm;
        int bars = 8; float dur = beat * 4 * bars;
        int n = (int)(SR * dur); var d = new float[n];
        float[][] chords =
        {
            new[] { 174.61f, 220f, 261.63f, 329.63f },
            new[] { 164.81f, 196f, 246.94f, 293.66f },
            new[] { 146.83f, 174.61f, 220f, 261.63f },
            new[] { 130.81f, 164.81f, 196f, 246.94f },
        };
        float[] roots = { 43.65f, 41.2f, 36.71f, 32.7f };
        float[] mel = { 659.25f, 0, 587.33f, 523.25f, 0, 587.33f, 659.25f, 0, 783.99f, 0, 659.25f, 587.33f, 523.25f, 0, 493.88f, 0 };
        float hp = 0;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / SR, bt = t / beat;
            int bi = (int)bt, barIdx = bi / 4, bar = barIdx % 4;
            float ib = (bt - bi) * beat;
            float nz = N();
            float kick = (bi % 2 == 0) ? Mathf.Sin(TAU * (48 + 70 * Mathf.Exp(-ib * 30)) * ib) * Mathf.Exp(-ib * 8) * 0.45f : 0;
            float snare = (bi % 2 == 1) ? nz * Mathf.Exp(-ib * 14) * 0.12f : 0;
            float e8 = bt * 2; int e8i = (int)e8; float i8 = (e8 - e8i) * beat / 2;
            float hat = (nz - hp) * Mathf.Exp(-i8 * 30) * 0.03f; hp = nz;
            float bf = roots[bar];
            float bass = (Mathf.Sin(TAU * bf * t) + 0.3f * Mathf.Sin(TAU * bf * 2 * t)) * Mathf.Exp(-ib * 2.5f) * 0.18f;
            // electric-piano chord on beats 1 and 3, with a tremolo
            float keys = 0;
            float cb = (bt % 2f) * beat;
            foreach (var f in chords[bar]) keys += Mathf.Sin(TAU * f * t) * (0.6f + 0.4f * Mathf.Sin(TAU * f * 2.001f * t));
            keys *= 0.035f * Mathf.Exp(-cb * 1.6f) * (0.85f + 0.15f * Mathf.Sin(t * 30f));
            float ld = 0;
            if (barIdx >= 4)
            {
                float lf = mel[e8i % 16];
                if (lf > 0) ld = Mathf.Sin(TAU * lf * t) * Mathf.Exp(-i8 * 5f) * 0.07f;
            }
            d[i] = Mathf.Clamp((kick + snare + hat + bass + keys + ld) * 0.85f, -1, 1);
        }
        return Clip("music", d);
    }
}
