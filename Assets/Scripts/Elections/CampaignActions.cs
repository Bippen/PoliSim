using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-B3 / SPEC §12 — the eight campaign actions, and §42's causal chain as the ONLY path an
    /// action's effect can take. PURE FUNCTIONS, WIRED TO NOTHING (R-N2).
    ///
    /// **The item's whole point, stated as an architectural constraint.** Spec §42 forbids
    /// `Campaign Action → +2 % Votes`. So no type in this file can express a vote share:
    /// <see cref="ChainTrace"/> has no share field, no preference field and no party index, and the
    /// only things an action produces are **persuasion** and **enthusiasm** pressures that a later
    /// layer must combine with compatibility and loyalty to get a preference. An action literally
    /// cannot write a vote share, because there is nowhere in its return type to put one.
    ///
    /// **The chain, in the spec's own order, as a product of necessary factors:**
    /// <code>
    /// reach      = audience x channelReach x §35Effectiveness(spend)
    /// salience   = how much this group cares about the message's issue   (0-1)
    /// exposure   = reach x attention                                     (people who noticed)
    /// relevance  = salience x issueMatch                                 (0-1)
    /// credibility= candidate credibility                                 (0-1)
    /// persuasion = exposure x relevance x credibility x persuasionWeight
    /// enthusiasm = exposure x credibility x enthusiasmWeight
    /// </code>
    /// Every stage MULTIPLIES. That is what makes the chain testable rather than decorative: set
    /// the audience to zero, or aim a message at an issue a group does not care about, or send a
    /// candidate nobody believes — and the effect is exactly zero, not merely smaller. A direct
    /// `+2 %` would survive all three; this cannot.
    ///
    /// **[AUTHORED-DRAFT] action costs and weights** (R-N4, one line each in the prototype log,
    /// all strikeable, all to be calibrated by play). The hour costs follow §9's own examples where
    /// it gives them (rally 4, interview 2, policy announcement 3); the money costs and the
    /// reach/persuasion/enthusiasm weights are game fiction shaped to §12's prose — a town hall
    /// persuades more per head and reaches fewer; door-knocking is the enthusiasm/turnout verb;
    /// television reaches most and persuades least per contact.
    /// </summary>
    public static class CampaignActions
    {
        /// <summary>What an action costs and how it behaves in the chain. All [AUTHORED-DRAFT].</summary>
        public readonly struct ActionSpec
        {
            public readonly CampaignActionKind Kind;
            public readonly double MoneyCost;
            public readonly double Hours;
            /// <summary>Fraction of the targeted audience the channel can touch at full spend.</summary>
            public readonly double ChannelReach;
            /// <summary>Share of those reached who actually attend to the message.</summary>
            public readonly double Attention;
            public readonly double PersuasionWeight;
            public readonly double EnthusiasmWeight;
            /// <summary>How much this action shifts the salience of the issue it is about (§18's channel, used by policy announcements most).</summary>
            public readonly double SalienceShift;
            /// <summary>True where the action is inherently local (its audience is one region).</summary>
            public readonly bool IsLocal;

            public ActionSpec(CampaignActionKind kind, double moneyCost, double hours, double channelReach,
                double attention, double persuasionWeight, double enthusiasmWeight, double salienceShift, bool isLocal)
            {
                Kind = kind; MoneyCost = moneyCost; Hours = hours; ChannelReach = channelReach;
                Attention = attention; PersuasionWeight = persuasionWeight;
                EnthusiasmWeight = enthusiasmWeight; SalienceShift = salienceShift; IsLocal = isLocal;
            }
        }

        /// <summary>
        /// Every stage of §42's chain, kept so a harness — and later §31's attribution — can show
        /// WHERE an effect came from. **Deliberately contains no vote share, no preference and no
        /// party**: this is the ceiling of what an action may produce.
        /// </summary>
        public readonly struct ChainTrace
        {
            public readonly CampaignActionKind Kind;
            public readonly double Reach;
            public readonly double Salience;
            public readonly double Exposure;
            public readonly double Relevance;
            public readonly double Credibility;
            public readonly double Persuasion;
            public readonly double Enthusiasm;
            public readonly double SalienceShift;

            public ChainTrace(CampaignActionKind kind, double reach, double salience, double exposure,
                double relevance, double credibility, double persuasion, double enthusiasm, double salienceShift)
            {
                Kind = kind; Reach = reach; Salience = salience; Exposure = exposure;
                Relevance = relevance; Credibility = credibility; Persuasion = persuasion;
                Enthusiasm = enthusiasm; SalienceShift = salienceShift;
            }

            /// <summary>True when every multiplicative stage was non-zero — i.e. the effect actually travelled the chain.</summary>
            public bool TravelledWholeChain =>
                Reach > 0 && Exposure > 0 && Relevance > 0 && Credibility > 0 && Persuasion > 0;
        }

        /// <summary>Who and what an action is aimed at. A null issue means a general message, which scores the group's AVERAGE salience rather than any issue's.</summary>
        public readonly struct ActionTarget
        {
            public readonly int RegionIndex;   // -1 = national
            public readonly int GroupIndex;    // -1 = all groups
            public readonly IssueId? Issue;

            public ActionTarget(int regionIndex, int groupIndex, IssueId? issue)
            {
                RegionIndex = regionIndex; GroupIndex = groupIndex; Issue = issue;
            }

            public static ActionTarget National(IssueId? issue = null) => new ActionTarget(-1, -1, issue);
        }

        /// <summary>The [AUTHORED-DRAFT] table. Hours follow §9's own figures where it gives them.</summary>
        public static ActionSpec Spec(CampaignActionKind kind)
        {
            switch (kind)
            {
                //                                          money    hrs  reach  atten  persu  enthu  salience local
                case CampaignActionKind.Rally:
                    return new ActionSpec(kind,             300_000,  4.0,  0.06,  0.80,  0.30,  1.00,  0.02,  true);
                case CampaignActionKind.TownHall:
                    return new ActionSpec(kind,              25_000,  3.0,  0.01,  0.95,  1.00,  0.55,  0.01,  true);
                case CampaignActionKind.DoorToDoor:
                    // ⚠ C-N2, RULED 2026-09-02: reach is 1.00 here BY DEFINITION, not by tuning. Door-to-door
                    // is the one action whose audience is an ABSOLUTE COUNT - W-B11 made it "the doors the
                    // volunteers can actually knock in the hours they have" and said in the same sentence
                    // that W-B3's placeholder reach fraction "no longer applies to it". The fraction survived
                    // in this table anyway, so Resolve() went on multiplying the knocked doors by 0.02: a
                    // canvass of 3 000 doors persuaded 60 people, and no rational personality would spend
                    // five hours on it. A knocked door is a reached door. Nothing else in this row moves -
                    // not the price, the hours, the attention or the weights - and the §34 bar is re-measured
                    // on the harness rather than assumed.
                    return new ActionSpec(kind,              15_000,  5.0,  1.00,  0.90,  0.55,  0.90,  0.00,  true);
                case CampaignActionKind.TelevisionAd:
                    return new ActionSpec(kind,             500_000,  1.0,  0.55,  0.35,  0.22,  0.15,  0.03,  false);
                case CampaignActionKind.DigitalAd:
                    return new ActionSpec(kind,             150_000,  1.0,  0.30,  0.45,  0.30,  0.20,  0.02,  false);
                case CampaignActionKind.SocialPost:
                    return new ActionSpec(kind,               5_000,  1.0,  0.12,  0.40,  0.20,  0.45,  0.02,  false);
                case CampaignActionKind.Interview:
                    return new ActionSpec(kind,                   0,  2.0,  0.20,  0.60,  0.45,  0.25,  0.02,  false);
                case CampaignActionKind.PolicyAnnouncement:
                    return new ActionSpec(kind,              50_000,  3.0,  0.18,  0.55,  0.50,  0.30,  0.10,  false);
                default:
                    throw new ArgumentException($"{kind} is not one of §12's eight campaign actions");
            }
        }

        /// <summary>The eight, in the spec's order — what a UI offers and what a harness sweeps.</summary>
        public static readonly CampaignActionKind[] TheEight =
        {
            CampaignActionKind.Rally,
            CampaignActionKind.TownHall,
            CampaignActionKind.DoorToDoor,
            CampaignActionKind.TelevisionAd,
            CampaignActionKind.DigitalAd,
            CampaignActionKind.SocialPost,
            CampaignActionKind.Interview,
            CampaignActionKind.PolicyAnnouncement,
        };

        /// <summary>
        /// Runs §42's chain for one action. Every stage multiplies the last, so a zero anywhere is
        /// a zero at the end — the property that makes "it travelled the chain" checkable.
        /// </summary>
        /// <param name="audience">People the action could in principle touch (region × group).</param>
        /// <param name="issueSalience">How much the target group cares about the message's issue, 0–1.</param>
        /// <param name="issueMatch">How well the party's position on that issue suits the group, 0–1.</param>
        /// <param name="credibility">The candidate's credibility with this audience, 0–1.</param>
        /// <param name="spend">Money actually spent, which enters through §35's saturating curve.</param>
        public static ChainTrace Resolve(ActionSpec spec, double audience, double issueSalience,
            double issueMatch, double credibility, double spend)
        {
            if (audience < 0) { throw new ArgumentException("audience cannot be negative"); }

            // 1. REACH - the audience the channel touches, scaled by §35's diminishing curve.
            //    A free action (interview) still reaches, because its cost is time not money.
            double spendFactor = spec.MoneyCost > 0
                ? CampaignEconomy.Effectiveness(spend, Math.Max(1.0, spec.MoneyCost))
                : 1.0;
            double reach = audience * spec.ChannelReach * spendFactor;

            // 2. SALIENCE - how much this audience cares about what the message is about.
            double salience = Clamp01(issueSalience);

            // 3. EXPOSURE - of those reached, who actually attended.
            double exposure = reach * Clamp01(spec.Attention);

            // 4. MESSAGE RELEVANCE - salience x how well the position suits them.
            double relevance = salience * Clamp01(issueMatch);

            // 5. CREDIBILITY - whether they believe the messenger.
            double credible = Clamp01(credibility);

            // 6. PERSUASION and ENTHUSIASM - the ONLY outputs. Not shares; pressures.
            double persuasion = exposure * relevance * credible * spec.PersuasionWeight;
            double enthusiasm = exposure * credible * spec.EnthusiasmWeight;

            // The salience shift an action leaves behind (§18's channel) also requires exposure -
            // an unheard announcement changes nobody's priorities.
            double salienceShift = exposure > 0 ? spec.SalienceShift * Clamp01(spendFactor) : 0.0;

            return new ChainTrace(spec.Kind, reach, salience, exposure, relevance, credible,
                persuasion, enthusiasm, salienceShift);
        }

        /// <summary>
        /// §42's chain evaluated across the uncertainty in its MEASURED inputs — the low, mid and
        /// high traces. W-E3's screen shows the low–high span and never the mid alone.
        ///
        /// **Why a band exists at all, and why it is not decoration.** A player does not know the
        /// electorate's true salience or how well a position suits a group; they know what POLLING
        /// told them, and a poll carries sampling error (§20–§21, W-B10). The honest estimate of an
        /// action's effect is therefore an interval, and its width is that measured error propagated
        /// through the chain — not an authored "±10 %" bolted on afterwards. Where a quantity was
        /// never measured at all (§36: the player did not buy that poll), there is no estimate to
        /// show and <see cref="Measured"/> is false; the screen must say so rather than print a
        /// number it cannot justify.
        /// </summary>
        public readonly struct ChainBand
        {
            public readonly ChainTrace Low;
            public readonly ChainTrace Mid;
            public readonly ChainTrace High;
            /// <summary>False when an input was never polled — then there is NO estimate, not a wide one.</summary>
            public readonly bool Measured;

            public ChainBand(ChainTrace low, ChainTrace mid, ChainTrace high, bool measured)
            {
                Low = low; Mid = mid; High = high; Measured = measured;
            }

            public static ChainBand Unmeasured(CampaignActionKind kind)
            {
                var empty = new ChainTrace(kind, 0, 0, 0, 0, 0, 0, 0, 0);
                return new ChainBand(empty, empty, empty, measured: false);
            }

            /// <summary>The span of the persuasion estimate, in the same units as the trace.</summary>
            public double PersuasionSpan => High.Persuasion - Low.Persuasion;

            public double EnthusiasmSpan => High.Enthusiasm - Low.Enthusiasm;
        }

        /// <summary>
        /// Resolves §42's chain at the low, mid and high ends of what was measured.
        ///
        /// ⚠ **Evaluating the two CORNERS is exact here, and only because of a property of
        /// `Resolve`.** Salience and issue-match enter the chain in exactly one place —
        /// `relevance = clamp01(salience) × clamp01(match)` — and every later stage multiplies by a
        /// non-negative factor, so persuasion is monotonically non-decreasing in both. The minimum
        /// over the uncertainty box is therefore at (low, low) and the maximum at (high, high); no
        /// interior point can beat either. `ChainBandHarness` asserts this by sweeping the box
        /// rather than trusting the argument. **If a future stage makes the chain non-monotone in
        /// either input, the corners stop being the extremes and this must become a sweep.**
        ///
        /// <paramref name="salienceError"/> and <paramref name="matchError"/> are the half-widths of
        /// the measured 95 % intervals, on the same 0–1 scale as the values themselves. Pass
        /// <paramref name="measured"/> false when the quantity was never polled at all.
        /// </summary>
        public static ChainBand ResolveBand(ActionSpec spec, double audience,
            double issueSalience, double salienceError,
            double issueMatch, double matchError,
            double credibility, double spend, bool measured = true)
        {
            if (salienceError < 0 || matchError < 0)
            {
                throw new ArgumentException("a measured interval's half-width cannot be negative");
            }

            if (!measured) { return ChainBand.Unmeasured(spec.Kind); }

            ChainTrace low = Resolve(spec, audience, issueSalience - salienceError,
                issueMatch - matchError, credibility, spend);
            ChainTrace mid = Resolve(spec, audience, issueSalience, issueMatch, credibility, spend);
            ChainTrace high = Resolve(spec, audience, issueSalience + salienceError,
                issueMatch + matchError, credibility, spend);

            return new ChainBand(low, mid, high, measured: true);
        }

        private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);
    }

    /// <summary>
    /// Where an action's pressures accumulate. Still **no vote shares**: this holds per-party persuasion and enthusiasm pressure (read by
    /// the entrant layer's awareness, the ledgers and the AI), and since A-LOOP q24 5a (`docs/specs/A_LOOP_PLAN.md` §5) the TALLY the vote
    /// reads - per party and per source, the chain's exposure and its exposure-weighted relevance × credibility - which
    /// <see cref="ShareEffects(double, double)"/> turns into each party's share effect through the sourced CACEs (`CampaignPersuasion`).
    /// The campaign no longer writes compatibility: the 40 000-per-point conversion and its clamp at 100 left the vote path with 5a.
    /// </summary>
    public sealed class CampaignPressure
    {
        private readonly double[] _persuasion;
        private readonly double[] _enthusiasm;

        /// <summary>q24 5a: the source slots - the eight actions in `CampaignActions.TheEight`'s order, then the party's earned coverage. CONVENTION (an index).</summary>
        public const int CoverageSlot = 8;

        /// <summary>q24 5a: the number of a party's own source slots. CONVENTION (an index bound).</summary>
        public const int OwnSlots = 9;

        /// <summary>q24 5a: the slot an inclusion predicate names for the attacks a party received (one per class in the tally). CONVENTION (an index).</summary>
        public const int AgainstSlot = 9;

        // q24 5a: [party][slot] exposure (people who noticed) and Σ exposure × relevance × credibility (the chain's persuasion over the action's
        // authored per-kind weight, so the party's strategy still shapes it); [party][class] the same for attacks received.
        private readonly double[][] _exposure;
        private readonly double[][] _quality;
        private readonly double[][] _againstExposure;
        private readonly double[][] _againstQuality;
        // q24 5a (the review's M2): each party's reach - the share of the electorate where it stands (`CampaignRun.Setup.StandingShare`); null = 1 everywhere.
        private readonly double[] _reach;

        /// <param name="reachShare">Each party's share of the electorate it can reach (where it stands); null = every party everywhere.</param>
        public CampaignPressure(int partyCount, double[] reachShare = null)
        {
            _reach = reachShare;
            _persuasion = new double[partyCount];
            _enthusiasm = new double[partyCount];
            _exposure = new double[partyCount][];
            _quality = new double[partyCount][];
            _againstExposure = new double[partyCount][];
            _againstQuality = new double[partyCount][];
            for (int p = 0; p < partyCount; p++)
            {
                _exposure[p] = new double[OwnSlots];
                _quality[p] = new double[OwnSlots];
                _againstExposure[p] = new double[CampaignPersuasion.ClassCount];
                _againstQuality[p] = new double[CampaignPersuasion.ClassCount];
            }
        }

        public int PartyCount => _persuasion.Length;

        /// <summary>q24 5a: the share of the electorate party <paramref name="partyIndex"/> can reach - its coverage is read over it, never past it.</summary>
        public double Reach(int partyIndex) => _reach == null || partyIndex < 0 || partyIndex >= _reach.Length ? 1.0 : _reach[partyIndex];

        /// <summary>
        /// W-B6 / §11's negative campaign: persuasion pressure landing AGAINST a party - <paramref name="share"/> of the attacker's contact
        /// <paramref name="trace"/>. The pressure is netted as before; since q24 5a the contact's exposure and quality, scaled by the share,
        /// enter the target's tally in the contact's class, and <see cref="ShareEffects(double, double)"/> gives them the sign against it.
        /// </summary>
        public void AddAgainst(int opponentIndex, CampaignActions.ChainTrace trace, double share)
        {
            if (share < 0) { throw new ArgumentException("pressure against an opponent is given as a positive share"); }
            _persuasion[opponentIndex] -= trace.Persuasion * share;
            if (Quality(trace) <= 0.0) { return; }   // a contact nobody found relevant or credible persuades nobody, and dilutes nothing
            int c = (int)CampaignPersuasion.ClassOf(trace.Kind);
            _againstExposure[opponentIndex][c] += trace.Exposure * share;
            _againstQuality[opponentIndex][c] += Quality(trace) * share;
        }

        public void Add(int partyIndex, CampaignActions.ChainTrace trace)
        {
            _persuasion[partyIndex] += trace.Persuasion;
            _enthusiasm[partyIndex] += trace.Enthusiasm;
            int slot = Array.IndexOf(CampaignActions.TheEight, trace.Kind);
            if (slot < 0 || Quality(trace) <= 0.0) { return; }   // §42: a zero anywhere in the chain is a contact that persuades nobody - it enters no tally
            _exposure[partyIndex][slot] += trace.Exposure;
            _quality[partyIndex][slot] += Quality(trace);
        }

        /// <summary>C-N1's coverage as the press carried it (`MediaSystem.ResolveCoverage`): pressure as <see cref="Add"/>, tallied in its own slot (earned).</summary>
        public void AddCoverage(int partyIndex, CampaignActions.ChainTrace trace)
        {
            _persuasion[partyIndex] += trace.Persuasion;
            _enthusiasm[partyIndex] += trace.Enthusiasm;
            if (Quality(trace) <= 0.0) { return; }
            _exposure[partyIndex][CoverageSlot] += trace.Exposure;
            _quality[partyIndex][CoverageSlot] += Quality(trace);
        }

        public double Persuasion(int partyIndex) => _persuasion[partyIndex];

        public double Enthusiasm(int partyIndex) => _enthusiasm[partyIndex];

        /// <summary>q24 5a: the exposure tallied in one of a party's own slots, or (with <see cref="AgainstSlot"/>) the attacks it received, summed over the classes.</summary>
        public double Exposure(int partyIndex, int slot)
        {
            if (slot < OwnSlots) { return _exposure[partyIndex][slot]; }
            double s = 0.0;
            foreach (double e in _againstExposure[partyIndex]) { s += e; }
            return s;
        }

        /// <summary>The chain's relevance × credibility behind a contact, times its exposure: its persuasion over the action's authored per-kind weight.</summary>
        private static double Quality(CampaignActions.ChainTrace trace)
        {
            double weight = CampaignActions.Spec(trace.Kind).PersuasionWeight;
            return weight > 0.0 ? trace.Persuasion / weight : 0.0;
        }

        /// <summary>q24 5a: every party's share effect (a FRACTION of the vote, signed), every source included.</summary>
        public double[] ShareEffects(double electorate, double norm) => ShareEffects(electorate, norm, null);

        /// <summary>
        /// q24 5a: each party's share effect (a FRACTION, signed, before `CampaignPersuasion.Apply` scales it to the party's size) from the
        /// sources <paramref name="include"/> admits - (party, slot) over the own slots and <see cref="AgainstSlot"/>; null admits every one.
        /// Coverage is read over the party's own reach (<see cref="Reach"/>). Per class, the admitted own exposures give the coverage and their
        /// mean quality the q; the effect is `CampaignPersuasion.ClassEffectPp`, and the attacks received count the same way with the sign
        /// against. `CampaignRun`'s truth and the night's attribution (`VoteAttribution`) both read this one function.
        /// </summary>
        public double[] ShareEffects(double electorate, double norm, Func<int, int, bool> include)
        {
            int n = _persuasion.Length;
            var effects = new double[n];
            int classes = CampaignPersuasion.ClassCount;
            for (int p = 0; p < n; p++)
            {
                var e = new double[classes];
                var q = new double[classes];
                for (int slot = 0; slot < OwnSlots; slot++)
                {
                    if (include != null && !include(p, slot)) { continue; }
                    int c = (int)SlotClass(slot);
                    e[c] += _exposure[p][slot];
                    q[c] += _quality[p][slot];
                }
                double pp = 0.0;
                for (int c = 0; c < classes; c++) { pp += CampaignPersuasion.ClassEffectPp((ContactClass)c, e[c], q[c], electorate, norm, Reach(p)); }
                if (include == null || include(p, AgainstSlot))
                {
                    for (int c = 0; c < classes; c++) { pp -= CampaignPersuasion.ClassEffectPp((ContactClass)c, _againstExposure[p][c], _againstQuality[p][c], electorate, norm, Reach(p)); }
                }
                effects[p] = pp / 100.0;
            }
            return effects;
        }

        /// <summary>q24 5a, the plan's (4)(b): each party's bound in points - Σ_class upper × coverage over its own contacts, plus the same over the attacks it received - which |effect| can never pass.</summary>
        public double[] BoundsPp(double electorate)
        {
            int n = _persuasion.Length;
            var bounds = new double[n];
            int classes = CampaignPersuasion.ClassCount;
            for (int p = 0; p < n; p++)
            {
                var e = new double[classes];
                for (int slot = 0; slot < OwnSlots; slot++) { e[(int)SlotClass(slot)] += _exposure[p][slot]; }
                for (int c = 0; c < classes; c++)
                {
                    bounds[p] += CampaignPersuasion.BoundPp((ContactClass)c, e[c], electorate, Reach(p)) + CampaignPersuasion.BoundPp((ContactClass)c, _againstExposure[p][c], electorate, Reach(p));
                }
            }
            return bounds;
        }

        /// <summary>q24 5a: one party's own exposure per contact class, every own slot summed - what its own books say it has already covered (the AI's marginal reading).</summary>
        public double[] OwnExposureByClass(int partyIndex)
        {
            var e = new double[CampaignPersuasion.ClassCount];
            for (int slot = 0; slot < OwnSlots; slot++) { e[(int)SlotClass(slot)] += _exposure[partyIndex][slot]; }
            return e;
        }

        /// <summary>q24 5a (review 881b's N3): one party's own Σ exposure × relevance × credibility per contact class - with <see cref="OwnExposureByClass"/>, the run's pooled-quality arithmetic the AI differences.</summary>
        public double[] OwnQualityByClass(int partyIndex)
        {
            var q = new double[CampaignPersuasion.ClassCount];
            for (int slot = 0; slot < OwnSlots; slot++) { q[(int)SlotClass(slot)] += _quality[partyIndex][slot]; }
            return q;
        }

        /// <summary>q24 5a, review 881b's M-r1: the quality tallied in one of a party's own slots - what an independent recomputation of its effect reads.</summary>
        public double QualityOf(int partyIndex, int slot) => _quality[partyIndex][slot];

        /// <summary>q24 5a, review 881b's M-r1: the attacks a party received in one class, exposure and quality.</summary>
        public void Against(int partyIndex, ContactClass c, out double exposure, out double quality)
        {
            exposure = _againstExposure[partyIndex][(int)c];
            quality = _againstQuality[partyIndex][(int)c];
        }

        /// <summary>q24 5a, the review's M5: over every party and class with any contact (own and against), how many the class's upper limit binds on, of how many.</summary>
        public void CapBinding(double norm, out int binding, out int total)
        {
            binding = 0; total = 0;
            int classes = CampaignPersuasion.ClassCount;
            for (int p = 0; p < _persuasion.Length; p++)
            {
                var e = new double[classes];
                var q = new double[classes];
                for (int slot = 0; slot < OwnSlots; slot++) { int c = (int)SlotClass(slot); e[c] += _exposure[p][slot]; q[c] += _quality[p][slot]; }
                for (int c = 0; c < classes; c++)
                {
                    if (e[c] > 0.0) { total++; if (CampaignPersuasion.CapBinds((ContactClass)c, e[c], q[c], norm)) { binding++; } }
                    if (_againstExposure[p][c] > 0.0) { total++; if (CampaignPersuasion.CapBinds((ContactClass)c, _againstExposure[p][c], _againstQuality[p][c], norm)) { binding++; } }
                }
            }
        }

        private static ContactClass SlotClass(int slot) => slot == CoverageSlot ? ContactClass.Earned : CampaignPersuasion.ClassOf(CampaignActions.TheEight[slot]);

        /// <summary>[AUTHORED-DRAFT] `EnthusiasmPerTurnoutPoint = 60 000` — enthusiasm pressure per point of turnout attribute (§26 consumes this, not the preference model). Since q24 5b the count reads doors at the sourced turnout CACE (`GotvTurnout`) and enthusiasm reaches no count: no live count reads this, and the AI values enthusiasm at zero (`CampaignAi`).</summary>
        public const double EnthusiasmPerTurnoutPoint = 60_000.0;

        public double[] ToTurnoutBonus()
        {
            var bonus = new double[_enthusiasm.Length];
            for (int i = 0; i < bonus.Length; i++)
            {
                bonus[i] = _enthusiasm[i] / EnthusiasmPerTurnoutPoint;
            }

            return bonus;
        }
    }
}
