﻿using SEWorldGenPlugin.Draw;
using SEWorldGenPlugin.Generator;
using SEWorldGenPlugin.ObjectBuilders;
using System;
using VRageMath;

namespace SEWorldGenPlugin.GUI.AdminMenu.SubMenus.StarSystemDesigner
{
    /// <summary>
    /// A class that is used to render a planet and its orbit into the scene
    /// </summary>
    public class MyPlanetOrbitRenderObject : MyAbstractStarSystemDesignerRenderObject
    {
        /// <summary>
        /// The parent object the orbit goes around
        /// </summary>
        private MySystemObject m_parent;

        /// <summary>
        /// The render object used to render the planets orbit
        /// </summary>
        private RenderCircle m_orbitRender;

        /// <summary>
        /// The render object used to render the planets sphere
        /// </summary>
        private RenderSphere m_planetRender;

        public MyPlanetOrbitRenderObject(MySystemPlanet planet) : base(planet)
        {
            m_parent = MyStarSystemGenerator.Static.StarSystem.GetById(planet.ParentId);
            double rad = CalculateRadius();
            m_orbitRender = new RenderCircle(CalculateWorldMatrix(), (float)rad, Color.Blue.ToVector4());
            m_planetRender = new RenderSphere(planet.CenterPosition, (float)planet.Diameter / 2, Color.Green.ToVector4());
        }

        public override void Draw()
        {
            m_orbitRender.Draw();
            m_planetRender.Draw();
        }

        public override double GetObjectRenderSize()
        {
            return 0;
        }

        /// <summary>
        /// Calculates the radius of this orbit
        /// </summary>
        /// <returns></returns>
        private double CalculateRadius()
        {
            if(m_parent == null)
            {
                return Vector3D.Distance(Vector3D.Zero, RenderObject.CenterPosition);
            }
            else
            {
                return Vector3D.Distance(m_parent.CenterPosition, RenderObject.CenterPosition);
            }
        }

        /// <summary>
        /// Calculates a world matrix for this orbit
        /// </summary>
        /// <returns>The world matrix</returns>
        private MatrixD CalculateWorldMatrix()
        {
            Vector3D center = Vector3D.Zero;
            if(m_parent != null)
            {
                center = m_parent.CenterPosition;
            }

            Vector3D forward = Vector3D.Subtract(RenderObject.CenterPosition, center);
            Vector3D fn = Vector3D.Normalize(forward);
            double radius = CalculateRadius();

            double elevation = Math.Asin(forward.Z / radius);
            double orbitRad = Math.Acos(forward.X / Math.Cos(elevation) / radius);
            
            MatrixD my = MatrixD.CreateRotationY(elevation);
            MatrixD mz = MatrixD.CreateRotationZ(orbitRad);

            Vector3D up = new Vector3D(0, 0, 1);
            up = Vector3D.Rotate(up, my * mz);

            return MatrixD.CreateWorld(center, fn, up);
        }
    }
}