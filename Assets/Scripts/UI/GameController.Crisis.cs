using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// MF q33 (K141-K144; the model is `SovereignCrisis`, built in record 930), drawn to D-WORKLIST §1 (Design's return of 2026-10-28, boards 32a-32e): THE
    /// DEBT CRISIS ON SCREEN.
    /// <list type="bullet">
    /// <item><b>The moment</b> (32a a default, 32b a programme): on the review day the trigger reads CCC, once per crisis (<see cref="TopStrip.CrisisPlateOwed"/>), one
    /// plate over the page below the strip - the head (Market access lost, the country and the review's day, the branch's stamp), the rating's ladder at the
    /// review with the debt and the deficit (the USA's third row: the dollar's discount, none - K143), and the branch's columns: for a default what follows
    /// (the haircut, the output cost, the years shut out, no borrowing) beside this year's budget with the cut; for a programme the lender, the three
    /// years and the loans' price beside the targets' path and what met and missed do. It stops the clock as the signing plate does (the gate in
    /// `GameController.Update`); <b>Continue</b> lets it run again and nothing else on the plate is a control. DECLARED: drawn in IMGUI over the page
    /// rather than as a Canvas takeover like the signing plate, because the board keeps the rail and the strip live above and beside it, and a live Canvas
    /// suppresses the IMGUI frame.</item>
    /// <item><b>The after-state</b>: the strip's chip (<see cref="TopStrip.CrisisChip"/>); the Desk's first card in World trade's place for as long as the
    /// crisis lasts (32c a default: the exclusion's timeline, the output cost landing, the cut so far, the haircut dated; 32d a programme: the target path
    /// with the running deficit drawn across this year's column, met and missed, the withheld year); the Budget's programme target on the balance strip and
    /// the if-passed panel's row (32e).</item>
    /// </list>
    /// Every figure is the model's: its constants (`SovereignCrisis.HaircutFraction`, `SovereignCrisis.OutputCostPercent`, `SovereignCrisis.ExclusionYears`,
    /// `SovereignCrisis.ProgrammeYears`, the targets by `SovereignCrisis.TargetFor`) and the country's own crisis state - never the board's illustrative 2031.
    /// </summary>
    public partial class GameController
    {
        /// <summary>The crises the player has acknowledged on this country object - the screen's, not the model's, so not saved (the K42 idiom: a game loaded on
        /// the day shows the plate again).</summary>
        private object _crisisPlateSeenCountry;
        private int _crisisPlateSeenCrises;
        /// <summary>The plate's Continue as the last Repaint laid it out - the input pass reads it before the page draws.</summary>
        private Rect _crisisContinueRect;
        /// <summary>L1: the plate was up on the last pass - its rising edge clears the keyboard and the drag.</summary>
        private bool _crisisPlateWasUp;
        /// <summary>L2: the plate's own slips (the head's sources, the haircut's stock) - opened after the slip delay and drawn over the plate, since the page's
        /// slip pass runs before the plate draws; a film's parked cursor never opens one.</summary>
        private string _crisisSlipHover;
        private float _crisisSlipSince;
        private readonly List<(Rect Rect, SlipContent Slip, string Id)> _crisisSlips = new List<(Rect Rect, SlipContent Slip, string Id)>();

        private static readonly Color PlatePaper = PoliSimTheme.Hex(0xF2EADB), PlateRule = PoliSimTheme.Hex(0xD5C8AB), PlateTrack = PoliSimTheme.Hex(0xE2D7C1),
            PlateScrim = new Color(28f / 255f, 21f / 255f, 14f / 255f, 0.58f), PlateButton = PoliSimTheme.Hex(0xB8995A), PlateButtonEdge = PoliSimTheme.Hex(0x7D6534),
            PlateLadderMuted = PoliSimTheme.Hex(0x8A7A5C);

        /// <summary>MF q33: the debt crisis's plate is owed and holds the clock (the strip names it among the holds).</summary>
        private bool CrisisPlateHolds() =>
            _playerCountry != null && _simulationManager != null && !_isGameOver
            && TopStrip.CrisisPlateOwed(_playerCountry, _simulationManager.CurrentDate, ReferenceEquals(_crisisPlateSeenCountry, _playerCountry) ? _crisisPlateSeenCrises : 0);

        /// <summary>The plate's Continue (and the film's, by reflection - one path): the crises entered so far are acknowledged, and the clock runs again.</summary>
        private void AcknowledgeCrisisPlate()
        {
            if (_playerCountry?.Crisis == null) { return; }
            _crisisPlateSeenCountry = _playerCountry;
            _crisisPlateSeenCrises = _playerCountry.Crisis.Crises;
        }

        /// <summary>Before the page draws: while the plate is up, a pointer event over the page is the plate's - Continue's press acknowledges it, every other
        /// one is swallowed, so nothing under the scrim answers. The rail and the strip stay live.</summary>
        private void CrisisPlateInput(Rect page)
        {
            if (!CrisisPlateHolds()) { _crisisPlateWasUp = false; return; }
            Event e = Event.current;
            // the review of wave/dcrisis, L1: on the plate's rising edge no control under it keeps the keyboard or a drag; while it is up no key reaches the page
            if (!_crisisPlateWasUp) { GUIUtility.keyboardControl = 0; GUIUtility.hotControl = 0; _crisisPlateWasUp = true; }
            if (e.type == EventType.KeyDown || e.type == EventType.KeyUp) { e.Use(); return; }
            bool pointer = e.type == EventType.MouseDown || e.type == EventType.MouseUp || e.type == EventType.MouseDrag || e.type == EventType.ScrollWheel;
            if (!pointer || !page.Contains(e.mousePosition)) { return; }
            if (e.type == EventType.MouseDown && e.button == 0 && _crisisContinueRect.Contains(e.mousePosition))
            {
                AcknowledgeCrisisPlate();
                AudioDirector.Fire(AudioCue.FolderSwitch);
            }
            e.Use();
        }

        private static string CrisisDay(System.DateTime d) => TopStrip.Day(d);

        private static string Pct(float v) => UiFormat.Number(v, 1) + " %";

        /// <summary>The rating's broad letter on the board's ladder (AAA, AA, A, BBB, BB, B, CCC) - the notch's sign dropped.</summary>
        private static string LadderLetter(CreditRating r) => CreditRatingSystem.Format(r).TrimEnd('+', '-', '−');

        private static readonly string[] Ladder = { "AAA", "AA", "A", "BBB", "BB", "B", "CCC" };

        /// <summary>The review, L9: the cut is the run's sum (`SovereignCrisisState.CutBillions`), so it reads "so far" in a first crisis and says it is the run's after a second crisis.</summary>
        private static string CutSpan(SovereignCrisisState s) => s.Crises > 1 ? " across the run's crises" : " so far";

        private static Dictionary<long, Texture2D> _hatches;

        /// <summary>The board's hatch (a 3 px stripe in <paramref name="ink"/> on a 6 px pitch over <paramref name="paper"/>), tiled across <paramref name="r"/>.</summary>
        private static void DrawHatch(Rect r, Color ink, Color paper)
        {
            if (Event.current.type != EventType.Repaint || r.width <= 0f || r.height <= 0f) { return; }
            if (_hatches == null) { _hatches = new Dictionary<long, Texture2D>(); }
            long key = ((long)((Color32)ink).GetHashCode() << 32) ^ (uint)((Color32)paper).GetHashCode();
            if (!_hatches.TryGetValue(key, out Texture2D tex) || tex == null)
            {
                tex = new Texture2D(6, 6, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.HideAndDontSave };
                var px = new Color[36];
                for (int y = 0; y < 6; y++) { for (int x = 0; x < 6; x++) { px[y * 6 + x] = (x + y) % 6 < 3 ? ink : paper; } }
                tex.SetPixels(px);
                tex.Apply();
                _hatches[key] = tex;
            }
            float unit = Mathf.Max(1f, V35.Px(6f));
            GUI.DrawTextureWithTexCoords(r, tex, new Rect(0f, 0f, r.width / unit, r.height / unit));
        }

        private static void Outline(Rect r, Color ink, float w)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            PoliSimTheme.Rule(new Rect(r.x, r.y, r.width, w), ink);
            PoliSimTheme.Rule(new Rect(r.x, r.yMax - w, r.width, w), ink);
            PoliSimTheme.Rule(new Rect(r.x, r.y, w, r.height), ink);
            PoliSimTheme.Rule(new Rect(r.xMax - w, r.y, w, r.height), ink);
        }

        /// <summary>A label in <paramref name="r"/>, Repaint only.</summary>
        private static void PlateText(Rect r, string text, GUIStyle face)
        {
            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(text)) { return; }
            // the films of e15a0eb4 (OVERFLOW at 14-30 px): the rect is never shorter than the face's own line (CalcSize - about 1.25 x the px), grown about
            // its centre, so a caller's px + 2 height can never make MeasuredLabel shrink the text
            float need = Mathf.Ceil(face.CalcSize(new GUIContent(text)).y);
            if (r.height < need) { r = new Rect(r.x, r.y + Mathf.Floor((r.height - need) * 0.5f), r.width, need); }
            PoliSimWidgets.MeasuredLabel(r, text, face);
        }

        /// <summary>A figure goes inside a bar only where it fits at its face (never shrunk below the floor); false where it does not, and the caller says it elsewhere.</summary>
        private static bool FitsIn(GUIStyle face, string text, float width) => width > 0f && Mathf.Ceil(face.CalcSize(new GUIContent(text)).x) + 2f <= width;

        private static float TextWidth(GUIStyle face, string text) => Mathf.Ceil(face.CalcSize(new GUIContent(text ?? string.Empty)).x);

        /// <summary>An outlined stamp (the branch's, CCC) ending at <paramref name="right"/>, centred on <paramref name="midY"/>; returns its rect.</summary>
        private Rect DrawCrisisStamp(float right, float midY, string text, Color ink, float px, float height)
        {
            GUIStyle face = V35Mono(px, ink, bold: true, TextAnchor.MiddleCenter);
            float w = TextWidth(face, text) + V35.Px(14f);
            var r = new Rect(right - w, Mathf.Round(midY - height * 0.5f), w, height);
            Outline(r, ink, 1.5f);
            PlateText(r, text, face);
            return r;
        }

        /// <summary>The settled deficit the programme's review reads (the trailing year to the last closed quarter, `CreditRatingSystem.GetSettledPosition`) and
        /// the quarter's day it runs to; null before a year of closings exists.</summary>
        private static float? SettledDeficit(Country c, System.DateTime today, out System.DateTime asOf)
        {
            asOf = ReleaseCalendar.GetCurrentPeriodStart(ClosingStat.Gdp, today.AddDays(-1));
            CreditRatingSystem.GetSettledPosition(c, today, out _, out float? deficit, out _);
            return deficit;
        }

        // ==============================================================================================================================================
        // The moment (boards 32a, 32b)
        // ==============================================================================================================================================

        /// <summary>The plate over <paramref name="page"/> (the frame's area coordinates), while it is owed.</summary>
        private void DrawCrisisPlate(Rect page)
        {
            if (!CrisisPlateHolds()) { return; }
            Country c = _playerCountry;
            SovereignCrisisState s = c.Crisis;
            System.DateTime today = _simulationManager.CurrentDate;
            bool isDefault = s.Phase == CrisisPhase.Default;
            Color branch = isDefault ? PoliSimTheme.Bad : PoliSimTheme.Caution;
            if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(page, PlateScrim); }

            float w = Mathf.Min(page.width - V35.Px(24f), V35.Px(1080f)), h = Mathf.Min(page.height - V35.Px(16f), V35.Px(614f));
            var plate = new Rect(Mathf.Round(page.x + (page.width - w) * 0.5f), Mathf.Round(page.y + Mathf.Min(V35.Px(23f), (page.height - h) * 0.5f)), w, h);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(plate.x + V35.Px(2f), plate.y + V35.Px(8f), plate.width, plate.height), new Color(0f, 0f, 0f, 0.35f));
                PoliSimTheme.Rule(plate, PlatePaper);
            }
            Outline(plate, StripLine, 1f);
            var inner = new Rect(plate.x + V35.Px(32f), plate.y + V35.Px(22f), plate.width - V35.Px(64f), plate.height - V35.Px(42f));

            // ---- the head ----
            float headH = V35.Px(34f), icon = V35.Px(30f);
            DrawV35Icon(new Rect(inner.x, inner.y + Mathf.Round((headH - icon) * 0.5f), icon, icon), "debt", branch);
            GUIStyle title = V35Serif(28f, PoliSimTheme.TextPrimary);
            float tx = inner.x + icon + V35.Px(16f), tw = TextWidth(title, "Market access lost");
            PlateText(new Rect(tx, inner.y, tw + 2f, headH), "Market access lost", title);
            Rect stamp = DrawCrisisStamp(inner.xMax, inner.y + headH * 0.5f, isDefault ? "DEFAULT" : "PROGRAMME", branch, 15f, V35.Px(24f));
            GUIStyle sub = V35Serif(17f, PoliSimTheme.TextMuted);
            float sx = tx + tw + V35.Px(16f);
            string subText = c.Name + " · the review of " + CrisisDay(s.StartedOn);
            PlateText(new Rect(sx, inner.y, Mathf.Max(1f, stamp.x - V35.Px(16f) - sx), headH), V35Fit(subText, sub, Mathf.Max(1f, stamp.x - V35.Px(16f) - sx), out _), sub);
            float ruleY = inner.y + headH + V35.Px(10f);
            if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(new Rect(inner.x, ruleY, inner.width, 1f), PoliSimTheme.TextPrimary); }
            _crisisSlips.Clear();
            _crisisSlips.Add((new Rect(inner.x, inner.y, inner.width, headH), CrisisHeadSlip(c, s, isDefault), "plate:head"));   // the review, L2: the head's slip carries the sources
            _crisisTerms = CrisisTerms(c, isDefault);   // K322 S5: the sources at level 2

            // ---- the foot: the caption and Continue ----
            float footH = V35.Px(36f);
            GUIStyle button = V35Serif(16f, PoliSimTheme.TextPrimary, TextAnchor.MiddleCenter);
            float bw = TextWidth(button, "Continue") + V35.Px(44f);
            var cont = new Rect(inner.xMax - bw, inner.yMax - footH, bw, footH);
            if (Event.current.type == EventType.Repaint)
            {
                _crisisContinueRect = cont;
                PoliSimTheme.Rule(cont, PlateButton);
            }
            Outline(cont, PlateButtonEdge, 1f);
            PlateText(cont, "Continue", button);
            // K322 S5 (K320 (2): no sentence at rest): the foot says the clock waits; until what is its slip
            GUIStyle footFace = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
            const string footText = "The clock waits";
            var footRect = new Rect(inner.x, cont.y, Mathf.Max(1f, cont.x - inner.x - V35.Px(14f)), footH);
            PlateText(footRect, footText, footFace);
            _crisisSlips.Add((new Rect(footRect.x, footRect.y, Mathf.Min(footRect.width, TextWidth(footFace, footText) + 2f), footRect.height), new SlipContent("PAUSED").Add("The clock waits until you continue"), "plate:foot"));

            // ---- the three columns ----
            float top = ruleY + 1f + V35.Px(16f), bottom = cont.y - V35.Px(16f);
            float gap = V35.Px(30f), leftW = V35.Px(300f), midW = V35.Px(340f);
            var left = new Rect(inner.x, top, leftW, bottom - top);
            var mid = new Rect(left.xMax + gap, top, midW, bottom - top);
            var right = new Rect(mid.xMax + gap, top, Mathf.Max(1f, inner.xMax - mid.xMax - gap), bottom - top);
            DrawPlateRating(left, c, today);
            DrawPlateWhatFollows(mid, c, s, isDefault);
            if (isDefault) { DrawPlateBudget(right, c); }
            else { DrawPlateTargets(right, s); }
            DrawCrisisSlips(plate);
        }

        /// <summary>The film's hook (the plate's slips never open on a parked cursor): an anchor id whose slip is drawn open; null for none.</summary>
        private string _crisisSlipForFilm;

        /// <summary>L2: the head's slip - what happened, and the sources the branch's figures rest on (the ones `SovereignCrisis` stores).</summary>
        private static SlipContent CrisisHeadSlip(Country c, SovereignCrisisState s, bool isDefault)
        {
            // K322 S5 (K320 (3), K321 (f): a level-1 slip is at most 4 lines of at most 10 counted words): what happened in fragments, the lender's words
            // on the Lender row's own slip, and the sources the level-2 sentence behind the marked SOURCES (<see cref="CrisisTerms"/>)
            var slip = new SlipContent("MARKET ACCESS LOST · " + CrisisDay(s.StartedOn).ToUpperInvariant())
                .Add("The scheduled review of " + CrisisDay(s.StartedOn) + " · the rating at CCC")
                .Add("The market closed");
            if (isDefault)
            {
                slip.Add("The United States defaults · read without the dollar's discount");
            }
            else
            {
                slip.Add(c.Name + " enters a programme");
            }
            slip.Add("[[SOURCES]]");
            return slip;
        }

        /// <summary>K322 S5: the plate's level-2 slips - SOURCES, the sentence the head's slip cited whole before its lines were capped (the review 2, N5:
        /// cited as the model holds them; the C&amp;T article is billed).</summary>
        private static Dictionary<string, SlipContent> CrisisTerms(Country c, bool isDefault)
        {
            string sources = isDefault
                ? "Cruces and Trebesch 2012, the " + UiFormat.Number(SovereignCrisis.HaircutFraction * 100f, 0) + " % haircut, and Gelos, Sahay and Sandleris 2011, the years shut out, as Tomz and Wright 2013 report them · Borensztein and Panizza 2009, the output cost"
                : c.Id == CountryId.Sweden || c.Id == CountryId.Poland
                    ? "Regulation (EC) No 332/2002, the facility · the Portugal Memorandum of Understanding of 2011, the three years and the 3 % end"
                    : "The Portugal Memorandum of Understanding of 2011, the three years and the 3 % end";
            return new Dictionary<string, SlipContent> { ["SOURCES"] = new SlipContent("SOURCES").Add(sources) };
        }

        private Dictionary<string, SlipContent> _crisisTerms = new Dictionary<string, SlipContent>();
        private string _crisisTermHover;
        private float _crisisTermSince;

        /// <summary>L2: the plate's slips, after the slip delay under the pointer (or the film's pinned one), drawn over the plate.</summary>
        private void DrawCrisisSlips(Rect bounds)
        {
            Event e = Event.current;
            if (e.type != EventType.Repaint) { return; }
            // K322 S5 (K321 (g)): the film records the plate's book - its slips and the level-2 terms they mark - so the words the plate moved are judged
            var book = new PeopleSlips.Book();
            foreach ((Rect _, SlipContent slip, string id) in _crisisSlips) { book.Anchors[id] = slip; }
            foreach (KeyValuePair<string, SlipContent> term in _crisisTerms) { book.Terms[term.Key] = term.Value; }
            PoliSim.Testing.DryGuiPass.NoteSlipBook(book);
            if (_crisisSlipForFilm != null)
            {
                foreach ((Rect r, SlipContent slip, string id) in _crisisSlips) { if (id == _crisisSlipForFilm) { SlipBox(slip, new Vector2(r.x + V35.Px(6f), r.center.y), bounds, 0, out _, out _, r, null); return; } }
                return;
            }
            if (PoliSim.Testing.CaptureIdentity.Armed) { return; }
            string hovered = null;
            SlipContent open = null;
            Rect anchor = default;
            foreach ((Rect r, SlipContent slip, string id) in _crisisSlips) { if (r.Contains(e.mousePosition)) { hovered = id; open = slip; anchor = r; break; } }
            float now = Time.realtimeSinceStartup;
            // a pointer on the open slip's level 2 keeps the chain open (K322 S5: SOURCES moved to level 2)
            if (hovered == null && _crisisSlipHover != null && _crisisOpenRect.Contains(e.mousePosition))
            {
                foreach ((Rect r, SlipContent slip, string id) in _crisisSlips) { if (id == _crisisSlipHover) { hovered = id; open = slip; anchor = r; break; } }
            }
            if (hovered != _crisisSlipHover) { _crisisSlipHover = hovered; _crisisSlipSince = now; _crisisTermHover = null; }
            if (hovered == null || now - _crisisSlipSince < SlipDelay) { _crisisOpenRect = default; return; }
            Rect box = SlipBox(open, _crisisOpenRect.width > 0f ? _crisisOpenAt : e.mousePosition, bounds, 0, out _, out List<(Rect Rect, string Term)> terms, anchor, null);
            if (_crisisOpenRect.width <= 0f) { _crisisOpenAt = e.mousePosition; }
            _crisisOpenRect = box;
            string termHovered = null;
            foreach ((Rect r, string t) in terms) { if (r.Contains(e.mousePosition)) { termHovered = t; break; } }
            if (termHovered != null && termHovered != _crisisTermHover) { _crisisTermHover = termHovered; _crisisTermSince = now; }
            if (_crisisTermHover != null && now - _crisisTermSince >= SlipDelay && _crisisTerms.TryGetValue(_crisisTermHover, out SlipContent termSlip))
            {
                Rect level2 = SlipBox(termSlip, _crisisOpenAt, bounds, 0, out _, out _, null, box);
                _crisisOpenRect = new Rect(Mathf.Min(box.x, level2.x), Mathf.Min(box.y, level2.y), Mathf.Max(box.xMax, level2.xMax) - Mathf.Min(box.x, level2.x), Mathf.Max(box.yMax, level2.yMax) - Mathf.Min(box.y, level2.y));
            }
        }

        private Rect _crisisOpenRect;
        private Vector2 _crisisOpenAt;

        /// <summary>The left column: the rating's ladder at the review (CCC filled; for the USA the standing rating, which keeps the dollar's discount,
        /// outlined where it differs), then the debt, the deficit and the market's third row.</summary>
        private void DrawPlateRating(Rect col, Country c, System.DateTime today)
        {
            float y = col.y;
            DrawV35SectionHead(new Rect(col.x, y, col.width, V35.Px(20f)), "the rating, at the review");
            y += V35.Px(20f) + V35.Px(10f);
            string standing = c.Rating != null && c.Rating.HasBeenReviewed ? LadderLetter(c.Rating.Rating) : null;
            float cg = V35.Px(3f), cw = Mathf.Min(V35.Px(40f), Mathf.Floor((col.width - cg * (Ladder.Length - 1)) / Ladder.Length)), ch = V35.Px(30f);   // L4: the seven cells inside the column
            for (int i = 0; i < Ladder.Length; i++)
            {
                var cell = new Rect(col.x + i * (cw + cg), y, cw, ch);
                bool ccc = Ladder[i] == "CCC";
                bool outlined = !ccc && standing == Ladder[i];
                Color ink = ccc ? PoliSimTheme.Card : outlined ? PoliSimTheme.TextPrimary : PlateLadderMuted;
                if (ccc && Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(cell, PoliSimTheme.Bad); }
                else { Outline(cell, outlined ? PoliSimTheme.TextPrimary : PlateRule, outlined ? 1.5f : 1f); }
                PlateText(cell, Ladder[i], V35Mono(V35.Floor, ink, bold: true, TextAnchor.MiddleCenter));
            }
            y += ch + V35.Px(10f);
            bool usa = c.Id == CountryId.USA;
            string note = usa ? (standing != null && standing != "CCC" ? "Standing " + standing + "; CCC without the dollar's discount" : "Read without the dollar's discount") : "CCC at this review";
            GUIStyle noteFace = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
            // K322 S5 (K320 (2): never cut with "…"): the note folds at its clause ("Standing AA"); the whole is the note's slip
            string noteAtRest = DeskFoldClauses(note, noteFace, col.width, out _);
            PlateText(new Rect(col.x, y, col.width, V35.Px(18f)), noteAtRest, noteFace);
            _crisisSlips.Add((new Rect(col.x, y, Mathf.Min(col.width, TextWidth(noteFace, noteAtRest) + 2f), V35.Px(18f)), new SlipContent("THE RATING · CCC").Add(note), "plate:note"));   // at every width, folded or not
            y += V35.Px(18f) + V35.Px(4f);
            float? deficit = SettledDeficit(c, today, out _) ?? c.Rating?.ReviewedDeficitPercent;
            var facts = new List<(string Icon, string Name, string Figure, Color Ink)>
            {
                ("debt", "Debt-to-GDP", UiFormat.Number(c.State.DebtToGdpRatio, 1) + " %", PoliSimTheme.TextPrimary),
                ("scales", "Deficit", deficit.HasValue ? Pct(deficit.Value) + " of GDP" : SymbolRegistry.Word(Symbol.Absent), PoliSimTheme.TextPrimary),
                usa ? ("coins", "The dollar's discount", "none", PoliSimTheme.Bad) : ("shield", "Market access", "closed", PoliSimTheme.Bad),
            };
            float rowH = V35.Px(34f);
            foreach ((string ic, string name, string figure, Color ink) in facts)
            {
                var row = new Rect(col.x, y, col.width, rowH);
                DrawPlateRow(row, ic, name, 16f, figure, 17f, ink);
                y += rowH;
            }
        }

        /// <summary>One ruled row: the icon, the name in the serif, the figure in the mono at the right.</summary>
        private void DrawPlateRow(Rect row, string icon, string name, float namePx, string figure, float figurePx, Color figureInk)
        {
            if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(new Rect(row.x, row.y, row.width, 1f), PlateRule); }
            float side = V35.Px(20f);
            DrawV35Icon(new Rect(row.x, row.y + Mathf.Round((row.height - side) * 0.5f), side, side), icon, PoliSimTheme.TextPrimary);
            GUIStyle fig = V35Mono(figurePx, figureInk, bold: true, TextAnchor.MiddleRight);
            float fw = TextWidth(fig, figure) + 2f;
            PlateText(new Rect(row.xMax - fw, row.y, fw, row.height), figure, fig);
            GUIStyle nameFace = V35Serif(namePx, PoliSimTheme.TextPrimary);
            float nx = row.x + side + V35.Px(10f), nw = Mathf.Max(1f, row.xMax - fw - V35.Px(10f) - nx);
            PlateText(new Rect(nx, row.y, nw, row.height), V35Fit(name, nameFace, nw, out _), nameFace);
        }

        /// <summary>The middle column: what follows a default, or the programme - one ruled row each, a figure at the right and a mono line under the name.</summary>
        private void DrawPlateWhatFollows(Rect col, Country c, SovereignCrisisState s, bool isDefault)
        {
            float y = col.y;
            DrawV35SectionHead(new Rect(col.x, y, col.width, V35.Px(20f)), isDefault ? "what follows" : "the programme");
            y += V35.Px(20f) + V35.Px(10f);
            // K322 S5 (K320 (2): a caption is one line, never cut, no sentence at rest): each row's line under its name is ONE line - folded at its clauses,
            // or a short form where the whole runs past one line (Rest) - and the words that leave it are the row's slip (Slip, else the name, figure and
            // line whole)
            var rows = new List<(string Icon, string Name, string Figure, Color Ink, string Sub, string Short, SlipContent Slip)>();
            if (isDefault)
            {
                float writtenOff = s.Haircuts != null && s.Haircuts.Count > 0 ? s.Haircuts[s.Haircuts.Count - 1].Billions : s.HaircutBillions;
                // the review, L2 and N4: the stock before and after, exact - the model writes off max(0, before) x the haircut fraction, so the state's own write-off gives back the stock it was taken from (today's stock has a day's flow on it)
                float before = writtenOff / SovereignCrisis.HaircutFraction, after = before - writtenOff;
                _crisisHaircutSlip = new SlipContent("HAIRCUT · " + UiFormat.Number(SovereignCrisis.HaircutFraction * 100f, 0) + " %")
                    .Add("Mean SZ-haircut · Cruces and Trebesch, via Tomz and Wright 2013")
                    .Add("Written off on " + CrisisDay(s.StartedOn) + " · a dated debt event")
                    .Add("The debt before, " + UiFormat.Money(before, MoneyUnit.Billions) + "; after, " + UiFormat.Money(after, MoneyUnit.Billions))
                    .Add("Not a surplus");
                rows.Add(("receipt", "Haircut", UiFormat.Number(SovereignCrisis.HaircutFraction * 100f, 0) + " %", PoliSimTheme.Bad,
                    UiFormat.Money(writtenOff, MoneyUnit.Billions) + " off the debt · " + UiFormat.Money(before, MoneyUnit.Billions) + " → " + UiFormat.Money(after, MoneyUnit.Billions), null, _crisisHaircutSlip));
                rows.Add(("chart", "Output cost", Pct(SovereignCrisis.OutputCostPercent) + " of GDP", PoliSimTheme.Bad,
                    "landed over a year · " + UiFormat.Money(s.OutputCostPerDay * SovereignCrisis.OutputCostDays, MoneyUnit.Billions), null, null));
                rows.Add(("gate", "Shut out of the market", UiFormat.Number(SovereignCrisis.ExclusionYears, 1) + " years", PoliSimTheme.Bad, "through " + CrisisDay(s.EndsOn), null, null));
                rows.Add(("scales", "Borrowing", "none", PoliSimTheme.Bad, "spending other than interest is cut to revenue less interest", "non-interest spending cut",
                    new SlipContent("BORROWING · NONE").Add("Spending other than interest is cut").Add("To revenue less interest")));
            }
            else
            {
                bool bop = c.Id == CountryId.Sweden || c.Id == CountryId.Poland;
                string facility = SovereignCrisis.FacilityOf(c.Id);
                rows.Add(("bank", "Lender", bop ? "EU + IMF" : "ESM", PoliSimTheme.TextPrimary, facility, bop ? "balance-of-payments facility" : null,
                    new SlipContent("LENDER · " + (bop ? "EU + IMF" : "ESM")).Add(char.ToUpperInvariant(facility[0]) + facility.Substring(1))));
                rows.Add(("cal", "Three years", "to " + CrisisDay(s.EndsOn), PoliSimTheme.TextPrimary, "reviewed each year on " + s.StartedOn.ToString("d MMM", CultureInfo.InvariantCulture), null, null));
                rows.Add(("coins", "Its loans", "no premium", PoliSimTheme.TextPrimary, "at the zone's rate, without the market's premium", null,
                    new SlipContent("ITS LOANS · NO PREMIUM").Add("At the zone's rate, without the market's premium")));
            }
            GUIStyle subFace = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
            float pad = V35.Px(8f), side = V35.Px(22f), textX = col.x + side + V35.Px(10f), textW = Mathf.Max(1f, col.xMax - textX), lineH = V35.Px(24f);
            foreach ((string ic, string name, string figure, Color ink, string subText, string rest, SlipContent rowSlip) in rows)
            {
                bool folded = rest != null;
                string subAtRest = rest ?? DeskFoldClauses(subText, subFace, textW, out folded);
                float subH = Mathf.Ceil(subFace.CalcSize(new GUIContent("Ag")).y);
                float rowH = pad + lineH + V35.Px(2f) + subH + pad;
                if (y + rowH > col.yMax + 1f) { break; }
                if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(new Rect(col.x, y, col.width, 1f), PlateRule); }
                DrawV35Icon(new Rect(col.x, y + pad + V35.Px(2f), side, side), ic, PoliSimTheme.TextPrimary);
                GUIStyle fig = V35Mono(19f, ink, bold: true, TextAnchor.MiddleRight);
                float fw = TextWidth(fig, figure) + 2f;
                PlateText(new Rect(col.xMax - fw, y + pad, fw, lineH), figure, fig);
                GUIStyle nameFace = V35Serif(17f, PoliSimTheme.TextPrimary);
                float nw = Mathf.Max(1f, col.xMax - fw - V35.Px(10f) - textX);
                PlateText(new Rect(textX, y + pad, nw, lineH), V35Fit(name, nameFace, nw, out _), nameFace);
                PlateText(new Rect(textX, y + pad + lineH + V35.Px(2f), textW, subH), subAtRest, subFace);
                SlipContent slip = rowSlip ?? (folded ? new SlipContent(name.ToUpperInvariant() + " · " + figure.ToUpperInvariant()).Add(char.ToUpperInvariant(subText[0]) + subText.Substring(1)) : null);
                if (slip != null) { _crisisSlips.Add((new Rect(col.x, y, col.width, rowH), slip, ic == "receipt" ? "plate:haircut" : "plate:" + ic)); }
                y += rowH;
            }
        }

        private SlipContent _crisisHaircutSlip;

        /// <summary>A default's right column: this year's budget at the last closed year's book - revenue against interest and the rest, the part of the rest
        /// over revenue less interest hatched as cut (`SovereignCrisis.CutToday`'s rule, a year at a time).</summary>
        private void DrawPlateBudget(Rect col, Country c)
        {
            float y = col.y;
            DrawV35SectionHead(new Rect(col.x, y, col.width, V35.Px(20f)), "this year's budget");
            // K322 S5 (K320 (2): no sentence at rest): the column's rule - the book it reads, what is paid and what is cut - is the head's slip
            var budgetSlip = new SlipContent("THIS YEAR'S BUDGET").Add("At the last closed year's book").Add("Interest is always paid").Add("The rest is cut to fit revenue");
            _crisisSlips.Add((new Rect(col.x, y, col.width, V35.Px(20f)), budgetSlip, "plate:budget"));
            y += V35.Px(20f) + V35.Px(10f);
            FiscalTurnReport year = _simulationManager.GetLastFiscalReport(c.Id);
            GUIStyle caption = V35Serif(15f, PoliSimTheme.TextMuted);
            if (year == null)
            {
                PlateText(new Rect(col.x, y, col.width, V35.Px(20f)), "No year closed yet", caption);
                budgetSlip.Add("The cut is drawn once the first year's book is");
                return;
            }
            float revenue = Mathf.Max(0f, year.Revenue), interest = Mathf.Max(0f, year.InterestOnDebt), rest = Mathf.Max(0f, year.TotalSpending - year.InterestOnDebt);
            float cut = Mathf.Clamp(rest - Mathf.Max(0f, revenue - interest), 0f, rest);
            GUIStyle big = V35Mono(30f, PoliSimTheme.Bad, bold: true);
            string cutText = UiFormat.Money(cut, MoneyUnit.Billions);
            float bigW = TextWidth(big, cutText) + 2f;
            PlateText(new Rect(col.x, y, bigW, V35.Px(36f)), cutText, big);
            PlateText(new Rect(col.x + bigW + V35.Px(10f), y + V35.Px(6f), Mathf.Max(1f, col.width - bigW - V35.Px(10f)), V35.Px(30f)), "a year, cut", V35Serif(16f, PoliSimTheme.TextMuted));
            y += V35.Px(36f) + V35.Px(8f);

            GUIStyle mono = V35Mono(V35.Floor, PoliSimTheme.TextPrimary);
            GUIStyle inBar = V35Mono(V35.Floor, PlatePaper, bold: true);
            float labelW = V35.Px(92f), barX = col.x + labelW + V35.Px(10f), barW = Mathf.Max(1f, col.xMax - barX), barH = V35.Px(20f);
            float scale = barW / Mathf.Max(0.0001f, Mathf.Max(revenue, interest + rest));
            PlateText(new Rect(col.x, y, labelW, barH), "revenue", mono);
            if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(new Rect(barX, y, revenue * scale, barH), PoliSimTheme.Neutral); }
            // the films of e15a0eb4: a figure goes inside its bar only where it fits at the floor; else the caption under the bars carries it
            string revenueText = UiFormat.Money(revenue, MoneyUnit.Billions), interestText = UiFormat.Money(interest, MoneyUnit.Billions);
            bool revenueIn = FitsIn(inBar, revenueText, revenue * scale - V35.Px(6f)), interestIn = FitsIn(inBar, interestText, interest * scale - V35.Px(6f));
            if (revenueIn) { PlateText(new Rect(barX + V35.Px(6f), y, revenue * scale - V35.Px(6f), barH), revenueText, inBar); }
            y += barH + V35.Px(6f);
            PlateText(new Rect(col.x, y, labelW, barH), "spending", mono);
            float kept = rest - cut;
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(barX, y, interest * scale, barH), PoliSimTheme.TextPrimary);
                PoliSimTheme.Rule(new Rect(barX + interest * scale, y, kept * scale, barH), PoliSimTheme.Neutral);
            }
            DrawHatch(new Rect(barX + (interest + kept) * scale, y, cut * scale, barH), PoliSimTheme.Bad, PlatePaper);
            if (interestIn) { PlateText(new Rect(barX + V35.Px(6f), y, interest * scale - V35.Px(6f), barH), interestText, inBar); }
            if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(new Rect(barX + revenue * scale - 1f, y - V35.Px(5f), 2f, barH + V35.Px(10f)), PoliSimTheme.TextPrimary); }
            y += barH + V35.Px(10f);

            float sw = V35.Px(12f), lx = col.x;
            foreach ((string words, int kind) in new[] { ("interest, paid", 0), ("the rest", 1), ("cut", 2) })
            {
                var swatch = new Rect(lx, y + Mathf.Round((V35.Px(18f) - sw) * 0.5f), sw, sw);
                if (kind == 2) { DrawHatch(swatch, PoliSimTheme.Bad, PlatePaper); }
                else if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(swatch, kind == 0 ? PoliSimTheme.TextPrimary : PoliSimTheme.Neutral); }
                float ww = TextWidth(mono, words) + 2f;
                PlateText(new Rect(lx + sw + V35.Px(6f), y, ww, V35.Px(18f)), words, mono);
                lx += sw + V35.Px(6f) + ww + V35.Px(14f);
            }
            y += V35.Px(18f) + V35.Px(8f);
            // K322 S5: a figure its bar could not hold, a line each under the key (never a sentence); the column's rule is the head's slip (above)
            float lineH = Mathf.Ceil(caption.CalcSize(new GUIContent("Ag")).y);
            foreach ((bool inside, string words) in new[] { (revenueIn, "Revenue " + revenueText), (interestIn, "Interest " + interestText) })
            {
                if (inside) { continue; }
                PlateText(new Rect(col.x, y, col.width, lineH), words, caption);
                y += lineH + V35.Px(2f);
            }
        }

        /// <summary>A programme's right column: the targets' path (the entry deficit, then the three years to the Treaty's reference, dashed) and what met and
        /// missed do, with the first review's day.</summary>
        private void DrawPlateTargets(Rect col, SovereignCrisisState s)
        {
            float y = col.y;
            DrawV35SectionHead(new Rect(col.x, y, col.width, V35.Px(20f)), "the targets, % of GDP");
            y += V35.Px(20f) + V35.Px(10f);
            var chart = new Rect(col.x, y, Mathf.Min(col.width - V35.Px(30f), V35.Px(300f)), V35.Px(170f));
            DrawTargetChart(chart, s, 1, false, null, PoliSimTheme.Caution);
            y += chart.height + V35.Px(10f);
            float rowH = V35.Px(32f);
            DrawPlateRow(new Rect(col.x, y, col.width, rowH), "check", "Met", 16f, "the next loan paid", 15f, PoliSimTheme.Good);
            y += rowH;
            DrawPlateRow(new Rect(col.x, y, col.width, rowH), "cross", "Missed", 16f, "next loan withheld", 15f, PoliSimTheme.Bad);
            y += rowH;
            DrawPlateRow(new Rect(col.x, y, col.width, rowH), "cal", "First review", 16f, CrisisDay(s.NextReviewOn), 15f, PoliSimTheme.TextPrimary);
        }

        /// <summary>
        /// The targets' step chart (boards 32b and 32d): the entry deficit in the neutral ink, then each programme year's target (`SovereignCrisis.TargetFor`);
        /// <paramref name="current"/>'s column outlined; a missed year's column hatched BAD where <paramref name="withheld"/>; the Treaty's reference dashed;
        /// <paramref name="running"/>, where given, drawn across the current column in <paramref name="runInk"/>.
        /// </summary>
        private void DrawTargetChart(Rect area, SovereignCrisisState s, int current, bool withheld, float? running, Color runInk)
        {
            float reference = AiFinanceMinistry.EuDeficitReferencePercent;
            var vals = new float[SovereignCrisis.ProgrammeYears + 1];
            vals[0] = s.EntryDeficitPercent;
            for (int i = 1; i <= SovereignCrisis.ProgrammeYears; i++) { vals[i] = SovereignCrisis.TargetFor(s.EntryDeficitPercent, i); }
            float maxV = Mathf.Max(reference, s.EntryDeficitPercent);
            if (running.HasValue) { maxV = Mathf.Max(maxV, running.Value); }
            maxV *= 1.15f;
            float baseY = area.yMax - V35.Px(26f), topY = area.y + V35.Px(20f);
            float unit = (baseY - topY) / Mathf.Max(0.0001f, maxV);
            // the review of wave/dcrisis, L4: every label inside the area - the right end keeps room for the running figure and the reference's label, and the
            // columns share what is left (the board's 56 px bars on a 74 px pitch where it is wide enough)
            GUIStyle refFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true);
            GUIStyle runFace = V35Mono(V35.Floor, runInk, bold: true);
            float room = TextWidth(refFace, "00.0 %") + V35.Px(6f);
            float pitch = Mathf.Min(V35.Px(74f), Mathf.Max(1f, area.width - room - V35.Px(8f)) / vals.Length), barW = Mathf.Round(pitch * 56f / 74f);
            GUIStyle value = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, TextAnchor.MiddleCenter);
            GUIStyle label = V35Mono(V35.Floor, PoliSimTheme.TextMuted, false, TextAnchor.MiddleCenter);
            float slack = Mathf.Max(0f, (pitch - barW) * 0.5f);
            for (int i = 0; i < vals.Length; i++)
            {
                float x = area.x + V35.Px(8f) + i * pitch;
                float v = Mathf.Max(0f, vals[i]);
                var bar = new Rect(x, baseY - v * unit, barW, v * unit);
                if (withheld && i == current - 1) { DrawHatch(bar, PoliSimTheme.Bad, V35.CardPaper); }
                else if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(bar, i == 0 ? PoliSimTheme.Neutral : PlateTrack); }
                if (i == current) { Outline(bar, PoliSimTheme.TextPrimary, 1.5f); }
                PlateText(new Rect(x - slack, bar.y - V35.Px(18f), barW + slack * 2f, V35.Px(16f)), UiFormat.Number(vals[i], 1), value);
                PlateText(new Rect(x - slack, baseY + V35.Px(6f), barW + slack * 2f, V35.Px(18f)), i == 0 ? "entry" : "year " + i.ToString(CultureInfo.InvariantCulture), label);
            }
            float refY = Mathf.Round(baseY - reference * unit);
            if (Event.current.type == EventType.Repaint)
            {
                float end = area.xMax;
                for (float x = area.x; x < end; x += V35.Px(6f)) { PoliSimTheme.Rule(new Rect(x, refY, Mathf.Min(V35.Px(3f), end - x), 1.5f), PoliSimTheme.TextPrimary); }
            }
            float ry = running.HasValue ? Mathf.Round(baseY - Mathf.Max(0f, running.Value) * unit) : float.NaN;
            // the review 2, N6: the two labels are placed as rects and kept apart - the reference's above its line, else below it; where the running figure
            // still meets it, the running figure moves to the left of its column
            string refText = UiFormat.Number(reference, 0) + " %";
            float refW = TextWidth(refFace, refText) + 2f;
            var refRect = new Rect(area.xMax - refW, refY - V35.Px(18f), refW, V35.Px(16f));
            bool hasRun = running.HasValue && current >= 1 && current < vals.Length;
            float runX = area.x + V35.Px(8f) + current * pitch;
            string runText = hasRun ? UiFormat.Number(running.Value, 1) : string.Empty;
            float rw = TextWidth(runFace, runText) + 2f;
            var runRect = new Rect(Mathf.Min(runX + barW + V35.Px(6f), area.xMax - rw), ry - V35.Px(9f), rw, V35.Px(18f));
            if (hasRun && runRect.Overlaps(refRect)) { refRect.y = refY + V35.Px(3f); }
            if (hasRun && runRect.Overlaps(refRect)) { runRect.x = Mathf.Max(area.x, runX - Mathf.Min(V35.Px(6f), slack) - rw - V35.Px(4f)); }
            PlateText(refRect, refText, refFace);
            if (hasRun)
            {
                float over = Mathf.Min(V35.Px(6f), slack);
                if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(new Rect(runX - over, ry - 1f, barW + over * 2f, 3f), runInk); }
                PlateText(runRect, runText, runFace);
            }
        }

        // ==============================================================================================================================================
        // The Desk's first card (boards 32c, 32d)
        // ==============================================================================================================================================

        /// <summary>The crisis's card in World trade's place: a default's exclusion and output cost, or a programme's targets - and its slip, the crisis's
        /// sentences (`FiscalRules.CrisisLines`).</summary>
        private void DrawDeskCrisisCard(Rect r)
        {
            Country c = _playerCountry;
            SovereignCrisisState s = c.Crisis;
            System.DateTime today = _simulationManager.CurrentDate;
            bool isDefault = s.Phase == CrisisPhase.Default;
            bool withheld = !isDefault && SovereignCrisis.Unfinanced(c, today);
            Rect inner = DrawV35Card(r);

            // the head: the icon, the branch, since when / with whom, the CCC stamp
            float headH = V35.Px(30f), icon = V35.Px(24f);
            var head = new Rect(inner.x, inner.y, inner.width, headH);
            DrawV35Icon(new Rect(head.x, head.y + Mathf.Round((headH - icon) * 0.5f), icon, icon), "debt", isDefault || withheld ? PoliSimTheme.Bad : PoliSimTheme.Caution);
            string name = isDefault ? "Default" : "Programme";
            GUIStyle nameFace = V35Serif(20f, PoliSimTheme.TextPrimary);
            float nx = head.x + icon + V35.Px(10f), nw = TextWidth(nameFace, name) + 2f;
            PlateText(new Rect(nx, head.y, nw, headH), name, nameFace);
            Rect stamp = DrawCrisisStamp(head.xMax, head.y + headH * 0.5f, "CCC", PoliSimTheme.Bad, V35.Floor, V35.Px(20f));
            string facility = SovereignCrisis.FacilityOf(c.Id);
            string subText = isDefault ? "since " + CrisisDay(s.StartedOn) : char.ToUpperInvariant(facility[0]) + facility.Substring(1);
            GUIStyle subFace = V35Serif(15f, PoliSimTheme.TextMuted);
            float sx = nx + nw + V35.Px(10f), sw = Mathf.Max(1f, stamp.x - V35.Px(10f) - sx);
            PlateText(new Rect(sx, head.y, sw, headH), V35Fit(subText, subFace, sw, out _), subFace);
            DeskAnchor(head, "card:crisis");
            var slip = new SlipContent(isDefault ? "DEFAULT" : "PROGRAMME");
            foreach (string line in FiscalRules.CrisisLines(c, today)) { slip.Add(line); }
            slip.Add("While it lasts this card takes the place of World trade, which stays on Stats, International");
            _deskSlipBook.Anchors["card:crisis"] = slip;

            var body = new Rect(inner.x, head.yMax + V35.Px(10f), inner.width, Mathf.Max(1f, inner.yMax - head.yMax - V35.Px(10f)));
            if (isDefault) { DrawDeskDefaultBody(body, c, s, today); }
            else { DrawDeskProgrammeBody(body, c, s, today, withheld); }
        }

        /// <summary>32c: the exclusion as a timeline with today's tick and the time left; the output cost landing; the cut so far; the haircut, dated.</summary>
        private void DrawDeskDefaultBody(Rect body, Country c, SovereignCrisisState s, System.DateTime today)
        {
            float y = body.y;
            GUIStyle mono = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
            GUIStyle monoInk = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true);

            // the exclusion
            DrawV35SectionHead(new Rect(body.x, y, body.width * 0.6f, V35.Px(26f)), "shut out of the market");
            GUIStyle left = V35Mono(22f, PoliSimTheme.TextPrimary, bold: true, TextAnchor.LowerRight);
            PlateText(new Rect(body.x, y, body.width, V35.Px(26f)), TopStrip.TimeLeft(today, s.EndsOn), left);
            y += V35.Px(26f) + V35.Px(6f);
            float span = Mathf.Max(1f, (float)(s.EndsOn.Date - s.StartedOn.Date).TotalDays);
            float frac = Mathf.Clamp01((float)(today.Date - s.StartedOn.Date).TotalDays / span);
            var track = new Rect(body.x, y, body.width, V35.Px(14f));
            float tick = Mathf.Round(track.x + frac * track.width);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(track, PlateTrack);
                PoliSimTheme.Rule(new Rect(track.x, track.y, tick - track.x, track.height), PoliSimTheme.Bad);
                PoliSimTheme.Rule(new Rect(tick - 1f, track.y - V35.Px(5f), 2f, track.height + V35.Px(10f)), PoliSimTheme.TextPrimary);
            }
            y += track.height + V35.Px(6f);
            string from = CrisisDay(s.StartedOn), to = CrisisDay(s.EndsOn), now = "today, " + CrisisDay(today);
            float fw = TextWidth(mono, from) + 2f, tw = TextWidth(mono, to) + 2f, nwid = TextWidth(monoInk, now) + 2f;
            PlateText(new Rect(body.x, y, fw, V35.Px(18f)), from, mono);
            PlateText(new Rect(body.xMax - tw, y, tw, V35.Px(18f)), to, mono);
            float nx = Mathf.Clamp(tick - nwid * 0.5f, body.x + fw + V35.Px(8f), body.xMax - tw - V35.Px(8f) - nwid);
            if (nx + nwid <= body.xMax - tw - V35.Px(4f) && nx >= body.x + fw + V35.Px(4f)) { PlateText(new Rect(nx, y, nwid, V35.Px(18f)), now, monoInk); }
            y += V35.Px(18f) + V35.Px(12f);

            // the output cost
            GUIStyle headFace = V35Serif(V35.Floor, PoliSimTheme.TextMuted, TextAnchor.LowerLeft);
            string costHead = "THE OUTPUT COST";
            float chw = TextWidth(headFace, costHead) + 2f;
            DrawV35SectionHead(new Rect(body.x, y, chw, V35.Px(22f)), costHead);
            PlateText(new Rect(body.x + chw + V35.Px(8f), y, Mathf.Max(1f, body.width * 0.6f - chw), V35.Px(22f)), Pct(SovereignCrisis.OutputCostPercent) + " of GDP over a year", V35Serif(15f, PoliSimTheme.TextMuted, TextAnchor.LowerLeft));
            string daysLeft = s.OutputCostDaysLeft > 0 ? s.OutputCostDaysLeft.ToString(CultureInfo.InvariantCulture) + (s.OutputCostDaysLeft == 1 ? " day left" : " days left") : "landed";
            PlateText(new Rect(body.x, y, body.width, V35.Px(22f)), daysLeft, V35Mono(16f, PoliSimTheme.TextPrimary, bold: true, TextAnchor.LowerRight));
            y += V35.Px(22f) + V35.Px(6f);
            float total = s.OutputCostPerDay * SovereignCrisis.OutputCostDays;
            float landed = s.OutputCostPerDay * Mathf.Max(0, SovereignCrisis.OutputCostDays - s.OutputCostDaysLeft);
            var costBar = new Rect(body.x, y, body.width, V35.Px(20f));
            float fill = total > 0f ? Mathf.Clamp01(landed / total) * costBar.width : 0f;
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(costBar, PlateTrack);
                PoliSimTheme.Rule(new Rect(costBar.x, costBar.y, fill, costBar.height), PoliSimTheme.Bad);
            }
            string landedText = UiFormat.Money(landed, MoneyUnit.Billions) + " landed", ofText = "of " + UiFormat.Money(total, MoneyUnit.Billions);
            GUIStyle inBar = V35Mono(V35.Floor, PlatePaper, bold: true);
            if (TextWidth(inBar, landedText) + V35.Px(12f) <= fill) { PlateText(new Rect(costBar.x + V35.Px(6f), costBar.y, fill - V35.Px(6f), costBar.height), landedText, inBar); }
            GUIStyle ofFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleRight);
            PlateText(new Rect(costBar.x, costBar.y, costBar.width - V35.Px(6f), costBar.height), ofText, ofFace);
            y += costBar.height + V35.Px(10f);

            // the cut so far, and the haircut, dated
            float rowH = V35.Px(30f);
            DrawPlateRow(new Rect(body.x, y, body.width, rowH), "scales", "Spending cut to revenue less interest", 16f, UiFormat.Money(s.CutBillions, MoneyUnit.Billions) + CutSpan(s), 16f, PoliSimTheme.Bad);
            y += rowH;
            if (s.Haircuts != null && s.Haircuts.Count > 0)
            {
                DatedAmount h = s.Haircuts[s.Haircuts.Count - 1];
                string haircutName = "Haircut, " + UiFormat.Number(SovereignCrisis.HaircutFraction * 100f, 0) + " %, on " + CrisisDay(h.Date), haircutFigure = UiFormat.Money(h.Billions, MoneyUnit.Billions) + " off";
                if (y + rowH <= body.yMax + 1f) { DrawPlateRow(new Rect(body.x, y, body.width, rowH), "receipt", haircutName, 16f, haircutFigure, 16f, PoliSimTheme.TextPrimary); }
                else
                {
                    // the review 2, N3: a row the card has no room for goes into the card's slip and is said, counted - never dropped quietly
                    if (_deskSlipBook.Anchors.TryGetValue("card:crisis", out SlipContent cardSlip)) { cardSlip.Add(haircutName + ": " + haircutFigure); }
                    if (Event.current.type == EventType.Repaint)
                    {
                        string key = Mathf.RoundToInt(body.height).ToString(CultureInfo.InvariantCulture);
                        if (key != _crisisRowFoldReported)
                        {
                            _crisisRowFoldReported = key;
                            _crisisRowsFolded++;
                            Debug.LogWarning(string.Format(CultureInfo.InvariantCulture, "CRISIS CARD: the dated haircut row does not fit the card's {0:0} px body - folded into the card's slip ({1} fold(s) so far).", body.height, _crisisRowsFolded));
                        }
                    }
                }
            }
        }

        private string _crisisRowFoldReported;
        private int _crisisRowsFolded;

        /// <summary>32d: the target path with the running deficit across this year's column, the figure against the target, the review's day and what met and
        /// missed do - or, in a withheld year, the missed year hatched, withheld through, the cut so far and the next review. DECLARED: the running deficit
        /// is the reading the review itself takes (`CreditRatingSystem.GetSettledPosition`: the trailing year to the last closed quarter), dated to that
        /// quarter on the card - the model keeps no programme-year-to-date deficit.</summary>
        private void DrawDeskProgrammeBody(Rect body, Country c, SovereignCrisisState s, System.DateTime today, bool withheld)
        {
            int year = Mathf.Clamp(s.ProgrammeYear, 1, SovereignCrisis.ProgrammeYears);
            float target = SovereignCrisis.TargetFor(s.EntryDeficitPercent, year);
            float? running = SettledDeficit(c, today, out System.DateTime asOf);
            Color runInk = withheld ? PoliSimTheme.Bad : PoliSimTheme.Caution;

            float chartW = Mathf.Min(V35.Px(300f), body.width * 0.48f);
            DrawV35SectionHead(new Rect(body.x, body.y, chartW, V35.Px(20f)), "deficit target, % of GDP");
            var chart = new Rect(body.x, body.y + V35.Px(24f), chartW, Mathf.Min(V35.Px(170f), body.height - V35.Px(24f)));
            DrawTargetChart(chart, s, year, withheld, running, runInk);

            float rx = body.x + chartW + V35.Px(26f), rw = Mathf.Max(1f, body.xMax - rx);
            float y = body.y;
            GUIStyle big = V35Mono(30f, runInk, bold: true);
            string figure = running.HasValue ? UiFormat.Number(running.Value, 1) + " %" : SymbolRegistry.Word(Symbol.Absent);
            PlateText(new Rect(rx, y, rw, V35.Px(36f)), figure, big);
            y += V35.Px(36f);
            GUIStyle sub = V35Serif(15f, PoliSimTheme.TextMuted);
            string subText = running.HasValue ? "the year to " + CrisisDay(asOf) + " · target " + UiFormat.Number(target, 1)
                                              : "no year has closed yet · target " + UiFormat.Number(target, 1);
            PlateText(new Rect(rx, y, rw, V35.Px(20f)), V35Fit(subText, sub, rw, out _), sub);
            y += V35.Px(22f);
            string verdict;
            Color verdictInk;
            if (withheld) { verdict = "Year " + (year - 1).ToString(CultureInfo.InvariantCulture) + " missed · loan withheld"; verdictInk = PoliSimTheme.Bad; }
            else if (!running.HasValue) { verdict = "Read at the review"; verdictInk = PoliSimTheme.TextMuted; }
            else if (running.Value > target) { verdict = "▲ " + UiFormat.Number(running.Value - target, 1) + " over, the year to " + asOf.ToString("d MMM", CultureInfo.InvariantCulture); verdictInk = PoliSimTheme.Caution; }
            else { verdict = "▼ " + UiFormat.Number(target - running.Value, 1) + " under, the year to " + asOf.ToString("d MMM", CultureInfo.InvariantCulture); verdictInk = PoliSimTheme.Good; }
            PlateText(new Rect(rx, y, rw, V35.Px(20f)), verdict, V35Mono(15f, verdictInk, bold: true));
            y += V35.Px(20f) + V35.Px(8f);

            float rowH = V35.Px(32f);
            var rows = new List<(string Icon, string Name, string Figure, Color Ink)>();
            if (withheld)
            {
                rows.Add(("cross", "Withheld through", CrisisDay(s.WithheldThrough), PoliSimTheme.Bad));
                rows.Add(("scales", s.Crises > 1 ? "Spending cut, the run's crises" : "Spending cut so far", UiFormat.Money(s.CutBillions, MoneyUnit.Billions), PoliSimTheme.Bad));
                rows.Add(("cal", "Next review", CrisisDay(s.NextReviewOn), PoliSimTheme.TextPrimary));
            }
            else
            {
                int days = Mathf.Max(0, (s.NextReviewOn.Date - today.Date).Days);
                rows.Add(("cal", "Review", CrisisDay(s.NextReviewOn) + " · " + days.ToString(CultureInfo.InvariantCulture) + " d", PoliSimTheme.TextPrimary));
                rows.Add(("check", "Met", "the next loan paid", PoliSimTheme.Good));
                rows.Add(("cross", "Missed", "next loan withheld", PoliSimTheme.Bad));
            }
            foreach ((string ic, string name, string fig, Color ink) in rows)
            {
                if (y + rowH > body.yMax + 1f) { break; }
                DrawPlateRow(new Rect(rx, y, rw, rowH), ic, name, 16f, fig, 15f, ink);
                y += rowH;
            }
        }

        // ==============================================================================================================================================
        // The Budget (board 32e)
        // ==============================================================================================================================================

        /// <summary>A programme year's verdict on a deficit, per cent of GDP: "met" in the good ink, or how far over in the bad ink; false outside a programme.</summary>
        private bool ProgrammeVerdict(float deficitPercent, out string words, out Color ink, out float target)
        {
            words = null; ink = PoliSimTheme.TextPrimary; target = 0f;
            SovereignCrisisState s = _playerCountry?.Crisis;
            if (s == null || s.Phase != CrisisPhase.Programme) { return false; }
            target = SovereignCrisis.TargetFor(s.EntryDeficitPercent, Mathf.Clamp(s.ProgrammeYear, 1, SovereignCrisis.ProgrammeYears));
            bool met = deficitPercent <= target;
            words = met ? "met" : UiFormat.Number(deficitPercent - target, 1) + " over";
            ink = met ? PoliSimTheme.Good : PoliSimTheme.Bad;
            return true;
        }

        /// <summary>
        /// 32e: the programme year's target on the Budget's balance strip, where the deficit is drafted - the draft's deficit in amber against the target's
        /// tick (with no draft, the reading the review takes, in the ink of record), ✓ met or ✗ over, and the review's day. Nothing outside a programme.
        /// </summary>
        private void DrawBudgetProgrammeTarget(float width, float? draftBalance)
        {
            SovereignCrisisState s = _playerCountry?.Crisis;
            if (s == null || s.Phase != CrisisPhase.Programme) { return; }
            System.DateTime today = _simulationManager.CurrentDate;
            float nominalGdp = Mathf.Max(0.0001f, _playerCountry.State.NominalGdp);
            float? deficit = draftBalance.HasValue ? -draftBalance.Value / nominalGdp * 100f : SettledDeficit(_playerCountry, today, out _);
            int year = Mathf.Clamp(s.ProgrammeYear, 1, SovereignCrisis.ProgrammeYears);
            float target = SovereignCrisis.TargetFor(s.EntryDeficitPercent, year);
            float h = V35.Px(62f);   // the bar and its scale line under it (14 + 2 + a 14 px line) inside the card's 12 px padding
            Rect row = GUILayoutUtility.GetRect(width, h, GUILayout.Width(width), GUILayout.Height(h));
            Rect inner = DrawV35Card(row);
            float x = inner.x, mid = inner.y + inner.height * 0.5f;
            float side = V35.Px(22f);
            DrawV35Icon(new Rect(x, Mathf.Round(mid - side * 0.5f), side, side), "debt", PoliSimTheme.Caution);
            x += side + V35.Px(12f);
            GUIStyle label = V35Serif(16f, PoliSimTheme.TextPrimary);
            string labelText = "Programme target, year " + year.ToString(CultureInfo.InvariantCulture);
            float lw = TextWidth(label, labelText) + 2f;
            PlateText(new Rect(x, inner.y, lw, inner.height), labelText, label);
            x += lw + V35.Px(14f);

            // the right end first: the review's day, then the verdict and the figure
            GUIStyle review = V35Mono(V35.Floor, PoliSimTheme.TextMuted, false, TextAnchor.MiddleRight);
            string reviewText = "review " + CrisisDay(s.NextReviewOn);
            float rw = TextWidth(review, reviewText) + 2f;
            PlateText(new Rect(inner.xMax - rw, inner.y, rw, inner.height), reviewText, review);
            bool drafted = draftBalance.HasValue;
            GUIStyle figFace = V35Mono(18f, drafted ? PoliSimTheme.Caution : PoliSimTheme.TextPrimary, bold: true);
            string figText = deficit.HasValue ? UiFormat.Number(deficit.Value, 1) + " %" : SymbolRegistry.Word(Symbol.Absent);
            float figW = TextWidth(figFace, figText) + 2f;
            string verdictText = null;
            Color verdictInk = PoliSimTheme.TextPrimary;
            bool met = false;
            if (deficit.HasValue && ProgrammeVerdict(deficit.Value, out verdictText, out verdictInk, out _)) { met = deficit.Value <= target; }
            GUIStyle verdictFace = V35Mono(15f, verdictInk, bold: true);
            float glyph = V35.Px(16f), vw = verdictText != null ? glyph + V35.Px(6f) + TextWidth(verdictFace, verdictText) + 2f : 0f;
            float barW = Mathf.Max(V35.Px(60f), Mathf.Min(V35.Px(520f), inner.xMax - rw - V35.Px(24f) - vw - V35.Px(14f) - figW - V35.Px(14f) - x));
            var bar = new Rect(x, inner.y + V35.Px(2f), barW, V35.Px(14f));
            float scaleMax = Mathf.Max(10f, Mathf.Ceil(Mathf.Max(target, s.EntryDeficitPercent, deficit ?? 0f)) + 1f);
            float tickX = Mathf.Round(bar.x + Mathf.Clamp01(target / scaleMax) * bar.width);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(bar, PlateTrack);
                if (deficit.HasValue) { PoliSimTheme.Rule(new Rect(bar.x, bar.y, Mathf.Clamp01(Mathf.Max(0f, deficit.Value) / scaleMax) * bar.width, bar.height), drafted ? PoliSimTheme.Caution : PoliSimTheme.Neutral); }
                PoliSimTheme.Rule(new Rect(tickX - 1f, bar.y - V35.Px(4f), 3f, bar.height + V35.Px(8f)), PoliSimTheme.TextPrimary);
            }
            GUIStyle scaleFace = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
            GUIStyle tickFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, TextAnchor.UpperCenter);
            float labelsY = bar.yMax + V35.Px(2f), labelsH = Mathf.Max(1f, inner.yMax - labelsY + V35.Px(4f));
            PlateText(new Rect(bar.x, labelsY, V35.Px(20f), labelsH), "0", scaleFace);
            string scaleText = UiFormat.Number(scaleMax, 0) + " % of GDP";
            float stw = TextWidth(scaleFace, scaleText) + 2f;
            PlateText(new Rect(bar.xMax - stw, labelsY, stw, labelsH), scaleText, scaleFace);
            string targetText = UiFormat.Number(target, 1);
            float ttw = TextWidth(tickFace, targetText) + 2f;
            if (tickX - ttw * 0.5f > bar.x + V35.Px(20f) && tickX + ttw * 0.5f < bar.xMax - stw - V35.Px(4f)) { PlateText(new Rect(tickX - ttw * 0.5f, labelsY, ttw, labelsH), targetText, tickFace); }
            x = bar.xMax + V35.Px(14f);
            PlateText(new Rect(x, inner.y, figW, inner.height), figText, figFace);
            x += figW + V35.Px(14f);
            if (verdictText != null)
            {
                if (Event.current.type == EventType.Repaint) { DrawStateGlyph(new Rect(x, Mathf.Round(mid - glyph * 0.5f), glyph, glyph), met ? Symbol.Good : Symbol.Bad, verdictInk); }
                PlateText(new Rect(x + glyph + V35.Px(6f), inner.y, vw - glyph - V35.Px(6f), inner.height), verdictText, verdictFace);
            }
            SlipAnchor(row, "budget:programme");
            var slip = new SlipContent("PROGRAMME TARGET · YEAR " + year.ToString(CultureInfo.InvariantCulture))
                .Add(UiFormat.Number(target, 1) + " % of GDP, reviewed on " + CrisisDay(s.NextReviewOn));
            slip.Add(drafted ? "Your draft: " + figText + " if passed" : "The reading the review takes, the trailing year to the last closed quarter: " + figText);
            slip.Add("A miss withholds the next year's loan");
            _budgetSlipBook.Anchors["budget:programme"] = slip;
            GUILayout.Space(V35.Px(V35.Gutter));
        }
    }
}
