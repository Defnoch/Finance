# Project Policy & AI Agent Instructions

## Angular Block Syntax Policy

Dit project gebruikt de nieuwe Angular block-syntax (`@if`, `@for`) in plaats van de oude structural directives (`*ngIf`, `*ngFor`).

- Gebruik altijd `@if` en `@for` in alle component templates.
- Gebruik geen `*ngIf` of `*ngFor` meer.
- Dit is verplicht voor alle nieuwe en bestaande code.
- Reden: `*ngFor` is deprecated vanaf Angular 20 en wordt verwijderd in Angular 22.

**Let op:** Controleer bij het toevoegen van nieuwe componenten of de juiste syntax wordt gebruikt.

---

## AI Agent Instructions

### Build Actions
- Controleer altijd eerst de huidige directory (`pwd`) voordat je een build-actie uitvoert.
- Controleer of de juiste build-bestanden aanwezig zijn (zoals `package.json`, `angular.json`, `tsconfig.json`).
- Voer build-commando’s alleen uit in de juiste projectmap (bijvoorbeeld `Finance.Web/client-app` voor de frontend).
- Rapporteer altijd het resultaat van de build en eventuele fouten, inclusief de exacte directory waaruit het commando is uitgevoerd.

### Algemeen
- Volg altijd de projectafspraken zoals vastgelegd in README.md, CONTRIBUTING.md en andere relevante documentatie.
- **Pas nooit handmatig wijzigingen toe aan de gegenereerde API clients (`ApiClient.cs` en `api-client.ts`)!** Gebruik altijd de juiste generator-processen (zoals de API client generator).
- Documenteer afwijkende of project-specifieke afspraken in dit bestand.

### Voorbeeld: Frontend Build
1. Voer `pwd` uit om de huidige directory te tonen.
2. Controleer of `package.json` en `angular.json` aanwezig zijn.
3. Voer pas daarna `npm run build` of `ng build` uit.
4. Rapporteer het resultaat en eventuele fouten.

---

## AI Agent Workflow: API Client Generation

### Belangrijke Regel
- **De API client (`api-client.ts`) mag nooit handmatig worden aangepast.**
- **Gebruik altijd het apiclientgenerator proces om wijzigingen door te voeren.**
- Indien een veld ontbreekt of gewijzigd moet worden:
  1. Pas de backend aan (model/controller).
  2. Genereer de API client opnieuw via de generator.
  3. Kopieer de gegenereerde client naar de frontend.
- Handmatige wijzigingen worden direct overschreven bij de volgende generatie.

### Stappen voor AI Agents
1. Controleer altijd of een wijziging in de API client nodig is.
2. Voer backend aanpassingen uit indien nodig.
3. Genereer de client via het apiclientgenerator proces.
4. Kopieer de gegenereerde client naar de juiste frontend locatie.
5. Test de build na elke wijziging.

> **Deze workflow is verplicht voor alle AI agents en developers.**

---

## Angular Material Dialog Policy

- Gebruik altijd de exacte structuur en imports van een bewezen werkend voorbeeld (zoals `features/test/dialog-animations-example-dialog.html` en de bijbehorende component) voor nieuwe of aangepaste dialogs.
- Gebruik uitsluitend de Angular Material primitives (`<mat-dialog-title>`, `<mat-dialog-content>`, `<mat-dialog-actions>`, `<mat-dialog-close>`) in de template, zonder extra wrappers of custom classes.
- Importeer altijd de relevante Material primitives als standalone imports in de dialog-component.
- Gebruik geen custom CSS voor overflow, max-height of andere layout-eigenschappen in dialogs, tenzij strikt noodzakelijk en goed gedocumenteerd.
- Gebruik `ChangeDetectionStrategy.OnPush` voor alle dialog-componenten.
- Sluit dialogs met `mat-dialog-close` op knoppen, zonder extra click-handlers tenzij functioneel vereist.
- Volg altijd de block-syntax policy (`@if`, `@for`) voor alle Angular templates.
- Test altijd de dialog op consistent gedrag (geen scrollbalk, correcte layout) na aanpassing.

> **Deze policy is verplicht voor alle Angular Material dialogs in dit project.**

*Laat dit bestand in de root van het project staan zodat alle AI agents en automation tools deze instructies kunnen volgen.*
