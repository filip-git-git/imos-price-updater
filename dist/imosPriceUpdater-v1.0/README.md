# imos Price Updater v1.0

Desktop application for updating material prices in IMOS CAD database from CSV files.

## Requirements

- Windows 10/11 (64-bit)
- .NET 8 Desktop Runtime
- SQL Server 2019/2022/2025
- Network access to SQL Server (TCP 1433)

## Installation

1. Extract ZIP to desired location (e.g., `C:\Tools\imosPriceUpdater`)
2. Copy `sample.config.json` to any location you prefer
3. Edit the copied file with your SQL Server settings
4. Run `imosPriceUpdater.exe`

## Configuration

Use `sample.config.json` as template. Edit it with your settings:

```json
{
  "SqlConnection": {
    "Server": "YOUR_SERVER",
    "Database": "IMOS",
    "AuthenticationMode": "SqlAuthentication",
    "Username": "IMOSADMIN",
    "Password": "YOUR_PASSWORD"
  },
  "ColumnMapping": {
    "SqlTable": "MAT",
    "CsvSearchColumn": "Purchase Item ID",
    "CsvPriceColumn": "assigned price",
    "SqlSearchColumn": "BESTELLUNG",
    "SqlPriceColumn": "COST"
  }
}
```

### Column Mapping Explained

| Field | Description | Example |
|-------|-------------|---------|
| `SqlTable` | Name of SQL table to update | `MAT` |
| `CsvSearchColumn` | Column name in your CSV file used to find rows | `Purchase Item ID` |
| `CsvPriceColumn` | Column name in CSV file containing prices | `assigned price` |
| `SqlSearchColumn` | Column name in SQL table (matches CsvSearchColumn) | `BESTELLUNG` |
| `SqlPriceColumn` | Column name in SQL table to update | `COST` |

**Important:** Column names in CSV must match exactly (case-sensitive).

In the application:
1. Click **"Load Configuration"** and select your config file
2. Click **"Test Connection"** to verify SQL Server access
3. Load your CSV file and proceed with price update

## Data Locations

| Type | Path |
|------|------|
| Logs | `%LOCALAPPDATA%\imosPriceUpdater\Logs\` |
| Backups | `%APPDATA%\imosPriceUpdater\backups\` |
| History | `%APPDATA%\imosPriceUpdater\history\` |

## Troubleshooting

- **Connection failed**: Verify SQL Server address and credentials
- **Permission denied**: Ensure user has read/write access to database
- **CSV not loading**: Check file encoding (UTF-8 recommended)

## Support

For issues, contact your system administrator or IT department.
