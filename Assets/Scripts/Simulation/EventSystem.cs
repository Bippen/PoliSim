using System.Collections.Generic;
using PoliSim.Data;
using UnityEngine;

namespace PoliSim.Simulation
{
    /// <summary>
    /// One canned economic event: a one-turn shock plus UI-facing text. Deliberately plain data -
    /// EventSystem.EventPool is hardcoded for now, but nothing downstream (EventSystem.ApplyEvent,
    /// or a UI reading SimulationManager.GetLastEvent) cares where an EconomicEvent came from, so the
    /// pool can later be swapped for AI-generated events without changing how they're consumed.
    /// </summary>
    public class EconomicEvent
    {
        public string Name;
        public string Description;
        /// <summary>P4-D2 (2026-09-04): the kind of shock, for the record and the pool check - never read by the model.</summary>
        public EventTag Tag;
        /// <summary>P4-D2: the history the event mirrors, prefixed with the law catalog's citation class - CONFIRMED (the figure is the record's), CONFIRMED-DIRECTION (the sign is, the size is the game's), DIRECTIONAL (a rhyme, not a measurement), GENRE-IDIOM (no record exists; a game's convention). Text for the record, never read by the model.</summary>
        public string Analogue;
        public float GdpShockPercent;
        public float InflationShockPoints;
        public float ApprovalEffect;
        /// <summary>P4-D2: the magnitude band, DERIVED from the GDP shock by <see cref="EventBands.Of"/> - never authored, so it cannot contradict the number.</summary>
        public EventBand Band => EventBands.Of(GdpShockPercent);
    }

    /// <summary>P4-D2: what kind of shock an event is. A label for the record and the pool check.</summary>
    public enum EventTag { External, Domestic, Commodity, Financial, Political, Natural, Health, Technology, Trade, Labour }

    /// <summary>P4-D2: the magnitude bands, on the GDP shock in percent of GDP, the model's own first term.</summary>
    public enum EventBand { Minor, Moderate, Major, Severe }

    /// <summary>
    /// P4-D2 (2026-09-04): the stepped band rule for events, the way LawCatalog's magnitude taxonomy is stated once and
    /// applied everywhere. On the absolute GDP shock: MINOR below 0.5 % (a headline more than a shock), MODERATE 0.5 to
    /// under 1.0 (felt in the year's figures), MAJOR 1.0 to under 2.0 (a real recession-sized quarter or a boom), SEVERE
    /// 2.0 and above (Sweden 2020 was -2.2 %, 2009 -4.3 % - the model's ceiling of 3 sits inside the record). [AUTHORED-DRAFT]
    /// edges, DERIVED placement: an event's band is computed, never typed.
    /// </summary>
    public static class EventBands
    {
        public const float ModerateFrom = 0.5f;   // [AUTHORED-DRAFT] band edge, percent of GDP
        public const float MajorFrom = 1.0f;      // [AUTHORED-DRAFT] band edge
        public const float SevereFrom = 2.0f;     // [AUTHORED-DRAFT] band edge
        public const float Ceiling = 3.0f;        // [AUTHORED-DRAFT] the largest one-year shock the pool may carry - Sweden's 2009 (-4.3 %) stays outside the game's reach on purpose

        public static EventBand Of(float gdpShockPercent)
        {
            float a = Mathf.Abs(gdpShockPercent);
            return a >= SevereFrom ? EventBand.Severe : a >= MajorFrom ? EventBand.Major : a >= ModerateFrom ? EventBand.Moderate : EventBand.Minor;
        }
    }

    /// <summary>
    /// Small random-event layer: each turn, a small chance an event fires for a given country, with
    /// a one-time GDP/inflation shock plus an approval effect. A game-rule heuristic, not economic
    /// theory, so it's kept out of MacroSystem. GDP shocks fade on their own via MacroSystem's
    /// existing output-gap reversion (PotentialGDP is untouched by events, so actual GDP drifts back
    /// toward it); inflation shocks are genuinely one-turn, since the Phillips Curve fully
    /// recomputes Inflation from scratch next turn rather than carrying a delta forward.
    /// </summary>
    public static class EventSystem
    {
        /// <summary>[AUTHORED-DRAFT] pacing figure: chance per country per turn that an event fires. P4-D1 (2026-09-04) MEASURED
        /// the old 0.12: one turn is one year (SimulationManager.DaysPerTurn), so a ten-year game expected 1.2 events and 28 % of
        /// games saw none - the slot on the Desk read "quiet" for most of most games. P4-D2 sets 0.40: four events in ten years,
        /// under 1 % of games with none. DEFENDED against the record, not against feel: Sweden 1990-2024 carried eight to ten shocks
        /// of the MAJOR and SEVERE bands (1992, 2001, 2008-09, 2015, 2018, 2020, 2022, 2023) - about 0.3 a year - and the pool is
        /// half MINOR and MODERATE events that history has more of; 0.40 is that rate plus the small ones. `EventRateDiagnostic`
        /// measures the realised rate against this figure on the simulation bar. `CabinetSystem.DecisionChancePerTurn` (0.12, its own
        /// constant, which matched this one until P4-D2) stays where it was: the Docket's cadence is not the news's.</summary>
        public const float EventChancePerTurn = 0.40f;

        private static System.Random RandomSource => SimulationRandom.For(SimulationRandom.Stream.Event);

        private static readonly List<EconomicEvent> EventPool = new List<EconomicEvent>
        {
            new EconomicEvent
            {
                Name = "Recession in a Trading Partner",
                Tag = EventTag.External, Analogue = "CONFIRMED: Germany 2009 (GDP -5.7 %, Destatis) took Swedish goods exports down ~17 % and Sweden's GDP -4.3 % (SCB); a partner's recession is the shock a small open economy feels first.",
                Description = "A major trading partner has slipped into recession, softening demand for our exports.",
                GdpShockPercent = -2.0f, InflationShockPoints = 0f, ApprovalEffect = -3f
            },
            new EconomicEvent
            {
                Name = "Global Commodity Price Spike",
                Tag = EventTag.Commodity, Analogue = "CONFIRMED: 2021-22 - Sweden's CPI reached 8.4 % in 2022 (SCB) on energy and commodity prices; the +1.5 pts here is a fraction of that year.",
                Description = "A spike in global commodity prices is pushing up costs across the economy.",
                GdpShockPercent = -1.0f, InflationShockPoints = 1.5f, ApprovalEffect = -4f
            },
            new EconomicEvent
            {
                Name = "Diplomatic Dispute With a Trade Partner",
                Tag = EventTag.Political, Analogue = "DIRECTIONAL: China's 2020 measures on Australian barley, wine and coal touched exports of roughly A$20 bn, about 1 % of Australia's GDP (DFAT).",
                Description = "A diplomatic dispute has disrupted trade with one of our partners.",
                GdpShockPercent = -1.5f, InflationShockPoints = 0.3f, ApprovalEffect = -2f
            },
            new EconomicEvent
            {
                Name = "Technology Breakthrough",
                Tag = EventTag.Technology, Analogue = "GENRE-IDIOM: no invention shows as a one-year GDP jump in any statistical record; a game's tempo, sized inside the MAJOR band.",
                Description = "A domestic technology breakthrough is boosting productivity.",
                GdpShockPercent = 1.5f, InflationShockPoints = 0f, ApprovalEffect = 3f
            },
            new EconomicEvent
            {
                Name = "Favorable Trade Agreement",
                Tag = EventTag.Trade, Analogue = "DIRECTIONAL: the EU-Korea FTA (2011) lifted EU exports to Korea by about 55 % over five years (European Commission); the sign is history's, the one-year pace is the game's.",
                Description = "A new trade agreement is opening up favorable terms with a partner.",
                GdpShockPercent = 1.0f, InflationShockPoints = -0.2f, ApprovalEffect = 2f
            },
            new EconomicEvent
            {
                Name = "Natural Disaster",
                Tag = EventTag.Natural, Analogue = "DIRECTIONAL: Hurricane Katrina (2005) cost about US$125 bn (NOAA), near 1 % of US GDP; Christchurch (2011) about NZ$40 bn, a fifth of New Zealand's.",
                Description = "A natural disaster has disrupted economic activity in the country.",
                GdpShockPercent = -2.5f, InflationShockPoints = 0.5f, ApprovalEffect = -5f
            },
            new EconomicEvent
            {
                Name = "Consumer Confidence Surge",
                Tag = EventTag.Domestic, Analogue = "GENRE-IDIOM: confidence indices lead spending by a quarter or two (Konjunkturinstitutet); a one-year +1 % of GDP is the upper edge of what that literature supports.",
                Description = "A wave of optimism is boosting consumer spending.",
                GdpShockPercent = 1.0f, InflationShockPoints = 0.2f, ApprovalEffect = 2f
            },
            new EconomicEvent
            {
                Name = "Energy Price Shock",
                Tag = EventTag.Commodity, Analogue = "CONFIRMED: the 1973-74 oil embargo - US CPI 11 % in 1974 (BLS), OECD growth halved; Europe's 2022 gas shock rhymed with it.",
                Description = "A sudden spike in energy prices is squeezing households and businesses.",
                GdpShockPercent = -1.5f, InflationShockPoints = 1.0f, ApprovalEffect = -3f
            },
            new EconomicEvent
            {
                Name = "Banking Sector Stress",
                Tag = EventTag.Financial, Analogue = "CONFIRMED: Sweden's banking crisis - GDP -1.2 % in 1992 and -2.0 % in 1993 (SCB), Nordbanken and Gota nationalised.",
                Description = "Stress at a major financial institution is tightening credit conditions.",
                GdpShockPercent = -2.0f, InflationShockPoints = 0f, ApprovalEffect = -4f
            },
            new EconomicEvent
            {
                Name = "Major Cyberattack on Financial Infrastructure",
                Tag = EventTag.Technology, Analogue = "DIRECTIONAL: NotPetya (2017) cost about US$10 bn worldwide (White House estimate); Maersk alone booked ~US$300 m.",
                Description = "A cyberattack has disrupted banking and payment systems nationwide.",
                GdpShockPercent = -1.0f, InflationShockPoints = 0f, ApprovalEffect = -3.5f
            },
            new EconomicEvent
            {
                Name = "Regional Conflict Disrupts Trade Routes",
                Tag = EventTag.External, Analogue = "DIRECTIONAL: Red Sea attacks (2024) halved Suez transits and doubled to tripled container rates (IMF PortWatch, Drewry WCI).",
                Description = "An armed conflict abroad has disrupted key shipping lanes our trade relies on.",
                GdpShockPercent = -1.5f, InflationShockPoints = 0.8f, ApprovalEffect = -3f
            },
            new EconomicEvent
            {
                Name = "Severe Drought Hits Agricultural Output",
                Tag = EventTag.Natural, Analogue = "CONFIRMED: Sweden's 2018 drought - the cereal harvest about 43 % below 2017, the smallest since 1959 (Jordbruksverket).",
                Description = "A severe drought has damaged agricultural output and pushed up food prices.",
                GdpShockPercent = -1.0f, InflationShockPoints = 0.6f, ApprovalEffect = -2.5f
            },
            new EconomicEvent
            {
                Name = "Public Health Emergency",
                Tag = EventTag.Health, Analogue = "CONFIRMED: COVID-19 - Sweden's GDP -2.2 % in 2020 (SCB), the euro area's -6.1 % (Eurostat).",
                Description = "An outbreak of illness is disrupting workplaces and consumer activity.",
                GdpShockPercent = -2.0f, InflationShockPoints = 0.3f, ApprovalEffect = -3f
            },
            new EconomicEvent
            {
                Name = "Sovereign Credit Rating Downgrade",
                Tag = EventTag.Financial, Analogue = "DIRECTIONAL: S&P's August 2011 US downgrade moved yields, not growth; Greece's 2010 downgrades preceded spreads above 10 points - the MINOR band fits a country with a market, not a programme.",
                Description = "A credit rating agency has downgraded our sovereign debt outlook.",
                GdpShockPercent = -0.5f, InflationShockPoints = 0.4f, ApprovalEffect = -3f
            },
            new EconomicEvent
            {
                Name = "Major Labor Strikes",
                Tag = EventTag.Labour, Analogue = "DIRECTIONAL: the UK's 1978-79 Winter of Discontent - 29 million working days lost (ONS); France's 1995 strikes cost a few tenths of a quarter's GDP (INSEE).",
                Description = "Widespread strikes have disrupted production across several industries.",
                GdpShockPercent = -1.0f, InflationShockPoints = 0.2f, ApprovalEffect = -2f
            },
            new EconomicEvent
            {
                Name = "Housing Market Correction",
                Tag = EventTag.Financial, Analogue = "CONFIRMED: Sweden 1991-93 - real house prices down by a quarter and more (SCB) alongside the banking crisis; US 2007-09 Case-Shiller -27 %.",
                Description = "A cooling housing market is weighing on construction and household wealth.",
                GdpShockPercent = -1.2f, InflationShockPoints = -0.3f, ApprovalEffect = -2.5f
            },
            new EconomicEvent
            {
                Name = "Major Foreign Investment Announcement",
                Tag = EventTag.Domestic, Analogue = "DIRECTIONAL: Northvolt's Skellefteå plant (announced 2017, ~EUR 4 bn) was near 0.8 % of Sweden's GDP on announcement; Intel's Magdeburg EUR 30 bn (2022) was later shelved.",
                Description = "A large foreign investor has committed to a major new domestic project.",
                GdpShockPercent = 1.2f, InflationShockPoints = 0f, ApprovalEffect = 2.5f
            },
            new EconomicEvent
            {
                Name = "Bumper Harvest",
                Tag = EventTag.Natural, Analogue = "DIRECTIONAL: Sweden's 2019 cereal harvest about 6.1 Mt, up ~90 % on 2018 (Jordbruksverket); agriculture is ~1.3 % of GDP, so +0.8 % of GDP is the game's size, not the sector's.",
                Description = "An unusually strong harvest is boosting output and easing food prices.",
                GdpShockPercent = 0.8f, InflationShockPoints = -0.4f, ApprovalEffect = 1.5f
            },
            new EconomicEvent
            {
                Name = "Tourism Boom",
                Tag = EventTag.External, Analogue = "DIRECTIONAL: Iceland 2010-17 - arrivals more than quadrupled, tourism to ~8 % of GDP (Statistics Iceland).",
                Description = "A surge in tourist arrivals is boosting the services sector.",
                GdpShockPercent = 1.0f, InflationShockPoints = 0.1f, ApprovalEffect = 2f
            },
            new EconomicEvent
            {
                Name = "Natural Resource Discovery",
                Tag = EventTag.Commodity, Analogue = "DIRECTIONAL: LKAB's Kiruna rare-earth find (announced January 2023, over 1 Mt of oxides); Norway's Ekofisk (1969) is the genre's original.",
                Description = "A newly discovered domestic resource deposit is drawing investment.",
                GdpShockPercent = 1.3f, InflationShockPoints = 0f, ApprovalEffect = 3f
            },
            new EconomicEvent
            {
                Name = "Successful Multilateral Trade Summit",
                Tag = EventTag.Trade, Analogue = "DIRECTIONAL: the Uruguay Round's conclusion (1994) - estimated world-income gains of US$200-500 bn a year (GATT and OECD estimates of the time).",
                Description = "A trade summit has produced favorable new terms with several partners.",
                GdpShockPercent = 0.7f, InflationShockPoints = -0.1f, ApprovalEffect = 2.5f
            },
            new EconomicEvent
            {
                Name = "Corruption Scandal Rocks Government",
                Tag = EventTag.Political, Analogue = "CONFIRMED: Brazil's Lava Jato (2015-16) - the president's approval fell to about 10 % (Datafolha) while GDP fell 3.5 % and 3.3 %; the approval figure here is the scandal's, the GDP the recession's.",
                Description = "Revelations of corruption have damaged public trust in the government.",
                GdpShockPercent = -0.3f, InflationShockPoints = 0f, ApprovalEffect = -5f
            },
            new EconomicEvent
            {
                Name = "Medical Breakthrough",
                Tag = EventTag.Health, Analogue = "GENRE-IDIOM: sized like Technology Breakthrough and for the same reason.",
                Description = "A domestic medical breakthrough is improving public health and productivity.",
                GdpShockPercent = 1.0f, InflationShockPoints = 0f, ApprovalEffect = 2.5f
            },
            new EconomicEvent
            {
                Name = "Stock Market Rally",
                Tag = EventTag.Financial, Analogue = "DIRECTIONAL: the wealth effect runs at 3-5 cents of spending per dollar of equity gains (Case, Quigley and Shiller; Federal Reserve staff estimates).",
                Description = "A strong rally in domestic markets is lifting investor and consumer confidence.",
                GdpShockPercent = 0.6f, InflationShockPoints = 0.1f, ApprovalEffect = 1.5f
            },
            // ---- P4-D2 (2026-09-04): the second half of the pool. Same three terms, each event tagged, banded by
            // ---- its GDP shock (EventBands) and carrying its analogue with the citation class the law catalog uses.
            new EconomicEvent
            {
                Name = "Currency Under Attack",
                Tag = EventTag.Financial, Analogue = "CONFIRMED: September 1992 - the Riksbank's 500 % marginal rate failed to hold the krona, floated 19 November 1992; the UK left the ERM the same autumn.",
                Description = "Speculators are testing the currency; the central bank is burning reserves and rates to hold the line.",
                GdpShockPercent = -1.5f, InflationShockPoints = 1.2f, ApprovalEffect = -4f
            },
            new EconomicEvent
            {
                Name = "Sharp Rate Rise Abroad",
                Tag = EventTag.External, Analogue = "DIRECTIONAL: the Federal Reserve's 2022 tightening (0.25 % to 4.5 % in a year) and Volcker's 1979-81 - capital flows and import prices follow the dollar.",
                Description = "A major central bank has tightened hard; capital is flowing out and borrowing costs are rising with it.",
                GdpShockPercent = -0.6f, InflationShockPoints = -0.3f, ApprovalEffect = -1f
            },
            new EconomicEvent
            {
                Name = "Pension Fund Losses",
                Tag = EventTag.Financial, Analogue = "DIRECTIONAL: the UK's LDI crisis (September 2022) - gilt yields spiked, the Bank of England bought bonds for a fortnight to stop pension funds selling into the fall.",
                Description = "A market lurch has torn a hole in pension balance sheets; savers are alarmed and the regulator is on the phone.",
                GdpShockPercent = -0.4f, InflationShockPoints = 0f, ApprovalEffect = -3f
            },
            new EconomicEvent
            {
                Name = "Export Boom in a Key Partner",
                Tag = EventTag.External, Analogue = "DIRECTIONAL: Germany's goods exports rose about 14 % in 2010 (Destatis) as China's demand for machinery recovered; suppliers upstream boomed with it.",
                Description = "A major partner's economy is running hot and pulling in our exports.",
                GdpShockPercent = 1.2f, InflationShockPoints = 0.2f, ApprovalEffect = 2f
            },
            new EconomicEvent
            {
                Name = "Global Recession",
                Tag = EventTag.External, Analogue = "CONFIRMED: 2009 - world GDP -1.3 % (IMF WEO), Sweden -4.3 % (SCB), world trade down more than a tenth.",
                Description = "The world economy has contracted; export orders are collapsing everywhere at once.",
                GdpShockPercent = -3.0f, InflationShockPoints = -0.8f, ApprovalEffect = -5f
            },
            new EconomicEvent
            {
                Name = "Sovereign Debt Crisis Contagion",
                Tag = EventTag.Financial, Analogue = "CONFIRMED: the euro crisis - the euro area's GDP -0.9 % in 2012 (Eurostat) as spreads in the periphery pulled credit tight across the bloc.",
                Description = "A neighbour's debt crisis is spreading through the banks and bond markets that we share.",
                GdpShockPercent = -1.2f, InflationShockPoints = 0f, ApprovalEffect = -3f
            },
            new EconomicEvent
            {
                Name = "Hospital System Crisis",
                Tag = EventTag.Health, Analogue = "DIRECTIONAL: the winter of 2022-23 - emergency waits at records across the NHS and Swedish regions; sick leave rather than output is the first-order cost.",
                Description = "A brutal winter season has overwhelmed the hospitals; waits are at records and the headlines are grim.",
                GdpShockPercent = -0.3f, InflationShockPoints = 0f, ApprovalEffect = -2.5f
            },
            new EconomicEvent
            {
                Name = "Wildfires",
                Tag = EventTag.Natural, Analogue = "CONFIRMED: Sweden's summer of 2018 - about 25,000 ha burned, the largest fires in modern times (MSB); Canada 2023 burned 18 million ha.",
                Description = "Forest fires are burning out of control through the driest summer on record.",
                GdpShockPercent = -0.5f, InflationShockPoints = 0.1f, ApprovalEffect = -2f
            },
            new EconomicEvent
            {
                Name = "Severe Flooding",
                Tag = EventTag.Natural, Analogue = "CONFIRMED: the Ahr valley floods (Germany, July 2021) - about EUR 40 bn of damage, near 1.1 % of GDP (German federal government).",
                Description = "Rivers have burst their banks across the lowlands; towns, roads and rail are under water.",
                GdpShockPercent = -0.8f, InflationShockPoints = 0.2f, ApprovalEffect = -2.5f
            },
            new EconomicEvent
            {
                Name = "Cold Snap Energy Squeeze",
                Tag = EventTag.Natural, Analogue = "DIRECTIONAL: Texas, February 2021 - a week of outages and spot power at the US$9,000/MWh cap; northern Europe's cold-week price spikes rhyme at a smaller scale.",
                Description = "A prolonged cold snap has sent power prices through the roof and left parts of the grid dark.",
                GdpShockPercent = -0.4f, InflationShockPoints = 0.6f, ApprovalEffect = -2f
            },
            new EconomicEvent
            {
                Name = "Major Industrial Accident",
                Tag = EventTag.Domestic, Analogue = "DIRECTIONAL: Deepwater Horizon (2010) - BP's total cost about US$65 bn over a decade; the local economy's loss was a fraction, the political cost was not.",
                Description = "An explosion at a major plant has killed workers and shut a piece of the country's industrial core.",
                GdpShockPercent = -0.3f, InflationShockPoints = 0f, ApprovalEffect = -2f
            },
            new EconomicEvent
            {
                Name = "Infrastructure Collapse",
                Tag = EventTag.Domestic, Analogue = "CONFIRMED-DIRECTION: the Morandi bridge (Genoa, August 2018) - 43 dead, the port's road link cut for two years; the political reckoning outran the economic one.",
                Description = "A major bridge has come down; the inquiry is asking who signed off on the inspections.",
                GdpShockPercent = -0.2f, InflationShockPoints = 0f, ApprovalEffect = -3f
            },
            new EconomicEvent
            {
                Name = "Terror Attack",
                Tag = EventTag.Political, Analogue = "DIRECTIONAL: the rally-round-the-flag effect - approval of the US president rose to about 90 % after September 2001 (Gallup); Stockholm's April 2017 attack moved the economy little.",
                Description = "An attack in the capital has shaken the country; the nation is rallying behind its institutions.",
                GdpShockPercent = -0.4f, InflationShockPoints = 0f, ApprovalEffect = 3f
            },
            new EconomicEvent
            {
                Name = "Migration Surge",
                Tag = EventTag.Political, Analogue = "CONFIRMED-DIRECTION: 2015 - about 163,000 asylum applications in Sweden (Migrationsverket); public spending rose, and the government's standing fell through the year.",
                Description = "Arrivals have surged past anything the reception system was built for; spending is up and so is the temperature of the debate.",
                GdpShockPercent = 0.3f, InflationShockPoints = 0f, ApprovalEffect = -3f
            },
            new EconomicEvent
            {
                Name = "Tech Sector Bust",
                Tag = EventTag.Technology, Analogue = "CONFIRMED: the dot-com bust (2000-02) - the Stockholm exchange lost about 70 % from its March 2000 peak; Ericsson shed tens of thousands of jobs.",
                Description = "The technology boom has turned; valuations are collapsing and the start-ups are laying off.",
                GdpShockPercent = -1.0f, InflationShockPoints = -0.2f, ApprovalEffect = -2f
            },
            new EconomicEvent
            {
                Name = "Productivity Wave",
                Tag = EventTag.Technology, Analogue = "DIRECTIONAL: the US acceleration of 1995-2000 - labour productivity growth roughly doubled to ~2.5 % a year (BLS) as computing diffused.",
                Description = "New tools are spreading through the economy faster than anyone forecast; output per hour is climbing.",
                GdpShockPercent = 0.8f, InflationShockPoints = -0.3f, ApprovalEffect = 1.5f
            },
            new EconomicEvent
            {
                Name = "Landmark Export Contract",
                Tag = EventTag.Trade, Analogue = "DIRECTIONAL: Saab's Gripen contract with Brazil (2014, about US$5 bn) - near 1 % of a year's Swedish goods exports, delivered over a decade.",
                Description = "A flagship exporter has landed a contract the size of a small industry.",
                GdpShockPercent = 0.5f, InflationShockPoints = 0f, ApprovalEffect = 1.5f
            },
            new EconomicEvent
            {
                Name = "Major Factory Closure",
                Tag = EventTag.Labour, Analogue = "CONFIRMED: Saab Automobile's bankruptcy (Trollhättan, December 2011) - about 3,500 direct jobs, a town of 55,000.",
                Description = "A plant that anchored a whole town is closing; the supply chain around it is going with it.",
                GdpShockPercent = -0.4f, InflationShockPoints = 0f, ApprovalEffect = -2.5f
            },
            new EconomicEvent
            {
                Name = "Forest Blight",
                Tag = EventTag.Natural, Analogue = "DIRECTIONAL: the spruce bark beetle outbreak of 2018-21 - around 8 million cubic metres of damaged timber a year at its peak (Skogsstyrelsen).",
                Description = "A pest outbreak is killing standing timber faster than it can be felled and sold.",
                GdpShockPercent = -0.3f, InflationShockPoints = 0.1f, ApprovalEffect = -1f
            },
            new EconomicEvent
            {
                Name = "Currency Appreciation Shock",
                Tag = EventTag.Financial, Analogue = "DIRECTIONAL: the Swiss franc's unpegging (January 2015) - up about 20 % against the euro in a day; Swiss exporters and the price level felt it for years.",
                Description = "The currency has jumped; exporters are losing orders and imported goods are suddenly cheap.",
                GdpShockPercent = -0.6f, InflationShockPoints = -0.5f, ApprovalEffect = 0f
            },
            new EconomicEvent
            {
                Name = "Commodity Prices Collapse",
                Tag = EventTag.Commodity, Analogue = "CONFIRMED: oil from about US$110 to under US$30 between mid-2014 and early 2016; euro-area inflation 0.0 % in 2015 (Eurostat) - an importer's windfall.",
                Description = "Commodity prices have fallen off a cliff; the import bill is shrinking and so is inflation.",
                GdpShockPercent = 0.4f, InflationShockPoints = -0.8f, ApprovalEffect = 1f
            },
            new EconomicEvent
            {
                Name = "Wage Round Overshoots",
                Tag = EventTag.Labour, Analogue = "DIRECTIONAL: the UK's 1975 (retail prices up 24 %, ONS) and Sweden's 1970s-80s rounds - pay settlements ahead of productivity fed straight into prices and devaluations.",
                Description = "The wage round has settled far above productivity; prices are following the pay slips up.",
                GdpShockPercent = -0.2f, InflationShockPoints = 1.0f, ApprovalEffect = -1f
            },
            new EconomicEvent
            {
                Name = "Anti-Government Protests",
                Tag = EventTag.Political, Analogue = "DIRECTIONAL: France's gilets jaunes (2018-19) - INSEE put the fourth-quarter 2018 cost near 0.1 point of growth; the government's approval fell far further.",
                Description = "Weeks of protest have shut city centres; the government's standing is taking the damage.",
                GdpShockPercent = -0.2f, InflationShockPoints = 0f, ApprovalEffect = -4f
            },
            new EconomicEvent
            {
                Name = "Trade Partner Turns Protectionist",
                Tag = EventTag.Trade, Analogue = "CONFIRMED-DIRECTION: the US steel and aluminium tariffs of 2018 (25 % and 10 %, Section 232) - European steel exports to the US fell by a fifth within a year (Eurofer).",
                Description = "A major partner has raised tariffs across the board; our exporters are on the wrong side of them.",
                GdpShockPercent = -0.8f, InflationShockPoints = 0.3f, ApprovalEffect = -1.5f
            },
            new EconomicEvent
            {
                Name = "Public Service Breakdown",
                Tag = EventTag.Political, Analogue = "CONFIRMED-DIRECTION: the Transportstyrelsen data breach (2017) - two ministers resigned; the state's competence, not its output, was what the public priced.",
                Description = "A core public system has failed in public view; ministers are answering for it and the opposition smells blood.",
                GdpShockPercent = -0.1f, InflationShockPoints = 0f, ApprovalEffect = -3f
            },
            new EconomicEvent
            {
                Name = "Sovereign Rating Upgrade",
                Tag = EventTag.Financial, Analogue = "DIRECTIONAL: Ireland regained A-grade ratings through 2014 after its programme; borrowing costs fell and the government claimed the credit.",
                Description = "The rating agencies have upgraded the sovereign; borrowing is cheaper and the treasury is smiling.",
                GdpShockPercent = 0.3f, InflationShockPoints = -0.1f, ApprovalEffect = 2f
            },
        };

        /// <summary>Rolls whether an event fires this turn; returns null (no event) most of the time.</summary>
        public static EconomicEvent TryRollEvent()
        {
            if (RandomSource.NextDouble() > EventChancePerTurn)
            {
                return null;
            }

            int index = RandomSource.Next(EventPool.Count);
            return EventPool[index];
        }

        /// <summary>Applies a one-time GDP/inflation/approval shock. No-op if economicEvent is null (the common case - no event this turn).</summary>
        public static void ApplyEvent(Country country, EconomicEvent economicEvent)
        {
            if (economicEvent == null)
            {
                return;
            }

            EconomyState state = country.State;
            state.GDP = Mathf.Max(MacroSystem.MinGdp, state.GDP * (1f + economicEvent.GdpShockPercent / 100f));
            state.Inflation = Mathf.Clamp(state.Inflation + economicEvent.InflationShockPoints, 0f, MacroSystem.MaxInflationPercent);
            state.ApprovalRating = Mathf.Clamp(state.ApprovalRating + economicEvent.ApprovalEffect, 0f, 100f);
        }
    }
}
