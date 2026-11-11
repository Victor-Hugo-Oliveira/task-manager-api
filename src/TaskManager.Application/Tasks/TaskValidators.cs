using FluentValidation;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tasks;

public class CreateTaskRequestValidator : AbstractValidator<CreateTaskRequest>
{
    // TimeProvider is injected so tests can control "now".
    public CreateTaskRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(TaskItem.TitleMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(TaskItem.DescriptionMaxLength);

        RuleFor(x => x.Priority)
            .IsInEnum();

        RuleFor(x => x.DueDate)
            .Must(dueDate => dueDate!.Value.ToUniversalTime() > timeProvider.GetUtcNow().UtcDateTime)
            .When(x => x.DueDate.HasValue)
            .WithMessage("Due date cannot be in the past.");
    }
}

public class UpdateTaskRequestValidator : AbstractValidator<UpdateTaskRequest>
{
    // No "past due date" rule here: an existing task may legitimately be overdue.
    public UpdateTaskRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(TaskItem.TitleMaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(TaskItem.DescriptionMaxLength);

        RuleFor(x => x.Priority)
            .IsInEnum();
    }
}

public class TaskQueryParametersValidator : AbstractValidator<TaskQueryParameters>
{
    public TaskQueryParametersValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
        RuleFor(x => x.Priority).IsInEnum();
        RuleFor(x => x.SortBy).IsInEnum();
        RuleFor(x => x.SortDirection).IsInEnum();

        RuleFor(x => x.Search)
            .MaximumLength(TaskQueryParameters.MaxSearchLength);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, TaskQueryParameters.MaxPageSize);
    }
}

public class UpdateTaskStatusRequestValidator : AbstractValidator<UpdateTaskStatusRequest>
{
    public UpdateTaskStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum();
    }
}
