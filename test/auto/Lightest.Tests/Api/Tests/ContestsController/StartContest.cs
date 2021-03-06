﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Lightest.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Lightest.Tests.Api.Tests.ContestsController
{
    public class StartContest : BaseTest
    {
        [Fact]
        public async Task NotFound()
        {
            var result = await _controller.StartContest(Guid.NewGuid());

            Assert.IsAssignableFrom<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task CategoryNotContest()
        {
            _category.Contest = false;
            AddDataToDb();
            await _context.SaveChangesAsync();

            var result = await _controller.StartContest(_category.Id);

            Assert.IsAssignableFrom<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task DefaultSettingsApplied()
        {
            AddDataToDb();
            await _context.SaveChangesAsync();

            var result = await _controller.StartContest(_category.Id);
            Assert.Equal(ContestSettings.Default.Length, result.Value.Length);
            Assert.NotNull(result.Value.StartTime);

            var settings = _context.Contests.First();
            Assert.Equal(ContestSettings.Default.Length, settings.Length);
            Assert.NotNull(settings.StartTime);
        }

        [Fact]
        public async Task StartDateUpdated()
        {
            AddContestToDb();
            AddDataToDb();
            await _context.SaveChangesAsync();

            var result = await _controller.StartContest(_category.Id);
            Assert.NotNull(result.Value.StartTime);

            var settings = _context.Contests.First();
            Assert.NotNull(settings.StartTime);
        }

        [Fact]
        public async Task EndDateUpdated()
        {
            _contest.Length = TimeSpan.FromHours(2);
            AddContestToDb();
            AddDataToDb();
            await _context.SaveChangesAsync();

            var result = await _controller.StartContest(_category.Id);
            Assert.Equal(result.Value.EndTime, result.Value.StartTime + result.Value.Length);

            var settings = _context.Contests.First();
            Assert.Equal(settings.EndTime, settings.StartTime + settings.Length);
        }
    }
}
