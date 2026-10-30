using System;
using System.Collections.Generic;
using System.Globalization;
using PoliSim.Data;
using PoliSim.Data.Generated;
using PoliSim.Simulation;

namespace PoliSim.UI
{
    /// <summary>
    /// S1 (K320 and K322, ruled 2026-10-30; UI v3.3 §1 and §7): **THE ENERGY PAGE'S SLIPS FOR THE WORDS ITS DECLUTTER TOOK OFF THE PAGE AT REST, BUILT
    /// FROM THE MODEL AND NOTHING ELSE.** At rest a label is at most three words, a caption one line never cut, and no sentence stands; every word the
    /// sweep took off the page is on a slip here at the anchor of the thing that lost it, in fragments of about eight words, at most four lines under
    /// the head - and the † view still prints the whole text where it stood. The page starts each pass from this book (`GameController.EnergyV35.cs`
    /// DrawEnergyTab) and draws the fragment each <see cref="Moved"/> keeps at rest, so what the page opens is what a reachability check reads: a check
    /// builds the same book from a country with <see cref="Build(Country, World)"/>. The words each anchor took are listed in
    /// `docs/declutter/moved_s1en.tsv`.
    /// <para>The slips the page writes as it draws (the prices, the plants, the grid, the dials' census) are the page's own and stay where they are
    /// built; this book holds the ones the declutter moved, and the section heads whose titles it shortened.</para>
    /// </summary>
    public static class EnergySlips
    {
        // ---- the anchors ----------------------------------------------------------------------------------------------------------------------
        public const string Target = "en:target";
        public const string Why = "en:why";
        public const string WhyRule = "en:why:rule";
        public const string WhyDerivation = "en:why:derivation";
        public const string WhyZones = "en:why:zones";
        public const string WhyLoad = "en:why:load";
        public const string WhyWater = "en:why:water";
        public const string WhyIncidence = "en:why:incidence";
        public const string WhyLeader = "en:why:leader";
        public const string Reaches = "en:reaches";
        public const string ReachesEts = "en:reaches:ets";
        public const string ReachesRetail = "en:reaches:retail";
        public const string ReachesPlanning = "en:reaches:planning";
        public const string ReachesOwnership = "en:reaches:ownership";
        public const string ReachesElectricityTax = "en:reaches:electax";
        public const string LawsLinkAnchor = "en:lawslink";
        public const string SectorsBill = "en:sectorsbill";
        public const string SectorsBillStatusAnchor = "en:sectorsbill:status";

        // ---- the section heads and the labels the declutter shortened (K320: a label is at most three words) -----------------------------------
        public const string WhyTitle = "Why this price";
        public const string WhyTitleBefore = "WHY THE PRICE IS WHAT IT IS";
        public const string ReachesTitle = "Other instruments";
        public const string ReachesTitleBefore = "WHAT ELSE REACHES IT";
        public const string RuleName = "The price rule";
        public const string RuleNameBefore = "THE RULE ON THE PRICE";
        public const string WaterGapName = "Water and reservoirs";
        public const string WaterGapNameBefore = "WATER VALUE AND RESERVOIRS";
        public const string LawsChip = "ELECTRICITY TAX LAWS ▸";
        public const string LawsChipBefore = "THE ELECTRICITY TAX'S LAWS ▸";

        /// <summary>The rule row's two source lines - the † view's; at rest they are the rule's slip.</summary>
        public const string RuleDeviceLine1 = "ONE DEVICE · THE FORM IS IN THE PARTS' NAMES";
        public const string RuleDeviceLine2 = "AND IN THE CAPTION THAT SAYS WHAT PRICED IT";

        /// <summary>
        /// One moved text: the fragment that stays at rest (null keeps the whole text there), the whole text as it stood - the † view prints it - and the
        /// slip's lines that carry it, each line a run of the whole text's words in their order, so the lines read together are the text.
        /// </summary>
        public sealed class Moved
        {
            public readonly string Rest, Full;
            public readonly string[] Lines;
            public Moved(string rest, string full, params string[] lines) { Rest = rest; Full = full; Lines = lines; }

            /// <summary>What the surface draws: the whole text behind the †, the fragment at rest.</summary>
            public string Drawn(bool provenance) => provenance || Rest == null ? Full : Rest;

            /// <summary>The slip: its head, then the lines.</summary>
            public SlipContent Slip(string head)
            {
                var slip = new SlipContent(head);
                foreach (string line in Lines) { slip.Add(line); }
                return slip;
            }
        }

        /// <summary>What the page knows that the model does not: the legislature's word, the electricity tax's laws enacted and before it, the pending sectors
        /// bill's timing words (null with none pending), and the role's lock on that bill (null where the player may introduce it).</summary>
        public sealed class Context
        {
            public string Legislature;
            public int LawsEnacted, LawsPending;
            public string PendingResolves;
            public string LockedBecause;
        }

        private static string Absent => SymbolRegistry.Word(Symbol.Absent);
        private static string Upper(CountryId id) => id.ToString().ToUpperInvariant();

        // ---- why the price is what it is ---------------------------------------------------------------------------------------------------

        public static readonly Moved LoadGrowth = new Moved("STATED, NOT MODELLED",
            "THE LOAD DOES NOT GROW WITH GDP OR ELECTRIFICATION · STATED, NOT MODELLED",
            "THE LOAD DOES NOT GROW WITH GDP OR ELECTRIFICATION", "STATED, NOT MODELLED");

        /// <summary>Sweden's zones row: the links' source line.</summary>
        public static readonly Moved ZonesLinks = new Moved("PEAK FLOW ⁄ NTC", "LINKS IN THE GUTTERS THEY JOIN · PEAK FLOW ⁄ NTC",
            "LINKS IN THE GUTTERS THEY JOIN", "PEAK FLOW ⁄ NTC");

        /// <summary>The note under a one-zone country's cells: what the real market has that the model's one zone does not.</summary>
        public static Moved ZonesNote(CountryId id)
        {
            if (id == CountryId.Italy)
            {
                return new Moved("ONE ZONE · SEVEN IN THE REAL MARKET", "ONE ZONE · THE REAL MARKET HAS SEVEN: NORD · CNOR · CSUD · SUD · CALA · SICI · SARD",
                    "ONE ZONE · THE REAL MARKET HAS SEVEN", "NORD · CNOR · CSUD · SUD · CALA · SICI · SARD");
            }
            if (id == CountryId.USA)
            {
                return new Moved("ONE ZONE · THREE IN THE REAL GRID", "ONE ZONE · THE REAL GRID IS THREE INTERCONNECTIONS: EASTERN · WESTERN · ERCOT",
                    "ONE ZONE · THE REAL GRID IS THREE INTERCONNECTIONS", "EASTERN · WESTERN · ERCOT");
            }
            return new Moved("ONE ZONE · NEIGHBOURS EXOGENOUS", "ONE ZONE · THE NEIGHBOURS ARE EXOGENOUS", "ONE ZONE · THE NEIGHBOURS ARE EXOGENOUS");
        }

        /// <summary>Sweden's water row caption.</summary>
        public static Moved WaterCaption(string marketUnit) => new Moved(marketUnit + " · PER BLOCK", marketUnit + " · PER BLOCK · UNDER THE RULE ROW'S THREE BLOCKS",
            marketUnit + " · PER BLOCK", "UNDER THE RULE ROW'S THREE BLOCKS");

        /// <summary>The water gap of a country with no reservoir modelled - its BILLED fold kept at rest where the fold is billed.</summary>
        public static Moved WaterAbsent(CountryId id)
        {
            string country = Upper(id);
            if (id == CountryId.USA || id == CountryId.France || id == CountryId.Poland)
            {
                return new Moved("HYDRO FOLD BILLED · NO RESERVOIR", "THE HYDRO SHIFT'S FOLD IS BILLED FOR " + country + " · NO RESERVOIR MODELLED",
                    "THE HYDRO SHIFT'S FOLD IS BILLED FOR " + country, "NO RESERVOIR MODELLED");
            }
            return new Moved("NO RESERVOIR MODELLED", "NO RESERVOIR MODELLED FOR " + country + " · SWEDEN'S IS THE ONE", "NO RESERVOIR MODELLED FOR " + country, "SWEDEN'S IS THE ONE");
        }

        /// <summary>The incidence bridge's caption.</summary>
        public static Moved IncidenceCaption() => new Moved(EnergyLedger.BookCurrency + " BN · PAID, THEN RECEIVED",
            EnergyLedger.BookCurrency + " BN · PAID, THEN RECEIVED · ONE SCALE WITH THE ROW ABOVE",
            EnergyLedger.BookCurrency + " BN · PAID, THEN RECEIVED", "ONE SCALE WITH THE ROW ABOVE");

        /// <summary>The leader joining the two ledgers: the figure they share at rest, what it means on its slip (the figure is the slip's head).</summary>
        public static Moved Leader(double outlay)
        {
            string same = string.Format(CultureInfo.InvariantCulture, "THE SAME {0:N1}", outlay);
            return new Moved(same, same + " - WHAT THE MARKET COST IS WHAT THE GENERATORS RECEIVED", "WHAT THE MARKET COST IS WHAT THE GENERATORS RECEIVED");
        }

        /// <summary>The rule row's foot: the head's figure re-derived from the body, printed so the reader can check it - the figure and the formula at rest,
        /// the weights on the slip. Where the two weights differ, both figures at rest and the reason on the slip.</summary>
        public static Moved Derivation(double wholesalePerMwh, double rederived, bool chain)
        {
            string over = chain ? "TWELVE BLOCKS" : "THREE BLOCKS";
            if (Math.Abs(rederived - wholesalePerMwh) < 0.05)
            {
                string head = string.Format(CultureInfo.InvariantCulture, "{0:0.0} = Σ PRICE × ENERGY ⁄ Σ ENERGY", wholesalePerMwh);
                return new Moved(head, head + ", " + over + " · ENERGY = SEED MW × HOURS PER BLOCK · THE PEAK BLOCK FILLS ITS LANE",
                    head + ", " + over, "ENERGY = SEED MW × HOURS PER BLOCK", "THE PEAK BLOCK FILLS ITS LANE");
            }
            return new Moved(string.Format(CultureInfo.InvariantCulture, "{0:0.0} CLEARED · {1:0.0} RE-DERIVED", wholesalePerMwh, rederived),
                string.Format(CultureInfo.InvariantCulture, "{0:0.0} IS THE CLEARING'S; Σ PRICE × ENERGY ⁄ Σ ENERGY, {1}, READS {2:0.0} HERE - THE TWO WEIGHTS DIFFER", wholesalePerMwh, over, rederived),
                string.Format(CultureInfo.InvariantCulture, "{0:0.0} IS THE CLEARING'S", wholesalePerMwh),
                string.Format(CultureInfo.InvariantCulture, "Σ PRICE × ENERGY ⁄ Σ ENERGY, {0}, READS {1:0.0} HERE", over, rederived), "THE TWO WEIGHTS DIFFER");
        }

        /// <summary>Whether the rule row prices Sweden's chain (the four zones, the links and the water) - the derivation then runs over twelve blocks.</summary>
        public static bool IsChain(EnergyMarket.Result r, bool sweden) => sweden && r.Links != null && r.Links.Length > 0 && r.WaterValue != null && r.WaterValue.Length >= 3;

        /// <summary>
        /// The head's figure re-derived from the body with the clearing's own weights - each block's seed MW times its hours, the energy it serves (the first
        /// film weighted by MW alone and read 73 against 58). The rule row's own arithmetic, kept in one place so the foot and its slip print one figure.
        /// </summary>
        public static double Rederived(EnergyMarket.Result r, bool sweden)
        {
            double weighted = 0, load = 0;
            if (r?.Zones == null) { return 0; }
            for (int z = 0; z < r.Zones.Length; z++)
            {
                int zi = r.ZoneNames != null && z < r.ZoneNames.Length ? EnergyLayer.ZoneIndex(r.ZoneNames[z]) : -1;
                for (int b = 0; b < r.Zones[z].Length && b < 3; b++)
                {
                    double mw = zi < 0 ? r.Zones[z][b].DemandMw : sweden ? EnergyLayerData.ZoneConsumptionBlockMw[zi][b] : EnergyLayerData.DispatchDemandMw[zi][b];
                    double energy = Math.Max(0.0, mw) * (zi < 0 ? 1.0 : EnergyLayerData.DispatchHours[zi][b]);
                    weighted += r.Zones[z][b].Price * energy; load += energy;
                }
            }
            return load > 0 ? weighted / load : 0;
        }

        // ---- what else reaches the layer: the instruments' gap rows -------------------------------------------------------------------------

        public static readonly Moved EtsAbsent = new Moved("NO FEDERAL CARBON PRICE", "THE ROW IS ZERO BY THE CATALOG · A FEDERAL CARBON PRICE DOES NOT EXIST",
            "THE ROW IS ZERO BY THE CATALOG", "A FEDERAL CARBON PRICE DOES NOT EXIST");

        public static readonly Moved RetailNoLine = new Moved("CLIMATE FUND OFF BUDGET", "THE CLIMATE FUND SITS OFF THE BUDGET · THE SUBSIDY DIAL'S COST STAYS WITH THE OTHER SECTORS' SUPPORT",
            "THE CLIMATE FUND SITS OFF THE BUDGET", "THE SUBSIDY DIAL'S COST STAYS WITH THE OTHER SECTORS' SUPPORT");

        public static readonly Moved RetailNoLevy = new Moved("RETAIL COMPONENTS BILLED", "THE RETAIL COMPONENTS ARE BILLED · THE SUBSIDY'S COST LANDS ON THE ENERGY LINE WITH NO RETAIL EFFECT",
            "THE RETAIL COMPONENTS ARE BILLED", "THE SUBSIDY'S COST LANDS ON THE ENERGY LINE", "WITH NO RETAIL EFFECT");

        public static readonly Moved PlanningAbsent = new Moved("NO FLEET ORDER", "THE DIAL REACHES NO ORDER - THE FLEET MOVES BY THE CONNECTION QUEUE ABOVE, UNPRICED · THE DIAL'S COST STAYS WITH THE OTHER SECTORS' SUPPORT",
            "THE DIAL REACHES NO ORDER", "THE FLEET MOVES BY THE CONNECTION QUEUE ABOVE, UNPRICED", "THE DIAL'S COST STAYS WITH THE OTHER SECTORS' SUPPORT");

        public static readonly Moved OwnershipAbsent = new Moved("NO OWNERSHIP TERM", "A STATE SHARE OF THE GENERATORS' RECEIPTS WOULD READ THE RENT THE PAGE PRINTS AND NOTHING READS · THE DIAL STILL MOVES THE SECTOR'S OUTPUT AND EMPLOYMENT",
            "A STATE SHARE OF THE GENERATORS' RECEIPTS", "WOULD READ THE RENT THE PAGE PRINTS", "AND NOTHING READS", "THE DIAL STILL MOVES THE SECTOR'S OUTPUT AND EMPLOYMENT");

        public static readonly Moved ElectricityTaxAbsent = new Moved("NO NATIONAL STATUTE", "THE STATES LEVY THEIR OWN GROSS-RECEIPTS TAXES · NO NATIONAL STATUTE FOR THIS HOUSE TO MOVE",
            "THE STATES LEVY THEIR OWN GROSS-RECEIPTS TAXES", "NO NATIONAL STATUTE FOR THIS HOUSE TO MOVE");

        /// <summary>The line beside the electricity tax's laws chip: the counts at rest; what the chip opens and why this category, on its slip.</summary>
        public static Moved LawsLink(string countryName, bool offered, int offeredCount, int enacted, int pending, string legislatureCaps)
        {
            if (offered)
            {
                string counts = string.Format(CultureInfo.InvariantCulture, "{0} OFFERED · {1} ENACTED · {2} BEFORE {3}", offeredCount, enacted, pending, legislatureCaps);
                return new Moved(counts, "OPENS LAWS ON THIS CATEGORY · " + counts + " · THE ONE CATEGORY THAT REACHES THE ENERGY LAYER",
                    "OPENS LAWS ON THIS CATEGORY", counts, "THE ONE CATEGORY THAT REACHES THE ENERGY LAYER");
            }
            string name = countryName.ToUpperInvariant();
            return new Moved("NO NATIONAL EXCISE · NOT OFFERED", "NO NATIONAL ELECTRICITY EXCISE FOR " + name + " · THE LAWS ARE NOT OFFERED",
                "NO NATIONAL ELECTRICITY EXCISE FOR " + name, "THE LAWS ARE NOT OFFERED");
        }

        /// <summary>The status under the sectors bill's action: whether a bill stands before the legislature at rest; what the dials travel in, or when the bill
        /// resolves, on its slip.</summary>
        public static Moved SectorsBillStatus(string legislature, string pendingResolves)
        {
            if (pendingResolves == null)
            {
                return new Moved("No Economic Sectors bill before " + legislature,
                    "No Economic Sectors bill before " + legislature + " - these dials travel in it, one draft with the Sectors page.",
                    ("No Economic Sectors bill before " + legislature).ToUpperInvariant(), "THESE DIALS TRAVEL IN IT", "ONE DRAFT WITH THE SECTORS PAGE");
            }
            var lines = new List<string> { ("An Economic Sectors bill is before " + legislature).ToUpperInvariant() };
            foreach (string part in pendingResolves.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries)) { lines.Add(part.ToUpperInvariant()); }
            return new Moved("Economic Sectors bill before " + legislature, "An Economic Sectors bill is before " + legislature + " - " + pendingResolves + ".", lines.ToArray());
        }

        /// <summary>A role's lock on a bill as the page keeps it at rest: its first clause - the role's word (IN OPPOSITION, JUNIOR PARTNER, PRESIDENT).</summary>
        public static string LockHead(string lockedBecause)
        {
            if (string.IsNullOrEmpty(lockedBecause)) { return lockedBecause; }
            int cut = lockedBecause.IndexOf(" · ", StringComparison.Ordinal);
            return cut > 0 ? lockedBecause.Substring(0, cut) : lockedBecause;
        }

        /// <summary>A slip whose lines are a text's clauses - the text split at its middle dots, each clause a line, in order.</summary>
        public static SlipContent Fragments(string head, string text)
        {
            var slip = new SlipContent(head);
            if (string.IsNullOrEmpty(text)) { return slip; }
            foreach (string part in text.Split(new[] { " · " }, StringSplitOptions.RemoveEmptyEntries)) { slip.Add(part.Trim()); }
            return slip;
        }

        // ---- the dials' range captions ------------------------------------------------------------------------------------------------------

        /// <summary>The Policy tab's dials as their rows name them (the literal first argument of each DrawDialRow call, which the dial checks read) - the
        /// range captions they speak at rest are the band's name alone.</summary>
        public static readonly string[] DialNames = { "Retail intervention", "Subsidy", "Market liberalisation", "Investment planning", "State ownership" };

        /// <summary>Every band slip the Policy tab can open, for a check: each dial's ten bands under the one anchor the page opens the draft's band on
        /// ("dial:" + the dial's name + ":band"). The page builds the draft's band alone, as the draft moves.</summary>
        public static IEnumerable<(string Anchor, SlipContent Slip)> BandSlips()
        {
            foreach (string name in DialNames)
            {
                if (!RangeCaptions.TryGet(name, out RangeCaptions.Dial dial)) { continue; }
                for (int b = 0; b < dial.Bands.Length; b++) { yield return ("dial:" + name + ":band", RangeCaptions.BandSlip(dial, b)); }
            }
        }

        // ---- the mandate ---------------------------------------------------------------------------------------------------------------------

        private static string Gw(double v) => v.ToString("0.#", CultureInfo.InvariantCulture);

        /// <summary>The mandate tile's name at rest - the target in its statute's own figures, at most three counted words; the headline as the statute's line
        /// reads it is the tile's slip.</summary>
        public static string MandateLabel(AiEnergyMinistry.Mandate m)
        {
            if (m == null) { return "No energy mandate"; }
            switch (m.Form)
            {
                case AiEnergyMinistry.MandateForm.CapacityPath:
                    if (m.WindGw != null && m.WindGw.Length > 0 && m.SolarGw != null && m.SolarGw.Length > 0 && m.WindGw[0].Year == m.SolarGw[0].Year)
                    {
                        return m.WindGw[0].Year.ToString(CultureInfo.InvariantCulture) + ": wind " + Gw(m.WindGw[0].Gw) + " · solar " + Gw(m.SolarGw[0].Gw) + " GW";
                    }
                    return "Capacity path";
                case AiEnergyMinistry.MandateForm.RenewableShare:
                    return "Renewables " + Gw(m.RenewableSharePercent) + " % by " + m.ShareYear.ToString(CultureInfo.InvariantCulture);
                case AiEnergyMinistry.MandateForm.FossilFree:
                    return "Fossil-free by " + m.FossilFreeYear.ToString(CultureInfo.InvariantCulture);
                case AiEnergyMinistry.MandateForm.Billed:
                    return "Billed mandate";
                default:
                    return "No statutory target";
            }
        }

        /// <summary>The mandate's slip: what the tile's figure counts and against what, the statute, and the headline whole. Read inside the country's fleet
        /// scope (the queued orders are this country's).</summary>
        public static SlipContent MandateSlip(Country c)
        {
            AiEnergyMinistry.Mandate m = AiEnergyMinistry.MandateOf(c.Id);
            var slip = new SlipContent("THE MANDATE");
            if (m == null) { return slip.Add("THE COUNTRY HOLDS NO STATUTORY ENERGY TARGET THE GAME CARRIES"); }
            switch (m.Form)
            {
                case AiEnergyMinistry.MandateForm.FossilFree:
                    slip.Add("THE FOSSIL FLEET WITH THE QUEUE").Add("COAL AND GAS, AGAINST A TARGET OF NONE");
                    break;
                case AiEnergyMinistry.MandateForm.CapacityPath:
                    slip.Add("WIND AND SOLAR WITH THE QUEUE").Add("AGAINST THE PATH THE STATUTE SETS");
                    break;
                default:
                    slip.Add("THE RENEWABLE SHARE WITH THE QUEUE · THE RECORD " + AiEnergyMinistry.RecordRenewableSharePercent(c.Id).ToString("0.0", CultureInfo.InvariantCulture) + " %");
                    break;
            }
            return slip.Add(m.Statute.ToUpperInvariant()).Add(m.Headline.ToUpperInvariant());
        }

        // ---- the book ------------------------------------------------------------------------------------------------------------------------

        /// <summary>
        /// The book for <paramref name="home"/> as a check builds it: the market cleared and the ledger computed as the page computes them, inside the country's
        /// fleet scope, with the page's defaults for what the model does not hold - the country's own legislature, its enacted electricity-tax laws, none
        /// before the legislature, no sectors bill pending, and no lock.
        /// </summary>
        public static PeopleSlips.Book Build(Country home, World world)
        {
            using (EnergyFleet.For(home))
            {
                EnergyMarket.Result r = EnergyMarket.Clear(home);
                EnergyLedger.Book book = EnergyLedger.Compute(home, r, Math.Max(0.0001f, home.State.PriceLevel), EnergyLedger.CreditFor(home));
                int enacted = 0;
                foreach (EnactedLaw law in home.EnactedLaws)
                {
                    LawDefinition definition = LawCatalog.GetById(law.LawId);
                    if (definition != null && definition.Category == LawCategory.ElectricityTax) { enacted++; }
                }
                return Build(home, r, book, new Context { Legislature = GameController.GetLegislatureName(home.Id), LawsEnacted = enacted });
            }
        }

        /// <summary>The book for <paramref name="home"/> from the page's own clearing <paramref name="r"/> and book <paramref name="ledger"/> - the page calls
        /// this inside the country's fleet scope at the head of every pass.</summary>
        public static PeopleSlips.Book Build(Country home, EnergyMarket.Result r, EnergyLedger.Book ledger, Context context)
        {
            var book = new PeopleSlips.Book();
            if (home == null || !EnergyLayer.Has(home.Id)) { return book; }
            CountryId id = home.Id;
            bool sweden = id == CountryId.Sweden, usa = id == CountryId.USA;
            string legislature = context?.Legislature ?? GameController.GetLegislatureName(id);

            // the Overview: the mandate, and why the price is what it is
            book.Anchors[Target] = MandateSlip(home);
            book.Anchors[Why] = new SlipContent(WhyTitle.ToUpperInvariant()).Add(WhyTitleBefore).Add("THE RULE, THE FLEET AND ITS ZONES")
                .Add("THE WATER AND THE TWO LEDGERS").Add("KEPT AS BUILT UNDER THE NEW PAGE");
            book.Anchors[WhyRule] = new SlipContent(RuleName.ToUpperInvariant()).Add(RuleNameBefore).Add(RuleDeviceLine1).Add(RuleDeviceLine2);
            if (r?.Zones != null && r.Zones.Length > 0)
            {
                book.Anchors[WhyDerivation] = Derivation(EnergyLedger.LoadWeightedPricePerMwh(r), Rederived(r, sweden), IsChain(r, sweden)).Slip("THE WHOLESALE, RE-DERIVED");
                var zones = new SlipContent("THE ZONES");
                if (sweden) { foreach (string line in ZonesLinks.Lines) { zones.Add(line); } }
                if (r.Links == null || r.Links.Length == 0 || r.ZoneNames == null) { foreach (string line in ZonesNote(id).Lines) { zones.Add(line); } }
                book.Anchors[WhyZones] = zones;
            }
            book.Anchors[WhyLoad] = LoadGrowth.Slip(Absent);
            bool waterKnown = sweden && r?.WaterValue != null && r.WaterValue.Length >= 3;
            if (waterKnown) { book.Anchors[WhyWater] = WaterCaption((usa ? "USD" : "EUR") + " PER MWh").Slip("WATER VALUE"); }
            else if (!sweden)
            {
                var water = new SlipContent(Absent).Add(WaterGapNameBefore);   // the gap row's old name first, then its reason
                foreach (string line in WaterAbsent(id).Lines) { water.Add(line); }
                book.Anchors[WhyWater] = water;
            }
            book.Anchors[WhyIncidence] = IncidenceCaption().Slip("INCIDENCE");
            if (ledger != null)
            {
                Moved leader = Leader(ledger.WholesaleOutlay);
                book.Anchors[WhyLeader] = leader.Slip(leader.Rest);
            }

            // the Policy tab: the sectors bill, the laws link and the instruments' gap rows
            Moved status = SectorsBillStatus(legislature, context?.PendingResolves);
            book.Anchors[SectorsBillStatusAnchor] = status.Slip("THE ECONOMIC SECTORS BILL");
            if (!string.IsNullOrEmpty(context?.LockedBecause)) { book.Anchors[SectorsBill] = Fragments(SymbolRegistry.Word(Symbol.Locked), context.LockedBecause.ToUpperInvariant()); }
            book.Anchors[Reaches] = new SlipContent(ReachesTitle.ToUpperInvariant()).Add(ReachesTitleBefore).Add("THE ONE LAW CATEGORY THAT REACHES THE LAYER")
                .Add("THE ETS PRICE, EACH DIAL'S REACH").Add("THE CARBON TAX, THE ELECTRICITY TAX · KEPT AS BUILT");
            bool offered = EnergyLayer.HasElectricityTax(id);
            Moved laws = LawsLink(home.Name, offered, GameController.ElectricityTaxLawCount, context?.LawsEnacted ?? 0, context?.LawsPending ?? 0, legislature.ToUpperInvariant());
            book.Anchors[LawsLinkAnchor] = laws.Slip(LawsChipBefore);
            int ci = EnergyLayer.Index(id);
            if (ci < 0 || EnergyLayerData.EtsPerT[ci] <= 0) { book.Anchors[ReachesEts] = EtsAbsent.Slip(Absent); }
            if (!SectorCouplings.HasEnergyLine(home)) { book.Anchors[ReachesRetail] = RetailNoLine.Slip(Absent); }
            else if (!EnergyLedger.HasPolicyLevy(id)) { book.Anchors[ReachesRetail] = RetailNoLevy.Slip(Absent); }
            book.Anchors[ReachesPlanning] = PlanningAbsent.Slip(Absent);
            book.Anchors[ReachesOwnership] = OwnershipAbsent.Slip(Absent);
            if (!offered) { book.Anchors[ReachesElectricityTax] = ElectricityTaxAbsent.Slip(Absent); }
            return book;
        }
    }
}
