using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Xml;
using System.Xml.Serialization;
using TiSun;
using TSFramework.Libs.Helpers;
using TSFramework.Libs.Interfaces;
using TSFramework.Libs.Models.Procedure;

namespace TSFramework.Libs.Providers
{
    public class StoreProcedureProvider
    {
        private static string StoredProcedureConfigPath => ConfigurationManager.AppSettings["App_ProcConfigPath"];

        private static string _providerName { get; set; }

        private static readonly object _lockObj = new object();
        private static XmlProviders _xmlProviders { get; set; }

        private static XmlProviders XmlProviders
        {
            get
            {
                if (_xmlProviders?.Procedures != null) return _xmlProviders;
                lock (_lockObj)
                {
                    if (_xmlProviders?.Procedures != null) return _xmlProviders;
                    var xmlProcedureFullPath = HostingEnvironment.MapPath(StoredProcedureConfigPath);
                    if (string.IsNullOrEmpty(xmlProcedureFullPath) || !Directory.Exists(xmlProcedureFullPath)) return new XmlProviders { Procedures = new List<XmlProcedure>() };

                    var allProcedureXmlFiles = Directory.GetFiles(xmlProcedureFullPath, "*.xml", SearchOption.AllDirectories);
                    var newXmlProviders = new XmlProviders { Procedures = new List<XmlProcedure>() };

                    foreach (var procedureXmlFile in allProcedureXmlFiles)
                    {
                        using (var reader = XmlReader.Create(procedureXmlFile))
                        {
                            var serializer = new XmlSerializer(typeof(XmlProviders));
                            var procedureXmlProvider = (XmlProviders)serializer.Deserialize(reader);
                            if (procedureXmlProvider == null || procedureXmlProvider.Procedures == null || procedureXmlProvider.Procedures.Count == 0) continue;

                            procedureXmlProvider.Procedures.ForEach(p =>
                            {
                                var providerProcedure =
                                    newXmlProviders.Procedures.FirstOrDefault(e => e.ProviderName == p.ProviderName);
                                if (providerProcedure == null)
                                {
                                    newXmlProviders.Procedures.Add(new XmlProcedure
                                    {
                                        ProviderName = p.ProviderName,
                                        Procedures = p.Procedures != null ? p.Procedures.Clone() : new List<Procedure>()
                                    });
                                }
                                else
                                {
                                    if (providerProcedure.Procedures == null)
                                    {
                                        providerProcedure.Procedures = new List<Procedure>();
                                    }

                                    if (p.Procedures != null)
                                    {
                                        p.Procedures.ForEach(sp =>
                                        {
                                            if (sp == null) return;
                                            if (providerProcedure.Procedures.Exists(pp =>
                                                    pp != null && sp.Value == pp.Value && sp.Name == pp.Name)) return;
                                            providerProcedure.Procedures.Add(sp);
                                        });
                                    }
                                }
                            });
                        }
                    }

                    _xmlProviders = newXmlProviders;
                    return _xmlProviders;
                }
            }
        }

        public Dictionary<string, IStoreProcedure> DicStoreProceduresProvider { get; private set; }

        public static StoreProcedureProvider Instance()
        {
            var storeProceduresProvider = new Dictionary<string, IStoreProcedure>();
            var spProviders =
                LibraryProvider<IStoreProcedure>.LoadLibrary(
                    ConfigurationManager.AppSettings["App_LibraryProcPath"] ?? "Procedures");

            if (spProviders != null)
            {
                var procs = XmlProviders.Procedures?.ToList() ?? new List<XmlProcedure>();
                procs.ForEach(xp =>
                {
                    var dProvider = DataService.GetInstance(xp.ProviderName);
                    var libSpProvider =
                        spProviders.ToList().FirstOrDefault(sp => sp.ProviderType == dProvider.GetType());
                    if (libSpProvider == null) return;
                    if (storeProceduresProvider.Keys.Contains(xp.ProviderName)) return;
                    var dict = (xp.Procedures ?? new List<Procedure>())
                        .Where(p => p != null && !string.IsNullOrEmpty(p.Name))
                        .GroupBy(p => p.Name)
                        .ToDictionary(g => g.Key, g => g.First().Value);
                    storeProceduresProvider.Add(xp.ProviderName,
                        libSpProvider.Instance(xp.ProviderName, dict));
                });
            }

            var nSpProviders = new StoreProcedureProvider { DicStoreProceduresProvider = storeProceduresProvider };

            return nSpProviders;
        }

        public static StoreProcedureProvider Instance(string providerName)
        {
            _providerName = providerName;
            var storeProceduresProvider = new Dictionary<string, IStoreProcedure>();
            var spProviders =
                LibraryProvider<IStoreProcedure>.LoadLibrary(
                    ConfigurationManager.AppSettings["App_LibraryProcPath"] ?? "Procedures");
            var dProvider = DataService.GetInstance(providerName);

            if (spProviders != null)
            {
                var procs = (XmlProviders.Procedures ?? new List<XmlProcedure>()).Where(xp => xp.ProviderName == providerName).ToList();
                procs.ForEach(xp =>
                {
                    var libSpProvider =
                        spProviders.ToList().FirstOrDefault(sp => sp.ProviderType == dProvider.GetType());
                    if (libSpProvider == null) return;
                    if (storeProceduresProvider.Keys.Contains(xp.ProviderName)) return;
                    var dict = (xp.Procedures ?? new List<Procedure>())
                        .Where(p => p != null && !string.IsNullOrEmpty(p.Name))
                        .GroupBy(p => p.Name)
                        .ToDictionary(g => g.Key, g => g.First().Value);
                    storeProceduresProvider.Add(xp.ProviderName,
                        libSpProvider.Instance(xp.ProviderName, dict));
                });
            }

            var nSpProviders = new StoreProcedureProvider { DicStoreProceduresProvider = storeProceduresProvider };

            return nSpProviders;
        }

        public int? Execute(string spName, string providerName, params object[] p)
        {
            if (string.IsNullOrEmpty(spName) || string.IsNullOrEmpty(providerName)) return null;

            var storeProcedureProvider = DicStoreProceduresProvider[providerName];
            return storeProcedureProvider?.ExecuteProcedure(spName, p);
        }

        public object ExecuteDataTable(string v, string dATA_PROVIDER_NAME, string customerCode, string password)
        {
            throw new NotImplementedException();
        }

        public DataTable ExecuteProcedure(string spName, bool useAlias, string providerName, params object[] p)
        {
            if (string.IsNullOrEmpty(spName) || string.IsNullOrEmpty(providerName)) return null;

            var storeProcedureProvider = DicStoreProceduresProvider[providerName];
            return storeProcedureProvider?.ExecuteProcedureDataTable(spName, useAlias, p);
        }

        public object ExecuteScalar(string spName, string providerName, params object[] p)
        {
            if (string.IsNullOrEmpty(spName) || string.IsNullOrEmpty(providerName)) return null;

            var storeProcedureProvider = DicStoreProceduresProvider[providerName];
            return storeProcedureProvider?.ExecuteProcedureScalar(spName, p);
        }

        public object ExecuteScalar<T>(string spName, string providerName, params object[] p)
        {
            if (string.IsNullOrEmpty(spName) || string.IsNullOrEmpty(providerName)) return null;

            var storeProcedureProvider = DicStoreProceduresProvider[providerName];
            return storeProcedureProvider?.ExecuteProcedureScalar<T>(spName, p);
        }

        public T ExecuteScalarObject<T>(string spName, string providerName, params object[] p) where T : new()
        {
            if (string.IsNullOrEmpty(spName) || string.IsNullOrEmpty(providerName)) return default(T);

            var storeProcedureProvider = DicStoreProceduresProvider[providerName];
            var lstDatas = storeProcedureProvider?.ExecuteProcedureTypedList<T>(spName, p);
            if (lstDatas == null || !lstDatas.Any()) return default(T);
            return lstDatas[0];
        }

        public List<T> ExecuteTypedList<T>(string spName, string providerName, params object[] p) where T : new()
        {
            if (string.IsNullOrEmpty(spName) || string.IsNullOrEmpty(providerName)) return null;
            if (string.IsNullOrEmpty(providerName)) return null;

            var storeProcedureProvider = DicStoreProceduresProvider[providerName];
            return storeProcedureProvider?.ExecuteProcedureTypedList<T>(spName, p);
        }
    }
}