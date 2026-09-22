using System.Globalization;
using System.Threading;

namespace PoliSim.UI
{
    /// <summary>
    /// §568 (2026-09-22, the sitting pass's Track 4, drift row D1) — **ONE NUMBER LOCALE PER DESK.**
    ///
    /// <para><b>The finding</b> (Design's whole-game reading, D1): one page read <c>30.1%</c> and the page beside
    /// it read <c>30,1%</c>. Both were right about their own site: money and every figure that goes through
    /// <see cref="UiFormat"/> is formatted in the invariant culture by a ruling that predates this (the seed's
    /// dollars are not this machine's kronor), while a figure interpolated straight into a string - <c>$"{x:F1}%"</c>
    /// - takes the THREAD's culture, which is whatever machine the game is running on. On the machine this project
    /// is built on that is sv-SE, so a comma decimal stood beside a point decimal on almost every screen, and on
    /// another machine the split would fall somewhere else entirely.</para>
    ///
    /// <para><b>What this does, and what it deliberately does not.</b> It installs ONE culture for the whole UI
    /// thread: the machine's own culture, with the INVARIANT number format in place of its own. So every number the
    /// game prints - through <see cref="UiFormat"/>, through an interpolation, through a <c>string.Format</c> that
    /// names no culture - reads with a point and no group separator, and the desk cannot disagree with itself.
    /// <b>Dates keep the machine's culture</b>: the calendar sheet's month name, its abbreviated weekday names and
    /// its first day of the week are read from <see cref="DateTimeFormatInfo.CurrentInfo"/> on purpose (board 1k),
    /// and a Swedish desk that says MÅN TIS ONS is the sheet working as drawn. Fixing the numbers by flipping the
    /// whole thread to invariant would have taken the calendar with it, which is why this replaces the number
    /// format alone.</para>
    ///
    /// <para><b>Installed once, at the game's own start</b> (<c>GameController.Start</c>), and by the film harness
    /// before it draws. It is deliberately NOT a fix at 195 call sites: a number's locale is a property of the
    /// surface, not of the site that happens to print it, and 195 hand-edits would leave the 196th to be found by a
    /// reader again. <see cref="PoliSim.EditorTools"/>'s NumberLocaleCheck holds the statement to one place and the
    /// separator to the point.</para>
    /// </summary>
    public static class UiCulture
    {
        /// <summary>The culture the UI thread runs in once <see cref="Install"/> has run: the machine's, with the invariant number format.</summary>
        public static CultureInfo Numbers { get; private set; }

        /// <summary>
        /// Installs the desk's one number locale on this thread. Safe to call more than once and from either the
        /// game or the harness: the second call sees the first's culture and re-derives from it, so a harness
        /// locale override (<c>-shotlocale=</c>) still decides the DATE names while the numbers stay put.
        /// </summary>
        public static void Install()
        {
            CultureInfo machine = CultureInfo.CurrentCulture;
            var ui = (CultureInfo)machine.Clone();
            ui.NumberFormat = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
            Numbers = ui;
            CultureInfo.DefaultThreadCurrentCulture = ui;
            Thread.CurrentThread.CurrentCulture = ui;
        }
    }
}
