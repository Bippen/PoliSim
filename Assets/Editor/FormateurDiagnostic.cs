using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// §646 (the formateur's evaluator, POLITICAL_SYSTEM_SPEC.md §5.3's premises 2-3): EVERY AI PARTY ANSWERS A PROPOSAL FROM THE FORMATION'S OWN
    /// MODEL. On Sweden's 2022 chamber with 2022's declarations (the backtest's record: M+KD+L carried by SD): (a) the formation's own government,
    /// proposed by M with Gamson's posts and all of SD's demands, is accepted by every party and passes with the formation's own support; (b) a partner
    /// offered no posts refuses (premise 2); (c) a cabinet a red line falls inside is refused with the line's basis; (d) a supporter whose demands the
    /// formateur refuses past its tolerance refuses; (e) under 2026's declarations V refuses to support a cabinet it is not in (its in-or-against
    /// rule); (f) the investiture gives every party its side and a reason. The formation itself is reproduced exactly by `FormationSweepDiagnostic`.
    /// </summary>
    public static class FormateurDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== FormateurDiagnostic (§646): the parties' answers to a proposal, from the formation's own model ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            try
            {
                WorldClock.ApplyStart(CountryId.Sweden);
                World world = WorldFactory.CreateDefault();
                Country sweden = world.GetCountry(CountryId.Sweden);
                sweden.ParliamentSeats.Clear();
                foreach ((string abbrev, int held) in new[] { ("S", 107), ("SD", 73), ("M", 68), ("V", 24), ("C", 24), ("KD", 19), ("MP", 18), ("L", 16) }) { sweden.ParliamentSeats[abbrev] = held; }
                DateTime date = SimulationManager.EpochDate;
                GovernmentFormation.View formed = GovernmentFormation.ViewOf(sweden, ElectionVintage.Sweden2022);
                var cabinet = formed.Cabinet.ConvertAll(c => c.Abbrev);
                var supporters = formed.Support.ConvertAll(c => c.Abbrev);

                FormationProposal Proposal(string formateur, IEnumerable<string> cab, IEnumerable<string> sup, bool allDemands)
                {
                    var p = new FormationProposal { Formateur = formateur };
                    p.CabinetParties.AddRange(cab);
                    foreach (KeyValuePair<string, List<CabinetPortfolio>> kv in GovernmentRecord.GamsonPosts(sweden, p.CabinetParties, formateur)) { p.Posts[kv.Key] = new List<CabinetPortfolio>(kv.Value); }
                    foreach (string s in sup)
                    {
                        p.Supporters.Add(s);
                        var names = new List<string>();
                        if (allDemands) { foreach (AgreementItem item in SupportAgreement.Demand(sweden, s, formateur, date, world).Items) { names.Add(SupportAgreement.KeyOf(item)); } }
                        p.AcceptedDemands[s] = names;
                    }
                    return p;
                }
                string Answers(ProposalVerdict v) { var parts = new List<string>(); foreach (PartyAnswer a in v.Answers) { parts.Add(a.Party + (a.Accepts ? " accepts" : " REFUSES")); } return string.Join(", ", parts); }

                // (a) the formation's own government, through the evaluator.
                ProposalVerdict own = Formateur.Answer(sweden, Proposal("M", cabinet, supporters, true), date, world, ElectionVintage.Sweden2022);
                Check(string.Join("+", cabinet) == "M+KD+L" && supporters.Contains("SD") && own.Passes && own.Investiture.SupportedSeats == formed.SupportedSeats && own.Investiture.OpposedSeats == formed.OpposedSeats,
                    F("(a) M proposes the formation's own {0} with {1}, Gamson's posts and all of SD's demands: {2}; the investiture passes, {3} carrying it and {4} against - the formation's own count", string.Join("+", cabinet), string.Join("+", supporters), Answers(own), own.Investiture.SupportedSeats, own.Investiture.OpposedSeats));

                // (b) a partner offered no posts.
                FormationProposal stingy = Proposal("M", cabinet, supporters, true);
                List<CabinetPortfolio> kdPosts = stingy.Posts.TryGetValue("KD", out List<CabinetPortfolio> kp) ? kp : new List<CabinetPortfolio>();
                if (!stingy.Posts.ContainsKey("M")) { stingy.Posts["M"] = new List<CabinetPortfolio>(); }
                stingy.Posts["M"].AddRange(kdPosts);
                stingy.Posts["KD"] = new List<CabinetPortfolio>();
                ProposalVerdict b = Formateur.Answer(sweden, stingy, date, world, ElectionVintage.Sweden2022);
                PartyAnswer kd = b.Answers.Find(x => x.Party == "KD");
                Check(kdPosts.Count > 0 && kd != null && !kd.Accepts && !b.Passes, F("(b) KD offered none of its {0} post(s): {1}", kdPosts.Count, kd?.Reason ?? "no answer"));

                // (c) a cabinet a red line falls inside.
                ProposalVerdict c = Formateur.Answer(sweden, Proposal("M", new[] { "M", "SD" }, new string[0], true), date, world, ElectionVintage.Sweden2022);
                PartyAnswer sdIn = c.Answers.Find(x => x.Party == "SD");
                Check(sdIn != null && !sdIn.Accepts && sdIn.Reason.StartsWith("refuses: a red line falls inside", StringComparison.Ordinal), F("(c) M invites SD into the cabinet under 2022's declarations: {0}", sdIn?.Reason ?? "no answer"));

                // (d) a supporter whose demands are all refused.
                ProposalVerdict d = Formateur.Answer(sweden, Proposal("M", cabinet, supporters, false), date, world, ElectionVintage.Sweden2022);
                PartyAnswer sdOut = d.Answers.Find(x => x.Party == "SD");
                Check(sdOut != null && !sdOut.Accepts && sdOut.Reason.Contains("demands"), F("(d) M refuses every demand SD tables: {0}", sdOut?.Reason ?? "no answer"));

                // (e) V's in-or-against rule under 2026's declarations.
                ProposalVerdict e = Formateur.Answer(sweden, Proposal("S", new[] { "S" }, new[] { "V" }, true), date, world, ElectionVintage.Sweden2026);
                PartyAnswer v = e.Answers.Find(x => x.Party == "V");
                Check(v != null && !v.Accepts && v.Reason.StartsWith("supports no cabinet it is not in", StringComparison.Ordinal), F("(e) S alone asks V's support under 2026's declarations: {0}", v?.Reason ?? "no answer"));

                // (f) every party's side and reason at the investiture - the sides adding up to the investiture's own count.
                bool reasons = own.Investiture.Reasons != null && Array.TrueForAll(own.Investiture.Reasons, r => !string.IsNullOrEmpty(r));
                var sides = new List<string>();
                int against = 0, carrying = 0;
                for (int p = 0; p < own.Parties.Count; p++)
                {
                    sides.Add(own.Parties[p].Abbrev + " " + own.Investiture.Sides[p]);
                    int held = sweden.ParliamentSeats.TryGetValue(own.Parties[p].Abbrev, out int h) ? h : 0;
                    if (own.Investiture.Sides[p] == CoalitionFormation.InvestitureSide.Against) { against += held; }
                    if (own.Investiture.Sides[p] == CoalitionFormation.InvestitureSide.InCabinet || own.Investiture.Sides[p] == CoalitionFormation.InvestitureSide.Supports) { carrying += held; }
                }
                Check(reasons && against == own.Investiture.OpposedSeats && carrying == own.Investiture.SupportedSeats,
                    F("(f) the investiture of (a), party by party, every side with its reason and the sides adding up to the count: {0}", string.Join(", ", sides)));

                // (g) two supporters that red-line each other cannot both stay: the formation's own government with C proposed beside SD.
                ProposalVerdict g = Formateur.Answer(sweden, Proposal("M", cabinet, new[] { "SD", "C" }, true), date, world, ElectionVintage.Sweden2022);
                PartyAnswer cAnswer = g.Answers.Find(x => x.Party == "C"), sdAnswer = g.Answers.Find(x => x.Party == "SD");
                Check(cAnswer != null && !cAnswer.Accepts && cAnswer.Reason.Contains("will not share the support") && sdAnswer != null && sdAnswer.Accepts,
                    F("(g) M proposes C as a second supporter beside SD: C {0}; SD {1}", cAnswer?.Reason ?? "no answer", sdAnswer?.Reason ?? "no answer"));

                // (h) K-1f under a named formateur: a party whose own leader is its declared candidate sits only in a cabinet its candidate leads.
                ProposalVerdict hVerdict = Formateur.Answer(sweden, Proposal("C", new[] { "C", "S" }, new string[0], true), date, world, ElectionVintage.Sweden2026);
                PartyAnswer sPartner = hVerdict.Answers.Find(x => x.Party == "S");
                Check(sPartner != null && !sPartner.Accepts && sPartner.Reason.Contains("K-1f"), F("(h) C invites S into a C-led cabinet under 2026's declarations: {0}", sPartner?.Reason ?? "no answer"));
            }
            catch (Exception ex) { failures++; sb.Append("    THREW: " + ex.GetType().Name + ": " + ex.Message + "\n" + ex.StackTrace + "\n"); }
            if (failures > 0) { Debug.LogError($"FORMATEUR: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
