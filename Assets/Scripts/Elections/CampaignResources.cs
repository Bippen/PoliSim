using System;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-B2 / SPEC §9 + §35 — campaign resources and the shape of what spending buys. PURE
    /// FUNCTIONS AND IMMUTABLE VALUES, WIRED TO NOTHING (R-N2).
    ///
    /// Three resources, because the spec names three and they constrain differently:
    /// - **Money** — refills between elections, spends on everything, and obeys §35.
    /// - **Time** — a fixed budget of hours PER CAMPAIGN DAY that cannot be saved, borrowed or
    ///   bought. It is the resource that makes a campaign a series of choices rather than a
    ///   shopping list: money can be raised, hours cannot.
    /// - **Volunteers** — the grassroots stock §26 turns into turnout and §10's offices grow.
    ///
    /// **§35's diminishing returns as a DECLARED CURVE, not a table of magic numbers.** The spec
    /// describes the shape in prose (€0→100k huge, 100k→500k large, 500k→2m moderate, 2m→10m
    /// small). A lookup table reproducing those four bands would be four magic numbers and a cliff
    /// at every boundary. Instead one saturating curve:
    /// <code>
    /// effectiveness(spend) = 1 - exp(-spend / scale)
    /// </code>
    /// Smooth, bounded in [0,1), with marginal return strictly decreasing everywhere — so the first
    /// krona is worth vastly more than the millionth **by construction rather than by decree**, and
    /// there is no threshold a player can game by spending just over it.
    ///
    /// **[AUTHORED-DRAFT] constants** (R-N4; one line each, all strikeable, all to be calibrated by
    /// play — and `MoneyScale` in particular should be re-derived from real party spending when
    /// W-F5 sources it):
    /// - `MoneyScale = 500_000` (SEK) — the spend at which ~63 % of an action's achievable effect
    ///   is bought. Chosen so the spec's four bands fall out of one curve: ~18 % of the effect at
    ///   100k, ~63 % at 500k, ~98 % at 2m, ~100 % at 10m.
    /// - `HoursPerCampaignDay = 12` — a long working day, so §9's action costs (rally 4, interview
    ///   2, debate prep 6, regional tour 8) force genuine daily trade-offs.
    /// - `VolunteerHoursPerDay = 3` — what one volunteer contributes to a day's ground game.
    ///
    /// **No resource can go negative.** `TrySpend` refuses rather than clamping, because a clamp
    /// would silently let an over-budget campaign proceed at a discount.
    /// </summary>
    public static class CampaignEconomy
    {
        public const double MoneyScale = 500_000.0;
        public const double HoursPerCampaignDay = 12.0;
        public const double VolunteerHoursPerDay = 3.0;

        /// <summary>§35's curve: the fraction of an action's achievable effect bought by <paramref name="spend"/>. Bounded in [0,1), strictly increasing, strictly concave.</summary>
        public static double Effectiveness(double spend, double scale = MoneyScale)
        {
            if (spend <= 0.0) { return 0.0; }
            if (scale <= 0.0) { throw new ArgumentException("scale must be positive"); }

            return 1.0 - Math.Exp(-spend / scale);
        }

        /// <summary>The marginal effect of the next unit of money at a given level of spend — the number that makes "diminishing" checkable rather than asserted.</summary>
        public static double MarginalEffectiveness(double spend, double scale = MoneyScale)
        {
            if (spend < 0.0) { return 0.0; }
            if (scale <= 0.0) { throw new ArgumentException("scale must be positive"); }

            return Math.Exp(-spend / scale) / scale;
        }

        /// <summary>Volunteer hours available for a day's ground game.</summary>
        public static double VolunteerHours(int volunteers) => Math.Max(0, volunteers) * VolunteerHoursPerDay;
    }

    /// <summary>
    /// A campaign's resources at a moment. Immutable: every spend returns a new pool, so a failed
    /// spend cannot leave a half-mutated one behind.
    /// </summary>
    public readonly struct ResourcePool
    {
        public readonly double Money;
        public readonly double Hours;
        public readonly int Volunteers;

        public ResourcePool(double money, double hours, int volunteers)
        {
            if (money < 0.0 || hours < 0.0 || volunteers < 0)
            {
                throw new ArgumentException("a resource pool cannot be created negative");
            }

            Money = money;
            Hours = hours;
            Volunteers = volunteers;
        }

        /// <summary>A fresh campaign day: money and volunteers carry over, hours reset. Time is the resource that cannot be banked.</summary>
        public ResourcePool StartDay() => new ResourcePool(Money, CampaignEconomy.HoursPerCampaignDay, Volunteers);

        /// <summary>
        /// Spends money and hours if BOTH are available, returning the resulting pool. Refuses
        /// (returns false, pool unchanged) rather than clamping — a clamp would let an over-budget
        /// campaign act at a silent discount, which is exactly the kind of quiet forgiveness that
        /// makes a resource system decorative.
        /// </summary>
        public bool TrySpend(double money, double hours, out ResourcePool result)
        {
            if (money < 0.0 || hours < 0.0)
            {
                throw new ArgumentException("a spend cannot be negative");
            }

            if (money > Money || hours > Hours)
            {
                result = this;
                return false;
            }

            result = new ResourcePool(Money - money, Hours - hours, Volunteers);
            return true;
        }

        public ResourcePool WithMoney(double money) => new ResourcePool(Math.Max(0.0, money), Hours, Volunteers);

        public ResourcePool AddVolunteers(int delta) =>
            new ResourcePool(Money, Hours, Math.Max(0, Volunteers + delta));

        public override string ToString() =>
            string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "money {0:N0}, hours {1:F1}, volunteers {2:N0}", Money, Hours, Volunteers);
    }
}
