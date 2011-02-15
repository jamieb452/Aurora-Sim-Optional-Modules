﻿/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using log4net;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using GlynnTucker.Cache;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using Aurora.Simulation.Base;

namespace OpenSim.Region.CoreModules.Asset
{
    public class GlynnTuckerAssetCache : IService, IImprovedAssetCache
    {
        #region Declares

        private static readonly ILog m_log =
                LogManager.GetLogger(
                MethodBase.GetCurrentMethod().DeclaringType);

        private ICache m_Cache;
        private ulong m_Hits;
        private ulong m_Requests;

        // Instrumentation
        private uint m_DebugRate;

        public string Name
        {
            get { return "GlynnTuckerAssetCache"; }
        }

        #endregion

        #region IService Members

        public void Initialize(IConfigSource config, IRegistryCore registry)
        {
            IConfig moduleConfig = config.Configs["Modules"];

            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("AssetCaching");
                //m_log.DebugFormat("[ASSET CACHE] name = {0} (this module's name: {1}). Sync? ", name, Name, m_Cache.IsSynchronized);

                if (name == Name)
                {
                    m_Cache = new GlynnTucker.Cache.SimpleMemoryCache();
                    
                    m_log.Info("[ASSET CACHE]: GlynnTucker asset cache enabled");

                    // Instrumentation
                    IConfig cacheConfig = config.Configs["AssetCache"];
                    if (cacheConfig != null)
                        m_DebugRate = (uint)cacheConfig.GetInt("DebugRate", 0);
                    registry.RegisterModuleInterface<IImprovedAssetCache>(this);
                }
            }
        }

        public void Start(IConfigSource config, IRegistryCore registry)
        {
        }

        public void PostStart(IConfigSource config, IRegistryCore registry)
        {
        }

        public void AddNewRegistry(IConfigSource config, IRegistryCore registry)
        {
            IConfig moduleConfig = config.Configs["Modules"];

            if (moduleConfig != null)
            {
                string name = moduleConfig.GetString("AssetCaching");
                //m_log.DebugFormat("[XXX] name = {0} (this module's name: {1}", name, Name);

                if (name == Name)
                {
                    registry.RegisterModuleInterface<IImprovedAssetCache>(this);
                }
            }
        }

        #endregion

        #region IImprovedAssetCache

        ////////////////////////////////////////////////////////////
        // IImprovedAssetCache
        //

        public void Cache(AssetBase asset)
        {
            if (asset != null)
                m_Cache.AddOrUpdate(asset.ID, asset);
        }

        public AssetBase Get(string id)
        {
            Object asset = null;
            m_Cache.TryGet(id, out asset);

            Debug(asset);

            return (AssetBase)asset;
        }

        public void Expire(string id)
        {
            Object asset = null;
            if (m_Cache.TryGet(id, out asset))
                m_Cache.Remove(id);
        }

        public void Clear()
        {
            m_Cache.Clear();
        }

        private void Debug(Object asset)
        {
            // Temporary instrumentation to measure the hit/miss rate
            if (m_DebugRate > 0)
            {
                ++m_Requests;
                if (asset != null)
                    ++m_Hits;

                if ((m_Requests % m_DebugRate) == 0)
                    m_log.DebugFormat("[ASSET CACHE]: Hit Rate {0} / {1} == {2}%", m_Hits, m_Requests, ((float)m_Hits / (float)m_Requests) * 100.0f);
            }
            // End instrumentation
        }

        #endregion
    }
}
