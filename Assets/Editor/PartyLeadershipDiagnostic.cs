using System.Globalization;
using System.Text;
using PoliSim.Data;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// C-D3 — **MP has two leaders and the model now carries both.**
    ///
    /// <para><b>The ruling</b> (Elias, 2026-08-31): *the model carries BOTH; the debate seats the one the
    /// party's own statutes or its published campaign materials put forward; if neither resolves it, seat
    /// neither and state the absence. <b>Never silently drop a real named person.</b>*</para>
    ///
    /// <para><b>The statutes were read, not assumed.</b> Miljöpartiet's stadgar elect <b>two equal
    /// språkrör</b> (§ 11.1) who must be of different genders (§ 11.2) and whose task is to represent the
    /// party (§ 11.4). ⚠ <b>They contain no clause designating one of them for debates or for any other
    /// setting.</b> So the ruling's fallback applies exactly: neither is seated, both are named, and the
    /// reason is the statute rather than a shrug.</para>
    ///
    /// <para>This asserts the four things that could go wrong.</para>
    /// <list type="number">
    /// <item><description><b>NOBODY IS DROPPED.</b> Every name in the sourced file appears in the model —
    /// checked by name, not by count, because a count matches while the wrong person is
    /// stored.</description></item>
    /// <item><description><b>MP carries two, and the debate seat is ABSENT BY DESIGN</b> with both
    /// språkrör named in the reason.</description></item>
    /// <item><description><b>The other seven resolve to their one leader.</b></description></item>
    /// <item><description>⚠ <b>The two absences stay distinct.</b> A party with no sourced leader reports
    /// `NotSourced`, never `AbsentByDesign` — the model not knowing who leads a party is a different
    /// statement from the party genuinely having two equals, and C-C8's precedent is that an absence must
    /// say which absence it is.</description></item>
    /// </list>
    /// </summary>
    public static class PartyLeadershipDiagnostic
    {
        /// <summary>The sourced roster, from `ElectionsData/sweden/party_leaders_2022.md` (vintage
        /// 2022-09-11). ⚠ Duplicated here ON PURPOSE: a diagnostic that read the same array it is checking
        /// would assert nothing. This is the file's content, and the assertion is that the model agrees
        /// with it.</summary>
        private static readonly (string Abbrev, string[] Leaders)[] Expected =
        {
            ("S", new[] { "Magdalena Andersson" }),
            ("SD", new[] { "Jimmie Akesson" }),
            ("M", new[] { "Ulf Kristersson" }),
            ("V", new[] { "Nooshi Dadgostar" }),
            ("C", new[] { "Annie Loof" }),
            ("KD", new[] { "Ebba Busch" }),
            ("MP", new[] { "Marta Stenevi", "Per Bolund" }),
            ("L", new[] { "Johan Pehrson" }),
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var sb = new StringBuilder();
            sb.Append("=== C-D3: the party leadership model - both spakror carried, neither seated ===\n");
            int failures = 0;

            System.Collections.Generic.IReadOnlyList<PoliticalParty> sweden = PartySystems.For(CountryId.Sweden);

            // ---- 1. nobody is dropped, checked BY NAME ----
            foreach ((string abbrev, string[] expected) in Expected)
            {
                PoliticalParty party = Find(sweden, abbrev);
                if (party.Abbrev == null)
                {
                    failures++;
                    Debug.LogError($"C-D3: {abbrev} is not in the Swedish party system at all.");
                    continue;
                }

                foreach (string name in expected)
                {
                    bool found = false;
                    foreach (PartyLeader l in party.Leaders) { found |= l.Name == name; }
                    if (found) { continue; }

                    failures++;
                    Debug.LogError($"C-D3: {name} is named as {abbrev}'s leader in party_leaders_2022.md and is NOT in the "
                                   + "model. A real named person has been dropped, which is the one thing this item's ruling "
                                   + "forbids outright.");
                }

                if (party.Leaders.Length != expected.Length)
                {
                    failures++;
                    Debug.LogError($"C-D3: {abbrev} carries {party.Leaders.Length} leader(s), the sourced file names {expected.Length}.");
                }
            }

            sb.Append(failures == 0
                ? "    1. nobody dropped   OK - every name in the sourced file is in the model, checked by NAME.\n"
                : "    1. nobody dropped   ⚠ see the errors above.\n");

            // ---- 2 and 3. the debate seat, per party ----
            sb.Append("\n    party   leaders                                          debate seat\n");
            sb.Append("    ------------------------------------------------------------------------------\n");

            int resolved = 0;
            int absentByDesign = 0;
            foreach (PoliticalParty party in sweden)
            {
                DebateSeat seat = party.ResolveDebateSeat(out PartyLeader leader, out string reason);
                var names = new StringBuilder();
                foreach (PartyLeader l in party.Leaders)
                {
                    if (names.Length > 0) { names.Append(" + "); }
                    names.Append(l.Name).Append(" (").Append(l.Office).Append(')');
                }

                string verdict;
                switch (seat)
                {
                    case DebateSeat.Resolved: resolved++; verdict = "seats " + leader.Name; break;
                    case DebateSeat.AbsentByDesign: absentByDesign++; verdict = "⚠ ABSENT BY DESIGN"; break;
                    default: verdict = "not sourced"; break;
                }

                sb.Append(F("    {0,-6} {1,-48} {2}\n", party.Abbrev, names.ToString(), verdict));

                if (party.Abbrev == "MP")
                {
                    bool right = seat == DebateSeat.AbsentByDesign
                                 && reason != null && reason.Contains("Marta Stenevi") && reason.Contains("Per Bolund");
                    if (!right)
                    {
                        failures++;
                        Debug.LogError("C-D3: MP's debate seat must be ABSENT BY DESIGN with BOTH sprakror named in the reason. "
                                       + "Seating one of two equals, or reporting the absence without naming who was not seated, "
                                       + "are the two ways this item goes wrong.");
                    }

                    sb.Append("           reason: ").Append(reason).Append('\n');
                }
            }

            if (resolved != 7 || absentByDesign != 1)
            {
                failures++;
                Debug.LogError($"C-D3: expected 7 parties to seat one leader and exactly 1 (MP) to be absent by design; "
                               + $"got {resolved} and {absentByDesign}.");
            }

            sb.Append(F("\n    2. the seats        {0} resolved, {1} absent by design.\n", resolved, absentByDesign));

            // ---- 4. the two absences stay distinct ----
            System.Collections.Generic.IReadOnlyList<PoliticalParty> germany = PartySystems.For(CountryId.Germany);
            int notSourced = 0;
            int wrongAbsence = 0;
            foreach (PoliticalParty party in germany)
            {
                DebateSeat seat = party.ResolveDebateSeat(out _, out _);
                if (seat == DebateSeat.NotSourced) { notSourced++; }
                if (seat == DebateSeat.AbsentByDesign) { wrongAbsence++; }
            }

            if (wrongAbsence > 0 || notSourced != germany.Count)
            {
                failures++;
                Debug.LogError($"C-D3: {germany.Count} German parties have no sourced leader, so every one must report "
                               + $"NotSourced; got {notSourced} NotSourced and {wrongAbsence} AbsentByDesign. "
                               + "\"The model does not know who leads this party\" and \"this party genuinely has two equal "
                               + "leaders\" are different statements and must not collapse into one.");
            }

            sb.Append(F("    3. the two absences OK - all {0} German parties report NOT SOURCED, none reports absent-by-design.\n", notSourced));

            sb.Append(F("\n=== PartyLeadershipDiagnostic: {0} ===\n", failures == 0 ? "ALL ASSERTIONS PASS" : failures + " FAILURE(S)"));

            if (failures == 0) { Debug.Log(sb.ToString()); CheckExit.Finish(0); }
            else { Debug.LogError(sb.ToString()); CheckExit.Finish(1); }
        }

        private static PoliticalParty Find(System.Collections.Generic.IReadOnlyList<PoliticalParty> parties, string abbrev)
        {
            foreach (PoliticalParty p in parties)
            {
                if (p.Abbrev == abbrev) { return p; }
            }

            return default;
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
