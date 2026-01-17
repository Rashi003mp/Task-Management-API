using Microsoft.EntityFrameworkCore;
using Task_Management_API.Data;
using Task_Management_API.Models;

namespace Task_Management_API.Services
{
    public interface ITaskService
    {
        Task<TaskDto?> GetTaskByIdAsync(int id);
        Task<List<TaskDto>> GetAllTasksAsync();
        Task<List<TaskDto>> GetUserTasksAsync(string userId);
        Task<TaskDto> CreateTaskAsync(string userId, CreateTaskRequest request);
        Task<TaskDto?> UpdateTaskAsync(int id, UpdateTaskRequest request);
        Task<bool> DeleteTaskAsync(int id);
    }

    public class TaskService : ITaskService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TaskService> _logger;

        public TaskService(ApplicationDbContext context, ILogger<TaskService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<TaskDto?> GetTaskByIdAsync(int id)
        {
            try
            {
                var task = await _context.Tasks
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                    return null;

                return MapToDto(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task by ID");
                throw;
            }
        }

        public async Task<List<TaskDto>> GetAllTasksAsync()
        {
            try
            {
                var tasks = await _context.Tasks
                    .Include(t => t.User)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return tasks.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tasks");
                throw;
            }
        }

        public async Task<List<TaskDto>> GetUserTasksAsync(string userId)
        {
            try
            {
                var tasks = await _context.Tasks
                    .Include(t => t.User)
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();

                return tasks.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user tasks");
                throw;
            }
        }

        public async Task<TaskDto> CreateTaskAsync(string userId, CreateTaskRequest request)
        {
            try
            {
                var task = new TaskItem
                {
                    Title = request.Title,
                    Description = request.Description,
                    UserId = userId,
                    Status = Models.TaskStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();

                // Reload with user data
                await _context.Entry(task).Reference(t => t.User).LoadAsync();

                return MapToDto(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                throw;
            }
        }

        public async Task<TaskDto?> UpdateTaskAsync(int id, UpdateTaskRequest request)
        {
            try
            {
                var task = await _context.Tasks
                    .Include(t => t.User)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                    return null;

                task.Title = request.Title;
                task.Description = request.Description;
                task.Status = request.Status;
                task.UpdatedAt = DateTime.UtcNow;

                _context.Tasks.Update(task);
                await _context.SaveChangesAsync();

                return MapToDto(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task");
                throw;
            }
        }

        public async Task<bool> DeleteTaskAsync(int id)
        {
            try
            {
                var task = await _context.Tasks.FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                    return false;

                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task");
                throw;
            }
        }

        private static TaskDto MapToDto(TaskItem task)
        {
            return new TaskDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                UserId = task.UserId,
                UserEmail = task.User?.Email ?? string.Empty,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt
            };
        }
    }
}
