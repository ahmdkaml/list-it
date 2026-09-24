using System;
using System.IO;
using System.Linq;
using ListIt.Core.Models;
using ListIt.Infrastructure.Persistence;
using Xunit;

namespace ListIt.Tests.Infrastructure.Persistence;

public class JsonTaskRepositoryTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _tempFilePath;

    public JsonTaskRepositoryTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "ListItTests_" + Guid.NewGuid().ToString("N"));
        _tempFilePath = Path.Combine(_tempDirectory, "tasks.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            try
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }

    [Fact]
    public void DefaultFilePath_ResolvesUnderLocalAppDataListIt()
    {
        var defaultPath = JsonTaskRepository.GetDefaultFilePath();
        var expectedFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ListIt");
        Assert.StartsWith(expectedFolder, defaultPath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("tasks.json", defaultPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NonExistentFile_ReturnsEmptyList()
    {
        var repo = new JsonTaskRepository(_tempFilePath);
        Assert.Empty(repo.GetAll());
        Assert.Null(repo.GetById(Guid.NewGuid()));
    }

    [Fact]
    public void Add_PersistsRecurringTask_AndPreservesInvariants()
    {
        // Arrange
        var repo = new JsonTaskRepository(_tempFilePath);
        var task = new ListitTask("Daily Review", TaskType.Recurring, TimeSpan.FromDays(1), "End of day sync", 4);

        // Act
        repo.Add(task);

        // Assert in-memory
        Assert.Single(repo.GetAll());
        var inMemory = repo.GetById(task.Id);
        Assert.NotNull(inMemory);

        // Re-read from disk with a fresh repository instance
        var reloadedRepo = new JsonTaskRepository(_tempFilePath);
        var loaded = reloadedRepo.GetById(task.Id);
        Assert.NotNull(loaded);

        Assert.Equal(task.Id, loaded.Id);
        Assert.Equal(TaskType.Recurring, loaded.Type);
        Assert.Equal("Daily Review", loaded.Title);
        Assert.Equal("End of day sync", loaded.Description);
        Assert.Equal(4, loaded.Urgency);
        Assert.Equal(DateTimeKind.Utc, loaded.StartTime.Kind);
        Assert.Equal(task.StartTime, loaded.StartTime);
        Assert.Equal(TimeSpan.FromDays(1), loaded.Interval);
    }

    [Fact]
    public void Add_PersistsFiniteTask_AndPreservesInvariants()
    {
        // Arrange
        var repo = new JsonTaskRepository(_tempFilePath);
        var task = new ListitTask(
            id: Guid.NewGuid(),
            title: "Submit tax documents",
            type: TaskType.Finite,
            interval: TimeSpan.FromDays(2),
            description: "Finance",
            urgency: 6,
            startTime: DateTime.UtcNow,
            requiredCompletions: 3,
            currentCompletions: 1);

        // Act
        repo.Add(task);

        // Re-read from disk with a fresh repository instance
        var reloadedRepo = new JsonTaskRepository(_tempFilePath);
        var loaded = reloadedRepo.GetById(task.Id);
        Assert.NotNull(loaded);

        Assert.Equal(task.Id, loaded.Id);
        Assert.Equal(TaskType.Finite, loaded.Type);
        Assert.Equal("Submit tax documents", loaded.Title);
        Assert.Equal("Finance", loaded.Description);
        Assert.Equal(6, loaded.Urgency);
        Assert.Equal(DateTimeKind.Utc, loaded.StartTime.Kind);
        Assert.Equal(task.StartTime, loaded.StartTime);
        Assert.Equal(3, loaded.RequiredCompletions);
        Assert.Equal(1, loaded.CurrentCompletions);
    }

    [Fact]
    public void Update_ModifiesTask_AndPersistsImmediately()
    {
        // Arrange
        var repo = new JsonTaskRepository(_tempFilePath);
        var task = new ListitTask("Task A", TaskType.Finite, requiredCompletions: 2);
        repo.Add(task);

        // Act
        task.RecordCompletion();
        task.UpdateDetails("Task A Renamed", "Updated description");
        task.SetUrgency(3);
        repo.Update(task);

        // Re-read from disk with a fresh repository instance
        var reloadedRepo = new JsonTaskRepository(_tempFilePath);
        var loaded = reloadedRepo.GetById(task.Id);
        Assert.NotNull(loaded);

        Assert.Equal("Task A Renamed", loaded.Title);
        Assert.Equal("Updated description", loaded.Description);
        Assert.Equal(3, loaded.Urgency);
        Assert.Equal(1, loaded.CurrentCompletions);
    }

    [Fact]
    public void Delete_RemovesTask_AndPersistsImmediately()
    {
        // Arrange
        var repo = new JsonTaskRepository(_tempFilePath);
        var task = new ListitTask("Temporary Task", TaskType.Finite, requiredCompletions: 1);
        repo.Add(task);
        Assert.Single(repo.GetAll());

        // Act
        var deleted = repo.Delete(task.Id);

        // Assert
        Assert.True(deleted);
        Assert.Empty(repo.GetAll());

        // Re-read from disk with a fresh repository instance
        var reloadedRepo = new JsonTaskRepository(_tempFilePath);
        Assert.Empty(reloadedRepo.GetAll());
        Assert.Null(reloadedRepo.GetById(task.Id));
    }

    [Fact]
    public void Add_DuplicateId_ThrowsInvalidOperationException()
    {
        var repo = new JsonTaskRepository(_tempFilePath);
        var task = new ListitTask("Task", TaskType.Finite, requiredCompletions: 1);
        repo.Add(task);

        Assert.Throws<InvalidOperationException>(() => repo.Add(task));
    }

    [Fact]
    public void Update_NonExistentTask_ThrowsKeyNotFoundException()
    {
        var repo = new JsonTaskRepository(_tempFilePath);
        var task = new ListitTask("Task", TaskType.Finite, requiredCompletions: 1);

        Assert.Throws<KeyNotFoundException>(() => repo.Update(task));
    }

    [Fact]
    public void Delete_NonExistentTask_ReturnsFalse()
    {
        var repo = new JsonTaskRepository(_tempFilePath);
        var result = repo.Delete(Guid.NewGuid());
        Assert.False(result);
    }
}
