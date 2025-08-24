// Program..: EMail.cs
// Author...: G. Wassink
// Design...:
// Date.....: 15/06/2019 Last revised: 18/11/2024
// Notice...: Copyright 1999, All Rights Reserved
// Notes....: C#13 .Net 9.0.10
// Files....: None
// Programs.:
// Reserved.: Type Class (EMail)

using gwEmail.Classes;
using System.Net.Mail;
using System.Net.Mime;

namespace gwEmail;

public class EMail 
{
	readonly EmailConfig mailConfig;
	readonly EmailManager mailManager;

	#region "Constructor"

	public EMail(EmailUserData data)
	{
		mailManager = new EmailManager();
		mailConfig = new EmailConfig()
		{
			ClientCredentialUserName = data.userName,
			ClientCredentialPassword = data.password,
			Host = data.MXRecord,
			EnableSsl = data.Ssl,
			TOs = data.To,
			CCs = data.CC,
			From = data.From,
			FromDisplayName = "GWHEMS",
			Priority = MailPriority.Normal
		};
	}

	public int Send(string subject, string body)
	{
		mailConfig.Subject = subject;

		return mailManager.SendMail(mailConfig, new EmailContent() { body = body });
	}

	#endregion

	private class EmailManager
	{
		public int SendMail(EmailConfig config, EmailContent content)
		{
			return Send(ConstructEmailMessage(config, content), config);
		}

		private MailMessage ConstructEmailMessage(EmailConfig config, EmailContent content)
		{
			var msg = new MailMessage();

			foreach (var to in config.TOs)
				if (!string.IsNullOrEmpty(to))
					msg.To.Add(to);

			foreach (var cc in config.CCs)
				if (!string.IsNullOrEmpty(cc))
					msg.CC.Add(cc);

			msg.From = new MailAddress(config.From, config.FromDisplayName, System.Text.Encoding.UTF8);
			msg.IsBodyHtml = content.IsHtml;
			msg.Body = content.body;
			msg.Priority = config.Priority;
			msg.Subject = config.Subject;
			msg.BodyEncoding = System.Text.Encoding.UTF8;
			msg.SubjectEncoding = System.Text.Encoding.UTF8;

			if (content.AttachFile != "")
				msg.Attachments.Add(new Attachment(content.AttachFile, MediaTypeNames.Application.Zip));

			return msg;
		}

		private static int Send(MailMessage msg, EmailConfig config)
		{
			try
			{
				using var client = new SmtpClient()
				{
					UseDefaultCredentials = false,
					Credentials = new System.Net.NetworkCredential(config.ClientCredentialUserName, config.ClientCredentialPassword),
					Host = config.Host,
					Port = 25,
					EnableSsl = config.EnableSsl
				};

				client.Send(msg);
				msg.Dispose();

				return 0;
			}
			catch
			{
				msg.Dispose();
				//throw new Exception($"Error in Send email: {e.Message}");
				return 1;
			}
		}
	}
}
