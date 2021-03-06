﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lightest.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Lightest.Tests.Api.Tests.CategoriesController
{
    public class GetCategories : BaseTest
    {
        [Fact]
        public async Task HasAdminAccess()
        {
            AddDataToDb();
            await _context.SaveChangesAsync();

            var result = await _controller.GetCategories(new Sieve.Models.SieveModel());

            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);

            var categoriesResult = okResult.Value as IEnumerable<Category>;

            Assert.NotNull(categoriesResult);
            Assert.Equal(3, categoriesResult.Count());
        }

        [Fact]
        public async Task NoAdminAccess()
        {
            AddDataToDb();
            await _context.SaveChangesAsync();

            _roleHelper.Setup(m => m.IsAdmin(It.Is<ApplicationUser>(u => u.Id == _user.Id)))
                .ReturnsAsync(false);

            var result = await _controller.GetCategories(new Sieve.Models.SieveModel());
            Assert.IsAssignableFrom<ForbidResult>(result);
        }
    }
}
