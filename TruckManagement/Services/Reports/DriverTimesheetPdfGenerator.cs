using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TruckManagement.DTOs.Reports;

namespace TruckManagement.Services.Reports;

public class DriverTimesheetPdfGenerator
{
    private readonly CultureInfo _dutchCulture;
    
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
                page.Margin(0.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8).FontFamily(Fonts.Arial));
                
                page.Content().Column(column =>
                {
                    // Header section
                    column.Item().Element(c => ComposeHeader(c, report));
                    
                    // Employee info and hours summary section (side by side)
                    column.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem(1).Element(c => ComposeEmployeeInfo(c, report.EmployeeInfo));
                        row.ConstantItem(20); // Space between sections
                        row.RelativeItem(1).Element(c => ComposeHoursSummary(c, report.HoursSummary));
                    });
                    
                    // Vacation and TvT section (side by side)
                    column.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem(1).Element(c => ComposeVacationSection(c, report.Vacation));
                        row.ConstantItem(20); // Space between sections
                        row.RelativeItem(1).Element(c => ComposeTvTSection(c, report.TimeForTime));
                    });
                    
                    // Daily breakdown table (main content)
                    column.Item().PaddingTop(15).Element(c => ComposeDailyBreakdown(c, report));
                    
                    // Grand total
                    column.Item().PaddingTop(10).Element(c => ComposeGrandTotal(c, report.GrandTotal));
                    
                    // Signature line
                    column.Item().PaddingTop(20).Element(c => ComposeSignature(c, report));
                });
            });
        }).GeneratePdf();
    }
    
    private void ComposeHeader(IContainer container, DriverTimesheetReport report)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text($"{report.PeriodRange}")
                    .FontSize(12).Bold();
                    
                column.Item().Text($"{report.Year}")
                    .FontSize(10);
                    
                column.Item().Text($"Periode_{report.PeriodNumber}")
                    .FontSize(10);
            });
            
            row.RelativeItem().AlignCenter().Column(column =>
            {
                column.Item().Text(report.CompanyName)
                    .FontSize(14).Bold();
            });
            
            row.RelativeItem().AlignRight().Column(column =>
            {
                column.Item().Text($"Personeel ID: {report.PersonnelId}")
                    .FontSize(10);
            });
        });
    }
    
    private void ComposeEmployeeInfo(IContainer container, EmployeeInfoSection employeeInfo)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2); // Label
                columns.RelativeColumn(1); // Value
            });
            
            table.Header(header =>
            {
                header.Cell().Text("Employee Information").Bold().FontSize(10);
                header.Cell().Text("");
            });
            
            // Employment type
            table.Cell().Text("dienstverband");
            table.Cell().Text($"{employeeInfo.EmploymentType} {employeeInfo.EmploymentPercentage:F0}%");
            
            // Birth date
            table.Cell().Text("geb. datum");
            table.Cell().Text(employeeInfo.BirthDate?.ToString("dd/MM/yyyy", _dutchCulture) ?? "");
            
            // Employment start
            table.Cell().Text("in dienst");
            table.Cell().Text(employeeInfo.EmploymentStartDate?.ToString("dd/MM/yyyy", _dutchCulture) ?? "");
            
            // Employment end
            table.Cell().Text("uitdienst");
            table.Cell().Text(employeeInfo.EmploymentEndDate?.ToString("dd/MM/yyyy", _dutchCulture) ?? "");
            
            // Commute distance
            table.Cell().Text("woonwerk km");
            table.Cell().Text(employeeInfo.CommuteKilometers.ToString("F0"));
        });
    }
    
    private void ComposeHoursSummary(IContainer container, HoursSummarySection hoursSummary)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1); // Percentage
                columns.RelativeColumn(1); // Hours
            });
            
            table.Header(header =>
            {
                header.Cell().Text("Uren").Bold();
                header.Cell().Text("");
            });
            
            table.Cell().Text("100%");
            table.Cell().Text(hoursSummary.Hours100.ToString("F2", _dutchCulture));
            
            table.Cell().Text("130%");
            table.Cell().Text(hoursSummary.Hours130.ToString("F2", _dutchCulture));
            
            table.Cell().Text("150%");
            table.Cell().Text(hoursSummary.Hours150.ToString("F2", _dutchCulture));
            
            table.Cell().Text("200%");
            table.Cell().Text(hoursSummary.Hours200.ToString("F2", _dutchCulture));
            
            table.Cell().Text("nacht 19%");
            table.Cell().Text(hoursSummary.TotalNightAllowanceAmount.ToString("C2", _dutchCulture));
        });
    }
    
    private void ComposeVacationSection(IContainer container, VacationSection vacation)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2); // Label
                columns.RelativeColumn(1); // Value
            });
            
            table.Header(header =>
            {
                header.Cell().Text("vakantie").Bold();
                header.Cell().Text("");
            });
            
            table.Cell().Text("uren jaar tegoed");
            table.Cell().Text(vacation.AnnualEntitlementHours.ToString("F1", _dutchCulture));
            
            table.Cell().Text("opgenomen uren");
            table.Cell().Text(vacation.HoursUsed.ToString("F1", _dutchCulture));
            
            table.Cell().Text("restant uren");
            table.Cell().Text(vacation.HoursRemaining.ToString("F1", _dutchCulture));
            
            table.Cell().Text("totaal vakantie dagen");
            table.Cell().Text(vacation.TotalVacationDays.ToString("F1", _dutchCulture));
        });
    }
    
    private void ComposeTvTSection(IContainer container, TvTSection tvt)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2); // Label
                columns.RelativeColumn(1); // Value
            });
            
            table.Header(header =>
            {
                header.Cell().Text("tijd voor tijd").Bold();
                header.Cell().Text("");
            });
            
            table.Cell().Text("gespaarde tvt uren");
            table.Cell().Text(tvt.SavedTvTHours.ToString("F1", _dutchCulture));
            
            table.Cell().Text("omgerekede tvt uren");
            table.Cell().Text(tvt.ConvertedTvTHours.ToString("F1", _dutchCulture));
            
            table.Cell().Text("opgenomen tvt uren");
            table.Cell().Text(tvt.UsedTvTHours.ToString("F1", _dutchCulture));
            
            table.Cell().Text("einde v/d maand tvt uren");
            table.Cell().Text(tvt.MonthEndTvTHours.ToString("F1", _dutchCulture));
        });
    }
    
    private void ComposeDailyBreakdown(IContainer container, DriverTimesheetReport report)
    {
        container.Table(table =>
        {
            // Define columns matching the Excel format
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(25); // week
                columns.ConstantColumn(50); // dag  
                columns.ConstantColumn(60); // datum
                columns.ConstantColumn(60); // Dienstcode
                columns.ConstantColumn(35); // Begin
                columns.ConstantColumn(35); // eind
                columns.ConstantColumn(35); // pauze
                columns.ConstantColumn(45); // Correcties
                columns.ConstantColumn(35); // uren
                columns.ConstantColumn(35); // 100%
                columns.ConstantColumn(35); // 130%
                columns.ConstantColumn(35); // 150%
                columns.ConstantColumn(35); // 200%
                columns.ConstantColumn(60); // vergoeding
                columns.ConstantColumn(35); // km
                columns.ConstantColumn(50); // vergoeding
                columns.ConstantColumn(35); // tvt uren
                columns.RelativeColumn(1); // Toelichting
            });
            
            // Header row
            table.Header(header =>
            {
                header.Cell().Text("week").Bold().FontSize(7);
                header.Cell().Text("dag").Bold().FontSize(7);
                header.Cell().Text("datum").Bold().FontSize(7);
                header.Cell().Text("Dienstcode").Bold().FontSize(7);
                header.Cell().Text("Begin").Bold().FontSize(7);
                header.Cell().Text("eind").Bold().FontSize(7);
                header.Cell().Text("pauze").Bold().FontSize(7);
                header.Cell().Text("Correcties").Bold().FontSize(7);
                header.Cell().Text("uren").Bold().FontSize(7);
                header.Cell().Text("100%").Bold().FontSize(7);
                header.Cell().Text("130%").Bold().FontSize(7);
                header.Cell().Text("150%").Bold().FontSize(7);
                header.Cell().Text("200%").Bold().FontSize(7);
                header.Cell().Text("vergoeding").Bold().FontSize(7);
                header.Cell().Text("KM").Bold().FontSize(7);
                header.Cell().Text("vergoeding").Bold().FontSize(7);
                header.Cell().Text("tvt uren").Bold().FontSize(7);
                header.Cell().Text("Toelichting").Bold().FontSize(7);
            });
            
            // Data rows
            foreach (var week in report.Weeks)
            {
                foreach (var day in week.Days)
                {
                    ComposeDailyRow(table, day);
                }
                
                // Week total row
                ComposeWeekTotalRow(table, week.WeekTotal);
                
                // Add some empty rows for spacing (like in Excel)
                for (int i = 0; i < 3; i++)
                {
                    ComposeEmptyRow(table);
                }
            }
        });
    }
    
    private void ComposeDailyRow(TableDescriptor table, DailyEntry day)
    {
        table.Cell().Text(day.WeekNumber.ToString()).FontSize(7);
        table.Cell().Text(day.DayName).FontSize(7);
        table.Cell().Text(day.Date.ToString("dd/MM/yyyy", _dutchCulture)).FontSize(7);
        table.Cell().Text(day.ServiceCode).FontSize(7);
        table.Cell().Text(day.StartTime?.ToString(@"hh\:mm") ?? "").FontSize(7);
        table.Cell().Text(day.EndTime?.ToString(@"hh\:mm") ?? "").FontSize(7);
        table.Cell().Text(day.BreakTime?.ToString(@"hh\:mm") ?? "").FontSize(7);
        table.Cell().Text(day.Corrections.ToString("F2", _dutchCulture)).FontSize(7);
        table.Cell().Text(day.TotalHours.ToString("F2", _dutchCulture)).FontSize(7);
        table.Cell().Text(day.Hours100.ToString("F2", _dutchCulture)).FontSize(7);
        table.Cell().Text(day.Hours130.ToString("F2", _dutchCulture)).FontSize(7);
        table.Cell().Text(day.Hours150.ToString("F2", _dutchCulture)).FontSize(7);
        table.Cell().Text(day.Hours200.ToString("F2", _dutchCulture)).FontSize(7);
        table.Cell().Text((day.TaxFreeAmount + day.TaxableAmount).ToString("C2", _dutchCulture)).FontSize(7);
        table.Cell().Text(day.Kilometers.ToString("F1", _dutchCulture)).FontSize(7);
        table.Cell().Text(day.KilometerAllowance.ToString("C2", _dutchCulture)).FontSize(7);
        table.Cell().Text(day.TvTHours.ToString("F1", _dutchCulture)).FontSize(7);
        table.Cell().Text(day.Remarks).FontSize(7);
    }
    
    private void ComposeWeekTotalRow(TableDescriptor table, WeeklyTotal weekTotal)
    {
        // Empty cells for week, day, date, service code
        table.Cell().Text("");
        table.Cell().Text("");
        table.Cell().Text("");
        table.Cell().Text("");
        table.Cell().Text("");
        table.Cell().Text("");
        table.Cell().Text("");
        table.Cell().Text("");
        
        // Totals
        table.Cell().Text(weekTotal.TotalHours.ToString("F2", _dutchCulture)).Bold().FontSize(7);
        table.Cell().Text(weekTotal.Hours100.ToString("F2", _dutchCulture)).Bold().FontSize(7);
        table.Cell().Text(weekTotal.Hours130.ToString("F2", _dutchCulture)).Bold().FontSize(7);
        table.Cell().Text(weekTotal.Hours150.ToString("F2", _dutchCulture)).Bold().FontSize(7);
        table.Cell().Text(weekTotal.Hours200.ToString("F2", _dutchCulture)).Bold().FontSize(7);
        table.Cell().Text((weekTotal.TaxFreeAmount + weekTotal.TaxableAmount).ToString("C2", _dutchCulture)).Bold().FontSize(7);
        table.Cell().Text(weekTotal.TotalKilometers.ToString("F1", _dutchCulture)).Bold().FontSize(7);
        table.Cell().Text(weekTotal.KilometerAllowance.ToString("C2", _dutchCulture)).Bold().FontSize(7);
        table.Cell().Text(weekTotal.TvTHours.ToString("F1", _dutchCulture)).Bold().FontSize(7);
        table.Cell().Text("");
    }
    
    private void ComposeEmptyRow(TableDescriptor table)
    {
        for (int i = 0; i < 18; i++) // 18 columns total
        {
            table.Cell().Text("");
        }
    }
    
    private void ComposeGrandTotal(IContainer container, TotalSection grandTotal)
    {
        container.Text($"Totaal Generaal: {grandTotal.TotalHours:F2} uren")
            .Bold().FontSize(12);
    }
    
    private void ComposeSignature(IContainer container, DriverTimesheetReport report)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"Totaal Generaal: {report.GrandTotal.TotalHours:F2}")
                .FontSize(10).Bold();
                
            row.RelativeItem().AlignRight().Text("handtekening ________________")
                .FontSize(10);
        });
    }
} 