using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;

namespace PoliSim.UI
{
    /// <summary>
    /// S11 (Design's return of 2026-10-30, `PoliSim-captures/design/RETURN_2026-10-30_S11/`, board 11a and RETURN.md's "For Code" 1): <b>THE SEJM'S THREE STEPS
    /// AS THE NIGHT DRAWS THEM</b> - for each step (Art. 154 ust. 1-2, the President's; ust. 3, the Sejm's; Art. 155 ust. 1, the President's second) the party put
    /// to the vote, its count and whether it carried, and the step the formation ends at. Read-only: nothing here is stored or installed.
    /// <para><b>Where the figures come from.</b> The projection (`SimulationManager.ProjectFormation`, a <see cref="FormationProjection"/>) keeps only its end - the
    /// expected cabinet and its vote, or the refused President's cabinet and the reason. The earlier steps' votes are read again here through the engine's own
    /// public functions, in the order `ProjectSejm` and `SejmStep` take them (<see cref="SejmFormation.TryOfRecord"/>, the record's designee,
    /// <see cref="SejmFormation.Carries"/>, <see cref="SejmFormation.TryRecordCabinet"/>, <see cref="GovernmentFormation.ViewOf(Country, DeclarationReading)"/>,
    /// <see cref="SejmFormation.PresidentAppoints(Country, DeclarationReading)"/>) - never a figure of the board's. <b>The projection stays the authority</b>: the
    /// re-read must reach its end (the same expected cabinet and count, the same refused cabinet, the snap election's day named in its reason), and where it does
    /// not the reader keeps the projection's end alone (<see cref="Partial"/>), says so in the log, and the night draws Design's fallback - the earlier cells
    /// refused without a mark.</para>
    /// </summary>
    public sealed class SejmNightSteps
    {
        /// <summary>One step as the night draws it.</summary>
        public sealed class Step
        {
            /// <summary>1 the President's step (Art. 154 ust. 1-2), 2 the Sejm's (ust. 3), 3 the President's second (Art. 155 ust. 1).</summary>
            public int Number;
            /// <summary>The party put to the vote (the prime minister's), null where no one was put.</summary>
            public string Asked;
            public List<string> CabinetParties = new List<string>();
            public List<string> SupportParties = new List<string>();
            /// <summary>The record's head where the record names the cabinet put ("Mateusz Morawiecki (PiS)"), else null.</summary>
            public string Head;
            /// <summary>A vote was taken at this step.</summary>
            public bool Voted;
            public bool Carried;
            public int For, Against, Abstaining, Members;
            /// <summary>Art. 155 ust. 1's majority - more for than against - rather than Art. 154's absolute majority.</summary>
            public bool SimpleMajority;
            /// <summary>Step 3's sides by party (`SejmFormation.PresidentsCabinet.Sides`); null for steps 1 and 2, whose vote is the Carries model: the cabinet and its
            /// supporters for, every other seat against, every member present.</summary>
            public List<DivisionSide> Sides;
        }

        public FormationProjection Projection;
        public readonly Step[] Steps = { new Step { Number = 1 }, new Step { Number = 2 }, new Step { Number = 3, SimpleMajority = true } };
        /// <summary>The step the formation ends at - 1 to 3 where a government is expected, 3 where none is (the third step refused or never put).</summary>
        public int EndStep = 3;
        public bool Expected;
        /// <summary>The re-read did not reach the projection's end: only the end is drawn (RETURN.md "For Code" 1's fallback).</summary>
        public bool Partial;
        public bool OfRecord;
        public DateTime PollingDay, FirstSitting;
        /// <summary>Where none is expected: the day the term is shortened and the snap election's polling day (DERIVED: each step on its window's last day, K304),
        /// MinValue where the projection's reason does not name the day this reader reaches.</summary>
        public DateTime ShortenedOn = DateTime.MinValue, SnapPollingDay = DateTime.MinValue;
        /// <summary>Where none is expected: the party whose cabinet serves on as caretaker - the President's refused cabinet (Art. 162 ust. 2 pkt 1, ust. 3), or the
        /// outgoing cabinet where none was appointed.</summary>
        public string CaretakerParty;

        /// <summary>The step that decided the night - the end step's vote, or where none is expected the last vote taken (null where no vote was taken at all).</summary>
        public Step Deciding
        {
            get
            {
                if (Expected) { return Steps[EndStep - 1]; }
                for (int i = Steps.Length - 1; i >= 0; i--) { if (Steps[i].Voted) { return Steps[i]; } }
                return null;
            }
        }

        /// <summary>The absolute majority of a vote's members (Art. 154 ust. 2-3, as <see cref="SejmFormation.Carries"/> reads it: more than half).</summary>
        public static int Needed(int members) => members / 2 + 1;

        /// <summary>
        /// The steps of <paramref name="pollingDay"/>'s Sejm on <paramref name="country"/>'s chamber, read against <paramref name="projection"/>. Pure: it reads the
        /// country and the record and writes nothing (`GovernmentRecord.FromView` is asked with no world, so no support template is cached).
        /// </summary>
        public static SejmNightSteps Read(Country country, DateTime pollingDay, FormationProjection projection)
        {
            var s = new SejmNightSteps { Projection = projection, PollingDay = pollingDay.Date };
            if (country == null || !SejmFormation.Applies(country.Id)) { s.Partial = true; return s; }
            s.FirstSitting = SejmFormation.FirstSittingAfter(country, pollingDay);
            if (projection == null) { s.Partial = true; return s; }
            try { ReadSteps(country, pollingDay, s); }
            catch (Exception e)
            {
                Debug.LogError($"CANVAS: SEJM NIGHT - the Sejm's steps could not be read again ({e.GetType().Name}: {e.Message}) - the night draws the projection's end alone");
                s.Partial = true;
            }
            string disagree = s.Partial ? null : Disagreement(s, projection);
            if (disagree != null)
            {
                Debug.LogError("CANVAS: SEJM NIGHT - the Sejm's steps as read again disagree with the projection (" + disagree + ") - the night draws the projection's end alone, the earlier cells refused without a mark");
                s.Partial = true;
            }
            if (s.Partial) { FromProjectionAlone(country, projection, s); }
            return s;
        }

        private static void ReadSteps(Country country, DateTime pollingDay, SejmNightSteps s)
        {
            bool ofRecord = SejmFormation.TryOfRecord(country.Id, pollingDay, out SejmFormation.StepsOfRecord record);
            s.OfRecord = ofRecord;
            DeclarationReading reading = DeclarationReading.OfElection(country.Id, pollingDay);

            // Art. 154 ust. 1-2: the record's designee where the record names one, else the formation's projection on the new Sejm (`ProjectSejm`'s first block)
            Step one = s.Steps[0];
            if (ofRecord && WorldClock.TryGovernmentAt(country.Id, record.Appointed, out WorldClock.GovernmentOfRecord designee) && designee.Cabinet != null)
            {
                Put(one, new List<string>(designee.Cabinet), new List<string>(designee.Support ?? new string[0]), SejmFormation.PartyOfHead(designee.Head) ?? designee.Cabinet[0], designee.Head);
            }
            else
            {
                GovernmentFormation.View view = GovernmentFormation.ViewOf(country, reading);
                if (view != null && view.HasGovernment) { Put(one, Keys(view.Cabinet), Keys(view.Support), GovernmentRecord.FromView(country, view, s.FirstSitting).PmParty, null); }
            }
            if (one.Voted) { Vote(country, one); }
            if (one.Voted && one.Carried) { s.EndStep = 1; s.Expected = true; return; }

            // ust. 3: the record's cabinet where the record names one; short of a majority (or with none named), the formation's projection put instead
            Step two = s.Steps[1];
            if (ofRecord && SejmFormation.TryRecordCabinet(country, pollingDay, out List<string> rc, out List<string> rs, out string rpm, out string rhead, out bool rCarries, out int rYes, out int rMembers))
            {
                Put(two, rc, rs, rpm, rhead);
                two.Carried = rCarries; two.For = rYes; two.Members = rMembers; two.Against = rMembers - rYes;
                if (rCarries) { s.EndStep = 2; s.Expected = true; return; }
            }
            GovernmentFormation.View elects = GovernmentFormation.ViewOf(country, reading);
            if (elects != null && elects.HasGovernment)
            {
                // the last vote put at the step is the one its cell shows
                Put(two, Keys(elects.Cabinet), Keys(elects.Support), GovernmentRecord.FromView(country, elects, s.FirstSitting).PmParty, null);
                Vote(country, two);
                if (two.Carried) { s.EndStep = 2; s.Expected = true; return; }
            }

            // K302, Art. 155 ust. 1: the President's cabinet and the Sejm's confidence by a simple majority; ust. 2, refused, the term shortened
            Step three = s.Steps[2];
            SejmFormation.PresidentsCabinet named = SejmFormation.PresidentAppoints(country, reading);
            s.EndStep = 3;
            if (named.Cabinet.Count > 0)
            {
                Put(three, new List<string>(named.Cabinet), new List<string>(named.Support), named.PmParty, null);
                three.Carried = named.Carried; three.For = named.For; three.Against = named.Against; three.Abstaining = named.Abstaining; three.Members = named.Members;
                three.Sides = new List<DivisionSide>(named.Sides);
                if (named.Carried) { s.Expected = true; return; }
            }
            // the shortening's day as `ProjectSejm` dates it in the player's Poland (`SejmStep`'s days, K304): ust. 3's fourteen days after its vote, the President's
            // fourteen, the vote on the appointment's fourteenth - or the President's own day where he finds no admissible cabinet
            DateTime sejmVoted = ofRecord ? record.SejmElected : s.FirstSitting.AddDays((one.Voted ? 2 : 1) * SejmFormation.StepWindowDays);
            DateTime appointedBy = sejmVoted.AddDays(SejmFormation.PresidentAppointsAfterSejmVoteDays);
            s.ShortenedOn = named.Cabinet.Count > 0 ? appointedBy.AddDays(SejmFormation.StepWindowDays) : appointedBy;
            s.SnapPollingDay = SejmFormation.ShortenedTermElectionDay(s.ShortenedOn);
            s.CaretakerParty = named.Cabinet.Count > 0 ? named.PmParty : country.Government?.PmParty;
        }

        private static void Put(Step step, List<string> cabinet, List<string> support, string pm, string head)
        {
            step.Voted = true;
            step.CabinetParties = cabinet ?? new List<string>();
            step.SupportParties = support ?? new List<string>();
            step.Asked = pm;
            step.Head = head;
        }

        /// <summary>Art. 154's vote, as <see cref="SejmFormation.Carries"/> takes it: the cabinet and its supporters for, every other seat against.</summary>
        private static void Vote(Country country, Step step)
        {
            step.Carried = SejmFormation.Carries(country, step.CabinetParties, step.SupportParties, out int yes, out int members);
            step.For = yes;
            step.Members = members;
            step.Against = members - yes;
            step.Abstaining = 0;
        }

        /// <summary>Null where the re-read reaches the projection's end; else what differs.</summary>
        private static string Disagreement(SejmNightSteps s, FormationProjection p)
        {
            if (s.Expected != p.Expected) { return "expected " + s.Expected + " against " + p.Expected; }
            if (s.Expected)
            {
                Step end = s.Steps[s.EndStep - 1];
                if (!SameKeys(end.CabinetParties, p.Cabinet)) { return "the cabinet " + string.Join("+", end.CabinetParties) + " against " + string.Join("+", p.Cabinet); }
                if (end.For != p.VotesFor || end.Members != p.Members) { return string.Format(CultureInfo.InvariantCulture, "the vote {0} of {1} against {2} of {3}", end.For, end.Members, p.VotesFor, p.Members); }
                if (StepOfProjection(p) != s.EndStep) { return "the step " + s.EndStep.ToString(CultureInfo.InvariantCulture) + " against the projection's \"" + p.Step + "\""; }
                return null;
            }
            if (s.Steps[2].Asked != p.RefusedPmParty) { return "the President's cabinet led by " + (s.Steps[2].Asked ?? "none") + " against " + (p.RefusedPmParty ?? "none"); }
            string day = s.SnapPollingDay.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
            if (p.Reason == null || p.Reason.IndexOf(day, StringComparison.Ordinal) < 0)
            {
                // the day alone is withheld: the steps agree, the date the reason names does not
                Debug.LogError("CANVAS: SEJM NIGHT - the snap election's day read again (" + day + ") is not the projection's (\"" + (p.Reason ?? "no reason") + "\") - the night names no day");
                s.SnapPollingDay = DateTime.MinValue;
            }
            return null;
        }

        /// <summary>The step a projection names in its own words - 1 for Art. 154 ust. 2's confidence, 2 for ust. 3's election, 3 for Art. 155 ust. 1's; 3 where none is
        /// expected.</summary>
        public static int StepOfProjection(FormationProjection p)
        {
            if (p == null || !p.Expected || p.Step == null) { return 3; }
            if (p.Step.IndexOf("ART. 155 UST. 1", StringComparison.Ordinal) >= 0) { return 3; }
            if (p.Step.IndexOf("ART. 154 UST. 3", StringComparison.Ordinal) >= 0) { return 2; }
            return 1;
        }

        /// <summary>RETURN.md "For Code" 1's fallback: only the end is known - the end step's party and, where expected, its vote as the projection gives it.</summary>
        private static void FromProjectionAlone(Country country, FormationProjection p, SejmNightSteps s)
        {
            for (int i = 0; i < s.Steps.Length; i++) { s.Steps[i] = new Step { Number = i + 1, SimpleMajority = i == 2 }; }
            s.Expected = p != null && p.Expected;
            s.EndStep = StepOfProjection(p);
            s.ShortenedOn = DateTime.MinValue;
            s.SnapPollingDay = DateTime.MinValue;
            if (p == null) { return; }
            Step end = s.Steps[s.EndStep - 1];
            if (s.Expected)
            {
                end.Voted = true; end.Carried = true; end.Asked = p.PmParty; end.Head = p.Head;
                end.CabinetParties = new List<string>(p.Cabinet); end.SupportParties = new List<string>(p.Support);
                end.For = p.VotesFor; end.Members = p.Members;
                // Art. 154's vote has no abstention (every other seat against); Art. 155's against is not on the projection, so it is not drawn
                end.Against = end.SimpleMajority ? -1 : p.Members - p.VotesFor;
            }
            else
            {
                end.Asked = p.RefusedPmParty;
                end.CabinetParties = new List<string>(p.RefusedCabinet);
                s.CaretakerParty = p.RefusedPmParty ?? country?.Government?.PmParty;
            }
        }

        private static List<string> Keys(List<(string Abbrev, int Seats)> parties)
        {
            var keys = new List<string>();
            if (parties != null) { foreach ((string abbrev, int _) in parties) { keys.Add(abbrev); } }
            return keys;
        }

        private static bool SameKeys(List<string> a, List<string> b)
        {
            if (a == null || b == null || a.Count != b.Count) { return false; }
            foreach (string k in a) { if (!b.Contains(k)) { return false; } }
            return true;
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------
        // THE FOOT (11b), DECLUTTERED (K320)
        // ---------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>K320 (2) as K321 (b) reads it: a line at rest holds at most this many counted words (LONG past it) - the foot's clauses drop whole against it as
        /// well as against the room before CONTINUE.</summary>
        public const int RestWords = 8;

        /// <summary>K321 (b)'s count: a word that holds a letter (figures are uncounted); split on spaces.</summary>
        public static int CountedWords(string text)
        {
            if (string.IsNullOrEmpty(text)) { return 0; }
            int n = 0;
            foreach (string token in text.Split(' '))
            {
                foreach (char c in token) { if (char.IsLetter(c)) { n++; break; } }
            }
            return n;
        }

        /// <summary>A cabinet's or a support's short names joined as one group: K312's rule through its one helper - "+" (KO+TD+NL), " · " only where a name
        /// holds a '+'. S10 (Design's return of 2026-10-30, 10a: "§11's verdicts now read KO+TD+NL") replaced S11's own " · " join here, so the foot, the
        /// steps' † lines and the Docket's titles and slips read one join.</summary>
        public static string Names(IReadOnlyList<string> keys)
        {
            var names = new List<string>(keys?.Count ?? 0);
            if (keys != null) { foreach (string k in keys) { names.Add(PartySystems.ShortName(CountryId.Poland, k)); } }
            return PartySystems.JoinShortNames(names);
        }
    }

    /// <summary>
    /// S11 (11b): the Sejm night's foot - its clauses in RETURN.md's fixed order (the outcome, the figure that decided it, the date or the caretaker), each short
    /// enough for K320's cap, and the whole sentence for the verdict's slip: the outcome first, then the procedure word for word as built before
    /// ("The Sejm first sits on ..., where the cabinet resigns; the next is appointed by Art. 154's steps, or failing them Art. 155's.").
    /// </summary>
    public sealed class SejmFoot
    {
        /// <summary>✓ where a government is expected, ✗ where none is (or the player's party left the chamber); null where the night names no outcome.</summary>
        public bool? Carried;
        public readonly List<string> Clauses = new List<string>();
        /// <summary>The whole verdict, the slip's sentence (and `_pendingElectionVerdict`).</summary>
        public string Whole;
    }
}
