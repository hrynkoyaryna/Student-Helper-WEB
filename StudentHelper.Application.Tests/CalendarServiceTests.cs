using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Application.Models.Calendar;
using StudentHelper.Application.Services;
using StudentHelper.Domain.Entities;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace StudentHelper.Application.Tests;

public class CalendarServiceTests
{
    private readonly Mock<IPersonalEventRepository> personalEventRepositoryMock;
    private readonly Mock<ILogger<CalendarService>> loggerMock;
    private readonly CalendarService calendarService;

    public CalendarServiceTests()
    {
        this.personalEventRepositoryMock = new Mock<IPersonalEventRepository>();
        this.loggerMock = new Mock<ILogger<CalendarService>>();
        var settingsMock = Options.Create(new ApplicationSettings
        {
            MinSearchCharacters = 3,
            ItemsPerPage = 10,
            CalendarStartHour = 7,
            MaxTaskDescriptionLength = 500
        });

        this.calendarService = new CalendarService(
            this.personalEventRepositoryMock.Object,
            this.loggerMock.Object,
            settingsMock);
    }

    [Fact]
    public async Task UpdateEventAsync_ShouldReturnFail_WhenEventNotFound()
    {
        // Виправлено: явне приведення до (PersonalEvent?)null
        this.personalEventRepositoryMock
            .Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonalEvent?)null);

        var request = new EditPersonalEventRequest { Id = 99, UserId = 1, Title = "Title" };
        var result = await this.calendarService.UpdateEventAsync(request);

        Assert.False(result.Success);
        Assert.Equal("Подію не знайдено.", result.Message);
    }

    [Fact]
    public async Task GetEventByIdAsync_ShouldReturnCorrectEvent()
    {
        var ev = new PersonalEvent { Id = 5 };
        this.personalEventRepositoryMock.Setup(x => x.GetByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(ev);

        var result = await this.calendarService.GetEventByIdAsync(5);

        Assert.NotNull(result);
        Assert.Equal(5, result!.Id);
    }

    // ... решта тестів залишається без змін, оскільки вони не викликають помилок NRT
}