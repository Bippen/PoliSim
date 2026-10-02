# fetch_log - ElectionsData/poland/raw/exit_poll_2025/ (item B4, 2025 run-off transfers, Ipsos exit poll and late poll), 2026-10-02

Command: `curl -s -L -A "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36" -H "Accept-Language: pl-PL,pl;q=0.9,en;q=0.8" -o <file> <url>`; bytes stored as received. Status is the final status after redirects. Digests in `SHA256SUMS.txt`. News pages are dynamic: the stored bytes are the page as served at the time below (an earlier probe of the Konkret24 page the same morning was 599,834 bytes; the article text was identical).

| UTC | HTTP | bytes | file | URL |
|---|---|---|---|---|
| 2026-10-02T06:35:56Z | 200 | 51992 | polsatnews_2025-06-01_przeplywy_w_ii_turze.html | https://www.polsatnews.pl/wiadomosc/2025-06-01/przeplywy-w-ii-turze-tak-glosowali-wyborcy-innych-kandydatow/ |
| 2026-10-02T06:35:57Z | 200 | 570797 | tvn24_st8487352_wyborcy_mentzena.html | https://tvn24.pl/polska/wyniki-wyborow-prezydenckich-2025-jak-glosowali-wyborcy-mentzena-tu-bez-zaskoczen-st8487352 |
| 2026-10-02T06:36:00Z | 200 | 569463 | tvn24_st8487890_wyborcy_holowni.html | https://tvn24.pl/polska/wyniki-wyborow-prezydenckich-2025-jak-glosowali-wyborcy-szymona-holowni-wiekszosc-tak-jak-prosil-st8487890 |
| 2026-10-02T06:36:00Z | 200 | 570148 | tvn24_st8486898_wyborcy_zandberga.html | https://tvn24.pl/polska/wyniki-wyborow-2025-jak-glosowali-wyborcy-adriana-zandberga-w-wiekszosci-zgodnie-st8486898 |
| 2026-10-02T06:36:02Z | 200 | 580989 | tvn24_st8491383_nieglosujacy_w_i_turze.html | https://tvn24.pl/polska/wyniki-wyborow-prezydenckich-2025-jak-glosowaly-osoby-ktore-nie-wybraly-sie-do-urn-w-pierwszej-turze-komentarze-ekspertow-st8491383 |
| 2026-10-02T06:36:03Z | 200 | 601160 | konkret24_st8492152_przeplywy_late_poll.html | https://konkret24.tvn24.pl/polityka/wyniki-wyborow-2025-z-biejat-i-zandberga-na-nawrockiego-niespodziewane-przeplywy-elektoratow-st8492152 |
| 2026-10-02T06:36:04Z | 200 | 95989 | secondary_polskieradio24_3531756_przeplywy_exit_poll.html [SECONDARY] | https://polskieradio24.pl/artykul/3531756,jak-zaglosowali-wyborcy-brauna-holowni-i-mentzena-przeplywy-elektoratu-w-ii-turze |
| 2026-10-02T06:36:04Z | 200 | 276744 | secondary_ipl_c1p2-27645385_exit_poll_przeplywy.html [SECONDARY] | https://i.pl/wyniki-exit-poll-drugiej-tury-tak-glosowali-wyborcy-innych-kandydatow/ar/c1p2-27645385 |
| 2026-10-02T06:36:05Z | 200 | 143633 | secondary_polityka_2302461_exit_poll.html [SECONDARY] | https://www.polityka.pl/tygodnikpolityka/kraj/2302461,1,wyniki-exit-poll-trzaskowski-lekko-przed-nawrockim-przewaga-wisi-na-cienkim-sznurku.read |
| 2026-10-02T06:36:05Z | 200 | 46559 | ipsos_pl-pl_exit-poll.html | https://www.ipsos.com/pl-pl/exit-poll |
| 2026-10-02T06:36:06Z | 200 | 28753 | [NOT KEPT] tvpinfo_87034640_exit_poll_2100.html | https://www.tvp.info/87034640/wyniki-wyborow-prezydenckich-2025-exit-poll-o-2100-kto-wygral-wybory-prezydenckie-w-polsce-wynik-rafala-trzaskowskiego-i-karola-nawrockiego-ilu-polakow-glosowalo-w-wyborach |

Notes
- **Ipsos publishes no flow table of its own.** Ipsos Polska's exit-poll page (kept) describes the method and sends readers to the broadcasters' sites ("Wyniki ostatnich badań exit poll dostępne są między innymi na stronach: ..." - TVN24 links); no 2025 table or PDF was found on ipsos.com. The exit poll and late poll were commissioned by **TVN24, TVP and Polsat News**; their own articles are therefore the primary publication: Polsat News (`polsatnews_*`), TVN24 (`tvn24_*`) and TVN24's Konkret24 (`konkret24_*`).
- TVP Info's 21:00 exit-poll page is a JavaScript shell (no figures in the served HTML); not kept.
- [SECONDARY] = a medium that did not commission the poll and reports it: Polskie Radio 24 (public radio; the only page found with all eleven eliminated candidates, "Dane exit poll z godziny 21"), i.pl (Polska Press) and Polityka.
- Published times (from the pages' `datePublished`): Polsat 2025-06-01 21:10 CEST; TVN24 Mentzen/Hołownia/Zandberg 21:02 CEST (updated later with late-poll figures); TVN24 non-voters 2025-06-02 00:18 CEST; Polskie Radio 24 2025-06-01 21:56 CEST (modified 22:17); i.pl 21:57 CEST; Konkret24 2025-06-02 18:10 CEST.
- The file `tvn24_st8491383_nieglosujacy_w_i_turze.html` was requested with a Polish "ł" in its name; the file system stored it as ASCII "l".
