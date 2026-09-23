using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using CodeMonitor.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CodeMonitor.Services
{
    public class EmailService
    {
        public bool SendReport(AnalysisReport report, string? senderEmail, string? appPassword, string? recipientEmail, string smtpServer = "smtp.office365.com", int smtpPort = 587)
        {
            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(appPassword))
            {
                Console.WriteLine("[EmailService] OUTLOOK_SENDER_EMAIL or OUTLOOK_APP_PASSWORD is not configured. Skipping email dispatch (Dry Run).");
                return false;
            }

            string targetRecipient = !string.IsNullOrWhiteSpace(recipientEmail) 
                ? recipientEmail! 
                : (!string.IsNullOrWhiteSpace(report.AuthorEmail) ? report.AuthorEmail : senderEmail!);

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Code Quality Monitor Bot", senderEmail));
                message.To.Add(new MailboxAddress(report.AuthorName.Length > 0 ? report.AuthorName : "Developer", targetRecipient));

                string prLabel = !string.IsNullOrWhiteSpace(report.PullRequestNumber) ? $"PR #{report.PullRequestNumber}" : report.Branch;
                string subjectPrefix = report.IsPassed ? "✅ [Approved]" : "🚨 [Action Required]";
                message.Subject = $"{subjectPrefix} Code Quality Report – {report.Repository} ({prLabel})";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = GenerateHtmlBody(report)
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                var secureOption = smtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                
                Console.WriteLine($"[EmailService] Connecting to {smtpServer}:{smtpPort} via TLS...");
                client.Connect(smtpServer, smtpPort, secureOption);
                
                Console.WriteLine($"[EmailService] Authenticating sender {senderEmail}...");
                client.Authenticate(senderEmail, appPassword);
                
                Console.WriteLine($"[EmailService] Dispatching personalized alert to PR author: {targetRecipient}...");
                client.Send(message);
                client.Disconnect(true);

                Console.WriteLine($"[EmailService] ✅ Email report successfully delivered to {targetRecipient}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] ❌ Email dispatch failed: {ex.Message}");
                return false;
            }
        }

        private string GenerateHtmlBody(AnalysisReport report)
        {
            var sb = new StringBuilder();
            string primaryColor = report.IsPassed ? "#1a7f37" : "#cf222e";
            string lightBgColor = report.IsPassed ? "#dafbe1" : "#ffebe9";
            string borderColor = report.IsPassed ? "#4ac26b" : "#ff8182";
            string headerTitle = report.IsPassed ? "✅ Code Quality Passed" : "🚨 Action Required: Code Quality Issues";
            string headerSubtitle = report.IsPassed 
                ? $"Great news! All modified files in your Pull Request meet the quality standards."
                : $"We found {report.Violations.Count} issue(s) that need to be resolved before your PR can be merged.";

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset='utf-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: 'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, Helvetica, Arial, sans-serif; background-color: #f6f8fa; margin: 0; padding: 20px; color: #1f2328; line-height: 1.5; }");
            sb.AppendLine(".email-wrapper { max-width: 680px; margin: 0 auto; background: #ffffff; border-radius: 10px; overflow: hidden; border: 1px solid #d0d7de; box-shadow: 0 4px 14px rgba(0,0,0,0.06); }");
            sb.AppendLine(".hero-banner { background: " + primaryColor + "; padding: 26px 24px; color: #ffffff; text-align: center; }");
            sb.AppendLine(".hero-banner h1 { margin: 0; font-size: 22px; font-weight: 700; letter-spacing: -0.2px; }");
            sb.AppendLine(".hero-banner p { margin: 6px 0 0 0; font-size: 14px; opacity: 0.95; }");
            sb.AppendLine(".content { padding: 24px; }");
            sb.AppendLine(".btn-pr { display: inline-block; background: #0969da; color: #ffffff !important; padding: 10px 20px; border-radius: 6px; font-weight: 600; font-size: 14px; text-decoration: none; margin: 12px 0 16px 0; }");
            sb.AppendLine(".stat-grid { display: table; width: 100%; margin-bottom: 24px; border-spacing: 8px; border-collapse: separate; }");
            sb.AppendLine(".stat-card { display: table-cell; background: #f6f8fa; border: 1px solid #d0d7de; border-radius: 6px; padding: 12px 8px; text-align: center; width: 33.33%; }");
            sb.AppendLine(".stat-card .label { font-size: 11px; text-transform: uppercase; color: #656d76; font-weight: 600; letter-spacing: 0.5px; }");
            sb.AppendLine(".stat-card .value { font-size: 18px; font-weight: 700; margin-top: 2px; }");
            sb.AppendLine(".issue-card { background: #ffffff; border: 1px solid #d0d7de; border-left: 5px solid " + primaryColor + "; border-radius: 6px; padding: 18px; margin-bottom: 22px; }");
            sb.AppendLine(".badge-error { background: #ffebe9; color: #cf222e; border: 1px solid #ff8182; padding: 2px 8px; border-radius: 12px; font-size: 11px; font-weight: 700; }");
            sb.AppendLine(".badge-warning { background: #fff8c5; color: #9a6700; border: 1px solid #d4a72c; padding: 2px 8px; border-radius: 12px; font-size: 11px; font-weight: 700; }");
            sb.AppendLine(".issue-title { font-size: 16px; font-weight: 700; color: #0969da; margin: 6px 0 10px 0; }");
            sb.AppendLine(".section-label { font-size: 12px; font-weight: 700; text-transform: uppercase; color: #656d76; letter-spacing: 0.5px; margin-top: 10px; margin-bottom: 3px; }");
            sb.AppendLine(".action-box { background: #ddf4ff; border: 1px solid #54aeff; border-radius: 6px; padding: 12px 16px; margin: 12px 0; font-size: 13px; }");
            sb.AppendLine(".action-box ol { margin: 6px 0 0 0; padding-left: 20px; }");
            sb.AppendLine(".action-box li { margin-bottom: 4px; }");
            sb.AppendLine(".code-box { background: #24292f; color: #f0f6fc; border-radius: 6px; padding: 12px 14px; font-family: Consolas, 'Courier New', monospace; font-size: 12px; line-height: 1.45; overflow-x: auto; white-space: pre-wrap; margin-top: 8px; }");
            sb.AppendLine(".footer { background: #f6f8fa; border-top: 1px solid #d0d7de; padding: 16px; font-size: 12px; color: #656d76; text-align: center; }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<div class='email-wrapper'>");
            
            // Hero Banner
            sb.AppendLine($"<div class='hero-banner'>");
            sb.AppendLine($"<h1>{headerTitle}</h1>");
            sb.AppendLine($"<p>{headerSubtitle}</p>");
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='content'>");

            // PR Button
            if (!string.IsNullOrWhiteSpace(report.PullRequestUrl))
            {
                sb.AppendLine("<div style='text-align:center;'>");
                sb.AppendLine($"<a class='btn-pr' href='{report.PullRequestUrl}'>👉 View Pull Request on GitHub &rarr;</a>");
                sb.AppendLine("</div>");
            }

            // Quick Stats Grid
            sb.AppendLine("<div class='stat-grid'>");
            sb.AppendLine($"<div class='stat-card'><div class='label'>Repository</div><div class='value' style='font-size:14px;'>{Path.GetFileName(report.Repository)}</div></div>");
            sb.AppendLine($"<div class='stat-card'><div class='label'>Quality Status</div><div class='value' style='color:{primaryColor};'>{(report.IsPassed ? "PASSED" : "FAILED")}</div></div>");
            sb.AppendLine($"<div class='stat-card'><div class='label'>Violations</div><div class='value' style='color:{primaryColor};'>{report.Violations.Count}</div></div>");
            sb.AppendLine("</div>");

            // Overview details
            sb.AppendLine("<div style='font-size:13px; color:#656d76; margin-bottom:20px; padding:10px; background:#f6f8fa; border-radius:6px; border:1px solid #d0d7de;'>");
            sb.AppendLine($"<b>PR Author:</b> {report.AuthorName} &lt;{report.AuthorEmail}&gt;<br/>");
            sb.AppendLine($"<b>Branch:</b> <code>{report.Branch}</code> &bull; <b>Commit:</b> <code>{report.CommitSha}</code>");
            sb.AppendLine("</div>");

            // Violations List
            if (report.Violations.Count > 0)
            {
                sb.AppendLine("<h2 style='font-size:17px; font-weight:700; margin:24px 0 14px 0; border-bottom:2px solid #e1e4e8; padding-bottom:6px;'>🔍 What Needs Your Attention:</h2>");

                int index = 1;
                foreach (var v in report.Violations)
                {
                    string badgeClass = v.Severity == ViolationSeverity.Error ? "badge-error" : "badge-warning";
                    string relFile = Path.GetFileName(v.TargetFile);

                    sb.AppendLine("<div class='issue-card'>");
                    
                    // Card Top Bar
                    sb.AppendLine("<div style='display:flex; justify-content:space-between; align-items:center;'>");
                    sb.AppendLine($"<span class='{badgeClass}'>{v.Severity.ToString().ToUpperInvariant()}</span>");
                    sb.AppendLine($"<span style='font-size:12px; color:#656d76; font-weight:600;'>Lines {v.LineNumber} &ndash; {v.EndLineNumber}</span>");
                    sb.AppendLine("</div>");

                    // Title
                    sb.AppendLine($"<div class='issue-title'>{index++}. [{v.RuleId}] {v.RuleName} in <code>{v.MemberName}()</code></div>");

                    // Where
                    sb.AppendLine("<div class='section-label'>📍 Where:</div>");
                    sb.AppendLine($"<div style='font-size:13px; font-weight:600;'><code>{relFile}</code> (Lines {v.LineNumber}&ndash;{v.EndLineNumber})</div>");

                    // What
                    sb.AppendLine("<div class='section-label'>⚠️ What is the issue:</div>");
                    sb.AppendLine($"<div style='font-size:13px;'>{v.Description} (<b>Measured: {v.ActualValue}</b> vs <b>Limit: {v.ThresholdValue}</b>)</div>");

                    // Why
                    sb.AppendLine("<div class='section-label'>💡 Why it matters:</div>");
                    sb.AppendLine($"<div style='font-size:13px; color:#57606a;'>{v.Rationale}</div>");

                    // How to fix
                    if (v.ActionSteps.Count > 0)
                    {
                        sb.AppendLine("<div class='action-box'>");
                        sb.AppendLine("<b>🛠️ How to fix it (Step-by-Step):</b><ol>");
                        foreach (var step in v.ActionSteps)
                        {
                            sb.AppendLine($"<li>{step}</li>");
                        }
                        sb.AppendLine("</ol></div>");
                    }

                    // Refactoring Code Example
                    if (!string.IsNullOrWhiteSpace(v.CodeExample))
                    {
                        sb.AppendLine("<div class='section-label'>💻 Refactoring Blueprint (Before vs After):</div>");
                        sb.AppendLine($"<div class='code-box'>{HttpUtility.HtmlEncode(v.CodeExample)}</div>");
                    }

                    sb.AppendLine("</div>");
                }
            }
            else
            {
                sb.AppendLine("<div style='text-align:center; padding:32px 16px;'>");
                sb.AppendLine("<h2 style='color:#1a7f37; margin-bottom:6px;'>🎉 Ready for Merge!</h2>");
                sb.AppendLine("<p style='font-size:14px; color:#656d76;'>Zero code quality issues found in this pull request.</p>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div>"); // end content
            sb.AppendLine($"<div class='footer'>Automated Code Quality Monitor &bull; Delivered via Microsoft Outlook SMTP &bull; {report.AnalysisTime:yyyy-MM-dd HH:mm} UTC</div>");
            sb.AppendLine("</div></body></html>");

            return sb.ToString();
        }
    }
}
