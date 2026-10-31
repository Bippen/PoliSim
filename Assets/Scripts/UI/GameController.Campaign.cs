using System.Collections.Generic;
using System.Globalization;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// W-E1 — **Campaign HQ**, the first screen of the Track E class. A `partial` companion of
    /// <see cref="GameController"/> drawn in the v3 idiom and NOT a re-skin of anything: the folded
    /// rail plus one full-bleed sheet, the board's 1156×680 placements inside it, type scaled from
    /// the board by `DeskPx` and floored at D4's 9 px, captions in the document face, ledger rows at
    /// the Desk's pitch. It reuses `DeskCaption`/`DeskBody`/`DeskNumeral`/`DrawDeskChipButton`
    /// directly rather than restating them, so the two stages share one type ladder — a player
    /// moving between them must not see two.
    ///
    /// **Two ways in.** The screen draws when `_campaignScreen` has a value. The screenshot driver
    /// stages one (`-shotcampaign`, a film that is never refreshed), and since C-R4b step 4a
    /// (2026-09-02) the rail's CAMPAIGN cell opens the LIVE campaign `SimulationManager` runs for the
    /// player's party - re-read from the running state once a frame, in LateUpdate before OnGUI (`LiveCampaignSnapshot`; GA-1-1).
    /// That cell is the wiring W-G1 foresaw: "an ordinary screen by adding the rail cell and nothing
    /// else". ⚠ It is a screen the player can WATCH, not yet one they can act from: until the HQ has an
    /// input path (step 4b) the party is AI-played and the queue shown is the day's decisions.
    ///
    /// **Every figure is DERIVED.** The screen lays out a <see cref="CampaignSnapshot"/> and
    /// computes nothing of its own beyond summing what it was handed, so "is that number real?" is
    /// answered by who filled the snapshot rather than by reading the drawing code. The race is read
    /// from a <see cref="Poll"/>, never from a preference vector — W-B10's rule that the UI never
    /// sees the truth is carried into the view layer by the type the view is given, and the poll's ±
    /// is drawn as a band around the bar rather than printed as a footnote, because the uncertainty
    /// IS the reading.
    ///
    /// **No sprite is invented.** The party identity marks are the delivered `mark_party_*` set
    /// (their first call site — `IconLibrary.GetPartyMark`); everything else is procedural in the
    /// existing idiom. A gap becomes a line for the Track H Design ask, never new art here.
    /// </summary>
    public partial class GameController
    {
        /// <summary>
        /// The snapshot the campaign screens draw. Non-null while the screenshot harness has staged
        /// one OR while the rail's CAMPAIGN cell has the live campaign open (`_liveCampaignOpen`,
        /// C-R4b step 4a); every other player path leaves it null and takes the Desk branch. Nothing
        /// serialises it - the live one is rebuilt from the running state, which the save replays.
        /// </summary>
        private CampaignSnapshot? _campaignScreen;

        /// <summary>
        /// The sheet's inner rect as the last Repaint measured it. IMGUI hands back a 1×1 dummy on
        /// the Layout event; the chip controls in the masthead are hit-tested against rects derived
        /// from this one, so they would jump between events without the cache. Same defect, same fix
        /// as `_deskInnerRect` (polisim-imgui-layout-facts, item 6).
        /// </summary>
        private Rect _campaignInnerRect;

        /// <summary>
        /// The screenshot harness's way in: a staged snapshot (a film, never refreshed), null clearing
        /// back to whatever screen the frame was on. Mirrors SetInstrumentLadder. The player's way in
        /// is the rail's CAMPAIGN cell (`DrawRailCampaignCell`, C-R4b step 4a), which opens the live
        /// campaign instead.
        /// </summary>
        internal void SetCampaignScreen(CampaignSnapshot? snapshot)
        {
            _campaignScreen = snapshot;
        }

        // ---- C-R4b step 4a (2026-09-02): the HQ over the LIVE campaign ------------------------------
        // The rail carries a CAMPAIGN cell while SimulationManager runs the player's campaign. Opening it
        // sets _campaignScreen from the live state and marks it live, so LateUpdate refreshes the snapshot once a frame (GA-1-1)
        // (the day advances under it); DESK and the area cells close it. The driver's staged snapshots
        // still arrive through SetCampaignScreen and are never refreshed - _liveCampaignOpen tells the two
        // apart. Until the HQ has an input path (step 4b) the party is AI-played and the "queue" is the day's
        // decisions off its ledger; LiveCampaignSnapshot says so.

        /// <summary>True while the HQ on screen is the live campaign's (refreshed once a frame, in LateUpdate), not a staged film.</summary>
        private bool _liveCampaignOpen;

        /// <summary>The live campaign's snapshot for the player's party, or null when none runs / the player's party is not in it.</summary>
        /// <summary>
        /// P2-0.3 (2026-09-02): **the campaign announces itself.** The election date (yyyy-MM-dd) of the
        /// campaign whose opening the player has acknowledged; null until they have. Derived against
        /// the live campaign every frame (`HasPendingCampaignOpening`), so the hold is a fact about
        /// state - it survives a load, and a harness that drives the sim directly still meets it.
        /// Persisted with the UI draft state.
        /// </summary>
        private string _campaignOpeningAcknowledgedElection;

        /// <summary>CL-2 (2026-09-13): the region picked on the campaign map - where the next local act goes (−1: the one-region rule, the strongest office's) - and the election whose campaign it was picked in. The session's, not the save's: a queued act carries its region in the record, a load clears the pick, and a pick from another campaign reads as none (`CampaignPick`).</summary>
        private int _campaignPickedRegion = -1;
        private System.DateTime _campaignPickedElection;

        /// <summary>K3 (2026-10-23): the party the next queued campaign acts attack (−1: the party's own case) and the election it was chosen in - the session's, as the map's pick is: a queued act carries its target in the record, a load clears the choice, and a choice from another campaign reads as none (`CampaignAttack`).</summary>
        private int _campaignAttackTarget = -1;
        private System.DateTime _campaignAttackElection;

        /// <summary>K3: the attack target for the campaign that is running, or −1 - never the player's own party, a party outside this campaign, or a choice made in another.</summary>
        private int CampaignAttack()
        {
            CampaignRun.State s = _simulationManager?.PlayerCampaign;
            if (s == null || _campaignAttackTarget < 0 || _campaignAttackTarget >= s.Setup.Parties.Length) { return -1; }
            if (s.Setup.Calendar.ElectionDate != _campaignAttackElection) { return -1; }
            return _campaignAttackTarget == LiveCampaignSnapshot.PlayerPartyIndex(s, _playerCountry) ? -1 : _campaignAttackTarget;
        }

        /// <summary>
        /// K1's BUILD rows 9 and 12 (2026-10-23; `docs/specs/A_LOOP_Q4_TABLE.md` R2): the TOPIC the next queued acts carry (an `IssueId`, −1 the
        /// general message) and their OUTLAY (1, 2 or 3 times the action's own price) - PARAMETERS of 5a's actions: the topic moves only where the
        /// effect lands (the chain's relevance for the party's contacts, q_p, which since q5 reads the event-moved salience - "respond to events"),
        /// the outlay only how many it reaches (§35's saturating reach, so a bigger rally or more television is more exposures, which coverage
        /// saturates); neither touches the class CACE (K5). The session's, keyed by the election as the attack target is.
        /// </summary>
        private int _campaignTopic = -1;
        private int _campaignOutlay = 1;
        private System.DateTime _campaignTopicElection;

        /// <summary>K1 row 9: the topic for the campaign that is running, or null (the general message) - only an issue contested in it.</summary>
        private IssueId? CampaignTopic()
        {
            CampaignRun.State s = _simulationManager?.PlayerCampaign;
            if (s == null || _campaignTopic < 0 || s.Setup.Calendar.ElectionDate != _campaignTopicElection) { return null; }
            if (s.Setup.TrueSalience == null || _campaignTopic >= s.Setup.TrueSalience.Length || double.IsNaN(s.Setup.TrueSalience[_campaignTopic])) { return null; }
            return (IssueId)_campaignTopic;
        }

        /// <summary>K1 row 12: the outlay multiple for the campaign that is running - 1 unless chosen in it.</summary>
        private int CampaignOutlay()
        {
            CampaignRun.State s = _simulationManager?.PlayerCampaign;
            return s != null && s.Setup.Calendar.ElectionDate == _campaignTopicElection && _campaignOutlay >= 1 && _campaignOutlay <= 3 ? _campaignOutlay : 1;
        }

        /// <summary>CL-2: the map's pick for the campaign that is running, or −1 - never a pick made in another campaign or an index outside this one's regions.</summary>
        private int CampaignPick()
        {
            // K1 row 5 (S-C2): the run-up's map picks too - the next office's site - keyed by the same election
            CampaignRun.Setup? setup = _simulationManager?.PlayerCampaign?.Setup ?? _simulationManager?.PlayerPreCampaign?.Staged;
            if (!setup.HasValue || _campaignPickedRegion < 0 || _campaignPickedRegion >= setup.Value.Regions.Length) { return -1; }
            return setup.Value.Calendar.ElectionDate == _campaignPickedElection ? _campaignPickedRegion : -1;
        }
        /// <summary>CL-2: the map is open over the live campaign (the HQ's masthead opens it; BACK TO HQ closes it); refreshed from the run once a frame in LateUpdate, with the HQ (GA-1-1).</summary>
        private bool _liveCampaignMapOpen;

        /// <summary>CL-2 (DS-10): a story broke for the player's party and no answer is queued - the clock holds, as it does for the opening, until one of the seven chips is pressed. State-derived and idempotent, like the opening's hold.</summary>
        private bool HasPendingScandalAnswer()
        {
            if (_simulationManager == null || _playerCountry == null || _isGameOver) { return false; }
            return _simulationManager.PendingPlayerScandal().HasValue && !_simulationManager.QueuedScandalResponse().HasValue;
        }

        /// <summary>
        /// True while the player's campaign has opened and its opening has not been acknowledged: the
        /// clock HELDs, the banner names it, and HQ opens (the Fed-chair pause is the template). Only
        /// for a party the player runs; a country whose campaign is not staged never holds.
        /// </summary>
        private bool HasPendingCampaignOpening()
        {
            CampaignRun.State campaign = _simulationManager?.PlayerCampaign;
            if (campaign == null || campaign.Finished || _playerCountry == null) { return false; }
            if (LiveCampaignSnapshot.PlayerPartyIndex(campaign, _playerCountry) < 0) { return false; }
            return _campaignOpeningAcknowledgedElection != CampaignOpeningKey(campaign);
        }

        private static string CampaignOpeningKey(CampaignRun.State campaign) =>
            campaign.Setup.Calendar.ElectionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        /// <summary>The interrupt's exit: the HQ's own chip, and the harness's way of pressing it. The clock runs again the next frame.</summary>
        private void AcknowledgeCampaignOpening()
        {
            CampaignRun.State campaign = _simulationManager?.PlayerCampaign;
            if (campaign == null) { return; }
            _campaignOpeningAcknowledgedElection = CampaignOpeningKey(campaign);
        }

        /// <summary>Opens the live HQ over the running campaign - the rail cell's own branch, also taken by the campaign's opening interrupt and by a load that lands on one.</summary>
        private void OpenLiveCampaign()
        {
            CampaignSnapshot? live = BuildLiveCampaignSnapshot();
            if (!live.HasValue) { return; }
            _onDesk = false;
            _liveCampaignOpen = true;
            _campaignScreen = live;
        }

        private CampaignSnapshot? BuildLiveCampaignSnapshot()
        {
            if (_simulationManager == null || _playerCountry == null) { return null; }
            if (_simulationManager.PlayerCampaign == null && _simulationManager.PlayerPreCampaign == null) { return null; }
            double perceived = EconomicVote.RecordTermIndex(_playerCountry, _simulationManager.CurrentDate);   // §752 (A1): what the election scores - the change over the term
            // CL-1: the run-up fills the same screen from the pre-campaign's state until the campaign opens on its outcome.
            if (_simulationManager.PlayerCampaign == null)
            {
                return LiveCampaignSnapshot.BuildPreCampaign(_simulationManager.PlayerPreCampaign, _playerCountry, perceived, _simulationManager.CampaignRecord, CampaignPick());
            }
            return LiveCampaignSnapshot.Build(_simulationManager.PlayerCampaign, _playerCountry, perceived, _simulationManager.CampaignRecord, CampaignPick());
        }

        /// <summary>§697: whether the player's country has a campaign map to draw - Sweden's valkrets cartogram today; Germany's Länder wait on Design (D-DE).</summary>
        private bool CampaignMapDrawn() => _playerCountry != null && _playerCountry.Id == PoliSim.Data.CountryId.Sweden;

        /// <summary>
        /// CL-2: the map over the live campaign - board 4a's cartogram as the REGION PICKER (a tile sets where the next local
        /// act goes; the party's offices framed; every valkrets honestly unknown, regional detail not being on sale in the
        /// live run). Opened from the HQ's masthead, closed from its own; rebuilt from the run once a frame in LateUpdate, with the HQ (GA-1-1).
        /// </summary>
        private void OpenLiveCampaignMap()
        {
            // a map left open when its campaign is gone (the run dropped after polling day, a load) closes rather than freezing
            bool runUp = _simulationManager?.PlayerCampaign == null && _simulationManager?.PlayerPreCampaign != null && !_simulationManager.PlayerPreCampaign.Finished;
            if ((_simulationManager?.PlayerCampaign == null && !runUp) || _playerCountry == null || !CampaignMapDrawn()) { CloseLiveCampaignMap(); return; }
            double perceived = EconomicVote.RecordTermIndex(_playerCountry, _simulationManager.CurrentDate);   // §752 (A1): what the election scores - the change over the term
            // K1 row 5 (S-C2): over the run-up the map is the office siting picker
            CampaignMapSnapshot? map = runUp
                ? LiveCampaignSnapshot.BuildPreCampaignMap(_simulationManager.PlayerPreCampaign, _playerCountry, perceived, _simulationManager.CampaignRecord, CampaignPick())
                : LiveCampaignSnapshot.BuildMap(_simulationManager.PlayerCampaign, _playerCountry, perceived, _simulationManager.CampaignRecord, CampaignPick());
            if (!map.HasValue) { CloseLiveCampaignMap(); return; }
            _campaignMapScreen = map;
            _liveCampaignMapOpen = true;
        }

        private void CloseLiveCampaignMap()
        {
            if (!_liveCampaignMapOpen) { return; }
            _liveCampaignMapOpen = false;
            _campaignMapScreen = null;
        }

        /// <summary>CL-2: a tile clicked on the live map - the next local act's region; the map redraws with the pick framed.</summary>
        private void PickCampaignRegion(int region)
        {
            CampaignRun.Setup? setup = _simulationManager?.PlayerCampaign?.Setup ?? _simulationManager?.PlayerPreCampaign?.Staged;   // S-C2: the run-up's map picks too
            if (!setup.HasValue) { return; }
            _campaignPickedRegion = region;
            _campaignPickedElection = setup.Value.Calendar.ElectionDate;
            if (_liveCampaignMapOpen) { OpenLiveCampaignMap(); }
        }

        /// <summary>CL-2: where a queued local act goes - the region picked on the map, else the one-region rule (`LiveCampaignSnapshot.StrongestRegion`).</summary>
        private int LocalActRegion()
        {
            int pick = CampaignPick();
            return pick >= 0 ? pick : StrongestCampaignRegion();
        }

        /// <summary>
        /// GA-1-1 (2026-10-14): THE LIVE HQ AND ITS MAP ARE SETTLED ONCE A FRAME, IN LateUpdate - after the day's ticks and before OnGUI, which Unity
        /// calls after it (the audit harness calls it before its own GUI pump) - so a campaign whose run drops (the day after polling day, or an extra
        /// election ordered over a run-up) closes its HQ before the frame is drawn. It used to re-read inside the draw: the dropped run closed the HQ in
        /// the Layout pass, Layout drew no sheet, Repaint fell through to Statistics, and the frame threw "Getting control 0's position in a group with
        /// only 0 controls when doing repaint" - the player landing on Statistics, not the desk. An HQ that closes itself now returns the player to the
        /// desk. The map, opened over the HQ, re-read the same way inside the draw and closes the same way here.
        /// </summary>
        private void LateUpdate()
        {
            RefreshLiveCampaignScreen();
            if (_liveCampaignMapOpen) { OpenLiveCampaignMap(); }
        }

        /// <summary>Re-reads the live state into the HQ snapshot, once a frame from <see cref="LateUpdate"/>; closes the screen if the campaign is gone.</summary>
        private void RefreshLiveCampaignScreen()
        {
            if (!_liveCampaignOpen) { return; }
            CampaignSnapshot? live = BuildLiveCampaignSnapshot();
            if (live.HasValue) { _campaignScreen = live; }
            else
            {
                CloseLiveCampaign();
                _onDesk = true;   // GA-1-1: the HQ closed itself (its run dropped) - back to the desk, not whatever tab lay under it
            }
        }

        private void CloseLiveCampaign()
        {
            if (!_liveCampaignOpen) { return; }
            _liveCampaignOpen = false;
            _campaignScreen = null;
            CloseLiveCampaignMap();
            CloseLiveDeclared();   // §657
        }

        /// <summary>
        /// C-R4b step 4b: where a queued LOCAL act goes when nothing is picked - the region of the party's largest office
        /// (by volunteers), else the largest electorate. The picker is the campaign map since CL-2 (`LocalActRegion`); this
        /// is the rule beneath it, shared with the HQ's own line through `LiveCampaignSnapshot.StrongestRegion`.
        /// </summary>
        private int StrongestCampaignRegion()
        {
            CampaignRun.State s = _simulationManager?.PlayerCampaign;
            if (s == null || _playerCountry == null) { return -1; }
            int p = LiveCampaignSnapshot.PlayerPartyIndex(s, _playerCountry);
            return LiveCampaignSnapshot.StrongestRegion(s, p, out _);
        }

        /// <summary>Whether the rail carries the CAMPAIGN cell: a campaign or its run-up runs for the player's party. CL-1: present through the run-up too - the
        /// pre-campaign is where the preparation verbs live, and a cell that appeared only on the campaign's first day would leave the 26 weeks before it
        /// unreachable, as they were. §694: read by the lit rule as well as the cell, so a page left open after its cell is gone lights the document instead.</summary>
        private bool RailCampaignCellPresent()
        {
            if (_simulationManager == null || _playerCountry == null) { return false; }
            return _simulationManager.PlayerCampaign != null
                ? LiveCampaignSnapshot.PlayerPartyIndex(_simulationManager.PlayerCampaign, _playerCountry) >= 0
                : _simulationManager.PlayerPreCampaign != null && !_simulationManager.PlayerPreCampaign.Finished && _simulationManager.PlayerPartyIndexForCampaign() >= 0;
        }

        /// <summary>The rail's CAMPAIGN cell: drawn only while a campaign runs for the player's party; a click opens the live HQ (or, if it is open, returns to the Desk).</summary>
        private void DrawRailCampaignCell(float cell, List<KeyValuePair<string, Rect>> cells)
        {
            if (!RailCampaignCellPresent()) { return; }
            UiPalette.SystemArea area = UiPalette.SystemArea.Political;
            bool selected = _railLitCell == RailLit.Campaign;   // §694: the one rule (was _liveCampaignOpen alone, beside the tab's own)
            Color areaInk = UiPalette.GetAreaColor(area);
            if (DrawRailCell("shell rail: CAMPAIGN", cell, selected, PoliSimTheme.AccentWash(area, RailActiveWashAlpha), areaInk,
                    "CAMPAIGN", selected ? areaInk : PoliSimTheme.TextSecondary, cells, out Rect slot))
            {
                if (_liveCampaignOpen)
                {
                    CloseLiveCampaign();
                    _onDesk = true;
                }
                else
                {
                    OpenLiveCampaign();
                }
            }
            if (Event.current.type != EventType.Repaint) { return; }
            DrawHeldCellMark(slot, HoldCell.Campaign);   // §648 (PF-11): the opening and a story point here
            Texture2D icon = IconLibrary.V35("ballot");   // §723 (v3.5): the rail's icons are the v3.5 set; the glyph slot is the tab's 24 px icon field
            if (icon == null) { return; }
            GUI.color = selected ? areaInk : PoliSimTheme.TextSecondary;
            GUI.DrawTexture(slot, icon, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        // ------------------------------------------------------------------------------------------
        // The masthead: who is campaigning, in what phase, with how long left.
        // ------------------------------------------------------------------------------------------
        private void DrawCampaignMasthead(Rect r, CampaignSnapshot s, string screenTitle, string sideChip = null, System.Action onSideChip = null,
            string secondChip = null, System.Action onSecondChip = null)
        {
            float ux = r.width / 1156f;
            float uy = r.height / 28f;

            float x = r.x;
            Texture2D mark = IconLibrary.GetPartyMark(s.MarkKey);
            if (mark != null)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    // Authored in its own colours — drawn untinted, per the accessor's contract; S5: a created party's white-cut cell in its ink
                    Color wasTint = GUI.color;
                    GUI.color = PoliSimTheme.MarkDrawTint(PlayerCountryId, s.PartyName, s.MarkKey);
                    GUI.DrawTexture(new Rect(x, r.y + Mathf.Round(2f * uy), Mathf.Round(24f * uy), Mathf.Round(24f * uy)),
                        mark, ScaleMode.ScaleToFit);
                    GUI.color = wasTint;
                }

                x += Mathf.Round(24f * uy) + Mathf.Round(8f * ux);
            }

            GUIStyle title = DeskCaption(11f, PoliSimTheme.TextPrimary, bold: true);
            string titleText = $"{s.PartyName} · {s.CountryName}".ToUpperInvariant();
            float titleWidth = Mathf.Ceil(title.CalcSize(new GUIContent(titleText)).x) + Mathf.Round(4f * ux);
            PoliSimWidgets.MeasuredLabel(new Rect(x, r.y, titleWidth, r.height), titleText, title);

            PoliSimWidgets.MeasuredLabel(
                new Rect(x + titleWidth + Mathf.Round(12f * ux), r.y, Mathf.Round(260f * ux), r.height),
                screenTitle, DeskCaption(9f, PoliSimTheme.TextSecondary));

            // Board 21c (§685): the header row held three kinds of chip in one face - two actions, a phase and a countdown, all at 8 px bold.
            // The actions are paper chips at the boards' 9.5; the phase is 17b's JOINED strip, the current phase filled (the fill is the state -
            // TextPrimary with paper ink, since light-on-brass fails D6) and not a control; the countdown is a plain figure with its caption.
            GUIStyle chipCaption = DeskCaption(9.5f, PoliSimTheme.TextPrimary, bold: true, anchor: TextAnchor.MiddleCenter);
            float chipHeight = Mathf.Min(r.height, StatsUnit(20f));
            float chipY = r.y + Mathf.Round((r.height - chipHeight) * 0.5f);

            float right = r.xMax;
            if (s.DaysUntilElection > 0)
            {
                string days = s.DaysUntilElection.ToString(CultureInfo.InvariantCulture);
                const string daysTail = "D TO POLLING DAY";
                float daysWidth = Mathf.Ceil(DeskCaption(13f, PoliSimTheme.TextPrimary, true).CalcSize(new GUIContent(days)).x) + StatsUnit(4f)
                    + Mathf.Ceil(DeskCaption(9.5f, PoliSimTheme.TextMuted).CalcSize(new GUIContent(daysTail)).x) + 4f;
                DrawFigurePair(right - daysWidth, r, days, daysTail);
                right -= daysWidth;
            }
            else
            {
                string daysText = s.DaysUntilElection == 0 ? "POLLING DAY" : "POLL CLOSED";
                GUIStyle plain = DeskCaption(9.5f, PoliSimTheme.TextPrimary);
                float daysWidth = Mathf.Ceil(plain.CalcSize(new GUIContent(daysText)).x) + 4f;
                PoliSimWidgets.MeasuredLabel(new Rect(right - daysWidth, r.y, daysWidth, r.height), daysText, plain);
                right -= daysWidth;
            }

            var phases = new List<CampaignPhase> { CampaignPhase.PreCampaign, CampaignPhase.Campaign };
            if (s.Phase >= CampaignPhase.ElectionDay) { phases.Add(CampaignPhase.ElectionDay); }
            right -= Mathf.Round(10f * ux);
            for (int i = phases.Count - 1; i >= 0; i--)
            {
                string phaseText = SpacedIdentifier(phases[i].ToString()).ToUpperInvariant();
                float phaseWidth = RowChipWidth(phaseText);
                if (i < phases.Count - 1) { right += 1f; }   // joined: one hairline between two cells, not two borders
                var cell = new Rect(right - phaseWidth, chipY, phaseWidth, chipHeight);
                DrawRowChip(cell, phaseText, s.Phase == phases[i] ? ChipFace.Filled : ChipFace.Paper, control: false);
                right = cell.x;
            }

            // P2-0.3: the opening interrupt's exit. The clock is held until this is pressed; the banner above
            // the sheet says so. Drawn only while the hold is on, so a re-opened HQ carries no stale chip.
            float nextX = right - Mathf.Round(4f * ux);
            if (HasPendingCampaignOpening())
            {
                const string trailText = "TAKE THE TRAIL";
                float trailWidth = Mathf.Ceil(chipCaption.CalcSize(new GUIContent(trailText)).x) + Mathf.Round(16f * ux);
                var trailRect = new Rect(nextX - trailWidth - Mathf.Round(6f * ux), chipY, trailWidth, chipHeight);
                if (DrawDeskChipButton(trailRect, trailText, chipCaption, selected: false, disabled: false))
                {
                    AcknowledgeCampaignOpening();
                }
                nextX = trailRect.x;
            }

            // CL-2: the screen's own side chip - the HQ's THE MAP, the map's BACK TO HQ - drawn only over the live campaign.
            if (!string.IsNullOrEmpty(sideChip) && onSideChip != null)
            {
                float sideWidth = Mathf.Ceil(chipCaption.CalcSize(new GUIContent(sideChip)).x) + Mathf.Round(16f * ux);
                var sideRect = new Rect(nextX - sideWidth - Mathf.Round(6f * ux), chipY, sideWidth, chipHeight);
                if (DrawDeskChipButton(sideRect, sideChip, chipCaption, selected: false, disabled: false)) { onSideChip(); }
                nextX = sideRect.x;
            }

            // §657: the HQ's second chip - DECLARED, the run-up's declarations (D-PS).
            if (!string.IsNullOrEmpty(secondChip) && onSecondChip != null)
            {
                float secondWidth = Mathf.Ceil(chipCaption.CalcSize(new GUIContent(secondChip)).x) + Mathf.Round(16f * ux);
                var secondRect = new Rect(nextX - secondWidth - Mathf.Round(6f * ux), chipY, secondWidth, chipHeight);
                if (DrawDeskChipButton(secondRect, secondChip, chipCaption, selected: false, disabled: false)) { onSecondChip(); }
            }
        }

        // ------------------------------------------------------------------------------------------
        // The strip: where the campaign stands in its own window.
        // ------------------------------------------------------------------------------------------
        private void DrawCampaignStrip(Rect r, CampaignSnapshot s)
        {
            float ux = r.width / 1156f;
            float uy = r.height / 53f;

            GUIStyle caption = DeskCaption(9f, PoliSimTheme.TextSecondary);
            float captionHeight = Mathf.Ceil(DeskCaptionHeight(caption));
            float y = r.y + Mathf.Round(6f * uy);

            // CL-1: through the run-up the strip counts the pre-campaign's days and names the day the campaign opens.
            bool runUp = s.Phase == CampaignPhase.PreCampaign;
            int total = Mathf.Max(1, runUp ? 7 * s.Calendar.PreCampaignWeeks : s.Calendar.TotalCampaignDays);
            int dayIndex = runUp ? Mathf.Max(0, (int)(s.Today - s.Calendar.PreCampaignStart).TotalDays) : s.CampaignDay;
            float progress = Mathf.Clamp01(dayIndex / (float)total);
            var track = new Rect(r.x, y + Mathf.Round(3f * uy), r.width * 0.42f, Mathf.Round(8f * uy));
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.RoundedCard(track, PoliSimTheme.Tile, PoliSimTheme.Hairline, 0f);
                if (progress > 0f)
                {
                    PoliSimTheme.RoundedBox(new Rect(track.x, track.y, Mathf.Max(1f, track.width * progress), track.height),
                        UiPalette.GetAreaColor(UiPalette.SystemArea.Political), 0f);
                }
            }

            float textLeft = track.xMax + Mathf.Round(12f * ux);
            PoliSimWidgets.MeasuredLabel(new Rect(textLeft, y, Mathf.Max(1f, r.xMax - textLeft), captionHeight),
                runUp
                    ? string.Format(CultureInfo.InvariantCulture,
                        "RUN-UP DAY {0} OF {1} · OPENED {2} · THE CAMPAIGN OPENS {3} · POLLING DAY {4}",
                        dayIndex, total,
                        s.Calendar.PreCampaignStart.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant(),
                        s.Calendar.CampaignStart.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant(),
                        s.Calendar.ElectionDate.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant())
                    : string.Format(CultureInfo.InvariantCulture,
                        "CAMPAIGN DAY {0} OF {1} · OPENED {2} · POLLING DAY {3}",
                        s.CampaignDay, total,
                        s.Calendar.CampaignStart.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant(),
                        s.Calendar.ElectionDate.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()),
                caption);

            PoliSimWidgets.MeasuredLabel(
                new Rect(r.x, y + captionHeight + Mathf.Round(6f * uy), r.width, captionHeight),
                string.Format(CultureInfo.InvariantCulture, "TODAY {0}",
                    s.Today.ToString("d MMMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant()),
                DeskCaption(8.5f, PoliSimTheme.TextMuted));

            // CL-2: the debate announced from the calendar - the day, its date, and who stands (W-B7's rule: the two leading
            // the published poll that day). Until this the debate happened unannounced on days 20 and 41.
            if (!runUp && s.DebateDays != null && s.DebateDays.Length > 0)
            {
                string debateText;
                if (s.NextDebateDay >= 0)
                {
                    int inDays = s.NextDebateDay - s.CampaignDay;
                    System.DateTime on = s.Calendar.CampaignStart.AddDays(s.NextDebateDay);
                    string when = inDays <= 0 ? "DEBATE TODAY" : inDays == 1 ? "DEBATE TOMORROW" : "DEBATE IN " + inDays.ToString(CultureInfo.InvariantCulture) + " DAYS";
                    debateText = when + " · " + on.ToString("d MMM", CultureInfo.InvariantCulture).ToUpperInvariant() + " · THE TWO LEADING THE PUBLISHED POLL STAND";
                }
                else
                {
                    var past = new string[s.DebateDays.Length];
                    for (int i = 0; i < past.Length; i++) { past[i] = s.DebateDays[i].ToString(CultureInfo.InvariantCulture); }
                    debateText = "THE DEBATES ARE PAST · DAYS " + string.Join(" AND ", past);
                }
                GUIStyle debateStyle = DeskCaption(8.5f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
                float debateWidth = Mathf.Ceil(debateStyle.CalcSize(new GUIContent(debateText)).x) + Mathf.Round(4f * ux);
                PoliSimWidgets.MeasuredLabel(new Rect(r.xMax - debateWidth, y + captionHeight + Mathf.Round(6f * uy), debateWidth, captionHeight), debateText, debateStyle);
            }
        }

        // ------------------------------------------------------------------------------------------
        // The ledger primitives, matching DrawDeskApprovalLedger's head and row exactly.
        // ------------------------------------------------------------------------------------------
        private float DrawCampaignLedgerHead(Rect r, string text, float ux, float uy)
        {
            GUIStyle header = DeskCaption(8.5f, PoliSimTheme.TextSecondary);
            float headerHeight = DeskCaptionHeight(header);
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, r.y, r.width, headerHeight), text, header);

            float y = r.y + headerHeight + Mathf.Round(3f * uy);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(new Rect(r.x, y, r.width, 1f), PoliSimTheme.Hairline);
            }

            return y + Mathf.Round(4f * uy);
        }

        /// <summary>One ledger row: a name at the left, its figure at the right, on one rect.</summary>
        private static void DrawCampaignRow(Rect rect, string name, string figure, GUIStyle nameStyle, GUIStyle figureStyle)
        {
            float figureWidth = Mathf.Ceil(figureStyle.CalcSize(new GUIContent(figure)).x) + 6f;
            PoliSimWidgets.MeasuredLabel(
                new Rect(rect.x, rect.y, Mathf.Max(1f, rect.width - figureWidth - 8f), rect.height), name, nameStyle);
            PoliSimWidgets.MeasuredLabel(new Rect(rect.xMax - figureWidth, rect.y, figureWidth, rect.height),
                figure, figureStyle);
        }

        /// <summary>An empty state: the sentence that says what is absent, never a zero pretending to be data (1m-r2).</summary>
        private static void DrawCampaignEmptyRow(Rect rect, string text, GUIStyle nameStyle)
        {
            var style = new GUIStyle(nameStyle);
            style.normal.textColor = PoliSimTheme.TextMuted;
            style.hover.textColor = PoliSimTheme.TextMuted;
            style.active.textColor = PoliSimTheme.TextMuted;
            style.focused.textColor = PoliSimTheme.TextMuted;
            PoliSimWidgets.MeasuredLabel(rect, text, style);
        }

        /// <summary>
        /// A purse amount with thousands separators, invariant - kronor in Sweden, the player's own currency elsewhere (`Purse`, GA-2-8).
        ///
        /// **Not `UiFormat.Number`**, which is a plain `F0` and filmed the war chest as `1120000` —
        /// a seven-digit money hero with no separators is illegible at a glance, which is the one
        /// job a hero numeral has. **Not `UiFormat.Money`** either: that is dollar-prefixed and
        /// tiered (`$1.12M`), and this sheet is denominated in the purse's national units at full precision because a
        /// campaign budget is spent in units a treasurer would recognise, not in rounded millions.
        /// Invariant, like every other numeric site here (B3's defect class). K110: the amount is in the purse's units, printed in its national currency
        /// (`LiveCampaignSetup.PursePerUnit` - the USA's re-denominated dollars, one to one elsewhere).
        /// </summary>
        private string Kronor(double amount)
        {
            return (amount * PoliSim.Elections.LiveCampaignSetup.PursePerUnit(PlayerCountryId, _simulationManager?.PlayerCampaign?.Setup ?? _simulationManager?.PlayerPreCampaign?.Staged)).ToString("N0", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// GA-2-8 (2026-10-14): the purse's word. Campaign money is a separate purse in national units (ruling S-14), so the HQ names the player's own
        /// currency - "kr" in Sweden, as the HQ was drawn, and the currency's code elsewhere (Germany's EUR, as a carbon rate's EUR/t reads), where
        /// Sweden's kronor stood on Germany's HQ. The figures are the model's one price table either way (`LiveCampaignSetup`: a German one billed).
        /// </summary>
        private string Purse => PlayerCountryId == PoliSim.Data.CountryId.Sweden ? "kr" : PoliSim.Data.EnergyLayer.CurrencyCode(PlayerCountryId);

        /// <summary>"TelevisionAd" -> "Television ad", so an enum reads as a verb rather than an identifier.</summary>
        private static string SpacedIdentifier(string pascal)
        {
            if (string.IsNullOrEmpty(pascal)) { return pascal; }

            var sb = new System.Text.StringBuilder(pascal.Length + 4);
            for (int i = 0; i < pascal.Length; i++)
            {
                if (i > 0 && char.IsUpper(pascal[i]))
                {
                    sb.Append(' ');
                    sb.Append(char.ToLowerInvariant(pascal[i]));
                }
                else
                {
                    sb.Append(pascal[i]);
                }
            }

            return sb.ToString();
        }
    }
}
