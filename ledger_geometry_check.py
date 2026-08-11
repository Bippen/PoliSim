#!/usr/bin/env python3
"""Ports LedgerRow.Columns() (Assets/Scripts/UI/LedgerRow.cs) and the width chain that feeds it
(GameController.cs: OnGUI -> DrawBudgetProcessTab -> DrawTaxLineRow/DrawWelfareProgramRow/SwfRow) to
evaluate whether the Budget screen's ledger trailing column fits its row AT A RESOLUTION THAT WAS NEVER
CAPTURED: 1440p (2560x1440). Written per polisim-verify (the algorithm-port pattern) because
screenshot_edge_check.py can only ever answer this question for the one resolution the Unity Editor Game
View was captured at (1600x929 delivered) - never for 2560x1440, which no capture on disk uses.

WHY A PORT INSTEAD OF A GUESS
    LedgerRow.Columns() computes four column widths from `row.width` and a font-derived `scale`, squeezes
    them together if they don't fit, and floors the squeeze at 0.35 so columns cannot shrink below
    legibility - which means, by construction, the code does NOT guarantee containment past that floor.
    Whether that floor is ever actually hit at 2560x1440 is an arithmetic question, not a visual one, so
    it is answered here in Python rather than by eyeballing a screenshot that does not exist.

CONSTANTS - every one is transcribed from source, cited by file:line as read 2026-08-11. If any of these
change, this must change with them (same discipline screenshot_edge_check.py's own header states for
SCREEN_MARGIN_FRACTION).
    GameController.cs
        ScreenMarginFraction = 0.02                              (line 132)
        RescaleStylesToScreen: labelFontSize =
            clamp(round(Screen.height * 0.022), 16, 28)          (line 1165)
        budgetFullScreen => rightColumnWidth = areaWidth          (lines 850-852)
        DrawBudgetProcessTab: local columnSpacing = 10,
            scrollbarAllowance = 18,
            categoryColumnWidth = clamp(usableWidth*0.16,
                labelFontSize*7, labelFontSize*10),
            summaryColumnWidth = usableWidth * 0.34,
            centerColumnWidth = usableWidth - category - summary  (lines 4677-4710)
    PoliSimWidgets.cs
        InnerWidth(outer, container) = outer - container.padding.horizontal
            - container.margin.horizontal                        (lines 211-216)
    LedgerRow.cs
        Columns(): nameWidth  = max(row.width*0.26, 14*scale*4)
                   figureWidth = max(row.width*0.19, 14*scale*3)
                   trailingWidth = max(row.width*0.11, 14*scale*2)
                   minTrack   = 14*scale*4, gap = 10*scale
                   squeeze = max(0.35, available/fixedTotal) when fixedTotal > available
                                                                   (lines ~158-215)

UNMEASURED RESIDUAL - stated up front, not buried: _boxStyle is `new GUIStyle(GUI.skin.box)`, Unity's
BUILT-IN default box style, never overridden. Its padding.horizontal (28px) and margin.horizontal (8px)
are taken from two in-repo comments that report them as directly measured at runtime (GameController.cs
lines ~858-868 and ~4655-4659), not from guessing Unity's default GUISkin. What this script CANNOT
account for: the vertical scrollbar Unity's GUILayout.BeginScrollView reserves inside the centre column
when content overflows vertically (it always does, on Budget - five to eight rows in a shorter box). That
scrollbar has a nonzero width this port does not subtract, which means every `row_width` below is an
UPPER BOUND on the real value - the true available width is somewhat smaller, so a PASS here is slightly
optimistic and a FAIL is if anything conservative on the pixel count but not on the verdict.

USAGE
    python3 ledger_geometry_check.py
"""

import sys


def clamp(x, lo, hi):
    return max(lo, min(hi, x))


def label_font_size(screen_height):
    """GameController.RescaleStylesToScreen, line 1165."""
    return clamp(round(screen_height * 0.022), 16, 28)


def inner_width(outer, padding_h, margin_h):
    """PoliSimWidgets.InnerWidth with childCount=1, child=None."""
    return max(1.0, outer - padding_h - margin_h)


BOX_PADDING_H = 28.0   # GUI.skin.box padding.horizontal, measured at runtime (GameController.cs ~4655)
BOX_MARGIN_H = 8.0     # GUI.skin.box margin.horizontal, measured at runtime (GameController.cs ~858)


def row_width_for(screen_width, screen_height, note=None):
    """Screen.width/height -> the Rect width LedgerRow.Draw actually receives as `row` on the Budget
    screen's Tax/Spending/Welfare/Infrastructure/SWF sub-tabs (all five share this exact container
    chain: OnGUI -> the budget-full-screen right column -> DrawBudgetProcessTab's centre column box)."""
    margin_x = screen_width * 0.02
    area_width = screen_width - 2 * margin_x               # OnGUI line 832
    right_column_width = area_width                        # budgetFullScreen, line 852

    content_width = inner_width(right_column_width, BOX_PADDING_H, BOX_MARGIN_H)   # DrawBudgetProcessTab l.4660

    local_column_spacing = 10.0
    scrollbar_allowance = 18.0
    usable_width = content_width - local_column_spacing * 2 - scrollbar_allowance  # line 4707

    font_size = label_font_size(screen_height)
    category_column_width = clamp(usable_width * 0.16, font_size * 7, font_size * 10)  # line 4709
    summary_column_width = usable_width * 0.34                                         # line 4710
    center_column_width = usable_width - category_column_width - summary_column_width  # line 4711

    row_width = inner_width(center_column_width, BOX_PADDING_H, BOX_MARGIN_H)  # centre box's own _boxStyle wrapper

    return {
        "label": note or f"{screen_width}x{screen_height}",
        "screen_width": screen_width,
        "screen_height": screen_height,
        "font_size": font_size,
        "area_width": area_width,
        "content_width": content_width,
        "usable_width": usable_width,
        "category_column_width": category_column_width,
        "summary_column_width": summary_column_width,
        "center_column_width": center_column_width,
        "row_width": row_width,
    }


def ledger_columns(row_width, font_size, trailing_need=0.0):
    """LedgerRow.Columns(), transcribed line for line."""
    ref_font_size = 13.0
    ref_knob_width = 14.0
    ref_column_gap = 10.0

    scale = font_size / ref_font_size
    gap = ref_column_gap * scale

    name_width = max(row_width * 0.26, ref_knob_width * scale * 4)
    figure_width = max(row_width * 0.19, ref_knob_width * scale * 3)
    trailing_width = max(row_width * 0.11, ref_knob_width * scale * 2)
    min_track = ref_knob_width * scale * 4

    if trailing_need > trailing_width:
        trailing_width = min(trailing_need, row_width * 0.34)

    fixed_total = name_width + figure_width + trailing_width
    available = row_width - gap * 3 - min_track

    squeeze = 1.0
    squeezed = False
    if fixed_total > available and fixed_total > 0:
        squeeze = max(0.35, available / fixed_total)
        name_width *= squeeze
        figure_width *= squeeze
        trailing_width *= squeeze
        squeezed = True

    track_width = max(min_track, row_width - name_width - figure_width - trailing_width - gap * 3)

    name_x = 0.0
    track_x = name_x + name_width + gap
    figure_x = track_x + track_width + gap
    trailing_x = figure_x + figure_width + gap
    trailing_x_max = trailing_x + trailing_width

    escape_px = trailing_x_max - row_width  # >0 means the trailing column's right edge is past the row

    return {
        "scale": scale,
        "name_width": name_width,
        "figure_width": figure_width,
        "trailing_width": trailing_width,
        "track_width": track_width,
        "min_track": min_track,
        "fixed_total_pre_squeeze": name_width if not squeezed else fixed_total,
        "available_for_fixed_cols": available,
        "squeeze_factor": squeeze,
        "squeeze_floor_hit": squeeze <= 0.35 + 1e-9,
        "trailing_x_max": trailing_x_max,
        "escape_px": escape_px,
        "escapes": escape_px > 0.05,  # sub-pixel tolerance only
    }


SCENARIOS = [
    (1600, 929, "captured (Editor Game View, this morning's clipfix2 pass - CALIBRATION)"),
    (2560, 1440, "1440p fullscreen, 16:9 (the reported resolution)"),
    (1920, 1080, "1080p fullscreen, 16:9 (context)"),
    (3840, 2160, "4K fullscreen, 16:9 (context)"),
]


def main():
    print(f'{"scenario":58} {"row.width":>10} {"font":>5} {"squeeze":>8} {"trailXmax":>10} {"escape px":>10}')
    any_escape = False
    for width, height, note in SCENARIOS:
        geo = row_width_for(width, height, note)
        cols = ledger_columns(geo["row_width"], geo["font_size"], trailing_need=0.0)
        flag = "  <-- TRAILING COLUMN ESCAPES ROW" if cols["escapes"] else ""
        if cols["escapes"]:
            any_escape = True
        print(f'{note[:58]:58} {geo["row_width"]:10.1f} {geo["font_size"]:5d} '
              f'{cols["squeeze_factor"]:8.3f} {cols["trailing_x_max"]:10.1f} {cols["escape_px"]:10.1f}{flag}')

    print()
    print("Full intermediate values for the two scenarios that matter:")
    for width, height, note in SCENARIOS[:2]:
        geo = row_width_for(width, height, note)
        cols = ledger_columns(geo["row_width"], geo["font_size"], trailing_need=0.0)
        print(f"\n--- {note} ({width}x{height}) ---")
        for k, v in geo.items():
            if k == "label":
                continue
            print(f"  {k:24} {v:.2f}" if isinstance(v, float) else f"  {k:24} {v}")
        for k, v in cols.items():
            print(f"  {k:24} {v:.4f}" if isinstance(v, float) else f"  {k:24} {v}")

    return 1 if any_escape else 0


if __name__ == "__main__":
    sys.exit(main())
