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
        private readonly Dictionary<Guid, TaskBase> _tasks = new();

        public int AddCallCount { get; private set; }
        public int UpdateCallCount { get; private set; }
        public int DeleteCallCount { get; private set; }

        public IReadOnlyList<TaskBase> GetAll()
        {
            return _tasks.Values.ToList().AsReadOnly();
        }

        public TaskBase? GetById(Guid id)
        {
            return _tasks.TryGetValue(id, out var task) ? task : null;
        }

        public void Add(TaskBase task)
        {
            AddCallCount++;
            _tasks[task.Id] = task;
        }

        public void Update(TaskBase task)
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
    public void CreateRecurringTask_CreatesDomainModel_AndAddsToRepository()
    {
        // Arrange
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);
        var times = new[] { new TimeOnly(9, 0), new TimeOnly(18, 0) };

        // Act
        var task = service.CreateRecurringTask("Daily Standup", times, "Engineering sync", 3);

        // Assert
        Assert.NotNull(task);
        Assert.Equal(TaskType.Recurring, task.Type);
        Assert.Equal("Daily Standup", task.Title);
        Assert.Equal("Engineering sync", task.Description);
        Assert.Equal(3, task.Urgency);
        Assert.Equal(2, task.AssignedTimes.Count);
        Assert.Equal(1, repo.AddCallCount);
        Assert.Same(task, repo.GetById(task.Id));
    }

    [Fact]
    public void CreateFiniteTask_CreatesDomainModel_AndAddsToRepository()
    {
        // Arrange
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);

        // Act
        var task = service.CreateFiniteTask("Submit Timesheet", requiredCompletions: 1, description: "Finance", urgency: 2);

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
        service.CreateFiniteTask("Task 1", 1);
        service.CreateRecurringTask("Task 2", new[] { new TimeOnly(10, 0) });

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
        var created = service.CreateFiniteTask("Task 1", 1);

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
        var task = service.CreateFiniteTask("Original Title", 2);

        // Act
        task.SetTitle("Updated Title");
        task.RecordCompletion();
        service.UpdateTask(task);

        // Assert
        Assert.Equal(1, repo.UpdateCallCount);
        var updated = service.GetTask(task.Id) as FiniteTask;
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
        var nonExistent = new FiniteTask("Ghost Task", 1);

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
        var task = service.CreateFiniteTask("To Delete", 1);

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

        Assert.Throws<ArgumentException>(() => service.CreateFiniteTask(invalidTitle, 1));
        Assert.Throws<ArgumentException>(() => service.CreateRecurringTask(invalidTitle, new[] { new TimeOnly(9, 0) }));
        Assert.Equal(0, repo.AddCallCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void CreateTask_InvalidUrgency_ThrowsArgumentOutOfRangeException_EnforcingDomain(int invalidUrgency)
    {
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.CreateFiniteTask("Title", 1, urgency: invalidUrgency));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.CreateRecurringTask("Title", new[] { new TimeOnly(9, 0) }, urgency: invalidUrgency));
        Assert.Equal(0, repo.AddCallCount);
    }

    [Fact]
    public void CreateRecurringTask_EmptyAssignedTimes_ThrowsArgumentException_EnforcingDomain()
    {
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);

        Assert.Throws<ArgumentException>(() => service.CreateRecurringTask("Title", Array.Empty<TimeOnly>()));
        Assert.Equal(0, repo.AddCallCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateFiniteTask_InvalidRequiredCompletions_ThrowsArgumentOutOfRangeException_EnforcingDomain(int invalidRequired)
    {
        var repo = new InMemoryTaskRepository();
        var service = new TaskService(repo);

        Assert.Throws<ArgumentOutOfRangeException>(() => service.CreateFiniteTask("Title", invalidRequired));
        Assert.Equal(0, repo.AddCallCount);
    }
}
