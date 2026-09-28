using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using TSFramework.Libs.Models.Mail;
using TSFramework.Libs.Processors;
using TSFramework.Libs.Utils;

namespace TSFramework.Libs.Providers
{
    public static class SmtpClientFactory
    {
        public static SmtpClient CreateClient(ConfigMailModel config)
        {
            return new SmtpClient
            {
                Host = config.Host,
                Port = config.Port,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential
                    (config.UserCredential, config.UPass),
                EnableSsl = true
            };
        }
    }

    public class MailProvider
    {
        private static ConfigMailModel _config;

        private static MailProvider _instance;

        /// <summary>
        ///     Contructor mail provider
        /// </summary>
        /// <param name="config">Config model</param>
        private MailProvider(ConfigMailModel config)
        {
            _config = new ConfigMailModel
            {
                Port = config.Port,
                Host = config.Host,
                UPass = config.UPass,
                UserCredential = config.UserCredential,
                UserCredentialName = config.UserCredentialName,
                EnableSsl = config.EnableSsl
            };
        }

        /// <summary>
        ///     Create new Instance of MailProvider
        /// </summary>
        /// <returns></returns>
        public static MailProvider Instance()
        {
            _config = new ConfigMailModel
            {
                Host = ConfigurationManager.AppSettings["Email_Host"],
                Port = string.IsNullOrEmpty(ConfigurationManager.AppSettings["Email_Port"])
                    ? 587
                    : int.Parse(ConfigurationManager.AppSettings["Email_Port"]),
                UPass = ConfigurationManager.AppSettings["Email_UPass"],
                UserCredential = ConfigurationManager.AppSettings["Email_UserCredential"],
                UserCredentialName = ConfigurationManager.AppSettings["Email_UserCredentialName"],
                EnableSsl = !string.IsNullOrEmpty(ConfigurationManager.AppSettings["Email_EnableSsl"]) && ConfigurationManager.AppSettings["Email_EnableSsl"] == "true"
            };
            if (_instance != null) return _instance;
            _instance = new MailProvider(_config);

            return _instance;
        }

        /// <summary>
        ///     Create new Instance of MailProvider
        /// </summary>
        /// <param name="sPort">Port mail server</param>
        /// <param name="sHost">Host mail server</param>
        /// <param name="sUserCredential">Mail address</param>
        /// <param name="sUPass">Password of mail</param>
        /// <param name="sUserCredentialName">Name of mail to display </param>
        /// <param name="enableSsl"></param>
        /// <returns>MailProvider</returns>
        public static MailProvider Instance(int sPort, string sHost, string sUserCredential, string sUPass,
            string sUserCredentialName, bool enableSsl)
        {
            _config = new ConfigMailModel
            {
                Port = sPort,
                Host = sHost,
                UPass = sUPass,
                UserCredential = sUserCredential,
                UserCredentialName = sUserCredentialName,
                EnableSsl = enableSsl
            };

            _instance = new MailProvider(_config);

            return _instance;
        }

        /// <summary>
        ///     Send email via task
        /// </summary>
        /// <param name="lstMails"></param>
        //public void PushEmail(List<MailModel> lstMails)
        //{
        //    AppProcessor.Logger.Message("PushEmail");
        //    var queueTasks = new Queue<Task>();
        //    lstMails.ForEach(mail => { Task.Factory.StartNew(() => { SendMail(mail); }); });
        //    new Thread(() =>
        //    {
        //        Task.Factory.StartNew(() =>
        //        {
        //            try
        //            {
        //                while (queueTasks.Count > 0)
        //                {
        //                    var taskMail = queueTasks.Dequeue();
        //                    taskMail.Start();
        //                    while (!taskMail.IsCompleted)
        //                    {
        //                    }
        //                }
        //            }
        //            catch (InvalidOperationException)
        //            {
        //            }
        //        });
        //    }).Start();
        //}
        public void PushEmail(List<MailModel> lstMails)
        {
            AppProcessor.Logger.Message("PushEmail START");

            foreach (var mail in lstMails)
            {
                try
                {
                    SendMail(mail); // GỬI THẲNG
                }
                catch (Exception ex)
                {
                    AppProcessor.Logger.Error(ex);
                }
            }

            AppProcessor.Logger.Message("PushEmail END");
        }

        private bool IsOnline()
        {
            try
            {
                    
                var tcp = new TcpClient();
                tcp.Connect(_config.Host, _config.Port);
                AppProcessor.Logger.Message("IsOnline");

                return true;
            }
            catch (Exception e)
            {
                new LogProvider().Error(e);

            }
            AppProcessor.Logger.Message("IsNotOnline");

            return false;
        }

        private void SendMail(MailModel mail)
        {
            if (!IsOnline()) return;

            string originalBody = mail?.Body ?? string.Empty;
            var pm = new PreMailer.Net.PreMailer(originalBody);
            var mailBody = pm.MoveCssInline(true);
            string finalBody = mail.IsBodyHtml && mailBody != null && !string.IsNullOrWhiteSpace(mailBody.Html)
                ? mailBody.Html
                : originalBody;

            var nMail = new MailMessage
            {
                From = new MailAddress(mail.From ?? _config.UserCredential,
                    mail.DisplayNameFrom ?? _config.UserCredentialName, System.Text.Encoding.UTF8),
                Subject = mail.Subject,
                SubjectEncoding = System.Text.Encoding.UTF8,
                BodyEncoding = System.Text.Encoding.UTF8,
                HeadersEncoding = System.Text.Encoding.UTF8,
                DeliveryNotificationOptions = DeliveryNotificationOptions.OnFailure,
                IsBodyHtml = mail.IsBodyHtml,
                Body = finalBody
            };

            if (mail.DicImgs != null && mail.DicImgs.Count > 0 && mail.IsBodyHtml)
            {
                var avHtml = AlternateView.CreateAlternateViewFromString
                    (finalBody, null, MediaTypeNames.Text.Html);

                foreach (var imgKey in mail.DicImgs.Keys)
                {
                    var ms = new MemoryStream(mail.DicImgs[imgKey]);
                    var inline = new LinkedResource(ms, MediaTypeNames.Image.Jpeg)
                    {
                        ContentId = imgKey
                    };
                    avHtml.LinkedResources.Add(inline);
                }

                nMail.AlternateViews.Add(avHtml);
            }

            if (mail.To != null && mail.To.Count > 0) mail.To.ForEach(m => { nMail.To.Add(new MailAddress(m)); });

            if (mail.Bcc != null && mail.Bcc.Count > 0) mail.Bcc.ForEach(m => { nMail.Bcc.Add(new MailAddress(m)); });

            if (mail.Cc != null && mail.Cc.Count > 0) mail.Cc.ForEach(m => { nMail.CC.Add(new MailAddress(m)); });

            var smtpClient = SmtpClientFactory.CreateClient(_config);
            AppProcessor.Logger.Message(_config.ToJson());
            smtpClient.Send(nMail);
        }
        //private void SendMail(MailModel mail)
        //{
        //    AppProcessor.Logger.Message("SendMail START");

        //    if (!IsOnline())
        //    {
        //        AppProcessor.Logger.Message("SMTP OFFLINE");
        //        return;
        //    }

        //    var pm = new PreMailer.Net.PreMailer(mail.Body);
        //    var mailBody = pm.MoveCssInline(true);

        //    var nMail = new MailMessage
        //    {
        //        From = new MailAddress(
        //            _config.UserCredential,
        //            _config.UserCredentialName
        //        ),
        //        Subject = mail.Subject,
        //        IsBodyHtml = mail.IsBodyHtml
        //    };

        //    mail.To?.ForEach(m => nMail.To.Add(m));
        //    mail.Cc?.ForEach(m => nMail.CC.Add(m));
        //    mail.Bcc?.ForEach(m => nMail.Bcc.Add(m));

        //    var smtpClient = SmtpClientFactory.CreateClient(_config);

        //    smtpClient.Send(nMail);

        //    AppProcessor.Logger.Message("SEND MAIL SUCCESS");
        //}

    }
}
