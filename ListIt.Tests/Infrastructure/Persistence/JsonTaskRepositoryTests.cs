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
        // Act
        var defaultPath = JsonTaskRepository.GetDefaultFilePath();

        // Assert
        var expectedFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ListIt");
        Assert.StartsWith(expectedFolder, defaultPath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("tasks.json", defaultPath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NonExistentFile_ReturnsEmptyList()
    {
        // Arrange & Act
        var repo = new JsonTaskRepository(_tempFilePath);

        // Assert
        Assert.Empty(repo.GetAll());
        Assert.Null(repo.GetById(Guid.NewGuid()));
    }

    [Fact]
    public void Add_PersistsRecurringTask_AndPreservesConcreteTypeAndInvariants()
    {
        // Arrange
        var repo = new JsonTaskRepository(_tempFilePath);
        var times = new[] { new TimeOnly(9, 0), new TimeOnly(18, 30) };
        var task = new RecurringTask("Daily Review", times, "End of day sync", 4);

        // Act
        repo.Add(task);

        // Assert in-memory
        Assert.Single(repo.GetAll());
        var inMemory = repo.GetById(task.Id);
        Assert.NotNull(inMemory);
        Assert.IsType<RecurringTask>(inMemory);

        // Re-read from disk with a fresh repository instance
        var reloadedRepo = new JsonTaskRepository(_tempFilePath);
        var loaded = reloadedRepo.GetById(task.Id);
        Assert.NotNull(loaded);
        var recurring = Assert.IsType<RecurringTask>(loaded);

        Assert.Equal(task.Id, recurring.Id);
        Assert.Equal(TaskType.Recurring, recurring.Type);
        Assert.Equal("Daily Review", recurring.Title);
        Assert.Equal("End of day sync", recurring.Description);
        Assert.Equal(4, recurring.Urgency);
        Assert.Equal(DateTimeKind.Utc, recurring.CreatedAt.Kind);
        Assert.Equal(task.CreatedAt, recurring.CreatedAt);
        Assert.Equal(2, recurring.AssignedTimes.Count);
        Assert.Equal(new TimeOnly(9, 0), recurring.AssignedTimes[0]);
        Assert.Equal(new TimeOnly(18, 30), recurring.AssignedTimes[1]);
    }

    [Fact]
    public void Add_PersistsFiniteTask_AndPreservesConcreteTypeAndInvariants()
    {
        // Arrange
        var repo = new JsonTaskRepository(_tempFilePath);
        var task = new FiniteTask("Submit tax documents", requiredCompletions: 3, currentCompletions: 1, description: "Finance", urgency: 6);

        // Act
        repo.Add(task);

        // Re-read from disk with a fresh repository instance
        var reloadedRepo = new JsonTaskRepository(_tempFilePath);
        var loaded = reloadedRepo.GetById(task.Id);
        Assert.NotNull(loaded);
        var finite = Assert.IsType<FiniteTask>(loaded);

        Assert.Equal(task.Id, finite.Id);
        Assert.Equal(TaskType.Finite, finite.Type);
        Assert.Equal("Submit tax documents", finite.Title);
        Assert.Equal("Finance", finite.Description);
        Assert.Equal(6, finite.Urgency);
        Assert.Equal(DateTimeKind.Utc, finite.CreatedAt.Kind);
        Assert.Equal(task.CreatedAt, finite.CreatedAt);
        Assert.Equal(3, finite.RequiredCompletions);
        Assert.Equal(1, finite.CurrentCompletions);
    }

    [Fact]
    public void Update_ModifiesTask_AndPersistsImmediately()
    {
        // Arrange
        var repo = new JsonTaskRepository(_tempFilePath);
        var task = new FiniteTask("Task A", requiredCompletions: 2);
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
        var finite = Assert.IsType<FiniteTask>(loaded);

        Assert.Equal("Task A Renamed", finite.Title);
        Assert.Equal("Updated description", finite.Description);
        Assert.Equal(3, finite.Urgency);
        Assert.Equal(1, finite.CurrentCompletions);
    }

    [Fact]
    public void Delete_RemovesTask_AndPersistsImmediately()
    {
        // Arrange
        var repo = new JsonTaskRepository(_tempFilePath);
        var task = new FiniteTask("Temporary Task", 1);
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
        // Arrange
        var repo = new JsonTaskRepository(_tempFilePath);
        var task = new FiniteTask("Task", 1);
        repo.Add(task);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => repo.Add(task));
    }

    [Fact]
    public void Update_NonExistentTask_ThrowsKeyNotFoundException()
    {
        // Arrange
        var repo = new JsonTaskRepository(_tempFilePath);
        var task = new FiniteTask("Task", 1);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => repo.Update(task));
    }

    [Fact]
    public void Delete_NonExistentTask_ReturnsFalse()
    {
        // Arrange
        var repo = new JsonTaskRepository(_tempFilePath);

        // Act
        var result = repo.Delete(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }
}
