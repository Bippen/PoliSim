namespace PoliSim.Wording
{
    /// <summary>UI-1 (J8, Elias's ruling of 2026-10-19: one possessive helper): English forms the runtime builds on a name it does not know at compile time,
    /// in a namespace of their own so the model's layers (Elections, Simulation, Data) need not reach into the UI to write a sentence. The UI's
    /// <see cref="PoliSim.UI.UiFormat"/> forwards to it.</summary>
    public static class EnglishText
    {
        /// <summary>A name's possessive in the name's own case: "SWEDEN'S", "Sweden's", and a name ending in s takes the apostrophe alone ("UNITED STATES'",
        /// "United States'") - never "STATES'S". An absent name (null or empty) has no possessive: the empty string, never a bare "'s".</summary>
        public static string Possessive(string name)
        {
            if (string.IsNullOrEmpty(name)) { return string.Empty; }
            return Possessive(name, char.IsUpper(name[name.Length - 1]));
        }

        /// <summary>The possessive in the SENTENCE's case - <paramref name="upper"/> where the sentence is set in capitals ("THE GOVERNMENT IS S'S"), which the
        /// name's own last letter cannot tell (a party's key "KD" in a lower-case sentence reads "KD's"). A name ending in s takes the apostrophe alone
        /// ("Harris'", "UNITED STATES'"); an <paramref name="abbreviation"/> - a party's key or short name, read letter by letter or as a word the party
        /// chose ("S", "PiS", "AVS", "M5S") - always takes 's ("S's", "PiS's"), as English writes an initialism's possessive. An absent name: the empty string.</summary>
        public static string Possessive(string name, bool upper, bool abbreviation = false)
        {
            if (string.IsNullOrEmpty(name)) { return string.Empty; }
            char last = name[name.Length - 1];
            if (!abbreviation && (last == 's' || last == 'S')) { return name + "'"; }
            return name + (upper ? "'S" : "'s");
        }
    }
}
