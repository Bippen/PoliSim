using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System;
using PoliSim.Data;
using PoliSim.Elections;
using PoliSim.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace PoliSim.UI
{
    /// <summary>
    /// CANVAS SCREEN 2 (2026-08-12) — the SIGNING ceremony: a full-screen takeover, a document over the scrim wash, no IMGUI
    /// compositing mid-screen. Built on the pilot's recorded patterns: `CanvasChrome` for everything faced and tinted, layout
    /// components rather than fixed rects wherever text can vary.
    ///
    /// **§749 (UI v3.5, the composition's signing plate - "26d's stance fix, one line a party"):** the document is the
    /// composition's plate - flat paper with a hairline edge - in three columns under one head. **The head**: the pen, *Division
    /// No. n*, the chamber and the day, the verdict as an outline stamp, and the bill's own title under them (the composition
    /// carries none; a signing that does not say what is signed is kept as built, and asked). **The division**: the count, for
    /// against against, over one bar of the votes cast with the line that carries it at the bar's middle - THE COUNT DECIDES
    /// (P3-A2), more than half the votes cast, the undecided abstaining and named under it; no fixed seat line, as the Budget's
    /// if-passed panel already says (§734). A budget contest (PS-3f) counts its two proposals the same way, the adopted one
    /// stamped. **The stances**: one line a party, in seat order - its mark, its name, its seats, its vote, its alignment - and the
    /// reason the model gave it on the row's slip (the §432 grouping retires: a line is a party now, so fourteen parties are
    /// fourteen lines). **The estimated impact**: one row an outcome in the Budget panel's own grammar (`EffectArrowsRenderer.V35Icon`
    /// / `V35Figure` / `V35Ink`), the scope on each row's slip. The ornate frame, the state seal's masthead, the institution and
    /// RESOLVED lines, the per-seat map and the arrows plate retire with the old document; the wax seal's beat on SIGN stays
    /// (§1g), landing beside the button.
    ///
    /// <para><b>The desk under the plate cannot show</b> (the composition dims it): the seam suppresses OnGUI while a Canvas
    /// screen is live, and IMGUI draws over every overlay Canvas, so a desk drawn beneath would draw over the plate. The wash
    /// stays the ground, and the plate is centred on it.</para>
    ///
    /// **Patterns this screen keeps (deliberate, not improvised):**
    /// - The canvas brass button: `CanvasChrome.FacedButton` over the delivered per-state strips.
    /// - `ui_scrim_takeover` as the CANVAS-SIDE ground (its second call site): the wash lives under the document, which is what
    ///   lets the IMGUI cover fade away without the wash disappearing.
    /// - The row grammar on Canvas (`CanvasRows`, board 21d): marks, outline stamps, fixed cells, and a slip on hover.
    ///
    /// **Declared deviations:** V-S1, the drop shadow is a single dark plate (no blurred shadow is delivered); V-S3, the
    /// pen-scratch beat is absent (no audio asset).
    /// </summary>
    public partial class SigningScreen
    {
        public GameObject Root { get; private set; }

        /// <summary>True once the seal has landed and settled — the seam watches this to begin CoverOut, the same watch-the-result idiom as the selector's selection.</summary>
        public bool Sealed => _seal != null && _seal.Settled;

        /// <summary>True once §A.13's entrance rows have played out (the document risen and settled,
        /// the controls faded in) — the harness's settle flag composes this in, so a capture never
        /// films the SIGN button mid-fade.</summary>
        public bool EntranceSettled => _entrance == null || _entrance.Settled;

        private SealDrop _seal;
        private DocumentEntrance _entrance;

        // §749: the plate's measures, in canvas units (the scaler's 1920 x 1080 reference - the composition's 1280 px times 1.5).
        private const float PaperWidthFraction = 1080f / 1280f;
        private const float PaperTop = 62f / 720f, PaperBottom = 44f / 720f;
        private const float PadX = 48f, PadY = 33f, Gap = 21f;
        private const float HeadHeight = 60f;
        /// <summary>The real films (real906/real906w, 89i): the plate's title clipped 1.5 px and the path's station statuses 0.9 px at their fixed heights - fixed
        /// headroom on those rows, never a flexible height and never a measured one (a measurement between the layout passes read unsettled widths, real906b/c).</summary>
        private const float TitleHeadroom = 4f;   // [AUTHORED-DRAFT] headroom for the font's line gap at both scales
        private const float StationRowHeadroom = 2f;   // [AUTHORED-DRAFT] headroom for the font's line gap at both scales, per station row
        private const float DivisionWidth = 450f, StancesWidth = 495f, ColumnGap = 45f;
        private const float RowHeight = 42f, MinStanceRow = 33f;
        /// <summary>The stance rows' share of the column at 16:9 - eight parties take their 42 each, a fourteen-party chamber closes them up toward 33.</summary>
        private const float StanceBudget = 550f;
        private const int Caption = 21, Figure = 22, Line = 24;

        private static Color Muted => PoliSimTheme.Hex(0x665E4F);

        /// <summary>§575: what a division's side is CALLED on the sheet - the authority's own abbreviation, falling back to the key for a record built
        /// before the side carried one.</summary>
        private static string Shown(DivisionSide side) => string.IsNullOrEmpty(side.ShortName) ? side.Abbrev : side.ShortName;

        /// <summary>Null when there is no record to sign — the caller drops the ceremony and the resolution stays silent (degradation costs the ceremony, never correctness).</summary>
        public static SigningScreen Build(Country country, DivisionRecord record, Action onSign) => Build(country, record, onSign, null, null, null);

        /// <summary>
        /// SP-7b (Elias's ruling I14, ask 2 (2a)): THE PLATE'S TWO ANSWERS - a statute the Sejm passed, presented to the player-president and held on his docket:
        /// the plate as the ceremony draws it, its sign row carrying <paramref name="answerLine"/> (on his desk, the day it is signed unanswered) and its
        /// answers - LATER (<paramref name="onLater"/>: the plate closes, the statute stays held and is answered on the Docket page) and VETO
        /// (<paramref name="onVeto"/>; null where the act takes none - the budget act, a constitutional amendment) on the paper face, SIGN on the brass. The seal
        /// drops on a signature; a veto or LATER settles the same beat with no seal shown (<see cref="Veto"/>).
        /// </summary>
        public static SigningScreen BuildAnswer(Country country, DivisionRecord record, string answerLine, Action onSign, Action onVeto, Action onLater) => Build(country, record, onSign, onVeto, onLater, answerLine ?? string.Empty);

        /// <summary>PS-6 US-35 (board 30b): the answer plate for a bill on the US player-president's desk - the same plate, with the bill's path, the ten days
        /// (<paramref name="desk"/>'s presentment and law days) and each verb's consequence.</summary>
        public static SigningScreen BuildAnswer(Country country, DivisionRecord record, string answerLine, Action onSign, Action onVeto, Action onLater, DeskContext desk) => Build(country, record, onSign, onVeto, onLater, answerLine ?? string.Empty, desk);

        private static SigningScreen Build(Country country, DivisionRecord record, Action onSign, Action onVeto, Action onLater, string answerLine, DeskContext desk = null)
        {
            if (country == null || record == null)
            {
                Debug.LogWarning("CANVAS: signing record missing - the ceremony is dropped, the resolution stays silent.");
                return null;
            }
            Texture2D scrimTexture = IconLibrary.GetChrome("ui_scrim_takeover");

            Canvas canvas = CanvasChrome.EnsureHost();
            var screen = new SigningScreen();

            var root = new GameObject("SigningScreen");
            screen.Root = root;
            root.transform.SetParent(canvas.transform, false);
            Stretch(root.AddComponent<RectTransform>());

            // The wash: canvas-side scrim ground, stretched whole, untinted (real-colour, §3.0a).
            var wash = new GameObject("Wash");
            wash.transform.SetParent(root.transform, false);
            Stretch(wash.AddComponent<RectTransform>());
            if (scrimTexture != null)
            {
                RawImage washImage = wash.AddComponent<RawImage>();
                washImage.texture = scrimTexture;
                washImage.raycastTarget = true; // swallow clicks outside the document
            }
            else
            {
                Image washFill = wash.AddComponent<Image>();
                washFill.color = new Color(0f, 0f, 0f, 0.75f);
            }

            Vector2 paperMin = new Vector2((1f - PaperWidthFraction) * 0.5f, PaperBottom), paperMax = new Vector2(1f - (1f - PaperWidthFraction) * 0.5f, 1f - PaperTop);

            // V-S1: one dark plate as the shadow.
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(root.transform, false);
            var shadowRect = shadow.AddComponent<RectTransform>();
            shadowRect.anchorMin = paperMin;
            shadowRect.anchorMax = paperMax;
            shadowRect.sizeDelta = new Vector2(6f, 6f);
            shadowRect.anchoredPosition = new Vector2(0f, -10f);
            Image shadowImage = shadow.AddComponent<Image>();
            shadowImage.color = new Color(0f, 0f, 0f, 0.4f);
            shadowImage.raycastTarget = false;

            // The plate: flat paper, a hairline edge.
            var document = new GameObject("Document");
            document.transform.SetParent(root.transform, false);
            var docRect = document.AddComponent<RectTransform>();
            docRect.anchorMin = paperMin;
            docRect.anchorMax = paperMax;
            docRect.sizeDelta = Vector2.zero;
            Image paper = document.AddComponent<Image>();
            paper.color = PoliSimTheme.Hex(0xF2EADB);
            CanvasRows.Edges(document.transform, PoliSimTheme.Hex(0x8A7A5C), 1.5f);

            var content = new GameObject("Content");
            content.transform.SetParent(document.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(PadX, PadY);
            contentRect.offsetMax = new Vector2(-PadX, -PadY);
            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = Gap;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            BuildHead(content.transform, country, record, root.transform);
            BuildUsPath(content.transform, country, record, desk, root.transform);   // PS-6 US-35 (board 30b): a Congress bill's path, the ten days on the desk, the pocket veto
            BuildColumns(content.transform, country, record, root.transform, desk);

            // The sign row: the button at the right, the seal's landing beside it.
            var signRow = new GameObject("SignRow");
            signRow.transform.SetParent(content.transform, false);
            signRow.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 60f);
            LayoutElement signHeight = signRow.AddComponent<LayoutElement>();
            signHeight.minHeight = signHeight.preferredHeight = 60f;
            HorizontalLayoutGroup rowLayout = signRow.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = TextAnchor.MiddleRight;
            rowLayout.spacing = 30f;
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = false;
            rowLayout.childForceExpandWidth = false;   // the defaults spread the landing and the button across the row (film749a: the seal dropped mid-plate)
            rowLayout.childForceExpandHeight = false;

            // ⚠ PLAYTEST FIX (2026-08-18): the seal used to drop, and the button read "SIGN", for
            // EVERY division regardless of record.Passed - a false player-facing claim, not a
            // cosmetic slip (a rejected bill was never enacted; there is nothing to sign). The
            // landing zone's 104x104 slot stays reserved either way, so the button sits in the
            // identical position for both verdicts; only what fills it, and what the button says,
            // now depends on the record.
            if (answerLine != null)
            {
                // SP-7b: the answer's line at the row's left - on the President's desk, and the day it is signed unanswered
                Text onDesk = CanvasChrome.MakeText(signRow.transform, "OnYourDesk", answerLine, PoliSimTheme.Document, Caption, Muted, TextAnchor.MiddleLeft);
                onDesk.horizontalOverflow = HorizontalWrapMode.Wrap;
                ((RectTransform)onDesk.transform).sizeDelta = new Vector2(AnswerLineWidth, 60f);
            }
            var landing = new GameObject("SealLanding");
            landing.transform.SetParent(signRow.transform, false);
            landing.AddComponent<RectTransform>().sizeDelta = new Vector2(104f, 104f);

            GameObject sealBeat;
            Texture2D sealTexture = record.Passed ? IconLibrary.GetChrome("ui_seal_official") : null;
            if (sealTexture != null)
            {
                // The wax seal is real-colour: as-authored, locked white.
                Image sealImage = CanvasChrome.AsAuthoredImage(landing.transform, "Seal",
                    CanvasChrome.Whole(sealTexture, "ui_seal_official#whole"));
                RectTransform sealRect = sealImage.rectTransform;
                sealRect.anchorMin = sealRect.anchorMax = new Vector2(0.5f, 0.5f);
                sealRect.sizeDelta = new Vector2(104f, 104f);
                sealImage.preserveAspect = true;
                sealBeat = sealImage.gameObject;
            }
            else
            {
                // No enactment, no seal to drop. SealDrop only ever animates its own RectTransform
                // (see its own class below) - it needs no Image - so an empty timer object carries
                // the identical settle beat with nothing visible to show, and Sign()/Sealed keep
                // driving the seam exactly as they do for a passed division.
                var timer = new GameObject("SealTimer");
                timer.transform.SetParent(landing.transform, false);
                RectTransform timerRect = timer.AddComponent<RectTransform>();
                timerRect.anchorMin = timerRect.anchorMax = new Vector2(0.5f, 0.5f);
                timerRect.sizeDelta = new Vector2(104f, 104f);
                sealBeat = timer;
            }

            screen._seal = sealBeat.AddComponent<SealDrop>();
            sealBeat.SetActive(false);

            CanvasGroup controls = answerLine == null ? BuildSignButton(signRow.transform, onSign, record.Passed, UsBillPath.Applies(country, record) && UsBillPath.OnVeto(record)) : BuildAnswerButtons(signRow.transform, onSign, onVeto, onLater);   // SP-7b: the plate's answers

            // §A.13's two rows that had no implementation (re-derived against the seam 2026-08-28,
            // omnibus roadmap item 4): row 4, the document rises 24px and settles −0.6° → 0° over
            // 240–500ms, ease-out cubic; row 6, the controls fade in LAST (700ms+). The seal thunk
            // (row 5) is §1g's own beat below, at its own 1.3 → 1.0 / 140ms - a declared deviation
            // from the envelope's 1.15 / 120ms, kept because §1g is the ceremony's own spec. Rows
            // 1–3 are the IMGUI seam's (GameController's takeover: lock, cover, hold-and-swap).
            // P3 close (2026-09-03): the layout is resolved NOW, not two frames on - the canvas guard photographed its rects unlaid (89d).
            LayoutRebuilder.ForceRebuildLayoutImmediate(document.GetComponent<RectTransform>());
            // PF-13 (2026-09-22, §578): TWICE, and the second pass is the fix. uGUI's `Text.preferredHeight` is computed at the rect's CURRENT width, so on the
            // first pass a wrapped text reports the height it would need at whatever width it had before the horizontal pass ran; the second rebuild
            // recomputes every preferred height against the widths the first pass settled, which is what the vertical pass then gives them. ⚠ A
            // ContentSizeFitter would NOT do here: the groups already drive these rects (childControlHeight), and two things driving one rect fight.
            LayoutRebuilder.ForceRebuildLayoutImmediate(document.GetComponent<RectTransform>());
            screen._entrance = document.AddComponent<DocumentEntrance>();
            screen._entrance.Controls = controls;

            // S-20: the capture-identity token, so a film of this board proves it is this board.
            PoliSim.Testing.CaptureIdentity.CanvasSurface = "signing";
            return screen;
        }

        /// <summary>Starts the seal beat (§1g: drop 1.3 → 1.0 over 140ms with a 6px settle). The seam watches <see cref="Sealed"/>.</summary>
        public void Sign()
        {
            if (_seal != null && !_seal.gameObject.activeSelf)
            {
                _seal.gameObject.SetActive(true);
            }
        }

        /// <summary>SP-7b: the beat with no seal shown - a veto (a statute returned is not sealed) or LATER (nothing answered); the seam watches <see cref="Sealed"/> as for SIGN.</summary>
        public void Veto()
        {
            if (_seal != null && !_seal.gameObject.activeSelf)
            {
                Image sealImage = _seal.GetComponent<Image>();
                if (sealImage != null) { sealImage.enabled = false; }
                _seal.gameObject.SetActive(true);
            }
        }

        public void SetVisible(bool visible)
        {
            if (Root != null) { Root.SetActive(visible); }
        }

        public void Destroy()
        {
            if (Root != null)
            {
                UnityEngine.Object.Destroy(Root);
                Root = null;
            }
        }

        /// <summary>§749: the head - the pen, the division's number, the chamber and the day, the verdict stamped; the bill's title under it, and the rule.</summary>
        private static void BuildHead(Transform parent, Country country, DivisionRecord record, Transform overlay)
        {
            Transform head = CanvasRows.HRow(parent, "Head", HeadHeight, 27f);
            Texture2D pen = IconLibrary.V35("pen");
            if (pen != null)
            {
                Image penImage = CanvasChrome.TintedImage(head, "Pen", CanvasChrome.Whole(pen, "icon_v35_pen#whole"), PoliSimTheme.Hex(0x8A6B21));
                penImage.preserveAspect = true;
                Fixed(penImage.gameObject, 45f, 45f);
            }
            // PS-6 US-18: a bill through both chambers carries both divisions on one plate - the first chamber's (record.OtherChamber) and the second's
            DivisionRecord first = record.OtherChamber;
            bool congress = InCongress(country, record);
            string numbers = first != null ? "Divisions No. " + first.Number.ToString(CultureInfo.InvariantCulture) + " and " + record.Number.ToString(CultureInfo.InvariantCulture)
                : "Division No. " + record.Number.ToString(CultureInfo.InvariantCulture);
            Text number = Word(head, numbers, PoliSimTheme.Body, 42, PoliSimTheme.TextPrimary);
            string day = record.Date.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
            string chamber = first != null ? Congress.ChamberName(country.Id, first.Chamber) + " and " + Congress.ChamberName(country.Id, record.Chamber)
                : congress ? Congress.ChamberName(country.Id, record.Chamber) : StartBrief.ChamberOf(country.Id);
            Word(head, chamber + " · " + day, PoliSimTheme.Body, 26, Muted);
            CanvasRows.Spacer(head);

            DivisionContest contest = record.Contest;
            string stamp = contest != null ? (contest.AlternativeAdopted ? "ALTERNATIVE ADOPTED" : "FRAMES ADOPTED")
                : record.Required > 0 && !record.Cloture ? (record.Passed ? "VETO OVERRIDDEN" : "VETO STANDS")   // §761: the Sejm's vote on the President's veto
                : record.Passed ? "CARRIED" : congress ? "LOST IN THE " + Congress.ChamberName(country.Id, record.Chamber).ToUpperInvariant() : "LOST";   // PS-6 US-18: the chamber that stopped it
            CanvasRows.Stamp(head, stamp, Figure, record.Passed ? PoliSimTheme.Good : PoliSimTheme.Bad);

            string axis = record.Axis == (int)BillAxis.Trade ? "THE OPENNESS AXIS" : "THE FISCAL AXIS";
            CanvasRows.Slip(number.gameObject, overlay, first != null ? numbers.ToUpperInvariant() : "DIVISION No. " + record.Number.ToString(CultureInfo.InvariantCulture), new[]
            {
                day.ToUpperInvariant() + " · " + stamp,
                "ALIGNMENT " + Signed(record.Alignment) + " ON " + axis + " - THE SEAT-WEIGHTED LEAN; " + (congress ? "IT DECIDES NOTHING HERE - EACH CHAMBER'S COUNT DOES"
                    : record.Required > 0 ? "IT DECIDES NOTHING HERE - THE 3/5 LINE DOES" : "IT BREAKS A TIE IN THE COUNT AND NOTHING ELSE"),   // §761
            });

            // The bill's own title: not in the composition, kept so the plate says what is signed (asked).
            Text title = CanvasChrome.MakeTextRealWeight(parent, "Title", EnumNames.ShownTitle(record.Title), PoliSimTheme.Body, 26, PoliSimTheme.TextPrimary, TextAnchor.MiddleLeft);
            title.horizontalOverflow = HorizontalWrapMode.Wrap;
            title.gameObject.AddComponent<LayoutElement>().minHeight = 39f + TitleHeadroom;   // the minimum only: a wrapped title keeps its own preferred height

            Rule(parent, "HeadRule", 1.5f, PoliSimTheme.Hex(0x2B2620));
        }

        /// <summary>§749: the three columns - the division, the stances, the estimated impact.</summary>
        private static void BuildColumns(Transform parent, Country country, DivisionRecord record, Transform overlay, DeskContext desk = null)
        {
            var grid = new GameObject("Columns");
            grid.transform.SetParent(parent, false);
            grid.AddComponent<RectTransform>();
            LayoutElement gridSize = grid.AddComponent<LayoutElement>();
            gridSize.minHeight = 300f;
            gridSize.flexibleHeight = 1f;
            HorizontalLayoutGroup columns = grid.AddComponent<HorizontalLayoutGroup>();
            columns.spacing = ColumnGap;
            columns.childAlignment = TextAnchor.UpperLeft;
            columns.childControlWidth = true;
            columns.childControlHeight = true;
            columns.childForceExpandWidth = false;
            columns.childForceExpandHeight = true;

            BuildDivision(Column(grid.transform, "Division", DivisionWidth, 0f), country, record, overlay);
            BuildStances(Column(grid.transform, "Stances", StancesWidth, 0f), country, record, overlay);
            Transform impact = Column(grid.transform, "Impact", 0f, 1f);
            if (desk != null && UsBillPath.Applies(country, record)) { BuildUsAnswers(impact, country, record, desk, overlay); }   // PS-6 US-35 (board 30b): your answer, each verb's consequence
            BuildImpact(impact, record, overlay);
        }

        /// <summary>
        /// §749: THE DIVISION - the count over one bar of the votes cast, the line that carries it at the bar's middle. P3-A2: THE COUNT DECIDES, seats for
        /// against seats against, the undecided abstaining, the alignment breaking a tie - so the line is more than half the votes CAST, not a fixed
        /// share of the chamber (with nobody undecided the two agree: 175 of Sweden's 349). A budget contest (PS-3f) is the same count between its two
        /// proposals, a tie keeping the government's frames.
        /// </summary>
        private static void BuildDivision(Transform column, Country country, DivisionRecord record, Transform overlay)
        {
            // PS-6 US-18: a bill through both chambers - each chamber's division in the order it voted (the first chamber's carried on the second's record);
            // a US division the first chamber stopped is drawn by the same block, its chamber's rule named
            if (record.Contest == null && InCongress(country, record))
            {
                ColumnHead(column, record.OtherChamber != null ? "The divisions" : "The division");
                if (record.OtherChamber != null) { ChamberDivision(column, country, record.OtherChamber, overlay); }
                ChamberDivision(column, country, record, overlay);
                // PS-6 US-21: a veto its first chamber sustained is put to that chamber alone - the other never votes on it
                if (OnVeto(record) && record.OtherChamber == null && !record.Passed)
                {
                    int other = record.Chamber == Congress.Senate ? Congress.House : Congress.Senate;
                    Note(column, "The " + Congress.ChamberName(country.Id, other) + " does not vote - the veto stands");
                }
                return;
            }
            ColumnHead(column, "The division");
            if (record.Contest != null)
            {
                DivisionContest c = record.Contest;
                Transform first = ProposalRow(column, c.VotesFor, ProposalName(c.ProposalFor), !c.AlternativeAdopted);
                ProposalRow(column, c.VotesAgainst, ProposalName(c.ProposalAgainst), c.AlternativeAdopted);
                int cast = c.VotesFor + c.VotesAgainst;
                CountBar(column, c.VotesFor, c.VotesAgainst, c.AlternativeAdopted ? PoliSimTheme.Bad : PoliSimTheme.Good, c.AlternativeAdopted ? PoliSimTheme.Good : PoliSimTheme.Bad, cast / 2 + 1);
                if (c.Abstentions > 0) { Note(column, c.Abstentions.ToString(CultureInfo.InvariantCulture) + " abstaining"); }
                CanvasRows.Slip(first.gameObject, overlay, "THE DIVISION · TWO PROPOSALS", new[]
                {
                    c.ProposalFor + " " + c.VotesFor.ToString(CultureInfo.InvariantCulture) + " · " + c.ProposalAgainst + " " + c.VotesAgainst.ToString(CultureInfo.InvariantCulture) + " · ABSTAINING " + c.Abstentions.ToString(CultureInfo.InvariantCulture),
                    "THE PROPOSAL WITH MORE VOTES IS ADOPTED - " + (cast / 2 + 1).ToString(CultureInfo.InvariantCulture) + " OF THE " + cast.ToString(CultureInfo.InvariantCulture) + " CAST; A TIE KEEPS THE GOVERNMENT'S FRAMES",
                });
                return;
            }
            if (record.Sides.Count == 0)
            {
                Note(column, "No sides recorded - this division predates the count");
                return;
            }

            int forSeats = 0, undecided = 0, against = 0;
            foreach (DivisionSide side in record.Sides)
            {
                if (side.Side > 0) { forSeats += side.Seats; } else if (side.Side < 0) { against += side.Seats; } else { undecided += side.Seats; }
            }
            int votesCast = forSeats + against;
            Transform count = CanvasRows.HRow(column, "Count", 66f, 15f);
            count.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;   // every word the row's height, set on its foot: the figures and the words share a line
            count.GetComponent<LayoutElement>().flexibleHeight = 0f;   // ⚠ a group that force-expands reports itself flexible, and the column then hands this row all its spare height (film749a: the count sank to the column's foot)
            Word(count, forSeats.ToString(CultureInfo.InvariantCulture), PoliSimTheme.Document, 45, PoliSimTheme.Good, TextAnchor.LowerLeft);
            Word(count, "for", PoliSimTheme.Body, Line, PoliSimTheme.TextPrimary, TextAnchor.LowerLeft);
            Fixed(Blank(count), 9f, 1f);
            Word(count, against.ToString(CultureInfo.InvariantCulture), PoliSimTheme.Document, 45, PoliSimTheme.Bad, TextAnchor.LowerLeft);
            Word(count, "against", PoliSimTheme.Body, Line, PoliSimTheme.TextPrimary, TextAnchor.LowerLeft);
            // §761: a QUALIFIED vote (the Sejm's override of the President's veto) - its line the votes it needed (3/5 of those voting), its base everyone
            // voting, the abstaining among them; the bar runs over all of them and the line sits at its own share
            if (record.Required > 0)
            {
                int voting = forSeats + against + undecided;
                CountBar(column, forSeats, against + undecided, PoliSimTheme.Good, PoliSimTheme.Bad, record.Required, voting > 0 ? Mathf.Clamp01((float)record.Required / voting) : 0.5f);
                if (undecided > 0) { Note(column, undecided.ToString(CultureInfo.InvariantCulture) + " abstaining - counted among those voting"); }
                CanvasRows.Slip(count.gameObject, overlay, "THE VOTE ON THE VETO", new[]
                {
                    "FOR " + forSeats.ToString(CultureInfo.InvariantCulture) + " · AGAINST " + against.ToString(CultureInfo.InvariantCulture) + " · ABSTAINING " + undecided.ToString(CultureInfo.InvariantCulture),
                    "THE SEJM OVERRIDES THE PRESIDENT'S VETO WITH 3/5 OF THOSE VOTING, ABSTENTIONS IN THE BASE - CONSTITUTION ART. 122 UST. 5",
                    "THE LINE: " + record.Required.ToString(CultureInfo.InvariantCulture) + ", 3/5 OF THE " + voting.ToString(CultureInfo.InvariantCulture) + " VOTING",
                });
                return;
            }
            CountBar(column, forSeats, against, PoliSimTheme.Good, PoliSimTheme.Bad, votesCast / 2 + 1);
            if (undecided > 0) { Note(column, undecided.ToString(CultureInfo.InvariantCulture) + " undecided - they abstain"); }
            CanvasRows.Slip(count.gameObject, overlay, "THE DIVISION", new[]
            {
                "FOR " + forSeats.ToString(CultureInfo.InvariantCulture) + " · AGAINST " + against.ToString(CultureInfo.InvariantCulture) + " · UNDECIDED " + undecided.ToString(CultureInfo.InvariantCulture),
                "THE COUNT DECIDES - FOR AGAINST AGAINST, THE UNDECIDED ABSTAINING; NO FIXED SEAT LINE",
                "THE LINE: " + (votesCast / 2 + 1).ToString(CultureInfo.InvariantCulture) + ", MORE THAN HALF OF THE " + votesCast.ToString(CultureInfo.InvariantCulture) + " VOTES CAST",
            });
        }

        /// <summary>PS-6 US-18: whether a division is Congress's - one of a bill's two chambers' (the second's carrying the first's, or the Senate's alone), or
        /// any division of a country whose bills go through both chambers (`Congress.Applies`), where even the House's count is "a majority of those voting, a
        /// tie failing" and no alignment breaks a tie.</summary>
        private static bool InCongress(Country country, DivisionRecord record) =>
            record.OtherChamber != null || record.Chamber == Congress.Senate || record.Cloture || Congress.Applies(country);

        /// <summary>PS-6 US-21: a Congress division that is a chamber's vote on the president's veto - its line the two thirds (`DivisionRecord.Required`), never
        /// a cloture line.</summary>
        private static bool OnVeto(DivisionRecord record) => record.Required > 0 && !record.Cloture;

        /// <summary>
        /// PS-6 US-18: ONE CHAMBER'S DIVISION ON A CONGRESS PLATE - the chamber and its verdict as a caption, the count, the bar and its line, and what
        /// abstained. The House counts a majority of those voting (the line more than half the votes cast, a tie failing); the Senate at cloture counts its
        /// yeas against three fifths of the senators sworn - the bar runs over every senator sworn, an abstaining one no yea, the line at its own share
        /// (`DivisionRecord.Required` of `DivisionRecord.Sworn`); by reconciliation a majority, the Vice President deciding an equal division (his vote named).
        /// </summary>
        private static void ChamberDivision(Transform column, Country country, DivisionRecord record, Transform overlay)
        {
            string name = Congress.ChamberName(country.Id, record.Chamber);
            bool onVeto = OnVeto(record);   // PS-6 US-21: the chamber's vote on the president's veto
            string procedure = onVeto ? " · on the veto" : record.Chamber != Congress.Senate ? string.Empty : record.Cloture ? " · cloture" : " · reconciliation";
            Note(column, name + procedure + " · " + (onVeto ? (record.Passed ? "overridden" : "sustained") : record.Passed ? "passed" : "rejected"));
            int forSeats = 0, undecided = 0, against = 0;
            foreach (DivisionSide side in record.Sides)
            {
                if (side.Side > 0) { forSeats += side.Seats; } else if (side.Side < 0) { against += side.Seats; } else { undecided += side.Seats; }
            }
            // PS-6 US-21: the vote on the veto of a bill that passed unasked records no sides - every member of its base is for the override (`Congress`'s one
            // override rule), the base its record's (`DivisionRecord.Sworn`)
            if (onVeto && record.Sides.Count == 0) { forSeats = record.Sworn; }
            else if (record.Sides.Count == 0)
            {
                Note(column, "Uncontested - nothing asked of it");
                return;
            }
            Transform count = CanvasRows.HRow(column, "Count", 66f, 15f);
            count.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            count.GetComponent<LayoutElement>().flexibleHeight = 0f;
            Word(count, forSeats.ToString(CultureInfo.InvariantCulture), PoliSimTheme.Document, 45, PoliSimTheme.Good, TextAnchor.LowerLeft);
            Word(count, "for", PoliSimTheme.Body, Line, PoliSimTheme.TextPrimary, TextAnchor.LowerLeft);
            Fixed(Blank(count), 9f, 1f);
            Word(count, against.ToString(CultureInfo.InvariantCulture), PoliSimTheme.Document, 45, PoliSimTheme.Bad, TextAnchor.LowerLeft);
            Word(count, "against", PoliSimTheme.Body, Line, PoliSimTheme.TextPrimary, TextAnchor.LowerLeft);
            string I(int n) => n.ToString(CultureInfo.InvariantCulture);
            int crossed = 0;
            foreach (DivisionSide side in record.Sides) { if (side.Defectors) { crossed += side.Seats; } }
            if (onVeto)
            {
                // PS-6 US-21: two thirds of those voting, the base every member of the division - the undecided counted against - and the line at its own share
                int members = Mathf.Max(record.Sworn, forSeats + against + undecided);
                CountBar(column, forSeats, members - forSeats, PoliSimTheme.Good, PoliSimTheme.Bad, record.Required, members > 0 ? Mathf.Clamp01((float)record.Required / members) : 0.667f);
                Note(column, "Two thirds of the " + I(members) + " voting: " + I(record.Required));
                if (undecided > 0) { Note(column, I(undecided) + " undecided - counted against"); }
                CanvasRows.Slip(count.gameObject, overlay, "THE " + name.ToUpperInvariant() + " · THE VOTE ON THE VETO", new[]
                {
                    "FOR " + I(forSeats) + " · AGAINST " + I(against) + " · UNDECIDED " + I(undecided) + (crossed > 0 ? " · " + I(crossed) + " DEFECTORS AMONG THEM" : string.Empty),
                    "A VETO IS OVERRIDDEN BY TWO THIRDS OF THOSE VOTING IN EACH CHAMBER, A QUORUM PRESENT - THE CHAMBER THE BILL BEGAN IN FIRST, ON THE SIDES THAT VOTED THE BILL",
                    "THE LINE: " + I(record.Required) + " OF THE " + I(members) + " - " + (record.Passed ? "OVERRIDDEN" : "THE VETO STANDS"),
                    "THE POCKET VETO IS NOT MODELLED · A BILL SENT TO YOU AS PRESIDENT IS LAW IF UNANSWERED FOR TEN DAYS, SUNDAYS EXCEPTED",
                });
                return;
            }
            if (crossed > 0) { Note(column, I(crossed) + " drawn defectors decided it"); }   // PS-6 US-21: crossers enter a division only where they change its verdict
            if (record.Cloture)
            {
                int sworn = Mathf.Max(record.Sworn, forSeats + against + undecided);
                CountBar(column, forSeats, sworn - forSeats, PoliSimTheme.Good, PoliSimTheme.Bad, record.Required, sworn > 0 ? Mathf.Clamp01((float)record.Required / sworn) : 0.6f);
                Note(column, "Three fifths of the " + I(record.Sworn) + " sworn: " + I(record.Required));
                if (undecided > 0) { Note(column, I(undecided) + " undecided - no yea"); }
                CanvasRows.Slip(count.gameObject, overlay, "THE " + name.ToUpperInvariant() + " · CLOTURE", new[]
                {
                    "FOR " + I(forSeats) + " · AGAINST " + I(against) + " · UNDECIDED " + I(undecided),
                    "CLOTURE NEEDS THREE FIFTHS OF THE SENATORS DULY CHOSEN AND SWORN - STANDING RULE XXII; PASSAGE THEN FOLLOWS BY A MAJORITY",
                    "THE LINE: " + I(record.Required) + " OF THE " + I(record.Sworn) + " SWORN ON THE DAY, NEVER A FIXED SIXTY - " + (forSeats >= record.Required ? "CLOTURE INVOKED" : "CLOTURE NOT INVOKED"),
                });
                return;
            }
            int cast = forSeats + against;
            bool vicePresident = !string.IsNullOrEmpty(record.VicePresidentParty);   // an equal division his vote decided: the line is the tie itself, not past it
            int line = vicePresident ? cast / 2 : cast / 2 + 1;
            CountBar(column, forSeats, against, PoliSimTheme.Good, PoliSimTheme.Bad, line);
            if (undecided > 0) { Note(column, I(undecided) + " undecided - they abstain"); }
            if (!string.IsNullOrEmpty(record.VicePresidentParty))
            {
                string party = PartySystems.ShortName(country.Id, record.VicePresidentParty);
                Note(column, "Tied · the Vice President (" + party + ") " + (record.VicePresidentVote > 0 ? "votes for" : record.VicePresidentVote < 0 ? "votes against" : "casts no vote"));
            }
            CanvasRows.Slip(count.gameObject, overlay, "THE " + name.ToUpperInvariant() + (record.Chamber == Congress.Senate ? " · RECONCILIATION" : string.Empty), new[]
            {
                "FOR " + I(forSeats) + " · AGAINST " + I(against) + " · UNDECIDED " + I(undecided),
                record.Chamber == Congress.Senate
                    ? "A RECONCILIATION BILL NEEDS NO CLOTURE - ITS DEBATE IS LIMITED TO TWENTY HOURS (2 U.S.C. 641(e)); A MAJORITY OF THOSE VOTING CARRIES IT, THE VICE PRESIDENT DECIDING AN EQUAL DIVISION; NO YEARLY LIMIT ON RECONCILIATION IS MODELLED"
                    : "A MAJORITY OF THOSE VOTING CARRIES IT, THE UNDECIDED ABSTAINING; A TIE FAILS",
                vicePresident
                    ? "THE LINE: " + I(line) + ", HALF OF THE " + I(cast) + " VOTES CAST - EQUALLY DIVIDED, THE VICE PRESIDENT DECIDES IT"
                    : "THE LINE: " + I(line) + ", MORE THAN HALF OF THE " + I(cast) + " VOTES CAST",
            });
        }

        /// <summary>One proposal of a budget contest: its votes, its name, ADOPTED stamped on the one the chamber took.</summary>
        private static Transform ProposalRow(Transform column, int votes, string name, bool adopted)
        {
            Transform row = CanvasRows.HRow(column, "Proposal", 54f, 15f);
            Word(row, votes.ToString(CultureInfo.InvariantCulture), PoliSimTheme.Document, 36, adopted ? PoliSimTheme.Good : PoliSimTheme.TextSecondary);
            Word(row, name, PoliSimTheme.Body, Caption, PoliSimTheme.TextPrimary);
            if (adopted) { CanvasRows.Stamp(row, "ADOPTED", Caption, PoliSimTheme.Good); }
            return row;
        }

        /// <summary>A proposal as the plate reads it: the record's upper-case name in sentence case, the tabling party by its shown name.</summary>
        private static string ProposalName(string recorded)
        {
            if (string.IsNullOrEmpty(recorded)) { return string.Empty; }
            if (recorded == "THE GOVERNMENT'S FRAMES") { return "The government's frames"; }
            const string alternative = "'S ALTERNATIVE";
            return recorded.EndsWith(alternative, StringComparison.Ordinal) ? UiFormat.Possessive(recorded.Substring(0, recorded.Length - alternative.Length), false, true) + " alternative" : recorded;
        }

        /// <summary>The bar of the votes cast - the left side's share, the right's - with the line that carries it at the middle and its figure under it.</summary>
        private static void CountBar(Transform column, int left, int right, Color leftInk, Color rightInk, int carries, float at = 0.5f)
        {
            var block = new GameObject("CountBar");
            block.transform.SetParent(column, false);
            block.AddComponent<RectTransform>();
            LayoutElement size = block.AddComponent<LayoutElement>();
            size.minHeight = size.preferredHeight = 84f;

            var bar = new GameObject("Bar");
            bar.transform.SetParent(block.transform, false);
            var barRect = bar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 1f);
            barRect.anchorMax = new Vector2(1f, 1f);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.sizeDelta = new Vector2(0f, 30f);
            barRect.anchoredPosition = new Vector2(0f, -12f);
            Image track = bar.AddComponent<Image>();
            track.color = PoliSimTheme.BarTrack;
            track.raycastTarget = false;
            int cast = left + right;
            if (cast <= 0) { return; }
            float share = (float)left / cast;
            Segment(bar.transform, "Left", 0f, share, leftInk);
            Segment(bar.transform, "Right", share, 1f, rightInk);

            var tick = new GameObject("Line");
            tick.transform.SetParent(block.transform, false);
            var tickRect = tick.AddComponent<RectTransform>();
            tickRect.anchorMin = tickRect.anchorMax = new Vector2(at, 1f);   // §761: a qualified vote's line sits at its own share of the bar (3/5), the count's at the middle
            tickRect.pivot = new Vector2(0.5f, 1f);
            tickRect.sizeDelta = new Vector2(4.5f, 48f);
            tickRect.anchoredPosition = new Vector2(0f, -3f);
            Image tickImage = tick.AddComponent<Image>();
            tickImage.color = PoliSimTheme.Hex(0x2B2620);
            tickImage.raycastTarget = false;

            Text label = CanvasChrome.MakeTextRealWeight(block.transform, "LineFigure", carries.ToString(CultureInfo.InvariantCulture), PoliSimTheme.Document, Caption, PoliSimTheme.TextPrimary, TextAnchor.UpperCenter);
            var labelRect = (RectTransform)label.transform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(at, 1f);
            labelRect.pivot = new Vector2(0.5f, 1f);
            labelRect.sizeDelta = new Vector2(150f, 30f);
            labelRect.anchoredPosition = new Vector2(0f, -54f);
        }

        private static void Segment(Transform bar, string name, float from, float to, Color ink)
        {
            var go = new GameObject(name);
            go.transform.SetParent(bar, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(from, 0f);
            r.anchorMax = new Vector2(to, 1f);
            r.offsetMin = r.offsetMax = Vector2.zero;
            Image image = go.AddComponent<Image>();
            image.color = ink;
            image.raycastTarget = false;
        }

        /// <summary>
        /// §749 (26d's stance fix): THE STANCES, ONE LINE A PARTY, in seat order - the mark, the name, the seats, the vote, the alignment; the reason the
        /// model gave it is the row's slip. The lines close up from 42 toward 33 units when a chamber has more parties than eight lines' room.
        /// </summary>
        private static void BuildStances(Transform column, Country country, DivisionRecord record, Transform overlay)
        {
            string axis = record.Axis == (int)BillAxis.Trade ? "openness axis" : "fiscal axis";
            ColumnHead(column, "Stances · " + axis);
            if (record.OtherChamber != null)
            {
                StancesInBothChambers(column, country, record.OtherChamber, record, overlay);
                return;
            }
            if (record.Sides.Count == 0)
            {
                Note(column, InCongress(country, record) ? "Uncontested - no stance asked" : "No sides recorded - this division predates the stances");
                return;
            }
            var names = new Dictionary<string, string>();
            foreach (PoliticalParty party in PartySystems.For(country.Id)) { names[party.Abbrev] = party.Name; }
            List<DivisionSide> ordered = record.Sides.OrderByDescending(s => s.Seats).ToList();   // stable: the record's order breaks a tie
            float rowHeight = Mathf.Clamp(StanceBudget / ordered.Count, MinStanceRow, RowHeight);
            DivisionContest contest = record.Contest;
            foreach (DivisionSide side in ordered)
            {
                string vote = contest != null
                    ? (side.Side > 0 ? "Frames" : side.Side < 0 ? "Alternative" : "Abstains")   // PS-3f (§633): a contest's side names the proposal - in full on the slip
                    : side.Defectors ? "Defected"   // PS-6 US-21: a party's drawn defectors, on the other side of its stance
                    : (side.Side > 0 ? "For" : side.Side < 0 ? "Against" : record.Required > 0 && !record.Cloture && !InCongress(country, record) ? "Abstains" : "Undecided");   // §761: on the vote on a veto the undecided abstain, counted among those voting (PS-6 US-18: not at cloture; PS-6 US-21: on a US veto the undecided count against)
                Color ink = contest != null ? (side.Side == 0 ? Muted : ((side.Side < 0) == contest.AlternativeAdopted ? PoliSimTheme.Good : PoliSimTheme.TextPrimary))
                    : side.Side > 0 ? PoliSimTheme.Good : side.Side < 0 ? PoliSimTheme.Bad : Muted;
                bool modelled = !string.IsNullOrEmpty(side.Reason);   // a record before the stance model carries no alignment worth printing

                Transform row = CanvasRows.HRow(column, "Stance " + side.Abbrev, rowHeight, 12f);
                CanvasRows.Mark(row, country.Id, side.Abbrev, 30f);
                CanvasRows.FixedCell(row, Shown(side), 90f, Figure, PoliSimTheme.TextPrimary);
                CanvasRows.FixedCell(row, side.Seats.ToString(CultureInfo.InvariantCulture), 60f, Figure, PoliSimTheme.TextPrimary, TextAnchor.MiddleRight);
                CanvasRows.FixedCell(row, vote, 132f, Figure, ink, TextAnchor.MiddleLeft, PoliSimTheme.Display);   // the Body's bold is the Display file (§648: a real weight)
                Text alignment = CanvasRows.FixedCell(row, modelled ? Signed(side.Alignment) : string.Empty, 0f, Figure, PoliSimTheme.TextPrimary, TextAnchor.MiddleRight);
                LayoutElement stretch = alignment.GetComponent<LayoutElement>();
                stretch.minWidth = 0f;
                stretch.flexibleWidth = 1f;
                Hairline(row);

                string full = contest != null ? (side.Side > 0 ? "FOR " + contest.ProposalFor : side.Side < 0 ? "FOR " + contest.ProposalAgainst : "ABSTAINS")
                    : side.Defectors ? "DEFECTED - VOTED " + (side.Side > 0 ? "FOR" : "AGAINST") + ", ACROSS ITS PARTY'S STANCE" : vote.ToUpperInvariant();
                var lines = new List<string> { full + " · " + UiFormat.Seats(side.Seats).ToUpperInvariant() };
                if (modelled)
                {
                    lines.Add("ALIGNMENT " + Signed(side.Alignment) + " ON THE " + axis.ToUpperInvariant());
                    lines.Add(side.Reason.ToUpperInvariant());
                }
                else { lines.Add("NO REASON RECORDED - THIS DIVISION PREDATES THE STANCE MODEL"); }
                CanvasRows.Slip(row.gameObject, overlay, Shown(side) + (names.TryGetValue(side.Abbrev, out string name) ? " · " + name : string.Empty), lines);
            }
        }

        /// <summary>
        /// PS-6 US-18: THE STANCES ON A TWO-CHAMBER PLATE - one line a party, its ONE stance (R-US17 premise (B): a party votes as one, the same in both chambers),
        /// its seats in each chamber in the order the bill took them (a note above the rows names the order), in the first chamber's seat order; the party's reason
        /// on the row's slip, as on a single plate.
        /// </summary>
        private static void StancesInBothChambers(Transform column, Country country, DivisionRecord first, DivisionRecord second, Transform overlay)
        {
            string firstName = Congress.ChamberName(country.Id, first.Chamber), secondName = Congress.ChamberName(country.Id, second.Chamber);
            var parties = new List<string>();
            foreach (DivisionSide s in first.Sides.OrderByDescending(s => s.Seats)) { if (!parties.Contains(s.Abbrev)) { parties.Add(s.Abbrev); } }
            foreach (DivisionSide s in second.Sides.OrderByDescending(s => s.Seats)) { if (!parties.Contains(s.Abbrev)) { parties.Add(s.Abbrev); } }
            if (parties.Count == 0)
            {
                Note(column, "Uncontested - no stance asked");
                return;
            }
            Note(column, firstName + " · " + secondName + " seats - one stance a party");
            // PS-6 US-21: a party's drawn defectors are a side of their own beside its stance's (entered only where they decided a chamber's verdict) - the row keeps
            // the party's whole seats and its stance; the defectors are named above the rows and on the row's slip
            int Seats(DivisionRecord r, string abbrev, bool defectors) { int n = 0; foreach (DivisionSide s in r.Sides) { if (s.Abbrev == abbrev && (!defectors || s.Defectors)) { n += s.Seats; } } return n; }
            var crossings = new List<string>();
            foreach (string abbrev in parties)
            {
                int x = Seats(first, abbrev, true), y = Seats(second, abbrev, true);
                if (x > 0) { crossings.Add(PartySystems.ShortName(country.Id, abbrev) + " " + x.ToString(CultureInfo.InvariantCulture) + " in the " + firstName); }
                if (y > 0) { crossings.Add(PartySystems.ShortName(country.Id, abbrev) + " " + y.ToString(CultureInfo.InvariantCulture) + " in the " + secondName); }
            }
            if (crossings.Count > 0) { Note(column, "Defected · " + string.Join(" · ", crossings)); }
            var names = new Dictionary<string, string>();
            foreach (PoliticalParty party in PartySystems.For(country.Id)) { names[party.Abbrev] = party.Name; }
            float rowHeight = Mathf.Clamp(StanceBudget / parties.Count, MinStanceRow, RowHeight);
            foreach (string abbrev in parties)
            {
                DivisionSide a = first.Sides.Find(s => s.Abbrev == abbrev && !s.Defectors) ?? first.Sides.Find(s => s.Abbrev == abbrev);
                DivisionSide b = second.Sides.Find(s => s.Abbrev == abbrev && !s.Defectors) ?? second.Sides.Find(s => s.Abbrev == abbrev);
                DivisionSide side = b ?? a;   // one stance: the same side and alignment in both chambers
                int inFirst = Seats(first, abbrev, false), inSecond = Seats(second, abbrev, false);
                int crossedFirst = Seats(first, abbrev, true), crossedSecond = Seats(second, abbrev, true);
                string vote = side.Side > 0 ? "For" : side.Side < 0 ? "Against" : "Undecided";
                Color ink = side.Side > 0 ? PoliSimTheme.Good : side.Side < 0 ? PoliSimTheme.Bad : Muted;
                bool modelled = !string.IsNullOrEmpty(side.Reason);

                Transform row = CanvasRows.HRow(column, "Stance " + abbrev, rowHeight, 12f);
                CanvasRows.Mark(row, country.Id, abbrev, 30f);
                CanvasRows.FixedCell(row, Shown(side), 90f, Figure, PoliSimTheme.TextPrimary);
                CanvasRows.FixedCell(row, inFirst.ToString(CultureInfo.InvariantCulture) + " · " + inSecond.ToString(CultureInfo.InvariantCulture), 110f, Figure, PoliSimTheme.TextPrimary, TextAnchor.MiddleRight);
                CanvasRows.FixedCell(row, vote, 132f, Figure, ink, TextAnchor.MiddleLeft, PoliSimTheme.Display);   // the single plate's width: "Undecided" fits it
                Text alignment = CanvasRows.FixedCell(row, modelled ? Signed(side.Alignment) : string.Empty, 0f, Figure, PoliSimTheme.TextPrimary, TextAnchor.MiddleRight);
                LayoutElement stretch = alignment.GetComponent<LayoutElement>();
                stretch.minWidth = 0f;
                stretch.flexibleWidth = 1f;
                Hairline(row);

                var lines = new List<string>
                {
                    vote.ToUpperInvariant() + " · " + UiFormat.Seats(inFirst).ToUpperInvariant() + " IN THE " + firstName.ToUpperInvariant() + " · " + UiFormat.Seats(inSecond).ToUpperInvariant() + " IN THE " + secondName.ToUpperInvariant(),
                    "ONE STANCE IN BOTH CHAMBERS - A PARTY VOTES AS ONE",
                };
                if (crossedFirst + crossedSecond > 0)
                {
                    lines.Add((crossedFirst > 0 ? crossedFirst.ToString(CultureInfo.InvariantCulture) + " DEFECTED IN THE " + firstName.ToUpperInvariant() : string.Empty)
                        + (crossedFirst > 0 && crossedSecond > 0 ? " · " : string.Empty)
                        + (crossedSecond > 0 ? crossedSecond.ToString(CultureInfo.InvariantCulture) + " DEFECTED IN THE " + secondName.ToUpperInvariant() : string.Empty)
                        + " - DRAWN FROM THE RECORDED ROLL CALLS, COUNTED BECAUSE THEY DECIDED THE CHAMBER'S VERDICT");
                }
                if (modelled)
                {
                    lines.Add("ALIGNMENT " + Signed(side.Alignment) + " ON THE " + (second.Axis == (int)BillAxis.Trade ? "OPENNESS AXIS" : "FISCAL AXIS"));
                    lines.Add(side.Reason.ToUpperInvariant());
                }
                CanvasRows.Slip(row.gameObject, overlay, Shown(side) + (names.TryGetValue(abbrev, out string name) ? " · " + name : string.Empty), lines);
            }
        }

        /// <summary>§749: THE ESTIMATED IMPACT, one row an outcome in the Budget panel's grammar (§734) - the icon, the name, the move in its direction's ink; the scope on the slip.</summary>
        private static void BuildImpact(Transform column, DivisionRecord record, Transform overlay)
        {
            ColumnHead(column, "Estimated impact · next year");
            if (record.Effects.Count == 0)
            {
                Note(column, "No estimate travelled with this division");
                return;
            }
            foreach (DivisionEffect effect in record.Effects)
            {
                Transform row = CanvasRows.HRow(column, "Effect " + effect.Name, RowHeight, 15f);
                Texture2D icon = IconLibrary.V35(EffectArrowsRenderer.V35Icon(effect.Name));
                if (icon != null)
                {
                    Image iconImage = CanvasChrome.TintedImage(row, "Icon", CanvasChrome.Whole(icon, "icon_v35_" + EffectArrowsRenderer.V35Icon(effect.Name) + "#whole"), PoliSimTheme.Hex(0x5D564A));
                    iconImage.preserveAspect = true;
                    Fixed(iconImage.gameObject, 30f, 30f);
                }
                Text name = Word(row, effect.Name, PoliSimTheme.Body, Line, PoliSimTheme.TextPrimary);
                name.GetComponent<LayoutElement>().flexibleWidth = 1f;
                string figure = EffectArrowsRenderer.V35Figure(effect.Name, effect.Value);
                Word(row, figure, PoliSimTheme.Document, Line, EffectArrowsRenderer.V35Ink(effect.Value, effect.Neutral ? (bool?)null : effect.HigherIsBetter), TextAnchor.MiddleRight);
                Hairline(row);
                CanvasRows.Slip(row.gameObject, overlay, effect.Name.ToUpperInvariant() + " · " + figure, new[]
                {
                    "NEXT YEAR, WITH AGAINST WITHOUT THIS ACT - THE PREVIEW HELD FOR THE TURN IT WAS DECIDED IN",
                    "NO EVENTS · ONE DETERMINISTIC POINT - NOT A RANGE",
                });
            }
        }

        private static Transform Column(Transform parent, string name, float width, float flexible)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            LayoutElement size = go.AddComponent<LayoutElement>();
            size.minWidth = size.preferredWidth = width;
            size.flexibleWidth = flexible;
            VerticalLayoutGroup v = go.AddComponent<VerticalLayoutGroup>();
            v.childAlignment = TextAnchor.UpperLeft;
            v.spacing = 0f;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return go.transform;
        }

        /// <summary>A column's head, the composition's small capitals as capitals at the floor, in the muted ink.</summary>
        private static void ColumnHead(Transform column, string text)
        {
            Text head = CanvasChrome.MakeTextRealWeight(column, "ColumnHead", text.ToUpperInvariant(), PoliSimTheme.Body, Caption, Muted, TextAnchor.UpperLeft);
            LayoutElement size = head.gameObject.AddComponent<LayoutElement>();
            size.minHeight = size.preferredHeight = 45f;
        }

        /// <summary>A line of the muted ink under a column's figures - what abstained, or why there is nothing to show.</summary>
        private static void Note(Transform column, string text)
        {
            Text note = CanvasChrome.MakeTextRealWeight(column, "Note", text, PoliSimTheme.Body, Caption, Muted, TextAnchor.MiddleLeft);
            note.horizontalOverflow = HorizontalWrapMode.Wrap;
            note.gameObject.AddComponent<LayoutElement>().minHeight = 33f;
        }

        /// <summary>A word at its own width in a row (the row grammar's caption, in any face).</summary>
        private static Text Word(Transform row, string text, Font font, int size, Color ink, TextAnchor anchor = TextAnchor.MiddleLeft)
        {
            Text t = CanvasChrome.MakeTextRealWeight(row, "Word", text, font, size, ink, anchor);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.gameObject.AddComponent<LayoutElement>();
            return t;
        }

        private static GameObject Blank(Transform row)
        {
            var go = new GameObject("Gap");
            go.transform.SetParent(row, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static void Fixed(GameObject go, float width, float height)
        {
            LayoutElement size = go.GetComponent<LayoutElement>();
            if (size == null) { size = go.AddComponent<LayoutElement>(); }   // never `??` on a UnityEngine.Object: its fake null passes it
            size.minWidth = size.preferredWidth = width;
            size.minHeight = size.preferredHeight = height;
            size.flexibleWidth = 0f;
        }

        /// <summary>The row's hairline at its foot, outside its layout.</summary>
        private static void Hairline(Transform row)
        {
            var go = new GameObject("Hairline");
            go.transform.SetParent(row, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0f, 0f);
            r.anchorMax = new Vector2(1f, 0f);
            r.pivot = new Vector2(0.5f, 0f);
            r.sizeDelta = new Vector2(0f, 1.5f);
            go.AddComponent<LayoutElement>().ignoreLayout = true;
            Image image = go.AddComponent<Image>();
            image.color = PoliSimTheme.Hex(0xE2D7C1);
            image.raycastTarget = false;
        }

        private static void Rule(Transform parent, string name, float height, Color ink)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, height);
            LayoutElement size = go.AddComponent<LayoutElement>();
            size.minHeight = size.preferredHeight = height;
            Image image = go.AddComponent<Image>();
            image.color = ink;
            image.raycastTarget = false;
        }

        /// <summary>An alignment with its sign, the minus a true minus (election night's form).</summary>
        private static string Signed(float value)
        {
            string magnitude = UiFormat.Number(Mathf.Abs(value), 2);
            return value > 0f ? "+" + magnitude : value < 0f ? "−" + magnitude : magnitude;
        }

        /// <summary>SP-7b: the answer line's width in the sign row, in canvas units - the row's width less the landing, the three answers and the gaps, with room spare.</summary>
        private const float AnswerLineWidth = 640f;

        /// <summary>SP-7b: an answer's face on the plate, in canvas units - narrower than SIGN alone, so three fit beside the line.</summary>
        private const float AnswerWidth = 200f;

        /// <summary>SP-7b: the plate's answers - LATER (the statute stays on the docket, answered there), VETO where the act takes one, both on the paper
        /// face, and SIGN on the brass - in one group that fades in last (§A.13's row 6), as the SIGN button alone does.</summary>
        private static CanvasGroup BuildAnswerButtons(Transform parent, Action onSign, Action onVeto, Action onLater)
        {
            var answers = new GameObject("Answers");
            answers.transform.SetParent(parent, false);
            int count = 1 + (onVeto != null ? 1 : 0) + (onLater != null ? 1 : 0);
            answers.AddComponent<RectTransform>().sizeDelta = new Vector2(count * AnswerWidth + (count - 1) * 30f, 60f);
            HorizontalLayoutGroup answersLayout = answers.AddComponent<HorizontalLayoutGroup>();
            answersLayout.childAlignment = TextAnchor.MiddleRight;
            answersLayout.spacing = 30f;
            answersLayout.childControlWidth = false;
            answersLayout.childControlHeight = false;
            answersLayout.childForceExpandWidth = false;
            answersLayout.childForceExpandHeight = false;
            if (onLater != null)
            {
                Button later = CanvasChrome.FacedButton(answers.transform, "LaterButton", "Later",
                    PoliSimTheme.Body, 27, PoliSimTheme.Hex(0x4A3A22), new Vector2(AnswerWidth, 60f), CanvasChrome.Face.Paper, FontStyle.Normal);
                later.onClick.AddListener(() => onLater());
            }
            if (onVeto != null)
            {
                Button veto = CanvasChrome.FacedButton(answers.transform, "VetoButton", "Veto",
                    PoliSimTheme.Body, 27, PoliSimTheme.Hex(0x4A3A22), new Vector2(AnswerWidth, 60f), CanvasChrome.Face.Paper, FontStyle.Normal);
                veto.onClick.AddListener(() => onVeto());
            }
            Button sign = CanvasChrome.FacedButton(answers.transform, "SignButton", "Sign",
                PoliSimTheme.Body, 27, PoliSimTheme.TextPrimary, new Vector2(AnswerWidth, 60f), CanvasChrome.Face.Brass, FontStyle.Normal);
            sign.onClick.AddListener(() => onSign());
            CanvasGroup group = answers.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        /// <summary>The canvas brass button pattern: uGUI Button + SpriteSwap over the delivered per-state strips. The label reads "Sign" only for a passed division - "File" for a lost one, matching the plate's own LOST stamp rather than claiming an enactment that did not happen. Returns the button's CanvasGroup - §A.13 row 6's fade handle (the controls fade in last).</summary>
        private static CanvasGroup BuildSignButton(Transform parent, Action onSign, bool passed, bool congressVeto = false)
        {
            // P6-A2: the face, its states and its degradation live in `CanvasChrome.FacedButton` now - this
            // method was the pattern the other Canvas screens copied, and a copied pattern is what let the
            // selector's controls end up with no face at all.
            Button control = CanvasChrome.FacedButton(parent, "SignButton", passed && !congressVeto ? "Sign" : "File",   // PS-6 US-35 (board 30b, For Code 7): an overridden bill asks no signature - File
                PoliSimTheme.Body, 27, PoliSimTheme.TextPrimary, new Vector2(300f, 60f), CanvasChrome.Face.Brass, FontStyle.Normal);   // §749: the composition's 200 x 40, the word in the body face
            control.onClick.AddListener(() => onSign());

            // Row 6's handle: the group starts invisible and non-interactable; DocumentEntrance brings
            // it in once the document has settled, so a click cannot land on a button that is not yet
            // there (input locks are the envelope's first row).
            CanvasGroup group = control.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            return group;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    /// <summary>
    /// §A.13 rows 4 and 6 (built 2026-08-28, omnibus roadmap item 4): the document rises 24px and
    /// settles −0.6° → 0° over 260ms with an ease-out cubic (the envelope's 240–500ms), and the
    /// controls fade in LAST - 460ms after the document starts (the envelope's 700ms mark, counted
    /// from the 240ms swap), over 200ms. Unscaled time, like the seal beat, so a held sim clock does
    /// not freeze the ceremony. The rest position is captured on the FIRST enable only: the seam can
    /// hide and re-show the screen, and re-capturing mid-rise would drift the document.
    /// </summary>
    public class DocumentEntrance : MonoBehaviour
    {
        private const float RiseSeconds = 0.26f;
        private const float RisePixels = 24f;
        private const float SettleDegrees = -0.6f;
        private const float ControlsDelaySeconds = 0.46f;
        private const float ControlsFadeSeconds = 0.2f;

        public CanvasGroup Controls;

        public bool Settled { get; private set; }

        private Vector2 _rest;
        private bool _restCaptured;
        private float _startTime;

        private void OnEnable()
        {
            var rect = (RectTransform)transform;
            if (!_restCaptured)
            {
                _rest = rect.anchoredPosition;
                _restCaptured = true;
            }

            _startTime = Time.unscaledTime;
            Settled = false;
            Apply(0f);
            if (Controls != null)
            {
                Controls.alpha = 0f;
                Controls.interactable = false;
                Controls.blocksRaycasts = false;
            }
        }

        private void Update()
        {
            float elapsed = Time.unscaledTime - _startTime;
            Apply(Mathf.Clamp01(elapsed / RiseSeconds));

            if (Controls != null)
            {
                float fade = Mathf.Clamp01((elapsed - ControlsDelaySeconds) / ControlsFadeSeconds);
                Controls.alpha = fade;
                bool live = fade >= 1f;
                Controls.interactable = live;
                Controls.blocksRaycasts = live;
            }

            if (elapsed >= ControlsDelaySeconds + ControlsFadeSeconds)
            {
                Settled = true;
            }
        }

        private void Apply(float t)
        {
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            var rect = (RectTransform)transform;
            rect.anchoredPosition = _rest + new Vector2(0f, -RisePixels * (1f - eased));
            rect.localRotation = Quaternion.Euler(0f, 0f, SettleDegrees * (1f - eased));
        }
    }

    /// <summary>§1g's seal beat: scale 1.3 → 1.0 over 140ms, a 6px settle nudge, then a short hold before <see cref="Settled"/> reports true and the seam covers out.</summary>
    public class SealDrop : MonoBehaviour
    {
        private const float DropSeconds = 0.14f;
        private const float HoldSeconds = 0.5f;

        public bool Settled { get; private set; }

        private float _startTime;

        private void OnEnable()
        {
            _startTime = Time.unscaledTime;
            transform.localScale = new Vector3(1.3f, 1.3f, 1f);
        }

        private void Update()
        {
            float elapsed = Time.unscaledTime - _startTime;
            float t = Mathf.Clamp01(elapsed / DropSeconds);
            float scale = Mathf.Lerp(1.3f, 1f, t);
            transform.localScale = new Vector3(scale, scale, 1f);

            var rect = (RectTransform)transform;
            rect.anchoredPosition = t >= 1f && elapsed < DropSeconds + 0.08f
                ? new Vector2(0f, -6f * (1f - (elapsed - DropSeconds) / 0.08f))
                : Vector2.zero;

            if (elapsed >= DropSeconds + HoldSeconds)
            {
                Settled = true;
            }
        }
    }
}
