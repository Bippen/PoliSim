using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// **F-A — `CollectionEfficiency` double-counts the collection loss.** Opened by D-14 (a), which
    /// ruled that the D-2 (c) revert stands and that the two findings underneath it become their own
    /// measured items, unattached to the reverted change.
    ///
    /// <para>⚠ <b>MEASURES AND PROPOSES. APPLIES NOTHING.</b> It reads the world the game actually seeds,
    /// computes what each basis implies, and prints. It writes no constant and has no code path that
    /// could — the sourced table below is a LOCAL copy for arithmetic, not a wiring of it.</para>
    ///
    /// <para><b>The finding, in one line.</b> `CollectionEfficiency` is solved as `Target / Implied`,
    /// where `Implied = Σ(rate × base)`. Under the UNIFORM stand-in bases that is a theoretical figure
    /// deliberately larger than reality, and CE marks it down — which is what a word like "efficiency"
    /// means. But the SOURCED base is `(realised revenue % of GDP) / (seeded rate %)`, so
    /// `rate × base` **is the realised revenue already**. Marking it down again is applying one
    /// correction twice.</para>
    ///
    /// <para><b>What this prints, per country</b>: the implied revenue on each basis, the calibration
    /// target, today's CE, and the CE each basis would need. ⚠ **The number to read is the last column**:
    /// where it exceeds 1, the four modelled instruments do not cover that country's tax system, and the
    /// constant would be measuring coverage rather than efficiency.</para>
    /// </summary>
    public static class CollectionEfficiencyBasisDiagnostic
    {
        /// <summary>
        /// The USA's sourced rows ONLY, from `COMPLETED.md §197`'s D-9 sheet (OECD Revenue Statistics
        /// `DSD_REV_COMP_OECD`, general government, % of GDP, 2022 — income `T_1110`, corporate `T_1210`,
        /// payroll `T_2000+T_3000`; the USA has no VAT line). ⚠ **Local to this diagnostic on purpose**: the
        /// USA is EXCLUDED from `TaxBaseTable` by F-B's ruling (the federal perimeter), so these rows exist
        /// nowhere the game runs and are read here solely to SIZE the perimeter mismatch below. The five
        /// countries' rows moved into `TaxBaseTable` when D-16 (a) landed (2026-09-04) and are read from there.
        /// </summary>
        private static readonly Dictionary<string, float> Sourced = new Dictionary<string, float>
        {
            { "USA/IncomeTax", 0.3077f },     { "USA/CorporateTax", 0.0955f },     { "USA/PayrollTax", 0.3929f },
        };

        /// <summary>The calibration targets `WorldFactory`'s doc solves CE against — its own recorded
        /// table, re-typed here so the arithmetic below is checkable against the file that owns it.
        /// Eurostat `gov_10a_taxag` 2024 for the EU five; the USA's is FEDERAL, which is F-B's subject.</summary>
        private static readonly Dictionary<CountryId, float> TargetPercent = new Dictionary<CountryId, float>
        {
            { CountryId.USA, 18.0f }, { CountryId.Germany, 40.9f }, { CountryId.France, 45.3f },
            { CountryId.Italy, 42.5f }, { CountryId.Poland, 37.6f }, { CountryId.Sweden, 42.2f },
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();

            World world = WorldFactory.CreateDefault();

            var sb = new StringBuilder();
            sb.Append("=== F-A: does CollectionEfficiency double-count the collection loss? MEASURED; D-16 (a) EXECUTED 2026-09-04 (COMPLETED.md §282) ===\n");
            sb.Append("    Implied = sum over implemented tax lines of (seeded rate % x base). CE is solved as Target/Implied.\n");
            sb.Append("    UNIFORM = the stand-in bases (TaxTypeBaseShares). SOURCED = TaxBaseTable, the basis the game RUNS ON for\n");
            sb.Append("    the five since D-16 (a); the USA stays on the stand-in (F-B, sized below). 'CE today' should now EQUAL\n");
            sb.Append("    'CE needed(SOURCED)' for the five, less the seed tariff decrement (pass 5) - the coverage bridge, solved.\n\n");
            sb.Append("    country    implied(UNIFORM)  implied(SOURCED)   target    CE today   CE needed(UNIFORM)  CE needed(SOURCED)\n");
            sb.Append("    ---------------------------------------------------------------------------------------------------------\n");

            int measured = 0, aboveOne = 0;
            foreach (Country c in world.Countries)
            {
                if (!TargetPercent.TryGetValue(c.Id, out float target)) { continue; }

                float uniform = 0f, sourced = 0f;
                foreach (TaxLine line in c.TaxLines)
                {
                    if (!line.IsImplemented || line.Type == TaxType.Tariffs) { continue; }

                    float rate = line.Rate;
                    uniform += rate * TaxTypeBaseShares.GetBaseShareOfGdp(line.Type);
                    sourced += rate * TaxBaseTable.BaseShareOfGdp(c.Id, line.Type);
                }

                if (uniform <= 0f || sourced <= 0f)
                {
                    Debug.LogError($"F-A: {c.Id} implies zero revenue on one of the two bases (uniform {uniform}, "
                                   + $"sourced {sourced}), so no CE can be formed and this row measured NOTHING.");
                    continue;
                }

                measured++;
                float needUniform = target / uniform;
                float needSourced = target / sourced;
                if (needSourced > 1f) { aboveOne++; }

                sb.Append(F("    {0,-9} {1,16:F2} {2,17:F2} {3,9:F1} {4,11:F4} {5,19:F4} {6,19:F4}{7}\n",
                    c.Id, uniform, sourced, target, c.CollectionEfficiency, needUniform, needSourced,
                    needSourced > 1f ? "  ** ABOVE 1 **" : ""));
            }

            // The enumeration rule: a run that measured no country has said nothing about the shape, and
            // would print exactly like a run where the shape was fine.
            if (measured == 0)
            {
                Debug.LogError("F-A: not one country was measured, so this run verified NOTHING about the basis.");
                Debug.LogError(sb.ToString());
                CheckExit.Finish(1);
                return;
            }

            sb.Append(F("\n    {0} of {1} countries need CE ABOVE 1 to hit their target on the sourced basis.\n", aboveOne, measured));
            sb.Append("\n    THE SHAPE, READ OFF THE TABLE\n    -----------------------------\n");
            sb.Append("    ⚠ On the UNIFORM basis the implied figure is deliberately LARGER than reality and CE marks it\n");
            sb.Append("    down - which is what the word 'efficiency' means and why the USA's CE is below 1.\n");
            sb.Append("    ⚠ On the SOURCED basis, base = realised revenue / seeded rate, so rate x base IS the realised\n");
            sb.Append("    revenue. Marking it down again would apply one correction twice. Where CE exceeds 1, the four\n");
            sb.Append("    modelled instruments UNDER-COVER that country's tax system, and the constant measures COVERAGE\n");
            sb.Append("    rather than efficiency - which is what Country.CollectionEfficiency's doc now says it is.\n");
            sb.Append("\n    THE RULING, EXECUTED (D-14 (a) taken at D-16, 2026-09-01; landed 2026-09-04, COMPLETED.md §282)\n    ------------------------------------------------------------------------------------------\n");
            sb.Append("    Exit 1 of the three this diagnostic used to list: the five run on the sourced bases (TaxBaseTable),\n");
            sb.Append("    CE is re-solved per country as Target / Implied(SOURCED) less the seed tariff decrement, and it is\n");
            sb.Append("    documented as the coverage bridge. The anchored primary balance is preserved by construction; the\n");
            sb.Append("    response family moves (TaxTransmissionDiagnostic). The USA keeps the uniform stand-in and its CE below 1\n");
            sb.Append("    for F-B's reason, sized below. This block still measures rather than trusts: 'CE today' against\n");
            sb.Append("    'CE needed(SOURCED)' is the check that the solve held.\n");
            // ---------------------------------------------------------------------------------------
            // F-B, the USA's perimeter mismatch. ⚠ It lives HERE rather than in its own file because a
            // second diagnostic would have reprinted this one's arithmetic to add one ratio - and a tool
            // that mostly restates another tool is a second thing to keep true. The finding is its own
            // register row; the measurement is one block.
            // ---------------------------------------------------------------------------------------
            sb.Append("\n    F-B: THE USA'S PERIMETER MISMATCH\n    --------------------------------\n");
            float usaSourced = 0f, usaUniform = 0f;
            foreach (Country c in world.Countries)
            {
                if (c.Id != CountryId.USA) { continue; }
                foreach (TaxLine line in c.TaxLines)
                {
                    if (!line.IsImplemented || line.Type == TaxType.Tariffs) { continue; }
                    usaUniform += line.Rate * TaxTypeBaseShares.GetBaseShareOfGdp(line.Type);
                    usaSourced += line.Rate * (Sourced.TryGetValue("USA/" + line.Type, out float s)
                        ? s
                        : TaxTypeBaseShares.GetBaseShareOfGdp(line.Type));
                }
            }

            if (usaSourced <= 0f)
            {
                Debug.LogError("F-B: the USA implies zero revenue on the sourced basis, so the perimeter mismatch "
                               + "cannot be sized and this block measured NOTHING.");
            }
            else
            {
                float federalTarget = TargetPercent[CountryId.USA];
                sb.Append(F("    The sourced bases are GENERAL GOVERNMENT for all six - that is what the OECD flow they came\n"
                            + "    from publishes. The USA's whole calibration is FEDERAL, by WorldFactory's own perimeter rule,\n"
                            + "    because the state and local layer is not modelled.\n\n"
                            + "    USA implied revenue on the SOURCED (general-government) bases : {0:F2} % of GDP\n"
                            + "    USA calibration target, FEDERAL receipts (CBO FY2025, on disk) : {1:F2} % of GDP\n"
                            + "    ⚠ THE MISMATCH                                                 : x{2:F3}\n",
                    usaSourced, federalTarget, usaSourced / federalTarget));
                sb.Append("    Both figures are sourced and neither is ours: the first is OECD general-government revenue\n");
                sb.Append("    divided by the game's own seeded rates, the second is CBO federal receipts. They are simply\n");
                sb.Append("    about different governments. ⚠ THE USA'S ROW OF THE D-2 (c) TABLE IS NOT THE BASE OF THE\n");
                sb.Append("    THING THIS MODEL TAXES, and no amount of re-solving CE fixes that - it is a perimeter error,\n");
                sb.Append("    not a scaling one.\n");
                sb.Append("\n    THE BILL (nothing invented): FEDERAL-ONLY revenue by tax type as a share of GDP - individual\n");
                sb.Append("    income, corporate income, and payroll - for one stated year. OECD Revenue Statistics publishes\n");
                sb.Append("    a sub-sector split; two API shapes were tried from here and returned 422 and 404, so the series\n");
                sb.Append("    is NAMED rather than quoted. ⚠ Until it is on disk the USA has no sourced base and must keep\n");
                sb.Append("    the uniform stand-in, whatever the other five do - which is itself an argument against landing\n");
                sb.Append("    the table for five countries and not the sixth.\n");
            }

            sb.Append("\n    NO CONSTANT WAS MOVED AND THIS FILE HAS NO CODE PATH THAT COULD MOVE ONE.\n");

            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args)
            => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
