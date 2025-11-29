using System.Runtime.Versioning;

using CPCRemote.Core.Models;
using CPCRemote.Service.Services;

using Microsoft.Extensions.Logging;

using Moq;

using NUnit.Framework;

namespace CPCRemote.Tests;

/// <summary>
/// Unit tests for the <see cref="AppCatalogService"/> class.
/// Tests slot lookup, save, remove, and launch operations.
/// </summary>
/// <remarks>
/// Note: The AppCatalogService uses AppContext.BaseDirectory for catalog storage.
/// Since the test runner shares a directory with the service output, tests account for
/// existing data and verify incremental behavior rather than assuming an empty state.
/// </remarks>
[TestFixture]
[NonParallelizable] // Tests share a catalog file - must run sequentially
[SupportedOSPlatform("windows10.0.22621.0")]
public class AppCatalogServiceTests
{
    private Mock<ILogger<AppCatalogService>> _loggerMock = null!;
    private Mock<ILogger<UserSessionLauncher>> _launcherLoggerMock = null!;
    private UserSessionLauncher _launcher = null!;
    private AppCatalogService _service = null!;
    private int _initialAppCount;

    [SetUp]
    public void Setup()
    {
        // Arrange
        _loggerMock = new Mock<ILogger<AppCatalogService>>();
        _launcherLoggerMock = new Mock<ILogger<UserSessionLauncher>>();

        // Use real launcher - tests won't actually launch apps since Session 0 isolation
        _launcher = new UserSessionLauncher(_launcherLoggerMock.Object);

        // Create service instance
        _service = new AppCatalogService(_loggerMock.Object, _launcher);

        // Record initial state (may have existing data from production catalog)
        _initialAppCount = _service.GetAllApps().Count;
    }

    [TearDown]
    public void TearDown()
    {
        // Clean up test entries created during tests (App99, TestSlotXXX patterns)
        // Don't remove production entries (App1-App10)
        var testSlots = _service.GetAllApps()
            .Where(a => a.Slot.StartsWith("TestSlot", StringComparison.OrdinalIgnoreCase) ||
                        a.Slot == "App99")
            .Select(a => a.Slot)
            .ToList();

        foreach (var slot in testSlots)
        {
            _service.RemoveAppAsync(slot).GetAwaiter().GetResult();
        }
    }

    #region GetAllApps Tests

    [Test]
    public void GetAllApps_WhenCalled_ReturnsNonNullList()
    {
        // Act
        var apps = _service.GetAllApps();

        // Assert
        Assert.That(apps, Is.Not.Null);
        Assert.That(apps, Is.InstanceOf<IReadOnlyList<AppCatalogEntry>>());
    }

    [Test]
    public async Task GetAllApps_AfterSaveNewEntry_IncrementsCount()
    {
        // Arrange
        var entry = CreateTestEntry("TestSlotNew", "Test App");

        // Act
        await _service.SaveAppAsync(entry);
        var apps = _service.GetAllApps();

        // Assert
        Assert.That(apps.Count, Is.EqualTo(_initialAppCount + 1));
        Assert.That(apps.Any(a => a.Slot == "TestSlotNew"), Is.True);
    }

    [Test]
    public async Task GetAllApps_AfterMultipleSaves_ReturnsAllEntries()
    {
        // Arrange
        await _service.SaveAppAsync(CreateTestEntry("TestSlotA", "App A"));
        await _service.SaveAppAsync(CreateTestEntry("TestSlotB", "App B"));
        await _service.SaveAppAsync(CreateTestEntry("TestSlotC", "App C"));

        // Act
        var apps = _service.GetAllApps();

        // Assert
        Assert.That(apps.Count, Is.EqualTo(_initialAppCount + 3));
        Assert.That(apps.Any(a => a.Slot == "TestSlotA"), Is.True);
        Assert.That(apps.Any(a => a.Slot == "TestSlotB"), Is.True);
        Assert.That(apps.Any(a => a.Slot == "TestSlotC"), Is.True);
    }

    #endregion

    #region GetAppBySlot Tests

    [Test]
    public void GetAppBySlot_NonExistingSlot_ReturnsNull()
    {
        // Act
        var app = _service.GetAppBySlot("NonExistentSlot99999");

        // Assert
        Assert.That(app, Is.Null);
    }

    [Test]
    public async Task GetAppBySlot_ExistingSlot_ReturnsApp()
    {
        // Arrange
        var entry = CreateTestEntry("TestSlotExisting", "Existing App");
        await _service.SaveAppAsync(entry);

        // Act
        var app = _service.GetAppBySlot("TestSlotExisting");

        // Assert
        Assert.That(app, Is.Not.Null);
        Assert.That(app!.Name, Is.EqualTo("Existing App"));
    }

    [Test]
    public async Task GetAppBySlot_CaseInsensitive_ReturnsApp()
    {
        // Arrange
        await _service.SaveAppAsync(CreateTestEntry("TestSlotCase", "Case Test"));

        // Act
        var app = _service.GetAppBySlot("testslotcase");

        // Assert
        Assert.That(app, Is.Not.Null);
        Assert.That(app!.Name, Is.EqualTo("Case Test"));
    }

    [Test]
    public void GetAppBySlot_NullSlot_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.GetAppBySlot(null!));
    }

    [Test]
    public void GetAppBySlot_EmptySlot_ReturnsNull()
    {
        // Act
        var app = _service.GetAppBySlot(string.Empty);

        // Assert
        Assert.That(app, Is.Null);
    }

    #endregion

    #region SaveAppAsync Tests

    [Test]
    public async Task SaveAppAsync_NewEntry_AddsToCollection()
    {
        // Arrange
        var entry = CreateTestEntry("TestSlotSaveNew", "New Save Test");

        // Act
        await _service.SaveAppAsync(entry);

        // Assert
        var retrieved = _service.GetAppBySlot("TestSlotSaveNew");
        Assert.That(retrieved, Is.Not.Null);
        Assert.That(retrieved!.Name, Is.EqualTo("New Save Test"));
    }

    [Test]
    public async Task SaveAppAsync_ExistingSlot_UpdatesEntry()
    {
        // Arrange
        await _service.SaveAppAsync(CreateTestEntry("TestSlotUpdate", "Original"));
        var countAfterFirst = _service.GetAllApps().Count;

        // Act
        await _service.SaveAppAsync(CreateTestEntry("TestSlotUpdate", "Updated"));

        // Assert
        var apps = _service.GetAllApps();
        Assert.That(apps.Count, Is.EqualTo(countAfterFirst), "Should not add duplicate slot");
        var updated = _service.GetAppBySlot("TestSlotUpdate");
        Assert.That(updated!.Name, Is.EqualTo("Updated"));
    }

    [Test]
    public async Task SaveAppAsync_PreservesAllFields()
    {
        // Arrange
        var entry = new AppCatalogEntry
        {
            Slot = "TestSlotFields",
            Name = "Field Test App",
            Path = @"C:\test\app.exe",
            Arguments = "--verbose --config=test.json",
            WorkingDirectory = @"C:\work",
            Category = "Testing",
            RunAsAdmin = true,
            Enabled = false
        };

        // Act
        await _service.SaveAppAsync(entry);

        // Assert
        var savedApp = _service.GetAppBySlot("TestSlotFields");
        Assert.That(savedApp, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(savedApp!.Name, Is.EqualTo("Field Test App"));
            Assert.That(savedApp.Path, Is.EqualTo(@"C:\test\app.exe"));
            Assert.That(savedApp.Arguments, Is.EqualTo("--verbose --config=test.json"));
            Assert.That(savedApp.WorkingDirectory, Is.EqualTo(@"C:\work"));
            Assert.That(savedApp.Category, Is.EqualTo("Testing"));
            Assert.That(savedApp.RunAsAdmin, Is.True);
            Assert.That(savedApp.Enabled, Is.False);
        });
    }

    [Test]
    public async Task SaveAppAsync_ConcurrentSaves_AllSucceed()
    {
        // Arrange
        var tasks = new List<Task>();
        for (int i = 1; i <= 5; i++)
        {
            var entry = CreateTestEntry($"TestSlotConcurrent{i}", $"Concurrent App {i}");
            tasks.Add(_service.SaveAppAsync(entry));
        }

        // Act
        await Task.WhenAll(tasks);

        // Assert
        var apps = _service.GetAllApps();
        Assert.That(apps.Count(a => a.Slot.StartsWith("TestSlotConcurrent")), Is.EqualTo(5));
    }

    #endregion

    #region RemoveAppAsync Tests

    [Test]
    public async Task RemoveAppAsync_ExistingSlot_ReturnsTrue()
    {
        // Arrange
        await _service.SaveAppAsync(CreateTestEntry("TestSlotRemove", "Remove Test"));

        // Act
        bool removed = await _service.RemoveAppAsync("TestSlotRemove");

        // Assert
        Assert.That(removed, Is.True);
        Assert.That(_service.GetAppBySlot("TestSlotRemove"), Is.Null);
    }

    [Test]
    public async Task RemoveAppAsync_NonExistingSlot_ReturnsFalse()
    {
        // Act
        bool removed = await _service.RemoveAppAsync("NonExistentSlot999");

        // Assert
        Assert.That(removed, Is.False);
    }

    [Test]
    public async Task RemoveAppAsync_CaseInsensitive_RemovesApp()
    {
        // Arrange
        await _service.SaveAppAsync(CreateTestEntry("TestSlotRemoveCase", "Case Remove"));

        // Act
        bool removed = await _service.RemoveAppAsync("testslotremovecase");

        // Assert
        Assert.That(removed, Is.True);
        Assert.That(_service.GetAppBySlot("TestSlotRemoveCase"), Is.Null);
    }

    [Test]
    public async Task RemoveAppAsync_DecreasesCount()
    {
        // Arrange
        await _service.SaveAppAsync(CreateTestEntry("TestSlotRemoveCount", "Count Test"));
        int countBefore = _service.GetAllApps().Count;

        // Act
        await _service.RemoveAppAsync("TestSlotRemoveCount");

        // Assert
        Assert.That(_service.GetAllApps().Count, Is.EqualTo(countBefore - 1));
    }

    #endregion

    #region LaunchApp Tests

    [Test]
    public void LaunchApp_NonExistingSlot_ReturnsFalse()
    {
        // Act
        bool launched = _service.LaunchApp("NonExistentLaunchSlot");

        // Assert
        Assert.That(launched, Is.False);
    }

    [Test]
    public async Task LaunchApp_DisabledApp_ReturnsFalse()
    {
        // Arrange
        var entry = CreateTestEntry("TestSlotDisabled", "Disabled App");
        entry.Enabled = false;
        await _service.SaveAppAsync(entry);

        // Act
        bool launched = _service.LaunchApp("TestSlotDisabled");

        // Assert
        Assert.That(launched, Is.False);
    }

    [Test]
    public async Task LaunchApp_EmptyPath_ReturnsFalse()
    {
        // Arrange
        var entry = new AppCatalogEntry
        {
            Slot = "TestSlotEmptyPath",
            Name = "Empty Path App",
            Path = "",
            Enabled = true
        };
        await _service.SaveAppAsync(entry);

        // Act
        bool launched = _service.LaunchApp("TestSlotEmptyPath");

        // Assert
        Assert.That(launched, Is.False);
    }

    [Test]
    public async Task LaunchApp_NonExistentPath_ReturnsFalse()
    {
        // Arrange
        var entry = new AppCatalogEntry
        {
            Slot = "TestSlotBadPath",
            Name = "Bad Path App",
            Path = @"C:\NonExistent\Path\fake-app-99999.exe",
            Enabled = true
        };
        await _service.SaveAppAsync(entry);

        // Act
        // The launcher fails in test context (no active console session)
        bool launched = _service.LaunchApp("TestSlotBadPath");

        // Assert
        Assert.That(launched, Is.False);
    }

    [Test]
    public void LaunchApp_NullSlot_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _service.LaunchApp(null!));
    }

    #endregion

    #region Persistence Tests

    [Test]
    public async Task ServiceReload_LoadsPersistedData()
    {
        // Arrange - save data with first instance
        await _service.SaveAppAsync(CreateTestEntry("TestSlotPersist", "Persist Test"));

        // Act - create new instance (will load from same path)
        var newService = new AppCatalogService(_loggerMock.Object, _launcher);

        // Assert
        var app = newService.GetAppBySlot("TestSlotPersist");
        Assert.That(app, Is.Not.Null);
        Assert.That(app!.Name, Is.EqualTo("Persist Test"));
    }

    #endregion

    #region Helper Methods

    private static AppCatalogEntry CreateTestEntry(string slot, string name)
    {
        return new AppCatalogEntry
        {
            Slot = slot,
            Name = name,
            Path = @"C:\Windows\notepad.exe",
            Enabled = true
        };
    }

    #endregion
}
