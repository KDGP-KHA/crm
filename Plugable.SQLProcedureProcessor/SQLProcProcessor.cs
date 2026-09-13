using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using TiSun;
using TSFramework.Libs.Extensions;
using TSFramework.Libs.Interfaces;

namespace Plugable.SQLProcedureProcessor
{
    public class SQLProcProcessor : IStoreProcedure
    {
        private const string PROCEDURE_QUERY = @"SELECT p.name        AS ProcedureName,
                                               p2.PARAMETER_NAME     AS ParameterName,
                                               p2.DATA_TYPE          AS DataType,
                                               p2.NUMERIC_PRECISION  AS NumberPrecision,
                                               p2.NUMERIC_SCALE      AS NumberScale
                                       FROM   sys.procedures AS p
                                               LEFT OUTER JOIN INFORMATION_SCHEMA.PARAMETERS AS p2
                                                    ON  p.name = p2.SPECIFIC_NAME
                                       WHERE p.name IS NOT NULL 
                                       ORDER BY p2.ORDINAL_POSITION";

        public SQLProcProcessor()
        {
            MappingProcedure = new Dictionary<string, string>();
            ProcProvider = null;
            ProviderName = null;
            SchemaName = null;
        }

        public SQLProcProcessor(string providerName, Dictionary<string, string> mappingSp, string schemaName = "dbo")
        {
            MappingProcedure = mappingSp;
            ProcProvider = DataService.GetInstance(providerName);
            ProviderName = providerName;
            SchemaName = schemaName;
        }

        private Dictionary<string, string> MappingProcedure { get; set; }

        private Dictionary<string, ProcedureModel> DicProcedures { get; set; }
        public Type ProviderType => typeof(SqlDataProvider);
        public DataProvider ProcProvider { get; set; }
        public string SchemaName { get; set; }
        public string ProviderName { get; set; }

        public Dictionary<string, ProcedureModel> ListProcedures =>
            DicProcedures ??
            (DicProcedures = GetProcedures() ?? new Dictionary<string, ProcedureModel>());

        public IStoreProcedure Instance(string providerName, Dictionary<string, string> mappingSp,
            string schemaName = "dbo")
        {
            return new SQLProcProcessor
            {
                ProviderName = providerName,
                MappingProcedure = mappingSp,
                ProcProvider = DataService.GetInstance(providerName),
                SchemaName = schemaName
            };
        }

        public Dictionary<string, ProcedureModel> GetProcedures()
        {
            var qCommand = new QueryCommand(PROCEDURE_QUERY, ProviderName);
            var spReader = ProcProvider.GetReader(qCommand);
            var dtSPs = new DataTable();
            dtSPs.Load(spReader);

            var lstProcedures = dtSPs.AsEnumerable()
                .GroupBy(row => row.Field<string>("ProcedureName"))
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => new ProcedureModel
                    {
                        Name = g.Key.ToString(),
                        Params = g.Select(p => new ProcedureParamModel
                        {
                            Name = p.Field<string>("ParameterName"),
                            DataType = string.Equals(p.Field<string>("DataType"), "date", StringComparison.OrdinalIgnoreCase)
                                ? DbType.Date
                                : ProcProvider.GetDbType(p.Field<string>("DataType")),
                            NumberPrecision = p.Field<byte?>("NumberPrecision"),
                            NumberScale = p.Field<int?>("NumberScale")
                        }).ToList()
                    });

            return lstProcedures;
        }

        public ProcedureModel GetProcedureInfoByName(string spName)
        {
            return ListProcedures[spName];
        }

        public DataTable ExecuteProcedureDataTable(string spName, bool useAlias, params object[] parameters)
        {
            var sp = InitStoreProcedure(spName, useAlias, parameters);
            if (sp == null) return null;
            var spReader = sp.Provider.GetReader(sp.Command);
            var dtSPs = new DataTable();
            dtSPs.Load(spReader);
            return dtSPs;
        }

        public int? ExecuteProcedure(string spName, params object[] parameters)
        {
            var sp = InitStoreProcedure(spName, false, parameters);
            if (sp == null) return -1;
            sp.Execute();
            var outputSp = (int?)sp.OutputValues[0];
            return outputSp ?? -1;
        }

        public List<T> ExecuteQueryTypedList<T>(string sQuery) where T : new()
        {
            var cmd = new QueryCommand(sQuery, ProviderName);
            var dataQuery = ProcProvider.GetDataSet(cmd)?.Tables[0];
            var lstObject = dataQuery.ToList<T>();
            return lstObject;
        }

        public object ExecuteQueryScalar<T>(string sQuery)
        {
            var cmd = new QueryCommand(sQuery, ProviderName);
            var lstObject = ProcProvider.ExecuteScalar(cmd);
            return lstObject;
        }

        public object ExecuteProcedureScalar(string spName, params object[] parameters)
        {
            var sp = InitStoreProcedure(spName, false, parameters);
            return sp?.ExecuteScalar();
        }

        public object ExecuteProcedureScalar<T>(string spName, params object[] parameters)
        {
            var sp = InitStoreProcedure(spName, false, parameters);
            return (T)sp?.ExecuteScalar<T>();
        }

        public List<T> ExecuteProcedureTypedList<T>(string spName, params object[] parameters) where T : new()
        {
            var sp = InitStoreProcedure(spName, false, parameters);
            return sp?.ExecuteTypedList<T>();
        }

        private StoredProcedure InitStoreProcedure(string keySpName, bool useAlias, params object[] parameters)
        {
            ProcedureModel spInfo;
            string spName;
            if (!useAlias)
            {
                spName = MappingProcedure?[keySpName] ?? string.Empty;
                if (string.IsNullOrEmpty(spName)) return null;
                spInfo = ListProcedures[spName];
            }
            else
            {
                spInfo = ListProcedures[keySpName];
                spName = keySpName;
            }

            if (spInfo == null) return null;
            var sp = new StoredProcedure(spName, ProcProvider);
            if (parameters == null) return sp;
            var pIdx = 0;
            spInfo.Params?.ForEach(p =>
            {
                if (pIdx >= parameters.Length) return;
                sp.Command.AddParameter(p.Name, parameters[pIdx], p.DataType, p.NumberScale, p.NumberPrecision);
                pIdx++;
            });
            sp.Command.AddReturnParameter();
            return sp;
        }
    }
}