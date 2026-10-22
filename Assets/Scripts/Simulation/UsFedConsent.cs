using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;

namespace PoliSim.Simulation
{
    /// <summary>
    /// PS-6 US-29 (`docs/specs/USA_STAGE_PLAN.md`; Elias's ruling F8 on R-US12 (a): "the four-year chair term (12 U.S.C. 242, read), the president nominating
    /// and the Senate confirming by majority (G9's precedents read)"): <b>THE SENATE'S CONSENT TO THE FED CHAIR.</b> The term is AF-7's (`CentralBankTerms.Fed`,
    /// the statute's 4 years); this is the other half of the statute's words - the Chairman "designated by the President, by and with the advice and consent of
    /// the Senate" (12 U.S.C. 242, on disk under `ElectionsData/rules/raw/central_banks/`).
    ///
    /// <para><b>By majority</b> (`ElectionsData/usa/raw/fed/`, its fetch log): the Senate's precedent of 21 Nov 2013 - "the threshold for cloture on
    /// nominations, not including those to the Supreme Court of the United States, is now a majority" (Congressional Record, S8418) - so no cloture line stands
    /// before the Fed chair's confirmation, and the nominee is confirmed where more senators vote for than against, the Vice President deciding an equal
    /// division (`UsSenate.Carries`, the Senate's majority path as `Congress` counts it). The 2017 precedent (the Supreme Court's) is BILLED and not needed.</para>
    ///
    /// <para><b>The count, DECLARED (asked).</b> Each senator votes his conference's bloc (R-US10 (a)'s caucus, as the Senate is seated): the president's
    /// party for the president's nominee, every other conference against - a party-line count, so a Senate the president's party does not hold refuses
    /// whoever he names. The two confirmations of record (Yellen 2014, Powell 2018) were read for this reading and not fitted: their tallies by party are in
    /// the fetch log's NOTE (`ElectionsData/usa/raw/fed/fetch_log.txt`) and held to the Senate's own roll-call XML by `FedChairConsentDiagnostic`'s (a), which
    /// says which of the two the bloc rule reproduces. Nothing about the nominee enters the count (his lean included): no source on disk ties a senator's
    /// vote to it.</para>
    ///
    /// <para><b>Who nominates.</b> The president of the day (`Congress.PresidentOn` - the game's sworn winner, else the president of record by date, else the
    /// one ACTING under R-US22). A player who is the president (or acts as one) chooses from the candidates, and his choice goes to this count
    /// (`SimulationManager.AppointCentralBankHead`); an AI president names the AI's pick (`CentralBankTerms.AiNominee`) on the term's day and on every day
    /// after it until the Senate confirms him; meanwhile the sitting chair serves on.</para>
    ///
    /// <para><b>The holdover is a DECLARED reading (asked), not the statute's words for the chair</b> (the review of 2026-10-22, M1). 12 U.S.C. 242's
    /// "shall continue to serve until their successors are appointed and have qualified" is said of the Board's MEMBERS and their terms of office, not of
    /// the Chairman's four-year designation, and the same section sends the Chairman's absence to a Vice Chairman ("as provided in section 244"). The game
    /// reads the members' holdover for the chair's designation - AF-7's reading for every bank (`CentralBankTerms`) - and no page or log line credits it to
    /// the statute. Whether the sitting chair holds over or the Vice Chair acts (section 244, and the Board's chair pro tempore of record) is asked.</para>
    ///
    /// <para><b>Which Senate.</b> The chamber the world seats (`Country.SecondChamberSeats` - the record's by date, and from the game's first class the game's,
    /// US-15 and US-16). An AI USA - the USA in every other country's start, and every world with no player - seats none (US-15's premise), so its consent is
    /// counted on the Senate of record by date (`UsCongressElection.SenateOn`, which is the record's where the game elected none) - R-US1 (a)'s "whatever the
    /// game does not elect is seated on its record's dates" (DECLARED).</para>
    /// </summary>
    public static class UsFedConsent
    {
        /// <summary>One day's count on the president's nominee.</summary>
        public sealed class Count
        {
            /// <summary>The president who nominates, as the game shows him, and his party's key.</summary>
            public string President, PresidentsParty;
            public int For, Against;
            /// <summary>The Vice President's party where his vote decided an equal division, else null.</summary>
            public string VicePresidentParty;
            /// <summary>The Senate counted is the record's by date because the world seats none (an AI USA).</summary>
            public bool SenateOfRecord;
            public bool Confirms;
        }

        /// <summary>Whether the head of this country's bank needs the Senate's consent: the USA's (12 U.S.C. 242).</summary>
        public static bool Applies(Country country) => country != null && country.Id == CountryId.USA;

        /// <summary>The Senate that consents on <paramref name="on"/>: the world's seated chamber, or - where it seats none - the record's by date.</summary>
        public static IReadOnlyDictionary<string, int> SenateFor(Country country, DateTime on, out bool ofRecord)
        {
            ofRecord = Congress.Sworn(country.SecondChamberSeats) <= 0;
            return ofRecord ? UsCongressElection.SenateOn(country, on) : country.SecondChamberSeats;
        }

        /// <summary>The count on the president's nominee on <paramref name="on"/>; null where it cannot be held - not the USA, no president found, or no Senate
        /// seated or on record that day (a caller treats null as no consent, and says so).</summary>
        public static Count Vote(Country country, DateTime on)
        {
            if (!Applies(country) || !Congress.PresidentOn(country, on, out string shownAs, out string party, out _) || string.IsNullOrEmpty(party)) { return null; }
            IReadOnlyDictionary<string, int> seats = SenateFor(country, on, out bool ofRecord);
            int sworn = Congress.Sworn(seats);
            if (sworn <= 0) { return null; }
            int own = seats.TryGetValue(party, out int n) ? n : 0;
            var count = new Count { President = shownAs, PresidentsParty = party, For = own, Against = sworn - own, SenateOfRecord = ofRecord };
            bool tie = count.For == count.Against;
            string vice = tie ? UsSenate.VicePresidentParty(country, on) : null;
            count.VicePresidentParty = vice;
            count.Confirms = UsSenate.Carries(count.For, count.Against, tie && vice == party);
            return count;
        }

        /// <summary>Whether the Senate of <paramref name="on"/> confirms the president's nominee (false where no count can be held).</summary>
        public static bool Confirms(Country country, DateTime on) => Vote(country, on)?.Confirms ?? false;

        /// <summary>The count in a page's capitals - "THE SENATE CONFIRMS, 53-47" or "THE SENATE REFUSES, 47-53", the Vice President named where he decides.</summary>
        public static string CountWords(Count count)
        {
            if (count == null) { return "NO SENATE COUNT CAN BE HELD"; }
            string tally = count.For.ToString(CultureInfo.InvariantCulture) + "-" + count.Against.ToString(CultureInfo.InvariantCulture);
            return (count.Confirms ? "THE SENATE CONFIRMS, " : "THE SENATE REFUSES, ") + tally
                   + (count.VicePresidentParty != null ? " - THE VICE PRESIDENT DECIDES THE EQUAL DIVISION" : string.Empty)
                   + (count.SenateOfRecord ? " (THE SENATE OF RECORD)" : string.Empty);
        }

        /// <summary>The count as a sentence ends - "the Senate would confirm your nominee 53-47" - for the choice's own paragraph.</summary>
        public static string CountSentence(Count count)
        {
            if (count == null) { return "no Senate count can be held today"; }
            string tally = count.For.ToString(CultureInfo.InvariantCulture) + "-" + count.Against.ToString(CultureInfo.InvariantCulture);
            return "the Senate would " + (count.Confirms ? "confirm" : "refuse") + " the nominee " + tally
                   + (count.VicePresidentParty != null ? ", the Vice President deciding the equal division" : string.Empty);
        }

        /// <summary>The Fed chair serving on past his term's end because no successor is confirmed, in a page's capitals; null where he does not. The holdover
        /// is the game's DECLARED reading (the class note), so no statute is cited for it.</summary>
        public static string HoldoverWords(Country country, FedChair chair, DateTime today)
        {
            if (!Applies(country) || chair == null || !CentralBankTerms.Ended(chair, today)) { return null; }
            return "THE TERM ENDED " + chair.TermEnds.ToString("d MMM yyyy", CultureInfo.InvariantCulture).ToUpperInvariant() + " · " + CountWords(Vote(country, today))
                   + " · THE CHAIR SERVES ON UNTIL A SUCCESSOR IS CONFIRMED";
        }

        /// <summary>The verb's refusal where the Senate refuses the player's nominee - what `SimulationManager.AppointCentralBankHead` hands back (the holdover
        /// the game's DECLARED reading, uncited).</summary>
        public static string RefusedWords(Count count) =>
            CountWords(count) + " · THE PRESIDENT'S PARTY HOLDS NO MAJORITY THERE · THE CHAIR SERVES ON UNTIL A SUCCESSOR IS CONFIRMED";
    }
}
