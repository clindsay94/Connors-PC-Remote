namespace CPCRemote.Service.Services;

using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using NAudio.CoreAudioApi;

/// <summary>
/// Service for controlling system audio volume and sending media keys.
/// Uses NAudio's CoreAudioApi for volume control and P/Invoke for media key simulation.
/// </summary>
[SupportedOSPlatform("windows10.0.22621.0")]
public sealed class MediaService : IDisposable
{
    private readonly ILogger<MediaService> _logger;
    private MMDeviceEnumerator? _enumerator;
    private bool _disposed;

    public MediaService(ILogger<MediaService> logger)
    {
        _logger = logger;
        try
        {
            _enumerator = new MMDeviceEnumerator();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize audio device enumerator");
        }
    }

    /// <summary>
    /// Gets the current system master volume level (0-100).
    /// </summary>
    public int GetVolume()
    {
        try
        {
            using var device = GetDefaultAudioDevice();
            if (device is null) return 0;

            return (int)(device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get volume");
            return 0;
        }
    }

    /// <summary>
    /// Sets the system master volume level.
    /// </summary>
    /// <param name="level">Volume level 0-100.</param>
    public void SetVolume(int level)
    {
        try
        {
            level = Math.Clamp(level, 0, 100);
            using var device = GetDefaultAudioDevice();
            if (device is null) return;

            device.AudioEndpointVolume.MasterVolumeLevelScalar = level / 100f;
            _logger.LogDebug("Volume set to {Level}%", level);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set volume to {Level}", level);
        }
    }

    /// <summary>
    /// Gets the current mute state.
    /// </summary>
    public bool GetMuteState()
    {
        try
        {
            using var device = GetDefaultAudioDevice();
            return device?.AudioEndpointVolume.Mute ?? false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get mute state");
            return false;
        }
    }

    /// <summary>
    /// Toggles the mute state and returns the new state.
    /// </summary>
    public bool ToggleMute()
    {
        try
        {
            using var device = GetDefaultAudioDevice();
            if (device is null) return false;

            bool newState = !device.AudioEndpointVolume.Mute;
            device.AudioEndpointVolume.Mute = newState;
            _logger.LogDebug("Mute toggled to {State}", newState);
            return newState;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle mute");
            return false;
        }
    }

    /// <summary>
    /// Sends a media key event (play/pause, next, previous, stop).
    /// </summary>
    public void SendMediaKey(Core.IPC.MediaAction action)
    {
        try
        {
            ushort vk = action switch
            {
                Core.IPC.MediaAction.PlayPause => VK_MEDIA_PLAY_PAUSE,
                Core.IPC.MediaAction.Next => VK_MEDIA_NEXT_TRACK,
                Core.IPC.MediaAction.Previous => VK_MEDIA_PREV_TRACK,
                Core.IPC.MediaAction.Stop => VK_MEDIA_STOP,
                _ => throw new ArgumentOutOfRangeException(nameof(action))
            };

            keybd_event((byte)vk, 0, KEYEVENTF_EXTENDEDKEY, 0);
            keybd_event((byte)vk, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            _logger.LogDebug("Sent media key: {Action}", action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send media key {Action}", action);
        }
    }

    private MMDevice? GetDefaultAudioDevice()
    {
        try
        {
            return _enumerator?.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No default audio device found");
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _enumerator?.Dispose();
    }

    // ── P/Invoke for media key simulation ──

    private const ushort VK_MEDIA_NEXT_TRACK = 0xB0;
    private const ushort VK_MEDIA_PREV_TRACK = 0xB1;
    private const ushort VK_MEDIA_STOP = 0xB2;
    private const ushort VK_MEDIA_PLAY_PAUSE = 0xB3;
    private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);
}
