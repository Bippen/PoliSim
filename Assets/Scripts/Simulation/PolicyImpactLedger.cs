using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>One attributed line of the impact ledger: what a family of dials contributed to a stat's
    /// divergence from the no-policy baseline.</summary>
    public struct ImpactLine
    {
        public string Family;
        public float Contribution;
    }

    /// <summary>
    /// C-C10 (P-G2) — **the impact ledger: the live-vs-shadow divergence, attributed to the dials that
    /// caused it, with the part that belongs to no single dial shown as its own line.**
    ///
    /// <para><b>The method is LEAVE-ONE-OUT</b>, the strongest attribution available for a model whose
    /// dials interact. Beside the real game runs C-C9's no-policy shadow, and — for each FAMILY of dials
    /// the player has actually touched — a world running everything the player did except that family.
    /// Then</para>
    ///
    /// <list type="bullet">
    /// <item><description><c>divergence = real − none</c>, the whole gap to explain;</description></item>
    /// <item><description><c>contribution(f) = real − except(f)</c>, what removing that family would
    /// have changed with every other dial still in play;</description></item>
    /// <item><description><c>interaction = divergence − Σ contribution(f)</c>.</description></item>
    /// </list>
    ///
    /// <para>⚠ <b>THE INTERACTION LINE IS NOT AN ERROR TERM, AND IT IS NOT OPTIONAL.</b> Measured before
    /// this class was written (`ImpactLedgerFeasibilityDiagnostic`, `COMPLETED.md` §106): over 12 turns of
    /// four dials on the USA it reaches <b>17.4 % of the divergence on Government Debt</b> and 12.6 % on
    /// the Budget. A tax rise and a spending rise meet in the same GDP; no decomposition into per-dial
    /// lines can be exact unless the model is linear in its dials, and it is not. So the ledger prints
    /// the interaction as a named line rather than folding it into the largest contributor or scaling the
    /// lines to close the sum — Elias's ruling for this item: <b>an honest residual beats a false
    /// identity.</b> With the interaction line present the ledger sums EXACTLY, by construction, and
    /// <see cref="PolicyImpactLedgerDiagnostic"/> asserts that identity rather than trusting it.</para>
    ///
    /// <para><b>Cost.</b> A family the player has never touched has an except-world identical to the real
    /// game, so it costs nothing to not have one; the fork happens on FIRST TOUCH and is exact, not an
    /// approximation (see <see cref="ShadowBaseline"/>'s fork constructor). A game where the player only
    /// ever moves taxes therefore pays for two counterfactual worlds, not eleven.</para>
    /// </summary>
    public sealed class PolicyImpactLedger
    {
        /// <summary>
        /// ⚠ **The families partition `PolicyDecision`'s fields, and the partition is CHECKED.**
        /// A hand-written list of field names is exactly the shape R4-1's clone-escape class warns about
        /// — a new field added to `PolicyDecision` would silently belong to no family and vanish from
        /// every attribution. So <see cref="AssertPartitionCovers"/> compares this table against the
        /// type's real fields at construction and throws naming the offender. The list is a hand-list
        /// because grouping dials into families is a JUDGEMENT and cannot be derived; that it is
        /// COMPLETE is not a judgement, and is not left to one.
        /// </summary>
        private static readonly Dictionary<string, string[]> Families = new Dictionary<string, string[]>
        {
            { "Taxes", new[] { "TaxRateOverrides" } },
            { "Welfare", new[] { "WelfareGenerosityOverrides" } },
            {
                "Spending", new[]
                {
                    "SpendingLineChanges", "SpendingNominalTargets", "SpendingPinChanges",   // P5-B2's figure and pin - the same spending family
                    "HealthcareSpendingChange", "DefenseSpendingChange",
                    "InfrastructureSpendingChange", "EducationSpendingChange", "JusticeSpendingChange",
                    "HomelandSecuritySpendingChange", "EnergySpendingChange", "HousingSpendingChange"
                }
            },
            { "Trade", new[] { "TariffRateChange", "PartnerTariffOverrides" } },
            // ⚠ S-18: since C-C7 every country has a central-bank head, so ApplyInterestRateChanges never
            // reads this field and its contribution is exactly zero. It stays a family of its own so the
            // ledger reports that zero rather than hiding a dead lever inside another family's line.
            { "Monetary", new[] { "InterestRateChange" } },
            {
                "Labour", new[]
                {
                    "MinimumWageOverride", "PaidFamilyLeaveWeeksOverride", "OvertimeRegulationOverride",
                    "RetrainingProgramOverride"
                }
            },
            {
                "Crime and justice", new[]
                {
                    "PoliceFundingOverride", "SentencingSeverityOverride", "BailReformOverride",
                    "DrugPolicyOverride", "JudicialFundingOverride", "BorderEnforcementOverride"
                }
            },
            {
                "Sectors", new[]
                {
                    "SectorSubsidyOverrides", "SectorRegulationOverrides", "SectorTaxCreditOverrides",
                    "SectorResearchGrantsOverrides", "SectorDeregulationNationalizationOverrides"
                }
            },
            {
                "Sovereign wealth", new[]
                {
                    "SwfContributionRateOverride", "SwfDomesticAllocationOverride", "SwfEquitiesWeightOverride",
                    "SwfBondsWeightOverride", "SwfInfrastructureWeightOverride", "SwfRealEstateWeightOverride"
                }
            },
            { "Demographics", new[] { "FamilyPolicyOverride", "ImmigrationPolicyOverride" } }
        };

        /// ⚠ Not bound at construction. `GameController.Start` runs BEFORE the player picks a country, so
        /// an id captured then is the default one and every later attribution would be read off the wrong
        /// country - which is exactly what the first film of this panel showed. The id travels with the
        /// call instead.
        private CountryId _playerCountryId;
        private readonly ShadowBaseline _none;
        private readonly Dictionary<string, ShadowBaseline> _except = new Dictionary<string, ShadowBaseline>();

        /// <summary>The no-policy counterfactual — C-C9's shadow, which this ledger measures against.</summary>
        public ShadowBaseline NoPolicy => _none;

        public PolicyImpactLedger(ShadowBaseline noPolicy)
        {
            AssertPartitionCovers();

            _none = noPolicy;
        }

        /// <summary>
        /// ⚠ **Call this BEFORE the real game advances the same turn.** A family first touched this turn
        /// forks from the state at the turn's START, which is precisely the state its except-world would
        /// have been in had it existed from the beginning — that is what makes the lazy fork exact.
        /// </summary>
        public void AdvanceTurn(SimulationManager realSim, World realWorld, CountryId playerCountryId, Dictionary<CountryId, PolicyDecision> decisions)
        {
            _playerCountryId = playerCountryId;
            decisions.TryGetValue(_playerCountryId, out PolicyDecision playerDecision);

            foreach (KeyValuePair<string, string[]> family in Families)
            {
                bool touched = playerDecision != null && Touches(playerDecision, family.Value);
                if (touched && !_except.ContainsKey(family.Key))
                {
                    _except[family.Key] = new ShadowBaseline(realSim, realWorld, _playerCountryId);
                }

                if (!_except.TryGetValue(family.Key, out ShadowBaseline world)) { continue; }

                world.AdvanceTurn(Strip(decisions, family.Value));
            }
        }

        /// <summary>
        /// The ledger for one stat: every touched family's contribution, then the interaction, in an
        /// order that reads — largest absolute contribution first, the interaction always last because it
        /// is the qualification on everything above it rather than another cause.
        /// </summary>
        public List<ImpactLine> LinesFor(Country realCountry, string statField, out float divergence)
        {
            float real = Read(realCountry, statField);
            CountryId id = realCountry.Id;
            float none = Read(_none.CountryFor(id), statField);
            divergence = real - none;

            var lines = new List<ImpactLine>();
            float attributed = 0f;
            foreach (KeyValuePair<string, ShadowBaseline> pair in _except)
            {
                float contribution = real - Read(pair.Value.CountryFor(id), statField);
                attributed += contribution;
                lines.Add(new ImpactLine { Family = pair.Key, Contribution = contribution });
            }

            lines.Sort((a, b) => Mathf.Abs(b.Contribution).CompareTo(Mathf.Abs(a.Contribution)));
            lines.Add(new ImpactLine { Family = "interaction", Contribution = divergence - attributed });
            return lines;
        }

        /// <summary>True once at least one family has been touched — before that the ledger has nothing
        /// to explain and the screen says so rather than printing an empty table.</summary>
        public bool HasAnything => _except.Count > 0;

        private static float Read(Country country, string statField)
        {
            if (country == null) { return 0f; }

            FieldInfo field = typeof(EconomyState).GetField(statField);
            return field == null ? 0f : (float)field.GetValue(country.State);
        }

        /// <summary>A copy of every country's decision with one family's fields returned to their
        /// untouched defaults. ⚠ The copy is by REFLECTION over every field, so a field added to
        /// `PolicyDecision` travels automatically; only the family membership is a hand-list, and that
        /// list is checked to be complete.</summary>
        private Dictionary<CountryId, PolicyDecision> Strip(Dictionary<CountryId, PolicyDecision> decisions, string[] familyFields)
        {
            var stripped = new Dictionary<CountryId, PolicyDecision>();
            var defaults = new PolicyDecision();
            var family = new HashSet<string>(familyFields);

            foreach (KeyValuePair<CountryId, PolicyDecision> pair in decisions)
            {
                if (pair.Key != _playerCountryId || pair.Value == null)
                {
                    stripped[pair.Key] = pair.Value ?? PolicyDecision.None();
                    continue;
                }

                var copy = new PolicyDecision();
                foreach (FieldInfo f in typeof(PolicyDecision).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    object source = family.Contains(f.Name) ? f.GetValue(defaults) : f.GetValue(pair.Value);
                    f.SetValue(copy, CopyValue(f.FieldType, source));
                }

                stripped[pair.Key] = copy;
            }

            return stripped;
        }

        /// <summary>Dictionaries are copied rather than shared, so a stripped decision can never be a
        /// second reference to the live one.</summary>
        private static object CopyValue(Type type, object value)
        {
            if (value is IDictionary source)
            {
                var copy = (IDictionary)Activator.CreateInstance(type);
                foreach (DictionaryEntry entry in source) { copy[entry.Key] = entry.Value; }
                return copy;
            }

            return value;
        }

        private static bool Touches(PolicyDecision decision, string[] familyFields)
        {
            var defaults = new PolicyDecision();
            foreach (string name in familyFields)
            {
                FieldInfo f = typeof(PolicyDecision).GetField(name);
                if (f == null) { continue; }

                object value = f.GetValue(decision);
                if (value is IDictionary dictionary) { if (dictionary.Count > 0) { return true; } continue; }
                if (!Equals(value, f.GetValue(defaults))) { return true; }
            }

            return false;
        }

        /// <summary>⚠ The completeness check. A `PolicyDecision` field in no family would be a dial the
        /// ledger silently never attributes; a name in the table that is not a field would be a family
        /// that quietly attributes nothing. Both throw, naming the offender.</summary>
        private static void AssertPartitionCovers()   // PolicyImpactLedgerCheck reaches it by reflection on the cheap bar (P5-B5) - a harness reaching private state is the project's idiom
        {
            var claimed = new HashSet<string>();
            foreach (KeyValuePair<string, string[]> family in Families)
            {
                foreach (string name in family.Value)
                {
                    if (typeof(PolicyDecision).GetField(name) == null)
                    {
                        throw new InvalidOperationException(
                            $"PolicyImpactLedger: family '{family.Key}' names '{name}', which is not a field of PolicyDecision.");
                    }

                    if (!claimed.Add(name))
                    {
                        throw new InvalidOperationException(
                            $"PolicyImpactLedger: '{name}' is claimed by more than one family, so leave-one-out would double-count it.");
                    }
                }
            }

            foreach (FieldInfo f in typeof(PolicyDecision).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!claimed.Contains(f.Name))
                {
                    throw new InvalidOperationException(
                        $"PolicyImpactLedger: PolicyDecision.{f.Name} belongs to no family, so the impact ledger would never attribute it. "
                        + "Add it to the family it belongs to - a dial with no family is a dial the player cannot be told about.");
                }
            }
        }

        public void Dispose()
        {
            foreach (KeyValuePair<string, ShadowBaseline> pair in _except) { pair.Value.Dispose(); }
            _except.Clear();
        }
    }
}
