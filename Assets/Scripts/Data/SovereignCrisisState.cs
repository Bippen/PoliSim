namespace PoliSim.Data
{
    /// <summary>MF q33 (K141-K144): where a country stands with the debt market - in it, in default (the USA's branch, K142), or in an official
    /// programme (the EU countries' branch, K142 and K144). <see cref="PoliSim.Simulation.SovereignCrisis"/> writes it; the fiscal day reads it.</summary>
    public enum CrisisPhase { None = 0, Default = 1, Programme = 2 }

    /// <summary>
    /// MF q33 (ruled 2026-10-18 q33; K141-K144, 2026-10-27): a country's debt-crisis state and the money the old clamps used to swallow, booked where a
    /// reader can see it. It rides the <see cref="Country"/> (the game holds several worlds at once, so a country's state never sits in a table keyed by
    /// its id), is saved with it, and is copied into the preview's clone (`SimulationManager.ClonePreviewCountry`).
    /// </summary>
    public class SovereignCrisisState
    {
        public CrisisPhase Phase;

        /// <summary>The day the standing crisis began (market access lost: the rating's review read CCC, K141); MinValue when none stands.</summary>
        public System.DateTime StartedOn;

        /// <summary>The default branch: the last day of the exclusion from the market. The programme branch: the programme's last day.</summary>
        public System.DateTime EndsOn;

        /// <summary>How many crises this country has entered in the run - a programme followed by a second is two.</summary>
        public int Crises;

        /// <summary>K212: the rating the trigger reads, taken at each scheduled review from the same settled position - the standing rating's, except that the
        /// USA's carries no reserve-currency discount (K143: "no exemption for its own currency"). <see cref="TriggerReviewedOn"/> is MinValue until the first.</summary>
        public CreditRating TriggerRating;
        public System.DateTime TriggerReviewedOn;

        // --- the default branch (the USA, K142) ---

        /// <summary>The face value written off at the default, in the book's billions (nominal) - the haircut, booked as a dated event on the debt ledger.</summary>
        public float HaircutBillions;

        /// <summary>The haircuts by date (the q32 review's H1): the rating's settled deficit, read off the debt stock's change, adds back a write-off inside its
        /// year (`CreditRatingSystem.GetSettledPosition`), so a default reads as no surplus.</summary>
        public System.Collections.Generic.List<DatedAmount> Haircuts = new System.Collections.Generic.List<DatedAmount>();

        /// <summary>The default's output cost still to land: the GDP taken off each day, and the days left (the event shock's idiom, `EventSystem.ApplyEvent`).</summary>
        public float OutputCostPerDay;
        public int OutputCostDaysLeft;

        // --- the programme branch (the euro countries and Sweden and Poland, K142 and K144) ---

        /// <summary>The deficit, per cent of GDP, the programme opened on (the trailing year the rating's review read), and the programme year now running (1 to 3).</summary>
        public float EntryDeficitPercent;
        public int ProgrammeYear;

        /// <summary>The day the running programme year closes and is reviewed against its target.</summary>
        public System.DateTime NextReviewOn;

        /// <summary>A review that found the target missed withholds the next year's disbursement: through this day the deficit is not financed (MinValue when
        /// nothing is withheld).</summary>
        public System.DateTime WithheldThrough;

        /// <summary>How many programme reviews found the year's target missed.</summary>
        public int TargetsMissed;

        // --- the money no clamp swallows any more (q33: "no money vanishes"), cumulative over the run ---

        /// <summary>Non-interest outlays cut because the deficit could not be financed - in default's exclusion, or in a programme year whose disbursement was
        /// withheld (`SovereignCrisis.CutToday`).</summary>
        public float CutBillions;

        /// <summary>Fund contributions the ceiling refused at booking - the money stayed in the budget.</summary>
        public float SwfRefusedBillions;

        /// <summary>Fund assets above the ceiling paid out into the budget with the structural draw (a fall in GDP or a return can carry the fund past it).</summary>
        public float SwfOverflowBillions;

        /// <summary>What the debt's runaway guards absorbed - zero in a sound run: a guard that binds is a bug report, and `MoneyIdentityDiagnostic` fails on it.</summary>
        public float GuardAbsorbedBillions;

        /// <summary>A field-for-field copy, for the preview's clone (its own, so the preview's turn writes nothing back; the haircut list copied too).</summary>
        public SovereignCrisisState Copy()
        {
            var copy = (SovereignCrisisState)MemberwiseClone();
            copy.Haircuts = Haircuts != null ? new System.Collections.Generic.List<DatedAmount>(Haircuts) : new System.Collections.Generic.List<DatedAmount>();
            return copy;
        }
    }

    /// <summary>MF q33: a dated sum in the book's billions (nominal).</summary>
    public struct DatedAmount
    {
        public System.DateTime Date;
        public float Billions;
    }
}
