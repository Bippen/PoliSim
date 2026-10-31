using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// S4 (Design's return of 2026-10-30): THE HQ'S PARTY IDENTITY - the race (each party's bar in its ink, the presidential first round by candidate with
    /// the top-two rule, the run-off as one head-to-head bar of its two finalists) and the GROUND (the tile map, a polled region in its leader's ink, and
    /// the region's plate). Who leads the race and who leads where is party identity as the whole subject, the chamber's own argument, so this file is on
    /// `PartyInkDrawSiteCheck`'s allow-list; it draws NO area accent (clause 2 by construction).
    ///
    /// <para><b>Only a regional poll colours a tile</b> (Design's "the map's three live states"): not polled is paper in a dashed rule; polled is the
    /// leader's ink with its mark and the signed lead; too close (the lead no larger than its own ±) is paper in an ink rule with ≈. The national poll never
    /// colours a tile. Colour never carries meaning alone: a polled tile names its leader by its mark (its word where the glyph is not installed).</para>
    /// </summary>
    public partial class GameController
    {
        /// <summary>The parties a poll carries - none for the empty poll a campaign holds before its first is fielded (its arrays are null).</summary>
        private static int HqPollCount(Poll p) => p.House == null ? 0 : p.PartyCount;

        /// <summary>A party's ink on the HQ - the chamber's laddered accessor, so the race, the map and the hemicycle cannot disagree about a colour.</summary>
        private Color HqInk(string abbrev) => PoliSimTheme.PartyLaddered(PlayerCountryId, abbrev);

        // ------------------------------------------------------------------------------------------
        // The race (94, 131) 330 wide.
        // ------------------------------------------------------------------------------------------

        /// <summary>The race block in <paramref name="r"/> (its rows' pitch shrinks to the room, never below 20 px at 1280); returns the y under it.</summary>
        private float DrawHqRace(Rect r, CampaignSnapshot s, PeopleSlips.Book book)
        {
            CampaignContest contest = HqContest(s);
            CampaignPollSource source = s.Hq != null ? s.Hq.PollSource : s.LatestPoll.SampleSize > 0 ? CampaignPollSource.PublicTracker : CampaignPollSource.LastCount;
            float y = r.y;
            var head = new Rect(r.x, y, r.width, HqH(26f));
            string title = contest == CampaignContest.PresidentialRunOff ? "The run-off" : contest == CampaignContest.PresidentialFirstVote ? "The candidates" : "The race";
            GUIStyle head18 = HqSerif(18f, PoliSimTheme.TextPrimary);
            HqLabel(new Rect(head.x, head.y, HqWidth(title, head18), head.height), title, head18);
            string stamp = source == CampaignPollSource.OwnPoll ? "YOUR POLL" : source == CampaignPollSource.PublicTracker ? "PUBLIC TRACKER" : "THE LAST COUNT · " + SymbolRegistry.Word(Symbol.Dated);
            HqStamp(head, stamp, PoliSimTheme.TextPrimary, right: true);
            y += head.height + HqH(4f);

            // who is on the ballot: the parties (or the candidates' parties) standing - nothing at 0.0 that does not stand (bug 2)
            var rows = new List<int>();
            for (int i = 0; i < HqPollCount(s.LatestPoll) && i < s.PartyNames.Length; i++)
            {
                bool stands = s.Hq != null ? s.Hq.Stands(i) : s.LatestPoll.Share(i) > 0.0;
                if (stands) { rows.Add(i); }
            }
            double top = 0.01;
            foreach (int i in rows) { top = System.Math.Max(top, s.LatestPoll.Share(i) + s.LatestPoll.MarginOfErrorPp(i) / 100.0); }
            double scale = System.Math.Min(1.0, System.Math.Ceiling(top * 20.0) / 20.0);   // the bars run to the next 5 %, named in the head's slip
            SlipAnchor(head, "hq:racehead");
            var headSlip = new SlipContent(stamp);
            if (source == CampaignPollSource.LastCount)
            {
                headSlip.Add("the last count, carried as the reading").Add("no sampling error, so no bands").Add("shares among the parties standing");
            }
            else
            {
                headSlip.Add("n " + HqN(s.LatestPoll.SampleSize) + " · fielded " + HqDate(s.LatestPoll.FieldDate)).Add("the rule under a bar · its 95 % interval").Add("sampling error only");
            }
            headSlip.Add(string.Format(CultureInfo.InvariantCulture, "bars to {0:F0} %", scale * 100.0));
            book.Anchors["hq:racehead"] = headSlip;

            if (contest == CampaignContest.PresidentialRunOff && rows.Count == 2) { return DrawHqRunOff(new Rect(r.x, y, r.width, r.yMax - y), s, rows, book); }
            if (contest == CampaignContest.PresidentialFirstVote) { rows.Sort((a, b) => s.LatestPoll.Share(b).CompareTo(s.LatestPoll.Share(a))); }   // the top two above the run-off's rule

            double tvPp = s.Hq?.TelevisionPp ?? 0.0;
            bool effect = tvPp > 0.0 && s.Hq.TelevisionUntil.HasValue;
            float divider = contest == CampaignContest.PresidentialFirstVote && rows.Count > 2 ? HqH(22f) : 0f;
            float extra = (effect ? HqH(20f) : 0f) + divider;
            float pitch = rows.Count > 0 ? Mathf.Clamp((r.yMax - y - extra) / rows.Count, HqH(20f), HqH(26f)) : HqH(26f);
            GUIStyle code = HqMono(14f, PoliSimTheme.TextPrimary);
            GUIStyle codeBold = HqMono(14f, PoliSimTheme.TextPrimary, bold: true);
            float codeW = HqW(42f);
            foreach (int i in rows) { codeW = Mathf.Max(codeW, HqWidth(HqShort(s.PartyNames[i]), codeBold) + V35.Px(2f)); }
            float barW = Mathf.Max(HqW(60f), HqW(118f) - (codeW - HqW(42f)));
            GUIStyle share = HqMono(14f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleRight);
            GUIStyle moe = HqMono(14f, PoliSimTheme.TextMuted);
            for (int k = 0; k < rows.Count; k++)
            {
                int i = rows[k];
                if (y + pitch > r.yMax + 0.5f) { break; }
                string key = s.PartyNames[i];
                double sh = s.LatestPoll.Share(i), e = s.LatestPoll.MarginOfErrorPp(i) / 100.0;
                bool player = i == s.PlayerPartyIndex;
                HqFill(new Rect(r.x, y, r.width, HqT(1f)), PoliSimTheme.RuleRow);
                var row = new Rect(r.x, y, r.width, pitch);
                float x = row.x;
                float markSide = V35.Px(16f);
                Texture2D mark = IconLibrary.GetPartyMark(HqMarkKey(key));
                if (mark != null && Event.current.type == EventType.Repaint) { HqTintedMark(new Rect(x, row.y + Mathf.Round((row.height - markSide) * 0.5f), markSide, markSide), mark, key, HqMarkKey(key)); }
                x += markSide + HqW(6f);
                HqLabel(new Rect(x, row.y, codeW, row.height), HqShort(key), player ? codeBold : code);
                x += codeW + HqW(6f);
                var track = new Rect(x, row.y + Mathf.Round((row.height - V35.Px(14f)) * 0.5f) + V35.Px(1f), barW, V35.Px(8f));
                HqFill(track, HqTrack);
                HqFill(new Rect(track.x, track.y, Mathf.Round(track.width * Mathf.Clamp01((float)(sh / scale))), track.height), HqInk(key));
                if (e > 0.0)
                {
                    float lo = Mathf.Clamp01((float)((sh - e) / scale)), hi = Mathf.Clamp01((float)((sh + e) / scale));
                    HqFill(new Rect(track.x + track.width * lo, track.y + V35.Px(10f), Mathf.Max(HqT(2f), track.width * (hi - lo)), HqT(3f)), PoliSimTheme.HairlineStrong);
                }
                x += barW + HqW(6f);
                HqLabel(new Rect(x, row.y, HqW(38f), row.height), HqF(100.0 * sh, 1), share);
                x += HqW(38f) + HqW(6f);
                HqLabel(new Rect(x, row.y, HqW(38f), row.height), "±" + (e > 0.0 ? HqF(100.0 * e, 1) : "0"), moe);
                x += HqW(38f) + HqW(6f);
                double mom = s.MomentumPp != null && i < s.MomentumPp.Length ? s.MomentumPp[i] : 0.0;
                if (System.Math.Abs(mom) >= 0.05)
                {
                    string m = (mom > 0 ? "▲" : "▼") + HqF(System.Math.Abs(mom), 1);
                    GUIStyle momFace = HqMono(14f, mom > 0 ? PoliSimTheme.Good : PoliSimTheme.Bad, bold: true, anchor: TextAnchor.MiddleRight);
                    HqLabel(new Rect(row.xMax - HqWidth(m, momFace), row.y, HqWidth(m, momFace), row.height), m, momFace);
                }
                string id = "hq:party:" + i.ToString(CultureInfo.InvariantCulture);
                SlipAnchor(row, id);
                var slip = new SlipContent(HqShort(key) + " · " + HqF(100.0 * sh, 1) + " %");
                slip.Add(e > 0.0 ? "95 % interval " + HqF(100.0 * System.Math.Max(0.0, sh - e), 1) + " to " + HqF(100.0 * (sh + e), 1) : "the count · no sampling error");
                if (System.Math.Abs(mom) >= 0.05) { slip.Add((mom > 0 ? "+" : "−") + HqF(System.Math.Abs(mom), 1) + " pp momentum since the last poll"); }
                if (contest == CampaignContest.PresidentialFirstVote) { slip.Add("the candidate of " + HqShort(key)); }
                if (player) { slip.Add("your party"); }
                book.Anchors[id] = slip;
                y += pitch;

                if (player && effect)
                {
                    // K67's run-up television effect under the party's own row
                    string line = "▲" + HqF(tvPp, 2) + " ±" + HqF(s.Hq.TelevisionBandPp, 2) + " · TV buy · " + HqDay(s.Hq.TelevisionUntil.Value);
                    var effectRect = new Rect(r.x + HqW(22f), y, r.width - HqW(22f), HqH(20f));
                    HqLabel(effectRect, line, HqMono(14f, PoliSimTheme.Good));
                    SlipAnchor(effectRect, "hq:tv");
                    book.Anchors["hq:tv"] = new SlipContent("TV BUY · ▲" + HqF(tvPp, 2) + " PP").Add("the buy's sourced effect while it airs").Add("±" + HqF(s.Hq.TelevisionBandPp, 2) + " · the estimate's 95 % band").Add("airs until " + HqDate(s.Hq.TelevisionUntil.Value) + " · gone by polling day");
                    y += effectRect.height;
                }
                if (divider > 0f && k == 1)
                {
                    // the first round: the top two go to the run-off
                    var div = new Rect(r.x, y, r.width, divider);
                    HqDashedLine(new Rect(div.x, div.y, div.width, HqT(1.5f)), PoliSimTheme.TextPrimary);
                    TwoRoundElection.Rule rule = TwoRoundElection.RuleOf(PlayerCountryId);
                    System.DateTime runOff = s.Calendar.ElectionDate.AddDays(rule != null ? rule.RunOffDay : 14);
                    string text = "▴ top two · run-off " + HqDay(runOff);
                    HqLabel(div, text, HqMono(14f, PoliSimTheme.TextSecondary));
                    SlipAnchor(div, "hq:toptwo");
                    book.Anchors["hq:toptwo"] = new SlipContent("THE RUN-OFF · " + HqDate(runOff).ToUpperInvariant()).Add("the top two of the first round meet").Add("unless one wins a majority outright");
                    y += divider;
                }
            }
            return y;
        }

        /// <summary>A dashed horizontal rule.</summary>
        private static void HqDashedLine(Rect r, Color ink)
        {
            if (Event.current.type != EventType.Repaint) { return; }
            float dash = Mathf.Max(3f, 4f * V35.Scale), gap = Mathf.Max(2f, 3f * V35.Scale);
            for (float x = r.x; x < r.xMax; x += dash + gap) { PoliSimTheme.Rule(new Rect(x, r.y, Mathf.Min(dash, r.xMax - x), r.height), ink); }
        }

        /// <summary>The run-off's head-to-head (board 4g): the two finalists only - their marks, codes and shares, one bar split at the share, the 50 tick, the
        /// leader's 95 % interval as a bracket, ≈ TOO CLOSE while it crosses 50, the momentum, and no debate (ruled).</summary>
        private float DrawHqRunOff(Rect r, CampaignSnapshot s, List<int> two, PeopleSlips.Book book)
        {
            int a = two[0], b = two[1];
            if (s.LatestPoll.Share(b) > s.LatestPoll.Share(a)) { int t = a; a = b; b = t; }
            double sa = s.LatestPoll.Share(a), sb = s.LatestPoll.Share(b), sum = sa + sb;
            double shareA = sum > 0.0 ? sa / sum : 0.5;
            double ea = s.LatestPoll.MarginOfErrorPp(a) / 100.0;
            string ka = s.PartyNames[a], kb = s.PartyNames[b];
            float y = r.y + HqH(2f);
            HqFill(new Rect(r.x, y, r.width, HqT(1f)), PoliSimTheme.RuleRow);
            var names = new Rect(r.x, y, r.width, HqH(34f));
            float markSide = V35.Px(18f);
            GUIStyle code16 = HqMono(16f, PoliSimTheme.TextPrimary, bold: true);
            GUIStyle share24 = HqMono(24f, PoliSimTheme.TextPrimary, bold: true);
            float x = names.x;
            Texture2D ma = IconLibrary.GetPartyMark(HqMarkKey(ka)), mb = IconLibrary.GetPartyMark(HqMarkKey(kb));
            if (ma != null && Event.current.type == EventType.Repaint) { HqTintedMark(new Rect(x, names.y + Mathf.Round((names.height - markSide) * 0.5f), markSide, markSide), ma, ka, HqMarkKey(ka)); }
            x += markSide + V35.Px(8f);
            string ca = HqShort(ka), cb = HqShort(kb), pa = HqF(100.0 * shareA, 1), pb = HqF(100.0 * (1.0 - shareA), 1);
            HqLabel(new Rect(x, names.y, HqWidth(ca, code16), names.height), ca, code16);
            x += HqWidth(ca, code16) + V35.Px(8f);
            HqLabel(new Rect(x, names.y, HqWidth(pa, share24), names.height), pa, share24);
            float rx = names.xMax;
            if (mb != null && Event.current.type == EventType.Repaint) { HqTintedMark(new Rect(rx - markSide, names.y + Mathf.Round((names.height - markSide) * 0.5f), markSide, markSide), mb, kb, HqMarkKey(kb)); }
            rx -= markSide + V35.Px(8f);
            HqLabel(new Rect(rx - HqWidth(cb, code16), names.y, HqWidth(cb, code16), names.height), cb, code16);
            rx -= HqWidth(cb, code16) + V35.Px(8f);
            HqLabel(new Rect(rx - HqWidth(pb, share24), names.y, HqWidth(pb, share24), names.height), pb, share24);
            y += names.height + HqH(4f);

            var bar = new Rect(r.x, y, r.width, HqH(24f));
            float split = Mathf.Round(bar.width * (float)shareA);
            HqFill(new Rect(bar.x, bar.y, Mathf.Max(0f, split - 1f), bar.height), HqInk(ka));
            HqFill(new Rect(bar.x + split, bar.y, bar.width - split, bar.height), HqInk(kb));
            float mid = bar.x + Mathf.Round(bar.width * 0.5f);
            HqFill(new Rect(mid - HqT(1f), bar.y - HqH(5f), HqT(2f), HqH(34f)), PoliSimTheme.TextPrimary);
            y += bar.height;

            // the leader's interval as a bracket under the split, 50 under the tick
            double lo = System.Math.Max(0.0, shareA - ea), hi = System.Math.Min(1.0, shareA + ea);
            var bracket = new Rect(r.x, y, r.width, HqH(30f));
            float bl = bracket.x + Mathf.Round(bracket.width * (float)lo), bh = bracket.x + Mathf.Round(bracket.width * (float)hi);
            HqFill(new Rect(bl, bracket.y + HqH(5f), Mathf.Max(1f, bh - bl), HqT(3f)), PoliSimTheme.TextPrimary);
            HqFill(new Rect(bl, bracket.y + HqH(1f), HqT(1f), HqH(11f)), PoliSimTheme.TextPrimary);
            HqFill(new Rect(bh - HqT(1f), bracket.y + HqH(1f), HqT(1f), HqH(11f)), PoliSimTheme.TextPrimary);
            HqLabel(new Rect(mid - HqW(30f), bracket.y + HqH(13f), HqW(60f), HqH(16f)), "50", HqMono(14f, PoliSimTheme.TextPrimary, anchor: TextAnchor.MiddleCenter));
            y += bracket.height;

            bool close = lo < 0.5 && hi > 0.5;
            var callRow = new Rect(r.x, y, r.width, HqH(30f));
            string band = ca + " " + HqF(100.0 * lo, 1) + "–" + HqF(100.0 * hi, 1);
            float bx = callRow.x;
            if (close) { bx = HqStamp(callRow, "≈ TOO CLOSE", PoliSimTheme.Caution).xMax + V35.Px(8f); }
            HqLabel(new Rect(bx, callRow.y, Mathf.Max(1f, callRow.xMax - bx), callRow.height), band, HqMono(14f, PoliSimTheme.TextMuted));
            y += callRow.height;

            var momRow = new Rect(r.x, y, r.width, HqH(24f));
            double ma2 = s.MomentumPp != null && a < s.MomentumPp.Length ? s.MomentumPp[a] : 0.0, mb2 = s.MomentumPp != null && b < s.MomentumPp.Length ? s.MomentumPp[b] : 0.0;
            int moving = System.Math.Abs(mb2) > System.Math.Abs(ma2) ? b : a;
            double mom = moving == a ? ma2 : mb2;
            if (System.Math.Abs(mom) >= 0.05)
            {
                string m = (mom > 0 ? "▲" : "▼") + HqF(System.Math.Abs(mom), 1) + " " + HqShort(s.PartyNames[moving]);
                HqLabel(new Rect(momRow.x, momRow.y, HqWidth(m, HqMono(14f, PoliSimTheme.Good, bold: true)), momRow.height), m, HqMono(14f, mom > 0 ? PoliSimTheme.Good : PoliSimTheme.Bad, bold: true));
            }
            const string noDebate = "no debate in the run-off";
            GUIStyle muted = HqMono(14f, PoliSimTheme.TextMuted, anchor: TextAnchor.MiddleRight);
            HqLabel(new Rect(momRow.xMax - HqWidth(noDebate, muted), momRow.y, HqWidth(noDebate, muted), momRow.height), noDebate, muted);
            y += momRow.height;

            var whole = new Rect(r.x, r.y, r.width, y - r.y);
            SlipAnchor(whole, "hq:runoff");
            book.Anchors["hq:runoff"] = new SlipContent("THE RUN-OFF · " + ca + " AGAINST " + cb)
                .Add(ca + " " + pa + " · " + cb + " " + pb + " · of the two")
                .Add(band + " · " + UiFormat.Possessive(ca) + " 95 % interval")
                .Add(close ? "it crosses 50 · too close to call" : "clear of 50")
                .Add("the other parties are not on the ballot");
            return y;
        }

        // ------------------------------------------------------------------------------------------
        // The ground (444, 131) 400 wide: the map's head, the tiles, the legend, the region's plate.
        // ------------------------------------------------------------------------------------------

        /// <summary>The ground the map draws: the live reading's, else a staged film's - its own offices on the country's tiles, every region unread.</summary>
        private static CampaignGround HqGroundOf(CampaignSnapshot s, CampaignTiles.TileMap map)
        {
            if (s.Hq?.Ground != null) { return s.Hq.Ground; }
            if (map == null) { return null; }
            var names = new string[map.Tiles.Length];
            var readings = new MapRegionReading[names.Length];
            var price = new double[names.Length];
            var marks = new OfficeMark[names.Length];
            var volunteers = new int[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = map.Key == "pl_sejm" ? "okreg " + map.Tiles[i].Id : map.Key == "us_states" ? map.Tiles[i].Id : map.Tiles[i].Label;
                readings[i] = SwingRegions.Unknown(names[i], 0.0);
                price[i] = double.NaN;
                volunteers[i] = -1;
                if (s.Offices != null) { foreach (RegionalOffice o in s.Offices) { if (o.RegionName == names[i]) { marks[i] = OfficeMark.Open; volunteers[i] = o.Volunteers; } } }
            }
            return new CampaignGround(names, readings, price, marks, volunteers, 0.0, -1, false, false, false, 0, 0.0, null, null, null);
        }

        /// <summary>Whether the map is the office picker now: the run-up with OFFICE open in the composer.</summary>
        private bool HqSiting(CampaignSnapshot s) => HqRunUp(s) && _hqComposer.HasValue && _hqComposer.Value == CampaignActionKind.EstablishOffice;

        private void DrawHqGround(CampaignSnapshot s, PeopleSlips.Book book)
        {
            // S7: an independent's run-up draws the 51 as its ballot drive (`GameController.CampaignBallot.cs`) - the map's subject is the ballot, not a poll
            UsBallotAccess.Drive ballot = HqBallotDrive(s);
            if (ballot != null) { DrawHqBallotGround(s, ballot, book); return; }
            CampaignContest contest = HqContest(s);
            string[] names = s.Hq?.Ground?.Names;
            CampaignTiles.TileMap map = CampaignTiles.For(PlayerCountryId, contest, names);
            CampaignGround ground = HqGroundOf(s, map);
            bool runUp = HqRunUp(s), siting = HqSiting(s);
            bool live = HqLive(out bool _);

            // the head: the regions, and the map's mode
            var head = HqR(444f, 131f, 400f, 26f);
            int regionCount = ground?.Names.Length ?? map?.Tiles.Length ?? 0;
            string unit = PlayerCountryId == CountryId.Poland ? (contest == CampaignContest.Chamber ? "okręgi" : "województwa")
                : PlayerCountryId == CountryId.Sweden ? "valkretsar" : PlayerCountryId == CountryId.Germany ? "Länder" : PlayerCountryId == CountryId.USA ? null : "regions";
            string title = "The " + regionCount.ToString(CultureInfo.InvariantCulture) + (unit != null ? " " + unit : string.Empty);   // S7: the USA's is Design's "The 51" - the District is no state
            GUIStyle head18 = HqSerif(18f, PoliSimTheme.TextPrimary);
            HqLabel(new Rect(head.x, head.y, HqWidth(title, head18), head.height), title, head18);
            int target = ground?.Target ?? -1;
            string targetName = target >= 0 && ground != null ? CampaignTiles.Display(ground.Names[target]).ToUpperInvariant() : "—";
            int offices = 0;
            if (ground != null) { foreach (OfficeMark m in ground.Offices) { if (m != OfficeMark.None) { offices++; } } }
            GUIStyle modeFace = HqMono(14f, siting ? PoliSimTheme.BrassBorder : PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleRight);
            string targetId = target >= 0 && ground != null ? (HqRegionForms(s, ground.Names[target])[2] ?? targetName) : targetName;
            string modeLead = siting ? "NEXT OFFICE · " : "TARGET · ";
            string mode = runUp && !siting ? "OFFICES · " + offices.ToString(CultureInfo.InvariantCulture)
                : HqFirstFitting(modeFace, Mathf.Max(1f, head.width - HqWidth(title, head18) - V35.Px(12f)), modeLead + targetName, modeLead + targetId.ToUpperInvariant());
            var modeRect = new Rect(head.xMax - HqWidth(mode, modeFace), head.y, HqWidth(mode, modeFace), head.height);
            HqLabel(modeRect, mode, modeFace);
            SlipAnchor(modeRect, "hq:mode");
            book.Anchors["hq:mode"] = siting
                ? new SlipContent("NEXT OFFICE · " + targetName).Add("where it opens · a tile picked moves it").Add(ground != null && ground.TargetPicked ? "your pick" : "the largest electorate without one")
                : runUp ? new SlipContent("OFFICES · " + offices.ToString(CultureInfo.InvariantCulture)).Add("no local acts in the run-up").Add("OFFICE turns the map into the picker")
                : new SlipContent("TARGET · " + targetName).Add("local acts land here · rally, town hall, doors, phones").Add(ground != null && ground.TargetPicked ? "your pick · a tile picked moves it" : "your strongest office · a tile picked moves it");

            if (map == null || ground == null)
            {
                // a country Design placed no tiles for (S7 drew the USA's) - the one-region rule places local acts
                var none = HqR(444f, 163f, 400f, 26f);
                HqLabel(none, "no tile map yet", HqSerif(15f, PoliSimTheme.TextSecondary));
                SlipAnchor(none, "hq:nomap");
                book.Anchors["hq:nomap"] = new SlipContent("NO TILE MAP").Add("no tiles placed for this country").Add("local acts: your strongest office");
                DrawHqPlate(HqR(444f, 199f, 400f, 166f), s, ground, map, siting, book);
                return;
            }

            // the tiles, at the board's px - one unit both ways, so a tile stays square
            float u = Mathf.Min(_hqUx, _hqUy);
            float side = Mathf.Round(map.TileSize * u), step = (map.TileSize + map.Gap) * u;
            float ox = Mathf.Round(HqX(map.OriginX)), oy = Mathf.Round(HqY(Mathf.Max(map.OriginY, HqMapTop)));   // S7: under the head (Design placed the US map on a page with no timeline)
            bool small = map.TileSize < 40f;
            var mapRect = new Rect(ox, oy, map.Width * u, map.Height * u);
            bool ballotMap = PlayerCountryId == CountryId.Germany && HqBallotMap(s);
            foreach (CampaignTiles.Tile tile in map.Tiles)
            {
                int region = CampaignTiles.RegionOf(map, tile, ground.Names);
                var t = new Rect(ox + Mathf.Round(tile.Column * step), oy + Mathf.Round(tile.Row * step), side, side);
                if (ballotMap)
                {
                    // SP-5 (S5, board 5c): the Länder in their ballot states; a tile pressed is where the next GATHER goes
                    DrawHqBallotTile(t, tile, region, s, book);
                    if (live && region >= 0 && PoliSimWidgets.Button(t, GUIContent.none, GUIStyle.none)) { _hqBallotLand = region; }
                    continue;
                }
                DrawHqTile(t, tile, map, region, ground, s, siting, small, book);
                bool clickable = live && region >= 0 && (!runUp || siting);
                if (clickable && PoliSimWidgets.Button(t, GUIContent.none, GUIStyle.none)) { PickCampaignRegion(region); }
            }

            // the legend, and the plate: under the map where it is wide, beside it where it is narrow (Sweden's tall valkretsar)
            bool beside = map.Width < 200f;
            Rect legendArea = beside ? HqR(624f, 163f + map.Height + 10f, 220f, 60f) : new Rect(Mathf.Round(HqX(444f)), mapRect.yMax + HqH(8f), HqW(400f), HqH(40f));
            float legendBottom = ballotMap ? DrawHqBallotLegend(legendArea, s) : DrawHqLegend(legendArea, ground, s, siting, book);
            Rect plate = beside ? HqR(624f, 163f, 220f, map.Height)
                : new Rect(Mathf.Round(HqX(444f)), legendBottom + HqH(10f), HqW(400f), Mathf.Max(HqH(110f), _hqPage.yMax - legendBottom - HqH(10f)));
            if (beside && legendArea.y > plate.yMax) { }
            DrawHqPlate(plate, s, ground, map, siting, book);
        }

        /// <summary>One tile in its live state: not polled, polled (the leader's ink, its mark, the lead), too close (≈); the office square, the target ring,
        /// the next office.</summary>
        private void DrawHqTile(Rect t, CampaignTiles.Tile tile, CampaignTiles.TileMap map, int region, CampaignGround ground, CampaignSnapshot s, bool siting, bool small, PeopleSlips.Book book)
        {
            MapRegionReading reading = region >= 0 && region < ground.Readings.Length ? ground.Readings[region] : SwingRegions.Unknown(tile.Label, 0.0);
            RegionalPolling.MapState state = RegionalPolling.StateOf(reading);
            bool next = siting && region >= 0 && region == ground.Target;
            bool target = !HqRunUp(s) && region >= 0 && region == ground.Target;
            Color numberInk = PoliSimTheme.TextPrimary;
            string leaderKey = state == RegionalPolling.MapState.Polled && reading.Leader >= 0 && reading.Leader < s.PartyNames.Length ? s.PartyNames[reading.Leader] : null;
            if (target && Event.current.type == EventType.Repaint)
            {
                float g = HqT(2f);
                PoliSimTheme.Rule(new Rect(t.x - 2f * g, t.y - 2f * g, t.width + 4f * g, t.height + 4f * g), PoliSimTheme.TextPrimary);   // the ring
                PoliSimTheme.Rule(new Rect(t.x - g, t.y - g, t.width + 2f * g, t.height + 2f * g), PoliSimTheme.Card);   // the paper gap
            }
            if (next) { HqFill(t, V35.SlipPaper); HqEdge(t, PoliSimTheme.Brass, HqT(2f)); }
            else if (leaderKey != null) { HqFill(t, HqInk(leaderKey)); numberInk = PoliSimTheme.ModalBackground; }
            else if (state == RegionalPolling.MapState.TooCloseToCall) { HqFill(t, PoliSimTheme.Card); HqEdge(t, PoliSimTheme.TextPrimary, HqT(1.5f)); }
            else { HqFill(t, PoliSimTheme.Card); HqDashed(t, PoliSimTheme.HairlineStrong, HqT(1.5f)); }

            float pad = V35.Px(4f), line = V35.Px(16f);
            GUIStyle number = HqMono(14f, numberInk, bold: true, anchor: map.LabelTopLeft ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter);
            var numberRect = map.LabelTopLeft ? new Rect(t.x + pad, t.y + V35.Px(3f), t.width - 2f * pad, line) : new Rect(t.x, t.y + (small ? V35.Px(7f) : V35.Px(3f)), t.width, line);
            bool us = map.Key == "us_states";
            if (us) { numberRect = new Rect(t.x, t.y + V35.Px(1f), t.width, V35.Px(15f)); }   // S7 (7a): the code over the electors, 15 px lines
            HqLabel(numberRect, tile.Id, number);
            if (us && state != RegionalPolling.MapState.TooCloseToCall && !next)
            {
                int electors = UsElectorsOf(tile.Id, s.Calendar.ElectionDate);
                string ev = electors > 0 ? electors.ToString(CultureInfo.InvariantCulture) : "—";
                var evRect = leaderKey != null ? new Rect(t.x + V35.Px(2f), t.y + V35.Px(16f), t.width - V35.Px(17f), V35.Px(15f)) : new Rect(t.x, t.y + V35.Px(16f), t.width, V35.Px(15f));
                HqLabel(evRect, ev, HqMono(14f, numberInk, anchor: leaderKey != null ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter));
            }

            if (leaderKey != null)
            {
                // the leader's mark (its word where the glyph is not installed) and, where both fit, the signed lead
                float markSide = V35.Px(14f);
                Texture2D glyph = HqPartyGlyph(leaderKey);
                string lead = "+" + HqF(reading.GapPp, 0);
                GUIStyle paper = HqMono(14f, PoliSimTheme.ModalBackground, bold: true);
                var bottomRow = new Rect(t.x + pad, t.yMax - V35.Px(3f) - line, t.width - 2f * pad, line);
                if (glyph != null)
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        Color was = GUI.color;
                        GUI.color = PoliSimTheme.ModalBackground;
                        GUI.DrawTexture(new Rect(bottomRow.xMax - markSide, bottomRow.y + Mathf.Round((line - markSide) * 0.5f), markSide, markSide), glyph, ScaleMode.ScaleToFit, true);
                        GUI.color = was;
                    }
                    if (!small && HqWidth(lead, paper) <= bottomRow.width - markSide - V35.Px(2f)) { HqLabel(new Rect(bottomRow.x, bottomRow.y, bottomRow.width - markSide, line), lead, paper); }
                }
                else
                {
                    string word = HqShort(leaderKey);
                    float ww = HqWidth(word, paper), lw = HqWidth(lead, paper);
                    if (!small && ww + V35.Px(3f) + lw <= bottomRow.width)
                    {
                        HqLabel(new Rect(bottomRow.x, bottomRow.y, lw, line), lead, paper);
                        HqLabel(new Rect(bottomRow.xMax - ww, bottomRow.y, ww, line), word, paper);
                    }
                    else if (ww <= t.width) { HqLabel(new Rect(t.x, bottomRow.y, t.width, line), word, HqMono(14f, PoliSimTheme.ModalBackground, bold: true, anchor: TextAnchor.MiddleCenter)); }
                }
            }
            else if (state == RegionalPolling.MapState.TooCloseToCall && !next)
            {
                Texture2D approx = IconLibrary.HqGlyph(V35.Scale >= 1.5f ? "approx_36" : "approx_18");
                var bottom = new Rect(t.x, t.yMax - V35.Px(2f) - V35.Px(20f), t.width, V35.Px(20f));
                if (approx != null)
                {
                    float a = V35.Px(18f);
                    if (Event.current.type == EventType.Repaint)
                    {
                        Color was = GUI.color;
                        GUI.color = PoliSimTheme.TextPrimary;
                        GUI.DrawTexture(new Rect(bottom.center.x - a * 0.5f, bottom.center.y - a * 0.5f, a, a), approx, ScaleMode.ScaleToFit, true);
                        GUI.color = was;
                    }
                }
                else if (!small || t.height >= V35.Px(30f)) { HqLabel(bottom, "≈", HqMono(18f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleCenter)); }
            }
            if (next && !small)
            {
                HqLabel(new Rect(t.x, t.yMax - V35.Px(3f) - line, t.width, line), "NEXT", HqMono(14f, PoliSimTheme.BrassBorder, bold: true, anchor: TextAnchor.MiddleCenter));
            }

            // the office square, top right
            OfficeMark office = region >= 0 && region < ground.Offices.Length ? ground.Offices[region] : OfficeMark.None;
            if (office != OfficeMark.None)
            {
                float sq = V35.Px(10f);
                var square = new Rect(t.xMax - V35.Px(3f) - sq, t.y + V35.Px(3f), sq, sq);
                if (office == OfficeMark.Open) { HqFill(square, PoliSimTheme.Brass); HqEdge(square, PoliSimTheme.BrassBorder, HqT(1f)); }
                else if (office == OfficeMark.Staged) { HqEdge(square, PoliSimTheme.Brass, HqT(1.5f)); }
                else { HqDashed(square, PoliSimTheme.Brass, HqT(1f)); }
            }

            // the tile's slip: its name, its reading, its office, the target
            string id = "hq:tile:" + (region >= 0 ? region.ToString(CultureInfo.InvariantCulture) : tile.Id);
            SlipAnchor(t, id);
            var slip = new SlipContent(map.Key == "us_states" ? tile.Label.ToUpperInvariant() + " · " + UsElectorsOf(tile.Id, s.Calendar.ElectionDate).ToString(CultureInfo.InvariantCulture) + " ELECTORS" : tile.Label.ToUpperInvariant());
            if (state == RegionalPolling.MapState.Unknown) { slip.Add("not polled · no regional reading"); }
            else
            {
                string leader = reading.Leader >= 0 && reading.Leader < s.PartyNames.Length ? HqShort(s.PartyNames[reading.Leader]) : "—";
                slip.Add(state == RegionalPolling.MapState.TooCloseToCall ? "too close · the lead inside its ±" : leader + " leads by " + HqF(reading.GapPp, 1) + " ± " + HqF(reading.GapErrorPp, 1));
                slip.Add("your poll · n " + HqN(reading.Poll.SampleSize) + " · " + HqDay(reading.Poll.FieldDate) + " · index " + HqF(reading.SwingIndex, 0));
            }
            if (office != OfficeMark.None) { slip.Add("office · " + (office == OfficeMark.Open ? "open" : office == OfficeMark.Staged ? "staged" : "planned")); }
            if (next) { slip.Add("the next office opens here"); }
            else if (target) { slip.Add("target · local acts land here"); }
            book.Anchors[id] = slip;
        }

        /// <summary>The legend under the map (mono 14, 14 px swatches, wrapping): the states the map can show now, and the office marks it holds. Returns its bottom.</summary>
        private float DrawHqLegend(Rect area, CampaignGround ground, CampaignSnapshot s, bool siting, PeopleSlips.Book book)
        {
            var items = new List<(string Text, int Kind)> { ("not polled", 0) };
            bool polledAny = false, closeAny = false, open = false, staged = false, planned = false;
            foreach (MapRegionReading r in ground.Readings) { RegionalPolling.MapState st = RegionalPolling.StateOf(r); polledAny |= st == RegionalPolling.MapState.Polled; closeAny |= st == RegionalPolling.MapState.TooCloseToCall; }
            foreach (OfficeMark m in ground.Offices) { open |= m == OfficeMark.Open; staged |= m == OfficeMark.Staged; planned |= m == OfficeMark.Planned; }
            bool regional = !HqRunUp(s) && ground.RegionalPollOnSale;
            if (regional || polledAny) { items.Add(("polled", 1)); }
            if (regional || closeAny) { items.Add(("too close", 2)); }
            if (open) { items.Add(("office", 3)); }
            if (staged) { items.Add(("office staged", 4)); }
            if (planned) { items.Add(("planned", 5)); }
            if (siting) { items.Add(("next office", 6)); }
            GUIStyle face = HqMono(14f, PoliSimTheme.TextPrimary);
            float sw = V35.Px(14f), x = area.x, y = area.y, lineH = HqH(18f), gapX = HqW(10f), gapY = HqH(4f);
            foreach ((string text, int kind) in items)
            {
                float w = sw + V35.Px(5f) + HqWidth(text, face);
                if (x > area.x && x + w > area.xMax) { x = area.x; y += lineH + gapY; }
                var swatch = new Rect(x, y + Mathf.Round((lineH - sw) * 0.5f), sw, sw);
                switch (kind)
                {
                    case 0: HqFill(swatch, PoliSimTheme.Card); HqDashed(swatch, PoliSimTheme.HairlineStrong, HqT(1.5f)); break;
                    case 1: HqFill(swatch, PoliSimTheme.TextSecondary); break;
                    case 2: HqFill(swatch, PoliSimTheme.Card); HqEdge(swatch, PoliSimTheme.TextPrimary, HqT(1.5f)); HqLabel(swatch, "≈", HqMono(14f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleCenter)); break;
                    case 3: HqFill(swatch, PoliSimTheme.Brass); HqEdge(swatch, PoliSimTheme.BrassBorder, HqT(1f)); break;
                    case 4: HqEdge(swatch, PoliSimTheme.Brass, HqT(1.5f)); break;
                    case 5: HqDashed(swatch, PoliSimTheme.Brass, HqT(1f)); break;
                    default: HqFill(swatch, V35.SlipPaper); HqEdge(swatch, PoliSimTheme.Brass, HqT(2f)); break;
                }
                HqLabel(new Rect(x + sw + V35.Px(5f), y, HqWidth(text, face), lineH), text, face);
                x += w + gapX;
            }
            var legend = new Rect(area.x, area.y, area.width, y + lineH - area.y);
            SlipAnchor(legend, "hq:legend");
            book.Anchors["hq:legend"] = new SlipContent("THE MAP").Add("only a regional poll colours a tile").Add("polled · the leader's ink, mark and lead").Add("too close · the lead inside its ±")
                .Add(string.Format(CultureInfo.InvariantCulture, "index · 100 at a tie, 0 at a {0:F0}-point lead", SwingRegions.FullScaleGapPp));   // the cartogram screen's scale, kept reachable
            return y + lineH;
        }

        /// <summary>The region's plate (board 4b-4g): the target region (the next office's, siting) - its name and seats, its stamp, every standing party's share
        /// with its band where it was polled, its source line, its office and its field report.</summary>
        private void DrawHqPlate(Rect plate, CampaignSnapshot s, CampaignGround ground, CampaignTiles.TileMap map, bool siting, PeopleSlips.Book book)
        {
            HqFill(plate, PoliSimTheme.CardInset);
            HqEdge(plate, PoliSimTheme.RuleRow, HqT(1f));
            float padX = HqW(12f), ix = plate.x + padX, iw = plate.width - 2f * padX, y = plate.y + HqH(6f);
            int region = ground?.Target ?? -1;
            if (ground != null && HqRunUp(s) && !siting)
            {
                // the run-up has no target (no local acts): the plate shows the party's first office, else where the next one opens
                for (int r = 0; r < ground.Offices.Length; r++) { if (ground.Offices[r] != OfficeMark.None) { region = r; break; } }
            }
            if (ground == null || region < 0 || region >= ground.Names.Length)
            {
                HqLabel(new Rect(ix, y, iw, HqH(28f)), "no region", HqSerif(16f, PoliSimTheme.TextSecondary));
                return;
            }
            MapRegionReading reading = ground.Readings[region];
            RegionalPolling.MapState state = RegionalPolling.StateOf(reading);
            string label = CampaignTiles.LabelOf(map, ground.Names[region], ground.Names);
            if (label.Length > 0) { label = char.ToUpperInvariant(label[0]) + label.Substring(1); }
            var head = new Rect(ix, y, iw, HqH(28f));
            GUIStyle name16 = HqSerif(16f, PoliSimTheme.TextPrimary);
            string stamp = siting ? "NEXT OFFICE" : state == RegionalPolling.MapState.Polled ? "YOUR POLL" : state == RegionalPolling.MapState.TooCloseToCall ? "≈ TOO CLOSE" : "NOT POLLED";
            string seats = ground.Seats != null && region < ground.Seats.Length ? ground.Seats[region].ToString(CultureInfo.InvariantCulture) + " seats" : null;
            GUIStyle muted = HqMono(14f, PoliSimTheme.TextMuted);
            float seatsW = HqWidth(seats, muted);
            float stampW = HqWidth(stamp, HqMono(14f, PoliSimTheme.TextPrimary, bold: true)) + V35.Px(12f);
            // the name, said shorter where it does not fit beside the seats and the stamp (K320: never cut) - and on a narrow plate the stamp takes its own row
            string[] names = HqRegionForms(s, ground.Names[region]);
            for (int i = 0; i < names.Length; i++) { if (!string.IsNullOrEmpty(names[i])) { names[i] = char.ToUpperInvariant(names[i][0]) + names[i].Substring(1); } }
            float room = iw - stampW - V35.Px(8f) - (seats != null ? seatsW + V35.Px(8f) : 0f);
            bool ownRow = HqWidth(names[1], name16) > room;
            if (ownRow) { room = iw - (seats != null ? seatsW + V35.Px(8f) : 0f); }
            string nameShown = HqFirstFitting(name16, room, names);
            float nameW = Mathf.Min(room, HqWidth(nameShown, name16));
            HqLabel(new Rect(head.x, head.y, nameW, head.height), nameShown, name16);
            if (seats != null) { HqLabel(new Rect(head.x + nameW + V35.Px(8f), head.y, seatsW, head.height), seats, muted); }
            if (ownRow) { y += head.height; head = new Rect(ix, y, iw, HqH(24f)); }
            HqStamp(head, stamp, siting ? PoliSimTheme.BrassBorder : PoliSimTheme.TextPrimary, dashed: !siting && state == RegionalPolling.MapState.Unknown, right: !ownRow);
            y += head.height;
            var slip = new SlipContent(label.ToUpperInvariant());
            double perUnit = HqPerUnit();

            // the polled shares, with their bands
            if (state != RegionalPolling.MapState.Unknown && !siting)
            {
                float barW = HqW(Mathf.Min(150f, plate.width / _hqUx - 24f - 14f - 40f - 38f - 50f - 24f));
                GUIStyle code = HqMono(14f, PoliSimTheme.TextPrimary);
                GUIStyle share = HqMono(14f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleRight);
                double top = 0.01;
                for (int p = 0; p < reading.Poll.PartyCount; p++) { top = System.Math.Max(top, reading.Poll.Share(p) + reading.Poll.MarginOfErrorPp(p) / 100.0); }
                double scale = System.Math.Min(1.0, System.Math.Ceiling(top * 20.0) / 20.0);
                for (int p = 0; p < reading.Poll.PartyCount && p < s.PartyNames.Length; p++)
                {
                    if (!ground.StandsIn(p, region) || reading.Poll.Share(p) <= 0.0 || (s.Hq != null && !s.Hq.Stands(p))) { continue; }
                    if (y + HqH(20f) > plate.yMax - HqH(21f)) { break; }
                    var row = new Rect(ix, y, iw, HqH(20f));
                    float x = row.x, markSide = V35.Px(14f);
                    Texture2D mark = IconLibrary.GetPartyMark(HqMarkKey(s.PartyNames[p]));
                    if (mark != null && Event.current.type == EventType.Repaint) { HqTintedMark(new Rect(x, row.y + Mathf.Round((row.height - markSide) * 0.5f), markSide, markSide), mark, s.PartyNames[p], HqMarkKey(s.PartyNames[p])); }
                    x += markSide + HqW(6f);
                    float plateCode = Mathf.Max(HqW(40f), HqWidth(HqShort(s.PartyNames[p]), HqMono(14f, PoliSimTheme.TextPrimary, bold: true)) + V35.Px(2f));
                    HqLabel(new Rect(x, row.y, plateCode, row.height), HqShort(s.PartyNames[p]), p == s.PlayerPartyIndex ? HqMono(14f, PoliSimTheme.TextPrimary, bold: true) : code);
                    x += plateCode + HqW(6f);
                    double sh = reading.Poll.Share(p), e = reading.Poll.MarginOfErrorPp(p) / 100.0;
                    var track = new Rect(x, row.y + Mathf.Round((row.height - V35.Px(14f)) * 0.5f) + V35.Px(1f), barW, V35.Px(8f));
                    HqFill(track, PoliSimTheme.RuleRow);
                    HqFill(new Rect(track.x, track.y, Mathf.Round(track.width * Mathf.Clamp01((float)(sh / scale))), track.height), HqInk(s.PartyNames[p]));
                    float lo = Mathf.Clamp01((float)((sh - e) / scale)), hi = Mathf.Clamp01((float)((sh + e) / scale));
                    HqFill(new Rect(track.x + track.width * lo, track.y + V35.Px(10f), Mathf.Max(HqT(2f), track.width * (hi - lo)), HqT(3f)), PoliSimTheme.HairlineStrong);
                    x += barW + HqW(6f);
                    HqLabel(new Rect(x, row.y, HqW(38f), row.height), HqF(100.0 * sh, 1), share);
                    x += HqW(38f) + HqW(6f);
                    HqLabel(new Rect(x, row.y, Mathf.Max(1f, row.xMax - x), row.height), "±" + HqF(100.0 * e, 1), muted);
                    y += row.height;
                }
                double price = region < ground.PollPrice.Length ? ground.PollPrice[region] : double.NaN;
                string sourceLine = "your poll · n " + HqN(reading.Poll.SampleSize) + " · " + HqDay(reading.Poll.FieldDate) + (double.IsNaN(price) ? string.Empty : " · " + SymbolRegistry.Word(Symbol.Billed) + " " + HqN(price * perUnit));
                HqPlateLine(ref y, ix, iw, plate, PoliSimTheme.TextPrimary, true, true, OfficeMark.None, sourceLine, sourceLine.Substring("your poll · ".Length),
                    "n " + HqN(reading.Poll.SampleSize) + " · " + HqDay(reading.Poll.FieldDate));
                string leader = reading.Leader >= 0 && reading.Leader < s.PartyNames.Length ? HqShort(s.PartyNames[reading.Leader]) : "—";
                slip.Add(state == RegionalPolling.MapState.TooCloseToCall ? "too close · " + leader + " leads by " + HqF(reading.GapPp, 1) + " inside ±" + HqF(reading.GapErrorPp, 1) : leader + " leads by " + HqF(reading.GapPp, 1) + " ±" + HqF(reading.GapErrorPp, 1));
                slip.Add(sourceLine);
                slip.Add("a regional poll's n reads closer than the national slice");
            }
            else if (siting)
            {
                HqPlateLine(ref y, ix, iw, plate, PoliSimTheme.TextPrimary, false, false, OfficeMark.None, ground.TargetPicked ? "your pick" : "largest electorate without one", ground.TargetPicked ? "your pick" : "largest without one");
                HqPlateLine(ref y, ix, iw, plate, PoliSimTheme.TextPrimary, false, true, OfficeMark.None, "from day one · upkeep " + HqMoney(ground.OfficeUpkeepPerDay) + "/day", "upkeep " + HqMoney(ground.OfficeUpkeepPerDay) + "/day", "from day one");
                slip.Add("the next office opens here").Add("on the campaign's first day · raises your turnout").Add("another tile picked moves it");
            }
            else
            {
                HqPlateLine(ref y, ix, iw, plate, PoliSimTheme.TextPrimary, false, false, OfficeMark.None, "no regional reading", "not polled");
                bool regional = ground.RegionalPollOnSale;
                string offer = !regional ? "POLL REGION · closed" : HqRunUp(s) ? "POLL REGION · from " + HqDay(s.Calendar.CampaignStart)
                    : string.Format(CultureInfo.InvariantCulture, "POLL REGION · n {0:N0} · ±{1:0.0} · {2}", ground.RegionalSample, RegionalPolling.MarginOfErrorPp(0.5, ground.RegionalSample), HqMoney(ground.RegionalPrice));
                HqPlateLine(ref y, ix, iw, plate, PoliSimTheme.TextSecondary, false, false, OfficeMark.None, offer, regional ? "POLL REGION · " + HqMoney(ground.RegionalPrice) : offer, "POLL REGION");
                slip.Add("not polled · nothing is known here");
                slip.Add(regional ? (HqRunUp(s) ? "POLL REGION opens with the campaign" : "POLL REGION buys a reading") : "no regional poll in this race · its count is national");
            }

            // the office, and today's report
            OfficeMark office = ground.Offices[region];
            if (office != OfficeMark.None && !siting)
            {
                int vol = ground.OfficeVolunteers[region];
                string state3 = office == OfficeMark.Open ? "open" : office == OfficeMark.Staged ? "staged" : "planned";
                string vols = HqN(System.Math.Max(0, vol)) + " vol";
                string upkeep = HqMoney(ground.OfficeUpkeepPerDay) + "/day";
                HqPlateLine(ref y, ix, iw, plate, PoliSimTheme.TextPrimary, false, true, office, "Office · " + state3 + " · " + vols + " · " + upkeep, state3 + " · " + vols + " · " + upkeep,
                    "Office · " + state3 + " · " + vols, state3 + " · " + vols);
                if (!HqRunUp(s) && !ground.TargetPicked) { slip.Add("your strongest office"); }
                slip.Add(office == OfficeMark.Open ? "office open" : office == OfficeMark.Staged ? "office staged · opens on the campaign's first day" : "office planned · opens on the campaign's first day");
            }
            if (ground.Reports != null)
            {
                foreach (FieldReport f in ground.Reports)
                {
                    if (f.Region != region) { continue; }
                    string figures = string.Format(CultureInfo.InvariantCulture, "{0:N0} vol · {1:N0} doors · {2:N0} calls", f.Volunteers, f.Doors, f.Calls);
                    HqPlateLine(ref y, ix, iw, plate, PoliSimTheme.TextSecondary, false, true, OfficeMark.None, "report · " + figures, figures);
                    break;
                }
            }
            SlipAnchor(plate, "hq:plate");
            while (slip.Lines.Count > 4) { slip.Lines.RemoveAt(slip.Lines.Count - 1); }
            book.Anchors["hq:plate"] = slip;
        }

        /// <summary>One line of the plate (mono 14, 21 high), with a rule above where it opens a group and the office square before it where it is an office's;
        /// a line that does not fit the plate is not drawn (its words are the plate's slip). <paramref name="forms"/> are its wordings, longest first.</summary>
        private void HqPlateLine(ref float y, float x, float w, Rect plate, Color ink, bool bold, bool ruled, OfficeMark office, params string[] forms)
        {
            float h = HqH(21f);
            if (y + h > plate.yMax - HqH(4f)) { return; }
            if (ruled) { HqFill(new Rect(x, y, w, HqT(1f)), PoliSimTheme.RuleRow); }
            float tx = x;
            if (office != OfficeMark.None)
            {
                float sq = V35.Px(14f);
                var square = new Rect(x, y + Mathf.Round((h - sq) * 0.5f), sq, sq);
                if (office == OfficeMark.Open) { HqFill(square, PoliSimTheme.Brass); HqEdge(square, PoliSimTheme.BrassBorder, HqT(1f)); } else { HqEdge(square, PoliSimTheme.Brass, HqT(1.5f)); }
                tx += sq + V35.Px(8f);
            }
            GUIStyle face = HqMono(14f, ink, bold);
            float room = Mathf.Max(1f, x + w - tx);
            HqLabel(new Rect(tx, y, room, h), HqFirstFitting(face, room, forms), face);   // K320: said shorter, never cut
            y += h;
        }
    }
}
