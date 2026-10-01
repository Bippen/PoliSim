using System.Globalization;
using System.Threading;

namespace PoliSim.UI
{
    /// <summary>
    /// §568 (2026-09-22, the sitting pass's Track 4, drift row D1), §718 (Elias's ruling of 2026-10-01, item 9) — **ONE FIXED ENGLISH CULTURE FOR ALL
    /// DATES AND NUMBERS.**
    ///
    /// <para><b>The finding</b> (Design's whole-game reading, D1): one page read <c>30.1%</c> and the page beside it read <c>30,1%</c> - a figure
    /// interpolated straight into a string (<c>$"{x:F1}%"</c>) takes the THREAD's culture, which is whatever machine the game runs on. §568 fixed the
    /// numbers alone and kept the dates in the machine's culture on purpose (board 1k's calendar sheet read its month and weekday names from the
    /// machine), so on the sv-SE machine this project is built on the desk calendar read <c>JAN.</c>, <c>MARS</c>, <c>MÅN TIS ONS</c> beside an
    /// English page.</para>
    ///
    /// <para><b>The ruling</b> (item 9): <i>one fixed English culture for all dates and numbers, with a check that fails on machine-locale
    /// formatting.</i> So the culture installed here no longer derives from the machine at all: <b>en-GB's names and calendar</b> (English month and
    /// weekday names, the week starting on Monday as board 1k's sheet was drawn) with <b>the invariant number format</b> (a point, the comma
    /// grouping, the hyphen minus - exactly §568's numbers, so no figure moves). The machine's culture reaches nothing the game formats.</para>
    ///
    /// <para><b>Installed once, at the game's own start</b> (<c>GameController.Start</c>), and by the film harness before it draws. It is deliberately
    /// NOT a fix at the call sites: a culture is a property of the surface, not of the site that happens to print it. <c>NumberLocaleCheck</c> holds
    /// it: the one install, the battery under hostile machine cultures, and no runtime source that builds or reads another culture.</para>
    /// </summary>
    public static class UiCulture
    {
        /// <summary>The one culture every surface formats in: en-GB's names and calendar with the invariant number format. Read-only.</summary>
        public static CultureInfo English { get; } = Build();

        /// <summary>§568's name for the installed culture, kept for its callers: it is <see cref="English"/>.</summary>
        public static CultureInfo Numbers => English;

        private static CultureInfo Build()
        {
            var english = (CultureInfo)CultureInfo.GetCultureInfo("en-GB").Clone();
            english.NumberFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            return CultureInfo.ReadOnly(english);
        }

        /// <summary>
        /// Installs the one culture on this thread and as every new thread's default - the culture and the UI culture both. Safe to call more than
        /// once and from either the game or the harness; whatever culture the machine (or a harness's <c>-shotlocale=</c>) set before it, nothing
        /// of that survives the call.
        /// </summary>
        public static void Install()
        {
            CultureInfo.DefaultThreadCurrentCulture = English;
            CultureInfo.DefaultThreadCurrentUICulture = English;
            Thread.CurrentThread.CurrentCulture = English;
            Thread.CurrentThread.CurrentUICulture = English;
        }
    }
}
