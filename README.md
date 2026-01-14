# QuickBooks Desktop -> Supabase (MVP)

Minimal local SOAP server for QuickBooks Web Connector (QBWC) to verify connectivity and capture qbXML responses.

## What this MVP does
- Exposes the QBWC SOAP endpoints via a local WCF service.
- Sends `CustomerQueryRq`, `InvoiceQueryRq`, `BillQueryRq`, and `EstimateQueryRq` (read-only).
- Invoices/Bills/Estimates use iterator paging (`PageSize` in `App.config`).
- Incremental mode uses `TimeModified` filters and stores state in `logs/sync_state.txt`.
- Logs raw qbXML requests and responses to disk so you can inspect data.
- Exports parsed JSON per entity to `logs/json`.

## Prereqs
- Windows + QuickBooks Desktop
- QuickBooks Web Connector (QBWC)
- .NET SDK (to build) or Visual Studio with .NET desktop development

## Install on a new computer (from scratch)
1) Clone this repo.
2) Install QuickBooks Desktop and open the company file once.
3) Install QuickBooks Web Connector.
4) Install the .NET SDK (or Visual Studio).
5) Reserve the local URL (PowerShell as Administrator):
   - `netsh http add urlacl url=http://+:8000/QBWC/ user=%USERNAME%`
6) Update `qwc/QuickBooksMVP.qwc`:
   - Set `<CompanyFile>` to your `.qbw` path (optional but recommended).
   - For first-time add, set `<IsReadOnly>false</IsReadOnly>`.
7) Update `src/ConnectorApp/App.config` if needed:
   - `Username`, `Password`, `CompanyFile`, `PageSize`, `SyncMode`.
8) Build:
   - `dotnet build .\src\ConnectorApp\ConnectorApp.csproj -c Release`
9) Run:
   - `.\src\ConnectorApp\bin\Release\net48\ConnectorApp.exe`
10) In QBWC, add `qwc/QuickBooksMVP.qwc` and click "Update Selected".
11) After first add succeeds, set `<IsReadOnly>true</IsReadOnly>` in the QWC file and re-add it.
12) Inspect logs:
   - `src/ConnectorApp/bin/Release/net48/logs`
13) If starting fresh, copy `qwc/QuickBooksMVP.template.qwc` to a new file and replace the GUIDs and company file path.

## Run (daily use)
1) Start the server:
   - `.\src\ConnectorApp\bin\Release\net48\ConnectorApp.exe`
2) In QBWC, click "Update Selected".
3) Inspect `src/ConnectorApp/bin/Release/net48/logs` for qbXML requests/responses (default).

## QWC file
- Path: `qwc/QuickBooksMVP.qwc`
- Update `<UserName>` to match `App.config` credentials if you change them.
- The default server URL is `http://localhost:8000/QBWC` (WSDL at `?wsdl`).
- First-time add to QuickBooks requires `IsReadOnly=false` so QBWC can register its FileID. After it is added, you can switch it back to `true`.

## Notes
- Credentials are checked against `App.config` appSettings. Defaults are `qbwc` / `qbwc`.
- This MVP only sends read-only QueryRq payloads.

## Next
- Add incremental sync logic + iterator paging.
- Parse qbXML into structured rows and upsert to Supabase.
