using System;
using System.Collections.Generic;
using PoliSim.Data;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-7 (Elias's ruling K155): <b>ITALY'S FORMATION - THREE MANDATES, EACH GOVERNMENT TO THE CONFIDENCE OF BOTH CHAMBERS (ART. 94), THEN ART. 88</b>.
    /// The President names a prime minister (Costituzione art. 92: "Il Presidente della Repubblica nomina il Presidente del Consiglio dei ministri e, su proposta
    /// di questo, i ministri"), and the government stands before both chambers for their confidence (art. 94, <see cref="ConfidenceProcedure.Article94"/>). A
    /// mandate fails where the formateur forms no government or its government is refused in either chamber; after <see cref="ConfidenceProcedure.ItalianMandates"/>
    /// failed mandates the chambers are dissolved (art. 88: "Il Presidente della Repubblica può, sentiti i loro Presidenti, sciogliere le Camere") and the new
    /// ones elected within art. 61's seventy days (<see cref="ConfidenceProcedure.ItalianElectionDay"/>).
    /// <para><b>Which government each mandate tries</b> (DECLARED, asked - the President's choice is not modelled): the formation's own government (the one formation
    /// code, `GovernmentFormation`) on three arithmetics in turn - (1) on the Camera's seats, (2) on the Senato's, (3) on both chambers' seats together; a mandate whose
    /// government an earlier mandate already put to the chambers, or that forms none, fails. <b>When</b> (K204): on the new chambers' first sitting, by the day loop (`SimulationManager.Italy.cs`), the night projecting them;
    /// every mandate that day (DECLARED - art. 94's ten days and the consultations are not dated). <b>The two entry points</b> (the review's L3 (b)): mandate 1 reads the
    /// chamber through `GovernmentFormation.ViewOf(country, reading)`, mandates 2 and 3 a seat table through its sibling under the same reading's vintage - DECLARED.</para>
    /// </summary>
    public static class ItalianFormation
    {
        /// <summary>One mandate: what was tried, on which arithmetic, and how each chamber voted.</summary>
        public sealed class Mandate
        {
            public int Number;
            public string Arithmetic;
            public GovernmentFormation.View View;
            public ConfidenceProcedure.MotionVote Camera;
            public ConfidenceProcedure.MotionVote Senato;
            public string Failure;
            public bool Granted => Failure == null && Camera != null && Senato != null && Camera.Carried && Senato.Carried;
        }

        /// <summary>The mandates tried on <paramref name="country"/>'s chambers as seated (the Camera's `ParliamentSeats`, the Senato's `SecondChamberSeats`) under
        /// <paramref name="reading"/>, in order, stopping at the first both chambers grant; <paramref name="granted"/> is that mandate, or null after three failures.</summary>
        public static List<Mandate> Run(Country country, DeclarationReading reading, out Mandate granted)
        {
            granted = null;
            var mandates = new List<Mandate>();
            var tried = new List<string>();
            IReadOnlyDictionary<string, int> camera = country.ParliamentSeats, senato = country.SecondChamberSeats;
            for (int n = 1; n <= ConfidenceProcedure.ItalianMandates; n++)
            {
                var m = new Mandate { Number = n, Arithmetic = n == 1 ? "the Camera's seats" : n == 2 ? "the Senato's seats" : "both chambers' seats together" };
                m.View = n == 1 ? GovernmentFormation.ViewOf(country, reading) : ViewOn(country, n == 2 ? senato : Sum(camera, senato), reading.Vintage);
                mandates.Add(m);
                if (m.View == null || !m.View.HasGovernment || m.View.Cabinet.Count == 0) { m.Failure = "the formateur forms no government on " + m.Arithmetic + (m.View?.Reason != null ? " (" + m.View.Reason + ")" : string.Empty); continue; }
                var cabinet = new List<string>(); var support = new List<string>();
                foreach ((string key, int _) in m.View.Cabinet) { cabinet.Add(key); }
                foreach ((string key, int _) in m.View.Support) { support.Add(key); }
                string signature = string.Join("+", Sorted(cabinet)) + "|" + string.Join("+", Sorted(support));
                if (tried.Contains(signature)) { m.Failure = "the government formed on " + m.Arithmetic + " was put to the chambers by an earlier mandate"; continue; }
                tried.Add(signature);
                string pm = Largest(camera, cabinet);
                m.Camera = ConfidenceProcedure.Article94(country, "Camera", camera, cabinet, support, pm);
                m.Senato = ConfidenceProcedure.Article94(country, "Senato", senato, cabinet, support, pm);
                if (m.Granted) { granted = m; break; }
                m.Failure = (!m.Camera.Carried ? "the Camera" : string.Empty) + (!m.Camera.Carried && !m.Senato.Carried ? " and " : string.Empty) + (!m.Senato.Carried ? "the Senato" : string.Empty) + " refused its confidence";
            }
            return mandates;
        }

        private static GovernmentFormation.View ViewOn(Country country, IReadOnlyDictionary<string, int> seats, ElectionVintage vintage)
        {
            var keys = new List<string>(); var counts = new List<int>();
            if (seats != null) { foreach (KeyValuePair<string, int> kv in seats) { keys.Add(kv.Key); counts.Add(kv.Value); } }
            return GovernmentFormation.ViewOf(country.Id, keys, counts, country.PlayerPartyAbbrev, vintage);
        }

        private static Dictionary<string, int> Sum(IReadOnlyDictionary<string, int> a, IReadOnlyDictionary<string, int> b)
        {
            var sum = new Dictionary<string, int>();
            foreach (IReadOnlyDictionary<string, int> chamber in new[] { a, b })
            {
                if (chamber == null) { continue; }
                foreach (KeyValuePair<string, int> kv in chamber) { sum.TryGetValue(kv.Key, out int n); sum[kv.Key] = n + kv.Value; }
            }
            return sum;
        }

        private static List<string> Sorted(List<string> keys) { var s = new List<string>(keys); s.Sort(StringComparer.Ordinal); return s; }

        /// <summary>The cabinet's party with the most Camera seats (ties to the earlier key in the cabinet's order) - the prime minister's party, as the formation reads it.</summary>
        private static string Largest(IReadOnlyDictionary<string, int> seats, List<string> cabinet)
        {
            string best = cabinet.Count > 0 ? cabinet[0] : null; int bestSeats = -1;
            foreach (string key in cabinet) { int n = seats != null && seats.TryGetValue(key, out int s) ? s : 0; if (n > bestSeats) { best = key; bestSeats = n; } }
            return best;
        }

        /// <summary>One line per mandate, for the log and the government's record.</summary>
        public static string Line(Mandate m) =>
            "mandate " + m.Number + " (on " + m.Arithmetic + "): " + (m.View != null && m.View.HasGovernment ? JoinKeys(m.View.Cabinet) + (m.View.Support.Count > 0 ? " with " + JoinKeys(m.View.Support) : string.Empty) + " - " : string.Empty)
            + (m.Granted ? "both chambers granted their confidence (" + m.Camera.For + "-" + m.Camera.Against + ", " + m.Senato.For + "-" + m.Senato.Against + ")" : m.Failure
               + (m.Camera != null ? " (Camera " + m.Camera.For + "-" + m.Camera.Against + ", Senato " + m.Senato.For + "-" + m.Senato.Against + ")" : string.Empty));

        private static string JoinKeys(List<(string Abbrev, int Seats)> parties)
        {
            var keys = new List<string>();
            foreach ((string key, int _) in parties) { keys.Add(key); }
            return string.Join("+", keys);
        }
    }
}
