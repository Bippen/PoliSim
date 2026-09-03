# The six countries' geographic centres and the map's adjacency — the table our side supplies (board 6b row 1, 2026-09-03)

Board 6b row 1 ruled the stylized map's FORM (six plates on a plain paper field, a 26×20 chip per country in its own outline at its centroid, snapped to a 24-grid; links the map's own `TradePartner` pairs in the Trade ink, dashed until a volume exists, weight ∝ the larger flow) and said the geometry was pending this table. This is the table. It is also the table `MapRenderer.CountryCentroids` reads, so the game and the board place the chips from one source.

## The centroids — one row per country

Latitude and longitude in degrees, WGS84. Each is the commonly cited geographic centre of the country's territory (the contiguous states for the USA), named after the reference point the national record uses. **Tag: AUTHORED-REFERENCE** — the figures are quoted from the reference points named, not measured by this project; a reader can check each against the point's own record.

| country | tag | centre (lat, lon) | the reference point |
|---|---|---|---|
| Sweden | SE | 62.3875 N, 16.3250 E | Flataklocken, Medelpad — the geographic centre of Sweden |
| Germany | DE | 51.1642 N, 10.4541 E | Niederdorla, Thuringia — the geographic centre of Germany |
| France | FR | 46.5386 N, 2.4306 E | Nassigny, Allier — the geographic centre of metropolitan France (IGN) |
| Italy | IT | 42.5167 N, 12.5167 E | Narni, Umbria — the geographic centre of Italy |
| Poland | PL | 52.0694 N, 19.4794 E | Piątek, Łódź Voivodeship — the geographic centre of Poland |
| United States | US | 39.8283 N, −98.5795 W | Lebanon, Kansas — the geographic centre of the contiguous United States |

## The projection the game uses

A flat plate: the six's own bounding box (longitude −98.58 … 19.48, latitude 39.83 … 62.39) mapped onto the map rect with a 6 % pad on every side, longitude to x and latitude to y (north up), then each chip snapped to the 24-grid (24 units at the 1280 face, scaling with the label face). No coastline, no curvature: the board's own ruling — "no coastline, no projection, no seventh shape".

## The adjacency — the map's own links

The links are `Country.TradePartners` at the seed, one line per unordered pair; nothing is added. At the seed the pairs are those the World Map has always drawn (USA–Germany, USA–France, USA–Poland, Germany–France, Germany–Sweden, Germany–Italy, Germany–Poland, France–Italy, Sweden–Poland, and any the seed holds beyond these); a pair with no volume yet draws dashed, and a pair's weight is the larger of its two flows relative to the largest on the map. The film `p5d_1280_02b_statistics_international` is the drawing.
