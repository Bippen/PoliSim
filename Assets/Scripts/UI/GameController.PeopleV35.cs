using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §732 (UI v3.5, Design's V35 composition): PEOPLE AS THE COMPOSITION LAYS IT - the population card (the bands as bars, working age in the data
    /// slate between the two dashed thresholds, the turnout lane beside them), then the sections as tiles on the twelve columns: DEPENDENCY (the four
    /// ratios and the age split's bar), ELECTORATE, HEALTH (four and the five supporting readouts), EDUCATION (the attainment bar), INFRASTRUCTURE,
    /// ENVIRONMENT (the emissions bar beside its two keys) and the immigration-and-poverty family. Each tile prints its name and figure and, while a
    /// draft moves it, the draft's effect (▲▼ in the outcome's ink where most agree which way is better, rule 5); its census - the unit, the source
    /// and its year, the range, the rank among the six, what reaches it - is its slip (<see cref="PeopleSlips.BuildPage"/>) and, behind the †, its
    /// dotted line under the section.
    ///
    /// <para><b>Read off the model, not the composition.</b> The composition's MIGRATION figures (SCB's flows, the OECD's born-abroad share) were
    /// researched for the board, and the model holds none of them: the section is the model's own family - irregular migration (‡ two definitions),
    /// the poverty gap, underemployment, homelessness. Its emissions headline is all gases (CO₂-eq), named so. The employment-by-sector card the
    /// composition does not draw is KEPT, after it, and asked; the electricity mix and its prices left for the Energy page, where they are drawn.</para>
    /// </summary>
    public partial class GameController
    {
        /// <summary>§732: each section's top in the page's content, by name - the film scrolls to a section by it.</summary>
        private readonly Dictionary<string, float> _peopleSectionTops = new Dictionary<string, float>(System.StringComparer.Ordinal);

        /// <summary>§732: this frame's book, built from the model at the head of the tab - the one `PeopleSlipReachabilityCheck` reads.</summary>
        private PeopleSlips.Book _peopleBook = new PeopleSlips.Book();

        /// <summary>Master Sequence step 5e: the People tab. §732: the v3.5 page - its title with the †, a scrolled page, the slips over the sheet.</summary>
        private void DrawDemographicsTab(float availableHeight, float availableWidth)
        {
            // P2-1.1: the sheet is sized to the FRAME, not to its content.
            GUILayout.BeginVertical(_frameSheetStyle, GUILayout.Width(availableWidth), GUILayout.ExpandHeight(true));
            BeginSlipAnchors();
            float titleHeight = V35.Px(44f);
            Rect titleRow = GUILayoutUtility.GetRect(10f, titleHeight, GUILayout.ExpandWidth(true), GUILayout.Height(titleHeight));
            DrawV35PageTitle(titleRow, "People");
            GUILayout.Space(V35.Px(4f));

            float scrollHeight = availableHeight - titleHeight - V35.Px(4f) - _labelStyle.fontSize * 2f;
            float contentWidth = StatsContentWidth(availableWidth);
            _peopleBook = PeopleSlips.BuildPage(_playerCountry, _world);
            int scrolledFrom = _slipAnchors.Count;
            _demographicsScrollPosition = GUILayout.BeginScrollView(_demographicsScrollPosition, GUILayout.Height(scrollHeight));
            V35.FloorGuarded = true;   // the v3.5 page: a shrink below the floor is an overflow here
            DrawPeopleV35(contentWidth);
            V35.FloorGuarded = false;
            GUILayout.EndScrollView();
            Rect view = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
            {
                // the anchors inside the scroll were registered in its content's coordinates; the slips draw over the sheet (Statistics' rule)
                for (int i = _slipAnchors.Count - 1; i >= scrolledFrom; i--)
                {
                    (string id, Rect r) = _slipAnchors[i];
                    var moved = new Rect(r.x + view.x - _demographicsScrollPosition.x, r.y + view.y - _demographicsScrollPosition.y, r.width, r.height);
                    float top = Mathf.Max(moved.y, view.y), bottom = Mathf.Min(moved.yMax, view.yMax);
                    if (bottom <= top) { _slipAnchors.RemoveAt(i); continue; }
                    _slipAnchors[i] = (id, new Rect(moved.x, top, moved.width, bottom - top));
                }
            }
            GUILayout.EndVertical();
            Rect sheet = GUILayoutUtility.GetLastRect();
            if (!DeskProvenance.On) { DrawSlips(_peopleBook, sheet); }   // the dense view prints the slips as lines; it opens none
        }

        /// <summary>One cell of a row of tiles: a tile, or a share card where it carries parts.</summary>
        private sealed class PeopleCell
        {
            public V35TileData Tile;
            public int Span;
            public string Anchor;
            public List<V35Part> Parts;
            public float Whole;
        }

        private PeopleCell PeopleTile(int span, string icon, string figure, string name, string anchor, Symbol? glyph = null) =>
            new PeopleCell { Span = span, Anchor = anchor, Tile = new V35TileData { Icon = icon, IconInk = UiPalette.GetAreaColor(UiPalette.SystemArea.Labor), Figure = figure, Name = name, Glyph = glyph, FigurePx = V35.FigureSmall } };

        /// <summary>A reading's figure, or the ABSENT glyph in its slot where the model holds none (never a zero).</summary>
        private PeopleCell PeopleReading(int span, string icon, float value, int decimals, string unit, string name, string anchor, Symbol? glyph = null) =>
            value < 0f ? PeopleTile(span, icon, null, name, anchor, Symbol.Absent) : PeopleTile(span, icon, UiFormat.Number(value, decimals) + unit, name, anchor, glyph);

        private PeopleCell PeopleShare(int span, string icon, string figure, string name, string anchor, List<V35Part> parts, float whole)
        {
            PeopleCell cell = PeopleTile(span, icon, figure, name, anchor);
            cell.Parts = parts;
            cell.Whole = whole;
            return cell;
        }

        private void DrawPeopleV35(float width)
        {
            Country country = _playerCountry;
            EconomyState s = country.State;
            if (Event.current.type == EventType.Repaint) { _peopleSectionTops["top"] = 0f; }
            DrawPeoplePopulationCard(width);
            DrawPeopleDense(width, "head:population", "head:turnout");

            PopulationCohorts cohorts = country.Cohorts;
            if (cohorts != null)
            {
                float total = cohorts.Total;
                PeopleSection("dependency", "Dependency", "head:dependency", width);
                DrawPeopleRow(width,
                    PeopleTile(3, "people", UiFormat.Number(cohorts.OldAgeDependencyRatio, 1), "Old-age ratio", "fig:oldage"),
                    PeopleTile(3, "people", UiFormat.Number(cohorts.TotalDependencyRatio, 1), "Total ratio", "fig:total"),
                    PeopleTile(3, "young", UiFormat.Number(cohorts.SchoolAgeShare, 1) + "%", "School age (0–19)", "fig:school"),
                    PeopleTile(3, "person", UiFormat.Number(cohorts.ElderlyShare, 1) + "%", "Elderly (65+)", "fig:elderly"));
                float young = total > 0f ? cohorts.InAgeRange(0, 14) / total * 100f : 0f;
                float working = total > 0f ? cohorts.InAgeRange(15, 64) / total * 100f : 0f;
                float old = total > 0f ? cohorts.InAgeRange(65, 999) / total * 100f : 0f;
                var ages = new List<V35Part>
                {
                    new V35Part("Under 15", UiFormat.Number(young, 0) + "%", V35.DataSlateLight, V35.OnDataDark, young, "card:agesplit"),
                    new V35Part("Working age 15–64", UiFormat.Number(working, 0) + "%", V35.DataSlate, V35.OnDataDark, working, "card:agesplit"),
                    new V35Part("65 and over", UiFormat.Number(old, 0) + "%", V35.DataSlateMid, V35.OnDataDark, old, "card:agesplit"),
                };
                DrawPeopleRow(width, PeopleShare(12, "pyr", UiFormat.Number(young + working + old, 0) + "%", "Age split", "card:agesplit", ages, 100f));
                DrawPeopleDense(width, "head:dependency", "fig:oldage", "fig:total", "fig:school", "fig:elderly", "card:agesplit");

                int votingAge = CohortVoterGroups.VotingAge(PlayerCountryId);
                CohortVoterGroups.Group[] groups = CohortVoterGroups.For(country);
                bool sourced = groups.Length > 0 && !double.IsNaN(groups[0].TurnoutBase);
                double eligible = CohortVoterGroups.EligiblePopulation(cohorts, votingAge);
                double votes = 0.0;
                if (sourced) { foreach (CohortVoterGroups.Group g in groups) { votes += g.PopulationShare * eligible * g.TurnoutBase / 100.0; } }
                PeopleSection("electorate", "Electorate", "head:electorate", width);
                DrawPeopleRow(width,
                    PeopleTile(3, "ballot", PeopleSlips.Millions((float)eligible), "Eligible voters", "fig:eligible"),
                    PeopleTile(3, "people", total > 0f ? UiFormat.Number((float)(eligible / total * 100.0), 1) + "%" : null, "Share of people", "fig:ofall", total > 0f ? (Symbol?)null : Symbol.Absent),
                    PeopleTile(3, "person", votingAge.ToString(CultureInfo.InvariantCulture), "Voting age", "fig:votingage"),
                    sourced ? PeopleTile(3, "check", "≈ " + PeopleSlips.Millions((float)votes), "Likely votes", "fig:votes", Symbol.Dated) : PeopleTile(3, "check", null, "Likely votes", "fig:votes", Symbol.Absent));
                DrawPeopleDense(width, "head:electorate", "fig:eligible", "fig:ofall", "fig:votingage", "fig:votes");
            }

            // ---- health ----
            PeopleSection("health", "Health", "section:health", width);
            HealthSeeds h = country.Health;
            if (h == null || !h.Seeded) { DrawPeopleEmpty(width, "This country carries no health family"); }
            else
            {
                PeopleCell mortality = PeopleReading(3, "heart", s.TreatableMortality, 0, string.Empty, "Treatable deaths", "health:mortality");
                if (DraftedSpending(HealthFamily.IsHealthLine, out float draft, out float standing))
                {
                    PeopleDraftChange(mortality, HealthFamily.ProjectTreatableMortality(country, draft) - HealthFamily.ProjectTreatableMortality(country, standing), 1, string.Empty, -1);
                }
                DrawPeopleRow(width,
                    PeopleReading(3, "cross", s.HealthCoverage, 1, "%", "Coverage", "health:coverage"),
                    PeopleReading(3, "bank", HealthFamily.PublicCoverageNow(country), 1, "%", "Public share", "health:public"),
                    mortality,
                    h.HasWaits ? PeopleReading(3, "clock", s.WaitCataractDays, 0, " d", "Cataract wait", "health:cataract") : PeopleTile(3, "clock", null, "Cataract wait", "health:cataract", Symbol.Absent));
                string[] supportingNames = { "Asthma + COPD", "Diabetes", "Heart failure", "Heart attack", "Stroke" };   // the composition's names; the slip carries the series'
                int[] spans = { 3, 3, 2, 2, 2 };
                var supporting = new PeopleCell[supportingNames.Length];
                for (int i = 0; i < supportingNames.Length; i++)
                {
                    supporting[i] = h.HasSupporting
                        ? PeopleReading(spans[i], "heart", HealthFamily.SupportingNow(country, i), 1, string.Empty, supportingNames[i], "health:sup:" + i.ToString(CultureInfo.InvariantCulture))
                        : PeopleTile(spans[i], "heart", null, supportingNames[i], "health:sup:" + i.ToString(CultureInfo.InvariantCulture), Symbol.Absent);
                }
                DrawPeopleRow(width, supporting);
                DrawPeopleDense(width, "section:health", "health:coverage", "health:public", "health:mortality", "health:cataract", "health:sup:0", "health:sup:1", "health:sup:2", "health:sup:3", "health:sup:4");
            }

            // ---- education ----
            PeopleSection("education", "Education", "section:education", width);
            EducationSeeds e = country.Education;
            if (e == null || !e.Seeded) { DrawPeopleEmpty(width, "This country carries no education family"); }
            else
            {
                PeopleCell leavers = e.HasEarlyLeavers ? PeopleReading(6, "young", s.EarlyLeavers, 1, "%", "Early school leavers", "education:leavers") : PeopleTile(6, "young", null, "Early school leavers", "education:leavers", Symbol.Absent);
                if (e.HasEarlyLeavers && DraftedSpending(EducationFamily.IsEducationLine, out float draft, out float standing))
                {
                    // the plate's 5c arrow: next year's early leavers WITH the per-pupil push against WITHOUT, in points
                    EducationFamily.Targets with = EducationFamily.TargetsFor(country, draft / Mathf.Max(0.0001f, s.PriceLevel));
                    EducationFamily.Targets without = EducationFamily.TargetsFor(country, standing / Mathf.Max(0.0001f, s.PriceLevel));
                    if (with.Leavers >= 0f && without.Leavers >= 0f)
                    {
                        float now = s.EarlyLeavers;
                        PeopleDraftChange(leavers, (now + (with.Leavers - now) * EducationFamily.LeaversReversionPerYear) - (now + (without.Leavers - now) * EducationFamily.LeaversReversionPerYear), 1, " pts", -1);
                    }
                }
                DrawPeopleRow(width,
                    PeopleTile(3, "book", null, "PISA score", "education:pisa", Symbol.Billed),
                    PeopleTile(3, "book", null, "Graduation rate", "education:graduation", Symbol.Billed),
                    leavers);
                var levels = new List<V35Part>
                {
                    new V35Part("Below upper secondary", UiFormat.Number(s.AttainmentBelowUpperSecondary, 0) + "%", V35.DataSand, V35.OnDataLight, s.AttainmentBelowUpperSecondary, "education:attainment"),
                    new V35Part("Upper secondary", UiFormat.Number(s.AttainmentUpperSecondary, 0) + "%", V35.DataSlateLight, V35.OnDataLight, s.AttainmentUpperSecondary, "education:attainment"),
                    new V35Part("Tertiary", UiFormat.Number(s.AttainmentTertiary, 0) + "%", V35.DataDeep, V35.OnDataDark, s.AttainmentTertiary, "education:attainment"),
                };
                DrawPeopleRow(width, PeopleShare(12, "book", UiFormat.Number(EducationFamily.AtLeastUpperSecondary(country), 1) + "%", "Upper secondary or more, aged 25–64", "education:attainment", levels, 100f));
                DrawPeopleDense(width, "section:education", "education:pisa", "education:graduation", "education:leavers", "education:attainment");
            }

            // ---- infrastructure ----
            PeopleSection("infrastructure", "Infrastructure", "section:infrastructure", width);
            InfrastructureSeeds f = country.Infrastructure;
            if (f == null || !f.Seeded) { DrawPeopleEmpty(width, "This country carries no infrastructure family"); }
            else
            {
                PeopleCell quality = PeopleReading(3, "town", s.RoadQuality, 1, string.Empty, "Road quality", "infra:quality", Symbol.Dated);
                if (DraftedSpending(InfrastructureFamily.IsInfrastructureLine, out float draft, out float standing))
                {
                    PeopleDraftChange(quality, InfrastructureFamily.ProjectRoadQuality(country, draft) - InfrastructureFamily.ProjectRoadQuality(country, standing), 1, " pts", 1);
                }
                DrawPeopleRow(width,
                    quality,
                    PeopleReading(3, "town", s.RoadConnectivity, 1, string.Empty, "Road connectivity", "infra:connectivity"),
                    PeopleTile(3, "clock", null, "Congestion", "infra:congestion", Symbol.Billed),
                    PeopleTile(3, "town", null, "Road length", "infra:length", Symbol.Absent));
                DrawPeopleDense(width, "section:infrastructure", "infra:quality", "infra:connectivity", "infra:congestion", "infra:length");
            }

            // ---- environment ----
            PeopleSection("environment", "Environment", "section:environment", width);
            EnvironmentSeeds env = country.Environment;
            if (env == null || !env.Seeded) { DrawPeopleEmpty(width, "This country carries no environment family"); }
            else
            {
                float ghg = EnvironmentFamily.GhgPerCapitaNow(country);
                float rest = Mathf.Max(0f, ghg - s.PowerCo2PerCapita - s.TransportCo2PerCapita);
                var split = new List<V35Part>
                {
                    new V35Part("Electricity", UiFormat.Number(s.PowerCo2PerCapita, 2) + " t", V35.DataSand, V35.OnDataLight, s.PowerCo2PerCapita, "env:ghg"),
                    new V35Part("Transport", UiFormat.Number(s.TransportCo2PerCapita, 2) + " t", V35.DataSlateLight, V35.OnDataLight, s.TransportCo2PerCapita, "env:ghg"),
                    new V35Part("Everything else", UiFormat.Number(rest, 2) + " t", V35.DataDeep, V35.OnDataDark, rest, "env:ghg"),
                };
                PeopleCell transport = PeopleReading(3, "trade", s.TransportCo2PerCapita, 2, " t", "Transport CO2", "env:transport");
                float standingRate = EnvironmentFamily.CarbonTaxRate(country);
                if (_taxRateInputs.TryGetValue(TaxType.CarbonTax, out float draftedRate) && !Mathf.Approximately(draftedRate, standingRate))
                {
                    // EN-4d: the fleet pays the ETS and is exempt of the carbon tax - the transport row answers the draft; emissions are no consensus outcome (rule 5)
                    PeopleDraftChange(transport, EnvironmentFamily.ProjectTransportCo2(country, draftedRate) - EnvironmentFamily.ProjectTransportCo2(country, standingRate), 2, " t", 0);
                }
                DrawPeopleRow(width,
                    PeopleShare(6, "smoke", UiFormat.Number(ghg, 2) + " t", "Emissions per person", "env:ghg", split, Mathf.Max(ghg, s.PowerCo2PerCapita + s.TransportCo2PerCapita)),
                    PeopleReading(3, "bolt", s.PowerCo2PerCapita, 2, " t", "Electricity CO2", "env:power"),
                    transport);
                DrawPeopleDense(width, "section:environment", "env:ghg", "env:power", "env:transport");
            }

            // ---- immigration · poverty depth (the model's family; the composition's MIGRATION figures are not the model's) ----
            PeopleSection("migration", "Immigration · poverty depth", "section:migration", width);
            MigrationPovertySeeds m = country.MigrationPoverty;
            if (m == null || !m.Seeded) { DrawPeopleEmpty(width, "This country carries no immigration and poverty family"); }
            else
            {
                PeopleCell irregular = PeopleReading(3, "passport", s.IrregularMigrationPer10k, m.MigrationIsStock ? 0 : 1, string.Empty, "Irregular migration", "mig:irregular");
                irregular.Tile.Mark = DeskProvenance.TwoDefinitionGlyph;
                DrawPeopleRow(width,
                    irregular,
                    PeopleReading(3, "bowl", s.PovertyGap, 1, "%", "Poverty gap", "mig:gap"),
                    PeopleReading(3, "jobs", s.Underemployment, 2, "%", "Underemployment", "mig:underemployment"),
                    PeopleReading(3, "home", s.HomelessPer10k, 0, string.Empty, "Homelessness", "mig:homeless"));
                DrawPeopleDense(width, "section:migration", "mig:irregular", "mig:gap", "mig:underemployment", "mig:homeless");
            }

            // ---- employment by sector: not in the composition - KEPT, and asked ----
            PeopleSection("employment", "Employment", "section:employment", width);
            var jobs = new List<V35Part>();
            float eight = 0f;
            int index = 0;
            foreach (Sector sector in country.Sectors)
            {
                // Spaced, NOT Of: SectorType.Energy resolves through the curated policy table to "Energy (Spending)", a spending line.
                jobs.Add(new V35Part(DisplayName.Spaced(sector.Type.ToString()), UiFormat.Number(sector.EmploymentShare, 1) + "%", UiPalette.GetCategoricalColor(index), PoliSimTheme.TextOnDesk, sector.EmploymentShare));
                eight += Mathf.Max(0f, sector.EmploymentShare);
                index++;
            }
            if (jobs.Count == 0) { DrawPeopleEmpty(width, "Not tracked for this country"); }
            else
            {
                float other = Mathf.Max(0f, 100f - eight);
                jobs.Add(new V35Part("Other", UiFormat.Number(other, 1) + "%", PoliSimTheme.Neutral, PoliSimTheme.TextOnDesk, other, "jobs:other"));
                DrawPeopleRow(width, PeopleShare(12, "sectors", UiFormat.Number(eight, 1) + "%", "Share of employment", "jobs:card", jobs, Mathf.Max(100f, eight)));
                DrawPeopleDense(width, "section:employment", "jobs:card", "jobs:other");
            }
            GUILayout.Space(V35.Px(12f));
        }

        /// <summary>A section's head: the serif in capitals at the floor, its slip on its words. Records the section's top for the film.</summary>
        private void PeopleSection(string key, string title, string anchor, float width)
        {
            GUILayout.Space(V35.Px(10f));
            float h = V35.Px(26f);
            Rect row = GUILayoutUtility.GetRect(width, h, GUILayout.Width(width), GUILayout.Height(h));
            if (Event.current.type == EventType.Repaint) { _peopleSectionTops[key] = row.y; }
            DrawV35SectionHead(row, title);
            GUIStyle face = V35Serif(V35.Floor, PoliSimTheme.TextMuted);
            SlipAnchor(new Rect(row.x, row.y, Mathf.Min(row.width, face.CalcSize(new GUIContent(title.ToUpperInvariant())).x + 4f), row.height), anchor);
            GUILayout.Space(V35.Px(6f));
        }

        /// <summary>A row of cells across the twelve columns, as tall as its tallest; a tile's slip on the whole tile, a share card's on its head and
        /// on each part.</summary>
        private void DrawPeopleRow(float width, params PeopleCell[] cells)
        {
            float gutter = V35.Px(V35.Gutter);
            var widths = new float[cells.Length];
            float rowHeight = 0f;
            for (int i = 0; i < cells.Length; i++)
            {
                widths[i] = V35Span(width, cells[i].Span);
                float h = cells[i].Parts != null ? V35ShareCardHeight(widths[i], cells[i].Tile, cells[i].Parts, cells[i].Whole) : V35TileHeight(cells[i].Tile);
                rowHeight = Mathf.Max(rowHeight, h);
            }
            Rect row = GUILayoutUtility.GetRect(width, rowHeight, GUILayout.Width(width), GUILayout.Height(rowHeight));
            float x = row.x;
            for (int i = 0; i < cells.Length; i++)
            {
                var r = new Rect(x, row.y, widths[i], rowHeight);
                if (cells[i].Parts != null) { SlipAnchor(DrawV35ShareCard(r, cells[i].Tile, cells[i].Parts, cells[i].Whole, SlipAnchor), cells[i].Anchor); }
                else
                {
                    DrawV35Tile(r, cells[i].Tile);
                    SlipAnchor(r, cells[i].Anchor);
                }
                x += widths[i] + gutter;
            }
            GUILayout.Space(gutter);
        }

        /// <summary>A family this country does not carry: one muted card that says so.</summary>
        private void DrawPeopleEmpty(float width, string text)
        {
            float h = V35.Px(V35.CardPadY) * 2f + V35.Px(V35.ListRow);
            Rect r = GUILayoutUtility.GetRect(width, h, GUILayout.Width(width), GUILayout.Height(h));
            Rect inside = DrawV35Card(r);
            if (Event.current.type == EventType.Repaint) { PoliSimWidgets.MeasuredLabel(inside, text, V35Serif(V35.Name, PoliSimTheme.TextMuted)); }
            GUILayout.Space(V35.Px(V35.Gutter));
        }

        /// <summary>
        /// The † dense view (V35_ASK rule 3): each named slip as one dotted line under its section - the head, then the lines - across the page's width.
        /// Lines, never columns; nothing drawn while the view is off.
        /// </summary>
        private void DrawPeopleDense(float width, params string[] anchors)
        {
            if (!DeskProvenance.On) { return; }
            GUIStyle face = V35SerifWrapped(V35.Floor, PoliSimTheme.TextMuted);
            foreach (string id in anchors)
            {
                if (!_peopleBook.Anchors.TryGetValue(id, out SlipContent slip)) { continue; }
                string text = SlipContent.Plain(slip.Head);
                foreach (string l in slip.Lines) { text += " · " + SlipContent.Plain(l); }
                float textHeight = Mathf.Ceil(face.CalcHeight(new GUIContent(text), width));
                float h = textHeight + V35.Px(6f);
                Rect r = GUILayoutUtility.GetRect(width, h, GUILayout.Width(width), GUILayout.Height(h));
                if (Event.current.type != EventType.Repaint) { continue; }
                for (float x = r.x; x < r.xMax; x += 4f) { PoliSimTheme.Rule(new Rect(x, r.y, 1f, 1f), PoliSimTheme.HairlineStrong); }
                GUI.Label(new Rect(r.x, r.y + V35.Px(4f), r.width, textHeight), text, face);
            }
            GUILayout.Space(V35.Px(4f));
        }

        /// <summary>The standing and the drafted total of the spending lines a family answers to; false where no such line carries a draft.</summary>
        private bool DraftedSpending(System.Func<SpendingCategory, bool> isLine, out float draft, out float standing)
        {
            bool live = false;
            draft = 0f;
            standing = 0f;
            foreach (SpendingLine line in _playerCountry.SpendingLines)
            {
                if (!isLine(line.Category)) { continue; }
                standing += line.Amount;
                if (_spendingLineInputs.TryGetValue(line.Category, out float drafted)) { draft += drafted; live = true; }
                else { draft += line.Amount; }
            }
            return live;
        }

        /// <summary>
        /// A draft's effect on a tile (the plates' 5c arrow): ▲▼ and the size, in the outcome's ink where most agree which way is better
        /// (<paramref name="consensus"/> +1 higher is better, -1 lower is, 0 neither - rule 5), a zero printed muted; and the line in the tile's slip.
        /// </summary>
        private void PeopleDraftChange(PeopleCell cell, float delta, int decimals, string unit, int consensus)
        {
            if (cell.Tile.Glyph == Symbol.Absent || float.IsNaN(delta)) { return; }
            float shown = (float)System.Math.Round(delta, decimals);
            if (shown == 0f)
            {
                cell.Tile.Change = UiFormat.Number(0f, decimals) + unit;
                cell.Tile.ChangeInk = PoliSimTheme.TextMuted;
            }
            else
            {
                cell.Tile.Change = (shown > 0f ? "▲ " : "▼ ") + UiFormat.Number(Mathf.Abs(shown), decimals) + unit;
                cell.Tile.ChangeInk = consensus == 0 ? V35.DirectionNeutral : (shown > 0f) == (consensus > 0) ? PoliSimTheme.Good : PoliSimTheme.Bad;
            }
            if (_peopleBook.Anchors.TryGetValue(cell.Anchor, out SlipContent slip))
            {
                slip.Add("IF THE DRAFT PASSES · " + (shown > 0f ? "+" : shown < 0f ? "−" : string.Empty) + UiFormat.Number(Mathf.Abs(shown), decimals) + unit.ToUpperInvariant() + " NEXT YEAR, WITH THE DRAFT AGAINST WITHOUT");
            }
        }

        /// <summary>
        /// The population card (the composition's): the head - the icon, the total and the name; the bands as bars, one 13 px row each, working age in the
        /// data slate and the dependent bands lighter, the two dashed thresholds at 15 and 65 across the card, five start-ages on the axis, the peak band's
        /// count at its bar's end and the axis's two ends under it; and the turnout column - its head with ◇, a hairline lane where a band can vote and the
        /// band's tick on it. Every band's row, and its turnout lane, opens the band's slip.
        /// </summary>
        private void DrawPeoplePopulationCard(float width)
        {
            Color area = UiPalette.GetAreaColor(UiPalette.SystemArea.Labor);
            PopulationCohorts cohorts = _playerCountry.Cohorts;
            if (cohorts == null)
            {
                DrawPeopleEmpty(width, "This country carries no cohort substrate yet - the instruments draw when it does");
                SlipAnchor(GUILayoutUtility.GetLastRect(), "head:population");
                return;
            }
            int votingAge = CohortVoterGroups.VotingAge(PlayerCountryId);
            CohortVoterGroups.Group[] groups = CohortVoterGroups.For(_playerCountry);
            bool sourced = groups.Length > 0 && !double.IsNaN(groups[0].TurnoutBase);
            float total = cohorts.Total, max = 0f;
            for (int i = 0; i < PopulationCohorts.CohortCount; i++) { max = Mathf.Max(max, cohorts.Counts[i]); }

            float pitch = V35.Px(13f), headHeight = V35.Px(30f);
            GUIStyle axis = V35Serif(V35.Floor, PoliSimTheme.TextMuted);
            GUIStyle axisRight = V35Serif(V35.Floor, PoliSimTheme.TextMuted, TextAnchor.MiddleRight);
            float lineHeight = Mathf.Ceil(axis.CalcSize(new GUIContent("0")).y);
            float cardHeight = V35.Px(V35.CardPadY) * 2f + headHeight + V35.Px(6f) + pitch * PopulationCohorts.CohortCount + V35.Px(4f) + lineHeight;
            Rect card = GUILayoutUtility.GetRect(width, cardHeight, GUILayout.Width(width), GUILayout.Height(cardHeight));
            Rect inner = DrawV35Card(card);
            float turnoutWidth = V35.Px(300f), columnGap = V35.Px(26f);
            var left = new Rect(inner.x, inner.y, Mathf.Max(1f, inner.width - turnoutWidth - columnGap), inner.height);
            var right = new Rect(left.xMax + columnGap, inner.y, turnoutWidth, inner.height);

            // the heads: the population's (the icon, the total, the name) and the turnout's (the icon, the word, ◇ where its series is dated)
            float icon = V35.Px(30f);
            DrawV35Icon(new Rect(left.x, left.y, icon, icon), "pyr", area);
            GUIStyle figureFace = V35Mono(26f, PoliSimTheme.TextPrimary, bold: true);
            GUIStyle nameFace = V35Serif(17f, PoliSimTheme.TextPrimary);
            string totalText = PeopleSlips.Millions(total);
            float fx = left.x + icon + V35.Px(12f);
            float fw = Mathf.Ceil(figureFace.CalcSize(new GUIContent(totalText)).x) + 2f;
            float nw = Mathf.Ceil(nameFace.CalcSize(new GUIContent("People by age")).x) + 2f;
            SlipAnchor(new Rect(left.x, left.y, icon + V35.Px(12f) + fw + V35.Px(12f) + nw, headHeight), "head:population");
            float turnoutIcon = V35.Px(26f);
            DrawV35Icon(new Rect(right.x, right.y + Mathf.Round((headHeight - turnoutIcon) * 0.5f), turnoutIcon, turnoutIcon), "ballot", area);
            float tw = Mathf.Ceil(nameFace.CalcSize(new GUIContent("Turnout")).x) + 2f;
            float glyphSide = V35.Px(16f);
            SlipAnchor(new Rect(right.x, right.y, turnoutIcon + V35.Px(10f) + tw + V35.Px(8f) + glyphSide, headHeight), "head:turnout");

            float rowsY = inner.y + headHeight + V35.Px(6f);
            float labelWidth = V35.Px(30f), labelGap = V35.Px(8f);
            GUIStyle peakFace = V35Mono(V35.Floor, PoliSimTheme.TextPrimary, bold: true);
            string peakText = PeopleSlips.Millions(max);
            float peakWidth = Mathf.Ceil(peakFace.CalcSize(new GUIContent(peakText)).x) + 2f, peakHeight = Mathf.Ceil(peakFace.CalcSize(new GUIContent(peakText)).y);
            float barX = left.x + labelWidth + labelGap;
            float barMax = Mathf.Max(1f, left.xMax - barX - V35.Px(8f) - peakWidth);
            for (int i = 0; i < PopulationCohorts.CohortCount; i++)
            {
                float y = rowsY + i * pitch;
                SlipAnchor(new Rect(left.x, y, left.width, pitch), "band:" + i.ToString(CultureInfo.InvariantCulture));
                SlipAnchor(new Rect(right.x, y, right.width, pitch), "band:" + i.ToString(CultureInfo.InvariantCulture));
            }
            if (Event.current.type != EventType.Repaint) { return; }

            GUI.Label(new Rect(fx, left.y + Mathf.Round((headHeight - Mathf.Ceil(figureFace.CalcSize(new GUIContent("0")).y)) * 0.5f), fw, Mathf.Ceil(figureFace.CalcSize(new GUIContent("0")).y)), totalText, figureFace);
            PoliSimWidgets.MeasuredLabel(new Rect(fx + fw + V35.Px(12f), left.y, nw, headHeight), "People by age", nameFace);
            float tx = right.x + turnoutIcon + V35.Px(10f);
            PoliSimWidgets.MeasuredLabel(new Rect(tx, right.y, tw, headHeight), "Turnout", nameFace);
            DrawStateGlyph(new Rect(tx + tw + V35.Px(8f), right.y + Mathf.Round((headHeight - glyphSide) * 0.5f), glyphSide, glyphSide), sourced ? Symbol.Dated : Symbol.Absent, PoliSimTheme.TextSecondary);

            float barHeight = V35.Px(10f);
            for (int i = 0; i < PopulationCohorts.CohortCount; i++)
            {
                float y = rowsY + i * pitch;
                int from = i * PopulationCohorts.CohortWidth;
                int to = i == PopulationCohorts.OpenBandIndex ? 999 : from + PopulationCohorts.CohortWidth - 1;
                bool workingAge = from >= 15 && from < 65;
                if (from == 0 || from == 15 || from == 40 || from == 65 || from == 90)
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(left.x, y + Mathf.Round((pitch - lineHeight) * 0.5f), labelWidth, lineHeight), from.ToString(CultureInfo.InvariantCulture), axisRight);
                }
                float length = max > 0f ? Mathf.Max(1f, barMax * cohorts.Counts[i] / max) : 1f;
                PoliSimTheme.Rule(new Rect(barX, y + Mathf.Round((pitch - barHeight) * 0.5f), length, barHeight), workingAge ? V35.DataSlate : V35.DataSlateLight);
                if (cohorts.Counts[i] >= max && max > 0f)
                {
                    PoliSimWidgets.MeasuredLabel(new Rect(barX + length + V35.Px(8f), y + Mathf.Round((pitch - peakHeight) * 0.5f), peakWidth, peakHeight), peakText, peakFace);
                }
                // the turnout lane: a hairline and the band's tick where the band can vote (and the series is sourced); nothing where it cannot
                if (sourced && to >= votingAge)
                {
                    PoliSimTheme.Rule(new Rect(right.x, y + V35.Px(6f), right.width, 1f), V35.CardEdge);
                    double turnout = PeopleSlips.BandTurnout(groups, Mathf.Max(from, votingAge));
                    if (!double.IsNaN(turnout))
                    {
                        float tickX = right.x + right.width * Mathf.Clamp01((float)turnout / 100f);
                        PoliSimTheme.Rule(new Rect(tickX - V35.Px(1f), y + V35.Px(1f), V35.Px(3f), V35.Px(11f)), V35.DataSlate);
                    }
                }
                // 15 and 65, the substrate's own thresholds: dashed across both columns at the top of their bands
                if (from == 15 || from == 65)
                {
                    DrawDashedRule(new Rect(left.x, y, left.width, 1f), V35.PyramidThreshold, 4f, 3f);
                    DrawDashedRule(new Rect(right.x, y, right.width, 1f), V35.PyramidThreshold, 4f, 3f);
                }
            }
            float axisY = rowsY + pitch * PopulationCohorts.CohortCount + V35.Px(4f);
            PoliSimWidgets.MeasuredLabel(new Rect(barX, axisY, barMax * 0.5f, lineHeight), "0", axis);
            PoliSimWidgets.MeasuredLabel(new Rect(barX + barMax * 0.5f, axisY, barMax * 0.5f, lineHeight), peakText, axisRight);
            PoliSimWidgets.MeasuredLabel(new Rect(right.x, axisY, right.width * 0.5f, lineHeight), "0", axis);
            PoliSimWidgets.MeasuredLabel(new Rect(right.x + right.width * 0.5f, axisY, right.width * 0.5f, lineHeight), "100%", axisRight);
        }
    }
}
