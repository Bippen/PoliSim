namespace PoliSim.Data
{
    /// <summary>
    /// How a chamber converts votes into seats.
    ///
    /// <para><b>One enum, six countries, and every member here is load-bearing for at least one of
    /// them.</b> This is deliberately NOT a generic "proportional / majoritarian" pair: the difference
    /// between D'Hondt and modified Sainte-Laguë is worth several seats to Sweden's smaller parties, and
    /// a model that flattened them would be wrong in exactly the place a player would notice.</para>
    ///
    /// <para>⚠ <see cref="IndirectlyElected"/> is not a formula, it is the ABSENCE of one, and it exists
    /// so the German Bundesrat and the French Sénat cannot be silently run through a vote-share
    /// calculation they have no votes for. Bundesrat seats follow Land governments; Sénat seats follow
    /// ~150,000 local councillors. Both are modelled as derived compositions, never as elections - see
    /// the roadmap's Phase 5.</para>
    /// </summary>
    public enum ElectoralFormula
    {
        /// <summary>Highest averages with divisors 1.2, 3, 5, 7... <b>Sweden.</b> The first divisor was 1.4 until 2018 and is 1.2 now; using the old value under-rewards small parties and is a silent one-to-two-seat error.</summary>
        SainteLagueModified,

        /// <summary>Highest averages with divisors 1, 2, 3, 4... <b>Poland (Sejm).</b> Favours large parties, and with 41 constituencies and NO levelling seats that disproportionality is a real property of Polish politics rather than an artefact to correct.</summary>
        DHondt,

        /// <summary>Hare quota plus largest remainders. <b>Italy</b>'s Rosatellum PR tier.</summary>
        LargestRemainder,

        /// <summary>Single-member plurality. <b>USA (House)</b>, and the constituency tier of several others.</summary>
        Fptp,

        /// <summary>Two-round single-member majority. <b>France (Assemblée nationale).</b> Round 1 is won outright only with >50% of votes cast AND >=25% of REGISTERED voters; otherwise the top two plus anyone clearing 12.5% of registered voters proceed. Both qualifying rules are shares of the register, not of turnout, and getting that wrong changes who is in round 2.</summary>
        TwoRound,

        /// <summary>Mixed-member proportional. <b>Germany.</b> Sainte-Laguë/Schepers over second votes decides every seat; constituency winners fill those seats from the bottom up, capped per Land by the party's second-vote entitlement.</summary>
        MixedMemberProportional,

        /// <summary>State-level winner-take-all electors. <b>USA (presidency).</b> Maine and Nebraska award two electors statewide and the rest by congressional district, which is why this cannot be modelled as a national popular vote.</summary>
        ElectoralCollege,

        /// <summary>Not elected by voters at all. <b>German Bundesrat, French Sénat.</b> Composition is derived from another body; see this enum's own doc comment.</summary>
        IndirectlyElected
    }
}
