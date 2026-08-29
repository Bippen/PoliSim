using System;
using System.Collections.Generic;

namespace PoliSim.Elections
{
    /// <summary>
    /// W-B4 / SPEC §10 — campaign offices. PURE MODEL, WIRED TO NOTHING (R-N2); the AI campaign
    /// (`CampaignRun`) carries one <see cref="OfficeNetwork"/> per party.
    ///
    /// **An office is organisation in a region, and organisation is what local reach was
    /// pretending to be.** W-B3's placeholder gave every rally and town hall the whole region as
    /// its audience pool wherever it was held — an office everywhere at full strength. Now a
    /// region's local audience is the region's electorate × (<see cref="VisitFraction"/> + the
    /// rest × the office's INFLUENCE): a candidate who visits a region with no organisation draws
    /// a quarter of what a full office can turn out. Influence is the office's volunteers over its
    /// capacity — it is recruited, not bought, so an office opened late is worth less.
    ///
    /// §10's five provisions, where they land: LOCAL ORGANISATION = influence (the local audience);
    /// VOLUNTEER RECRUITMENT = <see cref="RecruitPerDay"/> toward <see cref="VolunteerCapacity"/>;
    /// DOOR-TO-DOOR = the office's volunteer-hours, added to the region's ceiling on doors and
    /// spent by the office's own daily operation; ELECTION-DAY TURNOUT = that operation's contacts
    /// accumulate in <see cref="RegionalMobilization"/> (W-B11), which election day reads (W-D1);
    /// LOCAL POLLING = an office region counts as polled at <see cref="LocalPollSampleSize"/>
    /// (the map's gate, W-E2 — a rider, the seam is the snapshot). §10's five attributes: cost
    /// (<see cref="OpenCost"/>), staff capacity (<see cref="StaffCapacity"/>, W-B5 fills it),
    /// volunteer capacity, regional influence, maintenance (<see cref="MaintenancePerDay"/>,
    /// paid every day the office stands — unpaid, the office STARVES: no recruiting, no operation,
    /// and its influence decays). All **[AUTHORED-DRAFT]**, one line each in calibration entry 14.
    ///
    /// **Concentration against spread is an economics, not a rule:** the opening cost and the
    /// maintenance are fixed per office, the mobilisation an operation buys is concave in the
    /// region (§35). At a prototype war chest the fixed costs dominate and three offices beat ten;
    /// at a large one the concavity wins and spreading pays — the harness measures the crossover
    /// rather than asserting a side.
    /// </summary>
    public static class CampaignOffices
    {
        public const double OpenCost = 100_000.0;
        public const double MaintenancePerDay = 2_000.0;
        public const int StaffCapacity = 3;
        public const int VolunteerCapacity = 150;
        public const int RecruitPerDay = 5;
        /// <summary>What a visit without organisation draws, as a fraction of a full office's audience.</summary>
        public const double VisitFraction = 0.25;
        public const int LocalPollSampleSize = 300;
        /// <summary>The influence a starved office loses per unpaid day.</summary>
        public const double StarvationPerDay = 0.10;

        /// <summary>The audience pool a rally or town hall draws on in a region — the electorate scaled by organisation.</summary>
        public static double LocalAudience(double regionElectorate, double influence)
        {
            double f = influence < 0 ? 0 : (influence > 1 ? 1 : influence);
            return Math.Max(0.0, regionElectorate) * (VisitFraction + (1.0 - VisitFraction) * f);
        }
    }

    /// <summary>One office: where, since when, how many volunteers it has recruited, what it spends on its operation each day.</summary>
    public sealed class CampaignOffice
    {
        public readonly int Region;
        public readonly int DayOpened;
        /// <summary>Money the office puts into its own daily ground operation (leaflets, phones, lifts) — drawn from the party each day.</summary>
        public readonly double OperationsPerDay;
        public int Volunteers;
        /// <summary>0–1: recruited volunteers over capacity, less what starvation has cost.</summary>
        public double Influence;
        public int StarvedDays;

        public CampaignOffice(int region, int dayOpened, double operationsPerDay)
        {
            Region = region; DayOpened = dayOpened; OperationsPerDay = Math.Max(0.0, operationsPerDay);
        }

        public double VolunteerHoursToday => CampaignEconomy.VolunteerHours(Volunteers);
    }

    /// <summary>One party's offices over the regions.</summary>
    public sealed class OfficeNetwork
    {
        private readonly CampaignOffice[] _byRegion;
        private readonly List<CampaignOffice> _offices = new List<CampaignOffice>();

        public OfficeNetwork(int regionCount)
        {
            if (regionCount <= 0) { throw new ArgumentException("no regions"); }
            _byRegion = new CampaignOffice[regionCount];
        }

        public int RegionCount => _byRegion.Length;
        public int Count => _offices.Count;
        public IReadOnlyList<CampaignOffice> Offices => _offices;
        public CampaignOffice At(int region) => _byRegion[region];
        public bool HasOffice(int region) => _byRegion[region] != null;
        public double Influence(int region) => _byRegion[region]?.Influence ?? 0.0;
        public double VolunteerHours(int region) => _byRegion[region]?.VolunteerHoursToday ?? 0.0;

        public int TotalVolunteers
        {
            get { int n = 0; foreach (CampaignOffice o in _offices) { n += o.Volunteers; } return n; }
        }

        /// <summary>What the network costs per day standing still: maintenance plus every office's operation.</summary>
        public double DailyCost
        {
            get { double c = 0.0; foreach (CampaignOffice o in _offices) { c += CampaignOffices.MaintenancePerDay + o.OperationsPerDay; } return c; }
        }

        /// <summary>Open an office, paying <see cref="CampaignOffices.OpenCost"/> from <paramref name="money"/>. False (and nothing paid) if the region already has one or the party cannot afford it.</summary>
        public bool Open(int region, int day, double operationsPerDay, ref double money)
        {
            if (region < 0 || region >= _byRegion.Length) { throw new ArgumentOutOfRangeException(nameof(region)); }
            if (_byRegion[region] != null || money < CampaignOffices.OpenCost) { return false; }
            money -= CampaignOffices.OpenCost;
            var office = new CampaignOffice(region, day, operationsPerDay);
            _byRegion[region] = office;
            _offices.Add(office);
            return true;
        }

        /// <summary>
        /// One day of the network: maintenance is paid office by office (an office the party cannot
        /// pay for STARVES today — no recruiting, no operation, influence down), the paid offices
        /// recruit toward capacity, and each runs its operation in its region — contacts into
        /// <paramref name="gotv"/> for <paramref name="party"/>, bounded by the operation's money
        /// and the office's volunteer-hours (W-B11's `Contacts`). Returns the money spent today.
        /// </summary>
        public double Day(RegionalMobilization gotv, int party, GotvOperation operation, ref double money, out double contacts, double scale = 1.0)
        {
            double spent = 0.0;
            contacts = 0.0;
            foreach (CampaignOffice o in _offices)
            {
                if (money < CampaignOffices.MaintenancePerDay)
                {
                    o.StarvedDays++;
                    o.Influence = Math.Max(0.0, o.Influence - CampaignOffices.StarvationPerDay);
                    continue;
                }

                money -= CampaignOffices.MaintenancePerDay;
                spent += CampaignOffices.MaintenancePerDay;
                int capacity = (int)Math.Round(CampaignOffices.VolunteerCapacity * Math.Max(1.0, scale));   // W-B5: the field organizer scales recruiting and capacity
                o.Volunteers = Math.Min(capacity, o.Volunteers + (int)Math.Round(CampaignOffices.RecruitPerDay * Math.Max(1.0, scale)));
                o.Influence = (double)o.Volunteers / capacity;

                double budget = Math.Min(o.OperationsPerDay, money);
                if (budget > 0.0 && gotv != null)
                {
                    contacts += gotv.Operate(o.Region, party, operation, budget, o.VolunteerHoursToday, out double moneySpent, out _);
                    money -= moneySpent;
                    spent += moneySpent;
                }
            }

            return spent;
        }
    }
}
