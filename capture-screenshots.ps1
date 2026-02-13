<#
.SYNOPSIS
    Helper script to capture screenshots and GIF for LayoutIndicator README.
    Run this on Windows 11 after installing/launching LayoutIndicator.

.DESCRIPTION
    This script automates screenshot capture using built-in Windows tools.
    For the GIF, it provides instructions for using ShareX or ScreenToGif.

.NOTES
    Run from the LayoutIndicator project root:
        powershell -ExecutionPolicy Bypass -File capture-screenshots.ps1
#>

$assetsDir = Join-Path $PSScriptRoot "assets"
if (-not (Test-Path $assetsDir)) {
    New-Item -ItemType Directory -Path $assetsDir | Out-Null
}

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  LayoutIndicator Screenshot Capture Guide" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# ── Helper: wait for keypress ──────────────────────────────────────────
function Wait-ForKey($message) {
    Write-Host $message -ForegroundColor Yellow
    Write-Host "Press any key when ready..." -ForegroundColor DarkGray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    Write-Host ""
}

# ── Helper: capture a screen region to file ────────────────────────────
function Capture-Screen($filename, $description) {
    $path = Join-Path $assetsDir $filename

    Write-Host "CAPTURE: $description" -ForegroundColor Green
    Write-Host "  -> Saving to: assets\$filename" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  Method: Press Win+Shift+S to open Snip & Sketch" -ForegroundColor White
    Write-Host "  1. Select the region you want to capture" -ForegroundColor White
    Write-Host "  2. The snip is copied to clipboard" -ForegroundColor White
    Write-Host "  3. Press any key here, and it will be saved automatically" -ForegroundColor White
    Write-Host ""

    Wait-ForKey "  Take the screenshot now, then press any key to save from clipboard..."

    Add-Type -AssemblyName System.Windows.Forms
    $img = [System.Windows.Forms.Clipboard]::GetImage()
    if ($img) {
        $img.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Host "  SAVED: $path" -ForegroundColor Green
        $img.Dispose()
    } else {
        Write-Host "  WARNING: No image found on clipboard! Please retry manually." -ForegroundColor Red
        Write-Host "  Save manually to: $path" -ForegroundColor Red
    }
    Write-Host ""
}

# ────────────────────────────────────────────────────────────────────────
# Step 0: Ensure LayoutIndicator is running
# ────────────────────────────────────────────────────────────────────────

Write-Host "STEP 0: Preparation" -ForegroundColor Magenta
Write-Host "  Make sure LayoutIndicator.exe is running." -ForegroundColor White
Write-Host "  You should see the tray icon and the colored strip." -ForegroundColor White
Write-Host ""

$proc = Get-Process -Name "LayoutIndicator" -ErrorAction SilentlyContinue
if (-not $proc) {
    Write-Host "  LayoutIndicator is NOT running." -ForegroundColor Red
    Write-Host "  Please start it now." -ForegroundColor Red
}
else {
    Write-Host "  LayoutIndicator is running (PID: $($proc.Id))." -ForegroundColor Green
}
Wait-ForKey ""

# ────────────────────────────────────────────────────────────────────────
# Step 1: Screenshot — English layout (blue strip)
# ────────────────────────────────────────────────────────────────────────

Write-Host "STEP 1: English Layout Screenshot" -ForegroundColor Magenta
Write-Host "  1. Switch your keyboard to English (EN)" -ForegroundColor White
Write-Host "  2. You should see a BLUE strip on the right edge" -ForegroundColor White
Write-Host "  3. Capture the right portion of your screen showing the strip" -ForegroundColor White
Write-Host "     (include some desktop/app content for context)" -ForegroundColor White
Write-Host ""

Capture-Screen "strip-english.png" "Blue strip with English layout active"

# ────────────────────────────────────────────────────────────────────────
# Step 2: Screenshot — Russian layout (red strip)
# ────────────────────────────────────────────────────────────────────────

Write-Host "STEP 2: Russian Layout Screenshot" -ForegroundColor Magenta
Write-Host "  1. Switch your keyboard to Russian (RU)" -ForegroundColor White
Write-Host "  2. You should see a RED strip on the right edge" -ForegroundColor White
Write-Host "  3. Capture the same region as before" -ForegroundColor White
Write-Host ""

Capture-Screen "strip-russian.png" "Red strip with Russian layout active"

# ────────────────────────────────────────────────────────────────────────
# Step 3: Screenshot — System tray context menu
# ────────────────────────────────────────────────────────────────────────

Write-Host "STEP 3: Tray Menu Screenshot" -ForegroundColor Magenta
Write-Host "  1. Right-click the LayoutIndicator tray icon" -ForegroundColor White
Write-Host "  2. The context menu should appear (Mode, Strip Width, etc.)" -ForegroundColor White
Write-Host "  3. Capture just the menu" -ForegroundColor White
Write-Host ""

Capture-Screen "tray-menu.png" "System tray right-click context menu"

# ────────────────────────────────────────────────────────────────────────
# Step 4: GIF — Layout switch animation
# ────────────────────────────────────────────────────────────────────────

Write-Host "STEP 4: Layout Switch GIF" -ForegroundColor Magenta
Write-Host ""
Write-Host "  For the animated GIF, use one of these free tools:" -ForegroundColor White
Write-Host ""
Write-Host "  OPTION A: ScreenToGif (Recommended)" -ForegroundColor Cyan
Write-Host "    Download: https://www.screentogif.com/" -ForegroundColor DarkGray
Write-Host "    1. Open ScreenToGif -> Recorder" -ForegroundColor White
Write-Host "    2. Position the recording frame over the right edge of your screen" -ForegroundColor White
Write-Host "    3. Include ~200px of desktop + the full strip" -ForegroundColor White
Write-Host "    4. Start recording (F7)" -ForegroundColor White
Write-Host "    5. Switch keyboard: EN -> RU -> EN (pause 2s between each)" -ForegroundColor White
Write-Host "    6. Stop recording (F8)" -ForegroundColor White
Write-Host "    7. In the editor: File -> Save As -> GIF" -ForegroundColor White
Write-Host "    8. Save to: assets\layout-switch.gif" -ForegroundColor White
Write-Host ""
Write-Host "  OPTION B: ShareX" -ForegroundColor Cyan
Write-Host "    Download: https://getsharex.com/" -ForegroundColor DarkGray
Write-Host "    1. Capture -> Screen recording (GIF)" -ForegroundColor White
Write-Host "    2. Select region covering the strip area" -ForegroundColor White
Write-Host "    3. Switch layouts a few times during recording" -ForegroundColor White
Write-Host "    4. Stop and save to: assets\layout-switch.gif" -ForegroundColor White
Write-Host ""
Write-Host "  OPTION C: Windows built-in (Win+G Xbox Game Bar)" -ForegroundColor Cyan
Write-Host "    1. Press Win+G to open Game Bar" -ForegroundColor White
Write-Host "    2. Click Record, switch layouts, stop" -ForegroundColor White
Write-Host "    3. Convert MP4 to GIF using: ffmpeg -i video.mp4 -vf scale=600:-1 assets\layout-switch.gif" -ForegroundColor White
Write-Host ""
Write-Host "  Recommended GIF settings:" -ForegroundColor Yellow
Write-Host "    - Width: 400-600px" -ForegroundColor White
Write-Host "    - Duration: 5-8 seconds" -ForegroundColor White
Write-Host "    - Frame rate: 15 FPS" -ForegroundColor White
Write-Host "    - Show 2-3 layout switches" -ForegroundColor White
Write-Host ""

Wait-ForKey "Press any key when you've saved the GIF (or skip for now)..."

# ────────────────────────────────────────────────────────────────────────
# Summary
# ────────────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  Capture Complete!" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Expected files in assets/:" -ForegroundColor White

$expected = @("strip-english.png", "strip-russian.png", "tray-menu.png", "layout-switch.gif")
foreach ($file in $expected) {
    $path = Join-Path $assetsDir $file
    if (Test-Path $path) {
        $size = (Get-Item $path).Length / 1KB
        Write-Host "    [OK]   $file ($([math]::Round($size, 1)) KB)" -ForegroundColor Green
    } else {
        Write-Host "    [MISS] $file" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "  Next: commit and push to update the README with images." -ForegroundColor Yellow
Write-Host ""
