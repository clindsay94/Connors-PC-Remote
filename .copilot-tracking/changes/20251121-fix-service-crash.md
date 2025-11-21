<!-- markdownlint-disable-file -->

# Release Changes: Fix Service Crash

**Related Plan**: ARCHITECTURE_AND_DELIVERY_PLAN.md
**Implementation Date**: 2025-11-21

## Summary

Fixed a critical crash (EEMessageException) in the Windows Service when executing the 'Turn Screen Off' command by replacing the blocking PostMessage call with SendMessageTimeout.

## Changes

### Modified

- CPCRemote.Core/Helpers/CommandHelper.cs - Replaced PostMessage with SendMessageTimeout to prevent blocking and CLR exceptions in Session 0.

## Release Summary

**Total Files Affected**: 1

### Files Modified (1)

- CPCRemote.Core/Helpers/CommandHelper.cs - Changed P/Invoke method for screen off command.