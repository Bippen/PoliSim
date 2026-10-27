# K127: how far a created Regional party reaches in its own valkrets (a measurement, 2026-10-27)

**Why.** Elias ruled K127 (film a campaigned Regional party winning Riksdag seats by the 12 % valkrets route, K122), then, on the finding that no played campaign gets near the line, chose to measure first. A one-off Editor probe (`RegionalReachProbe`, run once as `probe127`, then deleted, not committed) measured four levels at the best placement on a 5x5 compass grid in each of the 29 valkretsar: (a) play's prediction at the Regional preset; (b) the same at Recognition 5 (no campaign); (c) the preference model at full awareness with the reach cap lifted (the most a perfect campaign could buy); (d) one seeded campaign (777) at the best valkrets with every stat at 5, the party played by its cast AI.

**The log, verbatim** (`PoliSim-captures/logs/probe127.log`):

```
=== RegionalReachProbe (K127): a Regional party's reach inside its own valkrets ===
    the line 12.0 %; the Regional preset's reach 0.65; 29 valkretsar
    measured  Stockholms kommun      best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Stockholms län         best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Göteborgs kommun       best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Östergötlands län      best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Uppsala län            best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Västra Götalands läns västra best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Jönköpings län         best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Dalarnas län           best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Skåne läns södra       best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Västmanlands län       best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Södermanlands län      best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Västra Götalands läns östra best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Gävleborgs län         best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Hallands län           best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Värmlands län          best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Skåne läns norra och östra best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Västra Götalands läns södra best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Norrbottens län        best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Västerbottens län      best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Västernorrlands län    best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Västra Götalands läns norra best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Skåne läns västra      best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Malmö kommun           best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Kalmar län             best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Kronobergs län         best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Örebro län             best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Jämtlands län          best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Blekinge län           best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  Gotlands län           best in-valkrets share: (a) play 3.11 %, (b) Recognition 5 7.80 %, (c) full awareness, uncapped 20.71 % at (2.5, 7.5)  <- CROSSES THE LINE
    measured  (d) a seeded campaign (777), Stockholms kommun at (2.5, 7.5), every stat 5, played by its cast AI: national 0.78 %, inside Stockholms kommun 8.07 % against the 12.0 % line
```

**Read.** (1) The cap binds: at Recognition 5 the share stops at exactly 7.80 % = the Regional reach 0.65 x the 12 % line; a campaign lifts the cap with awareness, and the AI's best campaign reaches 8.07 % - the line is crossed only near full awareness (20.71 % uncapped). (2) Every valkrets gives the same figures at the same placement (2.5, 7.5): the regional layer scales one national figure by the region's weight and does not read the region's own electorate, so Gotland and Stockholms kommun are alike.
