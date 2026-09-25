using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-3h (§635): THE SUPPORT AGREEMENT, ASSERTED. At Sweden's start the government of record's supporter, SD, holds an agreement of demands chosen
    /// from its own positions - laws it aligns with that M does not oppose, and a dial - every item OWED; with M governing, enacting an owed law
    /// delivers it the day the bill applies, repealing it breaks it and the break is recorded on the government; a dial moved past its target is
    /// delivered; the tally reads on the masthead's line. With SD seated the player may withdraw - recorded, the party struck from the support,
    /// the procedure part 6's; S (opposition) and KD (a partner) may not. The record rides the save (format 32).
    /// </summary>
    public static class SupportAgreementDiagnostic
    {
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder();
            int failures = 0;
            sb.Append("=== SupportAgreementDiagnostic (PS-3h, §635): the support agreement's items owed, delivered, broken ===\n");
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            using IDisposable epoch = SimulationManager.EpochScope();
            var go = new GameObject("SupportAgreementDiagnostic");
            try
            {
                WorldClock.ApplyStart(CountryId.Sweden);
                SimulationRandom.Seed(777);
                EnergyMarket.ResetCalibration();
                World world = WorldFactory.CreateDefault();
                SimulationManager sim = go.AddComponent<SimulationManager>();
                sim.SetWorld(world);
                sim.PlayerCountryId = CountryId.Sweden;
                Country sweden = world.GetCountry(CountryId.Sweden);
                GovernmentRecord g = sweden.Government;
                SupportAgreement sd = g.AgreementOf("SD");
                Check(g.Agreements.Count == 1 && sd != null && sd.Supporter == "SD", "Sweden's record carries one agreement, SD's");
                if (sd != null) { foreach (AgreementItem item in sd.Items) { sb.Append("    item      ").Append(item.Line()).Append(" - ").Append(item.Basis).Append('\n'); } }
                Check(sd != null && sd.Items.Count >= 2 && sd.Count(AgreementState.Owed) == sd.Items.Count, F("the demands are owed at the start ({0} items)", sd?.Items.Count ?? 0));
                AgreementItem lawItem = null; if (sd != null) { foreach (AgreementItem item in sd.Items) { if (item.Kind == AgreementItemKind.Law) { lawItem = item; break; } } }
                Check(lawItem != null, "at least one demand is a law");
                bool aligned = true;
                if (sd != null)
                {
                    foreach (AgreementItem item in sd.Items)
                    {
                        if (item.Kind != AgreementItemKind.Law) { continue; }
                        BillConcern concern = ParliamentSystem.GetLawBillConcern(sweden, new LawBill { LawId = item.LawId });
                        float wants = 0f, allows = 0f;
                        foreach (PartyStance st in StanceModel.Stances(sweden, concern)) { if (st.Party.Abbrev == "SD") { wants = st.Alignment; } if (st.Party.Abbrev == "M") { allows = st.Alignment; } }
                        if (wants <= 0f || allows < 0f) { aligned = false; }
                    }
                }
                Check(aligned, "every law demanded is one SD aligns with and M does not oppose - chosen from positions, not authored");

                // M governs: enacting an owed law delivers it the day the bill applies; repealing it breaks it and the break is recorded.
                sweden.PlayerPartyAbbrev = "M";
                int breaksBefore = g.Breaks.Count;
                if (lawItem != null)
                {
                    Check(sim.IntroduceLawBill(CountryId.Sweden, new LawBill { LawId = lawItem.LawId, IsRepeal = false }), "M introduces the owed law");
                    for (int i = 0; i < 25 && !sweden.EnactedLaws.Exists(e => e.LawId == lawItem.LawId); i++) { sim.AdvanceDay(); sim.AdvanceCountryDayTick(CountryId.Sweden); }
                    bool enacted = sweden.EnactedLaws.Exists(e => e.LawId == lawItem.LawId);
                    Check(enacted && lawItem.State == AgreementState.Delivered, F("the law passed and the item is DELIVERED the day it applied ({0})", lawItem.State));
                    if (enacted)
                    {
                        // The repeal: the chamber may refuse a repeal the supporter's bloc opposes, so the break is exercised on the book directly - the government undoing the law is what the track reads.
                        sweden.EnactedLaws.RemoveAll(e => e.LawId == lawItem.LawId);
                        sim.TrackAgreements(sweden);
                        Check(lawItem.State == AgreementState.Broken && g.Breaks.Count == breaksBefore + 1, F("repealed: the item is BROKEN and the break recorded on the government ({0})", g.Breaks.Count > breaksBefore ? g.Breaks[g.Breaks.Count - 1] : "none"));
                    }
                }
                AgreementItem dialItem = null; if (sd != null) { foreach (AgreementItem item in sd.Items) { if (item.Kind == AgreementItemKind.Dial) { dialItem = item; break; } } }
                if (dialItem != null)
                {
                    if (dialItem.Dial == AgreementDial.BorderEnforcement) { sweden.BorderEnforcementLevel = dialItem.Target; } else { sweden.PoliceFundingLevel = dialItem.Target; }
                    sim.TrackAgreements(sweden);
                    Check(dialItem.State == AgreementState.Delivered, F("the dial at its target delivers the item ({0})", dialItem.Name));
                }
                sb.Append("    tally     ").Append(sd?.Tally() ?? "-").Append('\n');

                // The withdrawal: SD may, S and KD may not.
                sweden.PlayerPartyAbbrev = "S";
                Check(!sim.WithdrawSupport(CountryId.Sweden, out string refused) && refused == "YOUR PARTY IS NOT A SUPPORT PARTY", F("S in opposition cannot withdraw support: {0}", refused));
                sweden.PlayerPartyAbbrev = "KD";
                Check(!sim.WithdrawSupport(CountryId.Sweden, out refused), "a junior partner cannot withdraw support (leaving is part 6's)");
                sweden.PlayerPartyAbbrev = "SD";
                int before = g.Breaks.Count;
                Check(sim.WithdrawSupport(CountryId.Sweden, out refused) && !g.Support.Contains("SD") && g.Breaks.Count == before + 1, "SD withdraws: struck from the support, recorded on the government - the procedure is part 6's");
                Check(GovernmentFormation.TryGovernment(sweden, out IReadOnlyList<string> cabinetNow, out IReadOnlyList<string> supportNow) && !supportNow.Contains("SD") && cabinetNow.Contains("M"), "the withdrawal reaches the chamber's votes: the formation reads the stored record without SD (the reader, s635)");

                // The record rides the save.
                Persistence.SaveGame save = Persistence.SaveGameService.CreateSaveGame(sim, world, CountryId.Sweden, null);
                Persistence.SaveGame back = Persistence.SaveGameService.Deserialize(Persistence.SaveGameService.Serialize(save));
                GovernmentRecord loaded = back.World.GetCountry(CountryId.Sweden).Government;
                Check(loaded != null && loaded.Agreements.Count == 1 && loaded.Agreements[0].Items.Count == sd.Items.Count && loaded.Agreements[0].Items[0].State == sd.Items[0].State, "the agreement rides the save (format 32)");
            }
            catch (Exception e) { failures++; sb.Append("    THREW: " + e.GetType().Name + ": " + e.Message + "\n" + e.StackTrace + "\n"); }
            finally { UnityEngine.Object.DestroyImmediate(go); EnergyMarket.ResetTurnState(); }
            if (failures > 0) { Debug.LogError($"SUPPORT AGREEMENT: {failures} failure(s).\n{sb}"); CheckExit.Finish(1); return; }
            Debug.Log(sb.ToString());
            CheckExit.Finish(0);
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
