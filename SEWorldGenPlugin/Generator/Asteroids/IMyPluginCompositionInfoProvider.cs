﻿using Sandbox.Game.World.Generator;
using VRage.Game;

/*
 * Code is taken from the Space Engineers code base. 
 */
namespace SEWorldGenPlugin.Generator.Asteroids
{
    internal interface IMyPluginCompositionInfoProvider
    {
        IMyCompositeDeposit[] Deposits
        {
            get;
        }

        IMyCompositeShape[] FilledShapes
        {
            get;
        }

        IMyCompositeShape[] RemovedShapes
        {
            get;
        }

        MyVoxelMaterialDefinition DefaultMaterial
        {
            get;
        }

        void Close();
    }
}