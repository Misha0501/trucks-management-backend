# Driver Employment Contract Template (AODriver)

**Version**: 2025.1  
**Language**: Dutch (nl-NL)  
**Based on**: TLN CAO Beroepsgoederenvervoer (Transport Workers Collective Agreement)  
**Source**: EmployeeTabel.xlsx → AODriver sheet

---

## Overview

This document defines the structure and content for generating driver employment contracts. The contract is legally compliant with Dutch labor law and the TLN CAO for professional goods transport.

---

## Missing Data Analysis

### ✅ **Fields We Have** (from `EmployeeContract`)
- All employee personal information (name, address, postcode, city, DOB, BSN, IBAN)
- All employment terms (dates, function, probation, schedule, working hours)
- Salary information (scale, step, hourly wage, travel expenses)
- Vacation data (days, ATV, allowance)
- Company information (name, address, postcode, city, BTW, KvK, phone)

### ⚠️ **Required Entity Changes**

**Two new fields needed in `EmployeeContract` entity:**

1. **`CreatedAt`** (DateTime, required)
   - **Purpose**: Timestamp when contract was created
   - **Used for**: Contract date printed on document ("Aldus overeengekomen...op [date]")
   - **Set by**: Backend when contract is created (DateTime.UtcNow)

2. **`CreatedByUserId`** (string, nullable)
   - **Purpose**: AspNetUserId of the user who created the contract
   - **Used for**: Contact person name in signature ("Namens deze: [Name]")
   - **Set by**: Backend from current user context (customerAdmin or globalAdmin)
   - **Resolved at generation**: Join with ApplicationUser to get FirstName + LastName

### 🔄 **Calculated Fields** (generated at runtime)

1. **Contract Type**: `LastWorkingDay != null ? "BEPAALDE" : "ONBEPAALDE"`
2. **Contact Person Name**: Resolve `CreatedByUserId` → `ApplicationUser.FirstName + " " + LastName`
3. **Vacation Age Group**: Lookup from `CAOVacationDays` based on age (e.g., "45 t/m 49 jaar")
4. **Statutory Vacation Days**: `VacationDays - 4`
5. **Extra Vacation Days**: `4` (always)
6. **Work Duration Text**: `WorkweekDuration + " uur per week"`
7. **Vacation Allowance Percent**: Format as percentage (e.g., `0.08 → "8%"`)

---

## Required Lookup Tables

### 1. **CAOPayScales** (61 rows)
Source: `TruckManagement/Docs/CAOPayScales_SeedData.csv`

**Columns**:
- `Scale` (A, B, C, D, E, F, G, H)
- `Step` (1-10, varies by scale)
- `WeeklyWage`, `FourWeekWage`, `MonthlyWage`
- `HourlyWage100`, `HourlyWage130`, `HourlyWage150`

**Usage**: Look up wage information by `PayScale` + `PayScaleStep`

### 2. **CAOVacationDays** (8 rows)
Source: `TruckManagement/Docs/CAOVacationDays_SeedData.csv`

**Columns**:
- `AgeFrom`, `AgeTo`
- `AgeGroupDescription` (e.g., "45 t/m 49 jaar")
- `VacationDays`

**Usage**: Calculate age from `DateOfBirth`, lookup vacation entitlement

---

## Contract Structure

### **Title**
```
ARBEIDSOEVEENKOMST VOOR [BEPAALDE/ONBEPAALDE] TIJD
```
- **BEPAALDE** = Fixed-term (if `LastWorkingDay` exists)
- **ONBEPAALDE** = Indefinite (if `LastWorkingDay` is null)

---

### **Header Section**

**Left Column (Werkgever)**:
- Bedrijfsnaam: `{{CompanyName}}`
- Adres: `{{CompanyAddress}}`
- Postcode/Plaats: `{{CompanyPostcode}} {{CompanyCity}}`
- Contact: `{{ContactPersonName}}`

**Right Column (Werknemer)**:
- Naam: `{{EmployeeFirstName}} {{EmployeeLastName}}`
- Adres: `{{EmployeeAddress}}`
- Postcode/Plaats: `{{EmployeePostcode}} {{EmployeeCity}}`
- Geb.datum: `{{DateOfBirth}}`
- BSN: `{{Bsn}}`

**Intro Line**:
```
zijn het volgende overeengekomen:
```

---

### **Articles (Artikelen)**

#### **Artikel 1: Datum indiensttreding, functie en plaats werkzaamheden**

**Para 1**:
```
Er is een dienstverband aangegaan met ingang van: {{DateOfEmployment|dd mmmm yyyy}}
[if LastWorkingDay] tot {{LastWorkingDay|dd mmmm yyyy}}[/if].
```

**Para 2**:
```
De werknemer is aangenomen voor de functie van {{Function}}.
```

**Para 3** (static):
```
De bij deze functie behorende werkzaamheden bestaan uit: Het vervoeren van goederen 
over weg en voorts alle werkzaam­heden die redelijkerwijs van hem verlangd kunnen worden. 
De werkzaamheden worden uitgevoerd vanuit de vestiging van werkgever of opdrachtgevers 
van werkgever.
```

---

#### **Artikel 2: Proeftijd**

**Conditional**:
```
[if ProbationPeriod]
Dit arbeidscontract is aangegaan met inachtneming van een wederzijdse proeftijd van 
{{ProbationPeriod}}. Gedurende deze proeftijd kunnen beide partijen het arbeidscontract 
met directe ingang beëindigen. Een schriftelijke bevestiging hiervan is niet benodigd. 
De proeftijd is van toepassing indien een dienstverband voor een minimale duur van 
12 maanden is aangegaan.
[else]
Er is geen proeftijd bij het aangaan van de dienstverband.
[/if]
```

---

#### **Artikel 3: Arbeidsduur, werktijden en overwerk**

**Para 1**:
```
De wekelijkse arbeidsduur bedraagt {{WorkweekDuration}} uur per week.
```

**Para 2**:
```
Werknemer verklaart op de hoogte te zijn van en in te stemmen met de door werkgever 
geregelde wekelijkse planning voor {{WeeklySchedule}}. Werknemer is tevens op de hoogte 
gesteld dat de werktijden tussen {{WorkingHours}} kunnen plaatsvinden. Werknemer is 
redelijkerwijs verplicht gehoor te geven aan een redelijk verzoek van ­werkgever om 
overwerk te verrichten.
```

---

#### **Artikel 4: Salaris**

```
Werknemer ontvangt conform de cao en loonschaal {{PayScale}} en trede {{PayScaleStep}} 
een salaris van € {{HourlyWage100Percent|0.00}} bruto per uur door werkgever te voldoen 
voor of op de laatste dag van de maand.
```

---

#### **Artikel 5: Reiskostenvergoeding**

```
De werknemer ontvangt geen vergoeding voor de reiskosten woon-werkverkeer. Indien achteraf 
hierover schriftelijk wordt overeengekomen bedraagt de vergoeding maximaal 
€ {{TravelExpenses|0.00}} per gereisde kilometer met een maximum reiskostenvergoeding van 
€ {{MaxTravelExpenses|0.00}} per maand. De reiskostenvergoeding is onbelast en maakt geen 
deel uit van het loon van de werknemer.
```

---

#### **Artikel 6: Salaris bij arbeidsongeschiktheid en wachtdagen**

**Para 1** (static):
```
1. In geval van arbeidsongeschiktheid wegens ziekte, ongeval en dergelijke, zal werkgever, 
indien en zolang in die periode het dienstverband voortduurt, gedurende de eerste 104 weken 
van arbeidsongeschiktheid 70% van het brutoloon door­betalen, doch gedurende de eerste 
52 weken ten minste het voor werknemer geldende ­minimumloon.
```

**Para 2** (static):
```
2. Werknemer verklaart op de hoogte te zijn van en in te stemmen met de door werkgever 
vastgestelde voorschriften in verband met ziekmelding en controle.
```

**Para 3** (static):
```
3. Wegens ziekte of arbeidsongeschiktheid heeft de werknemer gedurende de eerste dag van 
een ziekteperiode geen recht op loon. Dit geldt niet indien werknemer na het einde van een 
ziekteperiode binnen vier weken wederom ziek wordt.
```

---

#### **Artikel 7: Re-integratie na einde dienstverband**

**Para 1** (static):
```
- Werknemer is verplicht zich onmiddellijk ziek te melden bij werkgever conform de door 
werkgever vastgestelde voorschriften in verband met ziekmelding en controle als werknemer 
binnen 4 weken na het einde van de arbeidsovereenkomst ziek wordt en op dat moment niet 
werkzaam is bij een andere werkgever of een WW-uitkering ontvangt.
```

**Para 2** (static):
```
- Als werknemer ziek is op het moment dat hij uit dienst gaat en/of als werknemer voldoet 
aan het bepaalde in lid 1 van dit artikel, dient werknemer zich te blijven houden aan de 
door werkgever of regelgeving vastgestelde voorschriften in verband met ziekmelding en 
controle.
```

**Para 3** (static):
```
- Indien werknemer bovenstaande van de in dit artikel bepaalde overtreedt, verbeurt hij 
aan werkgever een direct opeisbare boete van 2.500,- voor iedere overtreding, alsmede een 
bedrag van 500,- voor iedere dag dat de overtreding voortduurt. De boete zal verschuldigd 
zijn door het enkele feit van de overtreding of niet-nakoming, maar laat onverminderd het 
recht om volledige schadevergoeding te vorderen. Deze boete is rechtsreeks verschuldigd aan 
werkgever en strekt deze tot voordeel. Hiermee wordt nadrukkelijk afgeweken van het bepaalde 
in artikel 7:650 leden 3 en 5 BW.
```

---

#### **Artikel 8: Vakantiedagen, ATV en vakantiebijslag**

**Para 1**:
```
Werknemers van {{VacationAgeGroup}} hebben recht op {{VacationDays}} vakantiedagen per 
kalenderjaar ({{StatutoryVacationDays}} wettelijke en {{ExtraVacationDays}} dagen 
bovenwettelijke) met behoud van salaris.
```

**Para 2** (static):
```
Bij andere leeftijden gelden andere rechten, zie hiervoor artilkel 67a van het cao. Van de 
vakantiedagen mogen maximaal drie weken worden opgenomen in een aaneengesloten periode. 
Verlofdagen worden vastgesteld door de werkgever, na overleg met de werknemer. Werkgever 
is bevoegd maximaal 14 dagen per jaar als verplichte vakantiedagen aan te wijzen. Tevens 
is het mogelijk dat een periode (maximaal één maand) wordt aangewezen i.v.m. bedrijfssluiting. 
Deze wordt door werkgever als verplichte vakantie aangewezen. Deze vakantiedagen komen in 
mindering op het vakantietegoed.
```

**Para 3**:
```
Werknemer heeft recht op {{Atv}} ATV dagen per kalenderjaar met behoud van salaris. Per jaar 
heeft de werknemer recht op een vakantiebijslag van {{VacationAllowancePercent}} over het op 
jaarbasis berekende brutosalaris.
```

**Para 4**:
```
Werknemer ontvangt in de maanden mei en november de vakantiebijslag van 
{{VacationAllowancePercent}} naar ratio tezamen met de maandelijkse salaris. Bij tussentijdse 
aanvang of beëindiging van het dienstverband worden de vakantie­rechten naar rato van het 
aantal maanden dat werknemer in dienst is, vastgesteld.
```

---

#### **Artikel 9: Arbeids- en bedrijfsregels** (static)

```
Werknemer verklaart op de hoogte te zijn van en in te stemmen met de bij werkgever geldende 
arbeids- en bedrijfsregels en een exemplaar van deze arbeids- en bedrijfsregels te hebben 
ontvangen.
```

---

#### **Artikel 10: Schadeclausule bij niet nakomen van verplichtingen** (static)

```
Indien werknemer om wat voor reden dan ook de arbeidsovereenkomst c.q. de werkzaamheden 
eenzijdig beëindigt zonder hier werkgever van tevoren van op de hoogte te stellen of zich 
niet houdt aan de geldende opzegtermijn is werknemer schadeplichtig. In afwijking van 
art. 7:650 leden 3 en 5 BW zal werknemer in dat geval een onmiddellijk opeisbare tot voordeel 
van de werkgever strekkende boete verbeuren van € 250,00 ineens (welke direct al wordt 
ingehouden op het salaris van werknemer) onverminderd het recht van werkgever om in plaats 
van deze boete, schadevergoeding te vorderen. Eveneens staat de werknemer de inhouding van 
de schadevergoeding op het loon toe rekening houdend met WML.
```

---

#### **Artikel 11: Wijzigingen** (static)

```
Werkgever behoudt zicht het recht voor om éénzijdig de arbeidsvoorwaarden te ­wijzigen, met 
inachtneming van hetgeen bepaald is in artikel 7:613 van het Burgerlijk Wetboek. Dit indien 
hij daarbij een zodanig zwaarwichtig belang heeft dat het belang van de werknemer dat door 
de wijziging wordt geschaad, daardoor naar maatstaven van redelijkheid en billijkheid moet 
wijken. Bij wijziging van fiscale regelgeving en/of fiscale besluiten waardoor een in deze 
overeenkomst genoemde fiscale faciliteit wordt gewijzigd, beperkt dan wel komt te vervallen 
zal de desbetreffende bepaling in deze overeenkomst overeenkomstig worden aangepast.
```

---

#### **Artikel 12: Geheimhoudingsbeding** (static)

```
Werknemer zal zowel gedurende als na afloop van deze arbeidsovereenkomst strikte 
geheimhouding betrachten omtrent al hetgeen bij de uitoefening van zijn/haar functie te 
zijner kennis komt in verband met zaken en belangen van de onderneming van werkgever of een 
met haar gelieerde onderneming. Deze plicht tot geheimhouding omvat eveneens alle gegevens 
c.q. bijzonderheden van relaties en cliënten van werkgever, waarvan werknemer uit hoofde van 
zijn/haar functie kennisneemt.
```

---

#### **Artikel 13: Teruggavebeding** (static)

```
Alle stukken en andere schriftelijke informatie en alle hard- en software die werknemer in 
het kader van zijn werkzaamheden heeft gebruikt en eigendom zijn van werkgever of welke 
werknemer in het kader van zijn werkzaamheden heeft vervaardigd, blijven/worden eigendom van 
werkgever. Werknemer dient deze bescheiden aan werkgever te retourneren indien werkgever 
daarom vraagt en/of indien deze arbeidsovereenkomst eindigt, zonder dat werknemer hiervan op 
enigerlei wijze een kopie mag behouden en/of aan derden mag verstrekken. Bij beëindiging van 
deze arbeidsovereenkomst is werknemer verplicht om alle door werkgever ter beschikking 
gestelde bedrijfseigendommen onmiddellijk aan werkgever terug te geven.
```

---

#### **Artikel 14: Gedragscode voor internet-, e-mail- en social media-gebruik** (static)

```
Werkgever hanteert een gedragscode voor internet-, e-mail- en social media-gebruik waarin 
voorschriften zijn opgenomen ten aanzien van het internet- en e-mailgebruik en (zakelijk) 
gebruik van social media. Werknemer heeft een exemplaar van de gedragscode voor internet-, 
e-mail- en social media-gebruik ontvangen en verklaart zich door ondertekening van deze 
arbeidsovereenkomst akkoord met de inhoud van deze gedragscode.
```

---

#### **Artikel 15: Slotbepalingen** (static)

**Para 1**:
```
Nederlands recht is van toepassing op deze arbeidsovereenkomst, alsmede op alle geschillen 
die samenhangen met of voortvloeien uit deze arbeidsovereenkomst. De van toepassing zijnde 
cao is Beroepsgoederenvervoer.
```

**Para 2**:
```
Werknemer verklaart een getekend exemplaar van dit contract en een geparafeerde exemplaar 
de arbeids- en bedrijfsregels te hebben ont­vangen.
```

---

### **Signature Section**

```
Aldus overeengekomen en in tweevoud getekend te {{CompanyCity}} op {{ContractDate|dd mmmm yyyy}}.

Werkgever: {{CompanyName}}
Namens deze: {{ContactPersonName}}

Handtekening werkgever: _______________________


Werknemer: {{EmployeeFirstName}} {{EmployeeLastName}}

Handtekening werknemer: _______________________
```

---

## Variable Mapping

| Variable | Source | Notes |
|----------|--------|-------|
| `CompanyName` | `EmployeeContract.CompanyName` | |
| `CompanyAddress` | `EmployeeContract.CompanyAddress` | |
| `CompanyPostcode` | `EmployeeContract.CompanyPostcode` | |
| `CompanyCity` | `EmployeeContract.CompanyCity` | |
| `ContactPersonName` | `ApplicationUser[CreatedByUserId].FirstName + LastName` | User who created contract |
| `EmployeeFirstName` | `EmployeeContract.EmployeeFirstName` | |
| `EmployeeLastName` | `EmployeeContract.EmployeeLastName` | |
| `EmployeeAddress` | `EmployeeContract.EmployeeAddress` | |
| `EmployeePostcode` | `EmployeeContract.EmployeePostcode` | |
| `EmployeeCity` | `EmployeeContract.EmployeeCity` | |
| `DateOfBirth` | `EmployeeContract.DateOfBirth` | Format: dd-MM-yyyy |
| `Bsn` | `EmployeeContract.Bsn` | |
| `DateOfEmployment` | `EmployeeContract.DateOfEmployment` | Format: dd mmmm yyyy |
| `LastWorkingDay` | `EmployeeContract.LastWorkingDay` | Optional, format: dd mmmm yyyy |
| `Function` | `EmployeeContract.Function` | e.g., "Chauffeur" |
| `ProbationPeriod` | `EmployeeContract.ProbationPeriod` | e.g., "1 maand" |
| `WorkweekDuration` | `EmployeeContract.WorkweekDuration` | e.g., 40 |
| `WeeklySchedule` | `EmployeeContract.WeeklySchedule` | e.g., "maandag t/m vrijdag" |
| `WorkingHours` | `EmployeeContract.WorkingHours` | e.g., "7:00 uur t/m 19:00 uur" |
| `PayScale` | `EmployeeContract.PayScale` | e.g., "D" |
| `PayScaleStep` | `EmployeeContract.PayScaleStep` | e.g., 5 |
| `HourlyWage100Percent` | `EmployeeContract.HourlyWage100Percent` OR lookup | Format: 0.00 |
| `TravelExpenses` | `EmployeeContract.TravelExpenses` | Format: 0.00 |
| `MaxTravelExpenses` | `EmployeeContract.MaxTravelExpenses` | Format: 0.00 |
| `VacationAgeGroup` | **CALCULATED** from age | e.g., "45 t/m 49 jaar" |
| `VacationDays` | `EmployeeContract.VacationDays` OR lookup | e.g., 25 |
| `StatutoryVacationDays` | **CALCULATED**: VacationDays - 4 | e.g., 21 |
| `ExtraVacationDays` | **FIXED**: 4 | Always 4 |
| `Atv` | `EmployeeContract.Atv` | e.g., 3.5 |
| `VacationAllowancePercent` | `EmployeeContract.VacationAllowance` | Format: 8% |
| `ContractDate` | `EmployeeContract.CreatedAt` | Date contract was created, Format: dd mmmm yyyy |
| `ContractType` | **CALCULATED**: LastWorkingDay ? "BEPAALDE" : "ONBEPAALDE" | |

---

## Date/Number Formatting

### Date Formats
- `dd-MM-yyyy`: Day-Month-Year (e.g., 18-11-2025)
- `dd mmmm yyyy`: Day Month(full name) Year in Dutch (e.g., 18 november 2025)

### Number Formats
- `0.00`: Decimal with 2 places (e.g., 18.71)
- `0.00%`: Percentage (e.g., 8.00%)

### Dutch Month Names
```
januari, februari, maart, april, mei, juni, 
juli, augustus, september, oktober, november, december
```

---

## Implementation Notes

1. **Gender neutrality**: Template uses "werknemer" (gender-neutral) throughout
2. **CAO compliance**: Contract references "Beroepsgoederenvervoer" CAO throughout
3. **Legal validity**: All articles are legally binding under Dutch law
4. **Signature requirements**: Contract requires both employer and employee signatures
5. **Document language**: All content in Dutch (nl-NL)
6. **PDF generation**: Will need proper Dutch character encoding (UTF-8)

---

## Next Steps for Implementation

### 1. Update EmployeeContract Entity
Add two new fields:
```csharp
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
public string? CreatedByUserId { get; set; }
```

### 2. Update Contract Creation Logic
When creating a contract (POST /drivers/create-with-contract or POST /employee-contracts):
```csharp
var contract = new EmployeeContract
{
    // ... existing fields ...
    CreatedAt = DateTime.UtcNow,
    CreatedByUserId = userManager.GetUserId(currentUser)
};
```

### 3. Create CAO Lookup Tables
- Create `CAOPayScale` entity
- Create `CAOVacationDays` entity
- Create migrations
- Seed data from CSV files in `TruckManagement/Docs/`

### 4. Build Contract Generation Service
- Create `ContractGenerationService` class
- Implement variable resolution logic
- Implement template engine (Handlebars, Scriban, or custom)
- Add PDF generation using QuestPDF or similar

### 5. Create API Endpoints
- `GET /drivers/{id}/contract/pdf` - Generate and download contract PDF
- `GET /drivers/{id}/contract/preview` - Preview contract as HTML

---

## Key Points for Implementation

### ContactPersonName Resolution
```csharp
// At contract generation time:
var createdByUser = await userManager.FindByIdAsync(contract.CreatedByUserId);
var contactPersonName = createdByUser != null 
    ? $"{createdByUser.FirstName} {createdByUser.LastName}"
    : contract.CompanyName; // fallback
```

### ContractDate Usage
```csharp
// Use CreatedAt for the contract date:
var contractDate = contract.CreatedAt.ToString("dd MMMM yyyy", new CultureInfo("nl-NL"));
// Example output: "18 november 2025"
```

### Important Notes
- **CreatedAt**: Set once when contract is created, never changes (even if contract is edited)
- **SignedAt**: Set when driver signs the contract (different from CreatedAt)
- **CreatedByUserId**: Stores who created the contract for accountability and signature purposes
- Both fields should be included in all contract DTOs (Get, List, etc.)

