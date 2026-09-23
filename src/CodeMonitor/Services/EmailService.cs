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
        public bool SendReport(AnalysisReport report, string? senderEmail, string? appPassword, string? recipientEmail, string? additionalRecipients = null, string smtpServer = "smtp.office365.com", int smtpPort = 587)
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

                // Add primary recipient (PR Author)
                message.To.Add(new MailboxAddress(report.AuthorName.Length > 0 ? report.AuthorName : "Developer", targetRecipient));
                Console.WriteLine($"[EmailService] Primary recipient (TO): {targetRecipient}");

                // Add configurable additional recipients (Manager, Reviewer, Team Leads) directly to TO list
                if (!string.IsNullOrWhiteSpace(additionalRecipients))
                {
                    var extraEmails = additionalRecipients.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var email in extraEmails)
                    {
                        var trimmed = email.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed) && trimmed.Contains("@") && !trimmed.Equals(targetRecipient, StringComparison.OrdinalIgnoreCase))
                        {
                            message.To.Add(new MailboxAddress("Reviewer / Lead", trimmed));
                            Console.WriteLine($"[EmailService] Additional recipient added (TO): {trimmed}");
                        }
                    }
                }

                string prInfo = !string.IsNullOrWhiteSpace(report.PullRequestNumber) ? $"PR #{report.PullRequestNumber}" : report.Branch;
                if (report.IsPassed)
                {
                    message.Subject = $"✅ All Quality Checks Passed: {report.Repository} ({prInfo})";
                }
                else
                {
                    message.Subject = $"🚨 Action Required: {report.Violations.Count} Code Issue(s) in {report.Repository} ({prInfo})";
                }

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = GenerateHtmlBody(report)
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                var secureOption = smtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                
                Console.WriteLine($"[EmailService] Connecting to {smtpServer}:{smtpPort} via TLS...");
                client.Connect(smtpServer, smtpPort, secureOption);
                
                Console.WriteLine($"[EmailService] Authenticating as {senderEmail}...");
                client.Authenticate(senderEmail, appPassword);
                
                Console.WriteLine($"[EmailService] Dispatching email to PR author: {targetRecipient}...");
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
            string statusColor = report.IsPassed ? "#16a34a" : "#dc2626";
            string bannerBg = report.IsPassed ? "#f0fdf4" : "#fef2f2";
            string bannerBorder = report.IsPassed ? "#bbf7d0" : "#fecaca";
            string authorName = !string.IsNullOrWhiteSpace(report.AuthorName) ? report.AuthorName : "Developer";

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset='utf-8'>");
            sb.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #f3f4f6; margin: 0; padding: 20px; color: #1f2937; line-height: 1.5; }");
            sb.AppendLine(".email-container { max-width: 650px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; border: 1px solid #e5e7eb; box-shadow: 0 4px 16px rgba(0,0,0,0.06); }");
            sb.AppendLine(".top-banner { background: " + bannerBg + "; border-bottom: 2px solid " + bannerBorder + "; padding: 24px; text-align: left; }");
            sb.AppendLine(".status-pill { display: inline-block; background: " + statusColor + "; color: #ffffff; font-size: 11px; font-weight: 700; text-transform: uppercase; padding: 4px 10px; border-radius: 20px; letter-spacing: 0.5px; margin-bottom: 8px; }");
            sb.AppendLine(".hero-title { margin: 4px 0 0 0; font-size: 20px; font-weight: 700; color: #111827; }");
            sb.AppendLine(".hero-subtitle { margin: 6px 0 0 0; font-size: 14px; color: #4b5563; }");
            sb.AppendLine(".main-body { padding: 24px; }");
            sb.AppendLine(".cta-btn { display: inline-block; background: #2563eb; color: #ffffff !important; font-size: 14px; font-weight: 600; text-decoration: none; padding: 12px 24px; border-radius: 8px; margin: 8px 0 20px 0; text-align: center; }");
            sb.AppendLine(".summary-box { background: #f9fafb; border: 1px solid #e5e7eb; border-radius: 8px; padding: 14px 16px; margin-bottom: 24px; font-size: 13px; color: #4b5563; }");
            sb.AppendLine(".issue-item { background: #ffffff; border: 1px solid #e5e7eb; border-left: 4px solid " + statusColor + "; border-radius: 8px; padding: 16px; margin-bottom: 18px; box-shadow: 0 1px 3px rgba(0,0,0,0.02); }");
            sb.AppendLine(".issue-header { display: flex; justify-content: space-between; align-items: baseline; margin-bottom: 8px; }");
            sb.AppendLine(".issue-name { font-size: 15px; font-weight: 700; color: #1e40af; }");
            sb.AppendLine(".pill-badge { background: #fee2e2; color: #991b1b; font-size: 11px; font-weight: 700; padding: 2px 8px; border-radius: 6px; }");
            sb.AppendLine(".field-row { margin: 6px 0; font-size: 13px; }");
            sb.AppendLine(".field-label { font-weight: 600; color: #374151; }");
            sb.AppendLine(".how-to-fix-box { background: #eff6ff; border: 1px solid #bfdbfe; border-radius: 6px; padding: 12px 14px; margin-top: 10px; font-size: 13px; color: #1e3a8a; }");
            sb.AppendLine(".how-to-fix-box ul { margin: 6px 0 0 0; padding-left: 18px; }");
            sb.AppendLine(".how-to-fix-box li { margin-bottom: 3px; }");
            sb.AppendLine(".code-preview { background: #1f2937; color: #f9fafb; border-radius: 6px; padding: 12px; font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace; font-size: 12px; line-height: 1.45; overflow-x: auto; white-space: pre-wrap; margin-top: 10px; }");
            sb.AppendLine(".footer-note { background: #f9fafb; border-top: 1px solid #e5e7eb; padding: 16px; text-align: center; font-size: 12px; color: #6b7280; }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<div class='email-container'>");

            // Top Banner
            sb.AppendLine("<div class='top-banner'>");
            if (report.IsPassed)
            {
                sb.AppendLine("<span class='status-pill' style='background:#16a34a;'>✅ Quality Gate Passed</span>");
                sb.AppendLine("<h1 class='hero-title'>Awesome job, " + HttpUtility.HtmlEncode(authorName) + "!</h1>");
                sb.AppendLine("<p class='hero-subtitle'>Your pull request passed all automated code quality checks and is ready for merge.</p>");
            }
            else
            {
                sb.AppendLine("<span class='status-pill' style='background:#dc2626;'>🚨 Action Required</span>");
                sb.AppendLine("<h1 class='hero-title'>Hey " + HttpUtility.HtmlEncode(authorName) + ", please check your PR</h1>");
                sb.AppendLine($"<p class='hero-subtitle'>We found <b>{report.Violations.Count} code quality items</b> to polish before merging.</p>");
            }
            sb.AppendLine("</div>");

            sb.AppendLine("<div class='main-body'>");

            // Call To Action Button
            if (!string.IsNullOrWhiteSpace(report.PullRequestUrl))
            {
                string prText = !string.IsNullOrWhiteSpace(report.PullRequestNumber) ? $"PR #{report.PullRequestNumber}" : "Pull Request";
                sb.AppendLine("<div style='text-align:center;'>");
                sb.AppendLine($"<a class='cta-btn' href='{report.PullRequestUrl}'>👉 Open & Fix {prText} on GitHub &rarr;</a>");
                sb.AppendLine("</div>");
            }

            // Quick Details Summary
            sb.AppendLine("<div class='summary-box'>");
            sb.AppendLine($"<b>Repository:</b> <code>{report.Repository}</code> &bull; <b>Branch:</b> <code>{report.Branch}</code><br/>");
            sb.AppendLine($"<b>Commit:</b> <code>{report.CommitSha}</code> &bull; <b>Files Analyzed:</b> {report.AnalyzedFiles.Count}");
            sb.AppendLine("</div>");

            // Issues Section
            if (report.Violations.Count > 0)
            {
                sb.AppendLine("<h2 style='font-size:16px; font-weight:700; margin:0 0 14px 0; color:#111827;'>📋 Issues Found & How to Fix Them:</h2>");

                int index = 1;
                foreach (var v in report.Violations)
                {
                    string relFile = Path.GetFileName(v.TargetFile);
                    sb.AppendLine("<div class='issue-item'>");
                    
                    // Card Title
                    sb.AppendLine($"<div style='margin-bottom:8px;'>");
                    sb.AppendLine($"<span class='pill-badge'>ISSUE #{index++}</span>");
                    sb.AppendLine($"<span class='issue-name' style='margin-left:6px;'>[{v.RuleId}] {v.RuleName}</span>");
                    sb.AppendLine("</div>");

                    // 1. Where
                    sb.AppendLine("<div class='field-row'>");
                    sb.AppendLine($"<span class='field-label'>📍 Location:</span> <code>{relFile}</code> &rarr; <b>Line {v.LineNumber} to {v.EndLineNumber}</b> (in method <code>{v.MemberName}()</code>)");
                    sb.AppendLine("</div>");

                    // 2. What
                    sb.AppendLine("<div class='field-row'>");
                    sb.AppendLine($"<span class='field-label'>⚠️ The Problem:</span> {v.Description} (<b>Measured: {v.ActualValue}</b> | Allowed: {v.ThresholdValue})");
                    sb.AppendLine("</div>");

                    // 3. Why
                    sb.AppendLine("<div class='field-row' style='color:#4b5563; font-size:12px;'>");
                    sb.AppendLine($"<span class='field-label' style='color:#4b5563;'>💡 Why it matters:</span> {v.Rationale}");
                    sb.AppendLine("</div>");

                    // 4. How to fix
                    if (v.ActionSteps.Count > 0)
                    {
                        sb.AppendLine("<div class='how-to-fix-box'>");
                        sb.AppendLine("<b>🛠️ How to fix it (Step-by-Step):</b>");
                        sb.AppendLine("<ul>");
                        foreach (var step in v.ActionSteps)
                        {
                            sb.AppendLine($"<li>{step}</li>");
                        }
                        sb.AppendLine("</ul></div>");
                    }

                    // 5. Code Example Blueprint
                    if (!string.IsNullOrWhiteSpace(v.CodeExample))
                    {
                        sb.AppendLine("<div style='margin-top:10px; font-size:12px; font-weight:600; color:#374151;'>💻 Refactoring Blueprint (Before vs After):</div>");
                        sb.AppendLine($"<div class='code-preview'>{HttpUtility.HtmlEncode(v.CodeExample)}</div>");
                    }

                    sb.AppendLine("</div>"); // end issue-item
                }
            }
            else
            {
                sb.AppendLine("<div style='text-align:center; padding:28px 12px;'>");
                sb.AppendLine("<p style='font-size:15px; color:#16a34a; font-weight:600;'>🎉 No code quality violations detected!</p>");
                sb.AppendLine("<p style='font-size:13px; color:#6b7280;'>Your code is clean, well-structured, and ready for team review.</p>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div>"); // end main-body

            sb.AppendLine($"<div class='footer-note'>Automated Code Quality Monitor &bull; Delivered via Microsoft Outlook SMTP &bull; {report.AnalysisTime:yyyy-MM-dd HH:mm} UTC</div>");
            sb.AppendLine("</div></body></html>");

            return sb.ToString();
        }
    }
}
