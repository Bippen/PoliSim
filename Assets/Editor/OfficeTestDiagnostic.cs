using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
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
