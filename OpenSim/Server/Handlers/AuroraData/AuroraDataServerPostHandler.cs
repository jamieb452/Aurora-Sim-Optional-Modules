﻿using Nini.Config;
using log4net;
using System;
using System.Reflection;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using OpenSim.Server.Base;
using OpenSim.Services.Interfaces;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse;
using Aurora.DataManager;
using Aurora.Framework;
using Aurora.Services.DataService;

namespace OpenSim.Server.Handlers.AuroraData
{
    public class AuroraDataServerPostHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IProfileConnector ProfileConnector = null;
        private IAgentConnector AgentConnector = null;
        private IRegionConnector GridConnector = null;
        private IEstateConnector EstateConnector = null;
        private IMuteListConnector MuteListConnector = null;
        private IOfflineMessagesConnector OfflineMessagesConnector = null;
        private IDirectoryServiceConnector DirectoryServiceConnector = null;

        public AuroraDataServerPostHandler() :
            base("POST", "/auroradata")
        {
            ProfileConnector = DataManager.RequestPlugin<IProfileConnector>("IProfileConnector");
            GridConnector = DataManager.RequestPlugin<IRegionConnector>("IRegionConnector");
            AgentConnector = DataManager.RequestPlugin<IAgentConnector>("IAgentConnector");
            EstateConnector = DataManager.RequestPlugin<IEstateConnector>("IEstateConnector");
            MuteListConnector = DataManager.RequestPlugin<IMuteListConnector>("IMuteListConnector");
            OfflineMessagesConnector = DataManager.RequestPlugin<IOfflineMessagesConnector>("IOfflineMessagesConnector");
            DirectoryServiceConnector = DataManager.RequestPlugin<IDirectoryServiceConnector>("IDirectoryServiceConnector");
        }

        public override byte[] Handle(string path, Stream requestData,
                OSHttpRequest httpRequest, OSHttpResponse httpResponse)
        {
            StreamReader sr = new StreamReader(requestData);
            string body = sr.ReadToEnd();
            sr.Close();
            body = body.Trim();

            //m_log.DebugFormat("[AuroraDataServerPostHandler]: query String: {0}", body);
            string method = "";
            Dictionary<string, object> request = new Dictionary<string, object>();
            try
            {
                request = ServerUtils.ParseQueryString(body);
                if (request.Count == 1)
                    request = ServerUtils.ParseXmlResponse(body);
                object value = null;
                request.TryGetValue("<?xml version", out value);
                if (value != null)
                    request = ServerUtils.ParseXmlResponse(body);



                if (!request.ContainsKey("METHOD"))
                    return FailureResult();

                method = request["METHOD"].ToString();

                switch (method)
                {
                    case "getprofile":
                        return GetProfile(request);
                    case "updateprofile":
                        return UpdateProfile(request);
                    case "updateinterests":
                        return UpdateInterests(request);
                    case "createprofile":
                        return CreateProfile(request);
                    case "removefromcache":
                        return RemoveFromCache(request);
                    case "updateusernotes":
                        return UpdateUserNotes(request);
                    case "getclassified":
                        return GetClassified(request);
                    case "addclassified":
                        return AddClassified(request);
                    case "deleteclassified":
                        return DeleteClassified(request);
                    case "addpick":
                        return AddPick(request);
                    case "deletepick":
                        return DeletePick(request);
                    case "updatepick":
                        return UpdatePick(request);
                    case "getpick":
                        return GetPick(request);
                    case "getagent":
                        return GetAgent(request);
                    case "updateagent":
                        return UpdateAgent(request);
                    case "createagent":
                        return CreateAgent(request);
                    case "removetelehub":
                        return RemoveTelehub(request);
                    case "addtelehub":
                        return AddTelehub(request);
                    case "findtelehub":
                        return FindTelehub(request);
                    case "loadestatesettings":
                        return LoadEstateSettings(request);
                    case "storeestatesettings":
                        return StoreEstateSettings(request);
                    case "linkregionestate":
                        return LinkRegionEstate(request);
                    case "deleteestate":
                        return DeleteEstate(request);
                    case "getregioninestate":
                        return GetRegionsInEstate(request);
                    case "getestates":
                        return GetEstates(request);
                    case "getmutelist":
                        return GetMuteList(request);
                    case "updatemute":
                        return UpdateMute(request);
                    case "deletemute":
                        return DeleteMute(request);
                    case "ismuted":
                        return IsMuted(request);
                    case "addofflinemessage":
                        return AddOfflineMessage(request);
                    case "getofflinemessages":
                        return GetOfflineMessages(request);
                    case "addlandobject":
                        return AddLandObject(request);
                    case "getparcelinfo":
                        return GetParcelInfo(request);
                    case "getparcelbyowner":
                        return GetParcelByOwner(request);
                    case "findland":
                        return FindLand(request);
                    case "findlandforsale":
                        return FindLandForSale(request);
                    case "findevents":
                        return FindEvents(request);
                    case "findeventsinregion":
                        return FindEventsInRegion(request);
                    case "findclassifieds":
                        return FindClassifieds(request);
                    case "geteventinfo":
                        return GetEventInfo(request);
                    case "findclassifiedsinregion":
                        return FindClassifiedsInRegion(request);
                }
                m_log.DebugFormat("[AuroraDataServerPostHandler]: unknown method {0} request {1}", method.Length, method);
            }
            catch (Exception e)
            {
                m_log.DebugFormat("[AuroraDataServerPostHandler]: Exception {0} in " + method, e);
            }

            return FailureResult();

        }

        private byte[] GetParcelInfo(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID INFOUUID = UUID.Parse(request["INFOUUID"].ToString());
            AuroraLandData land = DirectoryServiceConnector.GetParcelInfo(INFOUUID);

            result.Add("Land", land.ToKeyValuePairs());

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] GetParcelByOwner(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID OWNERID = UUID.Parse(request["OWNERID"].ToString());
            AuroraLandData[] lands = DirectoryServiceConnector.GetParcelByOwner(OWNERID);

            int i = 0;
            foreach (AuroraLandData land in lands)
            {
                result.Add(ConvertDecString(i), land.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] FindLand(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string QUERYTEXT = request["QUERYTEXT"].ToString();
            string CATEGORY = request["CATEGORY"].ToString();
            int STARTQUERY = int.Parse(request["STARTQUERY"].ToString());
            DirPlacesReplyData[] lands = DirectoryServiceConnector.FindLand(QUERYTEXT, CATEGORY, STARTQUERY);

            int i = 0;
            foreach (DirPlacesReplyData land in lands)
            {
                result.Add(ConvertDecString(i), land.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] FindLandForSale(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string SEARCHTYPE = request["SEARCHTYPE"].ToString();
            int PRICE = int.Parse(request["PRICE"].ToString());
            int AREA = int.Parse(request["AREA"].ToString());
            int STARTQUERY = int.Parse(request["STARTQUERY"].ToString());
            DirLandReplyData[] lands = DirectoryServiceConnector.FindLandForSale(SEARCHTYPE, PRICE.ToString(), AREA.ToString(), STARTQUERY);

            int i = 0;
            foreach (DirLandReplyData land in lands)
            {
                result.Add(ConvertDecString(i), land.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] FindEvents(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string QUERYTEXT = request["QUERYTEXT"].ToString();
            int FLAGS = int.Parse(request["FLAGS"].ToString());
            int STARTQUERY = int.Parse(request["STARTQUERY"].ToString());
            DirEventsReplyData[] lands = DirectoryServiceConnector.FindEvents(QUERYTEXT, FLAGS.ToString(), STARTQUERY);

            int i = 0;
            foreach (DirEventsReplyData land in lands)
            {
                result.Add(ConvertDecString(i), land.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] FindEventsInRegion(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string RegionName = request["REGIONNAME"].ToString();
            DirEventsReplyData[] lands = DirectoryServiceConnector.FindAllEventsInRegion(RegionName);

            int i = 0;
            foreach (DirEventsReplyData land in lands)
            {
                result.Add(ConvertDecString(i), land.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] FindClassifieds(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string QUERYTEXT = request["QUERYTEXT"].ToString();
            string CATEGORY = request["CATEGORY"].ToString();
            string QUERYFLAGS = request["QUERYFLAGS"].ToString();
            int STARTQUERY = int.Parse(request["STARTQUERY"].ToString());
            DirClassifiedReplyData[] lands = DirectoryServiceConnector.FindClassifieds(QUERYTEXT, CATEGORY, QUERYFLAGS, STARTQUERY);

            int i = 0;
            foreach (DirClassifiedReplyData land in lands)
            {
                result.Add(ConvertDecString(i), land.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] GetEventInfo(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string EVENTID = request["EVENTID"].ToString();
            EventData eventdata = DirectoryServiceConnector.GetEventInfo(EVENTID);

            result.Add("event", eventdata.ToKeyValuePairs());

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] FindClassifiedsInRegion(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string RegionName = request["REGIONNAME"].ToString();
            Classified[] classifieds = DirectoryServiceConnector.GetClassifiedsInRegion(RegionName);

            int i = 0;
            foreach (Classified classified in classifieds)
            {
                result.Add(ConvertDecString(i), classified.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] GetOfflineMessages(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID PRINCIPALID = UUID.Parse(request["PRINCIPALID"].ToString());
            OfflineMessage[] Messages = OfflineMessagesConnector.GetOfflineMessages(PRINCIPALID);

            int i = 0;
            foreach (OfflineMessage Message in Messages)
            {
                result.Add(ConvertDecString(i), Message.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] AddLandObject(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            AuroraLandData land = new AuroraLandData(request);
            LandData landData = ConvertFromAuroraLandData(land);
            DirectoryServiceConnector.AddLandObject(landData, land.RegionID, land.ForSale, land.EstateID, land.ShowInSearch, land.InfoUUID);

            return SuccessResult();
        }

        private byte[] AddOfflineMessage(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            OfflineMessage message = new OfflineMessage(request);
            OfflineMessagesConnector.AddOfflineMessage(message);

            return SuccessResult();
        }

        private byte[] GetMuteList(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID PRINCIPALID = UUID.Parse(request["PRINCIPALID"].ToString());
            MuteList[] Mutes = MuteListConnector.GetMuteList(PRINCIPALID);

            int i = 0;
            foreach (MuteList Mute in Mutes)
            {
                result.Add(ConvertDecString(i), Mute.ToKeyValuePairs());
                i++;
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] UpdateMute(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            MuteList Mute = new MuteList(request);
            UUID PRINCIPALID = UUID.Parse(request["PRINCIPALID"].ToString());
            MuteListConnector.UpdateMute(Mute, PRINCIPALID);

            return SuccessResult();
        }

        private byte[] DeleteMute(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID MUTEID = UUID.Parse(request["MUTEID"].ToString());
            UUID PRINCIPALID = UUID.Parse(request["PRINCIPALID"].ToString());
            MuteListConnector.DeleteMute(MUTEID, PRINCIPALID);

            return SuccessResult();
        }

        private byte[] IsMuted(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID MUTEID = UUID.Parse(request["MUTEID"].ToString());
            UUID PRINCIPALID = UUID.Parse(request["PRINCIPALID"].ToString());
            bool IsMuted = MuteListConnector.IsMuted(PRINCIPALID, MUTEID);
            result["Muted"] = IsMuted;

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] GetRegionsInEstate(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            int estateID = int.Parse(request["ESTATEID"].ToString());
            List<UUID> regionIDs = EstateConnector.GetRegions(estateID);
            Dictionary<string, object> estateresult = new Dictionary<string, object>();
            int i = 0;
            foreach (UUID regionID in regionIDs)
            {
                estateresult.Add(ConvertDecString(i), regionID);
                i++;
            }
            result["result"] = estateresult;

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] GetEstates(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            string search = request["SEARCH"].ToString();
            List<int> EstateIDs = EstateConnector.GetEstates(search);
            Dictionary<string, object> estateresult = new Dictionary<string, object>();
            int i = 0;
            foreach (int estateID in EstateIDs)
            {
                estateresult.Add(ConvertDecString(i), estateID);
                i++;
            }
            result["result"] = estateresult;

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        private byte[] DeleteEstate(Dictionary<string, object> request)
        {
            int EstateID = int.Parse(request["ESTATEID"].ToString());
            if (EstateConnector.DeleteEstate(EstateID))
                return SuccessResult();
            else
                return FailureResult();
        }

        private byte[] LinkRegionEstate(Dictionary<string, object> request)
        {
            int EstateID = int.Parse(request["ESTATEID"].ToString());
            string Password = request["PASSWORD"].ToString();
            UUID RegionID = new UUID(request["REGIONID"].ToString());
            if (EstateConnector.LinkRegion(RegionID,EstateID,Password))
                return SuccessResult();
            else
                return FailureResult();
        }

        private byte[] StoreEstateSettings(Dictionary<string, object> request)
        {
            //Warning! This services two different methods
            EstateSettings ES = new EstateSettings(request);
            if (EstateConnector.StoreEstateSettings(ES))
                return SuccessResult();
            else
                return FailureResult();
        }

        private byte[] LoadEstateSettings(Dictionary<string, object> request)
        {
            //Warning! This services two different methods
            EstateSettings ES = null;
            if (request.ContainsKey("ESTATEID"))
            {
                int EstateID = int.Parse(request["ESTATEID"].ToString());
                ES = EstateConnector.LoadEstateSettings(EstateID);
            }
            else
            {
                bool create = bool.Parse(request["CREATE"].ToString());
                string regionID = request["REGIONID"].ToString();
                ES = EstateConnector.LoadEstateSettings(new UUID(regionID), create);
            }
            Dictionary<string, object> result = ES.ToKeyValuePairs();
            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        #region Methods

        byte[] UpdateUserNotes(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
            {
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get profile");
                result["result"] = "null";
                string FailedxmlString = ServerUtils.BuildXmlResponse(result);
                //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", FailedxmlString);
                UTF8Encoding Failedencoding = new UTF8Encoding();
                return Failedencoding.GetBytes(FailedxmlString);
            }
            UUID targetID = UUID.Zero;
            if (request.ContainsKey("TARGETID"))
                UUID.TryParse(request["TARGETID"].ToString(), out targetID);
            string notes = "";
            if (request.ContainsKey("NOTES"))
                notes = request["NOTES"].ToString();

            IUserProfileInfo UserProfile = new IUserProfileInfo(request);
            ProfileConnector.UpdateUserNotes(principalID, targetID, notes, UserProfile);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] GetPick(Dictionary<string, object> request)
        {
            string pickID = "";
            if (request.ContainsKey("PICKID"))
                pickID = request["PICKID"].ToString();
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no pickID in request to get pick");

            ProfilePickInfo Pick = ProfileConnector.FindPick(pickID);
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (Pick == null)
                result["result"] = "null";
            else
            {
                result["result"] = Pick.ToKeyValuePairs();
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);

        }

        byte[] DeletePick(Dictionary<string, object> request)
        {
            string pickID = request["PICKID"].ToString();
            string principalID = request["PRINCIPALID"].ToString();

            ProfileConnector.DeletePick(new UUID(pickID), new UUID(principalID));
            return SuccessResult();
        }

        byte[] UpdatePick(Dictionary<string, object> request)
        {
            ProfilePickInfo pick = new ProfilePickInfo(request);
            ProfileConnector.UpdatePick(pick);

            return SuccessResult();
        }

        byte[] AddPick(Dictionary<string, object> request)
        {
            ProfilePickInfo pick = new ProfilePickInfo(request);
            ProfileConnector.AddPick(pick);

            return SuccessResult();
        }

        byte[] GetClassified(Dictionary<string, object> request)
        {
            string classifiedID = "";
            if (request.ContainsKey("CLASSIFIEDID"))
                classifiedID = request["CLASSIFIEDID"].ToString();
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no classifiedID in request to get classifed");

            Classified Classified = ProfileConnector.FindClassified(classifiedID);
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (Classified == null)
                result["result"] = "null";
            else
            {
                result["result"] = Classified.ToKeyValuePairs();
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] DeleteClassified(Dictionary<string, object> request)
        {
            string classifiedID = request["CLASSIFIEDID"].ToString();
            string principalID = request["PRINCIPALID"].ToString();

            ProfileConnector.DeleteClassified(new UUID(classifiedID), new UUID(principalID));
            return SuccessResult();
        }

        byte[] AddClassified(Dictionary<string, object> request)
        {
            Classified Classified = new Classified(request);
            ProfileConnector.AddClassified(Classified);

            return SuccessResult();
        }

        byte[] GetProfile(Dictionary<string, object> request)
        {
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get profile");

            IUserProfileInfo UserProfile = ProfileConnector.GetUserProfile(principalID);
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (UserProfile == null)
                result["result"] = "null";
            else
            {
                result["result"] = UserProfile.ToKeyValuePairs();
            }
             
            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] UpdateProfile(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
            {
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get profile");
                result["result"] = "null";
                string FailedxmlString = ServerUtils.BuildXmlResponse(result);
                m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", FailedxmlString);
                UTF8Encoding Failedencoding = new UTF8Encoding();
                return Failedencoding.GetBytes(FailedxmlString);
            }

            IUserProfileInfo UserProfile = new IUserProfileInfo(request);
            ProfileConnector.UpdateUserProfile(UserProfile);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] UpdateInterests(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
            {
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get profile");
                result["result"] = "null";
                string FailedxmlString = ServerUtils.BuildXmlResponse(result);
                m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", FailedxmlString);
                UTF8Encoding Failedencoding = new UTF8Encoding();
                return Failedencoding.GetBytes(FailedxmlString);
            }

            IUserProfileInfo UserProfile = new IUserProfileInfo(request);
            ProfileConnector.UpdateUserInterests(UserProfile);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] CreateProfile(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get profile");

            ProfileConnector.CreateNewProfile(principalID);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] RemoveFromCache(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get profile");

            ProfileConnector.RemoveFromCache(principalID);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] GetAgent(Dictionary<string, object> request)
        {
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get agent");

            IAgentInfo Agent = AgentConnector.GetAgent(principalID);
            Dictionary<string, object> result = new Dictionary<string, object>();
            if (Agent == null)
                result["result"] = "null";
            else
            {
                result["result"] = Agent.ToKeyValuePairs();
            }

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] UpdateAgent(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();

            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
            {
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to update agent");
                result["result"] = "null";
                string FailedxmlString = ServerUtils.BuildXmlResponse(result);
                m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", FailedxmlString);
                UTF8Encoding Failedencoding = new UTF8Encoding();
                return Failedencoding.GetBytes(FailedxmlString);
            }

            IAgentInfo Agent = new IAgentInfo(request);
            AgentConnector.UpdateAgent(Agent);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] CreateAgent(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            UUID principalID = UUID.Zero;
            if (request.ContainsKey("PRINCIPALID"))
                UUID.TryParse(request["PRINCIPALID"].ToString(), out principalID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no principalID in request to get profile");

            AgentConnector.CreateNewAgent(principalID);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] RemoveTelehub(Dictionary<string, object> request)
        {
            Dictionary<string, object> result = new Dictionary<string, object>();
            UUID regionID = UUID.Zero;
            if (request.ContainsKey("REGIONID"))
                UUID.TryParse(request["REGIONID"].ToString(), out regionID);
            else
                m_log.WarnFormat("[AuroraDataServerPostHandler]: no regionID in request to remove telehub");

            GridConnector.RemoveTelehub(regionID);
            result["result"] = "Successful";

            string xmlString = ServerUtils.BuildXmlResponse(result);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        byte[] AddTelehub(Dictionary<string, object> request)
        {
            Telehub telehub = new Telehub(request);
            GridConnector.AddTelehub(telehub);

            return SuccessResult();
        }

        byte[] FindTelehub(Dictionary<string, object> request)
        {
            UUID regionID = UUID.Zero;
            UUID.TryParse(request["REGIONID"].ToString(), out regionID);

            Dictionary<string, object> result = new Dictionary<string, object>();
            Telehub telehub = GridConnector.FindTelehub(regionID);
            if(telehub != null)
                result = telehub.ToKeyValuePairs();
            string xmlString = ServerUtils.BuildXmlResponse(result);
            //m_log.DebugFormat("[AuroraDataServerPostHandler]: resp string: {0}", xmlString);
            UTF8Encoding encoding = new UTF8Encoding();
            return encoding.GetBytes(xmlString);
        }

        #endregion

        #region Misc

        private byte[] SuccessResult()
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Success"));

            rootElement.AppendChild(result);

            return DocToBytes(doc);
        }

        private byte[] FailureResult()
        {
            return FailureResult(String.Empty);
        }

        private byte[] FailureResult(string msg)
        {
            XmlDocument doc = new XmlDocument();

            XmlNode xmlnode = doc.CreateNode(XmlNodeType.XmlDeclaration,
                    "", "");

            doc.AppendChild(xmlnode);

            XmlElement rootElement = doc.CreateElement("", "ServerResponse",
                    "");

            doc.AppendChild(rootElement);

            XmlElement result = doc.CreateElement("", "Result", "");
            result.AppendChild(doc.CreateTextNode("Failure"));

            rootElement.AppendChild(result);

            XmlElement message = doc.CreateElement("", "Message", "");
            message.AppendChild(doc.CreateTextNode(msg));

            rootElement.AppendChild(message);

            return DocToBytes(doc);
        }

        private byte[] DocToBytes(XmlDocument doc)
        {
            MemoryStream ms = new MemoryStream();
            XmlTextWriter xw = new XmlTextWriter(ms, null);
            xw.Formatting = Formatting.Indented;
            doc.WriteTo(xw);
            xw.Flush();

            return ms.ToArray();
        }

        // http://social.msdn.microsoft.com/forums/en-US/csharpgeneral/thread/68f7ca38-5cd1-411f-b8d4-e4f7a688bc03
        // By: A Million Lemmings
        public string ConvertDecString(int dvalue)
        {

            string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            string retVal = string.Empty;

            double value = Convert.ToDouble(dvalue);

            do
            {

                double remainder = value - (26 * Math.Truncate(value / 26));

                retVal = retVal + CHARS.Substring((int)remainder, 1);

                value = Math.Truncate(value / 26);

            } 
            while (value > 0);



            return retVal;

        }

        private LandData ConvertFromAuroraLandData(AuroraLandData data)
        {
            LandData adata = new LandData();
            adata.Area = data.Area;
            adata.AuctionID = data.AuctionID;
            adata.AuthBuyerID = data.AuthBuyerID;
            adata.Category = data.Category;
            adata.ClaimDate = data.ClaimDate;
            adata.ClaimPrice = data.ClaimPrice;
            adata.Description = data.Description;
            adata.Dwell = data.Dwell;
            adata.Flags = data.Flags;
            adata.GroupID = data.GroupID;
            adata.LandingType = data.LandingType;
            Vector3 Pos = new Vector3(data.LandingX, data.LandingY, data.LandingZ);
            adata.UserLocation = Pos;
            adata.LocalID = data.LocalID;
            Vector3 LookAt = new Vector3(data.LookAtX, data.LookAtY, data.LookAtZ);
            adata.UserLookAt = LookAt;
            adata.Maturity = data.Maturity;
            adata.Name = data.Name;
            adata.OwnerID = data.OwnerID;
            adata.GlobalID = data.ParcelID;
            adata.RegionID = data.RegionID;
            adata.SalePrice = (int)data.SalePrice;
            adata.SnapshotID = data.SnapshotID;
            adata.Status = data.Status;
            return adata;
        }

        #endregion
    }
}
