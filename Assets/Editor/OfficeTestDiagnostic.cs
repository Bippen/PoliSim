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
            // K-1 (2026-09-23): the world seats 2026's chamber now, so the 2022 record is tested on 2022's chamber, set for the
            // three formations and restored, with 2022's declarations - the backtest keeps asserting the government that actually formed from it.
            Country sweden = world.GetCountry(CountryId.Sweden);
            string swedenSaved = sweden.PlayerPartyAbbrev;
            var seatedChamber = new Dictionary<string, int>(sweden.ParliamentSeats);
            IReadOnlyList<PoliticalParty> swedenParties = PartySystems.For(CountryId.Sweden);
            sweden.ParliamentSeats.Clear();
            for (int p = 0; p < swedenParties.Count; p++) { sweden.ParliamentSeats[swedenParties[p].Abbrev] = CampaignAiHarness.Seats2022[p]; }

            sweden.PlayerPartyAbbrev = "M";
            GovernmentFormation.Formed asM = GovernmentFormation.Form(sweden, ElectionVintage.Sweden2022);
            sweden.PlayerPartyAbbrev = "SD";
            GovernmentFormation.Formed asSd = GovernmentFormation.Form(sweden, ElectionVintage.Sweden2022);
            sweden.PlayerPartyAbbrev = "S";
            GovernmentFormation.Formed asS = GovernmentFormation.Form(sweden, ElectionVintage.Sweden2022);
            sweden.PlayerPartyAbbrev = swedenSaved;
            sweden.ParliamentSeats.Clear();
            foreach (KeyValuePair<string, int> seat in seatedChamber) { sweden.ParliamentSeats[seat.Key] = seat.Value; }

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

            // --- K-1 (2026-09-23) and K-1f (2026-09-24): THE DECLARED SHAPES, as the formation reads them. The shapes on their own (a one-way
            // line refuses A->B only); the wiring (C->V one way; the S-M candidacy pair, one way in each direction, in BOTH vintages - every
            // election reads its own date's; the party rules in 2026's only - V's and MP's voting against, SD's refusing the support role, K-1g); the rule's vote-against
            // half on a chamber built so that half alone decides; the GAME'S path reading the rule (GovernmentFormation against the formation
            // given the rule); and the rules' effect over EVERY viable government of a chamber that forms some - the pinned film's own year-32
            // count (film603b and film607 count the same shares). K-1h and K-1g (ruled 2026-09-25): a party holds out only for a cabinet of its
            // own that could pass, and MP's, SD's and KD's 2026 declarations are wired beside V's; the seated chamber's outcome is printed, not
            // asserted, because nothing is tuned toward an outcome - what is asserted is that K-1f's own set forms a government under K-1h (i). ⚠ The C-with-V count is blind on the year-32 count (the right bloc
            // holds 180, so no cabinet with V is viable there); it is kept as a print and the one-way shape is asserted on its own. ---
            var oneWay = new RedLine(0, 1, RedLineKind.Declared, blocksSupport: true, basis: "probe", oneWay: true);
            var symmetric = new RedLine(0, 1, RedLineKind.Declared, blocksSupport: true, basis: "probe");
            var cabinetOnly = new RedLine(0, 1, RedLineKind.Declared, blocksSupport: false, basis: "probe");
            bool shapeHolds = oneWay.RefusesSupport(0, 1) && !oneWay.RefusesSupport(1, 0)
                && symmetric.RefusesSupport(0, 1) && symmetric.RefusesSupport(1, 0)
                && !cabinetOnly.RefusesSupport(0, 1) && !cabinetOnly.RefusesSupport(1, 0);
            int cIndex = -1, vIndex = -1, sIndex = -1, mIndex = -1;
            var seatedSeats = new int[swedenParties.Count];
            for (int p = 0; p < swedenParties.Count; p++)
            {
                seatedSeats[p] = swedenParties[p].SeedSeats;
                if (swedenParties[p].Abbrev == "C") { cIndex = p; }
                if (swedenParties[p].Abbrev == "V") { vIndex = p; }
                if (swedenParties[p].Abbrev == "S") { sIndex = p; }
                if (swedenParties[p].Abbrev == "M") { mIndex = p; }
            }
            List<RedLine> seatedLines = DeclaredRedLines.For(CountryId.Sweden, swedenParties);
            List<InOrAgainst> seatedRules = DeclaredRedLines.InOrAgainstFor(CountryId.Sweden, swedenParties);
            bool CandidacyPair(List<RedLine> lines)
            {
                bool sm = false, ms = false;
                foreach (RedLine line in lines)
                {
                    if (!DeclaredRedLines.IsCandidacy(line) || !line.OneWay) { continue; }
                    if (line.A == sIndex && line.B == mIndex) { sm = true; }
                    if (line.A == mIndex && line.B == sIndex) { ms = true; }
                }
                return sm && ms;
            }
            bool wiredOneWay = seatedLines.Exists(line => line.Kind == RedLineKind.Declared && line.A == cIndex && line.B == vIndex && line.OneWay);
            bool candidacyWired = CandidacyPair(seatedLines) && CandidacyPair(DeclaredRedLines.For(CountryId.Sweden, swedenParties, ElectionVintage.Sweden2022));
            // K-1g (2026-09-25): 2026's party rules are V's and MP's (they vote against) and SD's (it refuses the support role only); 2022's none.
            int mpIndex = -1, sdIndex = -1, kdIndex = -1;
            for (int p = 0; p < swedenParties.Count; p++) { if (swedenParties[p].Abbrev == "MP") { mpIndex = p; } if (swedenParties[p].Abbrev == "SD") { sdIndex = p; } if (swedenParties[p].Abbrev == "KD") { kdIndex = p; } }
            bool RuleIs(int party, bool votesAgainst) => seatedRules.Exists(r => r.Party == party && r.VotesAgainst == votesAgainst);
            bool partyRulesWired = seatedRules.Count == 3 && RuleIs(vIndex, true) && RuleIs(mpIndex, true) && RuleIs(sdIndex, false)
                && DeclaredRedLines.InOrAgainstFor(CountryId.Sweden, swedenParties, ElectionVintage.Sweden2022).Count == 0;
            bool KdToS(List<RedLine> lines) => lines.Exists(line => line.Kind == RedLineKind.Declared && line.OneWay && line.BlocksSupport && line.A == kdIndex && line.B == sIndex);
            bool kdWired = KdToS(seatedLines) && !KdToS(DeclaredRedLines.For(CountryId.Sweden, swedenParties, ElectionVintage.Sweden2022));

            // The vote-against half, where it alone decides: A 150 and B 169 refuse each other's support; X 30 may sit with neither (its only
            // admissible cabinet is itself, which scores below both, so it holds out against neither). Without a rule X abstains or supports,
            // and A or B governs (at most 169 against). With X's in-or-against rule X is never a supporter and votes against both: 199 and 180.
            int[] probeSeats = { 150, 169, 30 };
            var probeCompat = new double[3, 3];
            for (int a = 0; a < 3; a++) { for (int b = 0; b < 3; b++) { probeCompat[a, b] = a == b ? 100.0 : 50.0; } }
            var probeLines = new List<RedLine>
            {
                new RedLine(0, 1, RedLineKind.Declared, blocksSupport: true, basis: "probe: A and B refuse each other's support"),
                new RedLine(2, 0, RedLineKind.Declared, blocksSupport: false, basis: "probe: X sits with neither"),
                new RedLine(2, 1, RedLineKind.Declared, blocksSupport: false, basis: "probe: X sits with neither"),
            };
            CoalitionResult probeWithout = CoalitionFormation.Form(probeSeats, probeCompat, probeLines, negativeRule: true);
            CoalitionResult probeWith = CoalitionFormation.Form(probeSeats, probeCompat, probeLines, negativeRule: true,
                inOrAgainst: new List<InOrAgainst> { new InOrAgainst(2, "probe: X supports no cabinet it is not in") });
            int governsWithoutX = probeWithout.Viable.FindAll(g => (g.Cabinet & 4) == 0).Count;
            int governsWithoutXUnderRule = probeWith.Viable.FindAll(g => (g.Cabinet & 4) == 0).Count;
            int xSupportsUnderRule = probeWith.Viable.FindAll(g => (g.Support & 4) != 0).Count;
            bool voteAgainstDecides = governsWithoutX > 0 && governsWithoutXUnderRule == 0 && xSupportsUnderRule == 0;
            // K-1g: SD's form - the support role refused and no vote against declared - on the same chamber: X is behind no cabinet, and A or B
            // still governs, because X abstains.
            CoalitionResult probeNoSupport = CoalitionFormation.Form(probeSeats, probeCompat, probeLines, negativeRule: true,
                inOrAgainst: new List<InOrAgainst> { new InOrAgainst(2, "probe: X takes no support role", votesAgainst: false) });
            bool noSupportRoleHolds = probeNoSupport.Viable.FindAll(g => (g.Cabinet & 4) == 0).Count > 0 && probeNoSupport.Viable.FindAll(g => (g.Support & 4) != 0).Count == 0;

            double[,] swedenCompat = GovernmentFormation.Compatibility(swedenParties);
            CoalitionResult seatedFormation = CoalitionFormation.Form(seatedSeats, swedenCompat, seatedLines, negativeRule: true, inOrAgainst: seatedRules);
            CoalitionResult seatedWithoutRules = CoalitionFormation.Form(seatedSeats, swedenCompat, seatedLines, negativeRule: true);
            // K-1h (i), ruled 2026-09-25: a party holds out only for a cabinet of its own that could pass. Under K-1f's own set (V's rule, no KD line)
            // the seated chamber formed none while a party held out for any admissible cabinet (§607); the ruling's reading forms a government
            // there - the premise the formateur's first ruling rests on. Which one is printed, not asserted. ⚠ A weak guard: it reads the real
            // chamber, so a change of seats or positions could flip it with the hold-out untouched; a discriminating probe needs the rule it
            // replaced, which is removed (§639).
            List<RedLine> k1fLines = seatedLines.FindAll(line => !(line.Kind == RedLineKind.Declared && line.OneWay && line.A == kdIndex && line.B == sIndex));
            CoalitionResult k1fSeated = CoalitionFormation.Form(seatedSeats, swedenCompat, k1fLines, negativeRule: true, inOrAgainst: seatedRules.FindAll(r => r.Party == vIndex));
            bool holdOutPassableOnly = k1fSeated.Outcome != CoalitionOutcomeKind.NewElection && k1fSeated.Outcome != CoalitionOutcomeKind.Collapse;
            var yearThirtyTwo = new int[swedenParties.Count];
            foreach ((string abbrev, int held) in new[] { ("S", 94), ("SD", 63), ("M", 70), ("V", 27), ("C", 24), ("KD", 27), ("MP", 24), ("L", 20) })
            {
                for (int p = 0; p < swedenParties.Count; p++) { if (swedenParties[p].Abbrev == abbrev) { yearThirtyTwo[p] = held; } }
            }
            CoalitionResult played = CoalitionFormation.Form(yearThirtyTwo, swedenCompat, seatedLines, negativeRule: true, inOrAgainst: seatedRules);
            CoalitionResult playedWithoutRules = CoalitionFormation.Form(yearThirtyTwo, swedenCompat, seatedLines, negativeRule: true);

            // The game's own path: GovernmentFormation's formation (TryFormSeats, where the rules are passed), on both chambers, against the
            // formation given the rules - and the rules must tell the two apart on at least one of them, or this comparison could not see the
            // game dropping them. Read through ViewOf(CountryId, ...), which goes to TryFormSeats directly, so an INSTALLED seated government
            // (K-1b: once the Riksdag's vote is on record, the country's seated path returns the record, not the formation) cannot silence it;
            // the country's own path (TryGovernment, the stance model's and the picker's) is compared too while no record is installed.
            string Government(CoalitionResult r)
            {
                if (r.Outcome == CoalitionOutcomeKind.NewElection || r.Outcome == CoalitionOutcomeKind.Collapse) { return "none"; }
                var cab = new List<string>();
                var sup = new List<string>();
                for (int p = 0; p < swedenParties.Count; p++)
                {
                    if ((r.Government.Cabinet & (1 << p)) != 0) { cab.Add(swedenParties[p].Abbrev); }
                    else if ((r.Government.Support & (1 << p)) != 0) { sup.Add(swedenParties[p].Abbrev); }
                }
                return string.Join("+", cab) + (sup.Count > 0 ? " | " + string.Join("+", sup) : string.Empty);
            }
            string FormationPath(int[] chamber)
            {
                var abbrevs = new List<string>();
                var held = new List<int>();
                for (int p = 0; p < swedenParties.Count; p++) { abbrevs.Add(swedenParties[p].Abbrev); held.Add(chamber[p]); }
                GovernmentFormation.View view = GovernmentFormation.ViewOf(CountryId.Sweden, abbrevs, held, null);
                if (!view.HasGovernment) { return "none"; }
                var cab = new List<string>();
                var sup = new List<string>();
                foreach (PoliticalParty party in swedenParties)
                {
                    if (view.Cabinet.Exists(c => c.Abbrev == party.Abbrev)) { cab.Add(party.Abbrev); }
                    else if (view.Support.Exists(c => c.Abbrev == party.Abbrev)) { sup.Add(party.Abbrev); }
                }
                return string.Join("+", cab) + (sup.Count > 0 ? " | " + string.Join("+", sup) : string.Empty);
            }
            string CountryPath()
            {
                // PS-3h (§635): who governs is the STORED record - the chamber under test gets the record the game would store after an election on it.
                sweden.Government = PoliSim.Elections.GovernmentRecord.FromView(sweden, GovernmentFormation.ViewOf(sweden), PoliSim.Simulation.SimulationManager.EpochDate, world: world);
                if (!GovernmentFormation.TryGovernment(sweden, out IReadOnlyList<string> cab, out IReadOnlyList<string> sup)) { return "none"; }
                return string.Join("+", cab) + (sup.Count > 0 ? " | " + string.Join("+", sup) : string.Empty);
            }
            PoliSim.Elections.GovernmentRecord seatedRecord = sweden.Government;   // restored after the chambers under test
            bool installed = SeatedGovernment.TryInstalled(sweden, out SeatedGovernment.Record _);
            string gameSeated = FormationPath(seatedSeats);
            string gamePlayed = FormationPath(yearThirtyTwo);
            string countrySeated = installed ? "(installed record)" : CountryPath();
            sweden.ParliamentSeats.Clear();
            for (int p = 0; p < swedenParties.Count; p++) { sweden.ParliamentSeats[swedenParties[p].Abbrev] = yearThirtyTwo[p]; }
            string countryPlayed = installed ? "(installed record)" : CountryPath();
            sweden.ParliamentSeats.Clear();
            foreach (KeyValuePair<string, int> seat in seatedChamber) { sweden.ParliamentSeats[seat.Key] = seat.Value; }
            sweden.Government = seatedRecord;
            bool gameReadsRules = gameSeated == Government(seatedFormation) && gamePlayed == Government(played)
                && (installed || (countrySeated == gameSeated && countryPlayed == gamePlayed));
            bool rulesVisible = Government(seatedFormation) != Government(seatedWithoutRules) || Government(played) != Government(playedWithoutRules);

            int cBit = 1 << cIndex, vBit = 1 << vIndex, sBit = 1 << sIndex, mBit = 1 << mIndex;
            int cWithV = 0, sWithM = 0, rivalSupport = 0, vSupports = 0, ruleSupports = 0, kdWithS = 0;
            int mpBit = 1 << mpIndex, sdBit = 1 << sdIndex, kdBit = 1 << kdIndex;
            foreach (CoalitionResult formation in new[] { played, seatedFormation })
            {
                foreach (GovernmentOption g in formation.Viable)
                {
                    if ((g.Cabinet & vBit) != 0 && ((g.Cabinet & cBit) != 0 || (g.Support & cBit) != 0)) { cWithV++; }
                    if ((g.Cabinet & sBit) != 0 && (g.Cabinet & mBit) != 0) { sWithM++; }
                    if (((g.Cabinet & mBit) != 0 && (g.Support & sBit) != 0) || ((g.Cabinet & sBit) != 0 && (g.Support & mBit) != 0)) { rivalSupport++; }
                    if ((g.Support & vBit) != 0) { vSupports++; }
                    if ((g.Support & (mpBit | sdBit)) != 0) { ruleSupports++; }   // K-1g: MP and SD behind a cabinet they are not in
                    if ((g.Cabinet & sBit) != 0 && ((g.Cabinet & kdBit) != 0 || (g.Support & kdBit) != 0)) { kdWithS++; }   // K-1g: KD in or behind a cabinet holding S
                }
            }
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n    --- K-1 / K-1f: the declared shapes as the formation reads them ---\n"
                + "    the shape: one-way refuses A->B only {0}; symmetric both ways {1}; cabinet-blocking neither way {2}\n"
                + "    the wiring: C->V one way {3}; the S-M candidacy pair in both vintages {4}; the 2026 party rules (V, MP, SD), in 2026's only {5}\n"
                + "    the vote-against half (A 150, B 169, X 30): without X's rule {6} viable cabinet(s) without X; with it {7}, X behind {8} - decides {9}\n"
                + "    the game's path: seated '{10}' (the formation given the rules '{11}', without them '{12}'); year-32 '{13}' ('{14}', '{15}') - reads the rules {16}, visible {17}; the country's own path: seated '{25}', year-32 '{26}'\n"
                + "    the seated chamber, every rule held: {18} ({19} viable) - printed, not asserted\n"
                + "    the year-32 count and the seated chamber: {20} viable on the count; C in or behind a cabinet with V {21}; S and M in one cabinet {22}; a candidacy party behind its rival's cabinet {23}; V behind a cabinet it is not in {24}\n"
                + "    K-1g: the rules MP, SD (no support role) {27}, KD>S in 2026's only {28}; SD's form on the probe chamber - X behind nothing, A or B still governs {29}; MP or SD behind a cabinet it is not in {30}; KD in or behind a cabinet holding S {31}\n"
                + "    K-1h (i): K-1f's own set on the seated chamber forms {32} - a government, as the ruling reads it {33}\n",
                oneWay.RefusesSupport(0, 1) && !oneWay.RefusesSupport(1, 0), symmetric.RefusesSupport(0, 1) && symmetric.RefusesSupport(1, 0),
                !cabinetOnly.RefusesSupport(0, 1) && !cabinetOnly.RefusesSupport(1, 0), wiredOneWay, candidacyWired, partyRulesWired,
                governsWithoutX, governsWithoutXUnderRule, xSupportsUnderRule, voteAgainstDecides,
                gameSeated, Government(seatedFormation), Government(seatedWithoutRules), gamePlayed, Government(played), Government(playedWithoutRules), gameReadsRules, rulesVisible,
                seatedFormation.Outcome, seatedFormation.Viable.Count, played.Viable.Count, cWithV, sWithM, rivalSupport, vSupports, countrySeated, countryPlayed,
                partyRulesWired, kdWired, noSupportRoleHolds, ruleSupports, kdWithS, Government(k1fSeated), holdOutPassableOnly));
            if (!shapeHolds || !wiredOneWay || !candidacyWired || !partyRulesWired || !kdWired || !voteAgainstDecides || !noSupportRoleHolds || !holdOutPassableOnly || !gameReadsRules || !rulesVisible
                || cIndex < 0 || vIndex < 0 || sIndex < 0 || mIndex < 0 || mpIndex < 0 || sdIndex < 0 || kdIndex < 0
                || played.Viable.Count == 0 || cWithV > 0 || sWithM > 0 || rivalSupport > 0 || vSupports > 0 || ruleSupports > 0 || kdWithS > 0)
            {
                failures.Add("K-1 / K-1f: the declared shapes");
                Debug.LogError($"OFFICE: the declared shapes do not hold - the shape {(shapeHolds ? "ok" : "WRONG")}, C->V one way {wiredOneWay}, the candidacy pair {candidacyWired}, "
                               + $"the party rules (V, MP; SD no support role) {partyRulesWired}, KD>S {kdWired}; the vote-against half decides {voteAgainstDecides}, SD's form holds {noSupportRoleHolds}, K-1h (i) forms a government {holdOutPassableOnly}; the game's path reads the rules {gameReadsRules} "
                               + $"(seated '{gameSeated}' vs '{Government(seatedFormation)}', year-32 '{gamePlayed}' vs '{Government(played)}'), the rules visible {rulesVisible}; "
                               + $"over the year-32 count's {played.Viable.Count} and the seated chamber's {seatedFormation.Viable.Count} viable government(s): C with V {cWithV}, S with M {sWithM}, a candidacy "
                               + $"party behind its rival {rivalSupport}, V behind a cabinet it is not in {vSupports}, MP or SD behind one {ruleSupports}, KD with S {kdWithS}. ⚠ Each is a sourced declaration "
                               + "(coalition_declarations_2026.md, _2022.md); a count above zero means the formation no longer reads what a party said.");
            }

            // --- K-1 part (4): WHO GOVERNS ON DAY ONE. Sweden's seeded government is the formation's PROVISIONAL stand-in (the real one is
            // not on record); a country with no record is not provisional; a game-held election ends the standing; and the installed path the
            // Riksdag's vote will fill (K-1b, data only) reads a record as the government - checked here on 2022's chamber with 2022's real
            // cabinet, where the answer is public record, and refused when the record names a party the chamber does not seat. ---
            Country germany = world.GetCountry(CountryId.Germany);
            bool swedenProvisional = SeatedGovernment.IsProvisional(sweden);   // the seated table's own rule; the compass reads the stored record (§642, below)
            bool germanyProvisional = SeatedGovernment.IsProvisional(germany);
            sweden.ElectionHistory.Add(new ElectionRecord { Turn = 4, CountryId = CountryId.Sweden.ToString(), Method = ElectionMethod.SwedenTwoTier });
            bool provisionalAfterElection = SeatedGovernment.IsProvisional(sweden);
            sweden.ElectionHistory.RemoveAt(sweden.ElectionHistory.Count - 1);
            var tido = new SeatedGovernment.Record(SeatedGovernment.Standing.Installed, new[] { "M", "KD", "L" }, new[] { "SD" }, "probe: the 2022 Tidö record", new System.DateTime(2022, 10, 18));
            bool installedReads = SeatedGovernment.TryAsResult(tido, swedenParties, CampaignAiHarness.Seats2022, out CoalitionResult installedResult, out string installedReason);
            bool installedRight = installedReads && installedResult.Outcome == CoalitionOutcomeKind.ConfidenceAndSupply
                && installedResult.Government.CabinetSeats == 103 && installedResult.Government.SupportedSeats == 176;
            var stranger = new SeatedGovernment.Record(SeatedGovernment.Standing.Installed, new[] { "M", "XX" }, null, "probe: a party this chamber does not seat", new System.DateTime(2026, 10, 1));
            bool strangerRefused = !SeatedGovernment.TryAsResult(stranger, swedenParties, seatedSeats, out CoalitionResult _, out string strangerReason) && strangerReason != null;
            sb.Append(string.Format(CultureInfo.InvariantCulture,
                "\n    --- K-1 part (4): who governs on day one ---\n"
                + "    Sweden's seeded government provisional: {0}; Germany's (no record): {1}; Sweden's after a game-held election: {2}\n"
                + "    an installed record read as the government (2022's chamber, M+KD+L carried by SD): {3} - {4}; a record naming an unseated party refused: {5} ({6})\n",
                swedenProvisional, germanyProvisional, provisionalAfterElection,
                installedReads ? installedResult.Outcome.ToString() : "not read: " + installedReason,
                installedReads ? string.Format(CultureInfo.InvariantCulture, "cabinet {0}, supported {1}", installedResult.Government.CabinetSeats, installedResult.Government.SupportedSeats) : "-",
                strangerRefused, strangerReason));
            // §642 (the review): the compass's mark is the STORED record's standing - one answer with the cabinet it prints - and at the start the record is the seated table's.
            bool markReadsRecord = GovernmentFormation.IsProvisional(sweden) == sweden.Government.Provisional && GovernmentFormation.IsProvisional(germany) == germany.Government.Provisional && sweden.Government.Provisional == swedenProvisional && germany.Government.Provisional == germanyProvisional;
            if (!swedenProvisional || germanyProvisional || provisionalAfterElection || !installedRight || !strangerRefused || !markReadsRecord)
            {
                failures.Add("K-1 part (4): the seated government's standing");
                Debug.LogError("OFFICE: the day-one government's standing does not hold - Sweden provisional " + swedenProvisional + " (want True), Germany "
                               + germanyProvisional + " (want False), Sweden after a held election " + provisionalAfterElection + " (want False), the installed record "
                               + (installedRight ? "right" : "WRONG") + ", the unseated party " + (strangerRefused ? "refused" : "NOT refused")
                               + ", the compass's mark reads the stored record " + markReadsRecord + ". ⚠ The order: the formation's result stands in, marked provisional, replaced when the Riksdag votes.");
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
                            // P3-A2: the side is the model's own alignment against the undecided band (§246 term 4); the weight IS the alignment.
                            int expectedSide = !measured ? 0 : Mathf.Abs(weight) < StanceModel.UndecidedBand ? 0 : weight > 0f ? 1 : -1;
                            if (expectedSide != side) { sidesAgree = false; }
                            if (measured) { measuredSeats += seats; resummed += seats * weight; }
                        }
                        float alignment = ParliamentSystem.GetSeatWeightedAlignment(country, direction, axis);
                        float expectedAlignment = measuredSeats > 0f ? resummed / measuredSeats : 0f;
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
