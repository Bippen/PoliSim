using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Simulation;

namespace PoliSim.Elections
{
    /// <summary>
    /// PS-3h (2026-09-25, §635; the political-system spec's §5.3): THE SUPPORT AGREEMENT AS DATA. "A support agreement is a list of demands the
    /// supporting party chooses from its own positions, each one a law to enact or a dial target to reach; the formateur accepts the demands its
    /// own compatibility allows. The agreement is the support party's leverage for the whole term: the game tracks each item as owed, delivered
    /// or broken." The demands are chosen by the stance model the chamber votes with - the supporter's alignment with each law's own concern,
    /// the formateur's alignment its compatibility - never by an authored list; the Tidö agreement's own items (`sweden/tido_agreement.md`) are the
    /// reference the record compares against, not the data. A broken item is recorded on the government (<see cref="GovernmentRecord.Breaks"/>);
    /// a withdrawal is recorded too - its procedure (a new formation, an extra election, RF 6 kap. 7 §) is part 6's.
    /// </summary>
    public sealed class SupportAgreement
    {
        public string Supporter;
        public DateTime FormedOn;
        public string Basis;
        public List<AgreementItem> Items = new List<AgreementItem>();

        public int Count(AgreementState state) { int n = 0; foreach (AgreementItem item in Items) { if (item.State == state) { n++; } } return n; }

        /// <summary>The masthead's and the tab's one line: 3 OWED · 1 DELIVERED · 0 BROKEN.</summary>
        public string Tally() => string.Format(CultureInfo.InvariantCulture, "{0} OWED · {1} DELIVERED · {2} BROKEN", Count(AgreementState.Owed), Count(AgreementState.Delivered), Count(AgreementState.Broken));

        /// <summary>The number of demands a supporter tables - the spec gives no figure; five is the premise, stated [AUTHORED-DRAFT].</summary>
        public const int Demands = 5;
        /// <summary>A dial demand asks this many points beyond the standing level [AUTHORED-DRAFT].</summary>
        public const float DialStep = 10f;
        /// <summary>A dial item is broken when the government moves the dial this far below where it stood when the demand was made [AUTHORED-DRAFT].</summary>
        public const float DialBreakTolerance = 5f;
        /// <summary>PS-3i-2a (ruled 2026-09-26, §644): the share of its agreement's items an AI supporter tolerates broken - it withdraws its support once
        /// more than this share is broken (<see cref="PastTolerance"/>) [AUTHORED-DRAFT], a play-calibration entry. A share, not a count, so the
        /// tolerance scales with the agreement.</summary>
        public const float BrokenShareTolerated = 0.1f;

        /// <summary>Whether more than <see cref="BrokenShareTolerated"/> of the items are broken.</summary>
        public bool PastTolerance() => Items.Count > 0 && Count(AgreementState.Broken) > BrokenShareTolerated * Items.Count;

        /// <summary>
        /// The supporter's demands from its own positions: every law within the country's competence not yet enacted, scored by the supporter's
        /// alignment with the law's concern (the same `StanceModel` the chamber votes with); the formateur keeps those its own alignment does not
        /// oppose; the best <see cref="Demands"/> are the items, laws first, then a dial target where the supporter wants the dial moved and the
        /// formateur does not object (border enforcement on the immigration axis, police funding on the GAL-TAN axis - the two dials the crime
        /// and justice bill's own concern loads).
        /// <para>The demands are a pure function of the world's state - the chamber's seats and positions, the enacted laws, the two dials, the world's
        /// currency zones - so the scoring over the catalogue runs once per distinct state and the agreement is COPIED after; the key names what the
        /// scoring reads of the country. §642 (the ultrareview of PR #1): the table is THE WORLD's (<see cref="World.SupportAgreementTemplates"/>),
        /// never a static one - a second world can neither see the first's templates nor be answered with its competence. With no world there is no
        /// table and the scoring runs each time.</para>
        /// </summary>
        public static SupportAgreement Demand(Country country, string supporter, string formateur, DateTime formedOn, World world = null)
        {
            if (world == null) { return Score(country, supporter, formateur, formedOn, null); }
            string key = TemplateKey(country, supporter, formateur, formedOn);
            if (!world.SupportAgreementTemplates.TryGetValue(key, out SupportAgreement template)) { template = Score(country, supporter, formateur, formedOn, world); world.SupportAgreementTemplates[key] = template; }
            return template.Copy();
        }

        /// <summary>The template's key - what the scoring reads of the country.</summary>
        private static string TemplateKey(Country country, string supporter, string formateur, DateTime formedOn) =>
            string.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3:yyyyMMdd}|{4}|{5}|{6}|{7}", country.Id, supporter, formateur, formedOn, EnactedKey(country), country.BorderEnforcementLevel, country.PoliceFundingLevel, SeatsKey(country));

        private static string EnactedKey(Country country) { var ids = new List<string>(); foreach (EnactedLaw e in country.EnactedLaws) { ids.Add(e.LawId); } ids.Sort(StringComparer.Ordinal); return string.Join(",", ids); }
        private static string SeatsKey(Country country) { var parts = new List<string>(); if (country.ParliamentSeats != null) { foreach (KeyValuePair<string, int> kv in country.ParliamentSeats) { parts.Add(kv.Key + "=" + kv.Value.ToString(CultureInfo.InvariantCulture)); } } parts.Sort(StringComparer.Ordinal); return string.Join(",", parts); }

        /// <summary>A fresh agreement with the same items - the template stays untouched by the copy's own tracking.</summary>
        public SupportAgreement Copy()
        {
            var copy = new SupportAgreement { Supporter = Supporter, FormedOn = FormedOn, Basis = Basis };
            foreach (AgreementItem item in Items) { copy.Items.Add(new AgreementItem { Kind = item.Kind, LawId = item.LawId, Dial = item.Dial, Target = item.Target, StartValue = item.StartValue, Name = item.Name, State = item.State, DeliveredOn = item.DeliveredOn, BrokenOn = item.BrokenOn, Basis = item.Basis }); }
            return copy;
        }

        private static SupportAgreement Score(Country country, string supporter, string formateur, DateTime formedOn, World world)
        {
            var agreement = new SupportAgreement { Supporter = supporter, FormedOn = formedOn, Basis = "the supporter's own positions on the catalogue's laws and the dials, the formateur's compatibility (the spec's §5.3)" };
            // POSITIONS ALONE (the reader, s635): the stance model's government term would read the PLAYER's seat into every law scored - a partner player inflates
            // the supporter's want and the formateur's leave - so the demands are scored on term 1, the parties' own positions, with no government context.
            var context = (false, (IReadOnlyList<string>)new List<string>(), (IReadOnlyList<string>)new List<string>());
            var scored = new List<(float Score, float Weight, LawDefinition Law)>();
            foreach (LawDefinition law in LawCatalog.All)
            {
                if (!LawCatalog.IsWithinCompetence(world, country, law)) { continue; }
                if (country.EnactedLaws.Exists(e => e.LawId == law.Id)) { continue; }
                BillConcern concern = ParliamentSystem.GetLawBillConcern(country, new LawBill { LawId = law.Id, IsRepeal = false });
                if (concern.IsEmpty) { continue; }
                if (!Alignments(country, concern, supporter, formateur, context, out float wants, out float allows)) { continue; }
                if (wants <= 0.05f || allows < 0f) { continue; }
                scored.Add((wants, Math.Abs(concern.Direction), law));   // the weight: the law's net lean (the signed net of its deltas), the tie-break among a supporter's saturated wants (the reader, s635)
            }
            scored.Sort((a, b) => b.Score != a.Score ? b.Score.CompareTo(a.Score) : b.Weight != a.Weight ? b.Weight.CompareTo(a.Weight) : string.CompareOrdinal(a.Law.Id, b.Law.Id));
            for (int i = 0; i < scored.Count && agreement.Items.Count < Demands - 1; i++)
            {
                agreement.Items.Add(new AgreementItem { Kind = AgreementItemKind.Law, LawId = scored[i].Law.Id, Name = scored[i].Law.PlainName ?? scored[i].Law.Name, State = AgreementState.Owed, Basis = string.Format(CultureInfo.InvariantCulture, "the supporter's alignment {0:+0.00;-0.00}, the law's weight {1:0.#}", scored[i].Score, scored[i].Weight) });
            }
            // One dial demand: the dial the supporter wants moved most, where the formateur does not object.
            (AgreementDial Dial, StanceAxis Axis, float Standing)[] dials =
            {
                (AgreementDial.BorderEnforcement, StanceAxis.ImmigratePolicy, country.BorderEnforcementLevel),
                (AgreementDial.PoliceFunding, StanceAxis.Galtan, country.PoliceFundingLevel),
            };
            float bestWant = 0f; int best = -1;
            for (int i = 0; i < dials.Length; i++)
            {
                var concern = new BillConcern { Direction = DialStep };
                concern.Add(dials[i].Axis, DialStep);
                if (!Alignments(country, concern, supporter, formateur, context, out float wants, out float allows)) { continue; }
                if (wants > bestWant && allows >= 0f) { bestWant = wants; best = i; }
            }
            if (best >= 0)
            {
                agreement.Items.Add(new AgreementItem { Kind = AgreementItemKind.Dial, Dial = dials[best].Dial, Name = DialName(dials[best].Dial) + " to " + (dials[best].Standing + DialStep).ToString("0", CultureInfo.InvariantCulture),
                    Target = dials[best].Standing + DialStep, StartValue = dials[best].Standing, State = AgreementState.Owed, Basis = string.Format(CultureInfo.InvariantCulture, "the supporter's alignment {0:+0.00;-0.00} with a {1}-point move", bestWant, DialStep) });
            }
            return agreement;
        }

        private static bool Alignments(Country country, BillConcern concern, string supporter, string formateur, (bool Government, IReadOnlyList<string> Cabinet, IReadOnlyList<string> Support) context, out float wants, out float allows)
        {
            wants = 0f; allows = 0f; bool sawSupporter = false, sawFormateur = false;
            foreach (PartyStance stance in StanceModel.StancesOver(country, PartySystems.For(country.Id), null, concern, context.Government, context.Cabinet, context.Support))
            {
                if (stance.Party.Abbrev == supporter) { wants = stance.Alignment; sawSupporter = true; }
                if (stance.Party.Abbrev == formateur) { allows = stance.Alignment; sawFormateur = true; }
            }
            return sawSupporter && (sawFormateur || formateur == null);
        }

        /// <summary>Reads every item against the country: a law delivered when enacted and broken when repealed after delivery; a dial delivered at its target and broken when moved away past the tolerance. Returns the items newly broken.</summary>
        public List<AgreementItem> Track(Country country, DateTime date)
        {
            var broken = new List<AgreementItem>();
            foreach (AgreementItem item in Items)
            {
                if (item.State == AgreementState.Broken) { continue; }
                if (item.Kind == AgreementItemKind.Law)
                {
                    bool enacted = country.EnactedLaws.Exists(e => e.LawId == item.LawId);
                    if (item.State == AgreementState.Owed && enacted) { item.State = AgreementState.Delivered; item.DeliveredOn = date; }
                    else if (item.State == AgreementState.Delivered && !enacted) { item.State = AgreementState.Broken; item.BrokenOn = date; broken.Add(item); }
                }
                else
                {
                    float value = DialValue(country, item.Dial);
                    if (item.State == AgreementState.Owed && value >= item.Target) { item.State = AgreementState.Delivered; item.DeliveredOn = date; }
                    else if (value < item.StartValue - DialBreakTolerance) { item.State = AgreementState.Broken; item.BrokenOn = date; broken.Add(item); }
                }
            }
            return broken;
        }

        public static float DialValue(Country country, AgreementDial dial) => dial == AgreementDial.BorderEnforcement ? country.BorderEnforcementLevel : country.PoliceFundingLevel;
        public static string DialName(AgreementDial dial) => dial == AgreementDial.BorderEnforcement ? "Border enforcement" : "Police funding";
    }

    public enum AgreementItemKind { Law, Dial }
    public enum AgreementDial { BorderEnforcement, PoliceFunding }
    public enum AgreementState { Owed, Delivered, Broken }

    /// <summary>One demand: a law to enact, or a dial target to reach; owed until delivered, broken when undone.</summary>
    public sealed class AgreementItem
    {
        public AgreementItemKind Kind;
        public string LawId;
        public AgreementDial Dial;
        public float Target;
        public float StartValue;
        public string Name;
        public AgreementState State;
        public DateTime DeliveredOn;
        public DateTime BrokenOn;
        public string Basis;

        public string Line() => (Name ?? LawId) + " · " + State.ToString().ToUpperInvariant();
    }
}
