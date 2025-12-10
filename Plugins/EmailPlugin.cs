// Import packages
using CODE.Presents.SemanticKernel.Agents.Models;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace CODE.Presents.SemanticKernel.Agents.Plugins;

public class EmailPlugin
{
    // Mock data for the emails
    private readonly List<EmailModel> emails =
    [
      new EmailModel
      {
          Id = Guid.NewGuid(),
          Sender = "alice.johnson@example.com",
          Subject = "Kickoff Meeting – AI Agent for Contextual Chat & User Memory",
          Body = """
            We are launching a new initiative to develop an AI Agent platform for contextual chat applications. 
            The project aims to deliver a scalable, multi-user chat assistant with persistent memory, 
            user context isolation, and support for Retrieval-Augmented Generation (RAG). 
            
            The solution will leverage Azure Cosmos DB for chat history, user context, and semantic search, 
            ensuring low latency and elastic scaling.

            Agenda:
            1. Welcome & Introductions (10 min)
            2. Project Goals & Success Criteria (15 min)
               - Overview of AI Agent capabilities
               - Key use cases: chat history, contextual retrieval, user isolation
            3. Technical Architecture (20 min)
               - Azure Cosmos DB data modeling
               - Partition key strategy for user/tenant isolation
               - Vector search for semantic retrieval
            4. Development Plan & Milestones (15 min)
               - SDKs, tools, and best practices
               - Timeline and deliverables
            5. Roles & Responsibilities (10 min)
               - Team assignments
               - Collaboration expectations
            6. Q&A / Open Discussion (20 min)
            7. Next Steps & Action Items (10 min)
            
            Please RSVP and review the attached project brief before the meeting.

            Best regards,
            Alice Johnson
            Project Manager
          """,
          IsRead = false,
          ReceivedAt = DateTime.UtcNow.AddDays(-2)
      },
      new EmailModel
      {
          Id = Guid.NewGuid(),
          Sender = "alex.chen@example.com",
          Subject = "Request for Feedback: Q4 Product Roadmap & Feature Prioritization",
          Body = """
            Dear Team,

            As we approach the final quarter of 2025, we are reviewing our product roadmap to ensure alignment with customer needs and business objectives. Our focus remains on delivering scalable solutions for enterprise clients, with particular emphasis on AI-powered analytics, enhanced security features, and improved integration with Azure services.

            Key Points for Review:

            - Proposed timeline for new feature releases, including contextual chat enhancements and Cosmos DB integration.
            - Prioritization of customer-requested features versus internal innovation initiatives.
            - Resource allocation for cross-functional teams and anticipated impact on delivery schedules.
            
            Action Required:
            Please review the attached roadmap document and provide your feedback by November 28. Specifically, highlight any concerns regarding feature prioritization, dependencies, or resource constraints.

            Your input is critical to ensuring our roadmap reflects both market demands and our strategic goals.

            Thank you for your collaboration.

            Best regards,
            Alex Chen
            Product Manager
          """,
          IsRead = false,
          ReceivedAt = DateTime.UtcNow.AddDays(-1)
      },
      new EmailModel
      {
          Id = Guid.NewGuid(),
          Sender = "maria.lopez@example.com",
          Subject = "Action Required: Vendor Assessment for New Cloud Data Platform",
          Body = """
            Dear Colleagues,

            As part of our ongoing digital transformation initiative, we are evaluating vendors for a new cloud-based data platform to support scalable analytics and AI workloads. The goal is to select a solution that offers global distribution, elastic scaling, and robust security for our enterprise data needs.

            Key Points for Consideration:

            Review attached vendor shortlist and feature comparison matrix.
            Assess compatibility with existing Azure infrastructure and integration requirements.
            Identify potential risks and mitigation strategies for migration.

            Next Steps:
            Please provide your feedback and any additional requirements by December 2, 2025. We will consolidate input and schedule a follow-up meeting to finalize our selection criteria.

            Thank you for your attention and collaboration.

            Best regards,
            Maria Lopez
            IT Strategy Lead
          """,
          IsRead = true,
          ReceivedAt = DateTime.UtcNow.AddHours(-3)
      }
    ];

    [KernelFunction("get_emails")]
    [Description("Gets a list of all emails")]
    public async Task<List<EmailModel>> GetEmailsAsync()
    {
        return emails;
    }

    [KernelFunction("get_unread_emails")]
    [Description("Gets a list of **unread** emails")]
    public async Task<List<EmailModel>> GetUnreadEmailsAsync()
    {
        return emails.Where(email => email.IsRead == false).ToList();
    }

    [KernelFunction("get_email_by_id")]
    [Description("Gets an email by its ID")]
    public async Task<EmailModel?> GetEmailByIdAsync([Description("The GUID type ID of the email to retrieve")] Guid id)
    {
        return emails.FirstOrDefault(email => email.Id == id);
    }

    [KernelFunction("mark_email_as_read")]
    [Description("Marks an email as read using its ID")]
    public async Task<EmailModel?> MarkAsReadAsync([Description("The GUID type ID of the email to mark as read")] Guid id)
    {
        var email = emails.FirstOrDefault(email => email.Id == id);

        if (email == null)
        {
            return null;
        }

        email.IsRead = true;

        return email;
    }

    [KernelFunction("count_emails")]
    [Description("Counts the total number of emails")]
    public async Task<int> CountEmailsAsync()
    {
        return emails.Count;
    }

    [KernelFunction("count_unread_emails")]
    [Description("Counts the number of unread emails")]
    public async Task<int> CountUnreadEmailsAsync()
    {
        return emails.Count(email => email.IsRead == false);
    }
}
