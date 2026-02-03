# Network Monitor Service

Den här katalogen innehåller en .NET-tjänst som övervakar nätverkstrafik, analyserar paket, loggar händelser och skapar statistik per webbplats i en SQLite-databas.

## Funktioner

- Fångar paket med SharpPcap och tolkar dem med PacketDotNet.
- Loggar varje trafik-händelse i `traffic_events`.
- Aggregerar webbplatsstatistik per dag i `website_stats`.
- Konfigureras via `appsettings.json` eller miljövariabler.
- Har ett webgränssnitt där du kan välja nätverkskort.

## Kom igång

1. Installera .NET 8 SDK.
2. Säkerställ att SharpPcap har rättigheter att läsa nätverkskort (kan kräva admin/root).
3. Starta tjänsten:

```bash
cd NetworkMonitor
dotnet run
```

Öppna sedan webgränssnittet på `http://localhost:5000` (eller den port som Kestrel loggar). Där kan du välja nätverkskort och spara valet i `appsettings.json`. Starta om tjänsten för att byta capture‑enhet.

## Konfiguration

`appsettings.json` innehåller inställningar:

```json
{
  "Monitor": {
    "DatabasePath": "network-monitor.db",
    "DeviceName": "",
    "MaxPayloadBytes": 4096
  }
}
```

- `DatabasePath`: Sökväg till SQLite-databasen.
- `DeviceName`: Namn eller beskrivning av nätverkskortet som ska användas (tomt = första tillgängliga).
- `MaxPayloadBytes`: Max antal byte som analyseras per paket när HTTP-headers tolkas.

Miljövariabler kan användas med prefixet `NETWORKMONITOR_`, t.ex. `NETWORKMONITOR_Monitor__DatabasePath`.

## Databas

### traffic_events

| Kolumn | Beskrivning |
| --- | --- |
| timestamp | Tidpunkt (UTC) |
| source_ip | Käll-IP |
| destination_ip | Mål-IP |
| protocol | TCP/UDP |
| destination_port | Målport |
| hostname | Host-header (om HTTP) |
| url | URL (om HTTP) |
| bytes | Total längd |

### website_stats

| Kolumn | Beskrivning |
| --- | --- |
| hostname | Domän |
| date | Datum (YYYY-MM-DD) |
| request_count | Antal förfrågningar |
| bytes | Total mängd bytes |

## Nästa steg

- Lägg till DNS-parsning för att få domäner även för HTTPS.
- Lägg till schemalagda rapporter eller export.
- Integrera ett web-UI för visualisering.
