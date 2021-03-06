﻿using System;
using System.Threading.Tasks;
using Lightest.Data.Models;
using Lightest.Data.Models.TaskModels;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Lightest.Tests.Api.Tests.TestsController
{
    public class Get : BaseTest
    {
        [Fact]
        public async Task NoTestFound()
        {
            var result = await _controller.GetTest(Guid.NewGuid());

            Assert.IsAssignableFrom<NotFoundResult>(result);
        }

        [Fact]
        public async Task Forbidden()
        {
            _context.Tests.Add(_test);
            await _context.SaveChangesAsync();

            _accessServiceMock.Setup(m => m.CanEdit(It.IsAny<Guid>(),
                It.Is<ApplicationUser>(u => u.Id == _user.Id)))
                .ReturnsAsync(false);

            var result = await _controller.GetTest(_test.Id);

            Assert.IsAssignableFrom<ForbidResult>(result);
        }

        [Fact]
        public async Task Found()
        {
            _context.Tests.Add(_test);
            await _context.SaveChangesAsync();

            var result = await _controller.GetTest(_test.Id);

            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);

            var testResult = okResult.Value as Test;
            Assert.Equal(_test.Id, testResult.Id);
            Assert.Equal(_test.TaskId, testResult.TaskId);
            Assert.Equal(_test.Input, testResult.Input);
            Assert.Equal(_test.Output, testResult.Output);
        }
    }
}
