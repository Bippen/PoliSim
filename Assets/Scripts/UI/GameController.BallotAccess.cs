using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// SP-5 (S5, Design's return of 2026-10-30, board 5c): **FOUNDING OUTSIDE SWEDEN, DRAWN** - the country-rules slip (a § cell beside the race on the HQ
    /// head and the § RULES verb), the ballot-access task in the HQ's left column above the race while it is open (its stamp COLLECTING, ON THE BALLOT or
    /// REFUSED · n; a block for the notice and one for the Land lists, each a state square, the figure, a bar of signatures against need, the rule and the
    /// deadline; the constituencies as one line; the price per signature BILLED), the Länder tiles in their ballot states, the BALLOT ACCESS verbs on the
    /// Today panel (GATHER with SIZE, FILE, § RULES) with today's gathers as DRAFT rows, the strip's chip, and the Desk's Party funding tile. Every figure
    /// is the model's (`BallotAccess`, `PartyFunding`); every rule's words are the statute's, cited. The task reads the world's own date.
    /// </summary>
    public partial class GameController
    {
        /// <summary>The Land the next GATHER goes to - a tile pressed on the map; −1 for the list with the most signatures still to find.</summary>
        private int _hqBallotLand = -1;
        /// <summary>The next GATHER's SIZE, in thirds of the party's volunteers.</summary>
        private int _hqBallotSize = 1;
        /// <summary>The last refusal a ballot verb met, for its slip.</summary>
        private string _hqBallotRefusal;

        /// <summary>The player's party if it is a created one in a country whose ballot-access rules are built, with the election the task is for and today.</summary>
        private bool HqBallot(CampaignSnapshot s, out CreatedParty c, out System.DateTime pollingDay, out System.DateTime today)
        {
            c = null;
            pollingDay = s.Calendar.ElectionDate.Date;
            today = HqWorldToday(s);
            if (_playerCountry == null || BallotAccess.RulesOf(PlayerCountryId) != BallotAccess.Rules.Sourced) { return false; }
            c = BallotAccess.Created(PlayerCountryId, _playerCountry.PlayerPartyAbbrev);
            if (c == null) { return false; }
            BallotAccess.EnsureFor(c, pollingDay);
            return BallotAccess.TaskIsFor(c, pollingDay);   // the task is the first election's; a later election's is not modelled
        }

        /// <summary>Whether the task holds the left column: from the run-up until every list is filed - and, filed, until the lists' deadline passes (so ON THE
        /// BALLOT is seen); refused, until polling day.</summary>
        private bool HqBallotShown(CampaignSnapshot s)
        {
            if (!HqBallot(s, out CreatedParty c, out System.DateTime pollingDay, out System.DateTime today) || today > pollingDay) { return false; }
            BallotAccess.Deadlines(pollingDay, out System.DateTime _, out System.DateTime _, out System.DateTime lists);
            BallotAccess.TaskState state = BallotAccess.StateOf(PlayerCountryId, c, pollingDay, today);
            return state == BallotAccess.TaskState.Refused || state == BallotAccess.TaskState.Collecting || today <= lists;
        }


        /// <summary>The Land a GATHER goes to: the pressed tile while its list is open, else the open list with the most signatures still to find.</summary>
        private int HqBallotTarget(CreatedParty c, System.DateTime pollingDay, System.DateTime today)
        {
            if (_hqBallotLand >= 0 && _hqBallotLand < BallotAccess.LandCount && !BallotAccess.FiledOn(c, _hqBallotLand, pollingDay).HasValue
                && BallotAccess.Signatures(c, _hqBallotLand, pollingDay, today) < BallotAccess.LandListNeed(_hqBallotLand, pollingDay)) { return _hqBallotLand; }
            int best = -1, most = 0;
            for (int r = 0; r < BallotAccess.LandCount; r++)
            {
                if (BallotAccess.FiledOn(c, r, pollingDay).HasValue) { continue; }
                int left = BallotAccess.LandListNeed(r, pollingDay) - BallotAccess.Signatures(c, r, pollingDay, today) - BallotAccess.QueuedToday(c, r, pollingDay, today, out int _);
                if (left > most) { most = left; best = r; }
            }
            return best;
        }

        // ── the left column: the task ────────────────────────────────────────────────────────────────────

        /// <summary>The ballot-access block at the top of the left column (board 5c: head 26, a block per requirement, the constituencies' line, the price's line);
        /// returns the y under it.</summary>
        private float DrawHqBallotAccess(float x, float y, float w, CampaignSnapshot s, PeopleSlips.Book book)
        {
            if (!HqBallot(s, out CreatedParty c, out System.DateTime pollingDay, out System.DateTime today)) { return y; }
            BallotAccess.Deadlines(pollingDay, out System.DateTime noticeDay, out System.DateTime recognitionDay, out System.DateTime listsDay);
            BallotAccess.TaskState state = BallotAccess.StateOf(PlayerCountryId, c, pollingDay, today);
            int refused = BallotAccess.RefusedCount(c, pollingDay, today);
            Color partyInk = PoliSimTheme.PartyLaddered(PlayerCountryId, c.Key);

            // the head and its stamp
            var head = new Rect(x, y, w, HqH(26f));
            GUIStyle head18 = HqSerif(18f, PoliSimTheme.TextPrimary);
            HqLabel(new Rect(head.x, head.y, HqWidth("Ballot access", head18), head.height), "Ballot access", head18);
            string stamp = state == BallotAccess.TaskState.Refused ? "REFUSED · " + refused.ToString(CultureInfo.InvariantCulture)
                : state == BallotAccess.TaskState.OnTheBallot ? "ON THE BALLOT" : "COLLECTING";
            Color stampInk = state == BallotAccess.TaskState.Refused ? PoliSimTheme.Bad : state == BallotAccess.TaskState.OnTheBallot ? PoliSimTheme.Good : PoliSimTheme.TextPrimary;
            Rect stampRect = HqStamp(new Rect(head.xMax, head.y, 1f, head.height), stamp, stampInk, right: true);
            SlipAnchor(stampRect, state == BallotAccess.TaskState.Refused ? "hq:ballot:refused" : "hq:ballot");
            SlipAnchor(new Rect(head.x, head.y, HqWidth("Ballot access", head18), head.height), "hq:ballot");
            y += head.height;

            // the notice: no signatures - filed, or due by its day
            System.DateTime? noticeOn = BallotAccess.NoticeFiledOn(c, pollingDay);
            bool noticeLate = !noticeOn.HasValue && today > noticeDay;
            y = DrawHqBallotRow(x, y, w, "Notice", noticeOn.HasValue ? "✓ filed" : noticeLate ? "✗ late" : "due",
                noticeOn.HasValue ? PoliSimTheme.Good : noticeLate ? PoliSimTheme.Bad : PoliSimTheme.TextPrimary, noticeOn.HasValue ? 1f : 0f, noticeOn.HasValue, noticeLate,
                "BWahlG §18", noticeOn.HasValue ? "filed " + BallotDay(noticeOn.Value) : (noticeLate ? "passed " : "day ") + (noticeLate ? BallotDay(noticeDay) : BallotAccess.DayBefore(pollingDay, noticeDay).ToString(CultureInfo.InvariantCulture) + " · " + BallotDay(noticeDay)),
                partyInk, "hq:ballot:notice");

            // the Land lists: the signatures in against the sixteen needs
            int need = BallotAccess.TotalListNeed(pollingDay), have = 0, filed = BallotAccess.FiledCount(c, pollingDay);
            for (int r = 0; r < BallotAccess.LandCount; r++) { have += BallotAccess.Signatures(c, r, pollingDay, today); }
            bool listsPassed = today > listsDay;
            string listFigure = state == BallotAccess.TaskState.Refused && refused > 0 ? "✗ " + refused.ToString(CultureInfo.InvariantCulture) + " refused"
                : filed == BallotAccess.LandCount ? "✓ " + HqN(need) : HqN(have) + " / " + HqN(need);
            Color listInk = state == BallotAccess.TaskState.Refused ? PoliSimTheme.Bad : filed == BallotAccess.LandCount ? PoliSimTheme.Good : PoliSimTheme.TextPrimary;
            y = DrawHqBallotRow(x, y, w, "Land lists", listFigure, listInk, need > 0 ? have / (float)need : 0f, filed == BallotAccess.LandCount, state == BallotAccess.TaskState.Refused,
                "§27 · 1 ‰, ≤ " + HqN(BallotAccess.LandListCap), (listsPassed ? "passed " : "day " + BallotAccess.DayBefore(pollingDay, listsDay).ToString(CultureInfo.InvariantCulture) + " · ") + BallotDay(listsDay),
                partyInk, "hq:ballot:lists");

            // the constituencies: the rule as one line - the count is the Zweitstimme, so a constituency candidacy changes nothing the game counts
            GUIStyle muted = HqMono(14f, PoliSimTheme.TextMuted);
            var constituencies = new Rect(x, y, w, HqH(20f));
            HqFill(new Rect(x, y, w, HqT(1f)), PoliSimTheme.RuleRow);
            HqLabel(constituencies, "Constituencies · " + BallotAccess.PerConstituency.ToString(CultureInfo.InvariantCulture) + " each", muted);
            HqStamp(new Rect(constituencies.xMax, constituencies.y + HqH(1f), 1f, constituencies.height - HqH(2f)), "ABSENT", PoliSimTheme.TextMuted, dashed: true, right: true);
            SlipAnchor(constituencies, "hq:ballot:const");
            y += constituencies.height;

            // the price per signature: BILLED (no source prices one; Design's € 2.40 was illustrative)
            var price = new Rect(x, y, w, HqH(24f));
            HqFill(new Rect(x, y, w, HqT(1f)), PoliSimTheme.RuleRow);
            GUIStyle name16 = HqSerif(16f, PoliSimTheme.TextPrimary);
            HqLabel(new Rect(price.x, price.y, HqWidth("Per signature", name16), price.height), "Per signature", name16);
            HqStamp(new Rect(price.xMax, price.y + HqH(2f), 1f, price.height - HqH(4f)), "BILLED", PoliSimTheme.TextPrimary, dashed: true, right: true);
            SlipAnchor(price, "hq:ballot:price");
            y += price.height;

            // the slips
            book.Anchors["hq:ballot"] = new SlipContent("BALLOT ACCESS · " + stamp).Add("Notice, then sixteen Land lists").Add("Lists due " + BallotDay(listsDay) + " · 18:00").Add("GATHER, then FILE").Add("[[Not yet seated]] · BWahlG §18(2)");
            book.Terms["Not yet seated"] = new SlipContent("A NEW PARTY").Add("A party not seated since the last election with five members of its own must be recognised and gather support signatures before it can stand.");
            book.Anchors["hq:ballot:refused"] = new SlipContent("Refused · " + refused.ToString(CultureInfo.InvariantCulture) + " Länder")
                .Add(noticeLate ? "No notice by " + BallotDay(noticeDay) : "Lists short at day " + BallotAccess.DayBefore(pollingDay, listsDay).ToString(CultureInfo.InvariantCulture))
                .Add("No list votes · " + refused.ToString(CultureInfo.InvariantCulture) + " Länder").Add("Constituencies only where filed").Add("[[BWahlG §19, §27]]");
            book.Terms["BWahlG §19, §27"] = new SlipContent("REFUSED").Add("A list without its signatures by the deadline is not admitted, so the party cannot be voted for there.");
            book.Anchors["hq:ballot:notice"] = new SlipContent("Notice · " + (noticeOn.HasValue ? "filed " + BallotDay(noticeOn.Value) : "due " + BallotDay(noticeDay)))
                .Add("To the Federal Returning Officer").Add("No signatures · three board members").Add("Recognised by " + BallotDay(recognitionDay) + " · §18(4)").Add("FILE files it");
            book.Anchors["hq:ballot:lists"] = new SlipContent("Land lists · " + filed.ToString(CultureInfo.InvariantCulture) + " of 16 filed")
                .Add("1 ‰ of the last register, ≤ " + HqN(BallotAccess.LandListCap)).Add(HqN(have) + " of " + HqN(need) + " signatures in").Add("Due " + BallotDay(listsDay) + " · §19").Add("DERIVED · electorate as on disk");
            book.Anchors["hq:ballot:const"] = new SlipContent("Constituencies").Add(BallotAccess.PerConstituency.ToString(CultureInfo.InvariantCulture) + " signatures each · §20").Add("Only where the Land list stands").Add("ABSENT · no Erststimme in the count");
            book.Anchors["hq:ballot:price"] = new SlipContent("Per signature · BILLED").Add("No source prices a signature").Add("GATHER spends volunteer days").Add("No money moves until sourced");
            return y;
        }

        /// <summary>One requirement's block (board 5c): a 14 px state square and the name in serif 16, the figure (mono 14 bold) on the right; a bar of signatures
        /// against need, ink while collecting, the party's ink once filed, BAD when refused; the rule (mono 14 muted) and the deadline (mono 14 bold).</summary>
        private float DrawHqBallotRow(float x, float y, float w, string name, string figure, Color figureInk, float filledShare, bool done, bool refused, string rule, string deadline,
            Color partyInk, string slipId)
        {
            HqFill(new Rect(x, y, w, HqT(1f)), PoliSimTheme.RuleRow);
            float top = y + HqH(4f);
            float sq = V35.Px(14f);
            var line = new Rect(x, top, w, HqH(24f));
            var square = new Rect(x, line.y + Mathf.Round((line.height - sq) * 0.5f), sq, sq);
            if (done) { HqFill(square, partyInk); }
            else if (refused) { HqEdge(square, PoliSimTheme.Bad, HqT(1f)); HqFill(new Rect(square.x, square.center.y - HqT(1f), square.width, HqT(2f)), PoliSimTheme.Bad); }
            else { HqEdge(square, PoliSimTheme.TextPrimary, HqT(1.5f)); }
            GUIStyle name16 = HqSerif(16f, PoliSimTheme.TextPrimary);
            HqLabel(new Rect(square.xMax + V35.Px(7f), line.y, HqWidth(name, name16), line.height), name, name16);
            GUIStyle fig = HqMono(14f, figureInk, bold: true, anchor: TextAnchor.MiddleRight);
            HqLabel(new Rect(line.xMax - HqWidth(figure, fig), line.y, HqWidth(figure, fig), line.height), figure, fig);
            var bar = new Rect(x, line.yMax, w, HqH(8f));
            HqFill(bar, PoliSimTheme.RuleRow);
            HqFill(new Rect(bar.x, bar.y, Mathf.Round(bar.width * Mathf.Clamp01(filledShare)), bar.height), refused ? PoliSimTheme.Bad : done ? partyInk : PoliSimTheme.TextPrimary);
            var foot = new Rect(x, bar.yMax, w, HqH(20f));
            GUIStyle muted = HqMono(14f, PoliSimTheme.TextMuted);
            HqLabel(new Rect(foot.x, foot.y, HqWidth(rule, muted), foot.height), rule, muted);
            GUIStyle bold = HqMono(14f, refused ? PoliSimTheme.Bad : PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleRight);
            HqLabel(new Rect(foot.xMax - HqWidth(deadline, bold), foot.y, HqWidth(deadline, bold), foot.height), deadline, bold);
            float bottom = foot.yMax + HqH(2f);
            SlipAnchor(new Rect(x, y, w, bottom - y), slipId);
            return bottom;
        }

        // ── the map: the Länder in their ballot states ───────────────────────────────────────────────────

        /// <summary>Whether the HQ's map shows the ballot states (the task holds the left column).</summary>
        private bool HqBallotMap(CampaignSnapshot s) => HqBallotShown(s);

        /// <summary>One Land in its ballot state (board 5c): list filed - the party's ink with ✓; collecting - a 1.5 px ink rule and a 6 px progress bar; not
        /// started - dashed; refused - a 1 px BAD rule, struck. The picked Land (the next GATHER's) carries the brass ring.</summary>
        private void DrawHqBallotTile(Rect t, CampaignTiles.Tile tile, int region, CampaignSnapshot s, PeopleSlips.Book book)
        {
            if (!HqBallot(s, out CreatedParty c, out System.DateTime pollingDay, out System.DateTime today) || region < 0 || region >= BallotAccess.LandCount) { return; }
            int need = BallotAccess.LandListNeed(region, pollingDay), have = BallotAccess.Signatures(c, region, pollingDay, today);
            System.DateTime? filedOn = BallotAccess.FiledOn(c, region, pollingDay);
            BallotAccess.Deadlines(pollingDay, out System.DateTime _, out System.DateTime _, out System.DateTime lists);
            bool refused = BallotAccess.RefusedCount(c, pollingDay, today) > 0 && !BallotAccess.ListStands(c, region, pollingDay, today);
            bool filed = filedOn.HasValue && !refused;
            Color partyInk = PoliSimTheme.PartyLaddered(PlayerCountryId, c.Key);
            Color label = filed ? PoliSimTheme.ModalBackground : PoliSimTheme.TextPrimary;
            if (filed) { HqFill(t, partyInk); }
            else if (refused) { HqFill(t, PoliSimTheme.Card); HqEdge(t, PoliSimTheme.Bad, HqT(1f)); }
            else if (have > 0) { HqFill(t, PoliSimTheme.Card); HqEdge(t, PoliSimTheme.TextPrimary, HqT(1.5f)); }
            else { HqFill(t, PoliSimTheme.Card); HqDashed(t, PoliSimTheme.HairlineStrong, HqT(1.5f)); }
            float pad = V35.Px(5f), line = V35.Px(16f);
            HqLabel(new Rect(t.x + pad, t.y + V35.Px(3f), t.width - 2f * pad, line), tile.Id, HqMono(14f, label, bold: true));
            if (filed) { HqLabel(new Rect(t.x + pad, t.y + V35.Px(3f), t.width - 2f * pad, line), "✓", HqMono(14f, label, bold: true, anchor: TextAnchor.MiddleRight)); }
            if (!filed && !refused && have > 0)
            {
                var track = new Rect(t.x + pad, t.yMax - pad - V35.Px(6f), t.width - 2f * pad, V35.Px(6f));
                HqFill(track, PoliSimTheme.RuleRow);
                HqFill(new Rect(track.x, track.y, Mathf.Round(track.width * Mathf.Clamp01(have / (float)need)), track.height), PoliSimTheme.TextPrimary);
            }
            if (refused && Event.current.type == EventType.Repaint)
            {
                Matrix4x4 was = GUI.matrix;
                GUIUtility.RotateAroundPivot(-45f, t.center);
                float length = t.width * 1.28f;
                PoliSimTheme.Rule(new Rect(t.center.x - length * 0.5f, t.center.y - HqT(1f), length, HqT(2f)), PoliSimTheme.Bad);
                GUI.matrix = was;
            }
            if (region == HqBallotTarget(c, pollingDay, today) && !filed && !refused && today <= lists) { HqEdge(new Rect(t.x - HqT(3f), t.y - HqT(3f), t.width + HqT(6f), t.height + HqT(6f)), PoliSimTheme.Brass, HqT(2f)); }
            string id = "hq:ballot:land:" + region.ToString(CultureInfo.InvariantCulture);
            SlipAnchor(t, id);
            book.Anchors[id] = new SlipContent(tile.Label + " · Land list")
                .Add(filed ? "Filed ✓ " + BallotDay(filedOn.Value) : refused ? "REFUSED · deadline passed" : HqN(have) + " of " + HqN(need) + " signatures")
                .Add("Need: 1 ‰ of voters, ≤ " + HqN(BallotAccess.LandListCap))
                .Add("DERIVED · electorate as on disk")
                .Add(filed || refused ? "No GATHER here" : "Press · the next GATHER goes here");
        }

        /// <summary>The legend under the ballot map (board 5c): list filed, collecting, not started, refused.</summary>
        private float DrawHqBallotLegend(Rect area, CampaignSnapshot s)
        {
            HqBallot(s, out CreatedParty c, out System.DateTime _, out System.DateTime _);
            Color partyInk = c != null ? PoliSimTheme.PartyLaddered(PlayerCountryId, c.Key) : PoliSimTheme.TextPrimary;
            GUIStyle face = HqMono(14f, PoliSimTheme.TextPrimary);
            float sq = V35.Px(14f), x = area.x, rowY = area.y;
            string[] words = { "list filed", "collecting", "not started", "refused" };
            for (int i = 0; i < words.Length; i++)
            {
                // §965: an entry that would cross the map column's edge starts a second row - in one row "refused" ran into the Today column at 1280
                float entryW = sq + V35.Px(5f) + HqWidth(words[i], face);
                if (x > area.x && x + entryW > area.xMax) { x = area.x; rowY += HqH(20f); }
                var square = new Rect(x, rowY + Mathf.Round((HqH(20f) - sq) * 0.5f), sq, sq);
                if (i == 0) { HqFill(square, partyInk); }
                else if (i == 1) { HqFill(square, PoliSimTheme.Card); HqEdge(square, PoliSimTheme.TextPrimary, HqT(1.5f)); }
                else if (i == 2) { HqFill(square, PoliSimTheme.Card); HqDashed(square, PoliSimTheme.HairlineStrong, HqT(1f)); }
                else { HqFill(square, PoliSimTheme.Card); HqEdge(square, PoliSimTheme.Bad, HqT(1f)); HqFill(new Rect(square.x, square.center.y - HqT(1f), square.width, HqT(2f)), PoliSimTheme.Bad); }
                x = square.xMax + V35.Px(5f);
                HqLabel(new Rect(x, rowY, HqWidth(words[i], face), HqH(20f)), words[i], face);
                x += HqWidth(words[i], face) + V35.Px(14f);
            }
            return rowY + HqH(20f);
        }

        // ── the Today panel: the verbs ───────────────────────────────────────────────────────────────────

        /// <summary>The BALLOT ACCESS verbs under the Today head (board 5c): GATHER with SIZE x1 x2 x3, FILE, § RULES; today's gathers as DRAFT rows. Returns the y
        /// under them; unchanged where the task is not shown.</summary>
        private float DrawHqBallotVerbs(float x, float y, float w, CampaignSnapshot s, PeopleSlips.Book book)
        {
            if (!HqBallotShown(s) || !HqBallot(s, out CreatedParty c, out System.DateTime pollingDay, out System.DateTime today)) { return y; }
            bool live = HqLive(out bool _);
            BallotAccess.Deadlines(pollingDay, out System.DateTime noticeDay, out System.DateTime _, out System.DateTime listsDay);
            GUIStyle captionFace = HqMono(14f, PoliSimTheme.TextMuted);
            HqLabel(new Rect(x, y, w, HqH(22f)), "BALLOT ACCESS", captionFace);
            y += HqH(22f);
            GUIStyle verbFace = HqMono(14f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleCenter);
            float h = HqH(28f), gap = V35.Px(4f), vx = x;
            bool open = today <= listsDay;
            int target = HqBallotTarget(c, pollingDay, today);
            BallotAccess.QueuedToday(c, -1, pollingDay, today, out int used);
            bool canGather = live && open && target >= 0 && used + _hqBallotSize <= BallotAccess.MaxSize;
            float gw = HqWidth("GATHER", verbFace) + V35.Px(24f);
            if (HqVerb(new Rect(vx, y, gw, h), "GATHER", canGather ? HqVerbFace.Open : HqVerbFace.Closed, "hq:ballot:gather") && canGather)
            {
                _hqBallotRefusal = BallotAccess.TryGather(c, target, _hqBallotSize, pollingDay, today, out string why) ? null : why;
            }
            vx += gw + gap;
            for (int k = 1; k <= BallotAccess.MaxSize; k++)
            {
                string size = "×" + k.ToString(CultureInfo.InvariantCulture);
                float sw = HqWidth(size, verbFace) + V35.Px(14f);
                if (HqVerb(new Rect(vx, y, sw, h), size, _hqBallotSize == k ? HqVerbFace.Selected : HqVerbFace.Open, "hq:ballot:size")) { _hqBallotSize = k; }
                vx += sw + gap;
            }
            bool anyToFile = (!BallotAccess.NoticeFiledOn(c, pollingDay).HasValue && today <= noticeDay);
            for (int r = 0; r < BallotAccess.LandCount && !anyToFile; r++)
            {
                anyToFile = !BallotAccess.FiledOn(c, r, pollingDay).HasValue && BallotAccess.Signatures(c, r, pollingDay, today) >= BallotAccess.LandListNeed(r, pollingDay);
            }
            bool canFile = live && open && anyToFile;
            float fw = HqWidth("FILE", verbFace) + V35.Px(24f);
            if (HqVerb(new Rect(vx, y, fw, h), "FILE", canFile ? HqVerbFace.Open : HqVerbFace.Closed, "hq:ballot:file") && canFile)
            {
                BallotAccess.File(c, pollingDay, today, out bool _);
                _hqBallotRefusal = null;
            }
            vx += fw + gap;
            float rw = HqWidth("§ RULES", verbFace) + V35.Px(20f);
            var rulesRect = new Rect(vx, y, Mathf.Min(rw, Mathf.Max(1f, x + w - vx)), h);
            if (HqVerb(rulesRect, "§ RULES", HqVerbFace.Open, "hq:ballot:rules"))
            {
                if (_slipPins.Exists(p => p.Anchor == "hq:rules")) { _slipPins.RemoveAll(p => p.Anchor == "hq:rules"); } else { PinSlipOnAnchorForFilm("hq:rules", rulesRect.center); }
            }
            y += h + HqH(4f);

            // today's gathers, each a DRAFT row
            foreach (BallotGather g in c.BallotGathers)
            {
                if (g.On != today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)) { continue; }
                var row = new Rect(x, y, w, HqH(28f));
                HqFill(new Rect(row.x, row.y, w, HqT(1f)), PoliSimTheme.RuleRow);
                HqFill(new Rect(row.x, row.y + HqH(4f), V35.Px(4f), HqH(20f)), PoliSimTheme.Draft);
                GUIStyle rowFace = HqMono(14f, PoliSimTheme.TextPrimary, bold: true);
                string what = "Gather ×" + g.Size.ToString(CultureInfo.InvariantCulture) + " · " + GermanRegions.NameAt(g.Region);
                HqLabel(new Rect(row.x + V35.Px(10f), row.y, HqWidth(what, rowFace), row.height), what, rowFace);
                string sig = HqN(g.Signatures) + " sig.";
                GUIStyle sigFace = HqMono(14f, PoliSimTheme.Caution, bold: true, anchor: TextAnchor.MiddleRight);
                HqLabel(new Rect(row.xMax - HqWidth(sig, sigFace), row.y, HqWidth(sig, sigFace), row.height), sig, sigFace);
                y += row.height;
            }

            string targetName = target >= 0 ? GermanRegions.NameAt(target) : "no open list";
            book.Anchors["hq:ballot:gather"] = new SlipContent("GATHER · " + targetName)
                .Add("Volunteers on one Land's list").Add("×" + _hqBallotSize.ToString(CultureInfo.InvariantCulture) + " · " + HqN(BallotAccess.YieldOf(c, _hqBallotSize)) + " signatures · counts tomorrow")
                .Add("Three thirds a day · " + (BallotAccess.MaxSize - used).ToString(CultureInfo.InvariantCulture) + " left")
                .Add(_hqBallotRefusal != null ? "Refused · " + _hqBallotRefusal : open ? "A tile pressed picks the Land" : "Closed · the lists are due");
            book.Anchors["hq:ballot:size"] = new SlipContent("SIZE").Add("Thirds of your volunteers").Add("×1 · ×2 · ×3").Add("Draft figure · 1 signature a day each");
            book.Anchors["hq:ballot:file"] = new SlipContent("FILE").Add("The notice, while it is open").Add("Every list whose signatures are in").Add(canFile ? "Something to file today" : "Nothing to file today");
            book.Anchors["hq:ballot:rules"] = new SlipContent("§ RULES").Add("Opens the country-rules slip");
            return y + HqH(4f);
        }

        // ── the country-rules slip ───────────────────────────────────────────────────────────────────────

        /// <summary>The 20 px § cell beside the race name on the HQ head (board 5c), opening the country-rules slip; returns its width, 0 where none is drawn.</summary>
        private float DrawHqRulesCell(float x, Rect head, CampaignSnapshot s, PeopleSlips.Book book)
        {
            if (_playerCountry == null || BallotAccess.RulesOf(PlayerCountryId) == BallotAccess.Rules.NotModelled) { return 0f; }
            if (BallotAccess.Created(PlayerCountryId, _playerCountry.PlayerPartyAbbrev) == null) { return 0f; }
            float side = V35.Px(20f);
            var cell = new Rect(x, head.y + Mathf.Round((head.height - side) * 0.5f), side, side);
            HqEdge(cell, PoliSimTheme.HairlineStrong, HqT(1f));
            HqLabel(cell, "§", HqMono(14f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleCenter));
            SlipAnchor(cell, "hq:rules");
            book.Anchors["hq:rules"] = RulesSlip(PlayerCountryId, s.Calendar.ElectionDate.Date, book);
            return side;
        }

        /// <summary>The country-rules slip (board 5c, level 1 four fragments, level 2 one sentence): Germany's from the BWahlG with the deadlines of the election the
        /// task is for; a country whose rules are not on disk, ABSENT.</summary>
        private static SlipContent RulesSlip(CountryId country, System.DateTime pollingDay, PeopleSlips.Book book)
        {
            if (BallotAccess.RulesOf(country) != BallotAccess.Rules.Sourced)
            {
                return new SlipContent(country + " · a new party").Add("ABSENT · not yet sourced");
            }
            BallotAccess.Deadlines(pollingDay, out System.DateTime notice, out System.DateTime _, out System.DateTime lists);
            book.Terms["Recognised"] = new SlipContent("A NEW PARTY").Add("A party not seated since the last election must be recognised and gather support signatures before it can stand.");
            return new SlipContent("Germany · a new party")
                .Add("Notice · day " + BallotAccess.DayBefore(pollingDay, notice).ToString(CultureInfo.InvariantCulture) + " · BWahlG §18")
                .Add("[[Recognised]] as a party")
                .Add("Land lists · 1 ‰, ≤ " + BallotAccess.LandListCap.ToString("N0", CultureInfo.InvariantCulture) + " · day " + BallotAccess.DayBefore(pollingDay, lists).ToString(CultureInfo.InvariantCulture))
                .Add("Constituencies · " + BallotAccess.PerConstituency.ToString(CultureInfo.InvariantCulture) + " each");
        }

        // ── the strip ────────────────────────────────────────────────────────────────────────────────────

        /// <summary>The strip's ballot chip (board 5c): refused binds (BAD) - "Not standing · 13 Länder"; collecting is coming (AMB) - "Lists due · 20 Jan"; none
        /// once on the ballot or where no task is held.</summary>
        private TopStrip.Chip BallotStripChip(System.DateTime today, out bool binds)
        {
            binds = false;
            if (_playerCountry == null || BallotAccess.RulesOf(PlayerCountryId) != BallotAccess.Rules.Sourced) { return null; }
            CreatedParty c = BallotAccess.Created(PlayerCountryId, _playerCountry.PlayerPartyAbbrev);
            if (c == null || !BallotAccess.TryElection(PlayerCountryId, today, out System.DateTime pollingDay)) { return null; }
            BallotAccess.EnsureFor(c, pollingDay);
            if (!BallotAccess.TaskIsFor(c, pollingDay)) { return null; }
            BallotAccess.TaskState state = BallotAccess.StateOf(PlayerCountryId, c, pollingDay, today);
            BallotAccess.Deadlines(pollingDay, out System.DateTime _, out System.DateTime _, out System.DateTime lists);
            if (state == BallotAccess.TaskState.Refused)
            {
                int n = BallotAccess.RefusedCount(c, pollingDay, today);
                binds = true;
                var chip = new TopStrip.Chip { Id = "strip:ballot", Face = TopStrip.Face.Bad, Words = "Not standing · " + n.ToString(CultureInfo.InvariantCulture) + " Länder", SlipHead = "REFUSED · " + n.ToString(CultureInfo.InvariantCulture) + " LÄNDER" };
                chip.Slip.Add("No list votes there"); chip.Slip.Add("Lists due " + TopStrip.Day(lists) + " · passed"); chip.Slip.Add("BWahlG §19, §27");
                return chip;
            }
            if (state != BallotAccess.TaskState.Collecting || today > lists) { return null; }
            var due = new TopStrip.Chip { Id = "strip:ballot", Face = TopStrip.Face.Amber, Words = "Lists due · " + BallotDay(lists), SlipHead = "BALLOT ACCESS · COLLECTING" };
            due.Slip.Add(BallotAccess.FiledCount(c, pollingDay).ToString(CultureInfo.InvariantCulture) + " of 16 lists filed"); due.Slip.Add("Due " + TopStrip.Day(lists) + " · 18:00"); due.Slip.Add("The HQ's left column");
            return due;
        }

        // ── the Desk: Party funding ──────────────────────────────────────────────────────────────────────

        /// <summary>Whether the Desk draws the Party funding tile, and its words (board 5c): where the law funds parties (PartG §18) and the player's party is a created
        /// one - "none" with "&lt; 0.5 %" in amber until its own count qualifies it, then the year's sum (the votes' part, DERIVED).</summary>
        private bool DeskFunding(out string value, out string sub, out bool qualifies, out double share)
        {
            value = sub = null;
            qualifies = false;
            share = 0.0;
            if (_playerCountry == null || !PartyFunding.Funds(PlayerCountryId)) { return false; }
            CreatedParty c = BallotAccess.Created(PlayerCountryId, _playerCountry.PlayerPartyAbbrev);
            if (c == null) { return false; }
            for (int i = _playerCountry.ElectionHistory.Count - 1; i >= 0; i--)
            {
                ElectionRecord rec = _playerCountry.ElectionHistory[i];
                if (rec.Shares != null && rec.Shares.TryGetValue(c.Key, out double s)) { share = s; break; }
            }
            double sum = PartyFunding.YearSum(share, PartyFunding.ListVotes(share));
            qualifies = sum > 0.0;
            value = qualifies ? "€ " + (sum >= 1e6 ? (sum / 1e6).ToString("0.00", CultureInfo.InvariantCulture) + " M" : HqN(sum)) : "none";
            sub = qualifies ? "a year · DERIVED" : "< 0.5 %";
            return true;
        }

        /// <summary>The Party funding tile (board 5c): a 1.5 px ink outline, the label, the figure and its line; its slip the rule.</summary>
        private void DrawDeskFundingTile(Rect r)
        {
            if (!DeskFunding(out string value, out string sub, out bool qualifies, out double share)) { return; }
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(r, PoliSimTheme.Card);
                float t = Mathf.Max(1f, Mathf.Round(StatsUnit(1.5f)));
                PoliSimTheme.Rule(new Rect(r.x, r.y, r.width, t), PoliSimTheme.TextPrimary);
                PoliSimTheme.Rule(new Rect(r.x, r.yMax - t, r.width, t), PoliSimTheme.TextPrimary);
                PoliSimTheme.Rule(new Rect(r.x, r.y, t, r.height), PoliSimTheme.TextPrimary);
                PoliSimTheme.Rule(new Rect(r.xMax - t, r.y, t, r.height), PoliSimTheme.TextPrimary);
                float pad = Mathf.Round(StatsUnit(7f));
                GUIStyle label = DeskCaption(7.5f, PoliSimTheme.TextMuted);
                float lh = Mathf.Ceil(DeskCaptionHeight(label));
                HqLabel(new Rect(r.x + pad, r.y + Mathf.Round(StatsUnit(4f)), r.width - 2f * pad, lh), "Party funding", label);
                GUIStyle figure = V35Serif(17f, PoliSimTheme.TextPrimary);
                float fw = Mathf.Ceil(figure.CalcSize(new GUIContent(value)).x);
                var fr = new Rect(r.x + pad, r.y + lh + Mathf.Round(StatsUnit(6f)), Mathf.Min(fw, r.width - 2f * pad), r.yMax - r.y - lh - Mathf.Round(StatsUnit(10f)));
                HqLabel(fr, value, figure);
                GUIStyle subFace = DeskCaption(7f, qualifies ? PoliSimTheme.TextMuted : PoliSimTheme.Caution, bold: !qualifies);
                HqLabel(new Rect(fr.xMax + Mathf.Round(StatsUnit(6f)), fr.y, Mathf.Max(1f, r.xMax - pad - fr.xMax - Mathf.Round(StatsUnit(6f))), fr.height), sub, subFace);
            }
            DeskAnchor(r, "desk:funding");
            _deskSlipBook.Anchors["desk:funding"] = new SlipContent("Party funding · " + value)
                .Add("From 0.5 % of the list vote").Add("or 1 % in a Landtag election")
                .Add("PartG §18 · " + PartyFunding.PerVoteFirst.ToString("0.00", CultureInfo.InvariantCulture) + " € a vote, " + PartyFunding.RatesYear.ToString(CultureInfo.InvariantCulture))
                .Add(qualifies ? "Votes' part · [[ceilings ABSENT]]" : "Last count " + (share * 100.0).ToString("0.0", CultureInfo.InvariantCulture) + " %");
            _deskSlipBook.Terms["ceilings ABSENT"] = new SlipContent("NOT COUNTED").Add("The donations' part and the two ceilings of PartG §18 are not modelled, so the sum is the list votes' part alone.");
        }
    }
}
