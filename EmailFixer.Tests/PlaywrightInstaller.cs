namespace EmailFixer.Tests;

/// <summary>
/// Helper class для установки Playwright браузеров перед E2E тестами
/// Должен быть вызван перед запуском E2E тестов:
/// pwsh -Command "[System.Reflection.Assembly]::LoadFrom('packages/microsoft.playwright.1.48.0/lib/net8.0/Microsoft.Playwright.dll') | Out-Null; [Microsoft.Playwright.Program]::Main(new string[] { 'install' })"
/// </summary>
public static class PlaywrightInstaller
{
    /// <summary>
    /// Установить Playwright браузеры
    /// Запустить один раз перед первым запуском E2E тестов
    /// </summary>
    public static void Install()
    {
        // Эта функция требует запуска с помощью CLI:
        // dotnet test --filter "E2E" --configuration Release
        // ИЛИ установить вручную:
        // pwsh -Command "Microsoft.Playwright.Program.Main(new string[] { 'install' })"
    }
}
