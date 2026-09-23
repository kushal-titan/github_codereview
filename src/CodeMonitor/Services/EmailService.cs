using System;
using System.IO;
using System.Linq;
using System.Text;
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
                ? recipientEmail 
                : (!string.IsNullOrWhiteSpace(report.AuthorEmail) ? report.AuthorEmail : senderEmail);

            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Code Quality Monitor Bot", senderEmail));
                message.To.Add(new MailboxAddress(report.AuthorName.Length > 0 ? report.AuthorName : "Developer", targetRecipient));

                string statusTitle = report.IsPassed ? "Quality Gate Passed" : "Code Quality Violations Detected";
                message.Subject = $"{statusTitle} – {report.Repository} – {report.Branch}";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = GenerateHtmlBody(report)
                };

                message.Body = bodyBuilder.ToMessageBody();

                using var client = new SmtpClient();
                // Office 365 requires STARTTLS on port 587
                var secureOption = smtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                
                Console.WriteLine($"[EmailService] Connecting to {smtpServer}:{smtpPort}...");
                client.Connect(smtpServer, smtpPort, secureOption);
                
                Console.WriteLine($"[EmailService] Authenticating as {senderEmail}...");
                client.Authenticate(senderEmail, appPassword);
                
                Console.WriteLine($"[EmailService] Dispatching alert to {targetRecipient}...");
                client.Send(message);
                client.Disconnect(true);

                Console.WriteLine($"[EmailService] ✅ Email report successfully dispatched to {targetRecipient}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] ❌ Failed to dispatch email: {ex.Message}");
                return false;
            }
        }

        private string GenerateHtmlBody(AnalysisReport report)
        {
            var sb = new StringBuilder();
            string statusColor = report.IsPassed ? "#107c41" : "#d83b01";
            string statusBadge = report.IsPassed ? "QUALITY GATE PASSED" : "QUALITY VIOLATION DETECTED";

            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html><head><meta charset='utf-8'><style>");
            sb.AppendLine("body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f4f6f8; margin: 0; padding: 20px; color: #24292f; }");
            sb.AppendLine(".container { max-width: 680px; margin: 0 auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.08); }");
            sb.AppendLine(".header { background-color: " + statusColor + "; padding: 24px; color: #ffffff; text-align: center; }");
            sb.AppendLine(".header h1 { margin: 0; font-size: 22px; font-weight: 600; letter-spacing: 0.5px; }");
            sb.AppendLine(".header p { margin: 6px 0 0 0; opacity: 0.9; font-size: 14px; }");
            sb.AppendLine(".content { padding: 24px; }");
            sb.AppendLine(".meta-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; font-size: 13px; }");
            sb.AppendLine(".meta-table td { padding: 8px 12px; border-bottom: 1px solid #e1e4e8; }");
            sb.AppendLine(".meta-table td.label { font-weight: 600; width: 140px; color: #57606a; background: #f6f8fa; }");
            sb.AppendLine(".card { background: #fafbfc; border: 1px solid #e1e4e8; border-left: 4px solid " + statusColor + "; border-radius: 4px; padding: 16px; margin-bottom: 16px; }");
            sb.AppendLine(".card h3 { margin: 0 0 8px 0; font-size: 16px; color: #0969da; }");
            sb.AppendLine(".badge { display: inline-block; padding: 2px 8px; border-radius: 12px; font-size: 11px; font-weight: bold; }");
            sb.AppendLine(".badge-error { background: #ffebe9; color: #cf222e; }");
            sb.AppendLine(".badge-warning { background: #fff8c5; color: #9a6700; }");
            sb.AppendLine(".recommendation { background: #ddf4ff; border: 1px solid #54aeff; border-radius: 4px; padding: 12px; margin-top: 10px; font-size: 13px; }");
            sb.AppendLine(".footer { text-align: center; padding: 16px; font-size: 12px; color: #8c959f; border-top: 1px solid #e1e4e8; }");
            sb.AppendLine("</style></head><body>");

            sb.AppendLine("<div class='container'>");
            sb.AppendLine($"<div class='header'><h1>{statusBadge}</h1><p>Automated Roslyn AST Analysis Report</p></div>");
            sb.AppendLine("<div class='content'>");

            // Metadata Table
            sb.AppendLine("<table class='meta-table'>");
            sb.AppendLine($"<tr><td class='label'>Repository</td><td><b>{report.Repository}</b></td></tr>");
            sb.AppendLine($"<tr><td class='label'>Branch / Ref</td><td><code>{report.Branch}</code></td></tr>");
            if (!string.IsNullOrWhiteSpace(report.PullRequestNumber))
            {
                sb.AppendLine($"<tr><td class='label'>Pull Request</td><td><a href='{report.PullRequestUrl}' style='color:#0969da; text-decoration:none;'><b>PR #{report.PullRequestNumber}</b></a></td></tr>");
            }
            sb.AppendLine($"<tr><td class='label'>Commit SHA</td><td><code>{report.CommitSha}</code></td></tr>");
            sb.AppendLine($"<tr><td class='label'>Developer</td><td>{report.AuthorName} &lt;{report.AuthorEmail}&gt;</td></tr>");
            sb.AppendLine($"<tr><td class='label'>Summary</td><td><b>{report.ErrorCount} Error(s)</b>, <b>{report.WarningCount} Warning(s)</b></td></tr>");
            sb.AppendLine("</table>");

            if (report.Violations.Count > 0)
            {
                sb.AppendLine("<h2 style='font-size: 18px; margin-top: 24px; border-bottom: 2px solid #e1e4e8; padding-bottom: 6px;'>Violation Details & Recommendations</h2>");
                
                foreach (var v in report.Violations)
                {
                    string badgeClass = v.Severity == ViolationSeverity.Error ? "badge-error" : "badge-warning";
                    sb.AppendLine("<div class='card'>");
                    sb.AppendLine($"<div style='display:flex; justify-content:space-between; align-items:center; margin-bottom:8px;'>");
                    sb.AppendLine($"<span class='badge {badgeClass}'>{v.Severity.ToString().ToUpperInvariant()}</span>");
                    sb.AppendLine($"<span style='font-size:12px; color:#57606a;'>Line {v.LineNumber} - {v.EndLineNumber}</span>");
                    sb.AppendLine("</div>");
                    
                    sb.AppendLine($"<h3>[{v.RuleId}] {v.RuleName} &mdash; <code>{v.MemberName}()</code></h3>");
                    sb.AppendLine($"<p style='font-size:13px; margin:4px 0;'><b>File:</b> <code>{Path.GetFileName(v.TargetFile)}</code></p>");
                    sb.AppendLine($"<p style='font-size:13px; margin:4px 0;'><b>Metric:</b> Measured <b>{v.ActualValue}</b> (Allowed limit: <b>{v.ThresholdValue}</b>)</p>");
                    sb.AppendLine($"<p style='font-size:13px; margin:8px 0; color:#57606a;'><b>Impact:</b> {v.Rationale}</p>");

                    sb.AppendLine("<div class='recommendation'>");
                    sb.AppendLine($"<b>💡 Suggested Refactoring:</b><br/>{v.RecommendedFix}");
                    sb.AppendLine("</div>");
                    sb.AppendLine("</div>");
                }
            }
            else
            {
                sb.AppendLine("<div style='text-align:center; padding:32px 16px;'>");
                sb.AppendLine("<h2 style='color:#107c41; margin-bottom:8px;'>🎉 Great Job!</h2>");
                sb.AppendLine("<p style='font-size:14px; color:#57606a;'>All analyzed C# files passed quality criteria without any errors or warnings.</p>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div>");
            sb.AppendLine($"<div class='footer'>Generated automatically by Automated Code Quality Monitor at {report.AnalysisTime:yyyy-MM-dd HH:mm:ss UTC}</div>");
            sb.AppendLine("</div></body></html>");

            return sb.ToString();
        }
    }
}
