<!-- markdownlint-disable-file -->

# Release Changes: Fix UI State and Auth Logging

**Related Plan**: ARCHITECTURE_AND_DELIVERY_PLAN.md
**Implementation Date**: 2025-11-21

## Summary

Fixed an issue where the 'Start Service' button remained disabled after service installation, and improved service logging to diagnose authorization failures.

## Changes

### Modified

- CPCRemote.UI/ViewModels/ServiceManagementViewModel.cs - Added [NotifyCanExecuteChangedFor] attributes to ensure command states update when service status changes.
- CPCRemote.Service/Worker.cs - Enhanced logging to distinguish between missing Authorization headers and invalid tokens.

## Release Summary

**Total Files Affected**: 2

### Files Modified (2)

- CPCRemote.UI/ViewModels/ServiceManagementViewModel.cs - UI command state fix.
- CPCRemote.Service/Worker.cs - Logging improvements.