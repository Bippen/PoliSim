using System.Collections.Generic;
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
    /// D-5 (a)'s guard — **the office test answers the question R-CL1 asked, and answers it differently
    /// for different chambers.**
    ///
    /// <para><b>THE ENUMERATION.</b> Every country, and every party in it as the player's party. For each
    /// pair: can a government be formed, is that party in its cabinet, and does the answer change when the
    /// chamber changes? Four assertions and one report.</para>
    ///
    /// <list type="number">
    /// <item><description>⚠ <b>THE TEST MUST DISCRIMINATE.</b> In at least one country some parties must
    /// be in office and others out. **A test that says everyone governs, or nobody does, is not a test** —
    /// it is a constant wearing a function's name, and it would end (or never end) every game
    /// identically.</description></item>
    /// <item><description><b>Sweden 2022 must seat the government Sweden actually formed.</b> The chamber
    /// is the real one and the declared red lines are sourced, so the cabinet must be **M+KD+L** with SD
    /// outside it. ⚠ This is the assertion that can catch a wrong answer, because the answer is a matter
    /// of public record rather than of the model's opinion.</description></item>
    /// <item><description><b>Support is not office.</b> In Sweden 2022, SD must be OUT of the cabinet —
    /// it supported the Tidö government from outside and held no ministry. A model that counted support
    /// as office would let a player govern from opposition.</description></item>
    /// <item><description><b>No player party means no verdict.</b> A country with no player party must
    /// return "no government tested" with a reason, never "out of office" — ending a game on a modelling
    /// gap is the worst kind of invented verdict.</description></item>
    /// </list>
    ///
    /// <para>⚠ <b>Declared red lines are sourced for SWEDEN ONLY, and the table says so per country.</b>
    /// Everywhere else the government is formed on derived lines alone and may not be one that country
    /// would form. That is reported in the column rather than hidden behind a green result.</para>
    /// </summary>
    public static class OfficeTestDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();

            World world = WorldFactory.CreateDefault();
            var sb = new StringBuilder();
            var failures = new List<string>();

            sb.Append("=== D-5 (a): the office test - is the player's party in the cabinet? ===\n");
            sb.Append("    THE ENUMERATION: every country, every party in it as the player's party. Office is CABINET\n");
            sb.Append("    MEMBERSHIP - support from outside is not office, which is the Tido arrangement's own distinction.\n\n");
            sb.Append("    country      declared lines   cabinet formed              in office / out / untestable\n");
            sb.Append("    ---------------------------------------------------------------------------------------\n");

            bool anyDiscriminates = false;
            foreach (Country country in world.Countries)
            {
                IReadOnlyList<PoliticalParty> parties = PartySystems.For(country.Id);
                string savedParty = country.PlayerPartyAbbrev;

                int inOffice = 0, outOfOffice = 0, untestable = 0;
                string cabinet = null;
                bool sourced = DeclaredRedLines.IsSourced(country.Id);

                foreach (PoliticalParty party in parties)
                {
                    country.PlayerPartyAbbrev = party.Abbrev;
                    GovernmentFormation.Formed formed = GovernmentFormation.Form(country);
                    if (!formed.HasGovernment) { untestable++; continue; }
                    cabinet = formed.CabinetDescription;
                    if (formed.PlayerInCabinet) { inOffice++; } else { outOfOffice++; }
                }

                country.PlayerPartyAbbrev = savedParty;
                if (inOffice > 0 && outOfOffice > 0) { anyDiscriminates = true; }

                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "    {0,-12} {1,-16} {2,-26}  {3} / {4} / {5}\n",
                    country.Name, sourced ? "SOURCED" : "derived only",
                    cabinet ?? "(none)", inOffice, outOfOffice, untestable));
            }

            if (!anyDiscriminates)
            {
                failures.Add("the office test discriminates nowhere");
                Debug.LogError("OFFICE: in no country does the test put some parties in office and others out. ⚠ A test that "
                               + "says everyone governs, or nobody does, is a constant wearing a function's name - it would end "
                               + "(or never end) every game identically, which is exactly what D-5 (a) replaced.");
            }

            // --- Sweden 2022: the answer is a matter of public record, not of the model's opinion. ---
            Country sweden = world.GetCountry(CountryId.Sweden);
            string swedenSaved = sweden.PlayerPartyAbbrev;

            sweden.PlayerPartyAbbrev = "M";
            GovernmentFormation.Formed asM = GovernmentFormation.Form(sweden);
            sweden.PlayerPartyAbbrev = "SD";
            GovernmentFormation.Formed asSd = GovernmentFormation.Form(sweden);
            sweden.PlayerPartyAbbrev = "S";
            GovernmentFormation.Formed asS = GovernmentFormation.Form(sweden);
            sweden.PlayerPartyAbbrev = swedenSaved;

            sb.Append("\n    --- SWEDEN 2022, against the government that actually formed ---\n");
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "    cabinet: {0}   M in cabinet: {1}   SD in cabinet: {2} (supports: {3})   S in cabinet: {4}\n",
                asM.CabinetDescription, asM.PlayerInCabinet, asSd.PlayerInCabinet, asSd.PlayerSupports, asS.PlayerInCabinet));

            if (!asM.HasGovernment || !asM.PlayerInCabinet)
            {
                failures.Add("Sweden 2022: M not in cabinet");
                Debug.LogError($"OFFICE: Sweden 2022 formed '{asM.CabinetDescription}' and Moderaterna is not in it. The real "
                               + "cabinet was M+KD+L. ⚠ This assertion exists because the answer is public record rather than "
                               + "the model's opinion - a wrong government here is a wrong game-over rule.");
            }

            if (asSd.HasGovernment && asSd.PlayerInCabinet)
            {
                failures.Add("Sweden 2022: SD counted as in cabinet");
                Debug.LogError("OFFICE: Sweden 2022 puts SD in the cabinet. It held no ministry - it supported the Tido "
                               + "government from outside. ⚠ Counting support as office would let a player govern from "
                               + "opposition, which is the distinction `GovernmentOption` draws between Cabinet and Support.");
            }

            if (asS.HasGovernment && asS.PlayerInCabinet)
            {
                failures.Add("Sweden 2022: S counted as in cabinet");
                Debug.LogError("OFFICE: Sweden 2022 puts Socialdemokraterna in the cabinet. It lost office in 2022; a rule that "
                               + "keeps the largest party in government regardless of the arithmetic is not an office test.");
            }

            // --- No player party: a reason, never a verdict. ---
            Country noParty = world.GetCountry(CountryId.France);
            string franceSaved = noParty.PlayerPartyAbbrev;
            noParty.PlayerPartyAbbrev = null;
            GovernmentFormation.Formed none = GovernmentFormation.Form(noParty);
            noParty.PlayerPartyAbbrev = franceSaved;

            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n    --- NO PLAYER PARTY: hasGovernment={0}, reason=\"{1}\" ---\n", none.HasGovernment, none.Reason));

            if (none.HasGovernment || string.IsNullOrEmpty(none.Reason))
            {
                failures.Add("no-player-party returns a verdict");
                Debug.LogError("OFFICE: a country with no player party returned a government rather than a reason. ⚠ 'No "
                               + "government could be tested' and 'the player is out of office' are different states, and only "
                               + "one of them should end a game.");
            }

            // P2-0.2 (2026-09-02): the approval-threshold election rule is RETIRED, and this is the harness that
            // proves the old path is unreachable - by absence, which is the only proof a deleted path can have.
            // Reflection asks the type; the source scan asks every file under Assets/Scripts with comments
            // stripped (a name surviving in a comment is history, not a path).
            sb.Append("\n--- P2-0.2: the approval-threshold rule is gone ---\n");
            bool memberGone = typeof(ElectionSystem).GetMember("LosingThreshold").Length == 0
                              && typeof(ElectionSystem).GetMember("RunElection").Length == 0
                              && typeof(ElectionSystem).Assembly.GetType("PoliSim.Simulation.ElectionResult") == null;
            var thresholdSites = new List<string>();
            string scriptsRoot = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "Assets", "Scripts");
            foreach (string file in System.IO.Directory.GetFiles(scriptsRoot, "*.cs", System.IO.SearchOption.AllDirectories))
            {
                string code = SourceText.WithoutComments(System.IO.File.ReadAllText(file));
                if (code.Contains("LosingThreshold") || code.Contains("RunElection(")) { thresholdSites.Add(System.IO.Path.GetFileName(file)); }
            }
            sb.Append(string.Format(CultureInfo.InvariantCulture, "    ElectionSystem declares no threshold member: {0}; code sites naming one: {1}\n",
                memberGone, thresholdSites.Count == 0 ? "none" : string.Join(", ", thresholdSites)));
            if (!memberGone || thresholdSites.Count > 0)
            {
                failures.Add("the approval-threshold election path is reachable");
                Debug.LogError("OFFICE: the approval-threshold election rule exists or is named in code under Assets/Scripts. P2-0.2 "
                               + "retired it (COMPLETED.md section 218): the only election outcome is election night's count and the "
                               + "office test. Sites: " + string.Join(", ", thresholdSites));
            }

            // P2-3.2 (2026-09-02): EVERY COMPASS POINT TRACES TO A CHES ROW OR A SEAT-WEIGHTED DERIVATION. For
            // every country: each party point is its own published pair (or absent when either scale is NaN);
            // the chamber mean re-summed here from seats × published pairs equals CompassPositions.ChamberMean;
            // the cabinet mean re-summed over GovernmentFormation.Cabinet's members equals CabinetMean; and the
            // seats left out are exactly the seated parties without a pair. No point can come from anywhere else.
            sb.Append("\n--- P2-3.2: the compass's points against their derivations ---\n");
            foreach (Country country in world.Countries)
            {
                IReadOnlyList<PoliticalParty> chamberParties = PartySystems.For(country.Id);
                float lrSum = 0f, galSum = 0f; int seatSum = 0, unpaired = 0, partyPoints = 0;
                bool partyPointsTrace = true;
                foreach (PoliticalParty party in chamberParties)
                {
                    CompassPositions.Point? own = CompassPositions.Party(party);
                    bool pair = !float.IsNaN(party.LrEcon) && !float.IsNaN(party.Galtan);
                    if (own.HasValue != pair || (own.HasValue && (own.Value.LrEcon != party.LrEcon || own.Value.Galtan != party.Galtan))) { partyPointsTrace = false; }
                    if (own.HasValue) { partyPoints++; }
                    int seats = country.ParliamentSeats.TryGetValue(party.Abbrev, out int s) ? s : 0;
                    if (seats <= 0) { continue; }
                    if (!pair) { unpaired += seats; continue; }
                    lrSum += party.LrEcon * seats; galSum += party.Galtan * seats; seatSum += seats;
                }
                CompassPositions.Point? chamber = CompassPositions.ChamberMean(country, out int leftOut);
                bool chamberTraces = seatSum > 0
                    ? chamber.HasValue && Mathf.Abs(chamber.Value.LrEcon - lrSum / seatSum) < 1e-4f && Mathf.Abs(chamber.Value.Galtan - galSum / seatSum) < 1e-4f && chamber.Value.Seats == seatSum
                    : !chamber.HasValue;
                bool leftOutTraces = leftOut == unpaired;

                IReadOnlyList<string> cabinet = GovernmentFormation.Cabinet(country);
                var cabinetSet = new HashSet<string>(cabinet);
                float cLr = 0f, cGal = 0f; int cSeats = 0;
                foreach (PoliticalParty party in chamberParties)
                {
                    if (!cabinetSet.Contains(party.Abbrev)) { continue; }
                    int seats = country.ParliamentSeats.TryGetValue(party.Abbrev, out int s) ? s : 0;
                    if (seats <= 0 || float.IsNaN(party.LrEcon) || float.IsNaN(party.Galtan)) { continue; }
                    cLr += party.LrEcon * seats; cGal += party.Galtan * seats; cSeats += seats;
                }
                CompassPositions.Point? cabinetMean = CompassPositions.CabinetMean(country, out int _);
                bool cabinetTraces = cSeats > 0
                    ? cabinetMean.HasValue && Mathf.Abs(cabinetMean.Value.LrEcon - cLr / cSeats) < 1e-4f && Mathf.Abs(cabinetMean.Value.Galtan - cGal / cSeats) < 1e-4f
                    : !cabinetMean.HasValue;

                bool ok = partyPointsTrace && chamberTraces && leftOutTraces && cabinetTraces;
                if (!ok)
                {
                    failures.Add($"compass point does not trace for {country.Id}");
                    Debug.LogError($"OFFICE: {country.Id} compass: parties {partyPointsTrace}, chamber {chamberTraces}, left-out {leftOutTraces}, cabinet {cabinetTraces}.");
                }
                sb.Append(string.Format(CultureInfo.InvariantCulture,
                    "    {0,-8} parties {1,2} pairs; chamber ({2}) on {3,3} seats, {4,3} left out; cabinet [{5}] ({6}) {7}\n",
                    country.Id, partyPoints,
                    chamber.HasValue ? string.Format(CultureInfo.InvariantCulture, "{0:F2}, {1:F2}", chamber.Value.LrEcon, chamber.Value.Galtan) : "none",
                    seatSum, unpaired, string.Join("+", cabinet),
                    cabinetMean.HasValue ? string.Format(CultureInfo.InvariantCulture, "{0:F2}, {1:F2}", cabinetMean.Value.LrEcon, cabinetMean.Value.Galtan) : "none",
                    ok ? "ok" : "FAIL"));
            }

            // P2-3.3 (2026-09-02): THE ELECTORATE'S POINT DERIVES FROM THE COHORTS' FITTED ELECTORATE. For every
            // country: re-derive the compatibility-weighted mean over NationalElection.TryCompatibility's own
            // arrays (the parties election night predicts from) and hold CompassPositions.ElectorateMean to it;
            // where no electorate is fitted the point is absent, never a centre.
            sb.Append("\n--- P2-3.3: the electorate's point against its derivation ---\n");
            foreach (Country country in world.Countries)
            {
                CompassPositions.Point? electorate = CompassPositions.ElectorateMean(country, out int counted);
                bool fitted = NationalElection.TryCompatibility(country.Id, out string[] eKeys, out double[] eCompat, out double[] _, out double[] _);
                bool traces;
                string detail;
                if (!fitted)
                {
                    traces = !electorate.HasValue && counted == 0;
                    detail = "no fitted electorate - absent";
                }
                else
                {
                    var byAbbrev = new Dictionary<string, PoliticalParty>();
                    foreach (PoliticalParty party in PartySystems.For(country.Id)) { byAbbrev[party.Abbrev] = party; }
                    double lr = 0.0, gal = 0.0, w = 0.0; int n = 0;
                    for (int i = 0; i < eKeys.Length; i++)
                    {
                        if (!byAbbrev.TryGetValue(eKeys[i], out PoliticalParty party) || float.IsNaN(party.LrEcon) || float.IsNaN(party.Galtan) || eCompat[i] <= 0.0) { continue; }
                        lr += party.LrEcon * eCompat[i]; gal += party.Galtan * eCompat[i]; w += eCompat[i]; n++;
                    }
                    traces = w > 0.0
                        ? electorate.HasValue && counted == n && Mathf.Abs(electorate.Value.LrEcon - (float)(lr / w)) < 1e-4f && Mathf.Abs(electorate.Value.Galtan - (float)(gal / w)) < 1e-4f
                        : !electorate.HasValue;
                    detail = electorate.HasValue
                        ? string.Format(CultureInfo.InvariantCulture, "({0:F2}, {1:F2}) over {2} parties", electorate.Value.LrEcon, electorate.Value.Galtan, counted)
                        : "fitted, but no positioned party carries weight - absent";
                }
                if (!traces)
                {
                    failures.Add($"electorate point does not trace for {country.Id}");
                    Debug.LogError($"OFFICE: {country.Id} electorate point does not re-derive from TryCompatibility ({detail}).");
                }
                sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-8} electorate {1} {2}\n", country.Id, detail, traces ? "ok" : "FAIL"));
            }

            // P2-2.2 (2026-09-02): THE PER-SEAT MAP'S COUNTS EQUAL THE STANCE ARITHMETIC TO THE SEAT. For every
            // country, both bill directions and both axes: the sides sum to the seats the chamber holds, every
            // party's side is the sign of its stance times the bill's sign (the Laws page's own rule), and the
            // seat-weighted alignment re-summed from the enumeration equals the one the verdict reads.
            sb.Append("\n--- P2-2.2: the seat map's sides against the stance arithmetic ---\n");
            foreach (Country country in world.Countries)
            {
                int chamber = 0;
                foreach (KeyValuePair<string, int> kv in country.ParliamentSeats) { chamber += kv.Value; }
                foreach (BillAxis axis in new[] { BillAxis.Fiscal, BillAxis.Trade })
                {
                    foreach (float direction in new[] { 30f, -30f })
                    {
                        int forSeats = 0, againstSeats = 0, undecided = 0, listed = 0;
                        float resummed = 0f, measuredSeats = 0f;
                        bool sidesAgree = true;
                        foreach ((PoliticalParty party, int seats, int side, float weight, bool measured) in ParliamentSystem.SeatSides(country, direction, axis))
                        {
                            listed += seats;
                            if (side > 0) { forSeats += seats; } else if (side < 0) { againstSeats += seats; } else { undecided += seats; }
                            float stance = measured ? (axis == BillAxis.Trade ? PartySystems.TradeStance(party) : PartySystems.FiscalStance(party)) * Mathf.Sign(direction) : 0f;
                            int expectedSide = !measured ? 0 : stance > 0f ? 1 : stance < 0f ? -1 : 0;
                            if (expectedSide != side) { sidesAgree = false; }
                            if (measured) { measuredSeats += seats; resummed += seats * weight; }
                        }
                        float alignment = ParliamentSystem.GetSeatWeightedAlignment(country, direction, axis);
                        float expectedAlignment = measuredSeats > 0f ? resummed / measuredSeats : (axis == BillAxis.Trade ? ParliamentSystem.GetSeatWeightedAlignment(country, direction, BillAxis.Fiscal) : 0f);
                        bool ok = listed == chamber && sidesAgree && Mathf.Abs(alignment - expectedAlignment) < 1e-5f;
                        if (!ok)
                        {
                            failures.Add($"seat map disagrees with the stance arithmetic for {country.Id} {axis} {direction:+0;-0}");
                            Debug.LogError($"OFFICE: {country.Id} {axis} {direction:+0;-0}: sides list {listed} of {chamber} seats, sides agree {sidesAgree}, alignment {alignment:F5} vs re-summed {expectedAlignment:F5}.");
                        }
                        sb.Append(string.Format(CultureInfo.InvariantCulture, "    {0,-8} {1,-6} {2,3}: FOR {3,3}  UNDECIDED {4,3}  AGAINST {5,3}  of {6,3}  alignment {7:+0.000;-0.000}  {8}\n",
                            country.Id, axis, direction, forSeats, undecided, againstSeats, chamber, alignment, ok ? "ok" : "FAIL"));
                    }
                }
            }

            sb.Append("\n    ⚠ DECLARED RED LINES ARE SOURCED FOR SWEDEN ONLY. Everywhere else the government is formed on\n");
            sb.Append("    DERIVED lines alone and may not be one that country would form - reported in the column above\n");
            sb.Append("    rather than hidden behind a green result. Inventing Germany's declarations would be inventing the\n");
            sb.Append("    central political fact of its party system.\n");

            if (failures.Count == 0)
            {
                sb.Append("\n    CLEAN - the test discriminates, and Sweden 2022 seats the government Sweden seated.\n");
                Debug.Log(sb.ToString());
                CheckExit.Finish(0);
                return;
            }

            sb.Append(string.Format(CultureInfo.InvariantCulture, "\n    ⚠ {0} FAILURE(S) - see the errors above.\n", failures.Count));
            Debug.LogError(sb.ToString());
            CheckExit.Finish(1);
        }
    }
}
