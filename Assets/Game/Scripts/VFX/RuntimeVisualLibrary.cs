using System.Collections.Generic;
using UnityEngine;

public static class RuntimeVisualLibrary
{
    private static readonly Dictionary<string, Sprite> spriteCache = new();
    private static readonly Dictionary<PoolKey, Sprite> projectileFallbackCache = new();

    public static Sprite GetPlantSprite(PlantType plantType)
    {
        string resourceName = plantType switch
        {
            PlantType.CherryBomb => "FusionSprites/cherry_bomb",
            PlantType.CherryPeashooter => "FusionSprites/cherry_peashooter",
            PlantType.CherryRepeater => "FusionSprites/cherry_repeater",
            PlantType.Repeater => "FusionSprites/repeater",
            _ => null
        };

        if (string.IsNullOrEmpty(resourceName)) return null;
        return LoadSprite(resourceName, 256f);
    }

    public static Sprite GetProjectileSprite(PoolKey projectileKey)
    {
        if (projectileFallbackCache.TryGetValue(projectileKey, out var cached) && cached != null)
            return cached;

        Color coreColor;
        Color rimColor;
        Color highlightColor;
        bool isCherry = false;

        switch (projectileKey)
        {
            case PoolKey.CherryPeaProjectile:
                coreColor = new Color(0.92f, 0.12f, 0.16f, 1f);
                rimColor = new Color(0.45f, 0.02f, 0.05f, 1f);
                highlightColor = new Color(1f, 0.72f, 0.45f, 1f);
                isCherry = true;
                break;
            case PoolKey.SnowpeaProjectile:
                coreColor = new Color(0.35f, 0.85f, 1f, 1f);
                rimColor = new Color(0.1f, 0.45f, 0.8f, 1f);
                highlightColor = new Color(0.9f, 0.98f, 1f, 1f);
                break;
            case PoolKey.FirepeaProjectile:
                coreColor = new Color(1f, 0.48f, 0.1f, 1f);
                rimColor = new Color(0.8f, 0.12f, 0.02f, 1f);
                highlightColor = new Color(1f, 0.95f, 0.55f, 1f);
                break;
            case PoolKey.GoopeaProjectile:
                coreColor = new Color(0.7f, 0.25f, 0.95f, 1f);
                rimColor = new Color(0.35f, 0.05f, 0.55f, 1f);
                highlightColor = new Color(0.92f, 0.75f, 1f, 1f);
                break;
            default:
                coreColor = new Color(0.42f, 0.88f, 0.22f, 1f);
                rimColor = new Color(0.14f, 0.48f, 0.08f, 1f);
                highlightColor = new Color(0.85f, 1f, 0.65f, 1f);
                break;
        }

        var sprite = CreateOrbSprite(64, coreColor, rimColor, highlightColor, isCherry);
        projectileFallbackCache[projectileKey] = sprite;
        return sprite;
    }

    public static Sprite GetExplosionBurstSprite()
    {
        if (spriteCache.TryGetValue("__explosion_burst", out var cached) && cached != null)
            return cached;

        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float half = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float angle = Mathf.Atan2(dy, dx);
                float spike = 0.82f + 0.18f * Mathf.Sin(angle * 10f);
                float d = dist / spike;

                if (d >= 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    Color c;
                    if (d < 0.35f)
                        c = Color.Lerp(new Color(1f, 0.98f, 0.7f, 1f), new Color(1f, 0.65f, 0.15f, 1f), d / 0.35f);
                    else if (d < 0.75f)
                        c = Color.Lerp(new Color(1f, 0.65f, 0.15f, 1f), new Color(0.95f, 0.15f, 0.1f, 0.95f), (d - 0.35f) / 0.4f);
                    else
                        c = new Color(0.85f, 0.08f, 0.05f, Mathf.Clamp01((1f - d) / 0.25f));

                    tex.SetPixel(x, y, c);
                }
            }
        }

        tex.Apply();
        var sprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        spriteCache["__explosion_burst"] = sprite;
        return sprite;
    }

    private static Sprite LoadSprite(string resourcePath, float fallbackPpu)
    {
        if (spriteCache.TryGetValue(resourcePath, out var cached) && cached != null)
            return cached;

        var sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            spriteCache[resourcePath] = sprite;
            return sprite;
        }

        var tex = Resources.Load<Texture2D>(resourcePath);
        if (tex == null) return null;

        float ppu = Mathf.Max(tex.width, tex.height) / 1.35f;
        if (ppu <= 0f) ppu = fallbackPpu;
        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.15f), ppu);
        spriteCache[resourcePath] = sprite;
        return sprite;
    }

    private static Sprite CreateOrbSprite(int size, Color core, Color rim, Color highlight, bool addCherryStem)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float half = size * 0.5f;
        float radius = size * 0.38f;
        Vector2 center = new Vector2(half, half - (addCherryStem ? 3f : 0f));
        Vector2 lightPos = center + new Vector2(-radius * 0.32f, radius * 0.32f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y);
                float dist = Vector2.Distance(p, center);
                Color pixel = Color.clear;

                if (dist <= radius + 1.5f)
                {
                    float t = Mathf.Clamp01(dist / radius);
                    float edgeAlpha = Mathf.Clamp01((radius + 1.2f - dist) / 1.8f);
                    Color baseCol = Color.Lerp(core, rim, t * t);

                    float lightDist = Vector2.Distance(p, lightPos) / (radius * 0.45f);
                    if (lightDist < 1f)
                        baseCol = Color.Lerp(highlight, baseCol, lightDist * lightDist);

                    if (t > 0.86f)
                        baseCol = Color.Lerp(baseCol, new Color(0.08f, 0.05f, 0.05f, 1f), (t - 0.86f) / 0.14f);

                    baseCol.a = edgeAlpha;
                    pixel = baseCol;
                }

                if (addCherryStem)
                {
                    float sx = (x - (center.x + 2f));
                    float sy = (y - (center.y + radius - 2f));
                    if (sy >= 0 && sy <= 11f && Mathf.Abs(sx - sy * 0.35f) <= 2.2f)
                        pixel = new Color(0.28f, 0.65f, 0.15f, 1f);
                }

                tex.SetPixel(x, y, pixel);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 110f);
    }
}
