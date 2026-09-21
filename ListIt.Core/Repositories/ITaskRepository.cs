using System;
using System.Collections.Generic;
using Task = ListIt.Core.Models.Task;

namespace ListIt.Core.Repositories;

/// <summary>
/// Domain abstraction for task persistence.
/// Concrete implementations reside in ListIt.Infrastructure.
/// </summary>
public interface ITaskRepository
{
    IReadOnlyList<Task> GetAll();
    Task? GetById(Guid id);
    void Add(Task task);
    void Update(Task task);
    bool Delete(Guid id);
}
