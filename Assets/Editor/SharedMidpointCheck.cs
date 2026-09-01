using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **S-26's defence clause: the dial midpoint is stated ONCE, and a fifth statement fails.**
    ///
    /// <para><b>The finding.</b> The dial midpoint `50` was stated four separate times — the public
    /// `CrimeJusticeCouplings.NeutralDialLevel`, `MacroSystem`'s private `NeutralPolicyDialLevel`, a local
    /// in `PolicyWebRenderer` and a local in `SimulationManager`. ⚠ **Each of the four carried a comment
    /// saying the others existed**, and one said outright that unifying them was *"a refactor this pass
    /// deliberately does not do"*. The fact was known, written down four times, and grew anyway.</para>
    ///
    /// <para>⚠ <b>Which is why unifying them is not the fix.</b> Four statements were reduced to one and
    /// three references, and that lasts exactly until the next author needs a midpoint and types `50f`.
    /// **A cleanup with no guard behind it is a snapshot, not a change** — and this repo has the record to
    /// prove it: the comment saying "three statements predate this pass" was written to stop a fourth, and
    /// a fourth arrived. **Prose asking people not to do something is the mechanism this project has
    /// catalogued as failing more often than any other.**</para>
    ///
    /// <para><b>What it decides, and it is deliberately narrow.</b> A `50f` literal bound to an identifier
    /// whose name reads like a midpoint — neutral, midpoint, baseline, dial — anywhere but the one
    /// declaration that owns it. ⚠ **It does NOT judge every 50 in the codebase**, because most are not
    /// this fact and a check that guesses is worse than none: `MinCurrencyStrength`, `MaxBaseTariffRate`
    /// and `NeutralApprovalRating` are all `50f` and all mean something else. **The name is the evidence**,
    /// and where the name is silent this check says nothing rather than something unfounded.</para>
    ///
    /// <para>⚠ <b>WHAT IT CANNOT SEE, stated rather than implied.</b> A fifth statement called `x` escapes
    /// it, exactly as `RatchetResidency`'s enrolment is escaped by a bound not named `*Ceiling`. That is a
    /// naming convention doing structural work, and it is the honest limit of a check whose subject is a
    /// bare number. The alternative — flagging every `50f` — was measured and rejected: it would fire on
    /// three unrelated constants today, and a guard that cries wolf gets widened, which is how a ratchet
    /// stops discriminating.</para>
    /// </summary>
    public static class SharedMidpointCheck
    {
        /// <summary>The one declaration allowed to state the value, file and member.</summary>
        private const string OwnerFile = "CrimeJusticeCouplings.cs";
        private const string OwnerMember = "NeutralDialLevel";

        /// <summary>A `50f` bound to a name that reads like this fact. ⚠ The name is the evidence; a
        /// midpoint called something else is outside what this check claims to see.</summary>
        private static readonly Regex MidpointFifty = new Regex(
            @"\b(?:const|readonly)?\s*float\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*50f\b");

        private static readonly Regex ReadsLikeMidpoint = new Regex(
            @"neutral|midpoint|baseline|dial", RegexOptions.IgnoreCase);

        /// <summary>⚠ **50f's that READ like this fact and ARE NOT, each with the argument for why.** Both
        /// were found by this check's first run, and both are real: the name test is deliberately broad and
        /// broad tests produce false positives, which are answered with an argument rather than by narrowing
        /// the test until it stops noticing.
        ///
        /// <para>⚠ **It is policed, because an exception list is the softest place in any guard.** Every
        /// entry must name a member that EXISTS in the file it names, and no entry may name the owning
        /// declaration - so this list cannot be used to excuse the one thing the check is for.</para></summary>
        private static readonly (string File, string Member, string Reason)[] DifferentFact =
        {
            ("Sector.cs", "BaselineRegulationLevel",
             "a per-sector SERIALISED FIELD DEFAULT, and its own doc says the adjustment is measured from "
             + "THIS value and NOT from the uniform 50 - it is the value a save lacking the field loads at, "
             + "the pre-ruling anchor. Pointing it at the shared const would make it the very thing the "
             + "ruling that created it decided it must not be."),

            ("MacroSystem.cs", "NeutralApprovalRating",
             "the midpoint of an APPROVAL RATING, not of a policy dial. Same number, different quantity - "
             + "and if approval's midpoint ever moved it would move alone."),
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            var offenders = new List<string>();
            var excusedHits = new List<string>();
            int owned = 0;
            int scanned = 0;

            string scripts = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "Scripts");
            foreach (string path in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                // R-T3's enrolment: a commented-out declaration is not a declaration, and a comment
                // DISCUSSING the four statements would otherwise read as a fifth one.
                string text = SourceText.WithoutComments(File.ReadAllText(path));
                string file = Path.GetFileName(path);
                scanned++;

                foreach (Match m in MidpointFifty.Matches(text))
                {
                    string name = m.Groups[1].Value;
                    if (!ReadsLikeMidpoint.IsMatch(name)) { continue; }

                    if (file == OwnerFile && name == OwnerMember) { owned++; continue; }

                    bool excused = false;
                    foreach (var d in DifferentFact)
                    {
                        if (d.File == file && d.Member == name) { excused = true; excusedHits.Add(file + "." + name); break; }
                    }

                    if (!excused) { offenders.Add(file + ": " + name); }
                }
            }

            var sb = new StringBuilder();
            sb.Append("=== S-26's defence: the dial midpoint, stated once ===\n");
            sb.Append("    THE ENUMERATION: ").Append(scanned).Append(" source file(s) under Assets/Scripts; ")
              .Append(owned).Append(" owning declaration(s); ").Append(offenders.Count).Append(" restatement(s).\n");
            sb.Append("    ⚠ Only a 50f bound to a name reading neutral/midpoint/baseline/dial counts. Every other\n");
            sb.Append("    50f in this repo means something else - MinCurrencyStrength, MaxBaseTariffRate and\n");
            sb.Append("    NeutralApprovalRating among them - and a check that guessed would be worse than none.\n");
            sb.Append("    A fifth statement named something unlike those words escapes this, which is a naming\n");
            sb.Append("    convention doing structural work and is stated here rather than discovered later.\n");
            foreach (string o in offenders) { sb.Append("    ⚠ RESTATED  ").Append(o).Append('\n'); }

            sb.Append("\n    DIFFERENT FACT, SAME NUMBER - the exception list, printed in full with its arguments:\n");
            foreach (var d in DifferentFact)
            {
                sb.Append("      ").Append(d.File).Append('.').Append(d.Member).Append(" - ").Append(d.Reason).Append('\n');
            }

            // ⚠ THE EXCEPTION LIST, POLICED. An entry naming something that is not there excuses nothing and
            // hides that it excuses nothing; an entry naming the OWNER would turn this check off from inside.
            var deadExcuses = new List<string>();
            foreach (var d in DifferentFact)
            {
                if (d.File == OwnerFile && d.Member == OwnerMember)
                {
                    deadExcuses.Add(d.File + "." + d.Member + " (names the OWNING declaration)");
                    continue;
                }

                if (!excusedHits.Contains(d.File + "." + d.Member))
                {
                    deadExcuses.Add(d.File + "." + d.Member + " (matches nothing in the tree)");
                }
            }

            foreach (string x in deadExcuses) { sb.Append("      ⚠ DEAD EXCUSE  ").Append(x).Append('\n'); }

            // The enumeration rule: if the OWNING declaration cannot be found, this check has read a repo it
            // does not understand and its zero means nothing.
            if (owned != 1)
            {
                Debug.LogError("MIDPOINT: expected exactly ONE owning declaration ("
                               + OwnerFile + "." + OwnerMember + ") and found " + owned
                               + ". Either it was renamed or moved, or this check is scanning the wrong tree - "
                               + "either way this run verified NOTHING, which is not the same as finding nothing.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            if (deadExcuses.Count > 0)
            {
                Debug.LogError("MIDPOINT: " + deadExcuses.Count + " exception(s) excuse nothing - "
                               + string.Join(", ", deadExcuses.ToArray())
                               + ". ⚠ An exception that matches nothing is not harmless: it reads as coverage, it "
                               + "survives the thing it was written for, and the next reader takes it as evidence that "
                               + "the case was considered. Delete it or fix what it names.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            if (offenders.Count > 0)
            {
                Debug.LogError("MIDPOINT: " + offenders.Count + " restatement(s) of the dial midpoint - "
                               + string.Join(", ", offenders.ToArray())
                               + ". ⚠ Reference " + OwnerFile + "." + OwnerMember + " instead. S-26 was four "
                               + "statements of one fact, each carrying a comment saying the others existed - so a "
                               + "note asking for restraint is exactly the mechanism that already failed here.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }
    }
}
