using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// PS-6 US-35 (`docs/specs/USA_STAGE_PLAN.md` US-35; D-US part two's board 30a, `Congress US v3.5.dc.html`, drawn by Code through the Design channel as a
    /// PROPOSAL and read as the go-ahead by Elias on 2026-10-23): <b>THE CONGRESS VIEW</b> - the Politics page's Congress tab for the USA, read top to bottom:
    /// <list type="number">
    /// <item><b>Who governs</b>: the President, the House and the Senate, each with its party's code on its ink, and an outlined DIVIDED block where the
    /// presidency's party holds fewer than both chambers (the block's line says which).</item>
    /// <item><b>The House</b>: its seats as one bar, the majority's side first, ticked at the majority and at the override's two thirds of those voting, and
    /// its provenance as a chip - the record's or the game's, the day it was elected, and the day a House the game has elected sits where it is still ahead.</item>
    /// <item><b>The Senate</b>: one bar ticked at the majority, cloture and the override, the caucus's bracket over it where independents sit with it, over its
    /// three classes - one cell a senator, each class dated and marked the record's or the game's, an independent a Neutral cell beside his caucus, never in
    /// its ink, a seat the game elected to an unexpired term edged in brass, a vacant seat an empty cell.</item>
    /// <item><b>The next vote</b>: its day and what it elects.</item>
    /// <item><b>The lines a vote is measured against</b>, as arithmetic on the day's seats and never a forecast: each against the majority, holding it or
    /// short by the seats it lacks; the Vice President's vote last.</item>
    /// <item><b>Before the Senate</b>: the Fed chair as the model holds him - no nomination is held as pending (the player-president names only where the count
    /// would confirm, US-29), so the card says so, with the chair's term, the holdover where it runs, and the count BILLED (K17's crossing rule is owed).</item>
    /// </list>
    /// Every figure is `UsCongressView`'s, read from the chambers the world seats. ⚠ <b>This file draws party ink</b> (the parties' codes, the bars, the
    /// cells and the role chip's mark ground) and NO area accent - `PartyInkDrawSiteCheck` allows it by file, with its argument. Until US-34 installs the REP
    /// and DEM inks, `PoliSimTheme.Party` returns the Neutral for both, and the codes on the bars tell them apart.
    /// </summary>
    public partial class GameController
    {
        /// <summary>A party's ink on the Congress view: its own (US-34 installs REP's and DEM's), the Neutral for an independent or a seat no side holds.</summary>
        private static Color CongressInk(string party) => string.IsNullOrEmpty(party) ? PoliSimTheme.Neutral : PoliSimTheme.Party(CountryId.USA, party);

        private static string CongressDay(System.DateTime d) => d == System.DateTime.MinValue ? "-" : d.ToString("d MMM yyyy", CultureInfo.InvariantCulture);

        private static string N(int n) => n.ToString(CultureInfo.InvariantCulture);

        /// <summary>The review's L8: the view read once a frame - OnGUI runs several events a frame, and the read sorts every seat - for this country and day.</summary>
        private UsCongressView.View _congressViewCache;
        private int _congressViewFrame = -1;
        private Country _congressViewCountry;
        private System.DateTime _congressViewDay;

        private UsCongressView.View CongressViewThisFrame()
        {
            System.DateTime today = _simulationManager.CurrentDate;
            if (_congressViewFrame != Time.frameCount || !ReferenceEquals(_congressViewCountry, _playerCountry) || _congressViewDay != today)
            {
                _congressViewCache = UsCongressView.Read(_playerCountry, today);
                _congressViewFrame = Time.frameCount;
                _congressViewCountry = _playerCountry;
                _congressViewDay = today;
            }
            return _congressViewCache;
        }

        /// <summary>
        /// US-35: the Congress view in <paramref name="width"/> - the strip, then the two columns (the chambers and the next vote at the left, the lines and what is
        /// before the Senate at the right). Returns false, drawing nothing, where the country seats no Congress (the generic chamber card stands).
        /// </summary>
        private bool DrawCongressV35(float width)
        {
            UsCongressView.View v = CongressViewThisFrame();
            if (v == null) { return false; }
            float gutter = V35.Px(V35.Gutter);
            DrawWhoGoverns(v, width);
            GUILayout.Space(gutter);
            float left = V35Span(width, 7), right = width - left - gutter;
            float leftH = CongressHouseHeight() + gutter + CongressSenateHeight() + gutter + V35.Px(26f);
            float rightH = CongressLinesHeight() + gutter + CongressNominationHeight();
            Rect area = GUILayoutUtility.GetRect(width, Mathf.Max(leftH, rightH), GUILayout.Width(width), GUILayout.Height(Mathf.Max(leftH, rightH)));
            float y = area.y;
            DrawCongressHouse(v, new Rect(area.x, y, left, CongressHouseHeight()));
            y += CongressHouseHeight() + gutter;
            DrawCongressSenate(v, new Rect(area.x, y, left, CongressSenateHeight()));
            y += CongressSenateHeight() + gutter;
            DrawCongressNextVote(v, new Rect(area.x, y, left, V35.Px(26f)));
            float rx = area.x + left + gutter;
            DrawCongressLines(v, new Rect(rx, area.y, right, CongressLinesHeight()));
            DrawCongressNomination(new Rect(rx, area.y + CongressLinesHeight() + gutter, right, CongressNominationHeight()));
            GUILayout.Space(gutter);
            return true;
        }

        // ------------------------------------------------------------------ who governs

        private void DrawWhoGoverns(UsCongressView.View v, float width)
        {
            float h = V35.Px(54f), gap = V35.Px(10f);
            // K39 (Elias, 2026-10-23): YOUR NOMINEE's row is retired under the view and its action moves here - a line under the three cards where the row would stand
            bool nominee = TryUsNomineeToday(out UsTermLimit.Nominee pick, out List<string> choices, out System.DateTime pollingDay, out bool own, out bool outParty);
            float nomineeH = nominee ? V35.Px(8f) + V35.Px(26f) : 0f;
            Rect row = GUILayoutUtility.GetRect(width, h + nomineeH, GUILayout.Width(width), GUILayout.Height(h + nomineeH));
            if (nominee) { DrawWhoGovernsNominee(new Rect(row.x + V35.Px(96f) + gap, row.y + h + V35.Px(8f), row.width - V35.Px(96f) - gap, V35.Px(26f)), pick, choices, pollingDay, own, outParty); }
            float labelW = V35.Px(96f), dividedW = v.Divided ? V35.Px(220f) : 0f;
            float cardW = (row.width - labelW - gap * (v.Divided ? 4f : 3f) - dividedW) / 3f;
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(row.x, row.y + V35.Px(8f), labelW, V35.Px(18f)), "WHO GOVERNS", V35Serif(V35.Floor, PoliSimTheme.TextMuted));
                PoliSimWidgets.MeasuredLabel(new Rect(row.x, row.y + V35.Px(28f), labelW, V35.Px(18f)), CongressDay(v.On).ToUpperInvariant(), V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true));
            }
            float x = row.x + labelW + gap;
            string Sh(string key) => key == null ? "NONE" : PartySystems.ShortName(CountryId.USA, key);
            int indMajority = v.IndependentsIn(v.SenateMajority);
            (string Label, string Party, string Code, string Line, string Id, SlipContent Slip)[] cards =
            {
                ("PRESIDENT", v.PresidentsParty, Sh(v.PresidentsParty), (v.President != null ? WorldClock.SurnameOf(v.President) : "-") + " · to " + CongressDay(v.PresidentUntil), "congress:president",
                    new SlipContent("THE PRESIDENT · " + (v.President ?? "-").ToUpperInvariant() + " · " + Sh(v.PresidentsParty))
                        .Add((v.PresidentOfGame ? "ELECTED IN THE GAME" : "OF RECORD, BY DATE") + " · TO NOON ON " + CongressDay(v.PresidentUntil).ToUpperInvariant())
                        .Add(v.VicePresident != null ? "THE VICE PRESIDENT: " + v.VicePresident.ToUpperInvariant() + (v.VicePresidentParty != null ? " (" + Sh(v.VicePresidentParty) + ")" : string.Empty) : "NO VICE PRESIDENT IS IN OFFICE")),
                ("HOUSE", v.HouseMajority, Sh(v.HouseMajority) + " " + N(v.HouseMajoritySeats), "of " + N(v.HouseSeated) + " · " + (v.HouseOfGame ? "the game's" : "the record's"), "congress:house",
                    new SlipContent("THE HOUSE · " + Sh(v.HouseMajority) + " " + N(v.HouseMajoritySeats) + " OF " + N(v.HouseSeated))
                        .Add((v.HouseOfGame ? "THE GAME'S, ELECTED " : "THE RECORD'S, ELECTED ") + CongressDay(v.HouseElectedOn).ToUpperInvariant() + " · SEATED " + CongressDay(v.HouseSeatedOn).ToUpperInvariant())),
                ("SENATE", v.SenateMajority, Sh(v.SenateMajority) + " " + N(v.SenateMajoritySeats - indMajority) + (indMajority > 0 ? " + " + N(indMajority) : string.Empty),
                    (indMajority > 0 ? "caucus " : "") + N(v.SenateMajoritySeats) + " of " + N(v.SenateSworn), "congress:senate",
                    new SlipContent("THE SENATE · " + Sh(v.SenateMajority) + " " + N(v.SenateMajoritySeats - indMajority) + (indMajority > 0 ? " AND " + N(indMajority) + " INDEPENDENT" + (indMajority == 1 ? "" : "S") : string.Empty))
                        .Add("A " + (indMajority > 0 ? "CAUCUS" : "MAJORITY") + " OF " + N(v.SenateMajoritySeats) + " OF THE " + N(v.SenateSworn) + " SWORN")
                        .Add("DATED BY CLASS - EACH CLASS ROW SAYS WHOSE IT IS")),
            };
            foreach (var c in cards)
            {
                var r = new Rect(x, row.y, cardW, h);
                Rect inner = DrawV35Card(r);
                if (Event.current.type == EventType.Repaint)
                {
                    float lh = V35.Px(18f);
                    PoliSimWidgets.MeasuredLabel(new Rect(inner.x, inner.y - V35.Px(4f), inner.width, lh), c.Label, V35Serif(V35.Floor, PoliSimTheme.TextMuted));
                    GUIStyle codeFace = V35Mono(V35.Floor, V35.OnDataDark, bold: true, anchor: TextAnchor.MiddleCenter);
                    float cw = Mathf.Ceil(codeFace.CalcSize(new GUIContent(c.Code)).x) + V35.Px(14f);
                    var code = new Rect(inner.x, inner.y + lh - V35.Px(2f), cw, V35.Px(22f));
                    PoliSimTheme.Rule(code, CongressInk(c.Party));
                    PoliSimWidgets.MeasuredLabel(code, c.Code, codeFace);
                    float lx = code.xMax + V35.Px(8f);
                    GUIStyle lineFace = V35Serif(V35.Floor + 1f, PoliSimTheme.TextPrimary);
                    PoliSimWidgets.MeasuredLabel(new Rect(lx, code.y, Mathf.Max(1f, inner.xMax - lx), code.height), V35Fit(c.Line, lineFace, Mathf.Max(1f, inner.xMax - lx), out _), lineFace);
                }
                SlipAnchor(r, c.Id);
                _politicsSlipBook.Anchors[c.Id] = c.Slip;
                x += cardW + gap;
            }
            if (v.Divided)
            {
                var r = new Rect(x, row.y, dividedW, h);
                if (Event.current.type == EventType.Repaint)
                {
                    CongressOutline(r, PoliSimTheme.TextPrimary, 2f);
                    float px = V35.Px(12f);
                    PoliSimWidgets.MeasuredLabel(new Rect(r.x + px, r.y + V35.Px(8f), r.width - 2f * px, V35.Px(18f)), "DIVIDED", V35Mono(V35.Floor + 1f, PoliSimTheme.TextPrimary, bold: true));
                    PoliSimWidgets.MeasuredLabel(new Rect(r.x + px, r.y + V35.Px(28f), r.width - 2f * px, V35.Px(18f)), DividedLine(v), V35Serif(V35.Floor, PoliSimTheme.TextPrimary));
                }
                SlipAnchor(r, "congress:divided");
                _politicsSlipBook.Anchors["congress:divided"] = new SlipContent("DIVIDED GOVERNMENT")
                    .Add("THE PRESIDENT " + Sh(v.PresidentsParty) + " · THE HOUSE " + Sh(v.HouseMajority) + " · THE SENATE " + Sh(v.SenateMajority))
                    .Add("ONE MARK WHEREVER THE ROLE IS SHOWN - THE PRESIDENCY'S PARTY HOLDS FEWER THAN BOTH CHAMBERS");
            }
        }

        /// <summary>DIVIDED's line: the presidency's party against the chamber(s) it does not hold.</summary>
        private static string DividedLine(UsCongressView.View v)
        {
            string Sh(string key) => key == null ? "-" : PartySystems.ShortName(CountryId.USA, key);
            string congress = v.HouseDivided && v.SenateDivided && v.HouseMajority == v.SenateMajority ? Sh(v.HouseMajority) + " Congress"
                : v.HouseDivided && v.SenateDivided ? Sh(v.HouseMajority) + " House · " + Sh(v.SenateMajority) + " Senate"
                : v.HouseDivided ? Sh(v.HouseMajority) + " House" : Sh(v.SenateMajority) + " Senate";
            return Sh(v.PresidentsParty) + " President · " + congress;
        }

        // ------------------------------------------------------------------ the House

        /// <summary>
        /// K39 (Elias, 2026-10-23: "YOUR NOMINEE's action moves into the who-governs strip"): THE NOMINEE'S LINE under the strip's three cards, in the next
        /// vote's grammar - YOUR NOMINEE, the party's mark, the person who stands and why (YOUR OWN, YOUR PICK, THE VICE PRESIDENT, THE PARTY'S OWN), the polling
        /// day in its outlined chip, and NEXT CHOICE at the right where the party has people to step through. The row it replaces stands only where no Congress
        /// view is drawn (`DrawUsNomineeRow`); the reading, the step and the slips are the row's own.
        /// </summary>
        private void DrawWhoGovernsNominee(Rect r, UsTermLimit.Nominee nominee, List<string> choices, System.DateTime day, bool own, bool outParty)
        {
            string party = _playerCountry.PlayerPartyAbbrev;
            bool stepping = !own && choices != null && choices.Count > 1;
            GUIStyle label = V35Serif(V35.Floor, PoliSimTheme.TextMuted);
            GUIStyle nameFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true);
            GUIStyle chipFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleCenter);
            const string next = "NEXT CHOICE";
            float nextW = stepping ? Mathf.Ceil(chipFace.CalcSize(new GUIContent(next)).x) + V35.Px(20f) : 0f;
            var nextChip = new Rect(r.xMax - nextW, r.y, nextW, r.height);
            float x = r.x;
            string tag = UsNomineeTag(nominee, own);
            string dayText = CongressDay(day).ToUpperInvariant();
            float lw = Mathf.Ceil(label.CalcSize(new GUIContent("YOUR NOMINEE")).x) + V35.Px(10f);
            float side = V35.Px(20f);
            float dayW = Mathf.Ceil(chipFace.CalcSize(new GUIContent(dayText)).x) + V35.Px(12f);
            float tagW = Mathf.Ceil(label.CalcSize(new GUIContent(tag)).x) + V35.Px(10f);
            float limit = stepping ? nextChip.x - V35.Px(10f) : r.xMax;
            float nameBudget = Mathf.Max(V35.Px(16f), limit - x - lw - side - V35.Px(8f) - tagW - dayW - V35.Px(10f));
            string name = V35Fit(Surname(nominee.Person).ToUpperInvariant(), nameFace, nameBudget, out bool _);
            float nameW = Mathf.Min(nameBudget, Mathf.Ceil(nameFace.CalcSize(new GUIContent(name)).x) + V35.Px(10f));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(x, r.y, lw, r.height), "YOUR NOMINEE", label);
            }
            x += lw;
            DrawPartyMarkSlot(new Rect(x, r.y + Mathf.Round((r.height - side) * 0.5f), side, side), CountryId.USA, party);
            x += side + V35.Px(8f);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(x, r.y, nameW, r.height), name, nameFace);
                PoliSimWidgets.MeasuredLabel(new Rect(x + nameW, r.y, tagW, r.height), tag, label);
                var chip = new Rect(x + nameW + tagW, r.y + V35.Px(2f), dayW, r.height - V35.Px(4f));
                CongressOutline(chip, PoliSimTheme.TextPrimary, 1.5f);
                PoliSimWidgets.MeasuredLabel(chip, dayText, chipFace);
            }
            SlipAnchor(new Rect(r.x, r.y, Mathf.Max(1f, x + nameW + tagW + dayW - r.x), r.height), "congress:nominee");
            _politicsSlipBook.Anchors["congress:nominee"] = UsNomineeSlip(nominee, choices, day, own, outParty);
            if (!stepping) { return; }
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.RoundedCard(nextChip, PoliSimTheme.StockOff, PoliSimTheme.HairlineStrong, 0f);
                PoliSimWidgets.MeasuredLabel(nextChip, next, chipFace);
            }
            if (PoliSimWidgets.Button(nextChip, GUIContent.none, GUIStyle.none)) { StepUsNominee(nominee, choices, day, party); }
            SlipAnchor(nextChip, "congress:nominee:next");
            _politicsSlipBook.Anchors["congress:nominee:next"] = UsNomineeNextSlip(day);
        }

        private float CongressHouseHeight() => V35.Px(V35.CardPadY) * 2f + V35.Px(V35.CardIcon) + V35.Px(8f) + V35.Px(56f);

        private void DrawCongressHouse(UsCongressView.View v, Rect r)
        {
            Rect inner = DrawV35Card(r);
            float headH = V35.Px(V35.CardIcon);
            string chip = (v.HouseOfGame ? "GAME'S" : "RECORD'S") + " · ELECTED " + CongressDay(v.HouseElectedOn).ToUpperInvariant()
                          + (v.HouseSitsUntil != System.DateTime.MinValue ? " · SITS TO " + CongressDay(v.HouseSitsUntil).ToUpperInvariant() : string.Empty);
            DrawCongressHead(new Rect(inner.x, inner.y, inner.width, headH), v.HouseSeated, "House seats", chip, "congress:house:chip",
                new SlipContent(v.HouseOfGame ? "THE GAME'S HOUSE" : "THE RECORD'S HOUSE")
                    .Add("ELECTED " + CongressDay(v.HouseElectedOn).ToUpperInvariant() + " · SEATED " + CongressDay(v.HouseSeatedOn).ToUpperInvariant())
                    .Add(v.HouseSitsUntil != System.DateTime.MinValue ? "THE HOUSE THE GAME ELECTED SITS FROM " + CongressDay(v.HouseSitsUntil).ToUpperInvariant() + " - THE OLD HOUSE SITS UNTIL THEN"
                        : "THE NEXT HOUSE IS ELECTED ON " + CongressDay(v.NextVote).ToUpperInvariant()));
            var bar = new Rect(inner.x, inner.y + headH + V35.Px(8f), inner.width, V35.Px(30f));
            var segs = new List<(string Party, bool Independent, int Seats)>();
            if (v.HouseMajority != null) { segs.Add((v.HouseMajority, false, v.HouseMajoritySeats)); }
            foreach (KeyValuePair<string, int> kv in v.House) { if (kv.Key != v.HouseMajority && kv.Value > 0) { segs.Add((kv.Key, false, kv.Value)); } }
            DrawCongressBar(bar, segs, v.HouseSeated, "congress:house:bar");
            DrawCongressTick(bar, v.HouseMajorityLine.Figure, v.HouseSeated, N(v.HouseMajorityLine.Figure), "congress:house:majority",
                new SlipContent("MAJORITY · " + N(v.HouseMajorityLine.Figure) + " OF " + N(v.HouseSeated)).Add(HoldsWords(v.HouseMajorityLine)));
            DrawCongressTick(bar, v.HouseOverride.Figure, v.HouseSeated, N(v.HouseOverride.Figure), "congress:house:override",
                new SlipContent("OVERRIDE · TWO THIRDS OF THE " + N(v.HouseSeated) + " VOTING").Add(N(v.HouseOverride.Figure) + " · " + HoldsWords(v.HouseOverride)));
        }

        // ------------------------------------------------------------------ the Senate

        private float CongressClassRowHeight() => V35.Px(18f) + V35.Px(4f) + V35.Px(18f) + V35.Px(8f);

        private float CongressSenateHeight() => V35.Px(V35.CardPadY) * 2f + V35.Px(V35.CardIcon) + V35.Px(8f) + V35.Px(20f) + V35.Px(56f) + V35.Px(6f) + 3f * CongressClassRowHeight();

        private void DrawCongressSenate(UsCongressView.View v, Rect r)
        {
            Rect inner = DrawV35Card(r);
            float headH = V35.Px(V35.CardIcon);
            DrawCongressHead(new Rect(inner.x, inner.y, inner.width, headH), v.SenateSworn, "Senate seats", "BY CLASS · DATED PER ROW", "congress:senate:chip",
                new SlipContent("THE SENATE · DATED BY CLASS").Add("EACH CLASS IS ELECTED ON ITS OWN DAY, ONE EVERY SECOND YEAR").Add("A ROW SAYS WHOSE IT IS: THE RECORD'S OR THE GAME'S"));
            float y = inner.y + headH + V35.Px(8f);
            string first = v.SenateMajority;
            string other = null;
            foreach (KeyValuePair<string, int> kv in v.Senate) { if (kv.Key != first && kv.Value > 0) { other = kv.Key; } }
            int indFirst = v.IndependentsIn(first), indOther = v.IndependentsIn(other);
            var segs = new List<(string Party, bool Independent, int Seats)>();
            if (first != null) { segs.Add((first, false, v.SenateMajoritySeats - indFirst)); if (indFirst > 0) { segs.Add((first, true, indFirst)); } }
            if (other != null) { if (indOther > 0) { segs.Add((other, true, indOther)); } segs.Add((other, false, SeatsIn(v.Senate, other) - indOther)); }
            float members = Mathf.Max(1, v.SenateSworn);
            if (indFirst > 0 && Event.current.type == EventType.Repaint)
            {
                // the caucus's bracket: the majority's party and the independents who sit with it
                float bw = inner.width * v.SenateMajoritySeats / members;
                var bracket = new Rect(inner.x, y, bw, V35.Px(18f));
                PoliSimTheme.Rule(new Rect(bracket.x, bracket.yMax, bracket.width, 2f), PoliSimTheme.HairlineStrong);
                PoliSimWidgets.MeasuredLabel(bracket, "caucus with " + PartySystems.ShortName(CountryId.USA, first) + " · " + N(v.SenateMajoritySeats), V35Serif(V35.Floor, PoliSimTheme.TextPrimary, TextAnchor.MiddleCenter));
            }
            if (indFirst > 0)
            {
                SlipAnchor(new Rect(inner.x, y, inner.width * v.SenateMajoritySeats / members, V35.Px(20f)), "congress:senate:caucus");
                _politicsSlipBook.Anchors["congress:senate:caucus"] = new SlipContent("THE CAUCUS · " + N(v.SenateMajoritySeats))
                    .Add(PartySystems.ShortName(CountryId.USA, first) + " " + N(v.SenateMajoritySeats - indFirst) + " AND " + N(indFirst) + " INDEPENDENT" + (indFirst == 1 ? "" : "S"))
                    .Add("EACH INDEPENDENT IS COUNTED WITH THE CAUCUS HE SITS WITH AND DRAWN IN THE NEUTRAL, NEVER IN ITS INK");
            }
            y += V35.Px(20f);
            var bar = new Rect(inner.x, y, inner.width, V35.Px(30f));
            DrawCongressBar(bar, segs, v.SenateSworn, "congress:senate:bar");
            DrawCongressTick(bar, v.SenateMajorityLine.Figure, v.SenateSworn, N(v.SenateMajorityLine.Figure), "congress:senate:majority",
                new SlipContent("MAJORITY · " + N(v.SenateMajorityLine.Figure) + " OF " + N(v.SenateSworn)).Add("OR HALF AND THE VICE PRESIDENT'S VOTE").Add(HoldsWords(v.SenateMajorityLine)));
            DrawCongressTick(bar, v.Cloture.Figure, v.SenateSworn, N(v.Cloture.Figure), "congress:senate:cloture",
                new SlipContent("CLOTURE · THREE FIFTHS OF THE " + N(v.SenateSworn) + " SWORN").Add(N(v.Cloture.Figure) + " · " + HoldsWords(v.Cloture)).Add("A VACANCY LOWERS IT · RECONCILIATION NEEDS NONE"));
            DrawCongressTick(bar, v.SenateOverride.Figure, v.SenateSworn, N(v.SenateOverride.Figure), "congress:senate:override",
                new SlipContent("OVERRIDE · TWO THIRDS OF THE " + N(v.SenateSworn) + " VOTING").Add(N(v.SenateOverride.Figure) + " · " + HoldsWords(v.SenateOverride)));
            y += V35.Px(56f) + V35.Px(6f);
            foreach (UsCongressView.ClassRow row in v.Classes)
            {
                DrawCongressClass(row, new Rect(inner.x, y, inner.width, CongressClassRowHeight()));
                y += CongressClassRowHeight();
            }
        }

        private static int SeatsIn(IReadOnlyDictionary<string, int> seats, string party) => party != null && seats != null && seats.TryGetValue(party, out int n) ? n : 0;

        private void DrawCongressClass(UsCongressView.ClassRow row, Rect r)
        {
            string id = "congress:class:" + N(row.Class);
            int specials = 0;
            foreach (UsCongressView.Seat s in row.Seats) { if (s.Special) { specials++; } }
            float lh = V35.Px(18f);
            if (Event.current.type == EventType.Repaint)
            {
                GUIStyle name = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
                name.fontStyle = FontStyle.Bold;
                string head = "Class " + N(row.Class);
                float nw = Mathf.Ceil(name.CalcSize(new GUIContent(head)).x) + V35.Px(10f);
                PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, nw, lh), head, name);
                string mark = row.Game ? "GAME'S" : "RECORD'S";
                GUIStyle markFace = V35Mono(V35.Floor, row.Game ? PoliSimTheme.Brass : PoliSimTheme.TextMuted, bold: true, anchor: TextAnchor.MiddleRight);
                float mw = Mathf.Ceil(markFace.CalcSize(new GUIContent(mark)).x) + 2f;
                PoliSimWidgets.MeasuredLabel(new Rect(r.xMax - mw, r.y, mw, lh), mark, markFace);
                string when = "elected " + CongressDay(row.ElectedOn) + " · up " + CongressDay(row.UpOn) + (specials > 0 ? " · " + N(specials) + " special" + (specials == 1 ? "" : "s") + " the game's" : string.Empty);
                GUIStyle whenFace = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
                float wx = r.x + nw, ww = Mathf.Max(1f, r.xMax - mw - V35.Px(10f) - wx);
                PoliSimWidgets.MeasuredLabel(new Rect(wx, r.y, ww, lh), V35Fit(when, whenFace, ww, out _), whenFace);

                // one cell a senator, in the caucus order
                int n = Mathf.Max(1, row.Seats.Count);
                float gap = V35.Px(3f);
                float cell = Mathf.Min(V35.Px(13f), Mathf.Floor((r.width - gap * (n - 1)) / n));
                float cy = r.y + lh + V35.Px(4f), ch = V35.Px(18f), cx = r.x;
                foreach (UsCongressView.Seat s in row.Seats)
                {
                    var c = new Rect(cx, cy, cell, ch);
                    if (s.Side == null) { CongressOutline(c, PoliSimTheme.HairlineStrong, 1f); }
                    else
                    {
                        PoliSimTheme.Rule(c, s.Independent ? PoliSimTheme.Neutral : CongressInk(s.Side));
                        if (s.Special) { CongressOutline(c, V35.Brass, 2f); }
                    }
                    cx += cell + gap;
                }
            }
            SlipAnchor(new Rect(r.x, r.y, r.width, r.height - V35.Px(8f)), id);
            var slip = new SlipContent("CLASS " + N(row.Class) + " · " + (row.Game ? "THE GAME'S" : "THE RECORD'S") + " · ELECTED " + CongressDay(row.ElectedOn).ToUpperInvariant());
            var counts = new List<string>();
            foreach (string side in new[] { "DEM", "REP" })
            {
                int seats = row.Of(side, false), ind = row.Of(side, true);
                if (seats > 0) { counts.Add(PartySystems.ShortName(CountryId.USA, side) + " " + N(seats)); }
                if (ind > 0) { counts.Add("IND " + N(ind) + " (WITH " + PartySystems.ShortName(CountryId.USA, side) + ")"); }
            }
            if (row.Vacant > 0) { counts.Add(N(row.Vacant) + " VACANT"); }
            slip.Add(string.Join(" · ", counts)).Add("UP ON " + CongressDay(row.UpOn).ToUpperInvariant());
            if (specials > 0) { slip.Add("EDGED IN BRASS: " + N(specials) + " SEAT" + (specials == 1 ? "" : "S") + " THE GAME ELECTED TO AN UNEXPIRED TERM"); }
            _politicsSlipBook.Anchors[id] = slip;
        }

        private void DrawCongressNextVote(UsCongressView.View v, Rect r)
        {
            if (v.NextVote == System.DateTime.MinValue) { return; }
            string what = (v.NextMidterm ? "" : "the President, ") + "all " + N(v.HouseSeated) + ", class " + N(v.NextClass) + " (" + N(v.NextSenateSeats) + ")";
            if (Event.current.type == EventType.Repaint)
            {
                GUIStyle label = V35Serif(V35.Floor, PoliSimTheme.TextMuted);
                float lw = Mathf.Ceil(label.CalcSize(new GUIContent("NEXT VOTE")).x) + V35.Px(10f);
                PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, lw, r.height), "NEXT VOTE", label);
                GUIStyle chipFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleCenter);
                string day = CongressDay(v.NextVote).ToUpperInvariant();
                float cw = Mathf.Ceil(chipFace.CalcSize(new GUIContent(day)).x) + V35.Px(12f);
                var chip = new Rect(r.x + lw, r.y + V35.Px(2f), cw, r.height - V35.Px(4f));
                CongressOutline(chip, PoliSimTheme.TextPrimary, 1.5f);
                PoliSimWidgets.MeasuredLabel(chip, day, chipFace);
                float wx = chip.xMax + V35.Px(10f);
                GUIStyle whatFace = V35Serif(V35.Floor + 1f, PoliSimTheme.TextPrimary);
                PoliSimWidgets.MeasuredLabel(new Rect(wx, r.y, Mathf.Max(1f, r.xMax - wx), r.height), V35Fit(what, whatFace, Mathf.Max(1f, r.xMax - wx), out _), whatFace);
            }
            SlipAnchor(r, "congress:next");
            _politicsSlipBook.Anchors["congress:next"] = new SlipContent("NEXT VOTE · " + CongressDay(v.NextVote).ToUpperInvariant())
                .Add(UsCongressElection.ContestsWords(v.NextVote))
                .Add("CLASS " + N(v.NextClass) + ": " + N(v.NextSenateSeats) + " SEATS UP, THE SPECIALS OF RECORD INCLUDED");
        }

        // ------------------------------------------------------------------ the lines

        private float CongressLinesRow() => V35.Px(44f);

        private float CongressLinesHeight() => V35.Px(10f) * 2f + V35.Px(22f) + 6f * CongressLinesRow();

        private void DrawCongressLines(UsCongressView.View v, Rect r)
        {
            Rect card = r;
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(card, V35.CardPaper);
                CongressOutline(card, V35.CardEdge, 1f);
            }
            float px = V35.Px(V35.CardPadX), py = V35.Px(10f);
            var inner = new Rect(r.x + px, r.y + py, r.width - 2f * px, r.height - 2f * py);
            DrawV35SectionHead(new Rect(inner.x, inner.y, inner.width, V35.Px(20f)), "The lines a vote is measured against");
            SlipAnchor(new Rect(inner.x, inner.y, inner.width, V35.Px(20f)), "congress:lines");
            _politicsSlipBook.Anchors["congress:lines"] = new SlipContent("THE LINES A VOTE IS MEASURED AGAINST")
                .Add("ARITHMETIC ON THE DAY'S SEATS, NEVER A FORECAST")
                .Add("CLOTURE COUNTS THE SENATORS SWORN, SO A VACANCY LOWERS IT; AN OVERRIDE COUNTS THOSE VOTING, EVERY MEMBER SEATED (NO ABSENCE IS MODELLED); RECONCILIATION NEEDS NO CLOTURE");
            string Sh(string key) => key == null ? "-" : PartySystems.ShortName(CountryId.USA, key);
            int ind = v.IndependentsIn(v.SenateMajority);
            string Senate(UsCongressView.Line l) => Sh(l.Holder) + (ind > 0 ? " caucus " : " ") + N(l.Held) + (l.ByVicePresident ? " holds it with the VP" : l.Holds ? " holds it" : " · " + N(l.Short) + " short");
            string vp = v.VicePresident != null ? WorldClock.SurnameOf(v.VicePresident) + (v.VicePresidentParty != null ? " (" + Sh(v.VicePresidentParty) + ")" : "") + " breaks it" : "no Vice President in office";
            (string Name, string Figure, string Sub, string Who, string Id)[] rows =
            {
                ("Majority · House", N(v.HouseMajorityLine.Figure), "of " + N(v.HouseSeated), Sh(v.HouseMajorityLine.Holder) + " " + N(v.HouseMajorityLine.Held) + (v.HouseMajorityLine.Holds ? " holds it" : " · " + N(v.HouseMajorityLine.Short) + " short"), "congress:line:house"),
                ("Majority · Senate", N(v.SenateMajorityLine.Figure), "of " + N(v.SenateSworn) + ", or half and the VP", Senate(v.SenateMajorityLine), "congress:line:senate"),
                ("Cloture", N(v.Cloture.Figure), "three fifths of the " + N(v.SenateSworn) + " sworn", Senate(v.Cloture), "congress:line:cloture"),
                ("Override · House", N(v.HouseOverride.Figure), "two thirds of the " + N(v.HouseSeated) + " voting",
                    Sh(v.HouseOverride.Holder) + " " + N(v.HouseOverride.Held) + (v.HouseOverride.Holds ? " holds it" : " · " + N(v.HouseOverride.Short) + " short"), "congress:line:override:house"),
                ("Override · Senate", N(v.SenateOverride.Figure), "two thirds of the " + N(v.SenateSworn) + " voting", Senate(v.SenateOverride), "congress:line:override:senate"),
                ("The VP's vote", v.SenateSworn % 2 == 0 ? N(v.SenateSworn / 2) + "–" + N(v.SenateSworn / 2) : "-", v.SenateSworn % 2 == 0 ? "an equal division only" : "no equal division of " + N(v.SenateSworn) + " sworn", vp,   // the review's L3: an odd Senate cannot divide equally
                "congress:line:vp"),
            };
            float y = inner.y + V35.Px(22f);
            float nameW = V35.Px(130f), figW = V35.Px(80f), gap = V35.Px(10f);
            foreach (var row in rows)
            {
                var rr = new Rect(inner.x, y, inner.width, CongressLinesRow());
                if (Event.current.type == EventType.Repaint)
                {
                    PoliSimTheme.Rule(new Rect(rr.x, rr.yMax - 1f, rr.width, 1f), V35.ListRule);
                    PoliSimWidgets.MeasuredLabel(new Rect(rr.x, rr.y + V35.Px(4f), nameW, V35.Px(20f)), row.Name, V35Serif(V35.Floor + 1f, PoliSimTheme.TextPrimary));
                    PoliSimWidgets.MeasuredLabel(new Rect(rr.x + nameW, rr.y + V35.Px(2f), figW, V35.Px(24f)), row.Figure, V35Mono(18f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleRight));
                    float tx = rr.x + nameW + figW + gap, tw = Mathf.Max(1f, rr.xMax - tx);
                    GUIStyle sub = V35Serif(V35.Floor, PoliSimTheme.TextMuted), who = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
                    PoliSimWidgets.MeasuredLabel(new Rect(tx, rr.y + V35.Px(2f), tw, V35.Px(19f)), V35Fit(row.Sub, sub, tw, out _), sub);
                    PoliSimWidgets.MeasuredLabel(new Rect(tx, rr.y + V35.Px(21f), tw, V35.Px(19f)), V35Fit(row.Who, who, tw, out _), who);
                }
                SlipAnchor(rr, row.Id);
                _politicsSlipBook.Anchors[row.Id] = new SlipContent(row.Name.ToUpperInvariant() + " · " + row.Figure).Add(row.Sub.ToUpperInvariant()).Add(row.Who.ToUpperInvariant());
                y += CongressLinesRow();
            }
        }

        private static string HoldsWords(UsCongressView.Line l) =>
            l.Holder == null ? "NO PARTY HOLDS A MAJORITY" : PartySystems.ShortName(CountryId.USA, l.Holder) + " " + N(l.Held) + (l.ByVicePresident ? " HOLDS IT WITH THE VICE PRESIDENT'S VOTE" : l.Holds ? " HOLDS IT" : " · " + N(l.Short) + " SHORT");

        // ------------------------------------------------------------------ before the Senate (the Fed chair, as the model holds him)

        private List<(string Text, bool Billed)> CongressNominationLines()
        {
            var lines = new List<(string, bool)>();
            FedChair chair = _playerCountry.CurrentFedChair;
            System.DateTime today = _simulationManager.CurrentDate;
            if (chair != null)
            {
                lines.Add(("Chair of the " + GetCentralBankName(PlayerCountryId) + ": " + chair.Name + (chair.HasTerm() ? ", the term as chair to " + CongressDay(chair.TermEnds) + "." : "."), false));
                if (UsFedConsent.HoldoverWords(_playerCountry, chair, today) != null)
                {
                    lines.Add(("His term has ended; he serves on until a successor is confirmed, the game's reading.", false));
                }
            }
            lines.Add(("The President nominates; the Senate confirms by a majority.", false));
            lines.Add(("The count by a crossing rule on the chair's roll calls is billed.", true));
            return lines;
        }

        private float CongressNominationHeight() => V35.Px(10f) * 2f + V35.Px(22f) + V35.Px(24f) + CongressNominationLines().Count * V35.Px(20f);

        private void DrawCongressNomination(Rect r)
        {
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(r, V35.CardPaper);
                CongressOutline(r, V35.CardEdge, 1f);
            }
            float px = V35.Px(V35.CardPadX), py = V35.Px(10f);
            var inner = new Rect(r.x + px, r.y + py, r.width - 2f * px, r.height - 2f * py);
            DrawV35SectionHead(new Rect(inner.x, inner.y, inner.width, V35.Px(20f)), "Before the Senate");
            List<(string Text, bool Billed)> lines = CongressNominationLines();
            float y = inner.y + V35.Px(22f);
            if (Event.current.type == EventType.Repaint)
            {
                GUIStyle title = V35Serif(17f, PoliSimTheme.TextPrimary);
                title.fontStyle = FontStyle.Bold;
                PoliSimWidgets.MeasuredLabel(new Rect(inner.x, y, inner.width, V35.Px(22f)), "No nomination is before the Senate", title);
            }
            y += V35.Px(24f);
            var slip = new SlipContent("BEFORE THE SENATE").Add("NO NOMINATION IS HELD AS PENDING: THE PLAYER-PRESIDENT NAMES ONLY WHERE THE COUNT WOULD CONFIRM, AND AN AI PRESIDENT'S NOMINEE IS COUNTED ON THE TERM'S DAY");
            foreach ((string text, bool billed) in lines)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    float gx = inner.x;
                    if (billed)
                    {
                        float side = V35.Px(18f);
                        DrawStateGlyph(new Rect(gx, y + V35.Px(1f), side, side), Symbol.Billed, PoliSimTheme.TextPrimary);
                        gx += side + V35.Px(6f);
                    }
                    GUIStyle face = V35Serif(V35.Floor, PoliSimTheme.TextPrimary);
                    PoliSimWidgets.MeasuredLabel(new Rect(gx, y, Mathf.Max(1f, inner.xMax - gx), V35.Px(20f)), V35Fit(text, face, Mathf.Max(1f, inner.xMax - gx), out _), face);
                }
                slip.Add(text.ToUpperInvariant());
                y += V35.Px(20f);
            }
            slip.Add("BILLED: THE VICE CHAIR ACTING WHILE NO SUCCESSOR IS CONFIRMED; A NOMINATION UNDER A HOSTILE SENATE, ITS REFUSAL SHOWN AS AN EVENT");
            SlipAnchor(r, "congress:nomination");
            _politicsSlipBook.Anchors["congress:nomination"] = slip;
        }

        // ------------------------------------------------------------------ the parts

        /// <summary>A card head: the seats as the figure, the chamber's name, and an outlined provenance chip at the right with its slip.</summary>
        private void DrawCongressHead(Rect head, int seats, string name, string chip, string chipId, SlipContent chipSlip)
        {
            GUIStyle chipFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleCenter);
            float cw = Mathf.Ceil(chipFace.CalcSize(new GUIContent(chip)).x) + V35.Px(12f);
            var chipRect = new Rect(head.xMax - cw, head.y + Mathf.Round((head.height - V35.Px(22f)) * 0.5f), cw, V35.Px(22f));
            if (Event.current.type == EventType.Repaint)
            {
                GUIStyle figure = V35Mono(26f, PoliSimTheme.TextPrimary, bold: true);
                string text = N(seats);
                float fw = Mathf.Ceil(figure.CalcSize(new GUIContent(text)).x) + 2f;
                GUI.Label(new Rect(head.x, head.y, fw, head.height), text, figure);
                float nx = head.x + fw + V35.Px(10f);
                PoliSimWidgets.MeasuredLabel(new Rect(nx, head.y, Mathf.Max(1f, chipRect.x - V35.Px(8f) - nx), head.height), name, V35Serif(17f, PoliSimTheme.TextPrimary));
                CongressOutline(chipRect, PoliSimTheme.TextPrimary, 1.5f);
                PoliSimWidgets.MeasuredLabel(chipRect, chip, chipFace);
            }
            SlipAnchor(chipRect, chipId);
            _politicsSlipBook.Anchors[chipId] = chipSlip;
        }

        /// <summary>A chamber's seats as one bar: each segment its party's ink (an independent's the Neutral), its code on it where the code fits, its slip.</summary>
        private void DrawCongressBar(Rect bar, List<(string Party, bool Independent, int Seats)> segs, int total, string id)
        {
            float x = bar.x, of = Mathf.Max(1, total);
            var slip = new SlipContent("THE SEATS · " + N(total));
            foreach ((string party, bool independent, int seats) in segs)
            {
                if (seats <= 0) { continue; }
                float w = bar.width * seats / of;
                var seg = new Rect(x, bar.y, w, bar.height);
                string code = independent ? "I" : PartySystems.ShortName(CountryId.USA, party) + " " + N(seats);
                if (Event.current.type == EventType.Repaint)
                {
                    PoliSimTheme.Rule(seg, independent ? PoliSimTheme.Neutral : CongressInk(party));
                    PoliSimTheme.Rule(new Rect(seg.xMax - 1f, seg.y, 1f, seg.height), V35.CardPaper);
                    GUIStyle face = V35Mono(V35.Floor, V35.OnDataDark, bold: true, anchor: TextAnchor.MiddleCenter);
                    if (face.CalcSize(new GUIContent(code)).x + 4f <= w) { PoliSimWidgets.MeasuredLabel(seg, code, face); }
                }
                slip.Add((independent ? "INDEPENDENTS " + N(seats) + ", SITTING WITH " + PartySystems.ShortName(CountryId.USA, party) : PartySystems.ShortName(CountryId.USA, party) + " " + N(seats)).ToUpperInvariant());
                x += w;
            }
            SlipAnchor(bar, id);
            _politicsSlipBook.Anchors[id] = slip;
        }

        /// <summary>A line's tick through a bar at <paramref name="at"/> of <paramref name="total"/>, its figure under it, its slip.</summary>
        private void DrawCongressTick(Rect bar, int at, int total, string label, string id, SlipContent slip)
        {
            float tx = bar.x + bar.width * at / Mathf.Max(1, total);
            var tick = new Rect(Mathf.Round(tx - 1.5f), bar.y - V35.Px(6f), 3f, bar.height + V35.Px(12f));
            var under = new Rect(tx - V35.Px(30f), tick.yMax + V35.Px(1f), V35.Px(60f), V35.Px(18f));   // real905: the floor's figures measure 17.5 tall - 16 clipped them
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(tick, PoliSimTheme.TextPrimary);
                PoliSimWidgets.MeasuredLabel(under, label, V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleCenter));
            }
            SlipAnchor(new Rect(tick.x - V35.Px(4f), tick.y, tick.width + V35.Px(8f), tick.height + under.height), id);
            _politicsSlipBook.Anchors[id] = slip;
        }

        private static void CongressOutline(Rect r, Color ink, float w)
        {
            PoliSimTheme.Rule(new Rect(r.x, r.y, r.width, w), ink);
            PoliSimTheme.Rule(new Rect(r.x, r.yMax - w, r.width, w), ink);
            PoliSimTheme.Rule(new Rect(r.x, r.y, w, r.height), ink);
            PoliSimTheme.Rule(new Rect(r.xMax - w, r.y, w, r.height), ink);
        }

        /// <summary>US-35 (board 30c, answer 5): the role chip's mark on its party's ink square - the chip's party identity, drawn here because this file may draw
        /// party ink (`PartyInkDrawSiteCheck`); the Desk's masthead calls it.</summary>
        private void DrawRoleMarkOnInk(Rect square, CountryId country, string key)
        {
            if (Event.current.type != EventType.Repaint || string.IsNullOrEmpty(key)) { return; }
            PoliSimTheme.Rule(square, PoliSimTheme.Party(country, key));
            float inset = Mathf.Max(1f, Mathf.Round(square.width * 0.12f));
            var mark = new Rect(square.x + inset, square.y + inset, square.width - 2f * inset, square.height - 2f * inset);
            Texture2D emblem = null;
            foreach (PoliticalParty party in PartySystems.For(country)) { if (party.Abbrev == key) { emblem = IconLibrary.GetPartyMark(party.MarkName); break; } }
            if (emblem == null) { return; }
            Color before = GUI.color;
            GUI.color = V35.OnDataDark;
            GUI.DrawTexture(mark, emblem, ScaleMode.ScaleToFit, true);
            GUI.color = before;
        }
    }
}
