﻿using Lightest.Data;
using Lightest.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lightest.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckersController : ControllerBase
    {
        private readonly RelationalDbContext _context;

        public CheckersController(RelationalDbContext context)
        {
            _context = context;
        }

        // GET: api/Checkers
        [HttpGet]
        public IEnumerable<Checker> GetCheckers()
        {
            return _context.Checkers;
        }

        // GET: api/Checkers/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetChecker([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var checker = await _context.Checkers.FindAsync(id);

            if (checker == null)
            {
                return NotFound();
            }

            return Ok(checker);
        }

        // PUT: api/Checkers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutChecker([FromRoute] int id, [FromBody] Checker checker)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != checker.Id)
            {
                return BadRequest();
            }

            if (!CheckerExists(id))
            {
                return NotFound();
            }

            _context.Entry(checker).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: api/Checkers
        [HttpPost]
        public async Task<IActionResult> PostChecker([FromBody] Checker checker)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Checkers.Add(checker);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetChecker", new { id = checker.Id }, checker);
        }

        // DELETE: api/Checkers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteChecker([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var checker = await _context.Checkers.FindAsync(id);
            if (checker == null)
            {
                return NotFound();
            }

            _context.Checkers.Remove(checker);
            await _context.SaveChangesAsync();

            return Ok(checker);
        }

        private bool CheckerExists(int id)
        {
            return _context.Checkers.Any(e => e.Id == id);
        }
    }
}