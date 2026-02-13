using CopilotHub.Core.Models;
using CopilotHub.Core.Services;
using FluentAssertions;

namespace CopilotHub.Tests;

public class SessionManagerTests
{
    private readonly SessionManager _sut = new();

    [Fact]
    public void CreateSession_ShouldAddSessionToList()
    {
        var session = _sut.CreateSession(@"C:\temp", "Test");

        _sut.Sessions.Should().ContainSingle();
        session.Name.Should().Be("Test");
        session.WorkingDirectory.Should().Be(@"C:\temp");
        session.Status.Should().Be(SessionStatus.Running);
    }

    [Fact]
    public void CreateSession_WithEmptyName_ShouldAutoName()
    {
        var session = _sut.CreateSession(@"C:\temp");
        session.Name.Should().StartWith("Session ");
    }

    [Fact]
    public void CreateMultipleSessions_ShouldTrackAll()
    {
        _sut.CreateSession(@"C:\temp1", "S1");
        _sut.CreateSession(@"C:\temp2", "S2");
        _sut.CreateSession(@"C:\temp3", "S3");

        _sut.Sessions.Should().HaveCount(3);
    }

    [Fact]
    public void GetSession_ExistingId_ShouldReturnSession()
    {
        var session = _sut.CreateSession(@"C:\temp", "Test");

        var found = _sut.GetSession(session.Id);

        found.Should().NotBeNull();
        found!.Id.Should().Be(session.Id);
    }

    [Fact]
    public void GetSession_NonExistingId_ShouldReturnNull()
    {
        _sut.GetSession(Guid.NewGuid()).Should().BeNull();
    }

    [Fact]
    public void RemoveSession_ShouldRemoveFromList()
    {
        var session = _sut.CreateSession(@"C:\temp", "Test");
        _sut.RemoveSession(session.Id);

        _sut.Sessions.Should().BeEmpty();
    }

    [Fact]
    public void CompleteSession_ShouldUpdateStatus()
    {
        var session = _sut.CreateSession(@"C:\temp", "Test");

        _sut.CompleteSession(session.Id, SessionStatus.Completed);

        session.Status.Should().Be(SessionStatus.Completed);
    }

    [Fact]
    public void CompleteSession_ShouldRaiseEvent()
    {
        var session = _sut.CreateSession(@"C:\temp", "Test");
        var eventRaised = false;
        _sut.SessionCompleted += (_, args) =>
        {
            eventRaised = true;
            args.Session.Id.Should().Be(session.Id);
            args.FinalStatus.Should().Be(SessionStatus.Failed);
        };

        _sut.CompleteSession(session.Id, SessionStatus.Failed);

        eventRaised.Should().BeTrue();
    }

    [Fact]
    public void CompleteSession_NonExisting_ShouldNotThrow()
    {
        var act = () => _sut.CompleteSession(Guid.NewGuid(), SessionStatus.Completed);
        act.Should().NotThrow();
    }
}
