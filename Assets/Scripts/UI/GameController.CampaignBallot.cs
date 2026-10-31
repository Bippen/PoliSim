using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// S7 (Design's return of 2026-10-30, section 7 - `S7 USA Independent.dc.html` 7a and 7c; SP-6b): <b>THE INDEPENDENT'S BALLOT ACCESS ON THE HQ</b> - the
    /// 51 as the map in the run-up (each jurisdiction on the ballot, collecting, off, its deadline passed, or BILLED), what they add up to and the next
    /// deadlines in the left column, the target's card under the map, the per-state slip, and the BALLOT ACCESS verbs (GATHER, FILE, § RULES). Every figure
    /// is the drive's (<see cref="UsBallotAccess.Drive"/>) on the sourced rules (`UsBallotAccess2024`, NASS's July 2024 summary); a rule not on disk prints
    /// BILLED, never a figure.
    ///
    /// <para><b>Where the drive comes from.</b> SP-6b's independent run is not built (no start offers it; the run-up holds no drive of the player's): the HQ
    /// shows a drive only where one is handed to it (<see cref="StageUsBallotForFilm"/>, the film's staging, put back after each frame). The day the run
    /// exists, its drive is the one this draws.</para>
    ///
    /// <para><b>The independent's ink</b> is the one picked at creation from §5's ring (Design's For Code 3). The creation flow is offered in Sweden only on
    /// this base and picks no ink for a US independent, so the default is DECLARED: ring step <see cref="IndependentInkStep"/> - Design's own teal #387566,
    /// a ring cell - held clear of the seated US inks by the ring's fence (`PartyCreationFlow.InkClear`).</para>
    ///
    /// <para><b>K320 on the board</b> (K320 (8)): the tile prints its code over its electors; at rest every label is at most three words and every caption one
    /// line; the slip is the state's word and four fragments, its level 2 one sentence. What moved is listed in `docs/declutter/moved_s7us.tsv`.</para>
    /// </summary>
    public partial class GameController
    {
        /// <summary>DECLARED (see the summary): the independent's ink, the creation ring's step - Design's #387566.</summary>
        public const int IndependentInkStep = 11;

        /// <summary>The independent's ink: the ring's DECLARED step.</summary>
        public static Color IndependentInk() => PoliSimTheme.CreatedInkCandidate(IndependentInkStep);

        /// <summary>The ballot drive the HQ shows (the film's staging), and the jurisdiction the BALLOT ACCESS verbs act on.</summary>
        private UsBallotAccess.Drive _usBallotDrive;
        private string _usBallotTarget;

        /// <summary>The film's hook: a drive (null puts it back) and its target jurisdiction.</summary>
        internal void StageUsBallotForFilm(UsBallotAccess.Drive drive, string target)
        {
            _usBallotDrive = drive;
            _usBallotTarget = target;
        }

        /// <summary>The drive the HQ draws: the USA's run-up with a drive handed to it, else none.</summary>
        private UsBallotAccess.Drive HqBallotDrive(CampaignSnapshot s) => PlayerCountryId == CountryId.USA && HqRunUp(s) && _usBallotDrive != null ? _usBallotDrive : null;

        /// <summary>The target's rule: the picked jurisdiction, else the soonest open deadline's.</summary>
        private UsBallotAccess.Rule HqBallotTarget(UsBallotAccess.Drive d, System.DateTime today)
        {
            UsBallotAccess.Rule r = _usBallotTarget != null ? UsBallotAccess.RuleOf(d.PollingDay, _usBallotTarget) : null;
            if (r != null) { return r; }
            List<UsBallotAccess.Rule> next = UsBallotAccess.NextDeadlines(d, today, 1);
            return next.Count > 0 ? next[0] : null;
        }

        private static readonly Color BallotOffRule = PoliSimTheme.HairlineStrong;

        private static string BallotDay(System.DateTime d) => d.ToString("d MMM", CultureInfo.InvariantCulture);

        /// <summary>A jurisdiction's electors in force for the election on <paramref name="pollingDay"/> (the catalog's, carried forward past its last
        /// apportionment as the count carries them - R-US13 (a)); 0 where the catalog holds none.</summary>
        private static int UsElectorsOf(string state, System.DateTime pollingDay)
        {
            if (!ElectoralCollege.TryElectorsYear(pollingDay.Year, out int year)) { return 0; }
            foreach (var row in PoliSim.Elections.Generated.UsPresidentialReturns.States) { if (row.Year == year && row.State == state) { return row.Electors; } }
            return 0;
        }

        /// <summary>A jurisdiction's name as the tiles carry it (the UFL file's), else its code.</summary>
        private static string BallotName(string code)
        {
            foreach (CampaignTiles.Tile t in CampaignTiles.UsStates.Tiles) { if (t.Id == code) { return t.Label; } }
            return code;
        }

        // ------------------------------------------------------------------------------------------
        // The map (7a's middle column): the head, the tiles, the legend, the target's card.
        // ------------------------------------------------------------------------------------------
        private void DrawHqBallotGround(CampaignSnapshot s, UsBallotAccess.Drive d, PeopleSlips.Book book)
        {
            CampaignTiles.TileMap map = CampaignTiles.UsStates;
            System.DateTime today = HqWorldToday(s);
            UsBallotAccess.Rule target = HqBallotTarget(d, today);
            HqBallotTerms(book);

            var head = HqR(444f, 131f, 400f, 26f);
            const string title = "The 51";
            GUIStyle head18 = HqSerif(18f, PoliSimTheme.TextPrimary);
            HqLabel(new Rect(head.x, head.y, HqWidth(title, head18), head.height), title, head18);
            string mode = "TARGET · " + (target != null ? target.State : "—");
            GUIStyle modeFace = HqMono(14f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleRight);
            var modeRect = new Rect(head.xMax - HqWidth(mode, modeFace), head.y, HqWidth(mode, modeFace), head.height);
            HqLabel(modeRect, mode, modeFace);
            SlipAnchor(modeRect, "hq:mode");
            book.Anchors["hq:mode"] = new SlipContent(mode).Add("GATHER and FILE act here").Add("a tile picked moves it");

            float u = Mathf.Min(_hqUx, _hqUy);
            float side = Mathf.Round(map.TileSize * u), step = (map.TileSize + map.Gap) * u;
            float ox = Mathf.Round(HqX(map.OriginX)), oy = Mathf.Round(HqY(Mathf.Max(map.OriginY, HqMapTop)));
            bool live = HqLive(out bool _);
            foreach (CampaignTiles.Tile tile in map.Tiles)
            {
                var t = new Rect(ox + Mathf.Round(tile.Column * step), oy + Mathf.Round(tile.Row * step), side, side);
                UsBallotAccess.Rule r = UsBallotAccess.RuleOf(d.PollingDay, tile.Id);
                DrawHqBallotTile(t, tile, r, d.Of(tile.Id), today, target != null && target.State == tile.Id, book);
                if (PoliSimWidgets.Button(t, GUIContent.none, GUIStyle.none) && (live || _usBallotDrive != null)) { _usBallotTarget = tile.Id; }
            }
            float legendY = oy + map.Height * u + HqH(8f);
            float legendBottom = DrawHqBallotLegend(new Rect(Mathf.Round(HqX(444f)), legendY, HqW(400f), HqH(40f)), book);
            if (target != null) { DrawHqBallotCard(new Rect(Mathf.Round(HqX(444f)), legendBottom + HqH(10f), HqW(330f), 1f), target, d.Of(target.State), today); }
        }

        /// <summary>The top of a map's tiles at 1280: under the ground's head (S4's maps start here; Design's 7a placed the US map at 133 on a page with no
        /// timeline, so it keeps its x and sits under the head - DECLARED).</summary>
        private const float HqMapTop = 163f;

        /// <summary>One tile in its ballot face (7a's table): on the ballot in the independent's ink with paper text; collecting on paper in a 1.5 px ink rule with
        /// a progress hairline at its foot; off in a 1.5 px dashed rule; its deadline passed in a 1 px BAD rule struck by a 2 px BAD stroke at −45°; BILLED in a
        /// 1 px dashed rule, muted. The target carries the HQ's ring.</summary>
        private void DrawHqBallotTile(Rect t, CampaignTiles.Tile tile, UsBallotAccess.Rule r, UsBallotAccess.Petition p, System.DateTime today, bool target, PeopleSlips.Book book)
        {
            UsBallotAccess.Standing st = UsBallotAccess.StandingOf(r, p, today);
            if (target && Event.current.type == EventType.Repaint)
            {
                float g = HqT(2f);
                PoliSimTheme.Rule(new Rect(t.x - 2f * g, t.y - 2f * g, t.width + 4f * g, t.height + 4f * g), PoliSimTheme.TextPrimary);
                PoliSimTheme.Rule(new Rect(t.x - g, t.y - g, t.width + 2f * g, t.height + 2f * g), PoliSimTheme.Card);
            }
            Color ink = PoliSimTheme.TextPrimary;
            switch (st)
            {
                case UsBallotAccess.Standing.OnBallot: HqFill(t, IndependentInk()); ink = PoliSimTheme.ModalBackground; break;
                case UsBallotAccess.Standing.Collecting: HqFill(t, PoliSimTheme.Card); HqEdge(t, PoliSimTheme.TextPrimary, HqT(1.5f)); break;
                case UsBallotAccess.Standing.Off: HqFill(t, PoliSimTheme.Card); HqDashed(t, BallotOffRule, HqT(1.5f)); break;
                case UsBallotAccess.Standing.Lost: HqFill(t, PoliSimTheme.Card); HqEdge(t, PoliSimTheme.Bad, HqT(1f)); break;
                default: HqFill(t, PoliSimTheme.Card); HqDashed(t, PoliSimTheme.Hairline, HqT(1f)); ink = PoliSimTheme.TextMuted; break;
            }
            float line = V35.Px(15f);
            HqLabel(new Rect(t.x, t.y + V35.Px(1f), t.width, line), tile.Id, HqMono(14f, ink, bold: true, anchor: TextAnchor.MiddleCenter));
            string electors = r != null ? r.Electors.ToString(CultureInfo.InvariantCulture) : "—";
            HqLabel(new Rect(t.x, t.y + V35.Px(16f), t.width, line), electors, HqMono(14f, ink, anchor: TextAnchor.MiddleCenter));
            if (st == UsBallotAccess.Standing.Collecting)
            {
                double progress = UsBallotAccess.Progress(r, p);
                var track = new Rect(t.x + V35.Px(3f), t.yMax - V35.Px(2f) - HqT(3f), t.width - V35.Px(6f), HqT(3f));
                HqFill(track, HqTrack);
                if (!double.IsNaN(progress)) { HqFill(new Rect(track.x, track.y, Mathf.Round(track.width * (float)progress), track.height), PoliSimTheme.TextPrimary); }
            }
            if (st == UsBallotAccess.Standing.Lost && Event.current.type == EventType.Repaint) { PoliSimTheme.Stroke(t, -45f, HqT(2f), PoliSimTheme.Bad); }
            string id = "hq:ballot:" + tile.Id;
            SlipAnchor(t, id);
            book.Anchors[id] = HqBallotSlip(r, p, today, tile.Label);
        }

        /// <summary>The ballot slips' level 2 (7c: one sentence each), in the book every pass the map draws.</summary>
        private static void HqBallotTerms(PeopleSlips.Book book)
        {
            book.Terms["No votes"] = new SlipContent("NO VOTES").Add("A state where the petition is not filed by its deadline gives the candidate no votes.");
            book.Terms["BILLED"] = new SlipContent(SymbolRegistry.Word(Symbol.Billed)).Add("A rule not on disk is billed, never estimated, so no petition can be filed there.");
        }

        /// <summary>The per-state slip (7c): the state and its electors, then four fragments - the standing's own; level 2 one sentence (the marked term).</summary>
        private static SlipContent HqBallotSlip(UsBallotAccess.Rule r, UsBallotAccess.Petition p, System.DateTime today, string name)
        {
            if (r == null) { return new SlipContent(name).Add("no rule on disk").Add("Rule [[BILLED]]"); }
            var slip = new SlipContent(name + " · " + r.Electors.ToString(CultureInfo.InvariantCulture) + " electors");
            foreach (string l in HqBallotLines(r, p, today)) { slip.Add(l); }
            return slip;
        }

        /// <summary>A jurisdiction's four fragments by its standing (7c's table), each figure the drive's or the rule's; BILLED where the rule is.</summary>
        private static string[] HqBallotLines(UsBallotAccess.Rule r, UsBallotAccess.Petition p, System.DateTime today)
        {
            int gathered = p?.Gathered ?? 0;
            string need = r.Signatures >= 0 ? r.Signatures.ToString("N0", CultureInfo.InvariantCulture) : SymbolRegistry.Word(Symbol.Billed);
            string price = double.IsNaN(UsBallotAccess.PricePerSignatureUsd) ? "Per signature · " + SymbolRegistry.Word(Symbol.Billed) : "$ " + UsBallotAccess.PricePerSignatureUsd.ToString("0.00", CultureInfo.InvariantCulture) + " a signature";
            string fee = r.FeeRoute == "billed" ? "Fee · " + SymbolRegistry.Word(Symbol.Billed) : r.FeeUsd > 0 ? "Fee $ " + r.FeeUsd.ToString("N0", CultureInfo.InvariantCulture) + (r.FeeInstead ? " instead" : string.Empty) : price;
            switch (UsBallotAccess.StandingOf(r, p, today))
            {
                case UsBallotAccess.Standing.OnBallot:
                    return new[] { "Filed " + BallotDay(p.FiledOn), r.FeeInstead && gathered < r.Signatures ? "Fee paid instead" : gathered.ToString("N0", CultureInfo.InvariantCulture) + " of " + need, fee, "On the ballot" };
                case UsBallotAccess.Standing.Collecting:
                    return new[] { "Signatures " + gathered.ToString("N0", CultureInfo.InvariantCulture) + " of " + need, DeadlineLine(r, today), r.FeeUsd > 0 || r.FeeRoute == "billed" ? fee : price,
                        "Collecting · " + (100.0 * UsBallotAccess.Progress(r, p)).ToString("0", CultureInfo.InvariantCulture) + " %" };
                case UsBallotAccess.Standing.Off:
                    return new[] { "Not started · " + need + " needed", DeadlineLine(r, today), r.FeeUsd > 0 || r.FeeRoute == "billed" ? fee : price, "Off the ballot" };
                case UsBallotAccess.Standing.Lost:
                    return new[] { "Deadline passed · " + BallotDay(r.Deadline.Value), gathered.ToString("N0", CultureInfo.InvariantCulture) + " of " + need, "[[No votes]] here", "Lost" };
                default:
                    return new[] { "Signatures · " + need, r.Deadline.HasValue ? "Deadline " + BallotDay(r.Deadline.Value) : "Deadline · " + SymbolRegistry.Word(Symbol.Billed), "Rule [[BILLED]] · not on disk", "Not on the ballot" };
            }
        }

        private static string DeadlineLine(UsBallotAccess.Rule r, System.DateTime today)
        {
            int days = (int)(r.Deadline.Value.Date - today.Date).TotalDays;
            return "Deadline " + BallotDay(r.Deadline.Value) + " · " + days.ToString(CultureInfo.InvariantCulture) + (days == 1 ? " day" : " days");
        }

        /// <summary>The legend under the map (7a: mono 14, 14 px swatches): the four faces, and BILLED where a rule is.</summary>
        private float DrawHqBallotLegend(Rect area, PeopleSlips.Book book)
        {
            var items = new (string Text, UsBallotAccess.Standing Kind)[]
            {
                ("on the ballot", UsBallotAccess.Standing.OnBallot), ("collecting", UsBallotAccess.Standing.Collecting), ("off the ballot", UsBallotAccess.Standing.Off),
                ("deadline passed", UsBallotAccess.Standing.Lost), (SymbolRegistry.Word(Symbol.Billed), UsBallotAccess.Standing.Billed),
            };
            GUIStyle face = HqMono(14f, PoliSimTheme.TextPrimary);
            float sw = V35.Px(14f), x = area.x, y = area.y, lineH = HqH(18f), gapX = HqW(12f), gapY = HqH(4f);
            foreach ((string text, UsBallotAccess.Standing kind) in items)
            {
                float w = sw + V35.Px(5f) + HqWidth(text, face);
                if (x > area.x && x + w > area.xMax) { x = area.x; y += lineH + gapY; }
                var swatch = new Rect(x, y + Mathf.Round((lineH - sw) * 0.5f), sw, sw);
                switch (kind)
                {
                    case UsBallotAccess.Standing.OnBallot: HqFill(swatch, IndependentInk()); break;
                    case UsBallotAccess.Standing.Collecting: HqFill(swatch, PoliSimTheme.Card); HqEdge(swatch, PoliSimTheme.TextPrimary, HqT(1.5f)); break;
                    case UsBallotAccess.Standing.Off: HqDashed(swatch, BallotOffRule, HqT(1.5f)); break;
                    case UsBallotAccess.Standing.Lost:
                        HqFill(swatch, PoliSimTheme.Card); HqEdge(swatch, PoliSimTheme.Bad, HqT(1f));
                        if (Event.current.type == EventType.Repaint) { PoliSimTheme.Stroke(swatch, -45f, HqT(2f), PoliSimTheme.Bad); }
                        break;
                    default: HqFill(swatch, PoliSimTheme.Card); HqDashed(swatch, PoliSimTheme.Hairline, HqT(1f)); break;
                }
                HqLabel(new Rect(x + sw + V35.Px(5f), y, HqWidth(text, face), lineH), text, face);
                x += w + gapX;
            }
            var legend = new Rect(area.x, area.y, area.width, y + lineH - area.y);
            SlipAnchor(legend, "hq:legend");
            book.Anchors["hq:legend"] = new SlipContent("THE 51").Add("ink · filed, on the ballot").Add("outline · signatures being gathered").Add("struck · deadline passed, [[No votes]]")
                .Add("Rule [[BILLED]] · not on disk");
            return y + lineH;
        }

        /// <summary>The target's card under the map (Ohio's on 7a, drawn there pinned): the state's slip at rest - its title and four fragments.</summary>
        private void DrawHqBallotCard(Rect at, UsBallotAccess.Rule r, UsBallotAccess.Petition p, System.DateTime today)
        {
            string[] lines = HqBallotLines(r, p, today);
            float pad = HqW(10f), titleH = HqH(22f), lineH = HqH(20f);
            var card = new Rect(at.x, at.y, at.width, HqH(8f) + titleH + HqH(4f) + lines.Length * lineH + HqH(9f));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(card.x + HqT(2f), card.y + HqT(2f), card.width, card.height), new Color(0.169f, 0.149f, 0.125f, 0.35f));
                PoliSimTheme.Rule(card, V35.SlipPaper);
            }
            HqEdge(card, V35.SlipEdge, HqT(1f));
            float y = card.y + HqH(8f);
            string title = BallotName(r.State) + " · " + r.Electors.ToString(CultureInfo.InvariantCulture) + " electors";
            HqLabel(new Rect(card.x + pad, y, card.width - 2f * pad, titleH), title, HqSerif(16f, PoliSimTheme.TextPrimary));
            HqFill(new Rect(card.x + pad, y + titleH, card.width - 2f * pad, HqT(1f)), PoliSimTheme.RuleRow);
            y += titleH + HqH(4f);
            foreach (string l in lines)
            {
                HqLabel(new Rect(card.x + pad, y, card.width - 2f * pad, lineH), SlipContent.Plain(l), HqMono(14f, PoliSimTheme.TextPrimary));
                y += lineH;
            }
        }

        // ------------------------------------------------------------------------------------------
        // The left column (7a): ballot access, what the map adds up to, the next deadlines.
        // ------------------------------------------------------------------------------------------

        /// <summary>The ballot block at the top of the left column; returns the y under it.</summary>
        private float DrawHqBallotStanding(float x, float y, float w, CampaignSnapshot s, UsBallotAccess.Drive d, PeopleSlips.Book book)
        {
            System.DateTime today = HqWorldToday(s);
            UsBallotAccess.Tally(d, today, out int on, out int collecting, out int lost, out int onCount);
            int all = UsBallotAccess.Rules(d.PollingDay).Count;

            var head = new Rect(x, y, w, HqH(26f));
            GUIStyle head18 = HqSerif(18f, PoliSimTheme.TextPrimary);
            HqLabel(new Rect(head.x, head.y, HqWidth("Ballot access", head18), head.height), "Ballot access", head18);
            string count = onCount.ToString(CultureInfo.InvariantCulture) + " of " + all.ToString(CultureInfo.InvariantCulture);
            GUIStyle bold = HqMono(14f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleRight);
            HqLabel(new Rect(head.xMax - HqWidth(count, bold), head.y, HqWidth(count, bold), head.height), count, bold);
            y += head.height + HqH(4f);

            // what the map adds up to: the electors on, the bar of the 538, its key
            var figureRow = new Rect(x, y, w, HqH(30f));
            GUIStyle big = HqMono(24f, PoliSimTheme.TextPrimary, bold: true);
            string onText = on.ToString(CultureInfo.InvariantCulture);
            float bw = HqWidth(onText, big);
            HqLabel(new Rect(figureRow.x, figureRow.y, bw, figureRow.height), onText, big);
            string caption = "electors on · " + ElectoralCollege.MajorityToElect.ToString(CultureInfo.InvariantCulture) + " to win";
            GUIStyle muted = HqMono(14f, PoliSimTheme.TextMuted);
            HqLabel(new Rect(figureRow.x + bw + V35.Px(6f), figureRow.y + V35.Px(4f), HqWidth(caption, muted), figureRow.height - V35.Px(4f)), caption, muted);
            y += figureRow.height;

            var bar = new Rect(x, y, w, HqH(16f));
            float Px(int electors) => Mathf.Round(bar.width * electors / (float)ElectoralCollege.TotalElectors);
            HqFill(bar, PoliSimTheme.Card);
            HqFill(new Rect(bar.x, bar.y, Px(on), bar.height), IndependentInk());
            if (collecting > 0) { HqEdge(new Rect(bar.x + Px(on), bar.y, Mathf.Max(HqT(3f), Px(collecting)), bar.height), PoliSimTheme.TextPrimary, HqT(1.5f)); }
            if (lost > 0) { DrawHatch(new Rect(bar.xMax - Px(lost), bar.y, Px(lost), bar.height), PoliSimTheme.Bad, PoliSimTheme.Card); }
            HqEdge(bar, BallotOffRule, HqT(1f));
            float tick = bar.x + Px(ElectoralCollege.MajorityToElect) - HqT(1f);
            HqFill(new Rect(tick, bar.y - V35.Px(5f), HqT(2f), V35.Px(24f)), PoliSimTheme.TextPrimary);
            var reach = new Rect(x, figureRow.y, w, bar.yMax - figureRow.y);
            SlipAnchor(reach, "hq:reach");
            book.Anchors["hq:reach"] = new SlipContent("ELECTORS IN REACH")
                .Add("on the ballot " + onText + " · collecting " + collecting.ToString(CultureInfo.InvariantCulture))
                .Add("lost " + lost.ToString(CultureInfo.InvariantCulture) + " · " + ElectoralCollege.MajorityToElect.ToString(CultureInfo.InvariantCulture) + " to win")
                .Add("on + collecting " + (on + collecting).ToString(CultureInfo.InvariantCulture) + (on + collecting < ElectoralCollege.MajorityToElect ? " · short of the tick" : string.Empty));
            y += bar.height;

            var keyRow = new Rect(x, y, w, HqH(22f));
            float kx = keyRow.x, sw = V35.Px(12f);
            GUIStyle keyFace = HqMono(14f, PoliSimTheme.TextPrimary, anchor: TextAnchor.LowerLeft);
            foreach ((string text, int kind) in new[] { ("on " + onText, 0), ("collecting " + collecting.ToString(CultureInfo.InvariantCulture), 1), ("lost " + lost.ToString(CultureInfo.InvariantCulture), 2) })
            {
                var swatch = new Rect(kx, keyRow.yMax - V35.Px(3f) - sw, sw, sw);
                if (kind == 0) { HqFill(swatch, IndependentInk()); }
                else if (kind == 1) { HqEdge(swatch, PoliSimTheme.TextPrimary, HqT(1.5f)); }
                else { DrawHatch(swatch, PoliSimTheme.Bad, PoliSimTheme.Card); }
                float tw = HqWidth(text, keyFace);
                HqLabel(new Rect(swatch.xMax + V35.Px(5f), keyRow.y, tw, keyRow.height), text, keyFace);
                kx = swatch.xMax + V35.Px(5f) + tw + HqW(12f);
            }
            y += keyRow.height;

            // the next deadlines: four rows, each over its progress bar
            y += HqH(14f);
            var nextHead = new Rect(x, y, w, HqH(26f));
            HqLabel(new Rect(nextHead.x, nextHead.y, HqWidth("Next deadlines", head18), nextHead.height), "Next deadlines", head18);
            y += nextHead.height;
            List<UsBallotAccess.Rule> next = UsBallotAccess.NextDeadlines(d, today, 4);
            if (next.Count == 0)
            {
                HqLabel(new Rect(x, y, w, HqH(24f)), "none open", HqMono(14f, PoliSimTheme.TextMuted));
                y += HqH(24f);
            }
            foreach (UsBallotAccess.Rule r in next)
            {
                UsBallotAccess.Petition p = d.Of(r.State);
                HqFill(new Rect(x, y, w, HqT(1f)), PoliSimTheme.RuleRow);
                var row = new Rect(x, y + HqH(3f), w, HqH(24f));
                GUIStyle code = HqMono(14f, PoliSimTheme.TextPrimary, bold: true);
                HqLabel(new Rect(row.x, row.y, HqW(28f), row.height), r.State, code);
                HqLabel(new Rect(row.x + HqW(36f), row.y, HqW(24f), row.height), r.Electors.ToString(CultureInfo.InvariantCulture), muted);
                double progress = UsBallotAccess.Progress(r, p);
                string sig = r.Signatures >= 0
                    ? (p?.Gathered ?? 0).ToString("N0", CultureInfo.InvariantCulture) + " / " + r.Signatures.ToString("N0", CultureInfo.InvariantCulture)
                    : SymbolRegistry.Word(Symbol.Billed);
                if (r.FeeInstead && r.Signatures >= 0 && (p?.Gathered ?? 0) == 0) { sig = "or $ " + r.FeeUsd.ToString("N0", CultureInfo.InvariantCulture); }
                HqLabel(new Rect(row.x + HqW(68f), row.y, HqWidth(sig, code), row.height), sig, code);
                bool behind = !double.IsNaN(progress) && progress < 0.5;
                string day = BallotDay(r.Deadline.Value);
                GUIStyle dayFace = HqMono(14f, behind ? PoliSimTheme.Bad : PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleRight);
                HqLabel(new Rect(row.xMax - HqWidth(day, dayFace), row.y, HqWidth(day, dayFace), row.height), day, dayFace);
                var track = new Rect(x, row.yMax, w, HqH(6f));
                HqFill(track, HqTrack);
                if (!double.IsNaN(progress)) { HqFill(new Rect(track.x, track.y, Mathf.Round(track.width * (float)progress), track.height), PoliSimTheme.TextPrimary); }
                var whole = new Rect(x, y, w, track.yMax + HqH(5f) - y);
                string id = "hq:next:" + r.State;
                SlipAnchor(whole, id);
                book.Anchors[id] = HqBallotSlip(r, p, today, BallotName(r.State));
                y = whole.yMax;
            }
            return y;
        }

        // ------------------------------------------------------------------------------------------
        // The day column (7a): BALLOT ACCESS - GATHER, FILE, § RULES.
        // ------------------------------------------------------------------------------------------
        private float DrawHqBallotVerbs(float x, float y, float w, CampaignSnapshot s, UsBallotAccess.Drive d, PeopleSlips.Book book)
        {
            System.DateTime today = HqWorldToday(s);
            UsBallotAccess.Rule target = HqBallotTarget(d, today);
            UsBallotAccess.Petition p = target != null ? d.Of(target.State) : null;
            GUIStyle captionFace = HqMono(14f, PoliSimTheme.TextMuted);
            HqLabel(new Rect(x, y, w, HqH(22f)), "BALLOT ACCESS", captionFace);
            y += HqH(22f);
            float gap = HqW(4f), cellH = HqH(28f);

            // GATHER: priced per signature - BILLED, so ABSENT
            var gather = new Rect(x, y, HqW(130f), cellH);
            bool priced = !double.IsNaN(UsBallotAccess.PricePerSignatureUsd);
            HqVerb(gather, "GATHER", priced ? HqVerbFace.Open : HqVerbFace.Absent, "hq:ballot:gather");
            book.Anchors["hq:ballot:gather"] = priced
                ? new SlipContent("GATHER").Add("signatures in the target state")
                : new SlipContent(SymbolRegistry.Word(Symbol.Absent)).Add("gather · " + UsBallotAccess.PriceBilled).Add("a signature cannot be priced").Add("opens once a price is sourced");

            // FILE: the target's petition, where the rule allows it today
            var file = new Rect(gather.xMax + gap, y, HqW(100f), cellH);
            bool canFile = UsBallotAccess.CanFile(target, p, today, out string why);
            if (HqVerb(file, "FILE", canFile ? HqVerbFace.Open : HqVerbFace.Closed, "hq:ballot:file") && canFile) { UsBallotAccess.File(target, p, today, out string _); }
            double fee = UsBallotAccess.FilingFeeUsd(target);
            book.Anchors["hq:ballot:file"] = canFile
                ? new SlipContent("FILE · " + target.State).Add(target.FeeInstead && (p?.Gathered ?? 0) < target.Signatures ? "the fee instead of signatures" : "the petition, signatures met")
                    .Add(double.IsNaN(fee) ? "fee · " + SymbolRegistry.Word(Symbol.Billed) + " · not charged" : fee > 0.0 ? "fee $ " + fee.ToString("N0", CultureInfo.InvariantCulture) : "no fee named")
                    .Add("puts the candidate on its ballot")
                : new SlipContent("FILE" + (target != null ? " · " + target.State : string.Empty)).Add(why ?? "no target");

            // § RULES: the target's rule, the summary's own words behind it
            var rules = new Rect(file.xMax + gap, y, HqW(120f), cellH);
            if (HqVerb(rules, "§ RULES", HqVerbFace.Done, "hq:ballot:rules")) { PinSlipOnAnchorForFilm("hq:ballot:rules", rules.center); }
            book.Anchors["hq:ballot:rules"] = HqBallotRulesSlip(target);
            return y + cellH;
        }

        /// <summary>The § RULES slip: the target's count and its basis, the deadline and its rule, the fee, and the source.</summary>
        private static SlipContent HqBallotRulesSlip(UsBallotAccess.Rule r)
        {
            if (r == null) { return new SlipContent("§ RULES").Add("no target"); }
            var slip = new SlipContent(BallotName(r.State).ToUpperInvariant() + " · BALLOT ACCESS");
            slip.Add(r.Signatures >= 0 ? r.Signatures.ToString("N0", CultureInfo.InvariantCulture) + " signatures · " + ShortBasis(r.SignaturesBasis) : "signatures · " + SymbolRegistry.Word(Symbol.Billed));
            slip.Add(r.Deadline.HasValue ? "file by " + BallotDay(r.Deadline.Value) + " · " + ShortBasis(r.DeadlineBasis) : "deadline · " + SymbolRegistry.Word(Symbol.Billed));
            slip.Add(r.FeeRoute == "billed" ? "fee · " + SymbolRegistry.Word(Symbol.Billed) : r.FeeUsd > 0 ? "fee $ " + r.FeeUsd.ToString("N0", CultureInfo.InvariantCulture) + (r.FeeInstead ? " instead" : string.Empty) : "no fee named");
            slip.Add("NASS summary · July 2024");
            return slip;
        }

        /// <summary>A basis as the § RULES slip prints it: "count" says nothing beside the figure; a formula keeps its head, a BILLED one its reason's head.</summary>
        private static string ShortBasis(string basis)
        {
            if (string.IsNullOrEmpty(basis) || basis == "count") { return "the summary's count"; }
            int cut = basis.IndexOf(';');
            string head = cut > 0 ? basis.Substring(0, cut) : basis;
            int dash = head.IndexOf(" - ", System.StringComparison.Ordinal);
            return dash > 0 ? head.Substring(0, dash) : head;
        }
    }
}
