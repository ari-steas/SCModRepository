﻿using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace SC.SUGMA.GameState
{
    internal class PlayerTracker : ComponentBase
    {
        public static PlayerTracker I;
        public readonly Dictionary<long, IMyPlayer> AllPlayers = new Dictionary<long, IMyPlayer>();

        #region Base Methods

        public override void Init(string id)
        {
            base.Init(id);
            I = this;

            UpdatePlayers();
        }

        public override void Close()
        {
            I = null;
        }

        private int _ticks = 0;

        public override void UpdateTick()
        {
            _ticks++;

            if (_ticks % 59 == 0)
                UpdatePlayers();
        }

        #endregion

        #region Public Methods

        public IMyFaction GetGridFaction(IMyCubeGrid grid)
        {
            if (grid.BigOwners.Count == 0)
                return null;

            return MyAPIGateway.Session.Factions.TryGetPlayerFaction(grid.BigOwners[0]);
        }

        #endregion

        private void UpdatePlayers()
        {
            List<IMyPlayer> allPlayersList = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(allPlayersList);

            AllPlayers.Clear();
            foreach (var player in allPlayersList)
            {
                AllPlayers.Add(player.IdentityId, player);
            }
        }
    }
}