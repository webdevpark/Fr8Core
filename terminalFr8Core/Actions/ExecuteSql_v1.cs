﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;

using Hub.Managers;
using TerminalBase.BaseClasses;
using TerminalBase.Infrastructure;
using TerminalSqlUtilities;
using terminalFr8Core.Infrastructure;
using Data.Entities;
using Data.States;

namespace terminalFr8Core.Actions
{
    public class ExecuteSql_v1 : BaseTerminalAction
    {
        private const string DefaultDbProvider = "System.Data.SqlClient";


        #region Configuration

        public override ConfigurationRequestType ConfigurationEvaluator(ActionDO curActionDO)
        {
            return ConfigurationRequestType.Initial;
        }

        protected override Task<ActionDO> InitialConfigurationResponse(ActionDO curActionDO, AuthorizationTokenDO authTokenDO)
        {
            using (var updater = Crate.UpdateStorage(curActionDO))
            {
                AddLabelControl(

                    updater.CrateStorage,
                    "NoConfigLabel",
                    "No configuration",
                    "This action does not require any configuration."
                    );
            }

            return Task.FromResult(curActionDO);
        }

        #endregion Configuration

        #region Execution

        private QueryDTO ExtractSqlQuery(PayloadDTO payload)
        {
            var sqlQueryCrate = Crate.FromDto(payload.CrateStorage).CratesOfType<StandardQueryCM>(x => x.Label == "Sql Query").FirstOrDefault();

            if (sqlQueryCrate == null) { return null; }

            
            var queryCM = sqlQueryCrate.Content;
            if (queryCM == null || queryCM.Queries == null || queryCM.Queries.Count == 0) { return null; }

            return queryCM.Queries[0];
        }

        private ColumnInfo CreateColumnInfo(
            string columnName, Dictionary<string, DbType> columnTypes)
        {
            DbType dbType;
            if (!columnTypes.TryGetValue(columnName, out dbType))
            {
                throw new ApplicationException("No column db-type found.");
            }

            var columnInfo = new ColumnInfo(columnName, dbType);
            return columnInfo;
        }

        private SelectQuery BuildQuery(string connectionString,
            QueryDTO query, Dictionary<string, DbType> columnTypes)
        {
            var tableInfo = new TableInfo(query.Name);
            var tableColumns = columnTypes.Keys.Where(x => x.StartsWith(query.Name)).ToList();

            var columns = tableColumns.Select(x => CreateColumnInfo(x, columnTypes)).ToList();

            var returnedQuery = new SelectQuery(connectionString, tableInfo, columns, query.Criteria);
            return returnedQuery;
        }

        private StandardPayloadDataCM BuildStandardPayloadData(
            Table data, Dictionary<string, DbType> columnTypes)
        {
            var findObjectHelper = new FindObjectHelper();

            var payloadCM = new StandardPayloadDataCM();

            var tableData = new List<PayloadObjectDTO>();
            
            if (data.Rows != null)
            {
                foreach (var row in data.Rows)
                {
                    if (row.Values == null) { continue; }

                    var payloadObject = new PayloadObjectDTO();
                    foreach (var fieldValue in row.Values)
                    {
                        var columnName = data.TableInfo.ToString() + "." + fieldValue.Field;

                        DbType dbType;
                        if (!columnTypes.TryGetValue(columnName, out dbType))
                        {
                            throw new ApplicationException("No column data type found.");
                        }

                        payloadObject.PayloadObject.Add(
                            new FieldDTO()
                            {
                                Key = fieldValue.Field,
                                Value = findObjectHelper.ConvertValueToString(fieldValue.Value, dbType)
                            }
                        );
                    }

                    tableData.Add(payloadObject);
                }
            }

            payloadCM.PayloadObjects.AddRange(tableData);

            return payloadCM;
        }

        private async Task<string> ExtractConnectionString(ActionDO actionDO)
        {
            var upstreamCrates = await GetCratesByDirection<StandardDesignTimeFieldsCM>(
                actionDO,
                CrateDirection.Upstream
            );

            if (upstreamCrates == null) { return null; }

            var connectionStringCrate = upstreamCrates
                .FirstOrDefault(x => x.Label == "Sql Connection String");

            if (connectionStringCrate == null) { return null; }

            var connectionStringCM = connectionStringCrate.Content;

            if (connectionStringCM == null) { return null; }

            var connectionStringFields = connectionStringCM.Fields;
            if (connectionStringFields == null || connectionStringFields.Count == 0) { return null; }

            return connectionStringFields[0].Key;
        }

        public async Task<PayloadDTO> Run(ActionDO curActionDO, Guid containerId, AuthorizationTokenDO authTokenDO)
        {
            var findObjectHelper = new FindObjectHelper();
            var payload = await GetProcessPayload(curActionDO, containerId);

            var columnTypes = await findObjectHelper.ExtractColumnTypes(this, curActionDO);
            if (columnTypes == null)
            {
                throw new ApplicationException("No column types crate found.");
            }

            var queryPayloadValue = ExtractSqlQuery(payload);
            if (queryPayloadValue == null)
            {
                throw new ApplicationException("No Sql Query payload crate found.");
            }

            var connectionString = await ExtractConnectionString(curActionDO);

            var query = BuildQuery(connectionString, queryPayloadValue, columnTypes);

            var dbProvider = DbProvider.GetDbProvider(DefaultDbProvider);
            var data = dbProvider.ExecuteQuery(query);
            var payloadCM = BuildStandardPayloadData(data, columnTypes);

            var payloadCMCrate = Data.Crates.Crate.FromContent("Sql Query Result", payloadCM);

            using (var updater = Crate.UpdateStorage(payload))
            {
                updater.CrateStorage.Add(payloadCMCrate);
            }

            return payload;
        }

        #endregion Execution
    }
}