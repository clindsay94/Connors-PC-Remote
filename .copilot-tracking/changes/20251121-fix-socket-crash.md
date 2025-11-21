<!-- markdownlint-disable-file -->

# Release Changes: Fix Socket Crash

**Related Plan**: ARCHITECTURE_AND_DELIVERY_PLAN.md
**Implementation Date**: 2025-11-21

## Summary

Fixed a service crash caused by unhandled System.IO.IOException (SocketException) when the HttpListener transport connection is aborted.

## Changes

### Modified

- CPCRemote.Service/Worker.cs - Added try-catch blocks around GetContextAsync and request processing loop to handle socket errors gracefully.

## Release Summary

**Total Files Affected**: 1

### Files Modified (1)

- CPCRemote.Service/Worker.cs - Improved error handling for network transport exceptions.