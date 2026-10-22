using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// SEAT-1 (Elias's ruling J2, 2026-10-19, `COMPLETED.md` §869; the feature list's SEAT-1): THE CHAMBERS OF RECORD THAT NO LIST TABLE SEATS, RE-DERIVED
    /// FROM THE STORED PAGES. Every figure `PartySystems` carries for them is read again here from the files it cites, and the check fails while one differs:
    /// <list type="bullet">
    /// <item><description><b>The XVIII Camera</b> (`PartySystems.ItalyCamera2018`): each deputy's group on the declared day from the Camera's open data
    /// (`italy/raw/reseat_2022/dati_camera_leg18_adesioni_gruppi.csv`), the mixed group's components from the Camera's historical portal (`italy/raw/camera2018/`:
    /// the MISTO page names every component, each component's page dates its members), each group or component under the key the table names - 630 seats,
    /// seat for seat. The declared day is held to the window the comment claims (the same table from 20 April to 7 May 2018, a different one on the 19th
    /// and on 8 May).</description></item>
    /// <item><description><b>The XIX Senato</b> (`PartySystems.ItalySenato2022`): each group's total on the groups page of 18 Oct 2022 (`italy/raw/senato2022/`), split
    /// into elected and for-life by the Senato's open data (`dati_senato_leg19_adesioni_gruppi.csv`, `dati_senato_leg19_mandati.csv`) - 200 elected, six for
    /// life, five of them in a group; the page names no component of the Misto group.</description></item>
    /// <item><description><b>AVS</b> (J2 on E-48): SI's and EV's rows are CHES 2024's on every field, and AVS sits at their equal-weight mean - a planted
    /// three-to-one weighting gives another point, so a seat weighting would fail here.</description></item>
    /// <item><description><b>France's XVIe by nuance</b> (`PartySystems.FranceAssembly2022`, E-47 (b)): the Ministry's FE page, ungzipped from its stored body,
    /// its "Sièges par nuance de candidats" table read nuance by nuance - 577.</description></item>
    /// <item><description><b>The worlds that seat them</b>: a world opened on Italy's start (21 Jul 2022) seats the XVIII Camera and France's XVIe at their
    /// tables, with no deviation stated and Italy's declaration said on its brief, its card's caption and the chamber's declared size.</description></item>
    /// </list>
    /// Every stored file is held to its folder's SHA256SUMS first. The comparator's teeth are planted (a table with one seat moved fails it). The epoch is
    /// restored whatever happens (`SimulationManager.EpochScope`).
    /// </summary>
    public static class ItalySeatingDiagnostic
    {
        /// <summary>The dati.camera.it group labels of the XVIII (the label's text before its dates) and the keys they sit as - a group bearing a 2022 list's
        /// name as that list (DECLARED, `PartySystems.ItalyCamera2018`'s rule); MISTO is read component by component below.</summary>
        private static readonly Dictionary<string, string> CameraGroups = new Dictionary<string, string>
        {
            { "MOVIMENTO 5 STELLE", "M5S" }, { "LEGA - SALVINI PREMIER", "Lega" }, { "PARTITO DEMOCRATICO", "PD" },
            { "FORZA ITALIA - BERLUSCONI PRESIDENTE", "FI" }, { "FRATELLI D'ITALIA", "FdI" }, { "LIBERI E UGUALI", "LeU" },
        };

        /// <summary>The mixed group's components by their storia.camera.it slug's head, and the keys they sit as (the same rule).</summary>
        private static readonly (string SlugHead, string Key)[] CameraComponents =
        {
            ("misto-centro-democratico", "PlusE"), ("misto-maie-movimento-associativo-italiani-all-estero-sogno-italia", "MAIE"),
            ("misto-minoranze-linguistiche", "SVP"), ("misto-civica-popolare-ap-psi-area-civica", "CP"), ("misto-noi-l-italia-usei", "NcI"),
        };

        /// <summary>The XIX Senato's groups by their dati.senato.it id (the page's `sgrp` id is the same number).</summary>
        private static readonly Dictionary<int, string> SenatoGroups = new Dictionary<int, string>
        {
            { 85, "FdI" }, { 49, "PD" }, { 33, "Lega" }, { 71, "M5S" }, { 56, "FI" }, { 91, "AzIV" }, { 92, "CdI" }, { 69, "Aut" }, { 9, "Misto" },
        };

        /// <summary>The FE page's nuance labels (Latin-1, as stored) and the keys they sit as - a 2022 nuance with a 2024 nuance's label as that key (DECLARED).</summary>
        private static readonly Dictionary<string, string> FranceNuances = new Dictionary<string, string>
        {
            { "Ensemble ! (Majorité présidentielle)", "ENS" }, { "Nouvelle union populaire écologique et sociale", "NUP" }, { "Rassemblement National", "RN" },
            { "Les Républicains", "LR" }, { "Divers gauche", "DVG" }, { "Divers droite", "DVD" }, { "Régionaliste", "REG" }, { "Divers centre", "DVC" },
            { "Union des Démocrates et des Indépendants", "UDI" }, { "Divers", "DIV" }, { "Droite souverainiste", "DSV" }, { "Ecologistes", "ECO" },
            { "Divers extrême gauche", "DXG" }, { "Parti radical de gauche", "RDG" }, { "Reconquête !", "REC" }, { "Divers extrême droite", "DXD" },
        };

        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== ItalySeatingDiagnostic (SEAT-1, J2 §869): the chambers of record no list table seats, re-derived from the stored pages ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                using (SimulationManager.EpochScope())
                {
                    string data = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ElectionsData"));
                    string reseat = Path.Combine(data, "italy", "raw", "reseat_2022"), camera = Path.Combine(data, "italy", "raw", "camera2018");
                    string senato = Path.Combine(data, "italy", "raw", "senato2022"), france = Path.Combine(data, "france", "raw", "legislatives2022");

                    // ---- every stored file is the file its folder's SHA256SUMS holds ----
                    foreach (string folder in new[] { reseat, camera, senato, france })
                    {
                        List<string> bad = DigestFaults(folder, out int held);
                        Check(held > 0 && bad.Count == 0, F("{0}: {1} file(s) held to SHA256SUMS.txt{2}", Path.GetFileName(folder), held, bad.Count == 0 ? string.Empty : " - " + string.Join("; ", bad)));
                    }

                    // ---- the XVIII Camera ----
                    string adhesions = Path.Combine(reseat, "dati_camera_leg18_adesioni_gruppi.csv");
                    DateTime read = PartySystems.ItalyCamera2018Read;
                    Dictionary<string, int> camera2018 = CameraOn(adhesions, camera, read, out List<string> cameraFaults, out int mistoUnaffiliated);
                    Check(cameraFaults.Count == 0, cameraFaults.Count == 0 ? "the mixed group's components: every one the MISTO page names is read, and every member of the five active on the day is in MISTO that day"
                        : "the mixed group's components: " + string.Join("; ", cameraFaults));
                    string cameraDiff = Diff(camera2018, PartySystems.ItalyCamera2018);
                    Check(cameraDiff == null, F("the XVIII Camera on {0:yyyy-MM-dd}, from the stored pages: {1} - PartySystems.ItalyCamera2018 {2}", read, Line(camera2018), cameraDiff ?? "seat for seat"));
                    Check(camera2018.Values.Sum() == PartySystems.ItalyCamera2018Size && PartySystems.ItalyCamera2018Size == 630 && mistoUnaffiliated == 1,
                        F("630 deputies ({0}), one mixed-group member in no component ({1})", camera2018.Values.Sum(), mistoUnaffiliated));
                    Dictionary<string, int> constituted = CameraGroupsOn(adhesions, new DateTime(2018, 3, 27));
                    Check(constituted.Values.Sum() == 630, F("every deputy declared a group on 27 Mar 2018 - {0} of 630 ({1})", constituted.Values.Sum(), Line(constituted)));
                    // the window the declared day's comment claims: the same table on 7 May, another on the 19th of April and on the 8th of May
                    string may7 = Diff(CameraOn(adhesions, camera, new DateTime(2018, 5, 7), out _, out _), PartySystems.ItalyCamera2018);
                    string apr19 = Diff(CameraOn(adhesions, camera, new DateTime(2018, 4, 19), out _, out _), PartySystems.ItalyCamera2018);
                    string may8 = Diff(CameraOn(adhesions, camera, new DateTime(2018, 5, 8), out _, out _), PartySystems.ItalyCamera2018);
                    Check(may7 == null && apr19 != null && may8 != null, F("the declared day's window: the same table on 7 May 2018 ({0}); not on 19 Apr ({1}) nor on 8 May ({2})",
                        may7 ?? "the same", apr19 ?? "THE SAME", may8 ?? "THE SAME"));
                    Check(PartySystems.SeatsSourced(ElectionVintage.Italy2018) && ReferenceEquals(PartySystems.SeatsAt(ElectionVintage.Italy2018), PartySystems.ItalyCamera2018)
                          && PartySystems.ChamberSizeAt(CountryId.Italy, ElectionVintage.Italy2018) == 630 && PartySystems.ChamberSizeAt(CountryId.Italy, ElectionVintage.Italy2022) == 400,
                        "the XVIII Camera is the Italy2018 vintage's table, on disk, at its own 630; the XIX's stays 400");
                    var italyKeys = new HashSet<string>(PartySystems.For(CountryId.Italy).Select(p => p.Abbrev));
                    Check(PartySystems.ItalyCamera2018.All(r => italyKeys.Contains(r.Abbrev)), "every key of the XVIII Camera's table is a roster key");

                    // ---- the XIX Senato ----
                    Dictionary<int, (string Name, int Total)> page = SenatoPage(Path.Combine(senato, "wayback_senato_grp_20221018194007.html"), out int pageTotal, out bool namesComponents);
                    Dictionary<int, (int Elected, int ForLife)> split = SenatoSplit(Path.Combine(reseat, "dati_senato_leg19_adesioni_gruppi.csv"), Path.Combine(reseat, "dati_senato_leg19_mandati.csv"),
                        new DateTime(2022, 10, 18), out int electedMandates, out int lifeMandates);
                    var senatoFaults = new List<string>();
                    foreach ((string group, string name, string list, int elected, int forLife) in PartySystems.ItalySenato2022)
                    {
                        int id = SenatoGroups.FirstOrDefault(kv => kv.Value == group).Key;
                        if (!page.TryGetValue(id, out (string Name, int Total) onPage)) { senatoFaults.Add(group + " is not on the page"); continue; }
                        if (Squash(onPage.Name) != Squash(name)) { senatoFaults.Add(F("{0}: the page names it \"{1}\", the table \"{2}\"", group, onPage.Name, name)); }
                        split.TryGetValue(id, out (int Elected, int ForLife) bySplit);
                        if (bySplit.Elected != elected || bySplit.ForLife != forLife) { senatoFaults.Add(F("{0}: the open data give {1} elected + {2} for life, the table {3} + {4}", group, bySplit.Elected, bySplit.ForLife, elected, forLife)); }
                        if (onPage.Total != elected + forLife) { senatoFaults.Add(F("{0}: the page's {1} is not {2} + {3}", group, onPage.Total, elected, forLife)); }
                        if (list != null && !italyKeys.Contains(list)) { senatoFaults.Add(group + " sits as '" + list + "', not a roster key"); }
                    }
                    if (page.Count != PartySystems.ItalySenato2022.Length) { senatoFaults.Add(F("the page lists {0} groups, the table {1}", page.Count, PartySystems.ItalySenato2022.Length)); }
                    int electedSum = PartySystems.ItalySenato2022.Sum(r => r.Elected), lifeInGroups = PartySystems.ItalySenato2022.Sum(r => r.ForLife);
                    Check(senatoFaults.Count == 0, senatoFaults.Count == 0 ? F("the XIX Senato on 18 Oct 2022: {0} - each group's page total its open-data elected and for-life senators", string.Join(", ",
                        PartySystems.ItalySenato2022.Select(r => r.Group + " " + r.Elected + (r.ForLife > 0 ? "+" + r.ForLife : string.Empty)))) : "the XIX Senato: " + string.Join("; ", senatoFaults));
                    Check(electedSum == PartySystems.ItalySenato2022Elected && electedMandates == 200 && lifeMandates == PartySystems.ItalySenato2022ForLife && pageTotal == electedSum + lifeInGroups && lifeInGroups == 5,
                        F("200 elected seated ({0}; {1} elected mandates), {2} senators for life ({3} in a group, the page's {4} = {0} + {3}) - none seated", electedSum, electedMandates, lifeMandates, lifeInGroups, pageTotal));
                    Check(!namesComponents && ReferenceEquals(PartySystems.SecondChamberGroupsAt(ElectionVintage.Italy2022), PartySystems.ItalySenato2022) && PartySystems.SecondChamberGroupsAt(ElectionVintage.Italy2018) == null,
                        "the groups page names no component of the Misto group (so it sits whole); the table is the Italy2022 vintage's second chamber, and no other's");

                    // ---- AVS (J2 on E-48): SI's and EV's rows, and their equal-weight mean ----
                    Dictionary<string, Dictionary<string, double>> ches = ChesRows(Path.Combine(data, "positions", "raw", "CHES_2024_final_v2.csv"), "8", "SI", "EV");
                    var fields = new (string Column, Func<PoliticalParty, float> Field)[]
                    {
                        ("lrecon", p => p.LrEcon), ("galtan", p => p.Galtan), ("eu_position", p => p.EuPosition), ("lrgen", p => p.LrGen), ("environment", p => p.Environment),
                        ("regions", p => p.Regions), ("spendvtax", p => p.SpendVsTax), ("immigrate_policy", p => p.ImmigratePolicy), ("deregulation", p => p.Deregulation),
                        ("redistribution", p => p.Redistribution), ("people_v_elite", p => p.PeopleVsElite), ("anti_elite_salience", p => p.AntiEliteSalience),
                        ("civlib_laworder", p => p.CivLibLawOrder), ("nationalism", p => p.Nationalism),
                    };
                    int rowsMatch = 0;
                    foreach (PoliticalParty member in PartySystems.ItalyAvsMembers)
                    {
                        bool all = ches.TryGetValue(member.Abbrev, out Dictionary<string, double> row);
                        foreach ((string column, Func<PoliticalParty, float> field) in fields) { all &= row != null && row.ContainsKey(column) && Math.Abs(Math.Round(row[column], 2) - field(member)) < 0.0005; }
                        if (all) { rowsMatch++; }
                    }
                    PoliticalParty avs = PartySystems.RealRoster(CountryId.Italy).First(p => p.Abbrev == "AVS");
                    bool equalMean = avs.HasPosition && avs.SeedSeats == 12;
                    foreach ((string _, Func<PoliticalParty, float> field) in fields)
                    {
                        equalMean &= Math.Abs(field(avs) - (field(PartySystems.ItalyAvsMembers[0]) + field(PartySystems.ItalyAvsMembers[1])) / 2.0) < 1e-4;
                    }
                    Check(rowsMatch == 2 && equalMean, F("J2 (E-48): SI's and EV's rows are CHES 2024's ({0} of 2, all fourteen fields); AVS sits at their equal-weight mean - lrecon {1:0.000}, galtan {2:0.000}, EU {3:0.000}, nationalism {4:0.000}",
                        rowsMatch, avs.LrEcon, avs.Galtan, avs.EuPosition, avs.Nationalism));
                    // the planted teeth: a three-to-one weighting is another point (a seat weighting fails the check above), and a member missing a field is averaged out
                    PoliticalParty weighted = PartySystems.JointList("W", "planted", 0, null, new[] { (PartySystems.ItalyAvsMembers[0], 3), (PartySystems.ItalyAvsMembers[1], 1) });
                    PoliticalParty gap = PartySystems.EqualWeightList("G", "planted", 0, null, new[] { PartySystems.ItalyAvsMembers[0], new PoliticalParty("X", "planted", float.NaN, 9f, 0) });
                    Check(Math.Abs(weighted.LrEcon - avs.LrEcon) > 0.1 && Math.Abs(gap.LrEcon - PartySystems.ItalyAvsMembers[0].LrEcon) < 1e-6 && Math.Abs(gap.Galtan - (PartySystems.ItalyAvsMembers[0].Galtan + 9f) / 2f) < 1e-4,
                        F("planted: SI three to one moves lrecon to {0:0.000} (the equal mean {1:0.000}); a member without lrecon leaves SI's {2:0.000}", weighted.LrEcon, avs.LrEcon, gap.LrEcon));

                    // ---- France's XVIe by the Ministry's nuances (E-47 (b)) ----
                    Dictionary<string, int> fe = FrancePage(Path.Combine(france, "wayback_interieur_FE_20220705224641.html.gz"), out List<string> unknown, out List<string> zero);
                    var feSeated = fe.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
                    string feDiff = Diff(feSeated, PartySystems.FranceAssembly2022);
                    Check(unknown.Count == 0 && feDiff == null && feSeated.Values.Sum() == 577 && PartySystems.ChamberSizeAt(CountryId.France, ElectionVintage.France2022) == 577,
                        F("the FE page's seats by nuance: {0} - PartySystems.FranceAssembly2022 {1}; {2} seats; the nuances that seated nobody: {3}{4}", Line(feSeated), feDiff ?? "seat for seat",
                            feSeated.Values.Sum(), string.Join(", ", zero), unknown.Count == 0 ? string.Empty : "; LABELS NOT READ: " + string.Join(", ", unknown)));
                    var franceKeys = new HashSet<string>(PartySystems.For(CountryId.France).Select(p => p.Abbrev));
                    Check(PartySystems.SeatsSourced(ElectionVintage.France2022) && ReferenceEquals(PartySystems.SeatsAt(ElectionVintage.France2022), PartySystems.FranceAssembly2022)
                          && PartySystems.FranceAssembly2022.All(r => franceKeys.Contains(r.Abbrev)), "the XVIe is the France2022 vintage's table, on disk, every key a roster key");

                    // ---- the comparator's teeth, planted ----
                    var moved = PartySystems.ItalyCamera2018.ToDictionary(r => r.Abbrev, r => r.Seats);
                    moved["M5S"] -= 1; moved["Misto"] += 1;
                    Check(Diff(moved, PartySystems.ItalyCamera2018) != null && Diff(new Dictionary<string, int>(moved) { ["M5S"] = 222, ["Misto"] = 1 }, PartySystems.ItalyCamera2018) == null,
                        "planted: one seat moved from M5S to Misto fails the comparison, and moved back passes");

                    // ---- the worlds that seat them: Italy's start, 21 Jul 2022 ----
                    DateTime start = WorldClock.StartDate(CountryId.Italy);
                    WorldClock.ApplyStart(CountryId.Italy);
                    World world = WorldFactory.CreateDefault();
                    Country italy = world.GetCountry(CountryId.Italy), fr = world.GetCountry(CountryId.France);
                    string italySeated = Diff(italy.ParliamentSeats.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value), PartySystems.ItalyCamera2018);
                    string franceSeated = Diff(fr.ParliamentSeats.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value), PartySystems.FranceAssembly2022);
                    Check(italySeated == null && franceSeated == null && WorldClock.SeatedVintage(CountryId.Italy, start) == ElectionVintage.Italy2018 && WorldClock.SeatedVintage(CountryId.France, start) == ElectionVintage.France2022,
                        F("a world on Italy's start ({0:yyyy-MM-dd}) seats the XVIII Camera ({1}) and France's XVIe ({2})", start, italySeated ?? "seat for seat", franceSeated ?? "seat for seat"));
                    string deviation = WorldClock.SeatingDeviation(CountryId.Italy, start), declared = WorldClock.SeatingDeclaration(CountryId.Italy, start);
                    Check(deviation == null && WorldClock.SeatingDeviation(CountryId.France, start) == null && declared != null && declared.Contains("DECLARED") && declared.Contains("20 April 2018"),
                        F("no deviation is stated on Italy's start or France's there; Italy's declaration: \"{0}\"", declared));
                    StartPoints.TryPlayable(CountryId.Italy, out StartPoints.StartPoint italyStart);
                    string brief = StartBrief.Text(italyStart);
                    WorldClock.PickerView view = WorldClock.PickerViewOf(CountryId.Italy, start);
                    string positions = PartySystems.PositionDeclaration(CountryId.Italy);
                    Check(brief.Contains("11 parties sit in the Camera, with 1 member of its mixed group in no component (630 seats, the election of 4 March 2018); " + declared + ". " + positions + ".")
                          && positions.Contains("1.795") && !brief.Contains("not yet sourced") && !brief.Contains("E-47"),
                        "Italy's brief says 11 parties and the mixed group's remainder, the Camera's 630 seats of 4 March 2018, its keying rule and AVS's declared point, and no E-47 stand-in: \"" + brief + "\"");
                    // the declared point beside the fit's typed one (L1): the same mean, rounded - never more than 0.005 apart
                    Check(Math.Abs(avs.LrEcon - 1.80) <= 0.0051 && Math.Abs(avs.Galtan - 1.70) <= 0.0051, F("AVS's runtime point ({0:0.000}, {1:0.000}) is the backtests' typed (1.80, 1.70) to rounding, as the brief declares", avs.LrEcon, avs.Galtan));
                    Check(view.Vintage == ElectionVintage.Italy2018 && view.TotalSeats == 630 && view.Deviation == null && PartySystems.SeatingTag(view.Vintage) == "BY ITS GROUPS OF 20 APR 2018"
                          && PartySystems.SeatingTag(ElectionVintage.Italy2022) == null,
                        F("Italy's card reads the XVIII Camera ({0} seats), its caption tagged \"{1}\"", view.TotalSeats, PartySystems.SeatingTag(view.Vintage)));
                    // M2: the declared size is the SEATED vintage's, never the count's - the XVIII's 630 past the XIX's first sitting (nothing re-seats Italy by date),
                    // the 20th Bundestag's 736 in this world, the USA's House by its own dates (the 117th's 434 at its election, the 118th's 435)
                    Country de = world.GetCountry(CountryId.Germany), us = world.GetCountry(CountryId.USA);
                    Check(PartySystems.DeclaredSize(italy, start) == 630 && PartySystems.DeclaredSize(italy, new DateTime(2023, 1, 1)) == 630 && PartySystems.DeclaredSize(de, start) == 736
                          && PartySystems.DeclaredSize(us, start) == 434 && PartySystems.DeclaredSize(us, new DateTime(2023, 2, 1)) == 435,
                        F("the chamber's declared size is the seated vintage's: Italy {0} (and {1} on 1 Jan 2023), Germany {2}, the USA {3} then {4} on 1 Feb 2023",
                            PartySystems.DeclaredSize(italy, start), PartySystems.DeclaredSize(italy, new DateTime(2023, 1, 1)), PartySystems.DeclaredSize(de, start), PartySystems.DeclaredSize(us, start), PartySystems.DeclaredSize(us, new DateTime(2023, 2, 1))));
                    sb.Append("    the government on Italy's start: ").Append(italy.Government == null ? "none" : italy.Government.PmParty + " [" + string.Join("+", italy.Government.Cabinet) + "]"
                        + (SeatedGovernment.IsProvisional(italy) ? " PROVISIONAL" : string.Empty)).Append('\n');
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.GetType().Name).Append(": ").Append(ex.Message).Append('\n'); }

            sb.Append(failures == 0 ? "=== ItalySeatingDiagnostic: the XVIII Camera, the XIX Senato, AVS and the XVIe, from the stored pages ===" : F("=== ItalySeatingDiagnostic: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        // ---------------------------------------------------------------------------------------------------------------------------------------------
        // The readers - pure functions of the stored files.
        // ---------------------------------------------------------------------------------------------------------------------------------------------

        /// <summary>The XVIII Camera on a day by the keys `PartySystems.ItalyCamera2018` names: each group from the adhesions, MISTO split into the five
        /// components active that day (their pages' member dates) and the members in none.</summary>
        internal static Dictionary<string, int> CameraOn(string adhesionsCsv, string componentFolder, DateTime day, out List<string> faults, out int unaffiliated)
        {
            faults = new List<string>();
            Dictionary<string, List<string>> members = CameraMembersOn(adhesionsCsv, day);
            var seats = new Dictionary<string, int>();
            foreach (KeyValuePair<string, List<string>> group in members)
            {
                if (group.Key == "MISTO") { continue; }
                if (!CameraGroups.TryGetValue(group.Key, out string key)) { faults.Add("a group the table does not name: " + group.Key); continue; }
                seats[key] = group.Value.Count;
            }
            var misto = new HashSet<string>(members.TryGetValue("MISTO", out List<string> m) ? m : new List<string>());
            string mistoPage = Path.Combine(componentFolder, "storia_camera_gruppi_misto-misto-23-03-2018.html");
            string mistoHtml = File.ReadAllText(mistoPage, Encoding.UTF8);
            var named = Regex.Matches(mistoHtml, "href=\"\\s*/gruppi/(misto-[a-z0-9-]+)\\s*\"\\s*title=\"accedi alla componente").Cast<Match>().Select(x => x.Groups[1].Value).Distinct().ToList();
            if (named.Count == 0) { faults.Add("the MISTO page names no component"); }
            var placed = new HashSet<string>();
            foreach (string slug in named)
            {
                // a component whose slug dates it outside the day is read from the slug; one whose slug carries no date is read from its stored page
                Match dated = Regex.Match(slug, "-(\\d\\d)-(\\d\\d)-(\\d{4})(?:-(\\d\\d)-(\\d\\d)-(\\d{4}))?$");
                string file = Path.Combine(componentFolder, "storia_camera_gruppi_" + slug + ".html");
                List<string> on = null;
                if (File.Exists(file)) { on = ComponentMembersOn(File.ReadAllText(file, Encoding.UTF8), day); }
                else if (dated.Success)
                {
                    DateTime from = new DateTime(int.Parse(dated.Groups[3].Value), int.Parse(dated.Groups[2].Value), int.Parse(dated.Groups[1].Value));
                    DateTime to = dated.Groups[6].Success ? new DateTime(int.Parse(dated.Groups[6].Value), int.Parse(dated.Groups[5].Value), int.Parse(dated.Groups[4].Value)) : DateTime.MaxValue;
                    if (from <= day && day < to) { faults.Add("a component active on the day is not stored: " + slug); }
                    continue;
                }
                else { faults.Add("an undated component is not stored: " + slug); continue; }
                if (on.Count == 0) { continue; }
                (string head, string key) = CameraComponents.FirstOrDefault(c => slug.StartsWith(c.SlugHead, StringComparison.Ordinal));
                if (key == null) { faults.Add(F("a component active on the day that the table does not name: {0} ({1} members)", slug, on.Count)); continue; }
                foreach (string member in on)
                {
                    if (!misto.Contains(member)) { faults.Add(member + " is in " + key + " but not in MISTO on the day"); }
                    if (!placed.Add(member)) { faults.Add(member + " is in two components"); }
                }
                seats[key] = (seats.TryGetValue(key, out int had) ? had : 0) + on.Count;
            }
            unaffiliated = misto.Count(x => !placed.Contains(x));
            if (unaffiliated > 0) { seats["Misto"] = unaffiliated; }
            return seats;
        }

        /// <summary>Each XVIII group's members on a day ("FIRSTNAME SURNAME"), by the group's label without its dates.</summary>
        private static Dictionary<string, List<string>> CameraMembersOn(string adhesionsCsv, DateTime day)
        {
            string d = day.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            var groups = new Dictionary<string, List<string>>();
            string[] lines = File.ReadAllLines(adhesionsCsv, Encoding.UTF8);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] f = SplitQuoted(lines[i], ',');
                if (f.Length < 7) { continue; }
                if (string.CompareOrdinal(f[3], d) > 0 || (f[4].Length > 0 && string.CompareOrdinal(f[4], d) <= 0)) { continue; }   // in the group from its start, to the day before its end
                string label = Regex.Replace(f[6], "\\s*\\([0-9.\\-]+\\)$", string.Empty);
                if (!groups.TryGetValue(label, out List<string> list)) { groups[label] = list = new List<string>(); }
                list.Add((f[2] + " " + f[1]).ToUpperInvariant());
            }
            return groups;
        }

        private static Dictionary<string, int> CameraGroupsOn(string adhesionsCsv, DateTime day) => CameraMembersOn(adhesionsCsv, day).ToDictionary(kv => kv.Key, kv => kv.Value.Count);

        /// <summary>A component page's members on a day - its "accedi alla scheda di NAME (dd.mm.yyyy-[dd.mm.yyyy])" titles, from the first date to the day
        /// before the second.</summary>
        private static List<string> ComponentMembersOn(string html, DateTime day)
        {
            var on = new List<string>();
            foreach (Match x in Regex.Matches(html, "title=\"accedi alla scheda di\\s+([^\"(:]+?)\\s*\\((\\d\\d)\\.(\\d\\d)\\.(\\d{4})-(?:(\\d\\d)\\.(\\d\\d)\\.(\\d{4}))?\\)\""))
            {
                DateTime from = new DateTime(int.Parse(x.Groups[4].Value), int.Parse(x.Groups[3].Value), int.Parse(x.Groups[2].Value));
                DateTime to = x.Groups[7].Success ? new DateTime(int.Parse(x.Groups[7].Value), int.Parse(x.Groups[6].Value), int.Parse(x.Groups[5].Value)) : DateTime.MaxValue;
                string name = x.Groups[1].Value.Trim().ToUpperInvariant();
                if (from <= day && day < to && !on.Contains(name)) { on.Add(name); }
            }
            return on;
        }

        /// <summary>The Senato's groups page: each group's name and total by its `sgrp` id, the page's total, and whether it names a component.</summary>
        internal static Dictionary<int, (string Name, int Total)> SenatoPage(string path, out int total, out bool namesComponents)
        {
            string html = File.ReadAllText(path, Encoding.UTF8);
            var groups = new Dictionary<int, (string Name, int Total)>();
            foreach (Match x in Regex.Matches(html, "tipodoc=sgrp&amp;id=(\\d+)&amp;leg=19\">([^<]+)</a>\\s*</td>\\s*<td[^>]*>\\s*(\\d+)\\s*</td>"))
            {
                groups[int.Parse(x.Groups[1].Value)] = (System.Net.WebUtility.HtmlDecode(x.Groups[2].Value).Trim(), int.Parse(x.Groups[3].Value));
            }
            Match t = Regex.Match(html, "Totale(?:</strong>)?\\s*</t[dh]>\\s*<t[dh][^>]*>\\s*(\\d+)");
            total = t.Success ? int.Parse(t.Groups[1].Value) : -1;
            namesComponents = html.IndexOf("omponent", StringComparison.OrdinalIgnoreCase) >= 0;
            return groups;
        }

        /// <summary>The XIX Senato on a day by group id: its elected senators and its senators for life (each senator once per group, whatever office).</summary>
        internal static Dictionary<int, (int Elected, int ForLife)> SenatoSplit(string adhesionsCsv, string mandatesCsv, DateTime day, out int electedMandates, out int lifeMandates)
        {
            string d = day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            var kind = new Dictionary<string, bool>();   // senator -> elected
            string[] mandates = File.ReadAllLines(mandatesCsv, Encoding.UTF8);
            electedMandates = 0; lifeMandates = 0;
            for (int i = 1; i < mandates.Length; i++)
            {
                string[] f = SplitQuoted(mandates[i], ',');
                if (f.Length < 8 || f[5] != "2022-10-13") { continue; }   // the mandates from the first sitting
                bool elected = f[7] == "elettivo";
                kind[f[0]] = elected;
                if (elected) { electedMandates++; } else { lifeMandates++; }
            }
            var seen = new HashSet<string>();
            var split = new Dictionary<int, (int Elected, int ForLife)>();
            string[] lines = File.ReadAllLines(adhesionsCsv, Encoding.UTF8);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] f = SplitQuoted(lines[i], ',');
                if (f.Length < 8) { continue; }
                if (string.CompareOrdinal(f[4], d) > 0 || (f[5].Length > 0 && string.CompareOrdinal(f[5], d) < 0)) { continue; }
                Match id = Regex.Match(f[7], "/gruppo/(\\d+)$");
                if (!id.Success || !seen.Add(f[0] + "|" + id.Groups[1].Value) || !kind.TryGetValue(f[0], out bool elected)) { continue; }
                int g = int.Parse(id.Groups[1].Value);
                split.TryGetValue(g, out (int Elected, int ForLife) was);
                split[g] = elected ? (was.Elected + 1, was.ForLife) : (was.Elected, was.ForLife + 1);
            }
            return split;
        }

        /// <summary>The FE page's "Sièges par nuance de candidats" table - the first table after that heading - by key, from the stored gzip body (Latin-1).</summary>
        internal static Dictionary<string, int> FrancePage(string gzPath, out List<string> unknown, out List<string> zero)
        {
            unknown = new List<string>(); zero = new List<string>();
            string html;
            using (var gz = new GZipStream(File.OpenRead(gzPath), CompressionMode.Decompress))
            using (var buffer = new MemoryStream())
            {
                gz.CopyTo(buffer);
                byte[] bytes = buffer.ToArray();
                var chars = new char[bytes.Length];
                for (int i = 0; i < bytes.Length; i++) { chars[i] = (char)bytes[i]; }   // Latin-1: each byte is its code point
                html = new string(chars);
            }
            int head = html.IndexOf("par nuance de candidats</h3>", StringComparison.Ordinal);
            int end = head < 0 ? -1 : html.IndexOf("</tbody>", head, StringComparison.Ordinal);
            var seats = new Dictionary<string, int>();
            if (head < 0 || end < 0) { unknown.Add("the seats table is not on the page"); return seats; }
            foreach (Match x in Regex.Matches(html.Substring(head, end - head), "<td style=\"text-align:left\">([^<]+)</td>\\s*<td style=\"text-align:center\">(\\d+)</td>"))
            {
                string label = x.Groups[1].Value.Trim();
                int n = int.Parse(x.Groups[2].Value);
                if (!FranceNuances.TryGetValue(label, out string key)) { unknown.Add(label); continue; }
                seats[key] = n;
                if (n == 0) { zero.Add(key); }
            }
            return seats;
        }

        /// <summary>CHES 2024's rows for a country's named parties, every numeric column by name.</summary>
        private static Dictionary<string, Dictionary<string, double>> ChesRows(string path, string country, params string[] parties)
        {
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            string[] head = SplitQuoted(lines[0], ',');
            var result = new Dictionary<string, Dictionary<string, double>>();
            for (int i = 1; i < lines.Length; i++)
            {
                string[] f = SplitQuoted(lines[i], ',');
                if (f.Length != head.Length || f[0] != country || Array.IndexOf(parties, f[2]) < 0) { continue; }
                var row = new Dictionary<string, double>();
                for (int c = 0; c < head.Length; c++) { if (double.TryParse(f[c], NumberStyles.Float, CultureInfo.InvariantCulture, out double v)) { row[head[c]] = v; } }
                result[f[2]] = row;
            }
            return result;
        }

        /// <summary>The files a folder's SHA256SUMS.txt holds whose bytes differ (or are missing); <paramref name="held"/> the lines read.</summary>
        internal static List<string> DigestFaults(string folder, out int held)
        {
            var faults = new List<string>();
            held = 0;
            string sums = Path.Combine(folder, "SHA256SUMS.txt");
            if (!File.Exists(sums)) { faults.Add("no SHA256SUMS.txt"); return faults; }
            foreach (string line in File.ReadAllLines(sums))
            {
                Match x = Regex.Match(line, "^([0-9a-f]{64}) \\*?(.+)$");
                if (!x.Success) { continue; }
                held++;
                string file = Path.Combine(folder, x.Groups[2].Value.Trim());
                if (!File.Exists(file)) { faults.Add(x.Groups[2].Value + " missing"); continue; }
                using (var sha = System.Security.Cryptography.SHA256.Create())
                {
                    string got = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(file))).Replace("-", string.Empty).ToLowerInvariant();
                    if (got != x.Groups[1].Value) { faults.Add(x.Groups[2].Value + " differs"); }
                }
            }
            return faults;
        }

        /// <summary>Null where the two tables seat the same keys at the same seats (zero rows ignored), else the differences.</summary>
        internal static string Diff(IReadOnlyDictionary<string, int> got, IReadOnlyList<(string Abbrev, int Seats)> want)
        {
            var w = new Dictionary<string, int>();
            foreach ((string k, int n) in want) { if (n != 0) { w[k] = n; } }
            var parts = new List<string>();
            foreach (string k in got.Keys.Union(w.Keys).OrderBy(x => x, StringComparer.Ordinal))
            {
                int a = got.TryGetValue(k, out int ga) ? ga : 0, b = w.TryGetValue(k, out int wb) ? wb : 0;
                if (a != b) { parts.Add(F("{0} {1} (table {2})", k, a, b)); }
            }
            return parts.Count == 0 ? null : "DIFFERS: " + string.Join(", ", parts);
        }

        private static string Line(IReadOnlyDictionary<string, int> seats) =>
            string.Join(", ", seats.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => kv.Key + " " + kv.Value));

        private static string Squash(string s) => Regex.Replace(s ?? string.Empty, "\\s+", " ").Trim();

        private static string[] SplitQuoted(string line, char sep)
        {
            var fields = new List<string>();
            var cur = new StringBuilder();
            bool quoted = false;
            foreach (char ch in line.TrimEnd('\r'))
            {
                if (ch == '"') { quoted = !quoted; continue; }
                if (ch == sep && !quoted) { fields.Add(cur.ToString()); cur.Clear(); continue; }
                cur.Append(ch);
            }
            fields.Add(cur.ToString());
            return fields.ToArray();
        }

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
    }
}
