using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TruckManagement.Entities;

namespace TruckManagement.Services
{
    /// <summary>
    /// Builds driver employment contract PDFs based on the TLN CAO Beroepsgoederenvervoer template.
    /// Generates legally compliant Dutch employment contracts with all required articles and signatures.
    /// </summary>
    public class DriverContractPdfBuilder
    {
        private readonly CultureInfo _dutchCulture;
        
        // Define consistent colors for the contract (matching existing report style)
        private static readonly string HeaderColor = "#2563eb"; // Blue
        private static readonly string AccentColor = "#f3f4f6"; // Light gray
        private static readonly string BorderColor = "#d1d5db"; // Medium gray
        private static readonly string TextColor = "#374151"; // Dark gray
        private static readonly string LightTextColor = "#6b7280"; // Lighter gray for labels
        
        public DriverContractPdfBuilder()
        {
            _dutchCulture = new CultureInfo("nl-NL");
            
            // Configure QuestPDF license (Community edition)
            QuestPDF.Settings.License = LicenseType.Community;
        }
        
        /// <summary>
        /// Generates a driver employment contract PDF.
        /// </summary>
        /// <param name="contract">The employee contract data</param>
        /// <param name="payScale">CAO pay scale information</param>
        /// <param name="vacationDays">CAO vacation days information</param>
        /// <param name="createdByUser">User who created the contract (for signature)</param>
        /// <returns>PDF file as byte array</returns>
        public byte[] BuildContractPdf(
            EmployeeContract contract,
            CAOPayScale payScale,
            CAOVacationDays vacationDays,
            ApplicationUser? createdByUser)
        {
            // Calculate dynamic fields
            var contractType = contract.LastWorkingDay.HasValue ? "BEPAALDE" : "ONBEPAALDE";
            var contactPersonName = createdByUser != null 
                ? $"{createdByUser.FirstName} {createdByUser.LastName}" 
                : contract.CompanyName;
            var statutoryVacationDays = (contract.VacationDays ?? 0) - 4;
            var extraVacationDays = 4;
            var vacationAllowancePercent = ((contract.VacationAllowance ?? 0) * 100).ToString("0", _dutchCulture) + "%";
            
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2.5f, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial).FontColor(TextColor));
                    
                    page.Content().Column(column =>
                    {
                        // Title
                        column.Item().Element(c => ComposeTitle(c, contractType));
                        
                        // Header section (Werkgever and Werknemer info)
                        column.Item().PaddingTop(20).Element(c => ComposeHeader(c, contract, contactPersonName));
                        
                        // "zijn het volgende overeengekomen:"
                        column.Item().PaddingTop(15).Text("zijn het volgende overeengekomen:")
                            .FontSize(11).Italic();
                        
                        // Articles 1-15
                        column.Item().PaddingTop(20).Element(c => ComposeArticle1(c, contract));
                        column.Item().PaddingTop(12).Element(c => ComposeArticle2(c, contract));
                        column.Item().PaddingTop(12).Element(c => ComposeArticle3(c, contract));
                        column.Item().PaddingTop(12).Element(c => ComposeArticle4(c, contract, payScale));
                        column.Item().PaddingTop(12).Element(c => ComposeArticle5(c, contract));
                        column.Item().PaddingTop(12).Element(c => ComposeArticle6(c));
                        column.Item().PaddingTop(12).Element(c => ComposeArticle7(c));
                        column.Item().PaddingTop(12).Element(c => ComposeArticle8(c, contract, vacationDays, statutoryVacationDays, extraVacationDays, vacationAllowancePercent));
                        column.Item().PaddingTop(12).Element(c => ComposeArticle9(c));
                        column.Item().PaddingTop(12).Element(c => ComposeArticle10(c));
                        column.Item().PaddingTop(12).Element(c => ComposeArticle11(c));
                        column.Item().PaddingTop(12).Element(c => ComposeArticle12(c));
                        column.Item().PaddingTop(12).Element(c => ComposeArticle13(c));
                        column.Item().PaddingTop(12).Element(c => ComposeArticle14(c));
                        column.Item().PaddingTop(12).Element(c => ComposeArticle15(c));
                        
                        // Signature section
                        column.Item().PaddingTop(30).Element(c => ComposeSignature(c, contract, contactPersonName));
                    });
                    
                    // Footer with page numbers
                    page.Footer().AlignCenter().DefaultTextStyle(x => x.FontSize(9).FontColor(LightTextColor)).Text(text =>
                    {
                        text.Span("Pagina ");
                        text.CurrentPageNumber();
                        text.Span(" van ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }
        
        private void ComposeTitle(IContainer container, string contractType)
        {
            container.Background(HeaderColor).Padding(15).Column(column =>
            {
                column.Item().AlignCenter().Text($"ARBEIDSOVEREENKOMST VOOR {contractType} TIJD")
                    .FontSize(16).Bold().FontColor(Colors.White);
            });
        }
        
        private void ComposeHeader(IContainer container, EmployeeContract contract, string contactPersonName)
        {
            container.Border(1).BorderColor(BorderColor).Padding(15).Row(row =>
            {
                // Left column: Werkgever
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("WERKGEVER").FontSize(11).Bold().FontColor(HeaderColor);
                    column.Item().PaddingTop(8).Text($"Bedrijfsnaam: {contract.CompanyName}").FontSize(10);
                    column.Item().PaddingTop(4).Text($"Adres: {contract.CompanyAddress}").FontSize(10);
                    column.Item().PaddingTop(4).Text($"Postcode/Plaats: {contract.CompanyPostcode} {contract.CompanyCity}").FontSize(10);
                    column.Item().PaddingTop(4).Text($"Contact: {contactPersonName}").FontSize(10);
                });
                
                // Spacing
                row.ConstantItem(30);
                
                // Right column: Werknemer
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("WERKNEMER").FontSize(11).Bold().FontColor(HeaderColor);
                    column.Item().PaddingTop(8).Text($"Naam: {contract.EmployeeFirstName} {contract.EmployeeLastName}").FontSize(10);
                    column.Item().PaddingTop(4).Text($"Adres: {contract.EmployeeAddress}").FontSize(10);
                    column.Item().PaddingTop(4).Text($"Postcode/Plaats: {contract.EmployeePostcode} {contract.EmployeeCity}").FontSize(10);
                    column.Item().PaddingTop(4).Text($"Geb.datum: {(contract.DateOfBirth.HasValue ? FormatDateShort(contract.DateOfBirth.Value.ToString("yyyy-MM-dd")) : "")}").FontSize(10);
                    column.Item().PaddingTop(4).Text($"BSN: {contract.Bsn}").FontSize(10);
                });
            });
        }
        
        private void ComposeArticle1(IContainer container, EmployeeContract contract)
        {
            container.Column(column =>
            {
                column.Item().Text("Artikel 1: Datum indiensttreding, functie en plaats werkzaamheden")
                    .FontSize(11).Bold().FontColor(HeaderColor);
                
                var lastWorkingDayText = contract.LastWorkingDay.HasValue 
                    ? $" tot {FormatDateLong(contract.LastWorkingDay.Value.ToString("yyyy-MM-dd"))}" 
                    : "";
                
                column.Item().PaddingTop(6).Text($"Er is een dienstverband aangegaan met ingang van: {(contract.DateOfEmployment.HasValue ? FormatDateLong(contract.DateOfEmployment.Value.ToString("yyyy-MM-dd")) : "")}{lastWorkingDayText}.")
                    .FontSize(10).LineHeight(1.4f);
                
                column.Item().PaddingTop(6).Text($"De werknemer is aangenomen voor de functie van {contract.Function}.")
                    .FontSize(10).LineHeight(1.4f);
                
                column.Item().PaddingTop(6).Text("De bij deze functie behorende werkzaamheden bestaan uit: Het vervoeren van goederen over weg en voorts alle werkzaam­heden die redelijkerwijs van hem verlangd kunnen worden. De werkzaamheden worden uitgevoerd vanuit de vestiging van werkgever of opdrachtgevers van werkgever.")
                    .FontSize(10).LineHeight(1.4f);
            });
        }
        
        private void ComposeArticle2(IContainer container, EmployeeContract contract)
        {
            container.Column(column =>
            {
                column.Item().Text("Artikel 2: Proeftijd")
                    .FontSize(11).Bold().FontColor(HeaderColor);
                
                // Check if probation period exists and is not "0" (treat "0" as no probation)
                bool hasProbation = !string.IsNullOrWhiteSpace(contract.ProbationPeriod) 
                                    && contract.ProbationPeriod.Trim() != "0"
                                    && contract.ProbationPeriod.Trim() != "0 maanden"
                                    && contract.ProbationPeriod.Trim() != "0 maand";
                
                if (hasProbation)
                {
                    column.Item().PaddingTop(6).Text($"Dit arbeidscontract is aangegaan met inachtneming van een wederzijdse proeftijd van {contract.ProbationPeriod}. Gedurende deze proeftijd kunnen beide partijen het arbeidscontract met directe ingang beëindigen. Een schriftelijke bevestiging hiervan is niet benodigd. De proeftijd is van toepassing indien een dienstverband voor een minimale duur van 12 maanden is aangegaan.")
                        .FontSize(10).LineHeight(1.4f);
                }
                else
                {
                    column.Item().PaddingTop(6).Text("Er is geen proeftijd bij het aangaan van de dienstverband.")
                        .FontSize(10).LineHeight(1.4f);
                }
            });
        }
        
        private void ComposeArticle3(IContainer container, EmployeeContract contract)
        {
            container.Column(column =>
            {
                column.Item().Text("Artikel 3: Arbeidsduur, werktijden en overwerk")
                    .FontSize(11).Bold().FontColor(HeaderColor);
                
                column.Item().PaddingTop(6).Text($"De wekelijkse arbeidsduur bedraagt {contract.WorkweekDuration} uur per week.")
                    .FontSize(10).LineHeight(1.4f);
                
                column.Item().PaddingTop(6).Text($"Werknemer verklaart op de hoogte te zijn van en in te stemmen met de door werkgever geregelde wekelijkse planning voor {contract.WeeklySchedule}. Werknemer is tevens op de hoogte gesteld dat de werktijden tussen {contract.WorkingHours} kunnen plaatsvinden. Werknemer is redelijkerwijs verplicht gehoor te geven aan een redelijk verzoek van ­werkgever om overwerk te verrichten.")
                    .FontSize(10).LineHeight(1.4f);
            });
        }
        
        private void ComposeArticle4(IContainer container, EmployeeContract contract, CAOPayScale payScale)
        {
            container.Column(column =>
            {
                column.Item().Text("Artikel 4: Salaris")
                    .FontSize(11).Bold().FontColor(HeaderColor);
                
                var hourlyWage = contract.HourlyWage100Percent ?? payScale.HourlyWage100;
                
                column.Item().PaddingTop(6).Text($"Werknemer ontvangt conform de cao en loonschaal {contract.PayScale} en trede {contract.PayScaleStep} een salaris van € {FormatCurrency(hourlyWage)} bruto per uur door werkgever te voldoen voor of op de laatste dag van de maand.")
                    .FontSize(10).LineHeight(1.4f);
            });
        }
        
        private void ComposeArticle5(IContainer container, EmployeeContract contract)
        {
            container.Column(column =>
            {
                column.Item().Text("Artikel 5: Reiskostenvergoeding")
                    .FontSize(11).Bold().FontColor(HeaderColor);
                
                column.Item().PaddingTop(6).Text($"De werknemer ontvangt geen vergoeding voor de reiskosten woon-werkverkeer. Indien achteraf hierover schriftelijk wordt overeengekomen bedraagt de vergoeding maximaal € {FormatCurrency(contract.TravelExpenses ?? 0)} per gereisde kilometer met een maximum reiskostenvergoeding van € {FormatCurrency(contract.MaxTravelExpenses ?? 0)} per maand. De reiskostenvergoeding is onbelast en maakt geen deel uit van het loon van de werknemer.")
                    .FontSize(10).LineHeight(1.4f);
            });
        }
        
        private void ComposeArticle6(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("Artikel 6: Salaris bij arbeidsongeschiktheid en wachtdagen")
                    .FontSize(11).Bold().FontColor(HeaderColor);
                
                column.Item().PaddingTop(6).Text("1. In geval van arbeidsongeschiktheid wegens ziekte, ongeval en dergelijke, zal werkgever, indien en zolang in die periode het dienstverband voortduurt, gedurende de eerste 104 weken van arbeidsongeschiktheid 70% van het brutoloon door­betalen, doch gedurende de eerste 52 weken ten minste het voor werknemer geldende ­minimumloon.")
                    .FontSize(10).LineHeight(1.4f);
                
                column.Item().PaddingTop(6).Text("2. Werknemer verklaart op de hoogte te zijn van en in te stemmen met de door werkgever vastgestelde voorschriften in verband met ziekmelding en controle.")
                    .FontSize(10).LineHeight(1.4f);
                
                column.Item().PaddingTop(6).Text("3. Wegens ziekte of arbeidsongeschiktheid heeft de werknemer gedurende de eerste dag van een ziekteperiode geen recht op loon. Dit geldt niet indien werknemer na het einde van een ziekteperiode binnen vier weken wederom ziek wordt.")
                    .FontSize(10).LineHeight(1.4f);
            });
        }
        
        private void ComposeArticle7(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("Artikel 7: Re-integratie na einde dienstverband")
                    .FontSize(11).Bold().FontColor(HeaderColor);
                
                column.Item().PaddingTop(6).Text("- Werknemer is verplicht zich onmiddellijk ziek te melden bij werkgever conform de door werkgever vastgestelde voorschriften in verband met ziekmelding en controle als werknemer binnen 4 weken na het einde van de arbeidsovereenkomst ziek wordt en op dat moment niet werkzaam is bij een andere werkgever of een WW-uitkering ontvangt.")
                    .FontSize(10).LineHeight(1.4f);
                
                column.Item().PaddingTop(6).Text("- Als werknemer ziek is op het moment dat hij uit dienst gaat en/of als werknemer voldoet aan het bepaalde in lid 1 van dit artikel, dient werknemer zich te blijven houden aan de door werkgever of regelgeving vastgestelde voorschriften in verband met ziekmelding en controle.")
                    .FontSize(10).LineHeight(1.4f);
                
                column.Item().PaddingTop(6).Text("- Indien werknemer bovenstaande van de in dit artikel bepaalde overtreedt, verbeurt hij aan werkgever een direct opeisbare boete van 2.500,- voor iedere overtreding, alsmede een bedrag van 500,- voor iedere dag dat de overtreding voortduurt. De boete zal verschuldigd zijn door het enkele feit van de overtreding of niet-nakoming, maar laat onverminderd het recht om volledige schadevergoeding te vorderen. Deze boete is rechtsreeks verschuldigd aan werkgever en strekt deze tot voordeel. Hiermee wordt nadrukkelijk afgeweken van het bepaalde in artikel 7:650 leden 3 en 5 BW.")
                    .FontSize(10).LineHeight(1.4f);
            });
        }
        
        private void ComposeArticle8(IContainer container, EmployeeContract contract, CAOVacationDays vacationDays, int statutoryVacationDays, int extraVacationDays, string vacationAllowancePercent)
        {
            container.Column(column =>
            {
                column.Item().Text("Artikel 8: Vakantiedagen, ATV en vakantiebijslag")
                    .FontSize(11).Bold().FontColor(HeaderColor);
                
                column.Item().PaddingTop(6).Text($"Werknemers van {vacationDays.AgeGroupDescription} hebben recht op {contract.VacationDays} vakantiedagen per kalenderjaar ({statutoryVacationDays} wettelijke en {extraVacationDays} dagen bovenwettelijke) met behoud van salaris.")
                    .FontSize(10).LineHeight(1.4f);
                
                column.Item().PaddingTop(6).Text("Bij andere leeftijden gelden andere rechten, zie hiervoor artilkel 67a van het cao. Van de vakantiedagen mogen maximaal drie weken worden opgenomen in een aaneengesloten periode. Verlofdagen worden vastgesteld door de werkgever, na overleg met de werknemer. Werkgever is bevoegd maximaal 14 dagen per jaar als verplichte vakantiedagen aan te wijzen. Tevens is het mogelijk dat een periode (maximaal één maand) wordt aangewezen i.v.m. bedrijfssluiting. Deze wordt door werkgever als verplichte vakantie aangewezen. Deze vakantiedagen komen in mindering op het vakantietegoed.")
                    .FontSize(10).LineHeight(1.4f);
                
                column.Item().PaddingTop(6).Text($"Werknemer heeft recht op {contract.Atv} ATV dagen per kalenderjaar met behoud van salaris. Per jaar heeft de werknemer recht op een vakantiebijslag van {vacationAllowancePercent} over het op jaarbasis berekende brutosalaris.")
                    .FontSize(10).LineHeight(1.4f);
                
                column.Item().PaddingTop(6).Text($"Werknemer ontvangt in de maanden mei en november de vakantiebijslag van {vacationAllowancePercent} naar ratio tezamen met de maandelijkse salaris. Bij tussentijdse aanvang of beëindiging van het dienstverband worden de vakantie­rechten naar rato van het aantal maanden dat werknemer in dienst is, vastgesteld.")
                    .FontSize(10).LineHeight(1.4f);
            });
        }
        
        private void ComposeArticle9(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("Artikel 9: Arbeids- en bedrijfsregels")
                    .FontSize(11).Bold().FontColor(HeaderColor);
                
                column.Item().PaddingTop(6).Text("Werknemer verklaart op de hoogte te zijn van en in te stemmen met de bij werkgever geldende arbeids- en bedrijfsregels en een exemplaar van deze arbeids- en bedrijfsregels te hebben ontvangen.")
                    .FontSize(10).LineHeight(1.4f);
            });
        }
        
        private void ComposeArticle10(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("Artikel 10: Schadeclausule bij niet nakomen van verplichtingen")
                    .FontSize(11).Bold().FontColor(HeaderColor);
                
                column.Item().PaddingTop(6).Text("Indien werknemer om wat voor reden dan ook de arbeidsovereenkomst c.q. de werkzaamheden eenzijdig beëindigt zonder hier werkgever van tevoren van op de hoogte te stellen of zich niet houdt aan de geldende opzegtermijn is werknemer schadeplichtig. In afwijking van art. 7:650 leden 3 en 5 BW zal werknemer in dat geval een onmiddellijk opeisbare tot voordeel van de werkgever strekkende boete verbeuren van € 250,00 ineens (welke direct al wordt ingehouden op het salaris van werknemer) onverminderd het recht van werkgever om in plaats van deze boete, schadevergoeding te vorderen. Eveneens staat de werknemer de inhouding van de schadevergoeding op het loon toe rekening houdend met WML.")
                    .FontSize(10).LineHeight(1.4f);
            });
        }
        
        private void ComposeArticle11(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("Artikel 11: Wijzigingen")
                    .FontSize(11).Bold().FontColor(HeaderColor);
                
                column.Item().PaddingTop(6).Text("Werkgever behoudt zicht het recht voor om éénzijdig de arbeidsvoorwaarden te ­wijzigen, met inachtneming van hetgeen bepaald is in artikel 7:613 van het Burgerlijk Wetboek. Dit indien hij daarbij een zodanig zwaarwichtig belang heeft dat het belang van de werknemer dat door de wijziging wordt geschaad, daardoor naar maatstaven van redelijkheid en billijkheid moet wijken. Bij wijziging van fiscale regelgeving en/of fiscale besluiten waardoor een in deze overeenkomst genoemde fiscale faciliteit wordt gewijzigd, beperkt dan wel komt te vervallen zal de desbetreffende bepaling in deze overeenkomst overeenkomstig worden aangepast.")
                    .FontSize(10).LineHeight(1.4f);
            });
        }
        
        private void ComposeArticle12(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("Artikel 12: Geheimhoudingsbeding")
                    .FontSize(11).Bold().FontColor(HeaderColor);
                
                column.Item().PaddingTop(6).Text("Werknemer zal zowel gedurende als na afloop van deze arbeidsovereenkomst strikte geheimhouding betrachten omtrent al hetgeen bij de uitoefening van zijn/haar functie te zijner kennis komt in verband met zaken en belangen van de onderneming van werkgever of een met haar gelieerde onderneming. Deze plicht tot geheimhouding omvat eveneens alle gegevens c.q. bijzonderheden van relaties en cliënten van werkgever, waarvan werknemer uit hoofde van zijn/haar functie kennisneemt.")
                    .FontSize(10).LineHeight(1.4f);
            });
        }
        
        private void ComposeArticle13(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("Artikel 13: Teruggavebeding")
                    .FontSize(11).Bold().FontColor(HeaderColor);
                
                column.Item().PaddingTop(6).Text("Alle stukken en andere schriftelijke informatie en alle hard- en software die werknemer in het kader van zijn werkzaamheden heeft gebruikt en eigendom zijn van werkgever of welke werknemer in het kader van zijn werkzaamheden heeft vervaardigd, blijven/worden eigendom van werkgever. Werknemer dient deze bescheiden aan werkgever te retourneren indien werkgever daarom vraagt en/of indien deze arbeidsovereenkomst eindigt, zonder dat werknemer hiervan op enigerlei wijze een kopie mag behouden en/of aan derden mag verstrekken. Bij beëindiging van deze arbeidsovereenkomst is werknemer verplicht om alle door werkgever ter beschikking gestelde bedrijfseigendommen onmiddellijk aan werkgever terug te geven.")
                    .FontSize(10).LineHeight(1.4f);
            });
        }
        
        private void ComposeArticle14(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("Artikel 14: Gedragscode voor internet-, e-mail- en social media-gebruik")
                    .FontSize(11).Bold().FontColor(HeaderColor);
                
                column.Item().PaddingTop(6).Text("Werkgever hanteert een gedragscode voor internet-, e-mail- en social media-gebruik waarin voorschriften zijn opgenomen ten aanzien van het internet- en e-mailgebruik en (zakelijk) gebruik van social media. Werknemer heeft een exemplaar van de gedragscode voor internet-, e-mail- en social media-gebruik ontvangen en verklaart zich door ondertekening van deze arbeidsovereenkomst akkoord met de inhoud van deze gedragscode.")
                    .FontSize(10).LineHeight(1.4f);
            });
        }
        
        private void ComposeArticle15(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("Artikel 15: Slotbepalingen")
                    .FontSize(11).Bold().FontColor(HeaderColor);
                
                column.Item().PaddingTop(6).Text("Nederlands recht is van toepassing op deze arbeidsovereenkomst, alsmede op alle geschillen die samenhangen met of voortvloeien uit deze arbeidsovereenkomst. De van toepassing zijnde cao is Beroepsgoederenvervoer.")
                    .FontSize(10).LineHeight(1.4f);
                
                column.Item().PaddingTop(6).Text("Werknemer verklaart een getekend exemplaar van dit contract en een geparafeerde exemplaar de arbeids- en bedrijfsregels te hebben ont­vangen.")
                    .FontSize(10).LineHeight(1.4f);
            });
        }
        
        private void ComposeSignature(IContainer container, EmployeeContract contract, string contactPersonName)
        {
            container.Column(column =>
            {
                column.Item().Text($"Aldus overeengekomen en in tweevoud getekend te {contract.CompanyCity} op {FormatDateLong(contract.CreatedAt.ToString("yyyy-MM-dd"))}.")
                    .FontSize(10).LineHeight(1.4f);
                
                column.Item().PaddingTop(20).Row(row =>
                {
                    // Werkgever signature
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Werkgever: {contract.CompanyName}").FontSize(10).Bold();
                        col.Item().PaddingTop(4).Text($"Namens deze: {contactPersonName}").FontSize(10);
                        col.Item().PaddingTop(15).Border(1).BorderColor(BorderColor).Height(50);
                        col.Item().AlignCenter().PaddingTop(4).Text("Handtekening werkgever").FontSize(9).FontColor(LightTextColor);
                    });
                    
                    row.ConstantItem(30);
                    
                    // Werknemer signature
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text($"Werknemer: {contract.EmployeeFirstName} {contract.EmployeeLastName}").FontSize(10).Bold();
                        col.Item().PaddingTop(23); // Match spacing with other column
                        col.Item().Border(1).BorderColor(BorderColor).Height(50);
                        col.Item().AlignCenter().PaddingTop(4).Text("Handtekening werknemer").FontSize(9).FontColor(LightTextColor);
                    });
                });
            });
        }
        
        // Helper methods for date/number formatting
        
        private string FormatDateShort(string? date)
        {
            if (string.IsNullOrWhiteSpace(date)) return "";
            
            if (DateTime.TryParse(date, out var dt))
            {
                return dt.ToString("dd-MM-yyyy", _dutchCulture);
            }
            return date;
        }
        
        private string FormatDateLong(string? date)
        {
            if (string.IsNullOrWhiteSpace(date)) return "";
            
            if (DateTime.TryParse(date, out var dt))
            {
                return dt.ToString("dd MMMM yyyy", _dutchCulture);
            }
            return date;
        }
        
        private string FormatCurrency(decimal amount)
        {
            return amount.ToString("N2", _dutchCulture);
        }
    }
}


