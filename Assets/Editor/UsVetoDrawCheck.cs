using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using PoliSim.Data;
using PoliSim.Elections;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace PoliSim.EditorTools
{
    /// <summary>
    /// PS-6, R-US17's rule (c) - THE FIRST RUN'S INSTRUMENT (`COMPLETED.md` §834). It runs the rule the fenced block in
    /// `docs/specs/USA_STAGE_PLAN.md` states (preregistered §832, amended §833 by Elias's answer on S2's reading; Elias's rulings H1-H3) on the US veto
    /// record (`ElectionsData/usa/us_veto_measures.csv`, `Tools/us_veto_prep.pl`), calling the pure <see cref="UsVeto"/> on chambers built from the CSV,
    /// so the backtest runs the game's arithmetic: the four cells of recorded crossing (HP, HO, SP, SO) and their thin-cell pooling; per measure of the
    /// veto path, under premise (B) and leave-one-out, the exact chance each chamber passes it, that it is sent, named by rule (a) and overridden; the
    /// totals, the precision against B1's 45 of 166; the record bootstrap (BOOT) and the Clopper-Pearson intervals (CP) every fitted figure prints with;
    /// the seeded realizations (DRAWN); INFLUENCE; the eighteen declared readings; the identities against US-10's record (§802); THE DECISION and its
    /// decisive digest; the ruling record and its mirror line in the plan.
    ///
    /// <para>Pinned in the cheap bar and the documents bar: `docs/generated/US_VETO_DRAW.md` and the catalog
    /// (`Assets/Editor/Generated/UsVetoDefections.cs`) are what <see cref="Write"/> writes - a byte of difference fails. The block's digest must be the
    /// last of <see cref="RuleDigests"/>, read from the working copy and the index blob alike; the CSV and the prep tool must match their pins. US-10's
    /// tool, document and check are not touched.</para>
    /// </summary>
    public static class UsVetoDrawCheck
    {
        // ------------------------------------------------------------------ the block's digests, the pins and the ruling record (THE RULING RECORD AND THE BARS)
        /// <summary>The block's SHA-256 digests: the preregistered one first, then one per amendment naming Elias's ruling; the block must equal the last.</summary>
        private static readonly (string Digest, string Why)[] RuleDigests =
        {
            ("577456a92d1ff7380bd44374b0c6fd02738452f09dfd53396f5aa988887e877a", "preregistered (§832)"),
            ("24de40a77bbaddfaccb98df91a648206fcd5c47bf1b5b6a658caba7270058c9c", "AMENDED (Elias's answer on S2's reading, 2026-10-16, §833): 577456a9 -> 24de40a7"),
        };

        /// <summary>THE PINS, filled at the preregistering commit (§832) by hashing bytes: the record's SHA-256, the prep tool's git blob.</summary>
        private const string SourcePin = "505c3fa19ec816d280f8df29c00165d9987ee1b72cbc5a154164e1282e845dec";
        private const string PrepPin = "358eba1c4a3f1dd0af7e65dc91feaae20ba77558";

        /// <summary>THE RULING RECORD: NONE (before the instrument), INSTALLED-PENDING-E4, INSTALLED, ASKED or RULED. NONE fails every run, so the
        /// first-run commit registers the outcome and its decisive digest.</summary>
        private const string RulingState = "INSTALLED-PENDING-E4";
        private const string RulingSection = "§834";
        /// <summary>INSTALLED only: the COMPLETED.md section that records the reply to Elias carrying the rates (E4).</summary>
        private const string RulingE4Section = "";
        private const string RulingOutcome = "INSTALL";
        private const string RulingDecisiveDigest = "12da26b99c3506c38bbd17e5ecf5b0ab38f50e00a503faa53418bc3123842866";
        /// <summary>RULED only: the ruling's words and the option it names - one THE STOPS' OPTIONS lists, or (c) amended by name ("(c) amended by " and
        /// the amendment's name). The mirror shows the option.</summary>
        private const string RulingRuled = "", RulingOption = "";
        /// <summary>A CHANGED OUTCOME is registered ASKED with the old and new outcomes and digests: the old ones here (empty for the first registration).</summary>
        private const string RulingPriorOutcome = "", RulingPriorDigest = "";
        private static readonly string[] BuildableOptions =
        {
            "(c) as measured", "pool all four cells into one", "pool per chamber across roles (R11)", "source more roll calls",
            "R1, reading (A), as play's premise", "source the Senate's cloture and failed votes and run again", "keep the blocs with no draw",
            "(c) with R12's drawn veto",
        };
        private static bool Buildable(string option) => Array.IndexOf(BuildableOptions, option) >= 0 || (option.StartsWith("(c) amended by ", StringComparison.Ordinal) && option.Length > "(c) amended by ".Length);

        // ------------------------------------------------------------------ paths
        private const string CsvRelative = "ElectionsData/usa/us_veto_measures.csv";
        private const string PrepRelative = "Tools/us_veto_prep.pl";
        private const string PlanRelative = "docs/specs/USA_STAGE_PLAN.md";
        private const string DocRelative = "docs/generated/US_VETO_DRAW.md";
        private const string BacktestDocRelative = "docs/generated/US_VETO_BACKTEST.md";
        private const string PolandCsvRelative = "ElectionsData/poland/third_readings_term10.csv";
        private const string CatalogRelative = "Assets/Editor/Generated/UsVetoDefections.cs";
        private const string BlockBegin = "<!-- R-US17 RULE BEGIN -->\n", BlockEnd = "<!-- R-US17 RULE END -->";
        private static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        // ------------------------------------------------------------------ the entry points
        public static void Run()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsVetoDrawCheck (PS-6, R-US17's rule (c), §834): the draw of defectors on the US veto path, measured on the record ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                Product p = MeasureRun(sb, Check);
                // byte for byte, no normalization ("fails on any byte of difference"); both paths are -text in .gitattributes, so a checkout keeps LF
                byte[] docOnDisk = BytesIfExists(DocRelative), catOnDisk = BytesIfExists(CatalogRelative);
                byte[] docWant = new UTF8Encoding(false).GetBytes(p.Document), catWant = new UTF8Encoding(false).GetBytes(p.Catalog);
                bool docSame = docOnDisk != null && docOnDisk.SequenceEqual(docWant), catSame = catOnDisk != null && catOnDisk.SequenceEqual(catWant);
                Check(docSame, docOnDisk == null ? F("the document {0} does not exist - write it: -executeMethod PoliSim.EditorTools.UsVetoDrawCheck.Write", DocRelative)
                    : docSame ? F("the document {0} is what this run writes, byte for byte - {1} lines", DocRelative, p.Document.Count(c => c == '\n'))
                    : F("the document {0} is STALE ({1}) - regenerate: -executeMethod PoliSim.EditorTools.UsVetoDrawCheck.Write", DocRelative, ByteDifference(docOnDisk, p.Document)));
                Check(catSame, catOnDisk == null ? F("the catalog {0} does not exist - write it: -executeMethod PoliSim.EditorTools.UsVetoDrawCheck.Write", CatalogRelative)
                    : catSame ? F("the catalog {0} is what this run generates, byte for byte - the lists the runtime indexes", CatalogRelative)
                    : F("the catalog {0} differs from the generated one ({1}) - an edited catalog list, or a stale one: regenerate with Write", CatalogRelative, ByteDifference(catOnDisk, p.Catalog)));
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.Message).Append('\n').Append(ex.StackTrace).Append('\n'); }
            sb.Append(failures == 0 ? "=== UsVetoDrawCheck: the rule's run holds ===" : F("=== UsVetoDrawCheck: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        /// <summary>Writes the document and the catalog from this run - refused while any other check fails (the ruling record NONE included).</summary>
        public static void Write()
        {
            CheckExit.ArmLogFold();
            var sb = new StringBuilder("=== UsVetoDrawCheck.Write (§834): docs/generated/US_VETO_DRAW.md and the catalog ===\n");
            int failures = 0;
            void Check(bool ok, string what) { if (!ok) { failures++; } sb.Append(ok ? "    ok        " : "    FAIL      ").Append(what).Append('\n'); }
            try
            {
                Product p = MeasureRun(sb, Check);
                if (failures > 0) { Check(false, "a check above failed - nothing is written"); }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(Root, CatalogRelative)));
                    File.WriteAllText(Path.Combine(Root, DocRelative), p.Document, new UTF8Encoding(false));
                    File.WriteAllText(Path.Combine(Root, CatalogRelative), p.Catalog, new UTF8Encoding(false));
                    Check(true, F("written: {0} ({1} lines) and {2}", DocRelative, p.Document.Count(c => c == '\n'), CatalogRelative));
                }
            }
            catch (Exception ex) { failures++; sb.Append("    FAIL      threw: ").Append(ex.Message).Append('\n').Append(ex.StackTrace).Append('\n'); }
            sb.Append(failures == 0 ? "=== UsVetoDrawCheck.Write: written ===" : F("=== UsVetoDrawCheck.Write: {0} FAILED ===", failures));
            if (failures == 0) { Debug.Log(sb.ToString()); } else { Debug.LogError(sb.ToString()); }
            CheckExit.Finish(failures == 0 ? 0 : 1);
        }

        private sealed class Product { public string Document, Catalog; }

        private static byte[] BytesIfExists(string relative) { string p = Path.Combine(Root, relative); return File.Exists(p) ? File.ReadAllBytes(p) : null; }
        /// <summary>Where the bytes on disk part from the generated text: a byte order mark or another encoding by name, else the first differing line
        /// (the bytes decoded as UTF-8 with no preamble detection, so a BOM is never hidden).</summary>
        private static string ByteDifference(byte[] onDisk, string generated)
        {
            if (onDisk.Length >= 3 && onDisk[0] == 0xEF && onDisk[1] == 0xBB && onDisk[2] == 0xBF) { return "a UTF-8 byte order mark on disk"; }
            if (onDisk.Length >= 2 && ((onDisk[0] == 0xFF && onDisk[1] == 0xFE) || (onDisk[0] == 0xFE && onDisk[1] == 0xFF))) { return "UTF-16 on disk"; }
            return FirstDifference(new UTF8Encoding(false).GetString(onDisk), generated);
        }
        private static string Lf(string s) => s.Replace("\r\n", "\n");
        private static string FirstDifference(string a, string b)
        {
            string[] x = a.Split('\n'), y = b.Split('\n');
            for (int i = 0; i < Math.Max(x.Length, y.Length); i++)
            {
                string l = i < x.Length ? x[i] : "<none>", r = i < y.Length ? y[i] : "<none>";
                if (l != r) { return F("line {0}: on disk '{1}', generated '{2}'", i + 1, Clip(l.Replace("\r", "<CR>")), Clip(r.Replace("\r", "<CR>"))); }
            }
            return "no line differs";
        }
        private static string Clip(string s) => s.Length > 90 ? s.Substring(0, 90) + "..." : s;

        // ================================================================== THE RECORD (THE TERMS)
        private sealed class Split
        {
            public int Yea, Nay, Present, NotVoting;
            public int Members => Yea + Nay + Present + NotVoting;
            public int Voting => Yea + Nay + Present;
        }

        private sealed class Final
        {
            public string Text, Date, Question;
            public bool Roll;
            public int No;
            public Split R, D, I;   // null where the yea is "-" (no split)
            public Split Of(string party) => party == "R" ? R : party == "D" ? D : I;
        }

        private sealed class Measure
        {
            public int Row, Congress, Counted = -1;
            public string Name, Id, Kind, Outcome, DecisionDate, President, PP, OP, Title, SameDay, CaucusText;
            public Final House, Senate;
            public bool Vetoed, Unpredictable, Expedited, Reconciliation, Appropriation, DebtLimit;
            public char Origin;
            public List<string> BudgetMatches = new List<string>();
            public List<(string Caucus, int N)> CaucusIn = new List<(string, int)>();
            public List<string> CaucusOut = new List<string>();
            public Dictionary<string, string> Raw;
            public Final F(char ch) => ch == 'H' ? House : Senate;
            public IEnumerable<char> RollChambers() { if (House.Roll) { yield return 'H'; } if (Senate.Roll) { yield return 'S'; } }
            public string Label => UsVetoDrawCheck.F("{0} ({1}th)", Name, Congress);
        }

        private sealed class RollCall
        {
            public string Id;
            public char Ch;
            public int Congress, No, Index;
            public string Date;
            public Measure Owner;
            public List<Measure> Measures = new List<Measure>();
            public Final Final => Owner.F(Ch);
        }

        private sealed class SeatsOf { public int R, D, Sworn; public string Date; public int Of(string party) => party == "R" ? R : D; }

        private static readonly HashSet<string> ExpeditedKinds = new HashSet<string>(StringComparer.Ordinal) { "CRA disapproval", "arms-sale disapproval", "War Powers", "national emergency", "D.C. disapproval" };
        private static readonly string[] ReconciliationTests = { "PROVIDE FOR RECONCILIATION", "PROVIDING FOR RECONCILIATION", "RECONCILIATION PURSUANT TO", "PURSUANT TO SECTION 2002 OF THE CONCURRENT RESOLUTION" };
        private static readonly string[] AppropriationTests = { "MAKING APPROPRIATIONS", "MAKING CONTINUING APPROPRIATIONS", "MAKING FURTHER CONTINUING APPROPRIATIONS", "CONTINUING APPROPRIATIONS", "APPROPRIATIONS ACT" };
        private static readonly string[] DebtLimitTests = { "DEBT LIMIT", "FISCAL RESPONSIBILITY ACT", "BIPARTISAN BUDGET ACT" };
        private static readonly string[] Presidents = { "Donald J. Trump (first term)", "Joseph R. Biden Jr.", "Donald J. Trump (second term)" };
        private static readonly string[] UnpredictableAsserted = { "119|H.R. 131", "119|H.R. 504" };

        /// <summary>The record, read once per run: every row, the counted measures, the roll calls with their owners, the seats of each Congress.</summary>
        private sealed class Record
        {
            public string SourceDigest;
            public List<Measure> All = new List<Measure>();
            public List<Measure> Counted = new List<Measure>();
            public List<Measure> UnpredictableRows = new List<Measure>();
            public Dictionary<string, RollCall> Rolls = new Dictionary<string, RollCall>(StringComparer.Ordinal);
            public List<RollCall> RollOrder = new List<RollCall>();   // first appearance, counted CSV order, House before Senate
            public Dictionary<string, SeatsOf> Seats = new Dictionary<string, SeatsOf>(StringComparer.Ordinal);   // "<congress> house|senate"
            public List<string> Defects = new List<string>();
            public List<string> KindsPrinted = new List<string>();
            public List<string> NearMisses = new List<string>();
            public List<string> CaucusIgnored = new List<string>();
            public SeatsOf SeatsFor(int congress, char ch) => Seats[congress.ToString(CultureInfo.InvariantCulture) + (ch == 'H' ? " house" : " senate")];
            public string RollId(Measure m, char ch) { Final f = m.F(ch); return f.Roll ? RollIdOf(ch, m.Congress, f.Date, f.No) : null; }
        }

        private static string RollIdOf(char ch, int congress, string date, int no) => F("{0}|{1}|{2}|{3}", ch, congress, date, no);

        private static Record ReadRecord(byte[] csvBytes)
        {
            var rec = new Record { SourceDigest = Sha256Hex(csvBytes) };
            List<Dictionary<string, string>> rows = ReadCsv(Encoding.UTF8.GetString(csvBytes), CsvRelative);
            int row = 0;
            foreach (Dictionary<string, string> x in rows)
            {
                var m = new Measure
                {
                    Row = row++, Raw = x, Congress = int.Parse(x["congress"], CultureInfo.InvariantCulture), Name = x["measure"], Kind = x["kind"], Outcome = x["outcome"],
                    DecisionDate = x["decision_date"], President = x["president"], PP = x["president_party"], Title = x["title"], SameDay = x["same_day"], CaucusText = x["senate_I_caucus"],
                };
                m.Id = m.Congress.ToString(CultureInfo.InvariantCulture) + "|" + m.Name;
                m.OP = m.PP == "R" ? "D" : m.PP == "D" ? "R" : "?";
                m.House = FinalOf(x, "house"); m.Senate = FinalOf(x, "senate");
                if (m.Name.StartsWith("H.", StringComparison.Ordinal)) { m.Origin = 'H'; }
                else if (m.Name.StartsWith("S.", StringComparison.Ordinal)) { m.Origin = 'S'; }
                else { rec.Defects.Add(F("DEFECT: {0}'s measure begins neither \"H.\" nor \"S.\" - no ORIGIN", m.Id)); }
                if (m.Outcome.Contains("veto")) { m.Vetoed = true; }
                else if (m.Outcome != "signed" && m.Outcome != "law without signature") { rec.Defects.Add(F("DEFECT: {0}'s outcome '{1}' is neither VETOED nor NOT VETOED", m.Id, m.Outcome)); }
                m.Expedited = ExpeditedKinds.Contains(m.Kind);
                string upper = m.Title.ToUpperInvariant();
                foreach (string t in ReconciliationTests) { if (upper.Contains(t)) { m.Reconciliation = true; m.BudgetMatches.Add("RECONCILIATION \"" + t + "\""); } }
                foreach (string t in AppropriationTests) { if (upper.Contains(t)) { m.Appropriation = true; m.BudgetMatches.Add("APPROPRIATION \"" + t + "\""); } }
                foreach (string t in DebtLimitTests) { if (upper.Contains(t)) { m.DebtLimit = true; m.BudgetMatches.Add("DEBT LIMIT \"" + t + "\""); } }
                if (m.CaucusText != "-")
                {
                    foreach (string kv in m.CaucusText.Split(' '))
                    {
                        string[] p = kv.Split(':');
                        if (p.Length == 2 && (p[0] == "R" || p[0] == "D") && int.TryParse(p[1], NumberStyles.None, CultureInfo.InvariantCulture, out int n)) { m.CaucusIn.Add((p[0], n)); }
                        else { m.CaucusOut.Add(kv); }
                    }
                }
                m.Unpredictable = m.Vetoed && !m.House.Roll && !m.Senate.Roll;
                rec.All.Add(m);
                if (m.Unpredictable) { rec.UnpredictableRows.Add(m); continue; }
                m.Counted = rec.Counted.Count;
                rec.Counted.Add(m);
            }

            // the counted measures: the president's party, the roll calls and their owners, the kinds printed, the budget class's near misses
            foreach (Measure m in rec.Counted)
            {
                if (m.PP != "R" && m.PP != "D") { rec.Defects.Add(F("DEFECT: counted {0}'s president's party '{1}' is neither R nor D", m.Id, m.PP)); }
                if (!m.Expedited && m.Kind != "bill" && m.Kind != "joint resolution") { rec.KindsPrinted.Add(F("{0}: kind '{1}' - RULE XXII", m.Id, m.Kind)); }
                string upper = m.Title.ToUpperInvariant();
                if (m.BudgetMatches.Count == 0 && (upper.Contains("APPROPRIATION") || upper.Contains("RECONCILIATION") || upper.Contains("BUDGET"))) { rec.NearMisses.Add(F("{0}: \"{1}\"", m.Id, m.Title)); }
                if (m.Senate.Roll) { foreach (string c in m.CaucusOut) { rec.CaucusIgnored.Add(F("{0}: caucus entry '{1}' is neither R nor D - outside every bloc", m.Id, c)); } }
                foreach (char ch in m.RollChambers())
                {
                    Final f = m.F(ch);
                    string id = RollIdOf(ch, m.Congress, f.Date, f.No);
                    if (string.CompareOrdinal(f.Date, "2017-01-03") < 0) { rec.Defects.Add(F("DEFECT: {0}'s roll call {1} is dated before 2017-01-03 (H1, \"since 2017\")", m.Id, id)); }
                    if (!rec.Rolls.TryGetValue(id, out RollCall rc))
                    {
                        rc = new RollCall { Id = id, Ch = ch, Congress = m.Congress, No = f.No, Date = f.Date, Owner = m, Index = rec.RollOrder.Count };
                        rec.Rolls[id] = rc; rec.RollOrder.Add(rc);
                    }
                    else if (!SameSplits(rc.Final, f)) { rec.Defects.Add(F("DEFECT: roll call {0} is final for {1} and {2} with different party splits - ONE roll call everywhere", id, rc.Owner.Id, m.Id)); }
                    rc.Measures.Add(m);
                }
            }

            // SEATS OF A CONGRESS - literally us_veto_backtest.pl's %seatsOf over the counted rows: ordered by house_final_date (ordinal, stable), the
            // entry replaced iff none is stored or the row's final date is at or after the stored date, and the stored date then overwritten IN EVERY CASE
            foreach (Measure m in rec.Counted.OrderBy(x => x.House.Date, StringComparer.Ordinal))
            {
                foreach (char ch in new[] { 'H', 'S' })
                {
                    Final f = m.F(ch);
                    if (!f.Roll) { continue; }
                    int r = MembersOf(f.R), d = MembersOf(f.D), sworn = r + d + MembersOf(f.I);
                    if (ch == 'S') { foreach ((string c, int n) in m.CaucusIn) { if (c == "R") { r += n; } else { d += n; } } }
                    string key = m.Congress.ToString(CultureInfo.InvariantCulture) + (ch == 'H' ? " house" : " senate");
                    if (!rec.Seats.TryGetValue(key, out SeatsOf s) || string.CompareOrdinal(f.Date, s.Date ?? string.Empty) >= 0) { rec.Seats[key] = s = new SeatsOf { R = r, D = d, Sworn = sworn }; }
                    s.Date = f.Date;
                }
            }
            return rec;
        }

        private static int MembersOf(Split s) => s == null ? 0 : s.Members;
        private static bool SameSplits(Final a, Final b) => SameSplit(a.R, b.R) && SameSplit(a.D, b.D) && SameSplit(a.I, b.I);
        private static bool SameSplit(Split a, Split b) => (a == null && b == null) || (a != null && b != null && a.Yea == b.Yea && a.Nay == b.Nay && a.Present == b.Present && a.NotVoting == b.NotVoting);

        private static Final FinalOf(Dictionary<string, string> x, string ch)
        {
            var f = new Final { Text = x[ch + "_final"], Date = x[ch + "_final_date"], Question = x[ch + "_question"] };
            if (f.Text.StartsWith("roll ", StringComparison.Ordinal))
            {
                f.Roll = true;
                f.No = int.Parse(f.Text.Substring(5), NumberStyles.None, CultureInfo.InvariantCulture);
            }
            Split SplitOf(string p)
            {
                if (x[ch + "_" + p + "_yea"] == "-") { return null; }
                return new Split
                {
                    Yea = int.Parse(x[ch + "_" + p + "_yea"], CultureInfo.InvariantCulture), Nay = int.Parse(x[ch + "_" + p + "_nay"], CultureInfo.InvariantCulture),
                    Present = int.Parse(x[ch + "_" + p + "_present"], CultureInfo.InvariantCulture), NotVoting = int.Parse(x[ch + "_" + p + "_not_voting"], CultureInfo.InvariantCulture),
                };
            }
            f.R = SplitOf("R"); f.D = SplitOf("D"); f.I = SplitOf("I");
            return f;
        }

        /// <summary>RFC 4180 as us_veto_backtest.pl's read_csv: quoted fields, doubled quotes, CR dropped, a quote inside an unquoted field and a field
        /// running over a line refused, lines whose first field begins "#" skipped, the header by name, every row of the header's field count.</summary>
        private static List<Dictionary<string, string>> ReadCsv(string text, string path)
        {
            var records = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool inq = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inq)
                {
                    if (c == '"') { if (i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; } else { inq = false; } continue; }
                    if (c == '\n') { throw new InvalidDataException(path + ": a field runs over a line"); }
                    field.Append(c); continue;
                }
                if (c == '"') { if (field.Length > 0) { throw new InvalidDataException(path + ": a quote inside an unquoted field"); } inq = true; }
                else if (c == ',') { row.Add(field.ToString()); field.Clear(); }
                else if (c == '\r') { }
                else if (c == '\n') { row.Add(field.ToString()); field.Clear(); records.Add(row); row = new List<string>(); }
                else { field.Append(c); }
            }
            if (field.Length > 0 || row.Count > 0) { row.Add(field.ToString()); records.Add(row); }
            if (inq) { throw new InvalidDataException(path + ": an unterminated quote"); }
            records = records.Where(r => !(r.Count == 1 && r[0].Length == 0) && !r[0].StartsWith("#", StringComparison.Ordinal)).ToList();
            List<string> head = records[0];
            var rows = new List<Dictionary<string, string>>();
            foreach (List<string> r in records.Skip(1))
            {
                if (r.Count != head.Count) { throw new InvalidDataException(F("{0}: a row of {1} fields against {2}", path, r.Count, head.Count)); }
                var d = new Dictionary<string, string>(StringComparer.Ordinal);
                for (int c = 0; c < head.Count; c++) { d[head[c]] = r[c]; }
                rows.Add(d);
            }
            return rows;
        }

        // ================================================================== EXACTNESS: rationals on BigInteger, and the number format
        /// <summary>A rational on BigInteger, always reduced, its denominator positive. Equal is never "below".</summary>
        private readonly struct Q : IComparable<Q>
        {
            public readonly BigInteger N, D;
            public Q(BigInteger n, BigInteger d)
            {
                if (d.IsZero) { throw new DivideByZeroException("a rational with denominator 0"); }
                if (d.Sign < 0) { n = -n; d = -d; }
                BigInteger g = BigInteger.GreatestCommonDivisor(BigInteger.Abs(n), d);
                if (!g.IsZero && !g.IsOne) { n /= g; d /= g; }
                if (n.IsZero) { d = BigInteger.One; }
                N = n; D = d;
            }
            public static Q Zero => new Q(0, 1);
            public static Q One => new Q(1, 1);
            public static Q Of(long n) => new Q(n, 1);
            public static Q Of(UsVeto.Odds o) => new Q(o.Passing, o.Total);
            public bool IsZero => N.IsZero;
            public static Q operator +(Q a, Q b) => new Q(a.N * b.D + b.N * a.D, a.D * b.D);
            public static Q operator -(Q a, Q b) => new Q(a.N * b.D - b.N * a.D, a.D * b.D);
            public static Q operator *(Q a, Q b) => new Q(a.N * b.N, a.D * b.D);
            public static Q operator /(Q a, Q b) => new Q(a.N * b.D, a.D * b.N);
            public int CompareTo(Q o) => (N * o.D).CompareTo(o.N * D);
            public static bool operator <(Q a, Q b) => a.CompareTo(b) < 0;
            public static bool operator >(Q a, Q b) => a.CompareTo(b) > 0;
            public static bool operator <=(Q a, Q b) => a.CompareTo(b) <= 0;
            public static bool operator >=(Q a, Q b) => a.CompareTo(b) >= 0;
            public bool Equals(Q o) => N == o.N && D == o.D;
            /// <summary>The nearest double, the quotient taken on 63 or more significant bits whatever the size of N and D.</summary>
            public double ToDouble()
            {
                if (N.IsZero) { return 0.0; }
                BigInteger a = BigInteger.Abs(N);
                int shift = (int)(63 - ((long)Math.Floor(BigInteger.Log(a, 2)) - (long)Math.Floor(BigInteger.Log(D, 2))));
                BigInteger q = shift >= 0 ? (a << shift) / D : a / (D << -shift);
                double r = (double)q * Math.Pow(2, -shift);
                return N.Sign < 0 ? -r : r;
            }
            public override string ToString() => N.ToString(CultureInfo.InvariantCulture) + "/" + D.ToString(CultureInfo.InvariantCulture);

            /// <summary>The exact value of a finite double.</summary>
            public static Q FromDouble(double x)
            {
                if (double.IsNaN(x) || double.IsInfinity(x)) { throw new ArgumentException("not a finite double"); }
                if (x == 0) { return Zero; }
                long bits = BitConverter.DoubleToInt64Bits(x);
                bool neg = bits < 0;
                int exp = (int)((bits >> 52) & 0x7FF);
                long mant = bits & 0xFFFFFFFFFFFFFL;
                if (exp == 0) { exp = 1; } else { mant |= 1L << 52; }
                exp -= 1075;
                BigInteger n = neg ? -mant : mant;
                return exp >= 0 ? new Q(n * BigInteger.Pow(2, exp), 1) : new Q(n, BigInteger.Pow(2, -exp));
            }
        }

        private static BigInteger FloorDiv(BigInteger a, BigInteger b) { BigInteger q = BigInteger.DivRem(a, b, out BigInteger r); return (r.Sign != 0 && (r.Sign < 0) != (b.Sign < 0)) ? q - 1 : q; }

        /// <summary>Rounded half up on the exact value to <paramref name="decimals"/> places, invariant, "." the mark.</summary>
        private static string Round(Q v, int decimals, BigInteger scale)
        {
            BigInteger t = FloorDiv(2 * v.N * scale + v.D, 2 * v.D);
            bool neg = t.Sign < 0; BigInteger a = BigInteger.Abs(t);
            BigInteger ip = BigInteger.DivRem(a, BigInteger.Pow(10, decimals), out BigInteger fp);
            return (neg ? "-" : "") + ip.ToString(CultureInfo.InvariantCulture) + (decimals > 0 ? "." + fp.ToString(CultureInfo.InvariantCulture).PadLeft(decimals, '0') : "");
        }
        /// <summary>A share, probability or rate: percent to one decimal ("41.3 %").</summary>
        private static string Pct(Q v) => Round(v, 1, 1000) + " %";
        private static string Pct(double v) => double.IsNaN(v) ? "UNDEFINED" : Pct(Q.FromDouble(v));
        /// <summary>An expected count: two decimals.</summary>
        private static string Cnt(Q v) => Round(v, 2, 100);
        private static string Cnt(double v) => double.IsNaN(v) ? "UNDEFINED" : Cnt(Q.FromDouble(v));

        private static string F(string format, params object[] args) => string.Format(CultureInfo.InvariantCulture, format, args);
        private static string Sha256Hex(byte[] bytes) { using (var sha = SHA256.Create()) { return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2", CultureInfo.InvariantCulture))); } }
        private static string Sha256Hex(string text) => Sha256Hex(Encoding.UTF8.GetBytes(text));

        // ================================================================== A RUN'S CONFIGURATION: the default, and each declared reading's one change
        private sealed class Config
        {
            public string Key = "default", Name = "the default - premise (B), one stance; four cells by chamber and role; leave one out";
            public bool PremiseA;          // R1: (A), chamber by chamber as recorded, with the draw and the Vice President
            public string Cells = "role";  // role | contested (R2) | procedure (R3) | label (R4)
            public bool InSample;          // R5
            public bool SelectionRemoved;  // R6
            public bool Joint;             // R7
            public bool Binomial;          // R8 (doubles, exact pmf)
            public bool PresidentOnDate;   // R9
            public bool PresentOutside;    // R10
            public bool CrossRolePool;     // R11
            public int NMin = UsVeto.MinimumObservations;   // R14: 20
            public int Override;           // R15: 1 (a) the passage's full drawn crossers, 2 (b) a fresh keyed draw
            public bool BudgetAsKind;      // R16 (a)
            public bool BudgetOutOfFit;    // R16 (b)
            public bool DebtOutOfBudget;   // R18
        }
        private static readonly Config Default = new Config();

        /// <summary>BLOC STANCE (us_veto_backtest.pl's bloc_passes and party_bloc_opposed): YEA iff yea &gt; nay + present - a tie NAY, present standing
        /// against (F3); R10: YEA iff yea &gt; nay. No split counts as no yea.</summary>
        private static bool Yea(Split s, Config c) => s != null && (c.PresentOutside ? s.Yea > s.Nay : s.Yea > s.Nay + s.Present);
        /// <summary>NAY: a split whose stance is not YEA. A label with no split has no stance - never NAY for V or the fit, never YEA for an observation.</summary>
        private static bool Nay(Split s, Config c) => s != null && !Yea(s, c);

        /// <summary>THE RECORD BUDGET CLASS (from the title); R16 (a) scores it as its kind; R18 takes the debt-limit tests out of it.</summary>
        private static bool InBudget(Measure m, Config c) => !c.BudgetAsKind && (m.Reconciliation || m.Appropriation || (m.DebtLimit && !c.DebtOutOfBudget));

        /// <summary>SENATE PROCEDURE: MAJORITY (expedited) for an EXPEDITED kind; else MAJORITY (reconciliation) where a RECONCILIATION test matches
        /// (R16 (a): reconciliation as RULE XXII); else RULE XXII.</summary>
        private static string ProcedureOf(Measure m, Config c) => m.Expedited ? "MAJORITY (expedited)" : (m.Reconciliation && !c.BudgetAsKind) ? "MAJORITY (reconciliation)" : "RULE XXII";
        private static bool IsXxii(Measure m, Config c) => ProcedureOf(m, c) == "RULE XXII";

        /// <summary>THE VETO PATH (record): a counted measure with at least one roll call on which the PP's bloc stance is NAY.</summary>
        private static bool OnV(Measure m, Config c) => m.Counted >= 0 && m.RollChambers().Any(ch => Nay(m.F(ch).Of(m.PP), c));

        /// <summary>THE PRESIDENT ON A DATE (R9): by the 20th Amendment's noon of 20 January.</summary>
        private static string PartyOnDate(string date) =>
            string.CompareOrdinal(date, "2017-01-20") < 0 ? "D" : string.CompareOrdinal(date, "2021-01-20") < 0 ? "R" : string.CompareOrdinal(date, "2025-01-20") < 0 ? "D" : "R";
        private static string Other(string p) => p == "R" ? "D" : "R";

        // ================================================================== THE FIT
        private sealed class Obs
        {
            public RollCall Rc;
            public char Role;        // 'P' the president's party, 'O' the other
            public string Party;     // R or D
            public string Cell;      // its own cell
            public int D, M;
            public bool StanceYea;   // its party's own bloc stance on the roll call (R2 prints the cells split by stance)
            public string Id => Rc.Id;
        }

        private sealed class Cell
        {
            public string Name, Partner;
            public bool Pooled, Fitted;
            public int NOwn, N;
            public List<Obs> Own = new List<Obs>(), Drawn = new List<Obs>();
        }

        private sealed class Fit
        {
            public Config Cfg;
            public List<Obs> All = new List<Obs>();
            public List<RollCall> Fitted = new List<RollCall>();
            public Dictionary<string, Cell> Cells = new Dictionary<string, Cell>(StringComparer.Ordinal);
            public string[] Names;
            public Dictionary<string, Obs> OpOf = new Dictionary<string, Obs>(StringComparer.Ordinal);   // a roll call's OP observation (R7)
            public List<string> NoOp = new List<string>(), MZero = new List<string>();
            public int SelectionDropped;
            /// <summary>Each drawn list's K at a bloc of S seats, index by index - computed once (the replicates reuse it).</summary>
            public Dictionary<(string, int), int[]> KCache = new Dictionary<(string, int), int[]>();
        }

        private static int[] KsOf(Fit fit, string cell, int seats)
        {
            if (fit.KCache.TryGetValue((cell, seats), out int[] k)) { return k; }
            List<Obs> d = fit.Cells[cell].Drawn;
            k = new int[d.Count];
            for (int i = 0; i < d.Count; i++) { k[i] = UsVeto.Crossers(seats, d[i].D, d[i].M); }
            fit.KCache[(cell, seats)] = k;
            return k;
        }

        /// <summary>FIT ORDER: ascending share (d1*m2 against d2*m1 in integers), ties by the roll call's IDENTITY, ordinal.</summary>
        private static int FitOrder(Obs a, Obs b)
        {
            int c = ((long)a.D * b.M).CompareTo((long)b.D * a.M);
            if (c != 0) { return c; }
            c = string.CompareOrdinal(a.Id, b.Id);
            return c != 0 ? c : a.Role.CompareTo(b.Role);
        }

        private static string[] CellNamesOf(Config c)
        {
            switch (c.Cells)
            {
                case "procedure": return new[] { "HP MAJORITY", "HP RULE XXII", "HO MAJORITY", "HO RULE XXII", "SP MAJORITY", "SP RULE XXII", "SO MAJORITY", "SO RULE XXII" };
                case "label": return new[] { "HR NAY", "HR YEA", "HD NAY", "HD YEA", "SR NAY", "SR YEA", "SD NAY", "SD YEA" };   // R4: label in place of role, stance kept
                default: return new[] { "HP", "HO", "SP", "SO" };
            }
        }
        private static string ProcWord(Measure m, Config c) => ProcedureOf(m, c).StartsWith("MAJORITY", StringComparison.Ordinal) ? " MAJORITY" : " RULE XXII";

        /// <summary>The cell a thin cell pools with: the same role and stance in the other chamber (R11: the other role of its chamber; R3 within its
        /// procedure; R4 the same label in the other chamber).</summary>
        private static string PartnerOf(string cell, Config c)
        {
            char other = cell[0] == 'H' ? 'S' : 'H';
            if (c.CrossRolePool) { return cell[0].ToString() + (cell[1] == 'P' ? 'O' : 'P') + cell.Substring(2); }
            return other + cell.Substring(1);
        }

        private static string ObsCell(char ch, char role, string party, bool stanceYea, Measure owner, Config c)
        {
            switch (c.Cells)
            {
                case "procedure": return ch.ToString() + role + ProcWord(owner, c);
                case "label": return ch.ToString() + party + (stanceYea ? " YEA" : " NAY");
                default: return ch.ToString() + role;
            }
        }

        /// <summary>The cell a measure's party draws from in a chamber: the PP standing NAY, the OP standing YEA.</summary>
        private static string DrawCell(char ch, char role, Measure j, Config c)
        {
            switch (c.Cells)
            {
                case "procedure": return ch.ToString() + role + ProcWord(j, c);
                case "label": return ch.ToString() + (role == 'P' ? j.PP + " NAY" : j.OP + " YEA");
                default: return ch.ToString() + role;
            }
        }

        /// <summary>
        /// THE FITTED ROLL CALLS and their observations, per chamber: every distinct roll call of the counted measures on which the PP of its OWNER has bloc
        /// stance NAY gives a PP observation (d = PP yea, m = PP voting) and, only where the OP's bloc stance is YEA, an OP observation (d = OP nay + OP
        /// present, m = OP voting); not voting outside m, independents in no observation, m = 0 excluded and listed. Then THIN CELLS, decided once here on
        /// the full fit: a cell with n_own below N_MIN pools with its partner, drawing from the union in fit order; a union below N_MIN is UNFITTED.
        /// </summary>
        private static Fit FitOf(Record rec, Config c)
        {
            var fit = new Fit { Cfg = c, Names = CellNamesOf(c) };
            foreach (string n in fit.Names) { fit.Cells[n] = new Cell { Name = n }; }
            foreach (RollCall rc in rec.RollOrder)
            {
                if (c.BudgetOutOfFit && rc.Measures.Any(m => InBudget(m, Default))) { continue; }
                string pp = c.PresidentOnDate ? PartyOnDate(rc.Date) : rc.Owner.PP, op = Other(pp);
                Final f = rc.Final;
                Split sp = f.Of(pp), so = f.Of(op);
                bool ppYea = Yea(sp, c), opYea = Yea(so, c);
                if (c.Cells == "contested")
                {
                    if (sp == null || so == null || ppYea == opYea) { continue; }   // R2: every contested roll call, each party against its own stance
                    fit.Fitted.Add(rc);
                    AddObs(fit, rc, 'P', pp, ppYea ? sp.Nay + sp.Present : sp.Yea, sp.Voting, ppYea, c);
                    AddObs(fit, rc, 'O', op, opYea ? so.Nay + so.Present : so.Yea, so.Voting, opYea, c);
                    continue;
                }
                if (!Nay(sp, c)) { continue; }
                fit.Fitted.Add(rc);
                if (c.SelectionRemoved && !RecordedBlocPasses(rc, pp, c)) { fit.SelectionDropped++; }   // R6: the PP cells keep only roll calls already passing at K = 0
                else { AddObs(fit, rc, 'P', pp, sp.Yea, c.PresentOutside ? sp.Yea + sp.Nay : sp.Voting, false, c); }
                if (opYea) { AddObs(fit, rc, 'O', op, c.PresentOutside ? so.Nay : so.Nay + so.Present, c.PresentOutside ? so.Yea + so.Nay : so.Voting, true, c); }
                else { fit.NoOp.Add(F("{0} (owner {1}): the OP ({2}) also stands NAY - no OP observation", rc.Id, rc.Owner.Id, op)); }
            }
            foreach (Cell cell in fit.Cells.Values) { cell.Own.Sort(FitOrder); cell.NOwn = cell.Own.Count; }
            foreach (Cell cell in fit.Cells.Values)
            {
                Cell partner = fit.Cells[PartnerOf(cell.Name, c)];
                cell.Pooled = cell.NOwn < c.NMin;
                cell.Drawn = cell.Pooled ? cell.Own.Concat(partner.Own).ToList() : cell.Own.ToList();
                cell.Drawn.Sort(FitOrder);
                cell.Partner = cell.Pooled ? partner.Name : null;
                cell.N = cell.Drawn.Count;
                cell.Fitted = cell.N >= c.NMin;
            }
            return fit;
        }

        private static void AddObs(Fit fit, RollCall rc, char role, string party, int d, int m, bool stanceYea, Config c)
        {
            if (m <= 0) { fit.MZero.Add(F("{0} (owner {1}): the {2} observation of {3} has m = 0 - excluded", rc.Id, rc.Owner.Id, role == 'P' ? "PP" : "OP", party)); return; }
            var o = new Obs { Rc = rc, Role = role, Party = party, D = d, M = m, StanceYea = stanceYea, Cell = ObsCell(rc.Ch, role, party, stanceYea, rc.Owner, c) };
            fit.All.Add(o);
            fit.Cells[o.Cell].Own.Add(o);
            if (role == 'O') { fit.OpOf[rc.Id] = o; }
        }

        /// <summary>A roll call's recorded bloc result at K = 0: (A)'s test on the roll call as listed - the blocs by their recorded stances, members as
        /// listed (House independents outside, the Senate's caucus entries in their blocs), the owner's SENATE PROCEDURE, the Vice President with the
        /// PP's stance (on a fitted roll call NAY, so a tie fails), cloture over the roll call's members of every label.</summary>
        private static bool RecordedBlocPasses(RollCall rc, string pp, Config c)
        {
            UsVeto.ChamberVote v = RecordedVote(rc.Owner, rc.Ch, pp, c, out _, out _, out _, out _);
            return UsVeto.ChamberOutcome(v, 0, 0);
        }

        /// <summary>(A)'s chamber on a roll call: the R and D blocs by their recorded stances (members as the roll call lists them, the Senate's caucus
        /// entries in their blocs), House independents outside; Senate MAJORITY with the Vice President voting the PP's stance in that chamber
        /// (<paramref name="vicePresident"/> false: none, a tie failing - US-10 MODE); RULE XXII over the roll call's members of every label.</summary>
        private static UsVeto.ChamberVote RecordedVote(Measure m, char ch, string pp, Config c, out int sPp, out int sOp, out bool ppYea, out bool opYea, bool vicePresident = true, bool us10 = false)
        {
            Final f = m.F(ch);
            string op = Other(pp);
            int r = MembersOf(f.R), d = MembersOf(f.D);
            if (ch == 'S') { foreach ((string cc, int n) in m.CaucusIn) { if (cc == "R") { r += n; } else { d += n; } } }
            bool ry = Yea(f.R, c), dy = Yea(f.D, c);
            ppYea = pp == "R" ? ry : dy; opYea = pp == "R" ? dy : ry;
            sPp = pp == "R" ? r : d; sOp = pp == "R" ? d : r;
            var v = new UsVeto.ChamberVote { Yea0 = (ry ? r : 0) + (dy ? d : 0), Nay0 = (ry ? 0 : r) + (dy ? 0 : d), PpSeats = sPp, OpSeats = sOp };
            if (ch == 'H') { v.Procedure = UsVeto.Procedure.House; }
            else
            {
                bool majority = us10 ? m.Expedited : ProcedureOf(m, c).StartsWith("MAJORITY", StringComparison.Ordinal);
                v.Procedure = majority ? UsVeto.Procedure.SenateMajority : UsVeto.Procedure.SenateCloture;
                v.Sworn = MembersOf(f.R) + MembersOf(f.D) + MembersOf(f.I);
                v.VicePresident = vicePresident ? (ppYea ? 1 : -1) : 0;
            }
            return v;
        }

        // ================================================================== A MEASURE OF V: its two chambers under the run's premise
        private sealed class Chamber
        {
            public char Ch;
            public bool HasRoll, Undrawn, Budget, PpNay, OpYea;
            public UsVeto.ChamberVote Vote;
            public string PpCell, OpCell;   // null: that party does not draw
            public int OvrYea0, OvrNay0;
            public string Procedure;
        }

        private sealed class Setup
        {
            public Measure M;
            public Chamber H, S;
            public int SppH;
            public bool PpHouseNay;
            public HashSet<string> RollIds = new HashSet<string>(StringComparer.Ordinal);
            /// <summary>The indices of the roll calls final for the measure (-1 for none) - the replicates' leave-one-out test.</summary>
            public int RollA = -1, RollB = -1;
            public Chamber Of(char ch) => ch == 'H' ? H : S;
            public void AddRoll(Record rec, char ch) { string id = rec.RollId(M, ch); RollIds.Add(id); int i = rec.Rolls[id].Index; if (RollA < 0) { RollA = i; } else { RollB = i; } }
        }

        /// <summary>
        /// THE BACKTEST'S PREMISE (B), ONE STANCE: the PP's one stance NAY in both chambers; the OP's its bloc stance on the House roll call if the PP's
        /// House stance there is NAY, otherwise on the Senate roll call; the SEATS OF A CONGRESS; draws in both chambers whether or not a chamber has a roll
        /// call (none for a RECORD BUDGET CLASS measure, nor for a party with S = 0). House: 2Y &gt; W_H, independents counting against. Senate MAJORITY:
        /// 2Y &gt; W_S, the Vice President voting the PP's NAY on a tie; RULE XXII: cloture at floor((3W + 4) / 5) of the SEATS' sworn.
        /// R1 reads (A) instead (<see cref="SetupA"/>).
        /// </summary>
        private static Setup SetupOf(Record rec, Measure j, Config c)
        {
            if (c.PremiseA) { return SetupA(rec, j, c); }
            var s = new Setup { M = j, PpHouseNay = true };
            foreach (char ch in j.RollChambers()) { s.AddRoll(rec, ch); }
            string pp = j.PP, op = j.OP;
            bool houseNay = j.House.Roll && Nay(j.House.Of(pp), c);
            Final opFrom = houseNay ? j.House : j.Senate;
            bool opYea = Yea(opFrom.Of(op), c);
            bool budget = InBudget(j, c);
            foreach (char ch in new[] { 'H', 'S' })
            {
                SeatsOf seats = rec.SeatsFor(j.Congress, ch);
                int sPp = seats.Of(pp), sOp = seats.Of(op), w = seats.Sworn;
                var v = new UsVeto.ChamberVote { Yea0 = opYea ? sOp : 0, PpSeats = sPp, OpSeats = sOp, Sworn = w };
                v.Nay0 = w - v.Yea0;
                string proc = ch == 'H' ? "House majority" : ProcedureOf(j, c);
                if (ch == 'H') { v.Procedure = UsVeto.Procedure.House; }
                else if (proc.StartsWith("MAJORITY", StringComparison.Ordinal)) { v.Procedure = UsVeto.Procedure.SenateMajority; v.VicePresident = -1; }
                else { v.Procedure = UsVeto.Procedure.SenateCloture; }
                var chamber = new Chamber
                {
                    Ch = ch, HasRoll = j.F(ch).Roll, Budget = budget, PpNay = true, OpYea = opYea, Vote = v, Procedure = proc,
                    PpCell = !budget && sPp > 0 ? DrawCell(ch, 'P', j, c) : null,
                    OpCell = !budget && opYea && sOp > 0 ? DrawCell(ch, 'O', j, c) : null,
                    OvrYea0 = v.Yea0, OvrNay0 = v.Nay0,
                };
                if (ch == 'H') { s.H = chamber; s.SppH = sPp; } else { s.S = chamber; }
            }
            return s;
        }

        /// <summary>R1, (A) chamber by chamber as recorded, with the draw and the Vice President: each chamber's blocs by their recorded stances; a draw
        /// only where the PP stands NAY and the OP YEA (UNCONTESTED otherwise); a chamber with no roll call passes undrawn; (a) on the House roll call's
        /// PP members; the override over the roll call's blocs, or the SEATS where none.</summary>
        private static Setup SetupA(Record rec, Measure j, Config c)
        {
            var s = new Setup { M = j };
            foreach (char ch in j.RollChambers()) { s.AddRoll(rec, ch); }
            bool budget = InBudget(j, c);
            foreach (char ch in new[] { 'H', 'S' })
            {
                var chamber = new Chamber { Ch = ch, HasRoll = j.F(ch).Roll, Budget = budget, Procedure = ch == 'H' ? "House majority" : ProcedureOf(j, c) };
                if (!chamber.HasRoll)
                {
                    SeatsOf seats = rec.SeatsFor(j.Congress, ch);
                    chamber.Undrawn = true;
                    chamber.Vote = new UsVeto.ChamberVote { Procedure = ch == 'H' ? UsVeto.Procedure.House : UsVeto.Procedure.SenateMajority };
                    chamber.OvrYea0 = seats.Of(j.OP); chamber.OvrNay0 = seats.Of(j.PP);
                }
                else
                {
                    UsVeto.ChamberVote v = RecordedVote(j, ch, j.PP, c, out int sPp, out int sOp, out bool ppYea, out bool opYea);
                    chamber.Vote = v; chamber.PpNay = !ppYea; chamber.OpYea = opYea;
                    bool contested = !ppYea && opYea;
                    chamber.PpCell = contested && !budget && sPp > 0 ? DrawCell(ch, 'P', j, c) : null;
                    chamber.OpCell = contested && !budget && sOp > 0 ? DrawCell(ch, 'O', j, c) : null;
                    chamber.OvrYea0 = v.Yea0; chamber.OvrNay0 = v.Nay0;
                    if (ch == 'H') { s.SppH = sPp; s.PpHouseNay = !ppYea; }
                }
                if (ch == 'H') { s.H = chamber; } else { s.S = chamber; }
            }
            return s;
        }

        // ================================================================== SCORING: the exact enumeration, and the replicates' doubles
        /// <summary>A chamber's counts over its index pairs (weights: 1 per index, or a replicate's multiplicities; doubles hold these integers exactly).</summary>
        private struct Counts { public double Tot, Pass, PassNamed, PassOvr, Matters, OvrFresh; }

        /// <summary>The empty-list occurrences of one source (the LOO lists, the replicates, INFLUENCE's removals): each (measure, chamber, party), counted.</summary>
        private sealed class EmptyLog
        {
            public int Count;
            public SortedDictionary<string, int> By = new SortedDictionary<string, int>(StringComparer.Ordinal);
            public void Add(string what) { Count++; By.TryGetValue(what, out int n); By[what] = n + 1; }
            public string Words() => Count == 0 ? "none" : F("{0}: ", Count) + string.Join("; ", By.Select(kv => kv.Value == 1 ? kv.Key : F("{0} ({1} times)", kv.Key, kv.Value)));
        }

        /// <summary>The weighted K values a party's list gives: its drawn list less the excluded roll calls, each index weighted; the single K = 0 where the
        /// party does not draw or nothing is left (an empty list, counted).</summary>
        private static List<(int K, double W)> Ks(Fit fit, string cell, int seats, Func<Obs, double> weight, EmptyLog empty, string what)
        {
            if (cell != null)
            {
                List<Obs> d = fit.Cells[cell].Drawn;
                int[] k = KsOf(fit, cell, seats);
                var h = new double[UsVeto.Cap(seats) + 1];   // K never exceeds the cap
                bool any = false;
                for (int i = 0; i < d.Count; i++) { double w = weight(d[i]); if (w > 0) { h[k[i]] += w; any = true; } }
                if (any)
                {
                    var list = new List<(int, double)>();
                    for (int x = 0; x < h.Length; x++) { if (h[x] > 0) { list.Add((x, h[x])); } }
                    return list;
                }
                empty?.Add(what);
            }
            return new List<(int, double)> { (0, 1.0) };
        }

        /// <summary>R8: K ~ Binomial(S, sum d / sum m) over the list less the excluded roll calls, capped at floor((S - 1) / 2) - the cap taking the upper tail.</summary>
        private static List<(int K, double W)> BinomialKs(Fit fit, string cell, int seats, Func<Obs, double> weight, EmptyLog empty, string what)
        {
            if (cell == null) { return new List<(int, double)> { (0, 1.0) }; }
            double sd = 0, sm = 0;
            foreach (Obs o in fit.Cells[cell].Drawn) { double w = weight(o); if (w > 0) { sd += w * o.D; sm += w * o.M; } }
            if (sm <= 0) { empty?.Add(what); return new List<(int, double)> { (0, 1.0) }; }
            double p = sd / sm;
            int cap = UsVeto.Cap(seats);
            var list = new List<(int, double)>();
            double below = 0;
            for (int k = 0; k < cap; k++) { double pk = BinomPmf(k, seats, p); list.Add((k, pk)); below += pk; }
            list.Add((cap, Math.Max(0, 1 - below)));
            return list;
        }

        /// <summary>The (Kp, Ko) pairs of a chamber with their weights: independent draws (the product of the two lists), or R7's joint draw - one index
        /// over the PP cell's roll calls giving both parties' shares, Ko = 0 where that roll call has no OP observation.</summary>
        private static List<(int Kp, int Ko, double W)> PairsOf(Fit fit, Chamber ch, Func<Obs, double> weight, EmptyLog empty, string what)
        {
            Config c = fit.Cfg;
            if (c.Joint && ch.PpCell != null)
            {
                var joint = new Dictionary<(int, int), double>();
                foreach (Obs o in fit.Cells[ch.PpCell].Drawn)
                {
                    double w = weight(o);
                    if (w <= 0) { continue; }
                    int kp = UsVeto.Crossers(ch.Vote.PpSeats, o.D, o.M);
                    int ko = ch.OpCell != null && fit.OpOf.TryGetValue(o.Id, out Obs oo) ? UsVeto.Crossers(ch.Vote.OpSeats, oo.D, oo.M) : 0;
                    joint.TryGetValue((kp, ko), out double x); joint[(kp, ko)] = x + w;
                }
                if (joint.Count == 0) { empty?.Add(what + " PP (joint)"); return new List<(int, int, double)> { (0, 0, 1.0) }; }
                return joint.OrderBy(kv => kv.Key.Item1).ThenBy(kv => kv.Key.Item2).Select(kv => (kv.Key.Item1, kv.Key.Item2, kv.Value)).ToList();
            }
            List<(int K, double W)> p = c.Binomial ? BinomialKs(fit, ch.PpCell, ch.Vote.PpSeats, weight, empty, what + " PP") : Ks(fit, ch.PpCell, ch.Vote.PpSeats, weight, empty, what + " PP");
            List<(int K, double W)> q = c.Binomial ? BinomialKs(fit, ch.OpCell, ch.Vote.OpSeats, weight, empty, what + " OP") : Ks(fit, ch.OpCell, ch.Vote.OpSeats, weight, empty, what + " OP");
            var pairs = new List<(int, int, double)>(p.Count * q.Count);
            foreach (var a in p) { foreach (var b in q) { pairs.Add((a.K, b.K, a.W * b.W)); } }
            return pairs;
        }

        /// <summary>
        /// One chamber's counts: pass (<see cref="UsVeto.ChamberOutcome"/>), pass and named by (a) (<see cref="UsVeto.NamedByA"/>, the House only), pass and
        /// override on the passage's recorded sides (<see cref="UsVeto.OverrideCarries"/> - crossers counting only where they entered, i.e. where the draw
        /// matters; R15 (a) always, (b) a fresh draw), and whether the draw matters (the outcome at the drawn pair differs from (0, 0)).
        /// </summary>
        private static Counts CountChamber(Setup s, Chamber ch, List<(int Kp, int Ko, double W)> pairs, int overrideMode)
        {
            var r = new Counts();
            if (ch.Undrawn)
            {
                r.Tot = 1; r.Pass = 1;
                r.PassNamed = ch.Ch == 'S' || UsVeto.NamedByA(s.SppH, 0) ? 1 : 0;
                r.PassOvr = UsVeto.OverrideCarries(ch.OvrYea0, ch.OvrNay0) ? 1 : 0;
                r.OvrFresh = r.PassOvr;
                return r;
            }
            bool pass0 = UsVeto.ChamberOutcome(ch.Vote, 0, 0);
            foreach ((int kp, int ko, double w) in pairs)
            {
                r.Tot += w;
                bool pass = UsVeto.ChamberOutcome(ch.Vote, kp, ko);
                int d = kp - ko;
                if (pass)
                {
                    r.Pass += w;
                    if (ch.Ch == 'S' || UsVeto.NamedByA(s.SppH, s.PpHouseNay ? s.SppH - kp : 0)) { r.PassNamed += w; }
                    int moved = overrideMode == 1 || !pass0 ? d : 0;
                    if (UsVeto.OverrideCarries(ch.OvrYea0 + moved, ch.OvrNay0 - moved)) { r.PassOvr += w; }
                }
                if (pass != pass0) { r.Matters += w; }
                if (UsVeto.OverrideCarries(ch.OvrYea0 + d, ch.OvrNay0 - d)) { r.OvrFresh += w; }
            }
            return r;
        }

        /// <summary>A measure's figures: P_H, P_S, P(sent), P(named), omega, P(the draw matters) per chamber - exact rationals where the run is exact (every
        /// run but R8), and doubles always.</summary>
        private sealed class Score
        {
            public Q PH, PS, Sent, Named, Omega, MattersH, MattersS, OvrH, OvrS;
            public double DPH, DPS, DSent, DNamed, DOmega, DMattersH, DMattersS, DOvrH, DOvrS;
            public bool IdentityA = true;
        }

        private static Score ScoreOf(Fit fit, Setup s, Func<Obs, double> weight, bool exact, EmptyLog empty)
        {
            List<(int, int, double)> ph = PairsOf(fit, s.H, weight, empty, s.M.Id + " House"), ps = PairsOf(fit, s.S, weight, empty, s.M.Id + " Senate");
            Counts h = CountChamber(s, s.H, ph, fit.Cfg.Override), g = CountChamber(s, s.S, ps, fit.Cfg.Override);
            var sc = new Score();
            double pOvrH = fit.Cfg.Override == 2 ? h.Pass / h.Tot * (h.OvrFresh / h.Tot) : h.PassOvr / h.Tot;
            double pOvrS = fit.Cfg.Override == 2 ? g.Pass / g.Tot * (g.OvrFresh / g.Tot) : g.PassOvr / g.Tot;
            sc.DPH = h.Pass / h.Tot; sc.DPS = g.Pass / g.Tot; sc.DSent = sc.DPH * sc.DPS; sc.DNamed = h.PassNamed / h.Tot * sc.DPS; sc.DOmega = pOvrH * pOvrS;
            sc.DMattersH = h.Matters / h.Tot; sc.DMattersS = g.Matters / g.Tot; sc.DOvrH = pOvrH; sc.DOvrS = pOvrS;
            // the identity of THE VETO: under the cap (a) fires exactly where the PP's House stance is NAY and S_PP,H >= 1
            sc.IdentityA = h.PassNamed == ((s.PpHouseNay && s.SppH >= 1) ? h.Pass : 0);
            if (exact)
            {
                Q Of(double n, double t) => new Q(new BigInteger(n), new BigInteger(t));
                sc.PH = Of(h.Pass, h.Tot); sc.PS = Of(g.Pass, g.Tot); sc.Sent = sc.PH * sc.PS; sc.Named = Of(h.PassNamed, h.Tot) * sc.PS;
                Q oh = fit.Cfg.Override == 2 ? Of(h.Pass, h.Tot) * Of(h.OvrFresh, h.Tot) : Of(h.PassOvr, h.Tot);
                Q og = fit.Cfg.Override == 2 ? Of(g.Pass, g.Tot) * Of(g.OvrFresh, g.Tot) : Of(g.PassOvr, g.Tot);
                sc.Omega = oh * og; sc.OvrH = oh; sc.OvrS = og; sc.MattersH = Of(h.Matters, h.Tot); sc.MattersS = Of(g.Matters, g.Tot);
            }
            return sc;
        }

        // ================================================================== A RUN: the fit, the veto path, every measure scored, the totals, THE DECISION
        private sealed class Totals
        {
            public Q ESent, EXxii, ENamed, EHits, PNoneXxii, PNone, PNoneExp;
            public double DESent, DEXxii, DENamed, DEHits, DPNoneXxii, DPNone, DPNoneExp;
            public bool Exact;
        }

        private sealed class RunResult
        {
            public Config Cfg;
            public Fit Fit;
            public List<Measure> V, VXxii, VExp;
            public int VCount;
            public Dictionary<Measure, Setup> Setups = new Dictionary<Measure, Setup>();
            public Dictionary<Measure, Score> Loo = new Dictionary<Measure, Score>();
            public Totals T;
            public bool S3, S2, S2Empty, S2Never, S1, S1Evaluated, IdentityA = true;
            public string Outcome;
            public EmptyLog Empty = new EmptyLog();
            public HashSet<string> Unfitted = new HashSet<string>(StringComparer.Ordinal);
        }

        private static Func<Obs, double> Excluding(HashSet<string> ids, HashSet<string> also = null) =>
            o => (ids != null && ids.Contains(o.Id)) || (also != null && also.Contains(o.Id)) ? 0.0 : 1.0;

        /// <summary>Runs the rule under a configuration: exact and leave one out (R5 in-sample), every measure of V, the totals and the decision.
        /// <paramref name="removed"/> takes roll calls out of every cell with pooling and fitted status held (INFLUENCE); <paramref name="fit"/> reuses a fit.</summary>
        private static RunResult RunOf(Record rec, Config c, Fit fit = null, HashSet<string> removed = null)
        {
            var r = new RunResult { Cfg = c, Fit = fit ?? FitOf(rec, c) };
            r.V = rec.Counted.Where(m => OnV(m, c)).ToList();
            r.VXxii = r.V.Where(m => IsXxii(m, c)).ToList();
            r.VExp = r.V.Where(m => m.Expedited).ToList();
            r.VCount = rec.Counted.Count(m => m.Vetoed);
            foreach (Cell cell in r.Fit.Cells.Values) { if (!cell.Fitted) { r.Unfitted.Add(cell.Name); } }
            bool exact = !c.Binomial;
            foreach (Measure j in r.V)
            {
                Setup s = SetupOf(rec, j, c);
                r.Setups[j] = s;
                Score sc = ScoreOf(r.Fit, s, Excluding(c.InSample ? null : s.RollIds, removed), exact, r.Empty);
                r.Loo[j] = sc;
                r.IdentityA &= sc.IdentityA;
            }
            r.T = TotalsOf(r, r.V, exact);
            Decide(r);
            return r;
        }

        private static Totals TotalsOf(RunResult r, IEnumerable<Measure> set, bool exact)
        {
            var t = new Totals { Exact = exact, ESent = Q.Zero, EXxii = Q.Zero, ENamed = Q.Zero, EHits = Q.Zero, PNoneXxii = Q.One, PNone = Q.One, PNoneExp = Q.One, DPNoneXxii = 1, DPNone = 1, DPNoneExp = 1 };
            foreach (Measure j in set)
            {
                Score s = r.Loo[j];
                bool xxii = r.VXxii.Contains(j), exp = j.Expedited;
                t.DESent += s.DSent; t.DENamed += s.DNamed; if (j.Vetoed) { t.DEHits += s.DNamed; }
                t.DPNone *= 1 - s.DSent;
                if (xxii) { t.DEXxii += s.DSent; t.DPNoneXxii *= 1 - s.DSent; }
                if (exp) { t.DPNoneExp *= 1 - s.DSent; }
                if (!exact) { continue; }
                t.ESent += s.Sent; t.ENamed += s.Named; if (j.Vetoed) { t.EHits += s.Named; }
                t.PNone *= Q.One - s.Sent;
                if (xxii) { t.EXxii += s.Sent; t.PNoneXxii *= Q.One - s.Sent; }
                if (exp) { t.PNoneExp *= Q.One - s.Sent; }
            }
            return t;
        }

        /// <summary>
        /// THE DECISION, read only from the run's exact LOO figures (R8: its doubles). S3 first: any cell UNFITTED after pooling (then S1 and S2 are not
        /// evaluated). S2: V_XXII empty, or 2 * P(NONE_XXII) &gt;= 1 (Elias's answer, §833: the typical world). S1, evaluated only where E[named] &gt; 0:
        /// 166 * E[hits] &lt; 45 * E[named]. The outcome is exactly one of INSTALL, S3, S1, S2, S1+S2.
        /// </summary>
        private static void Decide(RunResult r)
        {
            r.S3 = r.Unfitted.Count > 0;
            if (r.S3) { r.Outcome = "S3"; return; }
            Totals t = r.T;
            r.S2Empty = r.VXxii.Count == 0;
            r.S2Never = t.Exact ? Q.Of(2) * t.PNoneXxii >= Q.One : 2 * t.DPNoneXxii >= 1;
            r.S2 = r.S2Empty || r.S2Never;
            r.S1Evaluated = t.Exact ? t.ENamed > Q.Zero : t.DENamed > 0;
            r.S1 = r.S1Evaluated && (t.Exact ? Q.Of(166) * t.EHits < Q.Of(45) * t.ENamed : 166 * t.DEHits < 45 * t.DENamed);
            r.Outcome = r.S1 && r.S2 ? "S1+S2" : r.S1 ? "S1" : r.S2 ? "S2" : "INSTALL";
        }

        // ================================================================== THE PRINT's intervals: BOOT, the record bootstrap; CP, Clopper-Pearson
        private const int B = 2000;
        private static readonly string[] TotalKeys = { "e_sent", "e_xxii", "e_named", "e_hits", "e_fp", "e_miss", "precision", "hitrate", "pnone_xxii", "pnone", "pnone_exp", "e_ovr", "pnone_ovr", "ovr_named" };
        private const int PH_ = 0, PS_ = 1, SENT_ = 2, NAMED_ = 3, OMEGA_ = 4, MH_ = 5, MS_ = 6, OH_ = 7, OS_ = 8, PER_ = 9;

        private sealed class Boot
        {
            public Dictionary<string, double[]> Tot = new Dictionary<string, double[]>(StringComparer.Ordinal);   // key, or key@president
            public Dictionary<Measure, double[][]> Per = new Dictionary<Measure, double[][]>();
            public Dictionary<string, double[]> Mean = new Dictionary<string, double[]>(StringComparer.Ordinal);
            public bool IdentityA = true;
            public EmptyLog Empty = new EmptyLog();
        }

        /// <summary>THE PRINT's keyed picks, replicate by replicate: pick t of replicate b is floor(Unit("R-US17|boot|b|t") * M) - the same for every run and
        /// reading, so taken once per measurement.</summary>
        private static int[][] PicksOf(int m)
        {
            var picks = new int[B][];
            for (int b = 1; b <= B; b++)
            {
                var p = new int[m];
                for (int t = 0; t < m; t++) { p[t] = UsVeto.Index(UsVeto.Unit("R-US17|boot|" + b.ToString(CultureInfo.InvariantCulture) + "|" + t.ToString(CultureInfo.InvariantCulture)), m); }
                picks[b - 1] = p;
            }
            return picks;
        }

        /// <summary>
        /// THE PRINT's BOOT, B = 2000. Replicate b makes M picks with replacement from the counted measures in CSV order, pick t the index
        /// floor(Unit("R-US17|boot|b|t") * M); its cells hold the observations of the roll calls OWNED by the picked measures with the picks'
        /// multiplicity, pooling and fitted status held from the full fit; every measure j of the full V is scored from the replicate's cells, leave one
        /// out, picked or not; the totals run over the picks with multiplicity in pick order, in IEEE double. R17 (<paramref name="cellBoot"/>):
        /// the owners of observations resampled instead (floor(Unit("R-US17|cboot|b|t") * O)), the scoring set fixed - the real V and VETO_j.
        /// </summary>
        private static Boot BootOf(Record rec, RunResult r, bool full, bool cellBoot, int[][] keyedPicks)
        {
            var boot = new Boot();
            int m = rec.Counted.Count;
            var groups = new List<string> { "" };
            if (full) { groups.AddRange(Presidents); }
            foreach (string g in groups) { foreach (string k in TotalKeys) { boot.Tot[k + (g.Length > 0 ? "@" + g : "")] = new double[B]; } }
            if (full) { foreach (Measure j in r.V) { boot.Per[j] = Enumerable.Range(0, PER_).Select(_ => new double[B]).ToArray(); } }
            var inV = new HashSet<Measure>(r.V); var inX = new HashSet<Measure>(r.VXxii);
            List<int> owners = cellBoot ? r.Fit.All.Select(o => o.Rc.Owner.Counted).Distinct().OrderBy(x => x).ToList() : null;   // the owners of observations, CSV order
            // the mean share's sums in pick order: each cell's drawn observations grouped by owner, in fit order
            var byOwner = new Dictionary<string, Dictionary<int, List<Obs>>>(StringComparer.Ordinal);
            if (full)
            {
                foreach (Cell cell in r.Fit.Cells.Values)
                {
                    var d = new Dictionary<int, List<Obs>>();
                    foreach (Obs o in cell.Drawn) { if (!d.TryGetValue(o.Rc.Owner.Counted, out var l)) { d[o.Rc.Owner.Counted] = l = new List<Obs>(); } l.Add(o); }
                    byOwner[cell.Name] = d; boot.Mean[cell.Name] = new double[B];
                }
            }
            var mult = new double[m];
            int[] picks = null;
            for (int b = 1; b <= B; b++)
            {
                Array.Clear(mult, 0, m);
                if (!cellBoot)
                {
                    picks = keyedPicks[b - 1];
                    for (int t = 0; t < m; t++) { mult[picks[t]]++; }
                }
                else
                {
                    int o = owners.Count;
                    for (int t = 0; t < o; t++) { mult[owners[UsVeto.Index(UsVeto.Unit(F("R-US17|cboot|{0}|{1}", b, t)), o)]]++; }
                }
                var per = new Dictionary<Measure, Score>();
                foreach (Measure j in r.V)
                {
                    Setup s = r.Setups[j];
                    int ra = r.Cfg.InSample ? -1 : s.RollA, rb = r.Cfg.InSample ? -1 : s.RollB;   // leave one out: every copy of j's roll calls
                    Score sc = ScoreOf(r.Fit, s, o => o.Rc.Index == ra || o.Rc.Index == rb ? 0.0 : mult[o.Rc.Owner.Counted], false, boot.Empty);
                    per[j] = sc;
                    boot.IdentityA &= sc.IdentityA;
                    if (full)
                    {
                        double[][] a = boot.Per[j];
                        a[PH_][b - 1] = sc.DPH; a[PS_][b - 1] = sc.DPS; a[SENT_][b - 1] = sc.DSent; a[NAMED_][b - 1] = sc.DNamed; a[OMEGA_][b - 1] = sc.DOmega; a[MH_][b - 1] = sc.DMattersH; a[MS_][b - 1] = sc.DMattersS;
                        a[OH_][b - 1] = sc.DOvrH; a[OS_][b - 1] = sc.DOvrS;
                    }
                }
                foreach (string g in groups)
                {
                    double eS = 0, eX = 0, eN = 0, eH = 0, pX = 1, pA = 1, pE = 1, vc = 0, eO = 0, pO = 1;
                    IEnumerable<Measure> order = cellBoot ? rec.Counted.Where(x => inV.Contains(x) || x.Vetoed) : picks.Select(t => rec.Counted[t]);
                    foreach (Measure x in order)
                    {
                        if (g.Length > 0 && x.President != g) { continue; }
                        if (x.Vetoed) { vc++; }
                        if (!inV.Contains(x)) { continue; }
                        Score sc = per[x];
                        eS += sc.DSent; eN += sc.DNamed; if (x.Vetoed) { eH += sc.DNamed; }
                        pA *= 1 - sc.DSent;
                        if (inX.Contains(x)) { eX += sc.DSent; pX *= 1 - sc.DSent; }
                        if (x.Expedited) { pE *= 1 - sc.DSent; }
                        eO += sc.DOmega; pO *= 1 - sc.DOmega;   // THE OVERRIDE's totals, over the same picks
                    }
                    string sfx = g.Length > 0 ? "@" + g : "";
                    boot.Tot["e_ovr" + sfx][b - 1] = eO; boot.Tot["pnone_ovr" + sfx][b - 1] = pO; boot.Tot["ovr_named" + sfx][b - 1] = eN == 0 ? double.NaN : eO / eN;
                    boot.Tot["e_sent" + sfx][b - 1] = eS; boot.Tot["e_xxii" + sfx][b - 1] = eX; boot.Tot["e_named" + sfx][b - 1] = eN; boot.Tot["e_hits" + sfx][b - 1] = eH;
                    boot.Tot["e_fp" + sfx][b - 1] = eN - eH; boot.Tot["e_miss" + sfx][b - 1] = vc - eH;
                    boot.Tot["precision" + sfx][b - 1] = eN == 0 ? double.NaN : eH / eN; boot.Tot["hitrate" + sfx][b - 1] = vc == 0 ? double.NaN : eH / vc;
                    boot.Tot["pnone_xxii" + sfx][b - 1] = pX; boot.Tot["pnone" + sfx][b - 1] = pA; boot.Tot["pnone_exp" + sfx][b - 1] = pE;
                }
                if (full)
                {
                    foreach (Cell cell in r.Fit.Cells.Values)
                    {
                        double sum = 0, n = 0;
                        Dictionary<int, List<Obs>> d = byOwner[cell.Name];
                        for (int t = 0; t < m; t++) { if (d.TryGetValue(picks[t], out var l)) { foreach (Obs o in l) { sum += (double)o.D / o.M; n++; } } }
                        boot.Mean[cell.Name][b - 1] = n == 0 ? double.NaN : sum / n;
                    }
                }
            }
            return boot;
        }

        /// <summary>The 95% percentile interval of 2000 replicates: the UNDEFINED (NaN) sort first; k of them at 50 or more prints
        /// "UNDEFINED IN k OF 2000"; else lo = sorted[49], hi = sorted[1950], with " (k of 2000 replicates undefined)" for 1 &lt;= k &lt; 50.</summary>
        private static string Interval(double[] values, Func<double, string> fmt)
        {
            double[] s = values.ToArray();
            Array.Sort(s, (x, y) => double.IsNaN(x) ? (double.IsNaN(y) ? 0 : -1) : double.IsNaN(y) ? 1 : x.CompareTo(y));
            int k = s.Count(double.IsNaN);
            if (k >= 50) { return F("UNDEFINED IN {0} OF {1} BOOT", k, B); }
            return F("[{0}, {1}]", fmt(s[49]), fmt(s[1950])) + (k > 0 ? F(" ({0} of {1} replicates undefined)", k, B) : "") + " BOOT";
        }
        /// <summary>The interval's bounds, or none (Has false) where 50 or more replicates are UNDEFINED.</summary>
        private static (bool Has, double Lo, double Hi) Bounds(double[] values)
        {
            double[] s = values.ToArray();
            Array.Sort(s, (x, y) => double.IsNaN(x) ? (double.IsNaN(y) ? 0 : -1) : double.IsNaN(y) ? 1 : x.CompareTo(y));
            return (s.Count(double.IsNaN) < 50, s[49], s[1950]);
        }

        private static double[] _logFact;
        private static double LogFact(int n)
        {
            if (_logFact == null || _logFact.Length <= n) { var a = new double[Math.Max(n + 1, 512)]; for (int i = 1; i < a.Length; i++) { a[i] = a[i - 1] + Math.Log(i); } _logFact = a; }
            return _logFact[n];
        }
        private static double BinomPmf(int k, int n, double p)
        {
            if (p <= 0) { return k == 0 ? 1 : 0; }
            if (p >= 1) { return k == n ? 1 : 0; }
            return Math.Exp(LogFact(n) - LogFact(k) - LogFact(n - k) + k * Math.Log(p) + (n - k) * Math.Log(1 - p));
        }
        private static double BinomCdf(int x, int n, double p) { double s = 0; for (int k = 0; k <= x; k++) { s += BinomPmf(k, n, p); } return s; }

        /// <summary>Clopper-Pearson exact, 95%: with x of n, lo solves P(X &gt;= x | n, p) = 0.025 and hi solves P(X &lt;= x | n, p) = 0.025, each by
        /// bisection on the exact binomial sum to |delta p| &lt;= 1e-12; lo = 0 at x = 0, hi = 1 at x = n.</summary>
        private static (double Lo, double Hi) ClopperPearson(int x, int n)
        {
            double Bisect(Func<double, bool> below)
            {
                double a = 0, b = 1;
                while (b - a > 1e-12) { double mid = (a + b) / 2; if (below(mid)) { a = mid; } else { b = mid; } }
                return (a + b) / 2;
            }
            double lo = x == 0 ? 0 : Bisect(p => 1 - BinomCdf(x - 1, n, p) < 0.025);
            double hi = x == n ? 1 : Bisect(p => BinomCdf(x, n, p) > 0.025);
            return (lo, hi);
        }

        // ================================================================== THE SEEDED REALIZATIONS (DRAWN)
        private sealed class Drawn
        {
            public double[] Sent = new double[1000], Xxii = new double[1000], Named = new double[1000], Hits = new double[1000], Fp = new double[1000], Miss = new double[1000];
            /// <summary>Per president: "sent|xxii|named|hits|fp|miss@president".</summary>
            public Dictionary<string, double[]> Per = new Dictionary<string, double[]>(StringComparer.Ordinal);
            public long Calls;
            public bool BudgetDrewNothing = true;
        }

        /// <summary>
        /// Master seeds 1..1000 run THE PLAY KEY over every measure of V on the leave-one-out lists through <see cref="UsVeto.Draw"/> - the gate's own
        /// draw: the title "&lt;congress&gt;th Congress &lt;measure&gt;", R as REP and D as DEM, the date the chamber's final date (decision_date where it
        /// has none), serving every vote of that chamber. A RECORD BUDGET CLASS measure goes to the draw as a budget vote, which it refuses: the counter
        /// must not move.
        /// </summary>
        private static Drawn RealizationsOf(Record rec, RunResult r)
        {
            var dr = new Drawn();
            var lists = new Dictionary<(Measure, char, char), List<(int, int)>>();
            foreach (Measure j in r.V)
            {
                Setup s = r.Setups[j];
                foreach (char ch in new[] { 'H', 'S' })
                {
                    Chamber c = s.Of(ch);
                    foreach ((char role, string cell) in new[] { ('P', c.PpCell), ('O', c.OpCell) })
                    {
                        lists[(j, ch, role)] = cell == null ? null : r.Fit.Cells[cell].Drawn.Where(o => !s.RollIds.Contains(o.Id)).Select(o => (o.D, o.M)).ToList();
                    }
                }
            }
            long before = UsVeto.DrawCalls;
            foreach (string p in Presidents) { foreach (string k in new[] { "sent", "xxii", "named", "hits", "fp", "miss" }) { dr.Per[k + "@" + p] = new double[1000]; } }
            for (int seed = 1; seed <= 1000; seed++)
            {
                double sent = 0, xxii = 0, named = 0, hits = 0;
                var ps = Presidents.ToDictionary(p => p, p => new double[4], StringComparer.Ordinal);   // sent, xxii, named, hits
                foreach (Measure j in r.V)
                {
                    Setup s = r.Setups[j];
                    string title = F("{0}th Congress {1}", j.Congress, j.Name);
                    bool sentJ = true; int kpH = 0;
                    foreach (char ch in new[] { 'H', 'S' })
                    {
                        Chamber c = s.Of(ch);
                        string when = Ymd(j.F(ch).Date) ? j.F(ch).Date : j.DecisionDate;
                        DateTime date = DateTime.ParseExact(when, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                        UsVeto.Chamber chamber = ch == 'H' ? UsVeto.Chamber.House : UsVeto.Chamber.Senate;
                        int kp = 0, ko = 0;
                        if (c.Budget)
                        {
                            long calls = UsVeto.DrawCalls;
                            kp = UsVeto.Draw(CountryId.USA, true, seed, date, title, chamber, Code(j.PP), c.Vote.PpSeats, lists[(j, ch, 'P')], out _);
                            ko = UsVeto.Draw(CountryId.USA, true, seed, date, title, chamber, Code(j.OP), c.Vote.OpSeats, lists[(j, ch, 'O')], out _);
                            dr.BudgetDrewNothing &= UsVeto.DrawCalls == calls && kp == 0 && ko == 0;
                        }
                        else
                        {
                            if (c.PpCell != null) { kp = UsVeto.Draw(CountryId.USA, false, seed, date, title, chamber, Code(j.PP), c.Vote.PpSeats, lists[(j, ch, 'P')], out _); }
                            if (c.OpCell != null) { ko = UsVeto.Draw(CountryId.USA, false, seed, date, title, chamber, Code(j.OP), c.Vote.OpSeats, lists[(j, ch, 'O')], out _); }
                        }
                        bool pass = c.Undrawn || UsVeto.ChamberOutcome(c.Vote, kp, ko);
                        sentJ &= pass;
                        if (ch == 'H') { kpH = kp; }
                    }
                    if (!sentJ) { continue; }
                    double[] pj = ps[j.President];
                    sent++; pj[0]++;
                    if (r.VXxii.Contains(j)) { xxii++; pj[1]++; }
                    if (UsVeto.NamedByA(s.SppH, s.PpHouseNay ? s.SppH - kpH : 0)) { named++; pj[2]++; if (j.Vetoed) { hits++; pj[3]++; } }
                }
                dr.Sent[seed - 1] = sent; dr.Xxii[seed - 1] = xxii; dr.Named[seed - 1] = named; dr.Hits[seed - 1] = hits; dr.Fp[seed - 1] = named - hits; dr.Miss[seed - 1] = r.VCount - hits;
                foreach (string p in Presidents)
                {
                    double[] pj = ps[p];
                    int vcp = rec.Counted.Count(m => m.President == p && m.Vetoed);
                    dr.Per["sent@" + p][seed - 1] = pj[0]; dr.Per["xxii@" + p][seed - 1] = pj[1]; dr.Per["named@" + p][seed - 1] = pj[2]; dr.Per["hits@" + p][seed - 1] = pj[3];
                    dr.Per["fp@" + p][seed - 1] = pj[2] - pj[3]; dr.Per["miss@" + p][seed - 1] = vcp - pj[3];
                }
            }
            dr.Calls = UsVeto.DrawCalls - before;
            return dr;
        }
        private static bool Ymd(string s) => s != null && s.Length == 10 && DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        private static string Code(string party) => party == "R" ? "REP" : "DEM";

        /// <summary>Mean, sample sd (1/(N-1), in seed order), the 26th and 975th sorted values.</summary>
        private static (double Mean, double Sd, double Lo, double Hi) Stats(double[] x)
        {
            double sum = 0; foreach (double v in x) { sum += v; }
            double mean = sum / x.Length, ss = 0;
            foreach (double v in x) { ss += (v - mean) * (v - mean); }
            double[] s = x.OrderBy(v => v).ToArray();
            return (mean, Math.Sqrt(ss / (x.Length - 1)), s[25], s[974]);
        }

        // ================================================================== THE DECLARED READINGS (each runs the whole rule with one change)
        private static List<Config> Readings() => new List<Config>
        {
            new Config { Key = "R1", Name = "R1 - (A), chamber by chamber as recorded, with the draw and the Vice President, in place of (B)", PremiseA = true },
            new Config { Key = "R2", Name = "R2 - the fit over every contested roll call (R and D bloc stances opposite), each party against its own stance", Cells = "contested" },
            new Config { Key = "R3", Name = "R3 - cells split by Senate procedure, MAJORITY apart from RULE XXII (eight cells)", Cells = "procedure" },
            new Config { Key = "R4", Name = "R4 - cells by party label (R/D) in place of role", Cells = "label" },
            new Config { Key = "R5", Name = "R5 - in-sample, no LOO", InSample = true },
            new Config { Key = "R6", Name = "R6 - selection removed: the PP cells keep only roll calls whose recorded bloc result at K = 0 already passed", SelectionRemoved = true },
            new Config { Key = "R7", Name = "R7 - a joint draw: one index per chamber over the PP cell's roll calls gives both parties' shares", Joint = true },
            new Config { Key = "R8", Name = "R8 - the binomial, K ~ Binomial(S, sum d / sum m), capped (doubles, exact pmf)", Binomial = true },
            new Config { Key = "R9", Name = "R9 - the fitted roll calls read with the PP of THE PRESIDENT ON A DATE", PresidentOnDate = true },
            new Config { Key = "R10", Name = "R10 - present outside the stance and the observation (YEA iff yea > nay; d over yea + nay)", PresentOutside = true },
            new Config { Key = "R11", Name = "R11 - a thin cell pooled with the other role of its chamber", CrossRolePool = true },
            new Config { Key = "R14", Name = "R14 - N_MIN = 20", NMin = 20 },
            new Config { Key = "R15a", Name = "R15 (a) - the override with the passage's full drawn crossers, whether or not they entered the passage", Override = 1 },
            new Config { Key = "R15b", Name = "R15 (b) - the override with a fresh keyed draw (\"|override\") from the same cells", Override = 2 },
            new Config { Key = "R16a", Name = "R16 (a) - the RECORD BUDGET CLASS scored as its kind with the draw (reconciliation as RULE XXII)", BudgetAsKind = true },
            new Config { Key = "R16b", Name = "R16 (b) - the RECORD BUDGET CLASS's roll calls removed from the fit", BudgetOutOfFit = true },
            new Config { Key = "R18", Name = "R18 - debt-limit titles outside the RECORD BUDGET CLASS (scored as their kind with the draw)", DebtOutOfBudget = true },
        };

        // ================================================================== THE US-10 IDENTITIES (US-10 MODE: its kinds, no reconciliation path, no budget class, no Vice President, all rows)
        private sealed class Us10
        {
            public int F3All, OneStance, OneStanceDisagree, Sent, Vetoable, Sendable, ANamed, AVetoed, AVetoes;
            public List<string> Lines = new List<string>();
        }

        private static bool OpposedF3(Measure m, char ch) { Split s = m.F(ch).Of(m.PP); return m.F(ch).Roll && s != null && s.Nay + s.Present > s.Yea; }

        private static Us10 Us10Of(Record rec)
        {
            var u = new Us10();
            foreach (Measure m in rec.All)
            {
                // (i) per measure of F3, at K = 0 with the OP forced YEA on the full seats: (B)'s send verdict against US-10's one stance
                if (OpposedF3(m, 'H') || OpposedF3(m, 'S'))
                {
                    u.F3All++;
                    SeatsOf h = rec.SeatsFor(m.Congress, 'H'), s = rec.SeatsFor(m.Congress, 'S');
                    int ho = h.Of(m.OP), so = s.Of(m.OP);
                    var vh = new UsVeto.ChamberVote { Procedure = UsVeto.Procedure.House, Yea0 = ho, Nay0 = h.Sworn - ho };
                    var vs = new UsVeto.ChamberVote { Procedure = m.Expedited ? UsVeto.Procedure.SenateMajority : UsVeto.Procedure.SenateCloture, Yea0 = so, Nay0 = s.Sworn - so, Sworn = s.Sworn };
                    bool ours = UsVeto.ChamberOutcome(vh, 0, 0) && UsVeto.ChamberOutcome(vs, 0, 0);
                    bool theirs = 2 * ho > h.Sworn && (m.Expedited ? 2 * so > s.Sworn : so >= (3 * s.Sworn + 4) / 5);
                    if (ours) { u.OneStance++; }
                    if (ours != theirs) { u.OneStanceDisagree++; }
                }
                // (ii) (A) at K = 0 over all rows: chamber by chamber as recorded
                bool sent = true;
                foreach (char ch in new[] { 'H', 'S' })
                {
                    if (!m.F(ch).Roll) { continue; }
                    UsVeto.ChamberVote v = RecordedVote(m, ch, m.PP, Default, out _, out _, out _, out _, vicePresident: false, us10: true);
                    sent &= UsVeto.ChamberOutcome(v, 0, 0);
                }
                if (sent)
                {
                    u.Sent++;
                    bool opposed = new[] { 'H', 'S' }.Any(ch => m.F(ch).Of(m.PP) != null && !Yea(m.F(ch).Of(m.PP), Default));
                    if (opposed) { u.Vetoable++; }
                    if (m.Vetoed) { u.Sendable++; }
                }
                // (iii) (a) on the recorded House votes
                Split hs = m.House.Of(m.PP);
                if (m.Vetoed) { u.AVetoes++; }
                if (hs != null && UsVeto.NamedByA(hs.Members, hs.Nay)) { u.ANamed++; if (m.Vetoed) { u.AVetoed++; } }
            }
            return u;
        }

        // ================================================================== THE PRINT: labels
        /// <summary>A printed figure: its value and the label's tail ("FITTED n=.. m=.. 95% [..] BOOT"), or a TOO THIN form alone.</summary>
        private struct Fig
        {
            public string Value, Tail;
            public string Full => Tail == null ? Value : Value + " " + Tail;
        }

        private static int NOf(Fit fit, IEnumerable<string> cells) => cells.SelectMany(c => fit.Cells[c].Drawn).Select(o => o.Id + "#" + o.Role).Distinct(StringComparer.Ordinal).Count();
        private static string PooledSuffix(Fit fit, ICollection<string> cells) =>
            string.Concat(fit.Names.Where(cells.Contains).Where(c => fit.Cells[c].Pooled).Select(c => F(" ({0} pooled with {1})", c, fit.Cells[c].Partner)));
        private static List<string> CellsRead(Setup s, bool house = true, bool senate = true)
        {
            var l = new List<string>();
            if (house) { l.Add(s.H.PpCell); l.Add(s.H.OpCell); }
            if (senate) { l.Add(s.S.PpCell); l.Add(s.S.OpCell); }
            return l.Where(c => c != null).Distinct(StringComparer.Ordinal).ToList();
        }
        private static List<string> CellsRead(RunResult r, IEnumerable<Measure> set) => set.SelectMany(j => CellsRead(r.Setups[j])).Distinct(StringComparer.Ordinal).ToList();

        /// <summary>A derived figure's label: UNFITTED where it reads an UNFITTED cell; UNFITTED m=0 over an empty set; else the value with n (distinct
        /// observations by identity and role over the union of the full drawn lists it reads), each pooled cell named, m, and the BOOT interval.</summary>
        private static Fig Derived(string value, Fit fit, List<string> cells, int? m, double[] boot, Func<double, string> fmt)
        {
            if (cells.Any(c => !fit.Cells[c].Fitted)) { return new Fig { Value = "UNFITTED" }; }
            if (m == 0) { return new Fig { Value = "UNFITTED m=0" }; }
            return new Fig { Value = value, Tail = F("FITTED n={0}{1}{2} 95% {3}", NOf(fit, cells), PooledSuffix(fit, cells), m.HasValue ? " m=" + m.Value.ToString(CultureInfo.InvariantCulture) : "", Interval(boot, fmt)) };
        }

        /// <summary>A RECORD BUDGET CLASS measure's figure: NOT FITTED, byte for byte the block's two forms - "0/1" or "1/1 at K = 0, no draw".</summary>
        private static string NoDraw(Q v) => v.IsZero ? "0/1" : "1/1 at K = 0, no draw";
        /// <summary>THE OUTCOME as THE DECISION spells it ("S1 + S2"); the digest line alone writes "S1+S2".</summary>
        private static string Spelled(string outcome) => outcome.Replace("+", " + ");

        private static string Short(string digest) => digest == null || digest.Length < 8 ? digest ?? "-" : digest.Substring(0, 8);

        // ================================================================== THE MEASUREMENT: the run, its assertions, the document, the catalog, the ruling record
        private static Product MeasureRun(StringBuilder sb, Action<bool, string> Check)
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            var doc = new StringBuilder();
            void L(string line = "") { doc.Append(line).Append('\n'); }

            // ---------------------------------------------------------- the record, the pins, the block
            byte[] csvBytes = File.ReadAllBytes(Path.Combine(Root, CsvRelative));
            Record rec = ReadRecord(csvBytes);
            Check(rec.SourceDigest == SourcePin, F("the SOURCE PIN: the record's SHA-256 {0} is the pin {1} - a run on other bytes is a DEFECT until an amendment naming Elias's ruling re-pins it", Short(rec.SourceDigest), Short(SourcePin)));
            // the prep tool's git blob as git computes it (`git hash-object`, the path's clean conversion applied): an unstaged edit is caught, a CRLF checkout is not a defect
            string prepBlob = TryGitBytes("hash-object -- " + PrepRelative, out byte[] hashOut) ? Encoding.ASCII.GetString(hashOut).Trim() : "git hash-object failed";
            Check(prepBlob == PrepPin, F("the PREP PIN: {0}'s git blob {1} is the pin {2}", PrepRelative, Short(prepBlob), Short(PrepPin)));
            string plan = Lf(File.ReadAllText(Path.Combine(Root, PlanRelative), new UTF8Encoding(false))).TrimStart('﻿');
            string blockDigest = BlockDigestOf(plan);
            (string Digest, string Why) last = RuleDigests[RuleDigests.Length - 1];
            Check(blockDigest == last.Digest, blockDigest == last.Digest ? F("the block's digest {0} is the RuleDigests list's last ({1})", Short(blockDigest), last.Why)
                : F("the block's digest {0} is not the RuleDigests list's last ({1}) - the block edited without a listed digest; a change without a ruling fails", Short(blockDigest), Short(last.Digest)));
            bool gitOk = TryGitBytes("show :" + PlanRelative, out byte[] indexBytes);
            string indexDigest = gitOk ? BlockDigestOf(Encoding.UTF8.GetString(indexBytes).TrimStart('﻿')) : "-";   // the blob's raw lines: a CR in them is a failure
            Check(gitOk && indexDigest == blockDigest, F("the block's digest read from the index blob's raw bytes ({0}) equals the working copy's ({1})", Short(indexDigest), Short(blockDigest)));
            Check(RuleDigests[0].Why == "preregistered (§832)" && RuleDigests.Skip(1).All(d => d.Why.StartsWith("AMENDED (", StringComparison.Ordinal)), "the RuleDigests list: the preregistered digest first (§832), then one AMENDED entry per ruling");
            foreach (string d in rec.Defects) { Check(false, d); }
            string unpredSet = "{" + string.Join(", ", rec.UnpredictableRows.Select(m => m.Id)) + "}";
            Check(rec.UnpredictableRows.Select(m => m.Id).SequenceEqual(UnpredictableAsserted), F("the UNPREDICTABLE set is {0} - asserted exactly {{119|H.R. 131, 119|H.R. 504}} (H2)", unpredSet));
            Check(rec.Counted.All(m => m.PP == "R" || m.PP == "D"), "every counted row's PP is R or D");
            int quorumBad = rec.RollOrder.Count(rc => { Final f = rc.Final; int voting = new[] { f.R, f.D, f.I }.Sum(s => s?.Voting ?? 0), members = new[] { f.R, f.D, f.I }.Sum(s => s?.Members ?? 0); return voting < members / 2 + 1; });
            Check(quorumBad == 0, F("QUORUM: on every roll call of a counted measure ({0}) VOTING over R, D and I is at least floor(MEMBERS / 2) + 1 - {1} short", rec.RollOrder.Count, quorumBad));

            // SEATS against US_VETO_BACKTEST.md's table
            string[] backtest = File.ReadAllLines(Path.Combine(Root, BacktestDocRelative), Encoding.UTF8);
            Check(backtest.Length > 0 && backtest[0].Contains("sha256 " + SourcePin), "US-10's document (§802) is stamped with the source pin - its record is the one this rule reads");
            int seatsBad = 0, seatsRows = 0;
            foreach (string line in backtest)
            {
                System.Text.RegularExpressions.Match mt = System.Text.RegularExpressions.Regex.Match(line, @"^\| (\d+)th \| (\d+) - (\d+) \((\d+)\) \| (\d+) - (\d+) \((\d+)\) \|$");
                if (!mt.Success) { continue; }
                seatsRows++;
                int G(int i) => int.Parse(mt.Groups[i].Value, CultureInfo.InvariantCulture);
                int c = G(1);
                if (!rec.Seats.TryGetValue(c + " house", out SeatsOf h) || !rec.Seats.TryGetValue(c + " senate", out SeatsOf s) || h.R != G(2) || h.D != G(3) || h.Sworn != G(4) || s.R != G(5) || s.D != G(6) || s.Sworn != G(7)) { seatsBad++; }
            }
            Check(seatsRows > 0 && seatsRows == rec.Seats.Count / 2 && seatsBad == 0, F("SEATS OF A CONGRESS equal US_VETO_BACKTEST.md's seats table: {0} Congresses, {1} differing", seatsRows, seatsBad));
            Check(rec.Seats.Where(kv => kv.Key.EndsWith(" senate", StringComparison.Ordinal)).All(kv => (2 * kv.Value.Sworn + 2) / 3 >= (3 * kv.Value.Sworn + 4) / 5),
                "with every senator voting, the override's two thirds is never below cloture's three fifths of the sworn");

            // B1's yardstick, from the Sejm's CSV exactly as us_veto_backtest.pl
            int b1Named = 0, b1Vetoed = 0;
            foreach (Dictionary<string, string> p in ReadCsv(File.ReadAllText(Path.Combine(Root, PolandCsvRelative), Encoding.UTF8), PolandCsvRelative))
            {
                if (p["veto_exempt"].Length > 0) { continue; }
                string o = p["outcome"];
                if (!(o == "vetoed" || o.StartsWith("signed", StringComparison.Ordinal) || o.StartsWith("referred to Tribunal", StringComparison.Ordinal))) { continue; }
                if (2 * int.Parse(p["pis_no"], CultureInfo.InvariantCulture) <= int.Parse(p["pis_members"], CultureInfo.InvariantCulture)) { continue; }
                b1Named++; if (o == "vetoed") { b1Vetoed++; }
            }
            Check(b1Named == 166 && b1Vetoed == 45, F("B1'S YARDSTICK recomputed from the Sejm's CSV: {0} vetoed of {1} named (45 of 166)", b1Vetoed, b1Named));

            // UNIT against PresidentialVeto.Draw on eight fixed inputs; Applies the USA's alone; a contested budget act draws nothing; no other country draws
            var unitInputs = new (int Seed, string Date, string Title)[]
            {
                (777, "2024-03-15", "Ustawa o zmianie ustawy"), (1, "2023-12-01", "Budzet"), (424242, "2025-06-30", "Ustawa"), (0, "2024-01-01", ""),
                (123456789, "2024-02-29", "Ustawa o podatku dochodowym"), (-5, "2026-10-16", "Test"), (2147483647, "2019-11-12", "Ustawa o KRS"), (42, "2020-05-05", "A"),
            };
            var unitLines = new List<string>();
            bool unitOk = true;
            foreach (var u in unitInputs)
            {
                DateTime when = DateTime.ParseExact(u.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                double ours = UsVeto.Unit(u.Seed.ToString(CultureInfo.InvariantCulture) + "|Poland|" + u.Date + "|" + u.Title), theirs = PresidentialVeto.Draw(u.Seed, CountryId.Poland, when, u.Title);
                unitOk &= ours == theirs;
                unitLines.Add(F("{0}|Poland|{1}|{2} -> {3}", u.Seed, u.Date, u.Title, ours.ToString("G17", CultureInfo.InvariantCulture)));
            }
            Check(unitOk, F("UNIT equals PresidentialVeto.Draw on 8 fixed Poland-format inputs outside any plant scope ({0})", unitLines[0]));
            var countries = (CountryId[])Enum.GetValues(typeof(CountryId));
            Check(countries.Length == 6 && countries.All(c => UsVeto.Applies(c) == (c == CountryId.USA)), "UsVeto.Applies is true for CountryId.USA alone, over all six values");
            long callsBefore = UsVeto.DrawCalls;
            // a planted contested budget act: the PP against, the OP for, one crosser from reversing its K = 0 outcome, planted at the largest share
            var marginVote = new UsVeto.ChamberVote { Procedure = UsVeto.Procedure.House, Yea0 = 217, Nay0 = 218, PpSeats = 218, OpSeats = 217 };
            var bigList = new List<(int, int)> { (0, 200), (100, 200) };
            int kpBudget, koBudget;
            using (UsVeto.PlantIndex(int.MaxValue))
            {
                kpBudget = UsVeto.Draw(CountryId.USA, true, 777, new DateTime(2026, 3, 1), "A contested appropriation", UsVeto.Chamber.House, "REP", 218, bigList, out _);
                koBudget = UsVeto.Draw(CountryId.USA, true, 777, new DateTime(2026, 3, 1), "A contested appropriation", UsVeto.Chamber.House, "DEM", 217, bigList, out _);
            }
            bool marginOne = MarginOf(new Chamber { Ch = 'H', Vote = marginVote, PpCell = "HP", OpCell = "HO" }) == "+1";
            Check(marginOne && kpBudget == 0 && koBudget == 0 && UsVeto.ChamberOutcome(marginVote, kpBudget, koBudget) == UsVeto.ChamberOutcome(marginVote, 0, 0) && UsVeto.DrawCalls == callsBefore,
                F("a planted contested budget act at margin 1, planted at the largest share, draws zero: K {0} and {1}, its K = 0 outcome kept, the draw counter moved by {2}", kpBudget, koBudget, UsVeto.DrawCalls - callsBefore));
            int otherK = 0;
            foreach (CountryId c in countries.Where(c => c != CountryId.USA)) { otherK += UsVeto.Draw(c, false, 777, new DateTime(2026, 3, 1), "A bill", UsVeto.Chamber.House, "REP", 218, bigList, out _); }
            Check(otherK == 0 && UsVeto.DrawCalls == callsBefore, "the five other countries draw nothing - the counter reads 0 on their path");

            // ---------------------------------------------------------- the default run
            Fit fit = FitOf(rec, Default);
            RunResult main = RunOf(rec, Default, fit);
            Check(fit.All.All(o => 2L * o.D <= o.M), F("every share d/m <= 1/2 - {0}", fit.All.Where(o => 2L * o.D > o.M).Select(o => F("violated by {0} {1} {2} of {3}", o.Id, o.Role, o.D, o.M)).FirstOrDefault() ?? "none above"));
            Check(!fit.All.Any(o => o.Rc.Owner.Unpredictable) && rec.UnpredictableRows.All(m => !m.House.Roll && !m.Senate.Roll), "the UNPREDICTABLE measures own no observation");
            var budgetDraws = main.Setups.Values.Where(s => s.H.Budget && (s.H.PpCell != null || s.H.OpCell != null || s.S.PpCell != null || s.S.OpCell != null)).Select(s => s.M.Id).ToList();
            Check(budgetDraws.Count == 0, F("no RECORD BUDGET CLASS measure draws on the record - {0}", budgetDraws.Count == 0 ? "none does" : "drawing: " + string.Join(", ", budgetDraws)));
            Check(main.IdentityA, "THE VETO's identity: under the cap (a) fires exactly where the PP's House stance is NAY and S_PP,H >= 1 - every measure of V");
            Check(main.V.All(j => main.Loo[j].Named.Equals(main.Loo[j].Sent)) && main.T.ENamed.Equals(main.T.ESent), "under (B) NAMED = VETOABLE SENT on every measure, and E[named] = E[vetoable sent]");

            // the histograms of K against the pair-by-pair enumeration (UsVeto.PassProbability on the lists)
            int enumBad = 0, enumChecked = 0;
            foreach (Measure j in main.V)
            {
                Setup s = main.Setups[j];
                foreach (Chamber ch in new[] { s.H, s.S })
                {
                    if (ch.Undrawn) { continue; }
                    List<(int, int)> Lst(string cell) => cell == null ? null : fit.Cells[cell].Drawn.Where(o => !s.RollIds.Contains(o.Id)).Select(o => (o.D, o.M)).ToList();
                    var v = new UsVeto.ChamberVote { Procedure = ch.Vote.Procedure, Yea0 = ch.Vote.Yea0, Nay0 = ch.Vote.Nay0, Sworn = ch.Vote.Sworn, VicePresident = ch.Vote.VicePresident, PpSeats = ch.Vote.PpSeats, OpSeats = ch.Vote.OpSeats, PpList = Lst(ch.PpCell), OpList = Lst(ch.OpCell) };
                    UsVeto.Odds hist = UsVeto.PassProbability(v);
                    int[] kp = UsVeto.CrossersOf(v.PpList, v.PpSeats), ko = UsVeto.CrossersOf(v.OpList, v.OpSeats);
                    long brute = 0;
                    foreach (int a in kp) { foreach (int b in ko) { if (UsVeto.ChamberOutcome(v, a, b)) { brute++; } } }
                    Q mine = ch.Ch == 'H' ? main.Loo[j].PH : main.Loo[j].PS;
                    enumChecked++;
                    if (hist.Passing != brute || hist.Total != (long)kp.Length * ko.Length || !Q.Of(hist).Equals(mine)) { enumBad++; }
                }
            }
            Check(enumBad == 0, F("P(pass) on the histograms of K equals the enumeration of index pairs - {0} chambers checked, {1} differing", enumChecked, enumBad));

            // the second run in one session, in the same domain
            RunResult again = RunOf(rec, Default);
            string decisiveText = DecisiveText(rec, main);
            string decisive = Sha256Hex(decisiveText);
            Check(Sha256Hex(DecisiveText(rec, again)) == decisive, F("a second run in this session gives the same decisive digest {0}", Short(decisive)));

            // the DIAGNOSTIC: full-cell, in-sample
            var diag = new Dictionary<Measure, Score>();
            foreach (Measure j in main.V) { diag[j] = ScoreOf(fit, main.Setups[j], _ => 1.0, true, null); }

            // BOOT, the realizations, INFLUENCE
            int[][] keyed = PicksOf(rec.Counted.Count);
            Boot boot = BootOf(rec, main, true, false, keyed);
            Check(boot.IdentityA, "THE VETO's identity holds in every bootstrap replicate");
            Drawn drawn = RealizationsOf(rec, main);
            Check(drawn.BudgetDrewNothing, "the realizations: every RECORD BUDGET CLASS measure refused by UsVeto.Draw as a budget vote - its counter unmoved, K = 0");
            var drawnChecks = new (string Name, double[] X, Q Exact)[] { ("E[vetoable sent]", drawn.Sent, main.T.ESent), ("E_XXII[vetoable sent]", drawn.Xxii, main.T.EXxii), ("E[hits]", drawn.Hits, main.T.EHits) };
            foreach (var dc in drawnChecks)
            {
                if (main.S3) { continue; }   // a figure reading an UNFITTED cell has no exact value to check
                var st = Stats(dc.X);
                double exact = dc.Exact.ToDouble();
                bool ok = st.Sd == 0 ? st.Mean == exact : Math.Abs(st.Mean - exact) <= 4 * st.Sd / Math.Sqrt(1000);
                Check(ok, F("THE SEEDED REALIZATIONS: {0}'s mean {1} over 1000 worlds agrees with the exact {2} within 4 sd / sqrt(1000) = {3}", dc.Name,
                    st.Mean.ToString("0.0000", CultureInfo.InvariantCulture), exact.ToString("0.0000", CultureInfo.InvariantCulture), (4 * st.Sd / Math.Sqrt(1000)).ToString("0.0000", CultureInfo.InvariantCulture)));
            }
            var influence = new List<(RollCall Rc, string Outcome)>();
            var influenceEmpty = new EmptyLog();
            bool influenceIdentity = true;
            foreach (RollCall rc in fit.Fitted.OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                RunResult rr = RunOf(rec, Default, fit, new HashSet<string>(StringComparer.Ordinal) { rc.Id });
                foreach (var kv in rr.Empty.By) { for (int i = 0; i < kv.Value; i++) { influenceEmpty.Add(rc.Id + " out: " + kv.Key); } }
                influenceIdentity &= rr.IdentityA;
                if (rr.Outcome != main.Outcome) { influence.Add((rc, rr.Outcome)); }
            }

            // the readings
            var readings = new Dictionary<string, (Config Cfg, RunResult R, Boot Boot)>(StringComparer.Ordinal);
            foreach (Config c in Readings())
            {
                RunResult rr = RunOf(rec, c);
                Boot rb = BootOf(rec, rr, c.Override != 0, false, keyed);
                readings[c.Key] = (c, rr, rb);
                Check(rr.IdentityA && rb.IdentityA, F("THE VETO's identity holds under {0}, in its run and every replicate", c.Key));
            }
            Check(influenceIdentity, "THE VETO's identity holds in every INFLUENCE removal's run");
            Boot cellBoot = BootOf(rec, main, false, true, keyed);
            Check(cellBoot.IdentityA, "THE VETO's identity holds in every R17 cell-bootstrap replicate");
            Us10 us10 = Us10Of(rec);

            // ---------------------------------------------------------- the US-10 identities, against its document
            string oneStanceLine = backtest.FirstOrDefault(l => l.StartsWith(F("- Of the {0} measures the president's party opposed in a chamber", us10.F3All), StringComparison.Ordinal));
            Check(us10.OneStanceDisagree == 0 && oneStanceLine != null && oneStanceLine.Contains(F("sends **{0}**", us10.OneStance)),
                F("US-10's record (s802), not this rule's figure - (i) per measure of F3 ({0}), at K = 0 with the OP forced YEA on full seats, (B)'s send verdict equals US-10's one-stance verdict ({1} differing), the count {2} the document's", us10.F3All, us10.OneStanceDisagree, us10.OneStance));
            Check(backtest.Any(l => l.Contains(F("it sends {0}; of those, **{1}** are VETOABLE", us10.Sent, us10.Vetoable))) && backtest.Any(l => l.Contains(F("it could have sent **{0}** to the president", us10.Sendable))),
                F("US-10's record (s802), not this rule's figure - (ii) (A) at K = 0 over all rows: {0} sent, {1} vetoable, {2} vetoes sendable, as the document's chamber-by-chamber counts", us10.Sent, us10.Vetoable, us10.Sendable));
            string aRow = backtest.FirstOrDefault(l => l.StartsWith("| (a) B1 transposed", StringComparison.Ordinal) && l.Contains("| the three terms |"));
            Check(aRow != null && aRow.Contains(F("| the three terms | {0} | {1} (", us10.ANamed, us10.AVetoed)) && aRow.Contains(F(") | {0} | {1} |", us10.AVetoes - us10.AVetoed, us10.ANamed - us10.AVetoed)),
                F("US-10's record (s802), not this rule's figure - (iii) (a) on the recorded House votes: {0} named, {1} vetoed among them, {2} outside, {3} named not vetoed - the document's (a) pooled row", us10.ANamed, us10.AVetoed, us10.AVetoes - us10.AVetoed, us10.ANamed - us10.AVetoed));

            // ---------------------------------------------------------- THE DECISION's figures, the verdict
            Totals T = main.T;
            List<string> cellsV = CellsRead(main, main.V), cellsX = CellsRead(main, main.VXxii), cellsE = CellsRead(main, main.VExp);
            int vN = main.V.Count, vX = main.VXxii.Count, vE = main.VExp.Count, vC = main.VCount;
            int vetoesInV = main.V.Count(j => j.Vetoed);
            Q b1 = new Q(45, 166);
            var unfit = new Fig { Value = "UNFITTED" };   // when S3 fires, S1 and S2 are not evaluated and their figures print UNFITTED
            Fig fSent = main.S3 ? unfit : Derived(Cnt(T.ESent), fit, cellsV, vN, boot.Tot["e_sent"], Cnt);
            Fig fXxii = main.S3 ? unfit : vX == 0 ? new Fig { Value = "UNFITTED m=0" } : Derived(Cnt(T.EXxii), fit, cellsX, vX, boot.Tot["e_xxii"], Cnt);
            Fig fNamed = main.S3 ? unfit : Derived(Cnt(T.ENamed), fit, cellsV, vN, boot.Tot["e_named"], Cnt);
            Fig fHits = main.S3 ? unfit : Derived(Cnt(T.EHits), fit, cellsV, vN, boot.Tot["e_hits"], Cnt);
            Fig fFp = main.S3 ? unfit : Derived(Cnt(T.ENamed - T.EHits), fit, cellsV, vN, boot.Tot["e_fp"], Cnt);
            Fig fMiss = main.S3 ? unfit : Derived(Cnt(Q.Of(vC) - T.EHits), fit, cellsV, vC, boot.Tot["e_miss"], Cnt);
            Fig fPrec = main.S3 ? unfit : T.ENamed.IsZero ? new Fig { Value = "UNDEFINED (denominator 0)" } : Derived(Pct(T.EHits / T.ENamed), fit, cellsV, vN, boot.Tot["precision"], Pct);
            Fig fHit = main.S3 ? unfit : vC == 0 ? new Fig { Value = "no counted veto (H2)" } : Derived(Pct(T.EHits / Q.Of(vC)), fit, cellsV, vC, boot.Tot["hitrate"], Pct);
            Fig fNoneX = main.S3 ? unfit : vX == 0 ? new Fig { Value = "UNFITTED m=0" } : Derived(Pct(T.PNoneXxii), fit, cellsX, vX, boot.Tot["pnone_xxii"], Pct);
            Fig fNone = main.S3 ? unfit : Derived(Pct(T.PNone), fit, cellsV, vN, boot.Tot["pnone"], Pct);
            Fig fNoneE = main.S3 ? unfit : vE == 0 ? new Fig { Value = "UNFITTED m=0" } : Derived(Pct(T.PNoneExp), fit, cellsE, vE, boot.Tot["pnone_exp"], Pct);
            string fragile = influence.Count == 0 ? "no single roll call flips the outcome" : F("FRAGILE: {0} of {1} roll calls each flip the outcome", influence.Count, fit.Fitted.Count);
            var prec = Bounds(boot.Tot["precision"]);
            bool precInterval = !main.S3 && !T.ENamed.IsZero && prec.Has;
            var r13 = new List<(string Key, string Outcome)>();
            if (!main.S3)
            {
                string Out(bool s1, bool s2) => s1 && s2 ? "S1+S2" : s1 ? "S1" : s2 ? "S2" : "INSTALL";
                r13.Add(("R13 (S2 at E_XXII[vetoable sent] = 0 exactly)", Out(main.S1, main.S2Empty || T.EXxii.IsZero)));
                r13.Add(("R13 (S2 at E_XXII[vetoable sent] < 1)", Out(main.S1, main.S2Empty || T.EXxii < Q.One)));
                r13.Add(("R13 (S2 over every kind)", Out(main.S1, vN == 0 || Q.Of(2) * T.PNone >= Q.One)));
                r13.Add(("R13 (S1 on the BOOT upper bound)", Out(precInterval && Q.Of(166) * Q.FromDouble(prec.Hi) < Q.Of(45), main.S2)));
                r13.Add(("R13 (S1 on the BOOT lower bound)", Out(precInterval && Q.Of(166) * Q.FromDouble(prec.Lo) < Q.Of(45), main.S2)));
            }
            // every declared reading and its outcome, in R-number order; a reading whose outcome differs is named in the verdict line
            var readingOutcomes = new List<(string Key, string Outcome)>();
            foreach (string k in new[] { "R1", "R2", "R3", "R4", "R5", "R6", "R7", "R8", "R9", "R10", "R11" }) { readingOutcomes.Add((k, readings[k].R.Outcome)); }
            readingOutcomes.Add(("R12", main.Outcome));   // the drawn veto keeps the sending and the precision: S1 and S2 read the same figures
            readingOutcomes.AddRange(r13);
            foreach (string k in new[] { "R14", "R15a", "R15b", "R16a", "R16b" }) { readingOutcomes.Add((ReadingName(k), readings[k].R.Outcome)); }
            readingOutcomes.Add(("R17", main.Outcome));   // intervals only
            readingOutcomes.Add(("R18", readings["R18"].R.Outcome));
            var differing = readingOutcomes.Where(x => x.Outcome != main.Outcome).ToList();
            string readingsClause = differing.Count == 0 ? "the same under every declared reading" : "the outcome differs under " + string.Join(", ", differing.Select(x => x.Key)) + ": " + string.Join(", ", differing.Select(x => Spelled(x.Outcome)));
            string verdict;
            if (main.Outcome == "INSTALL")
            {
                verdict = F("By R-US17's rule (c) as ruled (H1-H3), the draw installs: under Rule XXII a bloc Congress with the draw sends {0} vetoable measures of the record {1} (all {2} were sent on the record), P(none under Rule XXII) {3}; over every kind it sends and names {4}, {5} of them vetoed {6}; precision {7} against B1's 27.1 % (45 of 166), the draw-free share of V {8} of {9}; hit rate {10} of the {11} counted vetoes {12}; every cell fitted; H.R. 131 and H.R. 504 (119th) declared unpredictable, in no count (H2); {13}; {14} - held INSTALLED-PENDING-E4: the rates go to Elias before US-21 wires them (E4).",
                    fXxii.Value, fXxii.Tail, vX, fNoneX.Full, fNamed.Full, fHits.Value, fHits.Tail, fPrec.Full, vetoesInV, vN, fHit.Value, vC, fHit.Tail, fragile, readingsClause);
            }
            else
            {
                var stops = new List<string>();
                if (main.S3) { stops.Add("S3, TOO THIN: " + string.Join("; ", fit.Names.Where(n => !fit.Cells[n].Fitted).Select(n => TooThin(fit.Cells[n], Default)))); }
                if (main.S1) { stops.Add(F("S1, FAR MORE: E[hits] {0} of E[named] {1}, precision {2} below B1's 27.1 % (45 of 166)", fHits.Full, fNamed.Full, fPrec.Full)); }
                if (main.S2) { stops.Add(main.S2Empty ? "S2, NEVER SENDS - case \"no Rule XXII measure on the veto path\": V_XXII is empty, its figures UNFITTED m=0" : F("S2, NEVER SENDS - case \"never sends\": P(NONE_XXII) {0}, at least one half", fNoneX.Full)); }
                verdict = F("R-US17's rule (c) does not install: {0}; {1}; {2} - asked.", string.Join("; ", stops), fragile, readingsClause);
            }

            // ---------------------------------------------------------- THE RULING RECORD and its mirror
            Check(new[] { "NONE", "INSTALLED-PENDING-E4", "INSTALLED", "ASKED", "RULED" }.Contains(RulingState), "the ruling record's state is one of NONE, INSTALLED-PENDING-E4, INSTALLED, ASKED, RULED");
            if (RulingState == "NONE") { Check(false, F("the ruling record is NONE - the first-run commit registers the outcome {0} and the decisive digest {1}", Spelled(main.Outcome), decisive)); }
            else
            {
                Check(RulingOutcome == main.Outcome, F("{0}: the outcome {1} is the registered one ({2}) - a changed outcome fails until it is registered ASKED with the old and new outcomes and digests", RulingState, Spelled(main.Outcome), Spelled(RulingOutcome)));
                Check(RulingDecisiveDigest == decisive, F("{0}: the decisive digest {1} is the registered one ({2}) - with the outcome unchanged, a commit re-pins it naming the old, the new, its cause and \"outcome unchanged\"", RulingState, Short(decisive), Short(RulingDecisiveDigest)));
                if (RulingState == "INSTALLED-PENDING-E4" || RulingState == "INSTALLED") { Check(RulingOutcome == "INSTALL", F("{0} is registered only on an INSTALL", RulingState)); }
                if (RulingState == "INSTALLED") { Check(RulingE4Section.Length > 0, "INSTALLED names the section that recorded the reply to Elias carrying the rates (E4)"); }
                if (RulingState == "RULED") { Check(Buildable(RulingOption) && RulingRuled.Length > 0, F("RULED: the ruling's words recorded and an option that can be built ('{0}')", RulingOption)); }
                Check((RulingPriorOutcome.Length == 0) == (RulingPriorDigest.Length == 0), "a changed outcome's registration carries the old outcome and the old digest together");
                Check(RulingPriorOutcome.Length == 0 || ((RulingState == "ASKED" || RulingState == "RULED") && (RulingPriorOutcome != RulingOutcome || RulingPriorDigest != RulingDecisiveDigest)), "a prior pair is registered only ASKED (or RULED after it), and differs from the current outcome or digest");
            }
            string mirror = MirrorOf();
            string planLine = MirrorLineOf(plan);
            Check(RulingState == "NONE" || planLine == mirror, F("the plan's line after the block mirrors the ruling record: '{0}'{1}", Clip(planLine), planLine == mirror || RulingState == "NONE" ? "" : F(" - the record says '{0}'", mirror)));

            // ---------------------------------------------------------- THE DOCUMENT
            L(F("<!-- GENERATED by PoliSim.EditorTools.UsVetoDrawCheck.Write from {0} (sha256 {1}). DO NOT EDIT BY HAND. Re-derive: -executeMethod PoliSim.EditorTools.UsVetoDrawCheck.Write -->", CsvRelative, rec.SourceDigest));
            L("# R-US17's rule (c) on the record - the draw of defectors on the US veto path, its first run");
            L();
            L(F("**Stamp.** SourceDigest `{0}`; the pins: source `{1}` ({2}), prep `{3}` (the prep tool's git blob `{4}`: {5}); the block's digest `{6}` (the RuleDigests list's last: {7}); the decisive digest `{8}`; the ruling record: {9}{10}.",
                rec.SourceDigest, SourcePin, rec.SourceDigest == SourcePin ? "equal" : "DIFFERS", PrepPin, prepBlob, prepBlob == PrepPin ? "equal" : "DIFFERS", blockDigest, last.Why, decisive, RulingState == "NONE" ? "NONE" : mirror.Replace("**Ruling record:** ", "").TrimEnd('.'), RulingPriorOutcome.Length == 0 ? "" : F(" - registered after a changed outcome: {0} (digest {1}) -> {2} (digest {3})", Spelled(RulingPriorOutcome), RulingPriorDigest, Spelled(RulingOutcome), RulingDecisiveDigest)));
            L();
            L("The rule is the fenced block of `docs/specs/USA_STAGE_PLAN.md` between its R-US17 RULE markers (Elias's rulings H1-H3; preregistered §832, amended §833). Parties vote as blocs; only on a measure on the veto path does a party draw crossers from the recorded crossing of its role in that chamber, and rule (a) then decides the veto. Every FITTED figure prints `FITTED n=<n> [m=<m>] 95% [<lo>, <hi>] CP|BOOT` (CP: Clopper-Pearson for a cell proportion; BOOT: the record bootstrap, 2000 replicates). Counts of the record are not fitted. DRAWN figures are the seed noise of single games, a check on the exact figures. Exact rationals decide; percentages round half up.");
            L();

            // 1. THE RECORD
            L("## 1. The record");
            L();
            foreach (string p in Presidents.Concat(new[] { "" }))
            {
                var all = rec.All.Where(m => p.Length == 0 || m.President == p).ToList();
                var cnt = rec.Counted.Where(m => p.Length == 0 || m.President == p).ToList();
                int cv = cnt.Count(m => m.Vetoed), un = all.Count(m => m.Unpredictable);
                L(F("- {0}: {1} measures and {2} vetoes in all; {3} counted measures; {4}", p.Length == 0 ? "the three terms" : p, all.Count, all.Count(m => m.Vetoed), cnt.Count,
                    cv == 0 ? F("0 counted vetoes; {0} declared unpredictable", un) : F("{0} counted vetoes{1}", cv, un > 0 ? F("; {0} declared unpredictable", un) : "")));
            }
            L();
            L(UnpredictableLine(rec));
            L();
            var f3Counted = rec.Counted.Where(m => OpposedF3(m, 'H') || OpposedF3(m, 'S')).ToList();
            var vNotF3 = main.V.Where(j => !f3Counted.Contains(j)).ToList();
            var f3NotV = f3Counted.Where(j => !main.V.Contains(j)).ToList();
            L(F("- THE VETO PATH (record), V: **{0}** counted measures, beside F3's {1} (US-10's count over all rows). In V and not in F3: {2}. In F3 and not in V: {3}.", vN, us10.F3All,
                vNotF3.Count == 0 ? "none" : string.Join(", ", vNotF3.Select(m => m.Id)), f3NotV.Count == 0 ? "none" : string.Join(", ", f3NotV.Select(m => m.Id))));
            L(F("- V_XXII, V's measures under RULE XXII: **{0}**. V_EXP, V's measures of an EXPEDITED kind: {1}. Counted vetoes: {2} of the {3} counted measures; in V, {4} of {5}.", vX, vE, vC, rec.Counted.Count, vetoesInV, vN));
            L();
            L("THE RECORD BUDGET CLASS, every counted match with its test:");
            L();
            foreach (Measure m in rec.Counted.Where(x => x.BudgetMatches.Count > 0)) { foreach (string t in m.BudgetMatches) { L(F("- {0}{1}: {2}", m.Id, main.V.Contains(m) ? " (in V)" : "", t)); } }
            L();
            L(F("NEAR MISSES (a counted title containing APPROPRIATION, RECONCILIATION or BUDGET that matches no test): {0}", rec.NearMisses.Count == 0 ? "none." : rec.NearMisses.Count.ToString(CultureInfo.InvariantCulture)));
            foreach (string nm in rec.NearMisses) { L("- " + nm); }
            L();

            // 2. THE CELLS
            L("## 2. The cells");
            L();
            var roleWords = new Dictionary<string, string> { { "HP", "the president's party standing NAY in the House" }, { "HO", "the other party standing YEA on those House roll calls" }, { "SP", "the president's party standing NAY in the Senate" }, { "SO", "the other party standing YEA on those Senate roll calls" } };
            foreach (string n in fit.Names)
            {
                Cell cell = fit.Cells[n];
                L(F("### {0} - {1}", n, roleWords[n]));
                L();
                if (!cell.Fitted) { L("- " + TooThin(cell, Default)); }
                else
                {
                    L(F("- n_own {0}, n {1}: FITTED, {2}", cell.NOwn, cell.N, cell.Pooled ? F("POOLED with {0} - it draws from the union", cell.Partner) : "not pooled - it draws from its own list"));
                    int x1 = cell.Drawn.Count(o => o.D >= 1), x10 = cell.Drawn.Count(o => 10L * o.D >= o.M);
                    string suffix = cell.Pooled ? F(" ({0} pooled with {1})", n, cell.Partner) : "";
                    (double lo1, double hi1) = ClopperPearson(x1, cell.N); (double lo10, double hi10) = ClopperPearson(x10, cell.N);
                    Q mean = Q.Zero; foreach (Obs o in cell.Drawn) { mean += new Q(o.D, o.M); } mean = mean / Q.Of(cell.N);
                    L(F("- q_any (the share of observations with d >= 1): {0} FITTED n={1}{2} 95% [{3}, {4}] CP", Pct(new Q(x1, cell.N)), cell.N, suffix, Pct(lo1), Pct(hi1)));
                    L(F("- q10 (the share with 10d >= m): {0} FITTED n={1}{2} 95% [{3}, {4}] CP", Pct(new Q(x10, cell.N)), cell.N, suffix, Pct(lo10), Pct(hi10)));
                    L(F("- the mean share: {0} FITTED n={1}{2} 95% {3}", Pct(mean), cell.N, suffix, Interval(boot.Mean[n], Pct)));
                }
                L("- the drawn list in fit order (identity - owner - d of m - its own cell):");
                int i = 0;
                foreach (Obs o in cell.Drawn) { L(F("  {0}. {1} - {2} - {3} of {4} - {5}", ++i, o.Id, o.Rc.Owner.Id, o.D, o.M, o.Cell)); }
                L();
            }
            foreach (char ch in new[] { 'H', 'S' })
            {
                var calls = fit.Fitted.Where(rc => rc.Ch == ch).ToList();
                L(F("- SELECTION ON PASSAGE, {0}: {1} of {2} PP-NAY roll calls failed at K = 0 on their recorded bloc result (not fitted).", ch == 'H' ? "the House" : "the Senate", calls.Count(rc => !RecordedBlocPasses(rc, rc.Owner.PP, Default)), calls.Count));
            }
            L();

            // 3. PER MEASURE OF V
            L("## 3. Per measure of the veto path (premise (B), exact, leave one out)");
            L();
            foreach (Measure j in main.V)
            {
                Setup s = main.Setups[j];
                Score sc = main.Loo[j];
                double[][] pb = boot.Per[j];
                bool budget = s.H.Budget;
                L(F("### {0} - {1}; {2}; budget class {3}; {4}; {5}", j.Id, j.Kind, ProcedureOf(j, Default), j.BudgetMatches.Count == 0 ? "-" : string.Join(" + ", j.BudgetMatches.Select(x => x.Substring(0, x.IndexOf(" \"", StringComparison.Ordinal))).Distinct()), j.President, j.Outcome));
                L();
                foreach (Chamber ch in new[] { s.H, s.S })
                {
                    string name = ch.Ch == 'H' ? "House" : "Senate";
                    L(F("- {0}: {1}; {2}; MARGIN {3}; {4}; same day: {5}", name, ch.Procedure, BlocMargin(ch), MarginOf(ch), ch.HasRoll ? F("roll {0} ({1})", j.F(ch.Ch).No, j.F(ch.Ch).Date) : F("no roll call ({0})", j.F(ch.Ch).Text), j.SameDay == name ? "yes" : "-"));
                }
                List<string> rH = CellsRead(s, true, false), rS = CellsRead(s, false, true), rB = CellsRead(s);
                L(F("- cells read: {0}", rB.Count == 0 ? "none (no draw)" : string.Join(", ", rB)));
                string P(Q v, List<string> cells, double[] b) => budget ? NoDraw(v) : Derived(Pct(v), fit, cells, 1, b, Pct).Full;
                L(F("- P_H {0}", P(sc.PH, rH, pb[PH_])));
                L(F("- P_S {0}", P(sc.PS, rS, pb[PS_])));
                L(F("- P(sent) {0}", P(sc.Sent, rB, pb[SENT_])));
                L(F("- P(named) {0}", P(sc.Named, rB, pb[NAMED_])));
                L(F("- omega {0}", P(sc.Omega, rB, pb[OMEGA_])));
                L(F("- P(the draw matters): House {0}; Senate {1}", P(sc.MattersH, rH, pb[MH_]), P(sc.MattersS, rS, pb[MS_])));
                L();
            }
            L("### The DIAGNOSTIC (for US-21's assertion only: full-cell, in-sample, reduced rationals)");
            L();
            foreach (Measure j in main.V)
            {
                bool u = CellsRead(main.Setups[j]).Any(c => main.Unfitted.Contains(c));
                L(F("- {0}: P(sent) = {1}; P(named) = {2}", j.Id, u ? "UNFITTED" : diag[j].Sent.ToString(), u ? "UNFITTED" : diag[j].Named.ToString()));
            }
            L();

            // 4. THE TOTALS
            L("## 4. The totals (each count's DRAWN spread: section 7)");
            L();
            L("Pooled over the three terms:");
            L();
            L(F("- E[vetoable sent] = {0}; beside |V| = {1}, all sent on the record (not fitted)", fSent.Full, vN));
            L(F("- E_XXII[vetoable sent] = {0}; beside |V_XXII| = {1}, all sent on the record (not fitted)", fXxii.Full, vX));
            L(F("- E[named] = {0}; beside V_COUNT = {1} counted vetoes", fNamed.Full, vC));
            L(F("- E[hits] = {0}", fHits.Full));
            L(F("- E[false positives] = {0}", fFp.Full));
            L(F("- E[misses] = {0}", fMiss.Full));
            L(F("- PRECISION = {0}; beside B1's yardstick 27.1 % (45 of 166) and the draw-free share of V, {1} of {2} (not fitted)", fPrec.Full, vetoesInV, vN));
            L(F("- HIT RATE = {0}", fHit.Full));
            L(F("- P(NONE_XXII) = {0}", fNoneX.Full));
            L(F("- P(NONE) = {0}", fNone.Full));
            L(F("- P(NONE_EXP) = {0}", fNoneE.Full));
            L();
            var perPres = new Dictionary<string, (List<Measure> V, List<Measure> X, List<Measure> E, int VC, Totals T)>(StringComparer.Ordinal);
            foreach (string p in Presidents)
            {
                var vp = main.V.Where(j => j.President == p).ToList(); var xp = main.VXxii.Where(j => j.President == p).ToList(); var ep = main.VExp.Where(j => j.President == p).ToList();
                int vcp = rec.Counted.Count(m => m.President == p && m.Vetoed);
                Totals tp = TotalsOf(main, vp, true);
                perPres[p] = (vp, xp, ep, vcp, tp);
                string sfx = "@" + p;
                List<string> cp = CellsRead(main, vp), cx = CellsRead(main, xp), ce = CellsRead(main, ep);
                Fig D(Q v, List<string> cells, int m, string key, Func<Q, string> fq, Func<double, string> fd) => main.S3 ? unfit : Derived(fq(v), fit, cells, m, boot.Tot[key + sfx], fd);
                L(F("{0}:", p));
                L();
                L(F("- E[vetoable sent] = {0}; E_XXII[vetoable sent] = {1}; E[named] = {2}; E[hits] = {3}", D(tp.ESent, cp, vp.Count, "e_sent", Cnt, Cnt).Full, D(tp.EXxii, cx, xp.Count, "e_xxii", Cnt, Cnt).Full,
                    D(tp.ENamed, cp, vp.Count, "e_named", Cnt, Cnt).Full, D(tp.EHits, cp, vp.Count, "e_hits", Cnt, Cnt).Full));
                L(F("- E[false positives] = {0}; E[misses] = {1}", D(tp.ENamed - tp.EHits, cp, vp.Count, "e_fp", Cnt, Cnt).Full, D(Q.Of(vcp) - tp.EHits, cp, vcp, "e_miss", Cnt, Cnt).Full));
                string precP = main.S3 ? "UNFITTED" : tp.ENamed.IsZero ? "UNDEFINED (denominator 0)" : D(tp.EHits / tp.ENamed, cp, vp.Count, "precision", Pct, Pct).Full;
                string hitP = main.S3 ? "UNFITTED" : vcp == 0 ? "no counted veto (H2)" : D(tp.EHits / Q.Of(vcp), cp, vcp, "hitrate", Pct, Pct).Full;
                L(F("- PRECISION = {0}; HIT RATE = {1}", precP, hitP));
                L(F("- P(NONE_XXII) = {0}; P(NONE) = {1}; P(NONE_EXP) = {2}", D(tp.PNoneXxii, cx, xp.Count, "pnone_xxii", Pct, Pct).Full, D(tp.PNone, cp, vp.Count, "pnone", Pct, Pct).Full, D(tp.PNoneExp, ce, ep.Count, "pnone_exp", Pct, Pct).Full));
                L();
            }

            // 5. THE COUNTED VETOES, AND THE MEASURES NAMED AND NOT VETOED
            L("## 5. Every counted veto, and every measure named and not vetoed");
            L();
            string NamedLabel(Measure j) => main.Setups[j].H.Budget ? NoDraw(main.Loo[j].Named) : Derived(Pct(main.Loo[j].Named), fit, CellsRead(main.Setups[j]), 1, boot.Per[j][NAMED_], Pct).Full;
            foreach (Measure m in rec.Counted.Where(x => x.Vetoed)) { L(F("- {0} ({1}, {2}): P(named) {3}", m.Id, m.Kind, m.Outcome, main.V.Contains(m) ? NamedLabel(m) : "not in V")); }
            L();
            L("Named with P > 0 and not vetoed:");
            L();
            foreach (Measure j in main.V.Where(x => !x.Vetoed && main.Loo[x].Named > Q.Zero)) { L(F("- {0} ({1}, {2}): P(named) {3}", j.Id, j.Kind, j.Outcome, NamedLabel(j))); }
            L();

            // 6. THE OVERRIDE
            L("## 6. The override (printed only, never deciding)");
            L();
            OverrideLines(main, fit, boot, L);
            foreach (string p in Presidents)
            {
                var vp = main.V.Where(x => x.President == p).ToList();
                Q eo = Q.Zero, pn = Q.One, en = Q.Zero;
                foreach (Measure x in vp) { eo += main.Loo[x].Omega; pn *= Q.One - main.Loo[x].Omega; en += main.Loo[x].Named; }
                List<string> cp = CellsRead(main, vp);
                string sfx = "@" + p;
                L(F("- {0}: the expected overrides {1}; P(override | named) {2}; P(no measure of V overridden) {3}", p,
                    main.S3 ? "UNFITTED" : Derived(Cnt(eo), fit, cp, vp.Count, boot.Tot["e_ovr" + sfx], Cnt).Full,
                    main.S3 ? "UNFITTED" : en.IsZero ? "UNDEFINED (denominator 0)" : Derived(Pct(eo / en), fit, cp, vp.Count, boot.Tot["ovr_named" + sfx], Pct).Full,
                    main.S3 ? "UNFITTED" : Derived(Pct(pn), fit, cp, vp.Count, boot.Tot["pnone_ovr" + sfx], Pct).Full));
            }
            foreach (Measure m in rec.Counted.Where(x => x.Raw["override_house"] != "-" || x.Raw["override_senate"] != "-"))
            {
                string model = "not in V";
                if (main.V.Contains(m))
                {
                    Setup s = main.Setups[m]; Score sc = main.Loo[m];
                    model = F("the House's P(pass and override) {0}; the Senate's {1}; omega {2}", Derived(Pct(sc.OvrH), fit, CellsRead(s, true, false), 1, boot.Per[m][OH_], Pct).Full,
                        Derived(Pct(sc.OvrS), fit, CellsRead(s, false, true), 1, boot.Per[m][OS_], Pct).Full, Derived(Pct(sc.Omega), fit, CellsRead(s), 1, boot.Per[m][OMEGA_], Pct).Full);
                }
                L(F("- {0}, recorded: {1}; the model: {2}", m.Id, OverrideWords(m), model));
            }
            L(F("- 116|H.R. 6395 is in V: {0}.", main.V.Any(j => j.Id == "116|H.R. 6395") ? "yes" : "no"));
            L("- UNPREDICTABLE (H2), their sustained votes on their own line, in no count: " + string.Join("; ", rec.UnpredictableRows.Select(m => F("{0} ({1}th) {2}", m.Name, m.Congress, OverrideWords(m)))) + ".");
            L();

            // 7. THE SEEDED REALIZATIONS
            L("## 7. The seeded realizations (DRAWN: master seeds 1-1000, the play key, the leave-one-out lists)");
            L();
            string Dr(double[] x, string figure, Fig fig, List<string> cells)
            {
                if (fig.Tail == null) { return fig.Value; }
                var st = Stats(x);
                return F("{0} DRAWN 1000 worlds [{1}, {2}] - seed noise of a single game, not fit uncertainty; checks {3} FITTED n={4} above", Cnt(st.Mean), st.Lo.ToString("0", CultureInfo.InvariantCulture), st.Hi.ToString("0", CultureInfo.InvariantCulture), figure, NOf(fit, cells));
            }
            L("Pooled over the three terms:");
            L();
            L(F("- vetoable sent: {0}", Dr(drawn.Sent, "E[vetoable sent]", fSent, cellsV)));
            L(F("- vetoable sent under Rule XXII: {0}", Dr(drawn.Xxii, "E_XXII[vetoable sent]", fXxii, cellsX)));
            L(F("- named: {0}", Dr(drawn.Named, "E[named]", fNamed, cellsV)));
            L(F("- hits: {0}", Dr(drawn.Hits, "E[hits]", fHits, cellsV)));
            L(F("- false positives: {0}", Dr(drawn.Fp, "E[false positives]", fFp, cellsV)));
            L(F("- misses: {0}", Dr(drawn.Miss, "E[misses]", fMiss, cellsV)));
            L();
            foreach (string p in Presidents)
            {
                var pp = perPres[p];
                List<string> cp = CellsRead(main, pp.V), cx = CellsRead(main, pp.X);
                Fig Has(int m) => main.S3 ? unfit : m == 0 ? new Fig { Value = "UNFITTED m=0" } : new Fig { Value = "", Tail = "" };
                L(F("{0}: vetoable sent {1}; under Rule XXII {2}; named {3}; hits {4}; false positives {5}; misses {6}", p,
                    Dr(drawn.Per["sent@" + p], "E[vetoable sent]", Has(pp.V.Count), cp), Dr(drawn.Per["xxii@" + p], "E_XXII[vetoable sent]", Has(pp.X.Count), cx),
                    Dr(drawn.Per["named@" + p], "E[named]", Has(pp.V.Count), cp), Dr(drawn.Per["hits@" + p], "E[hits]", Has(pp.V.Count), cp),
                    Dr(drawn.Per["fp@" + p], "E[false positives]", Has(pp.V.Count), cp), Dr(drawn.Per["miss@" + p], "E[misses]", Has(pp.VC), cp)));
                L();
            }
            L(F("- UsVeto.Draw calls over the 1000 worlds: {0}; every RECORD BUDGET CLASS measure refused as a budget vote, the counter unmoved: {1}.", drawn.Calls, drawn.BudgetDrewNothing ? "yes" : "NO"));
            foreach (var dc in drawnChecks)
            {
                if (main.S3) { L(F("- asserted: {0} - not evaluated (it reads an UNFITTED cell).", dc.Name)); continue; }
                var st = Stats(dc.X);
                L(F("- asserted: {0}'s mean {1} against the exact {2}, the difference within 4 sd / sqrt(1000) = {3}.", dc.Name, Cnt(st.Mean), Cnt(dc.Exact), Cnt(4 * st.Sd / Math.Sqrt(1000))));
            }
            L();

            // 8. COMPANIONS
            int aNamedVetoed = rec.Counted.Count(m => m.Vetoed && m.House.Of(m.PP) != null && UsVeto.NamedByA(m.House.Of(m.PP).Members, m.House.Of(m.PP).Nay));
            int bothVetoed = rec.Counted.Count(m => m.Vetoed && OpposedF3(m, 'H') && OpposedF3(m, 'S'));
            int oneStanceVetoes = rec.Counted.Count(m => m.Vetoed && (OpposedF3(m, 'H') || OpposedF3(m, 'S')) && OneStanceSends(rec, m));
            int cbcVetoable = rec.Counted.Count(m => SentChamberByChamber(m) && new[] { 'H', 'S' }.Any(ch => Nay(m.F(ch).Of(m.PP), Default)));
            int cbcSendable = rec.Counted.Count(m => m.Vetoed && SentChamberByChamber(m));
            L("## 8. Companions (not fitted; the counted base only, V_COUNT the denominator; us_veto_backtest.pl's own definitions)");
            L();
            L(F("- (a) named and vetoed: {0} of {1}", aNamedVetoed, vC));
            L(F("- both chambers (F3's count in the House and the Senate), vetoed: {0} of {1}", bothVetoed, vC));
            L(F("- one stance (each party one stance in both chambers), vetoes sent: {0} of {1}", oneStanceVetoes, vC));
            L(F("- chamber by chamber, as recorded: {0} vetoable measures sent; vetoes sendable, {1} of {2}", cbcVetoable, cbcSendable, vC));
            L(F("- US-10's figures over all rows: `{0}`.", BacktestDocRelative));
            L();

            // 9. LISTS
            L("## 9. Lists");
            L();
            var shared = rec.RollOrder.Where(rc => rc.Measures.Count > 1).ToList();
            L(F("- Shared roll calls: {0}", shared.Count == 0 ? "none" : string.Join("; ", shared.Select(rc => rc.Id + " - " + string.Join(", ", rc.Measures.Select(m => m.Id)) + " (owner " + rc.Owner.Id + ")"))));
            L(F("- Caucus entries ignored as neither R nor D: {0}", rec.CaucusIgnored.Count == 0 ? "none" : string.Join("; ", rec.CaucusIgnored)));
            L(F("- Kinds other than bill or joint resolution under RULE XXII: {0}", rec.KindsPrinted.Count == 0 ? "none" : string.Join("; ", rec.KindsPrinted)));
            L(F("- Roll calls with no OP observation: {0}", fit.NoOp.Count == 0 ? "none" : string.Join("; ", fit.NoOp)));
            L(F("- Observations with m = 0: {0}", fit.MZero.Count == 0 ? "none" : string.Join("; ", fit.MZero)));
            L(F("- Empty-list occurrences (each the single index K = 0) - the leave-one-out lists: {0}", main.Empty.Words()));
            L(F("- Empty-list occurrences - the bootstrap replicates: {0}", boot.Empty.Words()));
            L(F("- Empty-list occurrences - INFLUENCE's removals: {0}", influenceEmpty.Words()));
            L();

            // 10. THE IDENTITIES
            L("## 10. The identities (asserted; a failure is a DEFECT and fails the instrument)");
            L();
            L(F("- SEATS OF A CONGRESS equal US_VETO_BACKTEST.md's seats table ({0} Congresses): {1}", seatsRows, seatsBad == 0 ? "holds" : "FAILS"));
            foreach (int c in rec.Seats.Keys.Select(k => int.Parse(k.Split(' ')[0], CultureInfo.InvariantCulture)).Distinct().OrderBy(x => x))
            {
                SeatsOf h = rec.SeatsFor(c, 'H'), s = rec.SeatsFor(c, 'S');
                L(F("  - {0}th: House R {1} - D {2} (sworn {3}); Senate R {4} - D {5}, independents in their caucus (sworn {6})", c, h.R, h.D, h.Sworn, s.R, s.D, s.Sworn));
            }
            L(F("- US-10's record (s802), not this rule's figure - (i) one stance over F3's {0}, at K = 0 with the OP forced YEA on full seats: (B) sends {1}; US-10's one-stance verdict on every measure ({2} differing).", us10.F3All, us10.OneStance, us10.OneStanceDisagree));
            L(F("- US-10's record (s802), not this rule's figure - (ii) (A) at K = 0 over all rows: {0} measures sent, {1} vetoable, {2} vetoes sendable.", us10.Sent, us10.Vetoable, us10.Sendable));
            L(F("- US-10's record (s802), not this rule's figure - (iii) (a) on the recorded House votes: {0} named, {1} vetoed among them ({1} of {0}), {2} outside, {3} named and not vetoed.", us10.ANamed, us10.AVetoed, us10.AVetoes - us10.AVetoed, us10.ANamed - us10.AVetoed));
            L(F("- V less F3: {0}. Measures of V where the OP's recorded stance is NAY, with their K = 0 verdicts: {1}.", vNotF3.Count == 0 ? "none" : string.Join(", ", vNotF3.Select(m => m.Id)),
                string.Join(", ", main.V.Where(j => !main.Setups[j].H.OpYea).Select(j => F("{0} ({1})", j.Id, ZeroVerdict(main.Setups[j]))).DefaultIfEmpty("none"))));
            L(F("- Every share d/m <= 1/2: {0}. (a) fires exactly where the PP's House stance is NAY, and NAMED = VETOABLE SENT under (B): {1}. The histograms of K equal the enumeration on {2} chambers.",
                fit.All.All(o => 2L * o.D <= o.M) ? "holds" : "FAILS", main.IdentityA ? "holds" : "FAILS", enumChecked));
            L(F("- The UNPREDICTABLE set {0}; B1 = {1} of {2}; UNIT = PresidentialVeto.Draw on the 8 inputs ({3}); the seeded means agree with the exact figures (section 7); the pins {4}.", unpredSet, b1Vetoed, b1Named, unitOk ? "equal" : "DIFFER",
                rec.SourceDigest == SourcePin && prepBlob == PrepPin ? "equal" : "DIFFER"));
            foreach (string ul in unitLines) { L("  - " + ul); }
            L();

            // 11. INFLUENCE
            L("## 11. INFLUENCE (printed only)");
            L();
            L(F("- k = {0} of {1} distinct fitted roll calls: {2}", influence.Count, fit.Fitted.Count, influence.Count == 0 ? "no single roll call's removal changes the outcome." : "each removal below changes the outcome."));
            foreach (var x in influence) { L(F("  - {0} (owner {1}): {2}", x.Rc.Id, x.Rc.Owner.Id, Spelled(x.Outcome))); }
            L();

            // 12. DECLARED READINGS
            L("## 12. Declared readings (each runs the whole rule with one change; never a stop)");
            L();
            // in R-number order; R12, R13 and R17 change no fitted figure of the run and print the default's headline beside their own
            var readingConfigs = Readings().ToDictionary(c => c.Key, StringComparer.Ordinal);
            foreach (string key in new[] { "R1", "R2", "R3", "R4", "R5", "R6", "R7", "R8", "R9", "R10", "R11", "R12", "R13", "R14", "R15a", "R15b", "R16a", "R16b", "R17", "R18" })
            {
                if (key == "R12")
                {
                    L("### R12 - the veto drawn at (a)'s FITTED precision (F3's form): expected vetoes = E[named] * precision");
                    L();
                    L(F("- the outcome: {0} - the same (a drawn veto changes neither the sending nor the precision)", Spelled(main.Outcome)));
                    L(F("- expected vetoes: {0}", main.S3 ? "UNFITTED" : T.ENamed.IsZero ? "UNDEFINED (denominator 0)" : Derived(Cnt(T.ENamed * (T.EHits / T.ENamed)), fit, cellsV, vN, boot.Tot["e_hits"], Cnt).Full));
                    foreach (string hl in Headline(main, boot)) { L("- " + hl); }
                    L();
                    continue;
                }
                if (key == "R13")
                {
                    L("### R13 - the decision's own variants, from the default's figures");
                    L();
                    foreach (var x in r13) { L(F("- {0}: {1}{2}", x.Key, Spelled(x.Outcome), x.Outcome == main.Outcome ? " - the same" : " - DIFFERS")); }
                    if (!precInterval) { L("- S1 on a BOOT bound: not evaluated (the precision has no interval)."); }
                    foreach (string hl in Headline(main, boot)) { L("- " + hl); }
                    L();
                    continue;
                }
                if (key == "R17")
                {
                    L("### R17 - the cell bootstrap (the scoring set fixed, the owners of observations resampled; intervals only)");
                    L();
                    L(F("- the outcome: {0} - the same", Spelled(main.Outcome)));
                    foreach (string hl in Headline(main, cellBoot)) { L("- " + hl); }
                    L();
                    continue;
                }
                (Config cfg, RunResult rr, Boot rb) = readings[readingConfigs[key].Key];
                Fit rf = rr.Fit;
                L(F("### {0}", cfg.Name));
                L();
                L(F("- the outcome: {0}{1}", Spelled(rr.Outcome), rr.Outcome == main.Outcome ? " - the same" : " - DIFFERS"));
                L(F("- the cells: {0}", string.Join("; ", rf.Names.Select(n => rf.Cells[n].Fitted
                    ? F("{0} n_own {1} n {2} FITTED{3}", n, rf.Cells[n].NOwn, rf.Cells[n].N, rf.Cells[n].Pooled ? " pooled with " + rf.Cells[n].Partner : "")
                    : TooThin(rf.Cells[n], cfg)))));
                if (cfg.Cells == "contested") { foreach (string n in rf.Names) { L(F("- {0} split by stance: {1} observations against a YEA stance, {2} against a NAY stance", n, rf.Cells[n].Own.Count(o => o.StanceYea), rf.Cells[n].Own.Count(o => !o.StanceYea))); } }
                if (cfg.SelectionRemoved) { L(F("- {0} PP observations dropped; {1}", rf.SelectionDropped, string.Join("; ", rf.Names.Where(n => rf.Cells[n].NOwn == 0).Select(n => n + ": no selection-free observation on disk").DefaultIfEmpty("no cell left empty")))); }
                if (cfg.PresentOutside)
                {
                    // R10 prints V and F3's comparison recomputed with present outside: F3 as the PP's nays above its yeas in a roll-call chamber
                    bool F3r(Measure m) => m.RollChambers().Any(ch => { Split s = m.F(ch).Of(m.PP); return s != null && s.Nay > s.Yea; });
                    var f3r = rec.Counted.Where(F3r).ToList();
                    L(F("- V under R10: {0} counted measures, beside F3 recomputed with present outside: {1} over all rows. In V and not in F3: {2}. In F3 and not in V: {3}.", rr.V.Count, rec.All.Count(F3r),
                        string.Join(", ", rr.V.Where(j => !f3r.Contains(j)).Select(j => j.Id).DefaultIfEmpty("none")), string.Join(", ", f3r.Where(j => !rr.V.Contains(j)).Select(j => j.Id).DefaultIfEmpty("none"))));
                }
                if (cfg.Override != 0) { OverrideLines(rr, rf, rb, L); }
                foreach (string hl in Headline(rr, rb)) { L("- " + hl); }
                L();
            }

            // 13. THE DECISION
            L("## 13. The decision");
            L();
            L(F("- S3, TOO THIN: {0}", main.S3 ? "FIRES - " + string.Join("; ", fit.Names.Where(n => !fit.Cells[n].Fitted).Select(n => TooThin(fit.Cells[n], Default))) + "; S1 and S2 not evaluated" : "every cell fitted - does not fire"));
            if (!main.S3)
            {
                L(F("- S2, NEVER SENDS: {0}", main.S2Empty ? "FIRES - case \"no Rule XXII measure on the veto path\": V_XXII is empty, its figures UNFITTED m=0" : main.S2Never ? F("FIRES - case \"never sends\": 2 * P(NONE_XXII) >= 1, P(NONE_XXII) {0}", fNoneX.Full) : F("does not fire - V_XXII holds {0} measures, and 2 * P(NONE_XXII) < 1: P(NONE_XXII) {1}", vX, fNoneX.Full)));
                L(F("- S1, FAR MORE: {0}", !main.S1Evaluated ? "not evaluated (denominator 0)" : main.S1 ? F("FIRES - 166 * E[hits] < 45 * E[named]: precision {0}", fPrec.Full) : F("does not fire - 166 * E[hits] >= 45 * E[named]: precision {0} against B1's 27.1 % (45 of 166)", fPrec.Full)));
                if (precInterval) { L(F("- B1's 27.1 % lies {0} the precision's BOOT interval [{1}, {2}].", b1 < Q.FromDouble(prec.Lo) ? "below" : b1 > Q.FromDouble(prec.Hi) ? "above" : "inside", Pct(prec.Lo), Pct(prec.Hi))); }
            }
            L(F("- THE OUTCOME: {0}", Spelled(main.Outcome)));
            L(F("- THE VERDICT: {0}", verdict));
            L(F("- The decisive digest: `{0}`.", decisive));
            L("- Whatever the outcome, the full document goes to Elias (E4).");

            // the check's own log: the headline
            sb.Append(F("    ----      THE OUTCOME: {0}\n    ----      THE VERDICT: {1}\n    ----      the decisive digest: {2}\n", Spelled(main.Outcome), verdict, decisive));
            sb.Append(F("    ----      measured in {0:0.0} s\n", clock.Elapsed.TotalSeconds));
            return new Product { Document = doc.ToString(), Catalog = CatalogOf(rec, fit, blockDigest, decisive, main.Outcome) };
        }

        private static string ReadingName(string key) => key == "R15a" ? "R15 (a)" : key == "R15b" ? "R15 (b)" : key == "R16a" ? "R16 (a)" : key == "R16b" ? "R16 (b)" : key;

        /// <summary>TOO THIN (H4's form): an UNFITTED cell prints its own count, the cell it pooled with and its union's size, with no figure.</summary>
        private static string TooThin(Cell cell, Config c) => F("{0} UNFITTED n_own={1}, pooled with {2} n={3} - fewer than {4} observations", cell.Name, cell.NOwn, cell.Partner ?? PartnerOf(cell.Name, c), cell.N, c.NMin);

        /// <summary>THE OVERRIDE's figures over V (R15 prints them under its two variants): the expected overrides, P(override | named), P(no override).</summary>
        private static void OverrideLines(RunResult r, Fit fit, Boot boot, Action<string> L)
        {
            if (r.S3) { L("- The override's figures: UNFITTED."); return; }
            Q eOvr = Q.Zero, pNoOvr = Q.One;
            foreach (Measure j in r.V) { eOvr += r.Loo[j].Omega; pNoOvr *= Q.One - r.Loo[j].Omega; }
            List<string> cellsV = CellsRead(r, r.V);
            int vN = r.V.Count;
            // the replicates' totals are BootOf's: over each replicate's picks, with multiplicity, in pick order
            L(F("- The expected overrides over V (the sum of omega_j): {0}", Derived(Cnt(eOvr), fit, cellsV, vN, boot.Tot["e_ovr"], Cnt).Full));
            L(F("- P(override | named): {0}", r.T.ENamed.IsZero ? "UNDEFINED (denominator 0)" : Derived(Pct(eOvr / r.T.ENamed), fit, cellsV, vN, boot.Tot["ovr_named"], Pct).Full));
            L(F("- US-21's sentence \"under it no Congress of record overrides\", measured again with the draw: P(no measure of V overridden) {0}", Derived(Pct(pNoOvr), fit, cellsV, vN, boot.Tot["pnone_ovr"], Pct).Full));
            L("- omega_j per measure of V: " + string.Join("; ", r.V.Select(j => F("{0} {1}", j.Id, r.Setups[j].H.Budget ? NoDraw(r.Loo[j].Omega) : Derived(Pct(r.Loo[j].Omega), fit, CellsRead(r.Setups[j]), 1, boot.Per[j][OMEGA_], Pct).Full))));
        }

        /// <summary>THE MIRROR's place: the first non-blank line after the block's END marker.</summary>
        private static string MirrorLineOf(string planLf)
        {
            int end = planLf.IndexOf(BlockEnd, StringComparison.Ordinal);
            if (end < 0) { return "-"; }
            int nl = planLf.IndexOf('\n', end);
            foreach (string line in (nl < 0 ? "" : planLf.Substring(nl + 1)).Split('\n')) { if (line.Trim().Length > 0) { return line; } }
            return "-";
        }

        /// <summary>A reading's headline figures, each labelled: E_XXII[vetoable sent], P(NONE_XXII), E[vetoable sent], E[hits], precision, hit rate.</summary>
        private static IEnumerable<string> Headline(RunResult r, Boot b)
        {
            Totals t = r.T; Fit fit = r.Fit;
            if (r.S3)
            {
                // when S3 fires, S1 and S2 are not evaluated and their figures print UNFITTED - under a reading as under the default
                foreach (string name in new[] { "E_XXII[vetoable sent]", "P(NONE_XXII)", "E[vetoable sent]", "E[hits]", "PRECISION", "HIT RATE" }) { yield return name + " = UNFITTED"; }
                yield break;
            }
            List<string> cv = CellsRead(r, r.V), cx = CellsRead(r, r.VXxii);
            Func<Q, double, string> cnt = (q, d) => t.Exact ? Cnt(q) : Cnt(d), pct = (q, d) => t.Exact ? Pct(q) : Pct(d);
            yield return F("E_XXII[vetoable sent] = {0}", r.VXxii.Count == 0 ? "UNFITTED m=0" : Derived(cnt(t.EXxii, t.DEXxii), fit, cx, r.VXxii.Count, b.Tot["e_xxii"], Cnt).Full);
            yield return F("P(NONE_XXII) = {0}", r.VXxii.Count == 0 ? "UNFITTED m=0" : Derived(pct(t.PNoneXxii, t.DPNoneXxii), fit, cx, r.VXxii.Count, b.Tot["pnone_xxii"], Pct).Full);
            yield return F("E[vetoable sent] = {0}", Derived(cnt(t.ESent, t.DESent), fit, cv, r.V.Count, b.Tot["e_sent"], Cnt).Full);
            yield return F("E[hits] = {0}", Derived(cnt(t.EHits, t.DEHits), fit, cv, r.V.Count, b.Tot["e_hits"], Cnt).Full);
            bool noNamed = t.Exact ? t.ENamed.IsZero : t.DENamed == 0;
            yield return F("PRECISION = {0}", noNamed ? "UNDEFINED (denominator 0)" : Derived(t.Exact ? Pct(t.EHits / t.ENamed) : Pct(t.DEHits / t.DENamed), fit, cv, r.V.Count, b.Tot["precision"], Pct).Full);
            yield return F("HIT RATE = {0}", r.VCount == 0 ? "no counted veto (H2)" : Derived(t.Exact ? Pct(t.EHits / Q.Of(r.VCount)) : Pct(t.DEHits / r.VCount), fit, cv, r.VCount, b.Tot["hitrate"], Pct).Full);
        }

        private static string UnpredictableLine(Record rec)
        {
            var u = rec.UnpredictableRows;
            string names = u.Select(m => m.Congress).Distinct().Count() == 1 && u.Count > 0
                ? string.Join(", ", u.Select(m => m.Name)) + F(" ({0}th)", u[0].Congress) : string.Join(", ", u.Select(m => m.Label));
            return F("UNPREDICTABLE (H2): {0} - passed both chambers without a roll call; never counted for or against.", u.Count == 0 ? "none" : names);
        }

        private static string OverrideWords(Measure m)
        {
            var parts = new List<string>();
            foreach (string ch in new[] { "house", "senate" })
            {
                if (m.Raw["override_" + ch] == "-") { continue; }
                parts.Add(F("{0} {1} {2}-{3} ({4})", ch == "house" ? "House" : "Senate", m.Raw["override_" + ch], m.Raw["override_" + ch + "_yea"], m.Raw["override_" + ch + "_nay"], m.Raw["override_" + ch + "_result"]));
            }
            return parts.Count == 0 ? "no override vote" : string.Join("; ", parts);
        }

        /// <summary>The bloc margin at K = 0, in words.</summary>
        private static string BlocMargin(Chamber ch)
        {
            if (ch.Undrawn) { return "no roll call: passes undrawn"; }
            UsVeto.ChamberVote v = ch.Vote;
            bool pass = UsVeto.ChamberOutcome(v, 0, 0);
            string m = F("bloc margin at K = 0 {0:+0;-0;0} (Y0 {1}, N0 {2})", v.Yea0 - v.Nay0, v.Yea0, v.Nay0);
            switch (v.Procedure)
            {
                case UsVeto.Procedure.House: return F("{0}: {1}", m, pass ? "passes" : "fails");
                case UsVeto.Procedure.SenateMajority: return F("{0}, the Vice President {1} on a tie: {2}", m, v.VicePresident > 0 ? "YEA" : v.VicePresident < 0 ? "NAY" : "absent", pass ? "passes" : "fails");
                default: return F("{0}, cloture {1} of {2} sworn: {3}", m, (3 * v.Sworn + 4) / 5, v.Sworn, pass ? "passes" : "fails");
            }
        }

        /// <summary>MARGIN: the smallest |D|, with the sign that reverses the K = 0 outcome, among D in [-cap_OP, cap_PP] (cap = floor((S - 1) / 2) for a
        /// party that draws, 0 otherwise); "unreachable" where none exists.</summary>
        private static string MarginOf(Chamber ch)
        {
            if (ch.Undrawn) { return "unreachable"; }
            UsVeto.ChamberVote v = ch.Vote;
            bool pass0 = UsVeto.ChamberOutcome(v, 0, 0);
            int capP = ch.PpCell != null ? UsVeto.Cap(v.PpSeats) : 0, capO = ch.OpCell != null ? UsVeto.Cap(v.OpSeats) : 0;
            if (pass0) { for (int d = 1; d <= capO; d++) { if (!UsVeto.ChamberOutcome(v, 0, d)) { return "-" + d.ToString(CultureInfo.InvariantCulture); } } }
            else { for (int d = 1; d <= capP; d++) { if (UsVeto.ChamberOutcome(v, d, 0)) { return "+" + d.ToString(CultureInfo.InvariantCulture); } } }
            return "unreachable";
        }

        private static string ZeroVerdict(Setup s) => (s.H.Undrawn || UsVeto.ChamberOutcome(s.H.Vote, 0, 0)) && (s.S.Undrawn || UsVeto.ChamberOutcome(s.S.Vote, 0, 0)) ? "sent at K = 0" : "not sent at K = 0";

        private static bool OneStanceSends(Record rec, Measure m)
        {
            SeatsOf h = rec.SeatsFor(m.Congress, 'H'), s = rec.SeatsFor(m.Congress, 'S');
            int ho = h.Of(m.OP), so = s.Of(m.OP);
            return 2 * ho > h.Sworn && (m.Expedited ? 2 * so > s.Sworn : so >= (3 * s.Sworn + 4) / 5);
        }

        private static bool SentChamberByChamber(Measure m) =>
            new[] { 'H', 'S' }.All(ch => !m.F(ch).Roll || UsVeto.ChamberOutcome(RecordedVote(m, ch, m.PP, Default, out _, out _, out _, out _, vicePresident: false, us10: true), 0, 0));

        // ================================================================== THE DECISIVE DIGEST, THE MIRROR, THE CATALOG, THE BLOCK
        /// <summary>THE DECISIVE DIGEST's lines, each "key=value" ending LF, in the block's order; a figure reading an UNFITTED cell is written UNFITTED.</summary>
        private static string DecisiveText(Record rec, RunResult r)
        {
            var sb = new StringBuilder();
            void L(string k, object v) { sb.Append(k).Append('=').Append(Convert.ToString(v, CultureInfo.InvariantCulture)).Append('\n'); }
            L("source", rec.SourceDigest);
            // READING, declared before the first run (the s834 sweep, item 1): each template line is expanded where it stands, XX in HP, HO, SP, SO -
            // every status line, then every pooled, n_own and n line, then the obs lines cell by cell (key-major), as "then p_named" spells the p lines
            string[] xx = { "HP", "HO", "SP", "SO" };
            foreach (string c in xx) { L("cell." + c + ".status", r.Fit.Cells[c].Fitted ? "FITTED" : "UNFITTED"); }
            foreach (string c in xx) { L("cell." + c + ".pooled", r.Fit.Cells[c].Pooled ? r.Fit.Cells[c].Partner : "-"); }
            foreach (string c in xx) { L("cell." + c + ".n_own", r.Fit.Cells[c].NOwn); }
            foreach (string c in xx) { L("cell." + c + ".n", r.Fit.Cells[c].N); }
            foreach (string c in xx)
            {
                Cell cell = r.Fit.Cells[c];
                for (int i = 0; i < cell.Drawn.Count; i++) { Obs o = cell.Drawn[i]; L(F("cell.{0}.obs.{1}", c, i), F("{0}|{1}|{2}|{3}|{4}", o.Id, o.Rc.Owner.Congress, o.Rc.Owner.Name, o.D, o.M)); }
            }
            for (int i = 0; i < rec.UnpredictableRows.Count; i++) { L("unpredictable." + i.ToString(CultureInfo.InvariantCulture), rec.UnpredictableRows[i].Id); }
            L("counted", rec.Counted.Count);
            L("v_count", r.VCount);
            L("v", r.V.Count);
            L("v_xxii", r.VXxii.Count);
            bool Unfit(Measure j) => CellsRead(r.Setups[j]).Any(c => r.Unfitted.Contains(c));
            foreach (Measure j in r.V) { L("p_sent." + j.Id, Unfit(j) ? "UNFITTED" : r.Loo[j].Sent.ToString()); }
            foreach (Measure j in r.V) { L("p_named." + j.Id, Unfit(j) ? "UNFITTED" : r.Loo[j].Named.ToString()); }
            bool anyV = r.V.Any(Unfit), anyX = r.VXxii.Any(Unfit);
            L("e_sent", anyV ? "UNFITTED" : r.T.ESent.ToString());
            L("e_sent_xxii", anyX ? "UNFITTED" : r.T.EXxii.ToString());
            L("e_named", anyV ? "UNFITTED" : r.T.ENamed.ToString());
            L("e_hits", anyV ? "UNFITTED" : r.T.EHits.ToString());
            L("p_none_xxii", anyX ? "UNFITTED" : r.T.PNoneXxii.ToString());
            L("outcome", r.Outcome);
            return sb.ToString();
        }

        /// <summary>THE MIRROR: the line after the block mirrors the state, section and ruling, never a digest.</summary>
        private static string MirrorOf()
        {
            switch (RulingState)
            {
                case "INSTALLED-PENDING-E4": return F("**Ruling record:** INSTALLED-PENDING-E4 ({0}): (c).", RulingSection);
                case "INSTALLED": return F("**Ruling record:** INSTALLED ({0}; E4 {1}): (c).", RulingSection, RulingE4Section);
                case "ASKED": return F("**Ruling record:** ASKED ({0}): {1}.", RulingSection, RulingOutcome.Replace("+", " + "));
                case "RULED": return F("**Ruling record:** RULED ({0}): {1}.", RulingSection, RulingOption);
                default: return "**Ruling record:** NONE";
            }
        }

        /// <summary>The block's digest: SHA-256 over the UTF-8 bytes of the lines strictly between the two marker lines, fence lines included, each ending
        /// LF with no CR.</summary>
        private static string BlockDigestOf(string planLf)
        {
            int b0 = planLf.IndexOf(BlockBegin, StringComparison.Ordinal), b1 = planLf.IndexOf(BlockEnd, StringComparison.Ordinal);
            if (b0 < 0 || b1 <= b0) { return "-"; }
            return Sha256Hex(planLf.Substring(b0 + BlockBegin.Length, b1 - b0 - BlockBegin.Length));
        }

        /// <summary>git's standard output as bytes; false when git is absent or fails (reported by the caller, never passed quietly).</summary>
        private static bool TryGitBytes(string arguments, out byte[] output)
        {
            output = null;
            try
            {
                var info = new System.Diagnostics.ProcessStartInfo("git", arguments) { WorkingDirectory = Root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true };
                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(info))
                {
                    if (process == null) { return false; }
                    process.ErrorDataReceived += (s, e) => { };
                    process.BeginErrorReadLine();
                    using (var ms = new MemoryStream())
                    {
                        process.StandardOutput.BaseStream.CopyTo(ms);
                        process.WaitForExit(30000);
                        output = ms.ToArray();
                    }
                    return process.ExitCode == 0;
                }
            }
            catch (Exception) { return false; }
        }

        /// <summary>
        /// THE CATALOG (HOW THE FIGURES REACH THE GAME): SourceDigest, the pins, RuleDigest, DecisiveDigest, Outcome, MinimumObservations and M; for each of
        /// HP, HO, SP and SO, Fitted, PooledWith and the EXACT LIST THE CELL DRAWS FROM - merged for a pooled cell, in fit order, each entry (Identity,
        /// OwnerIndex in counted CSV order, Defectors, Voting). No fitted figure is written in it, so it carries no label.
        /// </summary>
        private static string CatalogOf(Record rec, Fit fit, string ruleDigest, string decisive, string outcome)
        {
            var c = new StringBuilder();
            void L(string line = "") { c.Append(line).Append('\n'); }
            L("// <auto-generated>");
            L(F("// GENERATED by PoliSim.EditorTools.UsVetoDrawCheck.Write from {0} - DO NOT EDIT. The cheap bar regenerates it and fails on any byte of difference.", CsvRelative));
            L("// </auto-generated>");
            L("namespace PoliSim.Elections.Generated");
            L("{");
            L("    /// <summary>");
            L("    /// GENERATED - DO NOT EDIT. R-US17's rule (c) (the fenced block of `docs/specs/USA_STAGE_PLAN.md`, Elias's rulings H1-H3): the four cells of");
            L("    /// recorded crossing on the USA's veto path, each the exact list its draw indexes - already merged for a pooled cell, in fit order - for `UsVeto`");
            L("    /// once US-21's gate wires it. The runtime indexes these arrays and never sorts or merges them; leave-one-out stays in the instrument");
            L("    /// (`UsVetoDrawCheck`). OwnerIndex is the owning measure's index among the counted measures in CSV order (THE PRINT's replicate rule picks over M).");
            L("    /// </summary>");
            L("    public static class UsVetoDefections");
            L("    {");
            L(F("        public const string SourceDigest = \"{0}\";", rec.SourceDigest));
            L(F("        public const string SourcePin = \"{0}\";", SourcePin));
            L(F("        public const string PrepPin = \"{0}\";", PrepPin));
            L(F("        public const string RuleDigest = \"{0}\";", ruleDigest));
            L(F("        public const string DecisiveDigest = \"{0}\";", decisive));
            L(F("        public const string Outcome = \"{0}\";", Spelled(outcome)));
            L(F("        public const int MinimumObservations = {0};", UsVeto.MinimumObservations));
            L(F("        public const int M = {0};", rec.Counted.Count));
            L();
            L("        /// <summary>One observation of a cell's drawn list: a recorded roll call's crossers (Defectors) of its party's voting members (Voting).</summary>");
            L("        public readonly struct Entry");
            L("        {");
            L("            public readonly string Identity;");
            L("            public readonly int OwnerIndex, Defectors, Voting;");
            L("            public Entry(string identity, int ownerIndex, int defectors, int voting) { Identity = identity; OwnerIndex = ownerIndex; Defectors = defectors; Voting = voting; }");
            L("        }");
            L();
            L("        /// <summary>A cell: fitted or not, the cell it pooled with (\"-\" where it draws from its own list), and the exact list it draws from.</summary>");
            L("        public sealed class Cell");
            L("        {");
            L("            public readonly string Name, PooledWith;");
            L("            public readonly bool Fitted;");
            L("            public readonly Entry[] List;");
            L("            public Cell(string name, bool fitted, string pooledWith, Entry[] list) { Name = name; Fitted = fitted; PooledWith = pooledWith; List = list; }");
            L("        }");
            foreach (string n in new[] { "HP", "HO", "SP", "SO" })
            {
                Cell cell = fit.Cells[n];
                L();
                L(F("        public static readonly Cell {0} = new Cell(\"{0}\", {1}, \"{2}\", new[]", n, cell.Fitted ? "true" : "false", cell.Pooled ? cell.Partner : "-"));
                L("        {");
                foreach (Obs o in cell.Drawn) { L(F("            new Entry(\"{0}\", {1}, {2}, {3}),", o.Id, o.Rc.Owner.Counted, o.D, o.M)); }
                L("        });");
            }
            L("    }");
            L("}");
            return c.ToString();
        }
    }
}
