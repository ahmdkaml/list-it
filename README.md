# List-it

A minimalist, distraction-free desktop task widget for Windows designed to stay accessible directly on your desktop.

---

## 1. App Purpose & Core Features

### Purpose
`List-it` is an always-accessible, lightweight desktop companion built for quick capture and minimal cognitive overhead. Rather than living inside a browser tab or cluttering the Windows taskbar, `List-it` is designed to exist directly on the desktop surface, providing immediate access to personal tasks without disrupting the user's active workflow.

### Core Features
- **Desktop-Anchored Widget:** Unobtrusive, borderless widget interface integrated into the Windows desktop environment.
- **Quick Task Capture:** Focused input box for instant keyboard-driven task addition.
- **Task Tracking & Cleanup:** Simple checkbox completion and one-click task deletion.
- **Minimalist Aesthetic:** Clean, distraction-free dark theme engineered to blend into desktop wallpapers and setups.
- **Zero-Friction Autostart:** Automatically launches on user login so tasks are present upon starting the workstation.

---

## 2. Technology Stack

- **Runtime & Framework:** [.NET 8](https://dotnet.microsoft.com/) (`net8.0-windows`), C# 12
- **UI Platform:** Windows Presentation Foundation (WPF) with hardware-accelerated XAML rendering
- **OS Interoperability:** Win32 P/Invoke (`user32.dll`) for shell window parenting and desktop anchoring
- **Data Serialization:** `System.Text.Json` (8.0.5) for lightweight task persistence
- **Packaging & Deployment:** [WiX Toolset v5](https://wixtoolset.org/) (`WixToolset.Sdk` and `WixToolset.Util.wixext`)
- **Solution Format:** Visual Studio XML Solution (`.slnx`)

---

## 3. Architecture Overview

The solution is organized into two primary projects:

```
ListIt.slnx
├── ListIt/                  # Core WPF Desktop Application
│   ├── App.xaml             # Application lifecycle entry point
│   ├── MainWindow.xaml      # Widget presentation and UI templates
│   ├── MainWindow.xaml.cs   # View interaction and Win32 desktop interop
│   └── Models/              # Domain data structures (Task)
└── ListIt.Installer/        # WiX Toolset v5 MSI Packaging Project
    └── Package.wxs          # WiX deployment manifest and custom actions
```

### Component Architecture
- **Presentation & Interop (`ListIt`):**
  - Pure WPF client configured without heavy third-party UI dependencies.
  - Utilizes Windows API (`user32.dll`) during startup to anchor the window within the desktop shell hierarchy (`Progman` / `WorkerW`).
- **Domain Model:**
  - Lightweight data representations capturing task state and identity.

---

## 4. Packaging, Installation & Autostart

`List-it` features an integrated deployment and installation pipeline configured for self-contained, zero-dependency distribution.

### Standalone Executable Packaging
The application is configured in `ListIt.csproj` with high-performance standalone publishing settings:
- **`PublishSingleFile=true`**: Bundles runtime components, managed assemblies, and native libraries into a single executable.
- **`SelfContained=true`**: Embeds the .NET 8 runtime directly into the distribution, removing the prerequisite for users to install the .NET Desktop Runtime.
- **`PublishReadyToRun=true`**: Precompiles IL to native machine code (R2R Ahead-Of-Time compilation) for instant cold-start execution.
- **`RuntimeIdentifier=win-x64`**: Explicitly targets 64-bit Windows architectures.

### MSI Installer (WiX Toolset v5)
The `ListIt.Installer` project packages the application into a standard Windows Installer (`.msi`):
- **Project-Linked Publishing:** References `ListIt.csproj` with `Publish="true"`, automatically building and harvesting the published application artifacts during the MSI build.
- **Installation Directory:** Deploys application files into `ProgramFiles64Folder\List-it`.
- **Upgrade Support:** Employs `MajorUpgrade` rules to ensure smooth updates and prevent side-by-side duplicate installations.

### Autostart via Windows Task Scheduler
Instead of relying on standard registry run keys, `List-it` leverages Windows Task Scheduler for robust logon activation:
- **Install Action:** During MSI installation, deferred custom actions invoke `schtasks.exe` using `WixQuietExec`:
  ```cmd
  schtasks.exe /Create /TN "List-it" /TR "\"[INSTALLFOLDER]ListIt.exe\"" /SC ONLOGON /RU "[LogonUser]" /IT /RL LIMITED /F
  schtasks.exe /Run /TN "List-it"
  ```
  This registers an interactive (`/IT`), non-elevated (`/RL LIMITED`) scheduled task tied to the installing user's logon session (`/SC ONLOGON /RU "[LogonUser]"`), and immediately triggers it so the application runs without requiring a logoff/reboot.
- **Uninstall Action:** During product removal, a custom action executes `schtasks.exe /Delete /TN "List-it" /F` to ensure no orphaned scheduled tasks remain.
