using System;
using System.IO;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §695 (PS-4; decision 5): **EVERY REAL PARTY'S CAMPAIGN CAST, READ OFF ITS CHES POSITION.** Prints, for each country's real roster, the three
    /// CHES items the rule reads and the cast it gives, with the test that decided; and holds that (a) every real party is cast by the rule and
    /// none Chaotic (no survey item measures it); (b) the rule's order is its stated one - a planted party salient AND left-libertarian is
    /// Populist, one libertarian but economically right is not Grassroots, one missing every item is Professional; (c) the live campaign reads the
    /// rule - `LiveCampaignSetup.PersonalityOf` gives every Swedish party its rule cast, a Splinter its parent's - and no hand-cast array stands.
    /// </summary>
    public static class CampaignCastDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== CampaignCastDiagnostic (§695): every real party's campaign cast, derived from its CHES position ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                int parties = 0, chaotic = 0;
                foreach (CountryId country in (CountryId[])Enum.GetValues(typeof(CountryId)))
                {
                    PoliticalParty[] roster = PartySystems.RealRoster(country);
                    if (roster == null || roster.Length == 0) { continue; }
                    sb.Append("  ").Append(country).Append('\n');
                    foreach (PoliticalParty p in roster)
                    {
                        parties++;
                        AiPersonality cast = CampaignCasts.Of(p);
                        if (cast == AiPersonality.Chaotic) { chaotic++; }
                        sb.Append($"    {p.Abbrev,-8} salience {F(p.AntiEliteSalience)}  GAL-TAN {F(p.Galtan)}  econ {F(p.LrEcon)}  people-v-elite {F(p.PeopleVsElite)}  -> {cast,-13} ({CampaignCasts.Why(p)})\n");
                    }
                }
                Check(parties > 0 && chaotic == 0, $"every real party is cast by the rule - {parties} parties, {chaotic} Chaotic (no CHES item measures inconsistency)");

                var both = new PoliticalParty("X1", "planted", 2f, 1f, 0, antiEliteSalience: 6f, peopleVsElite: 1f);
                var rightLibertarian = new PoliticalParty("X2", "planted", 8f, 2f, 0, antiEliteSalience: 1f, peopleVsElite: 5f);
                var blank = new PoliticalParty("X3", "planted", float.NaN, float.NaN, 0);
                Check(CampaignCasts.Of(both) == AiPersonality.Populist && CampaignCasts.Of(rightLibertarian) == AiPersonality.Professional && CampaignCasts.Of(blank) == AiPersonality.Professional,
                    $"the stated order holds: salient and left-libertarian -> {CampaignCasts.Of(both)}; libertarian, economically right, representative-sceptic -> {CampaignCasts.Of(rightLibertarian)}; no items -> {CampaignCasts.Of(blank)}");

                PoliticalParty[] sweden = PartySystems.RealRoster(CountryId.Sweden);
                string[] keys = LiveCampaignSetup.SwedenParties;
                int wrong = 0; string first = null;
                for (int i = 0; i < keys.Length; i++)
                {
                    int at = Array.FindIndex(sweden, x => x.Abbrev == keys[i]);
                    AiPersonality live = LiveCampaignSetup.PersonalityOf(i, keys[i]);
                    if (at < 0 || live != CampaignCasts.Of(sweden[at])) { wrong++; first = first ?? $"{keys[i]}: the campaign casts {live}, the rule {(at < 0 ? "no party" : CampaignCasts.Of(sweden[at]).ToString())}"; }
                }
                string setup = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Application.dataPath), "Assets/Scripts/Elections/LiveCampaignSetup.cs"));
                Check(wrong == 0 && !setup.Contains("AiPersonality.Chaotic, AiPersonality.Establishment"),
                    $"the live campaign reads the rule for every Swedish party and no hand-cast array stands{(first != null ? " - FIRST: " + first : string.Empty)}");
            }
            catch (Exception e) { failures++; sb.Append("    FAIL      threw: ").Append((e.InnerException ?? e).Message).Append('\n'); }
            sb.Append(failures == 0 ? "    CLEAN\n" : $"    {failures} failure(s)\n");
            if (failures > 0) { Debug.LogError(sb.ToString()); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(float v) => float.IsNaN(v) ? "  -  " : v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).PadLeft(5);
    }
}
