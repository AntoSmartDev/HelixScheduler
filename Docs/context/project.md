## Specifiche Progetto (docs/context/PROJECT.md)
PROJECT: HelixScheduler (open-source). Goal: a lightweight scheduling engine that computes free slots for one or more resources over a date range, applying:
- Positive rules (availability) and negative events (busy/unavailability)
- Multi-resource rules/events (e.g., doctor + room)
- Resource hierarchy (N parents possible; e.g., Room belongs to Site; Doctor can belong to multiple Sites)
- Resource properties (tree/subtree) used for filtering (e.g., Specialization -> Surgery -> ...). Property selection supports includeDescendants true/false.

CORE CONCEPTS:
- Resource: generic schedulable entity. Currently schedulable: Personale (doctor) and Stanza (room). Site is also a Resource (top-level in hierarchy).
- Rule: recurring or single availability/unavailability; can attach to 1..N resources.
- BusyEvent: domain event normalized (appointment/meeting/etc.) always modeled as an unavailability time range; can attach to 1..N resources.
- Availability semantics: final availability = (union of positive rules) minus (union of busy/unavailability). Default query semantics is Intersection: slots where ALL requested resources are free concurrently.
- Important behavior: if a multi-resource rule blocks both resources, and a busy event blocks only one resource, the other resource must become free again for that time range (i.e., availability is per-resource, then intersection).
- Timezone: store and compute in UTC. Precision: TimeSpan minute precision, range operations need split/trimming.

API GOALS:
- GET /api/availability/slots?from=YYYY-MM-DD&to=YYYY-MM-DD&resources=...&propertyFilters=...&includeDescendants=...
- Returns slots (UTC), with optional “explain” later (not required now).

IMPLEMENTATION CONSTRAINTS:
- Use EF Core (latest, .NET 10 target), SQL Server.
- Keep engine simple: generate occurrences within requested window; optimize later. Avoid over-engineering.
- Architecture: Clean-ish split: Domain/Core (engine abstractions + value objects), Infrastructure (EF/repositories), WebApi (controllers).
- DB-first is NOT required, but EF migrations must be included and schema kept minimal.

DO NOT:
- Couple engine to medical domain classes; medical is only sample data.
- Implement capacity now (phase 2); just design extensible spots.

# Documento di Visione (v1)

1. Obiettivo del progetto
1.1 Visione
HelixScheduler è un motore di scheduling agnostico dal dominio che calcola disponibilità (slot) su una o più risorse, applicando:
•	regole ricorrenti e puntuali
•	eventi positivi e negativi
•	gerarchie tra risorse
•	filtri per proprietà
•	(fase 2) capacità/concorrenza e vincoli avanzati
L’obiettivo non è “fare un calendario”, ma fornire un core deterministico e testabile, che domini diversi possano usare per:
•	sanità (medici/stanza/macchinari)
•	prenotazioni (staff/desk/room)
•	manufacturing (macchine/operatori/linee)
•	servizi (consulenti/aule/strumenti)
•	logistica (mezzi/magazzini/piazzole)
________________________________________
1.2 Requisiti chiave (non negoziabili)
R1 — Multi-risorsa
Uno schedule o evento può coinvolgere N risorse (es. medico + stanza). L’incrocio deve essere nativo, non “a valle” con join manuali.
R2 — Disponibilità finale per risorsa = positivi − negativi
Il motore deve supportare il modello:
•	regole positive (aperture, disponibilità)
•	eventi/regole negative (assenze, chiusure, appuntamenti, blackout)
R3 — Eventi multi-risorsa devono sbloccare correttamente risorse non coinvolte
Caso fondamentale discusso:
•	X: “medico 7 + stanza 1 disponibili lun-mer 14–18” (positivo)
•	Y: “medico 7 indisponibile il 10/03” (negativo solo medico)
Risultato atteso:
•	il medico è indisponibile quel giorno
•	la stanza deve tornare disponibile (se non bloccata da altro)
Questo implica che il motore deve ragionare per occupazioni per risorsa e non trattare un blocco multi-risorsa come “oggetto monolitico”.
R4 — Gerarchia risorse (non speciale per la sede)
“Sede” non è un caso a parte: è una risorsa padre nella gerarchia. La gerarchia serve per:
•	delimitare la ricerca
•	applicare vincoli “contenitore”
•	ottimizzare calcolo e query
R5 — Separazione tra motore e dominio
•	Le regole (scheduling rules) sono strutturali e relativamente poche.
•	Gli eventi di dominio (appuntamenti, riunioni, prenotazioni, ecc.) sono molti e vengono forniti al motore come input “normalizzato” (busy slots), per evitare che la tabella regole esploda.
R6 — Performance e leggibilità
•	target: decine/centinaia risorse
•	range tipico: giorni/settimane, max ~3 mesi
•	CPU/memoria da centellinare
•	evitare algoritmi “magici”: preferire pipeline semplice e ottimizzabile
R7 — Timezone
•	tutto in UTC; conversioni UI/dominio fuori dal motore.
________________________________________
1.3 Cosa produce il motore
Il motore produce:
•	slot di disponibilità normalizzati (start/end) per risorsa o per combinazione di risorse richieste
•	pronti per essere mostrati in UI (FullCalendar / DevExpress Scheduler / ecc.)
Non produce:
•	prenotazioni persistite
•	regole di business del dominio (es. “per visita X serve specializzazione Y” → quello è filtro proprietà/domino)
________________________________________
1.4 Filosofia progettuale
•	Modello minimo ma aperto: pochi concetti forti, estensibili (risorsa, regola, busy slot, proprietà).
•	Evitare eccesso di astrazione: niente “framework” prematuro; modularità sì, ma “piccoli mattoni” chiari.
•	Compatibilità non richiesta: possiamo ripartire da progetto nuovo, portando solo ciò che è buono.
________________________________________
2. Concetti fondamentali
2.1 Risorsa (Resource)
Una risorsa è qualunque entità che può essere:
•	schedulata (ha regole di disponibilità)
•	occupata (da eventi/busy slots)
•	filtrata (per proprietà)
Esempi:
•	Sede, Stanza, Medico (fase 1)
•	Macchinario come proprietà (fase 1) → eventualmente schedulabile (fase 2/3)
Una risorsa deve avere un identificatore stabile (ResourceId), indipendente dal dominio.
________________________________________
2.2 Gerarchia risorse (DAG: multi-parent)
Le risorse sono collegate da relazioni padre→figlio, con vincoli:
•	una risorsa può avere N padri
•	una risorsa può avere N figli
•	no cicli (DAG)
Perché serve:
•	query tipo “in questa sede”
•	vincoli di contenitore (“stanza dentro sede”)
•	ottimizzazione (riduci set risorse candidate)
Esempio:
•	Medico 7 può lavorare in Sede A e Sede B (multi-parent)
•	Stanza appartiene di norma ad una sede (ma il modello non lo impone)
________________________________________
2.3 Risorsa schedulabile vs proprietà
Concetto deciso:
•	Schedulabili oggi: PERSONALE, STANZA (e SEDE come contenitore con regole di apertura)
•	Proprietà oggi: macchinari, specializzazioni, caratteristiche stanza…
Nota: una proprietà può evolvere in risorsa schedulabile se in futuro serve “occupazione” nel tempo.
________________________________________
2.4 Proprietà di risorsa (ResourceProperty) ad albero
Le proprietà sono un catalogo gerarchico (parentId) per abilitare filtri UX e semantica “subtree”.
Esempi:
•	Specializzazione Medici → Chirurgia → Chirurgia X → Chirurgia XY
•	Caratteristiche Stanza → Attrezzature → OCT → modello X
Le risorse hanno una relazione many-to-many con le proprietà (ResourcePropertyAssignment).
________________________________________
2.5 Filtro proprietà: Subtree + includeDescendants
Regola confermata:
•	filtro per ID (mai LIKE)
•	supporto a:
o	includeDescendants = false: solo selectedId
o	includeDescendants = true: selectedId + tutti i discendenti a profondità arbitraria
Formalmente:
•	PropertyId IN Closure(selectedId)
Questo è fondamentale per UI “a selezioni progressive”.
________________________________________
2.6 Regole di scheduling vs eventi di dominio
Scheduling Rules
•	poche, stabili
•	descrivono pattern (settimanali, mensili, ogni N giorni, singoli, range-exclude…)
•	possono essere positive/negative
Domain Events
•	molti, dinamici
•	rappresentano occupazioni concrete (appuntamento, riunione, prenotazione, blackout)
•	arrivano al motore già come busy slots (negativi) multi-risorsa
Questo risponde alla tua esigenza:
“la maggior parte dei record deve stare negli eventi di dominio, non nelle regole”.
________________________________________
2.7 Multi-risorsa: cosa significa davvero
Una regola o un evento può riferirsi a N risorse.
Esempio:
•	regola positiva: “medico 7 + stanza 1 disponibili lun/mer 15–19”
•	evento negativo: “appuntamento 10/03 16–17 occupa medico 7 e stanza 1”
•	evento negativo: “assenza 10/03 occupa solo medico 7 (stanza libera)”
Questo richiede che l’algoritmo lavori su:
•	occupazioni per risorsa
•	intersezioni tra disponibilità risorse richieste
•	sottrazioni correttamente per “scope” (solo le risorse incluse nell’evento)



3. Modello di Scheduling
Questa sezione definisce come HelixScheduler rappresenta il tempo, le regole e le indisponibilità, indipendentemente dal dominio.
________________________________________
3.1 Regole di Scheduling (Scheduling Rules)
Una Scheduling Rule rappresenta una regola strutturale di disponibilità o indisponibilità, tipicamente stabile nel tempo.
Caratteristiche:
•	è ricorrente o puntuale
•	è positiva (disponibilità) o negativa (indisponibilità)
•	è associata a una o più risorse
•	non rappresenta un evento “reale” del dominio
Esempi:
•	“Medico 7 lavora lun-mer 14–18”
•	“Stanza 1 disponibile 8–20”
•	“Chiusura sede 1–3 aprile (tutto il giorno)”
•	“Medico 8 in ferie ogni agosto”
Le regole sono poche e non crescono in modo proporzionale all’uso del sistema.
________________________________________
3.2 Tipi di regole supportate (concettuali)
HelixScheduler non è legato a un set rigido di tipi, ma il minimo funzionale include:
•	SingleRule
→ valida in una data specifica
•	WeeklyRule
→ valida in giorni della settimana, con range temporale
•	MonthlyRule
→ valida in giorni/mese (es. 15 di ogni mese)
•	RepeatingRule (ogni N giorni)
•	RangeExcludeRule
→ blocco continuo su un periodo
Tutte condividono:
•	intervallo orario (TimeFrom, TimeTo)
•	semantica positiva / negativa
•	associazione multi-risorsa
________________________________________
3.3 Eventi di Dominio (Busy Slots)
Gli Eventi di Dominio rappresentano occupazioni reali, generate dal sistema applicativo:
•	appuntamenti
•	prenotazioni
•	riunioni
•	interventi
•	blackout operativi
Caratteristiche:
•	sempre negativi (rendono indisponibile qualcosa)
•	molti
•	volatili
•	forniti al motore come input
Esempio:
•	“Appuntamento 10/03 16–17 occupa medico 7 e stanza 1”
•	“Riunione staff 12/03 9–11 occupa medico 7”
Il motore non deve conoscere il dominio:
•	riceve solo una lista di BusySlot
•	ogni BusySlot specifica:
o	start / end (UTC)
o	risorse coinvolte
________________________________________
3.4 Perché separare Regole ed Eventi
Decisione architetturale fondamentale:
Scheduling Rules	Domain Events
poche	molte
stabili	dinamici
strutturali	operativi
persistite dal motore	generate dal dominio
ricorrenti	puntuali
Benefici:
•	tabella scheduler non esplode
•	il motore resta prevedibile
•	i domini restano indipendenti
•	è possibile integrare più domini sullo stesso motore
________________________________________
3.5 Multi-risorsa: modello concettuale
Una regola o un evento non è legato a una sola risorsa.
Concetto chiave:
una regola/evento definisce un insieme di occupazioni, una per risorsa coinvolta
Questo evita il problema discusso:
un evento che blocca medico + stanza
non deve “bloccare” implicitamente altre risorse
Ogni occupazione è valutata per risorsa, poi intersecata.
________________________________________
3.6 Semantica positiva / negativa
Per ogni risorsa R:
Disponibilità finale(R) =
  (Unione di tutte le regole positive su R)
  − (Unione di tutte le regole negative su R)
  − (BusySlots che coinvolgono R)
Solo dopo questa fase si fa l’intersezione tra risorse richieste.
________________________________________
4. Modello di Query e Semantica di Ricerca
Questa sezione definisce come si interroga il motore e cosa significa una query.
________________________________________
4.1 Query di disponibilità – concetto base
Una query chiede:
“Dammi gli slot temporali in cui tutte queste condizioni sono soddisfatte”
La query è composta da:
1.	Periodo di ricerca (from, to)
2.	Risorse richieste
3.	Filtri per proprietà
4.	Opzioni di calcolo
________________________________________
4.2 Risorse richieste (Required Resources)
La query può richiedere:
•	una singola risorsa
→ “slot in cui è libero il medico 7”
•	più risorse
→ “slot in cui sono liberi medico 7 e stanza 1”
Default semantico:
Intersection (AND logico)
Uno slot è valido solo se:
•	esiste contemporaneamente per tutte le risorse richieste
________________________________________
4.3 Risorse implicite tramite gerarchia
Se una risorsa è figlia di un’altra, la gerarchia può influenzare il calcolo.
Esempio:
•	query su medico 7
•	il medico lavora in stanza 1
•	stanza 1 appartiene a sede A
Opzione di query:
•	includere o meno i padri gerarchici come vincoli
Questo permette query come:
•	“slot medico 7 nella sede A”
•	“slot medico 7 ovunque”
________________________________________
4.4 Filtri per proprietà
Le proprietà:
•	non generano slot
•	filtrano le risorse candidate
Esempio:
“slot con medico specializzato in Oculistica”
Pipeline:
1.	selezione proprietà (ID)
2.	espansione subtree (se richiesto)
3.	selezione risorse compatibili
4.	calcolo disponibilità solo su quelle risorse
________________________________________
4.5 Query complesse supportate (esempi)
•	“slot in cui sono liberi medico 7 e stanza 1”
•	“slot in cui è libero un medico oculista in una stanza con macchinario X”
•	“slot in cui esiste almeno una stanza compatibile con il medico”
•	“slot in cui medico 7 è libero e esiste una stanza libera con proprietà Y”
Queste query sono risolvibili perché:
•	risorse, proprietà e gerarchie sono concetti separati
•	il motore lavora per intersezioni progressive
________________________________________
4.6 Politica di generazione slot
Decisione confermata:
•	generare le occorrenze solo nel periodo richiesto
•	prima applicare indisponibilità
•	poi disponibilità
•	poi intersezione tra risorse
Nessun pre-calcolo globale, nessuna cache prematura.
________________________________________

5. Pipeline di Calcolo della Disponibilità
Questa sezione descrive come HelixScheduler calcola gli slot disponibili in modo deterministico, leggibile e ottimizzabile.
L’obiettivo non è “essere furbi”, ma:
•	essere corretti
•	essere prevedibili
•	essere facili da estendere
•	evitare ricalcoli inutili
________________________________________
5.1 Visione d’insieme della pipeline
Per una query di disponibilità, la pipeline è sempre:
1.	Normalizzazione input
2.	Selezione risorse candidate
3.	Caricamento regole scheduler
4.	Caricamento eventi di dominio (busy slots)
5.	Generazione timeline per risorsa
6.	Applicazione indisponibilità
7.	Intersezione tra risorse
8.	Produzione slot finali
Ogni step ha responsabilità univoca.
________________________________________
5.2 Step 1 – Normalizzazione input
La query viene normalizzata in una struttura interna:
•	Period (UTC, inclusivo)
•	elenco risorse richieste
•	filtri proprietà (con opzione includeDescendants)
•	modalità di intersezione (default: AND)
•	opzioni future (capacità, priorità, ecc.)
Tutto ciò che segue lavora solo su questa struttura.
________________________________________
5.3 Step 2 – Selezione risorse candidate
Prima di calcolare qualsiasi slot:
1.	si filtrano le risorse compatibili tramite proprietà
2.	si applica l’eventuale espansione gerarchica
3.	si ottiene l’insieme minimo di risorse da considerare
Questo step è cruciale per le performance:
non si genera disponibilità per risorse che verranno scartate dopo.
________________________________________
5.4 Step 3 – Caricamento regole scheduler
Per ogni risorsa candidata:
•	si caricano solo le regole che:
o	intersecano il periodo richiesto
o	sono associate alla risorsa (direttamente o indirettamente)
Le regole vengono caricate una sola volta per query.
________________________________________
5.5 Step 4 – Caricamento eventi di dominio (Busy Slots)
Il dominio applicativo fornisce al motore:
•	una lista di BusySlot
•	già normalizzati in UTC
•	già risolti a livello di risorse coinvolte
HelixScheduler non sa cosa sia un appuntamento o una riunione:
•	sa solo che qualcosa occupa una risorsa in un intervallo
Questo garantisce:
•	disaccoppiamento totale
•	riuso del motore in domini diversi
________________________________________
5.6 Step 5 – Generazione timeline per risorsa
Per ogni risorsa R:
1.	si genera la timeline delle regole positive
2.	la timeline è limitata al periodo richiesto
3.	la precisione è basata su TimeRange (no slot predefiniti)
Output:
una lista di intervalli temporali “potenzialmente disponibili” per R
________________________________________
5.7 Step 6 – Applicazione indisponibilità
Sempre per risorsa:
1.	si applicano le regole negative
2.	si applicano i BusySlot
3.	le sottrazioni sono:
o	contenute
o	parziali
o	totali
Il risultato è:
disponibilità finale per singola risorsa
Nota importante (punto chiave discusso):
•	un evento che occupa medico + stanza
o	rende indisponibile il medico
o	rende indisponibile la stanza
•	ma non elimina la disponibilità della stanza per altri medici
Questo è possibile perché:
•	ogni risorsa è trattata indipendentemente
•	l’intersezione avviene dopo
________________________________________
5.8 Step 7 – Intersezione tra risorse
A questo punto esistono N timeline indipendenti:
•	timeline(medico 7)
•	timeline(stanza 1)
•	timeline(macchinario X) → se schedulabile in futuro
L’intersezione:
•	avviene solo sugli intervalli temporali
•	mantiene gli slot in cui tutte le risorse sono disponibili
Semantica:
slot valido ⇔
  slot ∈ R1 ∧ slot ∈ R2 ∧ ... ∧ slot ∈ Rn
________________________________________
5.9 Step 8 – Produzione output
Il risultato finale è una lista di slot:
•	ordinati
•	normalizzati
•	pronti per UI (FullCalendar, DevExpress, ecc.)
•	privi di riferimenti al dominio
________________________________________
6. Modello di Estensione e Roadmap Evolutiva
Questa sezione chiarisce cosa non è ancora implementato, ma è stato volutamente previsto.
________________________________________
6.1 Capacità delle risorse (Fase 2)
Concetto:
•	ogni risorsa può avere una Capacity (default = 1)
•	un evento occupa 1 unità di capacità
•	slot valido se:
•	occupazioni concorrenti < capacity
Esempio:
•	stanza con capacità 2
•	due appuntamenti in parallelo → ok
•	terzo → no
Nota:
•	la capacità non cambia la pipeline
•	cambia solo la valutazione delle indisponibilità
________________________________________
6.2 Priorità e override (Fase futura)
Possibile estensione:
•	regole con priorità
•	eventi che sovrascrivono altri eventi
Per ora non implementato intenzionalmente:
•	aumenterebbe complessità
•	non richiesto dai casi d’uso attuali
________________________________________
6.3 Semantiche alternative (OR, ANY)
Oggi:
•	default = AND (intersection)
Estendibile a:
•	OR tra gruppi di risorse
•	“almeno una stanza compatibile”
Questo può essere modellato:
•	a livello di query
•	senza modificare il core del motore
________________________________________
6.4 Timezone
Decisione ferma:
•	tutto il core lavora in UTC
•	conversioni solo a livello UI / API
________________________________________
6.5 Perché non un algoritmo “più furbo”
Scelta consapevole:
•	evitare algoritmi ultra-complessi
•	privilegiare:
o	leggibilità
o	verificabilità
o	facilità di debug
o	estendibilità reale
L’ottimizzazione viene dopo, guidata da metriche reali.

7. Modello Dati Concettuale (Scheduler Core)
Questa sezione descrive il modello concettuale minimo del motore, indipendente da qualsiasi dominio applicativo.
________________________________________
7.1 Resource
La Resource è l’unità fondamentale dello scheduler.
Caratteristiche:
•	rappresenta qualunque entità schedulabile
•	è agnostica dal dominio
•	può avere più padri
•	può avere proprietà
Concettualmente:
Resource
- Id
- Code / ExternalKey
- IsSchedulable
- Capacity (future)
Esempi di risorse:
•	Medico
•	Stanza
•	Sede
•	(future) Macchinario schedulabile
________________________________________
7.2 Gerarchia delle risorse
Le risorse possono essere collegate tramite relazioni many-to-many padre/figlio:
Sede
 ├─ Stanza 1
 │   └─ Medico 7
 └─ Stanza 2
Oppure:
Medico 7
 ├─ Sede A
 └─ Sede B
Caratteristiche chiave:
•	nessun vincolo rigido di “albero puro”
•	la gerarchia è informativa e funzionale al calcolo
•	usata per:
o	propagazione di indisponibilità
o	ottimizzazione delle query
o	comprensione del contesto
________________________________________
7.3 ResourceProperty (albero di proprietà)
Le proprietà sono metadati filtrabili, non schedulabili (di default).
Struttura:
ResourceProperty
- Id
- ParentId (nullable)
- Code
- Description
Esempi:
Specializzazione
 ├─ Chirurgia
 │   ├─ Chirurgia X
 │   └─ Chirurgia Y
 └─ Pediatria
Caratteristiche Stanza
 ├─ Con Macchinario X
 └─ Con Balcone
Associazione:
Resource ↔ ResourceProperty (many-to-many)
Supporto query:
•	PropertyId = X
•	includeDescendants = true
•	filtro: IN (X + tutti i discendenti)
⚠️ Nessun LIKE, nessuna stringa
→ solo match su ID (performance + chiarezza)
________________________________________
7.4 ScheduleRule (regole di disponibilità)
Le regole definiscono quando una risorsa è disponibile o indisponibile.
Concetti chiave:
•	sempre legate ad almeno una risorsa
•	positive o negative
•	ripetitive o puntuali
Astrazione:
ScheduleRule
- Id
- ResourceId
- Type (Positive | Negative)
- Period / Recurrence
- TimeRange
________________________________________
7.5 BusySlot (eventi di dominio)
I BusySlot rappresentano occupazioni reali.
Caratteristiche:
•	prodotti dal dominio applicativo
•	multi-risorsa
•	sempre negativi
•	temporanei
BusySlot
- From (UTC)
- To (UTC)
- ResourceIds[]
Esempi:
•	appuntamento medico
•	riunione
•	intervento
•	evento esterno
HelixScheduler non persiste i BusySlot:
•	li riceve
•	li applica
•	li dimentica
________________________________________
8. Esempio Completo – Dominio Medico
Questa sezione serve come caso d’uso di riferimento, non come vincolo.
________________________________________
8.1 Setup dominio
Risorse:
•	Sede A
•	Stanza 1 (figlia di Sede A)
•	Medico 7 (lavora in Stanza 1)
Proprietà:
•	Medico 7 → Specializzazione → Oculistica
•	Stanza 1 → Macchinario → OCT
________________________________________
8.2 Regole scheduler
•	Sede A: lun–ven 08–20
•	Stanza 1: lun–ven 09–18
•	Medico 7: lun–mer 14–18
________________________________________
8.3 Eventi di dominio
•	Appuntamento:
o	10/03 15–16
o	risorse: Medico 7 + Stanza 1
•	Permesso medico:
o	10/03 intera giornata
o	risorsa: Medico 7
________________________________________
8.4 Query supportate
✔️ “Slot in cui sono liberi Medico 7 e Stanza 1”
✔️ “Slot per visita oculistica con macchinario OCT”
✔️ “Slot in Sede A con medico specializzato”
✔️ “Slot dove la stanza è libera anche se il medico è occupato”
Tutte risolvibili senza logica custom nel core.
________________________________________
9. Linee Guida Open Source
HelixScheduler nasce per essere:
•	adottabile
•	estendibile
•	non invasivo
________________________________________
9.1 Cosa è (e cosa non è)
È:
•	un motore di scheduling
•	indipendente dal dominio
•	orientato alla composizione
Non è:
•	un gestionale
•	un calendario UI
•	un ORM di eventi
________________________________________
9.2 Filosofia del progetto
•	semplicità > furbizia
•	chiarezza > micro-ottimizzazioni premature
•	estendere senza rompere
•	dominio separato dal motore
________________________________________
9.3 Integrazioni previste
Esempi:
•	ASP.NET MVC + FullCalendar
•	Blazor WASM + DevExpress Scheduler
•	API-first per frontend custom
________________________________________
9.4 Evoluzione controllata
Ogni nuova feature deve:
•	rispettare il modello concettuale
•	non introdurre coupling
•	essere opzionale

