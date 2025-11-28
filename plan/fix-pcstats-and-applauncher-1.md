---
goal: Fix PC Stats temperature reporting errors and App Launcher dynamic naming
version: 1.0
date_created: 2025-11-27
last_updated: 2025-11-27
owner: CPC-Bridge-Architect
status: Complete
tags: [bug, feature, smartthings, windows-service, api-contract]
---

# Introduction

![Status: Complete](https://img.shields.io/badge/status-Complete-green)

This plan addresses two critical issues discovered in the SmartThings Edge driver logs:

1. **PC Stats Temperature Error**: The driver crashes with Invalid value for PC Stats.cpuTemp value: {value={}} error when the Windows service returns null temperature values.

2. **App Launcher Dynamic Names**: The presentation layer shows hardcoded Slot 1, Slot 2 etc. instead of the actual app names from the PC catalog.

## 1. Requirements and Constraints

- **REQ-001**: Temperature attributes must only be emitted when valid numeric values are available
- **REQ-002**: When temperatures are unavailable (HWiNFO not running), gracefully degrade without errors
- **REQ-003**: App Launcher should display friendly app names from the PC catalog
- **CON-001**: SmartThings capability enums are static - cannot dynamically change enum values at runtime
- **CON-002**: Presentation files are deployed with the driver and cannot fetch runtime data
- **CON-003**: HWiNFO shared memory must be enabled by the user on their PC
- **GUD-001**: Follow Golden Rule of Drift - changes on Windows side must be mirrored on Lua side
- **PAT-001**: Use null-safe JSON serialization for optional sensor data

## 2. Implementation Steps

### Implementation Phase 1: Fix Lua Temperature Handling

- GOAL-001: Prevent emit_event crashes when temperature values are nil or not numbers

| Task     | Description                                                                                          | Completed | Date       |
| -------- | ---------------------------------------------------------------------------------------------------- | --------- | ---------- |
| TASK-001 | Update update_stats() in init.lua to validate cpuTemp and gpuTemp are valid numbers before emit      | ✅        | 2025-11-27 |
| TASK-002 | Add type validation helper function is_valid_sensor_value(val) that returns true for non-nil numbers | ✅        | 2025-11-27 |
| TASK-003 | Add debug logging when temperatures are skipped due to nil/invalid values                            | ✅        | 2025-11-27 |

### Implementation Phase 2: Fix C# JSON Serialization for Null Temperatures

- GOAL-002: Ensure /stats endpoint returns proper JSON structure (omit null fields)

| Task     | Description                                                                               | Completed | Date       |
| -------- | ----------------------------------------------------------------------------------------- | --------- | ---------- |
| TASK-004 | Verify HardwareMonitor.GetStats() returns PcStats class with nullable floats correctly    | ✅        | 2025-11-27 |
| TASK-005 | Update JSON serialization options in Worker.cs to use JsonIgnoreCondition.WhenWritingNull | ✅        | 2025-11-27 |
| TASK-006 | Add unit test for /stats JSON serialization with null temperature values                  | ✅        | 2025-11-27 |

### Implementation Phase 3: App Launcher Dynamic Naming via selectedAppName Attribute

- GOAL-003: Add a read-only attribute to display the resolved app name alongside the slot selection

| Task     | Description                                                                                     | Completed | Date       |
| -------- | ----------------------------------------------------------------------------------------------- | --------- | ---------- |
| TASK-007 | Add selectedAppName attribute to aloneorganic04790.appLauncher capability (read-only string)    | ✅        | 2025-11-27 |
| TASK-008 | Update Lua driver to emit selectedAppName event when slot is selected using cached catalog data | ✅        | 2025-11-27 |
| TASK-009 | Update presentation to display selectedAppName as a label below the slot selector               | ✅        | 2025-11-27 |
| TASK-010 | Update handle_set_app_slot to also emit the resolved app name                                   | ✅        | 2025-11-27 |

### Implementation Phase 4: Verification and Testing

- GOAL-004: Verify all fixes work end-to-end with real hardware

| Task     | Description                                                            | Completed         | Date       |
| -------- | ---------------------------------------------------------------------- | ----------------- | ---------- |
| TASK-011 | Build and deploy Windows Service with updated JSON serialization       | ✅                | 2025-11-27 |
| TASK-012 | Package and deploy SmartThings driver with Lua fixes                   | ✅                | 2025-11-27 |
| TASK-013 | Verify logcat shows no more Invalid value for PC Stats.cpuTemp errors  | Pending User Test |            |
| TASK-014 | Verify app launcher shows friendly names via selectedAppName attribute | Pending User Test |            |

### Bonus: Customizable Sensor Configuration

- GOAL-005: Allow users to customize which HWiNFO sensors map to stats

| Task     | Description                                                                         | Completed | Date       |
| -------- | ----------------------------------------------------------------------------------- | --------- | ---------- |
| TASK-015 | Add SensorOptions class with configurable sensor patterns                           | ✅        | 2025-11-27 |
| TASK-016 | Update appsettings.json with sensors section including custom sensor support        | ✅        | 2025-11-27 |
| TASK-017 | Update HardwareMonitor to use IOptionsMonitor<SensorOptions> for pattern matching   | ✅        | 2025-11-27 |
| TASK-018 | Add CustomSensors support via JsonExtensionData for arbitrary sensor values in JSON | ✅        | 2025-11-27 |

## 3. Alternatives

- **ALT-001**: Remove cpuTemp/gpuTemp entirely from the driver if temperature monitoring proves unreliable (nuclear option)
- **ALT-002**: Use SmartThings device preferences to manually configure app names
- **ALT-003**: Accept the slot number limitation and just display Slot 1, Slot 2 etc.

## 4. Dependencies

- **DEP-001**: HWiNFO64 must be running with Shared Memory Support enabled on the Windows PC
- **DEP-002**: SmartThings CLI (smartthings) for capability/presentation updates
- **DEP-003**: .NET 10 SDK for building Windows Service

## 5. Files

- **FILE-001**: c:\Users\Connor\CPCRemoteDriver\src\init.lua - Lua driver main file
- **FILE-002**: c:\Users\Connor\CPCRemoteDriver\capabilities\aloneorganic04790.pcstats.json
- **FILE-003**: c:\Users\Connor\CPCRemoteDriver\capabilities\aloneorganic04790.pcstats.presentation.json
- **FILE-004**: c:\Users\Connor\CPCRemoteDriver\capabilities\aloneorganic04790.applauncher.json
- **FILE-005**: c:\Users\Connor\CPCRemoteDriver\capabilities\aloneorganic04790.applauncher.presentation.json
- **FILE-006**: c:\Users\Connor\CPCRemoteDriver\profiles\cpc-remote-profile.yaml
- **FILE-007**: p:\Connor\Connors-PC-Remote\CPCRemote.Service\Worker.cs
- **FILE-008**: p:\Connor\Connors-PC-Remote\CPCRemote.Service\Services\HardwareMonitor.cs

## 6. Testing

- **TEST-001**: Verify /stats endpoint returns valid JSON when HWiNFO is running (temps present)
- **TEST-002**: Verify /stats endpoint returns valid JSON when HWiNFO is NOT running (temps omitted)
- **TEST-003**: Verify Lua driver handles both scenarios without crashing
- **TEST-004**: Verify app name appears in SmartThings app when slot is selected
- **TEST-005**: Verify smartthings edge:drivers:logcat shows no more temperature errors

## 7. Risks and Assumptions

- **RISK-001**: SmartThings presentation templates may not support all display options for new attributes
- **RISK-002**: Adding new capability attributes requires capability version bump
- **ASSUMPTION-001**: HWiNFO Pro is running with Shared Memory Support enabled
- **ASSUMPTION-002**: User will enable HWiNFO shared memory in settings

## 8. Related Specifications

- SmartThings Edge Driver Development: https://developer-preview.smartthings.com/docs/edge-device-drivers/
- SmartThings Capability Presentations: https://developer-preview.smartthings.com/docs/devices/capabilities/presentation/
- HWiNFO Shared Memory Interface: https://www.hwinfo.com/forum/threads/shared-memory-interface.5258/

---

## Root Cause Analysis

### Error: Invalid value for PC Stats.cpuTemp value: {value={}}

**Root Cause**: The C# service returns null for temperature values when HWiNFO shared memory is unavailable. The JSON serializer converts this to {cpuTemp: null}. The Lua driver calls cap_stats.cpuTemp(data.cpuTemp) which passes nil. The SmartThings capability framework wraps this in {value={}} (empty table), which fails validation because the schema expects {value: number}.

**Fix**: Check data.cpuTemp is a valid number before emitting:

    if type(data.cpuTemp) == number and data.cpuTemp > 0 then
        device:emit_event(cap_stats.cpuTemp(data.cpuTemp))
    end

### Issue: App names showing as App1, App2 instead of friendly names

**Root Cause**: SmartThings capability presentations are static - deployed with driver and cannot be changed at runtime. The alternatives array has hardcoded values like Slot 1, Slot 2. Even though Lua driver fetches /apps catalog, there is no way to inject those values into the presentation dropdown.

**Fix**: Add a new selectedAppName read-only attribute that displays the resolved name. When user selects App2, the driver looks up the catalog and emits selectedAppName = Steam which displays below the dropdown.
