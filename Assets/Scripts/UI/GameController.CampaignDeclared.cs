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
    /// **Board 21c** (§685): a party in a sentence is its 4b MARK, not its letter; the date stands alone under the sentence (a stamp, no "SINCE");
    /// the citation ids are provenance and sit behind the desk's † (the one control, <see cref="DeskProvenance"/>) and on the row's slip; the
    /// DECLARED rule's paragraph is the DECLARED head's slip; the type is at the 9.5 caption floor; the lifted rows are at the reduced presence
    /// (TextMuted, never struck through - the strike is 10c's zero); and a record checked and holding nothing lifted draws the em dash (a NIL -
    /// never ABSENT, which would say nothing was consulted). The sentences themselves stay: a declaration is a sentence.
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

            BeginSlipAnchors();
            var book = new PeopleSlips.Book();
            DrawCampaignMasthead(Board(0f, 0f, 1156f, 28f), snapshot, "WHO WILL GOVERN WITH WHOM", "BACK TO HQ", CloseLiveDeclared);

            DateTime today = _simulationManager != null ? _simulationManager.CurrentDate : DateTime.MinValue;
            CountryId country = _playerCountry.Id;
            DrawDeclaredStanding(Board(0f, 36f, 730f, 576f), country, today, book);
            DrawDeclaredLifted(Board(758f, 36f, 398f, 576f), country, today, book);

            if (Event.current.type == EventType.Repaint) { PoliSimTheme.Rule(Board(0f, 620f, 1156f, 1f), PoliSimTheme.HairlineStrong); }
            DrawSlips(book, inner);
        }

        /// <summary>A dated fact as the row draws it (21c): the party's mark, the verb, the other party's mark, the rest - the sentence with its
        /// parties as marks. A segment is a mark key or words.</summary>
        internal static List<(bool Mark, string Text)> DeclaredSegments(DeclaredRedLines.DatedFact f)
        {
            var s = new List<(bool Mark, string Text)> { (true, f.Party) };
            switch (f.Kind)
            {
                case DeclaredRedLines.FactKind.PairLine:
                    s.Add((false, "will not " + (f.OneWay ? "sit in or back a cabinet with" : f.BlocksSupport ? "depend on" : "sit with")));
                    s.Add((true, f.Other));
                    break;
                case DeclaredRedLines.FactKind.Candidacy: s.Add((false, "names " + f.Candidate + " for prime minister")); break;
                case DeclaredRedLines.FactKind.InOrAgainst: s.Add((false, "will be in the government or vote against it")); break;
                default: s.Add((false, "will not take the support role")); break;
            }
            return s;
        }

        /// <summary>One declaration's row: the sentence with marks and its kind at the right, the date alone under it (and, behind †, the source
        /// keys); the slip carries the sentence in words, the date and the keys. Returns the row's height.</summary>
        private float DrawDeclaredRow(Rect r, CountryId country, DeclaredRedLines.DatedFact f, string dateLine, string tag, Color ink, PeopleSlips.Book book, string id)
        {
            GUIStyle words = DeskBody(13f, ink);
            GUIStyle date = DeskCaption(9.5f, PoliSimTheme.TextMuted);
            float pad = StatsUnit(4f);
            float lineH = Mathf.Ceil(words.CalcSize(new GUIContent("Ag")).y);
            float dateH = Mathf.Ceil(DeskCaptionHeight(date));
            float h = pad + lineH + StatsUnit(2f) + dateH + pad;
            var line = new Rect(r.x, r.y + pad, r.width, lineH);
            float tagW = 0f;
            if (!string.IsNullOrEmpty(tag))
            {
                GUIStyle tagStyle = DeskCaption(9.5f, ink, false, TextAnchor.MiddleRight);
                tagW = Mathf.Ceil(tagStyle.CalcSize(new GUIContent(tag)).x) + 2f;
                PoliSimWidgets.MeasuredLabel(new Rect(line.xMax - tagW, line.y, tagW, line.height), tag, tagStyle);
            }
            float x = line.x;
            float gap = StatsUnit(5f);
            foreach ((bool mark, string text) in DeclaredSegments(f))
            {
                if (mark)
                {
                    DrawPartyMarkSlot(new Rect(x, line.y, StatsUnit(16f), line.height), country, text);
                    x += StatsUnit(16f) + gap;
                    continue;
                }
                float w = Mathf.Ceil(words.CalcSize(new GUIContent(text)).x) + 2f;
                PoliSimWidgets.MeasuredLabel(new Rect(x, line.y, Mathf.Min(w, Mathf.Max(1f, line.xMax - tagW - x)), line.height), text, words);
                x += w + gap;
            }
            string keys = SourceKeysOf(f.Basis);
            string dated = DeskProvenance.On ? dateLine + " · " + keys : dateLine;
            PoliSimWidgets.MeasuredLabel(new Rect(r.x, line.yMax + StatsUnit(2f), r.width, dateH), dated, date);
            var row = new Rect(r.x, r.y, r.width, h);
            SlipAnchor(row, id);
            (string sentence, string _) = SayDeclared(country, f);
            book.Anchors[id] = SlipWrapped(sentence.ToUpperInvariant(), dateLine + " · " + keys);
            DrawRowRule(row);
            return h;
        }

        private void DrawDeclaredStanding(Rect r, CountryId country, DateTime today, PeopleSlips.Book book)
        {
            float headH = StatsUnit(20f);
            float y = r.y;
            var head = new Rect(r.x, y, r.width, headH);
            // The desk's one PROVENANCE control (D16 §2), at the head's right; the source keys stand behind it.
            GUIStyle glyph = DeskCaption(12f, DeskProvenance.On ? PoliSimTheme.TextPrimary : PoliSimTheme.TextMuted, false, TextAnchor.MiddleCenter);
            float tabW = glyph.CalcSize(new GUIContent(DeskProvenance.Glyph)).x + StatsUnit(14f);
            var tab = new Rect(head.xMax - tabW, head.y, tabW, head.height - 1f);
            if (Event.current.type == EventType.Repaint)
            {
                PoliSimTheme.Rule(tab, DeskProvenance.On ? PoliSimTheme.Card : PoliSimTheme.StockOff);
                if (DeskProvenance.On) { PoliSimTheme.Rule(new Rect(tab.x, tab.y, tab.width, 2f), PoliSimTheme.Brass); }
                PoliSimWidgets.MeasuredLabel(tab, DeskProvenance.Glyph, glyph);
            }
            if (PoliSimWidgets.Button(tab, GUIContent.none, GUIStyle.none)) { DeskProvenance.On = !DeskProvenance.On; }
            DrawRowSectionHead(new Rect(head.x, head.y, head.width - tabW - StatsUnit(8f), head.height), "DECLARED", "AS OF " + Day(today));
            SlipAnchor(new Rect(head.x, head.y, head.width - tabW, head.height), "declared");
            book.Anchors["declared"] = new SlipContent("DECLARED")
                .Add("A PARTY SAID THIS, AND IT IS ON THE RECORD")
                .Add("EACH ITEM IS FROM THE PARTY'S OWN RECORD AND DATED BY IT")
                .Add("A DECLARATION STANDS UNTIL A LATER ONE REPLACES IT")
                .Add("NOTHING ON THIS PAGE IS MEASURED -")
                .Add("THE MODEL'S DISTANCES BETWEEN PARTIES ARE NOT SHOWN HERE");
            y += headH;

            List<DeclaredRedLines.DatedFact> standing = DeclaredRedLines.StandingOn(country, today);
            if (standing.Count == 0)
            {
                PoliSimWidgets.MeasuredLabel(new Rect(r.x, y, r.width, StatsUnit(24f)), "—", DeskBody(13f, PoliSimTheme.TextPrimary));
                return;
            }

            // The formation page's order: the lines, then the candidacies under their own caption, then each party's rule about its own role
            // (an in-or-against rule, SD's refusal of the support role).
            string[] heads = { null, "CANDIDACIES", "EACH PARTY'S RULE ABOUT ITS OWN ROLE" };
            int n = 0;
            for (int group = 0; group < 3; group++)
            {
                bool captioned = heads[group] == null;
                foreach (DeclaredRedLines.DatedFact f in standing)
                {
                    int of = f.Kind == DeclaredRedLines.FactKind.PairLine ? 0 : f.Kind == DeclaredRedLines.FactKind.Candidacy ? 1 : 2;
                    if (of != group) { continue; }
                    if (!captioned)
                    {
                        y += StatsUnit(8f);
                        var groupHead = new Rect(r.x, y, r.width, headH);
                        DrawRowSectionHead(groupHead, heads[group]);
                        if (group == 1)
                        {
                            SlipAnchor(groupHead, "declared/candidacies");
                            book.Anchors["declared/candidacies"] = new SlipContent("CANDIDACIES").Add("THE PARTIES SAID THIS").Add("THE REFUSAL BETWEEN RIVALS IS THE MODEL'S RULE");
                        }
                        y += headH;
                        captioned = true;
                    }
                    if (y + StatsUnit(40f) > r.yMax) { return; }
                    (string _, string t) = SayDeclared(country, f);
                    y += DrawDeclaredRow(new Rect(r.x, y, r.width, 0f), country, f, Day(f.From), t, PoliSimTheme.TextPrimary, book, "declared/" + (n++).ToString(CultureInfo.InvariantCulture));
                }
            }
        }

        private void DrawDeclaredLifted(Rect r, CountryId country, DateTime today, PeopleSlips.Book book)
        {
            float headH = StatsUnit(20f);
            float y = r.y;
            var head = new Rect(r.x, y, r.width, headH);
            DrawRowSectionHead(head, "LIFTED SINCE THE LAST ELECTION");
            SlipAnchor(head, "lifted");
            book.Anchors["lifted"] = new SlipContent("LIFTED SINCE THE LAST ELECTION").Add("ALSO DECLARED - A LIFTED LINE IS A DECLARATION TOO");
            y += headH;

            List<DeclaredRedLines.DatedFact> lifted = DeclaredRedLines.LiftedSince(country, WorldClock.ElectionDayOf(country, WorldClock.SeatedVintage(country, today)), today);
            if (lifted.Count == 0)
            {
                // A NIL: the record was checked and holds nothing lifted - the em dash (19a), never ABSENT.
                var nil = new Rect(r.x, y, r.width, StatsUnit(26f));
                PoliSimWidgets.MeasuredLabel(nil, "—", DeskBody(13f, PoliSimTheme.TextMuted));
                SlipAnchor(nil, "lifted/none");
                book.Anchors["lifted/none"] = new SlipContent("NONE LIFTED").Add("THE RECORD WAS CHECKED AND HOLDS NOTHING LIFTED");
                return;
            }
            int n = 0;
            foreach (DeclaredRedLines.DatedFact f in lifted)
            {
                if (y + StatsUnit(40f) > r.yMax) { break; }
                y += DrawDeclaredRow(new Rect(r.x, y, r.width, 0f), country, f, "LIFTED " + Day(f.Until), null, PoliSimTheme.TextMuted, book, "lifted/" + (n++).ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
