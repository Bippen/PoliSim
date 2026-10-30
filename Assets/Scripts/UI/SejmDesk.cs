using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;

namespace PoliSim.UI
{
    /// <summary>
    /// S11 (Design's return of 2026-10-30, boards 11d and 11e, built decluttered under K320): <b>THE SEJM'S STEPS AFTER THE NIGHT</b>, as data - a pure function of the
    /// player's Poland and the day, read by the Docket (`GameController.DrawPolishDeskItems`), the strip's place 4 (<see cref="TopStrip.SejmStepChip"/>) and the
    /// announcement plate (`GameController.SejmPlate.cs`).
    /// <para><b>The record read.</b> The engine keeps the procedure on the government record it runs on (<see cref="SejmFormation"/>, saved with it): its stage, the
    /// day the next step falls, and its dated <see cref="SejmFormation.Log"/> - "the steps as they fell, dated", one line per step in `SimulationManager.Sejm.cs`'s own
    /// words. Each Docket entry is one log line: its day is the line's own date (the engine's day, never the board's), and its short words are read from the line's
    /// citation and figures (DECLARED: a reading of the engine's wording; a line it does not recognise becomes a generic entry with the line itself in its slip, so
    /// nothing is dropped and nothing is guessed). The steps' own sentences stay in the engine's log; at rest the entry is decluttered (K320): a title of the article
    /// and the outcome, one short line, the rest in the slip.</para>
    /// </summary>
    public static class SejmDesk
    {
        /// <summary>One step's Docket entry - its day, its words at rest, its slip, and what the plate reads of it.</summary>
        public sealed class Entry
        {
            public DateTime Day;
            public string Key;
            /// <summary>The title before its day ("Art. 154 · PiS's cabinet refused").</summary>
            public string Label;
            /// <summary>The muted line under it, null for none.</summary>
            public string Line;
            public readonly List<string> Slip = new List<string>();
            /// <summary>0 the first sitting, 1 to 3 the steps (Art. 154 ust. 1-2, ust. 3, Art. 155 ust. 1), 4 the shortened term; -1 a line not recognised.</summary>
            public int Step = -1;
            /// <summary>A vote (or the step's failure) the plate prints: the article, the party put, the result.</summary>
            public bool Decides;
            public bool Refused;
            public string Article, Asked, Result;
            /// <summary>The shortened term's polling day (Step 4).</summary>
            public DateTime PollingDay = DateTime.MinValue;
        }

        /// <summary>DECLARED (K163's idiom, as the Art. 158 entries): a concluded procedure's entries stay on the Docket for this many days after its last step.</summary>
        public const int ListedDaysAfter = 30;

        private static readonly Regex LineRx = new Regex(@"^(\d{4}-\d{2}-\d{2}): (.*)$", RegexOptions.Singleline);

        /// <summary>The player's Poland's procedure record, null where none runs or ran on the government in office.</summary>
        public static SejmFormation Record(Country country) =>
            country != null && SejmFormation.Applies(country.Id) ? country.Government?.Sejm : null;

        /// <summary>The procedure's entries, oldest first - each recognised log line one entry (the night's own line, the Sejm's election, is the night's and is not
        /// listed; a designation joins the first sitting's slip; a carried path's "concluded" line repeats its install and is not listed).</summary>
        public static List<Entry> Entries(SejmFormation f)
        {
            var entries = new List<Entry>();
            if (f?.Log == null) { return entries; }
            for (int i = 0; i < f.Log.Count; i++)
            {
                Match m = LineRx.Match(f.Log[i] ?? string.Empty);
                if (!m.Success || !DateTime.TryParseExact(m.Groups[1].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime day)) { continue; }
                string text = m.Groups[2].Value;
                if (text.StartsWith("the Sejm is elected;", StringComparison.Ordinal)) { continue; }
                if (text.StartsWith("the President designates ", StringComparison.Ordinal) && entries.Count > 0 && entries[entries.Count - 1].Step == 0)
                {
                    entries[entries.Count - 1].Slip.Add(Designates(text));
                    continue;
                }
                Entry e = Classify(day, text);
                if (e == null) { continue; }
                e.Key = "sejm:" + day.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ":" + i.ToString(CultureInfo.InvariantCulture);
                entries.Add(e);
            }
            return entries;
        }

        /// <summary>Whether the procedure's entries are listed on <paramref name="today"/>: while it runs, and for <see cref="ListedDaysAfter"/> days after its last line.</summary>
        public static bool Listed(SejmFormation f, DateTime today)
        {
            if (f == null) { return false; }
            if (f.Open) { return true; }
            List<Entry> entries = Entries(f);
            return entries.Count > 0 && (today.Date - entries[entries.Count - 1].Day.Date).TotalDays <= ListedDaysAfter;
        }

        /// <summary>The day the term was shortened (Art. 155 ust. 2) and its polling day, read off the record's concluding line; false where it was not.</summary>
        public static bool TryShortened(SejmFormation f, out DateTime orderedOn, out DateTime pollingDay)
        {
            orderedOn = pollingDay = DateTime.MinValue;
            if (f == null || f.Open) { return false; }
            List<Entry> entries = Entries(f);
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].Step != 4) { continue; }
                orderedOn = entries[i].Day;
                pollingDay = entries[i].PollingDay;
                return true;
            }
            return false;
        }

        private static string Short(string key) => PartySystems.ShortName(CountryId.Poland, key) ?? key;

        private static string Possessive(string key) => PoliSim.Wording.EnglishText.Possessive(Short(key), false, true);

        /// <summary>A "+"-joined list of keys in the engine's line, in short names joined as the night joins them.</summary>
        private static string Cabinet(string joined) => SejmNightSteps.Names(joined.Split('+'));

        private static string Need(string members) => int.TryParse(members, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? SejmNightSteps.Needed(n).ToString(CultureInfo.InvariantCulture) : "?";

        private static string Day(string iso) => DateTime.TryParseExact(iso, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d) ? d.ToString("d MMM", CultureInfo.InvariantCulture) : iso;

        private static string Designates(string text)
        {
            Match head = Regex.Match(text, @"^the President designates (.+?), the record's designee");
            if (head.Success) { return "designates " + head.Groups[1].Value + " · Art. 154.1"; }
            Match cand = Regex.Match(text, @"^the President designates (\S+?)'s candidate for (\S+)");
            return cand.Success ? "designates " + Possessive(cand.Groups[1].Value) + " candidate · Art. 154.1" : "the President designates · Art. 154.1";
        }

        /// <summary>One log line read: its step, its title and line at rest, its slip, and what the plate reads. Null for a line not listed.</summary>
        private static Entry Classify(DateTime day, string text)
        {
            var e = new Entry { Day = day };
            Match m;
            if (text.StartsWith("the new Sejm's first sitting - ", StringComparison.Ordinal))
            {
                m = Regex.Match(text, @"- (\S+?)'s cabinet resigns");
                string who = m.Success ? Possessive(m.Groups[1].Value) + " cabinet" : "the cabinet";
                e.Step = 0; e.Label = "Art. 162 · First sitting"; e.Line = who + ": caretaker";
                e.Slip.Add(who + " resigns at the first sitting · Art. 162.1");
                e.Slip.Add("it serves on until the next is appointed · Art. 162.3");
                return e;
            }
            if (text.StartsWith("no cabinet the Sejm could carry", StringComparison.Ordinal))
            {
                e.Step = 1; e.Decides = true; e.Refused = true; e.Label = "Art. 154 · No designee"; e.Line = "no appointment in 14 days";
                e.Article = "Art. 154.1"; e.Result = "no designee";
                e.Slip.Add("no cabinet the Sejm could carry is designated");
                e.Slip.Add("the President's 14 days pass · Art. 154.1");
                e.Slip.Add("the Sejm's own step follows · Art. 154.3");
                return e;
            }
            if ((m = Regex.Match(text, @"^the President appoints the cabinet the Sejm elected - (\S+) led by (\S+) takes office")).Success)
            {
                e.Step = 2; e.Label = "Art. 154.3 · " + Cabinet(m.Groups[1].Value) + " in office"; e.Line = "led by " + Short(m.Groups[2].Value);
                e.Slip.Add("the President appoints the cabinet the Sejm elected · Art. 154.3");
                e.Slip.Add("led by " + Short(m.Groups[2].Value));
                return e;
            }
            if ((m = Regex.Match(text, @"^the President appoints (\S+) led by (\S+?)(?: with (\S+))? \(Art\. 154 ust\. 1\)")).Success)
            {
                e.Step = 1; e.Label = "Art. 154 · " + Possessive(m.Groups[2].Value) + " cabinet appointed"; e.Line = "confidence vote next";
                e.Slip.Add(Cabinet(m.Groups[1].Value) + (m.Groups[3].Success ? " · with " + Cabinet(m.Groups[3].Value) : string.Empty));
                e.Slip.Add("appointed · Art. 154.1");
                e.Slip.Add("the Sejm's confidence within 14 days · Art. 154.2");
                return e;
            }
            if ((m = Regex.Match(text, @"^the President appoints (\S+) led by (\S+?)(?: with (\S+))? \(Art\. 155 ust\. 1\)")).Success)
            {
                e.Step = 3; e.Label = "Art. 155 · " + Possessive(m.Groups[2].Value) + " cabinet appointed"; e.Line = "confidence vote · last step";
                e.Slip.Add(Cabinet(m.Groups[1].Value) + (m.Groups[3].Success ? " · with " + Cabinet(m.Groups[3].Value) : string.Empty));
                e.Slip.Add("appointed by the President · Art. 155.1");
                e.Slip.Add("confidence by more for than against");
                e.Slip.Add("refused, the term is shortened · Art. 155.2");
                return e;
            }
            if (text.StartsWith("the Sejm's confidence in the President's cabinet - ", StringComparison.Ordinal))
            {
                m = Regex.Match(text, @"cabinet - (\S+) led by (\S+?)(?: with \S+)?: (\d+) for, (\d+) against, (\d+) abstaining of (\d+) members - (carried|refused)");
                if (!m.Success) { return Generic(e, text); }
                bool carried = m.Groups[7].Value == "carried";
                e.Step = 3; e.Decides = true; e.Refused = !carried; e.Asked = m.Groups[2].Value;
                e.Label = "Art. 155 · " + (carried ? "Carried " : "Refused ") + m.Groups[3].Value + " to " + m.Groups[4].Value;
                e.Line = m.Groups[5].Value == "0" ? (carried ? "more for than against" : "more for needed") : m.Groups[5].Value + " abstaining";
                e.Article = "Art. 155.1 · " + Short(m.Groups[2].Value) + " asked"; e.Result = m.Groups[3].Value + " for · " + m.Groups[4].Value + " against";
                e.Slip.Add(Cabinet(m.Groups[1].Value) + " · " + m.Groups[3].Value + " for, " + m.Groups[4].Value + " against");
                e.Slip.Add(m.Groups[5].Value + " abstaining of " + m.Groups[6].Value);
                e.Slip.Add("more for than against carries it · Art. 155.1");
                return e;
            }
            if ((m = Regex.Match(text, @"^the Sejm's confidence in (\S+) - (\d+) of (\d+) members for: (carried|refused) \(Art\. 154 ust\. 2\)")).Success)
            {
                bool carried = m.Groups[4].Value == "carried";
                e.Step = 1; e.Decides = true; e.Refused = !carried;
                e.Label = "Art. 154 · " + Cabinet(m.Groups[1].Value) + (carried ? " carried" : " refused"); e.Line = m.Groups[2].Value + " for · " + Need(m.Groups[3].Value) + " needed";
                e.Article = "Art. 154.1–2 · " + Cabinet(m.Groups[1].Value); e.Result = m.Groups[2].Value + " for · " + Need(m.Groups[3].Value) + " needed";
                e.Slip.Add(m.Groups[2].Value + " of " + m.Groups[3].Value + " for · " + (carried ? "carried" : "refused"));
                e.Slip.Add("an absolute majority · Art. 154.2");
                if (!carried) { e.Slip.Add("it resigns and serves on · Art. 162.2"); e.Slip.Add("the Sejm elects within 14 days · Art. 154.3"); }
                return e;
            }
            if ((m = Regex.Match(text, @"^the Sejm elects (.+?), the record's, and (\S+), (\d+) of (\d+) members")).Success)
            {
                string pm = SejmFormation.PartyOfHead(m.Groups[1].Value);
                e.Step = 2; e.Decides = true; e.Asked = pm;
                e.Label = "Art. 154.3 · " + (pm != null ? Possessive(pm) + " candidate" : m.Groups[1].Value) + " elected"; e.Line = m.Groups[3].Value + " for · " + Need(m.Groups[4].Value) + " needed";
                e.Article = "Art. 154.3 · " + (pm != null ? Possessive(pm) + " candidate" : "the record's"); e.Result = m.Groups[3].Value + " for · " + Need(m.Groups[4].Value) + " needed";
                e.Slip.Add(m.Groups[1].Value + " and " + Cabinet(m.Groups[2].Value));
                e.Slip.Add(m.Groups[3].Value + " of " + m.Groups[4].Value + " for · an absolute majority");
                return e;
            }
            if ((m = Regex.Match(text, @"^the record's cabinet, (\S+), holds (\d+) of (\d+) members")).Success)
            {
                e.Step = 2; e.Decides = true; e.Refused = true;
                e.Label = "Art. 154.3 · " + Cabinet(m.Groups[1].Value) + " short"; e.Line = m.Groups[2].Value + " for · " + Need(m.Groups[3].Value) + " needed";
                e.Article = "Art. 154.3 · " + Cabinet(m.Groups[1].Value); e.Result = m.Groups[2].Value + " for · " + Need(m.Groups[3].Value) + " needed";
                e.Slip.Add("the record's cabinet holds " + m.Groups[2].Value + " of " + m.Groups[3].Value);
                e.Slip.Add("short of a majority · the projection is put instead");
                return e;
            }
            if (text.StartsWith("the Sejm elects no prime minister", StringComparison.Ordinal))
            {
                m = Regex.Match(text, @"the President appoints a cabinet on (\d{4}-\d{2}-\d{2})");
                e.Step = 2; e.Decides = true; e.Refused = true; e.Label = "Art. 154.3 · No prime minister";
                e.Line = m.Success ? "Art. 155 · cabinet by " + Day(m.Groups[1].Value) : "Art. 155 follows";
                e.Article = "Art. 154.3"; e.Result = "no prime minister";
                e.Slip.Add("the Sejm elects no prime minister · Art. 154.3");
                Match until = Regex.Match(text, @"run to (\d{4}-\d{2}-\d{2})");
                if (until.Success) { e.Slip.Add("its 14 days run to " + Day(until.Groups[1].Value)); }
                if (m.Success) { e.Slip.Add("the President appoints by " + Day(m.Groups[1].Value) + " · Art. 155.1"); }
                return e;
            }
            if ((m = Regex.Match(text, @"^the Sejm elects (\S+?)'s candidate prime minister and (\S+), (\d+) of (\d+) members")).Success)
            {
                e.Step = 2; e.Decides = true; e.Asked = m.Groups[1].Value;
                e.Label = "Art. 154.3 · " + Possessive(m.Groups[1].Value) + " candidate elected"; e.Line = m.Groups[3].Value + " for · " + Need(m.Groups[4].Value) + " needed";
                e.Article = "Art. 154.3 · " + Possessive(m.Groups[1].Value) + " candidate"; e.Result = m.Groups[3].Value + " for · " + Need(m.Groups[4].Value) + " needed";
                e.Slip.Add(Cabinet(m.Groups[2].Value) + " · " + m.Groups[3].Value + " of " + m.Groups[4].Value + " for");
                e.Slip.Add("an absolute majority · Art. 154.3");
                return e;
            }
            if (text.StartsWith("concluded - ", StringComparison.Ordinal))
            {
                if (text.IndexOf("Art. 155 ust. 2", StringComparison.Ordinal) < 0) { return null; }   // a carried path's close repeats its install
                m = Regex.Match(text, @"elections ordered for (\d{4}-\d{2}-\d{2})");
                if (m.Success && DateTime.TryParseExact(m.Groups[1].Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime polling)) { e.PollingDay = polling; }
                e.Step = 4; e.Label = "Art. 155 · Term shortened";
                e.Line = e.PollingDay != DateTime.MinValue ? "snap election · " + e.PollingDay.ToString("d MMM yyyy", CultureInfo.InvariantCulture) : "snap election ordered";
                e.Slip.Add("the President shortens the term · Art. 155.2");
                if (e.PollingDay != DateTime.MinValue) { e.Slip.Add("polling day " + e.PollingDay.ToString("ddd d MMM yyyy", CultureInfo.InvariantCulture) + " · Art. 98.5"); }
                e.Slip.Add("the refused cabinet serves on as caretaker");
                return e;
            }
            return Generic(e, text);
        }

        /// <summary>A line not recognised: listed as the Sejm's step on its day, its own words in the slip - never dropped, never guessed.</summary>
        private static Entry Generic(Entry e, string text)
        {
            e.Step = -1; e.Label = "The Sejm's steps"; e.Line = null;
            var line = new System.Text.StringBuilder();
            foreach (string word in text.Split(' '))
            {
                if (line.Length > 0 && line.Length + 1 + word.Length > 56) { e.Slip.Add(line.ToString()); line.Clear(); }
                if (line.Length > 0) { line.Append(' '); }
                line.Append(word);
            }
            if (line.Length > 0) { e.Slip.Add(line.ToString()); }
            return e;
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // THE ANNOUNCEMENT (11e)
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>One of the plate's three struck steps: its name, article and party, its result (BAD) and its day.</summary>
        public sealed class PlateStep
        {
            public string Name, Article, Result;
            public string Asked;
            public DateTime Day = DateTime.MinValue;
        }

        /// <summary>What the announcement plate shows: the day the term was shortened, the three steps, the polling day, the caretaker - from the engine's record
        /// (<see cref="PlateOf(Country, SejmFormation, DateTime)"/>) or, for a staged film, from the night's steps (<see cref="PlateOf(SejmNightSteps)"/>).</summary>
        public sealed class Plate
        {
            public DateTime OrderedOn, PollingDay;
            public string Caretaker;
            public readonly PlateStep[] Steps = { new PlateStep { Name = "The President's cabinet" }, new PlateStep { Name = "The Sejm's own" }, new PlateStep { Name = "The President's second" } };
            /// <summary>STAGED: built from a projection for a film, not from the engine's record.</summary>
            public bool Staged;
        }

        /// <summary>The plate from the engine's record: each step's deciding entry (its day the log's), the order's day and the polling day - <paramref name="pollingDay"/>
        /// the manager's own (`SimulationManager.ExtraElectionDate`), else the record's.</summary>
        public static Plate PlateOf(Country country, SejmFormation f, DateTime pollingDay)
        {
            if (!TryShortened(f, out DateTime ordered, out DateTime recorded)) { return null; }
            var plate = new Plate { OrderedOn = ordered, PollingDay = pollingDay != DateTime.MinValue ? pollingDay : recorded, Caretaker = country?.Government?.PmParty };
            foreach (Entry e in Entries(f))
            {
                if (!e.Decides || e.Step < 1 || e.Step > 3) { continue; }
                PlateStep step = plate.Steps[e.Step - 1];
                step.Article = e.Article; step.Result = e.Result; step.Asked = e.Asked; step.Day = e.Day;
            }
            return plate;
        }

        /// <summary>STAGED: the plate the night's steps give where none is expected - each step's party and count as <see cref="SejmNightSteps"/> read them, dated on
        /// the steps' own days (DERIVED, K304: each on its window's last day). Null where a government is expected or the day is not known.</summary>
        public static Plate PlateOf(SejmNightSteps steps)
        {
            if (steps == null || steps.Expected || steps.SnapPollingDay == DateTime.MinValue) { return null; }
            var plate = new Plate { OrderedOn = steps.ShortenedOn, PollingDay = steps.SnapPollingDay, Caretaker = steps.CaretakerParty, Staged = true };
            bool designee = steps.Steps[0].Voted;
            DateTime sejmVoted = steps.OfRecord && SejmFormation.TryOfRecord(CountryId.Poland, steps.PollingDay, out SejmFormation.StepsOfRecord record) ? record.SejmElected
                : steps.FirstSitting.AddDays((designee ? 2 : 1) * SejmFormation.StepWindowDays);
            for (int i = 0; i < 3; i++)
            {
                SejmNightSteps.Step st = steps.Steps[i];
                PlateStep p = plate.Steps[i];
                p.Asked = st.Voted ? st.Asked : null;
                p.Day = i == 0 ? (designee ? sejmVoted : steps.FirstSitting.AddDays(SejmFormation.StepWindowDays)) : i == 1 ? sejmVoted : steps.ShortenedOn;
                if (i == 0) { p.Article = st.Voted ? "Art. 154.1–2 · " + Short(st.Asked) + " asked" : "Art. 154.1"; p.Result = st.Voted ? st.For.ToString(CultureInfo.InvariantCulture) + " for · " + SejmNightSteps.Needed(st.Members).ToString(CultureInfo.InvariantCulture) + " needed" : "no designee"; }
                else if (i == 1) { p.Article = st.Voted ? "Art. 154.3 · " + Possessive(st.Asked) + " candidate" : "Art. 154.3"; p.Result = st.Voted ? st.For.ToString(CultureInfo.InvariantCulture) + " for · " + SejmNightSteps.Needed(st.Members).ToString(CultureInfo.InvariantCulture) + " needed" : "no prime minister"; }
                else { p.Article = st.Voted ? "Art. 155.1 · " + Short(st.Asked) + " asked" : "Art. 155.1"; p.Result = st.Voted ? st.For.ToString(CultureInfo.InvariantCulture) + " for · " + st.Against.ToString(CultureInfo.InvariantCulture) + " against" : "no cabinet appointed"; }
            }
            return plate;
        }

        /// <summary>
        /// The snap election's campaign as the manager calendars it (`SimulationManager.ChamberCampaignCalendar`, PS-3i §636): no run-up, and the campaign proper
        /// whole weeks back from the polling day, at most <see cref="CampaignCalendar.DefaultCampaignWeeks"/>, from the order. ⚠ The manager's rule is private, so it is
        /// read again here from its public parts; the plate prints the day it gives.
        /// </summary>
        public static CampaignCalendar SnapCampaign(DateTime orderedOn, DateTime pollingDay)
        {
            int weeks = Math.Max(0, Math.Min(CampaignCalendar.DefaultCampaignWeeks, (int)((pollingDay - orderedOn).TotalDays / 7)));
            return new CampaignCalendar(pollingDay, weeks, 0);
        }
    }
}
