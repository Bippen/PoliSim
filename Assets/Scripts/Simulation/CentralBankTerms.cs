using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Simulation
{
    /// <summary>
    /// AF-7 (Elias's rulings q7 and q10 of 2026-10-18): <b>EACH CENTRAL BANK'S HEAD SERVES THE TERM ITS OWN STATUTE SETS</b>, in place of one
    /// cycle on the election turns that no election uses any more (GA-0-29). The Fed by R-US12 (a) - 12 U.S.C. 242; the ECB for the euro members -
    /// the Statute of the ESCB and of the ECB, Art. 11.2 (TFEU Art. 283(2)); the Riksbank by its act, 7 kap. 7 §; the NBP by its act, Art. 9. Each
    /// row carries the statute's own words, and `CentralBankTermCheck` holds every row to its saved page (each page to its SHA-256).
    ///
    /// <para><b>The day a term ends, the head is replaced</b> (q10: "AI-run banks get a head", GA-0-53): a bank the player's government appoints raises
    /// the hold on that day and the sitting head serves on until the pick (the Fed's Vice Chair acts instead - K15, `UsFedConsent`) (12 U.S.C. 242: members "shall continue to serve until their successors are
    /// appointed and have qualified" - read for the others as the same holdover, DECLARED); every other bank's appointer is the AI, which appoints at once
    /// (<see cref="AiAppointment"/>). The ECB's President is the European Council's to appoint (Art. 11.2), never a member government's, so the euro
    /// members' bank is always AI-run.</para>
    ///
    /// <para><b>Not modelled, stated:</b> re-appointment and its limits (the Riksbank's once, the NBP's two consecutive terms, the ECB's none) - every
    /// appointment draws a fresh shortlist; removal before the term's end. The US Senate's consent to the President's designation is US-29's (`UsFedConsent`).</para>
    /// </summary>
    public static class CentralBankTerms
    {
        /// <summary>One bank's statute: who heads it, for how long, who appoints, and the words the term rests on, on the page they are read from.</summary>
        public sealed class Statute
        {
            /// <summary>The key the bank's deterministic shortlist is drawn on - fixed per bank, never renumbered (<see cref="SimulationRandom.CentralBankShortlistOn"/>).</summary>
            public int Key;
            public string Institution;
            /// <summary>How a sentence names the bank (the candidates' prose, <see cref="FederalReserveSystem"/>'s `{bank}`).</summary>
            public string ShortName;
            public string HeadTitle;
            public int TermYears;
            public string Appointer;
            public string Citation;
            /// <summary>The citation as a page prints it - the statute's name and article in the words a player reads, never a section sign.</summary>
            public string PageCitation;
            /// <summary>The saved page, from the project root.</summary>
            public string Source;
            /// <summary>The statute's words the term is read from, as the page carries them (whitespace normalised).</summary>
            public string Words;
            /// <summary>A reading the statute leaves open, DECLARED; null where the words decide.</summary>
            public string Reading;
        }

        private const string Folder = "ElectionsData/rules/raw/central_banks/";

        /// <summary>[SOURCED] 12 U.S.C. 242 (United States Code, 2024 edition, govinfo): the Chairman designated by the President, by and with the
        /// advice and consent of the Senate, for a term of 4 years.</summary>
        public static readonly Statute Fed = new Statute
        {
            Key = 1, Institution = "the Federal Reserve", ShortName = "the Fed", HeadTitle = "Chair", TermYears = 4,
            Appointer = "the President, with the Senate's advice and consent", Citation = "12 U.S.C. 242", PageCitation = "12 U.S.C. 242",
            Source = Folder + "us_12usc242_govinfo2024.htm",
            Words = "to serve as Chairman of the Board for a term of 4 years",
        };

        /// <summary>[SOURCED] Lag (2022:1568) om Sveriges riksbank, 7 kap. 7 § (riksdagen.se): five members appointed for five or six years, the
        /// General Council choosing the riksbankschef among them. The statute leaves five or six open - six is DECLARED (the reading below).</summary>
        public static readonly Statute Riksbank = new Statute
        {
            Key = 2, Institution = "Sveriges Riksbank", ShortName = "the Riksbank", HeadTitle = "Governor", TermYears = 6,
            Appointer = "the Riksbank's General Council", Citation = "Lag (2022:1568) om Sveriges riksbank, 7 kap. 7 §", PageCitation = "the Riksbank Act, SFS 2022:1568, ch. 7",
            Source = Folder + "se_riksdagen_sfs_2022-1568_riksbankslag.html",
            Words = "Direktionen ska bestå av fem ledamöter som utses för en tid av fem eller sex år. Av dessa ska fullmäktige utse en ordförande (riksbankschef).",
            Reading = "the act allows five or six years; the game seats every governor for six (DECLARED, asked)",
        };

        /// <summary>[SOURCED] Ustawa o Narodowym Banku Polskim, Art. 9 ust. 1-2 (the consolidated text, Dz.U. 2022 poz. 2025, ELI): the Prezes NBP
        /// appointed by the Sejm on the President's motion, for six years, at most two consecutive terms.</summary>
        public static readonly Statute Nbp = new Statute
        {
            Key = 3, Institution = "Narodowy Bank Polski", ShortName = "the NBP", HeadTitle = "President", TermYears = 6,
            Appointer = "the Sejm, on the President's motion", Citation = "Ustawa o Narodowym Banku Polskim, Art. 9 ust. 2", PageCitation = "the NBP Act, Art. 9",
            Source = Folder + "pl_eli_DU_2022_2025_text.html",
            Words = "Kadencja Prezesa NBP wynosi 6 lat.",
        };

        /// <summary>[SOURCED] Protocol (No 4) on the Statute of the ESCB and of the ECB, Art. 11.2 (EUR-Lex via the Publications Office): the
        /// President appointed by the European Council by a qualified majority, for eight years, not renewable.</summary>
        public static readonly Statute Ecb = new Statute
        {
            Key = 4, Institution = "the European Central Bank", ShortName = "the ECB", HeadTitle = "President", TermYears = 8,
            Appointer = "the European Council, by a qualified majority", Citation = "Statute of the ESCB and of the ECB, Art. 11.2", PageCitation = "the ESCB Statute, Art. 11.2",
            Source = Folder + "eu_tfeu_protocol04_escb_statute_cellar.xhtml",
            Words = "Their term of office shall be eight years and shall not be renewable.",
        };

        public static IReadOnlyList<Statute> All => new[] { Fed, Riksbank, Nbp, Ecb };

        /// <summary>The statute of the bank that sets this country's rate: its own for the USA, Sweden and Poland; the ECB's for a euro member.</summary>
        public static Statute For(CountryId country)
        {
            switch (country)
            {
                case CountryId.USA: return Fed;
                case CountryId.Sweden: return Riksbank;
                case CountryId.Poland: return Nbp;
                default: return Ecb;
            }
        }

        /// <summary>The day a term begun on <paramref name="began"/> ends under <paramref name="statute"/>.</summary>
        public static DateTime TermEnds(Statute statute, DateTime began) => began.Date.AddYears(statute.TermYears);

        /// <summary>Seats <paramref name="head"/> for a full term from <paramref name="began"/>; returns it.</summary>
        public static FedChair Seat(FedChair head, Statute statute, DateTime began)
        {
            head.TermBegan = began.Date;
            head.TermEnds = TermEnds(statute, began);
            return head;
        }

        /// <summary>True from the day <paramref name="head"/>'s term ends. A head with no term recorded (a harness's hand-made head) never ends.</summary>
        public static bool Ended(FedChair head, DateTime today) => head != null && head.HasTerm() && today.Date >= head.TermEnds;

        /// <summary>The shortlist an AI appointer draws at a term's end - deterministic on the game's seed, the bank and the day, so every world of one
        /// game draws the same names and no counted stream moves (<see cref="SimulationRandom.CentralBankShortlistOn"/>).</summary>
        public static List<FedChair> AiShortlist(Statute statute, DateTime termEnds) =>
            FederalReserveSystem.GenerateCandidates(statute.ShortName, SimulationRandom.CentralBankShortlistOn(statute.Key, termEnds));

        /// <summary>
        /// The AI's appointment at a term's end: from the drawn shortlist, the candidate whose lean is nearest the rule (the smallest |RateBias|, the
        /// first on a tie; since K56 every shortlist carries a no-lean candidate, so this is it) - the no-lean choice the seed's heads already are (DECLARED: the AI's appointer has no sourced preference over the lean), seated
        /// for a full term from <paramref name="today"/>.
        /// </summary>
        public static FedChair AiAppointment(Statute statute, FedChair outgoing, DateTime today) => Seat(AiNominee(statute, outgoing, today), statute, today);

        /// <summary>PS-6 US-29: the AI's pick unseated - the nominee an AI president sends to the Senate (`UsFedConsent`). The shortlist is keyed on the outgoing
        /// head's term's end (else <paramref name="today"/>), so every day of a holdover names the same nominee.</summary>
        public static FedChair AiNominee(Statute statute, FedChair outgoing, DateTime today)
        {
            List<FedChair> shortlist = AiShortlist(statute, outgoing != null && outgoing.HasTerm() ? outgoing.TermEnds : today.Date);
            FedChair pick = null;
            foreach (FedChair c in shortlist)
            {
                if (pick == null || Math.Abs(c.RateBias) < Math.Abs(pick.RateBias)) { pick = c; }
            }
            return pick;
        }
    }
}
