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

        /// <summary>
        /// The plate a procedural chart is drawn ON - paper, not the dark-dashboard near-black this was
        /// until 2026-08-10.
        ///
        /// ⚠ **Three renderers carried this identical value and all three were missed**, because a chart
        /// with no data yet draws no plate: at turn 0 the graphs say "No data yet" and the map is empty,
        /// so every v2.0 capture to date showed paper where real play would show black. PolicyWeb was
        /// only found first because its ring renders immediately.
        ///
        /// Rule 10 draws the line exactly here: the plate and frame AROUND a procedural chart are the
        /// v2.0 pack's business, the marks inside are not. Node inks, edge good/bad and area accents all
        /// stay exactly as they were - they are already on the aged palette.
        private static readonly Color BackgroundColor = PoliSimTheme.Card;
        private static readonly Color GridLineColor = PoliSimTheme.Hairline;
        private static readonly Color TradeLineColor = PoliSimTheme.TextSecondary;

        /// <summary>
        /// Fixed illustrative node position per country, normalized (0-1) - two loose clusters (USA
        /// alone west, the five European countries east), the European five kept in roughly their
        /// real relative east-west/north-south order (Sweden north, Poland east, France west, Italy
        /// south, Germany central) purely so the layout still reads sensibly, but with NO attempt at
        /// geographic accuracy or borders - this is a network diagram, not a map. Spaced generously to
        /// avoid the label-crowding the earlier coastline-constrained layouts had.
        /// </summary>
        /// <summary>
        /// P5-2 (board 6b row 1, 2026-09-03): THE GEOGRAPHIC CENTRES, one row per country - the table our side supplies. Each is
        /// the commonly cited geographic centre of the country's territory (the contiguous states for the USA), latitude and
        /// longitude in degrees, WGS84, as published on the national reference the point is named after. No coastline, no
        /// projection beyond a flat plate over the six's own bounding box - "six plates on a plain paper field". Tagged
        /// AUTHORED-REFERENCE: the figures are quoted, not measured here, and the reference point is named so it can be checked.
        /// The same table is delivered to Design as docs/COUNTRY_CENTROIDS.md.
        /// </summary>
        private static readonly Dictionary<CountryId, Vector2> CountryCentroids = new Dictionary<CountryId, Vector2>
        {
            { CountryId.USA, new Vector2(-98.5795f, 39.8283f) },      // Lebanon, Kansas - the geographic centre of the contiguous United States
            { CountryId.Sweden, new Vector2(16.3250f, 62.3875f) },    // Flataklocken, Medelpad - Sweden's geographic centre
            { CountryId.Poland, new Vector2(19.4794f, 52.0694f) },    // Piatek - the geographic centre of Poland
            { CountryId.Germany, new Vector2(10.4541f, 51.1642f) },   // Niederdorla, Thuringia - Germany's geographic centre
            { CountryId.France, new Vector2(2.4306f, 46.5386f) },     // Nassigny, Allier - the geographic centre of metropolitan France
            { CountryId.Italy, new Vector2(12.5167f, 42.5167f) },     // Narni, Umbria - the geographic centre of Italy
        };

        /// <summary>The 24-grid the chips snap to (board 6b row 1), in the map's own units at 1x; the Desk and the pair page place the chips identically because both snap the same way.</summary>
        private const float GridUnit = 24f;

        /// <summary>The centroids projected onto the unit square over the six's bounding box, padded so no chip sits on the edge - the normalised positions the rest of the renderer reads.</summary>
        private static readonly Dictionary<CountryId, Vector2> CountryMapPositions = ProjectCentroids();

        private static Dictionary<CountryId, Vector2> ProjectCentroids()
        {
            float lonMin = float.MaxValue, lonMax = float.MinValue, latMin = float.MaxValue, latMax = float.MinValue;
            foreach (Vector2 c in CountryCentroids.Values)
            {
                lonMin = Mathf.Min(lonMin, c.x); lonMax = Mathf.Max(lonMax, c.x);
                latMin = Mathf.Min(latMin, c.y); latMax = Mathf.Max(latMax, c.y);
            }
            const float pad = 0.06f;
            var result = new Dictionary<CountryId, Vector2>();
            foreach (KeyValuePair<CountryId, Vector2> kv in CountryCentroids)
            {
                float x = pad + (kv.Value.x - lonMin) / Mathf.Max(0.001f, lonMax - lonMin) * (1f - 2f * pad);
                float y = pad + (latMax - kv.Value.y) / Mathf.Max(0.001f, latMax - latMin) * (1f - 2f * pad);
                result[kv.Key] = new Vector2(x, y);
            }
            return result;
        }

        /// <summary>The chip's plate at 1x - board 6b row 1: 26 x 20, in the country's own outline at its centre.</summary>
        private const float ChipWidth = 26f;
        private const float ChipHeight = 20f;

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
            _lastLabelStyle = labelStyle;   // P5-2: the links snap to the same grid the chips do

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

            var nodeRects = new Dictionary<CountryId, Rect>();
            Dictionary<CountryId, Vector2> placed = SnappedPositions(rect, countries, labelReserveWidth, labelStyle);   // board 7a: the snap and the west-push, once
            foreach (Country country in countries)
            {
                Vector2 pixel = placed[country.Id];
                float diameter = nodeDiameters[country.Id];
                // P5-2 (board 6b row 1): a chip, 26 x 20 at 1x, in the country's own outline at its centre - the node's size no longer
                // says anything (the disc's diameter did); the chip carries the two-letter tag, the label beside it keeps the name.
                float u = Mathf.Max(1f, labelStyle.fontSize) / 14f;
                var nodeRect = new Rect(Mathf.Round(pixel.x - ChipWidth * u * 0.5f), Mathf.Round(pixel.y - ChipHeight * u * 0.5f), Mathf.Round(ChipWidth * u), Mathf.Round(ChipHeight * u));
                DrawChip(nodeRect, UiPalette.GetCountryColor(country.Id), country.Id, labelStyle);
                nodeRects[country.Id] = nodeRect;

                if (nodeRect.Contains(mousePosition))
                {
                    hoveredCountry = country.Id;
                    if (isClick)
                    {
                        clickedCountry = country.Id;
                    }
                }
            }

            // R-SP5: the names on their ladder, measured; drawn after every node so no name sits under a dot.
            foreach (LabelPlacement label in PlaceLabels(countries, nodeRects, labelStyle))
            {
                GUI.Label(label.Rect, label.Text, label.Style);
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

                    // P5-2 (board 6b row 1): the weight is the LARGER flow of the pair, and a link with no volume yet draws dashed.
                    float volume = Mathf.Max(link.ExportVolume, link.ImportVolume);
                    maxVolume = Mathf.Max(maxVolume, volume);
                    pairs.Add((country.Id, link.PartnerId, volume));
                }
            }

            Dictionary<CountryId, Vector2> placed = SnappedPositions(rect, countries, labelReserveWidth, _lastLabelStyle);   // board 7a: the links follow a pushed chip
            foreach ((CountryId a, CountryId b, float volume) in pairs)
            {
                Vector2 from = placed[a];
                Vector2 to = placed[b];
                float t = volume / maxVolume;
                float thickness = Mathf.Lerp(MinLineThickness, MaxLineThickness, t);
                float alpha = Mathf.Lerp(MinLineAlpha, MaxLineAlpha, t);
                var ink = new Color(TradeLineColor.r, TradeLineColor.g, TradeLineColor.b, alpha);
                if (volume <= 0f) { DrawDashedSegment(from, to, MinLineThickness, new Color(TradeLineColor.r, TradeLineColor.g, TradeLineColor.b, MinLineAlpha)); }
                else { DrawLineSegment(from, to, thickness, ink); }
            }
        }

        /// <summary>Draws a thick line as a rotated, stretched solid-color rect (GUIUtility.RotateAroundPivot) rather than a per-pixel distance field - cheap enough to redraw every frame (trade topology is static, but this keeps the code simple and stays correct even if it weren't), and GUI.matrix is always restored immediately after so rotation never leaks into anything drawn afterward.</summary>
        private GUIStyle _lastLabelStyle;

        /// <summary>P5-2: the 24-grid snap - the map's own units scale with the label face, so the Desk and the pair page snap alike.
        /// ⚠ Board 7a (2026-09-04): the pitch is the board's 24 at 1x. The first build halved it (a 12-unit snap) without saying so;
        /// 7a's arithmetic - DE and PL in one row in adjacent cells - is for the 24 pitch, and the deviation is withdrawn rather than
        /// carried: the board's cell column is the one the build now reads.</summary>
        private static float SnapUnit(GUIStyle labelStyle) =>
            Mathf.Max(4f, GridUnit * (labelStyle != null ? Mathf.Max(1f, labelStyle.fontSize) / 14f : 1f));

        private static Vector2 Snap(Vector2 pixel, Rect rect, float unit) =>
            new Vector2(rect.x + Mathf.Round((pixel.x - rect.x) / unit) * unit, rect.y + Mathf.Round((pixel.y - rect.y) / unit) * unit);

        /// <summary>
        /// Board 7a (2026-09-04, the one finding of placing the chips from the centroid table): a 26-wide chip on a 24 pitch overlaps its
        /// horizontal neighbour by 2 px, and the table puts two centroids that close (DE-PL, 9.03 deg of longitude - under two cells on
        /// any rect narrower than ~713 px). THE RULE, one and no new number: when two snapped chips share a row in adjacent cells, the
        /// WESTERN chip moves one cell west; west always has room (the USA holds it, the pad is never breached), east would breach
        /// the pad at PL. Repeated until no row holds an adjacent pair, so a push that lands beside a third chip pushes again. The
        /// links read the same positions, so a pushed chip's link ends move with it. Logged once per rect size and label face, so
        /// a film's log says which chip moved and where.
        /// </summary>
        private static Dictionary<CountryId, Vector2> SnappedPositions(Rect rect, IReadOnlyList<Country> countries, float labelReserveWidth, GUIStyle labelStyle)
        {
            float unit = SnapUnit(labelStyle);
            var positions = new Dictionary<CountryId, Vector2>();
            foreach (Country country in countries)
            {
                if (!CountryMapPositions.ContainsKey(country.Id)) { continue; }
                positions[country.Id] = Snap(ToPixel(rect, CountryMapPositions[country.Id], labelReserveWidth), rect, unit);
            }

            var pushed = new List<string>();
            for (int pass = 0; pass < 8; pass++)
            {
                bool moved = false;
                foreach (CountryId a in new List<CountryId>(positions.Keys))
                {
                    foreach (CountryId b in positions.Keys)
                    {
                        if (a == b) { continue; }
                        Vector2 pa = positions[a], pb = positions[b];
                        if (Mathf.Abs(pa.y - pb.y) > 0.5f) { continue; }                       // not the same row
                        if (Mathf.Abs(Mathf.Abs(pa.x - pb.x) - unit) > 0.5f) { continue; }     // not adjacent cells
                        CountryId western = pa.x < pb.x ? a : b;
                        Vector2 before = positions[western];
                        positions[western] = new Vector2(before.x - unit, before.y);
                        pushed.Add($"{western} one cell west ({before.x - rect.x:0},{before.y - rect.y:0}) -> ({before.x - unit - rect.x:0},{before.y - rect.y:0}) beside {(western == a ? b : a)}");
                        moved = true;
                        break;
                    }
                    if (moved) { break; }
                }
                if (!moved) { break; }
            }

            string key = $"{rect.width:0}x{rect.height:0}/{unit:0}";
            if (_loggedPlacements.Add(key))
            {
                var cells = new List<string>();
                foreach (KeyValuePair<CountryId, Vector2> kv in positions) { cells.Add($"{kv.Key} ({kv.Value.x - rect.x:0},{kv.Value.y - rect.y:0})"); }
                Debug.Log($"MAP: chips on the {unit:0}-pitch at {rect.width:0}x{rect.height:0} - {string.Join(", ", cells)}; west-push: {(pushed.Count == 0 ? "none" : string.Join("; ", pushed))}.");
            }
            return positions;
        }

        private static readonly HashSet<string> _loggedPlacements = new HashSet<string>();

        private static GUIStyle _chipTagStyle;

        /// <summary>P5-2: the chip - a paper plate in the country's own outline with its two-letter tag.</summary>
        private static void DrawChip(Rect rect, Color outline, CountryId id, GUIStyle labelStyle)
        {
            Color previous = GUI.color;
            GUI.color = outline; GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = PoliSimTheme.Card; GUI.DrawTexture(new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width - 3f, rect.height - 3f), Texture2D.whiteTexture);
            GUI.color = previous;
            if (_chipTagStyle == null || _chipTagStyle.fontSize != Mathf.Max(7, Mathf.RoundToInt(labelStyle.fontSize * 0.55f)))
            {
                _chipTagStyle = new GUIStyle(labelStyle) { fontSize = Mathf.Max(7, Mathf.RoundToInt(labelStyle.fontSize * 0.55f)), alignment = TextAnchor.MiddleCenter, wordWrap = false, fontStyle = FontStyle.Bold };
                if (PoliSimTheme.Document != null) { _chipTagStyle.font = PoliSimTheme.Document; }
                _chipTagStyle.padding = new RectOffset(0, 0, 0, 0);
            }
            _chipTagStyle.normal.textColor = outline;
            GUI.Label(rect, ChipTag(id), _chipTagStyle);
        }

        private static string ChipTag(CountryId id)
        {
            switch (id)
            {
                case CountryId.Sweden: return "SE";
                case CountryId.Germany: return "DE";
                case CountryId.France: return "FR";
                case CountryId.Italy: return "IT";
                case CountryId.Poland: return "PL";
                case CountryId.USA: return "US";
                default: return id.ToString().Substring(0, 2).ToUpperInvariant();
            }
        }

        /// <summary>P5-2: a dashed link - a link the model holds with no volume yet.</summary>
        private void DrawDashedSegment(Vector2 from, Vector2 to, float thickness, Color color)
        {
            Vector2 delta = to - from;
            float length = delta.magnitude;
            if (length < 1f) { return; }
            Vector2 dir = delta / length;
            const float dash = 6f, gap = 4f;
            for (float d = 0f; d < length; d += dash + gap)
            {
                float end = Mathf.Min(length, d + dash);
                DrawLineSegment(from + dir * d, from + dir * end, thickness, color);
            }
        }

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

        /// <summary>
        /// R-SP5 (the stage-prep micro-pass, 2026-08-28): country names on the map take §A.9a's resort
        /// ladder - the full name, then the abbreviation, then shrink toward the guard's floor - and the
        /// renderer MEASURES what it laid down (LastMinLabelSeparation) for the harness to assert. The
        /// abbreviation rung is ISO 3166-1 alpha-3: a standard identifier, not an invention (the seed
        /// sourcing already speaks in these codes). No label is ever moved off its node's row: a name
        /// that still cannot clear the floor after the ladder is RECORDED, never nudged.
        /// </summary>
        private static readonly Dictionary<CountryId, string> Iso3Codes = new Dictionary<CountryId, string>
        {
            { CountryId.USA, "USA" },
            { CountryId.Sweden, "SWE" },
            { CountryId.Germany, "DEU" },
            { CountryId.France, "FRA" },
            { CountryId.Italy, "ITA" },
            { CountryId.Poland, "POL" },
        };

        /// <summary>The clearance every label must keep from every other label and every other country's node - asserted by the harness after the map's captures, carried per rung on the ladder film.</summary>
        public const float MinLabelSeparationPx = 4f;

        private readonly List<(string Text, Rect Rect)> _lastLabelRects = new List<(string Text, Rect Rect)>();

        /// <summary>What the last Draw laid down, for the harness: the label rects; the smallest gap between any label and any other label or node; the highest rung the ladder needed (1 the full name, 3 the abbreviation, 4 the shrink); and the first pair still under the floor, or null.</summary>
        public IReadOnlyList<(string Text, Rect Rect)> LastLabelRects => _lastLabelRects;
        public float LastMinLabelSeparation { get; private set; } = float.PositiveInfinity;
        public int LastLabelRung { get; private set; } = 1;
        public string LastLabelViolation { get; private set; }

        private struct LabelPlacement
        {
            public CountryId Id;
            public string Text;
            public Rect Rect;
            public GUIStyle Style;
            public int Rung;
        }

        /// <summary>The gap between two rects along whichever axis separates them; zero when they overlap.</summary>
        private static float Gap(Rect a, Rect b)
        {
            float dx = Mathf.Max(b.x - a.xMax, a.x - b.xMax, 0f);
            float dy = Mathf.Max(b.y - a.yMax, a.y - b.yMax, 0f);
            return Mathf.Max(dx, dy);
        }

        private static Rect LabelRect(Rect node, string text, GUIStyle style)
        {
            Vector2 size = style.CalcSize(new GUIContent(text));
            return new Rect(node.xMax + LabelGap, node.center.y - size.y * 0.5f, size.x, size.y);
        }

        private static bool Clears(LabelPlacement p, int index, List<LabelPlacement> all, Dictionary<CountryId, Rect> nodeRects)
        {
            for (int j = 0; j < all.Count; j++)
            {
                if (j == index)
                {
                    continue;
                }

                if (Gap(p.Rect, all[j].Rect) < MinLabelSeparationPx || Gap(p.Rect, nodeRects[all[j].Id]) < MinLabelSeparationPx)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>How wide the label may be before its right edge comes within the floor of the nearest obstacle to its right on its own rows - the width the shrink rung aims at.</summary>
        private static float AllowedWidth(LabelPlacement p, int index, List<LabelPlacement> all, Dictionary<CountryId, Rect> nodeRects)
        {
            float allowed = float.PositiveInfinity;
            for (int j = 0; j < all.Count; j++)
            {
                if (j == index)
                {
                    continue;
                }

                foreach (Rect obstacle in new[] { all[j].Rect, nodeRects[all[j].Id] })
                {
                    bool onMyRows = obstacle.yMax > p.Rect.y - MinLabelSeparationPx && obstacle.y < p.Rect.yMax + MinLabelSeparationPx;
                    if (onMyRows && obstacle.x >= p.Rect.x)
                    {
                        allowed = Mathf.Min(allowed, obstacle.x - MinLabelSeparationPx - p.Rect.x);
                    }
                }
            }

            return Mathf.Max(0f, allowed);
        }

        /// <summary>
        /// §A.9a's ladder for the map's names (R-SP5): every name at full size to the right of its node;
        /// a name whose rect comes within MinLabelSeparationPx of another label or another country's
        /// node takes its ISO code; one that still does shrinks toward the guard's floor to the width
        /// that clears; one that still does not clear is recorded, never nudged - no repositioning
        /// algorithm is invented here. Labels are settled left to right so a label's obstacles to its
        /// left are already final. Deterministic, measured, no search.
        /// </summary>
        private List<LabelPlacement> PlaceLabels(IReadOnlyList<Country> countries, Dictionary<CountryId, Rect> nodeRects, GUIStyle labelStyle)
        {
            // Measured and drawn unwrapped: the label style wraps by default, and a wrapped name is not a
            // rect the ladder can reason about.
            var flat = new GUIStyle(labelStyle) { wordWrap = false };
            var placements = new List<LabelPlacement>(countries.Count);
            foreach (Country country in countries)
            {
                placements.Add(new LabelPlacement
                {
                    Id = country.Id,
                    Text = country.Name,
                    Style = flat,
                    Rung = 1,
                    Rect = LabelRect(nodeRects[country.Id], country.Name, flat),
                });
            }

            placements.Sort((a, b) => a.Rect.x.CompareTo(b.Rect.x));

            int highestRung = 1;
            for (int i = 0; i < placements.Count; i++)
            {
                LabelPlacement p = placements[i];
                if (!Clears(p, i, placements, nodeRects) && Iso3Codes.TryGetValue(p.Id, out string code) && code != p.Text)
                {
                    p.Text = code;
                    p.Rung = 3;
                    p.Rect = LabelRect(nodeRects[p.Id], p.Text, p.Style);
                }

                if (!Clears(p, i, placements, nodeRects))
                {
                    float allowed = AllowedWidth(p, i, placements, nodeRects);
                    float fullWidth = p.Style.CalcSize(new GUIContent(p.Text)).x;
                    if (allowed < fullWidth && fullWidth > 0f)
                    {
                        int size = Mathf.Max(PoliSimWidgets.MinMeasuredLabelFontSize, Mathf.FloorToInt(p.Style.fontSize * allowed / fullWidth));
                        p.Style = new GUIStyle(flat) { fontSize = size };
                        p.Rung = 4;
                        p.Rect = LabelRect(nodeRects[p.Id], p.Text, p.Style);
                    }
                }

                placements[i] = p;
                highestRung = Mathf.Max(highestRung, p.Rung);
            }

            // The measurement the harness asserts: the smallest gap between any label and any other
            // label or node, and the first pair still under the floor.
            float minGap = float.PositiveInfinity;
            string violation = null;
            _lastLabelRects.Clear();
            for (int i = 0; i < placements.Count; i++)
            {
                _lastLabelRects.Add((placements[i].Text, placements[i].Rect));
                for (int j = 0; j < placements.Count; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }

                    float gap = Mathf.Min(Gap(placements[i].Rect, placements[j].Rect), Gap(placements[i].Rect, nodeRects[placements[j].Id]));
                    if (gap < minGap)
                    {
                        minGap = gap;
                    }

                    if (gap < MinLabelSeparationPx && violation == null)
                    {
                        violation = placements[i].Text + " / " + placements[j].Text;
                    }
                }
            }

            LastMinLabelSeparation = placements.Count > 1 ? minGap : float.PositiveInfinity;
            LastLabelRung = highestRung;
            LastLabelViolation = violation;
            return placements;
        }

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
                // The map's own label backdrop - a near-black plate behind text, which on paper needs to be
                // paper. Alpha kept so it still lifts a label off whatever it overlaps.
                var color = new Color(PoliSimTheme.Card.r, PoliSimTheme.Card.g, PoliSimTheme.Card.b, 0.92f);
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
