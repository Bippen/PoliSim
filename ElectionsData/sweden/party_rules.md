# Sweden - a new party's rules: registration and ballot access, state party funding (SP-3, `COMPLETED.md` §625)

Fetched 2026-09-25 from riksdagen.se, the Riksdag's consolidated statute texts, stored byte for byte under
`raw/party_rules/` with `SHA256SUMS` beside them (vallagen's page is the same consolidation as `raw/calendar/`'s,
"t.o.m. SFS 2026:989"; the party-support act "t.o.m. SFS 2018:1406").

## Registration of a party designation (ballot access) - vallagen (2005:837) 2 kap.

| id | clause, verbatim (whitespace collapsed) |
|---|---|
| [VL-2-3] | 2 kap. 3 § "En partibeteckning ska registreras om följande villkor är uppfyllda: 1. Partibeteckningen ska bestå av eller innehålla ord. Den får innehålla en partisymbol. 2. Om ett parti inte redan är representerat i den beslutande församling som anmälan gäller, ska anmälan ha ett dokumenterat stöd av minst a) för val till riksdagen: 1 500 personer som har rösträtt i hela landet, b) för val till region- eller kommunfullmäktige: 100 respektive 50 personer som har rösträtt i den region eller kommun som anmälan gäller, c) för val till Europaparlamentet: 1 500 personer som har rösträtt i hela landet. 3. Partibeteckningen ska inte kunna antas bli förväxlad med en beteckning som redan a) är registrerad, eller b) har anmälts för registrering …" Lag (2019:923). |
| [VL-2-5] | 2 kap. 5 § "Om en partibeteckning registreras för val till riksdagen, gäller registreringen också för val till region- och kommunfullmäktige i hela landet samt för val till Europaparlamentet." |
| [VL-2-7] | 2 kap. 7 § "En registrerad partibeteckning ska avregistreras om partiet 1. begär det, eller 2. inte har anmält kandidater för två ordinarie val i följd …" Lag (2019:923). |

**The rule in the game (SP-3):** a created Swedish party that is not represented in the Riksdag registers its
designation with the documented support of **1 500 eligible voters** - the run-up task of the spec's §2.7 - and a
designation that could be confused with a registered one is refused (the name check). The deadlines for registration and candidate notification are on the page - an application for registration must reach the authority "senast den sista februari det år då ordinarie val till riksdagen … ska hållas", the last day of February of the election year - and are not yet carried; the task's calendar is SP-3's next cut.

## State support to political parties - lag (1972:625) om statligt stöd till politiska partier

| id | clause, verbatim (whitespace collapsed) |
|---|---|
| [PS-2] | 2 § "Partistöd lämnas som mandatbidrag. Varje mandatbidrag utgör 324 704 kronor." Lag (2018:1406). |
| [PS-3] | 3 § "Antalet mandatbidrag som varje parti erhåller bestämmes årligen med hänsyn till utgången i de två närmast föregående ordinarie valen … Första året under den fyraårsperiod som följer efter val till riksdagen erhåller varje parti så många mandatbidrag som motsvarar en sjättedel gånger antalet vunna mandat i det senaste valet plus fem sjättedelar gånger antalet vunna mandat i närmast föregående val. Andra året … halva … plus halva … Tredje året och fjärde året … fem sjättedelar … plus en sjättedel … Har parti i något av valen ej blivit företrätt i riksdagen, räknas beträffande sådant val i stället för mandat antalet hela tiondels procentenheter röster över 2,5 procent som partiet erhållit i valet i hela landet. Har parti i något av valen blivit företrätt i riksdagen men ej fått 4 procent av rösterna i hela landet, räknas beträffande sådant val dels antalet mandat, dels antalet hela tiondels procentenheter röster över 2,5 procent. Om det sammanlagda antalet mandat och tiondels procentenheter röster över 2,5 procent överstiger fjorton, räknas dock ej överskjutande tal." Lag (1995:345). |
| [PS-6] | 6 § "Parti som vid val till riksdagen fått minst 4 procent av rösterna i hela landet får för varje år för vilket valet gäller ett helt grundstöd. Helt grundstöd utgör 5 803 200 kronor." |
| [PS-7] | 7 § "Parti som blivit företrätt i riksdagen men ej fått 4 procent av rösterna i hela landet erhåller för varje år för vilket valet gäller så många fjortondelar av ett helt grundstöd som motsvarar antalet vunna mandat." |
| [PS-8] | 8 § "Parti som avses i 6 eller 7 § får utöver grundstödet tilläggsstöd för varje år för vilket valet gäller med 16 350 kronor för varje vunnet mandat, om partiet är företrätt i regeringen och annars 24 300 kronor för varje vunnet mandat." |
| [PS-9] | 9 § "Har parti vid val till riksdagen fått minst 4 procent av rösterna i hela landet och erhåller partiet en lägre procentandel än fyra vid närmast följande val, utgår avtrappat grundstöd för de fyra därpå följande åren, första året med 75 procent …" |

**The rule in the game (SP-3):** the state support a party draws is the act's - mandate contributions of 324 704 kr
on the two latest ordinary elections' seats (weighted by the year of the period), a party outside the Riksdag
counting whole tenth-points of its national vote above 2.5 % instead of seats, capped at fourteen; the basic support
of 5 803 200 kr at 4 % of the vote, fourteenths of it per seat below 4 %; the supplement per seat. **A created party
with no election behind it draws nothing** - the spec's *a party below the line starts and stays without it* is the
act's own reading. The figures are the page's consolidated amounts and carry its "t.o.m." stamp; the act's
indexation is not modelled (the amounts are statute, not index-linked).
