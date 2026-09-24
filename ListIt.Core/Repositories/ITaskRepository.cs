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
    IReadOnlyList<ListitTask> GetAll();
    ListitTask? GetById(Guid id);
    void Add(ListitTask task);
    void Update(ListitTask task);
    bool Delete(Guid id);
}
