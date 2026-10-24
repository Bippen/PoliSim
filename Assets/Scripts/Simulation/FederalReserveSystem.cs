using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// The independent central bank mechanic: when a head's statutory term ends (AF-7, `CentralBankTerms` - each bank's own statute), the
    /// player picks the next head of their own bank from 2-3 fictional candidates (an AI appointer picks from a keyed shortlist), and that
    /// head's RateBias (on top of TaylorRule's suggested rate) drives the rate every turn, bypassing PolicyDecision.
    /// InterestRateChange entirely (see CurrencySystem.ApplyInterestRateChanges) - the USA, Sweden and Poland; the euro members' shared rate
    /// takes the ECB President's lean (EurozoneRateSystem).
    ///
    /// Every FedChair here (CandidatePool and GetDefaultChair) is an ORIGINAL FICTIONAL character -
    /// never a real past or present Federal Reserve chair or any real person. Keep it that way for
    /// any future additions to the pool.
    /// </summary>
    public static class FederalReserveSystem
    {
        /// <summary>CONVENTION - how many candidates a chair shortlist offers, at least. A presentation choice about how much of a decision the screen shows, not a claim about real appointment practice.</summary>
        private const int MinCandidateCount = 2;

        /// <summary>CONVENTION - the upper end of the same shortlist size.</summary>
        private const int MaxCandidateCount = 3;

        /// <summary>
        /// CONVENTION - a reversion speed, and the one the eurozone system derives its own from.
        /// Fraction of the gap between USA's current rate and this turn's target (TaylorRule's
        /// suggestion plus the chair's RateBias) that closes each turn (0-1) - matches
        /// CurrencySystem.CurrencyStrengthDamping's value and role (a real central bank moves
        /// gradually, not straight to its own textbook target every meeting). Without this, the rate
        /// can jump the entire distance between two very different values in one turn (e.g. observed
        /// 5.05% -> 0.00%), which overshoots, corrects, and overshoots again - an undamped
        /// interest-rate/output-gap feedback loop that was the real root cause of persistent
        /// turn-to-turn volatility (see "Federal Reserve Rate Damping" in CLAUDE.md), not a bug in
        /// TaylorRule or the output-gap calculation themselves. (Historical framing: that loop ran
        /// through the level output gap the rule read until pass 4, 2026-08-26; the damping stays and
        /// was re-measured after the fix - max single-turn move 0.30 pp, no reversal cycle. Public
        /// since pass 4 so the Federal Reserve tab can name the speed it shows.)
        /// </summary>
        public const float RateAdjustmentSpeed = 0.15f;

        // Isolated from EventSystem's own System.Random and UnityEngine.Random - drawing candidates at
        // an election boundary must never perturb any other RNG consumer's sequence. (It named
        // GameController's _previewRandom as a third isolated stream until C-C14 deleted that field with
        // the rolled display margin it existed for.)
        private static System.Random RandomSource => SimulationRandom.For(SimulationRandom.Stream.FederalReserve);

        /// <summary>Board 5f: the size of the pool a nomination draws from - printed on the appointment lever, never the names.</summary>
        public static int PoolSize => CandidatePool.Count;

        private static readonly List<FedChair> CandidatePool = new List<FedChair>
        {
            new FedChair(
                "Marcus Thackeray", FedChairPhilosophy.Hawkish,
                "Prioritizes price stability above all - argues inflation, once entrenched, is far costlier to unwind than a soft patch in hiring.",
                1.5f),
            new FedChair(
                "Ines Kowalski", FedChairPhilosophy.Hawkish,
                "A former bank supervisor who believes {bank} waited too long to act last cycle and wants a wider margin against renewed price pressure.",
                1.25f),
            new FedChair(
                "Theodore Voss", FedChairPhilosophy.Moderate,
                "Sees the Taylor Rule as a sound starting point and prefers small, data-driven adjustments over dramatic moves in either direction.",
                0f),
            new FedChair(
                "Priya Anand", FedChairPhilosophy.Moderate,
                "Weighs price stability against the labour market evenly, wary of letting either drift too far from where {bank} wants it.",
                0.25f),
            new FedChair(
                "Roland Kade", FedChairPhilosophy.Moderate,
                "A pragmatist who adjusts gradually and avoids surprising markets with sharp policy swings.",
                -0.25f),
            new FedChair(
                "Simone Delacroix", FedChairPhilosophy.Dovish,
                "Argues the labor market deserves more patience - keeping credit cheap to protect jobs even if inflation runs a little hot for a while.",
                -1.25f),
            new FedChair(
                "Nathaniel Osei", FedChairPhilosophy.Dovish,
                "Believes the risks of choking off growth outweigh the risks of moderately elevated inflation, especially after a period of high unemployment.",
                -1.5f),
        };

        /// <summary>
        /// Draws 2-3 candidates from CandidatePool with distinct philosophies, then appends the no-lean one where none was drawn (K56: 2-4 in all; never all the same
        /// lean), for the player to choose the country's next central-bank head from at an election
        /// boundary - see GameController's central-bank panel. Since K56 one of them always has no lean
        /// (the pool's no-lean candidate is appended to a draw that holds none - a list may then hold four, and nothing drawn is removed).
        ///
        /// <para>⚠ S-15 (2026-09-02): the pool's prose is written against a `{bank}` placeholder and
        /// resolved HERE, per country, into a fresh <see cref="FedChair"/>. Until this the pool said
        /// "the Fed" verbatim, so a Swedish player was offered Riksbank governors who talked about the
        /// Federal Reserve. The pool entries are shared templates and are never mutated; a save carries
        /// the resolved text, so a candidate drawn for Sweden still reads as Sweden's after a load.</para>
        /// </summary>
        public static List<FedChair> GenerateCandidates(CountryId country) => GenerateCandidates(BankShortName(country), RandomSource);

        /// <summary>AF-7: the same draw on a named bank and a given generator - the player's shortlist on the counted stream (above), an AI appointer's on
        /// its keyed one (<see cref="CentralBankTerms.AiShortlist"/>). The draws are taken in the order they always were.</summary>
        public static List<FedChair> GenerateCandidates(string bank, System.Random rng)
        {
            var philosophies = new List<FedChairPhilosophy>
            {
                FedChairPhilosophy.Hawkish, FedChairPhilosophy.Moderate, FedChairPhilosophy.Dovish
            };
            Shuffle(philosophies, rng);

            int count = rng.Next(MinCandidateCount, MaxCandidateCount + 1);
            var candidates = new List<FedChair>(count);
            for (int i = 0; i < count; i++)
            {
                List<FedChair> withPhilosophy = CandidatePool.FindAll(c => c.Philosophy == philosophies[i]);
                FedChair template = withPhilosophy[rng.Next(withPhilosophy.Count)];
                candidates.Add(new FedChair(template.Name, template.Philosophy,
                    template.Description.Replace("{bank}", bank), template.RateBias));
            }

            // K56 (ruled 2026-10-23): "every central-bank shortlist carries a no-lean candidate" - so an AI appointer, which seats the smallest lean
            // (`CentralBankTerms.AiNominee`), can always seat a neutral head, and so can the player. Where the draw holds none, the pool's no-lean candidate
            // is APPENDED - never in a drawn candidate's place (the review's M1: replacing the moderate took a mild lean from a third of the player's lists),
            // so a full draw becomes four [DECLARED: MaxCandidateCount bounds the DRAW, a presentation convention, and the join sits beyond it; the
            // draws above are taken unchanged, so neither generator's sequence moves].
            if (!candidates.Exists(c => c.RateBias == 0f))
            {
                FedChair neutral = CandidatePool.Find(c => c.RateBias == 0f);
                if (neutral != null)
                {
                    var joined = new FedChair(neutral.Name, neutral.Philosophy, neutral.Description.Replace("{bank}", bank), neutral.RateBias);
                    candidates.Add(joined);
                }
            }

            return candidates;
        }

        /// <summary>How the institution is referred to inside a sentence, per country - the bank's statute row (<see cref="CentralBankTerms.For"/>):
        /// the ECB for a euro member since AF-7 gave the ECB a head.</summary>
        public static string BankShortName(CountryId country) => CentralBankTerms.For(country).ShortName;

        private static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Moves a Fed-chair country's CurrencyZone.InterestRate partway (RateAdjustmentSpeed) toward
        /// this turn's target - TaylorRule's suggested rate plus the current chair's RateBias, clamped
        /// to CurrencySystem's sane bounds - rather than jumping straight there, bypassing
        /// PolicyDecision.InterestRateChange entirely. Called from CurrencySystem.
        /// ApplyInterestRateChanges for any country with a non-null CurrentFedChair.
        /// </summary>
        public static void ApplyFedChairInterestRate(Country country)
        {
            float suggested = TaylorRule.GetSuggestedInterestRate(country);
            float target = Mathf.Clamp(
                suggested + country.CurrentFedChair.RateBias,
                CurrencySystem.MinInterestRate, CurrencySystem.MaxInterestRate);
            float current = country.CurrencyZone.InterestRate;
            country.CurrencyZone.InterestRate = current + (target - current) * RateAdjustmentSpeed;
        }
    }
}
