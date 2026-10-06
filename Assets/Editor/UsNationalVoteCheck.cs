using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Elections.Generated;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6 US-5 (`COMPLETED.md` §789): THE NATIONAL-VOTE PROOF - can the vote model hold two parties? It asks R-US15, which vote the US poll is,
    /// with the measurement. The design is DECLARED before the build (`docs/specs/USA_STAGE_PLAN.md`, US-5): each history's electorate is fitted to
    /// that history's last election before the predicted one (T-1) - free by <see cref="VoteModel.Calibrate"/>, and held at the σ and τ of each
    /// country <see cref="PartySystems.TryElectorate"/> holds (and, added with the measurement and DECLARED there, at the four's median), the mean
    /// on the segment from DEM's point to REP's at the place that gives T-1's split; the loyalty from T-1 against T-2, each party's share of all
    /// votes cast; the prediction in the live chain's order, composed here because nothing of it is live for the USA
    /// (<see cref="VoteModel.PredictShares"/> → the compatibility scale → <see cref="PreferenceModel.Preference"/> →
    /// <see cref="EconomicVote.ApplyRecordShift"/>; no created party, so the entrant layer has nothing to add), the economic vote read on a world
    /// built on the eve as a new game builds one (the epoch set before the world is created). The House history reads the Clerk of the House's
    /// recapitulation, the presidential one US-3's catalog - both generated (<see cref="UsPresidentialReturns"/>).
    ///
    /// <para><b>What it proves before it prints.</b> With two parties a fit pins one number, the two-party split; and the compatibility scale hands
    /// the spatial shares back unchanged, so an electorate fitted to T-1 returns T-1's split through the preference layer whatever the loyalty.
    /// Each fit is checked for it - the poll no farther from the prior than the fit's own residual - so a history's national miss is its no-change
    /// miss plus the economic vote. The fits give the same poll; they part on what the spread decides away from the fitted points - what a third
    /// placed unit takes, and how far the split moves when a party's point moves - and the card prints both. The economic vote is printed apart,
    /// never folded into the history's miss. E1's factors close 2024 on the eve world's poll by construction (in-sample).</para>
    ///
    /// <para>Not live: nothing here reaches <see cref="PartySystems"/> or the game's poll (US-6's change). Pinned in the cheap bar through the US
    /// model card's marked block (`docs/reference/US_ELECTIONS.md`, its second block), written by <see cref="WriteReadings"/>; a card that no longer
    /// says what this measures fails here. R-US15's rule is run over the 2024 cases and its verdict printed; a verdict the rule leaves to Elias fails
    /// here, to be asked. <see cref="GateSection"/> prints the same cases in the R-EL13 form after GateReRun's verdict.</para>
    /// </summary>
    public static class UsNationalVoteCheck
    {
        private enum Contest { House, President }

        /// <summary>A history: its contest, its last election before the predicted one (T-1: the prior, and what the electorate is fitted to) and the
        /// one before (T-2: with T-1, the loyalty).</summary>
        private readonly struct History
        {
            public readonly string Key;
            public readonly Contest Of;
            public readonly int T1, T2;
            public History(string key, Contest of, int t1, int t2) { Key = key; Of = of; T1 = t1; T2 = t2; }
            public string Label => F("{0} {1} {2} against {3}", Key, Of == Contest.House ? "House" : "presidential", T1, T2);
        }

        /// <summary>A case: a history predicting an election, on the world built on its eve where one can be, scored on the contests the election
        /// held - the history's own first where it was held.</summary>
        private readonly struct Case
        {
            public readonly int Year;
            public readonly History History;
            public readonly bool OnWorld;
            public readonly Contest[] Scored;
            public Case(int year, History history, bool onWorld, params Contest[] scored) { Year = year; History = history; OnWorld = onWorld; Scored = scored; }
        }

        /// <summary>The cases the design declares: 2024 by each history on its eve world; out of sample, 2022's House on its eve world and 2020 with
        /// none (no world can be built before the USA's first government of record, `WorldClock.Governments` - <see cref="GovernmentRecord.AtStart"/>
        /// would have none to seat).</summary>
        private static readonly Case[] Cases =
        {
            new Case(2024, new History("(a)", Contest.House, 2022, 2020), true, Contest.House, Contest.President),
            new Case(2024, new History("(b)", Contest.President, 2020, 2016), true, Contest.President, Contest.House),
            new Case(2022, new History("(a)", Contest.House, 2020, 2018), true, Contest.House),
            new Case(2022, new History("(b)", Contest.President, 2020, 2016), true, Contest.House),
            new Case(2020, new History("(a)", Contest.House, 2018, 2016), false, Contest.House, Contest.President),
            new Case(2020, new History("(b)", Contest.President, 2016, 2012), false, Contest.President, Contest.House),
        };

        /// <summary>The held spreads: the countries whose electorates <see cref="PartySystems.TryElectorate"/> holds, read there at run time - the
        /// list checked against every country it answers for.</summary>
        private static readonly CountryId[] Held = { CountryId.Sweden, CountryId.Germany, CountryId.Poland, CountryId.Italy };

        /// <summary>The figures R-US15 is ruled on, pinned as the card prints them: 2024's miss by each history on its own contest, before and after
        /// the economic vote (points, two-party), and the economic vote itself (DEM, points). A refreshed card cannot move them unseen: the writer
        /// refuses while a pin fails.</summary>
        private static readonly (int Year, string History, string Before, string After, string Shift)[] Pins =
        {
            (2024, "(a)", "+0.08", "-2.42", "+2.50"),
            (2024, "(b)", "-3.02", "-5.52", "+2.50"),
        };

        /// <summary>The minor candidates the plan names, by the FEC workbook's column labels; every other column is "the rest".</summary>
        private static readonly string[] NamedMinors = { "STEIN", "KENNEDY", "OLIVER" };

        /// <summary>A fit, and what it leaves undecided: the split it gives (the REP share of the two parties), its place on the DEM-REP segment where
        /// held, how far the split moves when the mean slides one and two units along the line dividing the parties, how far it moves when DEM's
        /// point moves one unit toward REP's (a nominee off the party's point), and the spatial share it gives a unit placed midway between them.</summary>
        private sealed class Fit
        {
            public string Name;
            public VoteModel.Electorate Electorate;
            public double Place = double.NaN, Split, SlideOne, SlideTwo, MovedPoint, Third;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsNationalVoteCheck (PS-6 US-5, §789): the national vote by two histories, the vote model's two-party fit, E1's factors ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string expected = CardBlock(card, digest);
                string onDisk = CardBlockOf(File.ReadAllText(CardPath(), Encoding.UTF8));
                Check(onDisk == expected, onDisk == null ? F("the model card ({0}) carries no US-5 readings block", CardRelative)
                    : onDisk == expected ? F("the model card's US-5 readings ({0}) say what this measures - {1} lines", CardRelative, card.Count)
                    : F("the model card's US-5 readings ({0}) are STALE - regenerate: -executeMethod PoliSim.EditorTools.UsNationalVoteCheck.WriteReadings", CardRelative));
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }

            sb.Append(failures == 0 ? "=== UsNationalVoteCheck: the reading holds ===" : F("=== UsNationalVoteCheck: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>Writes the card's US-5 block from this reading - the stamp and its END marker placed once by hand; refuses while any check fails.</summary>
        public static void WriteReadings()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsNationalVoteCheck.WriteReadings (§789): the US model card's US-5 readings ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                var card = new List<string>();
                string digest = Measure(sb, Check, card);
                string path = CardPath();
                string text = File.ReadAllText(path, Encoding.UTF8);
                string old = CardBlockOf(text);
                if (failures > 0) { Check(false, "a check above failed - the card is not written; fix what failed, then write"); }
                else if (old == null) { Check(false, F("the model card ({0}) carries no US-5 readings block to write into", CardRelative)); }
                else
                {
                    string written = text.Replace("\r\n", "\n").Replace(old, CardBlock(card, digest));
                    if (text.Contains("\r\n")) { written = written.Replace("\n", "\r\n"); }
                    File.WriteAllText(path, written, new UTF8Encoding(false));
                    Check(true, F("the model card's US-5 readings written ({0}) - {1} lines", CardRelative, card.Count));
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }

            sb.Append(failures == 0 ? "=== WriteReadings: written ===" : F("=== WriteReadings: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>The reading <see cref="Run"/> checks and <see cref="WriteReadings"/> writes. Returns the digest the block is stamped with - the
        /// catalog's source digests and the held electorates, so a regenerated catalog or a refitted country makes the card stale.</summary>
        private static string Measure(StringBuilder sb, Action<bool, string> Check, List<string> card)
        {
            var clock = Stopwatch.StartNew();
            PoliticalParty[] roster = PartySystems.RealRoster(CountryId.USA);
            string[] keys = roster.Select(p => p.Abbrev).ToArray();
            Check(keys.SequenceEqual(new[] { "REP", "DEM" }), F("the US roster is REP and DEM, in that order: {0}", string.Join(", ", keys)));
            VoteModel.PartyPoint[] points = roster.Select(p => new VoteModel.PartyPoint(p.Abbrev, p.LrEcon, p.Galtan)).ToArray();
            double w = VoteShareBacktest.UsEconomicWeight;
            CountryId[] holders = ((CountryId[])Enum.GetValues(typeof(CountryId))).Where(id => PartySystems.TryElectorate(id, out _, out _)).OrderBy(id => id).ToArray();
            Check(holders.SequenceEqual(Held.OrderBy(id => id)), F("the held spreads are every country PartySystems.TryElectorate holds: {0}", string.Join(", ", holders)));

            // ---- the eve worlds: the economic vote each case's poll carries
            var eves = new Dictionary<int, Eve>();
            foreach (int year in Cases.Where(c => c.OnWorld).Select(c => c.Year).Distinct())
            {
                Eve e = ReadEve(year);
                eves[year] = e;
                bool unified = e.LargestParty == e.PmParty;
                Check(e.Kind == WorldClock.ExecutiveKind.Presidency && e.PmParty == "DEM",
                    F("{0}'s eve world ({1:yyyy-MM-dd}, stepped to {2:yyyy-MM-dd}): the government of record a presidency of DEM - {3}", year, e.Epoch, e.Day, e.Executive));
                Check(e.SeatedOfRecord, F("{0}: the House seated on the eve is the one elected at the House election before it ({1:yyyy-MM-dd}), its seats the record's", year, e.PreviousElection));
                Check(e.Term.Unemployment.Complete && e.Term.Inflation.Complete && e.TookOffice.HasValue && e.TookOffice.Value < e.Epoch && e.TookOffice.Value == e.GovernmentFrom,
                    F("{0}: the record judged over the term at both ends, the term begun before the eve on the government of record's first day - {1}", year, EconomicVote.Describe(e.Term)));
                double magnitude = e.Magnitudes.TryGetValue("DEM", out double m) ? m : double.NaN;
                Check(e.Magnitudes.Count == 1 && magnitude == (unified ? EconomicVote.UsPresidentUnified : EconomicVote.UsPresidentDivided),
                    F("{0}: the House's largest party {1}, so the government {2} and DEM's economic vote {3:0.00} (Table 9.1's presidential row)", year, e.LargestParty, unified ? "unified" : "divided", magnitude));
                Check(e.Shift.Count == 1 && e.Shift.ContainsKey("DEM"), F("{0}: the shift names DEM alone: {1:+0.00;-0.00} pp", year, 100.0 * e.Shift.Values.FirstOrDefault()));
            }

            // ---- the fits, one set a T-1
            long fitsFrom = clock.ElapsedMilliseconds;
            Dictionary<(Contest, int), (VoteModel.Electorate Electorate, double Mad)> free = FreeFits(points, w);
            var fits = new Dictionary<(Contest, int), List<Fit>>();
            foreach (Case c in Cases)
            {
                var key = (c.History.Of, c.History.T1);
                if (!fits.ContainsKey(key)) { fits[key] = FitAll(points, w, SharesOf(c.History.Of, c.History.T1), free[key], Check, F("{0} {1}", c.History.Of, c.History.T1)); }
            }
            long calibrateMs = clock.ElapsedMilliseconds - fitsFrom;

            // ---- the cases: the poll each fit gives, the identity, the miss on each contest with and without the economic vote
            card.Add("### The national vote by each history");
            card.Add("");
            card.Add("Two-party, signed: REP's predicted share of the two parties less the record's, in points. \"Before\" is the poll the vote model gives (every fit gives the same - the identity holds on each); \"after\" adds the economic vote of the eve world, printed in its own column. The loyalty is each party's, T-1 against T-2. \"(own)\" marks the history's own contest; 2022 held no presidential vote, so the presidential history is scored there on the House alone.");
            card.Add("");
            card.Add("| predicted | history | T-1 split (R) | loyalty R / D | economic vote (DEM, pp) | poll R before → after | scored on | record (R) | miss before | miss after |");
            card.Add("|---|---|---:|---:|---:|---:|---|---:|---:|---:|");
            var polls = new Dictionary<(int, string), double[]>();
            var misses = new Dictionary<(int Year, string History, Contest Scored), (double Before, double After, bool World)>();
            foreach (Case c in Cases)
            {
                double[] prior = SharesOf(c.History.Of, c.History.T1);
                double[] loyalty = LoyaltyModel.PartyLoyalties(prior, SharesOf(c.History.Of, c.History.T2));
                double priorSplit = TwoParty(prior);
                double[] poll = null;
                double worst = 0.0;
                foreach (Fit fit in fits[(c.History.Of, c.History.T1)])
                {
                    double[] spatial = VoteModel.PredictShares(points, fit.Electorate, w);
                    double[] p = PreferenceModel.Preference(GateReRun.ToCompatScale(spatial), prior, loyalty);
                    double off = Math.Abs(p[0] - priorSplit), residual = Math.Abs(spatial[0] - priorSplit);
                    Check(off <= residual + 1e-12, F("{0} {1}, {2}: the poll {3:0.000000} is no farther from the prior {4:0.000000} than the fit's residual {5:0.0e+0} - the identity", c.Year, c.History.Key, fit.Name, p[0], priorSplit, residual));
                    worst = Math.Max(worst, off);
                    if (poll == null || fit.Name == "free") { poll = p; }
                }

                Eve eve = c.OnWorld ? eves[c.Year] : null;
                double shiftDem = eve != null && eve.Shift.TryGetValue("DEM", out double sd) ? sd : 0.0;
                double[] after = eve != null ? EconomicVote.ApplyRecordShift(keys, poll, eve.Shift) : poll;
                polls[(c.Year, c.History.Key)] = after;
                sb.Append(F("  {0} {1}: prior R {2:0.0000}; loyalty R {3:0.00} D {4:0.00}; the poll R {5:0.0000} before the economic vote (every fit within {6:0.0e+0} of the prior), {7:0.0000} after it ({8})\n",
                    c.Year, c.History.Label, priorSplit, loyalty[0], loyalty[1], poll[0], worst, after[0], eve == null ? "no world" : F("DEM {0:+0.00;-0.00} pp", 100.0 * shiftDem)));
                for (int k = 0; k < c.Scored.Length; k++)
                {
                    double record = TwoParty(SharesOf(c.Scored[k], c.Year));
                    double before = 100.0 * (poll[0] - record), afterMiss = 100.0 * (after[0] - record);
                    misses[(c.Year, c.History.Key, c.Scored[k])] = (before, afterMiss, eve != null);
                    sb.Append(F("      scored on the {0} {1}: record R {2:0.0000}; miss {3:+0.00;-0.00} before, {4} after\n", c.Scored[k] == Contest.House ? "House" : "presidential", c.Year, record, before,
                        eve == null ? "no world" : F("{0:+0.00;-0.00}", afterMiss)));
                    card.Add(F("| {0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} | {8} | {9} |",
                        k == 0 ? c.Year.ToString(CultureInfo.InvariantCulture) : "", k == 0 ? c.History.Label : "", k == 0 ? Pct(priorSplit) : "", k == 0 ? F("{0:0.0} / {1:0.0}", loyalty[0], loyalty[1]) : "",
                        k == 0 ? (eve == null ? "no world" : F("{0:+0.00;-0.00}", 100.0 * shiftDem)) : "", k == 0 ? (eve == null ? Pct(poll[0]) : F("{0} → {1}", Pct(poll[0]), Pct(after[0]))) : "",
                        F("{0} {1}{2}", c.Scored[k] == Contest.House ? "House" : "president", c.Year, c.Scored[k] == c.History.Of ? " (own)" : ""), Pct(record), Pp(before), eve == null ? "-" : Pp(afterMiss)));
                }

                foreach (var pin in Pins.Where(x => x.Year == c.Year && x.History == c.History.Key))
                {
                    Check(c.Scored[0] == c.History.Of, F("pinned: {0} {1} is scored first on its own contest", c.Year, c.History.Key));
                    double own = TwoParty(SharesOf(c.Scored[0], c.Year));
                    string b = Pp(100.0 * (poll[0] - own)), a = eve == null ? "-" : Pp(100.0 * (after[0] - own)), s = Pp(100.0 * shiftDem);
                    Check(b == pin.Before && a == pin.After && s == pin.Shift, F("pinned: {0} {1} on its own contest - miss {2} before and {3} after the economic vote of {4} (pinned {5}, {6}, {7})",
                        c.Year, c.History.Key, b, a, s, pin.Before, pin.After, pin.Shift));
                }
            }

            Check(Pins.All(p => Cases.Any(c => c.Year == p.Year && c.History.Key == p.History)), "every pin names a case");

            // ---- R-US15's rule over the 2024 cases, and the out-of-sample comparisons, generated (the plan cites them)
            card.Add("");
            card.Add(Verdict(misses, Check, sb));
            card.Add("");
            card.Add(OutOfSample(misses));
            card.Add("");
            card.Add("The economic vote on each eve world, as the record over the term reads it:");
            card.Add("");
            foreach (KeyValuePair<int, Eve> kv in eves.OrderByDescending(x => x.Key))
            {
                card.Add(F("- {0}: {1}. The House's largest party {2}; DEM's economic vote {3:0.00}; DEM {4:+0.00;-0.00} pp.", kv.Key, EconomicVote.Describe(kv.Value.Term), kv.Value.LargestParty,
                    kv.Value.Magnitudes.TryGetValue("DEM", out double mm) ? mm : double.NaN, 100.0 * (kv.Value.Shift.TryGetValue("DEM", out double s2) ? s2 : 0.0)));
            }

            // ---- the fits, and what the spread decides away from the fitted points
            card.Add("");
            card.Add("### The fits and what they leave undecided");
            card.Add("");
            card.Add(F("Each history's electorate fitted to its T-1 (wEcon {0:0.00}); the positions are the roster's. Free: `VoteModel.Calibrate`'s grid. Held: each country's σ and τ as `PartySystems.TryElectorate` holds them, and the four's median (σ and τ each the median of the four) - the mean on the segment from DEM's point to REP's at the place (0 at DEM, 1 at REP) that gives T-1's split. \"Slide\": the largest change in the split, in points, when the mean moves one or two units either way along the line dividing the two parties. \"Moved point\": the change in the split, in points, when DEM's point moves one unit toward REP's - a nominee off the party's point. \"Midway unit\": the spatial share the fit gives a third unit placed at the parties' midpoint. Both last columns are spatial, before the loyalty draws the poll back toward the prior.", w));
            card.Add("");
            card.Add("| T-1 | fit | μ econ, μ soc | σ | τ | place | split (R) | residual (pp) | slide ±1 / ±2 (pp) | moved point (pp) | midway unit |");
            card.Add("|---|---|---|---:|---:|---:|---:|---:|---:|---:|---:|");
            foreach (KeyValuePair<(Contest, int), List<Fit>> kv in fits.OrderBy(x => x.Key.Item1).ThenByDescending(x => x.Key.Item2))
            {
                double target = TwoParty(SharesOf(kv.Key.Item1, kv.Key.Item2));
                bool first = true;
                foreach (Fit fit in kv.Value)
                {
                    card.Add(F("| {0} | {1} | {2:0.000}, {3:0.000} | {4:0.00} | {5:0.00} | {6} | {7} | {8:0.0000} | {9:0.00} / {10:0.00} | {11} | {12:0.0} % |",
                        first ? F("{0} {1} (R {2})", kv.Key.Item1 == Contest.House ? "House" : "presidential", kv.Key.Item2, Pct(target)) : "", fit.Name,
                        fit.Electorate.MuEcon, fit.Electorate.MuSoc, fit.Electorate.Sigma, fit.Electorate.Tau, double.IsNaN(fit.Place) ? "-" : F("{0:0.0000}", fit.Place),
                        Pct(fit.Split), 100.0 * Math.Abs(fit.Split - target), fit.SlideOne, fit.SlideTwo, Pp(fit.MovedPoint), 100.0 * fit.Third));
                    first = false;
                }
            }

            List<Fit> all = fits.Values.SelectMany(f => f).ToList();
            card.Add("");
            card.Add(F("Every fit holds its T-1 split (free: within Calibrate's grid; held: solved). Away from the fitted points the spread decides: a nominee one unit off DEM's point moves the split by between {0} and {1} points, by the fit, and the midway unit's share runs from {2:0.0} % to {3:0.0} % - what the choice of spread decides, and the two-party split at the fitted points does not.",
                Pp(all.Max(f => f.MovedPoint)), Pp(all.Min(f => f.MovedPoint)), 100.0 * all.Min(f => f.Third), 100.0 * all.Max(f => f.Third)));

            // ---- E1's factors: 2024, on each history's eve-world poll (in-sample by construction)
            card.Add("");
            card.Add("### E1's factors on the 2024 eve world's poll (in-sample by construction)");
            card.Add("");
            var field = UsPresidentialReturns.Candidates.Where(c => c.Year == 2024).ToArray();
            long total = field.Sum(c => c.Votes);
            long jurisdictions = UsPresidentialReturns.States.Where(s => s.Year == 2024).Sum(s => s.VotesTotal);
            long votesR = UsPresidentialReturns.States.Where(s => s.Year == 2024).Sum(s => s.VotesR), votesD = UsPresidentialReturns.States.Where(s => s.Year == 2024).Sum(s => s.VotesD);
            Check(field.Count(c => c.Ticket == "R") == 1 && field.Count(c => c.Ticket == "D") == 1 && total == jurisdictions
                  && field.Single(c => c.Ticket == "R").Votes == votesR && field.Single(c => c.Ticket == "D").Votes == votesD,
                "2024's field: one REP and one DEM nominee, each the jurisdictions' own figure, the columns together the jurisdictions' total");
            double trump = (double)votesR / total, harris = (double)votesD / total;
            var minors = new List<(string Name, double Share)>();
            foreach (string name in NamedMinors)
            {
                var hit = field.Where(c => c.Candidate == name).ToArray();
                Check(hit.Length == 1, F("2024's field holds {0} once", name));
                minors.Add((name, hit.Length == 1 ? (double)hit[0].Votes / total : 0.0));
            }

            double rest = 1.0 - trump - harris - minors.Sum(x => x.Share);
            card.Add(F("The field: Trump (REP) {0}, Harris (DEM) {1}; at their shares of record as base: {2}, the rest {3} (every other column of the FEC's workbook, None of These Candidates and the scattered write-ins among them). The nominees together hold {4} of the vote, so a poll exact on the two-party split gives both factors that figure - the column of factors ÷ the nominees' share divides it out. The field's sum is one by construction (E1's normalisation), printed as the design asks.",
                Pct(trump), Pct(harris), string.Join(", ", minors.Select(x => F("{0} {1}", Title(x.Name), Pct(x.Share)))), Pct(rest), Pct(trump + harris)));
            card.Add("");
            card.Add("| history | poll R / D | Trump's factor | Harris's factor | the field's sum | factors ÷ the nominees' share (R / D) | the poll's two-party miss on the presidential 2024 (pp) |");
            card.Add("|---|---:|---:|---:|---:|---:|---:|");
            foreach (Case c in Cases.Where(x => x.Year == 2024))
            {
                double[] p = polls[(c.Year, c.History.Key)];
                double fT = trump / p[0], fH = harris / p[1];
                double sum = fT * p[0] + fH * p[1] + minors.Sum(x => x.Share) + rest;
                double record = TwoParty(SharesOf(Contest.President, 2024));
                card.Add(F("| {0} | {1} / {2} | {3:0.000000} | {4:0.000000} | {5:0.000000} | {6:0.0000} / {7:0.0000} | {8} |", c.History.Label, Pct(p[0]), Pct(p[1]), fT, fH, sum, fT / (trump + harris), fH / (trump + harris), Pp(100.0 * (p[0] - record))));
                sb.Append(F("  E1 2024 {0}: Trump {1:0.000000}, Harris {2:0.000000}\n", c.History.Key, fT, fH));
            }

            sb.Append(F("  measured in {0} ms (the fits {1} ms)\n", clock.ElapsedMilliseconds, calibrateMs));
            var held = new StringBuilder();
            foreach (CountryId id in Held) { PartySystems.TryElectorate(id, out VoteModel.Electorate e, out double ew); held.Append(id).Append(e).Append(ew.ToString("R", CultureInfo.InvariantCulture)); }
            return Digest(UsPresidentialReturns.StateSourceDigest + UsPresidentialReturns.HouseSourceDigest + UsPresidentialReturns.CandidateSourceDigest + held
                          + string.Join(",", points.Select(p => F("{0}:{1:R}:{2:R}", p.Name, p.Econ, p.Soc))) + w.ToString("R", CultureInfo.InvariantCulture));
        }

        /// <summary>R-US15's rule (`docs/specs/USA_STAGE_PLAN.md`): "Code expects to recommend (a), unless its national miss is well above (b)'s" - run
        /// on 2024, each history on its own contest, before and after the economic vote. (a) not above (b) both ways recommends (a); otherwise "well
        /// above" is Elias's to judge, and the check fails, to be asked. The presidential contest's comparison is printed beside it.</summary>
        private static string Verdict(Dictionary<(int Year, string History, Contest Scored), (double Before, double After, bool World)> misses, Action<bool, string> Check, StringBuilder sb)
        {
            var a = misses[(2024, "(a)", Contest.House)];
            var b = misses[(2024, "(b)", Contest.President)];
            var aP = misses[(2024, "(a)", Contest.President)];
            bool notAbove = Math.Abs(a.Before) <= Math.Abs(b.Before) && Math.Abs(a.After) <= Math.Abs(b.After);
            bool presToo = Math.Abs(aP.Before) <= Math.Abs(b.Before) && Math.Abs(aP.After) <= Math.Abs(b.After);
            string verdict = notAbove
                ? F("**By R-US15's rule, code recommends (a)**: (a)'s 2024 miss on its own contest is not above (b)'s, before the economic vote ({0} against {1}) and after it ({2} against {3}). On the presidential vote, (a) is {4} ({5} and {6}).",
                    Pp(a.Before), Pp(b.Before), Pp(a.After), Pp(b.After), presToo ? "the closer as well" : "not the closer", Pp(aP.Before), Pp(aP.After))
                : F("**R-US15's rule does not decide**: (a)'s 2024 miss on its own contest is above (b)'s ({0} against {1} before, {2} against {3} after) - whether it is \"well above\" is Elias's to judge, to be asked.",
                    Pp(a.Before), Pp(b.Before), Pp(a.After), Pp(b.After));
            Check(notAbove, F("R-US15's rule decides: {0}", verdict.Replace("**", string.Empty)));
            sb.Append("  ").Append(verdict.Replace("**", string.Empty)).Append('\n');
            return verdict;
        }

        /// <summary>Out of sample: for each earlier predicted year and each contest both histories are scored on, the closer history - before the
        /// economic vote, and after it where a world was built.</summary>
        private static string OutOfSample(Dictionary<(int Year, string History, Contest Scored), (double Before, double After, bool World)> misses)
        {
            var parts = new List<string>();
            foreach (int year in misses.Keys.Select(k => k.Year).Where(y => y != 2024).Distinct().OrderByDescending(y => y))
            {
                foreach (Contest contest in new[] { Contest.House, Contest.President })
                {
                    if (!misses.TryGetValue((year, "(a)", contest), out var a) || !misses.TryGetValue((year, "(b)", contest), out var b)) { continue; }
                    string Closer(double x, double y) => Math.Abs(x) < Math.Abs(y) ? "(a)" : Math.Abs(y) < Math.Abs(x) ? "(b)" : "neither";
                    parts.Add(F("{0}'s {1} vote - {2} the closer before the economic vote ({3} against {4}){5}", year, contest == Contest.House ? "House" : "presidential", Closer(a.Before, b.Before), Pp(a.Before), Pp(b.Before),
                        a.World && b.World ? F(", {0} after it ({1} against {2})", Closer(a.After, b.After), Pp(a.After), Pp(b.After)) : " (no world)"));
                }
            }

            return "Out of sample, (a) against (b): " + string.Join("; ", parts) + ".";
        }

        /// <summary>GateReRun's US section (after its verdict, outside the gate): each case in the R-EL13 form - no world, no economic vote - the free
        /// fit's and each held fit's national miss and the miss with §8's loyalty, on the first contest the case is scored on (the history's own where
        /// it was held), two-party and signed. A fit's check that fails is named here as well as in <see cref="Run"/>.</summary>
        internal static string GateSection()
        {
            var sb = new StringBuilder();
            PoliticalParty[] roster = PartySystems.RealRoster(CountryId.USA);
            VoteModel.PartyPoint[] points = roster.Select(p => new VoteModel.PartyPoint(p.Abbrev, p.LrEcon, p.Galtan)).ToArray();
            double w = VoteShareBacktest.UsEconomicWeight;
            sb.Append("\nUSA (PS-6 US-5, R-US15's evidence) - NOT IN THE R-EL13 GATE, and not live. Each history's electorate fitted to its own last election before the\n");
            sb.Append("predicted one (T-1), free (VoteModel.Calibrate) and held at each fitted country's spread; loyalty T-1 against T-2; no world and no economic\n");
            sb.Append("vote here (UsNationalVoteCheck reads them on the eve worlds). Two-party, signed: REP's predicted share less the record's, in points.\n");
            var failed = new List<string>();
            void Check(bool ok, string what) { if (!ok) { failed.Add(what); } }
            Dictionary<(Contest, int), (VoteModel.Electorate Electorate, double Mad)> free = FreeFits(points, w);
            var fitted = new Dictionary<(Contest, int), List<Fit>>();
            double heldGap = 0.0, freeGap = 0.0;
            foreach (Case c in Cases)
            {
                var key = (c.History.Of, c.History.T1);
                if (!fitted.ContainsKey(key)) { fitted[key] = FitAll(points, w, SharesOf(c.History.Of, c.History.T1), free[key], Check, F("{0} {1}", c.History.Of, c.History.T1)); }
                double[] prior = SharesOf(c.History.Of, c.History.T1);
                double[] loyalty = LoyaltyModel.PartyLoyalties(prior, SharesOf(c.History.Of, c.History.T2));
                double record = TwoParty(SharesOf(c.Scored[0], c.Year));
                var parts = new List<string>();
                foreach (Fit fit in fitted[key])
                {
                    double[] spatial = VoteModel.PredictShares(points, fit.Electorate, w);
                    double[] p = PreferenceModel.Preference(GateReRun.ToCompatScale(spatial), prior, loyalty);
                    double gap = 100.0 * Math.Abs(spatial[0] - p[0]);
                    if (fit.Name == "free") { freeGap = Math.Max(freeGap, gap); } else { heldGap = Math.Max(heldGap, gap); }
                    parts.Add(F("{0} {1} -> {2}", fit.Name, Pp(100.0 * (spatial[0] - record)), Pp(100.0 * (p[0] - record))));
                }
                sb.Append(F("  {0} {1} on the {2} {0}: national -> +§8 | {3}\n", c.Year, c.History.Label, c.Scored[0] == Contest.House ? "House" : "presidential", string.Join(" | ", parts)));
            }

            sb.Append(F("  Each held fit holds its T-1 split, so its national and +§8 agree (within {0:0.0e+0} pp); the free fit's differ by its grid residual, at most\n", heldGap));
            sb.Append(F("  {0:0.000} pp here: with two parties the vote model returns the prior, whatever the loyalty.\n", freeGap));
            if (failed.Count > 0) { sb.Append(F("  ⚠ {0} check(s) of the fits failed - UsNationalVoteCheck fails on the same: {1}\n", failed.Count, string.Join("; ", failed))); }
            return sb.ToString();
        }

        // ---- the fits

        /// <summary>Calibrate's free fit to each T-1's two-party split - the grid searches run side by side (pure functions, each the grid's own
        /// deterministic answer whatever the order; ArtifactIdentityCheck's precedent), since together they are most of this instrument's time.</summary>
        private static Dictionary<(Contest, int), (VoteModel.Electorate Electorate, double Mad)> FreeFits(VoteModel.PartyPoint[] points, double w)
        {
            (Contest, int)[] keys = Cases.Select(c => (c.History.Of, c.History.T1)).Distinct().ToArray();
            var found = new (VoteModel.Electorate Electorate, double Mad)[keys.Length];
            System.Threading.Tasks.Parallel.For(0, keys.Length, new System.Threading.Tasks.ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount) }, i =>
            {
                double target = TwoParty(SharesOf(keys[i].Item1, keys[i].Item2));
                VoteModel.Electorate e = VoteModel.Calibrate(points, new[] { target, 1.0 - target }, w, out double mad);
                found[i] = (e, mad);
            });
            var result = new Dictionary<(Contest, int), (VoteModel.Electorate Electorate, double Mad)>();
            for (int i = 0; i < keys.Length; i++) { result[keys[i]] = found[i]; }
            return result;
        }

        /// <summary>The free fit, the four held fits, and the fit held at the four's median spread, to a T-1's two-party split.</summary>
        private static List<Fit> FitAll(VoteModel.PartyPoint[] points, double w, double[] t1, (VoteModel.Electorate Electorate, double Mad) free, Action<bool, string> Check, string label)
        {
            double target = TwoParty(t1);
            var result = new List<Fit> { Describe(points, w, "free", free.Electorate, double.NaN) };
            Check(free.Mad < 0.05, F("{0}: Calibrate's free fit within its grid's reach - MAD {1:0.0000} pp", label, free.Mad));
            var sigmas = new List<double>();
            var taus = new List<double>();
            foreach (CountryId id in Held)
            {
                bool has = PartySystems.TryElectorate(id, out VoteModel.Electorate e, out double _);
                Check(has, F("{0}: {1}'s electorate is held", label, id));
                if (!has) { continue; }
                sigmas.Add(e.Sigma);
                taus.Add(e.Tau);
                result.Add(HeldFit(points, w, target, e.Sigma, e.Tau, id + "'s spread", Check, label));
            }

            if (sigmas.Count == Held.Length) { result.Add(HeldFit(points, w, target, Median(sigmas), Median(taus), "the four's median", Check, label)); }
            return result;
        }

        /// <summary>A held fit: σ and τ as given, the mean on the segment from DEM's point to REP's at the place that gives <paramref name="target"/>,
        /// found by bisection (the REP share rises along the segment).</summary>
        private static Fit HeldFit(VoteModel.PartyPoint[] points, double w, double target, double sigma, double tau, string name, Action<bool, string> Check, string label)
        {
            var e = new VoteModel.Electorate(0.0, 0.0, sigma, tau);
            double lo = 0.0, hi = 1.0, rLo = RepAt(points, w, e, lo), rHi = RepAt(points, w, e, hi);
            Check(rLo < target && target < rHi, F("{0}: {1} brackets the split on the segment - R {2:0.0000} at DEM's point, {3:0.0000} at REP's", label, name, rLo, rHi));
            for (int i = 0; i < 80; i++) { double mid = 0.5 * (lo + hi); if (RepAt(points, w, e, mid) < target) { lo = mid; } else { hi = mid; } }
            double place = 0.5 * (lo + hi);
            Fit fit = Describe(points, w, name, Along(points, e, place), place);
            Check(Math.Abs(fit.Split - target) < 1e-9, F("{0}: {1} gives the split to 1e-9 ({2:0.0e+0})", label, name, Math.Abs(fit.Split - target)));
            return fit;
        }

        private static double Median(List<double> values)
        {
            List<double> v = values.OrderBy(x => x).ToList();
            return v.Count % 2 == 1 ? v[v.Count / 2] : 0.5 * (v[v.Count / 2 - 1] + v[v.Count / 2]);
        }

        /// <summary>The electorate with <paramref name="e"/>'s σ and τ, its mean at <paramref name="place"/> on the segment from DEM's point to REP's.</summary>
        private static VoteModel.Electorate Along(VoteModel.PartyPoint[] points, VoteModel.Electorate e, double place)
            => new VoteModel.Electorate(points[1].Econ + place * (points[0].Econ - points[1].Econ), points[1].Soc + place * (points[0].Soc - points[1].Soc), e.Sigma, e.Tau);

        private static double RepAt(VoteModel.PartyPoint[] points, double w, VoteModel.Electorate e, double place) => VoteModel.PredictShares(points, Along(points, e, place), w)[0];

        /// <summary>A fit's split, its slide along the dividing line, the split's answer to DEM's point moved one unit toward REP's, and the midway
        /// unit's share.</summary>
        private static Fit Describe(VoteModel.PartyPoint[] points, double w, string name, VoteModel.Electorate e, double place)
        {
            var fit = new Fit { Name = name, Electorate = e, Place = place, Split = VoteModel.PredictShares(points, e, w)[0] };
            // the line dividing the parties is perpendicular to the gradient of the choice index, (w·ΔE, (1-w)·ΔS)
            double gx = w * (points[0].Econ - points[1].Econ), gy = (1.0 - w) * (points[0].Soc - points[1].Soc), norm = Math.Sqrt(gx * gx + gy * gy);
            double tx = -gy / norm, ty = gx / norm;
            double Moved(double k) => VoteModel.PredictShares(points, new VoteModel.Electorate(e.MuEcon + k * tx, e.MuSoc + k * ty, e.Sigma, e.Tau), w)[0];
            fit.SlideOne = 100.0 * Math.Max(Math.Abs(Moved(1.0) - fit.Split), Math.Abs(Moved(-1.0) - fit.Split));
            fit.SlideTwo = 100.0 * Math.Max(Math.Abs(Moved(2.0) - fit.Split), Math.Abs(Moved(-2.0) - fit.Split));
            // a nominee off the party's point: DEM's point one unit along the segment toward REP's
            double dx = points[0].Econ - points[1].Econ, dy = points[0].Soc - points[1].Soc, length = Math.Sqrt(dx * dx + dy * dy);
            var movedDem = new[] { points[0], new VoteModel.PartyPoint(points[1].Name, points[1].Econ + dx / length, points[1].Soc + dy / length) };
            fit.MovedPoint = 100.0 * (VoteModel.PredictShares(movedDem, e, w)[0] - fit.Split);
            var withMidway = new[] { points[0], points[1], new VoteModel.PartyPoint("midway", 0.5 * (points[0].Econ + points[1].Econ), 0.5 * (points[0].Soc + points[1].Soc)) };
            fit.Third = VoteModel.PredictShares(withMidway, e, w)[2];
            return fit;
        }

        // ---- the data: the generated catalog's sums, in the roster's order (REP, DEM), as shares of all votes cast

        private static double[] SharesOf(Contest contest, int year)
        {
            long r = 0, d = 0, t = 0;
            if (contest == Contest.House) { foreach (var h in UsPresidentialReturns.House) { if (h.Year == year) { r += h.VotesR; d += h.VotesD; t += h.VotesTotal; } } }
            else { foreach (var s in UsPresidentialReturns.States) { if (s.Year == year) { r += s.VotesR; d += s.VotesD; t += s.VotesTotal; } } }
            if (t <= 0) { throw new InvalidOperationException(F("the catalog holds no {0} vote of {1}", contest, year)); }
            return new[] { (double)r / t, (double)d / t };
        }

        private static double TwoParty(double[] shares) => shares[0] / (shares[0] + shares[1]);

        // ---- the eve world: built as a new game builds one, the epoch first; one day stepped by the game's loop; the record read over the term

        private sealed class Eve
        {
            public DateTime Epoch, Day, PreviousElection, GovernmentFrom;
            public DateTime? TookOffice;
            public PerceivedPerformance.TermReading Term;
            public Dictionary<string, double> Shift, Magnitudes;
            public WorldClock.ExecutiveKind Kind;
            public string PmParty, Executive, LargestParty;
            public bool SeatedOfRecord;
        }

        /// <summary>The world on the eve of <paramref name="year"/>'s House election of record (the chamber that election seated: its election day less
        /// one), stepped to the election day. Seeded, the RNG and the energy state put back, the host destroyed, the epoch restored. Read with it, to
        /// hold the reading to the record: the House the previous House election seated (its seats as the record gives them), and the first day of the
        /// government of record the world seats.</summary>
        private static Eve ReadEve(int year)
        {
            IReadOnlyList<WorldClock.ChamberOfRecord> chambers = WorldClock.Chambers(CountryId.USA);
            int at = chambers.ToList().FindIndex(ch => ch.ElectionDay.Year == year);
            if (at < 1) { throw new InvalidOperationException(F("no House election of record in {0} with one before it", year)); }
            WorldClock.ChamberOfRecord chamber = chambers[at], previous = chambers[at - 1];
            var eve = new Eve { Epoch = chamber.ElectionDay.AddDays(-1), PreviousElection = previous.ElectionDay };
            GameObject host = null;
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            try
            {
                using (SimulationManager.EpochScope())
                {
                    SimulationRandom.Seed(777);
                    EnergyMarket.ResetCalibration();
                    SimulationManager.SetEpoch(eve.Epoch);   // before the world exists - SetEpoch's own contract, and a new game's order
                    World world = WorldFactory.CreateDefault();
                    host = new GameObject("UsNationalVoteCheck");
                    var sim = host.AddComponent<SimulationManager>();
                    sim.SetWorld(world);
                    sim.AdvanceDay();
                    Country usa = world.GetCountry(CountryId.USA);
                    eve.Day = sim.CurrentDate;
                    eve.TookOffice = EconomicVote.TookOffice(usa);
                    eve.Shift = EconomicVote.RecordOverTerm(usa, sim.CurrentDate, out eve.Term);
                    eve.Magnitudes = EconomicVote.Magnitudes(usa);
                    eve.Kind = usa.Government.Kind;
                    eve.PmParty = usa.Government.PmParty;
                    eve.Executive = usa.Government.Executive;
                    eve.LargestParty = usa.ParliamentSeats.OrderByDescending(kv => kv.Value).First().Key;
                    Dictionary<string, int> ofRecord = PartySystems.InitialSeats(CountryId.USA, previous.Vintage);
                    eve.SeatedOfRecord = previous.ElectionDay < chamber.ElectionDay && ofRecord != null && ofRecord.Count == usa.ParliamentSeats.Count
                                         && ofRecord.All(kv => usa.ParliamentSeats.TryGetValue(kv.Key, out int n) && n == kv.Value);
                    if (WorldClock.TryGovernmentAt(CountryId.USA, eve.Epoch, out WorldClock.GovernmentOfRecord government)) { eve.GovernmentFrom = government.From; }
                }
            }
            finally
            {
                if (host != null) { UnityEngine.Object.DestroyImmediate(host); }
                EnergyMarket.ResetTurnState();
                SimulationRandom.RestoreState(seedWas, drawsWas);
            }

            return eve;
        }

        // ---- the US model card's second block (docs/reference/US_ELECTIONS.md) - written by WriteReadings

        private const string CardRelative = "docs/reference/US_ELECTIONS.md";
        private const string CardStamp = "<!-- GENERATED by PoliSim.EditorTools.UsNationalVoteCheck.WriteReadings. DO NOT EDIT BY HAND. source-digest: ";
        private const string CardEnd = "<!-- END GENERATED -->";

        private static string CardPath() => Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), CardRelative);

        private static string Digest(string text)
        {
            using (var sha = SHA256.Create()) { return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", string.Empty).ToLowerInvariant(); }
        }

        private static string CardBlock(List<string> lines, string digest)
        {
            var block = new StringBuilder(CardStamp).Append(digest).Append(" -->\n");
            foreach (string line in lines) { block.Append(line).Append('\n'); }
            return block.Append(CardEnd).ToString();
        }

        private static string CardBlockOf(string card)
        {
            string text = card.Replace("\r\n", "\n");
            int start = text.IndexOf(CardStamp, StringComparison.Ordinal);
            int end = start < 0 ? -1 : text.IndexOf(CardEnd, start, StringComparison.Ordinal);
            return end < 0 ? null : text.Substring(start, end + CardEnd.Length - start);
        }

        private static string Pct(double share) => F("{0:0.00} %", 100.0 * share);
        private static string Pp(double points) => F("{0:+0.00;-0.00}", points);
        private static string Title(string surname) => surname.Substring(0, 1) + surname.Substring(1).ToLowerInvariant();
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
