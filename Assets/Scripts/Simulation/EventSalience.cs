using System;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>The event classes whose salience jump and decay are fitted (`Tools/aloop_salience_fit.pl`), each named for the issue it moves.</summary>
    public enum SalienceClass { Economy, Health, Migration, Security, Crime, Natural }

    /// <summary>
    /// A-LOOP q5 (2026-10-19, `docs/specs/A_LOOP_PLAN.md` §6, Elias's ruling J1): <b>events move salience</b>.
    /// <list type="bullet">
    /// <item><b>The vector.</b> A country's salience is the sourced wave its readers already hold - `StanceModel.SalienceOf` (Eurobarometer 105, the
    /// floor where the survey's top five do not name an issue) for the stance model, the campaign's own wave (`LiveCampaignSetup`) for the
    /// campaign - plus the country's stored displacement, `Country.SalienceShift`: what the events still hold above the seed, per issue, null while
    /// nothing does. Held within the survey's own scale: a reader clamps seed + shift to [0, 1] (Eurobarometer's two-mention shares sum to about 2,
    /// so no issue's share is bounded by another's).</item>
    /// <item><b>An event of a tag moves the issue its tag names</b> (<see cref="TryClassOf"/>, DECLARED) by its class's FITTED jump - the rise the
    /// survey measured at the excess's PEAK (K69, Elias 2026-10-23: the post-wave or a later wave, `Generated.EventSalienceFit`) - times the event's
    /// SEVERITY (K68, `SeverityOf`), on the day it lands (`EventSystem.ApplyEvent`), and only an ADVERSE event does (<see cref="Moves"/>).</item>
    /// <item><b>The jump is HELD flat</b> for its class's hold - the days from the events to the peak's wave, the survey's date for the level it
    /// measured (`Country.SalienceHoldDays`) - <b>then decays back to the seed</b> geometrically, daily, at the class's fitted rate (<see cref="AdvanceDay"/>,
    /// first in the day). So, for one event, the game reads at the survey's date the level the survey measured, and the fitted curve after it - never a
    /// level the series did not show (review 882b's M-A; the back-cast to the event's day is reported by the tool, never landed). Two events in
    /// consecutive years stack: the second lands on the first's leftover and the restarted hold freezes that leftover too, so the second reads about
    /// 1.35 jumps where each decaying on its own clock would read 1.20 (an overstatement of at most ~0.016 of share; with an Economy event every year
    /// the level settles near 1.54 jumps) - DECLARED (the s882c review's M-1).</item>
    /// <item><b>The readers:</b> the stance model's opinion cost (<see cref="Of"/>) and the campaign's relevance (`CampaignRun.Setup.TrueSalience`,
    /// moved by <see cref="ApplyToCampaign"/> from the shift the campaign's record took on its window's first day, so a load replays the same campaign).</item>
    /// </list>
    /// <para>⚠ <b>Declared readings</b> (the plan leaves them open): the tag map (<see cref="TryClassOf"/>) - a political event names no issue in the
    /// survey's set and moves none; an event moves its issue only when it is adverse (it costs output or costs approval), since the
    /// question asks for the country's PROBLEMS and a boom names none; a second event of a class during the hold, no smaller than what stands (K95), adds its jump and restarts the
    /// hold; the campaign reads its window's first day and an event landing inside a running window does not reach it; an issue the campaign's
    /// wave does not measure (NaN) stays out of the campaign however far events move it, and moves the stance model's floor instead.</para>
    /// <para>⚠ <b>Where the peak is later than the first wave</b> (K69; Health's peak is the second wave after the pandemic, Natural's the second after
    /// Sweden's summer of 2018) the event RAMPS (K94, Elias 2026-10-24): it lands the first wave's measured level (`FirstLevelOf`) held flat to the
    /// first wave's date (`FirstHoldDaysOf`), then rises LINEARLY to the peak's level by the peak's date (`RampDaysOf`, a step a day stored in
    /// `Country.SalienceRampPerDay`), then decays as fitted - DECLARED: linear between the two measured waves, the series holding nothing between them.
    /// Where the peak is the first wave there is no ramp and the hold is §882's.</para>
    /// <para>⚠ <b>A minor event never extends a bigger standing hold</b> (K95, Elias 2026-10-24): an event lands, and restarts its issue's hold, only if
    /// its jump is at least what stands on the issue that day after the morning's decay - while a ramp runs, the hold's peak (the level plus the ramp's unfinished rest, review L-2); a smaller one lands nothing and leaves the hold as it is.
    /// Where it lands it adds to what stands (§882's stacking, DECLARED unchanged) and its ramp replaces any unfinished ramp on the issue.</para>
    /// <para>⚠ <b>The USA's salience moves too, at scale one</b> (K69, then K103, Elias 2026-10-24): its seeds are Gallup's "most important problem"
    /// (open-ended; a respondent may name several, its table's Total row above 100 %), the jump Eurobarometer's two-answer QA3 share, and the jump lands on
    /// the USA as measured (`InstrumentScale` is one for every country). K69 first scaled it by Gallup's mentions a respondent over Eurobarometer's
    /// (`Generated.EventSalienceFit.UsConversion`); tested out of sample under K102 by a criterion declared before the figures
    /// (`Tools/aloop_salience_fit.pl`'s header), that conversion FAILED - Gallup's base is not stable across years and a surging dominant issue's share
    /// ratio runs near 1.7, not 0.71 - and K103 dropped it. The bases, the test's figures and its verdict stay generated (`Generated.EventSalienceFit`)
    /// and re-derived by `SalienceFitCheck` as the record of why.</para>
    /// <para>⚠ <b>Crime and Security stay inert</b> (K70, declared): the fit resolves neither (Crime's excess gone before the next wave, Security's
    /// jump's interval spanning zero), their jumps are zero, and their events move no salience.</para>
    /// <para>⚠ <b>Where the shift reaches the campaign, and where it cancels</b> (review 882's M-3, under q24's persuasion): a contact's effect is the
    /// sourced CACE times its message's relevance over the electorate's mean relevance (`CampaignPersuasion.ElectorateMean`). A GENERAL message's
    /// relevance is the mean salience over the contested issues - the same issues for every party - so a shift scales every party's general
    /// relevance and the norm by one common factor and cancels (but for the clamps): events do not make a general campaign stronger or weaker.
    /// The shift REACHES the campaign through the relative terms: an ISSUE message on the raised issue gains against the norm and one on another
    /// issue loses; each party's ground (the issue it leads with, salience times its match) and the electorate's top issue (the strategy's
    /// "prioritised" message) can change; and a commissioned issue poll measures the moved salience.</para>
    /// <para>⚠ <b>The jump scales by the event's SEVERITY</b> (K68, Elias 2026-10-23, answering review 882's M-6): the fit's own event keeps the full
    /// fitted figure - the 2022 shock ("War on the Doorstep") lands `EventSalienceFit.EconomyJump` and holds it for the class's hold - and a smaller
    /// event of the class lands proportionally less. The severity is read off what the pool already holds (`SeverityOf`): the event's adverse size
    /// over its class's anchor's, on the anchor's own adverse measure - the GDP shock, the measure the pool's magnitude bands are cut on
    /// (`EventBands`), or the approval effect where the anchor costs no output (Migration's 2015 surge raised spending) - and never above one
    /// (`SeverityCeiling`). The hold and the decay stay the class's: the survey's timing, not the event's size.</para>
    /// </summary>
    public static class EventSalience
    {
        /// <summary>CONVENTION - an epsilon, not a claim about the world: once every issue's shift is below this (share units) the vector is back at
        /// its seed and the stored shift is dropped, so a faded jump stops writing the country rather than shrinking through the denormals.</summary>
        private const float SettledBelow = 1e-6f;

        /// <summary>CONVENTION - a ceiling (K68's reading): an event larger than its class's anchor lands the fitted jump and no more - the series measured
        /// no larger event of the class, so a figure above it would be an extrapolation the fit does not carry (DECLARED; the alternative, an uncapped
        /// ratio, is named in the record).</summary>
        private const float SeverityCeiling = 1f;

        /// <summary>DECLARED (K68): the pool events a class's severity is measured against - each the pool's own entry for the fit's dated event
        /// (`Tools/aloop_salience_fit.pl`'s design): Economy "War on the Doorstep" (Russia's invasion of 2022, the price shock the fit brackets); Health
        /// "Public Health Emergency" (COVID-19, the pandemic of 2020); Migration "Migration Surge" (2015); Natural "Severe Drought Hits Agricultural
        /// Output" and "Wildfires" (both Sweden's summer of 2018, the fit's one unit - its drought and its fires; the smaller of the two is the reference,
        /// so both land the full figure); Security "Terror Attack" (the attacks of 2016-17); Crime "Gang Violence Wave" (Sweden's autumn of 2023). A class
        /// whose anchors are not in the pool moves nothing (`EventSalienceDiagnostic` fails on it).</summary>
        public static string[] AnchorsOf(SalienceClass salienceClass)
        {
            switch (salienceClass)
            {
                case SalienceClass.Health: return new[] { "Public Health Emergency" };
                case SalienceClass.Migration: return new[] { "Migration Surge" };
                case SalienceClass.Security: return new[] { "Terror Attack" };
                case SalienceClass.Crime: return new[] { "Gang Violence Wave" };
                case SalienceClass.Natural: return new[] { "Severe Drought Hits Agricultural Output", "Wildfires" };
                default: return new[] { "War on the Doorstep" };
            }
        }

        /// <summary>DECLARED (K68): whether a class's severity is read on the GDP shock (true: some anchor of it costs output) or on the approval effect
        /// (false: Migration's anchor raises output and costs approval).</summary>
        public static bool SeverityOnOutput(SalienceClass salienceClass)
        {
            foreach (string name in AnchorsOf(salienceClass)) { EconomicEvent a = EventSystem.PoolEvent(name); if (a != null && a.GdpShockPercent < 0f) { return true; } }
            return false;
        }

        /// <summary>An event's adverse size on a measure: the GDP shock's fall (percent of GDP) or the approval effect's fall (points), zero where it rises.</summary>
        private static float AdverseSize(EconomicEvent economicEvent, bool onOutput)
            => onOutput ? Mathf.Max(0f, -economicEvent.GdpShockPercent) : Mathf.Max(0f, -economicEvent.ApprovalEffect);

        /// <summary>K68: the event's SEVERITY, 0..1 - its adverse size over the smallest adverse size among its class's anchors (`AnchorsOf`), on the
        /// class's measure (`SeverityOnOutput`), held at most `SeverityCeiling`. An anchor reads exactly 1. Zero for an event that moves no salience
        /// (favourable, political, or without an anchor in the pool to scale against).</summary>
        public static float SeverityOf(EconomicEvent economicEvent)
        {
            if (!Moves(economicEvent) || !TryClassOf(economicEvent.Tag, out SalienceClass salienceClass)) { return 0f; }
            bool onOutput = SeverityOnOutput(salienceClass);
            float reference = float.PositiveInfinity;
            foreach (string name in AnchorsOf(salienceClass))
            {
                EconomicEvent a = EventSystem.PoolEvent(name);
                if (a == null) { continue; }
                float size = AdverseSize(a, onOutput);
                if (size > 0f && size < reference) { reference = size; }
            }
            if (float.IsPositiveInfinity(reference)) { return 0f; }
            return Mathf.Min(SeverityCeiling, AdverseSize(economicEvent, onOutput) / reference);
        }

        /// <summary>The jump an event lands, in Eurobarometer's share units, before the instrument's scale: its severity times its class's fitted jump
        /// (zero for an inert class or an event that moves nothing).</summary>
        public static float JumpFor(EconomicEvent economicEvent)
            => TryClassOf(economicEvent.Tag, out SalienceClass salienceClass) ? SeverityOf(economicEvent) * JumpOf(salienceClass) : 0f;

        /// <summary>K103 (Elias, 2026-10-24): the scale from the fit's instrument to the country's seed - one for every country, the USA included: the
        /// Eurobarometer jump lands as measured. K69's Gallup-over-Eurobarometer conversion (`Generated.EventSalienceFit.UsConversion`) failed its
        /// out-of-sample test (K102) and is not applied; it stays generated as the record.</summary>
        public static float InstrumentScale(CountryId country) => 1f;

        /// <summary>DECLARED (`docs/specs/A_LOOP_PLAN.md` §6 (2): "stated per tag"): the class, and so the issue, an event of each tag moves. Every
        /// economic tag names the Economy slot (rising prices and the economic situation share it, StanceModel's convention); War too (review 882b's
        /// M-C): the war in the pool is the Economy fit's own anchor event - Russia's invasion of 2022 - and its measured salience effect IS the
        /// Economy class's; Health the Healthcare slot; Migration Immigration; Security Defense (EB105's "security and defence"); Crime Crime;
        /// Natural Climate. Political names none: a scandal, a protest or a dispute has no slot in the survey's set (W-F3's "threats to democracy"
        /// note), and false is returned.</summary>
        public static bool TryClassOf(EventTag tag, out SalienceClass salienceClass)
        {
            switch (tag)
            {
                case EventTag.External: case EventTag.Domestic: case EventTag.Commodity: case EventTag.Financial:
                case EventTag.Technology: case EventTag.Trade: case EventTag.Labour: case EventTag.War:
                    salienceClass = SalienceClass.Economy; return true;
                case EventTag.Health: salienceClass = SalienceClass.Health; return true;
                case EventTag.Migration: salienceClass = SalienceClass.Migration; return true;
                case EventTag.Security: salienceClass = SalienceClass.Security; return true;
                case EventTag.Crime: salienceClass = SalienceClass.Crime; return true;
                case EventTag.Natural: salienceClass = SalienceClass.Natural; return true;
                default: salienceClass = SalienceClass.Economy; return false;
            }
        }

        /// <summary>The issue slot a class moves - the one the fit's QA3 item maps to.</summary>
        public static IssueId IssueOf(SalienceClass salienceClass)
        {
            switch (salienceClass)
            {
                case SalienceClass.Health: return IssueId.Healthcare;
                case SalienceClass.Migration: return IssueId.Immigration;
                case SalienceClass.Security: return IssueId.Defense;
                case SalienceClass.Crime: return IssueId.Crime;
                case SalienceClass.Natural: return IssueId.Climate;
                default: return IssueId.Economy;
            }
        }

        /// <summary>The class's FITTED jump in share units, the survey's - zero for a class the fit could not resolve (inert; `Generated.EventSalienceFit`).</summary>
        public static float JumpOf(SalienceClass salienceClass)
        {
            switch (salienceClass)
            {
                case SalienceClass.Health: return (float)EventSalienceFit.HealthJump;
                case SalienceClass.Migration: return (float)EventSalienceFit.MigrationJump;
                case SalienceClass.Security: return (float)EventSalienceFit.SecurityJump;
                case SalienceClass.Crime: return (float)EventSalienceFit.CrimeJump;
                case SalienceClass.Natural: return (float)EventSalienceFit.NaturalJump;
                default: return (float)EventSalienceFit.EconomyJump;
            }
        }

        /// <summary>K94: the level the class lands on the event's day, in share units - the survey's measured rise at its FIRST wave after the event
        /// (FITTED, `Generated.EventSalienceFit`), which equals the jump where the peak is that wave; zero for an inert class.</summary>
        public static float FirstLevelOf(SalienceClass salienceClass)
        {
            switch (salienceClass)
            {
                case SalienceClass.Health: return EventSalienceFit.HealthGenerated ? (float)EventSalienceFit.HealthFirstWaveJump : 0f;
                case SalienceClass.Migration: return EventSalienceFit.MigrationGenerated ? (float)EventSalienceFit.MigrationFirstWaveJump : 0f;
                case SalienceClass.Security: return EventSalienceFit.SecurityGenerated ? (float)EventSalienceFit.SecurityFirstWaveJump : 0f;
                case SalienceClass.Crime: return EventSalienceFit.CrimeGenerated ? (float)EventSalienceFit.CrimeFirstWaveJump : 0f;
                case SalienceClass.Natural: return EventSalienceFit.NaturalGenerated ? (float)EventSalienceFit.NaturalFirstWaveJump : 0f;
                default: return EventSalienceFit.EconomyGenerated ? (float)EventSalienceFit.EconomyFirstWaveJump : 0f;
            }
        }

        /// <summary>K94: the days the first wave's level is held flat from the event (DERIVED by the fit: the units' mean days from the event to the
        /// first wave after it) - the whole hold where the peak is that wave; zero for an inert class.</summary>
        public static int FirstHoldDaysOf(SalienceClass salienceClass)
        {
            switch (salienceClass)
            {
                case SalienceClass.Health: return EventSalienceFit.HealthFirstHoldDays;
                case SalienceClass.Migration: return EventSalienceFit.MigrationFirstHoldDays;
                case SalienceClass.Security: return EventSalienceFit.SecurityFirstHoldDays;
                case SalienceClass.Crime: return EventSalienceFit.CrimeFirstHoldDays;
                case SalienceClass.Natural: return EventSalienceFit.NaturalFirstHoldDays;
                default: return EventSalienceFit.EconomyFirstHoldDays;
            }
        }

        /// <summary>K94: the days of the LINEAR ramp from the first wave's level to the peak's (DERIVED by the fit: the hold less the first hold, the
        /// measured interval between the two waves) - zero where the peak is the first wave or the class is inert.</summary>
        public static int RampDaysOf(SalienceClass salienceClass)
        {
            switch (salienceClass)
            {
                case SalienceClass.Health: return EventSalienceFit.HealthRampDays;
                case SalienceClass.Migration: return EventSalienceFit.MigrationRampDays;
                case SalienceClass.Security: return EventSalienceFit.SecurityRampDays;
                case SalienceClass.Crime: return EventSalienceFit.CrimeRampDays;
                case SalienceClass.Natural: return EventSalienceFit.NaturalRampDays;
                default: return EventSalienceFit.EconomyRampDays;
            }
        }

        /// <summary>The class's FITTED decay rate, a day - zero for an inert class (its shift never jumps, so it never decays).</summary>
        public static double LambdaOf(SalienceClass salienceClass)
        {
            switch (salienceClass)
            {
                case SalienceClass.Health: return EventSalienceFit.HealthLambdaPerDay;
                case SalienceClass.Migration: return EventSalienceFit.MigrationLambdaPerDay;
                case SalienceClass.Security: return EventSalienceFit.SecurityLambdaPerDay;
                case SalienceClass.Crime: return EventSalienceFit.CrimeLambdaPerDay;
                case SalienceClass.Natural: return EventSalienceFit.NaturalLambdaPerDay;
                default: return EventSalienceFit.EconomyLambdaPerDay;
            }
        }

        /// <summary>The class's hold in days (DERIVED by the fit: the units' mean days from the event to the wave of the excess's peak, K69) - zero for an
        /// inert class.</summary>
        public static int HoldDaysOf(SalienceClass salienceClass)
        {
            switch (salienceClass)
            {
                case SalienceClass.Health: return EventSalienceFit.HealthHoldDays;
                case SalienceClass.Migration: return EventSalienceFit.MigrationHoldDays;
                case SalienceClass.Security: return EventSalienceFit.SecurityHoldDays;
                case SalienceClass.Crime: return EventSalienceFit.CrimeHoldDays;
                case SalienceClass.Natural: return EventSalienceFit.NaturalHoldDays;
                default: return EventSalienceFit.EconomyHoldDays;
            }
        }

        /// <summary>DERIVED: the share of a class's shift standing one day later once its hold is over, exp(−λ) - one game day is one calendar day (`SimulationManager.DaysPerTurn`).</summary>
        public static float DailyPersistence(SalienceClass salienceClass) => (float)Math.Exp(-LambdaOf(salienceClass));

        /// <summary>DERIVED: the half-life the fitted rate declares, in days after the hold (ln 2 ⁄ λ); infinity for an inert class.</summary>
        public static double HalfLifeDays(SalienceClass salienceClass) { double l = LambdaOf(salienceClass); return l > 0.0 ? Math.Log(2.0) / l : double.PositiveInfinity; }

        /// <summary>The class an issue's shift decays at - the class whose jump lands on it (each issue is one class's).</summary>
        public static bool TryClassOfIssue(IssueId issue, out SalienceClass salienceClass)
        {
            foreach (SalienceClass c in (SalienceClass[])Enum.GetValues(typeof(SalienceClass)))
            {
                if (IssueOf(c) == issue) { salienceClass = c; return true; }
            }
            salienceClass = SalienceClass.Economy;
            return false;
        }

        /// <summary>DECLARED: an event moves its issue only when it is ADVERSE - it costs output or costs approval. QA3 asks for the country's most
        /// important ISSUES (its problems); a boom, a breakthrough or a bumper harvest raises none. A price shock alone does not make an event
        /// adverse (review 882's H-1: four booms carry a small inflation shock); every price shock in the pool also costs output or approval.</summary>
        public static bool Moves(EconomicEvent economicEvent)
            => economicEvent != null && (economicEvent.GdpShockPercent < 0f || economicEvent.ApprovalEffect < 0f);

        /// <summary>The event's jump on the country's salience, on the day it lands: its severity times its class's fitted jump, times the country's
        /// instrument scale (K68, K69) - the event's PEAK level. Where the class ramps (K94) the event lands its first wave's level (the same severity
        /// and scale) and the day's ramp step to the peak is stored (`Country.SalienceRampPerDay`); otherwise it lands the jump. K95: it lands, and
        /// restarts the issue's hold, only if its jump is at least what already stands on the issue (the shift that day, after the morning's decay, plus a running ramp's unfinished rest - review L-2);
        /// a smaller event lands nothing and leaves the standing hold as it is (logged `SALIENCE-BELOW:`). Held so the seed + shift stays in [0, 1].
        /// Logged (`SALIENCE:`) where it moves anything (an issue already at the top of the scale moves nothing). Nothing for a null event, a political
        /// or a favourable one, or an inert class - and then the country is untouched to the bit.</summary>
        public static void Land(Country country, EconomicEvent economicEvent)
        {
            if (country == null || !Moves(economicEvent) || !TryClassOf(economicEvent.Tag, out SalienceClass salienceClass)) { return; }
            float severity = SeverityOf(economicEvent);
            float scale = InstrumentScale(country.Id);
            float jump = severity * JumpOf(salienceClass) * scale;
            if (!(jump > 0f)) { return; }
            IssueId issue = IssueOf(salienceClass);
            float standingNow = country.SalienceShift != null && (int)issue < country.SalienceShift.Length ? country.SalienceShift[(int)issue] : 0f;
            // review L-2: while a ramp runs, what stands is the hold's PEAK - the level now plus the ramp's unfinished rest - not the flat phase's level.
            // A ramp counts only under a standing shift (n910b): with nothing standing on the issue a stored step is a leftover, never a standing peak
            if (standingNow != 0f && country.SalienceRampPerDay != null && (int)issue < country.SalienceRampPerDay.Length && country.SalienceRampPerDay[(int)issue] != 0f
                && country.SalienceHoldDays != null && (int)issue < country.SalienceHoldDays.Length)
            {
                standingNow += country.SalienceRampPerDay[(int)issue] * Math.Min(country.SalienceHoldDays[(int)issue], RampDaysOf(salienceClass));
            }
            if (jump < standingNow)   // K95: below what stands - a minor event never extends a bigger standing hold, and adds nothing
            {
                Debug.Log(string.Format(CultureInfo.InvariantCulture, "SALIENCE-BELOW: {0} - {1} ({2}, class {3}): its jump {4:0.0000} is below the {5:0.0000} standing on {6}; nothing lands and the hold ({7} days left) stands",
                    country.Id, economicEvent.Name, economicEvent.Tag, salienceClass, jump, standingNow, issue,
                    country.SalienceHoldDays != null && (int)issue < country.SalienceHoldDays.Length ? country.SalienceHoldDays[(int)issue] : 0));
                return;
            }
            int rampDays = RampDaysOf(salienceClass);
            float landed = rampDays > 0 ? severity * FirstLevelOf(salienceClass) * scale : jump;   // K94: the first wave's level, ramping to the jump
            float seed = StanceModel.SalienceOf(country.Id, issue, out _);
            if (country.SalienceShift == null) { country.SalienceShift = new float[IssueVector.IssueCount]; }
            else if (country.SalienceShift.Length < IssueVector.IssueCount) { Array.Resize(ref country.SalienceShift, IssueVector.IssueCount); }   // a shorter vector keeps every issue's shift
            if (country.SalienceHoldDays == null) { country.SalienceHoldDays = new int[IssueVector.IssueCount]; }
            else if (country.SalienceHoldDays.Length < IssueVector.IssueCount) { Array.Resize(ref country.SalienceHoldDays, IssueVector.IssueCount); }
            float before = country.SalienceShift[(int)issue];
            country.SalienceShift[(int)issue] = Mathf.Clamp(before + landed, -seed, 1f - seed);
            if (country.SalienceShift[(int)issue] == before) { return; }   // already at the scale's top: nothing moved, nothing logged
            country.SalienceHoldDays[(int)issue] = HoldDaysOf(salienceClass);
            if (rampDays > 0)   // K94: the ramp step; an older ramp's unfinished rest on the issue is replaced by this event's (DECLARED)
            {
                if (country.SalienceRampPerDay == null) { country.SalienceRampPerDay = new float[IssueVector.IssueCount]; }
                else if (country.SalienceRampPerDay.Length < IssueVector.IssueCount) { Array.Resize(ref country.SalienceRampPerDay, IssueVector.IssueCount); }
                country.SalienceRampPerDay[(int)issue] = (jump - landed) / rampDays;
            }
            else if (country.SalienceRampPerDay != null && (int)issue < country.SalienceRampPerDay.Length) { country.SalienceRampPerDay[(int)issue] = 0f; }
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "SALIENCE: {0} - {1} ({2}, class {3}) raises {4} by {5:0.0000}: {6:0.0000} -> {7:0.0000} (seed {8:0.00}, severity {11:0.000}, instrument scale {12:0.0000}, held {9} days, then half-life {10:0.0} days)",
                country.Id, economicEvent.Name, economicEvent.Tag, salienceClass, issue, country.SalienceShift[(int)issue] - before, seed + before,
                seed + country.SalienceShift[(int)issue], seed, HoldDaysOf(salienceClass), HalfLifeDays(salienceClass), severity, scale));
        }

        /// <summary>The day, first in the day (`SimulationManager.AdvanceDay`): an issue still in its hold counts a day off it and keeps its shift; one
        /// past it decays by its class's daily persistence; the stored shift (and its holds) dropped once every issue is back at its seed. Nothing while
        /// no event holds anything (null). DECLARED (review 882's L-2): a shift standing on an issue whose class the fit leaves inert (no rate to decay
        /// at - reachable only through a refit under a standing shift) is dropped that day, never held for ever. K94: in the last `RampDaysOf` days of
        /// its hold an issue with a ramp step rises by it each day (held under 1 − seed), so the first wave's level reaches the peak's on the hold's
        /// last day; the first `FirstHoldDaysOf` days it stands flat.</summary>
        public static void AdvanceDay(Country country)
        {
            float[] shift = country?.SalienceShift;
            if (shift == null) { return; }
            int[] hold = country.SalienceHoldDays;
            float[] ramp = country.SalienceRampPerDay;
            bool standing = false;
            for (int i = 0; i < shift.Length; i++)
            {
                if (shift[i] == 0f) { continue; }
                bool live = TryClassOfIssue((IssueId)i, out SalienceClass c) && LambdaOf(c) > 0.0;
                if (live && hold != null && i < hold.Length && hold[i] > 0)
                {
                    if (ramp != null && i < ramp.Length && ramp[i] != 0f && hold[i] <= RampDaysOf(c))   // K94: the linear ramp to the peak
                    {
                        shift[i] = Mathf.Min(shift[i] + ramp[i], 1f - StanceModel.SalienceOf(country.Id, (IssueId)i, out _));
                    }
                    hold[i]--;
                    if (hold[i] == 0 && ramp != null && i < ramp.Length) { ramp[i] = 0f; }
                    standing = true; continue;
                }
                shift[i] *= live ? DailyPersistence(c) : 0f;
                if (Mathf.Abs(shift[i]) < SettledBelow) { shift[i] = 0f; } else { standing = true; }
            }
            if (!standing) { country.SalienceShift = null; country.SalienceHoldDays = null; country.SalienceRampPerDay = null; }
        }

        /// <summary>The stance model's reading: the seed (`StanceModel.SalienceOf`) plus the country's shift, clamped to [0, 1];
        /// <paramref name="moved"/> true where events hold the issue off its seed. With no shift it is the seed itself, to the bit.</summary>
        public static float Of(Country country, IssueId issue, out bool measured, out bool moved)
        {
            float seed = StanceModel.SalienceOf(country.Id, issue, out measured);
            float shift = country.SalienceShift != null && (int)issue < country.SalienceShift.Length ? country.SalienceShift[(int)issue] : 0f;
            moved = shift != 0f;
            return moved ? Mathf.Clamp01(seed + shift) : seed;
        }

        /// <summary>The issue's salience as the country's stored vector holds it (the stance model's seed plus the shift).</summary>
        public static float Of(Country country, IssueId issue) => Of(country, issue, out _, out _);

        /// <summary>The shift as it stands, a copy - what a campaign's record keeps from its window's first day (null while nothing stands).</summary>
        public static float[] Snapshot(Country country) => country?.SalienceShift == null ? null : (float[])country.SalienceShift.Clone();

        /// <summary>The campaign's reading: each issue its own wave measures (not NaN) moved by <paramref name="shift"/> (a record's
        /// <see cref="Snapshot"/>), clamped to [0, 1], in place. An issue the campaign's wave does not measure stays out of the race. Nothing for a
        /// null shift, so a campaign no event touched is staged exactly as before.</summary>
        public static void ApplyToCampaign(float[] shift, double[] trueSalience)
        {
            if (shift == null || trueSalience == null) { return; }
            for (int i = 0; i < trueSalience.Length && i < shift.Length; i++)
            {
                if (double.IsNaN(trueSalience[i]) || shift[i] == 0f) { continue; }
                trueSalience[i] = Math.Min(1.0, Math.Max(0.0, trueSalience[i] + shift[i]));
            }
        }
    }
}
