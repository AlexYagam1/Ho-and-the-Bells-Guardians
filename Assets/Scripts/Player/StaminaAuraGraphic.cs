using UnityEngine;

namespace HoAndTheBells.Player
{
    /// <summary>Soft screen edges whose coverage follows current stamina and remains anchored at the bottom when empty.</summary>
    public sealed class StaminaAuraGraphic : UnityEngine.UI.MaskableGraphic
    {
        public const float EmptyCoverage = 0.04f;
        private float coverage = 0.5f;
        public float Coverage => coverage;
        public void SetAppearance(float extent, Color tint)
        {
            extent = Mathf.Clamp01(extent);
            if (Mathf.Approximately(coverage, extent) && color == tint) return;
            coverage = extent; color = tint; SetVerticesDirty();
        }
        protected override void Awake() { base.Awake(); raycastTarget = false; }
        public static float EdgeAlpha(Vector2 uv, float extent)
        {
            float side = Mathf.Clamp01((Mathf.Abs(uv.x - 0.5f) - 0.42f) / 0.08f);
            float bottom = Mathf.Clamp01((0.09f - uv.y) / 0.09f);
            float top = Mathf.Clamp01((uv.y - 0.91f) / 0.09f) * Mathf.InverseLerp(0.92f, 1f, extent);
            float upperFade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(Mathf.Max(0f, extent - 0.08f), extent, uv.y));
            if (extent >= 0.999f) upperFade = 1f;
            float edge = Mathf.Max(bottom, Mathf.Max(side * upperFade, top));
            return edge * edge * Mathf.Clamp01(extent / 0.08f);
        }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh)
        {
            vh.Clear();
            const int columns = 64, rows = 48;
            Rect rect = GetPixelAdjustedRect();
            for (int y = 0; y <= rows; y++)
                for (int x = 0; x <= columns; x++)
                {
                    var uv = new Vector2((float)x / columns, (float)y / rows);
                    var tint = color; tint.a *= EdgeAlpha(uv, coverage);
                    vh.AddVert(new Vector3(rect.xMin + uv.x * rect.width, rect.yMin + uv.y * rect.height), tint, uv);
                }
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                {
                    int i = y * (columns + 1) + x;
                    vh.AddTriangle(i, i + columns + 1, i + 1);
                    vh.AddTriangle(i + 1, i + columns + 1, i + columns + 2);
                }
        }
    }
}
