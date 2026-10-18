using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// AF-31 (q31, ruled 2026-10-18): **EVERY CARD'S OPTIONS TRADE OFF.** GA-9-1 measured the meeting drip: "Stand firm on the original position" won
    /// approval and the trade balance both against the conciliatory statement, so answering every card for its approval gained approval for nothing.
    /// This check reads every card the player answers - the cabinet decision pool and the foreign-policy meeting pool, by reflection (the project's idiom:
    /// `DomesticMoneyBasisDiagnostic`) - and FAILS on any option that DOMINATES another on the same card: at least as good on every LASTING count and
    /// better on one. So every option costs something that lasts, on some count, against each of its rivals.
    ///
    /// <para><b>The counts and their directions</b> (DECLARED): approval and budget are better higher; the crime index, the poverty rate and youth
    /// unemployment better lower. NOT counts, each with its reason in <see cref="NotCounts"/>: the trade-balance nudge (the trade system recomputes the
    /// balance at the next boundary, so it costs nothing that lasts - the review of AF-31 - and the game reads the balance in neutral ink), the state
    /// lean (a label no rule reads, §755) and the label. Every field of both option structs must be one or the other: a new effect field fails this check
    /// until it is classified, so none can go uncounted. The figures are compared as authored: the scalings applied at the pick (POPULARITY on a cabinet
    /// option's approval, `AuthoredImpactScale` on budget) are positive factors per count and cannot turn dominance into a trade-off, or back.</para>
    ///
    /// <para>It fails, too, on a pool it cannot read, or one that reads empty - a check that found nothing to hold has held nothing.</para>
    /// </summary>
    public static class CardTradeOffCheck
    {
        private static string F(string f, params object[] a) => string.Format(CultureInfo.InvariantCulture, f, a);

        /// <summary>The lasting counts by FIELD name, shared by both option structs where they carry the field: +1 better higher, -1 better lower.</summary>
        private static readonly Dictionary<string, float> Counted = new Dictionary<string, float>
        {
            { "ApprovalEffect", +1f },
            { "BudgetImpact", +1f },
            { "CrimeIndexShock", -1f },
            { "PovertyRateShock", -1f },
            { "YouthUnemploymentShock", -1f },
        };

        /// <summary>The fields that are not counts, each with its reason.</summary>
        private static readonly Dictionary<string, string> NotCounts = new Dictionary<string, string>
        {
            { "Label", "the option's words" },
            { "TradeBalanceShock", "a nudge the trade system overwrites at the next boundary (TradeSystem recomputes the balance), read in neutral ink - no lasting cost or gain" },
            { "StateLean", "a label no rule reads (§755 retired its reader)" },
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== CardTradeOffCheck (AF-31, q31): every card's options trade off - no option wins on every lasting count ===\n");
            int failures = 0;

            // Every field classified - a new effect field cannot go uncounted.
            var countsCabinet = new List<FieldInfo>();
            var countsMeeting = new List<FieldInfo>();
            foreach ((Type type, List<FieldInfo> into) in new[] { (typeof(CabinetDecisionOption), countsCabinet), (typeof(ForeignPolicyMeetingOption), countsMeeting) })
            {
                foreach (FieldInfo f in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (Counted.ContainsKey(f.Name)) { into.Add(f); continue; }
                    if (NotCounts.ContainsKey(f.Name)) { continue; }
                    failures++;
                    sb.Append(F("    FAIL      {0}.{1} is neither a lasting count nor a named non-count - classify it in CardTradeOffCheck before a card can carry it\n", type.Name, f.Name));
                }
            }

            var cards = new List<(string Card, List<(string Label, float[] Counts)> Options)>();
            int cabinetCards = 0, meetingCards = 0;
            FieldInfo cabinetField = typeof(CabinetSystem).GetField("DecisionPool", BindingFlags.NonPublic | BindingFlags.Static);
            if (cabinetField?.GetValue(null) is IDictionary cabinetPool)
            {
                foreach (DictionaryEntry entry in cabinetPool)
                {
                    if (!(entry.Value is List<CabinetDecision> decisions)) { continue; }
                    foreach (CabinetDecision decision in decisions)
                    {
                        var options = new List<(string, float[])>();
                        foreach (CabinetDecisionOption option in decision.Options) { options.Add((option.Label, Counts(option, countsCabinet))); }
                        cards.Add(("cabinet " + entry.Key + ": " + decision.Name, options));
                        cabinetCards++;
                    }
                }
            }

            FieldInfo meetingField = typeof(ForeignPolicySystem).GetField("MeetingPool", BindingFlags.NonPublic | BindingFlags.Static);
            if (meetingField?.GetValue(null) is List<ForeignPolicyMeeting> meetings)
            {
                foreach (ForeignPolicyMeeting meeting in meetings)
                {
                    var options = new List<(string, float[])>();
                    foreach (ForeignPolicyMeetingOption option in meeting.Options) { options.Add((option.Label, Counts(option, countsMeeting))); }
                    cards.Add(("meeting: " + meeting.Name, options));
                    meetingCards++;
                }
            }

            if (cabinetCards == 0) { failures++; sb.Append("    FAIL      the cabinet decision pool (CabinetSystem.DecisionPool) read no card - renamed, retyped or empty\n"); }
            if (meetingCards == 0) { failures++; sb.Append("    FAIL      the foreign-policy meeting pool (ForeignPolicySystem.MeetingPool) read no card - renamed, retyped or empty\n"); }

            int pairs = 0;
            foreach ((string card, List<(string Label, float[] Counts)> options) in cards)
            {
                if (options.Count < 2) { failures++; sb.Append(F("    FAIL      {0}: {1} option(s) - a card with one answer trades nothing off\n", card, options.Count)); continue; }
                for (int i = 0; i < options.Count; i++)
                {
                    for (int j = 0; j < options.Count; j++)
                    {
                        if (i == j) { continue; }
                        pairs++;
                        if (!Dominates(options[i].Counts, options[j].Counts, out string where)) { continue; }
                        failures++;
                        sb.Append(F("    FAIL      {0}: \"{1}\" dominates \"{2}\" - at least as good on every lasting count, better on {3}\n", card, options[i].Label, options[j].Label, where));
                    }
                }
            }

            var countNames = new List<string>(Counted.Keys);
            sb.Append(F("    measured  {0} cabinet card(s) and {1} meeting card(s), {2} ordered option pair(s) compared on {3} lasting counts ({4}); not counted: {5}\n",
                cabinetCards, meetingCards, pairs, countNames.Count, string.Join(", ", countNames), string.Join(", ", NotCounts.Keys)));

            if (failures > 0) { Debug.LogError(F("CARD TRADE-OFF: {0} failure(s).\n{1}", failures, sb)); CheckExit.Finish(1); return; }
            sb.Append("    VERDICT: every option on every card costs something that lasts, on some count, against each of its rivals.\n");
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        /// <summary>An option's lasting counts in <see cref="Counted"/>'s key order, signed so that higher is better; a count the struct does not carry is 0.</summary>
        private static float[] Counts(object option, List<FieldInfo> fields)
        {
            var values = new float[Counted.Count];
            int k = 0;
            foreach (KeyValuePair<string, float> count in Counted)
            {
                FieldInfo f = fields.Find(x => x.Name == count.Key);
                values[k++] = f == null ? 0f : count.Value * Convert.ToSingle(f.GetValue(option), CultureInfo.InvariantCulture);
            }
            return values;
        }

        /// <summary>True where <paramref name="a"/> is at least as good as <paramref name="b"/> on every count and better on at least one (both already signed
        /// higher-is-better); <paramref name="where"/> names the counts it is better on.</summary>
        private static bool Dominates(float[] a, float[] b, out string where)
        {
            var better = new List<string>();
            var names = new List<string>(Counted.Keys);
            for (int k = 0; k < a.Length; k++)
            {
                if (a[k] < b[k]) { where = null; return false; }
                if (a[k] > b[k]) { better.Add(names[k]); }
            }
            where = string.Join(", ", better);
            return better.Count > 0;
        }
    }
}
