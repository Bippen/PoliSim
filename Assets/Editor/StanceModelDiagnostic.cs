using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// P3-A2 (2026-09-03): the stance model's "done when", asserted on the seeded world. Five drafts of ONE
    /// magnitude, one per bill category - a spending cut (spendvtax toward the taxes end, with a cut on a line
    /// old voters depend on), a labour bill (lrecon toward the state), a crime bill (galtan toward the
    /// authoritarian end), a sector bill (deregulation toward deregulated), a tariff rise (openness toward
    /// closed) - scored on Sweden with the player seated in the formed cabinet's anchor party, so the
    /// government terms are live. Asserted: the same magnitude produces DIFFERENT for / undecided / against
    /// counts across the five (no two identical); at least one cabinet or support partner splits from the
    /// anchor on at least one draft (a partner refusing a far bill); every party's side is the model's own
    /// alignment against the band; and the USA - no formation, no spendvtax - scores every draft on the axes
    /// it has, with the fallbacks printed. Deterministic, no stream drawn.
    /// </summary>
    public static class StanceModelDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            var failures = new List<string>();
            World world = WorldFactory.CreateDefault();
            Country sweden = world.GetCountry(CountryId.Sweden);
            Country usa = world.GetCountry(CountryId.USA);

            // The player in the cabinet's anchor party, so cohesion and the opposition's line are live.
            bool formed = GovernmentFormation.TryGovernment(sweden, out IReadOnlyList<string> cabinet, out IReadOnlyList<string> support);
            string anchor = null;
            int anchorSeats = -1;
            foreach (string abbrev in cabinet)
            {
                int seats = sweden.ParliamentSeats.TryGetValue(abbrev, out int s) ? s : 0;
                if (seats > anchorSeats) { anchorSeats = seats; anchor = abbrev; }
            }
            sweden.PlayerPartyAbbrev = anchor;
            sb.Append(string.Format(CultureInfo.InvariantCulture, "=== StanceModelDiagnostic (P3-A2) ===\n    Sweden's formed government: {0}; cabinet {1}; support {2}; the player seated in {3}.\n",
                formed ? "formed" : "NONE", string.Join("+", cabinet), support.Count > 0 ? string.Join("+", support) : "none", anchor ?? "no party"));
            if (!formed || anchor == null) { failures.Add("no government forms from Sweden's seeded chamber - the government terms cannot be exercised"); }

            const float Magnitude = 20f;
            var drafts = new List<(string Name, BillConcern Concern)>
            {
                ("BUDGET: a spending cut", new BillConcern { Direction = -Magnitude }.Add(StanceAxis.SpendVsTax, Magnitude)),
                ("LABOUR: more support", new BillConcern { Direction = Magnitude }.Add(StanceAxis.LrEcon, -Magnitude)),
                ("CRIME: harsher sentencing", new BillConcern { Direction = -Magnitude }.Add(StanceAxis.Galtan, Magnitude)),
                ("SECTORS: deregulation", new BillConcern { Direction = -Magnitude }.Add(StanceAxis.Deregulation, Magnitude)),
                ("TRADE: a tariff rise", new BillConcern { Direction = Magnitude }.Add(StanceAxis.Openness, -Magnitude)),
            };
            drafts[0].Concern.Cuts.Add((SpendingCategory.SocialSecurity, null, 0.2f));

            var splits = new List<(string Name, int For, int Undecided, int Against)>();
            bool anyPartnerSplit = false;
            foreach ((string name, BillConcern concern) in drafts)
            {
                int forSeats = 0, undecided = 0, against = 0, chamber = 0;
                int anchorSide = 0;
                var partnerSplits = new List<string>();
                List<PartyStance> stances = StanceModel.Stances(sweden, concern);
                foreach (PartyStance st in stances) { if (st.Party.Abbrev == anchor) { anchorSide = st.Side; } }
                sb.Append(string.Format(CultureInfo.InvariantCulture, "\n--- {0} (magnitude {1}) ---\n", name, Magnitude));
                foreach (PartyStance st in stances)
                {
                    chamber += st.Seats;
                    if (st.Side > 0) { forSeats += st.Seats; } else if (st.Side < 0) { against += st.Seats; } else { undecided += st.Seats; }
                    int expected = !st.Measured ? 0 : Mathf.Abs(st.Alignment) < StanceModel.UndecidedBand ? 0 : st.Alignment > 0f ? 1 : -1;
                    if (expected != st.Side) { failures.Add($"{name}: {st.Party.Abbrev}'s side {st.Side} is not its alignment {st.Alignment:F3} against the band"); }
                    bool partner = st.Party.Abbrev != anchor && (cabinet.Contains(st.Party.Abbrev) || support.Contains(st.Party.Abbrev));
                    if (partner && st.Side != anchorSide) { partnerSplits.Add(st.Party.Abbrev); anyPartnerSplit = true; }
                    sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-3} {1,3} seats  {2,-9} {3:+0.00;-0.00}  | {4}\n",
                        st.Party.Abbrev, st.Seats, st.Side > 0 ? "FOR" : st.Side < 0 ? "AGAINST" : st.Measured ? "UNDECIDED" : "UNMEASURED", st.Alignment, string.Join("; ", st.Reasons)));
                }
                float alignment = ParliamentSystem.GetSeatWeightedAlignment(sweden, concern);
                sb.Append(string.Format(CultureInfo.InvariantCulture, "    FOR {0}  UNDECIDED {1}  AGAINST {2}  of {3}  alignment {4:+0.000;-0.000}  {5}{6}\n",
                    forSeats, undecided, against, chamber, alignment, alignment > 0f ? "PASSES" : "FAILS",
                    partnerSplits.Count > 0 ? "  · partner(s) split from " + anchor + ": " + string.Join(", ", partnerSplits) : ""));
                if (chamber != PartySystems.ChamberSeats(CountryId.Sweden)) { failures.Add($"{name}: the sides list {chamber} seats, the chamber holds {PartySystems.ChamberSeats(CountryId.Sweden)}"); }
                splits.Add((name, forSeats, undecided, against));
            }

            for (int a = 0; a < splits.Count; a++)
            {
                for (int b = a + 1; b < splits.Count; b++)
                {
                    if (splits[a].For == splits[b].For && splits[a].Undecided == splits[b].Undecided && splits[a].Against == splits[b].Against)
                    {
                        failures.Add($"'{splits[a].Name}' and '{splits[b].Name}' produce the same split ({splits[a].For}/{splits[a].Undecided}/{splits[a].Against}) at one magnitude");
                    }
                }
            }
            if (!anyPartnerSplit) { failures.Add("no cabinet or support partner split from the anchor on any of the five drafts - the cohesion term never let a partner refuse"); }

            // The USA: no formation, no spendvtax, no EU item - the fallbacks must print, not fail.
            sb.Append("\n--- the USA on the same five (no formation, GPS-2019 lrecon/galtan only) ---\n");
            foreach ((string name, BillConcern concern) in drafts)
            {
                int forSeats = 0, undecided = 0, against = 0, unmeasured = 0;
                foreach (PartyStance st in StanceModel.Stances(usa, concern))
                {
                    if (!st.Measured) { unmeasured += st.Seats; } else if (st.Side > 0) { forSeats += st.Seats; } else if (st.Side < 0) { against += st.Seats; } else { undecided += st.Seats; }
                }
                sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-28} FOR {1,3}  UNDECIDED {2,3}  AGAINST {3,3}  UNMEASURED {4,3}\n", name, forSeats, undecided, against, unmeasured));
            }

            sb.Append("\n    The weights are §246's five [AUTHORED-DRAFT] constants; the positions CHES 2024 / GPS 2019; the salience EB105 / Gallup;\n");
            sb.Append("    Sweden's voter profile the ecological estimate from the 2022 valkrets returns over the 2024 pyramids. Nothing here is a roll call.\n");

            if (failures.Count == 0)
            {
                sb.Append("\n=== StanceModelDiagnostic: ALL ASSERTIONS PASS ===\n");
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
            }
            else
            {
                sb.Append($"\n=== StanceModelDiagnostic: {failures.Count} FAILURE(S) ===\n");
                foreach (string f in failures) { sb.Append("    ").Append(f).Append('\n'); }
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
            }
        }
    }
}
