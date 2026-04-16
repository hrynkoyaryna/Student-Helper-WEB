using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Application.Models.Calendar;
using StudentHelper.Application.Services;
using StudentHelper.Domain.Entities;
using Xunit;

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

    #region CreateEventAsync Tests

    [Fact]
    public async Task CreateEventAsync_ShouldReturnFail_WhenUserIdIsInvalid()
    {
        var request = new CreatePersonalEventRequest
        {
            UserId = 0,
            Title = "Meeting",
            Date = new DateOnly(2026, 3, 25),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0)
        };

        var result = await this.calendarService.CreateEventAsync(request);

        Assert.False(result.Success);
        Assert.Equal("Некоректний користувач.", result.Message);
    }

    [Fact]
    public async Task CreateEventAsync_ShouldReturnFail_WhenTitleIsEmpty()
    {
        var request = new CreatePersonalEventRequest
        {
            UserId = 1,
            Title = "   ",
            Date = new DateOnly(2026, 3, 25),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0)
        };

        var result = await this.calendarService.CreateEventAsync(request);

        Assert.False(result.Success);
        Assert.Equal("Назва події є обов'язковою.", result.Message);
    }

    [Fact]
    public async Task CreateEventAsync_ShouldReturnFail_WhenEndTimeIsEarlierThanOrEqualToStartTime()
    {
        var request = new CreatePersonalEventRequest
        {
            UserId = 1,
            Title = "Lecture",
            Date = new DateOnly(2026, 3, 25),
            StartTime = new TimeOnly(12, 0),
            EndTime = new TimeOnly(12, 0)
        };

        var result = await this.calendarService.CreateEventAsync(request);

        Assert.False(result.Success);
        Assert.Equal("Час завершення має бути пізніше за час початку.", result.Message);
    }

    [Fact]
    public async Task CreateEventAsync_ShouldReturnSuccessWithWarning_WhenEventOverlapsExistingEvent()
    {
        var date = new DateOnly(2026, 3, 25);
        var existingEvent = new PersonalEvent
        {
            Id = 1,
            Title = "Existing event",
            UserId = 1,
            StartAt = date.ToDateTime(new TimeOnly(10, 0)),
            EndAt = date.ToDateTime(new TimeOnly(11, 0)),
        };

        this.personalEventRepositoryMock
            .Setup(x => x.GetByUserIdAndDateAsync(1, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalEvent> { existingEvent });

        var request = new CreatePersonalEventRequest
        {
            UserId = 1,
            Title = "New event",
            Date = date,
            StartTime = new TimeOnly(10, 30), // Перетин
            EndTime = new TimeOnly(11, 30),
        };

        var result = await this.calendarService.CreateEventAsync(request);

        // ПЕРЕВІРКА: тепер перетин повертає успіх з попередженням
        Assert.True(result.Success);
        Assert.Equal("Подію успішно створено, але вона ПЕРЕТИНАЄТЬСЯ у часі з іншою вашою подією!", result.Message);
    }

    [Fact]
    public async Task CreateEventAsync_ShouldReturnSuccess_AndSaveEvent_WhenRequestIsValid()
    {
        var date = new DateOnly(2026, 3, 25);
        this.personalEventRepositoryMock
            .Setup(x => x.GetByUserIdAndDateAsync(1, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalEvent>());

        PersonalEvent addedEvent = null!;
        this.personalEventRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PersonalEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PersonalEvent, CancellationToken>((evt, _) => addedEvent = evt)
            .Returns(Task.CompletedTask);

        var request = new CreatePersonalEventRequest
        {
            UserId = 1,
            Title = "Valid event",
            Date = date,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(15, 30),
            Color = "#ff0000"
        };

        var result = await this.calendarService.CreateEventAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(addedEvent);
        Assert.Equal(new DateTime(2026, 3, 25, 14, 0, 0), addedEvent.StartAt);
        Assert.Equal(DateTimeKind.Unspecified, addedEvent.StartAt.Kind); 
        Assert.Equal("#ff0000", addedEvent.Color);
    }

    #endregion

    #region UpdateEventAsync Tests

    [Fact]
    public async Task UpdateEventAsync_ShouldReturnFail_WhenEventNotFound()
    {
        this.personalEventRepositoryMock
            .Setup(x => x.GetByIdAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PersonalEvent)null);

        var request = new EditPersonalEventRequest { Id = 99, UserId = 1, Title = "Title" };
        var result = await this.calendarService.UpdateEventAsync(request);

        Assert.False(result.Success);
        Assert.Equal("Подію не знайдено.", result.Message);
    }

    [Fact]
    public async Task UpdateEventAsync_ShouldReturnSuccess_AndUpdateFields_WhenValid()
    {
        var existingEvent = new PersonalEvent { Id = 1, UserId = 1, Title = "Old" };
        this.personalEventRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEvent);
        this.personalEventRepositoryMock.Setup(x => x.GetByUserIdAndDateAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<PersonalEvent>());

        var request = new EditPersonalEventRequest 
        { 
            Id = 1, UserId = 1, Title = "New Title", 
            Date = new DateOnly(2026, 3, 25), StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0),
            Color = "#123456"
        };

        var result = await this.calendarService.UpdateEventAsync(request);

        Assert.True(result.Success);
        Assert.Equal("New Title", existingEvent.Title);
        Assert.Equal("#123456", existingEvent.Color);
    }

    [Fact]
    public async Task UpdateEventAsync_ShouldIgnoreItself_WhenCheckingOverlaps()
    {
        var existingEvent = new PersonalEvent { Id = 1, UserId = 1, StartAt = new DateTime(2026, 3, 25, 10, 0, 0), EndAt = new DateTime(2026, 3, 25, 11, 0, 0) };
        this.personalEventRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEvent);
        this.personalEventRepositoryMock.Setup(x => x.GetByUserIdAndDateAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<PersonalEvent> { existingEvent });

        var request = new EditPersonalEventRequest { Id = 1, UserId = 1, Title = "Update", Date = new DateOnly(2026, 3, 25), StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0) };

        var result = await this.calendarService.UpdateEventAsync(request);

        Assert.True(result.Success);
        Assert.Equal("Подію успішно оновлено!", result.Message); // Без попередження про перетин
    }

    #endregion

    #region DeleteEventAsync Tests

    [Fact]
    public async Task DeleteEventAsync_ShouldReturnSuccess_WhenOwnerDeletes()
    {
        var existingEvent = new PersonalEvent { Id = 1, UserId = 1 };
        this.personalEventRepositoryMock.Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existingEvent);

        var result = await this.calendarService.DeleteEventAsync(1, 1);

        Assert.True(result.Success);
        this.personalEventRepositoryMock.Verify(x => x.DeleteAsync(existingEvent, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Get Methods Tests

    [Fact]
    public async Task GetUserEventsAsync_ShouldReturnEventsList()
    {
        var events = new List<PersonalEvent> { new PersonalEvent { Id = 1, Title = "E1" } };
        this.personalEventRepositoryMock.Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(events);

        var result = await this.calendarService.GetUserEventsAsync(1);

        Assert.Single(result);
        Assert.Equal("E1", result.First().Title);
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

    #endregion
}