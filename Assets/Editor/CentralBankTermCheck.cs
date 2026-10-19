using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// AF-7 (Elias's rulings q7 and q10 of 2026-10-18; the feature list's row AF-7, done when "each bank's term sourced and held by a check"): <b>EACH
    /// CENTRAL BANK'S HEAD SERVES ITS OWN STATUTE'S TERM</b> (<see cref="CentralBankTerms"/>). It holds:
    /// <list type="bullet">
    /// <item>(a) every statute row to its saved page - the page to its SHA-256 in SHA256SUMS.txt, the row's words found on it, and the term the row
    /// carries read back off those words (a reading the words leave open must be DECLARED on the row and lie inside what they allow);</item>
    /// <item>(b) the seed - the USA's, Sweden's and Poland's heads and the euro members' shared ECB President each seated from the world's start for its
    /// statute's term, every own-currency country with a head (the own-currency dial is retired, q10);</item>
    /// <item>(c) the AI's appointment - with every term planted to end two days on, a world with no player replaces each head on its term's day with the
    /// candidate nearest the rule from the keyed shortlist, seated for a full term, the ECB's once for the three members; a second world of the same seed
    /// seats the same names; no counted stream moves;</item>
    /// <item>(d) the player's bank - with Sweden the player's country, its head serves on past the term's end and the hold is raised (no AI appointment);
    /// the player's pick through the manager's verb is seated for the Riksbank's term from that day and the hold ends.</item>
    /// <item>(e) a counterfactual with the player seated - the fork `ShadowBaseline` builds - seats the AI's keyed pick for the player's bank on the term's day,
    /// while the played world it was forked from holds (the review's M1).</item>
    /// </list>
    /// The preview's damping (q10) is `PreviewParityDiagnostic`'s.
    /// </summary>
    public static class CentralBankTermCheck
    {
        private const int Seed = 777;

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== CentralBankTermCheck (AF-7, q7/q10): each central bank's head serves its own statute's term ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            int seedWas = SimulationRandom.MasterSeed;
            Dictionary<SimulationRandom.Stream, int> drawsWas = SimulationRandom.CaptureDrawCounts();
            var hosts = new List<GameObject>();
            try
            {
                using (SimulationManager.EpochScope()) { Measure(Check, sb, hosts); }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex).Append('\n'); }
            finally
            {
                foreach (GameObject go in hosts) { if (go != null) { UnityEngine.Object.DestroyImmediate(go); } }
                EnergyMarket.ResetTurnState();
                SimulationRandom.RestoreState(seedWas, drawsWas);
            }
            sb.Append(failures == 0 ? "=== CentralBankTermCheck: every bank's term is its statute's ===" : F("=== CentralBankTermCheck: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private static readonly Dictionary<string, int> NumberWords = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "four", 4 }, { "five", 5 }, { "six", 6 }, { "eight", 8 }, { "fyra", 4 }, { "fem", 5 }, { "sex", 6 }, { "åtta", 8 },
        };

        private static int Number(string token) => int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : NumberWords.TryGetValue(token, out int w) ? w : -1;

        /// <summary>The terms each row's words allow, read off the words themselves: "term of 4 years", "wynosi 6 lat", "shall be eight years", "fem eller sex år".</summary>
        private static List<int> TermsInWords(string words)
        {
            var terms = new List<int>();
            Match either = Regex.Match(words, @"tid av (\w+) eller (\w+) år");
            if (either.Success) { terms.Add(Number(either.Groups[1].Value)); terms.Add(Number(either.Groups[2].Value)); return terms; }
            Match one = Regex.Match(words, @"(?:term of|shall be|wynosi) (\w+) (?:years|lat)\b");
            if (one.Success) { terms.Add(Number(one.Groups[1].Value)); }
            return terms;
        }

        private static string Words(string html)
        {
            string s = Regex.Replace(html ?? string.Empty, @"<script\b.*?</script>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            s = Regex.Replace(s, @"<style\b.*?</style>", string.Empty, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            s = Regex.Replace(s, "<[^>]*>", " ");
            s = s.Replace("&nbsp;", " ").Replace("&#160;", " ").Replace(" ", " ").Replace("&amp;", "&");
            return Regex.Replace(s, @"\s+", " ");
        }

        private static void Measure(Action<bool, string> Check, StringBuilder sb, List<GameObject> hosts)
        {
            string Day(DateTime d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            // ---- (a) every statute to its page ----
            var keys = new HashSet<int>();
            foreach (CentralBankTerms.Statute s in CentralBankTerms.All)
            {
                bool page = PresidencyDiagnostic.Page(s.Source, out string html);
                string text = Words(html);
                bool found = page && text.Contains(Regex.Replace(s.Words, @"\s+", " "));
                List<int> allowed = TermsInWords(s.Words);
                bool termRead = allowed.Count > 0 && allowed.Contains(s.TermYears) && (allowed.Count == 1 || !string.IsNullOrEmpty(s.Reading));
                Check(page && found && termRead && keys.Add(s.Key),
                    F("(a) {0} ({1}): {2} held to its SHA-256, holding \"{3}\" - the term {4} years, read off the words as [{5}]{6}",
                        s.Institution, s.Citation, System.IO.Path.GetFileName(s.Source), s.Words, s.TermYears, string.Join(", ", allowed),
                        string.IsNullOrEmpty(s.Reading) ? string.Empty : "; DECLARED: " + s.Reading));
            }

            // ---- (b) the seed ----
            SimulationRandom.Seed(Seed);
            World seeded = WorldFactory.CreateDefault();
            DateTime epoch = SimulationManager.EpochDate;
            foreach (Country c in seeded.Countries)
            {
                bool shared = CurrencySystem.SharesCurrencyZoneWithOthers(c, seeded);
                CentralBankTerms.Statute s = CentralBankTerms.For(c.Id);
                FedChair head = shared ? c.CurrencyZone.Head : c.CurrentFedChair;
                bool ok = head != null && (shared ? c.CurrentFedChair == null && s == CentralBankTerms.Ecb : s != CentralBankTerms.Ecb)
                    && head.TermBegan == epoch.Date && head.TermEnds == epoch.Date.AddYears(s.TermYears) && Mathf.Approximately(head.RateBias, 0f);
                Check(ok, F("(b) {0}: {1} {2} ({3}), seated {4} to {5} - {6} years from the start, no lean{7}", c.Id, s.ShortName, head?.Name ?? "NO HEAD",
                    shared ? "the shared zone's" : "its own", Day(head?.TermBegan ?? DateTime.MinValue), Day(head?.TermEnds ?? DateTime.MinValue), s.TermYears,
                    shared ? string.Empty : " - its own currency has a head, so the retired dial was read by no country"));
            }

            // ---- (c) the AI's appointment, every term planted to end two days on ----
            Dictionary<SimulationRandom.Stream, int> before = SimulationRandom.CaptureDrawCounts();
            string first = PlantedRun(hosts, player: null, out SimulationManager sim, out DateTime ends, out string firstFault);
            Check(firstFault == null, "(c) " + (firstFault ?? F("with no player, every head replaced on its term's day {0}: {1}", Day(ends), first)));
            string second = PlantedRun(hosts, player: null, out _, out _, out string secondFault);
            Check(secondFault == null && second == first, F("(c) a second world of the same seed seats the same heads: {0}", second ?? secondFault));
            Dictionary<SimulationRandom.Stream, int> after = SimulationRandom.CaptureDrawCounts();
            before.TryGetValue(SimulationRandom.Stream.FederalReserve, out int fedBefore);
            after.TryGetValue(SimulationRandom.Stream.FederalReserve, out int fedAfter);
            Check(fedBefore == fedAfter, F("(c) the AI's shortlists move no counted stream - the Federal Reserve stream {0} -> {1} draws", fedBefore, fedAfter));

            // ---- (d) the player's bank ----
            PlantedRun(hosts, player: CountryId.Sweden, out SimulationManager psim, out DateTime pends, out string playerFault);
            Country sweden = psim?.World?.GetCountry(CountryId.Sweden);
            FedChair sitting = sweden?.CurrentFedChair;
            bool held = sitting != null && sitting.TermEnds == pends && psim.CentralBankHeadAwaitsPlayer(CountryId.Sweden);
            Check(held, F("(d) Sweden the player's: {0} serves on past {1} and the hold is raised{2}", sitting?.Name ?? "NO HEAD", Day(pends), playerFault == null ? string.Empty : " (" + playerFault + ")"));
            if (psim != null && sweden != null)
            {
                var pick = new FedChair("AF-7 probe governor", FedChairPhilosophy.Dovish, "the check's probe", -1.25f);
                bool appointed = psim.AppointCentralBankHead(CountryId.Sweden, pick, out string refused);
                FedChair now = sweden.CurrentFedChair;
                bool seated = appointed && now != null && now.Name == pick.Name && now.TermBegan == psim.CurrentDate.Date
                    && now.TermEnds == psim.CurrentDate.Date.AddYears(CentralBankTerms.Riksbank.TermYears) && !psim.CentralBankHeadAwaitsPlayer(CountryId.Sweden);
                Check(seated, F("(d) the player's pick through AppointCentralBankHead seated {0} to {1} ({2} years) and the hold ends{3}", Day(now?.TermBegan ?? DateTime.MinValue),
                    Day(now?.TermEnds ?? DateTime.MinValue), CentralBankTerms.Riksbank.TermYears, appointed ? string.Empty : " - REFUSED: " + refused));
            }

            // ---- (e) a counterfactual with the player seated (the review's M1): the fork `ShadowBaseline` builds - the impact ledger's except-world, the
            // no-policy world - appoints the player's bank by the AI's keyed rule on the term's day, while the played world it was forked from holds ----
            SimulationRandom.Seed(Seed);
            World played = WorldFactory.CreateDefault();
            var host = new GameObject("CentralBankTermCheck (e)"); hosts.Add(host);
            SimulationManager real = host.AddComponent<SimulationManager>();
            real.SetWorld(played);
            real.PlayerCountryId = CountryId.Sweden;
            DateTime eEnds = real.CurrentDate.Date.AddDays(2);
            FedChair outgoing = played.GetCountry(CountryId.Sweden).CurrentFedChair;
            outgoing.TermEnds = eEnds;
            ShadowBaseline fork = null;
            try
            {
                fork = new ShadowBaseline(real, played, CountryId.Sweden, insideTurn: true);
                fork.AdvanceTurn();
                for (int d = 0; d < 3; d++) { real.AdvanceDay(); }
                FedChair forked = fork.CountryFor(CountryId.Sweden).CurrentFedChair;
                FedChair nearest = null;
                foreach (FedChair x in CentralBankTerms.AiShortlist(CentralBankTerms.Riksbank, eEnds)) { if (nearest == null || Mathf.Abs(x.RateBias) < Mathf.Abs(nearest.RateBias)) { nearest = x; } }
                bool replaced = forked != null && nearest != null && forked.Name == nearest.Name && forked.RateBias == nearest.RateBias
                    && forked.TermBegan == eEnds && forked.TermEnds == eEnds.AddYears(CentralBankTerms.Riksbank.TermYears);
                FedChair still = played.GetCountry(CountryId.Sweden).CurrentFedChair;
                bool holds = ReferenceEquals(still, outgoing) && real.CentralBankHeadAwaitsPlayer(CountryId.Sweden);
                Check(replaced && holds, F("(e) Sweden the player's: the counterfactual fork seats the keyed pick {0} {1:+0.00;-0.00;0.00} on {2} to {3}; the played world it was forked from holds {4} and raises the hold",
                    forked?.Name ?? "NONE", forked?.RateBias ?? 0f, Day(forked?.TermBegan ?? DateTime.MinValue), Day(forked?.TermEnds ?? DateTime.MinValue), still?.Name ?? "NONE"));
            }
            finally { fork?.Dispose(); }
        }

        /// <summary>A default world of the check's seed, every head's term planted to end two days on, stepped three days; returns who sits where after, or
        /// the fault. With <paramref name="player"/> set (no party seated: the instrument's hand, which governs), that country's bank is the player's.</summary>
        private static string PlantedRun(List<GameObject> hosts, CountryId? player, out SimulationManager sim, out DateTime ends, out string fault)
        {
            fault = null;
            SimulationRandom.Seed(Seed);
            World world = WorldFactory.CreateDefault();
            var go = new GameObject("CentralBankTermCheck"); hosts.Add(go);
            sim = go.AddComponent<SimulationManager>();
            sim.SetWorld(world);
            if (player.HasValue) { sim.PlayerCountryId = player.Value; }
            ends = sim.CurrentDate.Date.AddDays(2);
            var outgoing = new Dictionary<FedChair, CentralBankTerms.Statute>();
            foreach (Country c in world.Countries)
            {
                FedChair head = c.CurrentFedChair ?? c.CurrencyZone.Head;
                if (head == null) { fault = c.Id + " has no head to plant"; return null; }
                head.TermEnds = ends;
                outgoing[head] = CentralBankTerms.For(c.Id);
            }
            var parts = new List<string>();
            for (int d = 0; d < 3; d++)
            {
                sim.AdvanceDay();
                if (sim.CurrentDate.Date == ends.AddDays(-1))
                {
                    foreach (Country c in world.Countries)
                    {
                        if (!outgoing.ContainsKey(c.CurrentFedChair ?? c.CurrencyZone.Head)) { fault = F("{0}'s head was replaced before its term's day", c.Id); return null; }
                    }
                }
                if (sim.CurrentDate.Date != ends) { continue; }
                foreach (Country c in world.Countries)
                {
                    bool shared = c.CurrentFedChair == null;
                    FedChair now = shared ? c.CurrencyZone.Head : c.CurrentFedChair;
                    CentralBankTerms.Statute s = CentralBankTerms.For(c.Id);
                    if (player.HasValue && player.Value == c.Id)
                    {
                        if (!outgoing.ContainsKey(now)) { fault = F("{0} is the player's, and its head was replaced by the AI", c.Id); return null; }
                        parts.Add(F("{0} awaits the player", c.Id));
                        continue;
                    }
                    List<FedChair> shortlist = CentralBankTerms.AiShortlist(s, ends);
                    FedChair nearest = null;
                    foreach (FedChair x in shortlist) { if (nearest == null || Mathf.Abs(x.RateBias) < Mathf.Abs(nearest.RateBias)) { nearest = x; } }
                    bool ok = now != null && !outgoing.ContainsKey(now) && nearest != null && now.Name == nearest.Name && now.RateBias == nearest.RateBias
                        && now.TermBegan == ends && now.TermEnds == ends.AddYears(s.TermYears);
                    if (!ok) { fault = F("{0}: {1} is not the nearest-the-rule pick of the keyed shortlist ({2}) seated {3} years from {4:yyyy-MM-dd}", c.Id, now?.Name ?? "none", nearest?.Name ?? "none", s.TermYears, ends); return null; }
                    parts.Add(F("{0} {1} {2} {3:+0.00;-0.00;0.00} to {4:yyyy-MM-dd}", c.Id, s.ShortName, now.Name, now.RateBias, now.TermEnds));
                }
                var zones = new HashSet<CurrencyZone>();
                foreach (Country c in world.Countries) { if (c.CurrentFedChair == null) { zones.Add(c.CurrencyZone); } }
                if (zones.Count != 1) { fault = F("the euro members share {0} zones, not one", zones.Count); return null; }
            }
            if (parts.Count == 0) { fault = "the term's day was never reached"; return null; }
            return string.Join("; ", parts);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
