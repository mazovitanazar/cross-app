# CrossApp
Наскрізний проєкт з крос-платформного програмування.

Предметна область: Склад.
Сутності: Product (товар), StockBatch (партія), Warehouse (склад), Movement (переміщення).
Призначення: облік залишків товарів по партіях.

## Запуск
```bash
dotnet build
dotnet run --project src/Cli
```

Середовище
.NET SDK 10.0, Windows 11 x64

---

### Крок 2. Перший коміт і відправка на GitHub/GitLab

1. **Перевірте статус і заіндексуйте зміни:**
   ```cmd
   git status
   git add .
   ```

   ## Додаткове завдання
Виконано self-contained публікацію застосунку під дві різні RID-платформи:
- **Windows (`win-x64`)**: `src/Cli/bin/Release/net10.0/win-x64/publish/`
- **Linux (`linux-x64`)**: `src/Cli/bin/Release/net10.0/linux-x64/publish/`
- **Підтримка формату JSON**: Реалізовано обробку прапорця `--json` для виводу системної інформації у форматі JSON з коректним кодуванням UTF-8 (кирилиця).
   ```bash
   dotnet run --project src/Cli -- --json
   ```
