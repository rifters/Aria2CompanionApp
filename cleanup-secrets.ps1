# ====================================================================
# ARIA2 COMPANION APP - SECRET CLEANUP SCRIPT
# ====================================================================
# This script removes your personal RPC token and IP from Git history
# 
# ??  WARNING: This rewrites Git history and requires force push
# ??  All collaborators will need to re-clone after running this
# ====================================================================

Write-Host "?? Aria2 Companion Secret Cleanup" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""

# Check if git-filter-repo is installed
if (!(Get-Command git-filter-repo -ErrorAction SilentlyContinue)) {
    Write-Host "? git-filter-repo not found. Installing via pip..." -ForegroundColor Yellow
    python -m pip install git-filter-repo
}

# Backup current state
Write-Host "?? Creating backup..." -ForegroundColor Yellow
git branch backup-before-cleanup
Write-Host "? Backup branch created: backup-before-cleanup" -ForegroundColor Green
Write-Host ""

# Create sanitized Settings.cs
Write-Host "?? Creating sanitized Settings.cs..." -ForegroundColor Yellow
$sanitizedSettings = @"

/// <summary>
/// Default configuration for Aria2 Companion App.
/// These are safe defaults for first-time users.
/// 
/// ACTUAL SETTINGS ARE STORED IN: settings.json (gitignored)
/// 
/// To configure:
/// 1. Run the app
/// 2. Right-click tray icon ? Settings
/// 3. Enter your Aria2 server details
/// 4. Settings saved to settings.json (never committed to Git)
/// </summary>
internal static class Settings
{
    // Default RPC connection (localhost)
    public const string RpcUrl = "http://localhost:6800/jsonrpc";
    public const string WsUrl = "ws://localhost:6800/jsonrpc";

    // Empty by default - user must configure
    public const string Token = "";

    // Default download directory on Aria2 server
    public const string DefaultAria2DownloadDir = "/downloads";

    // Polling interval in milliseconds
    public const int PollIntervalMs = 3000;
}
"@

Set-Content -Path "src/TrayApp/Settings.cs" -Value $sanitizedSettings -Encoding UTF8
Write-Host "? Settings.cs sanitized with safe defaults" -ForegroundColor Green
Write-Host ""

# Create .gitignore entry if not exists
Write-Host "?? Updating .gitignore..." -ForegroundColor Yellow
$gitignoreContent = Get-Content .gitignore -Raw -ErrorAction SilentlyContinue
if ($gitignoreContent -notmatch "settings\.json") {
    Add-Content -Path .gitignore -Value "`n# User-specific settings (contains secrets)`nsettings.json"
    Write-Host "? Added settings.json to .gitignore" -ForegroundColor Green
} else {
    Write-Host "? settings.json already in .gitignore" -ForegroundColor Green
}
Write-Host ""

# Commit sanitized version
Write-Host "?? Committing sanitized version..." -ForegroundColor Yellow
git add src/TrayApp/Settings.cs .gitignore
git commit -m "Security: Sanitize Settings.cs for public release

- Remove hardcoded RPC token and IP address
- Add safe defaults (localhost, empty token)
- Settings.cs now only used as template
- Actual config stored in settings.json (gitignored)

BREAKING CHANGE: Users must configure settings via UI on first run"

Write-Host "? Sanitized commit created" -ForegroundColor Green
Write-Host ""

# Rewrite history to remove old secrets
Write-Host "?? Rewriting Git history to remove secrets..." -ForegroundColor Red
Write-Host "   This will take a moment..." -ForegroundColor Yellow

git filter-repo --force --path src/TrayApp/Settings.cs --invert-paths --refs HEAD~3..HEAD

Write-Host "? History rewritten" -ForegroundColor Green
Write-Host ""

# Show what needs to be done next
Write-Host "? CLEANUP COMPLETE!" -ForegroundColor Green
Write-Host ""
Write-Host "NEXT STEPS:" -ForegroundColor Cyan
Write-Host "1. ??  CHANGE YOUR ARIA2 RPC TOKEN IMMEDIATELY" -ForegroundColor Yellow
Write-Host "   The old token (7iQzqgg6...) is now public - rotate it!" -ForegroundColor Yellow
Write-Host ""
Write-Host "2. Force push cleaned history:" -ForegroundColor White
Write-Host "   git push origin main --force" -ForegroundColor Gray
Write-Host ""
Write-Host "3. Notify any collaborators to re-clone the repo" -ForegroundColor White
Write-Host ""
Write-Host "4. Verify on GitHub that secrets are gone:" -ForegroundColor White
Write-Host "   https://github.com/rifters/Aria2CompanionApp/commits/main" -ForegroundColor Gray
Write-Host ""
Write-Host "Backup branch available: backup-before-cleanup" -ForegroundColor Cyan
