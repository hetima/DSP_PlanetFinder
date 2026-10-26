using System.Collections.Generic;
using UnityEngine;

namespace PlanetFinderMod
{
    /// <summary>
    /// Builds a small round thumbnail for a planet out of the theme thumbnail material that the game
    /// already loads for the starmap (ThemeProto.thumbMat[style]). Its albedo texture is an
    /// equirectangular planet map, so it is projected onto a sphere and shaded with a fixed light.
    /// Thumbnails are cached per (theme, style). When the game has no usable texture, null is
    /// returned and the row keeps the plain colour dot.
    /// </summary>
    public static class PlanetThumbnail
    {
        private const int Size = 24;
        private const int SrcW = 96;
        private const int SrcH = 48;

        private static readonly Dictionary<long, Sprite> cache = new Dictionary<long, Sprite>();
        private static bool loggedFallback;

        public static Sprite Get(PlanetData pd)
        {
            if (pd == null || pd.theme <= 0)
            {
                return null;
            }
            int style = pd.style;
            long key = ((long)pd.theme << 8) | (long)(style & 0xff);
            if (cache.TryGetValue(key, out Sprite cached))
            {
                return cached;
            }

            Sprite sprite = null;
            try
            {
                sprite = Build(pd.theme, style);
            }
            catch (System.Exception e)
            {
                if (!loggedFallback)
                {
                    loggedFallback = true;
                    PLFN.LogWarning("planet thumbnail failed, using colour dots: " + e.Message);
                }
            }
            if (sprite == null && !loggedFallback)
            {
                loggedFallback = true;
                PLFN.Log("planet thumbnail texture is not available, using colour dots");
            }
            cache[key] = sprite;
            return sprite;
        }

        private static Sprite Build(int theme, int style)
        {
            ThemeProto proto = LDB.themes.Select(theme);
            Material[] mats = proto?.thumbMat;
            if (mats == null || mats.Length == 0)
            {
                return null;
            }
            if (style < 0 || style >= mats.Length)
            {
                style = 0;
            }
            Material mat = mats[style] != null ? mats[style] : mats[0];
            Texture tex = PickAlbedo(mat);
            if (tex == null)
            {
                return null;
            }
            Color[] src = ReadTexture(tex);
            if (src == null)
            {
                return null;
            }

            Texture2D thumb = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            Color[] dst = new Color[Size * Size];
            Vector3 light = new Vector3(-0.45f, 0.55f, 0.7f).normalized;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float nx = (x + 0.5f) / Size * 2f - 1f;
                    float ny = (y + 0.5f) / Size * 2f - 1f;
                    float r2 = nx * nx + ny * ny;
                    if (r2 >= 1f)
                    {
                        dst[y * Size + x] = Color.clear;
                        continue;
                    }
                    float nz = Mathf.Sqrt(1f - r2);
                    float lon = Mathf.Atan2(nx, nz);
                    float lat = Mathf.Asin(Mathf.Clamp(ny, -1f, 1f));
                    int sx = Mathf.Clamp((int)((lon / (2f * Mathf.PI) + 0.5f) * (SrcW - 1)), 0, SrcW - 1);
                    int sy = Mathf.Clamp((int)((lat / Mathf.PI + 0.5f) * (SrcH - 1)), 0, SrcH - 1);
                    Color c = src[sy * SrcW + sx];
                    float ndl = Mathf.Clamp01(Vector3.Dot(new Vector3(nx, ny, nz), light));
                    float shade = 0.35f + 0.85f * ndl;
                    c.r *= shade;
                    c.g *= shade;
                    c.b *= shade;
                    c.a = r2 > 0.88f ? Mathf.Clamp01((1f - r2) / 0.12f) : 1f;
                    dst[y * Size + x] = c;
                }
            }
            thumb.SetPixels(dst);
            thumb.Apply(false, false);
            return Sprite.Create(thumb, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f), Size, 0, SpriteMeshType.FullRect);
        }

        private static Texture PickAlbedo(Material mat)
        {
            if (mat == null)
            {
                return null;
            }
            Texture t = mat.GetTexture("_AlbedoTexture");
            if (t == null)
            {
                t = mat.GetTexture("_Albedo");
            }
            if (t == null)
            {
                t = mat.GetTexture("_MainTex");
            }
            if (t == null)
            {
                t = mat.mainTexture;
            }
            return t;
        }

        private static Color[] ReadTexture(Texture tex)
        {
            RenderTexture rt = RenderTexture.GetTemporary(SrcW, SrcH, 0, RenderTextureFormat.ARGB32);
            RenderTexture prev = RenderTexture.active;
            Graphics.Blit(tex, rt);
            RenderTexture.active = rt;
            Texture2D flat = new Texture2D(SrcW, SrcH, TextureFormat.RGBA32, false);
            flat.ReadPixels(new Rect(0f, 0f, SrcW, SrcH), 0, 0);
            flat.Apply(false, false);
            Color[] pixels = flat.GetPixels();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            Object.Destroy(flat);
            return pixels;
        }
    }
}
