# Sweden - the Tidö agreement (Tidöavtalet: Överenskommelse för Sverige, 14 October 2022) as a list of items

The support agreement between the three governing parties (Moderaterna, Kristdemokraterna, Liberalerna) and
Sverigedemokraterna - "Samarbetsparti utanför regeringen" - read from the PDF a signatory published, stored byte for
byte since 2026-09-24 under `raw/records/liberalerna_tidoavtalet-overenskommelse-for-sverige-slutlig.pdf` (473575
bytes, sha256 `5a08f97d6d36d60527cde23c8e8ddb5fc2c1d2c77dd42786c7eaede9abb14ae2`); re-fetched 2026-09-25 with
`curl -sL -A "Mozilla/5.0"`, HTTP 200, byte-identical (server `last-modified: Fri, 14 Oct 2022 07:07:05 GMT`). The
government's own page named for this task, `https://www.regeringen.se/overenskommelser-och-avtal/2022/10/tidoavtalet-overenskommelse-for-sverige/`,
answers 404 ("Sidan kan inte hittas"), has no Wayback capture, and no regeringen.se `contentassets` PDF whose name holds
"tidoavtalet" exists in the CDX index apart from later bokslut/presentation files - so Liberalerna's copy is the primary
text and Moderaterna's copy (`raw/tido/moderaterna_Tidoavtalet-Overenskommelse-for-Sverige.pdf`, 354978 bytes, sha256
`06ca4955ec10ae91e0ec16d2c658147ee8fb3d355cde3165ff5c47f6ed7bd885`, server `last-modified: Fri, 14 Oct 2022 08:55:13 GMT`,
PDF `/Title` "SLUTLIG Överenskommelse för Sverige", Ghostscript 9.50 from Acrobat Pro DC) is the cross-check.
`raw/tido/SHA256SUMS` lists all three files (the 404 page included); `raw/tido/fetch_log.txt` the fetches.

**How the text was read (no PDF renderer on this machine):** every object of the PDF parsed, object streams included;
the page tree (`/Pages` → `/Kids`) walked so pages come in the document's order (63 PDF pages; the cover is unnumbered,
PDF page n prints page number n−1); each page's `/Contents` inflated with perl `Compress::Zlib`; literal strings
decoded as WinAnsi; the CID-hex runs of the Identity-H fonts (the 14 pt section titles, the bullets `•`, the en dashes
and the typographic quotes) decoded through each font's own ToUnicode CMap. Line breaks fall only on vertical moves of
the text matrix, so words are not split. Every quote below is verbatim (whitespace collapsed, the page's own line breaks
inside a sentence joined; one hyphen-at-line-end joined where the page breaks a word); the English after each is my
translation, not the page's. Nothing from memory. Cross-check against Moderaterna's copy: the literal-string text agrees
sentence for sentence; that copy's CID fonts carry no ToUnicode maps, so its headings, bullets and dashes could not be
decoded and the comparison is of the body text only (GAP G3).

**Counting rule (mine, stated so the counts can be reproduced):** an *item* is a reform heading under each project
plan's section "3. Reformer som ska genomföras i projektet" - the bold heading font of that section (font F3 12 pt in
Hälso- och sjukvård, Migration och integration and Andra samarbetsfrågor; font F10 11.04 pt in Klimat och energi,
Kriminalitet and Skolan, where the F3 lines are group titles instead). In Tillväxt och hushållsekonomi section 3 has no
reform headings, only four "bör inriktas mot:" groups with bullets, so there the bullets are the items. Sub-points
(`•` bullets, `1)`/`2)` steps) under an item are counted separately as sub-points, not as items. Two edge cases are
declared in the table's notes. The heading font of every run was read from the content stream, not guessed.

## The agreement's identity, verbatim

**[TA-COVER]** PDF page 1 (unnumbered cover): "Tidöavtalet: Överenskommelse för Sverige"
*Translation (mine):* The Tidö agreement: Agreement for Sweden. (The cover draws the words in two text objects,
"Överenskommelse för Sverige" and "Tidöavtalet:", the latter positioned above; both are the cover's only text.)

**[TA-TOC]** printed page 1 (PDF 2), the table of contents - a heading drawn in a font whose map the page does not set
(the glyph ids read Innehållsförteckning; GAP G4), then: "Överenskommelse för Sverige … 2 Direktiv samarbetsprojekt
Hälso- och sjukvården … 5 Direktiv samarbetsprojekt Klimat och energi … 11 Direktiv samarbetsprojekt Kriminalitet … 18
Direktiv samarbetsprojekt Migration och integration … 29 Direktiv samarbetsprojekt Skolan … 48 Direktiv samarbetsprojekt
Tillväxt och hushållsekonomi … 55 Direktiv andra samarbetsfrågor … 60" (the TOC draws the hyphen of "Hälso- och" as a
text object of its own between two line breaks; joined here as the page prints it)
*Translation (mine):* Agreement for Sweden 2; Directive, collaboration project Health care 5; Climate and energy 11;
Crime 18; Migration and integration 29; The school 48; Growth and household economy 55; Directive, other collaboration
questions 60. The seven areas, as the text names them, are therefore: **Hälso- och sjukvården, Klimat och energi,
Kriminalitet, Migration och integration, Skolan, Tillväxt och hushållsekonomi, andra samarbetsfrågor** (the running text
on page 2 lists six "samarbetsprojekt" plus "ett samarbetsprojekt med andra samarbetsfrågor", [TA-SAM3]).

**[TA-PARTIES]** printed page 2 (PDF 3), heading "Samarbete": "Samarbetspartierna Sverigedemokraterna, Moderaterna,
Kristdemokraterna och Liberalerna är överens om att ta ansvar för Sverige i ett gemensamt samarbete under mandatperioden
2022–2026. Samarbetspartier i regeringen är Moderaterna, Kristdemokraterna och Liberalerna. Samarbetsparti utanför
regeringen är Sverigedemokraterna."
*Translation (mine):* The cooperation parties the Sweden Democrats, the Moderates, the Christian Democrats and the
Liberals agree to take responsibility for Sweden in a joint cooperation during the 2022–2026 term. Cooperation parties
in the government are the Moderates, the Christian Democrats and the Liberals. Cooperation party outside the government
is the Sweden Democrats. - This is the only place the text names the parties as parties; **the PDF carries no signature
block, no leaders' names and no date line** (grep for Kristersson/Åkesson/Busch/Pehrson/"oktober"/"underteckn": no hit;
GAP G2). The date 14 October 2022 rests on the two PDFs' server and metadata stamps (above and in `records_by_date.md`
[TIDO]) and on the talman's statement of that day ([RD-PK14] there).

## The areas and their items

| area (as the text names it) | items (count) | sub-points | printed pages (PDF pages) |
|---|---|---|---|
| Direktiv samarbetsprojekt Hälso- och sjukvården | **24** = 23 headed items + 1 headless paragraph (note a) | 5 bullets (under "Jämställd vård …") | 5–10 (PDF 6–11); section 3 on 6–10 |
| Direktiv samarbetsprojekt Klimat och energi | **17** in two groups: Energi 13, Klimat 4 | 10 bullets | 11–17 (PDF 12–18); section 3 on 12–17 |
| Direktiv samarbetsprojekt Kriminalitet | **48** in four groups: "Mönsterbrytande åtgärder för att stoppa gängen" 10, "Krafttag mot ungdomsbrottsligheten" 7, "En fullständig och genomgripande översyn av strafflagstiftningen genomförs" 11, "Övriga reformer" 20 (note b) | 0 beyond the five Kriminalvården sub-headings counted as items | 18–28 (PDF 19–29); section 3 on 19–28 |
| Direktiv samarbetsprojekt Migration och integration | **33** | 94 bullets and 17 numbered steps (`1)`, `2)`, `1.`, `2.`) | 29–47 (PDF 30–48); section 3 on 31–47 |
| Direktiv samarbetsprojekt Skolan | **30** in five groups: "Kunskapsresultat och kunskapsinnehåll" 8, "Trygghet och arbetsro" 5, "Friskolor och valfrihet" 2, "Läraryrket" 5, "Likvärdig skola med kvalitet i hela landet" 10 | 6 bullets (under "Ny friskolelag") | 48–54 (PDF 49–55); section 3 on 50–54 |
| Direktiv samarbetsprojekt Tillväxt och hushållsekonomi | **26** bullets in four groups: "Företagande och produktivitet" 8, "Arbetsutbud" 7, "Effektivare jobbpolitik" 4, "Hushållsekonomi" 7 | - (the bullets are the items) | 55–59 (PDF 56–60); section 3 on 57–59 |
| Direktiv andra samarbetsfrågor | **13** | 0 | 60–62 (PDF 61–63); section 3 on 61–62 |
| **total** | **191** | | |

Notes. (a) The first reform paragraph of the health plan (printed page 6, "En utredning tillsätts med uppdrag att
analysera … statligt huvudmannaskap …") stands directly under "3. Reformer som ska genomföras i projektet" with no
heading of its own in the PDF; it is counted as one item. (b) "Kriminalvården" (printed page 25) is a heading over five
bulleted sub-headings ("Kriminalvården expanderas kraftigt" …); the five are counted as items, the group line is not.
Front matter (printed pages 2–4): six headed passages - Samarbete, Budgetsamarbete, Beredning, samordning och
samordningskanslier, Principer för extern kommunikation, Utnämningar och tillsättningar, EU-frågor i riksdagen - quoted
under "How it binds" below. Each project plan repeats a skeleton (1. Inledning; 1.1 Syfte och mål; 1.2 Avgränsningar;
1.3 Metod och modell; 2. Ägarskap och arbetsformer; 2.1 Kommunikation; 3. Reformer som ska genomföras i projektet).

### Hälso- och sjukvården (printed 5–10) - the first items verbatim

**[TA-H0]** p. 6, the headless first item: "En utredning tillsätts med uppdrag att analysera och belysa för och nackdelar
samt lämna förslag på möjligheterna att långsiktigt införa ett delvis eller helt statligt huvudmannaskap Utredningen ska
även beakta för- och nackdelar med regionala organisationer geografiskt baserade på exempelvis dagens sex
sjukvårdsregioner. …"
*Translation (mine):* An inquiry is appointed to analyse the pros and cons of, and propose how, a partly or wholly state
principalship of health care could be introduced in the long run; it shall also weigh regional organisations based on
e.g. today's six health-care regions. (The page has no full stop after "huvudmannaskap".)

**[TA-H1]** p. 6 "Ökad styrning och uppföljning av statliga medel - Nationella principer för ersättning och avgifter
införs. Dessa ska bygga på behovsprincipen och att en medicinsk bedömning görs så att patienten kommer till rätt del av
vården. Staten tar ett helhetsansvar över styrning och uppföljning av statliga medel till sjukvården. Utgångspunkten ska
vara att statliga medel som huvudregel är prestationsbaserade med tydlig uppföljning."
*Translation (mine):* Increased steering and follow-up of state funds - national principles for reimbursement and fees,
built on the needs principle and a medical assessment; the state takes overall responsibility for steering and
following up state funds to health care, as a rule performance-based.

**[TA-H2]** p. 7 "Nationell plan och styrning av kompetensförsörjningen - Behovsläget nu och framgent av medicinskt
utbildad vårdpersonal kartläggs nationellt. …"
*Translation (mine):* A national plan and steering of staffing: the present and future need of medically trained staff
is mapped nationally.

**[TA-H3]** p. 7 "Stärkt uppföljning av vårdens effektivitet och kvalitetsredovisning - IVO eller Myndigheten för vård-
och omsorgsanalys bör ges befogenhet att även granska regioners och kommuners verksamhet avseende medicinska resultat.
…"
*Translation (mine):* Stronger follow-up of efficiency and quality reporting: IVO or the Health and Care Analysis agency
should be empowered to audit regions' and municipalities' medical results.

**[TA-H4]** p. 7 "Inrätta en nationell vårdförmedling i statlig regi för att kapa köer inom hälso- och sjukvården -
Ledig kapacitet redovisas och möjliggör för patienter, som fått en medicinsk bedömning och väntar på vård, att välja att
få behandlingen eller operationen utförd på annat håll i landet om man så vill. …"
*Translation (mine):* A national, state-run care broker to cut queues: free capacity is published so that patients with
a medical assessment who are waiting may choose treatment elsewhere in the country.

**[TA-H5]** p. 7 "Nationell långsiktig plan för att eliminera bristen på vårdplatser ska tas fram - Utred och ta fram
en nationell plan för hur bristen på vårdplatser ska kunna åtgärdas. …"
*Translation (mine):* A national long-term plan to eliminate the shortage of hospital beds.

**[TA-H6]** p. 7 "Ökad samverkan i fler regionala centra, likt dagens sex regionala cancercenter" and **[TA-H7]** p. 7
"Reformera den digitala infrastrukturen i vården - Genomför en enhetlig och gemensam digital infrastruktur för den
svenska sjukvården som ersätter och kompletterar de 21 regionernas befintliga infrastruktur. …"
*Translation (mine):* More regional centres like today's six cancer centres; one common digital infrastructure for
Swedish health care replacing and complementing the 21 regions' own.

The remaining 16 items, by heading (p. 8–10): Nationell förlossningsplan; Utbyggd primärvård; Rätt till en fast
läkarkontakt; Cancervården och barncancervården ska ytterligare utvecklas och förbättras; Tandvårdsreform; Jämställd vård
samt vård och forskning om kvinnors sjukdomar och hälsa (five bullets); Anhörigsatsning (p. 8); Utveckla sjukvård i
landsbygd och glesbygd; Utveckla apotekens roll i vårdkedjan; Gör sjukvården mer flexibel efter patientens behov och
önskemål; Läkarens roll i vårdkedjan tydliggörs; Psykisk hälsa och suicidprevention; Stärk beroendevården; Kommunala
läkare (p. 9); Språkkrav ska utredas för personal i äldreomsorgen; Personlig assistans (p. 10).

### Klimat och energi (printed 11–17) - the first items verbatim

**[TA-K1]** p. 12, group "Energi": "Förutsättningar för investeringar i ny kärnkraft - Förutsättningarna för
investeringar i kärnkraft ska stärkas genom särskilda statliga kreditgarantier uppgående till 400 miljarder kronor, med
mer generösa villkor än dagens system. … Nya regler ska införas som förhindrar att politiken godtyckligt stänger ner
kärnkraftverk – kärnkraft ska garanteras rätten till drift och elproduktion så länge anläggningarna är i gott skick och
drivs på ett säkert sätt. Om staten tvingar fram en nedläggning ska ägare ha rätt till skadestånd."
*Translation (mine):* Conditions for investment in new nuclear power: special state credit guarantees of SEK 400
billion on more generous terms; new rules preventing politics from arbitrarily closing nuclear plants - a guaranteed
right to operate while in good condition and safely run; damages if the state forces a closure.

**[TA-K2]** p. 13 "Utred återstart av planerbar elproduktion i södra Sverige - En genomgående utredning av vad som
skulle krävas för återstart av Ringhals 1 och 2 bör genomföras förutsättningslöst och skyndsamt, samt vilken
systemnyttan skulle vara. …"
*Translation (mine):* Investigate restarting dispatchable generation in southern Sweden - what a restart of Ringhals 1
and 2 would require, without preconditions and promptly.

**[TA-K3]** p. 13 "Nytt energipolitiskt mål - Det energipolitiska målet ändras från 100 procent ”förnybart” till 100
procent ”fossilfritt”. Teknikneutraliteten återställs, där inget hållbart kraftslag diskrimineras i målformuleringen. …
Till det energipolitiska målet fogas också ett tydligt leveranssäkerhetsmål för elförsörjningen där systemoperatören,
idag Svenska kraftnät, pekas ut som ansvarig för måluppfyllelsen på lång och kort sikt. Planeringen för ökad
elanvändning bör utgå från ett nu prognosticerat elbehov på minst 300 terawattimmar 2045."
*Translation (mine):* New energy-policy target: from 100 per cent "renewable" to 100 per cent "fossil-free"; technology
neutrality restored; a security-of-supply target added with the system operator (today Svenska kraftnät) responsible;
planning from a forecast need of at least 300 TWh in 2045.

**[TA-K4]** p. 13 "Nya regler för elmarknaden - En ny utredning om elmarknadens utformning tillsätts med uppdrag att ta
fram förslag som syftar till att samtliga kraftslag ha likvärdiga spelregler samt en ordning där stödtjänster som krävs
för ett välfungerande elsystem prissätts … Angående slutkundsmarknaden bör anvisningsavtalen tas bort."
*Translation (mine):* New electricity-market rules: an inquiry for equal rules for all generation types and priced
ancillary services; default-supplier contracts abolished on the retail market.

**[TA-K5]** p. 13 "Styrning av myndigheter, statliga verk samt ny forskningsinriktning • Vattenfall bör omedelbart
påbörja planeringen av ny kärnkraft vid Ringhals och andra lämpliga platser. … • Svenska kraftnät får ett förtydligat
uppdrag att, genom att bland annat upphandla planerbar elproduktion, säkerställa driftsäkerheten i elsystemet. …"
*Translation (mine):* Steering of agencies and state enterprises: Vattenfall to start planning new nuclear at Ringhals
and elsewhere at once; Svenska kraftnät to secure operational reliability, including by procuring dispatchable
generation (five bullets in all, p. 13–14).

**[TA-K6]** p. 14 "Lagändringar för ny kärnkraft • Lagändringar för att möjliggöra ny kärnkraft. Förbuden i miljöbalken
att tillåta nya reaktorer på andra platser än i dag och ha fler än tio samtidigt i drift tas bort. Förbudet mot att
återstarta stängda reaktorer ska tas bort. …"
*Translation (mine):* Law changes for new nuclear: the Environmental Code's bans on new sites and on more than ten
reactors in operation removed; the ban on restarting closed reactors removed (three bullets).

The remaining 11 items, by heading: Bättre förutsättningar för kraftvärmen; Bättre förutsättningar för vattenkraften
(p. 14); Vindkraft; Solenergi; Energieffektivisering; Högkostnadsskydd, stöd till energibesparing och sänkta elpriser
(p. 15); Energimyndigheten (p. 16); group "Klimat": Översyn av stöd och styrmedel för ökad effektivitet (three bullets);
Laddinfrastrukturen byggs ut (p. 16); Miljötillståndsprocesserna förenklas och förkortas; Förslaget om CCS
(koldioxidinfångning) genomförs (p. 17).

### Kriminalitet (printed 18–28) - the first items verbatim

**[TA-C1]** p. 19, group "Mönsterbrytande åtgärder för att stoppa gängen": "Hemliga tvångsmedel - Utökade möjligheter
till preventiva tvångsmedel. Tilläggsdirektiv lämnas till utredningen om preventiva tvångsmedel (Ju 2021:15). … Förslagen
i SOU 2022:19 ska ligga till grund för lagstiftning och effekterna utvärderas i syfte att se om ytterligare skärpningar
är nödvändiga under mandatperioden."
*Translation (mine):* Covert coercive measures: wider preventive use, by supplementary terms to the Ju 2021:15 inquiry;
SOU 2022:19 to be legislated and evaluated.

**[TA-C2]** p. 19 "Utvisning av säkerhetshot - Utvisa fler gängkriminella. En möjlighet att kunna utvisa gängkriminella
som saknar svenskt medborgarskap utan att de dömts för brott ska utredas. …"
*Translation (mine):* Expulsion of security threats: investigate expelling gang criminals without Swedish citizenship
even without a conviction.

**[TA-C3]** p. 20 "Dubbla straff för gängkriminella - Skärp straffen kraftigt för gängkriminella för att låsa in de mest
brottsaktiva personerna. En särskild straffskärpningsgrund ska införas, liknande den som finns i Danmark, som medför
dubbla straff för brott som har samband med kriminella nätverk."
*Translation (mine):* Double sentences for gang crime, on the Danish model.

**[TA-C4]** p. 20 "Visitationszoner - Inför ett system med tidsbegränsade visitationszoner för att söka efter illegala
vapen och sprängmedel. Åklagare ska kunna besluta … Systemet ska utvärderas efter tre år."
*Translation (mine):* Time-limited stop-and-search zones decided by prosecutors, appealable to a court, evaluated after
three years.

**[TA-C5]** p. 20 "Anonyma vittnen - Ett system med anonyma vittnen ska införas. …"
*Translation (mine):* Anonymous witnesses to be introduced.

**[TA-C6]** p. 20 "Kriminalisering av deltagande i kriminella gäng - Det ska vara straffbart att delta i kriminella gäng.
Förslag till grundlagsändringar tas fram för att möjliggöra en kriminalisering av deltagande i, och samröre med,
kriminella organisationer. …"
*Translation (mine):* Participation in criminal gangs to be a crime; constitutional amendments prepared to allow it.

**[TA-C7]** p. 20 "Vistelseförbud - Inför möjlighet att döma ut vistelseförbud enligt dansk modell. …" and **[TA-C8]**
p. 20 "Ny huvudregel i sekretesslagstiftningen - Inför – i ett första led – en ny huvudregel i sekretesslagstiftningen:
all relevant information ska delas med brottsbekämpande myndigheter för att bekämpa brott. …"
*Translation (mine):* Residence bans on the Danish model; a new main rule in secrecy law that all relevant information
is shared with law-enforcement agencies.

The remaining 40 items, by heading: Obligatorisk häktning i fler fall; Förverkande (p. 21); group "Krafttag mot
ungdomsbrottsligheten": Ansvaret för unga som är grovt kriminella (p. 21); Straff för unga lagöverträdare; Ny påföljd för
unga; 24-timmarsgaranti i socialtjänsten; Lagen om vård av unga; Lagen om unga lagöverträdare; Föräldraansvar (p. 22);
group "En fullständig och genomgripande översyn av strafflagstiftningen genomförs": Skärpta straff för vålds- och
sexualbrott m.m.; Stärkt straffrättsligt skydd för poliser; Skärpta straff för gängrelaterad brottslighet;
Fängelsepresumtionen avskaffas; Billighetsskälen samt de försvårande och förmildrande omständigheterna i brottsbalken ses
över (p. 23); Dagens form av mängdrabatt avskaffas; Återfall ska straffas hårdare; En ny påföljd: Förvaringsdom;
Villkorlig frigivning; En stärkt nödvärnsrätt; Preskriptionsbestämmelserna ska revideras (p. 24); group "Övriga
reformer": En översyn av kamerabevakningslagen; Kriminalvården: Kriminalvården expanderas kraftigt, Överföring av
straffverkställighet, Kriminalisering av rymning, Rätten till permission inskränks, En utredning om att hyra
anstaltsplatser utomlands (p. 25); Provokativa åtgärder; Anonymitet i brottsbekämpningen; Skadestånd; Kontaktförbud;
Tullverket; Lag om särskild utlänningskontroll; Vapenlagstiftningen (p. 26); Ett nationellt tiggeriförbud utreds; Bekämpa
välfärdsbrottslighet; Pröva ett system med ungdomskriminalitetsnämnder och tillåt bevistalan mot unga i fler fall; Ny
socialtjänstlag och brottsförebyggande insatser för att bryta nyrekrytering; Ny socionomutbildning (p. 27); Breddad och
skärpt lagstiftning mot hedersrelaterat förtryck och hedersrelaterade maktstrukturer; Stöd och information till utsatta
(p. 28).

### Migration och integration (printed 29–47) - the first items verbatim

The plan's own framing, **[TA-M0]** p. 29 (1. Inledning): "Samtliga förslag som läggs fram inom ramen för projektet ska
vara i enlighet med de bindande internationella regler som Sverige åtagit sig att följa. Det innebär bland annat att
asylrätten kommer att upprätthållas." and its four goals (1.1): "Målet är att: • Skapa ett paradigmskifte i synen på
asylmottagande, för att utgångspunkten ska vara att skydd för den som flyr en konflikt eller kris ska erbjudas
tillfälligt och för den som flyr Sveriges närområde. Sverige ska inte i något avseende vara mer generöst i synen på asyl
än vad som följer som förpliktelser enligt EU-rätt eller andra juridiskt bindande internationella traktat. • Att
migrationspolitiken i övrigt ska vara ansvarsfull. • Att införa en kravbaserad integrationspolitik, där den som långvarigt
befinner sig i Sverige ska ta ansvar för att bli en del av det svenska. • Att komma till rätta med skuggsamhället."
*Translation (mine):* Every proposal shall comply with Sweden's binding international obligations, so the right of
asylum is upheld; the goals: a paradigm shift in asylum reception (protection temporary and for those fleeing Sweden's
neighbourhood; never more generous than EU law or binding treaties require), a responsible migration policy otherwise, a
demands-based integration policy, and dealing with the shadow society.

**[TA-M1]** p. 31 "Inre gränskontroller till Sverige ska stärkas 1) Polismyndigheten ska uppdras att genomföra inre
gränskontroller i högre grad där sådana gränskontroller bedöms ha effekt på irreguljär migration och gränsöverskridande
brottslighet. … 2) Förslag i SOU 2021:92 om effektivare åtgärder för att bl.a. utföra bevakning och inre
utlänningskontroller i gränsområden ska genomföras. 3) En departementspromemoria ska tas fram i syfte att se över hur
gränskontroller i högre utsträckning kan användas för att bekämpa irreguljär migration till Sverige. … Uppdrag till
Polismyndigheten ska fastställas i regleringsbrev för 2023. En proposition med förslag för att stärka gränskontroller och
transportöransvaret ska genomföras under 2023."
*Translation (mine):* Internal border controls strengthened: the Police to run more internal controls where they bite
on irregular migration; SOU 2021:92 implemented; a ministry memorandum on wider use of border controls (two bullets);
the Police's task set in the 2023 appropriation directions; a bill in 2023.

**[TA-M2]** p. 31 "Ökad användning av biometriska data i utlänningsärenden, avskaffande av preskription m.m. -
Tilläggsdirektiv ska beslutas till den pågående utredningen om åtgärder för att stärka återvändandeverksamheten (Ju
2022:12) för att ytterligare stärka användningen av biometri inom utlänningsrätten. …"
*Translation (mine):* More biometric data in alien cases and no limitation period for removal orders: supplementary
terms to the Ju 2022:12 inquiry (seven bullets, p. 31–32).

**[TA-M3]** p. 32 "Utökat arbete och förstärkta möjligheter till inre utlänningskontroller och effektivt
verkställighetsarbete 1) Satsningen ska ske med inriktningen att identifiera, omhänderta och säkerställa att personer som
befinner sig i Sverige utan tillstånd får lämna landet … 2) En breddad översyn ska göras av regelverket kring inre
utlänningskontroller …" including the bullet, p. 33: "• Lämna förslag till en ordning med informationsutbyte och
anmälningsplikt mellan Polisen och myndigheter som kan antas komma i kontakt med personer som befinner sig illegalt i
landet. Kommuner och myndigheter ska vara skyldiga att informera Migrationsverket och Polismyndigheten när de kommer i
kontakt med personer som vistas i Sverige utan tillstånd. … Undantag från informationsplikten behöver därför utredas
närmare."
*Translation (mine):* More internal alien controls and effective enforcement; a review of the rules, including a duty
for municipalities and agencies to report persons without a permit to the Migration Agency and the Police, with
exceptions (e.g. health care) to be examined.

**[TA-M4]** p. 33 "Samlat ansvar och intensifierat arbete för återvändandeverksamhet 1) Migrationsverket,
Polismyndigheten, Skatteverket och Kriminalvården ska ges ett förstärkt uppdrag att samverka när det gäller
återvändandeverksamheten … 2) … Antalet förvarsplatser ska öka …"
*Translation (mine):* Joint responsibility and intensified return work; more detention places; a residence obligation
for those refused but not detained.

**[TA-M5]** p. 34 "Asyllagstiftningen ska anpassas efter den rättsliga miniminivån enligt EU-rätten - Sveriges
lagstiftning för asylmottagande och anknytande regelverk och villkor ska anpassas för att inte vara mer generöst än vad
som är en skyldighet för en medlemsstat enligt EU-rätten. 1) Regeringen ska snarast efter regeringstillträdet besluta om
en proposition till riksdagen som motsvarar utskottsinitiativet i socialförsäkringsutskottet av den 3 maj 2021, för att
begränsa den humanitära skyddsgrunden. 2) En utredning ska genom en genomlysning av svensk rätt i förhållande till
EU-rätten ta fram förslag … Asylrelaterade uppehållstillstånd ska vara tidsbegränsade och institutet PUT ska utmönstras
till förmån för ett nytt system som utgår från berörd invandrares skyddsstatus. … Utredningen ska tillsättas våren 2023
och lämna delbetänkanden i lämplig ordning. Förslag om utmönstrande av permanenta uppehållstillstånd ska genomföras
senast våren 2024. Övriga förslag för att anpassa svensk asyllagstiftning till en rättslig miniminivå enligt EU-rätten
ska genomföras genom proposition till riksdagen senast våren 2026."
*Translation (mine):* Asylum law adjusted to the EU-law minimum: a bill at once matching the committee initiative of 3
May 2021 limiting the humanitarian ground; an inquiry (spring 2023) on every restriction EU law allows; asylum permits
temporary and permanent residence (PUT) phased out, by spring 2024; the rest by bill no later than spring 2026.

**[TA-M6]** p. 35 "Återinförd registrering för EES-medborgare", **[TA-M7]** p. 35 "Begränsning av vidarebosättning till
Sverige • Sverige ska under kommande mandatperioden ta emot 900 kvotflyktingar per år. …" and **[TA-M8]** p. 35 "Skärpta
villkor för arbetskraftsinvandring - … Utgångspunkten ska vara att arbetstillstånd endast ska beviljas om det arbete till
vilket arbetskraftsinvandring sker i normalfallet har en lönenivå motsvarande medianlönen. …"
*Translation (mine):* Registration of EEA citizens reintroduced; resettlement limited to 900 quota refugees a year;
labour immigration only at, as a rule, the median wage.

The remaining 25 items, by heading: Översyn av prövningen av asylansökan från säkra länder (p. 36); Transitcenter ska
införas för hela asylprocessen; Utred en möjlighet att utvisa en utländsk medborgares på grund av bristande vandel (p.
37); Asylsökandes ansvar för kostnaderna för mottagandet; Skärpta krav för medborgarskap (p. 38); Återkalla
uppehållstillstånd i fler fall; Skärpta kontroller av vandel i utlänningsärenden; Skärpta villkor för anhöriginvandring
(p. 39); Översyn av incitamentsstrukturer för frivillig återvandring; Folkräkning m.m. (p. 41); Åtgärder för minskade
tilldragningsfaktorer genom begränsade förmåner för icke-medborgare (with the sub-heading Aktuella förmåner, p. 42);
Förhöjd straffskala och regelskärpningar mot barnäktenskap, tvångsgifte, månggifte och fullmaktsäktenskap; Fler
utvisningar på grund av brott (p. 43); Villkorat bistånd och diplomatiska åtgärder för ökat återvändande; Begränsa rätten
till försörjningsstöd (p. 44); Utländsk finansiering av trossamfund och civila organisationer; Säkra bedömningar av
uppehållstillstånd p.g.a. studier; Rättssäkerhet på migrationsområdet; Integrationspolitikens målstruktur; Långsiktiga
strukturer för att lyfta utsatta områden (p. 45); Samhällsintroduktion m.m.; Bosättning för nyanlända; Sänkt
etableringsstöd (p. 46); Begränsning av rätten till tolk för personer med uppehållstillstånd och svenskt medborgarskap;
Ökade integrationsmöjligheter för de yngsta barnen (p. 47).

### Skolan (printed 48–54) - the first items verbatim

**[TA-S1]** p. 50, group "Kunskapsresultat och kunskapsinnehåll": "Ge skolans styrdokument tydligare
kunskapsinriktning - Skolans styrdokument (läroplaner, kursplaner och ämnesplaner) reformeras i enlighet med barns
kognitiva utveckling och får ökat fokus på inlärning, färdigheter samt fakta- och ämneskunskaper."
*Translation (mine):* Curricula and syllabi reformed in line with children's cognitive development, with more focus on
learning, skills and factual and subject knowledge.

**[TA-S2]** p. 50 "Utökad studietid - Skolans timplan utökas med fokus på svenska och matematik. …"
*Translation (mine):* More teaching time, focused on Swedish and mathematics.

**[TA-S3]** p. 50 "Betygssystem med kunskapsfokus - Författningsändringar och myndighetsuppdrag mot betygsinflation, till
exempel genom tydligare koppling mellan en skolas genomsnittliga resultat i nationella prov och genomsnittlig
betygsnivå. Betygssystemet ska även fortsättningsvis ha en gräns mellan godkänt och icke godkänt resultat."
*Translation (mine):* Grading against grade inflation, tying a school's average grades to its national-test results;
the pass/fail line kept.

**[TA-S4]** p. 50 "Förbättrad kunskapsuppföljning - Införandet påskyndas av digitala nationella prov som rättas centralt
och är jämförbara över tid. …"
*Translation (mine):* Centrally marked, comparable digital national tests brought forward; early checks and diagnostic
tests.

**[TA-S5]** p. 50 "Fler speciallärare och fler elever i särskilda undervisningsgrupper", **[TA-S6]** p. 50 "Utökad
studietid och lovskola samt obligatorisk läxhjälp för elever som behöver det", **[TA-S7]** p. 50 "Fler spetsklasser" and
**[TA-S8]** p. 50 "Läsning och läsförståelse av litterära texter ska få en större plats i skolan - Alla elever ska få
läsa både svenska och internationella skönlitterära klassiker. …"
*Translation (mine):* More special-needs teachers and special teaching groups; extra study time, holiday school and
compulsory homework help for those who need it; more advanced classes; reading of literary classics given more room,
with reading lists by an independent expert group.

The remaining 22 items, by heading: group "Trygghet och arbetsro": Stärk rektors ansvar och befogenheter; Stärk lärarens
befogenheter; Tydligare ordningsregler; Tryggare skolmiljöer och ökade insatser till särskilt våldsamma elever;
Anmälningsplikt (p. 51); group "Friskolor och valfrihet": Ny friskolelag (six bullets, p. 51–52); Obligatoriskt skolval
med bättre information och kortad kötid (p. 52); group "Läraryrket": Utveckla lärarutbildningen (p. 52); Minska
lärarnas administrativa börda; Avlasta lärarna genom övrig personal; Utveckla lärarrollen; Gör lärarrollen mer attraktiv
på de mest krävande skolorna (p. 53); group "Likvärdig skola med kvalitet i hela landet": Statligt ansvar ("Ny likvärdig
skolpeng …"); Tydligare reglering av kvaliteten i skolans verksamhet; Satsning på läroböcker och andra läromedel; Ökade
befogenheter för Skolinspektionen (p. 53); Förbättra elevhälsan; Skärpt granskning av skolor med konfessionell
inriktning; Snabbare integration och mer likvärdiga förutsättningar för nyanlända elever; Översyn av
modersmålsundervisningen; Insatser mot hedersrelaterat förtryck och kränkningar på grund av kön, sexuell läggning eller
könsidentitet; Förstärk kopplingen mellan studier på gymnasienivå och yrkeslivet (p. 54).

### Tillväxt och hushållsekonomi (printed 55–59) - the first items verbatim

The plan's framing, **[TA-T0]** p. 55: "Sverige är på väg in i en lågkonjunktur med betydande osäkerhet vad gäller djup,
längd och samhällseffekter. Den övergripande inriktningen för den ekonomiska politiken under mandatperioden ska vara att
motverka arbetslöshet och stärka Sveriges tillväxtförmåga. Fokus bör vara strukturellt och konjunkturellt motiverade
åtgärder. De reformförslag som tas fram inom ramen för arbetet kommer att behöva vägas mot konjunkturläge och utrymmet i
de offentliga finanserna."
*Translation (mine):* Sweden is entering a downturn; the overall line is to counter unemployment and strengthen growth
capacity; proposals must be weighed against the cycle and the fiscal room.

**[TA-T1]** p. 57, group "Företagande och produktivitet - Reformarbetet för företagande och produktivitet bör inriktas
mot: • Lägre administrativa kostnader för företagen. • Bolagsskatter och kapitalskatter ska vara konkurrenskraftiga och
främja fler nya företag som bidrar till jobb, tillväxt och välstånd. • Förbättra förutsättningarna för yrkesverksamma att
vidareutbilda sig vid högskolor och universitet. Stärk lärosätenas omställningsuppdrag. • Företagen bör ha goda
incitament att investera i forskning … • Omställningsstödet … • Bromma flygplats ska bevaras. Något beslut om att lägga
ned Bromma flygplats kommer inte fattas under mandatperioden. • Villkoren för små och medelstora företag att växa,
anställa och attrahera kapital ska förbättras, exempelvis genom förändringar i 3.12-regelverket. • Ägarskiften inom såväl
familjeföretag som till personal bör förenklas och underlättas …"
*Translation (mine):* Enterprise and productivity (8 items): lower administrative costs; competitive corporate and
capital taxes; further education for those in work; research incentives and staff options; the transition support used
for in-demand training; Bromma airport kept for the term; better terms for SMEs (the 3:12 rules); easier ownership
succession.

**[TA-T2]** p. 57–58, group "Arbetsutbud - Reformarbetet för ökat arbetsutbud bör inriktas mot: • Mer lönsamt att gå från
bidrag till arbete, utbilda sig och ta ökat ansvar på jobbet. … • Alla som uppbär försörjningsstöd av arbetsmarknadsskäl
ska delta i aktiviteter motsvarande sin arbetsförmåga för att få fullt bidrag. … • En stor bidragsreform genomförs som gör
det mer lönsamt att gå från bidrag till arbete och egen försörjning. Det ska ske dels genom lägre skatt på framför allt
låg- och medelinkomsttagare under mandatperioden. Dels genom införandet av ett bidragstak som gör att den som kan
arbeta, aldrig sammantaget kan få högre inkomster från bidrag än man skulle få genom eget arbete, samt genom införande
av ett tydligt och anpassat aktivitetskrav i hela Sverige för de som har försörjningsstöd …"
*Translation (mine):* Labour supply (7 items): work must pay more than benefits; activity requirements for social
assistance; a large benefits reform - lower tax on low and middle incomes during the term, a benefits cap, a nationwide
activity requirement; then sickness insurance, work-family flexibility, older workers, the municipal equalisation
system.

**[TA-T3]** p. 58, group "Effektivare jobbpolitik - … • Arbetsgivare som anställer långtidsarbetslösa bör mötas av lägre
anställningskostnader. • Bryta passiv långtidsarbetslöshet genom tydliga krav … • Den arbetsmarknadspolitiska
insatsfloran bör förenklas … • Ansvarsfördelningen mellan staten och kommunerna för långtidsarbetslösa bör utredas."
*Translation (mine):* A more effective jobs policy (4 items): cheaper hiring of the long-term unemployed; clear demands;
fewer programmes; the state/municipal split reviewed.

**[TA-T4]** p. 58–59, group "Hushållsekonomi - Åtgärderna riktade till hushållen bör ha följande inriktning: • Sänkt skatt
på arbetsinkomster och pensionsinkomst. • Ta fram förslag på hur pensionärernas ekonomi kan stärkas och valfriheten
värnas. • Lägre drivmedelspriser. • Sänkt skatt på sparande genom att en grundnivå på 300 000 kr i ISK görs skattefri. •
Åtgärder för att skydda hushållen mot framförallt elprischocker. • Reformer som bidrar till breddat ägande … • Åtgärder
för att minska barns utsatthet ska tas fram, bland annat genom att värna barns fritidsaktiviteter genom införandet av
ett fritidskort."
*Translation (mine):* Household economy (7 items): lower tax on earned and pension income; pensioners' finances; lower
fuel prices; a tax-free ISK floor of SEK 300 000; protection against electricity-price shocks; broader ownership; a
leisure card for children.

### Andra samarbetsfrågor (printed 60–62) - the first items verbatim

**[TA-A1]** p. 61 "Public service - Mediernas frihet ska värnas, och mångfalden av olika medier ska främjas. Public
service-mediernas oberoende ska bestå och dess långsiktiga finansiering vidmakthållas. … Detta ska vara inriktningen
inför den kommande tillståndsperioden 2026–2033."
*Translation (mine):* Public service: media freedom and plurality; public-service independence and long-term funding
kept; the line for the 2026–2033 licence period.

**[TA-A2]** p. 61 "Rättighetsskydd för abort - … En ny grundlagsutredning ska bl.a. utreda hur aborträtten kan stärkas.
Förslag tas fram om hur kvinnans rätt till abort kan ges skydd i regeringsformen."
*Translation (mine):* A new constitutional inquiry on protecting the right to abortion in the Instrument of Government.

**[TA-A3]** p. 61 "Kulturpolitiken ska värna kulturlivets oberoende och egenvärde gentemot nyttoändamål - Principen om
armslängds avstånd ska upprätthållas." **[TA-A4]** p. 61 "Fler långa stipendier för kulturskapare bör inrättas".
*Translation (mine):* Cultural policy at arm's length; more long grants for artists.

**[TA-A5]** p. 61 "Tjänstemannaansvar ska införas - Tillsätt en särskild utredare med uppdraget att lägga fram förslag
för hur ett återinfört tjänstemannaansvar skulle kunna se ut …"
*Translation (mine):* Reintroduced official liability (tjänstemannaansvar) to be investigated.

**[TA-A6]** p. 61 "Vallagen analyseras - Tillsätt en parlamentarisk kommitté med uppdraget att se över dagens svenska
valsystem med partispecifika valsedlar och dess påverkan på demokratin avseende exempelvis valdeltagande och
rättssäkerhet, med beaktande av Valmyndighetens tidigare synpunkter. Utredningen bör också ha i uppdrag att analysera
för- och nackdelarna med ett system med gemensamma valsedlar …"
*Translation (mine):* A parliamentary committee on the party-specific ballot papers and on a common ballot paper.

**[TA-A7]** p. 61 "Det nya medielandskapet - En studie ska göras av hur det nya medielandskapet påverkar
förutsättningarna för enskildas opinionsbildning. …"
*Translation (mine):* A study of the new media landscape and dominant social platforms.

The remaining 6 items, by heading (p. 62): Ett projekt ska genomföras för att stärka högstaligornas konkurrenskraft i den
svenska elitfotbollen och elithockeyn; En svensk kulturkanon ska tas fram; Djurvälfärdsutredning; Översyn av arvsrätten;
Allmänna arvsfondens inriktning; Översyn av folkbildningsrådet.

## How it binds - the agreement's own words

**[TA-SAM1]** p. 2 "Samarbetet ska lägga grunden för en långsiktigt hållbar samverkan, med syftet att genomföra
reformer som löser de stora samhällsproblem Sverige har på områden såsom bland annat kriminalitet, migration,
integration, ekonomi, skola, hälso- och sjukvård, energi och klimat. Samarbetet ska ske under villkor som tillgodoser
alla ingående parters intressen."
*Translation (mine):* The cooperation lays the ground for a long-term sustainable collaboration to carry out reforms in
crime, migration, integration, the economy, school, health care, energy and climate, on terms that satisfy every party's
interests.

**[TA-SAM2]** p. 2 - SD's role: "Alla frågor som samarbetet avser genomförs i gemensamma samarbetsprojekt mellan de
partier som ingår i överenskommelsen. Samarbetsparti som inte sitter i regeringen har fullt och lika inflytande över
frågor i samarbetsprojekten på samma sätt som partierna i regeringen. Det innebär medverkan fullt ut i
beredningsprocesserna avseende exempelvis utredningsdirektiv, propositioner till riksdagen, förordningsändringar som
följer av ny lagstiftning, EU-ärenden som påverkar de frågor som samarbetsprojektet omfattar, samt i förekommande fall
också uppdrag, utredare eller regleringsbrev till myndigheter i samarbetsprojekten. Samarbetsprojekten bedrivs med fasta
former för avstämning, information och beslut. I ärenden som faller inom ramen för samarbetsprojekten ska formell
samordning i Regeringskansliet ske på samma sätt mellan alla samarbetspartier. Samarbetet innebär också att de ingående
partierna inte ensidigt samverkar med andra partier i riksdagen i de frågor som direktiven till samarbetsprojekten
omfattar."
*Translation (mine):* Everything the cooperation covers is done in joint projects between the parties; the party
outside the government has full and equal influence in the projects, i.e. full participation in the preparation of
inquiry terms, bills, ordinances, EU matters and agency instructions; formal coordination in the Government Offices on
the same footing for all four; and NO party cooperates unilaterally with other parties in the Riksdag on the questions
the project directives cover.

**[TA-SAM3]** p. 2 "Under första årets samarbete, fram till och med budgetpropositionen 2024, kommer samarbetsprojekten
vara Tillväxt och hushållsekonomi, Kriminalitet, Migration och integration, Klimat och energi, Hälso- och sjukvård samt
Skola. Utöver detta finns ett samarbetsprojekt med andra samarbetsfrågor. Varje samarbetsprojekt har ett direktiv med
specificerat syfte, uppdrag och innehåll. Efter budgetpropositionens avlämnande kommer partiledarna besluta om
påföljande budgetårs samarbetsprojekt, som antingen är nya eller en fortsättning på föregående års samarbetsprojekt."
And: "Ansvarig för respektive samarbetsprojekt är statsråd och statssekreterare för respektive sakområde som projektet i
huvudsak avser. Projekten handläggs av tjänstemän i Regeringskansliet. I varje projekt deltar vanligtvis
statssekreteraren från de berörda departementen, en politisk tjänsteman från vartdera samarbetsparti samt politiska
företrädare och tjänstemän från berörda departement."
*Translation (mine):* For the first year, up to and including the budget bill for 2024, the projects are the six named
plus "other cooperation questions"; each has a directive; after each budget bill the party leaders decide the next
budget year's projects (new or continued). Each project is owned by the responsible minister and state secretary, run
by Government Offices officials, with one political official from each cooperation party.

**[TA-BUD1]** p. 2, heading "Budgetsamarbete": "Samarbetspartierna ingår en budgetöverenskommelse för mandatperioden
2022–2026. Budgetöverenskommelsen bygger på förutsättningen att budgetramverket ligger fast och att budgeten beslutas i
sin helhet. Budgeten förhandlas mellan samarbetspartierna i Regeringskansliet innan riksdagsbehandlingen påbörjas."
*Translation (mine):* The four parties enter a budget agreement for 2022–2026, on the premise that the fiscal framework
stands and that the budget is decided AS A WHOLE; the budget is negotiated between the four in the Government Offices
before the Riksdag's handling begins.

**[TA-BUD2]** p. 3 (continuing): "Utgångspunkten för en budgetöverenskommelse är samarbetsprojekten samt förhandling om
budget i sin helhet enligt gängse beredningsrutiner. Samarbetspartierna förbinder sig att rösta på regeringens budget så
att budgeten i sin helhet röstas igenom i riksdagen. Åtagandet är således att regeringen ingår en budgetöverenskommelse
med en ordnad process för budget och viktiga propositioner där inflytandet är specificerat för samarbetspartierna."
*Translation (mine):* The starting point is the projects plus negotiation of the whole budget by the usual routines;
THE PARTIES BIND THEMSELVES TO VOTE FOR THE GOVERNMENT'S BUDGET so that the budget as a whole passes the Riksdag; the
undertaking is that the government enters a budget agreement with an orderly process for the budget and important bills
in which the cooperation parties' influence is specified. (The word is "förbinder sig"; the text holds no sanction for
breach - GAP G5.)

**[TA-BER1]** p. 3, heading "Beredning, samordning och samordningskanslier": "I samarbetsprojekten har samarbetspartierna
fullt och lika inflytande över frågor som projekten omfattar. Samarbetspartierna avgör kollektivt de frågor som
samarbetet omfattar efter samordning. Samarbetsparti som inte ingår i regeringen har därmed samma kapacitet i
beredningen som regeringspartierna i samarbetsprojekten, det vill säga insyn och påverkan i samordningen och
förhandlingarna i de områden som projekten avser. Samarbetspartierna upprättar samordningskanslier på
Statsrådsberedningen vilka utgör plattform för förhandling och beredning mellan partierna och gentemot
Regeringskansliet. Syftet är att kunna förhandla politiska frågor med fullt och lika inflytande i samarbetsprojekten
mellan partier som sitter i regeringen och inte, samt bygga en ordning och kapacitet för systematisk politisk samordning
och förhandling i flera politiska frågor som pågår samtidigt. Regeringspartierna åtar sig att löpande hålla
samarbetsparti utanför regeringen underrättat i större politiska frågor."
*Translation (mine):* The parties decide the covered questions COLLECTIVELY after coordination; the party outside the
government has the same capacity in preparation as the governing parties; the parties set up coordination offices
(samordningskanslier) at the Prime Minister's Office (Statsrådsberedningen) as the platform for negotiation between the
parties and towards the Government Offices; the governing parties undertake to keep the outside party informed on major
political questions.

**[TA-BER2]** p. 3 - what happens on disagreement: "Samordningskanslierna bör vara organiserade för förhandlingar på
flera nivåer. Handläggare eller politiskt sakkunniga ansvarar och har mandat att förhandla samarbetsområdena. Nästa nivå
är statssekreterare, eller motsvarande chefsnivå, för att kunna hissa frågor för avdömning som inte löses på
handläggarnivå. De fåtal frågor, som inte lösts på statssekreterarnivå, avgörs på partiledarnivå. Ett inre kabinett
inrättas bestående av partiledare från de fyra samarbetspartierna."
*Translation (mine):* Negotiation at several levels: desk officers/political advisers with a mandate; then state
secretaries (or equivalent) to whom unresolved questions are lifted for adjudication; the few questions unresolved there
are decided at party-leader level; an INNER CABINET of the four party leaders is set up. The project plans add the same
ladder from the project side, **[TA-PROJ-HISS]** p. 6 (repeated in every plan): "Om frågor inte löses i projektet,
hissas frågan till samordningskanslierna för beredning." *(mine:* if a question is not resolved in the project it is
lifted to the coordination offices). The text provides no rule for a disagreement that survives the party leaders
(GAP G5).

**[TA-KOM1]** p. 3, heading "Principer för extern kommunikation": "Alla partier som ingår i samarbetet ska ha synlighet
och kunna vara avsändare vid beslut om reformer. I de frågor som samarbetet omfattar – samarbetsprojekten samt
budgetöverenskommelsen – finns endast en samordnad planering för extern kommunikation. I de frågor som samarbetet inte
omfattar, där inte gemensamma beslut fattas, finns ingen gemensam kommunikation."
*Translation (mine):* All four are visible and may announce reforms; within the cooperation's scope - the projects and
the budget agreement - there is only one coordinated communication plan; outside it, none.

**[TA-KOM2]** p. 3 "Samarbetspartierna bidrar till ett gott samarbetsklimat genom att uppträda med värdighet och tala
respektfullt om varandras centrala företrädare. Eventuell offentlig diskussion utifrån meningsskiljaktigheter i
sakfrågor mellan samarbetspartierna ska emellertid bejakas, som en naturlig del av den öppna, demokratiska
samhällsdebatten i ett fritt samhälle."
*Translation (mine):* Dignity and respect towards each other's leading representatives; public disagreement on
substance is nevertheless welcomed as a natural part of open debate.

**[TA-UTN]** p. 3–4, heading "Utnämningar och tillsättningar": "Samarbetspartierna har fullt och lika inflytande över
frågor i samarbetsprojekten, däribland beredningsprocesser av utredningsdirektiv samt förslag till utredare eller
motsvarande. Regeringen kommer under mandatperioden pröva företrädare eller motsvarande från samarbetspartierna med
exakt samma krav utifrån meriter som alla andra sökanden till poster under regeringens utnämningsmakt."
*Translation (mine):* Full and equal influence over inquiry terms and the choice of inquiry chairs; the government will
assess the cooperation parties' people for appointments by exactly the same merit requirements as any other applicant.

**[TA-EU]** p. 4, heading "EU-frågor i riksdagen": "I EU-ärenden ska samarbetsparti utanför regeringen informeras om
innehållet i regeringens faktapromemoria inför beredningen av EU-ärenden i riksdagen. Informationen ska ges före
behandlingen i riksdagens utskott, vilken föregår beredningen i EU-nämnden. Ansvaret för att lämna förnyad information
till samarbetsparti utanför regeringen samt söka majoritet i riksdagen åligger respektive statssekreterare."
*Translation (mine):* On EU matters the outside party is informed of the government's fact memorandum before committee
handling and the EU Affairs Committee; each state secretary is responsible for informing it and for seeking a Riksdag
majority.

**[TA-PROJ-AVGR]** p. 5 (1.2 Avgränsningar, repeated in every project plan) - the projects do not pre-empt the budget:
"Samtliga förslag i projektet med påverkan på budgetpropositionen ska behandlas i ordinarie budgetprocess där utgifter
måste vägas mot varandra. Det finns ingen garanti för att frågor som bereds i projektet automatiskt är garanterade
finansiering i budgetpropositionen. Den förhandlingen sker separat från samarbetsprojektet i budgetprocessen." And (1.
Inledning, p. 5): "… I ärenden som faller inom ramen för samarbetsprojekten ska formell samordning i Regeringskansliet
ske på samma sätt mellan alla samarbetspartier. Det blir garanten för fullt och lika inflytande mellan
samarbetspartierna." And (1.2, p. 5): "Avgränsningen av områden som behandlas i projektet är de frågor som tas upp under
avsnitt 3. Inga ytterligare frågor kommer att beredas i projektet."
*Translation (mine):* Every proposal with budget effect goes through the ordinary budget process, where outlays are
weighed against each other; nothing prepared in a project is guaranteed financing - that negotiation is separate, in the
budget process. Formal coordination on the same footing "is the guarantee of full and equal influence". Each project is
limited to the questions listed under its section 3; nothing else is prepared in it.

**[TA-PROJ-OWN]** p. 6 (2. Ägarskap och arbetsformer, repeated): "Samarbetsprojekten arbetar på uppdrag av partiledarna.
Projektet leds av en arbetsgrupp som återrapporterar till partiledarna på regelbunden basis. … Projektgruppen ses i
samarbetsprojekten med hög regelbundenhet för informationsutbyte, förhandlingar och inriktning till beslut. Projektet
bör ha utbyte på veckobasis. I normalfallet startar projekten upp i slutet av året, efter partiledarnas beslut. Efter
årsskiftet pågår projektarbetet i hög takt fram till budgetarbetet tar vid."
*Translation (mine):* The projects work on the party leaders' commission and report back regularly; weekly exchange;
normally started at year's end after the leaders' decision, running at pace until the budget work takes over.

**The rule as data (for the political-system spec):** four parties, three in cabinet and one (SD) outside with "fullt
och lika inflytande" inside seven named projects and the budget [TA-SAM2] [TA-BER1]; a budget agreement for the whole
term under which all four "förbinder sig att rösta på regeringens budget så att budgeten i sin helhet röstas igenom"
[TA-BUD2] - i.e. the frame vote of `budget_procedure.md` step 6 is bound for the four; no party deals unilaterally with
other parties on the projects' questions [TA-SAM2]; disagreement climbs officials → state secretaries → the four party
leaders' inner cabinet [TA-BER2], with no rule beyond that; projects are re-decided by the leaders after each budget bill
[TA-SAM3]; project items carry no financing guarantee - money is settled only in the budget negotiation [TA-PROJ-AVGR];
public disagreement on substance is allowed [TA-KOM2].

## Register of ids

| id | source | publisher | date | file | bytes | sha256 |
|---|---|---|---|---|---|---|
| [TA-COVER], [TA-TOC], [TA-PARTIES], [TA-SAM1]–[TA-SAM3], [TA-BUD1], [TA-BUD2], [TA-BER1], [TA-BER2], [TA-KOM1], [TA-KOM2], [TA-UTN], [TA-EU], [TA-PROJ-AVGR], [TA-PROJ-HISS], [TA-PROJ-OWN], [TA-H0]–[TA-H7], [TA-K1]–[TA-K6], [TA-C1]–[TA-C8], [TA-M0]–[TA-M8], [TA-S1]–[TA-S8], [TA-T0]–[TA-T4], [TA-A1]–[TA-A7] | https://www.liberalerna.se/wp-content/uploads/tidoavtalet-overenskommelse-for-sverige-slutlig.pdf | Liberalerna (a signatory's copy) - the same file as `records_by_date.md` [TIDO] | PDF CreationDate 2022-10-14 06:04 and 08:20 +02:00; server last-modified 2022-10-14 07:07:05 GMT; 63 PDF pages (printed 1–62) | `raw/records/liberalerna_tidoavtalet-overenskommelse-for-sverige-slutlig.pdf` (listed again in `raw/tido/SHA256SUMS` by relative path) | 473575 | 5a08f97d6d36d60527cde23c8e8ddb5fc2c1d2c77dd42786c7eaede9abb14ae2 |
| (cross-check only, no id quoted) | https://moderaterna.se/app/uploads/2022/10/Tidoavtalet-Overenskommelse-for-Sverige.pdf | Moderaterna (a signatory's copy) | PDF CreationDate 2022-10-14 08:55:11 Z; server last-modified 2022-10-14 08:55:13 GMT; Title "SLUTLIG Överenskommelse för Sverige"; 63 pages | `raw/tido/moderaterna_Tidoavtalet-Overenskommelse-for-Sverige.pdf` | 354978 | 06ca4955ec10ae91e0ec16d2c658147ee8fb3d355cde3165ff5c47f6ed7bd885 |
| (evidence of the 404) | https://www.regeringen.se/overenskommelser-och-avtal/2022/10/tidoavtalet-overenskommelse-for-sverige/ | Regeringskansliet | fetched 2026-09-25, HTTP 404 "Sidan kan inte hittas" | `raw/tido/regeringen_overenskommelser-och-avtal_2022-10_tidoavtalet_404.html` | 191283 | bec11b14ffb177ee8d1b74cfe7794d60af7bf6ec0ca4bddc228b9522df960beb |

Page references in this file are the document's printed page numbers (PDF page = printed + 1). The extraction script and
its outputs stayed in the session scratchpad (`pdfpages3.pl`, `tido3.txt`); nothing under `Assets/` was touched.

## GAPS

- **G1 - no copy on regeringen.se.** The government's page for the agreement is 404, uncaptured by Wayback, and the CDX
  index shows no regeringen.se PDF of the agreement itself (only the 2026 bokslut, the 2023 climate "tilläggsöverenskommelse"
  slides and a Kriminalvården uppdrag). The text stands on two signatories' copies, which agree in their body text (G3).
  The government's own reading of the agreement is on the bokslut page already stored ([RG-BOK] in `records_by_date.md`),
  not re-read here.
- **G2 - no signature block, names or date in the text.** The PDF names the four parties only as "Samarbetspartierna
  Sverigedemokraterna, Moderaterna, Kristdemokraterna och Liberalerna" [TA-PARTIES]; 14 October 2022 rests on the file
  stamps and on the talman's statement that day ([RD-PK14] in `records_by_date.md`), as `records_by_date.md` G3 already
  bills.
- **G3 - the Moderaterna cross-check is partial.** Its CID fonts (headings, bullets, dashes, typographic quotes) carry no
  ToUnicode maps, so only its literal-string body text was compared: sentence for sentence identical after whitespace
  normalisation, every difference being a run the M copy's extraction could not decode. The headings and the sentence
  "Det energipolitiska målet ändras från 100 procent ”förnybart” till 100 procent ”fossilfritt”." are therefore verified
  in the Liberalerna copy only.
- **G4 - the TOC heading** on printed page 1 is drawn in a font the page's content stream never selects, so its glyphs
  were not mapped; read as glyph ids they spell Innehållsförteckning. No quote depends on it.
- **G5 - no clause on breach, exit or a deadlock above the leaders.** "förbinder sig att rösta på regeringens budget"
  [TA-BUD2] and the escalation ladder to the inner cabinet [TA-BER2] are the whole of it; the text says nothing about what
  follows if the four leaders disagree, if a party votes against the budget, or how the agreement ends. Anything a game
  rule adds there is AUTHORED, not sourced.
- **G6 - later instruments not fetched:** the 2023 "tilläggsöverenskommelse" on climate (slides on regeringen.se,
  2023-11-14 in the CDX), any later renegotiated project directives (the text itself says the leaders re-decide projects
  yearly, [TA-SAM3]), and the 2026 bokslut PDF.
- **G7 - the first health item has no heading** in the PDF (note a); counted as an item on the text's structure, not on a
  heading.
- **G8 - item counts are my reading of the typography** (rule stated at the top); another reader counting bullets as
  items, or groups as items, would get other totals. The per-area heading lists above let any count be redone.
