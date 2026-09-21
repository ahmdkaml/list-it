using System;
using System.Collections.Generic;
using ListIt.Core.Models;

namespace ListIt.Core.Repositories;

/// <summary>
/// Domain abstraction for task persistence.
/// Concrete implementations reside in ListIt.Infrastructure.
/// </summary>
public interface ITaskRepository
{
    IReadOnlyList<TaskBase> GetAll();
    TaskBase? GetById(Guid id);
    void Add(TaskBase task);
    void Update(TaskBase task);
    bool Delete(Guid id);
}
