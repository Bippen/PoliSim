using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §679: **A CREATED PARTY'S DECLARATIONS FEED THE FORMATION LIKE EVERY PARTY'S - PROVED ON THE GAME'S OWN ROUND.** Asserted: with none registered,
    /// and with one registered that declares nothing, every reading (the lines, dated and not; the candidacies; the in-or-against rules) holds the
    /// sourced declarations exactly; each of the four declaration kinds becomes the rule it names, with the founder's basis; and **a created party's
    /// red line blocks a cabinet**: at Sweden's start, a party standing on M's nine positions and holding M's seats is in the cabinet the Speaker's
    /// round forms (`GovernmentFormation.ViewOfSitting`, the game's call); declaring a red line against its cabinet partner, the same round forms
    /// another cabinet without the pair, and the evaluator bars the first cabinet by that line, naming it.
    /// </summary>
    public static class CreatedPartyDeclarationsDiagnostic
    {
        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        private static string Sig(IEnumerable<RedLine> lines) => string.Join("|", lines.Select(l => F("{0}>{1}{2}{3}:{4}", l.A, l.B, l.BlocksSupport ? "s" : "c", l.OneWay ? "1" : "2", l.Basis)));

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== CreatedPartyDeclarationsDiagnostic (§679): a created party's declarations in the formation ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            using IDisposable created = CreatedParties.Scope();
            try
            {
                const CountryId se = CountryId.Sweden;
                WorldClock.ApplyStart(se);
                CreatedParties.Clear();
                PoliticalParty[] real = PartySystems.RealRoster(se);
                DateTime day = WorldClock.StartDate(se);
                string lines22 = Sig(DeclaredRedLines.For(se, real, ElectionVintage.Sweden2022)), lines26 = Sig(DeclaredRedLines.For(se, real, ElectionVintage.Sweden2026));
                string dated = Sig(DeclaredRedLines.ForDate(se, real, day));
                int rules = DeclaredRedLines.InOrAgainstFor(se, real, ElectionVintage.Sweden2026).Count, ruleAt = DeclaredRedLines.InOrAgainstAt(se, real, day).Count;
                int cands = DeclaredRedLines.Candidacies(se, ElectionVintage.Sweden2026).Count;

                // a created party that declares nothing adds nothing to the declared readings (the derived lines read its position, as every party's)
                var quiet = new CreatedParty { Country = se, Key = "QP", Name = "Tyst parti", LeaderName = "Q" };
                quiet.ApplyOrigin(PartyOrigin.Grassroots); quiet.PlaceOn(real[Array.FindIndex(real, p => p.Abbrev == "C")]);
                CreatedParties.TryRegister(quiet, real, out _);
                IReadOnlyList<PoliticalParty> withQuiet = PartySystems.For(se);
                bool noneAdded = DeclaredRedLines.For(se, withQuiet, ElectionVintage.Sweden2026).Count(l => l.Basis.Contains(DeclaredRedLines.CreatedPrefix)) == 0
                                 && DeclaredRedLines.InOrAgainstFor(se, withQuiet, ElectionVintage.Sweden2026).Count == rules && DeclaredRedLines.Candidacies(se, ElectionVintage.Sweden2026).Count == cands;
                CreatedParties.Clear();
                Check(Sig(DeclaredRedLines.For(se, real, ElectionVintage.Sweden2022)) == lines22 && Sig(DeclaredRedLines.For(se, real, ElectionVintage.Sweden2026)) == lines26
                      && Sig(DeclaredRedLines.ForDate(se, real, day)) == dated && noneAdded,
                    F("inert: none registered, every reading is the sourced one; a created party that declares nothing adds no declared line, rule or candidacy ({0} rules, {1} candidacies in 2026)", rules, cands));

                // the four kinds, each as the rule it names
                var d = new CreatedParty { Country = se, Key = "NP", Name = "Nya partiet", LeaderName = "N. Ledare" };
                d.ApplyOrigin(PartyOrigin.Grassroots); d.PlaceOn(real[Array.FindIndex(real, p => p.Abbrev == "C")]);
                d.RedLinesAgainst.Add("SD"); d.OneWayAgainst.Add("V"); d.BacksCandidateOf = "NP"; d.InOrAgainst = true;
                CreatedParties.TryRegister(d, real, out _);
                IReadOnlyList<PoliticalParty> all = PartySystems.For(se);
                int np = all.Count - 1, sd = Array.FindIndex(real, p => p.Abbrev == "SD"), v = Array.FindIndex(real, p => p.Abbrev == "V"), s = Array.FindIndex(real, p => p.Abbrev == "S"), m = Array.FindIndex(real, p => p.Abbrev == "M");
                List<RedLine> l26 = DeclaredRedLines.For(se, all, ElectionVintage.Sweden2026);
                bool red = l26.Any(l => l.A == np && l.B == sd && l.BlocksSupport && !l.OneWay && l.Basis.StartsWith(DeclaredRedLines.CreatedPrefix, StringComparison.Ordinal));
                bool oneWay = l26.Any(l => l.A == np && l.B == v && l.OneWay && l.BlocksSupport);
                bool candidacy = DeclaredRedLines.Candidacies(se, ElectionVintage.Sweden2026).Any(c => c.Abbrev == "NP") && l26.Any(l => l.A == np && l.B == s && DeclaredRedLines.IsCandidacy(l)) && l26.Any(l => l.A == s && l.B == np && DeclaredRedLines.IsCandidacy(l));
                bool inOrAgainst = DeclaredRedLines.InOrAgainstFor(se, all, ElectionVintage.Sweden2026).Any(r => r.Party == np && r.VotesAgainst) && DeclaredRedLines.InOrAgainstAt(se, all, day).Any(r => r.Party == np);
                bool datedToo = DeclaredRedLines.ForDate(se, all, day).Any(l => l.A == np && l.B == sd);
                d.BacksCandidateOf = "M";
                List<RedLine> backing = DeclaredRedLines.For(se, all, ElectionVintage.Sweden2026);
                bool backs = backing.Any(l => l.A == np && l.B == s && l.OneWay && DeclaredRedLines.IsCandidacy(l)) && !backing.Any(l => l.A == np && l.B == m && DeclaredRedLines.IsCandidacy(l));
                Check(red && oneWay && candidacy && inOrAgainst && datedToo && backs,
                    F("each kind is its rule: red line NP-SD symmetric and support-blocking {0}; one way NP>V {1}; its own candidacy paired with S's and M's both ways {2}; in-or-against voting against {3}; the dated reading too {4}; backing M's candidate refuses S's, not M's {5}", red, oneWay, candidacy, inOrAgainst, datedToo, backs));
                CreatedParties.Clear();

                // THE PROOF: the game's own round, a created party in the cabinet, then its red line against the partner
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                Country country = world.GetCountry(se);
                var twin = new CreatedParty { Country = se, Key = "NM", Name = "Nya Moderaterna", LeaderName = "N. Moderat" };
                twin.ApplyOrigin(PartyOrigin.Splinter); twin.ParentKey = "M"; twin.InheritedSlice = 0.1; twin.PlaceOn(real[m]);
                CreatedParties.TryRegister(twin, real, out _);
                int mSeats = country.ParliamentSeats.TryGetValue("M", out int held) ? held : 0;
                country.ParliamentSeats["NM"] = mSeats;   // the created party holds M's mandate, M none - M's place in the chamber taken
                country.ParliamentSeats["M"] = 0;
                GovernmentFormation.View before = GovernmentFormation.ViewOfSitting(country);
                List<string> cabinetBefore = before.Cabinet.Select(c => c.Abbrev).ToList();
                string partner = cabinetBefore.FirstOrDefault(a => a != "NM");
                sb.Append(F("    measured  without a line: {0} - cabinet {1}, support {2} ({3})\n", before.HasGovernment ? before.Outcome.ToString() : "NO GOVERNMENT", string.Join("+", cabinetBefore), string.Join("+", before.Support.Select(x => x.Abbrev)), before.Reason));
                Check(before.HasGovernment && cabinetBefore.Contains("NM") && partner != null, F("the round forms a cabinet with the created party in it: {0} (M's {1} seats held by NM)", string.Join("+", cabinetBefore), mSeats));

                twin.RedLinesAgainst.Add(partner ?? "KD");
                GovernmentFormation.View after = GovernmentFormation.ViewOfSitting(country);
                List<string> cabinetAfter = after.Cabinet.Select(c => c.Abbrev).ToList();
                sb.Append(F("    measured  NM declares a red line against {0}: {1} - cabinet {2}, support {3} ({4})\n", partner, after.HasGovernment ? after.Outcome.ToString() : "NO GOVERNMENT", string.Join("+", cabinetAfter), string.Join("+", after.Support.Select(x => x.Abbrev)), after.Reason));
                Check(!(cabinetAfter.Contains("NM") && cabinetAfter.Contains(partner)) && string.Join("+", cabinetAfter) != string.Join("+", cabinetBefore),
                    F("the red line blocks the cabinet: the round no longer forms {0}; it forms {1}", string.Join("+", cabinetBefore), after.HasGovernment ? string.Join("+", cabinetAfter) : "no government"));

                // the evaluator names the line that bars the first cabinet
                IReadOnlyList<PoliticalParty> parties = PartySystems.For(se);
                var seats = new int[parties.Count];
                int mask = 0;
                for (int p = 0; p < parties.Count; p++) { seats[p] = country.ParliamentSeats.TryGetValue(parties[p].Abbrev, out int h) ? h : 0; if (cabinetBefore.Contains(parties[p].Abbrev)) { mask |= 1 << p; } }
                CoalitionFormation.Chamber chamber = CoalitionFormation.Prepare(seats, GovernmentFormation.Compatibility(parties),
                    DeclaredRedLines.For(se, parties, GovernmentFormation.SittingVintage(country)), true, DeclaredRedLines.InOrAgainstFor(se, parties, GovernmentFormation.SittingVintage(country)));
                CoalitionFormation.CabinetEvaluation e = CoalitionFormation.Evaluate(chamber, mask);
                Check(!e.Admissible && e.InternalLine.Basis != null && e.InternalLine.Basis.StartsWith(DeclaredRedLines.CreatedPrefix, StringComparison.Ordinal),
                    F("the evaluator bars {0} by the created party's own declaration: \"{1}\"", string.Join("+", cabinetBefore), e.InternalLine.Basis));
            }
            catch (Exception ex) { failures++; sb.Append("    THREW: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace + "\n"); }
            finally { CreatedParties.Clear(); EnergyMarket.ResetTurnState(); }

            if (failures > 0) { Debug.LogError($"CREATED PARTY DECLARATIONS: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
