using Xunit;
using BoslaPlatform.Domain.Entities.Profile;
using BoslaPlatform.Domain.Enums;
using BoslaPlatform.Domain.Events.Specialists;
using System;
using System.Linq;
using System.Threading;

namespace Bosla.Unit.Tests;

public class SpecialistVerificationTests
{
    [Fact]
    public void Submit_SetsPendingStatus_AndDispatchesEvent()
    {
        var verification = new SpecialistVerification { SpecialistId = Guid.NewGuid() };

        verification.Submit();

        Assert.Equal(VerificationStatus.Pending, verification.Status);
        Assert.True(verification.IsSubmitted);
        Assert.NotNull(verification.SubmittedAt);
        Assert.NotNull(verification.LastUpdatedAt);
        Assert.Single(verification.DomainEvents);
        Assert.IsType<SpecialistVerificationSubmittedEvent>(verification.DomainEvents.First());
    }

    [Fact]
    public void Approve_SetsApprovedStatus_AndDispatchesEvent()
    {
        var specialistId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var verification = new SpecialistVerification { SpecialistId = specialistId };
        verification.Submit();

        verification.Approve(adminId);

        Assert.Equal(VerificationStatus.Approved, verification.Status);
        Assert.Equal(adminId, verification.ReviewedBy);
        Assert.NotNull(verification.ReviewedAt);
        Assert.NotNull(verification.LastUpdatedAt);
        Assert.Equal(2, verification.DomainEvents.Count);
        Assert.IsType<SpecialistVerificationApprovedEvent>(verification.DomainEvents.Last());
    }

    [Fact]
    public void Reject_SetsRejectedStatus_StoresNotes_AndDispatchesEvent()
    {
        var specialistId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var notes = "Please upload a clearer image of your national ID.";
        var verification = new SpecialistVerification { SpecialistId = specialistId };
        verification.Submit();

        verification.Reject(adminId, notes);

        Assert.Equal(VerificationStatus.Rejected, verification.Status);
        Assert.Equal(adminId, verification.ReviewedBy);
        Assert.NotNull(verification.ReviewedAt);
        Assert.Equal(notes, verification.AdminNotes);
        Assert.NotNull(verification.LastUpdatedAt);
        Assert.Equal(2, verification.DomainEvents.Count);
        Assert.IsType<SpecialistVerificationRejectedEvent>(verification.DomainEvents.Last());
    }

    [Fact]
    public void Reject_WithNullNotes_DoesNotStoreNotes()
    {
        var verification = new SpecialistVerification { SpecialistId = Guid.NewGuid() };
        verification.Submit();

        verification.Reject(Guid.NewGuid(), null);

        Assert.Equal(VerificationStatus.Rejected, verification.Status);
        Assert.Null(verification.AdminNotes);
    }

    [Fact]
    public void Submit_DoesNotOverwriteExistingSubmittedAt()
    {
        var verification = new SpecialistVerification { SpecialistId = Guid.NewGuid() };
        verification.Submit();
        var originalSubmittedAt = verification.SubmittedAt;

        Thread.Sleep(10);
        verification.Submit();

        Assert.Equal(originalSubmittedAt, verification.SubmittedAt);
    }

    [Fact]
    public void Approve_WithoutPriorSubmit_StillWorks()
    {
        var verification = new SpecialistVerification { SpecialistId = Guid.NewGuid() };

        verification.Approve(Guid.NewGuid());

        Assert.Equal(VerificationStatus.Approved, verification.Status);
        Assert.NotNull(verification.ReviewedAt);
        Assert.Single(verification.DomainEvents);
        Assert.IsType<SpecialistVerificationApprovedEvent>(verification.DomainEvents.First());
    }

    [Fact]
    public void Reject_WithoutPriorSubmit_StillWorks()
    {
        var verification = new SpecialistVerification { SpecialistId = Guid.NewGuid() };

        verification.Reject(Guid.NewGuid(), "Invalid documents");

        Assert.Equal(VerificationStatus.Rejected, verification.Status);
        Assert.Equal("Invalid documents", verification.AdminNotes);
        Assert.Single(verification.DomainEvents);
        Assert.IsType<SpecialistVerificationRejectedEvent>(verification.DomainEvents.First());
    }
}
