using FluentAssertions;
using Moq;
using TIKR.Infrastructure.Services;
using TIKR.Shared.DTOs;
using TIKR.Shared.Entities;
using TIKR.Shared.Enums;
using TIKR.Shared.Helpers;
using TIKR.Shared.Interfaces;

namespace TIKR.Infrastructure.Tests.Services;

public class EmailStructuredApplyServiceTests
{
    [Fact]
    public async Task ApplyAsync_ParseFailure_RecordsNoticeAndSkipsContacts()
    {
        var contacts = new Mock<IContactService>();
        var notices = new List<EmailExtractionNoticeDto>();
        var store = new Mock<IEmailIngestionNoticeStore>();
        store.Setup(s => s.Add(It.IsAny<EmailExtractionNoticeDto>()))
            .Callback<EmailExtractionNoticeDto>(notices.Add);
        var sut = new EmailStructuredApplyService(
            contacts.Object,
            Mock.Of<IKnowledgeService>(),
            Mock.Of<IHybridAiService>(),
            store.Object);

        var notice = await sut.ApplyAsync(
            EmailStructuredExtractResult.Failed("bad mime"),
            "clerk.eml",
            null,
            Mock.Of<IAuditService>(),
            Mock.Of<ICurrentUserService>());

        notice.ParseSucceeded.Should().BeFalse();
        notice.Message.Should().Contain("bad mime");
        notices.Should().ContainSingle();
        contacts.Verify(c => c.UpsertAsync(
            It.IsAny<CreateContactRequest>(),
            It.IsAny<IAuditService>(),
            It.IsAny<ICurrentUserService>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApplyAsync_Contact_UpsertsAndCreatesKnowledge()
    {
        var contacts = new Mock<IContactService>();
        contacts.Setup(c => c.UpsertAsync(
                It.IsAny<CreateContactRequest>(),
                It.IsAny<IAuditService>(),
                It.IsAny<ICurrentUserService>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new Contact { Id = Guid.NewGuid(), Name = "Ada" }, true));
        var knowledge = new Mock<IKnowledgeService>();
        knowledge.Setup(k => k.CreateAsync(
                It.IsAny<CreateKnowledgeEntryRequest>(),
                It.IsAny<IAuditService>(),
                It.IsAny<IHybridAiService>(),
                It.IsAny<ICurrentUserService>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new KnowledgeEntry
            {
                Id = Guid.NewGuid(),
                Title = "Email: Hello",
                Content = "note",
                Category = KnowledgeCategory.Contact
            });
        var sut = new EmailStructuredApplyService(
            contacts.Object,
            knowledge.Object,
            Mock.Of<IHybridAiService>(),
            Mock.Of<IEmailIngestionNoticeStore>());
        var extract = new EmailStructuredExtractResult(
            true,
            null,
            "ada@example.gov",
            null,
            "Hello",
            null,
            "body",
            false,
            [new ExtractedEmailContact("Ada", null, null, "ada@example.gov", null, null, ContactCategory.Custom)],
            null,
            null);

        var notice = await sut.ApplyAsync(
            extract,
            "hello.eml",
            null,
            Mock.Of<IAuditService>(),
            Mock.Of<ICurrentUserService>());

        notice.ParseSucceeded.Should().BeTrue();
        notice.ContactsCreated.Should().Be(1);
        notice.KnowledgeEntryId.Should().NotBeNull();
        contacts.VerifyAll();
        knowledge.VerifyAll();
    }
}
