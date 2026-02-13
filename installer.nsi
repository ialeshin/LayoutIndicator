; LayoutIndicator Installer Script
; Built with NSIS 3.x + Modern UI 2

;--------------------------------
; Includes

!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "FileFunc.nsh"

;--------------------------------
; General Configuration

!define PRODUCT_NAME "LayoutIndicator"
!define PRODUCT_VERSION "1.0.0"
!define PRODUCT_PUBLISHER "LayoutIndicator"
!define PRODUCT_EXE "LayoutIndicator.exe"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
!define PRODUCT_STARTUP_KEY "Software\Microsoft\Windows\CurrentVersion\Run"

Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "LayoutIndicator_Setup_${PRODUCT_VERSION}.exe"
InstallDir "$PROGRAMFILES64\${PRODUCT_NAME}"
InstallDirRegKey HKLM "${PRODUCT_UNINST_KEY}" "InstallLocation"
RequestExecutionLevel admin
SetCompressor /SOLID lzma
SetCompressorDictSize 32

;--------------------------------
; Version Info (shown in file properties on Windows)

VIProductVersion "1.0.0.0"
VIAddVersionKey "ProductName" "${PRODUCT_NAME}"
VIAddVersionKey "ProductVersion" "${PRODUCT_VERSION}"
VIAddVersionKey "CompanyName" "${PRODUCT_PUBLISHER}"
VIAddVersionKey "FileDescription" "${PRODUCT_NAME} Installer"
VIAddVersionKey "FileVersion" "${PRODUCT_VERSION}"
VIAddVersionKey "LegalCopyright" "Copyright (c) 2026 ${PRODUCT_PUBLISHER}"

;--------------------------------
; MUI2 Settings

!define MUI_ABORTWARNING
!define MUI_UNABORTWARNING

; Branding text at the bottom of installer
BrandingText "${PRODUCT_NAME} ${PRODUCT_VERSION}"

;--------------------------------
; Installer Pages

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE.txt"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_INSTFILES
!define MUI_FINISHPAGE_RUN "$INSTDIR\${PRODUCT_EXE}"
!define MUI_FINISHPAGE_RUN_TEXT "Launch ${PRODUCT_NAME}"
!define MUI_FINISHPAGE_RUN_NOTCHECKED
!insertmacro MUI_PAGE_FINISH

;--------------------------------
; Uninstaller Pages

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

;--------------------------------
; Languages

!insertmacro MUI_LANGUAGE "English"

;--------------------------------
; Installer Sections

Section "!${PRODUCT_NAME} (required)" SecCore
    SectionIn RO ; Required — cannot be unchecked

    ; Kill running instance before installing
    nsExec::ExecToLog 'taskkill /F /IM "${PRODUCT_EXE}" /T'

    SetOutPath "$INSTDIR"

    ; Install all application files
    File "publish\LayoutIndicator.exe"
    File "publish\LayoutIndicator.pdb"
    File "publish\D3DCompiler_47_cor3.dll"
    File "publish\PenImc_cor3.dll"
    File "publish\PresentationNative_cor3.dll"
    File "publish\vcruntime140_cor3.dll"
    File "publish\wpfgfx_cor3.dll"

    ; Only copy settings.json if it doesn't exist (preserve user config on upgrade)
    ${IfNot} ${FileExists} "$INSTDIR\settings.json"
        File "publish\settings.json"
    ${EndIf}

    ; Also keep a default copy for reference
    File /oname=settings.default.json "publish\settings.json"

    ; Copy license
    File "LICENSE.txt"

    ; Create Start Menu shortcuts
    CreateDirectory "$SMPROGRAMS\${PRODUCT_NAME}"
    CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk" "$INSTDIR\${PRODUCT_EXE}" "" "$INSTDIR\${PRODUCT_EXE}" 0
    CreateShortCut "$SMPROGRAMS\${PRODUCT_NAME}\Uninstall.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0

    ; Write uninstall registry keys
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "DisplayName" "${PRODUCT_NAME}"
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "UninstallString" '"$INSTDIR\uninstall.exe"'
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "QuietUninstallString" '"$INSTDIR\uninstall.exe" /S'
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "InstallLocation" "$INSTDIR"
    WriteRegStr HKLM "${PRODUCT_UNINST_KEY}" "DisplayIcon" '"$INSTDIR\${PRODUCT_EXE}"'
    WriteRegDWORD HKLM "${PRODUCT_UNINST_KEY}" "NoModify" 1
    WriteRegDWORD HKLM "${PRODUCT_UNINST_KEY}" "NoRepair" 1

    ; Calculate and write installed size
    ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
    IntFmt $0 "0x%08X" $0
    WriteRegDWORD HKLM "${PRODUCT_UNINST_KEY}" "EstimatedSize" $0

    ; Write uninstaller
    WriteUninstaller "$INSTDIR\uninstall.exe"
SectionEnd

Section "Desktop Shortcut" SecDesktop
    CreateShortCut "$DESKTOP\${PRODUCT_NAME}.lnk" "$INSTDIR\${PRODUCT_EXE}" "" "$INSTDIR\${PRODUCT_EXE}" 0
SectionEnd

Section "Start with Windows" SecStartup
    WriteRegStr HKCU "${PRODUCT_STARTUP_KEY}" "${PRODUCT_NAME}" '"$INSTDIR\${PRODUCT_EXE}"'
SectionEnd

;--------------------------------
; Section Descriptions

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
    !insertmacro MUI_DESCRIPTION_TEXT ${SecCore} "Install ${PRODUCT_NAME} core application files. (Required)"
    !insertmacro MUI_DESCRIPTION_TEXT ${SecDesktop} "Create a shortcut on your Desktop."
    !insertmacro MUI_DESCRIPTION_TEXT ${SecStartup} "Automatically start ${PRODUCT_NAME} when Windows starts."
!insertmacro MUI_FUNCTION_DESCRIPTION_END

;--------------------------------
; Uninstaller Section

Section "Uninstall"
    ; Kill running instance
    nsExec::ExecToLog 'taskkill /F /IM "${PRODUCT_EXE}" /T'

    ; Remove startup registry entry
    DeleteRegValue HKCU "${PRODUCT_STARTUP_KEY}" "${PRODUCT_NAME}"

    ; Remove uninstall registry key
    DeleteRegKey HKLM "${PRODUCT_UNINST_KEY}"

    ; Remove files
    Delete "$INSTDIR\LayoutIndicator.exe"
    Delete "$INSTDIR\LayoutIndicator.pdb"
    Delete "$INSTDIR\D3DCompiler_47_cor3.dll"
    Delete "$INSTDIR\PenImc_cor3.dll"
    Delete "$INSTDIR\PresentationNative_cor3.dll"
    Delete "$INSTDIR\vcruntime140_cor3.dll"
    Delete "$INSTDIR\wpfgfx_cor3.dll"
    Delete "$INSTDIR\settings.json"
    Delete "$INSTDIR\settings.default.json"
    Delete "$INSTDIR\LICENSE.txt"
    Delete "$INSTDIR\uninstall.exe"

    ; Remove shortcuts
    Delete "$SMPROGRAMS\${PRODUCT_NAME}\${PRODUCT_NAME}.lnk"
    Delete "$SMPROGRAMS\${PRODUCT_NAME}\Uninstall.lnk"
    RMDir "$SMPROGRAMS\${PRODUCT_NAME}"
    Delete "$DESKTOP\${PRODUCT_NAME}.lnk"

    ; Remove install directory (only if empty)
    RMDir "$INSTDIR"
SectionEnd
