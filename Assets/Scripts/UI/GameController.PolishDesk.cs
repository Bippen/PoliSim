using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;

namespace PoliSim.UI
{
    /// <summary>
    /// K197: THE POLISH DESK VERBS' OUTCOMES ON THE DOCKET - the verbs themselves are the Parliament page's (`DrawConfidence`: LEAVE THE GOVERNMENT for a
    /// junior partner, MOVE ART. 158 for the opposition); what they leave on the record is listed here for the month (<see cref="PolishDeskRecord"/>), in
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
            bool any = false;
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
                DrawDocketItem(key, "chamber", ink, PolishDeskRecord.Art158Words(_playerCountry, vote) + " · " + when, count + " · " + PolishDeskRecord.StandingWords(_playerCountry),
                    holdsTime: false, action: null, stampWords: "Information");
                any = true;
            }
            if (PolishDeskRecord.TryLeftOn(_playerCountry, today, out System.DateTime left))
            {
                string key = "left:" + left.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                string when = left.Date == today.Date ? "today" : DeskDay(left);
                _docketSlipBook.Anchors["docket:" + key] = new SlipContent(("You left the government · " + DeskDay(left)).ToUpperInvariant())
                    .Add(PolishDeskRecord.StandingWords(_playerCountry).ToUpperInvariant())
                    .Add("FOR INFORMATION · IT DOES NOT HOLD THE CLOCK · LISTED FOR A MONTH");
                DrawDocketItem(key, "chamber", ink, "You left the government · " + when, PolishDeskRecord.StandingWords(_playerCountry),
                    holdsTime: false, action: null, stampWords: "Information");
                any = true;
            }
            return any;
        }
    }
}
