using System.Collections.Generic;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// One fired event, tracked long enough to render a fading map marker - GameController owns the
    /// list (appended to in AdvanceTurn, pruned once fully faded), since SimulationManager.
    /// GetLastEvent only ever exposes the CURRENT turn's event, not a rolling history. Read-only
    /// snapshot; nothing here is ever written back into the simulation.
    /// </summary>
    public readonly struct MapEventMarker
    {
        public readonly CountryId CountryId;
        public readonly EconomicEvent Event;
        public readonly int TurnFired;

        public MapEventMarker(CountryId countryId, EconomicEvent economicEvent, int turnFired)
        {
            CountryId = countryId;
            Event = economicEvent;
            TurnFired = turnFired;
        }
    }

    /// <summary>
    /// Phase 5 of the UI revamp: an intentionally abstract "network" world-map widget - a full
    /// redesign, not a further iteration on the two earlier coastline-based attempts (a two-blob
    /// landmass pass, then a six-country-polygon pass), both of which fought the same underlying
    /// problem: hand-plotting a recognizable coastline from guessed vertices doesn't scale cleanly.
    /// This version sidesteps that entirely - a flat dark panel with a subtle grid, six GDP-sized
    /// circular nodes at fixed illustrative (not geographic) positions in two loose clusters, and
    /// trade-volume-weighted connecting lines reusing real TradeSystem/WorldFactory data. Same hand-
    /// drawn-Texture2D technique as GraphRenderer throughout; every stat and event this widget shows
    /// is read straight from existing Country/EconomyState/EconomicEvent/TradePartner data - no new
    /// simulation data of any kind.
    /// </summary>
    public class MapRenderer
    {
        private const int TextureWidth = 400;
        private const int TextureHeight = 240;
        private const int GridSpacing = 20;

        private const float BaseNodeDiameter = 22f;
        /// <summary>The smallest country's node is never drawn smaller than this fraction of the largest - legibility over strict GDP proportionality (Poland/Sweden must stay clearly visible next to the USA).</summary>
        private const float MinNodeSizeFraction = 0.6f;

        private const float MinEventDotDiameter = 6f;
        private const float MaxEventDotDiameter = 16f;

        private const float MinLineThickness = 1.5f;
        private const float MaxLineThickness = 5f;
        private const float MinLineAlpha = 0.22f;
        private const float MaxLineAlpha = 0.65f;

        private static readonly Color BackgroundColor = new Color(0.10f, 0.10f, 0.10f, 1f);
        private static readonly Color GridLineColor = new Color(0.14f, 0.14f, 0.15f, 1f);
        private static readonly Color TradeLineColor = new Color(0.62f, 0.62f, 0.68f, 1f);

        /// <summary>
        /// Fixed illustrative node position per country, normalized (0-1) - two loose clusters (USA
        /// alone west, the five European countries east), the European five kept in roughly their
        /// real relative east-west/north-south order (Sweden north, Poland east, France west, Italy
        /// south, Germany central) purely so the layout still reads sensibly, but with NO attempt at
        /// geographic accuracy or borders - this is a network diagram, not a map. Spaced generously to
        /// avoid the label-crowding the earlier coastline-constrained layouts had.
        /// </summary>
        private static readonly Dictionary<CountryId, Vector2> CountryMapPositions = new Dictionary<CountryId, Vector2>
        {
            { CountryId.USA, new Vector2(0.15f, 0.50f) },
            { CountryId.Sweden, new Vector2(0.72f, 0.12f) },
            { CountryId.Poland, new Vector2(0.90f, 0.32f) },
            { CountryId.Germany, new Vector2(0.68f, 0.34f) },
            { CountryId.France, new Vector2(0.50f, 0.50f) },
            { CountryId.Italy, new Vector2(0.66f, 0.70f) },
        };

        private Texture2D _backgroundTexture;
        private Texture2D _circleTexture;
        private Texture2D _lineTexture;

        /// <summary>
        /// Draws the map into <paramref name="rect"/>, handles hover/click hit-testing for both
        /// country nodes and event dots, and renders the hover tooltip itself (a floating overlay
        /// near the cursor - self-contained, so the caller doesn't need to manage tooltip layout).
        /// Returns the country/event clicked THIS event, if any (null most frames) - the caller owns
        /// what "selected" means (e.g. pinning a detail panel below the map). Interaction behavior is
        /// unchanged from the previous map iterations - this pass only redesigned the backdrop and
        /// node rendering.
        /// </summary>
        public void Draw(
            Rect rect,
            IReadOnlyList<Country> countries,
            CountryId playerCountryId,
            IReadOnlyList<MapEventMarker> eventMarkers,
            int currentTurn,
            int fadeTurns,
            GUIStyle labelStyle,
            out CountryId? clickedCountry,
            out MapEventMarker? clickedEvent)
        {
            EnsureTexturesInitialized();

            clickedCountry = null;
            clickedEvent = null;

            GUI.DrawTexture(rect, _backgroundTexture, ScaleMode.StretchToFill);

            Dictionary<CountryId, float> nodeDiameters = GetNodeDiameters(countries);

            // Measured against labelStyle (the style country names actually render in below), not a
            // fixed guess - recomputed every call since labelStyle's own font size changes every frame
            // in GameController.RescaleStylesToScreen as the window resizes. The original fixed 90f
            // LabelReserveWidth undersized this for "Sweden"/"Germany" at some window sizes, the same
            // label-truncation root cause found in the Sector/TaxLine/WelfareProgram/Policy-Web labels.
            float labelReserveWidth = GetLabelReserveWidth(countries, labelStyle);

            // Trade lines render first (underneath the nodes) - real TradePartner data, not decoration.
            DrawTradeLines(rect, countries, labelReserveWidth);

            Vector2 mousePosition = Event.current.mousePosition;
            bool isClick = Event.current.type == EventType.MouseDown && Event.current.button == 0;

            CountryId? hoveredCountry = null;
            MapEventMarker? hoveredEvent = null;

            foreach (MapEventMarker marker in eventMarkers)
            {
                int turnsSinceFired = currentTurn - marker.TurnFired;
                float fade = Mathf.Clamp01(1f - (float)turnsSinceFired / fadeTurns);
                if (fade <= 0f)
                {
                    continue;
                }

                Vector2 pixel = ToPixel(rect, CountryMapPositions[marker.CountryId], labelReserveWidth);
                float nodeDiameter = nodeDiameters[marker.CountryId];
                pixel += new Vector2(nodeDiameter * 0.6f, -nodeDiameter * 0.6f);

                float severity = GetSeverity(marker.Event);
                float diameter = Mathf.Lerp(MinEventDotDiameter, MaxEventDotDiameter, severity);
                Color baseColor = marker.Event.ApprovalEffect >= 0f ? UiPalette.PositiveChangeColor : UiPalette.NegativeChangeColor;
                Color dotColor = new Color(baseColor.r, baseColor.g, baseColor.b, fade);

                var dotRect = new Rect(pixel.x - diameter * 0.5f, pixel.y - diameter * 0.5f, diameter, diameter);
                DrawCircle(dotRect, dotColor);

                if (dotRect.Contains(mousePosition))
                {
                    hoveredEvent = marker;
                    if (isClick)
                    {
                        clickedEvent = marker;
                    }
                }
            }

            foreach (Country country in countries)
            {
                Vector2 pixel = ToPixel(rect, CountryMapPositions[country.Id], labelReserveWidth);
                Color nodeColor = UiPalette.GetCountryColor(country.Id);
                float diameter = nodeDiameters[country.Id];

                var nodeRect = new Rect(pixel.x - diameter * 0.5f, pixel.y - diameter * 0.5f, diameter, diameter);
                DrawCircle(nodeRect, nodeColor);

                var nameRect = new Rect(pixel.x + diameter * 0.5f + 3f, pixel.y - labelStyle.fontSize * 0.5f, labelReserveWidth, labelStyle.fontSize + 4f);
                GUI.Label(nameRect, country.Name, labelStyle);

                if (nodeRect.Contains(mousePosition))
                {
                    hoveredCountry = country.Id;
                    if (isClick)
                    {
                        clickedCountry = country.Id;
                    }
                }
            }

            if (hoveredCountry.HasValue)
            {
                DrawCountryTooltip(mousePosition, hoveredCountry.Value, countries, labelStyle);
            }
            else if (hoveredEvent.HasValue)
            {
                DrawEventTooltip(mousePosition, hoveredEvent.Value, labelStyle);
            }
        }

        /// <summary>GDP-proportional diameter per country, clamped so the smallest is never below MinNodeSizeFraction of the largest - legibility over strict proportionality.</summary>
        private static Dictionary<CountryId, float> GetNodeDiameters(IReadOnlyList<Country> countries)
        {
            float minGdp = float.MaxValue;
            float maxGdp = float.MinValue;
            foreach (Country country in countries)
            {
                minGdp = Mathf.Min(minGdp, country.State.GDP);
                maxGdp = Mathf.Max(maxGdp, country.State.GDP);
            }

            var diameters = new Dictionary<CountryId, float>();
            float range = maxGdp - minGdp;
            foreach (Country country in countries)
            {
                float t = range > 0.01f ? (country.State.GDP - minGdp) / range : 1f;
                float sizeFraction = Mathf.Lerp(MinNodeSizeFraction, 1f, t);
                diameters[country.Id] = BaseNodeDiameter * sizeFraction;
            }
            return diameters;
        }

        /// <summary>
        /// One line per unique bilateral TradePartner pair among the six countries (each pair has a
        /// reciprocal entry on both sides - see WorldFactory.AddBilateralTrade - so pairs are only
        /// drawn once, when the owning country's id is numerically less than the partner's, not
        /// twice). Thickness and opacity both scale with that pair's total trade volume (export +
        /// import), normalized against the largest pair in the network - the same "proportional to
        /// what it's showing" principle GraphRenderer's own bars/axes already use.
        /// </summary>
        private void DrawTradeLines(Rect rect, IReadOnlyList<Country> countries, float labelReserveWidth)
        {
            var pairs = new List<(CountryId a, CountryId b, float volume)>();
            float maxVolume = 1f;

            foreach (Country country in countries)
            {
                foreach (TradePartner link in country.TradePartners)
                {
                    if ((int)country.Id >= (int)link.PartnerId)
                    {
                        continue;
                    }
                    if (!CountryMapPositions.ContainsKey(link.PartnerId))
                    {
                        continue;
                    }

                    float volume = link.ExportVolume + link.ImportVolume;
                    maxVolume = Mathf.Max(maxVolume, volume);
                    pairs.Add((country.Id, link.PartnerId, volume));
                }
            }

            foreach ((CountryId a, CountryId b, float volume) in pairs)
            {
                Vector2 from = ToPixel(rect, CountryMapPositions[a], labelReserveWidth);
                Vector2 to = ToPixel(rect, CountryMapPositions[b], labelReserveWidth);
                float t = volume / maxVolume;
                float thickness = Mathf.Lerp(MinLineThickness, MaxLineThickness, t);
                float alpha = Mathf.Lerp(MinLineAlpha, MaxLineAlpha, t);

                DrawLineSegment(from, to, thickness, new Color(TradeLineColor.r, TradeLineColor.g, TradeLineColor.b, alpha));
            }
        }

        /// <summary>Draws a thick line as a rotated, stretched solid-color rect (GUIUtility.RotateAroundPivot) rather than a per-pixel distance field - cheap enough to redraw every frame (trade topology is static, but this keeps the code simple and stays correct even if it weren't), and GUI.matrix is always restored immediately after so rotation never leaks into anything drawn afterward.</summary>
        private void DrawLineSegment(Vector2 from, Vector2 to, float thickness, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 1f)
            {
                return;
            }

            float angleDegrees = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Matrix4x4 previousMatrix = GUI.matrix;
            Color previousColor = GUI.color;

            GUIUtility.RotateAroundPivot(angleDegrees, from);
            GUI.color = color;
            GUI.DrawTexture(new Rect(from.x, from.y - thickness * 0.5f, length, thickness), GetLineTexture(), ScaleMode.StretchToFill);

            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        /// <summary>Reserved space (px) on every side so a node - and, on the right, its name label, which always renders to the right of the node - can never sit flush against or past the panel's actual edge, at any panel size. The largest a node can ever be (BaseNodeDiameter, before the size-clamp shrinks smaller-GDP countries further); the label's own width is now measured (see GetLabelReserveWidth), not a fixed guess.</summary>
        private const float PanelMargin = 12f;
        private const float LabelGap = 3f;

        /// <summary>Widest country Name as rendered in labelStyle (the style the name labels actually use in Draw), plus a small right-side pad - recomputed every Draw call, not cached, since labelStyle's own font size changes every frame in GameController.RescaleStylesToScreen as the window resizes. Replaces a fixed 90f constant that undersized this for "Sweden"/"Germany" at some window sizes (they truncated to "Swede"/"Germa") - the same label-truncation root cause found in the Sector/TaxLine/WelfareProgram/Policy-Web labels.</summary>
        private static float GetLabelReserveWidth(IReadOnlyList<Country> countries, GUIStyle labelStyle)
        {
            float widest = 0f;
            foreach (Country country in countries)
            {
                widest = Mathf.Max(widest, labelStyle.CalcSize(new GUIContent(country.Name)).x);
            }
            return widest + 6f;
        }

        /// <summary>
        /// Maps a normalized (0-1) position to actual pixels within <paramref name="rect"/> - recomputed
        /// fresh every call from the panel's REAL current width/height (never a cached/stale value),
        /// same as the rest of this screen-relative UI. Insets the usable area by PanelMargin on every
        /// side, plus extra on the right for <paramref name="labelReserveWidth"/>, so a normalized
        /// position near 1.0 (e.g. Poland's) can never place a node or its label past the panel's actual
        /// boundary regardless of window size - the bug this fixes (Poland pushed off the visible edge
        /// at smaller windows) happened because the panel's real width was smaller than what the raw
        /// normalized*width math assumed the label had room for.
        /// </summary>
        private static Vector2 ToPixel(Rect rect, Vector2 normalized, float labelReserveWidth)
        {
            float rightInset = PanelMargin + labelReserveWidth + LabelGap + BaseNodeDiameter * 0.5f;
            float usableWidth = Mathf.Max(1f, rect.width - PanelMargin - rightInset);
            float usableHeight = Mathf.Max(1f, rect.height - PanelMargin * 2f);

            return new Vector2(
                rect.x + PanelMargin + normalized.x * usableWidth,
                rect.y + PanelMargin + normalized.y * usableHeight);
        }

        /// <summary>
        /// Normalized 0-1 severity derived from the SAME shock fields the 8-&gt;24 event pool already
        /// has (see CLAUDE.md's "Expanded Event Pool" for the documented per-field envelope this
        /// normalizes against) - no new "severity" field invented on EconomicEvent itself, just a
        /// UI-side reading of data that's already there.
        /// </summary>
        private static float GetSeverity(EconomicEvent economicEvent)
        {
            const float GdpEnvelope = 2.5f;
            const float InflationEnvelope = 1.5f;
            const float ApprovalEnvelope = 5f;

            float gdpFraction = Mathf.Abs(economicEvent.GdpShockPercent) / GdpEnvelope;
            float inflationFraction = Mathf.Abs(economicEvent.InflationShockPoints) / InflationEnvelope;
            float approvalFraction = Mathf.Abs(economicEvent.ApprovalEffect) / ApprovalEnvelope;

            float severity = gdpFraction * 0.5f + approvalFraction * 0.35f + inflationFraction * 0.15f;
            return Mathf.Clamp01(severity);
        }

        private void DrawCountryTooltip(Vector2 mousePosition, CountryId countryId, IReadOnlyList<Country> countries, GUIStyle labelStyle)
        {
            Country country = null;
            foreach (Country candidate in countries)
            {
                if (candidate.Id == countryId)
                {
                    country = candidate;
                    break;
                }
            }
            if (country == null)
            {
                return;
            }

            EconomyState state = country.State;
            string text = $"{country.Name}\nGDP: {UiFormat.Money(state.GDP, MoneyUnit.Billions)}\nUnemployment: {state.Unemployment:F2}%\nApproval: {state.ApprovalRating:F1}";
            DrawTooltipBox(mousePosition, text, labelStyle);
        }

        private void DrawEventTooltip(Vector2 mousePosition, MapEventMarker marker, GUIStyle labelStyle)
        {
            string text = $"{marker.Event.Name}\n(click for details)";
            DrawTooltipBox(mousePosition, text, labelStyle);
        }

        private void DrawTooltipBox(Vector2 mousePosition, string text, GUIStyle labelStyle)
        {
            var tooltipStyle = new GUIStyle(labelStyle) { wordWrap = true };
            Vector2 size = tooltipStyle.CalcSize(new GUIContent(text));
            float boxWidth = Mathf.Max(size.x + 12f, 140f);
            float boxHeight = tooltipStyle.CalcHeight(new GUIContent(text), boxWidth) + 8f;

            var boxRect = new Rect(mousePosition.x + 14f, mousePosition.y + 14f, boxWidth, boxHeight);
            GUI.DrawTexture(boxRect, GetSolidBackground(), ScaleMode.StretchToFill);
            GUI.Label(new Rect(boxRect.x + 6f, boxRect.y + 4f, boxRect.width - 12f, boxRect.height - 8f), text, tooltipStyle);
        }

        private Texture2D _tooltipBackground;
        private Texture2D GetSolidBackground()
        {
            if (_tooltipBackground == null)
            {
                _tooltipBackground = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                var color = new Color(0.05f, 0.05f, 0.06f, 0.92f);
                _tooltipBackground.SetPixels(new[] { color, color, color, color });
                _tooltipBackground.Apply(false);
            }
            return _tooltipBackground;
        }

        private void DrawCircle(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _circleTexture, ScaleMode.StretchToFill);
            GUI.color = previous;
        }

        private Texture2D GetLineTexture()
        {
            if (_lineTexture == null)
            {
                _lineTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
                var white = Color.white;
                _lineTexture.SetPixels(new[] { white, white, white, white });
                _lineTexture.Apply(false);
            }
            return _lineTexture;
        }

        private void EnsureTexturesInitialized()
        {
            if (_backgroundTexture == null)
            {
                _backgroundTexture = BuildBackgroundTexture();
            }
            if (_circleTexture == null)
            {
                _circleTexture = BuildCircleTexture(32);
            }
        }

        /// <summary>Flat dark panel (matching GraphRenderer's own background tone for consistency) with a very subtle fine grid - no ocean/landmass geography at all, per this redesign's explicitly abstract "network diagram" framing.</summary>
        private static Texture2D BuildBackgroundTexture()
        {
            var texture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            var pixels = new Color[TextureWidth * TextureHeight];
            for (int y = 0; y < TextureHeight; y++)
            {
                for (int x = 0; x < TextureWidth; x++)
                {
                    bool onGridLine = x % GridSpacing == 0 || y % GridSpacing == 0;
                    pixels[y * TextureWidth + x] = onGridLine ? GridLineColor : BackgroundColor;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
            return texture;
        }

        private static Texture2D BuildCircleTexture(int diameter)
        {
            var texture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            float radius = diameter / 2f;
            var pixels = new Color[diameter * diameter];

            for (int y = 0; y < diameter; y++)
            {
                for (int x = 0; x < diameter; x++)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    pixels[y * diameter + x] = dist <= radius ? Color.white : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
            return texture;
        }
    }
}
