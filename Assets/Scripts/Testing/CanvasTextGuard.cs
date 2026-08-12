using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PoliSim.Testing
{
    /// <summary>
    /// The Canvas cousin of the clipping-class guards (2026-08-12, built as scoped, on ruling).
    /// uGUI `Text` clips SILENTLY when its preferred height exceeds its rect — the failure mode that
    /// produced fourteen IMGUI instances arrived on a surface with no guard the day the Canvas path
    /// opened. This walks every loaded Canvas after `Canvas.ForceUpdateCanvases()` and flags:
    ///
    /// - `preferredHeight > rect.height` — the CLIPPING case (vertical default is Truncate);
    /// - `preferredWidth  > rect.width` when horizontal wrapping is OFF — the ESCAPE case, reported
    ///   under the same summary because a call site that turned wrapping off usually meant "fits".
    ///
    /// **Limits, verbatim from the scoping (rule 14):** coverage equals PINNED STATES — canvases
    /// build on demand, so a screen the driver never pins is never enumerated; `Overflow`-mode text
    /// that escapes is CONTAINMENT, not clipping, and a full ancestor-bounds sweep is the bounded
    /// extension this guard does not attempt; a fitting Text whose RECT clips against an ancestor
    /// mask is likewise unseen; TMP, if it ever arrives, has its own preferred-size API and belongs
    /// to the deferred-TMP record. Non-text content is out of scope entirely.
    ///
    /// **Fails at zero:** an assert that enumerated no Text verified nothing and says so as a
    /// failure, per the PartyMarkCoverageCheck precedent — a probe over an empty set must not
    /// report clean.
    /// </summary>
    public static class CanvasTextGuard
    {
        private const float Epsilon = 0.5f;

        /// <summary>
        /// Asserts over every active Text under every loaded Canvas. Returns the number of
        /// violations (0 is clean), or -1 when NOTHING was enumerated — the caller counts that as a
        /// failure, never as clean. Logs one line per violation and one summary line, in the run's
        /// standard grep-able shape.
        /// </summary>
        public static int Assert(string context)
        {
            Canvas.ForceUpdateCanvases();

            int texts = 0;
            int canvases = 0;
            int violations = 0;
            var detail = new StringBuilder();

            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                canvases++;
                foreach (Text text in canvas.GetComponentsInChildren<Text>(includeInactive: false))
                {
                    texts++;
                    Rect rect = text.rectTransform.rect;

                    if (text.preferredHeight > rect.height + Epsilon)
                    {
                        violations++;
                        detail.AppendLine($"CANVAS TEXT CLIP [{context}] '{Path(text.transform)}': preferredHeight {text.preferredHeight:F1} > rect {rect.height:F1} (\"{Shorten(text.text)}\")");
                    }

                    if (text.horizontalOverflow == HorizontalWrapMode.Overflow && text.preferredWidth > rect.width + Epsilon)
                    {
                        violations++;
                        detail.AppendLine($"CANVAS TEXT ESCAPE [{context}] '{Path(text.transform)}': preferredWidth {text.preferredWidth:F1} > rect {rect.width:F1} (\"{Shorten(text.text)}\")");
                    }
                }
            }

            if (texts == 0)
            {
                Debug.LogWarning($"CANVAS TEXT [{context}]: 0 texts under {canvases} canvas(es) - VERIFIED NOTHING; this is not evidence of fit.");
                return -1;
            }

            if (violations > 0)
            {
                Debug.LogError(detail.ToString().TrimEnd());
            }

            Debug.Log($"CANVAS TEXT [{context}]: {texts} text(s) across {canvases} canvas(es), {violations} violation(s).");
            return violations;
        }

        /// <summary>
        /// Both-directions self-test, per the standing guard discipline: a known-clipping Text and a
        /// known-fitting one on a throwaway canvas — a broken probe must not be able to report
        /// clean. Returns true when the probe catches the bad case AND passes the good one.
        /// </summary>
        public static bool SelfTest()
        {
            var root = new GameObject("CanvasTextGuardSelfTest");
            try
            {
                Canvas canvas = root.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                Text bad = MakeProbeText(root.transform, "KnownClipping",
                    "This deliberately long probe string must exceed a twelve-pixel-tall rect at any legible font size.",
                    new Vector2(120f, 12f));
                Text good = MakeProbeText(root.transform, "KnownFitting", "ok", new Vector2(400f, 80f));

                Canvas.ForceUpdateCanvases();
                bool caughtBad = bad.preferredHeight > bad.rectTransform.rect.height + Epsilon;
                bool passedGood = good.preferredHeight <= good.rectTransform.rect.height + Epsilon;

                if (caughtBad && passedGood)
                {
                    Debug.Log("SELFTEST canvas-text -> OK (known-clipping caught, known-fitting passed)");
                    return true;
                }

                Debug.LogError($"SELFTEST canvas-text FAILED: caughtBad={caughtBad} passedGood={passedGood} - the probe is broken, its clean reports are void.");
                return false;
            }
            finally
            {
                Object.Destroy(root);
            }
        }

        private static Text MakeProbeText(Transform parent, string name, string content, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.text = content;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.rectTransform.sizeDelta = size;
            return text;
        }

        private static string Path(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }

        private static string Shorten(string s) => s == null ? "" : (s.Length <= 40 ? s : s.Substring(0, 37) + "...");
    }
}
