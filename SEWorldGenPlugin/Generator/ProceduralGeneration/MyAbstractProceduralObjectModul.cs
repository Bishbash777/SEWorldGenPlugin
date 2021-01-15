﻿using Sandbox.Game.World.Generator;

namespace SEWorldGenPlugin.Generator.ProceduralGeneration
{
    /// <summary>
    /// Abstract class used to generate objects, that are not
    /// cell based, but world based. This means, once an object can get
    /// generated by this module, it will, regardless if any player or grid
    /// is in range of it.
    /// </summary>
    public abstract class MyAbstractProceduralObjectModul : IMyProceduralGeneratorModule
    {
        protected int m_seed;

        public MyAbstractProceduralObjectModul(int seed)
        {
            m_seed = seed;
        }

        /// <summary>
        /// Generates all objects, that this module should generate
        /// </summary>
        public abstract void GenerateObjects();

        public abstract void UpdateGpsForPlayer(MyEntityTracker entity);
    }
}