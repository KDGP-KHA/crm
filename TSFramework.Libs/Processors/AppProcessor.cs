using System.Configuration;
using TSFramework.Libs.BaseApps;
using TSFramework.Libs.Providers;

namespace TSFramework.Libs.Processors
{
    public static class AppProcessor
    {
        private static StoreProcedureProvider _storeProceduror;
        private static AuthorityProvider _author;
        private static LogProvider _logger;
        private static JobLogProvider _jobLogger;
        private static JobSchedulerProvider _jobber;
        private static MessageProvider _messagor;
        private static NotificationProvider _notifider;
        private static MailProvider _mailer;

        private static readonly object _lockProcedure = new object();
        public static StoreProcedureProvider ProcedureProvider
        {
            get
            {
                if (_storeProceduror != null) return _storeProceduror;
                lock (_lockProcedure)
                {
                    return _storeProceduror ?? (_storeProceduror = StoreProcedureProvider.Instance());
                }
            }
        }

        public static AuthorityProvider Author => _author ?? (_author =
            AuthorityProvider.Instance(
                ProcedureProvider?.DicStoreProceduresProvider?[
                    ConfigurationManager.AppSettings["App_DefaultDataProvider"]]));

        public static MessageProvider Messagor => _messagor ?? (_messagor =
            MessageProvider.Instance(ProcedureProvider.DicStoreProceduresProvider,
                BaseAppContext.Current.CurrentLanguageCode));

        public static LogProvider Logger => _logger ?? (_logger = new LogProvider());
        public static JobLogProvider JobLogger => _jobLogger ?? (_jobLogger = new JobLogProvider());
        public static JobSchedulerProvider Jobber => _jobber ?? (_jobber = new JobSchedulerProvider());
        public static NotificationProvider Notifider => _notifider ?? (_notifider = new NotificationProvider(Messagor));
        public static MailProvider Mailer => _mailer ?? (_mailer = MailProvider.Instance());
    }
}