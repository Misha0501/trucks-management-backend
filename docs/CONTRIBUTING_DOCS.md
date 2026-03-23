# Contributing to VervoerManager Documentation

This documentation is the **source of truth** for both the team and AI assistants. When you add or change a feature, update the docs so the next person or model can work without re-analysis.

---

## Instructions for AI models and developers adding new features

When you implement a new feature or significantly change existing behavior:

1. **Update or create a feature doc** in `docs/features/`
   - Use the [Feature doc template](#feature-doc-template) below
   - Create `docs/features/<FEATURE>.md` if it doesn't exist
   - Update the existing feature doc if the feature already exists

2. **Update the docs index** – Add or adjust the entry in `docs/INDEX.md` under "Features"

3. **Update API docs if you add endpoints** – Add new endpoints to `docs/api/ENDPOINTS.md`

4. **Update data docs if you change schema** – Update `docs/data/SCHEMA.md` and optionally `docs/data/ENTITIES.md`

5. **Update requirements if applicable** – If the work maps to a Phase 1 requirement, update `docs/requirements/PHASE1_INDEX.md` status in the **frontend repo**

6. **Commit the doc changes** – Commit documentation edits together with the code changes

---

## Feature doc template

When creating or extending `docs/features/<FEATURE>.md`:

```markdown
# [Feature Name]

## Purpose
[1–2 sentences: what this feature does for the user/business]

## Status
- [x] Implemented | [ ] Partial | [ ] Planned

## Frontend
- **Routes**: `/[locale]/path/...`
- **Key pages**: e.g. `app/[locale]/drivers/page.tsx`
- **Components**: Main UI components involved
- **Hooks**: e.g. `useDrivers`, `useCreateDriver`

## Backend
- **Endpoints**: e.g. `GET /drivers`, `POST /drivers`, `GET /drivers/{id}`
- **Key services**: If any
- **Key entities**: e.g. Driver, DriverCompensationSettings

## Key types / DTOs
- List request/response shapes used by this feature

## Flows
[Optional: describe main user flows, e.g. "Create driver → Assign company → Generate contract"]

## Related
- Links to related feature docs
- Requirements: e.g. R21, R23
```

---

## Checklist: adding a new feature

- [ ] Create or update `docs/features/<FEATURE>.md` using the template
- [ ] Add entry to `docs/INDEX.md` under "Features"
- [ ] Add endpoints to `docs/api/ENDPOINTS.md` (if new API)
- [ ] Update `docs/data/SCHEMA.md` or `docs/data/ENTITIES.md` (if schema changes)
- [ ] Update `docs/requirements/PHASE1_INDEX.md` in **frontend repo** (if from Phase 1 requirement)

---

## Checklist: modifying an existing feature

- [ ] Update `docs/features/<FEATURE>.md` with the new behavior
- [ ] Update `docs/api/ENDPOINTS.md` if endpoints changed
- [ ] Update schema docs if entities changed

---

## File naming conventions

- **Features**: `UPPERCASE.md` (e.g. `DRIVERS.md`, `PARTRIDES.md`)
- **Setup**: `UPPERCASE.md` (e.g. `DEVELOPMENT.md`)
- **Other**: `UPPERCASE.md` or `PascalCase.md` for consistency

---

## Where to put new docs

| Type | Location |
|------|----------|
| New feature | `docs/features/<FEATURE>.md` |
| New API endpoint group | Add to `docs/api/ENDPOINTS.md` |
| New entity | Add to `docs/data/SCHEMA.md` and optionally `docs/data/ENTITIES.md` |
| New requirement / phase | Frontend repo: `docs/requirements/` |
| New setup step | Add to `docs/setup/DEVELOPMENT.md` or `DEPLOYMENT.md` |
