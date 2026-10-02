using System.Collections.Generic;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// §737 (UI v3.5; the main session's rule, relayed 2026-10-01: *"Use a real unit only where the model computes one. Where a dial is an abstract
    /// index, use named settings, with the band edges marked [AUTHORED-DRAFT]. Never display a unit the model doesn't compute."*): THE NAMED
    /// SETTINGS of the dials the model holds as 0-100 indices whose 50 is the country's own status quo. The stops are DISPLAY bands over the
    /// continuous index - the knob still moves the index and the model still takes every effect on (level − 50); the stop holding 50 is today's.
    /// The names are Design's where Design proposed them (V35_ASK, the composition) and Code's mapping where the proposal's unit was not the
    /// model's (`PoliSim-captures/design/V35_answers/V35_ANSWERS.md` §1, sent to Design 2026-10-01). Every band edge is **[AUTHORED-DRAFT]** -
    /// a game figure, to be ruled - and the slip says so.
    /// </summary>
    public static class DialStops
    {
        public readonly struct Stop
        {
            public readonly string Name;
            /// <summary>The band's lower edge on the 0-100 index [AUTHORED-DRAFT]; the band runs to the next stop's edge (the last to 100).</summary>
            public readonly float From;
            public Stop(string name, float from) { Name = name; From = from; }
        }

        public sealed class Dial
        {
            public readonly string Key;
            public readonly string Title;
            public readonly Stop[] Stops;
            public Dial(string key, string title, params Stop[] stops) { Key = key; Title = title; Stops = stops; }

            /// <summary>The stop the index falls in.</summary>
            public Stop At(float level)
            {
                Stop s = Stops[0];
                foreach (Stop t in Stops) { if (level >= t.From) { s = t; } }
                return s;
            }

            /// <summary>The bands as the slip prints them: "LOOSE 0–35 · STANDARD 35–65 · STRICT 65–100".</summary>
            public string Bands()
            {
                var parts = new List<string>();
                for (int i = 0; i < Stops.Length; i++)
                {
                    float to = i + 1 < Stops.Length ? Stops[i + 1].From : 100f;
                    parts.Add(Stops[i].Name.ToUpperInvariant() + " " + Mathf.RoundToInt(Stops[i].From) + "–" + Mathf.RoundToInt(to));
                }
                return string.Join(" · ", parts);
            }
        }

        // ---- Labour (V35_ANSWERS §1, Labour) ----
        /// <summary>`OvertimeRegulationLevel`: 0 unregulated … 100 strict caps; the model computes no hours. [AUTHORED-DRAFT] edges. ⚠ Design's 35 h → 60 h
        /// would run opposite to the index; the stops keep the model's direction (Loose left).</summary>
        public static readonly Dial WorkingHours = new Dial("Working-hours rules", "Working-hours rules", new Stop("Loose", 0f), new Stop("Standard", 35f), new Stop("Strict", 65f));
        /// <summary>`RetrainingProgramLevel`: no spending computed. [AUTHORED-DRAFT] edges.</summary>
        public static readonly Dial Retraining = new Dial("Retraining", "Retraining", new Stop("Little", 0f), new Stop("Today's", 35f), new Stop("Much", 65f));
        /// <summary>`FamilyPolicyLevel`: 0 minimal … 100 pro-natalist; moves the birth rate; no money computed. [AUTHORED-DRAFT] edges.</summary>
        public static readonly Dial FamilySupport = new Dial("Family support", "Family support", new Stop("Minimal", 0f), new Stop("Today's", 35f), new Stop("Generous", 65f));

        // ---- Crime & justice (V35_ANSWERS §1, Crime) - set by law ----
        /// <summary>`SentencingSeverity`: 0 lenient … 100 harsh; moves incarceration per 100 000. [AUTHORED-DRAFT] edges.</summary>
        public static readonly Dial Sentencing = new Dial("Sentencing", "Sentencing", new Stop("Lenient", 0f), new Stop("Today's", 35f), new Stop("Harsh", 65f));
        /// <summary>`DrugPolicyLevel`: 0 decriminalised … 100 strict - Design's four stops, "Legal" read as Decriminalised (the model's 0). Sweden's 50 is
        /// Criminal. [AUTHORED-DRAFT] edges.</summary>
        public static readonly Dial DrugPolicy = new Dial("Drug possession", "Drug possession", new Stop("Decriminalised", 0f), new Stop("Fine", 20f), new Stop("Criminal", 40f), new Stop("Prison", 75f));
        /// <summary>`BorderEnforcementLevel`: 0 open … 100 strict - Design's four stops; Sweden's 50 is Spot checks. [AUTHORED-DRAFT] edges.</summary>
        public static readonly Dial Border = new Dial("Border checks", "Border checks", new Stop("Open", 0f), new Stop("Spot checks", 25f), new Stop("Systematic", 60f), new Stop("Closed", 85f));

        // ---- Sectors and Energy (V35_ANSWERS §1, Sectors) ----
        /// <summary>`TaxCreditLevel`: a general sector tax credit (not R&D-specific). [AUTHORED-DRAFT] edges.</summary>
        public static readonly Dial TaxCredits = new Dial("Tax credits", "Tax credits", new Stop("Less", 0f), new Stop("Today's", 35f), new Stop("More", 65f));
        /// <summary>`ResearchGrantsLevel`. [AUTHORED-DRAFT] edges.</summary>
        public static readonly Dial ResearchGrants = new Dial("Research grants", "Research grants", new Stop("Less", 0f), new Stop("Today's", 35f), new Stop("More", 65f));
        /// <summary>`DeregulationNationalizationLevel`: 0 nationalised … 100 deregulated; no ownership share computed. [AUTHORED-DRAFT] edges.</summary>
        public static readonly Dial Ownership = new Dial("Ownership", "Ownership", new Stop("Nationalised", 0f), new Stop("Mixed", 35f), new Stop("Deregulated", 65f));
        /// <summary>Energy `SubsidyLevel`: the retail price support; the cost a year is its figure. [AUTHORED-DRAFT] edges.</summary>
        public static readonly Dial RetailSupport = new Dial("Retail price support", "Retail price support", new Stop("None", 0f), new Stop("Today's", 35f), new Stop("Large", 65f));
    }
}
