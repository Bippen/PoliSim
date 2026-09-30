using System;
using System.Collections.Generic;

namespace PoliSim.Data
{
    /// <summary>
    /// §681: **THE PARTY FAMILIES, THE EXPERT SURVEY'S OWN CLASSIFICATION.** CHES 2024's `family` variable (`CHES_2024_final_v2.csv`, fetched
    /// byte-exact to `ElectionsData/positions/raw/`; the codebook's table, `CHES.2024.Codebook.pdf`: 1 Radical Right, 2 Conservatives, 3 Liberal,
    /// 4 Christian-Democratic, 5 Socialist, 6 Radical Left, 7 Green, 8 Regionalist, 9 No family, 10 Confessional, 11 Agrarian/Centre - "initially
    /// based on Hix and Lord (1997) and Marks and Wilson (2000)"). Each game key names the CHES party it is read from, so `EntrantLayerDiagnostic`
    /// proves every value against the file rather than against this table. <b>No family (-1)</b> where CHES classifies no single party for the
    /// key - an electoral alliance of parties in different families (France's UG, Italy's AVS, Poland's TD and the 2019 SLD list), a party CHES
    /// does not cover, or CHES's own "No family" (9). A party of no family is its own nest: the grouping never draws on it first.
    /// </summary>
    public static class PartyFamilies
    {
        public const int RadicalRight = 1, Conservatives = 2, Liberal = 3, ChristianDemocratic = 4, Socialist = 5, RadicalLeft = 6, Green = 7,
            Regionalist = 8, NoFamily = 9, Confessional = 10, AgrarianCentre = 11;

        /// <summary>The CHES country code and party name each key is read from, and its family as this table states it (proved against the CSV).</summary>
        public static readonly IReadOnlyList<(CountryId Country, string Key, int ChesCountry, string ChesParty, int Family)> Table = new[]
        {
            (CountryId.Sweden, "S", 16, "SAP", Socialist), (CountryId.Sweden, "SD", 16, "SD", RadicalRight), (CountryId.Sweden, "M", 16, "M", Conservatives),
            (CountryId.Sweden, "V", 16, "V", RadicalLeft), (CountryId.Sweden, "C", 16, "C", AgrarianCentre), (CountryId.Sweden, "KD", 16, "KD", ChristianDemocratic),
            (CountryId.Sweden, "MP", 16, "MP", Green), (CountryId.Sweden, "L", 16, "L", Liberal),
            (CountryId.Germany, "CDU", 3, "CDU", ChristianDemocratic), (CountryId.Germany, "CSU", 3, "CSU", ChristianDemocratic), (CountryId.Germany, "AfD", 3, "AfD", RadicalRight),
            (CountryId.Germany, "SPD", 3, "SPD", Socialist), (CountryId.Germany, "Grune", 3, "Grunen", Green), (CountryId.Germany, "Linke", 3, "DL", RadicalLeft),
            (CountryId.Germany, "BSW", 3, "BSW", RadicalLeft), (CountryId.Germany, "FDP", 3, "FDP", Liberal),
            (CountryId.France, "RN", 6, "RN", RadicalRight), (CountryId.France, "LR", 6, "LR", Conservatives), (CountryId.France, "ENS", 6, "RE", Liberal),
            (CountryId.France, "HOR", 6, "Horizons", Liberal), (CountryId.France, "SOC", 6, "PS", Socialist),
            (CountryId.Italy, "FdI", 8, "FDI", RadicalRight), (CountryId.Italy, "Lega", 8, "Lega", RadicalRight), (CountryId.Italy, "PD", 8, "PD", Socialist),
            (CountryId.Italy, "FI", 8, "FI", Conservatives), (CountryId.Italy, "AzIV", 8, "A", Liberal), (CountryId.Italy, "SVP", 8, "SVP", Regionalist),
            (CountryId.Italy, "PlusE", 8, "+E", Liberal), (CountryId.Italy, "M5S", 8, "MS5", NoFamily),
            (CountryId.Poland, "PiS", 26, "PiS", RadicalRight), (CountryId.Poland, "Konf", 26, "Konfederacja", RadicalRight), (CountryId.Poland, "KO", 26, "PO", ChristianDemocratic),
            (CountryId.Poland, "NL", 26, "Nowa Lewica", Socialist), (CountryId.Poland, "PSL", 26, "PSL", AgrarianCentre),
        };

        /// <summary>A key's family for the grouping, or -1 (no family, its own nest). CHES's "No family" is -1 here.</summary>
        public static int Of(CountryId country, string key)
        {
            foreach ((CountryId c, string k, int _, string _, int family) in Table) { if (c == country && k == key) { return family == NoFamily ? -1 : family; } }
            foreach (CreatedParty created in CreatedParties.Of(country)) { if (created.Key == key) { return OfCreated(created); } }
            return -1;
        }

        /// <summary>[AUTHORED-DRAFT] a created party's family: a Splinter's is its parent's; any other takes the family of the real party nearest it on
        /// the vote model's two axes - no expert has classified it.</summary>
        private static int OfCreated(CreatedParty party)
        {
            if (party.Origin == PartyOrigin.Splinter && !string.IsNullOrEmpty(party.ParentKey)) { return Of(party.Country, party.ParentKey); }
            double best = double.MaxValue;
            string nearest = null;
            foreach (PoliticalParty p in PartySystems.RealRoster(party.Country))
            {
                if (!p.HasPosition) { continue; }
                double d = (p.LrEcon - party.LrEcon) * (p.LrEcon - party.LrEcon) + (p.Galtan - party.Galtan) * (p.Galtan - party.Galtan);
                if (d < best) { best = d; nearest = p.Abbrev; }
            }
            return nearest == null ? -1 : Of(party.Country, nearest);
        }

        public static int[] For(CountryId country, IReadOnlyList<string> keys)
        {
            var f = new int[keys.Count];
            for (int i = 0; i < f.Length; i++) { f[i] = Of(country, keys[i]); }
            return f;
        }
    }
}
