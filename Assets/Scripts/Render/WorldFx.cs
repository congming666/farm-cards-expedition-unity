using System;
using System.Collections.Generic;
using UnityEngine;

// ================= GPU 世界特效（对象池 SpriteRenderer） =================
// 逻辑层 Expedition.particles 由 ExpeditionEffects.Spawn* 填充（纯数据，不绘制）。
// 旧版只有已停用的 Expedition.Render() 逐像素软件链路画粒子，GPU 世界里看不到特效；
// 本类在 WorldRenderer.Tick 中把这些粒子用共享贴图 + 对象池精灵呈现，逻辑层零改动。
public class WorldFx
{
    const int MAX = 240;
    const float PX = WorldRenderer.S;

    class Node { public SpriteRenderer sr; }
    readonly List<Node> nodes = new List<Node>();
    static Sprite dotSprite, ringSprite;
    Transform parent;

    public void Build(Transform parent) { this.parent = parent; EnsureSprites(); }

    static void EnsureSprites()
    {
        if (dotSprite == null) dotSprite = MakeSoftCircle();
        if (ringSprite == null) ringSprite = MakeRing();
    }

    Node GetNode(int i)
    {
        while (nodes.Count <= i)
        {
            var go = new GameObject("Fx");
            if (parent != null) go.transform.SetParent(parent);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dotSprite;
            sr.sortingOrder = 220; // 压在角色/怪物之上
            nodes.Add(new Node { sr = sr });
        }
        return nodes[i];
    }

    public void Sync(Expedition e)
    {
        if (e == null) { HideAll(); return; }
        var ps = e.particles;
        int n = Math.Min(ps.Count, MAX);
        for (int i = 0; i < n; i++)
        {
            var p = ps[i];
            var tr = GetNode(i).sr.transform;
            var sr = tr.GetComponent<SpriteRenderer>();
            tr.gameObject.SetActive(true);
            float t = p.maxLife > 0 ? Mathf.Clamp01(p.life / p.maxLife) : 0f; // 1(新生)->0(消亡)
            Color c = G.ParseColor(p.color);
            tr.position = new Vector3(p.x * PX, 0.4f, p.y * PX);
            float baseWorld = Mathf.Max(0.02f, p.size * PX);

            switch (p.type)
            {
                case "aoe":
                case "weaponRing":
                    sr.sprite = ringSprite;
                    float grow = p.type == "aoe" ? (1f + (1f - t) * 0.35f) : (1.25f - t * 0.25f);
                    tr.localScale = Vector3.one * (baseWorld * grow);
                    tr.rotation = Quaternion.identity;
                    c.a = t * 0.85f;
                    break;
                case "slash":
                    sr.sprite = ringSprite;
                    tr.localScale = Vector3.one * (baseWorld * 1.4f);
                    tr.rotation = Quaternion.Euler(0, 0, p.angle * Mathf.Rad2Deg);
                    c.a = t;
                    break;
                case "smoke":
                    sr.sprite = dotSprite;
                    tr.localScale = Vector3.one * (baseWorld * (1.35f - t * 0.35f) * 1.7f);
                    tr.rotation = Quaternion.identity;
                    c.a = t * 0.30f;
                    break;
                case "earthTrail":
                    sr.sprite = dotSprite;
                    tr.localScale = new Vector3(baseWorld * 1.8f, baseWorld * 0.55f, 1);
                    tr.rotation = Quaternion.Euler(0, 0, p.angle * Mathf.Rad2Deg);
                    c.a = t * 0.7f;
                    break;
                case "vine":
                    sr.sprite = dotSprite;
                    tr.localScale = new Vector3(baseWorld * 0.5f, baseWorld * 1.6f, 1);
                    tr.rotation = Quaternion.Euler(0, 0, p.angle * Mathf.Rad2Deg);
                    c.a = t;
                    break;
                default: // spark / chaff / 命中点等
                    sr.sprite = dotSprite;
                    float s = Mathf.Max(0.06f, baseWorld * 2.2f) * Mathf.Lerp(0.5f, 1f, t);
                    tr.localScale = Vector3.one * s;
                    tr.rotation = Quaternion.identity;
                    c.a = t;
                    break;
            }
            sr.color = c;
        }
        for (int i = n; i < nodes.Count; i++)
            if (nodes[i].sr.gameObject.activeSelf) nodes[i].sr.gameObject.SetActive(false);
    }

    void HideAll()
    {
        for (int i = 0; i < nodes.Count; i++)
            if (nodes[i].sr.gameObject.activeSelf) nodes[i].sr.gameObject.SetActive(false);
    }

    // 软圆点（中心实、边缘透），白色基底，运行时用 sr.color 染色
    static Sprite MakeSoftCircle()
    {
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float dx = x - (n - 1) * 0.5f, dy = y - (n - 1) * 0.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy) / (n * 0.5f);
            byte a = r >= 1f ? (byte)0 : (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(1f - r));
            px[y * n + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(px); tex.Apply(false); tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 64f);
    }

    // 圆环（AOE 范围圈 / 武器切换环 / 刀光）
    static Sprite MakeRing()
    {
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color32[n * n];
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            float dx = x - (n - 1) * 0.5f, dy = y - (n - 1) * 0.5f;
            float r = Mathf.Sqrt(dx * dx + dy * dy) / (n * 0.5f);
            float band = Mathf.Abs(r - 0.78f);
            byte a = band <= 0.14f ? (byte)Mathf.RoundToInt(255f * Mathf.Clamp01(1f - band / 0.14f)) : (byte)0;
            px[y * n + x] = new Color32(255, 255, 255, a);
        }
        tex.SetPixels32(px); tex.Apply(false); tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 64f);
    }
}
