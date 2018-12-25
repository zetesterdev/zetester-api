﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lightest.Api.Models;
using Lightest.Api.ResponseModels;
using Lightest.Api.Services.AccessServices;
using Lightest.Data;
using Lightest.Data.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Lightest.Api.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [Authorize]
    public class ProfileController : BaseUserController
    {
        private readonly IAccessService<ApplicationUser> _accessService;

        public ProfileController(RelationalDbContext context,
            IAccessService<ApplicationUser> accessService) : base(context)
        {
            _accessService = accessService;
        }

        [HttpGet]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ProfileViewModel>))]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetUsers()
        {
            var user = await GetCurrentUser();
            if (!_accessService.CheckAdminAccess(null, user))
            {
                return Forbid();
            }
            return Ok(_context.Users.Select(u => new ProfileViewModel
            {
                Id = u.Id,
                Email = u.Email,
                Name = u.Name,
                Surname = u.Surname
            }));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(200, Type = typeof(CompleteUser))]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetUser([FromRoute] string id)
        {
            var currentUser = await GetCurrentUser();

            var requestedUser = await _context.Users
                .Include(u => u.Groups)
                .ThenInclude(g => g.Group)
                .Include(u => u.Tasks)
                .ThenInclude(t => t.Task)
                .SingleOrDefaultAsync(u => u.Id == id);

            if (requestedUser == null)
            {
                return NotFound();
            }

            if (!_accessService.CheckReadAccess(requestedUser, currentUser))
            {
                return Forbid();
            }

            return Ok(new CompleteUser
            {
                Name = requestedUser.Name,
                Surname = requestedUser.Surname,
                Email = requestedUser.Email,
                Login = requestedUser.UserName,
                Tasks = requestedUser.Tasks.Select(t => new UserTaskViewModel
                {
                    Id = t.Task.Id,
                    Name = t.Task.Name,
                    Completed = t.Completed,
                    HighScore = t.HighScore
                }),
                Groups = requestedUser.Groups.Select(g => new BasicNameViewModel
                {
                    Id = g.GroupId,
                    Name = g.Group.Name
                })
            });
        }

        [HttpPut("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> PutUser([FromRoute] string id, [FromBody]PersonalDataRequest personalData)
        {
            if (id != personalData.UserId)
            {
                return BadRequest();
            }

            var currentUser = await GetCurrentUser();

            var requestedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

            if (requestedUser == null)
            {
                return NotFound();
            }

            if (!_accessService.CheckWriteAccess(requestedUser, currentUser))
            {
                return Forbid();
            }

            requestedUser.Name = personalData.Name;
            requestedUser.Surname = personalData.Surname;
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
