# Playwright BDD

The complete, self-contained onboarding guide is [README.html](README.html). It covers architecture, scenario IDs, configuration, utilities, execution, reports, setup, commands, troubleshooting, and known limitations.

Quick commands from the repository root:

```powershell
dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build --logger "trx"
dotnet test --configuration Release --no-build --filter "TestCategory=TC001"
```

Login tests require credentials through environment variables or the ignored local `logincredentials.json`. Dynamic retries with a positive retry count are not supported by the current Reqnroll/NUnit integration; see the HTML guide for details.