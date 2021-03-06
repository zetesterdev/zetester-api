﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lightest.AccessService.Interfaces;
using Lightest.Api.ResponseModels.Checker;
using Lightest.Api.ResponseModels.Language;
using Lightest.Api.ResponseModels.TaskViews;
using Lightest.Data;
using Lightest.Data.Models;
using Lightest.Data.Models.TaskModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;
using Sieve.Services;

namespace Lightest.Api.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    public class TasksController : BaseUserController
    {
        private readonly IAccessService<TaskDefinition> _accessService;
        private readonly ISieveProcessor _sieveProcessor;
        private readonly IRoleHelper _roleHelper;

        public TasksController(
            RelationalDbContext context,
            UserManager<ApplicationUser> userManager,
            IAccessService<TaskDefinition> accessService,
            IRoleHelper roleHelper,
            ISieveProcessor sieveProcessor) : base(context, userManager)
        {
            _accessService = accessService;
            _sieveProcessor = sieveProcessor;
            _roleHelper = roleHelper;
        }

        // GET: api/Tasks
        [HttpGet]
        [ProducesResponseType(typeof(TaskDefinition), 200)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetTasks([FromQuery] SieveModel sieveModel)
        {
            var user = await GetCurrentUser();

            // TODO: disallow teacher access (update to UI is required)
            if (!await _roleHelper.IsAdmin(user))
            {
                return Forbid();
            }

            var tasks = _context.Tasks.AsNoTracking();
            tasks = _sieveProcessor.Apply(sieveModel, tasks);

            return Ok(tasks);
        }

        // GET: api/Tasks/5
        [HttpGet("{id}")]
        [ProducesResponseType(200, Type = typeof(CompleteTaskView))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetTask([FromRoute] Guid id)
        {
            var task = await _context.Tasks
                .AsNoTracking()
                .Include(t => t.Tests)
                .Include(t => t.Languages)
                .ThenInclude(l => l.Language)
                .Include(t => t.Category)
                .Include(t => t.Checker)
                .SingleOrDefaultAsync(t => t.Id == id);

            var user = await GetCurrentUser();

            if (task == null)
            {
                return NotFound();
            }

            if (!await _accessService.CanRead(task.Id, user))
            {
                return Forbid();
            }

            var result = new CompleteTaskView
            {
                Id = task.Id,
                Name = task.Name,
                Points = task.Points,
                Public = task.Public,
                Examples = task.Examples,
                Description = task.Description,
                Category = task.Category,
                Checker = new BasicCheckerView
                {
                    Id = task.Checker.Id,
                    Name = task.Checker.Name,
                    Compiled = task.Checker.Compiled
                },
                Tests = task.Tests,
                Languages = task.Languages.Select(t => new BasicLanguageView
                {
                    Id = t.LanguageId,
                    Name = t.Language.Name,
                    MemoryLimit = t.MemoryLimit,
                    TimeLimit = t.TimeLimit
                })
            };

            if (!await _accessService.CanEdit(task.Id, user))
            {
                result.Tests = null;
                result.Checker = null;
            }

            return Ok(result);
        }

        // POST: api/Tasks
        [HttpPost]
        [ProducesResponseType(typeof(TaskDefinition), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> PostTask([FromBody] TaskDefinition task)
        {
            var user = await GetCurrentUser();

            if (!await _accessService.CanAdd(task, user))
            {
                return Forbid();
            }

            var category = await _context.Categories
                    .SingleOrDefaultAsync(c => c.Id == task.CategoryId);

            if (task.Public && !category.Public)
            {
                return BadRequest(nameof(task.Public));
            }

            if (category.Contest)
            {
                task.Public = true;
            }

            task.Users = new List<Assignment>
            {
                new Assignment { UserId = user.Id, CanRead = true, CanWrite = true, CanChangeAccess = true, IsOwner = true }
            };

            _context.Tasks.Add(task);

            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTask", new { id = task.Id }, task);
        }

        [HttpPost("{id}/languages")]
        [ProducesResponseType(200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SetLanguages([FromRoute] Guid id, [FromBody] TaskLanguage[] languages)
        {
            var task = await _context.Tasks
                .Include(t => t.Languages)
                .SingleOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            if (!await _accessService.CanEdit(task.Id, await GetCurrentUser()))
            {
                return Forbid();
            }

            task.Languages.Clear();
            foreach (var language in languages)
            {
                language.TaskId = id;
                task.Languages.Add(language);
            }

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("{id}/tests")]
        [ProducesResponseType(200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> SetTests([FromRoute] Guid id, [FromBody] Test[] tests)
        {
            var task = await _context.Tasks
                .Include(t => t.Tests)
                .SingleOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            if (!await _accessService.CanEdit(task.Id, await GetCurrentUser()))
            {
                return Forbid();
            }

            task.Tests.Clear();
            await _context.SaveChangesAsync();

            foreach (var test in tests)
            {
                test.Input = test.Input.Replace("\r\n", "\n");
                test.Output = test.Output.Replace("\r\n", "\n");
                test.TaskId = id;
                task.Tests.Add(test);
                _context.Tests.Add(test);
            }

            await _context.SaveChangesAsync();

            return Ok();
        }

        // PUT: api/Tasks/5
        [HttpPut("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutTask([FromRoute] Guid id, [FromBody] TaskDefinition task)
        {
            if (id != task.Id)
            {
                return BadRequest();
            }

            var dbEntry = _context.Tasks.Find(id);

            if (dbEntry == null)
            {
                return NotFound();
            }

            if (!await _accessService.CanEdit(task.Id, await GetCurrentUser()))
            {
                return Forbid();
            }

            dbEntry.CategoryId = task.CategoryId;
            dbEntry.Examples = task.Examples;
            dbEntry.Description = task.Description;
            dbEntry.Points = task.Points;
            dbEntry.CheckerId = task.CheckerId;
            dbEntry.Name = task.Name;

            if (dbEntry.Public != task.Public)
            {
                var category = _context.Categories.Find(task.CategoryId);
                if (category.Contest && !task.Public)
                {
                    ModelState.AddModelError(nameof(task.Public), "Contest tasks can only be public.");
                    return BadRequest(ModelState);
                }

                dbEntry.Public = task.Public;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // DELETE: api/Tasks/5
        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteTask([FromRoute] Guid id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }

            if (!await _accessService.CanEdit(task.Id, await GetCurrentUser()))
            {
                return Forbid();
            }

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return Ok(task);
        }
    }
}
