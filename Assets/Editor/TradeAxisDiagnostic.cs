using System;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// C-B3 / R-CL2 — **what the openness axis actually changed, per country, and where it could not
    /// be used.**
    ///
    /// Pass 6 deferred the Trade bill's own axis to *"where real parties land"*, and recorded that
    /// reading the FISCAL axis for a tariff bill was a stated stand-in until then. R-CL2 ruled CHES
    /// `eu_position` in as the openness axis. This measures the consequence rather than assuming it:
    /// for each country it prints the seat-weighted alignment a tariff RISE and a tariff CUT get on
    /// each axis, the pass/fail verdict each produces, and **whether the verdict moved**.
    ///
    /// <para>⚠ <b>THE EVIDENCE THIS ITEM RESTS ON IS HERE, NOT IN A TRAJECTORY DIFF.</b>
    /// `TrajectoryBaselineDump` passes no bills — its own header states the idiom, *"no player
    /// country, no bills"* — so the no-policy trajectories are predicted byte-identical and that
    /// diff is CONTAINMENT evidence only. Reading a clean trajectory diff as proof that a
    /// vote-scoring change is safe is exactly the fallacy W-G2 recorded against itself. The
    /// load-bearing evidence is the table below.</para>
    ///
    /// <para>⚠ <b>The USA is the stated carve-out.</b> GPS 2019 has no EU item, so no US party carries
    /// an openness position and the axis measures zero seats of the House. The alignment falls back
    /// to fiscal — the alternative, scoring the chamber at zero, would make every US tariff bill fail
    /// for want of DATA rather than for want of votes — and `TradeAxisAvailable` reports false so a
    /// screen can say which axis produced the verdict.</para>
    /// </summary>
    public static class TradeAxisDiagnostic
    {
        /// <summary>A tariff RISE, in tariff-rate points, and its mirror. The magnitude is irrelevant to
        /// the verdict — `WouldBillPass` reads the SIGN only — so ±1 is used and stated rather than a
        /// figure that might read as a calibrated policy.</summary>
        private const float Rise = 1f;
        private const float Cut = -1f;

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-B3 / R-CL2: the Trade bill's own axis, measured per country ===\n");
            sb.Append("    Sign only: WouldBillPass reads the direction's SIGN, so +1 is any tariff rise\n");
            sb.Append("    and -1 any cut. Alignment > 0 passes.\n\n");

            World world = WorldFactory.CreateDefault();
            int failures = 0;
            int moved = 0;

            sb.Append("    country   axis available   RISE fiscal -> trade      CUT fiscal -> trade     verdict moved?\n");

            foreach (Country country in world.Countries)
            {
                bool available = ParliamentSystem.TradeAxisAvailable(country);

                float riseFiscal = ParliamentSystem.GetSeatWeightedAlignment(country, Rise, BillAxis.Fiscal);
                float riseTrade = ParliamentSystem.GetSeatWeightedAlignment(country, Rise, BillAxis.Trade);
                float cutFiscal = ParliamentSystem.GetSeatWeightedAlignment(country, Cut, BillAxis.Fiscal);
                float cutTrade = ParliamentSystem.GetSeatWeightedAlignment(country, Cut, BillAxis.Trade);

                bool riseWasPass = riseFiscal > 0f, riseNowPass = riseTrade > 0f;
                bool cutWasPass = cutFiscal > 0f, cutNowPass = cutTrade > 0f;
                bool changed = riseWasPass != riseNowPass || cutWasPass != cutNowPass;
                if (changed) { moved++; }

                sb.Append(F("    {0,-9} {1,-16} {2,7:+0.000;-0.000} -> {3,7:+0.000;-0.000} {4,-4}  {5,7:+0.000;-0.000} -> {6,7:+0.000;-0.000} {7,-4}  {8}\n",
                    country.Id, available ? "yes" : "NO - fiscal",
                    riseFiscal, riseTrade, riseNowPass ? "PASS" : "fail",
                    cutFiscal, cutTrade, cutNowPass ? "PASS" : "fail",
                    changed ? "** MOVED **" : "-"));

                // ⚠ The carve-out is ASSERTED, not just described: where the axis is unavailable the
                // trade alignment must be EXACTLY the fiscal one, or the fallback is not a fallback.
                if (!available && !Mathf.Approximately(riseFiscal, riseTrade))
                {
                    failures++;
                    Debug.LogError(F("C-B3: {0} has no openness position on any seat, so the trade axis must fall back to fiscal exactly - got {1:F6} against {2:F6}.",
                        country.Id, riseTrade, riseFiscal));
                }

                // And where it IS available, the two axes must be reading different things - otherwise
                // the ruling bought nothing and the record should not claim it did.
                if (available && Mathf.Approximately(riseFiscal, riseTrade))
                {
                    sb.Append(F("      note: {0}'s two axes agree to within float precision on a rise.\n", country.Id));
                }
            }

            sb.Append("\n--- Coverage of the openness axis, per country ---\n");
            foreach (Country country in world.Countries)
            {
                int chamber = PartySystems.ChamberSeats(country.Id);
                int measured = 0, seatsWithFiscal = 0;
                foreach (PoliticalParty p in PartySystems.For(country.Id))
                {
                    int seats = country.ParliamentSeats.TryGetValue(p.Abbrev, out int s) ? s : 0;
                    if (p.HasEuPosition) { measured += seats; }
                    if (p.HasPosition) { seatsWithFiscal += seats; }
                }

                sb.Append(F("    {0,-9} openness {1,4} / {2,4} seats ({3,5:F1} %)   fiscal {4,4} / {2,4} seats\n",
                    country.Id, measured, chamber, chamber > 0 ? 100f * measured / chamber : 0f, seatsWithFiscal));
            }

            sb.Append("\n--- What this does NOT prove ---\n");
            sb.Append("    The no-policy trajectory dump passes no bills, so it cannot exercise this change\n");
            sb.Append("    at all; a byte-identical trajectory diff is CONTAINMENT evidence and nothing more.\n");
            sb.Append("    The table above is the load-bearing evidence. (W-G2 recorded this exact fallacy\n");
            sb.Append("    against itself: 'it is not evidence the parliament change is safe'.)\n");

            sb.Append(F("\n=== TradeAxisDiagnostic: {0}; the verdict moved in {1} of {2} countries ===\n",
                failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILURE(S)", moved, world.Countries.Count));

            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
