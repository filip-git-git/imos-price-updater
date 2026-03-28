# imos Price Updater

Desktop application for bulk updating material prices in IMOS CAD database from CSV files.

## Features

- CSV Import with preview
- Visual column mapping (no SQL knowledge required)
- Batch processing for large files (5000+ rows)
- Transaction support with rollback on failure

## Requirements

- Windows 10/11 (64-bit)
- .NET 8 Desktop Runtime
- SQL Server 2019/2022/2025

## Quick Start

1. Download `imosPriceUpdater-v1.0.1.zip` from releases
2. Extract to any folder
3. Copy `sample.config.json` and edit with your SQL settings
4. Run `imosPriceUpdater.exe`

## Configuration

Edit JSON config:
- `Server` - SQL Server address
- `Database` - IMOS database name
- `Username` / `Password` - SQL credentials
- `SqlTable` - Table to update (e.g., `MAT`)
- `CsvSearchColumn` / `SqlSearchColumn` - Key column mapping
- `CsvPriceColumn` / `SqlPriceColumn` - Price column mapping

## Tech Stack

- .NET 8 / WPF
- Microsoft.Data.SqlClient
- CsvHelper

## License

Apache License 2.0
