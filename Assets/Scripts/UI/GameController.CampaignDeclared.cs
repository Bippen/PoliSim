using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §657 (D-PS, ruled §621 and §648): **the run-up's declarations** - who will govern with whom, as the parties have said it, on today's date.
    /// Opened from Campaign HQ's masthead (DECLARED) in the run-up and the campaign, closed from its own (BACK TO HQ).
    ///
    /// **The DECLARED block only, never the MEASURED one** (§648): every row is a dated fact of `DeclaredRedLines`' timeline standing today
    /// (`DeclaredRedLines.StandingOn`), in the formation page's own grammar - the pair lines with the page's verbs and tags, then the candidacies
    /// under their caption, then the in-or-against rules and SD's refusal of the support role. The model's distances between parties are not on
    /// this page. A declaration is shown from its own date (the party's record, §621) and until the dated one that replaced it; the lifted ones
    /// since the sitting chamber's election stand in their own block, because a lifted line is a declaration too.
    ///
    /// **Honest fallbacks** (UI v3.3): the symbol registry is not built - Design's glyphs are owed as one zip - so every row is its words, and the
    /// row's source keys (the bracketed citation keys of its basis) print beside its date until the slip carries the basis on demand.
    /// </summary>
    public partial class GameController
    {
        private bool _liveDeclaredOpen;
        private Rect _campaignDeclaredInnerRect;

        /// <summary>The HQ's DECLARED chip: only where the country's declarations are dated (Sweden's timeline, §621).</summary>
        private bool DeclaredPageAvailable() =>
            _liveCampaignOpen && _playerCountry != null && DeclaredRedLines.HasTimeline(_playerCountry.Id);

        private void OpenLiveDeclared() { if (DeclaredPageAvailable()) { _liveDeclaredOpen = true; } }

        private void CloseLiveDeclared() { _liveDeclaredOpen = false; }

        private static readonly Regex SourceKey = new Regex(@"\[[A-Z]+[A-Z0-9]*-[A-Z0-9]+\]", RegexOptions.CultureInvariant);

        /// <summary>A fact's citation keys, in order, once each - the row's source until the slip carries the basis (UI v3.3's honest fallback).</summary>
        internal static string SourceKeysOf(string basis)
        {
            var keys = new List<string>();
            if (!string.IsNullOrEmpty(basis)) { foreach (Match m in SourceKey.Matches(basis)) { if (!keys.Contains(m.Value)) { keys.Add(m.Value); } } }
            return keys.Count == 0 ? "THE 2022 RECORD" : string.Join(" ", keys.ToArray());
        }

        /// <summary>A dated fact as the formation page says it: the sentence and its tag - the same verbs and tags as the DECLARED block (§648).</summary>
        internal static (string Sentence, string Tag) SayDeclared(CountryId country, DeclaredRedLines.DatedFact f)
        {
            string a = PartySystems.ShortName(country, f.Party);
            switch (f.Kind)
            {
                case DeclaredRedLines.FactKind.PairLine:
                    string b = PartySystems.ShortName(country, f.Other);
                    return (a + " will not " + (f.OneWay ? "sit in or back a cabinet with " : f.BlocksSupport ? "depend on " : "sit with ") + b,
                        f.OneWay ? "one way" : f.BlocksSupport ? "nor support" : "would support");
                case DeclaredRedLines.FactKind.Candidacy:
                    return (a + " names " + f.Candidate + " for prime minister", "candidacy");
                case DeclaredRedLines.FactKind.InOrAgainst:
                    return (a + " will be in the government or vote against it", "in or against");
                default:
                    return (a + " will not take the support role", "no support role");
            }
        }

        private static string Day(DateTime d) => d.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant();

        private void DrawCampaignDeclaredStage(float availableHeight, float availableWidth, CampaignSnapshot snapshot)
        {
            float innerWidth = PoliSimWidgets.InnerWidth(availableWidth, _boxStyle);

            GUILayout.BeginVertical(_frameSheetStyle, GUILayout.Width(availableWidth), GUILayout.ExpandHeight(true));
            Rect inner = GUILayoutUtility.GetRect(innerWidth, availableHeight, GUILayout.Width(innerWidth), GUILayout.Height(availableHeight));
            GUILayout.EndVertical();

            if (Event.current.type == EventType.Repaint) { _campaignDeclaredInnerRect = inner; }
            else if (_campaignDeclaredInnerRect.width > 1f) { inner = _campaignDeclaredInnerRect; }

            float ux = inner.width / DeskBoardInnerWidth;
            float uy = inner.height / DeskBoardInnerHeight;
            Rect Board(float x, float y, float w, float h) => new Rect(inner.x + x * ux, inner.y + y * uy, w * ux, h * uy);

            DrawCampaignMasthead(Board(0f, 0f, 1156f, 28f), snapshot, "WHO WILL GOVERN WITH WHOM", "BACK TO HQ", CloseLiveDeclared);

            DateTime today = _simulationManager != null ? _simulationManager.CurrentDate : DateTime.MinValue;
            CountryId country = _playerCountry.Id;
            DrawDeclaredStanding(Board(0f, 36f, 700f, 576f), country, today, ux, uy);
            DrawDeclaredLifted(Board(716f, 36f, 440f, 576f), country, today, ux, uy);

            if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(Board(0f, 620f, 1156f, 1f), PoliSimTheme.HairlineStrong); }
        }

        private void DrawDeclaredStanding(Rect r, CountryId country, DateTime today, float ux, float uy)
        {
            float y = DrawCampaignLedgerHead(r, "DECLARED — A PARTY SAID THIS, AND IT IS ON THE RECORD · AS OF " + Day(today), ux, uy);
            GUIStyle name = DeskBody(11f, PoliSimTheme.TextPrimary);
            GUIStyle tag = DeskCaption(10f, PoliSimTheme.TextSecondary, false, TextAnchor.MiddleRight);
            GUIStyle since = DeskCaption(8.5f, PoliSimTheme.TextMuted);
            GUIStyle caption = DeskCaption(8.5f, PoliSimTheme.TextSecondary);
            float rowHeight = Mathf.Max(Mathf.Round(17f * uy), name.CalcSize(new GUIContent("Ag")).y);
            float sinceHeight = Mathf.Ceil(DeskCaptionHeight(since));

            List<DeclaredRedLines.DatedFact> standing = DeclaredRedLines.StandingOn(country, today);
            if (standing.Count == 0)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight), "NONE ON RECORD", name);
                return;
            }

            // The formation page's order: the lines, then the candidacies under their own caption, then each party's rule about its own role
            // (an in-or-against rule, SD's refusal of the support role).
            string[] heads = { null, "CANDIDACIES — THE PARTIES SAID THIS; THE REFUSAL BETWEEN RIVALS IS THE MODEL'S RULE", "EACH PARTY'S RULE ABOUT ITS OWN ROLE" };
            for (int group = 0; group < 3; group++)
            {
                bool captioned = heads[group] == null;
                foreach (DeclaredRedLines.DatedFact f in standing)
                {
                    int of = f.Kind == DeclaredRedLines.FactKind.PairLine ? 0 : f.Kind == DeclaredRedLines.FactKind.Candidacy ? 1 : 2;
                    if (of != group) { continue; }
                    if (!captioned)
                    {
                        y += Mathf.Round(4f * uy);
                        PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, rowHeight), heads[group], caption);
                        y += rowHeight;
                        captioned = true;
                    }
                    if (y + rowHeight + sinceHeight > r.yMax) { return; }
                    (string sentence, string t) = SayDeclared(country, f);
                    DrawCampaignRow(new Rect(r.x, y, r.width, rowHeight), sentence, t, name, tag);
                    y += rowHeight;
                    PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, sinceHeight), "SINCE " + Day(f.From) + " · " + SourceKeysOf(f.Basis), since);
                    y += sinceHeight + Mathf.Round(3f * uy);
                }
            }
        }
        private void DrawDeclaredLifted(Rect r, CountryId country, DateTime today, float ux, float uy)
        {
            float y = DrawCampaignLedgerHead(r, "LIFTED SINCE THE LAST ELECTION — ALSO DECLARED", ux, uy);
            GUIStyle name = DeskBody(11f, PoliSimTheme.TextSecondary);
            GUIStyle since = DeskCaption(8.5f, PoliSimTheme.TextMuted);
            float rowHeight = Mathf.Max(Mathf.Round(17f * uy), name.CalcSize(new GUIContent("Ag")).y);
            float sinceHeight = Mathf.Ceil(DeskCaptionHeight(since));

            List<DeclaredRedLines.DatedFact> lifted = DeclaredRedLines.LiftedSince(country, WorldClock.ElectionDayOf(country, WorldClock.SeatedVintage(country, today)), today);
            if (lifted.Count == 0)
            {
                DrawCampaignEmptyRow(new Rect(r.x, y, r.width, rowHeight), "NONE LIFTED", name);
                y += rowHeight;
            }
            foreach (DeclaredRedLines.DatedFact f in lifted)
            {
                if (y + rowHeight + sinceHeight > r.yMax) { break; }
                (string sentence, string _) = SayDeclared(country, f);
                PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, rowHeight), sentence, name);
                y += rowHeight;
                PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, sinceHeight), "LIFTED " + Day(f.Until) + " · " + SourceKeysOf(f.Basis), since);
                y += sinceHeight + Mathf.Round(3f * uy);
            }

            DrawResultsNote(new Rect(r.x, y + Mathf.Round(8f * uy), r.width, r.yMax - y - Mathf.Round(8f * uy)), r,
                "EACH ITEM IS FROM THE PARTY'S OWN RECORD AND DATED BY IT; A DECLARATION STANDS UNTIL A LATER ONE REPLACES IT. NOTHING ON THIS PAGE IS MEASURED — THE MODEL'S DISTANCES BETWEEN PARTIES ARE NOT SHOWN HERE.",
                "Run-up declarations note");
        }
    }
}
