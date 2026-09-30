using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// §695 (PS-4; `POLITICAL_SYSTEM_SPEC.md` §10 decision 5, standing): **AN AI PARTY'S CAMPAIGN CAST IS DERIVED FROM ITS CHES POSITION BY A STATED
    /// RULE** - never a hand-written characterisation of a real party. It retires Sweden's eight hand-cast personalities (C-R4b, [AUTHORED-DRAFT]) and
    /// casts Germany's, and every other country's, by the same reading. The rule reads three CHES 2024 items, quoted on <see cref="PoliticalParty"/>'s
    /// fields, in a fixed order - the first that holds decides:
    /// <list type="number">
    /// <item><description><b>Populist</b> - `anti_elite_salience` above the scale's midpoint (&gt; 5): anti-establishment rhetoric is salient to the party
    /// (§32's populist: high-salience issues, aggressive attacks).</description></item>
    /// <item><description><b>Grassroots</b> - `galtan` in the libertarian/post-materialist third (&lt; 10/3) AND `lrecon` left of the centre (&lt; 5): the
    /// left-libertarian movement parties (§32's grassroots: volunteers, door-to-door, turnout).</description></item>
    /// <item><description><b>Establishment</b> - `people_v_elite` in the elected-representatives' third (&lt; 10/3): the party holds that elected office
    /// holders should make the most important decisions (§32's establishment: traditional media, broad messaging, moderate policies).</description></item>
    /// <item><description><b>Professional</b> - every other party (§32's professional: polling, swing voters, money spent efficiently).</description></item>
    /// </list>
    /// **Chaotic**, §32's fifth, is cast to no real party: no CHES item measures a strategy's inconsistency. A missing item (`NaN` - the USA's GPS pair
    /// carry no salience) fails its test and the rule reads on. The cut points are the scales' own midpoint and thirds - reading choices,
    /// [AUTHORED-DRAFT], each strikeable; `CampaignCastDiagnostic` prints every real party's three items beside its cast.
    /// </summary>
    public static class CampaignCasts
    {
        /// <summary>[AUTHORED-DRAFT] the salience scale's midpoint.</summary>
        public const float SalientAbove = 5f;
        /// <summary>[AUTHORED-DRAFT] a 0-10 scale's lower third.</summary>
        public const float LowerThird = 10f / 3f;
        /// <summary>[AUTHORED-DRAFT] the economic scale's centre.</summary>
        public const float EconomicCentre = 5f;

        /// <summary>The cast of a real party, by the rule.</summary>
        public static AiPersonality Of(PoliticalParty party)
        {
            if (party.AntiEliteSalience > SalientAbove) { return AiPersonality.Populist; }
            if (party.Galtan < LowerThird && party.LrEcon < EconomicCentre) { return AiPersonality.Grassroots; }
            if (party.PeopleVsElite < LowerThird) { return AiPersonality.Establishment; }
            return AiPersonality.Professional;
        }

        /// <summary>Which test decided - the diagnostic's and the check's reading.</summary>
        public static string Why(PoliticalParty party)
        {
            // invariant, as every figure the game prints (the one number locale) - the first run printed the machine's sv-SE commas
            System.Globalization.CultureInfo inv = System.Globalization.CultureInfo.InvariantCulture;
            if (party.AntiEliteSalience > SalientAbove) { return string.Format(inv, "anti-elite salience {0:0.00} > {1:0}", party.AntiEliteSalience, SalientAbove); }
            if (party.Galtan < LowerThird && party.LrEcon < EconomicCentre) { return string.Format(inv, "GAL-TAN {0:0.00} < 3.33 and economic left-right {1:0.00} < {2:0}", party.Galtan, party.LrEcon, EconomicCentre); }
            if (party.PeopleVsElite < LowerThird) { return string.Format(inv, "people-vs-elite {0:0.00} < 3.33", party.PeopleVsElite); }
            return "none of the three holds";
        }
    }
}
