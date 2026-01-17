using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Task_Management_API.Models;
using Task_Management_API.Services;

namespace Task_Management_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ITaskService taskService, ILogger<TasksController> logger)
        {
            _taskService = taskService;
            _logger = logger;
        }

        private string GetUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        /// <summary>
        /// Get all tasks (Admin only)
        /// </summary>
        /// <returns>List of all tasks</returns>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<TaskDto>>> GetAllTasks()
        {
            try
            {
                var tasks = await _taskService.GetAllTasksAsync();
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all tasks");
                return StatusCode(500, new { message = "An error occurred while retrieving tasks" });
            }
        }

        /// <summary>
        /// Get current user's tasks
        /// </summary>
        /// <returns>List of user's tasks</returns>
        [HttpGet("my-tasks")]
        public async Task<ActionResult<List<TaskDto>>> GetMyTasks()
        {
            try
            {
                var userId = GetUserId();
                var tasks = await _taskService.GetUserTasksAsync(userId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user tasks");
                return StatusCode(500, new { message = "An error occurred while retrieving your tasks" });
            }
        }

        /// <summary>
        /// Get a specific task by ID
        /// </summary>
        /// <param name="id">Task ID</param>
        /// <returns>Task details if found</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDto>> GetTaskById(int id)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(id);
                if (task == null)
                {
                    return NotFound(new { message = "Task not found" });
                }

                // Check authorization: Admin can view all, User can only view their own
                var userId = GetUserId();
                if (!IsAdmin() && task.UserId != userId)
                {
                    return Forbid();
                }

                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task by ID");
                return StatusCode(500, new { message = "An error occurred while retrieving the task" });
            }
        }

        /// <summary>
        /// Create a new task
        /// </summary>
        /// <param name="request">Task details</param>
        /// <returns>Created task</returns>
        [HttpPost]
        public async Task<ActionResult<TaskDto>> CreateTask([FromBody] CreateTaskRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid input" });
                }

                var userId = GetUserId();
                var task = await _taskService.CreateTaskAsync(userId, request);

                return CreatedAtAction(nameof(GetTaskById), new { id = task.Id }, task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                return StatusCode(500, new { message = "An error occurred while creating the task" });
            }
        }

        /// <summary>
        /// Update an existing task
        /// </summary>
        /// <param name="id">Task ID</param>
        /// <param name="request">Updated task details</param>
        /// <returns>Updated task</returns>
        [HttpPut("{id}")]
        public async Task<ActionResult<TaskDto>> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { message = "Invalid input" });
                }

                var task = await _taskService.GetTaskByIdAsync(id);
                if (task == null)
                {
                    return NotFound(new { message = "Task not found" });
                }

                // Check authorization: Admin can update all, User can only update their own
                var userId = GetUserId();
                if (!IsAdmin() && task.UserId != userId)
                {
                    return Forbid();
                }

                var updatedTask = await _taskService.UpdateTaskAsync(id, request);

                return Ok(updatedTask);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task");
                return StatusCode(500, new { message = "An error occurred while updating the task" });
            }
        }

        /// <summary>
        /// Delete a task (Admin only)
        /// </summary>
        /// <param name="id">Task ID</param>
        /// <returns>Success message</returns>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteTask(int id)
        {
            try
            {
                var success = await _taskService.DeleteTaskAsync(id);

                if (!success)
                {
                    return NotFound(new { message = "Task not found" });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task");
                return StatusCode(500, new { message = "An error occurred while deleting the task" });
            }
        }
    }
}
