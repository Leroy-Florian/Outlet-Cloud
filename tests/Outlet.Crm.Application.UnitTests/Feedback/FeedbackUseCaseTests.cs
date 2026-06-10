using Outlet.Crm.Application.Feedback;
using Outlet.Crm.Application.UnitTests.Fakes;
using Outlet.Crm.Domain.Feedback;
using Outlet.Crm.Domain.Products;

namespace Outlet.Crm.Application.UnitTests.Feedback;

public sealed class FeedbackUseCaseTests
{
    private static readonly DateTime Now = new(2026, 6, 9, 12, 0, 0, DateTimeKind.Utc);

    private readonly FakeFeedbackRepository _feedback = new();
    private readonly FakeProductRepository _products = new();
    private readonly FixedClock _clock = new(Now);

    private Product AddProduct()
    {
        var product = Product.Create("Outlet", null, Now).Value!;
        _products.Items.Add(product);
        return product;
    }

    private Domain.Feedback.Feedback AddFeedback(ProductId productId, string message = "It crashes") =>
        AddFeedback(productId, FeedbackCategory.Bug, message, Now);

    private Domain.Feedback.Feedback AddFeedback(ProductId productId, FeedbackCategory category, string message, DateTime receivedAt)
    {
        var feedback = Domain.Feedback.Feedback.Create(productId, category, message, null, null, receivedAt).Value!;
        _feedback.Items.Add(feedback);
        return feedback;
    }

    [Fact]
    public async Task Should_StoreNewFeedback_When_ProductExists()
    {
        var product = AddProduct();
        var useCase = new SubmitFeedbackUseCase(_feedback, _products, _clock);

        var result = await useCase.HandleAsync(new SubmitFeedbackCommand(
            product.Id.Value, FeedbackCategory.Bug, "It crashes", "ada@example.com", "outlet-cli@1.2.0"));

        result.IsSuccess.Should().BeTrue();
        var stored = _feedback.Items.Single();
        stored.Id.Should().Be(result.Value);
        stored.ProductId.Should().Be(product.Id);
        stored.Status.Should().Be(FeedbackStatus.New);
        stored.ReporterEmail!.Value.Should().Be("ada@example.com");
        stored.SourceApp.Should().Be("outlet-cli@1.2.0");
        stored.ReceivedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Should_Fail_When_SubmittingForUnknownProduct()
    {
        var useCase = new SubmitFeedbackUseCase(_feedback, _products, _clock);
        var productId = Guid.NewGuid();

        var result = await useCase.HandleAsync(new SubmitFeedbackCommand(
            productId, FeedbackCategory.Bug, "It crashes", null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NotFound(new ProductId(productId)));
        _feedback.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_ReporterEmailIsInvalid()
    {
        var product = AddProduct();
        var useCase = new SubmitFeedbackUseCase(_feedback, _products, _clock);

        var result = await useCase.HandleAsync(new SubmitFeedbackCommand(
            product.Id.Value, FeedbackCategory.Bug, "It crashes", "not-an-email", null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().StartWith("Email.Invalid:");
        _feedback.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Fail_When_MessageIsBlank()
    {
        var product = AddProduct();
        var useCase = new SubmitFeedbackUseCase(_feedback, _products, _clock);

        var result = await useCase.HandleAsync(new SubmitFeedbackCommand(
            product.Id.Value, FeedbackCategory.Bug, "   ", null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedbackErrors.MessageRequired);
        _feedback.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_TriageFeedback_When_ItIsNew()
    {
        var feedback = AddFeedback(ProductId.New());
        var useCase = new TriageFeedbackUseCase(_feedback);

        var result = await useCase.HandleAsync(new TriageFeedbackCommand(feedback.Id));

        result.IsSuccess.Should().BeTrue();
        feedback.Status.Should().Be(FeedbackStatus.Triaged);
        _feedback.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_When_TriagingUnknownFeedback()
    {
        var useCase = new TriageFeedbackUseCase(_feedback);
        var id = FeedbackId.New();

        var result = await useCase.HandleAsync(new TriageFeedbackCommand(id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedbackErrors.NotFound(id));
    }

    [Fact]
    public async Task Should_NotPersist_When_TriageTransitionIsInvalid()
    {
        var feedback = AddFeedback(ProductId.New());
        feedback.Resolve();
        var useCase = new TriageFeedbackUseCase(_feedback);

        var result = await useCase.HandleAsync(new TriageFeedbackCommand(feedback.Id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedbackErrors.InvalidTransition(FeedbackStatus.Resolved, FeedbackStatus.Triaged));
        _feedback.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_ResolveFeedback_When_ItIsTriaged()
    {
        var feedback = AddFeedback(ProductId.New());
        feedback.Triage();
        var useCase = new ResolveFeedbackUseCase(_feedback);

        var result = await useCase.HandleAsync(new ResolveFeedbackCommand(feedback.Id));

        result.IsSuccess.Should().BeTrue();
        feedback.Status.Should().Be(FeedbackStatus.Resolved);
        _feedback.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_When_ResolvingUnknownFeedback()
    {
        var useCase = new ResolveFeedbackUseCase(_feedback);
        var id = FeedbackId.New();

        var result = await useCase.HandleAsync(new ResolveFeedbackCommand(id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedbackErrors.NotFound(id));
    }

    [Fact]
    public async Task Should_NotPersist_When_ResolveTransitionIsInvalid()
    {
        var feedback = AddFeedback(ProductId.New());
        feedback.Dismiss();
        var useCase = new ResolveFeedbackUseCase(_feedback);

        var result = await useCase.HandleAsync(new ResolveFeedbackCommand(feedback.Id));

        result.IsFailure.Should().BeTrue();
        _feedback.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_DismissFeedback_When_ItIsNew()
    {
        var feedback = AddFeedback(ProductId.New());
        var useCase = new DismissFeedbackUseCase(_feedback);

        var result = await useCase.HandleAsync(new DismissFeedbackCommand(feedback.Id));

        result.IsSuccess.Should().BeTrue();
        feedback.Status.Should().Be(FeedbackStatus.Dismissed);
        _feedback.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_When_DismissingUnknownFeedback()
    {
        var useCase = new DismissFeedbackUseCase(_feedback);
        var id = FeedbackId.New();

        var result = await useCase.HandleAsync(new DismissFeedbackCommand(id));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(FeedbackErrors.NotFound(id));
    }

    [Fact]
    public async Task Should_NotPersist_When_DismissTransitionIsInvalid()
    {
        var feedback = AddFeedback(ProductId.New());
        feedback.Resolve();
        var useCase = new DismissFeedbackUseCase(_feedback);

        var result = await useCase.HandleAsync(new DismissFeedbackCommand(feedback.Id));

        result.IsFailure.Should().BeTrue();
        _feedback.UpdateCount.Should().Be(0);
    }

    [Fact]
    public async Task Should_ReturnNewestFirstWithCounts_When_InboxIsUnfiltered()
    {
        var product = AddProduct();
        var older = AddFeedback(product.Id, FeedbackCategory.Bug, "older", Now.AddHours(-2));
        var newest = AddFeedback(product.Id, FeedbackCategory.Question, "newest", Now);
        var middle = AddFeedback(product.Id, FeedbackCategory.Bug, "middle", Now.AddHours(-1));
        middle.Triage();
        older.Resolve();
        var useCase = new GetFeedbackInboxUseCase(_feedback, _products);

        var result = await useCase.HandleAsync(new GetFeedbackInboxQuery(null, null, null));

        result.IsSuccess.Should().BeTrue();
        var inbox = result.Value!;
        inbox.Items.Should().Equal(newest, middle, older);
        inbox.Counts.Should().Be(new FeedbackStatusCounts(1, 1, 1, 0));
        inbox.Counts.Total.Should().Be(3);
    }

    [Fact]
    public async Task Should_FilterByProduct_When_ProductIdIsProvided()
    {
        var product = AddProduct();
        var other = AddProduct();
        var mine = AddFeedback(product.Id);
        AddFeedback(other.Id);
        var useCase = new GetFeedbackInboxUseCase(_feedback, _products);

        var result = await useCase.HandleAsync(new GetFeedbackInboxQuery(product.Id.Value, null, null));

        result.Value!.Items.Should().Equal(mine);
        result.Value!.Counts.New.Should().Be(1);
    }

    [Fact]
    public async Task Should_Fail_When_InboxProductIsUnknown()
    {
        var productId = Guid.NewGuid();
        var useCase = new GetFeedbackInboxUseCase(_feedback, _products);

        var result = await useCase.HandleAsync(new GetFeedbackInboxQuery(productId, null, null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.NotFound(new ProductId(productId)));
    }

    [Fact]
    public async Task Should_KeepStatusCountsUnfiltered_When_FilteringByStatus()
    {
        var product = AddProduct();
        var triaged = AddFeedback(product.Id, FeedbackCategory.Bug, "triaged", Now.AddHours(-1));
        triaged.Triage();
        AddFeedback(product.Id, FeedbackCategory.Bug, "new", Now);
        var useCase = new GetFeedbackInboxUseCase(_feedback, _products);

        var result = await useCase.HandleAsync(new GetFeedbackInboxQuery(null, FeedbackStatus.Triaged, null));

        result.Value!.Items.Should().Equal(triaged);
        result.Value!.Counts.Should().Be(new FeedbackStatusCounts(1, 1, 0, 0));
    }

    [Fact]
    public async Task Should_ApplyCategoryFilterToCounts_When_FilteringByCategory()
    {
        var product = AddProduct();
        var bug = AddFeedback(product.Id, FeedbackCategory.Bug, "bug", Now);
        AddFeedback(product.Id, FeedbackCategory.Question, "question", Now.AddHours(-1));
        var useCase = new GetFeedbackInboxUseCase(_feedback, _products);

        var result = await useCase.HandleAsync(new GetFeedbackInboxQuery(null, null, FeedbackCategory.Bug));

        result.Value!.Items.Should().Equal(bug);
        result.Value!.Counts.Should().Be(new FeedbackStatusCounts(1, 0, 0, 0));
    }
}
