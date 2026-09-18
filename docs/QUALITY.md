# Verification record

Verified 18 September 2026 on Windows. This is an educational application; these checks do not establish clinical or production suitability.

## Automated checks

- 27/27 xUnit cases passed locally and in the Windows CI build.
- Cases cover insert/reopen persistence, update identity, deletion persistence, missing records, duplicate inserts and updates, required fields, length validation, invalid status, trimming, combined filtering, SQL-like search text, Unicode/apostrophes, CSV quoting, formula neutralization and empty export.
- Release build uses warnings as errors.
- Published executable smoke test passed in CI: blank-form validation, add, edit status, search, status filter, CSV export, rendering and deletion against an isolated temporary database. The smoke test calls the same deletion method as the UI; the confirmation dialog itself requires the manual step below.
- Dependency scan after updating SQLitePCLRaw to 2.1.13: no known vulnerable packages reported by the configured NuGet sources.
- [Verified build and distributable](https://github.com/PQDReal/labtrack/actions/runs/35305421367), implementation commit `1007e40`.
- Downloaded that CI ZIP, extracted it locally, and ran the packaged executable's UI smoke test successfully. ZIP SHA-256: `9B5DF59DA6D69ABB8F5478F438254BAAE88CB5BB3CAE22E88B59B576956B29E5`.

## Reproduce

```powershell
dotnet test tests/LabTrack.Tests -c Release
./scripts/publish.ps1
dotnet list tests/LabTrack.Tests package --vulnerable --include-transitive
```

The publish script also runs `scripts/smoke.ps1`, which uses a fresh directory under `artifacts/` and saves the test database, CSV, rendered image and PASS/FAIL record. It never uses the normal user database.

## Manual learning checks

- Launch the release, create synthetic samples, close and reopen to inspect persistence.
- Click Delete selected, choose No and check the row remains; choose Yes to delete it.
- Export with a filter active and open the CSV to confirm only matching samples are present.
- Resize the window and inspect controls. The supplied screenshot is generated from the form; native combo-box text can be omitted by WinForms DrawToBitmap.

These manual steps are provided for the learner; they are not claimed as automated coverage. Automated tests cannot demonstrate that the candidate understands the generated code.
