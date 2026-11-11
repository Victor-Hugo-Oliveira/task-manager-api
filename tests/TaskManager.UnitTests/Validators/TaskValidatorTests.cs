using FluentValidation.TestHelper;
using Microsoft.Extensions.Time.Testing;
using TaskManager.Application.Tasks;
using TaskManager.Domain.Enums;

namespace TaskManager.UnitTests.Validators;

public class CreateTaskRequestValidatorTests
{
    // "Now" is fixed so the due-date rule is deterministic.
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);
    private readonly CreateTaskRequestValidator _validator = new(new FakeTimeProvider(Now));

    private static CreateTaskRequest Valid() => new("Write tests", "details", TaskPriority.High, Now.UtcDateTime.AddDays(1));

    [Fact]
    public void Valid_request_passes()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Title_is_required(string? title)
    {
        _validator.TestValidate(Valid() with { Title = title! })
            .ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_longer_than_100_characters_fails()
    {
        _validator.TestValidate(Valid() with { Title = new string('a', 101) })
            .ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Description_longer_than_1000_characters_fails()
    {
        _validator.TestValidate(Valid() with { Description = new string('a', 1001) })
            .ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Due_date_in_the_past_fails()
    {
        _validator.TestValidate(Valid() with { DueDate = Now.UtcDateTime.AddMinutes(-1) })
            .ShouldHaveValidationErrorFor(x => x.DueDate)
            .WithErrorMessage("Due date cannot be in the past.");
    }

    [Fact]
    public void Missing_due_date_is_allowed()
    {
        _validator.TestValidate(Valid() with { DueDate = null })
            .ShouldNotHaveValidationErrorFor(x => x.DueDate);
    }

    [Fact]
    public void Undefined_priority_value_fails()
    {
        _validator.TestValidate(Valid() with { Priority = (TaskPriority)99 })
            .ShouldHaveValidationErrorFor(x => x.Priority);
    }
}

public class UpdateTaskRequestValidatorTests
{
    private readonly UpdateTaskRequestValidator _validator = new();

    [Fact]
    public void Due_date_in_the_past_is_allowed_on_update()
    {
        var request = new UpdateTaskRequest("Overdue task", null, TaskPriority.Low, new DateTime(2000, 1, 1));

        _validator.TestValidate(request).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Title_is_required()
    {
        _validator.TestValidate(new UpdateTaskRequest("", null, TaskPriority.Low, null))
            .ShouldHaveValidationErrorFor(x => x.Title);
    }
}

public class TaskQueryParametersValidatorTests
{
    private readonly TaskQueryParametersValidator _validator = new();

    [Fact]
    public void Defaults_are_valid()
    {
        _validator.TestValidate(new TaskQueryParameters()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Page_must_be_at_least_one(int page)
    {
        _validator.TestValidate(new TaskQueryParameters { Page = page })
            .ShouldHaveValidationErrorFor(x => x.Page);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Page_size_must_be_between_1_and_100(int pageSize)
    {
        _validator.TestValidate(new TaskQueryParameters { PageSize = pageSize })
            .ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public void Search_longer_than_100_characters_fails()
    {
        _validator.TestValidate(new TaskQueryParameters { Search = new string('a', 101) })
            .ShouldHaveValidationErrorFor(x => x.Search);
    }
}
