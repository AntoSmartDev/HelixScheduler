# HelixScheduler

**Deterministic scheduling engine for real-world resource planning on modern .NET**

HelixScheduler nasce per essere il **cuore logico dei sistemi di pianificazione**: il punto in cui la disponibilità viene definita una sola volta e calcolata correttamente, anche in presenza di regole complesse, gerarchie organizzative, capacità concorrenti e vincoli strutturali.

Visualizzare un calendario è semplice. Calcolare correttamente cosa può essere mostrato non lo è.

---

# Cos’è HelixScheduler

HelixScheduler è un **motore fondazionale deterministico** progettato per:

- modellare la disponibilità delle risorse
- applicare regole ricorrenti ed eccezioni
- considerare indisponibilità e occupazioni reali
- combinare più risorse tra loro
- restituire slot temporali realmente utilizzabili

Il suo obiettivo è risolvere in modo coerente e spiegabile la parte più critica dei sistemi di prenotazione e pianificazione:

> calcolare correttamente quando una combinazione di risorse è effettivamente disponibile.

---

## Determinismo

A parità di input — regole, indisponibilità, occupazioni, proprietà e opzioni di calcolo — il risultato è sempre identico.

Il motore non contiene logiche euristiche, randomness o comportamenti impliciti. Il calcolo è completamente deterministico e verificabile.

Questo rende HelixScheduler adatto a:

- ambienti regolamentati
- sistemi critici
- contesti in cui la riproducibilità è essenziale

---

# Perché nasce HelixScheduler

Nei sistemi reali la disponibilità dipende da molte entità:

- persone
- spazi
- strumenti
- strutture
- vincoli organizzativi
- proprietà e qualifiche

Implementare queste logiche direttamente su eventi di calendario porta rapidamente a:

- duplicazioni
- incoerenze
- conflitti difficili da individuare
- modelli non scalabili

HelixScheduler separa la **logica di disponibilità** dall’interfaccia utente e dalla gestione delle prenotazioni.

Il motore calcola. L’applicazione decide come utilizzare il risultato.

---

## Scalabilità del modello

HelixScheduler non materializza preventivamente eventi nel tempo.

Non genera calendari futuri né espande le regole in liste di eventi persistiti.

Il modello si basa su:

- poche regole strutturali
- intervalli normalizzati
- occupazioni dinamiche

Il numero di regole rimane tipicamente limitato e stabile nel tempo, anche in presenza di molte prenotazioni.

Il calcolo avviene su richiesta, su un intervallo temporale esplicito.

Questo evita:

- esplosioni di dati
- generazione preventiva di milioni di record
- sincronizzazioni complesse tra eventi duplicati

Il risultato è un sistema:

- più leggero
- più coerente
- più prevedibile

---

# Concetti fondamentali

## Risorse

Tutto è una risorsa:

- medico
- stanza
- strumento
- sede
- reparto
- macchina
- team

Le risorse possono essere organizzate in relazioni gerarchiche:

- Sede -> Stanza
- Reparto -> Medico
- Struttura -> Ambulatorio

È possibile richiedere che il calcolo tenga conto anche della disponibilità degli antenati (`includeResourceAncestors`).

Questo consente di modellare correttamente:

- chiusura sede
- blocchi organizzativi
- vincoli strutturali

Senza duplicare indisponibilità sulle risorse figlie.

---

## Regole

Le regole definiscono la **disponibilità strutturale**.

Esempi:

- lun–ven 09:00–18:00
- solo martedì
- ogni primo lunedì del mese
- fino a una certa data
- senza fine

La ricorrenza è opzionale: è una funzionalità, non un requisito.

---

## Indisponibilità

Bloccano completamente una risorsa.

Esempi:

- ferie
- chiusura sede
- manutenzione

Un’indisponibilità equivale a capacity = 0 per quell’intervallo.

---

## Occupazioni

In un sistema reale, le occupazioni derivano dalle prenotazioni del dominio e possono essere proiettate/inviate al motore come input normalizzato al momento del calcolo.

Rappresentano l’uso reale della risorsa.

Una prenotazione confermata genera un’occupazione.

Le occupazioni:

- consumano capacity
- possono coesistere se capacity > 1
- impediscono sovrapposizioni non valide

Un’occupazione può coinvolgere più risorse contemporaneamente.

Ad esempio, una visita che richiede medico e stanza genera un’occupazione coerente su entrambe le risorse, garantendo che il vincolo sia rispettato in modo atomico nel calcolo della disponibilità.

---

## Capacity

Ogni risorsa può avere una capacità.

Esempi:

- medico -> 1
- ecografo mobile -> 1
- laboratorio -> 3
- aula -> 20

La disponibilità viene calcolata considerando:

Disponibilità effettiva =
Disponibilità da regole
- indisponibilità
- occupazioni (fino al raggiungimento della capacity)

---

## Proprietà

Le risorse possono avere proprietà organizzate in categorie e gerarchie.

Esempio:

Diagnostica  
 -> Ecografia  
 -> RX  
 -> TAC

Con `includeDescendants` è possibile filtrare per una categoria e includere automaticamente tutte le specializzazioni.

---

# Problemi reali risolti

## Visita con medico + stanza

Per una visita servono:

- un medico
- una stanza

Lo slot esiste solo se entrambi sono disponibili nello stesso intervallo (AND).

---

## Strumento mobile condiviso

Per una visita serve anche un ecografo mobile.

- capacity = 1
- condiviso tra ambulatori

Lo slot esiste solo se:

medico AND stanza AND ecografo sono disponibili.

Nessuna doppia prenotazione.

---

## Più stanze equivalenti

Esistono più ambulatori equivalenti.

È sufficiente che almeno una stanza sia disponibile.

Questo viene modellato come OR tra risorse alternative.

---

## Sede chiusa per ferie

Una stanza appartiene a una sede.

Se la sede chiude:

- non serve inserire indisponibilità su ogni stanza
- basta inserirla sulla sede

Con `includeResourceAncestors` il vincolo si propaga automaticamente.

---

## Filtrare per caratteristiche della sede

Una visita richiede:

- stanza idonea
- sede accreditata
- sede appartenente a una certa area

Le proprietà sono sulla sede, non sulla stanza.

Il motore può selezionare solo risorse il cui contesto organizzativo soddisfa i vincoli, evitando duplicazioni.

---

# Interrogare la disponibilità

La disponibilità si richiede fornendo:

- intervallo temporale (date range, sempre UTC)
- durata slot (`slotMinutes`)
- risorse richieste
- eventuali gruppi AND / OR
- filtri proprietà
- filtri su antenati

Il motore restituisce slot coerenti.

---

## Slot duration e granularità

La disponibilità non è restituita come un intervallo continuo indistinto.

Ogni richiesta specifica:

- intervallo temporale
- durata dello slot
- opzionalmente la gestione dello slot residuo (`includeRemainderSlot`)

Il motore suddivide il range richiesto in finestre coerenti e verifica per ciascuna la disponibilità reale.

L’inclusione di eventuali slot residui è una scelta esplicita e opzionale, lasciata al consumer.

---

## Explainability

Il modello separa chiaramente:

- regole
- indisponibilità
- occupazioni
- filtri

Questo rende sempre possibile spiegare perché uno slot è disponibile o non disponibile.

La disponibilità è il risultato della combinazione esplicita di elementi tracciabili.

Ogni slot deriva da un insieme esplicito di regole e intervalli negativi (indisponibilità e occupazioni) verificabili.

---

# Multi-tenant

HelixScheduler supporta modalità multi-tenant con isolamento dei dati a livello Infrastructure.

Caratteristiche:

- Tabella Tenants con seed di default
- Identificazione tenant tramite header HTTP (`X-Tenant`, `X-Helix-Tenant`)
- Fallback automatico su tenant `default`
- Risposta 404 se il tenant richiesto non esiste
- Global query filters EF Core per isolamento dei dati (row-level isolation tramite tenantId)
- Nessuna modifica al Core del motore

Ogni tenant dispone di:

- risorse proprie
- regole proprie
- indisponibilità proprie
- occupazioni proprie
- proprietà e relazioni isolate

Il motore rimane deterministico e neutro rispetto al tenant.

---

# Architettura

- **Core** -> motore deterministico puro
- **Application** -> orchestrazione
- **Infrastructure** -> persistenza e isolamento
- **WebApi** -> esposizione HTTP
- **DemoWeb** -> interfaccia dimostrativa

Il Core è indipendente da database, HTTP o framework esterni.

Il motore può essere testato completamente in-memory, senza database o WebApi, consentendo unit test deterministici e riproducibili.

---

# Technology Stack

HelixScheduler è sviluppato con:

- .NET 10
- C#
- Entity Framework Core
- ASP.NET Core Web API

Caratteristiche tecniche:

- Core completamente indipendente dal framework
- Architettura a progetti separati (Core / Application / Infrastructure / WebApi)
- Supporto SQL Server
- Compatibile con ambienti cross-platform (.NET runtime)

Il motore può essere utilizzato:

- embedded in applicazioni .NET
- tramite WebApi HTTP
- in scenari multi-tenant

---

# Quickstart

1. Configura la connection string SQL Server.
2. Esegui le migration.
3. Avvia la WebApi.
4. Avvia la Demo.

Esempio migration:

```bash
dotnet ef database update \
  --project src/HelixScheduler.Infrastructure \
  --startup-project src/HelixScheduler.WebApi
```

---

# Demo Application

HelixScheduler include una **DemoWeb application** che mostra il motore in azione.

La demo è intenzionalmente:

- read-only
- priva di logica di scheduling nel frontend
- basata esclusivamente sulle API della WebApi

Questo dimostra che il motore è completamente separato dall’interfaccia.

---

## Explorer

La pagina **Explorer** consente di:

- navigare le risorse
- visualizzare le relazioni gerarchiche
- esplorare proprietà assegnate
- comprendere la struttura del dominio

Utilizza esclusivamente endpoint di catalogo:

- `GET /api/catalog/resource-types`
- `GET /api/catalog/resources`
- `GET /api/catalog/properties`

![Explorer](assets/explorer.png)

---

## Availability Search

La pagina **Availability** consente di:

- selezionare un intervallo temporale (UTC)
- impostare la durata dello slot (`slotMinutes`)
- combinare risorse (AND / OR)
- applicare filtri proprietà (`includeDescendants`)
- includere antenati (`includeResourceAncestors`)
- includere o escludere remainder slot

Invoca:

`POST /api/availability/compute`

Mostra:

- slot risultanti
- comportamento deterministico al variare dei parametri
- effetto di capacity, indisponibilità e occupazioni

![Availability](assets/availability.png)

---

## Flusso di interazione

UI -> WebApi -> Application -> Core -> risultato normalizzato -> UI

La demo non contiene logica di calcolo.  
Tutta la disponibilità è prodotta dal motore.

---

# Project status

Il modello concettuale è consolidato:

- risorse
- regole
- indisponibilità
- occupazioni
- capacity
- gerarchie
- filtri semantici
- multi-tenant

HelixScheduler è progettato per essere integrato, esteso e mantenuto nel tempo.

---

# License

Apache-2.0. See `LICENSE`.
