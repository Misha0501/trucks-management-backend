# 📋 Driver Contract PDF Generation - Implementation Plan

**Version:** 1.0  
**Date:** November 15, 2025  
**Status:** Planning Phase  

---

## 🎯 Objective

Implement automatic PDF contract generation when a driver is created or updated, with full versioning and historical tracking of all contract versions.

---

## 📐 High-Level Architecture

```
Driver Creation/Update Request
        ↓
Create/Update EmployeeContract in DB
        ↓
Trigger PDF Generation
        ↓
        ├─ Load Data (Driver, Contract, Company, CAO tables)
        ├─ Calculate Fields (Age, Vacation Days, Pay Scale)
        ├─ Apply Contract Template (15 Articles)
        ├─ Generate PDF Document
        └─ Store in DriverContractVersion entity
        ↓
Return Success + ContractVersionId
```

---

## 🗂️ Implementation Steps

### **Phase 1: Database Schema & Entities** ✅ **COMPLETED**

**Status:** ✅ All steps completed successfully  
**Date Completed:** November 15, 2025  
**Build Status:** ✅ 0 Errors, 96 Warnings (all pre-existing)  
**Migration:** `20251115170920_AddDriverContractVersionEntity.cs`

---

#### **Step 1.1: Create DriverContractVersion Entity** ✅

**Purpose:** Store all versions of generated contracts with full audit trail.

**Fields:**
- `Id` (Guid, PK) - Unique identifier for this contract version
- `DriverId` (Guid, FK → Driver) - Which driver this contract belongs to
- `EmployeeContractId` (Guid, FK → EmployeeContract) - The contract data at time of generation
- `VersionNumber` (int) - Sequential version number (1, 2, 3...)
- `PdfFileName` (string) - Filename stored in filesystem
- `PdfFilePath` (string) - Full path to PDF file
- `FileSize` (long) - Size in bytes
- `ContentType` (string) - Always "application/pdf"
- `GeneratedAt` (DateTime, UTC) - When this version was created
- `GeneratedByUserId` (string, nullable, FK → AspNetUsers) - Who triggered the generation
- `ContractDataSnapshot` (string, JSON) - Serialized snapshot of EmployeeContract at generation time (for audit)
- `IsLatestVersion` (bool) - True only for the most recent version
- `Status` (enum: Draft, Generated, Signed, Superseded, Archived) - Contract lifecycle status
- `Notes` (string, nullable) - Optional notes about this version

**Relationships:**
- Many-to-One with `Driver` (one driver → many contract versions)
- Many-to-One with `EmployeeContract` (one contract → many PDF versions over time)
- Many-to-One with `ApplicationUser` (who generated it)

**Indexes:**
- Unique index on (`DriverId`, `VersionNumber`) - ensure sequential versioning per driver
- Index on `EmployeeContractId` - fast lookup of all versions for a contract
- Index on `IsLatestVersion` + `DriverId` - quick access to current contract

**Why this approach?**
- ✅ Full version history (never delete old PDFs)
- ✅ Audit trail (who generated, when)
- ✅ Data snapshot (know exactly what data was used)
- ✅ Easy rollback (mark old version as latest)
- ✅ Track contract lifecycle (draft → signed → superseded)

---

#### **Step 1.2: Add ContractVersionStatus Enum** ✅

**Location:** `TruckManagement/Enums/ContractVersionStatus.cs`

**Values:**
- `Draft` = 0 - Contract generated but not yet finalized
- `Generated` = 1 - PDF created and ready for review
- `Signed` = 2 - Contract has been signed by driver
- `Superseded` = 3 - A newer version exists
- `Archived` = 4 - Old version, kept for records

---

#### **Step 1.3: Create Database Migration** ✅

**Command:** `dotnet ef migrations add AddDriverContractVersionEntity`

**Changes:**
- Create `DriverContractVersions` table
- Add foreign keys
- Add indexes
- Ensure UTC timestamps

---

#### **Step 1.4: Update ApplicationDbContext** ✅

**Changes:**
- Add `DbSet<DriverContractVersion> DriverContractVersions`
- Configure relationships in `OnModelCreating`
- Add query filter if needed (probably not - we want to see all versions)

---

### **Phase 2: File Storage Strategy** ✅ **COMPLETED**

**Status:** ✅ All steps completed successfully  
**Date Completed:** November 15, 2025  
**Build Status:** ✅ 0 Errors, 96 Warnings (all pre-existing)

---

#### **Step 2.1: Define Storage Structure** ✅

**Base Path:** `/storage/contracts/` (or from env variable `CONTRACTS_STORAGE_PATH`)

**Directory Structure:**
```
/storage/contracts/
    ├── {year}/
    │   ├── {month}/
    │   │   ├── {driverId}/
    │   │   │   ├── contract_v1_{timestamp}.pdf
    │   │   │   ├── contract_v2_{timestamp}.pdf
    │   │   │   └── contract_v3_{timestamp}.pdf
```

**Filename Format:**
```
contract_v{versionNumber}_{yyyyMMdd_HHmmss}_{driverId}.pdf

Example: contract_v1_20251115_143022_a7b3c4d5-e6f7-8901-2345-67890abcdef1.pdf
```

**Why this structure?**
- ✅ Year/Month folders prevent too many files in one directory
- ✅ DriverId subfolder groups all versions for one driver
- ✅ Version number in filename for quick identification
- ✅ Timestamp prevents naming collisions
- ✅ Easy to backup/archive by year/month

---

#### **Step 2.2: File Storage Service Interface** ✅

**Purpose:** Abstract file operations for easier testing and future cloud storage migration.

**File:** `TruckManagement/Interfaces/IContractStorageService.cs`

**Interface:** `IContractStorageService`

**Methods:**
- `Task<string> SaveContractPdfAsync(byte[] pdfBytes, Guid driverId, int versionNumber)`
  - Saves PDF to disk
  - Returns full file path
  
- `Task<byte[]> GetContractPdfAsync(string filePath)`
  - Retrieves PDF from disk
  - Returns byte array

- `Task<bool> DeleteContractPdfAsync(string filePath)`
  - Deletes PDF file (if needed)
  - Returns success/failure

- `Task<bool> FileExistsAsync(string filePath)`
  - Check if file exists
  - Returns bool

- `string GetContractFileName(Guid driverId, int versionNumber)`
  - Generates standardized filename
  - Format: `contract_v{version}_{timestamp}_{driverId}.pdf`

- `string GetStorageBasePath()`
  - Returns configured base storage path

**Implementation:** `LocalContractStorageService` (stores to local filesystem)

**File:** `TruckManagement/Services/LocalContractStorageService.cs`

**Features Implemented:**
- ✅ Configurable base path via `ContractStorage:BasePath` config key
- ✅ Automatic directory creation (year/month/driverId structure)
- ✅ Standardized filename generation with timestamp
- ✅ Comprehensive error handling (ArgumentException, FileNotFoundException, IOException)
- ✅ Full logging integration (info, debug, warning, error levels)
- ✅ Async file operations (SaveAsync, GetAsync, DeleteAsync, ExistsAsync)

**Error Handling:**
- Invalid inputs → `ArgumentException`
- Missing files → `FileNotFoundException`
- File operation failures → `IOException`
- All exceptions logged with context

**Future Migration Path:**
- Azure Blob Storage implementation
- AWS S3 implementation
- Just swap the implementation in DI (no code changes needed)

**Testing:**
- ✅ All methods compile successfully
- ✅ Interface fully implemented
- ✅ Build passes with 0 errors
- ✅ Ready for integration testing with PDF generation

---

### **Phase 3: PDF Generation Service**

#### **Step 3.1: Choose PDF Library** ✅ **ALREADY INSTALLED**

**Status:** ✅ QuestPDF already in use for driver reports  
**Version:** 2025.1.0 (Latest)  
**License:** Community (already configured in `DriverTimesheetPdfGenerator`)  
**Package:** Already in `TruckManagement.csproj` (line 25)

**Existing Usage:**
- `Services/Reports/DriverTimesheetPdfGenerator.cs`
- Generates driver timesheet reports (urenverantwoording)
- Professional styling with Dutch culture formatting
- Multi-page documents with tables, headers, footers

**Benefits:**
- ✅ No new dependencies needed
- ✅ Team already familiar with QuestPDF API
- ✅ Proven stable in production
- ✅ Consistent styling across all PDFs
- ✅ Same Dutch formatting patterns

**Reusable Patterns from Existing Report:**
```csharp
// License configuration
QuestPDF.Settings.License = LicenseType.Community;

// Document structure
Document.Create(container => { ... }).GeneratePdf()

// Dutch culture
new CultureInfo("nl-NL")

// Styling
HeaderColor, AccentColor, BorderColor, TextColor
FontFamily(Fonts.Arial)
```

**Decision:** ✅ Use existing QuestPDF installation (no additional setup needed)

---

#### **Step 3.2: Create Contract Template Builder** ✅ **COMPLETED**

**Status:** ✅ Fully implemented and building successfully  
**File:** `TruckManagement/Services/DriverContractPdfBuilder.cs`  
**Build Status:** ✅ 0 Errors, 99 Warnings (all pre-existing)

**Purpose:** Translate contract data into PDF structure.

**Class:** `DriverContractPdfBuilder`

**Implemented Features:**

1. **Dynamic Field Calculation** ✅
   - Contract type (BEPAALDE vs ONBEPAALDE) based on LastWorkingDay
   - Contact person name from CreatedByUser
   - Statutory/Extra vacation days calculation
   - Vacation allowance percentage formatting

2. **Dutch Formatting** ✅
   - Dates: "18 november 2025" (dd MMMM yyyy)
   - Short dates: "18-11-2025" (dd-MM-yyyy)
   - Currency: "€ 1.234,56" (nl-NL format)
   - Percentages: "8%"
   - Dutch month names via CultureInfo("nl-NL")

3. **PDF Structure** ✅
   - Professional header with blue color scheme
   - Werkgever/Werknemer information boxes
   - Contract title with type (BEPAALDE/ONBEPAALDE)
   - All 15 articles fully implemented:
     * Artikel 1: Employment date, function, work location
     * Artikel 2: Probation period (conditional)
     * Artikel 3: Working hours and overtime
     * Artikel 4: Salary (with CAO lookup)
     * Artikel 5: Travel expense reimbursement
     * Artikel 6: Sick pay and waiting days
     * Artikel 7: Reintegration after employment
     * Artikel 8: Vacation days, ATV, holiday allowance
     * Artikel 9-15: Static legal clauses
   - Signature section with boxes for both parties
   - Footer with page numbers ("Pagina X van Y")

4. **Styling** ✅
   - Consistent color scheme (matching existing reports)
   - Header Color: #2563eb (Blue)
   - Accent Color: #f3f4f6 (Light gray)
   - Border Color: #d1d5db (Medium gray)
   - Text Color: #374151 (Dark gray)
   - Professional fonts: Arial
   - Proper line spacing (1.4x for readability)
   - A4 page size with appropriate margins

**Method Signature:**
```csharp
public byte[] BuildContractPdf(
    EmployeeContract contract,
    CAOPayScale payScale,
    CAOVacationDays vacationDays,
    ApplicationUser? createdByUser
)
```

**Key Implementation Details:**
- Uses QuestPDF fluent API (same as DriverTimesheetPdfGenerator)
- Handles nullable DateTime fields properly
- Handles nullable decimal fields with fallbacks
- All text in Dutch (gender-neutral)
- CAO-compliant content
- Legally binding format

**Testing:**
- ✅ Compiles successfully
- ✅ All 15 articles implemented
- ✅ Date formatting tested
- ✅ Currency formatting tested
- ✅ Conditional logic (probation, last working day) tested
- ✅ No new lint errors introduced

---

#### **Step 3.3: Contract Data Service**
**Status**: ✅ COMPLETED

**Purpose:** Orchestrate data loading and PDF generation.

**Class:** `DriverContractService`

**Responsibilities:**
1. Load all required data from DB ✅
2. Validate data completeness ✅
3. Call PDF builder ✅
4. Save PDF to storage ✅
5. Create DriverContractVersion record ✅
6. Mark old versions as superseded ✅
7. Return contract version info ✅

**Implementation Summary:**
- Created `TruckManagement/Services/DriverContractService.cs` (340 lines)
- Full dependency injection setup in Program.cs
- Integrated into `POST /drivers/create-with-contract` endpoint
- Returns `ContractVersionId` in API response
- Graceful error handling (doesn't fail driver creation if PDF fails)

**Key Method:**
```csharp
public async Task<DriverContractVersion> GenerateContractAsync(
    Guid driverId,
    Guid employeeContractId,
    string? generatedByUserId = null
);
```

**Workflow:**
1. Load driver with all relations (Company, EmployeeContract, User)
2. Load contract creator user (for signature name)
3. Determine driver age from DateOfBirth
4. Look up CAO pay scale (Scale + Step + Year)
5. Look up vacation days (Age + Year)
6. Validate all required data is present
7. Call PDF builder
8. Save PDF to file system
9. Create DriverContractVersion entity
10. Mark previous version as `IsLatestVersion = false`
11. Save to database
12. Return new version

**Error Handling:**
- Missing required data → throw `InvalidOperationException`
- PDF generation fails → throw `ContractGenerationException`
- File save fails → rollback DB transaction

---

### **Phase 4: Integration Points** ✅ **COMPLETED**

#### **Step 4.1: Trigger Contract Generation on Driver Creation** ✅ **COMPLETED**

**Location:** `DriverEndpoints.cs` → `POST /drivers/create-with-contract`

**Implementation Status:**
- ✅ Injected `DriverContractService` into endpoint
- ✅ Calls `GenerateContractAsync` after transaction commit
- ✅ Error handling: logs error but doesn't rollback driver creation
- ✅ Returns `contractVersionId` in response

---

#### **Step 4.2: Trigger Contract Generation on Driver Update** ✅ **COMPLETED**

**Location:** `DriverEndpoints.cs` → `PUT /drivers/{driverId}/with-contract`

**Implementation Status:**
- ✅ Added contract change detection logic
- ✅ Tracks: PayScale, PayScaleStep, DateOfEmployment, LastWorkingDay, Function, BSN, WorkweekDuration, DateOfBirth
- ✅ Auto-regenerates contract if any tracked field changes
- ✅ Skips regeneration if no relevant fields changed
- ✅ Error handling: logs error but doesn't fail the update

**Note:** Only generates new version when contract-relevant fields change, preventing unnecessary PDF generations for non-contract updates (e.g., phone number, address).

---

#### **Step 4.3: Manual Contract Regeneration Endpoint** ✅ **COMPLETED**

**Purpose:** Allow admins to manually regenerate contract (e.g., after fixing data).

**Endpoint:** `POST /drivers/{driverId}/contracts/regenerate`

**Authorization:** `[Authorize(Roles = "customerAdmin,globalAdmin")]`

**Implementation Status:**
- ✅ Endpoint implemented in `DriverEndpoints.cs`
- ✅ Authorization checks for globalAdmin and customerAdmin
- ✅ Validates driver and contract existence
- ✅ Generates new contract version with incremented version number
- ✅ Returns version info with download URL

**Tested:** ✅ Successfully regenerated contract version 2 for existing driver

**Response Example:**
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "data": {
    "contractVersionId": "8f36cbe2-f916-4310-b398-52e4762db19d",
    "versionNumber": 2,
    "generatedAt": "2025-11-15T18:26:41.9410347Z",
    "pdfDownloadUrl": "/drivers/872c266f-2107-47c3-b0a7-fbfc22cc67a3/contracts/8f36cbe2-f916-4310-b398-52e4762db19d/download",
    "message": "Contract regenerated successfully"
  }
}
```

---

### **Phase 5: API Endpoints** ✅ **COMPLETED**

#### **Step 5.1: List Contract Versions** ✅ **COMPLETED**

**Endpoint:** `GET /drivers/{driverId}/contracts`

**Authorization:** `[Authorize(Roles = "driver,employer,customerAdmin,globalAdmin")]`

**Query Params:**
- `includeSuperseded` (bool, default: false) - Include old versions or only latest

**Implementation Status:**
- ✅ Endpoint implemented in `DriverEndpoints.cs`
- ✅ Authorization: Drivers can only view their own contracts, admins can view contracts for drivers in their companies
- ✅ Returns array of contract versions with metadata
- ✅ `includeSuperseded` parameter filters out old versions when `false`

**Tested:** ✅ Successfully retrieved 2 contract versions (latest "Generated" and old "Superseded")

**Response Example:**
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "data": [
    {
      "id": "8f36cbe2-f916-4310-b398-52e4762db19d",
      "versionNumber": 2,
      "status": "Generated",
      "generatedAt": "2025-11-15T18:26:41.941034Z",
      "generatedByUserId": "26759c98-4916-4c28-b343-8ac262057375",
      "generatedByUserName": "Admin User",
      "fileName": "contract_v2_20251115_182641_872c266f-2107-47c3-b0a7-fbfc22cc67a3.pdf",
      "fileSize": 108785,
      "isLatestVersion": true
    },
    {
      "id": "26b576a1-5a5f-4ad5-a9c0-8367a8a7bd17",
      "versionNumber": 1,
      "status": "Superseded",
      "generatedAt": "2025-11-15T18:15:32.446256Z",
      "generatedByUserId": "26759c98-4916-4c28-b343-8ac262057375",
      "generatedByUserName": "Admin User",
      "fileName": "contract_v1_20251115_181532_872c266f-2107-47c3-b0a7-fbfc22cc67a3.pdf",
      "fileSize": 108785,
      "isLatestVersion": false
    }
  ],
  "errors": null
}
```

---

#### **Step 5.2: Get Latest Contract Version** ✅ **COMPLETED**

**Endpoint:** `GET /drivers/{driverId}/contracts/latest`

**Authorization:** `[Authorize(Roles = "driver,employer,customerAdmin,globalAdmin")]`

**Implementation Status:**
- ✅ Endpoint implemented in `DriverEndpoints.cs`
- ✅ Returns only the latest version (where `isLatestVersion = true`)
- ✅ Same authorization logic as list endpoint

---

#### **Step 5.3: Download Contract PDF** ✅ **COMPLETED**

**Endpoint:** `GET /drivers/{driverId}/contracts/{versionId}/download`

**Authorization:** `[Authorize(Roles = "driver,employer,customerAdmin,globalAdmin")]`

**Implementation Status:**
- ✅ Endpoint implemented in `DriverEndpoints.cs`
- ✅ Uses `IContractStorageService` to retrieve PDF from file system
- ✅ Returns PDF as file download with proper content type and filename
- ✅ Authorization: Drivers can only download their own, admins can download for their companies
- ✅ Checks file existence before attempting download

**Response:**
- Content-Type: `application/pdf`
- Content-Disposition: `attachment; filename="contract_v2_20251115_182641_872c266f-2107-47c3-b0a7-fbfc22cc67a3.pdf"`
- Binary PDF data

---

#### **Step 5.4: Get Contract Version Details** ✅ **COMPLETED**

**Endpoint:** `GET /drivers/{driverId}/contracts/{versionId}`

**Authorization:** `[Authorize(Roles = "driver,employer,customerAdmin,globalAdmin")]`

**Implementation Status:**
- ✅ Endpoint implemented in `DriverEndpoints.cs`
- ✅ Returns detailed metadata including contract snapshot (JSON-serialized contract data at generation time)
- ✅ Includes driver name, generated by user name, file path, and all version metadata
- ✅ Uses `System.Text.Json` to parse contract snapshot

**Response Example:**
```json
{
  "isSuccess": true,
  "statusCode": 200,
  "data": {
    "id": "8f36cbe2-f916-4310-b398-52e4762db19d",
    "driverId": "872c266f-2107-47c3-b0a7-fbfc22cc67a3",
    "driverName": "Emily Driver",
    "versionNumber": 2,
    "status": "Generated",
    "generatedAt": "2025-11-15T18:26:41.941034Z",
    "generatedByUserId": "26759c98-4916-4c28-b343-8ac262057375",
    "generatedByUserName": "Admin User",
    "fileName": "contract_v2_20251115_182641_872c266f-2107-47c3-b0a7-fbfc22cc67a3.pdf",
    "filePath": "/app/storage/contracts/2025/11/872c266f-2107-47c3-b0a7-fbfc22cc67a3/contract_v2_20251115_182641_872c266f-2107-47c3-b0a7-fbfc22cc67a3.pdf",
    "fileSize": 108785,
    "isLatestVersion": true,
    "notes": null,
    "contractSnapshot": {
      // JSON object containing full contract data at time of generation
    }
  },
  "errors": null
}
```

---

### **Phase 6: DTOs** ✅ **IMPLEMENTED (Anonymous Objects)**

#### **Step 6.1: Create DTOs** ✅ **COMPLETED**

**Implementation Approach:**
- Used **anonymous objects** instead of formal DTOs in all Phase 5 endpoints
- This is a common pattern in minimal APIs and works well for this use case
- All necessary fields are included in responses with proper typing

**Fields Included in Responses:**
- ✅ All fields from DriverContractVersion entity
- ✅ Resolved user names (`generatedByUserName`)
- ✅ Download URLs (`pdfDownloadUrl`)
- ✅ File metadata (fileName, fileSize, contentType, filePath)
- ✅ Version metadata (versionNumber, status, isLatestVersion)
- ✅ Contract snapshot (JSON deserialization)

**Note:** If formal DTOs are needed later for OpenAPI documentation or stronger typing, they can be created from the existing anonymous object structures.

---

### **Phase 7: Business Logic & Validation** ✅ **COMPLETED**

#### **Step 7.1: Contract Data Validation** ✅ **COMPLETED**

**Implementation Status:**
- ✅ Validation implemented in `DriverContractService.GenerateContractAsync()`
- ✅ Throws `InvalidOperationException` if driver or contract not found
- ✅ Throws `InvalidOperationException` if critical data missing (Function, DateOfEmployment)
- ✅ CAO lookup with fallback: Uses contract data if CAO data not found in database
- ✅ Logs warnings when fallbacks are used
- ✅ All errors are caught and logged with stack traces

**Validation Checks:**
- ✅ Driver exists and not deleted
- ✅ Contract exists for driver
- ✅ Company exists (loaded via Include)
- ✅ Critical fields: Function, DateOfEmployment not null
- ✅ CAO pay scale lookup (with fallback to contract values)
- ✅ CAO vacation days lookup (with fallback to contract values)
- ✅ Age calculation for vacation days (with null check)
- ✅ Company address details (updated in database seeder)

**Error Handling:**
- Validation failures throw `InvalidOperationException` with clear messages
- PDF generation failures throw `InvalidOperationException`
- Storage failures throw `InvalidOperationException`
- All exceptions logged to console with full stack traces

---

#### **Step 7.2: Version Number Logic** ✅ **COMPLETED**

**Implementation Status:**
- ✅ Implemented in `DriverContractService.GenerateContractAsync()`
- ✅ Queries existing versions: `OrderByDescending(dcv => dcv.VersionNumber).FirstOrDefaultAsync()`
- ✅ Calculates next version: `(latestVersion?.VersionNumber ?? 0) + 1`
- ✅ Marks previous version as superseded: `latestVersion.IsLatestVersion = false` and `Status = Superseded`
- ✅ Sets new version as latest: `IsLatestVersion = true`, `Status = Generated`
- ✅ All changes saved in single `SaveChangesAsync()` transaction

**Code Implementation:**
```csharp
// Get next version number
var latestVersion = await _dbContext.DriverContractVersions
    .Where(dcv => dcv.DriverId == driverId)
    .OrderByDescending(dcv => dcv.VersionNumber)
    .FirstOrDefaultAsync();

int nextVersionNumber = (latestVersion?.VersionNumber ?? 0) + 1;

// Mark old versions as superseded
if (latestVersion != null)
{
    latestVersion.IsLatestVersion = false;
    latestVersion.Status = ContractVersionStatus.Superseded;
    _dbContext.DriverContractVersions.Update(latestVersion);
}
```

**Tested:** ✅ Successfully created version 2, marked version 1 as "Superseded"

---

#### **Step 7.3: Contract Snapshot Logic** ✅ **COMPLETED**

**Implementation Status:**
- ✅ Implemented in `DriverContractService.GenerateContractAsync()`
- ✅ Serializes entire `EmployeeContract` entity to JSON
- ✅ Stored in `DriverContractVersion.ContractDataSnapshot` field
- ✅ Can be deserialized in API endpoints using `System.Text.Json`

**Code Implementation:**
```csharp
// Create snapshot of contract data
var contractDataSnapshot = JsonConvert.SerializeObject(contract);

var newContractVersion = new DriverContractVersion
{
    // ... other fields ...
    ContractDataSnapshot = contractDataSnapshot,
    // ... other fields ...
};
```

**Usage in API:**
- ✅ Step 5.4 endpoint deserializes snapshot for viewing: `JsonSerializer.Deserialize<object>(contractVersion.ContractDataSnapshot)`
- ✅ Provides full audit trail of contract data at time of PDF generation
- ✅ Allows reconstruction of PDF content even if contract data changes

**Benefits:**
- Data integrity: Know exactly what was on the PDF
- Audit compliance: Complete history of all contract versions
- Dispute resolution: Can prove what was agreed at generation time

---

### **Phase 8: PDF Template Implementation** ✅ **COMPLETED**

#### **Step 8.1: Document Structure**

**Page Layout:**
- A4 size (210mm × 297mm)
- Margins: 20mm top/bottom, 25mm left/right
- Font: Arial or similar (readable, professional)
- Font sizes: 
  - Title: 16pt bold
  - Article headings: 12pt bold
  - Body text: 10pt normal
  - Footer: 8pt

**Sections:**
1. **Header**
   - Company name (left)
   - "ARBEIDSOVEREENKOMST" title (center, large)
   - Date (right)

2. **Introduction**
   - "De ondergetekende:" section
   - Employer details
   - Employee details

3. **Articles** (Artikelen 1-15)
   - Each article numbered and titled
   - Sub-sections with proper indentation
   - Bold headings, normal body text

4. **Signature Section**
   - Two columns: Employer | Employee
   - Place, date, signature lines
   - "Namens deze:" with contact person name

5. **Footer**
   - Page numbers: "Pagina 1 van 3"

---

#### **Step 8.2: Variable Replacement**

**Use the mapping from DriverContractTemplate.json**

**Examples:**
- `{{EmployerName}}` → Company.Name
- `{{EmployerAddress}}` → Company.Address + City
- `{{EmployeeName}}` → Driver.FirstName + LastName
- `{{DateOfBirth}}` → Driver.DateOfBirth (formatted)
- `{{ContractType}}` → "BEPAALDE TIJD" or "ONBEPAALDE TIJD"
- `{{DateOfEmployment}}` → Contract.DateOfEmployment (formatted)
- `{{Scale}}` → Contract.Scale
- `{{Step}}` → Contract.Step
- `{{WeeklyWage}}` → PayScale.WeeklyWage (formatted: "€ 785,13")
- `{{VacationDays}}` → VacationDays.VacationDays
- `{{ContactPersonName}}` → CreatedByUser.FirstName + LastName
- `{{ContractDate}}` → Contract.CreatedAt (formatted: "18 november 2025")

**See `DriverContractTemplate.md` for complete variable list (15 articles).**

---

#### **Step 8.3: Date & Currency Formatting**

**Dutch Date Format:**
```csharp
// Input: 2025-11-15
// Output: "15 november 2025"

CultureInfo dutchCulture = new CultureInfo("nl-NL");
string formatted = date.ToString("d MMMM yyyy", dutchCulture);
```

**Dutch Currency Format:**
```csharp
// Input: 785.13
// Output: "€ 785,13"

CultureInfo dutchCulture = new CultureInfo("nl-NL");
string formatted = amount.ToString("C2", dutchCulture);
```

**Helper Class:** `DutchFormatHelper`
- `FormatDate(DateTime date)`
- `FormatCurrency(decimal amount)`
- `FormatPercentage(decimal percentage)`

---

### **Phase 9: Error Handling & Logging**

#### **Step 9.1: Custom Exceptions**

**Create:**

1. **`ContractGenerationException`**
   - Thrown when PDF generation fails
   - Include inner exception details
   - Log full stack trace

2. **`ContractDataValidationException`**
   - Thrown when required data is missing/invalid
   - Include list of validation errors
   - Return to user with clear message

3. **`ContractStorageException`**
   - Thrown when file save/load fails
   - Include file path and error details

---

#### **Step 9.2: Logging Strategy**

**Log Levels:**

**Information:**
- Contract generation started (driverId, contractId)
- Contract generation completed (versionNumber, fileSize)
- Contract downloaded (driverId, versionId, userId)

**Warning:**
- Contract regeneration triggered (reason)
- Old contract file not found (shouldn't happen)
- Data snapshot deserialization failed

**Error:**
- PDF generation failed (exception details)
- File storage failed (path, exception)
- Required data missing (validation errors)

**Log Format:**
```csharp
_logger.LogInformation(
    "Generating contract for driver {DriverId}, contract {ContractId}, version {VersionNumber}",
    driverId, contractId, versionNumber
);

_logger.LogError(
    ex,
    "Failed to generate contract PDF for driver {DriverId}. Reason: {Reason}",
    driverId, ex.Message
);
```

---

### **Phase 10: Testing Strategy**

#### **Step 10.1: Unit Tests**

**Test Classes:**

1. **`DriverContractServiceTests`**
   - Test data loading
   - Test version numbering
   - Test snapshot creation
   - Mock DB and file storage

2. **`DriverContractPdfBuilderTests`**
   - Test variable replacement
   - Test date/currency formatting
   - Test Dutch text rendering
   - Verify PDF output structure

3. **`DutchFormatHelperTests`**
   - Test date formatting
   - Test currency formatting
   - Test edge cases (nulls, negatives)

**Mock Data:**
- Sample driver with all fields
- Sample contract with Scale E, Step 5
- Sample CAO pay scale
- Sample vacation days

---

#### **Step 10.2: Integration Tests**

**Test Scenarios:**

1. **Full Contract Generation Flow**
   - Create driver → verify PDF generated → verify DB record
   
2. **Version Incrementing**
   - Generate v1 → update driver → generate v2 → verify v1 marked superseded

3. **File Storage**
   - Generate contract → verify file exists → download → verify content

4. **Permission Checks**
   - Driver can download own contract
   - Driver cannot download other driver's contract
   - Admin can download any contract

---

#### **Step 10.3: Manual Testing**

**Test Cases:**

1. Create new driver → Check PDF generated → Open PDF → Verify all fields populated
2. Update driver contract → Check new version created → Compare v1 vs v2
3. Download contract as driver role → Verify download works
4. Download contract as admin → Verify download works
5. Try to access another driver's contract → Verify 403 Forbidden
6. Generate contract with missing data → Verify error message
7. Check contract snapshot → Verify JSON contains all data

---

### **Phase 11: Frontend Integration Points**

#### **Step 11.1: Driver Creation/Edit Form**

**Changes Needed:**

1. After driver creation → Show success message with contract generation status
2. Display contract version number in driver details
3. Add "View Contract" button → Opens PDF in new tab
4. Show contract generation date
5. Show who generated the contract

---

#### **Step 11.2: Contract Versions List**

**New UI Component:**

**Location:** Driver details page → "Contracts" tab

**Display:**
- Table with all contract versions
- Columns: Version, Date, Status, Generated By, Actions
- Actions: Download, View Details
- Highlight latest version

---

#### **Step 11.3: Contract Download**

**Implementation:**
- Call API endpoint: `GET /drivers/{id}/contracts/{versionId}/download`
- Browser automatically downloads PDF
- Filename: `contract_v2_20251115.pdf`

---

### **Phase 12: Deployment Considerations**

#### **Step 12.1: Environment Variables**

**Add to `.env` / `appsettings.json`:**

```json
{
  "ContractStorage": {
    "BasePath": "/storage/contracts",
    "MaxFileSizeMB": 10,
    "AllowedContentTypes": ["application/pdf"]
  }
}
```

---

#### **Step 12.2: File Storage Setup**

**For Docker:**
- Mount volume: `/storage/contracts` → Host directory
- Ensure write permissions
- Backup strategy (regular snapshots)

**For Production:**
- Ensure sufficient disk space
- Setup automated backups
- Monitor disk usage
- Consider retention policy (keep contracts for X years)

---

#### **Step 12.3: Database Backup**

**DriverContractVersions table:**
- Contains critical audit data
- Should be included in regular DB backups
- Consider separate backup schedule (more frequent)

---

## 📊 Implementation Order Summary

### **Sprint 1: Foundation (Days 1-2)**
1. Create `DriverContractVersion` entity
2. Create `ContractVersionStatus` enum
3. Create database migration
4. Update `ApplicationDbContext`
5. Test migration locally

### **Sprint 2: Storage (Day 3)**
6. Create `IContractStorageService` interface
7. Implement `LocalContractStorageService`
8. Setup storage directory structure
9. Test file save/load operations

### **Sprint 3: PDF Generation (Days 4-6)**
10. Install QuestPDF NuGet package
11. Create `DutchFormatHelper` utility
12. Create `DriverContractPdfBuilder` class
13. Implement all 15 articles from template
14. Test PDF generation with sample data

### **Sprint 4: Service Layer (Days 7-8)**
15. Create `DriverContractService`
16. Implement contract generation workflow
17. Implement version management
18. Implement snapshot creation
19. Add error handling

### **Sprint 5: API Endpoints (Days 9-10)**
20. Create DTOs
21. Create `GET /drivers/{id}/contracts` endpoint
22. Create `GET /drivers/{id}/contracts/latest` endpoint
23. Create `GET /drivers/{id}/contracts/{versionId}/download` endpoint
24. Create `POST /drivers/{id}/contracts/regenerate` endpoint
25. Add authorization checks

### **Sprint 6: Integration (Day 11)**
26. Integrate with `POST /drivers/create-with-contract`
27. Integrate with `PUT /drivers/{id}/with-contract`
28. Test end-to-end flow
29. Handle errors gracefully

### **Sprint 7: Testing & Polish (Days 12-13)**
30. Write unit tests
31. Write integration tests
32. Manual testing
33. Fix bugs
34. Documentation

### **Sprint 8: Frontend (Days 14-15)**
35. Update driver creation response
36. Add contract versions list UI
37. Add download button
38. Update driver details page

---

## ✅ Success Criteria

**Feature Complete When:**

1. ✅ Driver creation automatically generates contract PDF
2. ✅ Contract update generates new version (when relevant fields change)
3. ✅ All contract versions stored with full audit trail
4. ✅ PDF contains all 15 articles with correct data
5. ✅ Dutch formatting for dates and currency
6. ✅ CAO pay scale and vacation days correctly looked up
7. ✅ File storage works reliably
8. ✅ API endpoints secured with proper authorization
9. ✅ Drivers can download own contracts
10. ✅ Admins can download any contract
11. ✅ Version history visible in UI
12. ✅ Error handling works for all edge cases
13. ✅ Tests pass (unit + integration)
14. ✅ Documentation complete

---

## 🚨 Risk Mitigation

### **Risk 1: PDF Generation Failure**
**Mitigation:**
- Extensive validation before generation
- Detailed error logging
- Manual regeneration endpoint
- Don't block driver creation if PDF fails

### **Risk 2: File Storage Full**
**Mitigation:**
- Monitor disk usage
- Implement file size limits
- Setup alerts at 80% capacity
- Retention policy (archive old contracts)

### **Risk 3: Missing CAO Data**
**Mitigation:**
- Validate CAO lookup tables on startup
- Error if scale/step not found
- Update CAO data annually (via seeder)

### **Risk 4: Concurrent Version Creation**
**Mitigation:**
- Use database transactions
- Unique constraint on (DriverId, VersionNumber)
- Retry logic if version conflict

### **Risk 5: Large PDF Files**
**Mitigation:**
- Optimize images/fonts
- Set max file size (10MB)
- Compress PDFs if needed

---

## 📚 Documentation Deliverables

1. **Technical Docs:**
   - API endpoint documentation
   - Service class documentation (XML comments)
   - Database schema documentation

2. **User Docs:**
   - How to download contracts
   - How to view contract history
   - How to regenerate contracts

3. **Admin Docs:**
   - File storage setup
   - Backup procedures
   - Troubleshooting guide

---

## 🎯 Next Steps

**After completing this plan:**

1. Review with team
2. Estimate effort (story points/hours)
3. Break into tickets/tasks
4. Assign to sprint
5. Start implementation!

---

**End of Implementation Plan** 📋

