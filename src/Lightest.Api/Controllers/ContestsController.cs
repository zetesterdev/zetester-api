﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lightest.Api.RequestModels.ContestRequests;
using Lightest.Api.ResponseModels.ContestViews;
using Lightest.Api.ResponseModels.ContestViews.ContestTable;
using Lightest.Data;
using Lightest.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lightest.Api.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    public class ContestsController : BaseUserController
    {
        public ContestsController(RelationalDbContext context, UserManager<ApplicationUser> userManager) : base(context, userManager)
        {
        }

        [HttpPost("add-users")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<AddToContestView>))]
        [ProducesResponseType(403)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<ActionResult<IEnumerable<AddToContestView>>> AddToContest(AddToContestByNameRequest request)
        {
            if (string.IsNullOrEmpty(request.Pattern))
            {
                ModelState.AddModelError(nameof(request.Pattern), "Pattern required");
                return BadRequest(ModelState);
            }

            if (request.ContestId == default)
            {
                ModelState.AddModelError(nameof(request.ContestId), "ContestId required");
                return BadRequest(ModelState);
            }

            var category = await _context.Categories
                .AsNoTracking().Include(c => c.Users)
                .FirstOrDefaultAsync(c => c.Id == request.ContestId);

            if (category == null)
            {
                return NotFound(nameof(request.ContestId));
            }

            var users = _context.Users.Where(u => EF.Functions.Like(u.UserName, request.Pattern))
                .Select(u => u.Id);

            var result = new List<AddToContestView>();

            foreach (var userId in users)
            {
                if (category.Users.Any(u => u.UserId == userId))
                {
                    continue;
                }

                category.Users.Add(new CategoryUser
                {
                    UserId = userId,
                    CanRead = true
                });

                result.Add(new AddToContestView
                {
                    UserId = userId
                });
            }

            await _context.SaveChangesAsync();

            return result;
        }

        [HttpPost("{contestId}/start")]
        public async Task<ActionResult<ContestSettings>> StartContest(Guid contestId)
        {
            // TODO: implement assignment helper, which assigns tasks to all users from a list,
            // removes assignments from users, clears uploads.
            var dbSettings = _context.Contests.Find(contestId);
            if (dbSettings == null)
            {
                var settingsResult = await CreateDefaultSettings(contestId);
                dbSettings = settingsResult.Value;
                if (dbSettings == null)
                {
                    return settingsResult.Result;
                }
            }

            dbSettings.StartTime = DateTime.Now;
            if (dbSettings.Length.HasValue)
            {
                dbSettings.EndTime = dbSettings.StartTime + dbSettings.Length;
            }

            await _context.SaveChangesAsync();
            return dbSettings;
        }

        [HttpPost("{contestId}/reset")]
        public async Task<ActionResult<ContestSettings>> ResetContest(Guid contestId)
        {
            var dbSettings = _context.Contests.Find(contestId);
            if (dbSettings == null)
            {
                return NotFound();
            }

            dbSettings.StartTime = null;
            dbSettings.EndTime = null;
            await _context.SaveChangesAsync();

            return dbSettings;
        }

        [HttpPost("{contestId}/stop")]
        public async Task<ActionResult<ContestSettings>> StopContest(Guid contestId)
        {
            var dbSettings = _context.Contests.Find(contestId);
            if (dbSettings == null)
            {
                return NotFound();
            }

            dbSettings.EndTime = DateTime.Now;
            await _context.SaveChangesAsync();

            return dbSettings;
        }

        // TODO: Add get method
        [HttpPut("{contestId}/settings")]
        public async Task<ActionResult<ContestSettings>> ChangeSettings([FromRoute] Guid contestId, [FromBody] UpdateSettingsRequest settings)
        {
            var dbSettings = _context.Contests.Find(contestId);
            if (dbSettings == null)
            {
                var settingsResult = await CreateDefaultSettings(contestId);
                dbSettings = settingsResult.Value;
                if (dbSettings == null)
                {
                    return settingsResult.Result;
                }
            }

            dbSettings.StartTime = settings.StartTime;
            dbSettings.Length = settings.Length;

            if (dbSettings.StartTime.HasValue && dbSettings.Length.HasValue)
            {
                dbSettings.EndTime = dbSettings.StartTime + dbSettings.Length;
            }

            await _context.SaveChangesAsync();
            return dbSettings;
        }

        [HttpGet("{contestId}/settings")]
        public async Task<ActionResult<ContestSettings>> GetSettings([FromRoute] Guid contestId)
        {
            var dbSettings = _context.Contests.Find(contestId);
            if (dbSettings == null)
            {
                var category = await _context.Categories.AsNoTracking()
                                .Select(c => new { c.Contest, c.Id })
                                .FirstOrDefaultAsync(c => c.Id == contestId);

                if (category == null)
                {
                    return NotFound();
                }

                if (!category.Contest)
                {
                    ModelState.AddModelError(nameof(contestId), $"Category with id {contestId.ToString()} is not a contest");
                    return BadRequest(ModelState);
                }

                dbSettings = ContestSettings.Default;
            }

            await _context.SaveChangesAsync();
            return dbSettings;
        }

        private async Task<ActionResult<ContestSettings>> CreateDefaultSettings(Guid contestId)
        {
            var category = await _context.Categories.AsNoTracking()
                                .Select(c => new { c.Contest, c.Id })
                                .FirstOrDefaultAsync(c => c.Id == contestId);

            if (category == null)
            {
                return NotFound();
            }

            if (!category.Contest)
            {
                ModelState.AddModelError(nameof(contestId), $"Category with id {contestId.ToString()} is not a contest");
                return BadRequest(ModelState);
            }

            var dbSettings = ContestSettings.Default;
            dbSettings.CategoryId = contestId;
            _context.Contests.Add(dbSettings);

            return dbSettings;
        }

        [HttpGet("{contestId}/table")]
        public async Task<ActionResult<ContestTableView>> GetContestTable(Guid contestId)
        {
            var contest = await _context.Categories.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == contestId);

            if (contest == null)
            {
                return NotFound();
            }

            var tasks = _context.Tasks.AsNoTracking()
                .Include(t => t.Users)
                .Where(t => t.CategoryId == contestId)
                .Select(t => new TaskResultsView
                {
                    TaskId = t.Id,
                    UserResults = t.Users.Select(u => new UserResultView
                    {
                        Score = u.HighScore,
                        UserId = u.UserId
                    })
                });

            var result = new ContestTableView
            {
                ContestId = contestId,
                Name = contest.Name,
                TaskResults = tasks
            };

            return result;
        }
    }
}
