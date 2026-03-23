# VervoerManager Frontend – Development Setup

## Prerequisites
- Node.js 18+ (LTS)
- npm or pnpm

## Quick Start

```bash
git clone https://github.com/andreyroizin/trucks-management-frontend.git
cd trucks-management-frontend
npm install
cp .env.example .env.local   # if exists, or create manually
npm run dev
```

Open http://localhost:3000

## Environment Variables

Create `.env.local`:

```env
NEXT_PUBLIC_API_BASE_URL=http://localhost:5000
```

For local dev against live API:

```env
NEXT_PUBLIC_API_BASE_URL=https://api.vervoermanager.nl
```

## Commands

| Command | Description |
|---------|-------------|
| `npm run dev` | Start dev server (hot reload) |
| `npm run build` | Production build |
| `npm start` | Run production server |
| `npm run lint` | Run ESLint |

## Backend Required

The frontend calls the API. Either:
- Run backend locally (see backend repo `docs/setup/DEVELOPMENT.md`)
- Point `NEXT_PUBLIC_API_BASE_URL` at live API

## Locales

Supported: `en`, `nl`, `bg`. Default: `en`. URLs: `/[locale]/...` (e.g. `/nl/drivers`).
