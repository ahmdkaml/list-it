using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ListIt.Core.Models;
using ListIt.Core.Repositories;

namespace ListIt.Infrastructure.Persistence;

/// <summary>
/// Persists tasks as JSON to %LocalAppData%\ListIt\tasks.json (or a custom path for testing).
/// Thread-safe in-memory cache synchronized with disk writes.
/// </summary>
public class JsonTaskRepository : ITaskRepository
{
    private readonly string _filePath;
    private readonly object _lock = new();
    private readonly Dictionary<Guid, ListitTask> _tasks = new();
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public JsonTaskRepository(string? filePath = null)
    {
        _filePath = filePath ?? GetDefaultFilePath();
        Load();
    }

    public static string GetDefaultFilePath()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, "ListIt", "tasks.json");
    }

    public IReadOnlyList<ListitTask> GetAll()
    {
        lock (_lock)
        {
            return _tasks.Values.ToList().AsReadOnly();
        }
    }

    public ListitTask? GetById(Guid id)
    {
        lock (_lock)
        {
            return _tasks.TryGetValue(id, out var task) ? task : null;
        }
    }

    public void Add(ListitTask task)
    {
        if (task == null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        lock (_lock)
        {
            if (_tasks.ContainsKey(task.Id))
            {
                throw new InvalidOperationException($"Task with ID '{task.Id}' already exists.");
            }

            _tasks[task.Id] = task;
            Save();
        }
    }

    public void Update(ListitTask task)
    {
        if (task == null)
        {
            throw new ArgumentNullException(nameof(task));
        }

        lock (_lock)
        {
            if (!_tasks.ContainsKey(task.Id))
            {
                throw new KeyNotFoundException($"Task with ID '{task.Id}' was not found.");
            }

            _tasks[task.Id] = task;
            Save();
        }
    }

    public bool Delete(Guid id)
    {
        lock (_lock)
        {
            if (_tasks.Remove(id))
            {
                Save();
                return true;
            }

            return false;
        }
    }

    private void Load()
    {
        lock (_lock)
        {
            _tasks.Clear();

            if (!File.Exists(_filePath))
            {
                return;
            }

            var json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var dtos = JsonSerializer.Deserialize<List<TaskDto>>(json, SerializerOptions);
            if (dtos != null)
            {
                foreach (var dto in dtos)
                {
                    var task = dto.ToDomain();
                    _tasks[task.Id] = task;
                }
            }
        }
    }

    private void Save()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var dtos = _tasks.Values.Select(TaskDto.FromDomain).ToList();
        var json = JsonSerializer.Serialize(dtos, SerializerOptions);

        var tempFilePath = Path.Combine(directory ?? ".", $"{Path.GetFileName(_filePath)}.{Guid.NewGuid():N}.tmp");
        File.WriteAllText(tempFilePath, json);
        File.Move(tempFilePath, _filePath, overwrite: true);
    }
}
