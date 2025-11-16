using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TruckManagement.Entities;

namespace TruckManagement.Services
{
    /// <summary>
    /// Builds driver weekly invoice PDFs.
    /// Generates invoices in Dutch for drivers to submit to the company.
    /// </summary>
    public class DriverInvoicePdfBuilder
    {
        private readonly CultureInfo _dutchCulture;
        
        // Define consistent colors for the invoice (matching contract style)
        private static readonly string HeaderColor = "#2563eb"; // Blue
        private static readonly string AccentColor = "#f3f4f6"; // Light gray for accents
        private static readonly string BorderColor = "#d1d5db"; // Medium gray
        private static readonly string TextColor = "#374151"; // Dark gray
        private static readonly string LightTextColor = "#6b7280"; // Lighter gray for labels
        private static readonly string TableHeaderColor = "#f3f4f6"; // Light gray
        
        public DriverInvoicePdfBuilder()
        {
            _dutchCulture = new CultureInfo("nl-NL");
            
            // Configure QuestPDF license (Community edition)
            QuestPDF.Settings.License = LicenseType.Community;
        }
        
        /// <summary>
        /// Generates a driver weekly invoice PDF.
        /// </summary>
        public byte[] BuildInvoicePdf(
            Driver driver,
            ApplicationUser driverUser,
            Company company,
            decimal hourlyRate,
            int year,
            int weekNumber,
            decimal hoursWorked,
            decimal hourlyCompensation,
            decimal additionalCompensation,
            decimal exceedingContainerWaitingTime)
        {
            var totalAmount = hourlyCompensation + additionalCompensation;
            var invoiceDate = DateTime.UtcNow;
            
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
                        column.Item().Element(ComposeTitle);
                        
                        // From/To section
                        column.Item().PaddingTop(20).Element(c => ComposeFromTo(c, driverUser, driver, company));
                        
                        // Invoice details (Date and Week)
                        column.Item().PaddingTop(20).Element(c => ComposeInvoiceDetails(c, invoiceDate, year, weekNumber));
                        
                        // Line items table (includes total)
                        column.Item().PaddingTop(20).Element(c => ComposeLineItemsTable(
                            c, 
                            hoursWorked, 
                            hourlyRate, 
                            hourlyCompensation, 
                            additionalCompensation, 
                            exceedingContainerWaitingTime));
                        
                        // Payment terms
                        column.Item().PaddingTop(25).AlignCenter().Text("Betaling binnen 14 dagen")
                            .FontSize(10).FontColor(LightTextColor);
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
        
        private void ComposeTitle(IContainer container)
        {
            container.Background(HeaderColor).Padding(15).Column(column =>
            {
                column.Item().AlignCenter().Text("FACTUUR")
                    .FontSize(18).Bold().FontColor(Colors.White);
            });
        }
        
        private void ComposeFromTo(IContainer container, ApplicationUser driverUser, Driver driver, Company company)
        {
            container.Border(1).BorderColor(BorderColor).Padding(15).Row(row =>
            {
                // Left column: VAN (FROM) - Driver details
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("VAN:").FontSize(11).Bold().FontColor(HeaderColor);
                    column.Item().PaddingTop(8).Text($"{driverUser.FirstName} {driverUser.LastName}").FontSize(10).Bold();
                    
                    // Use driver's address from user or from latest contract
                    if (!string.IsNullOrWhiteSpace(driverUser.Address))
                    {
                        column.Item().PaddingTop(4).Text(driverUser.Address).FontSize(10);
                        column.Item().PaddingTop(4).Text($"{driverUser.Postcode} {driverUser.City}").FontSize(10);
                    }
                    
                    column.Item().PaddingTop(4).Text("Nederland").FontSize(10);
                    
                    // BSN is not directly on Driver entity - skipping for now
                    // If needed, it can be retrieved from the driver's employee contract
                });
                
                // Spacing
                row.ConstantItem(30);
                
                // Right column: AAN (TO) - Company details
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("AAN:").FontSize(11).Bold().FontColor(HeaderColor);
                    column.Item().PaddingTop(8).Text(company.Name ?? "").FontSize(10).Bold();
                    
                    if (!string.IsNullOrWhiteSpace(company.Address))
                        column.Item().PaddingTop(4).Text(company.Address).FontSize(10);
                    
                    if (!string.IsNullOrWhiteSpace(company.Postcode) || !string.IsNullOrWhiteSpace(company.City))
                        column.Item().PaddingTop(4).Text($"{company.Postcode} {company.City}").FontSize(10);
                    
                    if (!string.IsNullOrWhiteSpace(company.Country))
                        column.Item().PaddingTop(4).Text(company.Country).FontSize(10);
                    
                    if (!string.IsNullOrWhiteSpace(company.PhoneNumber))
                        column.Item().PaddingTop(4).Text($"Tel: {company.PhoneNumber}").FontSize(10);
                    
                    if (!string.IsNullOrWhiteSpace(company.Email))
                        column.Item().PaddingTop(4).Text($"Email: {company.Email}").FontSize(10);
                });
            });
        }
        
        private void ComposeInvoiceDetails(IContainer container, DateTime invoiceDate, int year, int weekNumber)
        {
            container.Border(1).BorderColor(BorderColor).Padding(15).Column(column =>
            {
                column.Item().Text($"Factuurdatum: {FormatDateLong(invoiceDate)}")
                    .FontSize(10);
                column.Item().PaddingTop(4).Text($"Week: Week {weekNumber}, {year}")
                    .FontSize(10).Bold();
            });
        }
        
        private void ComposeLineItemsTable(
            IContainer container, 
            decimal hoursWorked, 
            decimal hourlyRate, 
            decimal hourlyCompensation, 
            decimal additionalCompensation, 
            decimal exceedingContainerWaitingTime)
        {
            var totalAmount = hourlyCompensation + additionalCompensation;
            
            container.Border(1).BorderColor(BorderColor).Table(table =>
            {
                // Define 2 columns: Description and Amount
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Description
                    columns.RelativeColumn(1); // Amount
                });
                
                // Header row
                table.Header(header =>
                {
                    header.Cell().Background(TableHeaderColor).Padding(10).Text("Omschrijving")
                        .FontSize(11).Bold();
                    header.Cell().Background(TableHeaderColor).Padding(10).AlignRight().Text("Bedrag")
                        .FontSize(11).Bold();
                });
                
                // Row 1: Hourly wage (with detail)
                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(10)
                    .Text($"Uurloon ({FormatDecimal(hoursWorked)} uren × € {FormatCurrency(hourlyRate)})").FontSize(10);
                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(10)
                    .AlignRight().Text($"€ {FormatCurrency(hourlyCompensation)}").FontSize(10);
                
                // Row 2: Additional compensation
                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(10)
                    .Text("Aanvullende vergoeding").FontSize(10);
                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(10)
                    .AlignRight().Text($"€ {FormatCurrency(additionalCompensation)}").FontSize(10);
                
                // Row 3: Exceeding container waiting time (informational only, lighter style)
                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(10)
                    .Text($"Wachttijd container >2u ({FormatDecimal(exceedingContainerWaitingTime)} uur)")
                    .FontSize(9).FontColor(LightTextColor);
                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(10)
                    .AlignRight().Text("(informatief)").FontSize(9).FontColor(LightTextColor);
                
                // TOTAL ROW
                table.Cell().Background(AccentColor).Padding(12)
                    .Text("TE BETALEN").FontSize(12).Bold();
                table.Cell().Background(AccentColor).Padding(12)
                    .AlignRight().Text($"€ {FormatCurrency(totalAmount)}").FontSize(12).Bold().FontColor(HeaderColor);
            });
        }
        
        private void ComposeTotalSection(IContainer container, decimal totalAmount)
        {
            container.Border(1).BorderColor(BorderColor).Padding(15).Column(column =>
            {
                column.Item().AlignRight().Text($"Subtotaal: € {FormatCurrency(totalAmount)}")
                    .FontSize(11);
                column.Item().PaddingTop(10).AlignRight().Text($"TOTAAL: € {FormatCurrency(totalAmount)}")
                    .FontSize(14).Bold().FontColor(HeaderColor);
            });
        }
        
        // Helper methods for formatting
        
        private string FormatDateLong(DateTime date)
        {
            return date.ToString("dd MMMM yyyy", _dutchCulture);
        }
        
        private string FormatCurrency(decimal amount)
        {
            return amount.ToString("N2", _dutchCulture);
        }
        
        private string FormatDecimal(decimal value)
        {
            return value.ToString("0.##", _dutchCulture);
        }
    }
}

