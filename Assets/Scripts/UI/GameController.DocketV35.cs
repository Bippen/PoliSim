using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §731 (UI v3.5, Design's V35 composition): THE DOCKET AS A LIST - one card per thing waiting on the player, as the composition draws them: the
    /// area's stripe at the left, the icon, the item's name and one muted line under it, the urgency as a stamp (*Holds time* in the caution ink - the
    /// clock is held until it is answered - or *Can wait*), and its action. An item's body - a cabinet decision's options with their costs and
    /// estimates, a meeting's choices, the budget process - opens under its card when its action is taken (one open at a time), drawn by the same
    /// renderers the docket has always gathered (`DrawCabinetDecisionModal`, `DrawForeignPolicyMeetingModal`, `DrawBudgetBillStatusAndIntroduce`),
    /// kept as built inside the card. The central bank's appointment draws its candidates as the composition's cards (the portrait, the name, the
    /// philosophy as a chip, Appoint); each candidate's description is its card's slip.
    /// </summary>
    public partial class GameController
    {
        /// <summary>§731: the one docket item whose body is open, by key, or null. The film opens an item through this field.</summary>
        private string _docketOpenItem;

        /// <summary>§731: the docket's slips (the candidates' descriptions, the alerts' full words).</summary>
        private PeopleSlips.Book _docketSlipBook = new PeopleSlips.Book();

        private void DrawDecisionsTab(float availableHeight)
        {
            // P2-1.1: the sheet is sized to the FRAME, not to its content.
            GUILayout.BeginVertical(_frameSheetStyle, GUILayout.ExpandHeight(true));
            BeginSlipAnchors();
            _docketSlipBook = new PeopleSlips.Book();
            float titleHeight = V35.Px(44f);
            Rect titleRow = GUILayoutUtility.GetRect(10f, titleHeight, GUILayout.ExpandWidth(true), GUILayout.Height(titleHeight));
            DrawV35PageTitle(titleRow, "Docket");
            GUILayout.Space(V35.Px(4f));

            float scrollHeight = availableHeight - titleHeight - V35.Px(4f) - _labelStyle.fontSize * 2f;
            int anchorsFrom = _slipAnchors.Count;
            _decisionsScrollPosition = GUILayout.BeginScrollView(_decisionsScrollPosition, GUILayout.Height(scrollHeight));
            // the scroll view's width includes its scrollbar's lane: the list keeps clear of it, as Statistics' content width does
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUI.enabled = !_isGameOver;
            bool anyPending = false;

            // ---- the central bank's appointment: the item, then its candidates as cards ----
            if (_fedChairCandidates != null && _fedChairCandidates.Count > 0)
            {
                string bank = GetCentralBankName(PlayerCountryId);
                DrawDocketItem(null, "bank", UiPalette.GetAreaColor(UiPalette.SystemArea.Political), "Next " + bank + " " + CentralBankHeadWord(), "Choose who sets the policy rate", holdsTime: true, action: null);
                DrawDocketCandidates(_fedChairCandidates);
                anyPending = true;
            }

            // ---- a foreign-policy meeting ----
            ForeignPolicyMeeting meeting = _simulationManager.GetPendingForeignPolicyMeeting(PlayerCountryId);
            if (meeting != null)
            {
                if (DrawDocketItem("meeting", "globe", UiPalette.GetAreaColor(UiPalette.SystemArea.Global), "Foreign policy · " + meeting.Name, null, holdsTime: true, action: "Decide"))
                {
                    DrawDocketBody(() => DrawForeignPolicyMeetingModal(meeting, drawOwnFrame: false));
                }
                anyPending = true;
            }

            // ---- cabinet decisions: the minister's alert said who asks; the item names the portfolio, the minister and their philosophy ----
            foreach ((CabinetPortfolio portfolio, CabinetDecision decision) in _simulationManager.GetPendingCabinetDecisions(PlayerCountryId))
            {
                string minister = _playerCountry.CabinetMinisters != null && _playerCountry.CabinetMinisters.TryGetValue(portfolio, out CabinetMinister seated)
                    ? seated.Name + " · " + seated.Philosophy
                    : "No minister seated";
                string key = "cabinet:" + portfolio;
                _docketSlipBook.Anchors["docket:" + key] = new SlipContent("CABINET DECISION").Add(decision.Name.ToUpperInvariant());
                if (DrawDocketItem(key, "brief", UiPalette.GetAreaColor(UiPalette.GetPortfolioArea(portfolio)), "Cabinet decision · " + GetPortfolioName(portfolio), minister, holdsTime: true, action: "Decide"))
                {
                    DrawDocketBody(() => DrawCabinetDecisionModal(portfolio, decision, drawOwnFrame: false));
                }
                anyPending = true;
            }

            // ---- the year's cabinet events (a resignation, a leak): alerts that can wait ----
            foreach (CabinetEventRecord cabinetEvent in _playerCountry.CabinetEvents)
            {
                if ((_simulationManager.CurrentDate - cabinetEvent.Date).TotalDays > 365) { continue; }
                string key = "event:" + cabinetEvent.Date.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
                _docketSlipBook.Anchors["docket:" + key] = new SlipContent("MINISTER ALERT · " + DeskDay(cabinetEvent.Date)).Add(cabinetEvent.Text.ToUpperInvariant());
                DrawDocketItem(key, "bell", PoliSimTheme.Bad, "Minister alert · " + cabinetEvent.Text, null, holdsTime: false, action: null);
            }

            // ---- the budget process, the fourth thing that holds the clock ----
            if (_simulationManager.GetPendingBudgetProcess(PlayerCountryId))
            {
                if (DrawDocketItem("budget", "coins", UiPalette.GetAreaColor(UiPalette.SystemArea.Fiscal), "Budget process", "The year's budget is due", holdsTime: true, action: "Open"))
                {
                    DrawDocketBody(DrawBudgetBillStatusAndIntroduce);
                }
                anyPending = true;
            }

            GUI.enabled = true;
            if (!anyPending)
            {
                float h = V35.Px(48f);
                Rect empty = GUILayoutUtility.GetRect(10f, h, GUILayout.ExpandWidth(true), GUILayout.Height(h));
                Rect inside = DrawV35Card(empty);
                if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(inside, "Nothing is waiting on you", V35Serif(V35.Name, PoliSimTheme.TextMuted)); }
            }

            GUILayout.EndVertical();
            GUILayout.Space(Mathf.Max(GUI.skin.verticalScrollbar.fixedWidth, V35.Px(12f)) + V35.Px(6f));
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
            Rect view = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
            {
                // the anchors inside the scroll were registered in its content's coordinates; the slips draw over the sheet (Statistics' rule)
                for (int i = _slipAnchors.Count - 1; i >= anchorsFrom; i--)
                {
                    (string id, Rect r) = _slipAnchors[i];
                    var moved = new Rect(r.x + view.x - _decisionsScrollPosition.x, r.y + view.y - _decisionsScrollPosition.y, r.width, r.height);
                    float top = Mathf.Max(moved.y, view.y), bottom = Mathf.Min(moved.yMax, view.yMax);
                    if (bottom <= top) { _slipAnchors.RemoveAt(i); continue; }
                    _slipAnchors[i] = (id, new Rect(moved.x, top, moved.width, bottom - top));
                }
            }
            GUILayout.EndVertical();
            DrawSlips(_docketSlipBook, GUILayoutUtility.GetLastRect());
        }

        /// <summary>The central bank's head's title: the governor where a board has one, the chair otherwise (the Fed).</summary>
        private string CentralBankHeadWord() => PlayerCountryId == CountryId.USA ? "chair" : "governor";

        /// <summary>
        /// One docket item's card: the stripe in <paramref name="stripe"/>, the icon, the name and its muted line, the stamp, the action. Returns
        /// whether the item's body is open (its action toggles it; one item open at a time). An item with no key has no body and no action.
        /// </summary>
        private bool DrawDocketItem(string key, string icon, Color stripe, string title, string line, bool holdsTime, string action)
        {
            V35.FloorGuarded = true;
            float h = V35.Px(string.IsNullOrEmpty(line) ? 52f : 64f);
            Rect r = GUILayoutUtility.GetRect(10f, h, GUILayout.ExpandWidth(true), GUILayout.Height(h));
            Rect inner = DrawV35Card(r);
            bool open = key != null && _docketOpenItem == key;
            bool repaint = Event.current.type == EventType.Repaint;
            if (repaint) { PoliSimTheme.Rule(new Rect(r.x, r.y, V35.Px(4f), r.height), stripe); }
            float iconSide = V35.Px(V35.CardIcon);
            DrawV35Icon(new Rect(inner.x + V35.Px(4f), r.y + Mathf.Round((h - iconSide) * 0.5f), iconSide, iconSide), icon, stripe);

            // the right end: the action, then the stamp to its left
            float x = inner.xMax;
            if (!string.IsNullOrEmpty(action))
            {
                string label = open ? "Close" : action;
                GUIStyle face = V35Serif(V35.Name, PoliSimTheme.TextPrimary, TextAnchor.MiddleCenter);
                float w = Mathf.Ceil(face.CalcSize(new GUIContent(label)).x) + V35.Px(28f);
                var button = new Rect(x - w, r.y + Mathf.Round((h - V35.Px(34f)) * 0.5f), w, V35.Px(34f));
                if (repaint)
                {
                    PoliSimTheme.RoundedCard(button, PoliSimTheme.Tile, PoliSimTheme.HairlineStrong, 0f);
                    PoliSimWidgets.MeasuredLabel(button, label, face);
                }
                if (PoliSimWidgets.Button(button, GUIContent.none, GUIStyle.none)) { _docketOpenItem = open ? null : key; open = !open; }
                x = button.x - V35.Px(14f);
            }
            string stamp = holdsTime ? "Holds time" : "Can wait";
            GUIStyle stampFace = V35Mono(V35.Floor, holdsTime ? PoliSimTheme.Caution : PoliSimTheme.TextSecondary, bold: true, TextAnchor.MiddleCenter);
            float sw = Mathf.Ceil(stampFace.CalcSize(new GUIContent(stamp)).x) + V35.Px(14f);
            var stampRect = new Rect(x - sw, r.y + Mathf.Round((h - V35.Px(26f)) * 0.5f), sw, V35.Px(26f));
            if (repaint)
            {
                PoliSimTheme.RoundedCard(stampRect, V35.CardPaper, holdsTime ? PoliSimTheme.Caution : PoliSimTheme.TextSecondary, 0f);
                PoliSimWidgets.MeasuredLabel(stampRect, stamp, stampFace);
            }

            // the name and its line, between the icon and the stamp
            float tx = inner.x + V35.Px(4f) + iconSide + V35.Px(14f);
            float tw = Mathf.Max(1f, stampRect.x - V35.Px(12f) - tx);
            GUIStyle titleFace = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
            GUIStyle lineFace = V35Serif(V35.Floor, PoliSimTheme.TextMuted);
            float th = Mathf.Ceil(titleFace.CalcSize(new GUIContent("Ag")).y), lh = string.IsNullOrEmpty(line) ? 0f : Mathf.Ceil(lineFace.CalcSize(new GUIContent("Ag")).y);
            float ty = r.y + Mathf.Round((h - th - lh) * 0.5f);
            if (repaint)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(tx, ty, tw, th), V35Fit(title, titleFace, tw, out _), titleFace);
                if (lh > 0f) { PoliSimWidgets.MeasuredLabel(new Rect(tx, ty + th, tw, lh), V35Fit(line, lineFace, tw, out _), lineFace); }
            }
            if (key != null && _docketSlipBook.Anchors.ContainsKey("docket:" + key)) { SlipAnchor(new Rect(tx, ty, tw, th + lh), "docket:" + key); }
            V35.FloorGuarded = false;
            GUILayout.Space(V35.Px(V35.Gutter));
            return open;
        }

        /// <summary>An open item's body - the docket's own renderer for it, kept as built, on a card under the item.</summary>
        private void DrawDocketBody(System.Action body)
        {
            GUILayout.BeginVertical(V35CardStyle());
            body();
            GUILayout.EndVertical();
            GUILayout.Space(V35.Px(V35.Gutter));
        }

        /// <summary>
        /// The central bank's candidates as the composition's cards, two across: the portrait, the name, the philosophy as an outlined chip, and Appoint
        /// (the role's gate first - a player who may not appoint sees the reason, never a dead button). The description is the card's slip.
        /// </summary>
        private void DrawDocketCandidates(List<FedChair> candidates)
        {
            V35.FloorGuarded = true;
            float gutter = V35.Px(V35.Gutter);
            float h = V35.Px(110f);
            bool locked = !_simulationManager.PlayerMayIntroduce(PlayerCountryId, out string lockedBecause);
            for (int start = 0; start < candidates.Count; start += 2)
            {
                Rect row = GUILayoutUtility.GetRect(10f, h, GUILayout.ExpandWidth(true), GUILayout.Height(h));
                float w = (row.width - gutter) * 0.5f;
                for (int i = start; i < Mathf.Min(candidates.Count, start + 2); i++)
                {
                    FedChair c = candidates[i];
                    var card = new Rect(row.x + (i - start) * (w + gutter), row.y, w, h);
                    Rect inner = DrawV35Card(card);
                    float portraitW = V35.Px(64f), portraitH = Mathf.Min(inner.height, V35.Px(84f));
                    var portrait = new Rect(inner.x, inner.y + Mathf.Round((inner.height - portraitH) * 0.5f), portraitW, portraitH);
                    Texture2D face = IconLibrary.GetFedChairPortrait(c.Name);
                    if (Event.current.type == EventType.Repaint)
                    {
                        PoliSimTheme.Rule(portrait, PoliSimTheme.Brass);
                        if (face != null) { GUI.DrawTexture(new Rect(portrait.x + 3f, portrait.y + 3f, portrait.width - 6f, portrait.height - 6f), face, ScaleMode.ScaleAndCrop); }
                    }
                    string action = locked ? "Locked" : "Appoint";
                    GUIStyle actionFace = V35Serif(V35.Name, locked ? PoliSimTheme.TextMuted : PoliSimTheme.TextPrimary, TextAnchor.MiddleCenter);
                    float aw = Mathf.Ceil(actionFace.CalcSize(new GUIContent(action)).x) + V35.Px(28f);
                    var button = new Rect(inner.xMax - aw, inner.y + Mathf.Round((inner.height - V35.Px(34f)) * 0.5f), aw, V35.Px(34f));
                    float nx = portrait.xMax + V35.Px(14f), nw = Mathf.Max(1f, button.x - V35.Px(10f) - nx);
                    GUIStyle nameFace = V35Serif(V35.Name, PoliSimTheme.TextPrimary);
                    GUIStyle chipFace = V35Serif(V35.Floor, PoliSimTheme.TextSecondary, TextAnchor.MiddleCenter);
                    string chip = c.Philosophy.ToString();
                    float cw = Mathf.Ceil(chipFace.CalcSize(new GUIContent(chip)).x) + V35.Px(12f);
                    float ny = inner.y + Mathf.Round(inner.height * 0.5f) - V35.Px(24f);
                    if (Event.current.type == EventType.Repaint)
                    {
                        PoliSimWidgets.MeasuredLabel(new Rect(nx, ny, nw, V35.Px(22f)), V35Fit(c.Name, nameFace, nw, out _), nameFace);
                        var chipRect = new Rect(nx, ny + V35.Px(26f), Mathf.Min(cw, nw), V35.Px(22f));
                        PoliSimTheme.RoundedCard(chipRect, V35.CardPaper, PoliSimTheme.TextSecondary, 0f);
                        PoliSimWidgets.MeasuredLabel(chipRect, chip, chipFace);
                        PoliSimTheme.RoundedCard(button, PoliSimTheme.Tile, PoliSimTheme.HairlineStrong, 0f);
                        PoliSimWidgets.MeasuredLabel(button, action, actionFace);
                    }
                    string anchor = "docket:candidate:" + i;
                    var slip = new SlipContent(c.Name.ToUpperInvariant() + " · " + chip.ToUpperInvariant()).Add(c.Description.ToUpperInvariant());
                    if (locked && !string.IsNullOrEmpty(lockedBecause)) { slip.Add(lockedBecause.ToUpperInvariant()); }
                    _docketSlipBook.Anchors[anchor] = slip;
                    SlipAnchor(new Rect(nx, ny, nw, V35.Px(48f)), anchor);
                    if (!locked && PoliSimWidgets.Button(button, GUIContent.none, GUIStyle.none))
                    {
                        _playerCountry.CurrentFedChair = c;
                        _fedChairCandidates = null;
                        RecomputePolicyPreview();
                    }
                }
                GUILayout.Space(gutter);
            }
            V35.FloorGuarded = false;
        }
    }
}
