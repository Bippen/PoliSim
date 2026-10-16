using System;
using System.IO;
using System.Text;
using PoliSim.Data;
using PoliSim.Data.Generated;
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
    /// rule - `LiveCampaignSetup.PersonalityOf` gives every Swedish party its rule cast, a Splinter its parent's - and no hand-cast array stands;
    /// (d) US-32: the USA's two casts on the GPS 2019 reading, pinned, with every GPS reading not installed printed beside them (<see cref="UsaCasts"/>).
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

                failures += UsaCasts(sb);

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

        /// <summary>
        /// US-32 (ruling G7, §797): **THE USA'S TWO CASTS ON THE GPS 2019 READING, PINNED, AND EVERY READING NOT INSTALLED PRINTED WITH ITS MOVES.**
        /// The roster carries GPS's V4/V6 as the economic and social pair and V8 (populist rhetoric) as people-versus-elite (`UsGps2019`); the
        /// Republicans cast Professional (no salience item installed; V8 not in the elected-representatives' third) and the Democrats Grassroots
        /// (GAL-TAN and economics both on the left), and this holds both. G7 says a cast move is reported, each move's size, BEFORE it is
        /// installed - so the readings that would move one stay out of the roster and are printed here instead, for the report:
        /// (a) HELD - GPS's V9 (how important populist rhetoric is) read onto `anti_elite_salience`; the roster must still carry none, and installing
        /// it is a change to this pin made with Elias's answer. (b) PUT TO ELIAS - the codebook's own counterpart of CHES `people_v_elite`, V19
        /// (people should decide, 0 = ordinary people … 10 = leaders), reversed onto CHES's direction (10 - V19).
        /// </summary>
        private static int UsaCasts(StringBuilder sb)
        {
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            PoliticalParty[] usa = PartySystems.RealRoster(CountryId.USA);
            int rep = Array.FindIndex(usa, x => x.Abbrev == "REP"), dem = Array.FindIndex(usa, x => x.Abbrev == "DEM");
            if (rep < 0 || dem < 0) { Check(false, "US-32: the USA's roster holds REP and DEM"); return failures; }
            PoliticalParty r = usa[rep], d = usa[dem];

            Check(CampaignCasts.Of(r) == AiPersonality.Professional && CampaignCasts.Of(d) == AiPersonality.Grassroots,
                $"US-32: on the installed GPS reading the Republicans cast {CampaignCasts.Of(r)} ({CampaignCasts.Why(r)}), the Democrats {CampaignCasts.Of(d)} ({CampaignCasts.Why(d)}) - pinned Professional and Grassroots");
            Check(r.PeopleVsElite.Equals(UsGps2019.RepPopulistRhetoricV8) && d.PeopleVsElite.Equals(UsGps2019.DemPopulistRhetoricV8),
                string.Format(inv, "US-32: people-versus-elite is GPS's V8 (populist rhetoric, a reading) - REP {0:0.00}, DEM {1:0.00}; the [AUTHORED-DRAFT] placement is gone", r.PeopleVsElite, d.PeopleVsElite));
            Check(float.IsNaN(r.AntiEliteSalience) && float.IsNaN(d.AntiEliteSalience),
                "US-32 (a) HELD under G7: neither US party carries a salience - GPS's V9 stays out of the roster until Elias answers the move below");

            PoliticalParty With(PoliticalParty p, float salience, float peopleVsElite)
                => new PoliticalParty(p.Abbrev, p.Name, p.LrEcon, p.Galtan, p.SeedSeats, antiEliteSalience: salience, peopleVsElite: peopleVsElite);
            string Move(PoliticalParty from, PoliticalParty to) => CampaignCasts.Of(from) == CampaignCasts.Of(to)
                ? $"{CampaignCasts.Of(from)}, no move ({CampaignCasts.Why(to)})"
                : $"{CampaignCasts.Of(from)} -> {CampaignCasts.Of(to)} MOVES ({CampaignCasts.Why(to)})";

            // (a) V9 onto anti_elite_salience: the move's size is the item against the Populist test's cut
            float rV9 = UsGps2019.RepPopulistSaliencyV9, dV9 = UsGps2019.DemPopulistSaliencyV9;
            sb.Append(string.Format(inv, "    held      (a) V9 onto anti_elite_salience: REP {0:0.00} ({1:+0.00;-0.00} against the cut {2:0}) -> {3}\n",
                rV9, rV9 - CampaignCasts.SalientAbove, CampaignCasts.SalientAbove, Move(r, With(r, rV9, r.PeopleVsElite))));
            sb.Append(string.Format(inv, "    held      (a) V9 onto anti_elite_salience: DEM {0:0.00} ({1:+0.00;-0.00} against the cut {2:0}) -> {3}\n",
                dV9, dV9 - CampaignCasts.SalientAbove, CampaignCasts.SalientAbove, Move(d, With(d, dV9, d.PeopleVsElite))));

            // (b) 10 - V19 onto people_v_elite: the casts, and what the stance model's populism term (above the midpoint) would read
            float rV19 = 10f - UsGps2019.RepPeopleShouldDecideV19, dV19 = 10f - UsGps2019.DemPeopleShouldDecideV19;
            sb.Append(string.Format(inv, "    asked     (b) 10 - V19 onto people_v_elite: REP {0:0.00} (V8 {1:0.00}) -> {2}\n", rV19, r.PeopleVsElite, Move(r, With(r, r.AntiEliteSalience, rV19))));
            sb.Append(string.Format(inv, "    asked     (b) 10 - V19 onto people_v_elite: DEM {0:0.00} (V8 {1:0.00}) -> {2}\n", dV19, d.PeopleVsElite, Move(d, With(d, d.AntiEliteSalience, dV19))));
            sb.Append(string.Format(inv, "    asked     (b) the populism term reads a value above 5: on V8 {0}; on 10 - V19 {1}\n",
                Above(r.PeopleVsElite, d.PeopleVsElite), Above(rV19, dV19)));
            return failures;
        }

        private static string Above(float rep, float dem)
        {
            string s = (rep > 5f ? "REP" : string.Empty) + (rep > 5f && dem > 5f ? " and " : string.Empty) + (dem > 5f ? "DEM" : string.Empty);
            return s.Length == 0 ? "neither" : s;
        }

        private static string F(float v) => float.IsNaN(v) ? "  -  " : v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).PadLeft(5);
    }
}
