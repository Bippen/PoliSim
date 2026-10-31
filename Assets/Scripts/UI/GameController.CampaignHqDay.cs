using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// S4 (Design's return of 2026-10-30): the HQ's RIGHT COLUMN, the day (864, 131) 406 wide - the verbs in the board's faces, the information verbs (POLL,
    /// POLL REGION, PHONES), the composer a verb opens (AGAINST as a composer row, TOPIC as a stepper, SIZE as three cells), the queue, the field reports as
    /// their own block, and yesterday's acts in one line with the list in its slip. A story that waits for its answer takes the composer's place.
    /// </summary>
    public partial class GameController
    {
        /// <summary>The composer's key for POLL REGION, which has no verb of its own (a campaign poll queued with a region, Regional 5a) - outside the enum's values.</summary>
        private const CampaignActionKind HqPollRegion = (CampaignActionKind)(-1);

        private static readonly CampaignActionKind[] HqEight =
        {
            CampaignActionKind.Rally, CampaignActionKind.TownHall, CampaignActionKind.DoorToDoor, CampaignActionKind.TelevisionAd,
            CampaignActionKind.DigitalAd, CampaignActionKind.SocialPost, CampaignActionKind.PolicyAnnouncement, CampaignActionKind.Interview,
        };

        /// <summary>The eight's labels on their cells. INTERVIEW is drawn without the "?" the old chip carried (Design's question 3): it is a request - an outlet
        /// books it on the day (W-B9), and an unbooked one is skipped - and its slip says so.</summary>
        private static readonly string[] HqEightLabels = { "RALLY", "TOWN HALL", "DOORS", "TV AD", "DIGITAL", "POST", "POLICY", "INTERVIEW" };

        /// <summary>The verb as the composer's head and a queue row name it.</summary>
        private static string HqActName(CampaignActionKind kind, string target = null)
        {
            switch (kind)
            {
                case CampaignActionKind.Rally: return "Rally";
                case CampaignActionKind.TownHall: return "Town hall";
                case CampaignActionKind.DoorToDoor: return "Doors";
                case CampaignActionKind.TelevisionAd: return "TV ad";
                case CampaignActionKind.DigitalAd: return "Digital";
                case CampaignActionKind.SocialPost: return "Post";
                case CampaignActionKind.PolicyAnnouncement: return "Policy";
                case CampaignActionKind.Interview: return "Interview";
                case CampaignActionKind.CommissionPolling: return target != null && target.StartsWith("Regional poll", System.StringComparison.Ordinal) ? "Poll region" : "Poll";
                case CampaignActionKind.PhoneBank: return "Phones";
                case CampaignActionKind.EstablishOffice: return "Office";
                case CampaignActionKind.PrepareAdvertising: return "TV buy";
                case CampaignActionKind.Fundraise: return "Fundraise";
                case CampaignActionKind.RecruitStaff: return "Hire";
                default: return SpacedIdentifier(kind.ToString());
            }
        }

        /// <summary>Whether the live HQ can act (its run is going) - a staged film draws the verbs and acts on none.</summary>
        private bool HqLive(out bool runUp)
        {
            runUp = _liveCampaignOpen && _simulationManager != null && _simulationManager.PlayerCampaign == null && _simulationManager.PlayerPreCampaign != null && !_simulationManager.PlayerPreCampaign.Finished;
            return runUp || (_liveCampaignOpen && _simulationManager?.PlayerCampaign != null && !_simulationManager.PlayerCampaign.Finished);
        }

        /// <summary>The verb's cost on the composer, at the size chosen: purse units and hours, as the queue will carry them (`QueuedDecisionRecord.ToDecision`'s
        /// hours - the act's own whatever its size; only the price scales).</summary>
        private void HqComposerCost(CampaignSnapshot s, CampaignActionKind kind, out double money, out double hours)
        {
            money = hours = 0.0;
            PreCampaignRun.State pre = _simulationManager?.PlayerPreCampaign;
            CampaignRun.State run = _simulationManager?.PlayerCampaign;
            switch (kind)
            {
                case CampaignActionKind.EstablishOffice:
                case CampaignActionKind.PrepareAdvertising:
                case CampaignActionKind.Fundraise:
                    if (pre != null && run == null) { money = PreCampaignRun.Price(pre, kind); }
                    return;
                case CampaignActionKind.CommissionPolling:
                    if (run == null) { if (pre != null) { money = PreCampaignRun.Price(pre, kind); } return; }
                    money = run.Setup.InternalHouse.Cost; hours = CampaignAi.PollingHours; return;
                case HqPollRegion:   // POLL REGION: a poll queued with a region
                    money = RegionalPolling.Cost; hours = CampaignAi.PollingHours; return;
                case CampaignActionKind.PhoneBank:
                    money = GotvTurnout.PhoneDayMoney; hours = GotvTurnout.PhoneBankHours; return;
                default:
                    CampaignActions.ActionSpec spec = CampaignActions.Spec(kind);
                    money = spec.MoneyCost * CampaignOutlay(); hours = spec.Hours; return;
            }
        }

        /// <summary>The composer's Queue: the verb queued for the day the run steps next, with the composer's where, attack, topic and size; true when the run took it.</summary>
        private bool QueueHqComposer(CampaignSnapshot s)
        {
            if (!_hqComposer.HasValue || _simulationManager == null) { return false; }
            CampaignActionKind kind = _hqComposer.Value;
            PreCampaignRun.State pre = _simulationManager.PlayerPreCampaign;
            string refusal;
            bool ok;
            HqComposerCost(s, kind, out double money, out double _);
            if (_simulationManager.PlayerCampaign == null)
            {
                // the run-up: an office goes where the map sited it (none picked: the largest electorate without one)
                int site = kind == CampaignActionKind.EstablishOffice && pre != null ? LiveCampaignSnapshot.NextOfficeSite(pre, CampaignPick(), out _) : -1;
                ok = _simulationManager.QueueCampaignDecision(kind == HqPollRegion ? CampaignActionKind.CommissionPolling : kind, site, null, money, out refusal);
            }
            else if (kind == CampaignActionKind.CommissionPolling) { ok = _simulationManager.QueueCampaignDecision(kind, -1, null, money, out refusal); }
            else if (kind == HqPollRegion) { ok = _simulationManager.QueueCampaignDecision(CampaignActionKind.CommissionPolling, LocalActRegion(), null, money, out refusal); }   // Regional 5a: a poll of one region
            else if (kind == CampaignActionKind.PhoneBank) { ok = _simulationManager.QueueCampaignDecision(kind, LocalActRegion(), null, money, out refusal); }
            else
            {
                CampaignActions.ActionSpec spec = CampaignActions.Spec(kind);
                int region = spec.IsLocal ? LocalActRegion() : -1;   // CL-2: the map's pick, else the one-region rule
                ok = _simulationManager.QueueCampaignDecision(kind, region, CampaignTopic(), money, out refusal, againstParty: CampaignAttack());   // K3, K1 rows 9 and 12
            }
            if (refusal != null) { Debug.Log("CAMPAIGN QUEUE: " + refusal); }
            if (ok) { _hqComposer = null; }
            return ok;
        }

        // ------------------------------------------------------------------------------------------
        // The column.
        // ------------------------------------------------------------------------------------------
        private void DrawHqDay(CampaignSnapshot s, PeopleSlips.Book book)
        {
            float x = Mathf.Round(HqX(864f)), w = HqW(406f), y = Mathf.Round(HqY(131f));
            float bottom = _hqPage.yMax;
            bool live = HqLive(out bool liveRunUp);
            bool runUp = HqRunUp(s);
            string purse = Purse;

            // the head: Today, and what is queued
            HqSpends(s, out double queuedMoney, out double queuedHours, out double _, out double _);
            var head = new Rect(x, y, w, HqH(26f));
            GUIStyle head18 = HqSerif(18f, PoliSimTheme.TextPrimary);
            HqLabel(new Rect(head.x, head.y, HqWidth("Today", head18), head.height), "Today", head18);
            bool anyQueued = s.Queue != null && s.Queue.Length > 0;
            bool overHours = !runUp && queuedHours > s.Resources.Hours + 1e-9, overMoney = queuedMoney > s.Resources.Money + 1e-9;
            string queuedText = runUp ? HqMoney(queuedMoney) + " queued"
                : string.Format(CultureInfo.InvariantCulture, "{0} · {1:0.#} of {2:F0} h", HqMoney(queuedMoney), queuedHours, CampaignEconomy.HoursPerCampaignDay);
            GUIStyle queuedFace = HqMono(14f, overHours || overMoney ? PoliSimTheme.Bad : anyQueued ? PoliSimTheme.Caution : PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleRight);
            float qw = HqWidth(queuedText, queuedFace);
            var queuedRect = new Rect(head.xMax - qw, head.y, qw, head.height);
            HqLabel(queuedRect, queuedText, queuedFace);
            if (overHours || overMoney) { HqStamp(new Rect(queuedRect.x - V35.Px(8f), head.y, 1f, head.height), "OVER", PoliSimTheme.Bad, right: true); }
            SlipAnchor(queuedRect, "hq:queued");
            book.Anchors["hq:queued"] = new SlipContent("QUEUED")
                .Add("what the queue spends when the clock runs")
                .Add("it steps on " + HqDate(s.Today))
                .Add(overHours && overMoney ? "over the hours and the chest · refused" : overHours ? "over the day's hours · refused" : overMoney ? "over the chest · refused" : "within the day and the chest");
            y += head.height + HqH(2f);

            // S7 (7a): an independent's run-up opens the day on BALLOT ACCESS - GATHER, FILE, § RULES
            UsBallotAccess.Drive ballot = HqBallotDrive(s);
            if (ballot != null) { y = DrawHqBallotVerbs(x, y, w, s, ballot, book) + HqH(8f); }

            // the verbs' caption, and the verbs
            CampaignActionKind[] legal = CampaignLegality.LegalActions(s.Phase);
            bool finalWeek = s.DaysUntilElection <= GotvTurnout.PhoneFinalWeekDays && s.DaysUntilElection >= 0;
            if (!finalWeek) { legal = System.Array.FindAll(legal, k => k != CampaignActionKind.PhoneBank); }
            int all = System.Enum.GetValues(typeof(CampaignActionKind)).Length;
            string caption = (runUp ? "RUN-UP" : s.Phase == CampaignPhase.Campaign ? "CAMPAIGN" : s.Phase == CampaignPhase.ElectionDay ? "POLLING DAY" : "CLOSED")
                + " · " + legal.Length.ToString(CultureInfo.InvariantCulture) + " OF " + all.ToString(CultureInfo.InvariantCulture) + " OPEN";
            GUIStyle captionFace = HqMono(14f, PoliSimTheme.TextMuted);
            var captionRect = new Rect(x, y, w, HqH(22f));
            HqLabel(captionRect, caption, captionFace);
            SlipAnchor(captionRect, "hq:open");
            var openSlip = new SlipContent("OPEN TODAY · " + legal.Length.ToString(CultureInfo.InvariantCulture) + " OF " + all.ToString(CultureInfo.InvariantCulture));
            var names = new List<string>();
            foreach (CampaignActionKind k in legal) { names.Add(HqActName(k).ToLowerInvariant()); }
            for (int i = 0; i < names.Count && openSlip.Lines.Count < 4; i += 5) { openSlip.Add(string.Join(" · ", names.GetRange(i, Mathf.Min(5, names.Count - i)).ToArray())); }
            book.Anchors["hq:open"] = openSlip;
            y += captionRect.height;

            float gap = HqW(4f), cellH = HqH(28f);
            if (runUp) { y = DrawHqRunUpVerbs(x, y, w, s, book, live && liveRunUp); }
            else
            {
                float cw = HqW(98f);
                for (int i = 0; i < HqEight.Length; i++)
                {
                    var c = new Rect(x + (i % 4) * (cw + gap), y + (i / 4) * (cellH + gap), cw, cellH);
                    bool legalNow = CampaignLegality.IsLegal(HqEight[i], s.Phase);
                    HqVerbFace face = !legalNow ? HqVerbFace.Closed : _hqComposer.HasValue && _hqComposer.Value == HqEight[i] ? HqVerbFace.Selected : HqVerbFace.Open;
                    string id = "hq:verb:" + HqEight[i];
                    if (HqVerb(c, HqEightLabels[i], face, id) && live) { ToggleHqComposer(HqEight[i], s); }
                    book.Anchors[id] = HqVerbSlip(HqEight[i], legalNow);
                }
                y += 2f * cellH + gap;
            }

            // the information verbs: POLL, POLL REGION, PHONES
            y += HqH(8f);
            HqLabel(new Rect(x, y, w, HqH(22f)), "INFORMATION · PRICED", captionFace);
            y += HqH(22f);
            DrawHqInformationVerbs(new Rect(x, y, w, cellH), s, book, live, runUp, finalWeek);
            y += cellH;

            // the story that waits for its answer, else the composer
            if (s.Scandal.HasValue) { y = DrawHqStory(x, y + HqH(10f), w, s, book); }
            else if (_hqComposer.HasValue && live) { y = DrawHqComposer(x, y + HqH(10f), w, s, book); }

            // what is left of the column: the queue, the field reports, yesterday - rows that do not fit are counted on a line, their words on its slip
            float yesterdayH = HqH(8f) + HqH(26f);
            List<FieldReport> reports = s.Hq?.Ground?.Reports != null ? new List<FieldReport>(s.Hq.Ground.Reports) : new List<FieldReport>();
            float captionH = HqH(10f) + HqH(22f), rowQ = HqH(28f), rowR = HqH(26f);
            int queueCount = s.Queue?.Length ?? 0;
            float room = bottom - y - yesterdayH - 2f * captionH;
            int queueRows = Mathf.Max(1, queueCount), reportRows = Mathf.Max(1, reports.Count);
            while (queueRows * rowQ + reportRows * rowR > room && (queueRows > 1 || reportRows > 1))
            {
                if (reportRows >= queueRows && reportRows > 1) { reportRows--; } else if (queueRows > 1) { queueRows--; } else { reportRows--; }
            }
            y = DrawHqQueue(x, y, w, s, book, live, queueRows, purse);
            y = DrawHqReports(x, y, w, s, book, reports, reportRows);

            // yesterday
            y += HqH(8f);
            HqFill(new Rect(x, y, w, HqT(1f)), PoliSimTheme.HairlineStrong);
            var yest = new Rect(x, y, w, HqH(26f));
            int acts = s.Hq?.YesterdayActs ?? 0;
            string yesterday = acts > 0 ? "Yesterday · " + acts.ToString(CultureInfo.InvariantCulture) + (acts == 1 ? " act · " : " acts · ") + HqMoney(s.Hq.YesterdaySpend) : "Yesterday · nothing queued";
            HqLabel(yest, yesterday, HqMono(14f, PoliSimTheme.TextSecondary));
            SlipAnchor(yest, "hq:yesterday");
            var ySlip = new SlipContent("YESTERDAY");
            var lines = new List<string>();
            if (s.Hq != null) { foreach (string l in s.Hq.YesterdayLines) { string d = HqNoteLine(l); if (!lines.Contains(d)) { lines.Add(d); } } }
            // the run's intelligence lines (Regional 5a) are drawn as the field reports and the tiles now, not as yesterday's events
            if (s.Notes != null) { foreach (string n in s.Notes) { if (n.StartsWith("FIELD REPORT · ", System.StringComparison.Ordinal) || n.StartsWith("REGIONAL POLL · ", System.StringComparison.Ordinal)) { continue; } string d = HqNoteLine(n); if (!lines.Contains(d)) { lines.Add(d); } } }
            for (int i = 0; i < lines.Count && i < 3; i++) { ySlip.Add(lines[i]); }
            if (lines.Count > 3) { ySlip.Add("+" + (lines.Count - 3).ToString(CultureInfo.InvariantCulture) + " more"); }
            if (lines.Count == 0) { ySlip.Add("nothing ran yesterday"); }
            book.Anchors["hq:yesterday"] = ySlip;
        }

        /// <summary>A note as the yesterday slip lists it: the run's line without its "YESTERDAY · " head (the slip's own head says it), an okreg with its diacritic.</summary>
        private static string HqNoteLine(string note)
        {
            if (string.IsNullOrEmpty(note)) { return string.Empty; }
            string l = note.StartsWith("YESTERDAY · ", System.StringComparison.Ordinal) ? note.Substring("YESTERDAY · ".Length) : note;
            return l.Replace("okreg ", "okręg ").Replace("OKREG ", "OKRĘG ");
        }

        /// <summary>A campaign verb's slip: local or national, the hours and the price, and the interview's booking rule.</summary>
        private SlipContent HqVerbSlip(CampaignActionKind kind, bool legalNow)
        {
            CampaignActions.ActionSpec spec = CampaignActions.Spec(kind);
            var slip = new SlipContent(HqActName(kind).ToUpperInvariant());
            slip.Add(spec.IsLocal ? "local · lands in the target region" : "national · reaches the whole country");
            slip.Add(string.Format(CultureInfo.InvariantCulture, "{0:0.#} h · {1}", spec.Hours, spec.MoneyCost > 0.0 ? HqMoney(spec.MoneyCost) + " at ×1" : "no price"));
            if (kind == CampaignActionKind.Interview) { slip.Add("a request · an outlet books it on the day").Add("unbooked: skipped, the day's queue stands"); }
            else if (!legalNow) { slip.Add("not open in this phase"); }
            return slip;
        }

        /// <summary>The run-up's verb row - OFFICE, TV BUY, FUNDRAISE - and the HIRE group, its five posts (a post on the roster is done; an open one hires).</summary>
        private float DrawHqRunUpVerbs(float x, float y, float w, CampaignSnapshot s, PeopleSlips.Book book, bool live)
        {
            PreCampaignRun.State pre = _simulationManager?.PlayerPreCampaign;
            float gap = HqW(4f), cellH = HqH(28f);
            CampaignActionKind[] verbs = { CampaignActionKind.EstablishOffice, CampaignActionKind.PrepareAdvertising, CampaignActionKind.Fundraise };
            string[] labels = { "OFFICE", "TV BUY", "FUNDRAISE" };
            float[] widths = { HqW(132f), HqW(132f), HqW(134f) };
            double tariff = PreCampaignRun.LetterTariff(PlayerCountryId);
            float cx = x;
            for (int i = 0; i < verbs.Length; i++)
            {
                var c = new Rect(cx, y, widths[i], cellH);
                HqVerbFace face;
                if (verbs[i] == CampaignActionKind.Fundraise && double.IsNaN(tariff)) { face = HqVerbFace.Absent; }
                else if (verbs[i] == CampaignActionKind.Fundraise && (pre == null || !PreCampaignRun.FundraiseOpen(pre))) { face = HqVerbFace.Closed; }
                else { face = _hqComposer.HasValue && _hqComposer.Value == verbs[i] ? HqVerbFace.Selected : HqVerbFace.Open; }
                string id = "hq:verb:" + verbs[i];
                if (HqVerb(c, labels[i], face, id) && live) { ToggleHqComposer(verbs[i], s); }
                book.Anchors[id] = verbs[i] == CampaignActionKind.EstablishOffice
                    ? new SlipContent("OFFICE").Add("the map becomes the picker").Add("opens on the campaign's first day").Add("its doors raise your supporters' turnout")
                    : verbs[i] == CampaignActionKind.PrepareAdvertising
                        ? new SlipContent("TV BUY").Add("books television for the campaign").Add("on the polls for a week · faded by polling day")
                        : face == HqVerbFace.Absent
                            ? new SlipContent(SymbolRegistry.Word(Symbol.Absent)).Add("fundraise · no postal tariff on disk").Add("no letter can be priced")
                            : new SlipContent("FUNDRAISE").Add(pre != null ? PreCampaignRun.FundraiseLine(pre, Purse).ToLowerInvariant() : "a mailing to the party's supporters");
                cx += widths[i] + gap;
            }
            y += cellH + gap;

            // HIRE, and the five posts
            var hire = new Rect(x, y, HqW(52f), cellH);
            HqVerb(hire, "HIRE", HqVerbFace.Done, "hq:hire");
            book.Anchors["hq:hire"] = new SlipContent("HIRE").Add("five posts · a cell is a post").Add(HqMoney(CampaignStaff.SalaryPerDay) + " a day each, from the day after");
            string[] posts = { "MGR", "MEDIA", "POLL", "FIELD", "DIGI" };
            float pw = HqW(64f);
            for (int i = 0; i < CampaignStaff.TheFive.Length && i < posts.Length; i++)
            {
                StaffRole role = CampaignStaff.TheFive[i];
                bool held = pre != null ? pre.HasRole(role) : (s.Hq != null && s.Hq.StaffStates[i] > 0);
                var c = new Rect(hire.xMax + gap + i * (pw + gap), y, pw, cellH);
                string id = "hq:post:" + role;
                if (HqVerb(c, posts[i], held ? HqVerbFace.Done : HqVerbFace.Open, id) && live && !held)
                {
                    _simulationManager.QueueCampaignDecision(CampaignActionKind.RecruitStaff, -1, null, CampaignStaff.SalaryPerDay, out string refusal, role: (int)role);
                    if (refusal != null) { Debug.Log("CAMPAIGN QUEUE: " + refusal); }
                }
                book.Anchors[id] = new SlipContent(PreCampaignRun.RoleName(role).ToUpperInvariant()).Add(held ? "on the roster" : "open · " + HqMoney(CampaignStaff.SalaryPerDay) + " a day");
            }
            return y + cellH;
        }

        /// <summary>POLL, POLL REGION and PHONES (Design's items 2, 4 and 7): POLL REGION closed in the run-up (it opens with the campaign) and where the staging
        /// carries no regional derivation (a presidential count is national); PHONES dashed with its opening date until the final week.</summary>
        private void DrawHqInformationVerbs(Rect row, CampaignSnapshot s, PeopleSlips.Book book, bool live, bool runUp, bool finalWeek)
        {
            float gap = HqW(4f);
            CampaignGround ground = s.Hq?.Ground;
            var poll = new Rect(row.x, row.y, HqW(98f), row.height);
            var region = new Rect(poll.xMax + gap, row.y, HqW(150f), row.height);
            var phones = new Rect(region.xMax + gap, row.y, HqW(150f), row.height);

            bool pollLegal = CampaignLegality.IsLegal(CampaignActionKind.CommissionPolling, s.Phase);
            HqVerbFace pollFace = !pollLegal ? HqVerbFace.Closed : _hqComposer == CampaignActionKind.CommissionPolling ? HqVerbFace.Selected : HqVerbFace.Open;
            if (HqVerb(poll, "POLL", pollFace, "hq:poll") && live) { ToggleHqComposer(CampaignActionKind.CommissionPolling, s); }
            CampaignRun.State run = _simulationManager?.PlayerCampaign;
            string pollPrice = run != null ? HqMoney(run.Setup.InternalHouse.Cost) : _simulationManager?.PlayerPreCampaign != null ? HqMoney(PreCampaignRun.Price(_simulationManager.PlayerPreCampaign, CampaignActionKind.CommissionPolling)) : null;
            book.Anchors["hq:poll"] = new SlipContent("POLL").Add("a national poll of your own").Add("replaces the tracker in the race").Add(pollPrice != null ? pollPrice + " · " + SymbolRegistry.Word(Symbol.Billed) : SymbolRegistry.Word(Symbol.Billed));

            bool onSale = ground != null && ground.RegionalPollOnSale;
            bool regionOpen = !runUp && onSale && pollLegal && LocalActRegion() >= 0;
            HqVerbFace regionFace = !regionOpen ? HqVerbFace.Closed : _hqComposer == HqPollRegion ? HqVerbFace.Selected : HqVerbFace.Open;
            if (HqVerb(region, "POLL REGION", regionFace, "hq:pollregion") && live) { ToggleHqComposer(HqPollRegion, s); }
            var regionSlip = new SlipContent("POLL REGION");
            if (!onSale) { regionSlip.Add("closed in this race").Add("its count is national · no region to poll"); }
            else if (runUp) { regionSlip.Add("opens with the campaign, " + HqDate(s.Calendar.CampaignStart)); }
            else { regionSlip.Add("a poll of the target region only").Add("lands on the tile and the plate"); }
            if (onSale && ground != null)
            {
                regionSlip.Add(string.Format(CultureInfo.InvariantCulture, "n {0:N0} · ±{1:0.0} · {2}", ground.RegionalSample, RegionalPolling.MarginOfErrorPp(0.5, ground.RegionalSample), HqMoney(ground.RegionalPrice)));
                regionSlip.Add("narrower than the national poll's slice");
            }
            book.Anchors["hq:pollregion"] = regionSlip;

            bool phonesLegal = finalWeek && CampaignLegality.IsLegal(CampaignActionKind.PhoneBank, s.Phase);
            System.DateTime opens = HqFinalWeekOpens(s);
            string phonesLabel = phonesLegal || s.DaysUntilElection < 0 ? "PHONES" : "PHONES · " + HqDay(opens).ToUpperInvariant();
            HqVerbFace phonesFace = !phonesLegal ? HqVerbFace.Closed : _hqComposer == CampaignActionKind.PhoneBank ? HqVerbFace.Selected : HqVerbFace.Open;
            if (HqVerb(phones, phonesLabel, phonesFace, "hq:phones") && live) { ToggleHqComposer(CampaignActionKind.PhoneBank, s); }
            book.Anchors["hq:phones"] = new SlipContent("PHONES").Add("volunteers call your supporters out").Add(phonesLegal ? "open in the final week" : "opens with the final week, " + HqDate(opens))
                .Add(HqMoney(GotvTurnout.PhoneDayMoney) + " · " + HqF(GotvTurnout.PhoneBankHours, 0) + " h · in the target region");
        }

        /// <summary>The composer (board 4c-4e): the verb, where it lands, its cost; the AGAINST, TOPIC and SIZE rows for the eight; a foot note; Cancel and Queue.</summary>
        private float DrawHqComposer(float x, float y, float w, CampaignSnapshot s, PeopleSlips.Book book)
        {
            CampaignActionKind kind = _hqComposer.Value;
            bool eight = System.Array.IndexOf(HqEight, kind) >= 0;
            CampaignActions.ActionSpec spec = eight ? CampaignActions.Spec(kind) : default;
            HqComposerCost(s, kind, out double money, out double hours);
            float padX = HqW(10f), rowH = HqH(28f), cellH = HqH(26f);
            int rows = eight ? (spec.MoneyCost > 0.0 ? 3 : 2) : 0;
            float h = HqH(6f) + HqH(26f) + rows * (rowH + HqH(5f)) + HqH(5f) + HqH(30f) + HqH(8f);
            var plate = new Rect(x, y, w, h);
            HqFill(plate, PoliSimTheme.CardInset);
            HqEdge(plate, PoliSimTheme.RuleRow, HqT(1f));
            float ix = plate.x + padX, iw = plate.width - 2f * padX, iy = plate.y + HqH(6f);

            // the head: the verb, where, the cost
            string verb = (kind == HqPollRegion ? "Poll region" : HqActName(kind)) + (eight && spec.MoneyCost > 0.0 && CampaignOutlay() > 1 ? " ×" + CampaignOutlay().ToString(CultureInfo.InvariantCulture) : string.Empty);
            bool local = (eight && spec.IsLocal) || kind == CampaignActionKind.PhoneBank || kind == HqPollRegion;
            CampaignGround ground = s.Hq?.Ground;
            int whereRegion = kind == CampaignActionKind.EstablishOffice ? (ground != null ? ground.Target : -1) : local ? LocalActRegion() : -1;
            string[] whereForms = whereRegion >= 0 && ground != null && whereRegion < ground.Names.Length ? HqRegionForms(s, ground.Names[whereRegion])
                : new[] { kind == CampaignActionKind.EstablishOffice ? "no region left" : local ? "no region" : kind == CampaignActionKind.PrepareAdvertising ? "television" : "national" };
            string where = whereForms[0];
            string cost = (hours > 0.0 ? HqF(hours, 0) + " h · " : string.Empty) + HqMoney(money) + (kind == CampaignActionKind.EstablishOffice ? " to open" : string.Empty);
            var headRow = new Rect(ix, iy, iw, HqH(26f));
            GUIStyle verbFace = HqSerif(17f, PoliSimTheme.TextPrimary);
            float vw = HqWidth(verb, verbFace);
            HqLabel(new Rect(headRow.x, headRow.y, vw, headRow.height), verb, verbFace);
            GUIStyle costFace = HqMono(14f, PoliSimTheme.Caution, bold: true, anchor: TextAnchor.MiddleRight);
            float costW = HqWidth(cost, costFace);
            HqLabel(new Rect(headRow.xMax - costW, headRow.y, costW, headRow.height), cost, costFace);
            GUIStyle whereFace = HqMono(14f, PoliSimTheme.TextSecondary);
            float whereX = headRow.x + vw + V35.Px(8f);
            string whereShown = HqFirstFitting(whereFace, Mathf.Max(1f, headRow.xMax - costW - V35.Px(8f) - whereX), whereForms);
            HqLabel(new Rect(whereX, headRow.y, Mathf.Max(1f, headRow.xMax - costW - V35.Px(8f) - whereX), headRow.height), whereShown, whereFace);
            SlipAnchor(headRow, "hq:composer");
            book.Anchors["hq:composer"] = new SlipContent(verb.ToUpperInvariant() + " · " + where)
                .Add(cost)
                .Add(local ? "lands in the target region" : "a national act")
                .Add("outlined on the day and the cap until queued");
            iy += headRow.height;

            string foot = kind == CampaignActionKind.EstablishOffice ? "upkeep " + HqMoney(ground?.OfficeUpkeepPerDay ?? 0.0) + "/day"
                : kind == CampaignActionKind.PrepareAdvertising ? "a week on the polls"
                : kind == CampaignActionKind.Fundraise ? "a mailing"
                : kind == CampaignActionKind.CommissionPolling ? "your own house · " + SymbolRegistry.Word(Symbol.Billed)
                : kind == HqPollRegion ? "target region · " + SymbolRegistry.Word(Symbol.Billed)
                : kind == CampaignActionKind.PhoneBank ? "your supporters, by phone"
                : kind == CampaignActionKind.Interview ? "a request"
                : string.Empty;

            if (eight)
            {
                iy += HqH(5f);
                // AGAINST: none, then the rivals' marks - only those standing where the act lands (a national act: anywhere)
                var against = new Rect(ix, iy, iw, rowH);
                DrawHqComposerLabel(against, "Against");
                int attack = CampaignAttack();
                float cx = against.x + HqW(62f) + HqW(4f);
                if (HqCell(new Rect(cx, against.y + Mathf.Round((rowH - cellH) * 0.5f), HqW(54f), cellH), "none", attack < 0, null, null)) { SetHqAttack(-1); }
                cx += HqW(54f) + HqW(4f);
                foreach (int rival in HqRivals(s, local))
                {
                    var c = new Rect(cx, against.y + Mathf.Round((rowH - cellH) * 0.5f), HqW(30f), cellH);
                    if (c.xMax > against.xMax) { break; }
                    if (HqCell(c, null, attack == rival, s.PartyNames[rival], "hq:against:" + rival)) { SetHqAttack(attack == rival ? -1 : rival); }
                    book.Anchors["hq:against:" + rival] = new SlipContent("AGAINST · " + HqShort(s.PartyNames[rival])).Add("a named attack on a rival standing here").Add("none by default · named in the queue row");
                    cx += HqW(30f) + HqW(4f);
                }
                if (attack >= 0) { foot = "a named attack on " + HqShort(s.PartyNames[attack]); }
                iy += rowH + HqH(5f);

                // TOPIC: a stepper over the general message and the issues the campaign contests
                var topicRow = new Rect(ix, iy, iw, rowH);
                DrawHqComposerLabel(topicRow, "Topic");
                List<int> topics = HqTopicOptions();
                int at = Mathf.Max(0, topics.IndexOf(CampaignTopic().HasValue ? (int)CampaignTopic().Value : -1));
                float ty = topicRow.y + Mathf.Round((rowH - cellH) * 0.5f);
                float tx = topicRow.x + HqW(62f) + HqW(4f);
                if (HqCell(new Rect(tx, ty, HqW(28f), cellH), "◂", false, null, null)) { StepHqTopic(topics, at - 1); }
                tx += HqW(28f) + HqW(4f);
                string topicName = HqTopicName(topics.Count > 0 ? topics[at] : -1);
                HqCell(new Rect(tx, ty, HqW(110f), cellH), topicName, true, null, null);
                tx += HqW(110f) + HqW(4f);
                if (HqCell(new Rect(tx, ty, HqW(28f), cellH), "▸", false, null, null)) { StepHqTopic(topics, at + 1); }
                string ofN = (at + 1).ToString(CultureInfo.InvariantCulture) + " of " + Mathf.Max(1, topics.Count).ToString(CultureInfo.InvariantCulture);
                GUIStyle note = HqMono(14f, PoliSimTheme.TextMuted, anchor: TextAnchor.MiddleRight);
                HqLabel(new Rect(topicRow.xMax - HqWidth(ofN, note), topicRow.y, HqWidth(ofN, note), topicRow.height), ofN, note);
                SlipAnchor(topicRow, "hq:topic");
                var topicSlip = new SlipContent("TOPIC · " + ofN);
                var topicNames = new List<string>();
                foreach (int t in topics) { topicNames.Add(HqTopicName(t)); }
                for (int i = 0; i < topicNames.Count && topicSlip.Lines.Count < 3; i += 4) { topicSlip.Add(string.Join(" · ", topicNames.GetRange(i, Mathf.Min(4, topicNames.Count - i)).ToArray())); }
                topicSlip.Add("the issues this campaign contests");
                book.Anchors["hq:topic"] = topicSlip;
                iy += rowH + HqH(5f);

                // SIZE: three cells (an act with no price - the interview - has none)
                if (spec.MoneyCost > 0.0)
                {
                    var sizeRow = new Rect(ix, iy, iw, rowH);
                    DrawHqComposerLabel(sizeRow, "Size");
                    float sx = sizeRow.x + HqW(62f) + HqW(4f);
                    for (int k = 1; k <= 3; k++)
                    {
                        if (HqCell(new Rect(sx, sizeRow.y + Mathf.Round((rowH - cellH) * 0.5f), HqW(40f), cellH), "×" + k.ToString(CultureInfo.InvariantCulture), CampaignOutlay() == k, null, null)) { SetHqOutlay(k); }
                        sx += HqW(40f) + HqW(4f);
                    }
                    string sizeNote = "cost ×" + CampaignOutlay().ToString(CultureInfo.InvariantCulture);
                    HqLabel(new Rect(sizeRow.xMax - HqWidth(sizeNote, note), sizeRow.y, HqWidth(sizeNote, note), sizeRow.height), sizeNote, note);
                    SlipAnchor(sizeRow, "hq:size");
                    book.Anchors["hq:size"] = new SlipContent("SIZE · ×" + CampaignOutlay().ToString(CultureInfo.InvariantCulture)).Add("×1 · ×2 · ×3 the act's price").Add("more exposures, which coverage saturates").Add("the hours stay the act's own");
                    iy += rowH + HqH(5f);
                }
            }

            // the foot: the note, Cancel, Queue
            var footRow = new Rect(ix, iy + HqH(2f), iw, HqH(30f));
            GUIStyle footFace = HqMono(14f, PoliSimTheme.TextMuted);
            GUIStyle button = HqSerif(15f, PoliSimTheme.TextPrimary, anchor: TextAnchor.MiddleCenter);
            float qW = HqWidth("Queue", button) + HqW(28f), cW = HqWidth("Cancel", button) + HqW(24f), bh = HqH(28f);
            var queue = new Rect(footRow.xMax - qW, footRow.y + Mathf.Round((footRow.height - bh) * 0.5f), qW, bh);
            var cancel = new Rect(queue.x - HqW(6f) - cW, queue.y, cW, bh);
            float noteW = Mathf.Max(1f, cancel.x - V35.Px(6f) - footRow.x);
            HqLabel(new Rect(footRow.x, footRow.y, noteW, footRow.height), HqFirstFitting(footFace, noteW, foot, foot.Split(new[] { " · " }, System.StringSplitOptions.None)[0]), footFace);
            HqEdge(cancel, PoliSimTheme.HairlineStrong, HqT(1f));
            HqLabel(cancel, "Cancel", button);
            if (PoliSimWidgets.Button(cancel, GUIContent.none, GUIStyle.none)) { _hqComposer = null; }
            HqFill(queue, PoliSimTheme.BrassLight);
            HqEdge(queue, PoliSimTheme.BrassBorder, HqT(1f));
            HqLabel(queue, "Queue", button);
            if (PoliSimWidgets.Button(queue, GUIContent.none, GUIStyle.none)) { QueueHqComposer(s); }
            return plate.yMax;
        }

        private void DrawHqComposerLabel(Rect row, string label) =>
            HqLabel(new Rect(row.x, row.y, HqW(62f), row.height), label, HqMono(14f, PoliSimTheme.TextMuted));

        /// <summary>A composer cell: its word, or a party's mark (the rival's - in paper on the ink where picked: the S4 white mark, its short name where the
        /// glyph is not held); ink where picked. True when pressed.</summary>
        private bool HqCell(Rect r, string text, bool picked, string partyKey, string slipId)
        {
            HqFill(r, picked ? PoliSimTheme.TextPrimary : PoliSimTheme.Card);
            HqEdge(r, picked ? PoliSimTheme.TextPrimary : PoliSimTheme.HairlineStrong, HqT(1f));
            Color ink = picked ? PoliSimTheme.ModalBackground : PoliSimTheme.TextPrimary;
            if (partyKey != null)
            {
                float side = V35.Px(14f);
                var m = new Rect(r.center.x - side * 0.5f, r.center.y - side * 0.5f, side, side);
                Texture2D glyph = picked ? HqPartyGlyph(partyKey) : IconLibrary.GetPartyMark(HqMarkKey(partyKey));
                if (glyph != null && Event.current.type == EventType.Repaint)
                {
                    Color was = GUI.color;
                    if (picked) { GUI.color = PoliSimTheme.ModalBackground; }
                    GUI.DrawTexture(m, glyph, ScaleMode.ScaleToFit, true);
                    GUI.color = was;
                }
                else if (glyph == null) { HqLabel(r, HqShort(partyKey), HqMono(14f, ink, bold: true, anchor: TextAnchor.MiddleCenter)); }
            }
            else { HqLabel(r, text, HqMono(14f, ink, bold: true, anchor: TextAnchor.MiddleCenter)); }
            if (slipId != null) { SlipAnchor(r, slipId); }
            return _liveCampaignOpen && PoliSimWidgets.Button(r, GUIContent.none, GUIStyle.none);
        }

        /// <summary>A Polish party's S4 white mark (Design's glyphs, 28 px at 2560), or null - every other country's, and every glyph not installed.</summary>
        private Texture2D HqPartyGlyph(string partyKey)
        {
            if (PlayerCountryId != CountryId.Poland || string.IsNullOrEmpty(partyKey)) { return null; }
            return IconLibrary.HqGlyph("mark_pl_" + partyKey.ToLowerInvariant() + "_w_" + (V35.Scale >= 1.5f ? "28" : "14"));
        }

        /// <summary>The rivals the AGAINST row offers: every other party on the ballot that shares voters with the player's (review 2's L6), standing in the
        /// target region where the act is local (Design's item 1).</summary>
        private List<int> HqRivals(CampaignSnapshot s, bool local)
        {
            var rivals = new List<int>();
            CampaignRun.State run = _simulationManager?.PlayerCampaign;
            if (run == null) { return rivals; }
            int own = LiveCampaignSnapshot.PlayerPartyIndex(run, _playerCountry);
            int region = local ? LocalActRegion() : -1;
            for (int p = 0; p < run.Setup.Parties.Length; p++)
            {
                if (p == own || (s.Hq != null && !s.Hq.Stands(p))) { continue; }
                if (own >= 0 && run.Pressure.TargetShareOfReach(own, p) <= 0.0) { continue; }
                if (region >= 0 && !run.Setup.StandsIn(p, region)) { continue; }
                rivals.Add(p);
            }
            return rivals;
        }

        /// <summary>The film's hook (S4's staged shots): the composer opened on <paramref name="kind"/> with the attack, the size and the topic a player would
        /// set on it - each through the same setter the composer's cells call - or, with <paramref name="kind"/> null, every one put back (no composer, no
        /// attack, ×1, the general message). Returns the rival attacked, −1 for none (the first rival the AGAINST row offers where <paramref name="attackFirstRival"/>).</summary>
        internal int StageHqComposerForFilm(CampaignActionKind? kind, bool attackFirstRival, int outlay, int topicStep)
        {
            ComposeForFilm(kind);
            if (!kind.HasValue)
            {
                SetHqAttack(-1);
                SetHqOutlay(1);
                StepHqTopic(HqTopicOptions(), 0);
                return -1;
            }
            int rival = -1;
            if (attackFirstRival && _campaignScreen.HasValue)
            {
                bool local = System.Array.IndexOf(HqEight, kind.Value) >= 0 && CampaignActions.Spec(kind.Value).IsLocal;
                List<int> rivals = HqRivals(_campaignScreen.Value, local);
                if (rivals.Count > 0) { rival = rivals[0]; }
            }
            SetHqAttack(rival);
            SetHqOutlay(outlay);
            StepHqTopic(HqTopicOptions(), topicStep);
            return rival;
        }

        /// <summary>K3: the attack target set from the AGAINST row - a rival, or none (−1).</summary>
        private void SetHqAttack(int party)
        {
            CampaignRun.State run = _simulationManager?.PlayerCampaign;
            if (run == null) { return; }
            _campaignAttackTarget = party;
            _campaignAttackElection = run.Setup.Calendar.ElectionDate;
        }

        /// <summary>K1 row 12: the outlay set from the SIZE cells - 1, 2 or 3 times the act's price.</summary>
        private void SetHqOutlay(int k)
        {
            CampaignRun.State run = _simulationManager?.PlayerCampaign;
            if (run == null) { return; }
            if (_campaignTopicElection != run.Setup.Calendar.ElectionDate) { _campaignTopic = -1; }
            _campaignOutlay = Mathf.Clamp(k, 1, 3);
            _campaignTopicElection = run.Setup.Calendar.ElectionDate;
        }

        /// <summary>K1 row 9: the TOPIC stepper's options - the general message (−1), then every issue the campaign contests (its salience is defined), in
        /// `IssueId`'s order.</summary>
        private List<int> HqTopicOptions()
        {
            var options = new List<int> { -1 };
            double[] salience = _simulationManager?.PlayerCampaign?.Setup.TrueSalience;
            if (salience == null) { return options; }
            for (int i = 0; i < salience.Length; i++) { if (!double.IsNaN(salience[i])) { options.Add(i); } }
            return options;
        }

        /// <summary>The stepper moved to option <paramref name="index"/> (wrapping).</summary>
        private void StepHqTopic(List<int> options, int index)
        {
            CampaignRun.State run = _simulationManager?.PlayerCampaign;
            if (run == null || options.Count == 0) { return; }
            int i = ((index % options.Count) + options.Count) % options.Count;
            if (_campaignTopicElection != run.Setup.Calendar.ElectionDate) { _campaignOutlay = 1; }
            _campaignTopic = options[i];
            _campaignTopicElection = run.Setup.Calendar.ElectionDate;
        }

        /// <summary>A topic's word: the general message, or the issue's name (`IssueId`).</summary>
        private static string HqTopicName(int topic) => topic < 0 ? "General" : SpacedIdentifier(((IssueId)topic).ToString());

        /// <summary>The queue: its caption (and CLEAR), a row per act - the act, where and against whom, the hours and the price - and the rows that do not fit
        /// counted on one line.</summary>
        private float DrawHqQueue(float x, float y, float w, CampaignSnapshot s, PeopleSlips.Book book, bool live, int rows, string purse)
        {
            y += HqH(10f);
            GUIStyle captionFace = HqMono(14f, PoliSimTheme.TextMuted);
            var caption = new Rect(x, y, w, HqH(22f));
            HqLabel(caption, "QUEUED TODAY", captionFace);
            int count = s.Queue?.Length ?? 0;
            if (live && count > 0)
            {
                GUIStyle clear = HqMono(14f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleCenter);
                float cw = HqWidth("CLEAR", clear) + HqW(16f);
                var c = new Rect(caption.xMax - cw, caption.y + HqH(1f), cw, caption.height - HqH(2f));
                if (HqVerb(c, "CLEAR", HqVerbFace.Done, "hq:clear")) { _simulationManager.ClearCampaignQueue(); }
                book.Anchors["hq:clear"] = new SlipContent("CLEAR").Add("empties the next day's queue");
            }
            y += caption.height;
            GUIStyle act = HqSerif(15f, PoliSimTheme.TextPrimary);
            GUIStyle mods = HqMono(14f, PoliSimTheme.TextSecondary);
            GUIStyle cost = HqMono(14f, PoliSimTheme.Caution, bold: true, anchor: TextAnchor.MiddleRight);
            if (count == 0)
            {
                HqFill(new Rect(x, y, w, HqT(1f)), PoliSimTheme.RuleRow);
                var empty = new Rect(x, y, w, HqH(28f));
                HqLabel(empty, "nothing queued", HqSerif(15f, PoliSimTheme.TextSecondary));
                SlipAnchor(empty, "hq:empty");
                book.Anchors["hq:empty"] = HqRunUp(s) ? new SlipContent("NOTHING QUEUED").Add("run-up acts cost money, not hours")
                    : new SlipContent("NOTHING QUEUED").Add(string.Format(CultureInfo.InvariantCulture, "the day's {0:F0} hours are free", CampaignEconomy.HoursPerCampaignDay));
                return y + HqH(28f);
            }
            int shown = count > rows ? Mathf.Max(0, rows - 1) : count;
            for (int i = 0; i < shown; i++)
            {
                QueuedAction q = s.Queue[i];
                HqFill(new Rect(x, y, w, HqT(1f)), PoliSimTheme.RuleRow);
                var row = new Rect(x, y, w, HqH(28f));
                HqFill(new Rect(row.x, row.y + HqH(4f), HqW(4f), HqH(20f)), PoliSimTheme.Draft);
                string name = HqActName(q.Kind, q.TargetLabel);
                string where = HqQueueTarget(q.TargetLabel);
                string price = (q.Hours > 0.0 ? HqF(q.Hours, 0) + " h · " : string.Empty) + HqMoney(q.MoneyCost);
                float px = row.x + HqW(10f);
                float nw = HqWidth(name, act), pw = HqWidth(price, cost);
                HqLabel(new Rect(px, row.y, nw, row.height), name, act);
                float mx = px + nw + V35.Px(8f), mw = Mathf.Max(1f, row.xMax - pw - V35.Px(8f) - mx);
                HqLabel(new Rect(mx, row.y, mw, row.height), HqFirstFitting(mods, mw, HqQueueTargetForms(s, q.TargetLabel)), mods);
                HqLabel(new Rect(row.xMax - pw, row.y, pw, row.height), price, cost);
                SlipAnchor(row, "hq:q:" + i.ToString(CultureInfo.InvariantCulture));
                book.Anchors["hq:q:" + i.ToString(CultureInfo.InvariantCulture)] = new SlipContent(name.ToUpperInvariant() + " · " + SymbolRegistry.Word(Symbol.Draft))
                    .Add(where).Add(price).Add("draft until the clock runs");
                y += row.height;
            }
            if (shown < count)
            {
                HqFill(new Rect(x, y, w, HqT(1f)), PoliSimTheme.RuleRow);
                var more = new Rect(x, y, w, HqH(28f));
                HqLabel(more, "+" + (count - shown).ToString(CultureInfo.InvariantCulture) + " more queued", mods);
                SlipAnchor(more, "hq:qmore");
                var slip = new SlipContent("QUEUED · " + count.ToString(CultureInfo.InvariantCulture));
                for (int i = shown; i < count && slip.Lines.Count < 4; i++) { slip.Add(HqActName(s.Queue[i].Kind, s.Queue[i].TargetLabel) + " · " + HqQueueTarget(s.Queue[i].TargetLabel) + " · " + HqMoney(s.Queue[i].MoneyCost)); }
                book.Anchors["hq:qmore"] = slip;
                y += more.height;
            }
            return y;
        }

        /// <summary>A queue row's where, longest first: the region's forms (its label, its name, its tile's id) with the attack after them, then the attack alone.</summary>
        private string[] HqQueueTargetForms(CampaignSnapshot s, string target)
        {
            if (string.IsNullOrEmpty(target)) { return new[] { string.Empty }; }
            string t = target.StartsWith("Regional poll · ", System.StringComparison.Ordinal) ? target.Substring("Regional poll · ".Length) : target;
            int v = t.IndexOf(" · v ", System.StringComparison.Ordinal);
            string region = v >= 0 ? t.Substring(0, v) : t, against = v >= 0 ? "against " + t.Substring(v + 5) : null;
            var forms = new List<string>();
            foreach (string f in HqRegionForms(s, region)) { if (!string.IsNullOrEmpty(f)) { forms.Add(against != null ? f + " · " + against : f); } }
            if (against != null) { forms.Add(against); }
            return forms.ToArray();
        }

        /// <summary>A queue row's where, as the player reads it: an okreg with its diacritic, an attack as "against" (the record's "v").</summary>
        private static string HqQueueTarget(string target)
        {
            if (string.IsNullOrEmpty(target)) { return string.Empty; }
            string t = target.Replace(" · v ", " · against ");
            if (t.StartsWith("Regional poll · ", System.StringComparison.Ordinal)) { t = t.Substring("Regional poll · ".Length); }
            return CampaignTiles.Display(t);
        }

        /// <summary>The field reports (K83's intelligence as information, Design's regional surfaces): their own block, never mixed with the day's acts - the
        /// report glyph, the region, volunteers · doors · calls.</summary>
        private float DrawHqReports(float x, float y, float w, CampaignSnapshot s, PeopleSlips.Book book, List<FieldReport> reports, int rows)
        {
            y += HqH(10f);
            GUIStyle captionFace = HqMono(14f, PoliSimTheme.TextMuted);
            var caption = new Rect(x, y, w, HqH(22f));
            HqLabel(caption, "FIELD REPORTS", captionFace);
            if (reports.Count > 0)
            {
                string note = "INFORMATION · " + reports.Count.ToString(CultureInfo.InvariantCulture) + " TODAY";
                GUIStyle right = HqMono(14f, PoliSimTheme.TextMuted, anchor: TextAnchor.MiddleRight);
                HqLabel(new Rect(caption.xMax - HqWidth(note, right), caption.y, HqWidth(note, right), caption.height), note, right);
            }
            SlipAnchor(caption, "hq:reports");
            book.Anchors["hq:reports"] = new SlipContent("FIELD REPORTS").Add("information · nothing to decide").Add("one per open office").Add("doors and calls so far, your own books");
            y += caption.height;
            GUIStyle mono = HqMono(14f, PoliSimTheme.TextPrimary);
            GUIStyle bold = HqMono(14f, PoliSimTheme.TextPrimary, bold: true);
            if (reports.Count == 0)
            {
                HqFill(new Rect(x, y, w, HqT(1f)), PoliSimTheme.RuleRow);
                HqLabel(new Rect(x, y, w, HqH(26f)), "no reports yet", HqSerif(15f, PoliSimTheme.TextSecondary));
                return y + HqH(26f);
            }
            int shown = reports.Count > rows ? Mathf.Max(0, rows - 1) : reports.Count;
            float glyphSide = V35.Px(16f);
            Texture2D glyph = IconLibrary.HqGlyph(V35.Scale >= 1.5f ? "report_32" : "report_16");
            for (int i = 0; i < shown; i++)
            {
                FieldReport f = reports[i];
                HqFill(new Rect(x, y, w, HqT(1f)), PoliSimTheme.RuleRow);
                var row = new Rect(x, y, w, HqH(26f));
                float rx = row.x;
                if (glyph != null)
                {
                    if (Event.current.type == EventType.Repaint)
                    {
                        Color was = GUI.color;
                        GUI.color = PoliSimTheme.TextPrimary;
                        GUI.DrawTexture(new Rect(rx, row.y + Mathf.Round((row.height - glyphSide) * 0.5f), glyphSide, glyphSide), glyph, ScaleMode.ScaleToFit, true);
                        GUI.color = was;
                    }
                    rx += glyphSide + V35.Px(8f);
                }
                else
                {
                    // the glyph's honest fallback: its word (UI v3.3 §4.2)
                    GUIStyle word = HqMono(14f, PoliSimTheme.TextMuted);
                    HqLabel(new Rect(rx, row.y, HqWidth("REPORT", word), row.height), "REPORT", word);
                    rx += HqWidth("REPORT", word) + V35.Px(8f);
                }
                string region = s.Hq?.Ground != null && f.Region >= 0 && f.Region < s.Hq.Ground.Names.Length ? CampaignTiles.Display(s.Hq.Ground.Names[f.Region]) : "—";
                float regionW = HqW(118f);
                string[] regionForms = s.Hq?.Ground != null && f.Region >= 0 && f.Region < s.Hq.Ground.Names.Length ? HqRegionForms(s, s.Hq.Ground.Names[f.Region]) : new[] { region, region, region };
                HqLabel(new Rect(rx, row.y, regionW, row.height), HqFirstFitting(bold, regionW, regionForms[1], regionForms[2]), bold);
                rx += regionW;
                float figuresRoom = Mathf.Max(1f, row.xMax - rx);
                string figures = HqFirstFitting(mono, figuresRoom,
                    string.Format(CultureInfo.InvariantCulture, "{0:N0} vol · {1:N0} doors · {2:N0} calls", f.Volunteers, f.Doors, f.Calls),
                    string.Format(CultureInfo.InvariantCulture, "{0:N0} doors · {1:N0} calls", f.Doors, f.Calls),
                    string.Format(CultureInfo.InvariantCulture, "{0:N0} · {1:N0}", f.Doors, f.Calls));   // K320: said shorter, never cut - the whole report is the row's slip
                HqLabel(new Rect(rx, row.y, figuresRoom, row.height), figures, mono);
                string id = "hq:report:" + i.ToString(CultureInfo.InvariantCulture);
                SlipAnchor(row, id);
                book.Anchors[id] = new SlipContent("FIELD REPORT · " + region).Add(HqN(f.Volunteers) + " volunteers out").Add(HqN(f.Doors) + " doors opened · " + HqN(f.Calls) + " calls so far").Add("information · nothing to decide");
                y += row.height;
            }
            if (shown < reports.Count)
            {
                HqFill(new Rect(x, y, w, HqT(1f)), PoliSimTheme.RuleRow);
                var more = new Rect(x, y, w, HqH(26f));
                HqLabel(more, "+" + (reports.Count - shown).ToString(CultureInfo.InvariantCulture) + " more reports", mono);
                SlipAnchor(more, "hq:rmore");
                var slip = new SlipContent("FIELD REPORTS · " + reports.Count.ToString(CultureInfo.InvariantCulture));
                for (int i = shown; i < reports.Count && slip.Lines.Count < 4; i++)
                {
                    FieldReport f = reports[i];
                    string region = s.Hq?.Ground != null && f.Region >= 0 && f.Region < s.Hq.Ground.Names.Length ? CampaignTiles.Display(s.Hq.Ground.Names[f.Region]) : "—";
                    slip.Add(string.Format(CultureInfo.InvariantCulture, "{0} · {1:N0} vol · {2:N0} doors · {3:N0} calls", region, f.Volunteers, f.Doors, f.Calls));
                }
                book.Anchors["hq:rmore"] = slip;
                y += more.height;
            }
            return y;
        }

        /// <summary>CL-2 (DS-10): a story broke for the player's party - its kind and weight, the evidence as the party's people read it, the day, the seven
        /// answers as cells, and the state (the clock holds until one is queued). §17's sentences are the head's slip, in fragments (K320).</summary>
        private float DrawHqStory(float x, float y, float w, CampaignSnapshot s, PeopleSlips.Book book)
        {
            PendingScandalView story = s.Scandal.Value;
            bool live = HqLive(out bool _);
            float padX = HqW(10f), cellH = HqH(26f), gap = HqW(4f);
            GUIStyle cellFace = HqMono(14f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleCenter);
            // the cells' rows, measured before the plate is sized
            float rowW = w - 2f * padX, cx = 0f;
            int cellRows = 1;
            foreach (ScandalResponse response in Scandals.TheSeven)
            {
                float cw = HqWidth(Scandals.Caption(response), cellFace) + HqW(16f);
                if (cx > 0f && cx + cw > rowW) { cellRows++; cx = 0f; }
                cx += cw + gap;
            }
            float h = HqH(6f) + HqH(26f) + HqH(22f) + cellRows * (cellH + gap) + HqH(24f) + HqH(8f);
            var plate = new Rect(x, y, w, h);
            HqFill(plate, PoliSimTheme.CardInset);
            HqEdge(plate, PoliSimTheme.RuleRow, HqT(1f));
            float ix = plate.x + padX, iy = plate.y + HqH(6f);
            string headText = Scandals.KindCaption(story.Kind) + " · " + story.Severity.ToString().ToUpperInvariant();   // K320: the kind and the weight (the slip's head says a story broke)
            var head = new Rect(ix, iy, rowW, HqH(26f));
            HqLabel(head, headText, HqSerif(17f, PoliSimTheme.TextPrimary));
            SlipAnchor(head, "hq:story");
            book.Anchors["hq:story"] = new SlipContent("A STORY BROKE")
                .Add("apology · less damage later, a dip now")
                .Add("denial · works on weak evidence, fatal if it surfaces")
                .Add("staff sacrifice · cynical where no staff could carry it")
                .Add("your answer lands the morning the day steps");
            iy += head.height;
            string seen = string.Format(CultureInfo.InvariantCulture, "seen {0:F2} · day {1} · {2}", story.SeenEvidence, story.BrokeOnDay, HqDay(story.BrokeOn));
            var seenRect = new Rect(ix, iy, rowW, HqH(22f));
            HqLabel(seenRect, seen, HqMono(14f, PoliSimTheme.TextSecondary));
            SlipAnchor(seenRect, "hq:evidence");
            var evidence = new SlipContent("EVIDENCE · " + HqF(story.SeenEvidence, 2)).Add("as your people read it · not the truth");
            if (story.SeenEvidence < CampaignRun.DenyBelowSeenEvidence) { evidence.Add(string.Format(CultureInfo.InvariantCulture, "under {0:F2} a party's instinct denies", CampaignRun.DenyBelowSeenEvidence)); }
            evidence.Add("broke on " + HqDate(story.BrokeOn));
            book.Anchors["hq:evidence"] = evidence;
            iy += seenRect.height;
            cx = ix;
            foreach (ScandalResponse response in Scandals.TheSeven)
            {
                string caption = Scandals.Caption(response);
                float cw = HqWidth(caption, cellFace) + HqW(16f);
                if (cx > ix && cx + cw > ix + rowW) { cx = ix; iy += cellH + gap; }
                bool open = live && response != ScandalResponse.Resign && (response != ScandalResponse.SacrificeStaffMember || story.StaffOnRoster);
                bool queued = story.Queued.HasValue && story.Queued.Value == response;
                string id = "hq:answer:" + response;
                if (HqVerb(new Rect(cx, iy, cw, cellH), caption, queued ? HqVerbFace.Selected : open ? HqVerbFace.Open : HqVerbFace.Closed, id) && open)
                {
                    _simulationManager.QueueScandalResponse(response, out string refusal);
                    if (refusal != null) { Debug.Log("CAMPAIGN QUEUE: " + refusal); }
                }
                book.Anchors[id] = response == ScandalResponse.Resign ? new SlipContent(caption).Add("no second candidate is cast").Add("nobody to hand the campaign to")
                    : response == ScandalResponse.SacrificeStaffMember && !story.StaffOnRoster ? new SlipContent(caption).Add("nobody on the roster to sacrifice")
                    : new SlipContent(caption).Add("lands the morning the day steps");
                cx += cw + gap;
            }
            iy += cellH + gap;
            string state = story.Queued.HasValue ? "QUEUED · " + Scandals.Caption(story.Queued.Value) : "CLOCK HELD";
            var stateRect = new Rect(ix, iy, rowW, HqH(24f));
            HqLabel(stateRect, state, HqMono(14f, story.Queued.HasValue ? PoliSimTheme.TextPrimary : PoliSimTheme.Caution, bold: true));
            SlipAnchor(stateRect, "hq:storystate");
            book.Anchors["hq:storystate"] = story.Queued.HasValue ? new SlipContent("QUEUED").Add("it lands the morning the day steps") : new SlipContent("CLOCK HELD").Add("the clock holds until you answer");
            return plate.yMax;
        }
    }
}
