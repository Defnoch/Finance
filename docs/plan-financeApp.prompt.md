# TODO: Foreign key constraints in SQLite werken niet robuust genoeg in de huidige setup. Oplossing met interceptor geprobeerd, maar probleem is niet definitief opgelost. Nogmaals reviewen en robuust maken indien nodig.

## Plan: Requirements & Plan Finance-app (MVP)

Dit document beschrijft de doelen, scope, requirements en globale aanpak voor een persoonlijke finance-app met C#/.NET backend, een web-frontend (single-page app, bij voorkeur Angular) en SQLite, gebaseerd op ING-CSV-import. Het is bedoeld als uitgangspunt voor ontwerp en implementatie van een eerste MVP die lokaal in een self-hosted Docker-omgeving kan draaien. Focus ligt op robuuste import, vlakke categorieën, basisrapportage en een eenvoudige maar uitbreidbare architectuur.

---

### 1. Inleiding & doel

Doel van de applicatie:

- Inzicht krijgen in persoonlijke financiën op basis van banktransacties.
- Starten met één bank (ING) via CSV-export; later uitbreidbaar naar andere banken/formaten.
- Eenvoudige, snelle workflow: CSV uploaden, transacties (semi-)automatisch categoriseren, basisrapportage per maand/jaar en per categorie.
- Applicatie draait lokaal (ontwikkelomgeving) en in een self-hosted Docker-omgeving (light footprint).

Belangrijke uitgangspunten:

- Backend in C#/.NET (console of web-API, uitbreidbaar).
- Frontend als single-page webapp (bij voorkeur Angular).
- Opslag in SQLite (lokaal bestand, eenvoudig te back-uppen).
- Categorieën zijn vlak (geen hiërarchie in MVP).
- Ontwerp houdt rekening met toekomstig meerdere banken, meer kolommen en meer rapportagetypen.

---

### 2. Scope en out-of-scope (MVP)

**In scope (MVP)**

1. **Import**
   - Upload van ING CSV-bestand (zoals `Upload/upload.csv`) via frontend.
   - Parsed backend-import van ING CSV naar intern transactiemodel.
   - Basale validatie (headercontrole, datumnotatie, bedragformaat).
   - Voorkomen van dubbele imports (idempotent gedrag op basis van unieke sleutel / combinatie van velden).

2. **Categorieën**
   - Beheer van vlakke categorieën (CRUD): naam, type (inkomen/uitgave/neutraal), kleur (optioneel).
   - Toewijzing van categorie aan transactie (handmatig via UI).
   - Set standaardcategorieën (bijv. Top 5: Boodschappen, Wonen, Vervoer, Uit eten, Inkomen).
   - Geen subcategorieën, geen hiërarchie in MVP.

3. **Rapportage**
   - Overzicht van alle transacties (filter op periode, categorie, bedragstype).
   - Simpele aggregaties:
     - Totaal inkomsten/uitgaven per maand.
     - Totaal per categorie in geselecteerde periode.
   - Basisvisualisatie (bijv. tabel en eenvoudige grafiek: bar- of circle-chart) in frontend.

4. **Frontend/API**
      - Web-Single Page Application (bij voorkeur Angular) die met backend-API praat (REST/JSON).
      - Schermen:
     - CSV-upload scherm.
     - Overzicht transacties met filter- en sorteeropties.
     - Categoriebeheer.
     - Rapportage/overzicht scherm.
      - Backend-exposure van API endpoints voor bovenstaande (import, transacties, categorieën, rapportage).

5. **Opslag & Deployment**
      - SQLite database voor alle persistente data.
      - Configureerbare database-locatie (zodat deze in een persistent Docker-volume kan worden opgeslagen).
      - Applicatie als .NET-app (bijv. ASP.NET Core web-API) die in één of meer Docker-containers kan draaien.
      - Eenvoudige configuratie voor ontwikkel- en productieomgeving (minstens appsettings-dev/appsettings-prod).

**Out-of-scope (MVP)**

1. **Financiële features**
   - Geen automatische koppeling met bank-API’s (PSD2) in MVP.
   - Geen geavanceerde budgettering (geen budget per categorie, geen alerts).
   - Geen forecasting, geen uitgebreide cashflow-projecties.
   - Geen geavanceerde classificatielogica (AI/ML); hooguit eenvoudige match-regels als optionele nice-to-have.
   - Geen splitsing “structureel vs eenmalig” in rapportage (kan later op basis van extra transactiekenmerk).

2. **Domein / multi-bank**
   - Alleen ING-CSV in MVP.
   - Geen directe ondersteuning voor andere banken (maar architectuur moet extensie mogelijk maken).

3. **Beveiliging & multi-user**
   - Voor MVP: geen multi-user beheer, geen uitgebreide auth/roles.
   - In eerste instantie lokale/vertrouwde omgeving; alleen basale bescherming (bijv. eenvoudige auth of alleen LAN-toegang) als aanvulling, niet als hoofdonderdeel van MVP.

4. **Non-functional**
   - Geen volledige audit logging of uitgebreide monitoring (alleen minimale logging voor debugging/importfouten).
   - Geen schaalbaarheid- of loadtests verder dan “thuisgebruik”-schaal.

---

### 2b. Huidige implementatiestatus (jan 2026)

Deze paragraaf vat samen welke onderdelen van de MVP inmiddels (grotendeels) zijn geïmplementeerd, wat deels aanwezig is en wat nog ontbreekt. Dit helpt om de volgende iteraties gericht te plannen.

**Import**

- CSV-upload (ING)
  - Backend: `POST /api/import/ing` geïmplementeerd (`ImportController` + `ImportService` + `IngCsvImportStrategy`).
  - Frontend: uploadscherm (`ImportComponent`) aanwezig; toont `ImportResultDto` met aantallen en fouten.
  - Headers/kolommen afgestemd op `Upload/upload.csv`; foutmeldingen bij parsing worden in `ImportResultDto.Errors` teruggegeven.
  - Nog te verbeteren: expliciete, herkenbare melding bij header-mismatch en ontbrekende verplichte kolommen.
- Parsing & mapping ING CSV
  - Datums, bedragen en beschrijving worden correct naar het domeinmodel (`Transaction`) gemapt.
  - `SourceSystem`, `SourceReference`, `BookingDate`, `Amount`, `Currency`, `AccountIdentifier`, `Description` en `ImportBatchId` worden gevuld.
- Validatie & foutafhandeling
  - Fouten tijdens parsing worden verzameld in `ImportResultDto.Errors` en via de API geretourneerd.
  - Logging is minimaal (console/log); rijnummer/reden worden nog niet consequent in een gebruikersvriendelijke boodschap gegoten.
- Duplicate-detectie
  - Idempotente import aanwezig op basis van `SourceSystem` + `SourceReference` via `ITransactionRepository.ExistsBySourceReferenceAsync`.
  - `ImportResultDto` rapporteert `InsertedRecords` en `DuplicateRecords`.
- Opslag van imports
  - `ImportBatch`-entiteit bestaat en wordt gevuld met statistieken.
  - Er is nog geen aparte UI of API-overzicht voor geschiedenis van imports.

**Categorieën**

- Standaardcategorieën
  - Backend seedt een set standaardcategorieën via `CategoryQueryService` als de tabel leeg is.
  - De huidige set wijkt licht af van de voorbeeld-top-5 in dit document (o.a. `Boodschappen`, `Abonnementen`, `Wonen`, `Inkomen`, `Overig`). Dit is eenvoudig aanpasbaar als we exact de voorgestelde lijst willen.
- Overzicht & beheer
  - Backend: `ICategoryRepository`/`CategoryRepository` en `GET /api/categories` leveren een volledige lijst met `CategoryDto` (Name, Kind, ColorHex, Id).
  - Frontend: `CategoriesComponent` toont een read-only lijst categorieën.
  - Nog te bouwen: volledige CRUD (POST/PUT/DELETE) voor categorieën plus bijbehorende UI (toevoegen/bewerken/verwijderen, inclusief validatie en bescherming tegen verwijderen van gebruikte categorieën).
- Categorie toewijzing aan transacties
  - Backend: `CategoryAssignmentService` + `POST /api/categories/assign` werken en zijn met NUnit getest.
  - Frontend: er is nog geen transactiescherm waarin per transactie een categorie kan worden geselecteerd; de toewijzing is nu alleen via API/Swagger aan te sturen.
- Eenvoudige categorie-regels
  - Nog niet geïmplementeerd, conform de scope (pas in een latere fase).

**Rapportage**

- Transactieoverzicht
  - Backend: `TransactionReportService` + `GET /api/reports/transactions` bieden een filterbaar transactierapport op basis van datumrange, categorie, bedrag en zoektekst.
  - Frontend: in de rapportagepagina (`ReportsComponent`) is een eenvoudige transactietabel aanwezig met filters op datum en zoektekst.
  - Nog te doen: aparte transactie-overzichtpagina met inline categorie-edit, uitgebreidere filters (categorie, type inkomen/uitgave, min/max bedrag) en eventueel paginering.
- Maandoverzicht inkomsten/uitgaven
  - Backend: `MonthlySummaryService` + `GET /api/reports/monthly-summary` aggregeren per jaar/maand (`TotalIncome`, `TotalExpense`, `Net`).
  - Frontend: `ReportsComponent` toont dit in tabelvorm.
  - Nog te doen: eenvoudige grafische weergave (bijv. bar- of lijngrafiek) en handige periode-presets (laatste maand/kwartaal/jaar).
- Categorieoverzicht in periode
  - Backend: `CategorySummaryService` + `GET /api/reports/category-summary` groeperen transacties per `CategoryId` en berekenen `TotalAmount`.
  - `CategoryName` wordt nog niet via een join gevuld (staat nu leeg); UI toont desnoods de GUID.
  - Frontend: `ReportsComponent` toont een tabel met totalen per categorie.
  - Nog te doen: `CategoryName` invullen via `ICategoryRepository` en optionele grafiek (bar/pie).
- Ongecategoriseerde transacties
  - Er is nog geen speciaal filter voor "Ongecategoriseerd" in de API of UI; dit kan later worden toegevoegd (bv. via een extra filterflag).

**Frontend / schermen**

- CSV-upload scherm
  - Aanwezig (`ImportComponent`), met uploadknop, basisfeedback en resultaatsamenvatting.
- Categorieoverzicht
  - Aanwezig (read-only) in Angular (`CategoriesComponent`).
- Rapportage/overzicht scherm
  - Aanwezig (`ReportsComponent`), met:
    - Transactietabel (basisfilters).
    - Maandoverzicht-tabel.
    - Categorieoverzicht-tabel.
- Transactie-overzicht UI voor categorisatie
  - Nog niet aangelegd als afzonderlijk scherm; rapportage toont wel transacties, maar zonder categorie-dropdown/inline edit.

**Opslag & Deployment**

- SQLite
  - In gebruik via EF Core en `FinanceDbContext`; databasebestand `finance.db` wordt bij startup aangemaakt (`EnsureCreated`).
  - Connectionstring kan via configuratie worden aangepast, maar staat nu nog met een eenvoudige fallback in `Program.cs`.
- Docker / Synology
  - Dockerfiles en expliciete deployment-instructies ontbreken nog; dit volgt in een latere iteratie.
- Configuratie
  - Standaard `appsettings.json` wordt gebruikt; verdere scheiding tussen dev/prod is (nog) niet uitgewerkt, maar is conform .NET-standaard eenvoudig toe te voegen.

---

### 3. Domeinmodel (conceptueel)

Conceptueel domeinmodel (MVP, high-level):

1. **Transactie**
   - Representatie van één generieke financiële transactie (inkomst of uitgave), los van een specifieke bank.
   - Belangrijke attributen:
     - `TransactionId` (interne primary key).
     - `SourceSystem` (string/enum, bijv. `"ING"`, later `"RABO"`, etc.).
     - `SourceReference` (string, unieke referentie of hash voor duplicate-detectie; hoe deze wordt opgebouwd is verantwoordelijkheid van de importstrategie).
     - `BookingDate` (boekingsdatum).
     - `ValueDate` (indien beschikbaar, anders gelijk aan `BookingDate`).
     - `Amount` (decimal, positief voor inkomsten, negatief voor uitgaven).
     - `Currency` (string, in MVP altijd `"EUR"`, maar als attribuut aanwezig voor toekomstig gebruik).
     - `AccountIdentifier` (string; representatie van de eigen rekening, bijv. IBAN of ander id).
     - `CounterpartyIdentifier` (string, optioneel; representatie van de tegenrekening of tegenpartij).
     - `Description` (samengestelde, mens-leesbare beschrijving uit één of meerdere bronvelden).
     - `RawData` (optioneel, string/JSON met bron-specifieke details; wordt door de kernlogica niet geïnterpreteerd, maar kan gebruikt worden voor debugging of toekomstige features).
     - `CategoryId` (foreign key naar `Category`; null indien (nog) niet gecategoriseerd).
     - `ImportBatchId` (koppeling naar importrun `ImportBatch`).

2. **Categorie**
   - Vlakke categorie, geen hiërarchie.
   - Attributen:
     - `CategoryId`.
     - `Name` (uniek binnen gebruiker/omgeving).
     - `Kind` (enum: `Income`, `Expense`, `Neutral`).
     - `ColorHex` (optioneel, string `#RRGGBB`).
     - `IsDefault` (bool) – ter aanduiding dat het om een van de standaardcategorieën gaat.

3. **ImportBatch**
   - Representatie van één importactie (één upload), generiek voor elke bron.
   - Attributen:
     - `ImportBatchId`.
     - `SourceSystem` (bijv. `ING`).
     - `FileName`.
     - `ImportedAt`.
     - `TotalRecords`.
     - `InsertedRecords` (effectief toegevoegd).
     - `DuplicateRecords` (overgeslagen).
     - `Status` (geslaagd/mislukt/gedeeltelijk).
     - `ErrorMessage` (indien mislukt).

4. **Rapportage-view / aggregaties (conceptueel)**
   - Geen aparte entiteiten in database nodig, maar concepten:
     - `PeriodeSamenvatting` (per maand, per jaar).
     - `CategorieSamenvatting` (som bedrag per categorie, filterbaar op periode).
   - Deze worden runtime samengesteld vanuit transacties.

5. **Gebruiker / Config (MVP)**
   - In MVP kan user implicit zijn (single-user). Indien later nodig:
     - `User` entiteit.
     - `UserSettings` (bijv. default view, mappingregels).
   - Voor nu volstaat een configuratiebestand/appsettings voor globale instellingen (CSV lay-out, locatie database, etc.).

---

### 4. Functionele requirements als user stories

#### 4.1 Import

1. **CSV-upload (ING)**
   - Als gebruiker wil ik een ING-CSV-bestand kunnen uploaden via de webapp, zodat mijn banktransacties in het systeem worden ingelezen.
   - Acceptatiecriteria:
     - Upload-scherm accepteert minstens `.csv`.
     - Systeem valideert of headers overeenkomen met ING-export (zoals in `Upload/upload.csv`).
     - Bij mismatch krijgt gebruiker een duidelijke foutmelding.

2. **Parsing & mapping ING CSV**
   - Als systeem wil ik de kolommen uit het ING CSV-bestand correct mappen naar interne transactiefvelden, zodat de data consistent in de database wordt opgeslagen.
   - Acceptatiecriteria:
     - Datums worden correct parsed (NL-notatie, bijv. `dd-MM-yyyy`).
     - Bedragen worden correct parsed (komma als decimaal, plus/minteken).
     - Relevante tekstkolommen worden samengevoegd tot een bruikbare `Omschrijving`.
     - Alle transacties in bestand worden ingelezen in een transient representatie (voor validatie en opslag).

3. **Validatie & foutafhandeling**
   - Als gebruiker wil ik duidelijke feedback krijgen als een CSV-bestand niet ingelezen kan worden, zodat ik weet wat ik moet corrigeren.
   - Acceptatiecriteria:
     - Bij parsing-fout op één rij: transactie wordt overgeslagen of hele import faalt (MVP: hele import faalt, met rijnummer en reden).
     - Bij ontbrekende verplichte kolommen: import wordt afgebroken, foutmelding met lijst ontbrekende kolommen.
     - Errors worden gelogd (minimaal in console/logfile).

4. **Duplicate-detectie**
   - Als gebruiker wil ik niet dat dezelfde transactie meerdere keren wordt geïmporteerd, zodat mijn overzichten kloppen.
   - Acceptatiecriteria:
     - Systeem bepaalt een unieke sleutel op basis van combinatie velden (bijv. datum + bedrag + tegenrekening + omschrijving).
     - Bij herimport van overlappende perioden worden bestaande transacties niet nogmaals opgeslagen.
     - Importresultaat toont hoeveel transacties zijn overgeslagen als dubbel.

5. **Opslag van imports**
   - Als gebruiker wil ik achteraf kunnen zien wat een import heeft gedaan, zodat ik weet of alles goed is gegaan.
   - Acceptatiecriteria:
     - ImportBatch-entiteit wordt aangemaakt met statistieken (totaal, nieuw, dubbel, status, tijdstip).
     - Via UI (MVP optioneel) of logs is te zien of import geslaagd is.

#### 4.2 Categorieën

1. **Standaardcategorieën**
   - Als gebruiker wil ik dat er automatisch een set standaardcategorieën wordt aangemaakt, zodat ik snel aan de slag kan zonder alles zelf te bedenken.
   - Acceptatiecriteria:
     - Bij eerste start (of bij lege database) worden standaardcategorieën toegevoegd.
     - Minstens de Top 5 (voorbeeld): `Inkomen`, `Boodschappen`, `Wonen`, `Vervoer`, `Uit eten`.
     - Categorienamen zijn uniek.

2. **Overzicht & beheer categorieën**
   - Als gebruiker wil ik een scherm met een lijst van alle categorieën, zodat ik bestaande categorieën kan zien en bewerken.
   - Acceptatiecriteria:
     - Lijstweergave met naam, type, optioneel kleur.
     - Mogelijkheid om categorie toe te voegen, naam/type/kleur te wijzigen.
     - Beveiliging tegen verwijderen van categorieën die transacties gekoppeld hebben (MVP: of blokkeren of her-categoriseren naar een “Ongecategoriseerd”).

3. **Categorie toewijzing aan transacties**
   - Als gebruiker wil ik per transactie een categorie kunnen instellen, zodat ik mijn uitgaven kan groeperen.
   - Acceptatiecriteria:
     - In transactieoverzicht kan gebruiker per transactie een categorie selecteren uit dropdown.
     - Wijzigingen worden direct opgeslagen.
     - Standaardwaarde bij nieuwe transacties: `null` of `Ongecategoriseerd`.

4. **(Optioneel) Eenvoudige categorie-regels**
   - Als gebruiker wil ik (in een latere fase) eenvoudige regels kunnen instellen op basis van tekst in omschrijving, zodat categorieën automatisch worden voorgesteld.
   - Voor MVP: niet implementeren, maar model zo ontwerpen dat dit later toe te voegen is.

#### 4.3 Rapportage

1. **Transactieoverzicht**
   - Als gebruiker wil ik een lijst van al mijn transacties kunnen zien met filters, zodat ik gericht kan zoeken.
   - Acceptatiecriteria:
     - Kolommen: datum, omschrijving, bedrag, categorie, tegenrekening.
     - Filters: datumrange, categorie, bedrag (min/max), type (inkomen/uitgave).
     - Sortering op datum (default: aflopend), bedrag, categorie.

2. **Maandoverzicht inkomsten/uitgaven**
   - Als gebruiker wil ik een maandrapport kunnen zien van totale inkomsten en uitgaven, zodat ik inzicht krijg in mijn cashflow per maand.
   - Acceptatiecriteria:
     - Aggregatie per maand (jaar + maand).
     - Voor elke maand: totaal inkomsten (som positieve bedragen), totaal uitgaven (som negatieve bedragen).
     - Weergave als tabel; grafiek als eenvoudige lijn- of barchart (frontend).

3. **Categorieoverzicht in periode**
   - Als gebruiker wil ik per categorie kunnen zien hoeveel ik in een bepaalde periode heb uitgegeven of ontvangen, zodat ik mijn uitgavenpatroon begrijp.
   - Acceptatiecriteria:
     - Filter op datumrange.
     - Per categorie: som bedrag (scheiding inkomsten/uitgaven of netto, MVP: netto of twee kolommen).
     - Weergave als tabel + eenvoudig cirkeldiagram of barchart.

4. **Ongecategoriseerde transacties**
   - Als gebruiker wil ik snel kunnen zien welke transacties nog geen categorie hebben, zodat ik ze eenvoudig kan toewijzen.
   - Acceptatiecriteria:
     - Filter “Ongecategoriseerd” in transactieoverzicht.
     - Eenvoudige navigatie vanuit rapportage naar de onderliggende transacties.

#### 4.4 Frontend/API

1. **CSV-upload UI**
   - Als gebruiker wil ik in de browser een uploadknop zien, zodat ik een CSV-bestand kan selecteren en laten verwerken.
   - Acceptatiecriteria:
     - Bestand selecteren via file input.
     - Visuele feedback tijdens upload (spinner/progress).
     - Resultaatoverzicht na upload: aantal nieuwe transacties, aantal dubbele, eventuele fouten.

2. **Transactie-overzicht UI**
   - Als gebruiker wil ik een pagina met transacties in een tabel, zodat ik transacties kan bekijken en categoriseren.
   - Acceptatiecriteria:
     - Client-side paginering of lazy loading (indien nodig).
     - Inline wijziging van categorie.
     - Basis-filterbalk voor datum/categorie.

3. **Categoriebeheer UI**
   - Als gebruiker wil ik een overzichtspagina voor categorieën, zodat ik categorieën kan aanmaken, aanpassen of (eventueel) verwijderen.
   - Acceptatiecriteria:
     - Formulier voor toevoegen/bewerken.
     - Validatie (naam verplicht, type verplicht).

4. **Rapportage UI**
   - Als gebruiker wil ik een eenvoudige rapportagepagina met grafieken en tabellen, zodat ik snel een financieel overzicht heb.
   - Acceptatiecriteria:
     - Selectie van periode (bijv. via datepicker of presets: laatste maand, laatste 3 maanden, jaar).
     - Weergave maandoverzicht en categorieoverzicht.
     - Links naar onderliggende transacties.

5. **API endpoints (indicatief, REST)**
   - Import:
     - `POST /api/import/ing` – upload CSV, start import, retourneert ImportBatch-resultaat.
   - Transacties:
     - `GET /api/transactions` – met queryparameters voor filters.
     - `PATCH /api/transactions/{id}/category` – categorie toewijzen.
   - Categorieën:
     - `GET /api/categories`
     - `POST /api/categories`
     - `PUT /api/categories/{id}`
     - `DELETE /api/categories/{id}` (optioneel in MVP).
   - Rapportage:
     - `GET /api/reports/summary/monthly`
     - `GET /api/reports/summary/by-category`

#### 4.5 Opslag / Deployment

1. **SQLite-database**
   - Als ontwikkelaar wil ik dat de app SQLite gebruikt, zodat de applicatie eenvoudig te deployen en te back-uppen is.
   - Acceptatiecriteria:
     - Connectionstring configureerbaar via configuratiebestand/omgeving.
     - Database-bestand wordt aangemaakt indien niet aanwezig.
     - Migraties (bijv. via EF Core) initialiseren schema.

2. **Synology-deployable**
   - Als gebruiker wil ik de app op mijn Synology NAS kunnen draaien, zodat deze altijd beschikbaar is op mijn thuisnetwerk.
   - Acceptatiecriteria:
     - Applicatie is als self-contained .NET build of Docker image te draaien.
     - Applicatie luistert op een configureerbare poort/URL.
     - Externe configuratie voor db-pad en log-locatie, zodat persistent volumes op Synology gebruikt kunnen worden.

3. **Configuratie & omgevingen**
   - Als ontwikkelaar wil ik dev- en prod-configuraties kunnen scheiden, zodat ik lokaal kan testen met andere instellingen dan op Synology.
   - Acceptatiecriteria:
     - Gebruik van `appsettings.json` + `appsettings.Development.json` etc.
     - Overriden via environment variables waar nodig.

---

### 5. Korte technische architectuurschets

**Componenten (high level)**

1. **Backend (C#/.NET)**
   - Type: ASP.NET Core Web API (of minimal APIs) die endpoints aanbiedt voor import, transacties, categorieën en rapportage.
   - Laagopbouw:
     - API layer (controllers / endpoints) – HTTP, DTO’s, validatie.
     - Service layer – businesslogica (import, duplicate-detectie, categorie-logica, rapportage-aggregaties).
     - Persistence layer – repository/ORM-laag (bijv. EF Core) richting SQLite.
   - CSV-parser:
     - Modulair component (bijv. `IngCsvImporter`) die specifiek voor ING headers/kolommen implementeert.
     - Interface (bijv. `IBankImportParser`) waarmee later andere banken (Rabobank, ABN, etc.) kunnen worden toegevoegd.

2. **Frontend (Vue)**
   - Type: Vue SPA (Vue 3 aanbevolen) met router en één layout.
   - Pagina’s/views:
     - `UploadView` – upload ING CSV.
     - `TransactionsView` – lijst transacties + filters + categorie-edit.
     - `CategoriesView` – beheer categorieën.
     - `ReportsView` – rapportage/overzichten.
   - Communicatie:
     - HTTP/JSON calls naar backend-API.
     - Basis error-handling (meldingen bij mislukte calls).

3. **Database (SQLite)**
   - Tabelsuggesties:
     - `Transactions` (veldnamen aansluitend op domeinmodel).
     - `Categories`.
     - `ImportBatches`.
   - Mogelijke indexen:
     - Op `Date` voor rapportage.
     - Op `CategoryId`.
     - Op combinatie voor duplicate-detectie.
   - Migrations:
     - Gebruik EF Core migrations of een vergelijkbaar schema-beheersysteem.

4. **Extensibiliteit multi-bank**
   - Abstractie:
     - Interface voor importstrategie: `IBankCsvParser` of `IBankImportStrategy`.
     - Registry of mapping van “banktype” -> parser.
   - ING-implementatie:
     - Specifieke mapping van ING CSV headers naar interne velden.
     - Parser verwacht layout zoals `Upload/upload.csv`.
   - Later:
     - Toevoegen extra implementaties voor andere banken zonder bestaande logica te breken.

---

### 6. Default categorieën en aannames

**Default categorieën (MVP)**

Voor de eerste oplevering wordt een beperkte set standaardcategorieën gebruikt, met nadruk op veelvoorkomende posten:

1. `Inkomen` – type: `Inkomen`
2. `Boodschappen` – type: `Uitgaven`
3. `Wonen` – type: `Uitgaven`
4. `Vervoer` – type: `Uitgaven`
5. `Uit eten` – type: `Uitgaven`

Eventueel aanvullend (optioneel):

6. `Abonnementen` – type: `Uitgaven`
7. `Vrije tijd` – type: `Uitgaven`
8. `Overig` – type: `Neutraal/Overig`

Deze worden bij eerste initialisatie automatisch aangemaakt als ze nog niet bestaan.

**Belangrijke aannames voor MVP**

1. **Gebruiker & security**
   - Eén gebruiker / één dataset per installatie.
   - Applicatie wordt in vertrouwde omgeving gebruikt (thuisnetwerk), zonder uitgebreide auth/role-based access.
   - Later kan authentication (bijv. basic login of reverse proxy met auth) worden toegevoegd.

2. **Valuta & bedragen**
   - Alle bedragen zijn in EUR (ING CSV).
   - Positief bedrag = inkomend; negatief bedrag = uitgaand (of af te leiden uit veld/kolom van ING).

3. **CSV-formaat ING**
   - Layout is gelijk aan export gegenereerd door ING op het moment van ontwerp (zoals `Upload/upload.csv`).
   - Kolomnamen en volgorde kunnen worden vastgeklikt (headercontrole).
   - Als ING het formaat wijzigt, is een parser-update nodig.

4. **Data-hoeveelheid**
   - Thuisgebruik: maximaal enkele tienduizenden transacties, geen enterprise-schaal.
   - SQLite is voldoende voor performance en opslag.

5. **Synology deployment**
   - NAS kan .NET (via Docker of nativ) draaien.
   - SQLite-bestand wordt op een persistent volume of gedeelde map geplaatst.
   - Toegang typisch via intern netwerk; geen publieke internet-exposure vereist.

---

### 7. Architectuur volgens DDD en SOLID

#### 7.1 Lagen (Domain, Application, Infrastructure, API)

De backend wordt opgezet volgens een klassieke DDD-lagenarchitectuur:

1. **Domain layer**
   - Bevat de kern van het domein:
     - Entiteiten/aggregates: `Transaction`, `Category`, `ImportBatch`.
     - Domeininterfaces: o.a. `ITransactionRepository`, `ICategoryRepository`, `IImportBatchRepository`, `IBankImportStrategy` (en eventueel later een `IReportingService`).
   - Kent geen details van CSV, HTTP, SQLite of frontend.
   - Alle domeinlogica die bank-onafhankelijk is, leeft hier.

2. **Application layer (use cases)**
   - Implementeert de use cases (user stories) als services, zoals:
     - `ImportTransactionsUseCase` (CSV-bestand → transacties + importbatch).
     - `AssignCategoryUseCase` (categorie aan transactie koppelen).
     - `GetTransactionsUseCase` (filterbare transactielijsten).
     - `GetMonthlySummaryUseCase` en `GetCategorySummaryUseCase` (rapportages).
   - Orkestreert domain-objects en repositories, maar kent geen CSV- of database-details.

3. **Infrastructure layer**
   - Concrete implementaties van de domain-interfaces:
     - Repositories richting SQLite (bijv. via EF Core).
     - Importstrategieën voor banken, zoals `IngCsvImportStrategy` die `IBankImportStrategy` implementeert.
   - Kent CSV-formaatdetails, EF Core-configuratie, connectionstrings, etc.

4. **API / Presentation layer**
   - ASP.NET Core Web API (of minimal APIs):
     - Stelt HTTP-endpoints bloot voor frontend/CLI.
     - Mapt HTTP-requests naar application use cases en mapt domainresultaten naar DTO’s.
   - Bevat geen domeinlogica; alleen validatie, mapping en foutafhandeling.

5. **Frontend (SPA)**
   - Staat los van de backend en communiceert uitsluitend via de API (REST/JSON).
   - Geen directe koppeling met domain- of infrastructuurlagen.

#### 7.2 SOLID-principes in dit ontwerp

1. **Single Responsibility Principle (SRP)**
   - Elke klasse heeft één duidelijke verantwoordelijkheid:
     - `Transaction` modelleert alleen een transactie, zonder kennis van CSV of HTTP.
     - `IngCsvImportStrategy` weet alleen hoe een ING-CSV wordt vertaald naar generieke transactie-gegevens (`TransactionDraft`), niet hoe deze worden opgeslagen.
     - `ImportTransactionsUseCase` orkestreert alleen het importproces (strategie oproepen, duplicaten controleren, transacties opslaan, importbatch aanmaken).
     - Repositories zijn alleen verantwoordelijk voor datastore-toegang.

2. **Open/Closed Principle (OCP)**
   - Het systeem is open voor uitbreiding, gesloten voor wijziging:
     - Nieuwe banken worden toegevoegd door een nieuwe implementatie van `IBankImportStrategy` (bijv. `RaboCsvImportStrategy`), zonder dat bestaande domeinlogica of use cases aangepast hoeven te worden.
     - Nieuwe rapportages worden toegevoegd als nieuwe use cases en (indien nodig) extra methodes op rapportagediensten, zonder de entiteiten zelf aan te passen.

3. **Liskov Substitution Principle (LSP)**
   - Implementaties van een interface moeten zonder verrassingen uitwisselbaar zijn:
     - Elke `IBankImportStrategy` (ING of later RABO/ABN) retourneert dezelfde generieke structuur (`TransactionDraft`) die de rest van de applicatie kan verwerken.
     - Use cases hoeven geen speciale gevallen te behandelen per bank; ze spreken alleen met de abstractie.

4. **Interface Segregation Principle (ISP)**
   - Voorkomen van “te brede” interfaces:
     - Kleinere, gerichte interfaces zoals `ITransactionRepository`, `ICategoryRepository`, `IImportBatchRepository` en `IBankImportStrategy` in plaats van één allesomvattende repository of service.
     - Implementaties hoeven alleen de methodes te ondersteunen die ze werkelijk nodig hebben.

5. **Dependency Inversion Principle (DIP)**
   - Hoog-niveau modules (use cases) hangen af van abstracties, niet van concrete implementaties:
     - Application layer injecteert interfaces (`ITransactionRepository`, `IBankImportStrategyResolver`, etc.), niet directe EF Core of CSV-klassen.
     - Infrastructure layer levert concrete implementations (bijv. `EfTransactionRepository`, `IngCsvImportStrategy`).
   - Maakt het eenvoudig om te testen (mocks/fakes) en om later delen te vervangen (andere database, andere importmethode).

#### 7.3 Belangrijkste use cases (application services)

1. **ImportTransactionsUseCase**
   - Doel: een CSV-bestand voor een bepaalde `SourceSystem` (MVP: `"ING"`) importeren als transacties.
   - Input: `fileStream`, `sourceSystem`.
   - Werkwijze (high-level):
     - Resolve juiste `IBankImportStrategy` op basis van `sourceSystem`.
     - Parse naar een collectie `TransactionDraft`.
     - Voor elke draft: bepaal `SourceReference` en controleer via `ITransactionRepository` of er al een transactie met deze referentie bestaat.
     - Sla alleen nieuwe transacties op als `Transaction`-entiteiten.
     - Maak een `ImportBatch` aan met statistieken (totaal, nieuw, dubbelen).
     - Retourneer een DTO met importresultaat.

2. **AssignCategoryUseCase**
   - Doel: een bestaande transactie aan een bestaande categorie koppelen of de categorie wijzigen.
   - Input: `transactionId`, `categoryId`.
   - Werkwijze:
     - Haal transactie en categorie op via de repositories.
     - Valideer dat beide bestaan.
     - Stel `CategoryId` op de transactie in en sla op.

3. **GetTransactionsUseCase**
   - Doel: een filterbare lijst van transacties ophalen.
   - Input: filterobject (datum van/tot, categorie, bedrag min/max, zoekterm).
   - Output: lijst van `TransactionDto` die geschikt is voor de frontend.

4. **GetMonthlySummaryUseCase**
   - Doel: inkomsten/uitgaven per maand aggregeren.
   - Input: jaar of datumbereik.
   - Output: lijst `MonthlySummaryDto` met per maand `TotalIncome`, `TotalExpense`, `Net`.

5. **GetCategorySummaryUseCase**
   - Doel: totalen per categorie over een periode.
   - Input: datum van/tot.
   - Output: lijst `CategorySummaryDto` met per categorie het totaalbedrag.

Deze use cases vormen de brug tussen de API (HTTP) en het domein, en verbergen infrastructuurdetails achter duidelijk gedefinieerde interfaces.

# Projectafspraak: API Client genereren

- **De api-client (`src/app/core/api/api-client.ts`) mag NIET handmatig worden aangepast.**
- Alle wijzigingen aan de api-client moeten via het apiclientgenerator proces verlopen.
- Handmatige aanpassingen in deze file worden bij een volgende generatie overschreven.
- Zie deze notitie als harde afspraak voor alle ontwikkelaars.

**Laatste wijziging: 2026-02-01**
