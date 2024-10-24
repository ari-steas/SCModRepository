﻿using System.Collections.Generic;
using FusionSystems.Communication;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace FusionSystems.HeatParts
{
    internal class HeatSystem
    {
        private const float HeatCapacityPerSink = 60;
        private const float HeatDissipationPerRadiator = 1.5f;

        private static ModularDefinitionApi ModularApi => ModularDefinition.ModularApi;

        public GridHeatManager Parent;

        public int AssemblyId;
        public int BlockCount { get; private set; } = 0;

        private List<IMyCubeBlock> _radiatorBlocks = new List<IMyCubeBlock>();

        private HashSet<IMyCubeBlock> _occludedRadiators = new HashSet<IMyCubeBlock>();
        private HashSet<IMyCubeBlock> _visibleRadiators = new HashSet<IMyCubeBlock>();

        /// <summary>
        /// Total heat dispersed per second by this assembly.
        /// </summary>
        public float HeatDissipation
        {
            get
            {
                return ModularApi.GetAssemblyProperty<float>(AssemblyId, "HeatDissipation");
            }
            private set
            {
                ModularApi.SetAssemblyProperty(AssemblyId, "HeatDissipation", value);
            }
        }

        /// <summary>
        /// Total heat able to be stored by this assembly.
        /// </summary>
        public float HeatCapacity
        {
            get
            {
                return ModularApi.GetAssemblyProperty<float>(AssemblyId, "HeatCapacity");
            }
            private set
            {
                ModularApi.SetAssemblyProperty(AssemblyId, "HeatCapacity", value);
            }
        }
        

        public HeatSystem(int assemblyId, GridHeatManager parent)
        {
            AssemblyId = assemblyId;
            Parent = parent;

            HeatDissipation = 0;
            HeatCapacity = 0;

            Parent.Grid.OnBlockAdded += UpdateLoS;
            Parent.Grid.OnBlockRemoved += UpdateLoS;
        }

        public void OnBlockAdd(IMyCubeBlock block)
        {
            switch (block.BlockDefinition.SubtypeName)
            {
                case "Heat_Heatsink":
                    HeatCapacity += HeatCapacityPerSink;
                    Parent.HeatCapacity += HeatCapacityPerSink;
                    break;
                case "Heat_FlatRadiator":
                    _radiatorBlocks.Add(block);
                    _visibleRadiators.Add(block);
                    HeatDissipation += HeatDissipationPerRadiator;
                    Parent.HeatDissipation += HeatDissipationPerRadiator;
                    DoLoSCheck(block);
                    break;
            }

            BlockCount++;
        }

        public void OnBlockRemove(IMyCubeBlock block)
        {
            switch (block.BlockDefinition.SubtypeName)
            {
                case "Heat_Heatsink":
                    HeatCapacity -= HeatCapacityPerSink;
                    Parent.HeatCapacity -= HeatCapacityPerSink;
                    break;
                case "Heat_FlatRadiator":
                    _radiatorBlocks.Remove(block);
                    _occludedRadiators.Remove(block);
                    if (_visibleRadiators.Remove(block))
                    {
                        HeatDissipation -= HeatDissipationPerRadiator;
                        Parent.HeatDissipation -= HeatDissipationPerRadiator;
                    }
                    break;
            }

            BlockCount--;
        }

        public void OnClose()
        {
            Parent.Grid.OnBlockAdded -= UpdateLoS;
            Parent.Grid.OnBlockRemoved -= UpdateLoS;
        }

        private void UpdateLoS(IMySlimBlock newBlock)
        {
            Quaternion radQuaternion;
            foreach (var radiator in _radiatorBlocks)
            {
                radiator.Orientation.GetQuaternion(out radQuaternion);

                Vector3I offset = (Vector3I) (radQuaternion * (newBlock.Position - radiator.Position));

                // If block is in front of radiator panel
                if (offset.X == 0 && offset.Y == 0 && offset.Z > 0)
                {
                    DoLoSCheck(radiator);
                }
            }
        }

        private readonly List<Vector3I> _cellPositions = new List<Vector3I>();
        private void DoLoSCheck(IMyCubeBlock radiatorBlock)
        {
            MatrixD blockMatrix = radiatorBlock.WorldMatrix;
            float gridMaxExtents = Vector3.Distance(Parent.Grid.Max, Parent.Grid.Min) * Parent.Grid.GridSize;

            bool doesIntersect = false;

            if (ModularApi.IsDebug())
                DebugDraw.DebugDraw.AddLine(blockMatrix.Translation,
                    blockMatrix.Translation + blockMatrix.Backward * gridMaxExtents, Color.Bisque, 2);

            Parent.Grid.RayCastCells(blockMatrix.Translation,
                blockMatrix.Translation + blockMatrix.Backward * gridMaxExtents, _cellPositions);

            foreach (var cellPosition in _cellPositions)
            {
                if (cellPosition == radiatorBlock.Position || !Parent.Grid.CubeExists(cellPosition))
                    continue;

                doesIntersect = true;
                break;
            }
            _cellPositions.Clear();

            if (doesIntersect)
            {
                _visibleRadiators.Remove(radiatorBlock);
                if (_occludedRadiators.Add(radiatorBlock))
                {
                    HeatDissipation -= HeatDissipationPerRadiator;
                    Parent.HeatDissipation -= HeatDissipationPerRadiator;
                }
            }
            else
            {
                _occludedRadiators.Remove(radiatorBlock);
                if (_visibleRadiators.Add(radiatorBlock))
                {
                    HeatDissipation += HeatDissipationPerRadiator;
                    Parent.HeatDissipation += HeatDissipationPerRadiator;
                }
            }
        }
    }
}
