# Weather Data Analysis System

A console-based weather analysis system using Entity Framework Code First to analyze 156,945 temperature and humidity measurements from CSV data with interactive menu, mold risk calculation, and meteorological season detection.

## Overview

This project demonstrates Entity Framework Code First approach by automatically creating a SQLite database from C# classes and performing complex LINQ queries on weather data from 2016.

**Key Features:**
- Automatic database creation (Code First)
- CSV import with error handling (156,945 measurements)
- 10 different analyses (6 outdoor + 4 indoor)
- Interactive menu with Spectre.Console
- Mold risk calculation algorithm
- Meteorological season detection

## Tech Stack

- **.NET 10** - Application framework
- **Entity Framework Core 10** - ORM and Code First
- **SQLite** - Database
- **CsvHelper** - CSV parsing
- **Spectre.Console** - Interactive UI

## Analyses

### Outdoor Analysis:
1. Average temperature for selected date
2. Warmest/coldest days sorting
3. Driest/most humid days sorting
4. Mold risk calculation and sorting
5. Meteorological autumn detection (5 days < 10°C)
6. Meteorological winter detection (5 days < 0°C)

### Indoor Analysis:
1. Average temperature
2. Temperature sorting
3. Humidity sorting
4. Mold risk analysis

### Bonus Features:
- **Balcony Door Analysis** - Estimates when door was open based on temperature differences
- **Temperature Difference Analysis** - Identifies days with smallest/largest indoor-outdoor differences

## Architecture

**Layered Design:**
```
Presentation Layer  → AnalysisPresenter.cs (UI logic)
Service Layer       → AnalysisService.cs (business logic)
Data Layer          → WeatherDataContext.cs (database)
Model Layer         → Measurement.cs (entities)
```

**Design Principles:**
- Separation of Concerns
- DRY (Don't Repeat Yourself)
- Single Responsibility Principle
- Clean Code

## Quick Start

```bash
# Clone the repository
git clone [repo-url]

# Navigate to project
cd WeatherDataAnalysis

# Place TempFuktData.csv in project root

# Run
dotnet run
```

## Learning Outcomes

- Entity Framework Code First approach
- LINQ queries (GroupBy, Select, OrderBy, Average)
- Database design and ORM concepts
- CSV handling with error recovery
- Layered architecture implementation
- Interactive console UI with Spectre.Console


**Author:** Djan Karis Lomongo Freolo 

**Course:** KYHA_DSO25

**Published Date:** 26-11-2025
