using Outlet.Crm.Domain.Feedback;
using Outlet.Crm.Domain.Products;
using Outlet.Crm.Domain.Prospects;

namespace Outlet.Crm.Domain.UnitTests.Feedback;

public sealed class FeedbackTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private static Domain.Feedback.Feedback CreateFeedback(FeedbackCategory category = FeedbackCategory.Bug) =>
        Domain.Feedback.Feedback.Create(ProductId.New(), category, "It crashes", null, "outlet-cli@1.2.0", Now).Value!;

    [Fact]
    public void Should_StartAsNew_When_Created()
    {
        var productId = ProductId.New();
        var email = Email.Create("ada@example.com").Value!;

        var feedback = Domain.Feedback.Feedback.Create(
            productId, FeedbackCategory.FeatureRequest, "  Add dark mode  ", email, "  website  ", Now).Value!;

        feedback.Status.Should().Be(FeedbackStatus.New);
        feedback.ProductId.Should().Be(productId);
        feedback.Category.Should().Be(FeedbackCategory.FeatureRequest);
        feedback.Message.Should().Be("Add dark mode");
        feedback.ReporterEmail.Should().Be(email);
        feedback.SourceApp.Should().Be("website");
        feedback.ReceivedAt.Should().Be(Now);
    }

    [Fact]
    public void Should_RaiseReceivedEvent_When_Created()
    {
        var feedback = CreateFeedback(FeedbackCategory.Question);

        var received = feedback.DomainEvents.OfType<FeedbackReceivedEvent>().Single();
        received.FeedbackId.Should().Be(feedback.Id);
        received.ProductId.Should().Be(feedback.ProductId);
        received.Category.Should().Be(FeedbackCategory.Question);
    }

    [Fact]
    public void Should_NormalizeBlankSourceApp_When_Created()
    {
        var feedback = Domain.Feedback.Feedback.Create(
            ProductId.New(), FeedbackCategory.Other, "Hello", null, "   ", Now).Value!;

        feedback.SourceApp.Should().BeNull();
        feedback.ReporterEmail.Should().BeNull();
    }

    [Fact]
    public void Should_HaveNullScore_When_CreatedWithoutScore()
    {
        var feedback = CreateFeedback();

        feedback.Score.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(10)]
    public void Should_KeepScore_When_ScoreIsWithinNpsScale(int score)
    {
        var feedback = Domain.Feedback.Feedback.Create(
            ProductId.New(), FeedbackCategory.Other, "Hello", null, null, score, Now).Value!;

        feedback.Score.Should().Be(score);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void Should_Fail_When_ScoreIsOutOfRange(int score)
    {
        var result = Domain.Feedback.Feedback.Create(
            ProductId.New(), FeedbackCategory.Other, "Hello", null, null, score, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedbackErrors.ScoreOutOfRange);
    }

    [Fact]
    public void Should_Fail_When_MessageIsBlank()
    {
        var result = Domain.Feedback.Feedback.Create(ProductId.New(), FeedbackCategory.Bug, "   ", null, null, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedbackErrors.MessageRequired);
    }

    [Fact]
    public void Should_AcceptMessageOf4000Characters_When_Created()
    {
        var result = Domain.Feedback.Feedback.Create(
            ProductId.New(), FeedbackCategory.Bug, new string('x', 4000), null, null, Now);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Should_Fail_When_TrimmedMessageExceeds4000Characters()
    {
        var result = Domain.Feedback.Feedback.Create(
            ProductId.New(), FeedbackCategory.Bug, new string('x', 4001), null, null, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedbackErrors.MessageTooLong);
    }

    [Fact]
    public void Should_MoveToTriaged_When_TriagingANewFeedback()
    {
        var feedback = CreateFeedback();

        var result = feedback.Triage();

        result.IsSuccess.Should().BeTrue();
        feedback.Status.Should().Be(FeedbackStatus.Triaged);
        feedback.DomainEvents.OfType<FeedbackTriagedEvent>().Single().FeedbackId.Should().Be(feedback.Id);
    }

    [Fact]
    public void Should_Fail_When_TriagingATriagedFeedback()
    {
        var feedback = CreateFeedback();
        feedback.Triage();

        var result = feedback.Triage();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Feedback.InvalidTransition: A Triaged feedback cannot move to Triaged.");
        feedback.Status.Should().Be(FeedbackStatus.Triaged);
    }

    [Fact]
    public void Should_ResolveDirectly_When_FeedbackIsNew()
    {
        var feedback = CreateFeedback();

        var result = feedback.Resolve();

        result.IsSuccess.Should().BeTrue();
        feedback.Status.Should().Be(FeedbackStatus.Resolved);
        feedback.DomainEvents.OfType<FeedbackResolvedEvent>().Single().FeedbackId.Should().Be(feedback.Id);
    }

    [Fact]
    public void Should_Resolve_When_FeedbackIsTriaged()
    {
        var feedback = CreateFeedback();
        feedback.Triage();

        feedback.Resolve().IsSuccess.Should().BeTrue();
        feedback.Status.Should().Be(FeedbackStatus.Resolved);
    }

    [Fact]
    public void Should_Fail_When_ResolvingAResolvedFeedback()
    {
        var feedback = CreateFeedback();
        feedback.Resolve();

        var result = feedback.Resolve();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedbackErrors.InvalidTransition(FeedbackStatus.Resolved, FeedbackStatus.Resolved));
    }

    [Fact]
    public void Should_Fail_When_TriagingAResolvedFeedback()
    {
        var feedback = CreateFeedback();
        feedback.Resolve();

        var result = feedback.Triage();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedbackErrors.InvalidTransition(FeedbackStatus.Resolved, FeedbackStatus.Triaged));
        feedback.Status.Should().Be(FeedbackStatus.Resolved);
    }

    [Fact]
    public void Should_Dismiss_When_FeedbackIsNew()
    {
        var feedback = CreateFeedback();

        var result = feedback.Dismiss();

        result.IsSuccess.Should().BeTrue();
        feedback.Status.Should().Be(FeedbackStatus.Dismissed);
        feedback.DomainEvents.OfType<FeedbackDismissedEvent>().Single().FeedbackId.Should().Be(feedback.Id);
    }

    [Fact]
    public void Should_Dismiss_When_FeedbackIsTriaged()
    {
        var feedback = CreateFeedback();
        feedback.Triage();

        feedback.Dismiss().IsSuccess.Should().BeTrue();
        feedback.Status.Should().Be(FeedbackStatus.Dismissed);
    }

    [Fact]
    public void Should_Fail_When_DismissingADismissedFeedback()
    {
        var feedback = CreateFeedback();
        feedback.Dismiss();

        var result = feedback.Dismiss();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedbackErrors.InvalidTransition(FeedbackStatus.Dismissed, FeedbackStatus.Dismissed));
    }

    [Fact]
    public void Should_Fail_When_ResolvingADismissedFeedback()
    {
        var feedback = CreateFeedback();
        feedback.Dismiss();

        feedback.Resolve().IsFailure.Should().BeTrue();
        feedback.Status.Should().Be(FeedbackStatus.Dismissed);
    }

    [Fact]
    public void Should_EmbedId_When_FormattingNotFoundError()
    {
        var id = FeedbackId.New();

        FeedbackErrors.NotFound(id).Should().Be($"Feedback.NotFound: Feedback '{id.Value}' was not found.");
    }

    [Fact]
    public void Should_GenerateDistinctIds_When_CallingNew()
    {
        FeedbackId.New().Should().NotBe(FeedbackId.New());
    }
}
