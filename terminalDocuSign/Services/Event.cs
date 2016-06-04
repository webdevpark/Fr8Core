﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using AutoMapper;
using Fr8Data.Crates;
using Fr8Data.DataTransferObjects;
using Fr8Data.Managers;
using Fr8Data.Manifests;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StructureMap;
using Hub.Managers;
using terminalDocuSign.Interfaces;
using terminalDocuSign.Infrastructure;
using TerminalBase.Infrastructure;
using TerminalBase.Models;

namespace terminalDocuSign.Services
{
    public class Event : terminalDocuSign.Interfaces.IEvent
    {
        private readonly IDocuSignPlan _docuSignPlan;
        private readonly ICrateManager _crateManager;

        public Event()
        {
            _docuSignPlan = ObjectFactory.GetInstance<IDocuSignPlan>();
            _crateManager = ObjectFactory.GetInstance<ICrateManager>();
        }

        public async Task<Crate> Process(string curExternalEventPayload)
        {

            //if the event payload is Fr8 User ID, it is DocuSign Authentication Completed event
            if (curExternalEventPayload.Contains("fr8_user_id"))
            {
                var curFr8UserAndToken = ConfirmAuthentication(curExternalEventPayload);

                var hubCommunicator = ObjectFactory.GetInstance<IHubCommunicator>();

                hubCommunicator.Configure("terminalDocuSign", curFr8UserAndToken.Item1);

                try
                {
                    _docuSignPlan.CreateConnect(hubCommunicator, curFr8UserAndToken.Item2);
                }
                catch
                {
                    //create polling
                    _docuSignPlan.CreateOrUpdatePolling(hubCommunicator, curFr8UserAndToken.Item2);
                }
                finally
                {
                    //create MonitorAllDocuSignEvents plan
                    await _docuSignPlan.CreatePlan_MonitorAllDocuSignEvents(hubCommunicator, curFr8UserAndToken.Item2);
                }
            }


            //If this is a connect event
            Debug.WriteLine($"Received external payload: {curExternalEventPayload}");
            if (curExternalEventPayload.Contains("DocuSignEnvelopeInformation"))
            {
                Console.WriteLine("Connect event received by DocuSign terminal");
                return ProcessConnectEvent(curExternalEventPayload);
            }

            if (curExternalEventPayload.Contains("Polling Event"))
            {
                return ProcessPollingEvent(curExternalEventPayload);
            }

            return null;
        }

        private Crate ProcessPollingEvent(string curExternalEventPayload)
        {
            var eventCrate = JsonConvert.DeserializeObject<CrateDTO>(curExternalEventPayload);
            var eventManifest = _crateManager.FromDto(eventCrate).Get<DocuSignEnvelopeCM_v2>();
            var eventReportContent = new EventReportCM
            {
                EventNames = DocuSignEventParser.GetEventNames(eventManifest),
                ContainerDoId = "",
                EventPayload = new CrateStorage(Crate.FromContent("DocuSign Connect Event", eventManifest)),
                Manufacturer = "DocuSign",
                ExternalAccountId = eventManifest.ExternalAccountId
            };

            return Crate.FromContent("Standard Event Report", eventReportContent);
        }

        private Crate ProcessConnectEvent(string curExternalEventPayload)
        {
            // Connect events come only for a single envelope
            var curDocuSignEnvelopeInfo = DocuSignEventParser.GetEnvelopeInformation(curExternalEventPayload);
            // transform XML structure into DocuSignEnvelopeCM_v2
            var curDocuSingEnvelopCM = DocuSignEventParser.ParseXMLintoCM(curDocuSignEnvelopeInfo);
            var eventReportContent = new EventReportCM
            {
                EventNames = DocuSignEventParser.GetEventNames(curDocuSignEnvelopeInfo),
                ContainerDoId = "",
                EventPayload = new CrateStorage(Crate.FromContent("DocuSign Connect Event", curDocuSingEnvelopCM)),
                Manufacturer = "DocuSign",
                ExternalAccountId = curDocuSignEnvelopeInfo.EnvelopeStatus.Email
            };

            ////prepare the event report
            var curEventReport = Crate.FromContent("Standard Event Report", eventReportContent);
            return curEventReport;
        }

        private Tuple<string, AuthorizationToken> ConfirmAuthentication(string curExternalEventPayload)
        {
            var jo = (JObject)JsonConvert.DeserializeObject(curExternalEventPayload);
            var curFr8UserId = jo["fr8_user_id"].Value<string>();
            var authToken = JsonConvert.DeserializeObject<AuthorizationTokenDTO>(jo["auth_token"].ToString());

            if (authToken == null)
            {
                throw new ArgumentException("Authorization Token required");
            }

            if (string.IsNullOrEmpty(curFr8UserId))
            {
                throw new ArgumentException("Fr8 User ID is not in the correct format.");
            }

            return new Tuple<string, AuthorizationToken>(curFr8UserId, Mapper.Map<AuthorizationToken>(authToken));
        }
    }
}