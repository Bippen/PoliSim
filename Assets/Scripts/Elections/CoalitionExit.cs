using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// COAL-1 (RULINGS_2026-10-18 q1, q21; Elias's rulings K130-K133 of 2026-10-27, `docs/RULINGS_2026-10-23.md`): <b>AN AI COALITION PARTNER'S DECISION TO
    /// LEAVE</b> reads its own projected vote (A-LOOP, `docs/specs/A_LOOP_PLAN.md` §3 (2)): staying, its share is the prediction with the government's record
    /// shift as it stands (<see cref="EconomicVote.RecordPartsOverTerm"/>, its own shift among the governing parties'); leaving, it is opposition - its own
    /// shift is zero and it absorbs its part of the others' - one prediction each on the day (<see cref="NationalElection.TryPredictShares"/>). K130: it leaves
    /// when the share outside beats the share inside by more than one sigma_v (<see cref="FiscalVote.SwingSpread"/>, K64's robust scale of the incumbent's
    /// swing), tested at each budget vote, and never within <see cref="QuietDays"/> of a polling day. What the exit sets off is the manager's
    /// (`SimulationManager.CoalitionExit.cs`): the country's confidence procedure (K131, K132), else a minority whose next budget is voted on its new support.
    /// Deterministic: no stream is drawn.
    /// </summary>
    public static class CoalitionExit
    {
        /// <summary>K130 (Elias, 2026-10-27): "never within 90 days of a polling day" - read on both sides of one (DECLARED in the manager's comment).</summary>
        public const int QuietDays = 90;

        /// <summary>K130: the margin the share outside must beat the share inside by - one sigma_v, as a share fraction (K64's figure, FITTED).</summary>
        public static double Threshold => FiscalVote.SwingSpread;

        /// <summary>K130's test on the two projected shares (share fractions): leave when outside beats inside by more than <see cref="Threshold"/>.</summary>
        public static bool Leaves(double inside, double outside) => !double.IsNaN(inside) && !double.IsNaN(outside) && outside - inside > Threshold;

        /// <summary>K130: whether <paramref name="today"/> lies within <see cref="QuietDays"/> of either polling day given (MinValue: none known on that side).</summary>
        public static bool InQuietWindow(DateTime today, DateTime nextPolling, DateTime lastPolling)
        {
            if (nextPolling != DateTime.MinValue && (nextPolling.Date - today.Date).TotalDays <= QuietDays) { return true; }
            if (lastPolling != DateTime.MinValue && (today.Date - lastPolling.Date).TotalDays <= QuietDays) { return true; }
            return false;
        }

        /// <summary>
        /// A-LOOP §3 (2): the party's projected share inside the government and outside it, on <paramref name="on"/> - one prediction with the record
        /// shift as it stands, one with the party's own entry struck (it absorbs). False, NaN out, where no prediction can be made or the party has none.
        /// </summary>
        public static bool TryProjection(Country country, string party, DateTime on, out double inside, out double outside)
        {
            inside = double.NaN; outside = double.NaN;
            if (country == null || string.IsNullOrEmpty(party)) { return false; }
            if (_planted != null)
            {
                (double i, double o) = _planted(party);
                inside = i; outside = o;
                return !double.IsNaN(inside) && !double.IsNaN(outside);
            }
            EconomicVote.RecordParts parts = EconomicVote.RecordPartsOverTerm(country, on);
            if (!NationalElection.TryPredictShares(country.Id, out Dictionary<string, double> staying, parts.Total, on: on, played: country)) { return false; }
            var without = new Dictionary<string, double>(parts.Total);
            without.Remove(party);
            if (!NationalElection.TryPredictShares(country.Id, out Dictionary<string, double> leaving, without, on: on, played: country)) { return false; }
            if (!staying.TryGetValue(party, out double s) || !leaving.TryGetValue(party, out double l)) { return false; }
            inside = s; outside = l;
            return true;
        }

        private static Func<string, (double Inside, double Outside)> _planted;

        /// <summary>For the Editor's checks: every <see cref="TryProjection"/> inside the returned scope answers <paramref name="projection"/> (the party's
        /// share inside, outside), so each clause of K130 can be walked by name without staging an economy. Disposing the scope restores the model.</summary>
        public static IDisposable PlantProjection(Func<string, (double Inside, double Outside)> projection)
        {
            var restore = new PlantedScope(_planted);
            _planted = projection;
            return restore;
        }

        private sealed class PlantedScope : IDisposable
        {
            private readonly Func<string, (double Inside, double Outside)> _before;
            public PlantedScope(Func<string, (double Inside, double Outside)> before) { _before = before; }
            public void Dispose() { _planted = _before; }
        }
    }
}
