# TraceQ.Cli.Tests

Unit tests for the TraceQ CLI tool (`tq`). Tests validate command behavior using fixture CSV files.

## Running

```bash
dotnet test
```

## Test classes

| Class | File | Coverage |
|-------|------|----------|
| `ValidateCommandTests` | Root | Valid files (minimal + full), nonexistent file, wrong extension, empty file, missing required columns, row errors, unknown column warnings |

## Test data

Fixture files in `tests/TestData/cli/`:

| File | Purpose |
|------|---------|
| `valid_minimal.csv` | Two rows with only required columns (Number, Name) |
| `valid_full.csv` | Two rows with all 12 Windchill columns |
| `empty.csv` | Zero-byte file |
| `missing_number_column.csv` | CSV missing the required `Number` column |
| `row_errors.csv` | Rows with missing/blank `RequirementNumber` values |
| `unknown_columns.csv` | CSV with extra non-Windchill columns (warns but passes) |
| `not_a_csv.txt` | Wrong file extension |

Tests call `ValidateCommand.ExecuteAsync` directly and capture console output to verify messages and exit codes.

## Dependencies

- xUnit 2.5.3
- FluentAssertions 8.9.0
