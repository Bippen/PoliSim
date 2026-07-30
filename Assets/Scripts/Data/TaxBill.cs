using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>One TaxType's requested standing values, as captured at the moment a TaxBill is introduced.</summary>
    public struct TaxBillLine
    {
        public bool IsImplemented;
        public float Rate;

        public TaxBillLine(bool isImplemented, float rate)
        {
            IsImplemented = isImplemented;
            Rate = rate;
        }
    }

    /// <summary>
    /// Political Systems Overhaul Part B PILOT (Tax Policy tab only), Master Sequence step 4: one
    /// omnibus bill bundling EVERY TaxType's current draft state (implement/remove flag plus rate) at
    /// the moment the player presses "Introduce Bill" - a single bill per introduction rather than one
    /// bill per tax line, matching the roadmap's own "introduce the current draft" framing (a real
    /// omnibus tax bill, not twelve separate ones moving through Parliament in parallel). Takes
    /// ParliamentSystem.BillDurationDays real in-game days to resolve (introduction -&gt; a fixed wait,
    /// standing in for the roadmap's "committee/debate" stage without modeling committee mechanics
    /// separately - a deliberately simple first pass), counted down once per simulated day
    /// (SimulationManager.AdvanceLegislativeDay), independent of the 121-day turn boundary. Only one
    /// bill may be pending per country at a time (SimulationManager.IntroduceTaxBill).
    /// </summary>
    public class TaxBill
    {
        public Dictionary<TaxType, TaxBillLine> Lines = new Dictionary<TaxType, TaxBillLine>();
        public int DaysRemaining;

        public TaxBill() { }

        public TaxBill(Dictionary<TaxType, TaxBillLine> lines, int daysRemaining)
        {
            Lines = lines;
            DaysRemaining = daysRemaining;
        }
    }
}
