# Backend Services Reference

All services are registered in `Program.cs`. Extend this doc when adding new services.

---

## Interfaces & Implementations

| Interface | Implementation | Lifetime | Purpose |
|-----------|----------------|----------|---------|
| IEmailService | SmtpEmailService | Scoped | Send email (password reset, notifications) |
| IContractStorageService | LocalContractStorageService | Scoped | Save/retrieve/delete contract PDFs |
| ITelegramNotificationService | TelegramNotificationService | Singleton | Send Telegram messages to drivers |

---

## Domain Services (No Interface)

| Service | Lifetime | Purpose |
|---------|----------|---------|
| DriverCompensationService | Scoped | Driver compensation settings, defaults |
| DriverContractPdfBuilder | Scoped | Generate driver contract PDF (QuestPDF) |
| DriverContractService | Scoped | Contract generation, versioning, signing flow |
| DriverInvoicePdfBuilder | Scoped | Generate driver invoice PDF |
| DriverInvoiceService | Scoped | Calculate weekly invoice amounts |
| PartRideCalculator | (Used internally) | Part ride hour calculations |
| RideExecutionCalculationService | (Used internally) | Ride execution calculations |

---

## Report Services (Services/Reports/)

| Service | Purpose |
|---------|---------|
| ReportCalculationService | Aggregates ride execution data, totals |
| VacationCalculator | Vacation hours calculation |
| TvTCalculator | Tijd voor Tijd (overtime) calculation |
| OvertimeClassifier | Classify overtime types |

---

## Configuration Options

| Options Class | Config Section | Keys |
|---------------|----------------|------|
| TelegramOptions | Telegram | BotToken, BotUsername |
| StorageOptions | Storage | BasePath, BasePathCompanies, TmpPath, SignedContractsPath |

---

## Service Dependencies (Typical)

- **DriverContractService** → IContractStorageService, DriverContractPdfBuilder, ApplicationDbContext
- **DriverInvoiceService** → DriverInvoicePdfBuilder, ApplicationDbContext, PartRideCalculator, ReportCalculationService
- **TelegramNotificationService** → TelegramOptions, IServiceScopeFactory (for scoped DbContext in background)
- **SmtpEmailService** → Smtp config from IConfiguration

---

## Adding a New Service

1. Create interface in `Interfaces/` (if abstraction needed)
2. Create implementation in `Services/`
3. Register in `Program.cs`: `builder.Services.AddScoped<IService, ServiceImpl>()`
4. Add entry to this doc
