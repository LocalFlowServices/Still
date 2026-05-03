# Changelog

## 2026-05-03 — Batch CODEX-TRIAGE-STEP001-2026-05-03
- Files modified: `StillMenu/Scopable_App.cs`, `StillMenu/build_scopable.ps1`, `00_AppShell/data/current_status.json`, `00_AppShell/data/repair_ledger.json`, `docs/dev100/dev100_ledger.md`, `docs/implementation/implementation_report_2026-05-03.md`.
- Systems touched: Scopable WPF app shell, compile recovery tooling, App Shell status ledger.
- Canon state changes: none promoted; STEP-001 remains pending in this repo snapshot.
- Dev score changes: Scopable compile pathway 25→75.
- Tests run: repository/status and static inspection only in Linux container.
- Known limitations: Windows-only compile and launch still required on host.
- Next step: execute Windows build + launch validation and then proceed to STEP-001 Java module stabilization when sources are present.
