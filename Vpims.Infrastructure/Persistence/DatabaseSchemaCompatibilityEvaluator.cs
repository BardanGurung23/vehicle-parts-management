namespace Vpims.Infrastructure.Persistence;

public sealed class DatabaseSchemaCompatibilityEvaluator
{
    public DatabaseSchemaCompatibilityStatus Evaluate(DatabaseSchemaInspection inspection)
    {
        if (!inspection.InspectionSucceeded)
        {
            return DatabaseSchemaCompatibilityStatus.NotChecked;
        }

        if (!inspection.HasUserTables)
        {
            return DatabaseSchemaCompatibilityStatus.Empty;
        }

        if (inspection.MissingRequiredTables.Count > 0)
        {
            return DatabaseSchemaCompatibilityStatus.Incompatible;
        }

        if (inspection.MissingRequiredColumns.Count > 0)
        {
            return DatabaseSchemaCompatibilityStatus.Incompatible;
        }

        if (inspection.DeprecatedTablesPresent.Count > 0)
        {
            return DatabaseSchemaCompatibilityStatus.Incompatible;
        }

        if (!inspection.MigrationHistoryExists)
        {
            return DatabaseSchemaCompatibilityStatus.Incompatible;
        }

        if (inspection.AppliedMigrationCount == 0)
        {
            return DatabaseSchemaCompatibilityStatus.Incompatible;
        }

        if (inspection.UnknownAppliedMigrations.Count > 0)
        {
            return DatabaseSchemaCompatibilityStatus.Incompatible;
        }

        return DatabaseSchemaCompatibilityStatus.Compatible;
    }
}