using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// K275 (Elias, 2026-10-29; answering K272): IN THE PLAYER'S BOOK ONLY THE THRESHOLD-CREEP PART OF THE INCOME TAX'S YIELD IS CAPPED; THE PLAYER'S OWN RATE
    /// MOVES KEEP THEIR FULL EFFECT. The yield ratio the revenue reads is the ratio form times <see cref="TaxSchedule.CreepCapFactor"/>: the statute's creep
    /// (<see cref="TaxSchedule.StatuteCreep"/>, read at the statute's own rates) held to its bound (<see cref="TaxSchedule.CreepBound"/>, the statute's top
    /// marginal rate over the lever's seeded rate, DECLARED). It fails while any of its five statements is false:
    /// (1) at the seed the creep is exactly one and the revenue's ratio is the ratio form's to the bit, for every country;
    /// (2) the cap binds in no AI book, and an AI book's creep stays within a thousandth of one - the five AI books of each of the six shared no-policy
    ///     centuries (`NoPolicyCentury.For`), every year;
    /// (3) Poland's player book at unchanged rates stays bounded at years 40 and 100: the cap binds there (the ratio form's creep is past its bound, so the case
    ///     discriminates), the revenue's ratio is the ratio form's times the bound over the creep, and the take on the anchored base (the seeded rate times
    ///     the ratio) is at or under the statute's top marginal rate, where the ratio form's is past it;
    /// (4) the player's own moves keep their full effect at the current position - the lever one point up and one down and the top band one point up on its
    ///     own dial move the revenue's ratio by the same proportion as the ratio form's, at the seed's year and at years 40 and 100 (no kink: review 4's N6);
    /// (5) the top band at 0 leaves the tax positive at every probed year (review 3's H1);
    /// (6) K275's review M1: the flat-equivalent rate the behavioural terms read (`TaxSchedule.EffectiveRate`: participation, the Gini, a bill's hike) takes
    ///     the same factor - in Poland's and Germany's player books, every year the cap binds it is at or under the statute's top marginal rate, every year it
    ///     does not it is the uncapped rate to the bit, and at the probed years the three moves change the yield it stands on by the ratio form's proportion.
    /// It reads the shared centuries the long-run diagnostics read, so in the simulation bar it costs no run of its own.
    /// </summary>
    public static class IncomeTaxCreepCapDiagnostic
    {
        /// <summary>[AUTHORED] CONVENTION: the years statement (3) holds Poland's player book at.</summary>
        private static readonly int[] HeldYears = { 40, 100 };

        /// <summary>CONVENTION: the relative tolerance a proportional identity is held to - the two sides are one double product apart.</summary>
        private const double Tolerance = 1e-9;

        /// <summary>CONVENTION: the same identity on the flat-equivalent rate, which is stored as a float - a float's width on a yield of some ten points.</summary>
        private const double FloatTolerance = 1e-5;

        /// <summary>[AUTHORED] CONVENTION: how far an AI book's creep may stand from one - the lift is a product of float ratios (measured within 1.0003).</summary>
        private const double AiCreepSlack = 1e-3;

        /// <summary>Review 2's L-n1: whether a flat-equivalent rate stands on the uncapped yield times the revenue's factor, within the float tolerance.</summary>
        private static bool FactorsAgree(float effective, NoPolicyCentury.Year y, double revenueFactor)
        {
            double behavioural = (effective - y.IncomeTaxSeedRate + y.IncomeTaxSeedAer) / (y.IncomeTaxEffectiveUncapped - y.IncomeTaxSeedRate + y.IncomeTaxSeedAer);
            return Math.Abs(behavioural / revenueFactor - 1.0) <= FloatTolerance;
        }

        public static void Run()
        {
            CheckExit.ArmLogFold();
            bool ok = true;
            var sb = new StringBuilder();
            sb.Append("=== INCOME TAX CREEP CAP (K275): only the statute's threshold creep is capped, at the statute's top rate over the seeded lever; the player's own moves keep their full effect ===\n");
            CountryId[] order = { CountryId.Sweden, CountryId.Germany, CountryId.France, CountryId.Italy, CountryId.Poland, CountryId.USA };

            // (1) the seed
            sb.Append("\n    1. THE SEED - the creep, the bound, the factor, and the revenue's ratio against the ratio form's\n");
            SimulationRandom.Seed(NoPolicyCentury.Seed);
            EnergyMarket.ResetCalibration();
            World world = WorldFactory.CreateDefault();
            foreach (CountryId id in order)
            {
                Country c = world.GetCountry(id);
                TaxLine line = Find(c);
                if (line == null) { ok = false; Debug.LogError($"CREEP CAP: {id} has no income tax line."); continue; }
                double creep = TaxSchedule.StatuteCreep(c, line), bound = TaxSchedule.CreepBound(c, line), factor = TaxSchedule.CreepCapFactor(c, line);
                double ratio = TaxSchedule.YieldRatio(c, line, line.Rate), form = TaxSchedule.YieldRatioUncapped(c, line, line.Rate);
                sb.Append(F("    {0,-8} {1} · seed rate {2:F3} % · top rate {3:F3} % · bound {4:F4} · creep {5:R} · factor {6:R} · ratio {7:R} (the form's {8:R})\n",
                    id, TaxSchedule.CreepCapped(id) ? "CAPPED  " : TaxSchedule.Responds(id) ? "UNCAPPED" : "FLAT    ", TaxSchedule.RateSeedOf(line), TaxSchedule.StatuteTopMarginalRate(id), bound, creep, factor, ratio, form));
                if (creep != 1.0 || factor != 1.0 || ratio != form) { ok = false; Debug.LogError($"CREEP CAP: at the seed {id}'s creep reads {creep:R} and its factor {factor:R} (the ratio {ratio:R} against the form's {form:R}) - the cap binds, or the creep is not one, at the seed."); }
                if (TaxSchedule.CreepCapped(id) && !(bound > 1.0)) { ok = false; Debug.LogError($"CREEP CAP: {id}'s bound is {bound:R} - at or under one, the first year's creep would bind (the seeded lever {TaxSchedule.RateSeedOf(line):F3} at or above the statute's top rate {TaxSchedule.StatuteTopMarginalRate(id):F3})."); }
            }

            // (2) the AI books
            sb.Append("\n    2. THE AI BOOKS - the five AI countries of each shared no-policy century: the largest creep over the century and the years the cap bound\n");
            foreach (CountryId player in order)
            {
                IReadOnlyList<NoPolicyCentury.Year> run = NoPolicyCentury.For(player);
                double max = 0; int binds = 0, at = 0;
                for (int i = 0; i < run.Count; i++) { binds += run[i].AiCreepBinds; if (run[i].AiCreepMax > max) { max = run[i].AiCreepMax; at = i + 1; } }
                sb.Append(F("    the {0,-8} century: the AI books' largest creep {1:F6} (year {2}) · binding country-years {3}\n", player, max, at, binds));
                if (binds > 0) { ok = false; Debug.LogError($"CREEP CAP: in the {player} century the cap bound {binds} AI country-years - an AI government re-indexes its thresholds (K271), so its creep must stay inside the bound."); }
                if (!(max < 1.0 + AiCreepSlack)) { ok = false; Debug.LogError($"CREEP CAP: in the {player} century an AI book's creep reached {max:F6} (year {at}) - K271's lift no longer keeps its thresholds on its incomes."); }
            }

            // (3) Poland's player book at unchanged rates
            sb.Append("\n    3. POLAND'S PLAYER BOOK at unchanged rates - the creep against its bound, the revenue's ratio against the form's, the take on the anchored base against the statute's top rate\n");
            IReadOnlyList<NoPolicyCentury.Year> pl = NoPolicyCentury.For(CountryId.Poland);
            double plTop = TaxSchedule.StatuteTopMarginalRate(CountryId.Poland);
            int firstBind = 0;
            for (int i = 0; i < pl.Count; i++) { if (pl[i].IncomeTaxCreep > pl[i].IncomeTaxCreepBound) { firstBind = i + 1; break; } }
            sb.Append(F("    the cap first binds in year {0} (0: never)\n", firstBind));
            foreach (int year in HeldYears)
            {
                NoPolicyCentury.Year y = pl[year - 1];
                double take = y.IncomeTaxSeedRate * y.IncomeTaxRatio, formTake = y.IncomeTaxSeedRate * y.IncomeTaxRatioUncapped;
                double want = y.IncomeTaxRatioUncapped * y.IncomeTaxCreepBound / y.IncomeTaxCreep;
                sb.Append(F("    year {0,3}: creep {1:F4} against the bound {2:F4} · ratio {3:F4} (the form's {4:F4}) · take {5:F2} % against the top rate {6:F2} % (the form's {7:F2} %)\n",
                    year, y.IncomeTaxCreep, y.IncomeTaxCreepBound, y.IncomeTaxRatio, y.IncomeTaxRatioUncapped, take, plTop, formTake));
                if (!(y.IncomeTaxCreep > y.IncomeTaxCreepBound) || !(formTake > plTop)) { ok = false; Debug.LogError($"CREEP CAP: Poland's player book at year {year} reads a creep of {y.IncomeTaxCreep:F4} against its bound {y.IncomeTaxCreepBound:F4} and a form's take of {formTake:F2} against the top rate {plTop:F2} - the case no longer reaches the cap, so it proves nothing."); }
                if (Math.Abs(y.IncomeTaxRatio / want - 1.0) > Tolerance) { ok = false; Debug.LogError($"CREEP CAP: Poland's ratio at year {year} is {y.IncomeTaxRatio:R} where the form times the bound over the creep is {want:R}."); }
                if (!(take <= plTop)) { ok = false; Debug.LogError($"CREEP CAP: Poland's player book at unchanged rates takes {take:F2} % of its anchored base at year {year}, past the statute's top rate {plTop:F2} % - the creep is not bounded."); }
            }

            // (4) the player's own moves, and (5) the top band at 0
            sb.Append("\n    4. THE PLAYER'S OWN MOVES at the current position (Poland) - the proportion each move gives the revenue's ratio against the form's; 5. the top band at 0\n");
            foreach (int year in NoPolicyCentury.IncomeTaxProbeYears)
            {
                NoPolicyCentury.Year y = pl[year - 1];
                bool binding = y.IncomeTaxCreep > y.IncomeTaxCreepBound;
                (string Name, double Capped, double Form)[] moves =
                {
                    ("lever +1", y.TaxRiseCapped, y.TaxRiseUncapped), ("lever -1", y.TaxCutCapped, y.TaxCutUncapped),
                    ("top band +1", y.TaxBandCapped, y.TaxBandUncapped), ("top band at 0", y.TaxTopZeroCapped, y.TaxTopZeroUncapped),
                };
                sb.Append(F("    year {0,3} ({1}):", year, binding ? "the cap binds" : "inside the bound"));
                foreach ((string name, double capped, double form) in moves)
                {
                    double moved = capped / y.IncomeTaxRatio, formMoved = form / y.IncomeTaxRatioUncapped;
                    sb.Append(F(" {0} ×{1:F6} (form ×{2:F6})", name, moved, formMoved));
                    if (double.IsNaN(capped) || double.IsNaN(form) || Math.Abs(moved / formMoved - 1.0) > Tolerance) { ok = false; Debug.LogError($"CREEP CAP: in Poland's player book at year {year} the move '{name}' gives the revenue's ratio ×{moved:R} where the form gives ×{formMoved:R} - the player's own move does not keep its full effect."); }
                    if (!binding && capped != form) { ok = false; Debug.LogError($"CREEP CAP: in Poland's player book at year {year}, inside the bound, the move '{name}' reads {capped:R} against the form's {form:R} - the cap acts where it does not bind."); }
                }
                double kink = (y.TaxRiseCapped - y.IncomeTaxRatio) / (y.IncomeTaxRatio - y.TaxCutCapped), formKink = (y.TaxRiseUncapped - y.IncomeTaxRatioUncapped) / (y.IncomeTaxRatioUncapped - y.TaxCutUncapped);
                sb.Append(F(" · rise over cut {0:F9} (form {1:F9})\n", kink, formKink));
                if (Math.Abs(kink - formKink) > 1e-6) { ok = false; Debug.LogError($"CREEP CAP: in Poland's player book at year {year} a point's rise over a point's cut reads {kink:R} against the form's {formKink:R} - the lever is kinked at the current position (review 4's N6)."); }
                if (!(y.TaxTopZeroCapped > 0.0)) { ok = false; Debug.LogError($"CREEP CAP: in Poland's player book at year {year} the top band at 0 leaves a ratio of {y.TaxTopZeroCapped:R} - the income tax is deleted (review 3's H1)."); }
            }

            // (6) the flat-equivalent rate the behavioural terms read
            sb.Append("\n    6. THE BEHAVIOURAL RATE (review M1) - the flat-equivalent rate against the statute's top rate where the cap binds, the uncapped rate where it does not, and the player's moves on the yield it stands on\n");
            foreach (CountryId player in new[] { CountryId.Poland, CountryId.Germany })
            {
                IReadOnlyList<NoPolicyCentury.Year> run = NoPolicyCentury.For(player);
                double top = TaxSchedule.StatuteTopMarginalRate(player);
                int bound = 0, over = 0, inside = 0, apart = 0, unpinned = 0, planted = 0;
                for (int i = 0; i < run.Count; i++)
                {
                    NoPolicyCentury.Year y = run[i];
                    if (y.IncomeTaxCreep > y.IncomeTaxCreepBound)
                    {
                        bound++;
                        if (!(y.IncomeTaxEffective <= top)) { over++; }
                        // review 2's L-n1: the behavioural yield is the revenue's factor times the uncapped one - pinned, not just bounded
                        double revenueFactor = y.IncomeTaxRatio / y.IncomeTaxRatioUncapped;
                        if (!FactorsAgree(y.IncomeTaxEffective, y, revenueFactor)) { unpinned++; }
                        // and the check discriminates: a squared factor, and a rate pinned at the seed, both stay under the top rate and must both fail it
                        double formYield = y.IncomeTaxEffectiveUncapped - y.IncomeTaxSeedRate + y.IncomeTaxSeedAer;
                        float squared = (float)(y.IncomeTaxSeedRate + formYield * revenueFactor * revenueFactor - y.IncomeTaxSeedAer);
                        if (FactorsAgree(squared, y, revenueFactor) || FactorsAgree(y.IncomeTaxSeedRate, y, revenueFactor)) { planted++; }
                    }
                    else { inside++; if (y.IncomeTaxEffective != y.IncomeTaxEffectiveUncapped) { apart++; } }
                }
                sb.Append(F("    {0,-8} {1} years bound, {2} of them past the top rate {3:F2} % · {4} years inside, {5} of them off the uncapped rate · {6} bound years off the revenue's factor, {7} planted (a squared factor, a rate at the seed) passing", player, bound, over, top, inside, apart, unpinned, planted));
                foreach (int year in HeldYears) { NoPolicyCentury.Year y = run[year - 1]; sb.Append(F(" · year {0}: {1:F2} % (uncapped {2:F2} %)", year, y.IncomeTaxEffective, y.IncomeTaxEffectiveUncapped)); }
                sb.Append('\n');
                if (over > 0) { ok = false; Debug.LogError($"CREEP CAP: {player}'s player book reads a flat-equivalent rate past the statute's top rate {top:F2} % in {over} years the cap binds - the behavioural terms read creep the revenue no longer collects (review M1)."); }
                if (apart > 0) { ok = false; Debug.LogError($"CREEP CAP: {player}'s player book reads a flat-equivalent rate off the uncapped one in {apart} years the cap does not bind - the cap acts where it does not bind."); }
                if (unpinned > 0) { ok = false; Debug.LogError($"CREEP CAP: {player}'s player book reads a behavioural yield off the revenue's factor in {unpinned} years the cap binds - the flat-equivalent rate is not read through the same factor (review 2's L-n1)."); }
                if (planted > 0) { ok = false; Debug.LogError($"CREEP CAP: in {player}'s player book a planted squared factor or a rate pinned at the seed passed the factor check in {planted} years - the check does not discriminate."); }
                if (player == CountryId.Poland && bound == 0) { ok = false; Debug.LogError("CREEP CAP: Poland's player book never reaches the cap, so statement (6) proves nothing."); }
                if (player != CountryId.Poland) { continue; }
                foreach (int year in NoPolicyCentury.IncomeTaxProbeYears)
                {
                    NoPolicyCentury.Year y = run[year - 1];
                    double baseCapped = y.IncomeTaxEffective - y.IncomeTaxSeedRate + y.IncomeTaxSeedAer, baseForm = y.IncomeTaxEffectiveUncapped - y.IncomeTaxSeedRate + y.IncomeTaxSeedAer;   // the yield each stands on
                    (string Name, float Capped, float Form)[] moves = { ("lever +1", y.EffRiseCapped, y.EffRiseUncapped), ("lever -1", y.EffCutCapped, y.EffCutUncapped), ("top band +1", y.EffBandCapped, y.EffBandUncapped) };
                    sb.Append(F("    Poland   year {0,3}:", year));
                    foreach ((string name, float capped, float form) in moves)
                    {
                        double moved = (capped - y.IncomeTaxSeedRate + y.IncomeTaxSeedAer) / baseCapped, formMoved = (form - y.IncomeTaxSeedRate + y.IncomeTaxSeedAer) / baseForm;
                        sb.Append(F(" {0} ×{1:F6} (form ×{2:F6})", name, moved, formMoved));
                        if (float.IsNaN(capped) || float.IsNaN(form) || Math.Abs(moved / formMoved - 1.0) > FloatTolerance) { ok = false; Debug.LogError($"CREEP CAP: in Poland's player book at year {year} the move '{name}' moves the behavioural rate's yield ×{moved:R} where the form moves it ×{formMoved:R} - the player's move does not keep its proportion."); }
                    }
                    sb.Append('\n');
                }
            }

            Debug.Log(sb.ToString());
            Debug.Log(ok ? "=== IncomeTaxCreepCapDiagnostic: ALL ASSERTIONS PASS ===" : "=== IncomeTaxCreepCapDiagnostic: FAILED ===");
            CheckExit.Finish(ok ? 0 : 1);
        }

        private static TaxLine Find(Country c) { foreach (TaxLine l in c.TaxLines) { if (l.Type == TaxType.IncomeTax && l.IsImplemented) { return l; } } return null; }
        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
