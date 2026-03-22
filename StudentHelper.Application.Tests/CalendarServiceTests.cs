using Microsoft.Extensions.Logging;
using Moq;
using StudentHelper.Application.Interfaces;
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

        this.calendarService = new CalendarService(
            this.personalEventRepositoryMock.Object,
            this.loggerMock.Object);
    }

    [Fact]
    public async Task CreateEventAsync_ShouldReturnFail_WhenUserIdIsInvalid()
    {
        var request = new CreatePersonalEventRequest
        {
            UserId = 0,
            Title = "Meeting",
            Description = "Some description",
            Date = new DateOnly(2026, 3, 25),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
        };

        var result = await this.calendarService.CreateEventAsync(request);

        Assert.False(result.Success);
        Assert.Equal("Некоректний користувач.", result.ErrorMessage);

        this.personalEventRepositoryMock.Verify(
            x => x.GetByUserIdAndDateAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);

        this.personalEventRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PersonalEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        this.personalEventRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateEventAsync_ShouldReturnFail_WhenTitleIsEmpty()
    {
        var request = new CreatePersonalEventRequest
        {
            UserId = 1,
            Title = "   ",
            Description = "Some description",
            Date = new DateOnly(2026, 3, 25),
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
        };

        var result = await this.calendarService.CreateEventAsync(request);

        Assert.False(result.Success);
        Assert.Equal("Назва події є обов’язковою.", result.ErrorMessage);

        this.personalEventRepositoryMock.Verify(
            x => x.GetByUserIdAndDateAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);

        this.personalEventRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PersonalEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        this.personalEventRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateEventAsync_ShouldReturnFail_WhenEndTimeIsEarlierThanOrEqualToStartTime()
    {
        var request = new CreatePersonalEventRequest
        {
            UserId = 1,
            Title = "Lecture",
            Description = "Math lecture",
            Date = new DateOnly(2026, 3, 25),
            StartTime = new TimeOnly(12, 0),
            EndTime = new TimeOnly(12, 0),
        };

        var result = await this.calendarService.CreateEventAsync(request);

        Assert.False(result.Success);
        Assert.Equal("Час завершення має бути пізніше за час початку.", result.ErrorMessage);

        this.personalEventRepositoryMock.Verify(
            x => x.GetByUserIdAndDateAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);

        this.personalEventRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PersonalEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        this.personalEventRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateEventAsync_ShouldReturnFail_WhenEventOverlapsExistingEvent()
    {
        var date = new DateOnly(2026, 3, 25);

        var existingEvent = new PersonalEvent
        {
            Id = 1,
            Title = "Existing event",
            Description = "Already planned",
            UserId = 1,
            StartAt = DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(10, 0)), DateTimeKind.Local).ToUniversalTime(),
            EndAt = DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(11, 0)), DateTimeKind.Local).ToUniversalTime(),
        };

        this.personalEventRepositoryMock
            .Setup(x => x.GetByUserIdAndDateAsync(1, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalEvent> { existingEvent });

        var request = new CreatePersonalEventRequest
        {
            UserId = 1,
            Title = "New event",
            Description = "Overlap test",
            Date = date,
            StartTime = new TimeOnly(10, 30),
            EndTime = new TimeOnly(11, 30),
        };

        var result = await this.calendarService.CreateEventAsync(request);

        Assert.False(result.Success);
        Assert.Equal("Подія перетинається з уже існуючою подією.", result.ErrorMessage);

        this.personalEventRepositoryMock.Verify(
            x => x.GetByUserIdAndDateAsync(1, date, It.IsAny<CancellationToken>()),
            Times.Once);

        this.personalEventRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PersonalEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);

        this.personalEventRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateEventAsync_ShouldReturnSuccess_AndSaveEvent_WhenRequestIsValid()
    {
        var date = new DateOnly(2026, 3, 25);

        this.personalEventRepositoryMock
            .Setup(x => x.GetByUserIdAndDateAsync(1, date, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PersonalEvent>());

        PersonalEvent addedEvent = null;

        this.personalEventRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PersonalEvent>(), It.IsAny<CancellationToken>()))
            .Callback<PersonalEvent, CancellationToken>((evt, _) => addedEvent = evt)
            .Returns(Task.CompletedTask);

        var request = new CreatePersonalEventRequest
        {
            UserId = 1,
            Title = "Valid event",
            Description = "No overlap",
            Date = date,
            StartTime = new TimeOnly(14, 0),
            EndTime = new TimeOnly(15, 30),
        };

        var result = await this.calendarService.CreateEventAsync(request);

        Assert.True(result.Success);
        Assert.Equal(string.Empty, result.ErrorMessage);

        Assert.NotNull(addedEvent);
        Assert.Equal("Valid event", addedEvent!.Title);
        Assert.Equal("No overlap", addedEvent.Description);
        Assert.Equal(1, addedEvent.UserId);
        Assert.Equal(DateTimeKind.Utc, addedEvent.StartAt.Kind);
        Assert.Equal(DateTimeKind.Utc, addedEvent.EndAt.Kind);
        Assert.True(addedEvent.EndAt > addedEvent.StartAt);

        this.personalEventRepositoryMock.Verify(
            x => x.GetByUserIdAndDateAsync(1, date, It.IsAny<CancellationToken>()),
            Times.Once);

        this.personalEventRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PersonalEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);

        this.personalEventRepositoryMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetUserEventsAsync_ShouldReturnEventsFromRepository()
    {
        var events = new List<PersonalEvent>
        {
            new PersonalEvent
            {
                Id = 1,
                Title = "Event 1",
                Description = "Desc 1",
                UserId = 1,
                StartAt = DateTime.UtcNow,
                EndAt = DateTime.UtcNow.AddHours(1),
            },
            new PersonalEvent
            {
                Id = 2,
                Title = "Event 2",
                Description = "Desc 2",
                UserId = 1,
                StartAt = DateTime.UtcNow.AddHours(2),
                EndAt = DateTime.UtcNow.AddHours(3),
            },
        };

        this.personalEventRepositoryMock
            .Setup(x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var result = await this.calendarService.GetUserEventsAsync(1);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Title == "Event 1");
        Assert.Contains(result, x => x.Title == "Event 2");

        this.personalEventRepositoryMock.Verify(
            x => x.GetByUserIdAsync(1, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}