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

    [Fact]
    public async Task GetUserEventsAsync_ShouldReturnUserEvents()
    {
        // Arrange
        var userId = 1;
        var personalEvents = new List<PersonalEvent>
        {
            new PersonalEvent { Id = 1, Title = "Event 1", UserId = userId },
            new PersonalEvent { Id = 2, Title = "Event 2", UserId = userId }
        };

        this.personalEventRepositoryMock
            .Setup(x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(personalEvents);

        // Act
        var result = await this.calendarService.GetUserEventsAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("Event 1", result.First().Title);
        this.personalEventRepositoryMock.Verify(
            x => x.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task CreateEventAsync_ShouldCreateEvent_WhenValid()
    {
        // Arrange
        var request = new CreatePersonalEventRequest
        {
            UserId = 1,
            Title = "New Event",
            Description = "Test event",
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(1)),
            StartTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            EndTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(11)),
            Color = "#FF0000"
        };

        this.personalEventRepositoryMock
            .Setup(x => x.GetByUserIdAndDateAsync(
                request.UserId, 
                request.Date, 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalEvent>());

        this.personalEventRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PersonalEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        this.personalEventRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.calendarService.CreateEventAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Подію успішно створено!", result.Message);
        this.personalEventRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PersonalEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateEventAsync_ShouldReturnFail_WhenUserIdInvalid()
    {
        // Arrange
        var request = new CreatePersonalEventRequest
        {
            UserId = 0,  // Invalid user ID
            Title = "Event",
            Date = DateOnly.FromDateTime(DateTime.Now),
            StartTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            EndTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(11))
        };

        // Act
        var result = await this.calendarService.CreateEventAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Некоректний користувач.", result.Message);
        this.personalEventRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PersonalEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateEventAsync_ShouldReturnFail_WhenTitleEmpty()
    {
        // Arrange
        var request = new CreatePersonalEventRequest
        {
            UserId = 1,
            Title = "",  // Empty title
            Date = DateOnly.FromDateTime(DateTime.Now),
            StartTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            EndTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(11))
        };

        // Act
        var result = await this.calendarService.CreateEventAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Назва події є обов'язковою.", result.Message);
    }

    [Fact]
    public async Task CreateEventAsync_ShouldReturnFail_WhenEndTimeNotAfterStartTime()
    {
        // Arrange
        var request = new CreatePersonalEventRequest
        {
            UserId = 1,
            Title = "Event",
            Date = DateOnly.FromDateTime(DateTime.Now),
            StartTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(11)),
            EndTime = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10))  // Before start time
        };

        // Act
        var result = await this.calendarService.CreateEventAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Час завершення має бути пізніше за час початку.", result.Message);
    }

    [Fact]
    public async Task DeleteEventAsync_ShouldDeleteEvent_WhenUserIsOwner()
    {
        // Arrange
        var eventId = 5;
        var userId = 1;
        var personalEvent = new PersonalEvent { Id = eventId, UserId = userId };

        this.personalEventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(personalEvent);

        this.personalEventRepositoryMock
            .Setup(x => x.DeleteAsync(personalEvent, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        this.personalEventRepositoryMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await this.calendarService.DeleteEventAsync(eventId, userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Подію успішно видалено.", result.Message);
        this.personalEventRepositoryMock.Verify(
            x => x.DeleteAsync(personalEvent, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteEventAsync_ShouldReturnFail_WhenEventNotFound()
    {
        // Arrange
        this.personalEventRepositoryMock
            .Setup(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonalEvent?)null);

        // Act
        var result = await this.calendarService.DeleteEventAsync(999, 1);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Подію не знайдено.", result.Message);
        this.personalEventRepositoryMock.Verify(
            x => x.DeleteAsync(It.IsAny<PersonalEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DeleteEventAsync_ShouldReturnFail_WhenUserIsNotOwner()
    {
        // Arrange
        var eventId = 5;
        var personalEvent = new PersonalEvent { Id = eventId, UserId = 2 };

        this.personalEventRepositoryMock
            .Setup(x => x.GetByIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(personalEvent);

        // Act
        var result = await this.calendarService.DeleteEventAsync(eventId, 1);  // Different user

        // Assert
        Assert.False(result.Success);
        Assert.Equal("У вас немає прав для видалення цієї події.", result.Message);
        this.personalEventRepositoryMock.Verify(
            x => x.DeleteAsync(It.IsAny<PersonalEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}