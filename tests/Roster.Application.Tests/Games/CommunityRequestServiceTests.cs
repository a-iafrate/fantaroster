using NSubstitute;
using Roster.Application.Ports;
using Roster.Application.Ports.Data;
using Roster.Application.Services;
using Roster.Domain.Organizers;
using Shouldly;
using Xunit;

namespace Roster.Application.Tests.Games;

public class CommunityRequestServiceTests
{
    private readonly ICommunityRequestRepository _requests = Substitute.For<ICommunityRequestRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICommunityNotifier _notifier = Substitute.For<ICommunityNotifier>();
    private readonly CommunityRequestService _service;

    public CommunityRequestServiceTests()
    {
        _service = new CommunityRequestService(_requests, _unitOfWork, _notifier, TimeProvider.System);
    }

    private static CommunityRequest Pending() => new(Guid.NewGuid(), "org-1", "org@example.test", "A user group of developers", null, DateTimeOffset.UnixEpoch);

    [Fact]
    public async Task SubmitAsync_NewOrganizer_CreatesAPendingRequest()
    {
        var outcome = await _service.SubmitAsync("org-1", "org@example.test", "A user group of developers", "https://example.test");

        outcome.ShouldBe(CommunityRequestOutcome.Created);
        _requests.Received(1).Add(Arg.Is<CommunityRequest>(r => r.OrganizerId == "org-1" && r.Status == CommunityRequestStatus.Pending));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitAsync_WhenARequestIsPending_DoesNotCreateAnother()
    {
        _requests.GetLatestByOrganizerAsync("org-1", Arg.Any<CancellationToken>()).Returns(Pending());

        (await _service.SubmitAsync("org-1", "org@example.test", "Another description here", null)).ShouldBe(CommunityRequestOutcome.AlreadyPending);

        _requests.DidNotReceive().Add(Arg.Any<CommunityRequest>());
    }

    [Fact]
    public async Task SubmitAsync_WhenAlreadyACommunity_ReportsIt()
    {
        _requests.IsApprovedAsync("org-1", Arg.Any<CancellationToken>()).Returns(true);

        (await _service.SubmitAsync("org-1", "org@example.test", "Another description here", null)).ShouldBe(CommunityRequestOutcome.AlreadyCommunity);
    }

    [Fact]
    public async Task SubmitAsync_AfterARejection_AllowsANewRequest()
    {
        var rejected = Pending();
        rejected.Reject("admin@example.test", null, DateTimeOffset.UnixEpoch);
        _requests.GetLatestByOrganizerAsync("org-1", Arg.Any<CancellationToken>()).Returns(rejected);

        (await _service.SubmitAsync("org-1", "org@example.test", "A better description now", null)).ShouldBe(CommunityRequestOutcome.Created);
    }

    [Theory]
    [InlineData(true, CommunityRequestStatus.Approved)]
    [InlineData(false, CommunityRequestStatus.Rejected)]
    public async Task DecideAsync_SavesTheDecisionAndEmailsTheOrganizer(bool approve, CommunityRequestStatus expected)
    {
        var request = Pending();
        _requests.GetByIdAsync(request.Id, Arg.Any<CancellationToken>()).Returns(request);

        (await _service.DecideAsync(request.Id, approve, "A note", "admin@example.test")).ShouldBeTrue();

        request.Status.ShouldBe(expected);
        request.DecisionNote.ShouldBe("A note");
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notifier.Received(1).NotifyDecisionAsync(request, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DecideAsync_AlreadyDecidedOrUnknown_DoesNothing()
    {
        var decided = Pending();
        decided.Approve("admin@example.test", null, DateTimeOffset.UnixEpoch);
        _requests.GetByIdAsync(decided.Id, Arg.Any<CancellationToken>()).Returns(decided);

        (await _service.DecideAsync(decided.Id, false, null, "admin@example.test")).ShouldBeFalse();
        (await _service.DecideAsync(Guid.NewGuid(), true, null, "admin@example.test")).ShouldBeFalse();

        await _notifier.DidNotReceive().NotifyDecisionAsync(Arg.Any<CommunityRequest>(), Arg.Any<CancellationToken>());
    }
}
