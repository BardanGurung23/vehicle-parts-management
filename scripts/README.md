## Admin bootstrap

Use `bootstrap-first-admin.sh` only for non-demo environments where you want an extra manual admin account after the EF migration baseline has been applied.

Example:

```bash
./bootstrap-first-admin.sh "Admin User" admin@example.com 9800000000 SecurePass123
```

Optional fifth argument:

- PostgreSQL connection string override

If no override is provided, the script reads configuration through `Vpims.API/appsettings.json` plus your local development overrides.

For local Development and test, starting `Vpims.API` is usually enough. The API now auto-resets mismatched local databases, applies the committed EF Core baseline migration, and seeds the canonical demo dataset.

## Demo users

Use the `Vpims.SeedDemoUsers` tool to reapply the canonical Autonix demo dataset directly into the configured PostgreSQL database.

All seeded demo users use the same password:

- `DemoPass123!`

Seeded demo accounts:

- `demo.admin1@autonix.local`
- `demo.admin2@autonix.local`
- `demo.staff1@autonix.local`
- `demo.staff2@autonix.local`
- `demo.customer1@autonix.local`
- `demo.customer2@autonix.local`
- `demo.customer3@autonix.local`

## Demo dataset cleanup

Use `cleanup-demo-users.sh` to remove older ad hoc, verification, or end-to-end test users and keep only the intended `demo.*@autonix.local` submission accounts.

The cleanup tool now targets the EF-backed final schema instead of the legacy SQL-only table names.
