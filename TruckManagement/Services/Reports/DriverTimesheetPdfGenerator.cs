using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TruckManagement.DTOs.Reports;

namespace TruckManagement.Services.Reports;

public class DriverTimesheetPdfGenerator
{
    private readonly CultureInfo _dutchCulture;
    
    // Define consistent colors for the report
    private static readonly string HeaderColor = "#2563eb"; // Blue
    private static readonly string AccentColor = "#f3f4f6"; // Light gray
    private static readonly string BorderColor = "#d1d5db"; // Medium gray
    private static readonly string TextColor = "#374151"; // Dark gray
    
    public DriverTimesheetPdfGenerator()
    {
        _dutchCulture = new CultureInfo("nl-NL");
        
        // Configure QuestPDF license (Community edition)
        QuestPDF.Settings.License = LicenseType.Community;
    }
    
    public byte[] GenerateReportPdf(DriverTimesheetReport report)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial).FontColor(TextColor));
                
                page.Content().Column(column =>
                {
                    // Header section with better styling
                    column.Item().Element(c => ComposeHeader(c, report));
                    
                    // Employee info and hours summary section (side by side)
                    column.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeItem(1).Element(c => ComposeEmployeeInfo(c, report.EmployeeInfo));
                        row.ConstantItem(30); // More space between sections
                        row.RelativeItem(1).Element(c => ComposeHoursSummary(c, report.HoursSummary));
                    });
                    
                    // Vacation and TvT section (side by side)
                    column.Item().PaddingTop(15).Row(row =>
                    {
                        row.RelativeItem(1).Element(c => ComposeVacationSection(c, report.Vacation));
                        row.ConstantItem(30); // More space between sections
                        row.RelativeItem(1).Element(c => ComposeTvTSection(c, report.TimeForTime));
                    });
                    
                    // Daily breakdown table (main content)
                    column.Item().PaddingTop(20).Element(c => ComposeDailyBreakdown(c, report));
                    
                    // Grand total with better styling
                    column.Item().PaddingTop(15).Element(c => ComposeGrandTotal(c, report.GrandTotal));
                    
                    // Signature line
                    column.Item().PaddingTop(25).Element(c => ComposeSignature(c, report));
                });
            });
        }).GeneratePdf();
    }
    
    private void ComposeHeader(IContainer container, DriverTimesheetReport report)
    {
        container.Background(HeaderColor).Padding(15).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("URENVERANTWOORDING")
                    .FontSize(18).Bold().FontColor(Colors.White);
                    
                column.Item().PaddingTop(5).Text($"{report.PeriodRange}")
                    .FontSize(12).FontColor(Colors.White);
                    
                column.Item().Text($"Jaar: {report.Year} | Periode: {report.PeriodNumber}")
                    .FontSize(10).FontColor(Colors.White);
            });
            
            row.RelativeItem().AlignCenter().Column(column =>
            {
                column.Item().Text(report.CompanyName)
                    .FontSize(16).Bold().FontColor(Colors.White);
            });
            
            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().Text($"Personeel naam:")
                    .FontSize(9).FontColor(Colors.White);
                column.Item().Text($"{report.DriverName}")
                    .FontSize(11).Bold().FontColor(Colors.White);
                    
                column.Item().PaddingTop(8).Text($"Gegenereerd:")
                    .FontSize(8).FontColor(Colors.White);
                    
                // Convert to GMT+2 (Central European Time)
                var centralEuropeanTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Central European Standard Time");
                column.Item().Text($"{centralEuropeanTime:dd/MM/yyyy HH:mm}")
                    .FontSize(8).FontColor(Colors.White);
            });
        });
    }
    
    private void ComposeEmployeeInfo(IContainer container, EmployeeInfoSection employeeInfo)
    {
        container.Border(1).BorderColor(BorderColor).Padding(10).Column(column =>
        {
            // Section header
            column.Item().Background(AccentColor).Padding(8).Text("WERKNEMERSGEGEVENS")
                .Bold().FontSize(11).FontColor(HeaderColor);
            
            // Content table
            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2); // Label
                    columns.RelativeColumn(3); // Value
                });
                
                // Employment type
                table.Cell().Text("Dienstverband:").Bold().FontSize(9);
                table.Cell().Text($"{employeeInfo.EmploymentType} ({employeeInfo.EmploymentPercentage:F0}%)").FontSize(9);
                
                // Birth date
                table.Cell().PaddingTop(5).Text("Geboortedatum:").Bold().FontSize(9);
                table.Cell().PaddingTop(5).Text(employeeInfo.BirthDate?.ToString("dd/MM/yyyy", _dutchCulture) ?? "-").FontSize(9);
                
                // Employment start
                table.Cell().PaddingTop(5).Text("In dienst sinds:").Bold().FontSize(9);
                table.Cell().PaddingTop(5).Text(employeeInfo.EmploymentStartDate?.ToString("dd/MM/yyyy", _dutchCulture) ?? "-").FontSize(9);
                
                // Employment end
                table.Cell().PaddingTop(5).Text("Uit dienst:").Bold().FontSize(9);
                table.Cell().PaddingTop(5).Text(employeeInfo.EmploymentEndDate?.ToString("dd/MM/yyyy", _dutchCulture) ?? "-").FontSize(9);
                
                // Commute distance
                table.Cell().PaddingTop(5).Text("Woon-werk km:").Bold().FontSize(9);
                table.Cell().PaddingTop(5).Text($"{employeeInfo.CommuteKilometers:F0} km").FontSize(9);
            });
        });
    }
    
    private void ComposeHoursSummary(IContainer container, HoursSummarySection hoursSummary)
    {
        container.Border(1).BorderColor(BorderColor).Padding(10).Column(column =>
        {
            // Section header
            column.Item().Background(AccentColor).Padding(8).Text("URENOPGAVE")
                .Bold().FontSize(11).FontColor(HeaderColor);
            
            // Content table
            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Percentage (wider)
                    columns.RelativeColumn(1); // Hours (narrower)
                    columns.RelativeColumn(1); // Unit
                });
                
                // Header row
                table.Cell().Text("Tarief").Bold().FontSize(9).FontColor(HeaderColor);
                table.Cell().Text("Uren").Bold().FontSize(9).FontColor(HeaderColor);
                table.Cell().Text("").Bold().FontSize(9);
                
                // 100% hours
                table.Cell().PaddingTop(5).Text("100%").FontSize(9);
                table.Cell().PaddingTop(5).Text(hoursSummary.Hours100.ToString("F2", _dutchCulture)).FontSize(9);
                table.Cell().PaddingTop(5).Text("uur").FontSize(8).FontColor(Colors.Grey.Darken2);
                
                // 130% hours
                table.Cell().PaddingTop(3).Text("130%").FontSize(9);
                table.Cell().PaddingTop(3).Text(hoursSummary.Hours130.ToString("F2", _dutchCulture)).FontSize(9);
                table.Cell().PaddingTop(3).Text("uur").FontSize(8).FontColor(Colors.Grey.Darken2);
                
                // 150% hours
                table.Cell().PaddingTop(3).Text("150%").FontSize(9);
                table.Cell().PaddingTop(3).Text(hoursSummary.Hours150.ToString("F2", _dutchCulture)).FontSize(9);
                table.Cell().PaddingTop(3).Text("uur").FontSize(8).FontColor(Colors.Grey.Darken2);
                
                // 200% hours
                table.Cell().PaddingTop(3).Text("200%").FontSize(9);
                table.Cell().PaddingTop(3).Text(hoursSummary.Hours200.ToString("F2", _dutchCulture)).FontSize(9);
                table.Cell().PaddingTop(3).Text("uur").FontSize(8).FontColor(Colors.Grey.Darken2);
                
                // Night allowance
                table.Cell().PaddingTop(8).Text("Nacht 19%").FontSize(9).FontColor(Colors.Orange.Darken2);
                table.Cell().PaddingTop(8).Text(hoursSummary.TotalNightAllowanceAmount.ToString("C2", _dutchCulture)).FontSize(9).FontColor(Colors.Orange.Darken2);
                table.Cell().PaddingTop(8).Text("").FontSize(8);
            });
        });
    }
    
    private void ComposeVacationSection(IContainer container, VacationSection vacation)
    {
        container.Border(1).BorderColor(BorderColor).Padding(10).Column(column =>
        {
            // Section header
            column.Item().Background(AccentColor).Padding(8).Text("VAKANTIE-OVERZICHT")
                .Bold().FontSize(11).FontColor(HeaderColor);
            
            // Content table
            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2); // Label
                    columns.RelativeColumn(1); // Value (narrower)
                });
                
                table.Cell().Text("Jaarlijks tegoed:").Bold().FontSize(9);
                table.Cell().Text($"{vacation.AnnualEntitlementHours:F1} uur").FontSize(9);
                
                table.Cell().PaddingTop(5).Text("Opgenomen uren:").FontSize(9);
                table.Cell().PaddingTop(5).Text($"{vacation.HoursUsed:F1} uur").FontSize(9);
                
                table.Cell().PaddingTop(5).Text("Restant uren:").Bold().FontSize(9).FontColor(Colors.Green.Darken2);
                table.Cell().PaddingTop(5).Text($"{vacation.HoursRemaining:F1} uur").Bold().FontSize(9).FontColor(Colors.Green.Darken2);
                
                table.Cell().PaddingTop(8).Text("Vakantiedagen:").FontSize(9);
                table.Cell().PaddingTop(8).Text($"{vacation.TotalVacationDays:F1} dagen").FontSize(9);
            });
        });
    }
    
    private void ComposeTvTSection(IContainer container, TvTSection tvt)
    {
        container.Border(1).BorderColor(BorderColor).Padding(10).Column(column =>
        {
            // Section header
            column.Item().Background(AccentColor).Padding(8).Text("TIJD VOOR TIJD")
                .Bold().FontSize(11).FontColor(HeaderColor);
            
            // Content table
            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2); // Label
                    columns.RelativeColumn(1); // Value (narrower)
                });
                
                table.Cell().Text("Gespaard:").FontSize(9);
                table.Cell().Text($"{tvt.SavedTvTHours:F1} uur").FontSize(9);
                
                table.Cell().PaddingTop(5).Text("Omgerekend:").FontSize(9);
                table.Cell().PaddingTop(5).Text($"{tvt.ConvertedTvTHours:F1} uur").FontSize(9);
                
                table.Cell().PaddingTop(5).Text("Opgenomen:").FontSize(9);
                table.Cell().PaddingTop(5).Text($"{tvt.UsedTvTHours:F1} uur").FontSize(9);
                
                table.Cell().PaddingTop(5).Text("Einde maand:").Bold().FontSize(9).FontColor(Colors.Blue.Darken2);
                table.Cell().PaddingTop(5).Text($"{tvt.MonthEndTvTHours:F1} uur").Bold().FontSize(9).FontColor(Colors.Blue.Darken2);
            });
        });
    }
    
    private void ComposeDailyBreakdown(IContainer container, DriverTimesheetReport report)
    {
        container.Column(column =>
        {
            // Table header
            column.Item().Background(HeaderColor).Padding(8).Text("DAGELIJKSE URENVERANTWOORDING")
                .Bold().FontSize(12).FontColor(Colors.White);
            
            column.Item().Table(table =>
            {
                // Define columns with better proportions
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(35);  // Week
                    columns.ConstantColumn(25);  // Day
                    columns.ConstantColumn(60);  // Date
                    columns.ConstantColumn(50);  // Service Code
                    columns.ConstantColumn(40);  // Start
                    columns.ConstantColumn(40);  // End
                    columns.ConstantColumn(40);  // Rest
                    columns.ConstantColumn(40);  // Corrections
                    columns.ConstantColumn(45);  // Total Hours
                    columns.ConstantColumn(35);  // 100%
                    columns.ConstantColumn(35);  // 130%
                    columns.ConstantColumn(35);  // 150%
                    columns.ConstantColumn(35);  // 200%
                    columns.ConstantColumn(50);  // Allowances
                    columns.ConstantColumn(35);  // Km
                    columns.ConstantColumn(40);  // Km Allow
                    columns.ConstantColumn(35);  // TvT
                    columns.RelativeColumn(1);   // Remarks
                });
                
                // Header row with better styling
                table.Header(header =>
                {
                    header.Cell().Background(AccentColor).Padding(5).Text("Week").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("Dag").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("Datum").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("Code").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("Van").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("Tot").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("Rust").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("Corr.").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("Totaal").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("100%").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("130%").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("150%").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("200%").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("Toeslagen").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("Km").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("Km €").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("TvT").Bold().FontSize(8);
                    header.Cell().Background(AccentColor).Padding(5).Text("Opmerkingen").Bold().FontSize(8);
                });
                
                // Data rows
                foreach (var week in report.Weeks)
                {
                    // Week separator with subtle background
                    if (week.Days.Any())
                    {
                        table.Cell().ColumnSpan(18).Background("#fafafa").Padding(3).Text($"Week {week.WeekNumber}")
                            .Bold().FontSize(9).FontColor(HeaderColor);
                    }
                    
                    foreach (var day in week.Days)
                    {
                        ComposeDailyRow(table, day);
                    }
                    
                    // Week total with accent
                    ComposeWeekTotalRow(table, week.WeekTotal);
                    
                    // Empty separator row
                    ComposeEmptyRow(table);
                }
            });
        });
    }
    
    private void ComposeDailyRow(TableDescriptor table, DailyEntry day)
    {
        table.Cell().Padding(3).Text(day.WeekNumber.ToString()).FontSize(8);
        table.Cell().Padding(3).Text(day.DayName).FontSize(8);
        table.Cell().Padding(3).Text(day.Date.ToString("dd/MM/yyyy", _dutchCulture)).FontSize(8);
        table.Cell().Padding(3).Text(day.ServiceCode).FontSize(8);
        table.Cell().Padding(3).Text(day.StartTime?.ToString(@"hh\:mm") ?? "").FontSize(8);
        table.Cell().Padding(3).Text(day.EndTime?.ToString(@"hh\:mm") ?? "").FontSize(8);
        table.Cell().Padding(3).Text(day.BreakTime?.ToString(@"hh\:mm") ?? "").FontSize(8);
        table.Cell().Padding(3).Text(day.Corrections.ToString("F2", _dutchCulture)).FontSize(8);
        table.Cell().Padding(3).Text(day.TotalHours.ToString("F2", _dutchCulture)).FontSize(8);
        table.Cell().Padding(3).Text(day.Hours100.ToString("F2", _dutchCulture)).FontSize(8);
        table.Cell().Padding(3).Text(day.Hours130.ToString("F2", _dutchCulture)).FontSize(8);
        table.Cell().Padding(3).Text(day.Hours150.ToString("F2", _dutchCulture)).FontSize(8);
        table.Cell().Padding(3).Text(day.Hours200.ToString("F2", _dutchCulture)).FontSize(8);
        table.Cell().Padding(3).Text((day.TaxFreeAmount + day.TaxableAmount).ToString("C2", _dutchCulture)).FontSize(8);
        table.Cell().Padding(3).Text(day.Kilometers.ToString("F1", _dutchCulture)).FontSize(8);
        table.Cell().Padding(3).Text(day.KilometerAllowance.ToString("C2", _dutchCulture)).FontSize(8);
        table.Cell().Padding(3).Text(day.TvTHours.ToString("F1", _dutchCulture)).FontSize(8);
        table.Cell().Padding(3).Text(day.Remarks).FontSize(8);
    }
    
    private void ComposeWeekTotalRow(TableDescriptor table, WeeklyTotal weekTotal)
    {
        // Empty cells for week, day, date, service code, start, end, rest
        for (int i = 0; i < 7; i++)
        {
            table.Cell().Background("#f9fafb").Text("");
        }
        
        table.Cell().Background("#f9fafb").Padding(3).Text("WEEK TOTAAL").Bold().FontSize(8).FontColor(HeaderColor);
        
        // Totals with accent background
        table.Cell().Background("#f9fafb").Padding(3).Text(weekTotal.TotalHours.ToString("F2", _dutchCulture)).Bold().FontSize(8);
        table.Cell().Background("#f9fafb").Padding(3).Text(weekTotal.Hours100.ToString("F2", _dutchCulture)).Bold().FontSize(8);
        table.Cell().Background("#f9fafb").Padding(3).Text(weekTotal.Hours130.ToString("F2", _dutchCulture)).Bold().FontSize(8);
        table.Cell().Background("#f9fafb").Padding(3).Text(weekTotal.Hours150.ToString("F2", _dutchCulture)).Bold().FontSize(8);
        table.Cell().Background("#f9fafb").Padding(3).Text(weekTotal.Hours200.ToString("F2", _dutchCulture)).Bold().FontSize(8);
        table.Cell().Background("#f9fafb").Padding(3).Text((weekTotal.TaxFreeAmount + weekTotal.TaxableAmount).ToString("C2", _dutchCulture)).Bold().FontSize(8);
        table.Cell().Background("#f9fafb").Padding(3).Text(weekTotal.TotalKilometers.ToString("F1", _dutchCulture)).Bold().FontSize(8);
        table.Cell().Background("#f9fafb").Padding(3).Text(weekTotal.KilometerAllowance.ToString("C2", _dutchCulture)).Bold().FontSize(8);
        table.Cell().Background("#f9fafb").Padding(3).Text(weekTotal.TvTHours.ToString("F1", _dutchCulture)).Bold().FontSize(8);
        table.Cell().Background("#f9fafb").Text("");
    }
    
    private void ComposeEmptyRow(TableDescriptor table)
    {
        for (int i = 0; i < 18; i++) // 18 columns total
        {
            table.Cell().Padding(2).Text("");
        }
    }
    
    private void ComposeGrandTotal(IContainer container, TotalSection grandTotal)
    {
        container.Background(HeaderColor).Padding(15).Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("TOTAAL GENERAAL")
                    .FontSize(16).Bold().FontColor(Colors.White);
                    
                column.Item().Text($"Totale uren: {grandTotal.TotalHours:F2}")
                    .FontSize(14).FontColor(Colors.White);
            });
            
            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().Text("Totale vergoedingen:")
                    .FontSize(12).FontColor(Colors.White);
                column.Item().Text($"{grandTotal.TotalAllowances:C2}")
                    .FontSize(14).Bold().FontColor(Colors.White);
            });
        });
    }
    
    private void ComposeSignature(IContainer container, DriverTimesheetReport report)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Chauffeur:")
                    .FontSize(10).Bold();
                column.Item().PaddingTop(20).LineHorizontal(1).LineColor(BorderColor);
                column.Item().PaddingTop(5).Text("Handtekening & Datum")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
            });
            
            row.ConstantItem(50); // Space between signatures
            
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("Werkgever:")
                    .FontSize(10).Bold();
                column.Item().PaddingTop(20).LineHorizontal(1).LineColor(BorderColor);
                column.Item().PaddingTop(5).Text("Handtekening & Datum")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
            });
        });
    }
} 