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
    /// survey measured at its first wave after the event (`Generated.EventSalienceFit`) - on the day it lands (`EventSystem.ApplyEvent`), and only an
    /// ADVERSE event does (<see cref="Moves"/>).</item>
    /// <item><b>The jump is HELD flat</b> for its class's hold - the days the survey took to measure it, from the events to their first wave after
    /// (`Country.SalienceHoldDays`) - <b>then decays back to the seed</b> geometrically, daily, at the class's fitted rate (<see cref="AdvanceDay"/>,
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
    /// question asks for the country's PROBLEMS and a boom names none; a second event of a class during the hold adds its jump and restarts the
    /// hold; the campaign reads its window's first day and an event landing inside a running window does not reach it; an issue the campaign's
    /// wave does not measure (NaN) stays out of the campaign however far events move it, and moves the stance model's floor instead.</para>
    /// <para>⚠ <b>The USA moves no salience</b> (review 882b's M-D, an ask): its seeds are Gallup's single-mention "most important problem", not
    /// Eurobarometer's two-mention shares, and no Gallup analogue is fitted - a two-mention jump on a one-mention seed would be a transfer with no
    /// coefficient behind it, so <see cref="Land"/> leaves the USA at its seed (<see cref="MovesSalience"/>).</para>
    /// <para>⚠ <b>Where the shift reaches the campaign, and where it cancels</b> (review 882's M-3, under q24's persuasion): a contact's effect is the
    /// sourced CACE times its message's relevance over the electorate's mean relevance (`CampaignPersuasion.ElectorateMean`). A GENERAL message's
    /// relevance is the mean salience over the contested issues - the same issues for every party - so a shift scales every party's general
    /// relevance and the norm by one common factor and cancels (but for the clamps): events do not make a general campaign stronger or weaker.
    /// The shift REACHES the campaign through the relative terms: an ISSUE message on the raised issue gains against the norm and one on another
    /// issue loses; each party's ground (the issue it leads with, salience times its match) and the electorate's top issue (the strategy's
    /// "prioritised" message) can change; and a commissioned issue poll measures the moved salience.</para>
    /// <para>⚠ <b>One jump per class, whatever the event's band</b> (review 882's M-6 and 882b's M-B, an ask for Elias): every adverse Economy event
    /// lands the survey jump fitted on the 2022 price shock (`EventSalienceFit.EconomyJump`, some ten points of the two-mention share), a minor
    /// one as a severe one; band scaling is the ask's alternative.</para>
    /// </summary>
    public static class EventSalience
    {
        /// <summary>CONVENTION - an epsilon, not a claim about the world: once every issue's shift is below this (share units) the vector is back at
        /// its seed and the stored shift is dropped, so a faded jump stops writing the country rather than shrinking through the denormals.</summary>
        private const float SettledBelow = 1e-6f;

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

        /// <summary>The class's hold in days (DERIVED by the fit: the units' mean days from the event to the survey's first wave after it) - zero for an
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

        /// <summary>DECLARED (review 882b's M-D): whether events move a country's salience at all - every country whose seed is Eurobarometer's
        /// two-mention share; not the USA, whose seed is Gallup's single-mention figure and for which no analogue is fitted.</summary>
        public static bool MovesSalience(CountryId country) => country != CountryId.USA;

        /// <summary>The event's jump on the country's salience, on the day it lands: its class's fitted jump added to the issue's shift, held so the
        /// seed + shift stays in [0, 1], and the issue's hold set to the class's. Logged (`SALIENCE:`) where it moves anything (an issue already at the
        /// top of the scale moves nothing). Nothing for a null event, a political or a favourable one, an inert class or the USA - and then the
        /// country is untouched to the bit.</summary>
        public static void Land(Country country, EconomicEvent economicEvent)
        {
            if (country == null || !MovesSalience(country.Id) || !Moves(economicEvent) || !TryClassOf(economicEvent.Tag, out SalienceClass salienceClass)) { return; }
            float jump = JumpOf(salienceClass);
            if (!(jump > 0f)) { return; }
            IssueId issue = IssueOf(salienceClass);
            float seed = StanceModel.SalienceOf(country.Id, issue, out _);
            if (country.SalienceShift == null) { country.SalienceShift = new float[IssueVector.IssueCount]; }
            else if (country.SalienceShift.Length < IssueVector.IssueCount) { Array.Resize(ref country.SalienceShift, IssueVector.IssueCount); }   // a shorter vector keeps every issue's shift
            if (country.SalienceHoldDays == null) { country.SalienceHoldDays = new int[IssueVector.IssueCount]; }
            else if (country.SalienceHoldDays.Length < IssueVector.IssueCount) { Array.Resize(ref country.SalienceHoldDays, IssueVector.IssueCount); }
            float before = country.SalienceShift[(int)issue];
            country.SalienceShift[(int)issue] = Mathf.Clamp(before + jump, -seed, 1f - seed);
            if (country.SalienceShift[(int)issue] == before) { return; }   // already at the scale's top: nothing moved, nothing logged
            country.SalienceHoldDays[(int)issue] = HoldDaysOf(salienceClass);
            Debug.Log(string.Format(CultureInfo.InvariantCulture, "SALIENCE: {0} - {1} ({2}, class {3}) raises {4} by {5:0.0000}: {6:0.0000} -> {7:0.0000} (seed {8:0.00}, held {9} days, then half-life {10:0.0} days)",
                country.Id, economicEvent.Name, economicEvent.Tag, salienceClass, issue, country.SalienceShift[(int)issue] - before, seed + before,
                seed + country.SalienceShift[(int)issue], seed, HoldDaysOf(salienceClass), HalfLifeDays(salienceClass)));
        }

        /// <summary>The day, first in the day (`SimulationManager.AdvanceDay`): an issue still in its hold counts a day off it and keeps its shift; one
        /// past it decays by its class's daily persistence; the stored shift (and its holds) dropped once every issue is back at its seed. Nothing while
        /// no event holds anything (null). DECLARED (review 882's L-2): a shift standing on an issue whose class the fit leaves inert (no rate to decay
        /// at - reachable only through a refit under a standing shift) is dropped that day, never held for ever.</summary>
        public static void AdvanceDay(Country country)
        {
            float[] shift = country?.SalienceShift;
            if (shift == null) { return; }
            int[] hold = country.SalienceHoldDays;
            bool standing = false;
            for (int i = 0; i < shift.Length; i++)
            {
                if (shift[i] == 0f) { continue; }
                bool live = TryClassOfIssue((IssueId)i, out SalienceClass c) && LambdaOf(c) > 0.0;
                if (live && hold != null && i < hold.Length && hold[i] > 0) { hold[i]--; standing = true; continue; }
                shift[i] *= live ? DailyPersistence(c) : 0f;
                if (Mathf.Abs(shift[i]) < SettledBelow) { shift[i] = 0f; } else { standing = true; }
            }
            if (!standing) { country.SalienceShift = null; country.SalienceHoldDays = null; }
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
