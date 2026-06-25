using FluentValidation;
using PeopleGrid.Application.Features.Approvals.DTOs;

namespace PeopleGrid.Application.Features.Approvals.Validators;

public sealed class CreateApprovalFlowRequestValidator : AbstractValidator<CreateApprovalFlowRequest>
{
    public CreateApprovalFlowRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Steps).NotEmpty().WithMessage("Approval flow must have at least one active step.");
        RuleForEach(x => x.Steps).SetValidator(new ApprovalFlowStepRequestValidator());
        RuleFor(x => x.Steps).Must(x => x.Select(s => s.Sequence).Distinct().Count() == x.Count).WithMessage("Step sequence must be unique within a flow.");
    }
}

public sealed class UpdateApprovalFlowRequestValidator : AbstractValidator<UpdateApprovalFlowRequest>
{
    public UpdateApprovalFlowRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Steps).NotEmpty().WithMessage("Approval flow must have at least one active step.");
        RuleForEach(x => x.Steps).SetValidator(new ApprovalFlowStepRequestValidator());
        RuleFor(x => x.Steps).Must(x => x.Select(s => s.Sequence).Distinct().Count() == x.Count).WithMessage("Step sequence must be unique within a flow.");
    }
}

public sealed class ApprovalFlowStepRequestValidator : AbstractValidator<ApprovalFlowStepRequest>
{
    public ApprovalFlowStepRequestValidator()
    {
        RuleFor(x => x.Sequence).GreaterThan(0);
        RuleFor(x => x.ApproverType).NotEmpty().Must(x => new[] { "Role", "User", "LineManager", "HR" }.Contains(x)).WithMessage("Approver type is invalid.");
        RuleFor(x => x.SlaHours).GreaterThan(0).When(x => x.SlaHours is not null);
    }
}

public sealed class ApprovalDecisionRequestValidator : AbstractValidator<ApprovalDecisionRequest>
{
    public ApprovalDecisionRequestValidator()
    {
        RuleFor(x => x.Comments).MaximumLength(1000);
    }
}

public sealed class EscalateApprovalRequestValidator : AbstractValidator<EscalateApprovalRequest>
{
    public EscalateApprovalRequestValidator()
    {
        RuleFor(x => x.EscalatedToUserId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}
