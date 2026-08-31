using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// C-D4 (§38, ruled in as R-CL3): **one party's long-term political capital — the part of a party
    /// that survives an election and is still there at the next one.**
    ///
    /// <para><b>Why it needed building.</b> `PartyProfile` carries `Reputation`, `LeaderAppeal`,
    /// `CampaignEffectiveness`, `Funding` and `Organization` as **per-run constructor defaults**, and
    /// **no campaign state was persisted at all** — zero `Campaign` hits across
    /// `Assets/Scripts/Persistence`. Every game therefore began with every party identical and ended
    /// with nothing carried, which makes "long-term political capital" a phrase rather than a
    /// mechanic.</para>
    ///
    /// <para><b>It lives on `Country`, beside `ElectionHistory`</b> — W-G1's own precedent, and chosen
    /// for the same reason: that is the layer `SaveLoadRoundTripDiagnostic` actually reaches, so the
    /// persistence can be PROVEN rather than asserted. `UiDraftState` cannot be.</para>
    ///
    /// <para>⚠ <b>DONOR AND GRASSROOTS NETWORKS ARE SPECIFIED ABSENT, NOT INVENTED.</b> §38 names them.
    /// No `Donor` concept exists anywhere in this codebase, and there is no sourced figure for what a
    /// Swedish party's donor network is worth — Kammarkollegiet's register is the standing bill
    /// (C-D2). Inventing a donor stock to make the record look complete is exactly what §0.4 forbids,
    /// so the record carries the two things the model can actually observe and says so.</para>
    /// </summary>
    [Serializable]
    public class PartyCampaignCapital
    {
        /// <summary>The abbreviation `PartySystems` uses — the persisted key, matching
        /// `ElectionRecord.Seats`'s keys so the two can be joined without a second name table.</summary>
        public string PartyAbbrev;

        /// <summary>
        /// The party's standing with the public, 0–100.
        ///
        /// ⚠ **NOTHING MOVES THIS YET, and that asymmetry is deliberate rather than unfinished.** An
        /// election produces seats and vote shares; it produces no observation of a party's
        /// *reputation*. Any rule moving reputation after an election would need a coefficient nothing
        /// on disk sources, and the standing rule forbids inventing one to make a stock look alive.
        /// So reputation **persists** — which is itself the change, since before C-D4 it was a
        /// per-run default that reset every game — and its dynamics are a named future item.
        /// </summary>
        public double Reputation;

        /// <summary>
        /// The party's organisational strength, 0–100 — staff, offices, the machine that fights a
        /// campaign.
        ///
        /// ⚠ **This one DOES move, and its rule carries no invented coefficient.** See
        /// <see cref="CarryOver"/>: it moves in proportion to the change in the party's own seat count.
        /// </summary>
        public double OrganizationalStrength;

        /// <summary>The seat count this record was last updated against — the denominator of the next
        /// carry-over, so the ratio is always against the party's own previous mandate and never
        /// against a fixed reference.</summary>
        public int SeatsAtLastUpdate;
    }

    /// <summary>
    /// C-D4: the carry-over itself.
    /// </summary>
    public static class PartyCapital
    {
        /// <summary>
        /// Apply one election's result to a country's stored capital.
        ///
        /// <para>⚠ <b>THE RULE HAS NO INVENTED CONSTANT IN IT, and that is the whole design.</b>
        /// Organisational strength moves by the ratio of the party's new seat count to its previous
        /// one: <c>strength *= newSeats / seatsAtLastUpdate</c>. A party that doubles its mandate
        /// doubles its machine; one that halves it halves it. The ratio is the election's own number,
        /// not a coefficient chosen to feel right — and the SHAPE is the sourced one this project has
        /// already adopted twice: Sweden's public party funding is paid **per mandate** (the
        /// *mandatbidrag* of lag 1972:625), so "a party's organisation follows its seats" is how the
        /// largest component of its money actually arrives.</para>
        ///
        /// <para>⚠ <b>A party that wins ZERO seats keeps its organisation rather than losing all of
        /// it.</b> Multiplying by zero would delete the machine of a party that fell below the
        /// threshold by a tenth of a point, and nothing supports that; the record holds still and the
        /// seat baseline is left where it was, so the party's next result is measured against the last
        /// mandate it actually held.</para>
        ///
        /// <para>⚠ <b>WHAT THIS IS WORTH IN PLAY TODAY: NOTHING, AND THE RECORD SAYS SO.</b> The
        /// electorate does not move with the simulation, so two elections in one game return the same
        /// chamber — every ratio is exactly 1.0 and the carry-over is provably inert. It is built and
        /// proven against a forced seat change so that the day the electorate does move, the capital
        /// is already there and already persisted. Building it after that day would mean shipping a
        /// save-format change on top of a live mechanic instead of before it.</para>
        /// </summary>
        public static void CarryOver(List<PartyCampaignCapital> capital, IReadOnlyDictionary<string, int> seatsByParty)
        {
            if (capital == null || seatsByParty == null) { return; }

            foreach (PartyCampaignCapital record in capital)
            {
                if (record == null || !seatsByParty.TryGetValue(record.PartyAbbrev, out int newSeats)) { continue; }
                if (record.SeatsAtLastUpdate <= 0 || newSeats <= 0) { continue; }

                double ratio = newSeats / (double)record.SeatsAtLastUpdate;
                record.OrganizationalStrength = Clamp(record.OrganizationalStrength * ratio);
                record.SeatsAtLastUpdate = newSeats;
            }
        }

        /// <summary>The 0-100 scale every other party-side stock in this model uses
        /// (`ElectionScales.Clamp`'s range), applied here so a landslide cannot push a machine past
        /// what the rest of the model can read.</summary>
        private static double Clamp(double value) => value < 0 ? 0 : value > 100 ? 100 : value;

        /// <summary>Look one party's record up by its abbreviation, or null.</summary>
        public static PartyCampaignCapital For(List<PartyCampaignCapital> capital, string abbrev)
        {
            if (capital == null) { return null; }

            foreach (PartyCampaignCapital record in capital)
            {
                if (record != null && record.PartyAbbrev == abbrev) { return record; }
            }

            return null;
        }
    }
}
