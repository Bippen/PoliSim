using System;
using PoliSim.Data.Generated;

namespace PoliSim.Data
{
    /// <summary>
    /// THE ENERGY LAYER, STAGE 2 - THE PHYSICAL LAYER AS READOUTS (2026-09-10; POLISIM_ENERGY_SPECLET.md S1-S15 ruled §454; the data
    /// ENERGY_LAYER_SPINE.md §455; the build §457). Six countries' fleets by the environment family's seven labels, the 2023 load folded
    /// into three blocks and a peak, Sweden's four bidding zones with the six directed links between them, and the combustion CO₂ of the
    /// fuel the fleets burned - every figure from <see cref="EnergyLayerData"/>, itself generated from files derived from the sources of
    /// record. Pure functions over that catalog: nothing here holds state, nothing here writes <see cref="EconomyState"/>.
    ///
    /// <para><b>THE SINGLE BOOK, AT THE SEED.</b> The environment family's power figure (<see cref="EnvironmentSeeds.PowerCo2PerCapita"/>,
    /// EDGAR's Power Industry per head) decomposes here into the fleet's OWN combustion - main-activity plants' electricity, the part a
    /// dispatch will move - and a residual that is EDGAR's heat plants, CHP heat, refineries and other energy industries, PER COUNTRY BY
    /// METHOD: the residual is defined as the seed minus the derived part, so the identity holds exactly at the seed and its composition
    /// is printed (the known heat part; the remainder). Autoproducers' combustion is computed and shown but belongs to industry's book,
    /// which this model does not keep (IPCC 1A2/1A4, not 1A1).</para>
    ///
    /// <para><b>WHAT DOES NOT MOVE YET, STATED.</b> The fleet and the load are the 2023 seed; the family's yearly step keeps writing the
    /// power figure by its own elasticities until stage 3 gives the carbon tax a mechanism (dispatch) - retiring the elasticity before that
    /// would leave the tax without an effect on power for one stage. The thermal-capacity disagreement between sources is resolved by
    /// naming a definition (the office of record's, in the catalog's header) - never by averaging three.</para>
    /// </summary>
    public static class EnergyLayer
    {
        /// <summary>The seed year of every figure in the catalog.</summary>
        public const int Year = 2023;
        /// <remarks>CONVENTION - the hours the capacity factor is taken over; 2023 was not a leap year.</remarks>
        public const double HoursPerYear = 8760.0;

        public enum Block { Base, Mid, Peak }

        private static readonly int ClassMape = Array.IndexOf(EnergyLayerData.CombustionClasses, "MAPE");
        private static readonly int ClassMapChp = Array.IndexOf(EnergyLayerData.CombustionClasses, "MAPCHP");
        private static readonly int ClassMapH = Array.IndexOf(EnergyLayerData.CombustionClasses, "MAPH");
        private static readonly int ClassApe = Array.IndexOf(EnergyLayerData.CombustionClasses, "APE");
        private static readonly int ClassApChp = Array.IndexOf(EnergyLayerData.CombustionClasses, "APCHP");
        private static readonly int ClassApH = Array.IndexOf(EnergyLayerData.CombustionClasses, "APH");

        /// <summary>The catalog's code for a country, or null when the layer does not cover it.</summary>
        public static string Code(CountryId id)
        {
            switch (id)
            {
                case CountryId.USA: return "US";
                case CountryId.Sweden: return "SE";
                case CountryId.Germany: return "DE";
                case CountryId.France: return "FR";
                case CountryId.Italy: return "IT";
                case CountryId.Poland: return "PL";
                default: return null;
            }
        }

        public static int Index(CountryId id) { string c = Code(id); return c == null ? -1 : Array.IndexOf(EnergyLayerData.Countries, c); }
        public static bool Has(CountryId id) => Index(id) >= 0;

        /// <summary>The country's currency per BOOK dollar (the ECB 2023 reference rates: 10.62 SEK, 4.20 PLN, 0.925 EUR; 1 for the USA and for a country the catalog does not cover) - the bridge a tax line's national-currency figure crosses into the book, which is in US dollars for every country (EN-4c).</summary>
        public static double NationalPerUsd(CountryId id)
        {
            int i = Index(id);
            if (i < 0) { return 1.0; }
            return EnergyLayerData.NationalPerMarketCurrency[i] / Math.Max(1e-9, EnergyLayerData.UsdPerMarketCurrency[i]);
        }

        // ---- EN-3b: the reservoir dispatch's data ----------------------------------------------------------------
        /// <summary>The hydro file's row for a zone (SE1–SE4, DE, PL), or −1.</summary>
        public static int HydroZoneIndex(string zone) => Array.IndexOf(EnergyLayerData.HydroZones, zone);
        /// <summary>A Swedish zone's 2023 day-ahead price in a block, EUR/MWh - the seed water value (the exchange's, on the model's blocks).</summary>
        public static double SeedZonePrice(string zone, int block) => EnergyLayerData.HydroPriceBlock[HydroZoneIndex(zone)][block];
        /// <summary>The share of a continental price move the zone carried in 2023 - DERIVED from the year's hours.</summary>
        public static double ZoneBetaToProxy(string zone) => EnergyLayerData.HydroBetaToProxy[HydroZoneIndex(zone)];
        /// <summary>The zone's reservoir energy capacity, GWh (Energiföretagen); 0 where none is carried.</summary>
        public static double ReservoirCapacityGwh(string zone) => EnergyLayerData.ReservoirCapacityGwh[HydroZoneIndex(zone)];
        /// <summary>Sweden's reservoirs together, GWh - the four zones' capacities summed.</summary>
        public static double SwedenReservoirCapacityGwh() { double s = 0; foreach (string z in SwedenZones) { s += ReservoirCapacityGwh(z); } return s; }
        /// <summary>The share of a country's yearly hydro energy a reservoir operator can move between the blocks (0 where unsourced, BILLED).</summary>
        public static double HydroShiftableShare(CountryId id) { int i = Index(id); return i < 0 ? 0.0 : EnergyLayerData.HydroShiftableShare[i]; }

        /// <summary>The country's ISO currency code, for a figure presented in its own currency (a tax line's rate per tonne).</summary>
        public static string CurrencyCode(CountryId id)
        {
            switch (id)
            {
                case CountryId.Sweden: return "SEK";
                case CountryId.Poland: return "PLN";
                case CountryId.Germany: case CountryId.France: case CountryId.Italy: return "EUR";
                default: return "USD";
            }
        }

        // ---- the fleet -------------------------------------------------------------------------------------------
        public static double CapacityMw(CountryId id, int label) => EnergyLayerData.CapacityRecordMw[Index(id)][label];
        public static double CapacityEmberMw(CountryId id, int label) => EnergyLayerData.CapacityEmberMw[Index(id)][label];
        public static double GenerationGwh(CountryId id, int label) => EnergyLayerData.GenerationRecordGwh[Index(id)][label];
        public static double GenerationEmberGwh(CountryId id, int label) => EnergyLayerData.GenerationEmberGwh[Index(id)][label];

        /// <summary>The 2023 capacity factor - generation over capacity × the hours; 0 where there is no capacity.</summary>
        public static double Utilisation(CountryId id, int label)
        {
            double cap = CapacityMw(id, label);
            return cap > 0 ? GenerationGwh(id, label) * 1000.0 / (cap * HoursPerYear) : 0.0;
        }

        public static double TotalGenerationGwh(CountryId id) { double t = 0; for (int l = 0; l < EnergyLayerData.Labels.Length; l++) { t += GenerationGwh(id, l); } return t; }

        /// <summary>The mix DERIVED from the fleet's generation - the seven labels' shares of the total, % - the figure the family's static seed must equal within §342's point.</summary>
        public static float[] DerivedMix(CountryId id)
        {
            double total = TotalGenerationGwh(id);
            var shares = new float[EnergyLayerData.Labels.Length];
            for (int l = 0; l < shares.Length; l++) { shares[l] = total > 0 ? (float)(100.0 * GenerationGwh(id, l) / total) : 0f; }
            return shares;
        }

        /// <summary>The largest absolute gap between two share vectors, in points.</summary>
        public static float MaxShareGap(float[] a, float[] b)
        {
            float worst = 0f;
            for (int i = 0; i < a.Length && i < b.Length; i++) { worst = Math.Max(worst, Math.Abs(a[i] - b[i])); }
            return worst;
        }

        // ---- the load, folded ------------------------------------------------------------------------------------
        public readonly struct Blocks
        {
            public readonly string Zone, Country;
            public readonly double Periods, StepHours, Hours, EnergyGwh, MinMw, MeanMw, MaxMw, P90Mw, PeakHours, PeakMeanMw, MidHours, MidMeanMw, InlandDemandGwh, Scale;
            internal Blocks(int i)
            {
                Zone = EnergyLayerData.Zones[i]; Country = EnergyLayerData.ZoneCountry[i];
                Periods = EnergyLayerData.Periods[i]; StepHours = EnergyLayerData.StepHours[i]; Hours = EnergyLayerData.Hours[i]; EnergyGwh = EnergyLayerData.EnergyGwh[i];
                MinMw = EnergyLayerData.MinMw[i]; MeanMw = EnergyLayerData.MeanMw[i]; MaxMw = EnergyLayerData.MaxMw[i]; P90Mw = EnergyLayerData.P90Mw[i];
                PeakHours = EnergyLayerData.PeakHours[i]; PeakMeanMw = EnergyLayerData.PeakMeanMw[i]; MidHours = EnergyLayerData.MidHours[i]; MidMeanMw = EnergyLayerData.MidMeanMw[i];
                InlandDemandGwh = EnergyLayerData.InlandDemandGwh[i]; Scale = EnergyLayerData.Scale[i];
            }
            /// <summary>The base block: the minimum, every hour.</summary>
            public double BaseGwh => MinMw * Hours / 1000.0;
            /// <summary>The mid block above the base.</summary>
            public double MidAboveBaseGwh => (MidMeanMw - MinMw) * MidHours / 1000.0;
            /// <summary>The peak block above the base.</summary>
            public double PeakAboveBaseGwh => (PeakMeanMw - MinMw) * PeakHours / 1000.0;
            /// <summary>The three blocks' energy - what must equal the series (gate 2).</summary>
            public double BlocksGwh => BaseGwh + MidAboveBaseGwh + PeakAboveBaseGwh;
            /// <summary>The series' energy after the balance's factor (Poland's 0.95; 1 elsewhere).</summary>
            public double ScaledEnergyGwh => EnergyGwh * Scale;
            public double LevelMw(Block block) => block == Block.Peak ? PeakMeanMw : block == Block.Mid ? MidMeanMw : MinMw;
        }

        public static int ZoneCount => EnergyLayerData.Zones.Length;
        public static int ZoneIndex(string zone) => Array.IndexOf(EnergyLayerData.Zones, zone);
        public static Blocks BlocksOf(string zone) => new Blocks(ZoneIndex(zone));
        public static Blocks BlocksAt(int i) => new Blocks(i);
        public static Blocks BlocksOf(CountryId id) => new Blocks(ZoneIndex(Code(id)));

        // ---- Sweden's zones --------------------------------------------------------------------------------------
        public static string[] SwedenZones => EnergyLayerData.SwedishZones;

        /// <summary>The eSett production type a label is apportioned by: thermal carries coal, gas and other.</summary>
        private static int TypeOfLabel(int label)
        {
            string name = EnergyLayerData.Labels[label];
            switch (name)
            {
                case "hydro": return Array.IndexOf(EnergyLayerData.ZoneProductionTypes, "hydro");
                case "nuclear": return Array.IndexOf(EnergyLayerData.ZoneProductionTypes, "nuclear");
                case "wind": return Array.IndexOf(EnergyLayerData.ZoneProductionTypes, "wind");
                case "solar": return Array.IndexOf(EnergyLayerData.ZoneProductionTypes, "solar");
                default: return Array.IndexOf(EnergyLayerData.ZoneProductionTypes, "thermal");
            }
        }

        private static double ZoneTypeGwh(int zone, int type)
        {
            double v = EnergyLayerData.ZoneProductionGwh[zone][type];
            if (EnergyLayerData.ZoneProductionTypes[type] == "thermal") { v += EnergyLayerData.ZoneProductionGwh[zone][Array.IndexOf(EnergyLayerData.ZoneProductionTypes, "other")]; }
            return v;
        }

        /// <summary>A zone's share of Sweden's 2023 production of the label's type - the apportionment key (the per-zone capacity itself is BILLED; ENTSO-E 14.1.A per bidding zone).</summary>
        public static double ZoneShare(string zone, int label)
        {
            int z = Array.IndexOf(SwedenZones, zone); if (z < 0) { return 0.0; }
            int t = TypeOfLabel(label);
            double num = ZoneTypeGwh(z, t), den = 0;
            for (int k = 0; k < SwedenZones.Length; k++) { den += ZoneTypeGwh(k, t); }
            return den > 0 ? num / den : 0.0;
        }

        public static double ZoneCapacityMw(string zone, int label) => CapacityMw(CountryId.Sweden, label) * ZoneShare(zone, label);

        /// <summary>The zone's supply as an annual average, MW: its apportioned capacity at the national capacity factor per label.</summary>
        public static double ZoneAverageSupplyMw(string zone)
        {
            double mw = 0;
            for (int l = 0; l < EnergyLayerData.Labels.Length; l++) { mw += ZoneCapacityMw(zone, l) * Utilisation(CountryId.Sweden, l); }
            return mw;
        }

        public static double ZoneConsumptionGwh(string zone) { int z = Array.IndexOf(SwedenZones, zone); return z < 0 ? 0 : EnergyLayerData.ZoneConsumptionGwh[z]; }
        public static double ZoneProductionGwh(string zone) { int z = Array.IndexOf(SwedenZones, zone); if (z < 0) { return 0; } double t = 0; foreach (double v in EnergyLayerData.ZoneProductionGwh[z]) { t += v; } return t; }

        /// <summary>The directed link's capacity, MW; the NTC seed or the map's ceiling. Infinity when the pair is not a link.</summary>
        public static double LinkCapacityMw(string from, string to, bool mapMax = false)
        {
            for (int i = 0; i < EnergyLayerData.LinkFrom.Length; i++)
            {
                if (EnergyLayerData.LinkFrom[i] == from && EnergyLayerData.LinkTo[i] == to) { return mapMax ? EnergyLayerData.LinkMapMaxMw[i] : EnergyLayerData.LinkNtcMw[i]; }
            }
            return double.PositiveInfinity;
        }

        public readonly struct LinkFlow
        {
            public readonly string From, To;
            /// <summary>The flow the zonal balances require on the link, MW, positive southward (From → To in the chain's order).</summary>
            public readonly double FlowMw;
            /// <summary>The capacity in the flow's own direction (infinite when the links are set infinite).</summary>
            public readonly double CapacityMw;
            public LinkFlow(string from, string to, double flow, double capacity) { From = from; To = to; FlowMw = flow; CapacityMw = capacity; }
            public double Utilisation => double.IsInfinity(CapacityMw) || CapacityMw <= 0 ? 0.0 : Math.Abs(FlowMw) / CapacityMw;
            /// <summary>The link cannot carry what the balances ask of it.</summary>
            public bool Binding => Math.Abs(FlowMw) > CapacityMw;
        }

        /// <summary>A zone's average supply less its level in the block, MW.</summary>
        public static double ZoneSurplusMw(string zone, Block block) => ZoneAverageSupplyMw(zone) - BlocksOf(zone).LevelMw(block);

        /// <summary>
        /// The flows the zonal balances require along the chain SE1 → SE2 → SE3 → SE4 in a block: the cumulative surplus from the north, and
        /// the capacity of each link in the flow's own direction. With <paramref name="infiniteLinks"/> the four zones are one zone -
        /// the merged balance is the last cumulative flow (gate 3's identity). ⚠ A physical proxy on annual-average supply: which link
        /// BINDS in dispatch, and what it does to the price, is stage 3's clearing and is not claimed here.
        /// </summary>
        public static LinkFlow[] Flows(Block block, bool infiniteLinks)
        {
            string[] chain = SwedenZones;
            var flows = new LinkFlow[chain.Length - 1];
            double cumulative = 0;
            for (int k = 0; k < chain.Length - 1; k++)
            {
                cumulative += ZoneSurplusMw(chain[k], block);
                double cap = infiniteLinks ? double.PositiveInfinity : (cumulative >= 0 ? LinkCapacityMw(chain[k], chain[k + 1]) : LinkCapacityMw(chain[k + 1], chain[k]));
                flows[k] = new LinkFlow(chain[k], chain[k + 1], cumulative, cap);
            }
            return flows;
        }

        /// <summary>The merged zone's balance in a block - the sum of the four surpluses, MW.</summary>
        public static double MergedSurplusMw(Block block) { double s = 0; foreach (string z in SwedenZones) { s += ZoneSurplusMw(z, block); } return s; }

        // ---- the CO₂ identity ------------------------------------------------------------------------------------
        public readonly struct Co2
        {
            /// <summary>Main-activity plants' electricity: electricity-only plants' fuel plus CHP's electricity share - the part a dispatch will move, kt.</summary>
            public readonly double DerivedKt;
            /// <summary>Main-activity heat: heat-only plants plus CHP's heat share, kt - the known part of the residual.</summary>
            public readonly double MainHeatKt;
            /// <summary>The autoproducers' three classes, kt - industry's and services' book, not this field.</summary>
            public readonly double AutoproducerKt;
            /// <summary>The family's seed as a total: per head × the World Bank population, kt.</summary>
            public readonly double SeedTotalKt;
            public Co2(double derived, double mainHeat, double autoproducer, double seedTotal) { DerivedKt = derived; MainHeatKt = mainHeat; AutoproducerKt = autoproducer; SeedTotalKt = seedTotal; }
            /// <summary>The seed minus the derived part - by definition, so the identity holds at the seed.</summary>
            public double ResidualKt => SeedTotalKt - DerivedKt;
            /// <summary>The residual less its known heat part: refineries, other energy industries, and the gap between EDGAR's factors and the IPCC defaults. Negative where the defaults over-count EDGAR.</summary>
            public double RemainderKt => ResidualKt - MainHeatKt;
            public double DerivedShare => SeedTotalKt > 0 ? DerivedKt / SeedTotalKt : 0.0;
        }

        public static double PopulationM(CountryId id) => EnergyLayerData.PopulationM[Index(id)];

        /// <summary>The decomposition of the family's power figure for a country, given the seed it carries (the caller reads it off <see cref="EnvironmentSeeds"/>).</summary>
        public static Co2 Decomposition(CountryId id, float seedPowerCo2PerHead)
        {
            int i = Index(id);
            double shareMain = EnergyLayerData.ChpShareMain[i];
            double derived = 0, mainHeat = 0, auto = 0;
            for (int l = 0; l < EnergyLayerData.CombustionLabels.Length; l++)
            {
                double[] c = EnergyLayerData.Co2Kt[i][l];
                derived += c[ClassMape] + c[ClassMapChp] * shareMain;
                mainHeat += c[ClassMapH] + c[ClassMapChp] * (1.0 - shareMain);
                auto += c[ClassApe] + c[ClassApChp] + c[ClassApH];
            }
            double seedTotal = seedPowerCo2PerHead * EnergyLayerData.PopulationM[i] * 1000.0;   // t/head × million people = Mt; kt
            return new Co2(derived, mainHeat, auto, seedTotal);
        }

        /// <summary>The fleet's combustion by label, kt - main-activity electricity only (the derived part's composition).</summary>
        public static double DerivedKtByLabel(CountryId id, int combustionLabel)
        {
            int i = Index(id); double[] c = EnergyLayerData.Co2Kt[i][combustionLabel];
            return c[ClassMape] + c[ClassMapChp] * EnergyLayerData.ChpShareMain[i];
        }
    }
}
