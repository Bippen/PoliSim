namespace PoliSim.UI
{
    /// <summary>
    /// PS-6 US-35 (the review's M4): THE LAPSE CHIP'S MEMO - the last day a country was seen lapsed, and the day funding was first seen back after it, keyed on
    /// the country object observed: a different object (a load, a new game, a switch of country) starts it afresh, so one game's lapse never reads as another's
    /// restoration. Funding is "restored" on a day after the last lapsed day seen, within three days of it (a frame may step more than a day at speed); a lapse
    /// planted and lifted on one day (the film's 93c) is never a restoration, as the day must have moved. `UsCongressViewDiagnostic` (e) holds the cases.
    /// </summary>
    public sealed class LapseMemo
    {
        private object _country;
        private System.DateTime _lastLapsed;

        /// <summary>The day funding was seen restored; MinValue where none is held.</summary>
        public System.DateTime RestoredOn { get; private set; }

        /// <summary>Observes <paramref name="country"/> on <paramref name="today"/>, lapsed or not; returns whether today is the day funding was seen restored.</summary>
        public bool Observe(object country, System.DateTime today, bool lapsed)
        {
            System.DateTime day = today.Date;
            if (!ReferenceEquals(country, _country)) { _country = country; _lastLapsed = System.DateTime.MinValue; RestoredOn = System.DateTime.MinValue; }
            if (lapsed) { _lastLapsed = day; RestoredOn = System.DateTime.MinValue; return false; }
            if (_lastLapsed != System.DateTime.MinValue && day > _lastLapsed && (day - _lastLapsed).Days <= 3 && RestoredOn == System.DateTime.MinValue) { RestoredOn = day; }
            if (_lastLapsed != System.DateTime.MinValue && (day < _lastLapsed || (day - _lastLapsed).Days > 3)) { _lastLapsed = System.DateTime.MinValue; }
            return RestoredOn == day;
        }
    }
}
