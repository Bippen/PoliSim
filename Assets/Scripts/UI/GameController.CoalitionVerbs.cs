using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// COAL-2 with §9 (Design's return of 2026-10-30, `S9 Coalition Verbs`, boards 9a-9d; K320): <b>THE COALITION AND ITS VERBS, IN ONE GRAMMAR</b>, on Politics ›
    /// Cabinet under "The coalition". Every verb reads the same way at rest - the actor's mark, the verb's glyph, the target's mark, the verb in at most three
    /// words, its state stamp, its deadline - and everything longer is the slip's.
    /// <list type="bullet">
    /// <item><description><b>The coalition bar</b>: the parties in or behind the cabinet in their inks, the rest in the row rule, against the line that keeps a
    /// government (its tick and figure); each segment its "code seats", the code alone, or nothing. A verb hovered (or pinned by the film) draws the chamber it
    /// would leave: the actor's or the target's segment as a dashed outline (Design's "For Code" 2).</description></item>
    /// <item><description><b>One row per party in or behind the cabinet</b> (and the player's own in opposition, where a verb or a motion is his): its mark, code,
    /// seats, place, what it has in play (the motion chip and its deadline, the agreement's tally, the side it re-declared) and the verbs open to the player
    /// against it - a junior partner THREATEN and LEAVE on the lead's row; the head DISMISS on each partner's; a supporter ENFORCE and WITHDRAW on its own; after a
    /// count, RE-DECLARE on the player's own. A threat or an enforcement opens its composer under the rows (the demand, the deadline).</description></item>
    /// <item><description><b>In the Sejm</b> (Poland): the motions against the cabinet - each Art. 158 vote of the month (PiS's mark, the 158 glyph, the head's
    /// mark, the bar of votes for against the 231 tick, the figure, its stamp) and, for the opposition, the motion it may move (MOVE ART. 158, the verb's own
    /// gate). Art. 158 and LEAVE THE GOVERNMENT moved here from the Parliament page.</description></item>
    /// </list>
    /// The verbs are the manager's (`SimulationManager.CoalitionVerbs.cs`), each read through its gate so a refusal draws disabled with the verb's own reason.
    /// </summary>
    public partial class GameController
    {
        /// <summary>The composer open under the rows: a threat's, an enforcement's or a re-declaration's (null: none).</summary>
        private CoalitionVerb? _coalitionComposing;
        private int _coalitionDemandIndex, _coalitionDeadlineIndex;

        /// <summary>The party whose segment the bar draws dashed this frame - the verb hovered on the last repaint.</summary>
        private string _coalitionPreviewLast, _coalitionPreviewNext;

        /// <summary>The film's hook: a verb previewed without a pointer, as "verb:party" (a pointer's hover cannot be filmed); null in play.</summary>
        private string _coalitionPinnedPreview;

        /// <summary>The demands a threat may name, read once a day and government (scoring the catalogue is not a per-frame read).</summary>
        private List<AgreementItem> _threatDemands;
        private System.DateTime _threatDemandsOn = System.DateTime.MinValue;
        private GovernmentRecord _threatDemandsFor;

        private static readonly Color CoalitionNeutral = PoliSimTheme.RuleRow;

        private string Short(string key) => string.IsNullOrEmpty(key) ? string.Empty : PartySystems.ShortName(PlayerCountryId, key);

        private bool TryPartyOfKey(string key, out PoliticalParty party)
        {
            foreach (PoliticalParty p in PartySystems.For(PlayerCountryId)) { if (p.Abbrev == key) { party = p; return true; } }
            party = default;
            return false;
        }

        private int SeatsOfKey(string key) => _playerCountry?.ParliamentSeats != null && key != null && _playerCountry.ParliamentSeats.TryGetValue(key, out int n) ? n : 0;

        /// <summary>"After: 230 of 460" / "One short of 231" - the chamber a verb would leave, in the slip's fragments.</summary>
        private static string ShortWords(int governing, int line)
        {
            if (governing >= line) { return string.Format(CultureInfo.InvariantCulture, "A majority, {0} spare", governing - line); }
            int short_ = line - governing;
            return short_ == 1 ? string.Format(CultureInfo.InvariantCulture, "One short of {0}", line) : string.Format(CultureInfo.InvariantCulture, "{0} short of {1}", short_, line);
        }

        /// <summary>COAL-2: the coalition block at the top of the Cabinet tab - nothing where the country's coalition verbs are not modelled, for a presidency, or
        /// with no cabinet stored.</summary>
        private void DrawCoalitionV35(float width)
        {
            Country c = _playerCountry;
            GovernmentRecord g = c?.Government;
            if (g == null || !CoalitionVerbs.Applies(PlayerCountryId) || g.Kind == WorldClock.ExecutiveKind.Presidency || g.Cabinet.Count == 0 || c.ParliamentSeats == null) { return; }
            System.DateTime today = _simulationManager.CurrentDate;
            bool repaint = Event.current.type == EventType.Repaint;
            if (repaint) { _coalitionPreviewNext = null; }
            string previewKey = PreviewParty(g);
            string player = c.PlayerHoldsPresidency ? null : c.PlayerPartyAbbrev;
            // IMGUI: every choice that adds a laid-out row is read here, before any click in this pass can change it - a verb clicked in the rows (a composer
            // opened, a party leaving into opposition, a motion moved) shows from the next pass, so the Layout and the event passes request the same rects
            bool composing = _coalitionComposing.HasValue;
            bool sejm = ConfidenceProcedure.RulesOf(PlayerCountryId) == ConfidenceProcedure.Rules.Sejm;
            List<DivisionRecord> sejmVotes = sejm ? PolishDeskRecord.RecentArt158(c, today) : null;
            ConfidenceProcedure.MotionVote sejmMotion = sejm ? SejmMotionOf(c, g, player, today) : null;

            // ---- the coalition card: the head and the bar ----
            (int governing, int members, int line) = CoalitionVerbs.ChamberWithout(c, g, null, today);
            int scale = Mathf.Max(members, line * 2 - 1);
            float headH = V35.Px(26f), barH = V35.Px(22f), tickLabelH = V35.Px(16f), gap = V35.Px(6f);
            float cardH = V35.Px(V35.CardPadY) * 2f + headH + gap + V35.Px(5f) + barH + V35.Px(5f) + tickLabelH;
            Rect card = GUILayoutUtility.GetRect(width, cardH, GUILayout.Width(width), GUILayout.Height(cardH));
            Rect inner = DrawV35Card(card);
            var head = new Rect(inner.x, inner.y, inner.width, headH);
            GUIStyle headFace = V35Serif(18f, PoliSimTheme.TextPrimary);
            GUIStyle muted = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
            const string title = "The coalition";
            float tw = Mathf.Ceil(headFace.CalcSize(new GUIContent(title)).x) + 2f;
            string figure = string.Format(CultureInfo.InvariantCulture, "{0} of {1} · {2} to govern", governing, members, line);
            float fw = Mathf.Ceil(muted.CalcSize(new GUIContent(figure)).x) + 2f;
            string lowNote = PartySystems.HistoryNote(PlayerCountryId);
            Rect low = default;
            if (lowNote != null)
            {
                const string stamp = "LOW CONFIDENCE";
                float sw = Mathf.Ceil(muted.CalcSize(new GUIContent(stamp)).x) + V35.Px(12f);
                low = new Rect(head.xMax - sw, head.y + Mathf.Round((headH - V35.Px(22f)) * 0.5f), sw, V35.Px(22f));
                if (repaint) { DrawBox(low, PoliSimTheme.TextPrimary, 1f, false); GUI.Label(low, stamp, V35Mono(V35.Floor, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleCenter)); }
                SlipAnchor(low, "coal:low");
                _politicsSlipBook.Anchors["coal:low"] = new SlipContent("LOW CONFIDENCE").Add("The lists changed between elections").Add("Read by [[lineage]]");
                _politicsSlipBook.Terms["lineage"] = new SlipContent("BY LINEAGE").Add(lowNote.Replace("LOW CONFIDENCE - ", string.Empty));   // the note as built (`PartySystems.HistoryNote`), the night's own
            }
            if (repaint)
            {
                GUI.Label(new Rect(head.x, head.y, tw, headH), title, headFace);
                if (head.x + tw + V35.Px(14f) + fw <= (lowNote != null ? low.x - V35.Px(8f) : head.xMax)) { GUI.Label(new Rect(head.x + tw + V35.Px(14f), head.y, fw, headH), figure, muted); }
            }
            SlipAnchor(new Rect(head.x, head.y, tw + V35.Px(14f) + fw, headH), "coal:head");
            _politicsSlipBook.Anchors["coal:head"] = new SlipContent("THE COALITION")
                .Add(string.Format(CultureInfo.InvariantCulture, "{0} of {1} seats in or behind it", governing, members))
                .Add(string.Format(CultureInfo.InvariantCulture, "{0} keeps a government", line))
                .Add((g.Outcome ?? string.Empty).Replace("MajorityCoalition", "A majority").Replace("ConfidenceAndSupply", "On support").Replace("MinorityGovernment", "A minority"));

            var bar = new Rect(inner.x, head.yMax + gap + V35.Px(5f), inner.width, barH);
            var order = new List<string>();
            if (!string.IsNullOrEmpty(g.PmParty) && g.Cabinet.Contains(g.PmParty)) { order.Add(g.PmParty); }
            var partners = new List<string>(); foreach (string k in g.Cabinet) { if (k != g.PmParty) { partners.Add(k); } }
            partners.Sort((a, b) => SeatsOfKey(b).CompareTo(SeatsOfKey(a)) != 0 ? SeatsOfKey(b).CompareTo(SeatsOfKey(a)) : string.CompareOrdinal(a, b));
            order.AddRange(partners);
            var supporters = new List<string>(g.Support);
            supporters.Sort((a, b) => SeatsOfKey(b).CompareTo(SeatsOfKey(a)) != 0 ? SeatsOfKey(b).CompareTo(SeatsOfKey(a)) : string.CompareOrdinal(a, b));
            order.AddRange(supporters);
            var others = new List<string>();
            foreach (KeyValuePair<string, int> kv in c.ParliamentSeats) { if (kv.Value > 0 && !order.Contains(kv.Key)) { others.Add(kv.Key); } }
            others.Sort((a, b) => SeatsOfKey(b).CompareTo(SeatsOfKey(a)) != 0 ? SeatsOfKey(b).CompareTo(SeatsOfKey(a)) : string.CompareOrdinal(a, b));
            if (repaint)
            {
                float x = bar.x;
                GUIStyle segFace = V35Mono(V35.Floor, PoliSimTheme.ModalBackground, true);
                GUIStyle segInk = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, true);
                foreach (string k in order) { x = DrawCoalitionSegment(bar, x, k, SeatsOfKey(k), scale, true, k == previewKey, segFace, segInk); }
                foreach (string k in others) { x = DrawCoalitionSegment(bar, x, k, SeatsOfKey(k), scale, false, false, segFace, segInk); }
                float tx = bar.x + Mathf.Round(bar.width * (line - 0.5f) / scale);
                PoliSimTheme.Rule(new Rect(tx - 1f, bar.y - V35.Px(5f), 2f, barH + V35.Px(10f)), PoliSimTheme.TextPrimary);
                string lineText = line.ToString(CultureInfo.InvariantCulture);
                GUIStyle tickFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, false, TextAnchor.MiddleCenter);
                float lw = Mathf.Ceil(tickFace.CalcSize(new GUIContent(lineText)).x) + 4f;
                GUI.Label(new Rect(tx - lw * 0.5f, bar.yMax + V35.Px(5f), lw, tickLabelH), lineText, tickFace);
            }
            SlipAnchor(bar, "coal:bar");
            var names = new List<string>(); foreach (string k in order) { names.Add(Short(k) + " " + SeatsOfKey(k).ToString(CultureInfo.InvariantCulture)); }
            var rest = new List<string>(); foreach (string k in others) { rest.Add(Short(k) + " " + SeatsOfKey(k).ToString(CultureInfo.InvariantCulture)); }
            var barSlip = new SlipContent(string.Format(CultureInfo.InvariantCulture, "THE COALITION · {0} OF {1}", governing, members))
                .Add(string.Join(" · ", names))
                .Add(rest.Count > 0 ? "Outside: " + string.Join(" · ", rest) : "No party outside");
            barSlip.Add(string.Format(CultureInfo.InvariantCulture, "{0} to govern", line));
            if (previewKey != null)
            {
                (int after, int _, int _) = CoalitionVerbs.ChamberWithout(c, g, previewKey, today);
                barSlip.Add(string.Format(CultureInfo.InvariantCulture, "Dashed: {0} gone, {1} of {2}", Short(previewKey), after, members));
            }
            _politicsSlipBook.Anchors["coal:bar"] = barSlip;
            GUILayout.Space(V35.Px(10f));

            // ---- the rows ----
            var rows = new List<string>(order);
            bool playerRow = player != null && !rows.Contains(player) && (_simulationManager.CanRedeclare(PlayerCountryId, BlocSide.For, out string _) || CoalitionVerbs.OpenOf(c, CoalitionVerb.Threaten, player) != null
                || RedeclaredSince(c, player, today) != null);
            if (playerRow) { rows.Add(player); }
            float rowH = V35.Px(48f), headRowH = V35.Px(24f);
            float[] col = { V35.Px(40f), V35.Px(70f), V35.Px(70f), V35.Px(150f), V35.Px(360f) };
            Rect hr = GUILayoutUtility.GetRect(width, headRowH, GUILayout.Width(width), GUILayout.Height(headRowH));
            if (repaint)
            {
                string[] heads = { string.Empty, "party", "seats", "place", "in play", "your verbs" };
                float hx = hr.x;
                for (int i = 0; i < heads.Length; i++)
                {
                    float w = i < col.Length ? col[i] : hr.xMax - hx;
                    if (heads[i].Length > 0) { GUI.Label(new Rect(hx, hr.y, w, hr.height), heads[i], muted); }
                    hx += w;
                }
                PoliSimTheme.Rule(new Rect(hr.x, hr.yMax - 1f, hr.width, 1f), PoliSimTheme.HairlineStrong);
            }
            foreach (string key in rows)
            {
                Rect row = GUILayoutUtility.GetRect(width, rowH, GUILayout.Width(width), GUILayout.Height(rowH));
                DrawCoalitionRow(row, col, c, g, key, player, today);
                if (repaint) { PoliSimTheme.Rule(new Rect(row.x, row.yMax - 1f, row.width, 1f), PoliSimTheme.RuleRow); }
            }
            if (composing) { DrawCoalitionComposer(width, c, g, player); }
            GUILayout.Space(V35.Px(12f));

            // ---- In the Sejm (Poland) ----
            if (sejm) { DrawInTheSejm(width, c, g, player, today, sejmVotes, sejmMotion); }
            if (repaint) { _coalitionPreviewLast = _coalitionPreviewNext; }
            GUILayout.Space(V35.Px(V35.Gutter));
        }

        /// <summary>The party whose segment draws dashed: the film's pinned verb, else the verb hovered on the last repaint.</summary>
        private string PreviewParty(GovernmentRecord g)
        {
            string pinned = _coalitionPinnedPreview;
            if (!string.IsNullOrEmpty(pinned))
            {
                int colon = pinned.IndexOf(':');
                string party = colon >= 0 ? pinned.Substring(colon + 1) : null;
                if (!string.IsNullOrEmpty(party) && (g.Cabinet.Contains(party) || g.Support.Contains(party))) { return party; }
            }
            return _coalitionPreviewLast != null && (g.Cabinet.Contains(_coalitionPreviewLast) || g.Support.Contains(_coalitionPreviewLast)) ? _coalitionPreviewLast : null;
        }

        /// <summary>One segment of the coalition bar; returns the next segment's x.</summary>
        private float DrawCoalitionSegment(Rect bar, float x, string key, int seats, int scale, bool inked, bool dashed, GUIStyle onInk, GUIStyle onPaper)
        {
            if (seats <= 0 || scale <= 0) { return x; }
            float w = Mathf.Max(1f, Mathf.Round(bar.width * seats / (float)scale) - 1f);
            var r = new Rect(x, bar.y, w, bar.height);
            if (dashed) { DrawBox(r, PoliSimTheme.TextPrimary, Mathf.Max(1f, Mathf.Round(V35.Px(1.5f))), true); }
            else { PoliSimTheme.Rule(r, inked ? PoliSimTheme.PartyLaddered(PlayerCountryId, key) : CoalitionNeutral); }
            GUIStyle face = inked && !dashed ? onInk : onPaper;
            string full = Short(key) + " " + seats.ToString(CultureInfo.InvariantCulture);
            float pad = V35.Px(6f);
            string text = face.CalcSize(new GUIContent(full)).x + pad * 2f <= w ? full : face.CalcSize(new GUIContent(Short(key))).x + pad * 2f <= w ? Short(key) : null;
            if (text != null) { GUI.Label(new Rect(r.x + pad, r.y, w - pad, r.height), text, face); }
            return x + Mathf.Round(bar.width * seats / (float)scale);
        }

        /// <summary>A 1 px (or <paramref name="weight"/>) box, solid or dashed.</summary>
        private static void DrawBox(Rect r, Color ink, float weight, bool dashed)
        {
            if (!dashed)
            {
                PoliSimTheme.Rule(new Rect(r.x, r.y, r.width, weight), ink);
                PoliSimTheme.Rule(new Rect(r.x, r.yMax - weight, r.width, weight), ink);
                PoliSimTheme.Rule(new Rect(r.x, r.y, weight, r.height), ink);
                PoliSimTheme.Rule(new Rect(r.xMax - weight, r.y, weight, r.height), ink);
                return;
            }
            float dash = V35.Px(5f), space = V35.Px(3f);
            DrawDashedRule(new Rect(r.x, r.y, r.width, weight), ink, dash, space);
            DrawDashedRule(new Rect(r.x, r.yMax - weight, r.width, weight), ink, dash, space);
            for (float y = r.y; y < r.yMax; y += dash + space)
            {
                float h = Mathf.Min(dash, r.yMax - y);
                PoliSimTheme.Rule(new Rect(r.x, y, weight, h), ink);
                PoliSimTheme.Rule(new Rect(r.xMax - weight, y, weight, h), ink);
            }
        }

        /// <summary>The player's latest re-declaration since the latest count (any party's, by <paramref name="party"/>), or null.</summary>
        private static CoalitionMotion RedeclaredSince(Country c, string party, System.DateTime today)
        {
            System.DateTime count = CoalitionVerbs.LatestCount(c, today);
            if (count == System.DateTime.MinValue) { return null; }
            CoalitionMotion m = CoalitionVerbs.LatestOf(c, CoalitionVerb.Redeclare, party);
            return m != null && m.On.Date >= count.Date ? m : null;
        }

        /// <summary>One party's row: mark, code, seats, place, in play, and the player's verbs against it.</summary>
        private void DrawCoalitionRow(Rect row, float[] col, Country c, GovernmentRecord g, string key, string player, System.DateTime today)
        {
            bool repaint = Event.current.type == EventType.Repaint;
            bool known = TryPartyOfKey(key, out PoliticalParty party);
            float x = row.x;
            float side = V35.Px(22f);
            if (repaint && known) { HemicycleRenderer.DrawMark(new Rect(x, row.y + Mathf.Round((row.height - side) * 0.5f), side, side), PlayerCountryId, party); }
            x += col[0];
            if (repaint) { GUI.Label(new Rect(x, row.y, col[1], row.height), Short(key), V35Mono(15f, PoliSimTheme.TextPrimary, true)); }
            x += col[1];
            if (repaint) { GUI.Label(new Rect(x, row.y, col[2], row.height), SeatsOfKey(key).ToString(CultureInfo.InvariantCulture), V35Mono(V35.Floor, PoliSimTheme.TextPrimary, true)); }
            x += col[2];
            PlayerRole role = g.RoleOf(key);
            string place = role == PlayerRole.PrimeMinister ? "Lead · PM" : role == PlayerRole.JuniorPartner ? "Junior" : role == PlayerRole.Support ? "Outside · support" : "Opposition";
            if (key == player) { place += " · you"; }
            if (repaint) { GUI.Label(new Rect(x, row.y, col[3], row.height), place, V35Serif(V35.Name, PoliSimTheme.TextPrimary)); }
            SlipAnchor(new Rect(row.x, row.y, col[0] + col[1] + col[2] + col[3], row.height), "coal:row:" + key);
            _politicsSlipBook.Anchors["coal:row:" + key] = new SlipContent(Short(key).ToUpperInvariant() + " · " + UiFormat.Seats(SeatsOfKey(key)).ToUpperInvariant())
                .Add(known ? party.Name : key)
                .Add(role == PlayerRole.PrimeMinister || role == PlayerRole.JuniorPartner ? "Posts: " + g.PortfoliosOf(key).ToLowerInvariant() : role == PlayerRole.Support ? "Carries the cabinet from outside" : "Outside the government");
            x += col[3];
            DrawCoalitionInPlay(new Rect(x, row.y, col[4] - V35.Px(8f), row.height), c, g, key, player, today);
            x += col[4];
            DrawCoalitionVerbs(new Rect(x, row.y, Mathf.Max(1f, row.xMax - x), row.height), c, g, key, player, today);
        }

        /// <summary>What a party has in play: its open motion with its deadline, a motion settled this month, the agreement's tally, or the side it re-declared.</summary>
        private void DrawCoalitionInPlay(Rect cell, Country c, GovernmentRecord g, string key, string player, System.DateTime today)
        {
            CoalitionMotion threat = CoalitionVerbs.LatestOf(c, CoalitionVerb.Threaten, key);
            CoalitionMotion enforce = CoalitionVerbs.LatestOf(c, CoalitionVerb.Enforce, key);
            bool Listed(CoalitionMotion m) => m != null && (m.IsOpen || (m.SettledOn.Date <= today.Date && (today.Date - m.SettledOn.Date).TotalDays <= CoalitionVerbs.ListedDays));
            string anchor = "coal:play:" + key;
            if (Listed(threat) && (threat.IsOpen || threat.State == MotionState.Met))
            {
                bool open = threat.IsOpen;
                Rect chip = DrawMotionChip(cell, "threat", open ? "THREATENS TO LEAVE" : "MET · STAYS", open ? PoliSimTheme.Caution : PoliSimTheme.TextMuted, open, !open,
                    open ? "by " + CoalitionVerbs.ShortDay(threat.Deadline) : null);
                (int after, int members, int line) = CoalitionVerbs.ChamberWithout(c, g, key, today);
                SlipAnchor(chip, anchor);
                _politicsSlipBook.Anchors[anchor] = open
                    ? new SlipContent(Short(key).ToUpperInvariant() + " THREATENS TO LEAVE").Add("Demand: " + threat.DemandName).Add("Deadline " + DeskDay(threat.Deadline).ToLowerInvariant())
                        .Add(string.Format(CultureInfo.InvariantCulture, "If it goes: {0} of {1}", after, members)).Add("[[A threat]] · " + ShortWords(after, line))
                    : new SlipContent("THREAT MET · " + DeskDay(threat.SettledOn)).Add("Demand: " + threat.DemandName).Add(Short(key) + " stays");
                _politicsSlipBook.Terms["A threat"] = new SlipContent("A THREAT").Add("A threat is a demand with a deadline; nothing changes until the day it is met, carried out or withdrawn.");
                return;
            }
            if (Listed(enforce))
            {
                bool open = enforce.IsOpen;
                bool met = enforce.State == MotionState.Met;
                string words = open ? "ENFORCES" : met ? "MET" : enforce.State == MotionState.Refused ? "REFUSED" : "WITHDRAWN";
                Color ink = open ? PoliSimTheme.Caution : met ? PoliSimTheme.Good : enforce.State == MotionState.Refused ? PoliSimTheme.Bad : PoliSimTheme.TextMuted;
                Rect chip = DrawMotionChip(cell, "enforce", words, ink, open, false, open ? "due " + CoalitionVerbs.ShortDay(enforce.Deadline) : null);
                SlipAnchor(chip, anchor);
                (int after, int members, int line) = CoalitionVerbs.ChamberWithout(c, g, key, today);
                _politicsSlipBook.Anchors[anchor] = open
                    ? new SlipContent(Short(key).ToUpperInvariant() + " ENFORCES ITS AGREEMENT").Add("Point: " + enforce.DemandName).Add("Due " + DeskDay(enforce.Deadline).ToLowerInvariant())
                        .Add("Met: support goes on").Add(string.Format(CultureInfo.InvariantCulture, "Refused: may withdraw, {0} of {1}", after, members))
                    : new SlipContent(words + " · " + DeskDay(enforce.SettledOn)).Add("Point: " + enforce.DemandName).Add(met ? "Support goes on" : enforce.State == MotionState.Refused ? "Broken · " + Short(key) + " may withdraw" : "Lapsed");
                return;
            }
            SupportAgreement agreement = g.Support.Contains(key) ? g.AgreementOf(key) : null;
            if (agreement != null && agreement.Items.Count > 0)
            {
                int kept = agreement.Count(AgreementState.Delivered);
                Rect chip = DrawMotionChip(cell, "enforce", string.Format(CultureInfo.InvariantCulture, "AGREEMENT · {0} OF {1}", kept, agreement.Items.Count), PoliSimTheme.TextPrimary, false, false, "kept");
                SlipAnchor(chip, anchor);
                var slip = new SlipContent("THE SUPPORT AGREEMENT").Add(string.Format(CultureInfo.InvariantCulture, "{0} points agreed · {1} kept", agreement.Items.Count, kept));
                var owed = new List<string>(); foreach (AgreementItem item in CoalitionVerbs.OwedOf(agreement)) { owed.Add(item.Name); }
                if (owed.Count > 0) { slip.Add("Owed: " + string.Join(" · ", owed.GetRange(0, Mathf.Min(2, owed.Count))) + (owed.Count > 2 ? " · +" + (owed.Count - 2).ToString(CultureInfo.InvariantCulture) : string.Empty)); }
                slip.Add(string.Format(CultureInfo.InvariantCulture, "Broken: {0}", agreement.Count(AgreementState.Broken)));
                _politicsSlipBook.Anchors[anchor] = slip;
                return;
            }
            CoalitionMotion side = RedeclaredSince(c, key, today);
            if (side != null)
            {
                Rect chip = DrawMotionChip(cell, "redeclare", side.Demand, PoliSimTheme.TextPrimary, false, false, null);
                SlipAnchor(chip, anchor);
                _politicsSlipBook.Anchors[anchor] = new SlipContent("RE-DECLARED · " + DeskDay(side.On))
                    .Add(side.Demand + " " + CabinetWords(side.DemandName)).Add(side.ByPlayer ? "Your own declaration" : "After the count").Add("[[Re-declared]] until the next count");
                _politicsSlipBook.Terms["Re-declared"] = new SlipContent("RE-DECLARED").Add("After each count a party declares for the government it formed or against it, and the side stands until the next count.");
                return;
            }
            if (key == player && _simulationManager.CanRedeclare(PlayerCountryId, BlocSide.For, out string _))
            {
                Rect chip = DrawMotionChip(cell, "redeclare", "OPEN", PoliSimTheme.TextPrimary, true, false, null);
                SlipAnchor(chip, anchor);
                _politicsSlipBook.Anchors[anchor] = new SlipContent("RE-DECLARE · OPEN").Add("After the count, your side is open").Add("For, against, or neither").Add("[[Re-declared]] until the next count");
                _politicsSlipBook.Terms["Re-declared"] = new SlipContent("RE-DECLARED").Add("After each count a party declares for the government it formed or against it, and the side stands until the next count.");
            }
        }

        /// <summary>A motion's chip: 30 high, a 1.5 px rule (dashed while open), the verb's glyph and its words in mono 14 bold, then its deadline in the chip's
        /// ink; struck through where met. Returns the chip's rect (the deadline beside it included).</summary>
        private Rect DrawMotionChip(Rect cell, string glyph, string words, Color ink, bool dashed, bool struck, string deadline)
        {
            float h = V35.Px(30f), pad = V35.Px(8f), icon = V35.Px(16f), gapIn = V35.Px(6f);
            GUIStyle face = V35Mono(V35.Floor, ink, true);
            float textW = Mathf.Ceil(face.CalcSize(new GUIContent(words)).x) + 2f;
            var chip = new Rect(cell.x, cell.y + Mathf.Round((cell.height - h) * 0.5f), pad + icon + gapIn + textW + pad, h);
            float dw = 0f;
            if (Event.current.type == EventType.Repaint)
            {
                DrawBox(chip, struck ? PoliSimTheme.Hairline : ink, Mathf.Max(1f, Mathf.Round(V35.Px(1.5f))), dashed);
                DrawV35Icon(new Rect(chip.x + pad, chip.y + Mathf.Round((h - icon) * 0.5f), icon, icon), "verb:" + glyph, struck ? PoliSimTheme.HairlineStrong : ink);
                var text = new Rect(chip.x + pad + icon + gapIn, chip.y, textW, h);
                GUI.Label(text, words, face);
                if (struck) { PoliSimTheme.Rule(new Rect(text.x, Mathf.Round(text.center.y), textW - 2f, 1f), ink); }
            }
            if (!string.IsNullOrEmpty(deadline))
            {
                float x = chip.xMax + V35.Px(8f);
                dw = Mathf.Ceil(face.CalcSize(new GUIContent(deadline)).x) + 2f;
                if (x + dw <= cell.xMax && Event.current.type == EventType.Repaint) { GUI.Label(new Rect(x, chip.y, dw, h), deadline, face); }
                else if (x + dw > cell.xMax) { dw = 0f; }
                dw += V35.Px(8f);
            }
            return new Rect(chip.x, chip.y, chip.width + dw, h);
        }

        /// <summary>A verb's button: brass, 30 high, the verb's glyph 16 and its word in mono 14 bold caps; ink where selected; muted where disabled. Laid
        /// left to right from <paramref name="x"/>; returns the button's rect and whether it was clicked.</summary>
        private bool DrawVerbButton(ref float x, Rect cell, string glyph, string word, bool selected, bool disabled, out Rect r)
        {
            float h = V35.Px(30f), pad = V35.Px(10f), icon = V35.Px(16f), gapIn = V35.Px(6f);
            Color words = disabled ? PoliSimTheme.TextMuted : selected ? PoliSimTheme.ModalBackground : PoliSimTheme.TextPrimary;
            GUIStyle face = V35Mono(V35.Floor, words, true);
            float textW = Mathf.Ceil(face.CalcSize(new GUIContent(word)).x) + 2f;
            r = new Rect(x, cell.y + Mathf.Round((cell.height - h) * 0.5f), pad + icon + gapIn + textW + pad, h);
            x = r.xMax + V35.Px(4f);
            if (Event.current.type == EventType.Repaint)
            {
                Color fill = disabled ? PoliSimTheme.Tint(PoliSimTheme.StockOff, 0.55f) : selected ? PoliSimTheme.TextPrimary : PoliSimTheme.Brass;
                Color border = disabled ? PoliSimTheme.Tint(PoliSimTheme.HairlineStrong, 0.6f) : selected ? PoliSimTheme.TextPrimary : PoliSimTheme.BrassBorder;
                PoliSimTheme.RoundedCard(r, fill, border, 0f);
                DrawV35Icon(new Rect(r.x + pad, r.y + Mathf.Round((h - icon) * 0.5f), icon, icon), "verb:" + glyph, words);
                GUI.Label(new Rect(r.x + pad + icon + gapIn, r.y, textW, h), word, face);
            }
            bool ambient = GUI.enabled;
            GUI.enabled = ambient && !disabled;
            bool clicked = PoliSimWidgets.Button(r, GUIContent.none, GUIStyle.none);
            GUI.enabled = ambient;
            return clicked;
        }

        /// <summary>Marks the verb hovered this repaint, so the next frame's bar draws its chamber.</summary>
        private void NoteVerbHover(Rect r, string party)
        {
            if (Event.current.type == EventType.Repaint && r.Contains(Event.current.mousePosition)) { _coalitionPreviewNext = party; }
        }

        /// <summary>The verbs open to the player against a row's party - each drawn with its gate's own reason where refused.</summary>
        private void DrawCoalitionVerbs(Rect cell, Country c, GovernmentRecord g, string key, string player, System.DateTime today)
        {
            if (player == null) { return; }
            PlayerRole mine = g.RoleOf(player);
            float x = cell.x;
            if (mine == PlayerRole.JuniorPartner && key == g.PmParty)
            {
                (int after, int members, int line) = CoalitionVerbs.ChamberWithout(c, g, player, today);
                CoalitionMotion standing = CoalitionVerbs.OpenOf(c, CoalitionVerb.Threaten, player);
                if (standing != null)
                {
                    bool mayWithdraw = _simulationManager.CanWithdrawThreat(PlayerCountryId, out string withdrawWhy);
                    if (DrawVerbButton(ref x, cell, "threat", "WITHDRAW", false, !mayWithdraw, out Rect w))
                    {
                        if (!_simulationManager.WithdrawThreat(PlayerCountryId, out string refused)) { Debug.Log($"COALITION: refused - {refused}"); }
                    }
                    SlipAnchor(w, "coal:verb:withdraw-threat");
                    _politicsSlipBook.Anchors["coal:verb:withdraw-threat"] = mayWithdraw ? new SlipContent("WITHDRAW THE THREAT").Add("Demand: " + standing.DemandName).Add("Nothing changes · you stay") : SlipWrapped("WITHDRAW", withdrawWhy);
                }
                else
                {
                    bool mayThreaten = _simulationManager.CanThreaten(PlayerCountryId, out string threatWhy);
                    if (DrawVerbButton(ref x, cell, "threat", "THREATEN", _coalitionComposing == CoalitionVerb.Threaten, !mayThreaten, out Rect t))
                    {
                        _coalitionComposing = _coalitionComposing == CoalitionVerb.Threaten ? (CoalitionVerb?)null : CoalitionVerb.Threaten;
                        _coalitionDemandIndex = 0; _coalitionDeadlineIndex = 0;
                    }
                    NoteVerbHover(t, player);
                    SlipAnchor(t, "coal:verb:threaten");
                    _politicsSlipBook.Anchors["coal:verb:threaten"] = mayThreaten
                        ? new SlipContent("THREATEN TO LEAVE").Add("Pick a demand and a deadline").Add(string.Format(CultureInfo.InvariantCulture, "If you go: {0} of {1}", after, members)).Add("[[A threat]] · " + ShortWords(after, line))
                        : SlipWrapped("THREATEN", threatWhy);
                    _politicsSlipBook.Terms["A threat"] = new SlipContent("A THREAT").Add("A threat is a demand with a deadline; nothing changes until the day it is met, carried out or withdrawn.");
                }
                bool mayLeave = _simulationManager.CanLeaveGovernment(PlayerCountryId, out string leaveWhy);
                if (DrawVerbButton(ref x, cell, "leave", "LEAVE", _coalitionPinnedPreview == "leave:" + player, !mayLeave, out Rect l))
                {
                    if (!_simulationManager.LeaveGovernment(PlayerCountryId, out string refused)) { Debug.Log($"CONFIDENCE: refused - {refused}"); }
                    _constructiveProjection = null;   // the party is in opposition now - its constructive row projects afresh
                    _coalitionComposing = null;
                }
                NoteVerbHover(l, player);
                SlipAnchor(l, "coal:verb:leave");
                var gone = new List<string>(); foreach (string k in g.Cabinet) { if (k != player) { gone.Add(Short(k)); } }
                _politicsSlipBook.Anchors["coal:verb:leave"] = mayLeave
                    ? new SlipContent("LEAVE THE GOVERNMENT").Add(string.Format(CultureInfo.InvariantCulture, "After: {0} of {1}", after, members)).Add(ShortWords(after, line))
                        .Add("Your posts go to " + PartySystems.JoinShortNames(gone)).Add("[[Leaving]] · " + (ConfidenceProcedure.RulesOf(PlayerCountryId) == ConfidenceProcedure.Rules.Sejm ? "Art. 158 may follow" : "a motion may follow"))
                    : SlipWrapped("LEAVE", leaveWhy);
                _politicsSlipBook.Terms["Leaving"] = new SlipContent("LEAVING").Add(ConfidenceProcedure.RulesOf(PlayerCountryId) == ConfidenceProcedure.Rules.Sejm
                    ? "Leaving ends your ministers' posts the same day; the cabinet stays unless the Sejm removes it (Art. 158)."
                    : "Leaving ends your ministers' posts the same day; the cabinet stands until the chamber declares otherwise.");
                return;
            }
            if (mine == PlayerRole.PrimeMinister && g.Cabinet.Contains(key) && key != g.PmParty)
            {
                bool mayDismiss = _simulationManager.CanDismiss(PlayerCountryId, key, out string dismissWhy);
                if (DrawVerbButton(ref x, cell, "dismiss", "DISMISS", _coalitionPinnedPreview == "dismiss:" + key, !mayDismiss, out Rect d))
                {
                    if (!_simulationManager.Dismiss(PlayerCountryId, key, out string refused)) { Debug.Log($"COALITION: refused - {refused}"); }
                }
                NoteVerbHover(d, key);
                (int after, int members, int line) = CoalitionVerbs.ChamberWithout(c, g, key, today);
                SlipAnchor(d, "coal:verb:dismiss:" + key);
                _politicsSlipBook.Anchors["coal:verb:dismiss:" + key] = mayDismiss
                    ? new SlipContent("DISMISS " + Short(key).ToUpperInvariant()).Add(string.Format(CultureInfo.InvariantCulture, "After: {0} of {1}", after, members)).Add(ShortWords(after, line))
                        .Add("Its posts go to " + PartySystems.JoinShortNames(ShortNames(g.Cabinet.FindAll(k => k != key)))).Add("[[A dismissal]] · a motion may follow")
                    : SlipWrapped("DISMISS", dismissWhy);
                _politicsSlipBook.Terms["A dismissal"] = new SlipContent("A DISMISSAL").Add("A dismissed partner leaves the cabinet the same day; the chamber's confidence procedure opens, as after a partner leaves.");
                return;
            }
            if (key != player) { return; }
            if (mine == PlayerRole.Support)
            {
                (int after, int members, int line) = CoalitionVerbs.ChamberWithout(c, g, player, today);
                bool mayEnforce = _simulationManager.CanEnforce(PlayerCountryId, out string enforceWhy);
                if (DrawVerbButton(ref x, cell, "enforce", "ENFORCE", _coalitionComposing == CoalitionVerb.Enforce, !mayEnforce, out Rect e))
                {
                    _coalitionComposing = _coalitionComposing == CoalitionVerb.Enforce ? (CoalitionVerb?)null : CoalitionVerb.Enforce;
                    _coalitionDemandIndex = 0; _coalitionDeadlineIndex = 0;
                }
                NoteVerbHover(e, player);
                SlipAnchor(e, "coal:verb:enforce");
                _politicsSlipBook.Anchors["coal:verb:enforce"] = mayEnforce
                    ? new SlipContent("ENFORCE THE AGREEMENT").Add("Demand a point owed, by a day").Add("Met: support goes on").Add(string.Format(CultureInfo.InvariantCulture, "Refused: withdraw, {0} of {1}", after, members))
                    : SlipWrapped("ENFORCE", enforceWhy);
                if (DrawVerbButton(ref x, cell, "leave", "WITHDRAW", _coalitionPinnedPreview == "withdraw:" + player, false, out Rect w))
                {
                    if (!_simulationManager.WithdrawSupport(PlayerCountryId, out string refused)) { Debug.Log($"AGREEMENT: the withdrawal was refused - {refused}"); }
                    _coalitionComposing = null;
                }
                NoteVerbHover(w, player);
                SlipAnchor(w, "coal:verb:withdraw");
                _politicsSlipBook.Anchors["coal:verb:withdraw"] = new SlipContent("WITHDRAW YOUR SUPPORT").Add(string.Format(CultureInfo.InvariantCulture, "The cabinet: {0} of {1}", after, members)).Add(ShortWords(after, line)).Add("A motion may follow");
            }
            bool mayRedeclare = _simulationManager.CanRedeclare(PlayerCountryId, BlocSide.For, out string redeclareWhy);
            bool againstToo = _simulationManager.CanRedeclare(PlayerCountryId, BlocSide.Against, out string _);
            if (mayRedeclare || againstToo)
            {
                if (DrawVerbButton(ref x, cell, "redeclare", "RE-DECLARE", _coalitionComposing == CoalitionVerb.Redeclare, false, out Rect rd))
                {
                    _coalitionComposing = _coalitionComposing == CoalitionVerb.Redeclare ? (CoalitionVerb?)null : CoalitionVerb.Redeclare;
                }
                SlipAnchor(rd, "coal:verb:redeclare");
                _politicsSlipBook.Anchors["coal:verb:redeclare"] = new SlipContent("RE-DECLARE YOUR BLOC").Add("For the government, against, or neither").Add("Once after each count").Add("[[Re-declared]] until the next count");
                _politicsSlipBook.Terms["Re-declared"] = new SlipContent("RE-DECLARED").Add("After each count a party declares for the government it formed or against it, and the side stands until the next count.");
            }
        }

        /// <summary>The composer under the rows: a threat's demand and deadline, an enforcement's point and deadline, or a re-declaration's three sides.</summary>
        private void DrawCoalitionComposer(float width, Country c, GovernmentRecord g, string player)
        {
            float h = V35.Px(44f);
            Rect row = GUILayoutUtility.GetRect(width, h, GUILayout.Width(width), GUILayout.Height(h));
            bool repaint = Event.current.type == EventType.Repaint;
            if (repaint) { PoliSimTheme.Rule(row, V35.CardPaper); PoliSimTheme.Rule(new Rect(row.x, row.yMax - 1f, row.width, 1f), PoliSimTheme.RuleRow); }
            GUIStyle label = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
            GUIStyle value = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
            float x = row.x + V35.Px(8f);
            var cell = new Rect(row.x, row.y, row.width, row.height);
            if (_coalitionComposing == CoalitionVerb.Redeclare)
            {
                foreach (BlocSide side in new[] { BlocSide.For, BlocSide.Against, BlocSide.Neither })
                {
                    bool may = _simulationManager.CanRedeclare(PlayerCountryId, side, out string why);
                    string word = CoalitionVerbs.SideWord(side);
                    if (DrawVerbButton(ref x, cell, "redeclare", word, false, !may, out Rect b))
                    {
                        if (!_simulationManager.Redeclare(PlayerCountryId, side, out string refused)) { Debug.Log($"COALITION: refused - {refused}"); }
                        _coalitionComposing = null;
                    }
                    SlipAnchor(b, "coal:compose:" + word);
                    _politicsSlipBook.Anchors["coal:compose:" + word] = may
                        ? new SlipContent("RE-DECLARE " + word).Add(side == BlocSide.For ? "For " + PartySystems.JoinShortNames(ShortNames(g.Cabinet)) : side == BlocSide.Against ? "Against " + PartySystems.JoinShortNames(ShortNames(g.Cabinet)) : "Neither side")
                            .Add(PlayerCountryId == CountryId.Sweden ? "Your bloc from today" : "Recorded; no vote reads it")
                        : SlipWrapped(word, why);
                }
            }
            else
            {
                bool threat = _coalitionComposing == CoalitionVerb.Threaten;
                List<AgreementItem> choices = threat ? ThreatDemandsToday() : CoalitionVerbs.OwedOf(g.AgreementOf(player));
                if (choices.Count == 0) { _coalitionComposing = null; return; }
                _coalitionDemandIndex = ((_coalitionDemandIndex % choices.Count) + choices.Count) % choices.Count;
                int[] days = CoalitionVerbs.DeadlineDays;
                _coalitionDeadlineIndex = ((_coalitionDeadlineIndex % days.Length) + days.Length) % days.Length;
                string head = threat ? "DEMAND" : "POINT";
                float lw = Mathf.Ceil(label.CalcSize(new GUIContent(head)).x) + 2f;
                if (repaint) { GUI.Label(new Rect(x, row.y, lw, row.height), head, label); }
                x += lw + V35.Px(8f);
                if (DrawVerbButton(ref x, cell, threat ? "threat" : "enforce", "<", false, choices.Count < 2, out Rect _)) { _coalitionDemandIndex--; }
                AgreementItem pick = choices[_coalitionDemandIndex];
                float vw = Mathf.Min(V35.Px(300f), Mathf.Ceil(value.CalcSize(new GUIContent(pick.Name)).x) + 4f);
                if (repaint && value.CalcSize(new GUIContent(pick.Name)).x <= vw) { GUI.Label(new Rect(x, row.y, vw, row.height), pick.Name, value); }
                SlipAnchor(new Rect(x, row.y, vw, row.height), "coal:compose:demand");
                _politicsSlipBook.Anchors["coal:compose:demand"] = new SlipContent((threat ? "THE DEMAND · " : "THE POINT · ") + (_coalitionDemandIndex + 1).ToString(CultureInfo.InvariantCulture) + " OF " + choices.Count.ToString(CultureInfo.InvariantCulture))
                    .Add(pick.Name).Add(pick.Kind == AgreementItemKind.Law ? "A law to enact" : "A dial to reach").Add(threat ? "From your own positions" : "Owed under the agreement");
                x += vw + V35.Px(8f);
                if (DrawVerbButton(ref x, cell, threat ? "threat" : "enforce", ">", false, choices.Count < 2, out Rect _)) { _coalitionDemandIndex++; }
                x += V35.Px(12f);
                const string by = "BY";
                float bw = Mathf.Ceil(label.CalcSize(new GUIContent(by)).x) + 2f;
                if (repaint) { GUI.Label(new Rect(x, row.y, bw, row.height), by, label); }
                x += bw + V35.Px(8f);
                if (DrawVerbButton(ref x, cell, threat ? "threat" : "enforce", "<", false, false, out Rect _)) { _coalitionDeadlineIndex--; }
                string dayWords = days[_coalitionDeadlineIndex].ToString(CultureInfo.InvariantCulture) + " days";
                float dw = Mathf.Ceil(value.CalcSize(new GUIContent(dayWords)).x) + 4f;
                if (repaint) { GUI.Label(new Rect(x, row.y, dw, row.height), dayWords, value); }
                SlipAnchor(new Rect(x, row.y, dw, row.height), "coal:compose:deadline");
                _politicsSlipBook.Anchors["coal:compose:deadline"] = new SlipContent("THE DEADLINE").Add(DeskDay(_simulationManager.CurrentDate.AddDays(days[_coalitionDeadlineIndex])).ToLowerInvariant()).Add("30, 60 or 90 days").Add(threat ? "Unmet on its day: you leave" : "Unmet on its day: broken");
                x += dw + V35.Px(8f);
                if (DrawVerbButton(ref x, cell, threat ? "threat" : "enforce", ">", false, false, out Rect _)) { _coalitionDeadlineIndex++; }
                x += V35.Px(12f);
                string confirm = threat ? "THREATEN" : "ENFORCE";
                if (DrawVerbButton(ref x, cell, threat ? "threat" : "enforce", confirm, true, false, out Rect ok))
                {
                    string key = SupportAgreement.KeyOf(pick);
                    bool acted = threat ? _simulationManager.Threaten(PlayerCountryId, key, days[_coalitionDeadlineIndex], out string refused) : _simulationManager.Enforce(PlayerCountryId, key, days[_coalitionDeadlineIndex], out refused);
                    if (!acted) { Debug.Log($"COALITION: refused - {refused}"); }
                    _coalitionComposing = null;
                }
                SlipAnchor(ok, "coal:compose:confirm");
                _politicsSlipBook.Anchors["coal:compose:confirm"] = new SlipContent(confirm).Add(pick.Name).Add("By " + DeskDay(_simulationManager.CurrentDate.AddDays(days[_coalitionDeadlineIndex])).ToLowerInvariant());
            }
            if (DrawVerbButton(ref x, cell, "leave", "CANCEL", false, false, out Rect cancel)) { _coalitionComposing = null; }
            SlipAnchor(cancel, "coal:compose:cancel");
            _politicsSlipBook.Anchors["coal:compose:cancel"] = new SlipContent("CANCEL").Add("Nothing is sent");
        }

        /// <summary>A cabinet held as its keys joined by "+" (a re-declaration's), in the authority's short names by §10's join rule.</summary>
        private string CabinetWords(string joinedKeys) => string.IsNullOrEmpty(joinedKeys) ? string.Empty : PartySystems.JoinShortNames(ShortNames(joinedKeys.Split('+')));

        private List<string> ShortNames(IEnumerable<string> keys) { var names = new List<string>(); foreach (string k in keys) { names.Add(Short(k)); } return names; }

        /// <summary>The threat's demands, read once a day and government.</summary>
        private List<AgreementItem> ThreatDemandsToday()
        {
            GovernmentRecord g = _playerCountry?.Government;
            if (_threatDemands == null || _threatDemandsOn != _simulationManager.CurrentDate || !ReferenceEquals(_threatDemandsFor, g))
            {
                _threatDemands = _simulationManager.ThreatDemands(PlayerCountryId);
                _threatDemandsOn = _simulationManager.CurrentDate;
                _threatDemandsFor = g;
            }
            return _threatDemands;
        }

        /// <summary>
        /// §9 (board 9a's "In the Sejm"): THE MOTIONS AGAINST THE CABINET, in the one grammar - each Art. 158 vote of the month (K197's record,
        /// <see cref="PolishDeskRecord.RecentArt158"/>): the mover's mark, the 158 glyph, the head's mark, "Constructive vote · PiS's candidate", "Art. 158", a bar of
        /// the votes for against the 231 tick, "200 / 231", the stamp FAILED or PASSED with its day; then, for the opposition, the motion it may move - its
        /// projection, PREVIEW dashed, and MOVE ART. 158 drawn with the verb's own gate (`CanMoveNoConfidence`, K270: no quiet days). Moved from the Parliament page.
        /// </summary>
        private void DrawInTheSejm(float width, Country c, GovernmentRecord g, string player, System.DateTime today, List<DivisionRecord> votes, ConfidenceProcedure.MotionVote projected)
        {
            bool repaint = Event.current.type == EventType.Repaint;
            float headH = V35.Px(28f), rowH = V35.Px(48f);
            Rect head = GUILayoutUtility.GetRect(width, headH, GUILayout.Width(width), GUILayout.Height(headH));
            GUIStyle headFace = V35Serif(18f, PoliSimTheme.TextPrimary);
            GUIStyle muted = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
            const string title = "In the Sejm", sub = "motions against the cabinet";
            float tw = Mathf.Ceil(headFace.CalcSize(new GUIContent(title)).x) + 2f;
            if (repaint)
            {
                GUI.Label(new Rect(head.x, head.y, tw, headH), title, headFace);
                GUI.Label(new Rect(head.x + tw + V35.Px(14f), head.y, head.width - tw - V35.Px(14f), headH), sub, muted);
                PoliSimTheme.Rule(new Rect(head.x, head.yMax - 1f, head.width, 1f), PoliSimTheme.HairlineStrong);
            }
            SlipAnchor(new Rect(head.x, head.y, tw, headH), "coal:sejm");
            _politicsSlipBook.Anchors["coal:sejm"] = new SlipContent("IN THE SEJM").Add("Art. 158: a constructive vote").Add(string.Format(CultureInfo.InvariantCulture, "{0} deputies move it", ConfidenceProcedure.SejmMovers))
                .Add(string.Format(CultureInfo.InvariantCulture, "{0} of the statutory {1} carry it", PresidentialVeto.StatutoryDeputies / 2 + 1, PresidentialVeto.StatutoryDeputies)).Add("Voted on the motion's day");
            int line = PresidentialVeto.StatutoryDeputies / 2 + 1;
            int i = 0;
            foreach (DivisionRecord vote in votes)
            {
                Rect row = GUILayoutUtility.GetRect(width, rowH, GUILayout.Width(width), GUILayout.Height(rowH));
                string mover = PolishDeskRecord.MoverOf(vote);
                int forVotes = PolishDeskRecord.ForOf(vote);
                string when = vote.Date.Date == today.Date ? "TODAY" : CoalitionVerbs.ShortDay(vote.Date).ToUpperInvariant();
                DrawSejmMotionRow(row, mover, g.PmParty, "Constructive vote · " + EnglishCandidate(mover), forVotes, line, (vote.Passed ? "PASSED · " : "FAILED · ") + when, false, PoliSimTheme.TextPrimary);
                string anchor = "coal:sejm:" + i.ToString(CultureInfo.InvariantCulture);
                SlipAnchor(row, anchor);
                _politicsSlipBook.Anchors[anchor] = new SlipContent("ART. 158 · " + DeskDay(vote.Date))
                    .Add(string.Format(CultureInfo.InvariantCulture, "{0}: {1} for", EnglishCandidate(mover), forVotes))
                    .Add(string.Format(CultureInfo.InvariantCulture, "{0} needed of the statutory {1}", line, PresidentialVeto.StatutoryDeputies))
                    .Add(PolishDeskRecord.StandingWords(c));
                if (repaint) { PoliSimTheme.Rule(new Rect(row.x, row.yMax - 1f, row.width, 1f), PoliSimTheme.RuleRow); }
                i++;
            }
            if (projected != null)
            {
                Rect row = GUILayoutUtility.GetRect(width, rowH, GUILayout.Width(width), GUILayout.Height(rowH));
                bool signed = projected.For >= ConfidenceProcedure.SejmMovers;
                bool mayMove = _simulationManager.CanMoveNoConfidence(PlayerCountryId, out string moveWhy);
                float actW = V35.Px(150f);
                var motion = new Rect(row.x, row.y, row.width - actW, row.height);
                DrawSejmMotionRow(motion, player, g.PmParty, "Constructive vote · your candidate", projected.For, line, "PREVIEW", true, PoliSimTheme.TextPrimary);
                SlipAnchor(motion, "coal:sejm:move");
                var names = new List<string>(); foreach (string k in projected.Governing.Count > 0 ? projected.Governing : projected.SuccessorCabinet) { names.Add(Short(k)); }
                var slip = new SlipContent("ART. 158 · YOUR MOTION")
                    .Add(string.Format(CultureInfo.InvariantCulture, "{0} of {1} deputies for", projected.For, projected.Members))
                    .Add(signed ? string.Format(CultureInfo.InvariantCulture, "It needs {0} of the statutory {1}", projected.Needed, PresidentialVeto.StatutoryDeputies)
                                : string.Format(CultureInfo.InvariantCulture, "Not yet a motion: {0} must move it", ConfidenceProcedure.SejmMovers))
                    .Add("Your government: " + PartySystems.JoinShortNames(names));
                if (signed && !projected.PartnersAccept) { slip.Add(projected.Carried ? "A partner refuses · you govern without it" : "A partner refuses its place"); }
                _politicsSlipBook.Anchors["coal:sejm:move"] = slip;
                float bx = row.xMax - actW + V35.Px(8f);
                var cell = new Rect(bx, row.y, actW - V35.Px(8f), row.height);
                if (DrawVerbButton(ref bx, cell, "c158", "MOVE ART. 158", false, !signed || !mayMove, out Rect move))
                {
                    if (!_simulationManager.MoveNoConfidence(PlayerCountryId, out string refused, out _)) { Debug.Log($"CONFIDENCE: the motion was refused - {refused}"); }
                    _constructiveProjection = null;
                }
                SlipAnchor(move, "coal:sejm:move/act");
                _politicsSlipBook.Anchors["coal:sejm:move/act"] = !mayMove ? SlipWrapped("MOVE ART. 158", moveWhy)
                    : !signed ? new SlipContent("MOVE ART. 158").Add(string.Format(CultureInfo.InvariantCulture, "{0} deputies must move it", ConfidenceProcedure.SejmMovers)).Add(string.Format(CultureInfo.InvariantCulture, "Your candidate has {0}", projected.For))
                    : new SlipContent("MOVE ART. 158").Add("The Sejm votes today").Add(string.Format(CultureInfo.InvariantCulture, "{0} needed", projected.Needed));
                if (repaint) { PoliSimTheme.Rule(new Rect(row.x, row.yMax - 1f, row.width, 1f), PoliSimTheme.RuleRow); }
                i++;
            }
            if (i == 0)
            {
                Rect row = GUILayoutUtility.GetRect(width, V35.Px(30f), GUILayout.Width(width), GUILayout.Height(V35.Px(30f)));
                if (repaint) { GUI.Label(row, "No motion this month", V35Serif(V35.Floor, PoliSimTheme.TextMuted)); }
            }
        }

        /// <summary>The opposition's motion "In the Sejm" draws - its projection (cached a day and government, as the Parliament row cached it), or null where the
        /// player is not in opposition, the government is a caretaker or the Sejm's formation runs (Art. 154, not Art. 158).</summary>
        private ConfidenceProcedure.MotionVote SejmMotionOf(Country c, GovernmentRecord g, string player, System.DateTime today)
        {
            if (player == null || g.RoleOf(player) != PlayerRole.Opposition || g.Caretaker || (g.Sejm != null && g.Sejm.Open)) { return null; }
            if (_constructiveProjection == null || _constructiveProjectedOn != today || !ReferenceEquals(_constructiveProjectedFor, g))
            {
                _constructiveProjection = _simulationManager.ProjectConstructiveVote(PlayerCountryId);
                _constructiveProjectedOn = today;
                _constructiveProjectedFor = g;
            }
            return _constructiveProjection;
        }

        /// <summary>"PiS's candidate" - a party's candidate, in the possessive the record's lines use.</summary>
        private string EnglishCandidate(string party) => string.IsNullOrEmpty(party) ? "the candidate" : PoliSim.Wording.EnglishText.Possessive(Short(party), false, true) + " candidate";

        /// <summary>One motion against the cabinet in the grammar: actor's mark → glyph → target's mark, its words, "Art. 158" muted, the bar of votes for with the
        /// line's tick, the figure, and the stamp at the right (dashed while a preview).</summary>
        private void DrawSejmMotionRow(Rect row, string actor, string target, string words, int forVotes, int line, string stamp, bool dashed, Color ink)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            float x = row.x, mark = V35.Px(22f), glyph = V35.Px(20f), gapIn = V35.Px(10f);
            float my = row.y + Mathf.Round((row.height - mark) * 0.5f);
            bool hasA = TryPartyOfKey(actor, out PoliticalParty a), hasB = TryPartyOfKey(target, out PoliticalParty b);
            if (hasA) { HemicycleRenderer.DrawMark(new Rect(x, my, mark, mark), PlayerCountryId, a); }
            x += mark + gapIn;
            DrawV35Icon(new Rect(x, row.y + Mathf.Round((row.height - glyph) * 0.5f), glyph, glyph), "verb:c158", ink);
            x += glyph + gapIn;
            if (hasB) { HemicycleRenderer.DrawMark(new Rect(x, my, mark, mark), PlayerCountryId, b); }
            x += mark + gapIn;
            GUIStyle face = V35Serif(17f, PoliSimTheme.TextPrimary);
            float ww = Mathf.Ceil(face.CalcSize(new GUIContent(words)).x) + 2f;
            GUI.Label(new Rect(x, row.y, ww, row.height), words, face);
            x += ww + gapIn;
            GUIStyle muted = V35Mono(V35.Floor, PoliSimTheme.TextMuted);
            const string art = "Art. 158";
            float aw = Mathf.Ceil(muted.CalcSize(new GUIContent(art)).x) + 2f;
            GUI.Label(new Rect(x, row.y, aw, row.height), art, muted);
            x += aw + V35.Px(24f);
            GUIStyle stampFace = V35Mono(V35.Floor, ink, true, TextAnchor.MiddleCenter);
            float sw = Mathf.Ceil(stampFace.CalcSize(new GUIContent(stamp)).x) + V35.Px(12f);
            var stampRect = new Rect(row.xMax - sw, row.y + Mathf.Round((row.height - V35.Px(22f)) * 0.5f), sw, V35.Px(22f));
            DrawBox(stampRect, ink, Mathf.Max(1f, Mathf.Round(V35.Px(1.5f))), dashed);
            GUI.Label(stampRect, stamp, stampFace);
            GUIStyle figFace = V35Mono(16f, PoliSimTheme.TextPrimary, true);
            string fig = forVotes.ToString(CultureInfo.InvariantCulture) + " / " + line.ToString(CultureInfo.InvariantCulture);
            float figW = Mathf.Ceil(figFace.CalcSize(new GUIContent(fig)).x) + 2f;
            float barW = Mathf.Min(V35.Px(300f), stampRect.x - V35.Px(16f) - figW - V35.Px(12f) - x);
            if (barW > V35.Px(60f))
            {
                var bar = new Rect(x, row.y + Mathf.Round((row.height - V35.Px(14f)) * 0.5f), barW, V35.Px(14f));
                int statutory = PresidentialVeto.StatutoryDeputies;
                PoliSimTheme.Rule(bar, PoliSimTheme.Tint(PoliSimTheme.RuleRow, 0.7f));
                PoliSimTheme.Rule(new Rect(bar.x, bar.y, Mathf.Round(bar.width * Mathf.Clamp01(forVotes / (float)statutory)), bar.height), PoliSimTheme.TextPrimary);
                float tx = bar.x + Mathf.Round(bar.width * line / (float)statutory) - 1f;
                PoliSimTheme.Rule(new Rect(tx, bar.y - V35.Px(4f), 2f, bar.height + V35.Px(8f)), PoliSimTheme.TextPrimary);
                x = bar.xMax + V35.Px(12f);
            }
            GUI.Label(new Rect(x, row.y, figW, row.height), fig, figFace);
        }

        /// <summary>
        /// COAL-2 (Design's "For Code" 3-4): THE COALITION VERBS ON THE DOCKET - every verb of the month as Information, led by its glyph: the player's own threat
        /// (its demand and day) and how it settled; a leaving (the player's or an AI partner's, K130) and a dismissal with the standing kept; an enforcement and how
        /// it settled; the sides re-declared after a count (K137's, one entry). Information never holds the clock (K163's idiom, as the Art. 158 votes are).
        /// True where any was drawn.
        /// </summary>
        private bool DrawCoalitionDocketItems()
        {
            if (_playerCountry?.Government == null || !CoalitionVerbs.Applies(PlayerCountryId)) { return false; }
            System.DateTime today = _simulationManager.CurrentDate;
            Color ink = UiPalette.GetAreaColor(UiPalette.SystemArea.Political);
            string player = _playerCountry.PlayerPartyAbbrev;
            bool any = false, sidesDrawn = false;
            List<CoalitionMotion> listed = CoalitionVerbs.Listed(_playerCountry, today);
            foreach (CoalitionMotion m in listed)
            {
                System.DateTime day = m.IsOpen ? m.On : (m.SettledOn != System.DateTime.MinValue ? m.SettledOn : m.On);
                string when = day.Date == today.Date ? "today" : DeskDay(day).ToLowerInvariant();
                string key = "coal:" + m.Verb + ":" + m.Actor + ":" + m.On.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                string who = m.Actor == player && !_playerCountry.PlayerHoldsPresidency ? "You" : Short(m.Actor);
                string glyph, title, line;
                var slip = new SlipContent(string.Empty);
                switch (m.Verb)
                {
                    case CoalitionVerb.Threaten:
                        glyph = m.State == MotionState.Carried ? "leave" : "threat";
                        title = m.IsOpen ? who + (who == "You" ? " threaten to leave · by " : " threatens to leave · by ") + CoalitionVerbs.ShortDay(m.Deadline) : m.State == MotionState.Met ? "Threat met · " + when : m.State == MotionState.Carried ? "Threat carried out · " + when : "Threat withdrawn · " + when;
                        line = "Demand · " + m.DemandName;
                        slip = new SlipContent((m.IsOpen ? "THREAT · BY " + DeskDay(m.Deadline) : "THREAT · " + DeskDay(day))).Add("Demand: " + m.DemandName).Add("To " + Short(m.Target));
                        if (!m.IsOpen) { slip.Add(SettledWords(m)); }
                        break;
                    case CoalitionVerb.Leave:
                        glyph = "leave";
                        title = (who == "You" ? "You left the government" : who + " has left") + " · " + when;
                        line = m.Outcome ?? string.Empty;
                        slip = new SlipContent(("Left the government · " + DeskDay(day)).ToUpperInvariant()).Add(Short(m.Actor) + " left " + UiFormat.Possessive(Short(m.Target), false, true) + " cabinet").Add(m.ByPlayer ? "Your verb" : "At the budget vote");
                        break;
                    case CoalitionVerb.Dismiss:
                        glyph = "dismiss";
                        title = Short(m.Target) + " dismissed · " + when;
                        line = m.Outcome ?? string.Empty;
                        slip = new SlipContent(("Dismissed · " + DeskDay(day)).ToUpperInvariant()).Add(Short(m.Actor) + " dismissed " + Short(m.Target)).Add("Its posts re-shared that day");
                        break;
                    case CoalitionVerb.Enforce:
                        glyph = "enforce";
                        title = m.IsOpen ? who + (who == "You" ? " enforce · due " : " enforces · due ") + CoalitionVerbs.ShortDay(m.Deadline) : m.State == MotionState.Met ? "Demand met · " + when : m.State == MotionState.Refused ? "Demand refused · " + when : "Demand lapsed · " + when;
                        line = "Point · " + m.DemandName;
                        slip = new SlipContent((m.IsOpen ? "ENFORCED · DUE " + DeskDay(m.Deadline) : "ENFORCED · " + DeskDay(day))).Add("Point: " + m.DemandName).Add("Owed by " + Short(m.Target));
                        if (!m.IsOpen) { slip.Add(SettledWords(m)); }
                        break;
                    default:
                        glyph = "redeclare";
                        if (m.ByPlayer)
                        {
                            title = "You re-declared " + m.Demand + " · " + when;
                            line = m.Demand + " · " + CabinetWords(m.DemandName);
                            slip = new SlipContent(("Re-declared · " + DeskDay(day)).ToUpperInvariant()).Add(m.Demand + " the government").Add(PlayerCountryId == CountryId.Sweden ? "Your bloc from that day" : "Recorded; no vote reads it");
                            break;
                        }
                        if (sidesDrawn) { continue; }
                        sidesDrawn = true;
                        key = "coal:sides:" + m.On.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                        var forSide = new List<string>(); var against = new List<string>();
                        foreach (CoalitionMotion s in listed) { if (s.Verb == CoalitionVerb.Redeclare && !s.ByPlayer && s.On.Date == m.On.Date) { (s.Demand == "FOR" ? forSide : against).Add(Short(s.Actor)); } }
                        title = "Sides re-declared · " + when;
                        line = "For " + forSide.Count.ToString(CultureInfo.InvariantCulture) + " · against " + against.Count.ToString(CultureInfo.InvariantCulture);
                        slip = new SlipContent(("Sides re-declared · " + DeskDay(day)).ToUpperInvariant()).Add("For: " + string.Join(" · ", forSide)).Add("Against: " + string.Join(" · ", against)).Add("After the count");
                        break;
                }
                slip.Add("Information · listed a month");
                _docketSlipBook.Anchors["docket:" + key] = slip;
                DrawDocketItem(key, "verb:" + glyph, ink, title, line, holdsTime: false, action: null, stampWords: "Information");
                any = true;
            }
            return any;
        }

        /// <summary>A settled motion's outcome in a slip's fragment, from its state (the manager's outcome line names parties by key, for the log).</summary>
        private static string SettledWords(CoalitionMotion m)
        {
            switch (m.State)
            {
                case MotionState.Met: return m.Verb == CoalitionVerb.Threaten ? "Delivered · the threat struck" : "Delivered · support goes on";
                case MotionState.Carried: return "Undelivered · the party left";
                case MotionState.Refused: return "Undelivered · broken";
                default: return "Withdrawn";
            }
        }
    }
}
