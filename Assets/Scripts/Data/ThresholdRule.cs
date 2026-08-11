namespace PoliSim.Data
{
    /// <summary>
    /// The legal bar a party must clear to be allocated any seats.
    ///
    /// <para><b>Four fields, and every one of them exists because exactly one of the six countries needs
    /// it.</b> A single "threshold percent" would be wrong for four of the six, and wrong in a way that
    /// changes which parties are in parliament:</para>
    ///
    /// <list type="bullet">
    /// <item><description><b>Sweden</b> - 4% nationally, <i>or</i> 12% in a single constituency
    /// (<see cref="AlternativeConstituencyShare"/>).</description></item>
    /// <item><description><b>Germany</b> - 5% nationally, <i>or</i> three won constituencies
    /// (<see cref="BasicMandateSeats"/>). The basic-mandate clause was deleted by the 2023 electoral
    /// reform and <b>ordered reinstated by the Bundesverfassungsgericht on 30 July 2024</b> as an interim
    /// fix pending new legislation, so it is live law and not history.</description></item>
    /// <item><description><b>Poland</b> - 5% for a party but <b>8% for a coalition</b>
    /// (<see cref="CoalitionShare"/>), which is a real strategic constraint on how the Polish opposition
    /// organises, not a technicality.</description></item>
    /// <item><description><b>Italy</b> - 3% for a party, 10% for a coalition, or 20% within one region.
    /// Uses all four fields at once.</description></item>
    /// <item><description><b>France, USA</b> - no threshold. Single-member districts have no list to
    /// bar anyone from.</description></item>
    /// </list>
    ///
    /// <para>Shares are FRACTIONS (0.04), not percentages (4.0). One convention, chosen because every
    /// vote share elsewhere in this codebase is already a fraction and a mixed convention in threshold
    /// arithmetic would be a silent factor-of-100 waiting to happen.</para>
    ///
    /// <para>Minority exemption is deliberately NOT here. Germany's SSW and Italy's SVP are exempt as
    /// parties, not as a property of the chamber's rule, so the flag lives on the party - see
    /// <see cref="PoliSim.Simulation.SeatAllocation.ApplyThreshold"/>.</para>
    /// </summary>
    public struct ThresholdRule
    {
        /// <summary>National vote share required, as a fraction. Zero means no national bar.</summary>
        public double NationalShare;

        /// <summary>Share within a single constituency or region that admits a party regardless of its national share. Zero means no such route.</summary>
        public double AlternativeConstituencyShare;

        /// <summary>Share required of a COALITION rather than a single party. Zero means coalitions face the same bar as parties.</summary>
        public double CoalitionShare;

        /// <summary>Directly-won constituency seats that admit a party regardless of its national share. Zero means no basic-mandate clause.</summary>
        public int BasicMandateSeats;

        /// <summary>No bar at all - single-member-district systems, where there is no list to exclude anyone from.</summary>
        public static ThresholdRule None => new ThresholdRule();

        public static ThresholdRule Sweden => new ThresholdRule
        {
            NationalShare = 0.04,
            AlternativeConstituencyShare = 0.12
        };

        public static ThresholdRule Germany => new ThresholdRule
        {
            NationalShare = 0.05,
            BasicMandateSeats = 3
        };

        public static ThresholdRule PolandSejm => new ThresholdRule
        {
            NationalShare = 0.05,
            CoalitionShare = 0.08
        };

        public static ThresholdRule Italy => new ThresholdRule
        {
            NationalShare = 0.03,
            CoalitionShare = 0.10,
            AlternativeConstituencyShare = 0.20
        };
    }
}
