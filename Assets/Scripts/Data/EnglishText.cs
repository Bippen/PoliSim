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

        /// <summary>The review's M4: a CamelCase identifier spaced, keeping acronym runs intact - `HHSDiscretionary` reads "HHS Discretionary", not
        /// "H H S Discretionary", and `UBI` is left alone. A boundary is an upper-case letter that follows a lower-case one (the ordinary word break) or
        /// precedes one while following another upper-case letter (the end of an acronym run). Moved from the UI's <see cref="PoliSim.UI.DisplayName"/>,
        /// which forwards to it, so the model's names (<see cref="PoliSim.Data.EnumNames"/>) space an identifier the way the screens always have.</summary>
        public static string Spaced(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) { return identifier; }
            var builder = new System.Text.StringBuilder(identifier.Length + 8);
            builder.Append(identifier[0]);
            for (int i = 1; i < identifier.Length; i++)
            {
                char current = identifier[i];
                if (char.IsUpper(current))
                {
                    bool followsLower = char.IsLower(identifier[i - 1]);
                    bool endsAcronym = char.IsUpper(identifier[i - 1]) && i + 1 < identifier.Length && char.IsLower(identifier[i + 1]);
                    if (followsLower || endsAcronym) { builder.Append(' '); }
                }
                builder.Append(current);
            }
            return builder.ToString();
        }
    }
}
