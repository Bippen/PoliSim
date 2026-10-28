using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// PS-8 (Elias's ruling K163: "a foreign election reaches the player as a Docket/news item and a compact results card (a Design board)"): <b>AN ELECTION
    /// ABROAD ON THE DOCKET</b> - each election another country held in the last month (`WorldCalendar.RecentAbroad`) is a docket item in the K38 idiom of
    /// information that never holds the clock: the country and what it elected on the day, the headline under it, its slip carrying who governs now. Its action
    /// opens a compact results card under it - each list a bar by its seats, the seats and their change against the chamber that sat before, the share and its
    /// swing - and the line saying who governs. Minimal and in the docket's existing idiom: the board Design is asked for (D-WORKLIST §6) redraws it.
    /// </summary>
    public partial class GameController
    {
        /// <summary>L8: the day's elections abroad, read once a day (the day, the world and the player's country it was read for).</summary>
        private List<WorldCalendar.Result> _abroadResults;
        private System.DateTime _abroadDay;
        private World _abroadWorld;
        private CountryId _abroadPlayer;

        /// <summary>K163: the elections abroad of the last month as docket items; true where any was drawn.</summary>
        private bool DrawForeignElectionItems()
        {
            if (_simulationManager?.World == null) { return false; }
            System.DateTime today = _simulationManager.CurrentDate;
            // L8 (the s938 review): read once a day - every count, seating and formation abroad lands in the day's step, so the day's list stands until the next
            if (_abroadResults == null || _abroadDay != today || !ReferenceEquals(_abroadWorld, _simulationManager.World) || _abroadPlayer != PlayerCountryId)
            {
                _abroadResults = WorldCalendar.RecentAbroad(_simulationManager.World, PlayerCountryId, today);
                _abroadDay = today; _abroadWorld = _simulationManager.World; _abroadPlayer = PlayerCountryId;
            }
            List<WorldCalendar.Result> results = _abroadResults;
            foreach (WorldCalendar.Result r in results)
            {
                string key = "abroad:" + r.Key;
                Country abroad = _simulationManager.World.GetCountry(r.Country);
                string country = abroad != null ? abroad.Name : r.Country.ToString();
                _docketSlipBook.Anchors["docket:" + key] = new SlipContent((country + " · " + r.Elected + " · " + DeskDay(r.Day)).ToUpperInvariant())
                    .Add(r.Headline.ToUpperInvariant())
                    .Add(r.Governs.ToUpperInvariant())
                    .Add("FOR INFORMATION · IT DOES NOT HOLD THE CLOCK · LISTED FOR A MONTH");
                string when = r.Day == today.Date ? "today" : DeskDay(r.Day);
                if (DrawDocketItem(key, "globe", UiPalette.GetAreaColor(UiPalette.SystemArea.Global), country + " " + r.Verb + " " + r.Elected + " · " + when, r.Headline,
                        holdsTime: false, action: r.Rows.Count > 0 ? "Results" : null, stampWords: "Information"))
                {
                    DrawDocketBody(() => DrawForeignResultCard(r));
                }
            }
            return results.Count > 0;
        }

    }
}
