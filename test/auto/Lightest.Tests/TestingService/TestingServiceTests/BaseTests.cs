﻿using System.Net;
using Lightest.Data.CodeManagment.Services;
using Lightest.TestingService.Interfaces;
using Moq;

namespace Lightest.Tests.TestingService.TestingServiceTests
{
    public abstract class BaseTests : BaseTest
    {
        protected ITestingService _testingService =>
            new Lightest.TestingService.DefaultServices.DefaultTestingService(_context);

        protected readonly Mock<ITransferServiceFactory> _factoryMock;
        protected readonly Mock<ITransferService> _transferMock;
        protected readonly Mock<ICodeManagmentService> _uploadDataRepository;

        public BaseTests()
        {
            _transferMock = new Mock<ITransferService>();
            _factoryMock = new Mock<ITransferServiceFactory>();
            _factoryMock.Setup(f => f.Create(It.IsAny<IPAddress>(), It.IsAny<int>()))
                .Returns(_transferMock.Object);
            _uploadDataRepository = new Mock<ICodeManagmentService>();
        }
    }
}
