#!/usr/bin/env python3
"""Fails when a captured screen has content sitting on the edge of OnGUI's clip rect.

WHY THIS EXISTS
    Clipping has recurred twelve times in this project. Eleven were text inside a rect, and two guards
    were built for that shape - UiOverflowGuard (does this text fit the rect it was handed?) and
    UiContainmentGuard (does this child rect sit inside its container?). The twelfth was the RECT: five
    GUILayout groups laid out wider and taller than OnGUI's own BeginArea, so the area clipped them.
    Both guards reported zero while it was on screen, correctly - neither asks that question, and
    UiContainmentGuard's own doc says so. See CLAUDE.md, "Instance #12".

    This asks the frame question instead, and it needs no engine access: it reads the PNGs the capture
    pass already writes.

HOW IT DECIDES
    The desk colour is sampled from pixel (1,1) - outside the margin by construction, since the layout
    is inset by GameController.ScreenMarginFraction. Any pixel far enough from it is content. A screen
    is FLAGGED when content sits on the last drawable column or row inside the clip rect while the
    OPPOSITE edge is clear. That asymmetry is what separates a clip from a full-bleed background:
    DrawMenuBackground fills the whole screen on purpose, so 01_country_selector reads flush on all
    four edges and is deliberately not a finding.

    It reports flush-ness, NOT overrun magnitude - clipped content stops exactly at the boundary, so
    the pixels past it are gone and cannot be measured from the capture. A clean run says nothing about
    how much slack a screen has left, and nothing about aspect ratios that were not captured.

USAGE
    python3 screenshot_edge_check.py "screenshots/clipfix2_*.png"
    exit 0 = clean, 1 = at least one screen clipped, 2 = bad invocation
"""

import glob
import os
import sys

import numpy as np
from PIL import Image

# GameController.ScreenMarginFraction. If that constant changes, this must change with it.
SCREEN_MARGIN_FRACTION = 0.02

# Manhattan distance in RGB from the desk colour before a pixel counts as content. Low enough to catch
# the paper's own drop shadow, high enough to ignore PNG quantisation on the flat desk.
CONTENT_THRESHOLD = 30

# Sub-pixel seams and antialiasing put a handful of stray pixels on any edge; a real clipped panel puts
# hundreds. Same "a guard that cries wolf gets switched off" reasoning as the two C# guards.
FLUSH_MIN_PIXELS = 20


def analyse(path):
    image = np.array(Image.open(path).convert("RGB")).astype(int)
    height, width, _ = image.shape

    desk = image[1, 1]
    content = np.abs(image - desk).sum(2) > CONTENT_THRESHOLD

    margin_x = round(width * SCREEN_MARGIN_FRACTION)
    margin_y = round(height * SCREEN_MARGIN_FRACTION)

    return {
        "name": os.path.basename(path),
        "left": int(content[:, margin_x].sum()),
        "top": int(content[margin_y, :].sum()),
        "right": int(content[:, width - margin_x - 1].sum()),
        "bottom": int(content[height - margin_y - 1, :].sum()),
    }


def main(pattern):
    paths = sorted(glob.glob(pattern))
    if not paths:
        print(f"no captures matched {pattern!r}", file=sys.stderr)
        return 2

    flagged = []
    print(f'{"screen":46} {"left":>6} {"top":>6} {"right":>6} {"bottom":>7}')
    for path in paths:
        row = analyse(path)
        flush = lambda edge: row[edge] > FLUSH_MIN_PIXELS

        # Full-bleed on an axis (both sides flush) is a background, not a clip.
        clipped = (flush("right") and not flush("left")) or (flush("bottom") and not flush("top"))
        if clipped:
            flagged.append(row["name"])

        note = "  <-- CLIPPED" if clipped else ""
        print(f'{row["name"][:46]:46} {row["left"]:6} {row["top"]:6} '
              f'{row["right"]:6} {row["bottom"]:7}{note}')

    print(f"\n{len(paths)} screens, {len(flagged)} clipped")
    return 1 if flagged else 0


if __name__ == "__main__":
    if len(sys.argv) != 2:
        print(__doc__, file=sys.stderr)
        sys.exit(2)
    sys.exit(main(sys.argv[1]))
