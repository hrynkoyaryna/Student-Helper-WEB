using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using StudentHelper.Domain.Entities;
using StudentHelper.Infrastructure.Data;
using StudentHelper.Infrastructure.Services;
using Xunit;

namespace StudentHelper.Application.Tests;

public class TaskServiceTests
{
	private static StudentHelperDbContext CreateContext()
	{
		var options = new DbContextOptionsBuilder<StudentHelperDbContext>()
			.UseInMemoryDatabase(Guid.NewGuid().ToString())
			.Options;

		return new StudentHelperDbContext(options);
	}

	private static TaskService CreateService(StudentHelperDbContext context)
	{
		return new TaskService(context, new NullLogger<TaskService>());
	}

	[Fact]
	public async Task CreateTaskAsync_Should_Add_Task()
	{
		using var context = CreateContext();
		var service = CreateService(context);

		var task = new TaskItem
		{
			Title = "ϳ��������� �����������",
			Deadline = DateTime.UtcNow.AddDays(1),
			Status = "�������",
			Subject = "���� �����",
			UserId = 1
		};

		await service.CreateTaskAsync(task);

		var savedTask = await context.Tasks.FirstOrDefaultAsync();

		Assert.NotNull(savedTask);
		Assert.Equal("ϳ��������� �����������", savedTask!.Title);
		Assert.Equal(1, savedTask.UserId);
	}

	[Fact]
	public async Task CreateTaskAsync_Should_Set_Overdue_When_Deadline_In_Past()
	{
		using var context = CreateContext();
		var service = CreateService(context);

		var task = new TaskItem
		{
			Title = "����� ��������",
			Deadline = DateTime.UtcNow.AddDays(-1),
			Status = "�������",
			Subject = "���",
			UserId = 1
		};

		await service.CreateTaskAsync(task);

		var savedTask = await context.Tasks.FirstAsync();
		Assert.Equal("�����������", savedTask.Status);
	}

	[Fact]
	public async Task GetUserTasksAsync_Should_Return_Only_User_Tasks()
	{
		using var context = CreateContext();

		context.Tasks.AddRange(
			new TaskItem
			{
				Title = "�� ��������",
				Deadline = DateTime.UtcNow.AddDays(1),
				Status = "�������",
				Subject = "����������",
				UserId = 1
			},
			new TaskItem
			{
				Title = "���� ��������",
				Deadline = DateTime.UtcNow.AddDays(1),
				Status = "�������",
				Subject = "Գ����",
				UserId = 2
			});

		await context.SaveChangesAsync();

		var service = CreateService(context);

		var tasks = await service.GetUserTasksAsync(1);

		Assert.Single(tasks);
		Assert.Equal("�� ��������", tasks[0].Title);
	}

	[Fact]
	public async Task UpdateTaskAsync_Should_Update_Task_When_Owner_Matches()
	{
		using var context = CreateContext();

		var existingTask = new TaskItem
		{
			Title = "������ ���������",
			Deadline = DateTime.UtcNow.AddDays(1),
			Status = "�������",
			Subject = "���������",
			UserId = 1
		};

		context.Tasks.Add(existingTask);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		var updatedTask = new TaskItem
		{
			Id = existingTask.Id,
			Title = "����� ���������",
			Deadline = DateTime.UtcNow.AddDays(2),
			Status = "��������",
			Subject = "���������"
		};

		var result = await service.UpdateTaskAsync(updatedTask, 1);

		Assert.True(result.Success);

		var savedTask = await context.Tasks.FirstAsync();
		Assert.Equal("����� ���������", savedTask.Title);
		Assert.Equal("��������", savedTask.Status);
	}

	[Fact]
	public async Task UpdateTaskAsync_Should_Return_False_When_Task_Belongs_To_Another_User()
	{
		using var context = CreateContext();

		var existingTask = new TaskItem
		{
			Title = "���� ��������",
			Deadline = DateTime.UtcNow.AddDays(1),
			Status = "�������",
			Subject = "���",
			UserId = 2
		};

		context.Tasks.Add(existingTask);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		var updatedTask = new TaskItem
		{
			Id = existingTask.Id,
			Title = "���� �����",
			Deadline = DateTime.UtcNow.AddDays(2),
			Status = "��������",
			Subject = "���"
		};

		var result = await service.UpdateTaskAsync(updatedTask, 1);

		Assert.False(result.Success);
	}

	[Fact]
	public async Task DeleteTaskAsync_Should_Delete_Task_When_Owner_Matches()
	{
		using var context = CreateContext();

		var task = new TaskItem
		{
			Title = "�������� ����",
			Deadline = DateTime.UtcNow.AddDays(1),
			Status = "�������",
			Subject = "�������������",
			UserId = 1
		};

		context.Tasks.Add(task);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		var result = await service.DeleteTaskAsync(task.Id, 1);

		Assert.True(result.Success);
		Assert.Empty(context.Tasks);
	}

	[Fact]
	public async Task ChangeStatusAsync_Should_Change_Status()
	{
		using var context = CreateContext();

		var task = new TaskItem
		{
			Title = "������ ������",
			Deadline = DateTime.UtcNow.AddDays(1),
			Status = "�������",
			Subject = "Web",
			UserId = 1
		};

		context.Tasks.Add(task);
		await context.SaveChangesAsync();

		var service = CreateService(context);

		var result = await service.ChangeStatusAsync(task.Id, 1, "��������");

		Assert.True(result.Success);

		var updatedTask = await context.Tasks.FirstAsync();
		Assert.Equal("��������", updatedTask.Status);
	}
}
