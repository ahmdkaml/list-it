using System;
using System.Collections.Generic;
using System.Linq;
using ListIt.Core.Models;
using ListIt.Core.Repositories;
using ListIt.Core.Services;
using Xunit;

namespace ListIt.Tests.Application.Services;

public class TaskServiceTests
{
    private class InMemoryTaskRepository : ITaskRepository
    {
        private readonly Dictionary<Guid, ListitTask> _tasks = new();

        public int AddCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public int DeleteCallCount { get; private set; }

        public IReadOnlyList<ListitTask> GetAll()
        {
            return _tasks.Values.ToList().AsReadOnly();
        }

        public ListitTask? GetById(Guid id)
        {
            return _tasks.TryGetValue(id, out var task) ? task : null;
        }

        public void Add(ListitTask task)
        {
            AddCallCount++;
            _tasks[task.Id] = task;
        }

        public void Update(ListitTask task)
        {
            UpdateCallCount++;
            _tasks[task.Id] = task;
        }

        public bool Delete(Guid id)
        {
            DeleteCallCount++;
            return _tasks.Remove(id);
        }
    }

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TaskService(null!));
    }

    [Fact]
    public void CreateTask_Recurring_CreatesDomainModel_AndAddsToRepository()
    {
        // Arrange
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);

        // Act
        var task = service.CreateTask("Daily Standup", TaskType.Recurring, TimeSpan.FromDays(1), "Engineering sync", 3);

        // Assert
        Assert.NotNull(task);
        Assert.Equal(TaskType.Recurring, task.Type);
        Assert.Equal("Daily Standup", task.Title);
        Assert.Equal("Engineering sync", task.Description);
        Assert.Equal(3, task.Urgency);
        Assert.Equal(TimeSpan.FromDays(1), task.Interval);
        Assert.Equal(1, repo.AddCallCount);
        Assert.Same(task, repo.GetById(task.Id));
    }

    [Fact]
    public void CreateTask_Finite_CreatesDomainModel_AndAddsToRepository()
    {
        // Arrange
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);

        // Act
        var task = service.CreateTask("Submit Timesheet", TaskType.Finite, TimeSpan.FromHours(4), description: "Finance", urgency: 2, requiredCompletions: 1);

        // Assert
        Assert.NotNull(task);
        Assert.Equal(TaskType.Finite, task.Type);
        Assert.Equal("Submit Timesheet", task.Title);
        Assert.Equal(1, task.RequiredCompletions);
        Assert.Equal(0, task.CurrentCompletions);
        Assert.Equal(1, repo.AddCallCount);
        Assert.Same(task, repo.GetById(task.Id));
    }

    [Fact]
    public void GetAllTasks_ReturnsAllRepositoryTasks()
    {
        // Arrange
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);
        service.CreateTask("Task 1", TaskType.Finite, requiredCompletions: 1);
        service.CreateTask("Task 2", TaskType.Recurring);

        // Act
        var all = service.GetAllTasks();

        // Assert
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void GetTask_ReturnsTaskById_OrNullIfNotFound()
    {
        // Arrange
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);
        var created = service.CreateTask("Task 1", TaskType.Finite, requiredCompletions: 1);

        // Act
        var found = service.GetTask(created.Id);
        var notFound = service.GetTask(Guid.NewGuid());

        // Assert
        Assert.Same(created, found);
        Assert.Null(notFound);
    }

    [Fact]
    public void UpdateTask_ExistingTask_DelegatesToRepository()
    {
        // Arrange
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);
        var task = service.CreateTask("Original Title", TaskType.Finite, requiredCompletions: 2);

        // Act
        task.SetTitle("Updated Title");
        task.RecordCompletion();
        service.UpdateTask(task);

        // Assert
        Assert.Equal(1, repo.UpdateCallCount);
        var updated = service.GetTask(task.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Title", updated.Title);
        Assert.Equal(1, updated.CurrentCompletions);
    }

    [Fact]
    public void UpdateTask_NonExistentTask_ThrowsKeyNotFoundException()
    {
        // Arrange
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);
        var nonExistent = new ListitTask("Ghost Task", TaskType.Finite, requiredCompletions: 1);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => service.UpdateTask(nonExistent));
        Assert.Equal(0, repo.UpdateCallCount);
    }

    [Fact]
    public void UpdateTask_NullTask_ThrowsArgumentNullException()
    {
        // Arrange
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => service.UpdateTask(null!));
    }

    [Fact]
    public void DeleteTask_ExistingTask_ReturnsTrueAndCallsRepository()
    {
        // Arrange
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);
        var task = service.CreateTask("To Delete", TaskType.Finite, requiredCompletions: 1);

        // Act
        var result = service.DeleteTask(task.Id);

        // Assert
        Assert.True(result);
        Assert.Equal(1, repo.DeleteCallCount);
        Assert.Null(service.GetTask(task.Id));
    }

    [Fact]
    public void DeleteTask_NonExistentTask_ReturnsFalse()
    {
        // Arrange
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);

        // Act
        var result = service.DeleteTask(Guid.NewGuid());

        // Assert
        Assert.False(result);
        Assert.Equal(1, repo.DeleteCallCount);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateTask_EmptyTitle_ThrowsArgumentException_EnforcingDomain(string invalidTitle)
    {
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);

        Assert.Throws<ArgumentException>(() => service.CreateTask(invalidTitle, TaskType.Finite, requiredCompletions: 1));
        Assert.Throws<ArgumentException>(() => service.CreateTask(invalidTitle, TaskType.Recurring));
        Assert.Equal(0, repo.AddCallCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void CreateTask_InvalidUrgency_ThrowsArgumentOutOfRangeException_EnforcingDomain(int invalidUrgency)
    {
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.CreateTask("Title", TaskType.Finite, urgency: invalidUrgency, requiredCompletions: 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.CreateTask("Title", TaskType.Recurring, urgency: invalidUrgency));
        Assert.Equal(0, repo.AddCallCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateFiniteTask_InvalidRequiredCompletions_ThrowsArgumentOutOfRangeException_EnforcingDomain(int invalidRequired)
    {
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.CreateTask("Title", TaskType.Finite, requiredCompletions: invalidRequired));
        Assert.Equal(0, repo.AddCallCount);
    }
}
