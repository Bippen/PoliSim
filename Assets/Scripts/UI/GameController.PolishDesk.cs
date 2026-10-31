using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;

namespace PoliSim.UI
{
    /// <summary>
    /// K197: THE POLISH DESK VERBS' OUTCOMES ON THE DOCKET - the verbs themselves are the Cabinet tab's since COAL-2 with §9 (`DrawCoalitionV35`: LEAVE for a
    /// junior partner, MOVE ART. 158 for the opposition under "In the Sejm"); what they leave on the record is listed here for the month (<see cref="PolishDeskRecord"/>), in
    /// K163's idiom of information that never holds the clock: each Art. 158 vote the Sejm took with its count and who governs now, and the day the
    /// player's party left the cabinet with the standing the government kept. The day's vote is also the strip's place 4 (`TopStrip.Art158Chip`).
    /// </summary>
    public partial class GameController
    {
        /// <summary>K197: the Docket's Polish items; true where any was drawn.</summary>
        private bool DrawPolishDeskItems()
        {
            if (!PolishDeskRecord.Applies(_playerCountry)) { return false; }
            System.DateTime today = _simulationManager.CurrentDate;
            UnityEngine.Color ink = UiPalette.GetAreaColor(UiPalette.SystemArea.Political);
            bool any = DrawSejmStepItems(today, ink);
            foreach (DivisionRecord vote in PolishDeskRecord.RecentArt158(_playerCountry, today))
            {
                string key = "art158:" + vote.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ":" + vote.Number.ToString(CultureInfo.InvariantCulture);
                string when = vote.Date.Date == today.Date ? "today" : DeskDay(vote.Date);
                // the review's L2: one count for the line and the slip - the deputies seated who voted, as the division's title counts them (`ConfidenceProcedure.MotionVote.Title`),
                // and the majority Art. 158 ust. 1 reads off the STATUTORY number of deputies, said as such
                string count = PolishDeskRecord.ForOf(vote).ToString(CultureInfo.InvariantCulture) + " of " + PolishDeskRecord.MembersOf(vote).ToString(CultureInfo.InvariantCulture)
                    + " deputies for, " + (PresidentialVeto.StatutoryDeputies / 2 + 1).ToString(CultureInfo.InvariantCulture) + " needed of the statutory " + PresidentialVeto.StatutoryDeputies.ToString(CultureInfo.InvariantCulture);
                _docketSlipBook.Anchors["docket:" + key] = new SlipContent(("Art. 158 · " + DeskDay(vote.Date)).ToUpperInvariant())
                    .Add(vote.Title.ToUpperInvariant())
                    .Add(PolishDeskRecord.StandingWords(_playerCountry).ToUpperInvariant())
                    .Add("FOR INFORMATION · IT DOES NOT HOLD THE CLOCK · LISTED FOR A MONTH");
                DrawDocketItem(key, "verb:c158", ink, PolishDeskRecord.Art158Words(_playerCountry, vote) + " · " + when, count + " · " + PolishDeskRecord.StandingWords(_playerCountry),
                    holdsTime: false, action: null, stampWords: "Information");
                any = true;
            }
            // COAL-2 with §9: the player's leaving is the coalition verbs' Information entry now, in every country (`DrawCoalitionDocketItems`, led by the leave
            // glyph, the standing as the leaving left it) - PolishDeskRecord.TryLeftOn stays the record's reader
            return any;
        }

        /// <summary>
        /// S11 (Design's return of 2026-10-30, board 11d): THE SEJM'S STEPS ON THE DOCKET - one Information entry per step, newest on top, in the Art. 158 entries'
        /// grammar (the chamber, the political ink, "Information"): each dated by the engine's own day (<see cref="SejmDesk.Entries"/> reads the procedure's dated
        /// log), its title the article and the outcome, its line the figure or what follows, its slip the rest. Listed while the procedure runs and for
        /// <see cref="SejmDesk.ListedDaysAfter"/> days after its last step; information, never holding the clock. True where any was drawn.
        /// </summary>
        private bool DrawSejmStepItems(System.DateTime today, UnityEngine.Color ink)
        {
            SejmFormation f = SejmDesk.Record(_playerCountry);
            if (!SejmDesk.Listed(f, today)) { return false; }
            System.Collections.Generic.List<SejmDesk.Entry> entries = SejmDesk.Entries(f);
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                SejmDesk.Entry e = entries[i];
                string when = e.Day.Date == today.Date ? "today" : DeskDay(e.Day);
                var slip = new SlipContent((e.Label + " · " + DeskDay(e.Day)).ToUpperInvariant());
                foreach (string line in e.Slip) { slip.Add(line); }
                slip.Add("for information · it does not hold the clock");
                _docketSlipBook.Anchors["docket:" + e.Key] = slip;
                DrawDocketItem(e.Key, "chamber", ink, e.Label + " · " + when, e.Line, holdsTime: false, action: null, stampWords: "Information");
            }
            return entries.Count > 0;
        }
    }
}
