using MelonLoader;
using UnityEngine.Rendering;
using UnityEngine;

[assembly: MelonInfo(typeof(UnbeatableJudgment.Core), "UnbeatableJudgment", "1.0.0", "MDuh", null)]
[assembly: MelonGame("D-CELL GAMES", "UNBEATABLE")]

namespace UnbeatableJudgment
{
    public class Core : MelonMod
    {
        internal const int MaxLogEntries = 50;
        private static readonly List<string> _judgementLog = new List<string>(MaxLogEntries);
        private static readonly object _logLock = new object();
        private static GUIStyle _labelStyle;

        internal static bool IsDebug = false;

        private const int Last50Cap = 50;
        private const int FullCap = 100000;
        private const float TimeRangeMs = 150f;

        private static readonly object _dataLock = new object();
        private static readonly Queue<float> _last50 = new Queue<float>(Last50Cap);
        private static readonly Queue<float> _full = new Queue<float>(512);
        private static float _sumLast50 = 0f;
        private static float _sumFull = 0f;

        private static GUIStyle _triangleStyle;
        private static GUIStyle _textStyle;
        private static Material _glMat;

        public static void PushJudgementLine(string line)
        {
            lock (_logLock)
            {
                _judgementLog.Add(line);
                if (_judgementLog.Count > MaxLogEntries)
                    _judgementLog.RemoveAt(0); // remove oldest
            }
        }

        public static void PushJudgementOffset(float deltaMs)
        {
            lock (_dataLock)
            {
                _last50.Enqueue(deltaMs);
                _sumLast50 += deltaMs;
                if (_last50.Count > Last50Cap)
                {
                    _sumLast50 -= _last50.Dequeue();
                }

                _full.Enqueue(deltaMs);
                _sumFull += deltaMs;
                if (_full.Count > FullCap)
                {
                    _sumFull -= _full.Dequeue();
                }
            }
        }

        public override void OnGUI()
        {
            DrawErrorBar();

            // Debug Stuff Below
            if (!IsDebug)
                return;

            List<string> snapshot;
            lock (_logLock)
            {
                if (_judgementLog.Count == 0)
                    return;
                snapshot = new List<string>(_judgementLog);
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle()
                {
                    fontSize = 14,
                    normal = { textColor = Color.white }
                };
            }

            const float startX = 50f;
            const float startY = 100f;
            const float padding = 8f;
            const float lineHeight = 18f;
            const float width = 600f;

            int lines = snapshot.Count;
            float height = padding * 2f + (lines * lineHeight);

            var bgRect = new Rect(startX, startY, width, height);

            var prevColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.Box(bgRect, GUIContent.none);
            GUI.color = prevColor;

            for (int i = 0; i < lines; i++)
            {
                int idx = snapshot.Count - 1 - i; // newest first
                float y = startY + padding + (i * lineHeight);
                var rect = new Rect(startX + padding, y, width - (padding * 2f), lineHeight);
                GUI.Label(rect, snapshot[idx], _labelStyle);
            }
        }

        public override void OnUpdate() // Runs once per frame.
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.BackQuote))
            {
                lock (_logLock)
                    _judgementLog.Clear();

                lock (_dataLock)
                {
                    _last50.Clear(); _sumLast50 = 0f;
                    _full.Clear(); _sumFull = 0f;
                }
            }
        }

        private static void DrawErrorBar()
        {
            float screenW = Screen.width;
            float screenH = Screen.height;
            float barW = Mathf.Max(200f, screenW * 0.30f);
            float barH = Mathf.Max(50f, screenH * 0.03f);
            float bottomPad = screenH * 0.03f;

            var barRect = new Rect(
                (screenW - barW) * 0.5f,
                screenH - bottomPad - barH,
                barW,
                barH
            );

            // Snapshot data needed for drawing without holding locks long
            float avg50, avgFull;
            float[] last50Snapshot;
            int count50, countFull;

            lock (_dataLock)
            {
                count50 = _last50.Count;
                countFull = _full.Count;
                avg50 = (count50 > 0) ? (_sumLast50 / count50) : 0f;
                avgFull = (countFull > 0) ? (_sumFull / countFull) : 0f;

                last50Snapshot = new float[count50];
                _last50.CopyTo(last50Snapshot, 0);
            }

            // Background
            FillRect(barRect, new Color(0f, 0f, 0f, 0.5f));

            // Colored timing windows band
            float bandH = Mathf.Clamp(barH * 0.3f, 10f, 18f);
            float bandY = barRect.y + (barH - bandH) * 0.5f;

            Color darkOrange = new Color(1f, 140f / 255f, 0f, 0.8f);
            Color darkGreen = new Color(0f, 128f / 255f, 0f, 0.8f);
            Color darkBlue = new Color(0f, 102f / 255f, 204f / 255f, 0.8f);
            Color centerColor = new Color(1f, 1f, 1f, 0.9f);

            // Segments: [-150,-100] [-100,-50] [-50,-1] [-1,1] [1,50] [50,100] [100,150]
            DrawSegment(barRect, bandY, bandH, -150f, -100f, darkOrange);
            DrawSegment(barRect, bandY, bandH, -100f, -50f, darkGreen);
            DrawSegment(barRect, bandY, bandH, -50f, -1f, darkBlue);
            DrawSegment(barRect, bandY, bandH, -1f, 1f, centerColor);
            DrawSegment(barRect, bandY, bandH, 1f, 50f, darkBlue);
            DrawSegment(barRect, bandY, bandH, 50f, 100f, darkGreen);
            DrawSegment(barRect, bandY, bandH, 100f, 150f, darkOrange);

            // Tick marks for last 50
            for (int i = 0; i < last50Snapshot.Length; i++)
            {
                float ms = Mathf.Clamp(last50Snapshot[i], -TimeRangeMs, TimeRangeMs);
                float x = MsToX(ms, TimeRangeMs, barRect);
                FillRect(new Rect(x, barRect.y, 1f, barH), new Color(1f, 1f, 1f, 0.9f));
            }

            // Triangles
            float triX50 = MsToX(Mathf.Clamp(avg50, -TimeRangeMs, TimeRangeMs), TimeRangeMs, barRect);
            float triXFull = MsToX(Mathf.Clamp(avgFull, -TimeRangeMs, TimeRangeMs), TimeRangeMs, barRect);

            DrawTriangleGL(new Vector2(triX50, bandY - 5f), 25f, pointingDown: false, new Color(1f, 0.55f, 0f, 1f)); // top ▲/▼ (downward)
            DrawTriangleGL(new Vector2(triXFull, bandY + bandH + 5f), 25f, pointingDown: true, new Color(1f, 1f, 0f, 1f)); // bottom ▲ (upward)

            // Text below the bar: left-aligned "Avg since reset: {avgFull} ms"
            if (_textStyle == null)
            {
                _textStyle = new GUIStyle()
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 18,
                    normal = { textColor = Color.white }
                };
            }

            string avgText = $"Avg since reset: {avgFull:F2} ms";
            var textRect = new Rect(barRect.x, barRect.yMax + 2f, barRect.width, 22f);
            DrawLabelWithOutline(
                textRect,
                avgText,
                _textStyle,
                textColor: Color.white,
                outlineColor: Color.black,
                outlineSize: 1
            );
        }

        private static float MsToX(float ms, float rangeMs, Rect rect)
        {
            float t = Mathf.InverseLerp(-rangeMs, rangeMs, Mathf.Clamp(ms, -rangeMs, rangeMs));
            return rect.x + t * rect.width;
        }

        private static void DrawSegment(Rect barRect, float bandY, float bandH, float msA, float msB, Color c)
        {
            float x1 = MsToX(msA, TimeRangeMs, barRect);
            float x2 = MsToX(msB, TimeRangeMs, barRect);
            if (x2 < x1) { var tmp = x1; x1 = x2; x2 = tmp; }
            FillRect(new Rect(x1, bandY, Mathf.Max(1f, x2 - x1), bandH), c);
        }

        private static void FillRect(Rect r, Color c)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            EnsureGLMaterial();
            _glMat.SetPass(0);

            // Use pixel matrix so rect is in screen GUI coordinates (top-left origin)
            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);

            GL.Begin(GL.QUADS);
            GL.Color(c);
            GL.Vertex3(r.x, r.y, 0);
            GL.Vertex3(r.x + r.width, r.y, 0);
            GL.Vertex3(r.x + r.width, r.y + r.height, 0);
            GL.Vertex3(r.x, r.y + r.height, 0);
            GL.End();

            GL.PopMatrix();
        }

        private static void DrawTriangleGL(Vector2 apex, float size, bool pointingDown, Color color)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            EnsureGLMaterial();
            _glMat.SetPass(0);

            float halfBase = size * 0.6f;
            Vector2 p1 = apex;
            Vector2 p2, p3;

            if (pointingDown)
            {
                p2 = new Vector2(apex.x - halfBase, apex.y + size);
                p3 = new Vector2(apex.x + halfBase, apex.y + size);
            }
            else
            {
                p2 = new Vector2(apex.x - halfBase, apex.y - size);
                p3 = new Vector2(apex.x + halfBase, apex.y - size);
            }

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);

            GL.Begin(GL.TRIANGLES);
            GL.Color(color);
            GL.Vertex3(p1.x, p1.y, 0);
            GL.Vertex3(p2.x, p2.y, 0);
            GL.Vertex3(p3.x, p3.y, 0);
            GL.End();

            GL.PopMatrix();
        }

        private static void DrawLabelWithOutline(
            Rect rect,
            string text,
            GUIStyle style,
            Color textColor,
            Color outlineColor,
            int outlineSize
        )
        {
            // Outline: draw label offset around the main position
            var prevColor = style.normal.textColor;
            style.normal.textColor = outlineColor;

            // 8-directional outline
            for (int dx = -outlineSize; dx <= outlineSize; dx++)
            {
                for (int dy = -outlineSize; dy <= outlineSize; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var offRect = new Rect(rect.x + dx, rect.y + dy, rect.width, rect.height);
                    GUI.Label(offRect, text, style);
                }
            }

            // Main text
            style.normal.textColor = textColor;
            GUI.Label(rect, text, style);

            // Restore
            style.normal.textColor = prevColor;
        }

        private static void EnsureGLMaterial()
        {
            if (_glMat != null) return;

            var shader = Shader.Find("Hidden/Internal-Colored");
            _glMat = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            // Enable alpha blending, disable depth and culling for 2D overlay
            _glMat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            _glMat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            _glMat.SetInt("_Cull", (int)CullMode.Off);
            _glMat.SetInt("_ZWrite", 0);
            _glMat.SetInt("_ZTest", (int)CompareFunction.Always);
        }
    }
}