# Driver "Used By" Companies - Frontend Integration Guide

## 📋 Overview

This guide explains how to integrate the "Used By" companies feature for drivers in your frontend application. This feature allows a driver to belong to one company but be used by multiple companies (many-to-many relationship).

---

## 🎯 Feature Summary

- **Owner Company**: A driver has one primary company (`companyId`)
- **Used By Companies**: A driver can be used by multiple companies (`usedByCompanies[]`)
- **Same as Cars**: This works exactly like the car "Used By" companies feature

---

## 📡 API Endpoints

### 1. **GET /drivers** - List All Drivers

**Response includes `UsedByCompanies`:**

```json
{
  "success": true,
  "data": {
    "totalDrivers": 10,
    "pageNumber": 1,
    "pageSize": 100,
    "drivers": [
      {
        "id": "driver-guid-here",
        "companyId": "owner-company-guid",
        "companyName": "Owner Company Name",
        "carId": "car-guid-or-null",
        "carLicensePlate": "AB-123-CD",
        "user": {
          "id": "user-guid",
          "email": "driver@example.com",
          "firstName": "John",
          "lastName": "Doe"
        },
        "usedByCompanies": [
          {
            "id": "company-1-guid",
            "name": "Company A"
          },
          {
            "id": "company-2-guid",
            "name": "Company B"
          }
        ]
      }
    ]
  }
}
```

---

### 2. **GET /drivers/{driverId}/with-contract** - Get Driver Details

**Response includes `UsedByCompanies`:**

```json
{
  "success": true,
  "data": {
    "userId": "user-guid",
    "email": "driver@example.com",
    "firstName": "John",
    "lastName": "Doe",
    "driverId": "driver-guid",
    "companyId": "owner-company-guid",
    "companyName": "Owner Company Name",
    "carId": "car-guid-or-null",
    "carLicensePlate": "AB-123-CD",
    "contractId": "contract-guid",
    "contractStatus": "Signed",
    "files": [...],
    "usedByCompanies": [
      {
        "id": "company-1-guid",
        "name": "Company A"
      },
      {
        "id": "company-2-guid",
        "name": "Company B"
      }
    ],
    "createdAt": "2024-11-22T10:00:00Z"
  }
}
```

---

### 3. **POST /drivers/create-with-contract** - Create Driver

**Request body includes `usedByCompanyIds` (optional):**

```json
{
  "email": "newdriver@example.com",
  "password": "SecurePassword123!",
  "firstName": "Jane",
  "lastName": "Smith",
  "companyId": "owner-company-guid",
  "dateOfEmployment": "2024-11-22T00:00:00Z",
  "workweekDuration": 40.0,
  "function": "Chauffeur",
  "address": "123 Main St",
  "phoneNumber": "+31612345678",
  "postcode": "1234AB",
  "city": "Amsterdam",
  "country": "Netherlands",
  "usedByCompanyIds": [
    "company-1-guid",
    "company-2-guid",
    "company-3-guid"
  ]
}
```

**Notes:**
- `usedByCompanyIds` is **optional** and can be omitted
- The backend validates that all company IDs exist
- Non-existent companies are silently skipped (no error)

---

### 4. **PUT /users/{userId}/driver** - Update Driver

**Request body includes `usedByCompanyIds` (optional):**

```json
{
  "companyId": "new-owner-company-guid",
  "carId": "new-car-guid",
  "usedByCompanyIds": [
    "company-1-guid",
    "company-2-guid"
  ]
}
```

**Important Behaviors:**
- **`usedByCompanyIds: null`** → Don't update (keep existing associations)
- **`usedByCompanyIds: []`** → Clear all (remove all associations)
- **`usedByCompanyIds: [...]`** → Replace all (remove existing, add new)

**Response includes updated `usedByCompanies`:**

```json
{
  "success": true,
  "data": {
    "driverId": "driver-guid",
    "companyId": "company-guid",
    "companyName": "Company Name",
    "carId": "car-guid",
    "carLicensePlate": "AB-123-CD",
    "carVehicleYear": 2022,
    "carRegistrationDate": "2022-01-15",
    "usedByCompanies": [
      {
        "id": "company-1-guid",
        "name": "Company A"
      },
      {
        "id": "company-2-guid",
        "name": "Company B"
      }
    ]
  }
}
```

---

## 🎨 Frontend Implementation Examples

### Example 1: Display Driver with "Used By" Companies

```typescript
interface Driver {
  id: string;
  companyId: string;
  companyName: string;
  user: {
    firstName: string;
    lastName: string;
    email: string;
  };
  usedByCompanies: Array<{
    id: string;
    name: string;
  }>;
}

function DriverCard({ driver }: { driver: Driver }) {
  return (
    <div className="driver-card">
      <h3>{driver.user.firstName} {driver.user.lastName}</h3>
      <p>Email: {driver.user.email}</p>
      <p>Owner Company: {driver.companyName}</p>
      
      {driver.usedByCompanies.length > 0 && (
        <div>
          <h4>Can Be Used By:</h4>
          <ul>
            {driver.usedByCompanies.map((company) => (
              <li key={company.id}>{company.name}</li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
```

---

### Example 2: Multi-Select for "Used By" Companies (Create Driver)

```typescript
import { useState } from 'react';

interface Company {
  id: string;
  name: string;
}

function CreateDriverForm() {
  const [selectedCompanies, setSelectedCompanies] = useState<string[]>([]);
  const [availableCompanies, setAvailableCompanies] = useState<Company[]>([]);

  // Fetch available companies
  useEffect(() => {
    fetchCompanies().then(setAvailableCompanies);
  }, []);

  const handleSubmit = async (formData: any) => {
    const request = {
      ...formData,
      usedByCompanyIds: selectedCompanies // Add selected companies
    };
    
    await fetch('/drivers/create-with-contract', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(request)
    });
  };

  return (
    <form onSubmit={handleSubmit}>
      {/* Other form fields... */}
      
      <div>
        <label>Companies That Can Use This Driver:</label>
        <select 
          multiple 
          value={selectedCompanies}
          onChange={(e) => {
            const selected = Array.from(e.target.selectedOptions, option => option.value);
            setSelectedCompanies(selected);
          }}
        >
          {availableCompanies.map((company) => (
            <option key={company.id} value={company.id}>
              {company.name}
            </option>
          ))}
        </select>
      </div>
      
      <button type="submit">Create Driver</button>
    </form>
  );
}
```

---

### Example 3: Update "Used By" Companies (Edit Driver)

```typescript
function EditDriverUsedByCompanies({ driver }: { driver: Driver }) {
  const [selectedCompanies, setSelectedCompanies] = useState<string[]>(
    driver.usedByCompanies.map(c => c.id)
  );

  const handleUpdate = async () => {
    await fetch(`/users/${driver.user.id}/driver`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        usedByCompanyIds: selectedCompanies // Replace all associations
      })
    });
  };

  const handleClearAll = async () => {
    await fetch(`/users/${driver.user.id}/driver`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        usedByCompanyIds: [] // Clear all associations
      })
    });
  };

  return (
    <div>
      <h3>Edit Companies That Can Use This Driver</h3>
      
      {/* Multi-select dropdown */}
      <select 
        multiple 
        value={selectedCompanies}
        onChange={(e) => {
          const selected = Array.from(e.target.selectedOptions, option => option.value);
          setSelectedCompanies(selected);
        }}
      >
        {availableCompanies.map((company) => (
          <option key={company.id} value={company.id}>
            {company.name}
          </option>
        ))}
      </select>
      
      <button onClick={handleUpdate}>Save Changes</button>
      <button onClick={handleClearAll}>Clear All</button>
    </div>
  );
}
```

---

### Example 4: Using React Hook Form + Material-UI

```typescript
import { useForm, Controller } from 'react-hook-form';
import { Autocomplete, TextField, Chip } from '@mui/material';

interface DriverFormData {
  email: string;
  firstName: string;
  lastName: string;
  companyId: string;
  usedByCompanyIds: string[];
}

function CreateDriverFormMUI() {
  const { control, handleSubmit } = useForm<DriverFormData>();
  const [companies, setCompanies] = useState<Company[]>([]);

  const onSubmit = async (data: DriverFormData) => {
    await fetch('/drivers/create-with-contract', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      {/* Other fields... */}
      
      <Controller
        name="usedByCompanyIds"
        control={control}
        defaultValue={[]}
        render={({ field }) => (
          <Autocomplete
            multiple
            options={companies}
            getOptionLabel={(option) => option.name}
            value={companies.filter(c => field.value.includes(c.id))}
            onChange={(_, newValue) => {
              field.onChange(newValue.map(c => c.id));
            }}
            renderInput={(params) => (
              <TextField
                {...params}
                label="Companies That Can Use This Driver"
                placeholder="Select companies"
              />
            )}
            renderTags={(value, getTagProps) =>
              value.map((option, index) => (
                <Chip
                  label={option.name}
                  {...getTagProps({ index })}
                  key={option.id}
                />
              ))
            }
          />
        )}
      />
      
      <button type="submit">Create Driver</button>
    </form>
  );
}
```

---

## 🔑 Key Points for Frontend Developers

### 1. **Data Structure**
- `usedByCompanies` is an **array** of objects with `id` and `name`
- Always present in GET responses (even if empty `[]`)

### 2. **Creating Drivers**
- `usedByCompanyIds` is **optional** in POST requests
- Can be omitted entirely or set to `[]` (same effect)
- Use an array of company ID strings: `["guid-1", "guid-2"]`

### 3. **Updating Drivers**
- **Don't include `usedByCompanyIds`** if you don't want to update it
- **Send `[]`** to remove all associations
- **Send `[...ids]`** to replace all associations (replaces, not appends!)

### 4. **Authorization**
- **Customer Admins** can only assign companies they manage
- Invalid companies are silently skipped (no error thrown)
- Always verify user permissions before showing UI

### 5. **UI Recommendations**
- Use multi-select dropdowns or tag inputs
- Show owner company separately from "used by" companies
- Display a clear difference between owner and "can be used by"
- Add a "Clear All" button for easy removal

---

## ✅ Testing Checklist

- [ ] Can create driver without `usedByCompanyIds` (should work)
- [ ] Can create driver with `usedByCompanyIds: []` (should work)
- [ ] Can create driver with multiple companies in `usedByCompanyIds`
- [ ] GET /drivers returns `usedByCompanies` array
- [ ] GET /drivers/{id}/with-contract returns `usedByCompanies` array
- [ ] Can update driver with new `usedByCompanyIds` (replaces existing)
- [ ] Can clear all associations with `usedByCompanyIds: []`
- [ ] Omitting `usedByCompanyIds` in PUT request doesn't change associations
- [ ] Invalid company IDs are gracefully handled (no errors)
- [ ] Customer admin can only assign their own companies

---

## 🐛 Common Issues & Solutions

### Issue 1: "Used By" companies not showing
**Solution:** Ensure you're fetching the latest driver data after creating/updating.

### Issue 2: Associations not updating
**Solution:** Make sure you're sending `usedByCompanyIds`, not `usedByCompanies` (IDs vs objects).

### Issue 3: Getting 403 Forbidden
**Solution:** Customer admins can only assign companies they manage. Check user permissions.

### Issue 4: Duplicate entries
**Solution:** The backend prevents duplicates automatically (unique index). No need to check on frontend.

---

## 📞 Questions?

If you have questions about this feature, check:
1. The car "Used By" companies implementation (same logic)
2. The backend endpoint code in `DriverEndpoints.cs` and `UserEndpoints.cs`
3. The migration file: `20251122154457_AddDriverUsedByCompanies.cs`

---

**Happy Coding!** 🚀

