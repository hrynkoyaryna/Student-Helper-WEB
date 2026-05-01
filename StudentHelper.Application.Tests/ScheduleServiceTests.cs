using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using StudentHelper.Application.Interfaces;
using StudentHelper.Application.Models;
using StudentHelper.Application.Services;
using StudentHelper.Domain.Entities;
using Xunit;

#nullable enable

namespace StudentHelper.Application.Tests
{
    public class ScheduleServiceTests
    {
        private readonly Mock<IScheduleRepository> _repoMock;
        private readonly ScheduleService _service;

        public ScheduleServiceTests()
        {
            _repoMock = new Mock<IScheduleRepository>();
            _service = new ScheduleService(_repoMock.Object);
        }

        [Fact]
        public async Task CreateLessonForGroupAsync_ShouldReturnFail_WhenNoDatesMatch()
        {
            // StartDate == EndDate and DayOfWeek does not match -> no lessons
            var request = new CreateScheduleLessonRequest
            {
                GroupId = 1,
                SubjectId = 1,
                TeacherId = 1,
                StartDate = new DateTime(2026, 3, 25), // Wednesday
                EndDate = new DateTime(2026, 3, 25),   // same day
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 0, 0)
            };

            var result = await _service.CreateLessonForGroupAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Не знайдено жодної дати, що відповідає критеріям.", result.Message);
            _repoMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<ScheduleLesson>>()), Times.Never);
        }

        [Fact]
        public async Task CreateLessonForGroupAsync_ShouldCreateLessons_WhenValid()
        {
            // From Monday to next Monday (2 occurrences)
            var start = new DateTime(2026, 3, 23); // Monday
            var end = new DateTime(2026, 3, 30); // next Monday

            var request = new CreateScheduleLessonRequest
            {
                GroupId = 2,
                SubjectId = 3,
                TeacherId = 4,
                StartDate = start,
                EndDate = end,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeSpan(8, 30, 0),
                EndTime = new TimeSpan(10, 0, 0),
                Room = "101",
                LessonType = "Лекція"
            };

            IEnumerable<ScheduleLesson>? captured = null;
            _repoMock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ScheduleLesson>>()))
                .Callback<IEnumerable<ScheduleLesson>>(l => captured = l)
                .Returns(Task.CompletedTask);

            _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

            var result = await _service.CreateLessonForGroupAsync(request);

            Assert.True(result.Success);
            _repoMock.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<ScheduleLesson>>()), Times.Once);
            _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);

            Assert.NotNull(captured);
            var list = captured!.ToList();
            // Should contain at least two lessons: 2026-03-23 and 2026-03-30
            Assert.True(list.Count >= 2);
            Assert.Contains(list, s => s.Date.Date == start.Date && s.GroupId == 2 && s.Room == "101");
        }

        [Fact]
        public async Task CreateGroupAsync_ShouldReturnFail_WhenNameEmpty()
        {
            var result = await _service.CreateGroupAsync("   ");
            Assert.False(result.Success);
            Assert.Equal("Назва групи не може бути порожньою.", result.Message);
            _repoMock.Verify(r => r.CreateGroupAsync(It.IsAny<Group>()), Times.Never);
        }

        [Fact]
        public async Task CreateGroupAsync_ShouldReturnFail_WhenGroupExists()
        {
            _repoMock.Setup(r => r.GetAllGroupsAsync())
                .ReturnsAsync(new List<Group> { new Group { Name = "CS-101" } });

            var result = await _service.CreateGroupAsync("CS-101");

            Assert.False(result.Success);
            Assert.Equal("Група з такою назвою вже існує.", result.Message);
            _repoMock.Verify(r => r.CreateGroupAsync(It.IsAny<Group>()), Times.Never);
        }

        [Fact]
        public async Task CreateGroupAsync_ShouldCreateGroup_WhenValid()
        {
            _repoMock.Setup(r => r.GetAllGroupsAsync()).ReturnsAsync(new List<Group>());
            _repoMock.Setup(r => r.CreateGroupAsync(It.IsAny<Group>())).Returns(Task.CompletedTask);

            var result = await _service.CreateGroupAsync("NewGroup");

            Assert.True(result.Success);
            _repoMock.Verify(r => r.CreateGroupAsync(It.Is<Group>(g => g.Name == "NewGroup")), Times.Once);
        }

        [Fact]
        public async Task DeleteLessonAsync_ShouldReturnFail_WhenNotFound()
        {
            _repoMock.Setup(r => r.GetByIdAsync(5)).ReturnsAsync((ScheduleLesson?)null);

            var result = await _service.DeleteLessonAsync(5);

            Assert.False(result.Success);
            Assert.Equal("Заняття не знайдено.", result.Message);
            _repoMock.Verify(r => r.DeleteAsync(It.IsAny<ScheduleLesson>()), Times.Never);
        }

        [Fact]
        public async Task DeleteLessonAsync_ShouldDelete_WhenFound()
        {
            var lesson = new ScheduleLesson { Id = 7 };
            _repoMock.Setup(r => r.GetByIdAsync(7)).ReturnsAsync(lesson);
            _repoMock.Setup(r => r.DeleteAsync(lesson)).Returns(Task.CompletedTask);

            var result = await _service.DeleteLessonAsync(7);

            Assert.True(result.Success);
            _repoMock.Verify(r => r.DeleteAsync(lesson), Times.Once);
        }

        [Fact]
        public async Task GetGroupScheduleAsync_ForwardsToRepository()
        {
            var lessons = new List<ScheduleLesson> { new ScheduleLesson { Id = 1, GroupId = 9 } };
            _repoMock.Setup(r => r.GetByGroupIdAsync(9)).ReturnsAsync(lessons);

            var result = await _service.GetGroupScheduleAsync(9);

            Assert.Equal(lessons, result);
        }

        [Fact]
        public async Task GetLessonByIdAsync_ForwardsToRepository()
        {
            var lesson = new ScheduleLesson { Id = 11 };
            _repoMock.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(lesson);

            var result = await _service.GetLessonByIdAsync(11);

            Assert.Equal(lesson, result);
        }
    }
}
