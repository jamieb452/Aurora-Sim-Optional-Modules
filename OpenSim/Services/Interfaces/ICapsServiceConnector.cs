﻿using System;
using System.Collections.Generic;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenMetaverse;
using OpenMetaverse.StructuredData;

namespace OpenSim.Services.Interfaces
{
    public interface IPrivateCapsService
    {
        void AddCAPS(string method, string caps);
        void Initialise();
        string CapsRequest(string request, string path, string param,
                                  OSHttpRequest httpRequest, OSHttpResponse httpResponse);
        OSDMap PostToSendToSim { get; set; }
        string GetCAPS(string method);
        string CreateCAPS(string method);
        IPresenceService PresenceService { get; }
        IInventoryService InventoryService { get; }
        ILibraryService LibraryService { get; }
        IGridUserService GridUserService { get; }
        IGridService GridService { get; }
        string SimToInform { get; }
        string HostName { get; }
        ICAPSPublicHandler PublicHandler { get; }
        ulong RegionHandle { get; }
    }

    public interface ICAPSPublicHandler
    {
        void AddCapsService(IPrivateCapsService handler, string CAPS);
        IPrivateCapsService GetCapsService(ulong regionID);
    }

    public interface ICapsServiceConnector
    {
        List<IRequestHandler> RegisterCaps(UUID agentID, IHttpServer server, IPrivateCapsService handler); 
    }
}
